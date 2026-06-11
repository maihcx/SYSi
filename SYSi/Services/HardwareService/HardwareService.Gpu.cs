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
    private bool _pdhReady;
    private Dictionary<(uint hi, uint lo), int> _luidToGpuIndex = [];

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

        using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Class\{driverKey}");
        if (key == null)
        {
            return;
        }

        gpu.DriverVersion = key.GetValue("DriverVersion")?.ToString() ?? "N/A";
        gpu.DriverDate    = key.GetValue("DriverDate")?.ToString()    ?? "N/A";
        gpu.VramText      = ReadVram(key);
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

        NativeMethods.DISPLAY_DEVICE dd = new()
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

            if (match == null || !string.IsNullOrEmpty(match.Resolution))
            {
                continue;
            }

            match.Resolution   = $"{dm.dmPelsWidth} × {dm.dmPelsHeight}";
            match.RefreshRate  = $"{dm.dmDisplayFrequency} Hz";
            match.BitsPerPixel = $"{dm.dmBitsPerPel} bit";
        }
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

            try
            {
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
            catch { }
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

        try
        {
            lock (_pdhLock)
            {
                NativeMethods.PdhCollectQueryData(_gpuQuery);

                uint bufSize = 0, itemCount = 0;
                NativeMethods.PdhGetFormattedCounterArray(
                    _gpuCounter, NativeMethods.PDH_FMT_DOUBLE,
                    ref bufSize, out itemCount, IntPtr.Zero);

                if (bufSize == 0)
                {
                    return;
                }

                IntPtr buf = Marshal.AllocHGlobal((int)bufSize);
                try
                {
                    uint r = NativeMethods.PdhGetFormattedCounterArray(
                        _gpuCounter, NativeMethods.PDH_FMT_DOUBLE,
                        ref bufSize, out itemCount, buf);

                    if (r != 0 && r != 0x800007D2)
                    {
                        return;   // PDH_MORE_DATA is acceptable
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
                        IntPtr itemPtr = IntPtr.Add(buf, j * itemSize);
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
                finally { Marshal.FreeHGlobal(buf); }
            }
        }
        catch { }
    }

    private void DisposeGpuPdh()
    {
        lock (_pdhLock)
        {
            if (_gpuQuery == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.PdhCloseQuery(_gpuQuery);
            _gpuQuery = IntPtr.Zero;
            _pdhReady = false;
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
        try
        {
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
        }
        catch { }
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

    private static readonly Regex LuidRegex = new(@"luid_0x([0-9A-Fa-f]+)_0x([0-9A-Fa-f]+)",  RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PciIdRegex = new(@"VEN_([0-9A-Fa-f]{4})&DEV_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

    private static string LookupArchitecture(string vendor, string deviceId)
    {
        return !int.TryParse(deviceId, System.Globalization.NumberStyles.HexNumber, null, out int dev)
            ? "N/A"
            : vendor switch
        {
            "1002" => dev switch // AMD
            {
                0x7550 or 0x7551 or 0x7480
                    or 0x7590 or 0x75A0 => "RDNA 4",
                >= 0x1580 and <= 0x15BF => "RDNA 3.5",
                >= 0x7440 and <= 0x745F => "RDNA 3",
                >= 0x73A0 and <= 0x73FF => "RDNA 2",
                (>= 0x7310 and <= 0x731F) or (>= 0x7340 and <= 0x734F) => "RDNA 1",
                (>= 0x6860 and <= 0x687F) or (>= 0x66A0 and <= 0x66AF) => "GCN 5 (Vega)",
                (>= 0x67C0 and <= 0x67FF) or (>= 0x6980 and <= 0x699F) => "GCN 4 (Polaris)",
                _ => "AMD GCN",
            },
            "10DE" => dev switch // NVIDIA
            {
                >= 0x2600 and <= 0x27FF => "Ada Lovelace",
                (>= 0x2200 and <= 0x25FF) or (>= 0x2480 and <= 0x249F) => "Ampere",
                (>= 0x1E00 and <= 0x1FFF) or (>= 0x2180 and <= 0x21FF) => "Turing",
                (>= 0x1B00 and <= 0x1B80) or (>= 0x1C00 and <= 0x1C8F) => "Pascal",
                _ => "NVIDIA GPU",
            },
            "8086" => dev switch // Intel
            {
                (>= 0x4F80 and <= 0x4F90) or (>= 0x5690 and <= 0x56BF) => "Xe HPG (Arc)",
                >= 0x9A40 and <= 0x9A7F => "Xe LP (Tiger Lake)",
                >= 0x4C8A and <= 0x4C9A => "Xe LP (Rocket Lake)",
                _ => "Intel Graphics",
            },
            _ => "N/A",
        };
    }

    private static string LookupVramType(string vendor, string deviceId)
    {
        return !int.TryParse(deviceId, System.Globalization.NumberStyles.HexNumber, null, out int dev)
            ? "N/A"
            : vendor switch
        {
            "1002" => dev switch // AMD
            {
                // RDNA 4 / 3 / 2 / 1
                (0x7550 or 0x7551 or 0x7480 or 0x7590 or 0x75A0)
                    or (>= 0x7440 and <= 0x745F)
                    or (>= 0x73A0 and <= 0x73FF)
                    or (>= 0x7310 and <= 0x734F) => "GDDR6",
                (>= 0x6860 and <= 0x687F) or (>= 0x66A0 and <= 0x66AF) => "HBM2",
                >= 0x67C0 and <= 0x67FF => "GDDR5",
                _ => "GDDR",
            },
            "10DE" => dev switch // NVIDIA
            {
                >= 0x2600 and <= 0x27FF => "GDDR6X",  // Ada
                >= 0x2200 and <= 0x25FF => "GDDR6",   // Ampere
                >= 0x1E00 and <= 0x21FF => "GDDR6",   // Turing
                >= 0x1B00 and <= 0x1C8F => "GDDR5X",  // Pascal
                _ => "GDDR",
            },
            "8086" => "Shared",
            _ => "N/A",
        };
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

    private static string MapMemoryTypeCode(int code) => code switch
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
        _ => $"Type {code}",
    };
}