using System.Text.RegularExpressions;

namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    // ── Constants ────────────────────────────────────────────────────────────

    private static readonly Guid DisplayClassGuid = new("4D36E968-E325-11CE-BFC1-08002BE10318");
    private static readonly Guid DxgiFactory1Guid = new("7b7166ec-21c7-44ae-b21a-c9ae321ae369");
    private const uint MirroringFlag = 0x00000008;

    // ── PDH state ────────────────────────────────────────────────────────────

    private readonly object _pdhLock = new();
    private IntPtr _gpuQuery = IntPtr.Zero;
    private IntPtr _gpuCounter = IntPtr.Zero;
    private volatile bool _pdhReady;
    private Dictionary<(uint hi, uint lo), int> _luidToGpuIndex = [];

    // ── Reusable unmanaged buffer for PdhGetFormattedCounterArray ────────────
    // Alloc once, grow on demand, free in DisposeGpuPdh.
    // Avoids a Marshal.AllocHGlobal / FreeHGlobal round-trip every timer tick.

    private IntPtr _pdhBuf = IntPtr.Zero;
    private int _pdhBufSize = 0;

    // ── Cached GPU database lookup (built once from HardwareDatabase arrays) ─
    // Replaces O(n) FirstOrDefault on every BuildGpuInfo call with O(1) lookup.

    // Key: (vendorId uppercase, deviceId int) → Architecture / VramType
    // Ranges are expanded at startup into a flat (vendor, deviceId) → value map.
    // Trade-off: ~2 KB memory for potentially hundreds of lookup calls saved.

    private static readonly Dictionary<(string Vendor, int DeviceId), string> _archCache;
    private static readonly Dictionary<(string Vendor, int DeviceId), string> _vramCache;

    static HardwareService()
    {
        // Pre-expand range rules into flat dictionaries.
        // Each rule covers a small device ID range (typically ≤ 256 entries).
        // Total entries across all vendors: well under 5 000.
        _archCache = ExpandRangeRules(
            HardwareDatabase.GpuArchitectureDatabase,
            r => (r.VendorId.ToUpperInvariant(), r.MinDeviceId, r.MaxDeviceId, r.Architecture));

        _vramCache = ExpandRangeRules(
            HardwareDatabase.GpuVramDatabase,
            r => (r.VendorId.ToUpperInvariant(), r.MinDeviceId, r.MaxDeviceId, r.VramType));
    }

    /// <summary>
    /// Expands an array of range rules into a flat (vendor, deviceId) dictionary.
    /// Later entries win on overlap — same semantics as the original FirstOrDefault
    /// (which returned the first match, i.e. the entry with the lowest array index).
    /// We iterate in reverse so earlier entries overwrite later ones, preserving
    /// FirstOrDefault priority.
    /// </summary>
    private static Dictionary<(string, int), string> ExpandRangeRules<T>(
        T[] rules,
        Func<T, (string Vendor, int Min, int Max, string Value)> selector)
    {
        var dict = new Dictionary<(string, int), string>();

        // Iterate in reverse so that index-0 (highest priority) wins on collision.
        for (int i = rules.Length - 1; i >= 0; i--)
        {
            var (vendor, min, max, value) = selector(rules[i]);
            string v = vendor.ToUpperInvariant();
            for (int id = min; id <= max; id++)
            {
                dict[(v, id)] = value;
            }
        }

        return dict;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public List<GpuInfo> GetGpuInfoList()
    {
        var list = new List<GpuInfo>();

        IntPtr devInfo = NativeMethods.SetupDiGetClassDevs(
            ref Unsafe.AsRef(in DisplayClassGuid),
            IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT);

        if (devInfo == new IntPtr(-1))
        {
            return list;
        }

        NativeMethods.SP_DEVINFO_DATA devData = new()
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.SP_DEVINFO_DATA>()
        };

        for (uint i = 0; NativeMethods.SetupDiEnumDeviceInfo(devInfo, i, ref devData); i++)
        {
            list.Add(BuildGpuInfo(devInfo, ref devData));
        }

        NativeMethods.SetupDiDestroyDeviceInfoList(devInfo);

        EnrichWithDisplayInfo(list);
        RefreshGpuUsage(list);

        return list;
    }

    // ── Build ────────────────────────────────────────────────────────────────

    private static GpuInfo BuildGpuInfo(IntPtr devInfo, ref NativeMethods.SP_DEVINFO_DATA devData)
    {
        string name = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_DEVICEDESC);
        string mfg = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_MFG);
        string hwId = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_HARDWAREID);
        string driverKey = GetDeviceProperty(devInfo, ref devData, NativeMethods.SPDRP_DRIVER);

        var gpu = new GpuInfo
        {
            Name         = name,
            Manufacturer = ParseManufacturer(mfg, name),
            PnpDeviceId  = hwId.Split('\0')[0],   // MULTI_SZ — first entry only
        };

        EnrichFromRegistry(gpu, driverKey);
        return gpu;
    }

    // ── Registry enrichment ──────────────────────────────────────────────────

    private static void EnrichFromRegistry(GpuInfo gpu, string driverKey)
    {
        if (driverKey == "N/A")
        {
            return;
        }

        using var key = Registry.LocalMachine.OpenSubKey(
            $@"SYSTEM\CurrentControlSet\Control\Class\{driverKey}");
        if (key == null)
        {
            return;
        }

        gpu.DriverVersion  = key.GetValue("DriverVersion")?.ToString() ?? "N/A";
        gpu.DriverDate     = key.GetValue("DriverDate")?.ToString()    ?? "N/A";
        gpu.VramText       = ReadVram(key);
        gpu.VideoProcessor = RegistryString(key.GetValue("HardwareInformation.ChipType"));

        var (vendor, device) = ParsePciIds(gpu.PnpDeviceId);
        gpu.VideoArchitecture = LookupArchitecture(vendor, device);
        gpu.VideoMemoryType   = ReadMemoryType(key, vendor, device);
    }

    private static string ReadVram(RegistryKey key)
    {
        long bytes = key.GetValue("HardwareInformation.qwMemorySize") switch
        {
            long l => l,
            byte[] b when b.Length >= 8 => (long)BitConverter.ToUInt64(b, 0),
            int i => (long)(uint)i,
            _ => 0,
        };

        if (bytes <= 0 && key.GetValue("HardwareInformation.MemorySize") is int dw)
        {
            bytes = (long)(uint)dw;
        }

        // Intel iGPU stores size under the "0000" subkey
        if (bytes <= 0)
        {
            using var sub = key.OpenSubKey("0000");
            if (sub?.GetValue("HardwareInformation.qwMemorySize") is byte[] sb && sb.Length >= 8)
            {
                bytes = (long)BitConverter.ToUInt64(sb, 0);
            }
        }

        return bytes > 0 ? FormatBytes(bytes) : "Shared";
    }

    private static string ReadMemoryType(RegistryKey key, string vendor, string device)
    {
        string? fromReg = key.GetValue("HardwareInformation.MemoryType") switch
        {
            string s => s,
            byte[] b => Encoding.Unicode.GetString(b).TrimEnd('\0'),
            int n when n > 0 => MapMemoryTypeCode(n),
            _ => null,
        };

        return !string.IsNullOrEmpty(fromReg) ? fromReg : LookupVramType(vendor, device);
    }

    // ── Display info ─────────────────────────────────────────────────────────

    private static void EnrichWithDisplayInfo(List<GpuInfo> gpus)
    {
        if (gpus.Count == 0)
        {
            return;
        }

        var dd = new NativeMethods.DISPLAY_DEVICE
        { cb = (uint)Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };

        for (uint i = 0; NativeMethods.EnumDisplayDevices(null, i, ref dd, 0); i++)
        {
            if ((dd.StateFlags & MirroringFlag) != 0)
            {
                continue;
            }

            var dm = default(NativeMethods.DEVMODE);
            dm.dmSize = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>();

            if (!NativeMethods.EnumDisplaySettings(
                    dd.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm))
            {
                continue;
            }

            string adapter = dd.DeviceString.Trim();
            var match = gpus.FirstOrDefault(g =>
                    adapter.Contains(g.Name, StringComparison.OrdinalIgnoreCase) ||
                    g.Name.Contains(adapter, StringComparison.OrdinalIgnoreCase))
                ?? (gpus.Count == 1 ? gpus[0] : null);

            if (match == null)
            {
                continue;
            }

            string monitorName = GetMonitorName(dd.DeviceName, i);
            int displayIndex = match.Monitors.Count + 1;

            match.Monitors.Add(new MonitorInfo
            {
                DeviceName   = dd.DeviceName,
                MonitorName  = monitorName,
                DisplayLabel = $"Display {displayIndex}: {monitorName}",
                Resolution   = $"{dm.dmPelsWidth} × {dm.dmPelsHeight}",
                RefreshRate  = $"{dm.dmDisplayFrequency} Hz",
                BitsPerPixel = $"{dm.dmBitsPerPel} bit",
            });
        }
    }

    private static string GetMonitorName(string deviceName, uint adapterIndex)
    {
        var monitor = new NativeMethods.DISPLAY_DEVICE
        { cb = (uint)Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };

        if (NativeMethods.EnumDisplayDevices(deviceName, 0, ref monitor, 0))
        {
            string deviceId = monitor.DeviceID.Trim();
            string? nameFromEdid = GetMonitorNameFromRegistry(deviceId);
            if (!string.IsNullOrWhiteSpace(nameFromEdid))
            {
                return nameFromEdid;
            }
        }

        return $"Display {adapterIndex + 1}";
    }

    private static string? GetMonitorNameFromRegistry(string deviceId)
    {
        // deviceId: "MONITOR\ACQ279Q1\{4d36e96e-e325-11ce-bfc1-08002be10318}\0001"
        using var monitorsKey = Registry.LocalMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Enum\DISPLAY");

        if (monitorsKey == null)
        {
            return null;
        }

        foreach (string monitorId in monitorsKey.GetSubKeyNames())
        {
            if (!deviceId.Contains(monitorId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var monitorKey = monitorsKey.OpenSubKey(monitorId);
            if (monitorKey == null)
            {
                continue;
            }

            foreach (string instanceId in monitorKey.GetSubKeyNames())
            {
                using var instanceKey = monitorKey.OpenSubKey(instanceId);
                if (instanceKey == null)
                {
                    continue;
                }

                // Original code re-checked monitorId against deviceId here (redundant —
                // we already filtered on monitorId above). Removed.
                if (instanceKey.GetValue("HardwareID") == null)
                {
                    continue;
                }

                using var paramsKey = instanceKey.OpenSubKey(@"Device Parameters");
                if (paramsKey?.GetValue("EDID") is not byte[] edid)
                {
                    continue;
                }

                string? name = ParseMonitorNameFromEdid(edid);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;   // ← early-return as soon as we find the name
                }
            }
        }

        return null;
    }

    private static string? ParseMonitorNameFromEdid(byte[] edid)
    {
        if (edid.Length < 128)
        {
            return null;
        }

        for (int i = 54; i <= 108; i += 18)
        {
            if (edid[i] == 0x00 && edid[i + 1] == 0x00 &&
                edid[i + 2] == 0x00 && edid[i + 3] == 0xFC)
            {
                string name = Encoding.ASCII.GetString(edid, i + 5, 13).Trim();
                int newline = name.IndexOf('\n');
                if (newline >= 0)
                {
                    name = name[..newline];
                }

                return name.Trim();
            }
        }

        return null;
    }

    // ── GPU usage (PDH + DXGI LUID mapping) ──────────────────────────────────

    public void InitGpuPdh(List<GpuInfo> gpus)
    {
        lock (_pdhLock)
        {
            if (_pdhReady)
            {
                return;
            }

            if (NativeMethods.PdhOpenQuery(null, IntPtr.Zero, out _gpuQuery) != 0)
            {
                return;
            }

            uint r = NativeMethods.PdhAddCounter(
                _gpuQuery,
                @"\GPU Engine(*engtype_3D)\Utilization Percentage",
                IntPtr.Zero,
                out _gpuCounter);

            if (r != 0)
            {
                return;
            }

            NativeMethods.PdhCollectQueryData(_gpuQuery);
            _pdhReady = true;

            _luidToGpuIndex = BuildLuidMap(gpus);
        }
    }

    public void RefreshGpuUsage(List<GpuInfo> gpus)
    {
        if (!_pdhReady)
        {
            InitGpuPdh(gpus);
        }

        if (!_pdhReady || _gpuCounter == IntPtr.Zero)
        {
            return;
        }

        lock (_pdhLock)
        {
            NativeMethods.PdhCollectQueryData(_gpuQuery);

            // ── Size probe ───────────────────────────────────────────────────
            // Ask PDH how many bytes are needed this tick.
            uint bufSize = 0, itemCount = 0;
            NativeMethods.PdhGetFormattedCounterArray(
                _gpuCounter, NativeMethods.PDH_FMT_DOUBLE,
                ref bufSize, out itemCount, IntPtr.Zero);

            if (bufSize == 0)
            {
                return;
            }

            // ── Grow-only reusable buffer ────────────────────────────────────
            // Reallocate only when PDH needs more space than we have.
            // In steady state (GPU count / engine count unchanged) this path
            // is never taken after the first call — zero alloc per tick.
            if ((int)bufSize > _pdhBufSize)
            {
                if (_pdhBuf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_pdhBuf);
                }

                _pdhBuf     = Marshal.AllocHGlobal((int)bufSize);
                _pdhBufSize = (int)bufSize;
            }

            uint r = NativeMethods.PdhGetFormattedCounterArray(
                _gpuCounter, NativeMethods.PDH_FMT_DOUBLE,
                ref bufSize, out itemCount, _pdhBuf);

            if (r != 0 && r != 0x800007D2)
            {
                return;   // PDH_MORE_DATA is acceptable; anything else bail
            }

            // PDH_FMT_COUNTERVALUE_ITEM_W layout (x64):
            //   szName  : IntPtr  (8 bytes — pointer into buf)
            //   CStatus : uint    (4 bytes)
            //   padding : uint    (4 bytes)
            //   Value   : double  (8 bytes)
            int itemSize = IntPtr.Size + 16;
            var luidUsage = new Dictionary<(uint, uint), double>();

            for (int j = 0; j < (int)itemCount; j++)
            {
                IntPtr itemPtr = IntPtr.Add(_pdhBuf, j * itemSize);
                IntPtr namePtr = Marshal.ReadIntPtr(itemPtr);
                string name = namePtr != IntPtr.Zero
                    ? Marshal.PtrToStringUni(namePtr) ?? "" : "";

                double value = Marshal.PtrToStructure<double>(
                    IntPtr.Add(itemPtr, IntPtr.Size + 8));

                var luid = ParseLuid(name);
                if (luid == null)
                {
                    continue;
                }

                luidUsage.TryGetValue(luid.Value, out double cur);
                luidUsage[luid.Value] = cur + value;
            }

            foreach (var (luid, usage) in luidUsage)
            {
                if (_luidToGpuIndex.TryGetValue(luid, out int idx) && idx < gpus.Count)
                {
                    gpus[idx].UsagePercent = Math.Round(Math.Clamp(usage, 0, 100), 1);
                }
            }
        }
    }

    private void DisposeGpuPdh()
    {
        lock (_pdhLock)
        {
            if (_gpuQuery != IntPtr.Zero)
            {
                NativeMethods.PdhCloseQuery(_gpuQuery);
                _gpuQuery  = IntPtr.Zero;
                _pdhReady  = false;
            }

            // Free the reusable buffer together with the query.
            if (_pdhBuf != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_pdhBuf);
                _pdhBuf     = IntPtr.Zero;
                _pdhBufSize = 0;
            }
        }
    }

    // ── LUID mapping (DXGI) ──────────────────────────────────────────────────

    /// <summary>
    /// Enumerates DXGI adapters to build a LUID → gpus-list-index map.
    /// Called once during PDH init; used every refresh cycle.
    /// </summary>
    private static Dictionary<(uint hi, uint lo), int> BuildLuidMap(List<GpuInfo> gpus)
    {
        var map = new Dictionary<(uint, uint), int>();
        var guid = DxgiFactory1Guid;
        if (NativeMethods.CreateDXGIFactory1(ref guid, out IntPtr factory) != 0
            || factory == IntPtr.Zero)
        {
            return map;
        }

        // IDXGIFactory1::EnumAdapters1 is vtable slot 12
        var enumAdapters1 = VTableDelegate<NativeMethods.EnumAdapters1Delegate>(factory, 12);

        for (uint idx = 0; ; idx++)
        {
            if (enumAdapters1(factory, idx, out IntPtr adapter) == unchecked((int)0x887A0002))
            {
                break;   // DXGI_ERROR_NOT_FOUND
            }

            if (adapter == IntPtr.Zero)
            {
                break;
            }

            try
            {
                // IDXGIAdapter1::GetDesc1 is vtable slot 10
                var getDesc1 = VTableDelegate<NativeMethods.GetDesc1Delegate>(adapter, 10);
                var desc = new NativeMethods.DXGI_ADAPTER_DESC1();

                if (getDesc1(adapter, ref desc) == 0 && (desc.Flags & 2) == 0)
                {
                    var luid = ((uint)desc.AdapterLuid.HighPart, desc.AdapterLuid.LowPart);
                    string descName = new string(desc.Description).TrimEnd('\0');

                    int gpuIdx = gpus.FindIndex(g =>
                        descName.Contains(g.Name, StringComparison.OrdinalIgnoreCase) ||
                        g.Name.Contains(descName, StringComparison.OrdinalIgnoreCase));

                    if (gpuIdx < 0 && (int)idx < gpus.Count)
                    {
                        gpuIdx = (int)idx;   // position fallback
                    }

                    if (gpuIdx >= 0)
                    {
                        map[luid] = gpuIdx;
                    }
                }
            }
            finally { ComRelease(adapter); }
        }

        ComRelease(factory);
        return map;
    }

    // ── COM / vtable helpers ─────────────────────────────────────────────────

    private static T VTableDelegate<T>(IntPtr comObj, int slot) where T : Delegate
    {
        IntPtr vtable = Marshal.ReadIntPtr(comObj);
        IntPtr fn = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
        return Marshal.GetDelegateForFunctionPointer<T>(fn);
    }

    private static void ComRelease(IntPtr comObj)
    {
        IntPtr vtable = Marshal.ReadIntPtr(comObj);
        IntPtr releaseFn = Marshal.ReadIntPtr(vtable, 2 * IntPtr.Size);
        Marshal.GetDelegateForFunctionPointer<NativeMethods.ReleaseDelegate>(releaseFn)(comObj);
    }

    // ── Parsing helpers ──────────────────────────────────────────────────────

    private static readonly Regex LuidRegex = new(
        @"luid_0x([0-9A-Fa-f]+)_0x([0-9A-Fa-f]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PciIdRegex = new(
        @"VEN_([0-9A-Fa-f]{4})&DEV_([0-9A-Fa-f]{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Extracts the LUID from a PDH GPU Engine instance name.
    /// Format: "pid_XXX_luid_0xHHHHHHHH_0xLLLLLLLL_phys_N_eng_M_engtype_3D"
    /// </summary>
    private static (uint hi, uint lo)? ParseLuid(string name)
    {
        var m = LuidRegex.Match(name);

        return !m.Success
            ? null
            : uint.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out uint hi)
            && uint.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber, null, out uint lo)
            ? (hi, lo) : null;
    }

    /// <summary>Parses "VEN_XXXX&amp;DEV_XXXX" from a PnP hardware ID string.</summary>
    private static (string vendor, string device) ParsePciIds(string pnpId)
    {
        var m = PciIdRegex.Match(pnpId);

        return m.Success
            ? (m.Groups[1].Value.ToUpperInvariant(), m.Groups[2].Value.ToUpperInvariant())
            : ("", "");
    }

    // ── Lookup tables ────────────────────────────────────────────────────────

    /// <summary>
    /// O(1) lookup replacing the original O(n) FirstOrDefault scan.
    /// Uses the flat dictionary pre-built from GpuArchitectureDatabase at startup.
    /// </summary>
    private static string LookupArchitecture(string vendor, string deviceId)
    {
        if (!int.TryParse(deviceId, System.Globalization.NumberStyles.HexNumber,
                null, out int device))
        {
            return "N/A";
        }

        string v = vendor.ToUpperInvariant();

        if (_archCache.TryGetValue((v, device), out string? arch))
        {
            return arch;
        }

        return HardwareDatabase.GpuArchitectureDatabaseFallbacks.TryGetValue(v, out var fallback)
            ? fallback
            : "N/A";
    }

    /// <summary>
    /// O(1) lookup replacing the original O(n) FirstOrDefault scan.
    /// Uses the flat dictionary pre-built from GpuVramDatabase at startup.
    /// </summary>
    private static string LookupVramType(string vendor, string deviceId)
    {
        if (!int.TryParse(deviceId, System.Globalization.NumberStyles.HexNumber,
                null, out int device))
        {
            return "N/A";
        }

        string v = vendor.ToUpperInvariant();

        if (_vramCache.TryGetValue((v, device), out string? vram))
        {
            return vram;
        }

        return HardwareDatabase.GpuVramDatabaseFallbacks.TryGetValue(v, out var fallback)
            ? fallback
            : "N/A";
    }

    // ── Small helpers ────────────────────────────────────────────────────────

    private static string ParseManufacturer(string mfg, string name)
    {
        foreach (string s in (string[])[mfg, name])
        {
            if (s.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
            {
                return "NVIDIA";
            }

            if (s.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            {
                return "Intel";
            }

            if (s.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("ATI", StringComparison.OrdinalIgnoreCase))
            {
                return "AMD";
            }
        }
        return string.IsNullOrWhiteSpace(mfg) ? "N/A" : mfg;
    }

    private static string MapMemoryTypeCode(int code)
    {
        return HardwareDatabase.MemoryTypeDatabase.TryGetValue(code, out var name)
            ? name
            : $"Type {code}";
    }
}