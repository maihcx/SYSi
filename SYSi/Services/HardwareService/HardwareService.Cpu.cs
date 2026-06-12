using System.Management;
using System.Runtime.Intrinsics.X86;

namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    // ── State ────────────────────────────────────────────────────────────────

    private long _prevIdle, _prevKernel, _prevUser;
    private readonly object _cpuLock = new();

    private IntPtr _cpuClockQuery = IntPtr.Zero;
    private IntPtr _cpuClockCounter = IntPtr.Zero;
    private int _cpuBaseMHz;
    private readonly object _cpuClockLock = new();

    private static int _cachedBaseMHz;

    public HardwareService()
    {
        ReadSystemTimes(out _prevIdle, out _prevKernel, out _prevUser);
        InitCpuClockPdh();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public CpuInfo GetCpuInfo()
    {
        var info = new CpuInfo();
        try
        {
            ReadBasicCpuInfo(info);
            info.LogicalProcessors = Environment.ProcessorCount;
            info.PhysicalCores = GetPhysicalCoreCount();
            info.VirtualizationEnabled = GetVirtualizationEnabled();
            EnrichCpuFromSmbios(info);
        }
        catch { }

        RefreshCPUInfo(info);
        return info;
    }

    public void RefreshCPUInfo(CpuInfo info)
    {
        info.UsagePercent = GetCpuUsage();

        double currentCpuClock = GetCurrentCpuSpeedGHz();

        info.CurrentClockGHz = $"{currentCpuClock:F2} GHz";
        info.BoostRatio = $"{((currentCpuClock / (GetCpuBaseClockMHz() / 1000.0)) * 100):F2} %";
    }

    /// <summary>Delta-based CPU usage via GetSystemTimes — no PerformanceCounter overhead.</summary>
    private double GetCpuUsage()
    {
        try
        {
            ReadSystemTimes(out long idle, out long kernel, out long user);

            long dIdle, dKernel, dUser;
            lock (_cpuLock)
            {
                dIdle   = idle   - _prevIdle;
                dKernel = kernel - _prevKernel;
                dUser   = user   - _prevUser;
                _prevIdle   = idle;
                _prevKernel = kernel;
                _prevUser   = user;
            }

            long total = dKernel + dUser;
            return total <= 0 ? 0 : Math.Round(Math.Clamp((1.0 - (double)dIdle / total) * 100.0, 0, 100), 1);
        }
        catch { return 0; }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void ReadBasicCpuInfo(CpuInfo info)
    {
        info.Name         = GetCpuBrandViaCpuid();
        info.ShortName    = ParseCpuName(info.Name);
        info.Manufacturer = GetCpuVendor();

        int baseMHz = GetCpuBaseClockMHz();
        info.BaseClockGHz = baseMHz > 0 ? $"{baseMHz / 1000.0:F2} GHz" : "N/A";
    }

    // ── PDH current clock ────────────────────────────────────────────────────

    private void InitCpuClockPdh()
    {
        _cpuBaseMHz = GetCpuBaseClockMHz();

        if (_cpuBaseMHz == 0)
        {
            return;
        }

        lock (_cpuClockLock)
        {
            if (NativeMethods.PdhOpenQuery(null, 0, out _cpuClockQuery) != 0)
            {
                return;
            }

            NativeMethods.PdhAddEnglishCounter(
                _cpuClockQuery,
                @"\Processor Information(_Total)\% Processor Performance",
                0, out _cpuClockCounter);
            NativeMethods.PdhCollectQueryData(_cpuClockQuery);
        }
    }

    private static int GetCpuBaseClockMHz()
    {
        if (_cachedBaseMHz == 0)
        {
            _cachedBaseMHz = GetCpuBaseSpeedViaCpuid();
            if (_cachedBaseMHz == 0)
            {
                using var searcher = new ManagementObjectSearcher("select CurrentClockSpeed from Win32_Processor");
                foreach (var item in searcher.Get())
                {
                    _cachedBaseMHz = Convert.ToInt32((uint)item["CurrentClockSpeed"]);
                }
            }
        }

        return _cachedBaseMHz;
    }

    private double GetCurrentCpuSpeedGHz()
    {
        try
        {
            if (_cpuClockQuery == IntPtr.Zero || _cpuBaseMHz == 0)
            {
                return 0;
            }

            lock (_cpuClockLock)
            {
                if (NativeMethods.PdhCollectQueryData(_cpuClockQuery) != 0)
                {
                    return 0;
                }

                var value = new NativeMethods.PDH_FMT_COUNTERVALUE();
                if (NativeMethods.PdhGetFormattedCounterValue(
                        _cpuClockCounter,
                        NativeMethods.PDH_FMT_DOUBLE,
                        out _, out value) != 0)
                {
                    return 0;
                }

                // % Processor Performance × base = current MHz
                double currentMHz = value.doubleValue / 100.0 * _cpuBaseMHz;
                return currentMHz / 1000.0;
            }
        }
        catch { return 0; }
    }

    private void DisposeCpuClockPdh()
    {
        lock (_cpuClockLock)
        {
            if (_cpuClockQuery != IntPtr.Zero)
            {
                NativeMethods.PdhCloseQuery(_cpuClockQuery);
                _cpuClockQuery  = IntPtr.Zero;
                _cpuClockCounter = IntPtr.Zero;
            }
        }
    }

    // ── Speed helpers ────────────────────────────────────────────────────────

    private static int GetCpuBaseSpeedViaCpuid()
    {
        var (maxLeaf, _, _, _) = X86Base.CpuId(0, 0);
        if (maxLeaf < 0x16)
        {
            return 0;
        }

        var (eax, _, _, _) = X86Base.CpuId(0x16, 0);
        return eax & 0xFFFF;
    }

    // ── Brand / vendor ───────────────────────────────────────────────────────

    private static string GetCpuBrandViaCpuid()
    {
        var sb = new StringBuilder(48);
        foreach (uint leaf in new uint[] { 0x80000002, 0x80000003, 0x80000004 })
        {
            var info = X86Base.CpuId((int)leaf, 0);
            AppendLeaf(sb, info.Eax);
            AppendLeaf(sb, info.Ebx);
            AppendLeaf(sb, info.Ecx);
            AppendLeaf(sb, info.Edx);
        }
        return sb.ToString().Trim();
    }

    private static string GetCpuVendor()
    {
        var (_, ebx, ecx, edx) = X86Base.CpuId(0, 0);
        var bytes = new byte[12];
        BitConverter.GetBytes(ebx).CopyTo(bytes, 0);
        BitConverter.GetBytes(edx).CopyTo(bytes, 4);
        BitConverter.GetBytes(ecx).CopyTo(bytes, 8);
        return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
    }

    private static void AppendLeaf(StringBuilder sb, int reg)
    {
        var bytes = BitConverter.GetBytes(reg);
        foreach (var b in bytes)
        {
            sb.Append(b == 0 ? ' ' : (char)b);
        }
    }

    // ── System times ─────────────────────────────────────────────────────────

    private static void ReadSystemTimes(out long idle, out long kernel, out long user)
    {
        NativeMethods.GetSystemTimes(out var fi, out var fk, out var fu);
        idle   = ToLong(fi);
        kernel = ToLong(fk);
        user   = ToLong(fu);
    }

    private static long ToLong(NativeMethods.FILETIME ft)
        => ((long)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

    // ── Physical core count ──────────────────────────────────────────────────

    private static int GetPhysicalCoreCount()
    {
        try
        {
            uint len = 0;
            NativeMethods.GetLogicalProcessorInformation(IntPtr.Zero, ref len);
            if (len == 0)
            {
                return Environment.ProcessorCount;
            }

            IntPtr buf = Marshal.AllocHGlobal((int)len);
            try
            {
                if (!NativeMethods.GetLogicalProcessorInformation(buf, ref len))
                {
                    return Environment.ProcessorCount;
                }

                int size = Marshal.SizeOf<NativeMethods.SYSTEM_LOGICAL_PROCESSOR_INFORMATION>();
                int count = 0;
                for (int i = 0; i + size <= (int)len; i += size)
                {
                    var item = Marshal.PtrToStructure<NativeMethods.SYSTEM_LOGICAL_PROCESSOR_INFORMATION>(buf + i);
                    if (item.Relationship == 0)
                    {
                        count++;
                    }
                }
                return count > 0 ? count : Environment.ProcessorCount;
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        catch { return Environment.ProcessorCount; }
    }

    // ── Architecture ─────────────────────────────────────────────────────────

    private static string GetCpuArchitecture()
    {
        NativeMethods.GetNativeSystemInfo(out var si);
        return si.ProcessorArchitecture switch
        {
            0 => "x86",
            5 => "ARM",
            6 => "Itanium",
            9 => "x64",
            12 => "ARM64",
            _ => $"Unknown ({si.ProcessorArchitecture})",
        };
    }

    // ── Cache ────────────────────────────────────────────────────────────────

    private static (string L1, string L2, string L3) GetCpuCaches()
    {
        try
        {
            uint len = 0;
            NativeMethods.GetLogicalProcessorInformationEx(
                NativeMethods.LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache, IntPtr.Zero, ref len);
            if (len == 0)
            {
                return ("N/A", "N/A", "N/A");
            }

            IntPtr buf = Marshal.AllocHGlobal((int)len);
            try
            {
                if (!NativeMethods.GetLogicalProcessorInformationEx(
                    NativeMethods.LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache, buf, ref len))
                {
                    return ("N/A", "N/A", "N/A");
                }

                uint l1 = 0, l2 = 0, l3 = 0;
                int headerSize = Marshal.SizeOf<NativeMethods.SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>();
                long offset = 0;

                while (offset < len)
                {
                    IntPtr cur = IntPtr.Add(buf, (int)offset);
                    var header = Marshal.PtrToStructure<NativeMethods.SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(cur);
                    if (header.Relationship == NativeMethods.LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache)
                    {
                        var cache = Marshal.PtrToStructure<NativeMethods.CACHE_DESCRIPTOR>(
                            IntPtr.Add(cur, headerSize));
                        switch (cache.Level)
                        {
                            case 1: l1 += cache.Size; break;
                            case 2: l2 += cache.Size; break;
                            case 3: l3 += cache.Size; break;
                        }
                    }
                    offset += header.Size;
                }
                return (FormatBytes(l1), FormatBytes(l2), FormatBytes(l3));
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        catch { return ("N/A", "N/A", "N/A"); }
    }

    // ── CPU signature ────────────────────────────────────────────────────────

    private static (int Family, int Model, int Stepping, string ProcessorId) GetCpuSignature()
    {
        if (!X86Base.IsSupported)
        {
            return (0, 0, 0, "N/A");
        }

        var cpuid = X86Base.CpuId(1, 0);
        int eax = cpuid.Eax;

        int stepping = eax & 0xF;
        int model = (eax >> 4)  & 0xF;
        int family = (eax >> 8)  & 0xF;
        int extModel = (eax >> 16) & 0xF;
        int extFamily = (eax >> 20) & 0xFF;

        int displayFamily = family == 0xF ? family + extFamily : family;
        int displayModel = (family == 0x6 || family == 0xF) ? model + (extModel << 4) : model;

        return (displayFamily, displayModel, stepping,
            $"{(uint)cpuid.Edx:X8}{(uint)cpuid.Eax:X8}");
    }

    // ── Enrich from SMBIOS ───────────────────────────────────────────────────

    private static void EnrichCpuFromSmbios(CpuInfo info)
    {
        foreach (var s in ParseSmbios(4))
        {
            if (s.Length > 0x08)
            {
                info.Socket = s.Str(0x04);
            }
            break;
        }

        (info.L1Cache, info.L2Cache, info.L3Cache) = GetCpuCaches();
        info.Architecture = GetCpuArchitecture();

        var sig = GetCpuSignature();
        info.Family      = sig.Family   > 0 ? $"{sig.Family:X}" : "N/A";
        info.Model       = sig.Model    > 0 ? $"{sig.Model:X}" : "N/A";
        info.Stepping    = $"{sig.Stepping:X}";
        info.ProcessorId = sig.ProcessorId;

        info.CodeName     = GetCpuCodeName(info.Manufacturer, sig.Family, sig.Model);
        info.Instructions = string.Join(", ", GetSupportedInstructions());
        info.MaxTdp       = GetCpuMaxTdp(info.ShortName, info.Name);
    }

    // ── Virtualization ───────────────────────────────────────────────────────

    private static bool GetVirtualizationEnabled()
    {
        if (!X86Base.IsSupported)
        {
            return false;
        }

        var (_, _, ecx, _) = X86Base.CpuId(1, 0);

        bool vmxSupported = (ecx & (1 << 5)) != 0;
        bool hypervisorPresent = (ecx & (1 << 31)) != 0;

        bool svmSupported = false;
        var (maxExt, _, _, _) = X86Base.CpuId(unchecked((int)0x80000000), 0);
        if ((uint)maxExt >= 0x80000001)
        {
            var (_, _, ecxExt, _) = X86Base.CpuId(unchecked((int)0x80000001), 0);
            svmSupported = (ecxExt & (1 << 2)) != 0;
        }

        if (hypervisorPresent)
        {
            return true;
        }

        return vmxSupported || svmSupported;
    }

    // ── Misc ─────────────────────────────────────────────────────────────────

    private static string ParseCpuName(string cpuName)
        => cpuName.Replace("Intel(R) Core(TM)", "Core");

    private static readonly Dictionary<string, string> TdpLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        // Intel 13th/14th Gen desktop
        ["i9-14900K"] = "125 W (253 W PL2)",
        ["i9-13900K"] = "125 W (253 W PL2)",
        ["i7-14700K"] = "125 W (253 W PL2)",
        ["i7-13700K"] = "125 W (253 W PL2)",
        ["i5-14600K"] = "125 W (181 W PL2)",
        ["i5-13600K"] = "125 W (181 W PL2)",

        // AMD Ryzen 7000/9000 desktop
        ["7950X3D"] = "120 W",
        ["7950X"]   = "170 W",
        ["7800X3D"] = "120 W",
        ["7700X"]   = "105 W",
        ["7600X"]   = "105 W",
        ["9950X"]   = "170 W",
        ["9700X"]   = "65 W",
    };

    private static string GetCpuMaxTdp(string shortName, string fullName)
    {
        foreach (var (key, tdp) in TdpLookup)
        {
            if (shortName.Contains(key, StringComparison.OrdinalIgnoreCase)
                || fullName.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return tdp;
            }
        }
        return "N/A";
    }
    private static List<string> GetSupportedInstructions()
    {
        var list = new List<string>();
        if (!X86Base.IsSupported)
        {
            return list;
        }

        var (maxLeaf, _, _, _) = X86Base.CpuId(0, 0);
        var (maxExt, _, _, _)  = X86Base.CpuId(unchecked((int)0x80000000), 0);

        // Leaf 1
        if (maxLeaf >= 1)
        {
            var r1 = X86Base.CpuId(1, 0);
            AddFlags(list, r1.Edx, Leaf1Edx);
            AddFlags(list, r1.Ecx, Leaf1Ecx);
        }

        // Leaf 7, sub-leaf 0
        if (maxLeaf >= 7)
        {
            var r7 = X86Base.CpuId(7, 0);
            AddFlags(list, r7.Ebx, Leaf7Ebx);
            AddFlags(list, r7.Ecx, Leaf7Ecx);
            AddFlags(list, r7.Edx, Leaf7Edx);
        }

        // Extended leaf 0x80000001
        if ((uint)maxExt >= 0x80000001)
        {
            var rExt = X86Base.CpuId(unchecked((int)0x80000001), 0);
            AddFlags(list, rExt.Ecx, ExtEcx);
            AddFlags(list, rExt.Edx, ExtEdx);
        }

        return list;
    }

    private static void AddFlags(List<string> list, int register, (int Bit, string Name)[] flags)
    {
        foreach (var (bit, name) in flags)
        {
            if ((register & (1 << bit)) != 0)
            {
                list.Add(name);
            }
        }
    }

    private static string GetCpuCodeName(string manufacturer, int family, int model)
    {
        if (manufacturer.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            // Family 6 — displayModel = model + (extModel << 4)
            return family switch
            {
                6 => model switch
                {
                    0xBA or 0xB7 or 0xB5 => "Raptor Lake",
                    0x97 or 0x9A => "Alder Lake",
                    0x8F => "Sapphire Rapids",
                    0x8C or 0x8D => "Tiger Lake",
                    0xA5 or 0xA6 => "Comet Lake",
                    0x9E or 0x9D => "Coffee Lake / Kaby Lake",
                    0x55 => "Skylake-X",
                    0x4E or 0x5E => "Skylake",
                    0x3D or 0x47 => "Broadwell",
                    0x3C or 0x45 or 0x46 => "Haswell",
                    0x3A or 0x3E => "Ivy Bridge",
                    0x2A or 0x2D => "Sandy Bridge",
                    _ => "N/A"
                },
                _ => "N/A"
            };
        }

        if (manufacturer.Contains("AMD", StringComparison.OrdinalIgnoreCase))
        {
            return family switch
            {
                0x1A => "Zen 5",
                0x19 => model switch
                {
                    >= 0x60 and <= 0x6F => "Zen 4 (Mobile)",
                    >= 0x10 and <= 0x1F => "Zen 4",
                    >= 0x40 and <= 0x5F => "Zen 3+",
                    _ => "Zen 3"
                },
                0x17 => model switch
                {
                    >= 0x30 => "Zen 2",
                    _ => "Zen / Zen+"
                },
                _ => "N/A"
            };
        }

        return "N/A";
    }

    // ── Feature flag tables ─────────────────────────────────────────────────

    private static readonly (int Bit, string Name)[] Leaf1Edx =
    [
        (23, "MMX"),
        (25, "SSE"),
        (26, "SSE2"),
    ];

    private static readonly (int Bit, string Name)[] Leaf1Ecx =
    [
        (0,  "SSE3"),
        (9,  "SSSE3"),
        (12, "FMA3"),
        (19, "SSE4.1"),
        (20, "SSE4.2"),
        (25, "AES"),
        (28, "AVX"),
    ];

    private static readonly (int Bit, string Name)[] Leaf7Ebx =
    [
        (3,  "BMI1"),
        (5,  "AVX2"),
        (8,  "BMI2"),
        (16, "AVX512F"),
        (17, "AVX512DQ"),
        (28, "AVX512CD"),
        (30, "AVX512BW"),
        (31, "AVX512VL"),
    ];

    private static readonly (int Bit, string Name)[] Leaf7Ecx =
    [
        (8, "GFNI"),
        (9, "VAES"),
    ];

    private static readonly (int Bit, string Name)[] Leaf7Edx =
    [
        (4, "AVX512VNNI"),
        (8, "AVX512VP2INTERSECT"),
    ];

    private static readonly (int Bit, string Name)[] ExtEcx =
    [
        (6,  "SSE4A"),
        (16, "FMA4"),
        (21, "TBM"),
    ];

    private static readonly (int Bit, string Name)[] ExtEdx =
    [
        (29, "x86-64"),
        (31, "3DNow!"),
    ];
}