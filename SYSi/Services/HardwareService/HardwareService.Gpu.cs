namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    // ── Public API ───────────────────────────────────────────────────────────

    public List<GpuInfo> GetGpuInfoList()
    {
        var list = new List<GpuInfo>();
        try
        {
            var displayGuid = new Guid("4D36E968-E325-11CE-BFC1-08002BE10318");
            IntPtr devInfo  = NativeMethods.SetupDiGetClassDevs(
                ref displayGuid, IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT);

            if (devInfo == new IntPtr(-1)) return list;
            try
            {
                var devData = new NativeMethods.SP_DEVINFO_DATA
                    { cbSize = (uint)Marshal.SizeOf<NativeMethods.SP_DEVINFO_DATA>() };

                for (uint i = 0; NativeMethods.SetupDiEnumDeviceInfo(devInfo, i, ref devData); i++)
                {
                    string name      = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_DEVICEDESC);
                    string mfg       = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_MFG);
                    string hwId      = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_HARDWAREID);
                    string driverKey = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_DRIVER);

                    var gpu = new GpuInfo
                    {
                        Name        = name,
                        Manufacturer = ParseGpuManufacturer(mfg, name),
                        PnpDeviceId  = hwId.Split('\0')[0],  // MULTI_SZ — first string only
                    };

                    EnrichGpuFromRegistry(gpu, driverKey);
                    list.Add(gpu);
                }
            }
            finally { NativeMethods.SetupDiDestroyDeviceInfoList(devInfo); }

            EnrichGpuWithDisplayInfo(list);
            RefreshGpuUsage(list);
        }
        catch { }
        return list;
    }

    // ── Registry enrichment ──────────────────────────────────────────────────

    private static void EnrichGpuFromRegistry(GpuInfo gpu, string driverKey)
    {
        if (driverKey == "N/A") return;
        try
        {
            string regPath = $@"SYSTEM\CurrentControlSet\Control\Class\{driverKey}";
            using var key  = Registry.LocalMachine.OpenSubKey(regPath);
            if (key == null) return;

            gpu.DriverVersion = key.GetValue("DriverVersion")?.ToString() ?? "N/A";
            gpu.DriverDate    = key.GetValue("DriverDate")?.ToString()    ?? "N/A";

            gpu.VramText       = ReadVram(key);
            gpu.VideoProcessor = RegistryString(key.GetValue("HardwareInformation.ChipType"));
            gpu.VideoArchitecture = RegistryString(key.GetValue("HardwareInformation.DacType"));

            object? memTypeObj = key.GetValue("HardwareInformation.MemoryType");
            gpu.VideoMemoryType = memTypeObj switch
            {
                string s => s,
                byte[] b => Encoding.Unicode.GetString(b).TrimEnd('\0'),
                int n when n > 0 => ParseVideoMemoryType(n),
                _ => "N/A",
            };
        }
        catch { }
    }

    private static string ReadVram(RegistryKey key)
    {
        // Try QWORD first (8-byte binary or long)
        object? qw = key.GetValue("HardwareInformation.qwMemorySize");
        long bytes = qw switch
        {
            long l              => l,
            byte[] b when b.Length >= 8 => (long)BitConverter.ToUInt64(b, 0),
            int i               => (long)(uint)i,
            _                   => 0,
        };

        // Fallback: DWORD MemorySize
        if (bytes <= 0)
        {
            object? dw = key.GetValue("HardwareInformation.MemorySize");
            if (dw is int di) bytes = (long)(uint)di;
        }

        // Fallback: Intel shared memory stored under subkey "0000"
        if (bytes <= 0)
        {
            using var sub = key.OpenSubKey("0000");
            if (sub?.GetValue("HardwareInformation.qwMemorySize") is byte[] sb && sb.Length >= 8)
                bytes = (long)BitConverter.ToUInt64(sb, 0);
        }

        return bytes > 0 ? FormatBytes(bytes) : "Shared";
    }

    // ── Display info (resolution, refresh rate, bit depth) ───────────────────

    private static void EnrichGpuWithDisplayInfo(List<GpuInfo> gpus)
    {
        if (gpus.Count == 0) return;
        try
        {
            var dd = new NativeMethods.DISPLAY_DEVICE
                { cb = (uint)Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };

            for (uint i = 0; NativeMethods.EnumDisplayDevices(null, i, ref dd, 0); i++)
            {
                const uint MIRRORING = 0x00000008;
                if ((dd.StateFlags & MIRRORING) != 0) continue;

                var dm = default(NativeMethods.DEVMODE);
                dm.dmSize = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>();

                if (!NativeMethods.EnumDisplaySettings(dd.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm))
                    continue;

                // Match by name; fall back to first GPU when there is only one adapter
                string adapterName = dd.DeviceString.Trim();
                var match = gpus.FirstOrDefault(g =>
                    adapterName.Contains(g.Name, StringComparison.OrdinalIgnoreCase) ||
                    g.Name.Contains(adapterName, StringComparison.OrdinalIgnoreCase))
                    ?? (gpus.Count == 1 ? gpus[0] : null);

                if (match == null || !string.IsNullOrEmpty(match.Resolution)) continue;

                match.Resolution   = $"{dm.dmPelsWidth} × {dm.dmPelsHeight}";
                match.RefreshRate  = $"{dm.dmDisplayFrequency} Hz";
                match.BitsPerPixel = $"{dm.dmBitsPerPel} bit";
            }
        }
        catch { }
    }

    // ── GPU usage via PDH ────────────────────────────────────────────────────

    private static readonly object _gpuPdhLock = new();
    private static IntPtr _gpuQuery   = IntPtr.Zero;
    private static IntPtr _gpuCounter = IntPtr.Zero;
    private static bool   _gpuPdhReady;

    public void InitGpuPdh()
    {
        lock (_gpuPdhLock)
        {
            if (_gpuPdhReady) return;
            try
            {
                if (NativeMethods.PdhOpenQuery(null, IntPtr.Zero, out _gpuQuery) != 0) return;

                uint r = NativeMethods.PdhAddCounter(
                    _gpuQuery,
                    @"\GPU Engine(*engtype_3D)\Utilization Percentage",
                    IntPtr.Zero,
                    out _gpuCounter);

                if (r != 0) return;

                // First collect seeds the delta — second collect in RefreshGpuUsage gives real data.
                NativeMethods.PdhCollectQueryData(_gpuQuery);
                _gpuPdhReady = true;
            }
            catch { }
        }
    }

    public void RefreshGpuUsage(List<GpuInfo> gpus)
    {
        if (!_gpuPdhReady) InitGpuPdh();
        if (!_gpuPdhReady || _gpuCounter == IntPtr.Zero) return;

        try
        {
            lock (_gpuPdhLock)
            {
                NativeMethods.PdhCollectQueryData(_gpuQuery);

                uint bufSize = 0, itemCount = 0;
                NativeMethods.PdhGetFormattedCounterArray(
                    _gpuCounter, NativeMethods.PDH_FMT_DOUBLE, ref bufSize, out itemCount, IntPtr.Zero);

                if (bufSize == 0) return;

                IntPtr buf = Marshal.AllocHGlobal((int)bufSize);
                try
                {
                    uint r = NativeMethods.PdhGetFormattedCounterArray(
                        _gpuCounter, NativeMethods.PDH_FMT_DOUBLE, ref bufSize, out itemCount, buf);

                    if (r != 0) return;

                    // Accumulate usage per physical GPU index embedded in the instance name:
                    // "luid_0x00000000_0x00013B6C_phys_0_eng_0_engtype_3D"
                    var physUsage = new Dictionary<int, double>();
                    int itemSize  = Marshal.SizeOf<NativeMethods.PDH_FMT_COUNTERVALUE_ITEM>();

                    for (int j = 0; j < (int)itemCount; j++)
                    {
                        var item    = Marshal.PtrToStructure<NativeMethods.PDH_FMT_COUNTERVALUE_ITEM>(
                            IntPtr.Add(buf, j * itemSize));
                        int physIdx = ParsePhysIndex(item.szName);
                        if (physIdx < 0) physIdx = 0;

                        physUsage.TryGetValue(physIdx, out double cur);
                        physUsage[physIdx] = cur + item.FmtValue.doubleValue;
                    }

                    foreach (var (idx, usage) in physUsage)
                        if (idx < gpus.Count)
                            gpus[idx].UsagePercent = Math.Round(Math.Clamp(usage, 0, 100), 1);
                }
                finally { Marshal.FreeHGlobal(buf); }
            }
        }
        catch { }
    }

    private static void DisposeGpuPdh()
    {
        lock (_gpuPdhLock)
        {
            if (_gpuQuery == IntPtr.Zero) return;
            NativeMethods.PdhCloseQuery(_gpuQuery);
            _gpuQuery    = IntPtr.Zero;
            _gpuPdhReady = false;
        }
    }

    // ── Small helpers ────────────────────────────────────────────────────────

    /// <summary>Parses the "phys_N" segment from a PDH GPU Engine instance name.</summary>
    private static int ParsePhysIndex(string instanceName)
    {
        const string marker = "_phys_";
        int pos = instanceName.IndexOf(marker, StringComparison.Ordinal);
        if (pos < 0) return -1;

        int start = pos + marker.Length;
        int end   = instanceName.IndexOf('_', start);
        string num = end < 0 ? instanceName[start..] : instanceName[start..end];
        return int.TryParse(num, out int n) ? n : -1;
    }

    private static string ParseGpuManufacturer(string mfg, string name)
    {
        foreach (string s in new[] { mfg, name })
        {
            if (s.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
            if (s.Contains("Intel",  StringComparison.OrdinalIgnoreCase)) return "Intel";
            if (s.Contains("AMD",    StringComparison.OrdinalIgnoreCase) ||
                s.Contains("ATI",    StringComparison.OrdinalIgnoreCase)) return "AMD";
        }
        return string.IsNullOrWhiteSpace(mfg) ? "N/A" : mfg;
    }

    private static string ParseVideoMemoryType(int val) => val switch
    {
        1 => "Other",
        2 => "Unknown",
        3 => "VRAM",
        4 => "DRAM",
        5 => "SRAM",
        6 => "WRAM",
        7 => "EDO RAM",
        8 => "Burst Synchronous DRAM",
        9 => "Pipelined Burst SRAM",
        10 => "CDRAM",
        11 => "3DRAM",
        12 => "SDRAM",
        13 => "SGRAM",
        _ => $"Type {val}",
    };
}
