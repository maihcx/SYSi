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
            IntPtr devInfo = NativeMethods.SetupDiGetClassDevs(
                ref displayGuid, IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT);

            if (devInfo == new IntPtr(-1)) return list;
            try
            {
                var devData = new NativeMethods.SP_DEVINFO_DATA
                { cbSize = (uint)Marshal.SizeOf<NativeMethods.SP_DEVINFO_DATA>() };

                for (uint i = 0; NativeMethods.SetupDiEnumDeviceInfo(devInfo, i, ref devData); i++)
                {
                    string name = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_DEVICEDESC);
                    string mfg = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_MFG);
                    string hwId = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_HARDWAREID);
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
            using var key = Registry.LocalMachine.OpenSubKey(regPath);
            if (key == null) return;

            gpu.DriverVersion = key.GetValue("DriverVersion")?.ToString() ?? "N/A";
            gpu.DriverDate    = key.GetValue("DriverDate")?.ToString()    ?? "N/A";

            gpu.VramText       = ReadVram(key);
            gpu.VideoProcessor = RegistryString(key.GetValue("HardwareInformation.ChipType"));

            var (vendorId, deviceId) = ParsePciIds(gpu.PnpDeviceId);
            gpu.VideoArchitecture = LookupGpuArchitecture(vendorId, deviceId);

            object? memTypeObj = key.GetValue("HardwareInformation.MemoryType");
            gpu.VideoMemoryType = memTypeObj switch
            {
                string s => s,
                byte[] b => Encoding.Unicode.GetString(b).TrimEnd('\0'),
                int n when n > 0 => ParseVideoMemoryType(n),
                _ => "N/A",
            };

            if (gpu.VideoMemoryType == "N/A" || string.IsNullOrEmpty(gpu.VideoMemoryType))
            {
                gpu.VideoMemoryType = LookupVramType(vendorId, deviceId);
            }
        }
        catch { }
    }

    private static string ReadVram(RegistryKey key)
    {
        // Try QWORD first (8-byte binary or long)
        object? qw = key.GetValue("HardwareInformation.qwMemorySize");
        long bytes = qw switch
        {
            long l => l,
            byte[] b when b.Length >= 8 => (long)BitConverter.ToUInt64(b, 0),
            int i => (long)(uint)i,
            _ => 0,
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
    private static IntPtr _gpuQuery = IntPtr.Zero;
    private static IntPtr _gpuCounter = IntPtr.Zero;
    private static bool _gpuPdhReady;

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

                uint bufSize = 0;
                uint itemCount = 0;

                // 1st call — get required size (returns PDH_MORE_DATA = 0x800007D2)
                NativeMethods.PdhGetFormattedCounterArray(
                    _gpuCounter, NativeMethods.PDH_FMT_DOUBLE,
                    ref bufSize, out itemCount, IntPtr.Zero);

                if (bufSize == 0) return;

                IntPtr buf = Marshal.AllocHGlobal((int)bufSize);
                try
                {
                    uint r = NativeMethods.PdhGetFormattedCounterArray(
                        _gpuCounter, NativeMethods.PDH_FMT_DOUBLE,
                        ref bufSize, out itemCount, buf);

                    // PDH_CSTATUS_VALID_DATA = 0, PDH_MORE_DATA = 0x800007D2
                    if (r != 0 && r != 0x800007D2) return;

                    var physUsage = new Dictionary<int, double>();

                    // PDH_FMT_COUNTERVALUE_ITEM_W layout (Windows actual):
                    //   szName  : pointer to wide string  (8 bytes on x64, 4 on x86)
                    //   CStatus : uint   (4 bytes)
                    //   padding : uint   (4 bytes, alignment)
                    //   Value   : double (8 bytes)
                    // Total per item: IntPtr.Size + 16 bytes
                    int ptrSize = IntPtr.Size;          // 8 (x64) or 4 (x86)
                    int itemSize = ptrSize + 16;          // szName ptr + CStatus + pad + double

                    for (int j = 0; j < (int)itemCount; j++)
                    {
                        IntPtr itemPtr = IntPtr.Add(buf, j * itemSize);

                        // szName là pointer trỏ vào tên instance (nằm đâu đó trong buf)
                        IntPtr namePtr = Marshal.ReadIntPtr(itemPtr);
                        string name = namePtr != IntPtr.Zero
                            ? Marshal.PtrToStringUni(namePtr) ?? ""
                            : "";

                        // double value nằm ở offset: ptrSize + 8 (CStatus uint + 4 pad)
                        double value = Marshal.PtrToStructure<double>(
                            IntPtr.Add(itemPtr, ptrSize + 8));

                        int physIdx = ParsePhysIndex(name);
                        if (physIdx < 0) physIdx = 0;

                        physUsage.TryGetValue(physIdx, out double cur);
                        physUsage[physIdx] = cur + value;
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
        int end = instanceName.IndexOf('_', start);
        string num = end < 0 ? instanceName[start..] : instanceName[start..end];
        return int.TryParse(num, out int n) ? n : -1;
    }

    private static string ParseGpuManufacturer(string mfg, string name)
    {
        foreach (string s in new[] { mfg, name })
        {
            if (s.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
            if (s.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
            if (s.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("ATI", StringComparison.OrdinalIgnoreCase)) return "AMD";
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

    // "PCI\VEN_1002&DEV_66AF&..." → vendor="1002", device="66AF"
    private static (string vendor, string device) ParsePciIds(string pnpId)
    {
        string vendor = "", dev = "";
        var m = System.Text.RegularExpressions.Regex.Match(
            pnpId, @"VEN_([0-9A-Fa-f]{4})&DEV_([0-9A-Fa-f]{4})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success) { vendor = m.Groups[1].Value.ToUpper(); dev = m.Groups[2].Value.ToUpper(); }
        return (vendor, dev);
    }

    private static string LookupGpuArchitecture(string vendor, string deviceId)
    {
        // AMD: VEN_1002
        if (vendor == "1002")
        {
            if (!int.TryParse(deviceId, System.Globalization.NumberStyles.HexNumber, null, out int dev))
                return "N/A";
            return dev switch
            {
                // Navi 48: RX 9070 / 9070 XT / 9070 GRE  → 0x7550, 0x7551, 0x7480
                // Navi 44: RX 9060 / 9060 XT             → 0x7590, 0x75A0
                0x7550 or 0x7551 or 0x7480 or 0x7590 or 0x75A0 => "RDNA 4",
                // RDNA 3.5 (Navi 3x mobile/APU)
                >= 0x1580 and <= 0x15BF => "RDNA 3.5",
                // RDNA 3 (Navi 3x)
                >= 0x7440 and <= 0x745F => "RDNA 3",
                // RDNA 2 (Navi 2x)
                (0x73BF or 0x73A5 or 0x73AF) or (>= 0x73A0 and <= 0x73FF) => "RDNA 2",
                // RDNA 1 (Navi 1x)
                (>= 0x7310 and <= 0x731F) or (>= 0x7340 and <= 0x734F) => "RDNA 1",
                // Vega / GCN 5
                // ← Radeon Pro VII = 0x66AF
                (>= 0x6860 and <= 0x687F) or (>= 0x66A0 and <= 0x66AF) => "GCN 5 (Vega)",
                // Polaris / GCN 4
                (>= 0x67C0 and <= 0x67FF) or (>= 0x6980 and <= 0x699F) => "GCN 4 (Polaris)",
                _ => "AMD GCN"
            };
        }

        // NVIDIA: VEN_10DE
        if (vendor == "10DE")
        {
            if (!int.TryParse(deviceId, System.Globalization.NumberStyles.HexNumber, null, out int dev))
                return "N/A";
            return dev switch
            {
                // Ada Lovelace (RTX 40)
                >= 0x2600 and <= 0x27FF => "Ada Lovelace",
                // Ampere (RTX 30)
                (>= 0x2200 and <= 0x25FF) or (>= 0x2480 and <= 0x249F) => "Ampere",
                // Turing (RTX 20 / GTX 16)
                (>= 0x1E00 and <= 0x1FFF) or (>= 0x2180 and <= 0x21FF) => "Turing",
                // Pascal (GTX 10)
                (>= 0x1B00 and <= 0x1B80) or (>= 0x1C00 and <= 0x1C8F) => "Pascal",
                _ => "NVIDIA GPU"
            };
        }

        // Intel: VEN_8086
        if (vendor == "8086")
        {
            if (!int.TryParse(deviceId, System.Globalization.NumberStyles.HexNumber, null, out int dev))
                return "N/A";
            return dev switch
            {
                >= 0x4F80 and <= 0x4F90 => "Xe HPG (Arc)",
                >= 0x5690 and <= 0x56BF => "Xe HPG (Arc)",
                >= 0x9A40 and <= 0x9A7F => "Xe LP (Tiger Lake)",
                >= 0x4C8A and <= 0x4C9A => "Xe LP (Rocket Lake)",
                _ => "Intel Graphics"
            };
        }

        return "N/A";
    }

    private static string LookupVramType(string vendor, string deviceId)
    {
        if (!int.TryParse(deviceId, System.Globalization.NumberStyles.HexNumber, null, out int dev))
            return "N/A";

        if (vendor == "1002") // AMD
        {
            return dev switch
            {
                // RDNA 4
                (0x7550 or 0x7551 or 0x7480 or 0x7590 or 0x75A0) or
                // RDNA 3
                (>= 0x7440 and <= 0x745F) or
                // RDNA 2
                (>= 0x73A0 and <= 0x73FF) or
                // RDNA 1
                (>= 0x7310 and <= 0x734F) => "GDDR6",
                // Radeon Pro VII / Vega 20 → HBM2
                >= 0x66A0 and <= 0x66AF => "HBM2",
                // Vega 10
                >= 0x6860 and <= 0x687F => "HBM2",
                // Polaris
                >= 0x67C0 and <= 0x67FF => "GDDR5",
                _ => "GDDR"
            };
        }

        if (vendor == "10DE") // NVIDIA
        {
            return dev switch
            {
                >= 0x2600 and <= 0x27FF => "GDDR6X",  // Ada
                >= 0x2200 and <= 0x25FF => "GDDR6",   // Ampere
                >= 0x1E00 and <= 0x21FF => "GDDR6",   // Turing
                >= 0x1B00 and <= 0x1C8F => "GDDR5X",  // Pascal high-end
                _ => "GDDR"
            };
        }

        if (vendor == "8086") return "Shared";

        return "N/A";
    }
}
