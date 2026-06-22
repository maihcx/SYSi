using System.Management;
using System.Runtime.Intrinsics.X86;
using static SYSi.Services.HardwareService.HardwareDatabase;

namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    // ── State ────────────────────────────────────────────────────────────────

    private long _prevIdle, _prevKernel, _prevUser;
    private readonly object _cpuLock = new();

    private IntPtr _cpuClockQuery = IntPtr.Zero;
    private IntPtr _cpuClockCounter = IntPtr.Zero;
    private readonly object _cpuClockLock = new();

    // Base MHz: computed once, never changes at runtime.
    private static readonly Lazy<int> _cachedBaseMHz = new(
        ReadBaseMHz, isThreadSafe: true);

    // ── CPU rule lookup cache ─────────────────────────────────────────────────
    // CpuRulesDatabase is a flat array searched with FirstOrDefault (O(n)) on
    // every GetCpuInfo call. We convert it once into a nested dictionary:
    //   vendor (lowercase) → family → list of rules sorted by MinModel.
    // FindCpuRule then does two O(1) dictionary lookups + a tiny linear scan
    // over rules that share the same vendor+family — typically 1–3 entries.

    private static readonly Dictionary<string, Dictionary<int, List<HardwareDatabase.CpuModelRule>>>
        _cpuRuleIndex = BuildCpuRuleIndex();

    // TDP lookup: CpuTdpDatabase is already a Dictionary, but GetCpuMaxTdp
    // iterates it with string.Contains which is O(n * keyLen). We keep the
    // original dictionary as-is since it's small (~15 entries) and only called
    // once at startup — no meaningful gain from further caching.

    // ── Constructor ──────────────────────────────────────────────────────────

    public HardwareService()
    {
        ReadSystemTimes(out _prevIdle, out _prevKernel, out _prevUser);
        InitCpuClockPdh();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public CpuInfo GetCpuInfo()
    {
        var info = new CpuInfo();

        ReadBasicCpuInfo(info);
        info.LogicalProcessors     = Environment.ProcessorCount;
        info.PhysicalCores         = GetPhysicalCoreCount();
        info.VirtualizationEnabled = GetVirtualizationEnabled();
        EnrichCpuFromSmbios(info);
        RefreshCPUInfo(info);
        return info;
    }

    public void RefreshCPUInfo(CpuInfo info)
    {
        info.UsagePercent = GetCpuUsage();

        double currentCpuClock = GetCurrentCpuSpeedGHz();

        info.CurrentClockGHz = $"{currentCpuClock:F2} GHz";
        info.BoostRatio      = $"{currentCpuClock / (GetCpuBaseClockMHz() / 1000.0) * 100:F2} %";
    }

    // ── CPU usage (delta-based GetSystemTimes) ────────────────────────────────

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
            return total <= 0
                ? 0
                : Math.Round(Math.Clamp((1.0 - (double)dIdle / total) * 100.0, 0, 100), 1);
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

    // ── PDH current clock ─────────────────────────────────────────────────────

    private void InitCpuClockPdh()
    {
        if (_cachedBaseMHz.Value == 0)
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

    private static int GetCpuBaseClockMHz() => _cachedBaseMHz.Value;

    private double GetCurrentCpuSpeedGHz()
    {
        int baseMHz = GetCpuBaseClockMHz();

        try
        {
            if (_cpuClockQuery == IntPtr.Zero || baseMHz == 0)
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
                double currentMHz = value.doubleValue / 100.0 * baseMHz;
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
                _cpuClockQuery   = IntPtr.Zero;
                _cpuClockCounter = IntPtr.Zero;
            }
        }
    }

    // ── Base MHz (Lazy, computed once) ────────────────────────────────────────

    /// <summary>
    /// Reads base clock MHz. Tried in priority order:
    ///   1. NtPowerInformation (fastest, no WMI)
    ///   2. CPUID leaf 0x16 (Intel only)
    ///   3. WMI Win32_Processor (last resort — slowest)
    /// </summary>
    private static int ReadBaseMHz()
    {
        // ── 1. NtPowerInformation ─────────────────────────────────────────────
        int cpuCount = Environment.ProcessorCount;
        int structSize = Marshal.SizeOf<NativeMethods.PROCESSOR_POWER_INFORMATION>();
        IntPtr buffer = Marshal.AllocHGlobal(structSize * cpuCount);

        try
        {
            uint status = NativeMethods.CallNtPowerInformation(
                NativeMethods.POWER_INFORMATION_LEVEL.ProcessorInformation,
                IntPtr.Zero, 0,
                buffer, (uint)(structSize * cpuCount));

            if (status == 0)
            {
                int mhz = (int)Marshal.PtrToStructure<NativeMethods.PROCESSOR_POWER_INFORMATION>(buffer).MaxMhz;
                if (mhz > 0)
                {
                    return mhz;
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        // ── 2. CPUID leaf 0x16 (Intel Skylake+) ──────────────────────────────
        int cpuidMhz = GetCpuBaseSpeedViaCpuid();
        if (cpuidMhz > 0)
        {
            return cpuidMhz;
        }

        // ── 3. WMI fallback ───────────────────────────────────────────────────
        try
        {
            using var s = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor");
            foreach (var item in s.Get())
            {
                return Convert.ToInt32((uint)item["MaxClockSpeed"]);
            }
        }
        catch { }

        return 0;
    }

    // ── Speed helpers ─────────────────────────────────────────────────────────

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

    // ── Brand / vendor ────────────────────────────────────────────────────────

    private static readonly uint[] BrandLeaves = [0x80000002, 0x80000003, 0x80000004];

    private static string GetCpuBrandViaCpuid()
    {
        var sb = new StringBuilder(48);
        foreach (uint leaf in BrandLeaves)
        {
            var (Eax, Ebx, Ecx, Edx) = X86Base.CpuId((int)leaf, 0);
            AppendLeaf(sb, Eax);
            AppendLeaf(sb, Ebx);
            AppendLeaf(sb, Ecx);
            AppendLeaf(sb, Edx);
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

    // ── System times ──────────────────────────────────────────────────────────

    private static void ReadSystemTimes(out long idle, out long kernel, out long user)
    {
        NativeMethods.GetSystemTimes(out var fi, out var fk, out var fu);
        idle   = ToLong(fi);
        kernel = ToLong(fk);
        user   = ToLong(fu);
    }

    private static long ToLong(NativeMethods.FILETIME ft)
        => ((long)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

    // ── Physical core count ───────────────────────────────────────────────────

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

    // ── Architecture ──────────────────────────────────────────────────────────

    private static string GetCpuArchitecture()
    {
        NativeMethods.GetNativeSystemInfo(out var si);

        return HardwareDatabase.CpuArchitecturesDatabase.TryGetValue(
            si.ProcessorArchitecture, out var architecture)
            ? architecture
            : $"Unknown ({si.ProcessorArchitecture})";
    }

    // ── Cache ─────────────────────────────────────────────────────────────────

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

    // ── CPU signature ─────────────────────────────────────────────────────────

    private static (int Family, int Model, int Stepping, string ProcessorId) GetCpuSignature()
    {
        if (!X86Base.IsSupported)
        {
            return (0, 0, 0, "N/A");
        }

        var cpuid = X86Base.CpuId(1, 0);
        int eax = cpuid.Eax;

        int stepping = eax         & 0xF;
        int model = (eax >>  4) & 0xF;
        int family = (eax >>  8) & 0xF;
        int extModel = (eax >> 16) & 0xF;
        int extFamily = (eax >> 20) & 0xFF;

        int displayFamily = family == 0xF ? family + extFamily : family;
        int displayModel = (family == 0x6 || family == 0xF) ? model + (extModel << 4) : model;

        return (displayFamily, displayModel, stepping,
            $"{(uint)cpuid.Edx:X8}{(uint)cpuid.Eax:X8}");
    }

    // ── Enrich from SMBIOS ────────────────────────────────────────────────────

    private static void EnrichCpuFromSmbios(CpuInfo info)
    {
        (info.L1Cache, info.L2Cache, info.L3Cache) = GetCpuCaches();
        info.Architecture = GetCpuArchitecture();

        var sig = GetCpuSignature();
        info.Family      = sig.Family   > 0 ? $"{sig.Family:X}" : "N/A";
        info.Model       = sig.Model    > 0 ? $"{sig.Model:X}" : "N/A";
        info.Stepping    = $"{sig.Stepping:X}";
        info.ProcessorId = sig.ProcessorId;

        info.DesignId = $"{sig.Family}-0x{sig.Model:X2}-0x{sig.Model:X2}-{sig.Stepping}-{info.PhysicalCores}";

        if (IsEngineeringSample(info))
        {
            EsSampleRule? esMatch = FindEsMatch(info);
            if (esMatch != null)
            {
                info.Name = esMatch.RetailName;
                info.ShortName = ParseCpuName(info.Name);
            }
        }

        // Single FindCpuRule call — result shared across CodeName, Socket, TDP
        var rule = FindCpuRule(info.Manufacturer, sig.Family, sig.Model);

        info.CodeName     = rule?.CodeName ?? "N/A";
        info.Instructions = string.Join(", ", GetSupportedInstructions());
        info.MaxTdp       = GetCpuMaxTdp(info.ShortName, info.Name);

        foreach (var s in ParseSmbios(4))
        {
            if (s.Length > 0x08)
            {
                string rawSocket = s.Str(0x04);
                // Prefer database socket over raw SMBIOS string when available
                info.Socket = rule?.Socket ?? rawSocket;
            }
            break;
        }
    }

    // ── Virtualization ────────────────────────────────────────────────────────

    private static bool GetVirtualizationEnabled()
    {
        if (!X86Base.IsSupported)
        {
            return false;
        }

        var (_, _, ecx, _) = X86Base.CpuId(1, 0);

        bool vmxSupported = (ecx & (1 << 5))  != 0;
        bool hypervisorPresent = (ecx & (1 << 31)) != 0;

        if (hypervisorPresent)
        {
            return true;
        }

        bool svmSupported = false;
        var (maxExt, _, _, _) = X86Base.CpuId(unchecked((int)0x80000000), 0);
        if ((uint)maxExt >= 0x80000001)
        {
            var (_, _, ecxExt, _) = X86Base.CpuId(unchecked((int)0x80000001), 0);
            svmSupported = (ecxExt & (1 << 2)) != 0;
        }

        return vmxSupported || svmSupported;
    }

    // ── Misc ──────────────────────────────────────────────────────────────────

    private static string ParseCpuName(string cpuName)
        => cpuName.Replace("Intel(R) Core(TM)", "Core");

    private static string GetCpuMaxTdp(string shortName, string fullName)
    {
        foreach (var (key, tdp) in HardwareDatabase.CpuTdpDatabase)
        {
            if (shortName.Contains(key, StringComparison.OrdinalIgnoreCase)
                || fullName.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return tdp;
            }
        }
        return "N/A";
    }

    private static bool IsEngineeringSample(CpuInfo info)
    {
        bool brandEs = info.Name.Contains("Genuine Intel", StringComparison.OrdinalIgnoreCase);

        bool steppingEs = int.TryParse(info.Stepping, System.Globalization.NumberStyles.HexNumber,
                                       null, out int stepping) && stepping < 1;

        return brandEs || steppingEs;
    }

    public static EsSampleRule? FindEsMatch(CpuInfo info)
    {
        if (info == null)
        {
            return null;
        }

        if (!int.TryParse(info.Family, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int family) ||
            !int.TryParse(info.Model, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int model) ||
            !int.TryParse(info.Stepping, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int stepping))
        {
            return null;
        }

        int cores = info.PhysicalCores;

        for (int i = 0; i < EsSamplesDatabase.Length; i++)
        {
            var r = EsSamplesDatabase[i];

            if (r.Family == family &&
                model >= r.MinModel &&
                model <= r.MaxModel &&
                (r.Stepping == -1 || r.Stepping == stepping) &&
                (r.CoreCount == -1 || r.CoreCount == cores))
            {
                return r;
            }
        }

        return null;
    }

    // ── Instruction set detection ─────────────────────────────────────────────

    private static List<string> GetSupportedInstructions()
    {
        var instructions = new List<string>();

        if (!X86Base.IsSupported)
        {
            return instructions;
        }

        var (maxLeaf, _, _, _) = X86Base.CpuId(0, 0);
        var (maxExt, _, _, _) = X86Base.CpuId(unchecked((int)0x80000000), 0);

        // Cache CPUID results — avoid redundant kernel transitions for
        // features that share the same (leaf, subleaf).
        var cpuidCache = new Dictionary<(int Leaf, int SubLeaf), (int Eax, int Ebx, int Ecx, int Edx)>();

        foreach (var feature in HardwareDatabase.CpuFeaturesDatabase)
        {
            bool supportedLeaf = (uint)feature.Leaf >= 0x80000000
                ? (uint)maxExt >= (uint)feature.Leaf
                : maxLeaf >= feature.Leaf;

            if (!supportedLeaf)
            {
                continue;
            }

            if (!cpuidCache.TryGetValue((feature.Leaf, feature.SubLeaf), out var cpuid))
            {
                cpuid = X86Base.CpuId(feature.Leaf, feature.SubLeaf);
                cpuidCache[(feature.Leaf, feature.SubLeaf)] = cpuid;
            }

            int registerValue = feature.Register switch
            {
                HardwareDatabase.CpuidRegister.Eax => cpuid.Eax,
                HardwareDatabase.CpuidRegister.Ebx => cpuid.Ebx,
                HardwareDatabase.CpuidRegister.Ecx => cpuid.Ecx,
                HardwareDatabase.CpuidRegister.Edx => cpuid.Edx,
                _ => 0
            };

            if ((registerValue & (1 << feature.Bit)) != 0)
            {
                instructions.Add(feature.Name);
            }
        }

        return instructions;
    }

    // ── CPU rule index ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a two-level index over CpuRulesDatabase:
    ///   vendor (lowercase) → family → rules list
    /// Replaces O(n) FirstOrDefault with two O(1) dictionary lookups +
    /// a short linear scan over rules sharing the same vendor+family (typically 1–3).
    /// Built once at static init; never modified afterward.
    /// </summary>
    private static Dictionary<string, Dictionary<int, List<HardwareDatabase.CpuModelRule>>>
        BuildCpuRuleIndex()
    {
        var index = new Dictionary<string, Dictionary<int, List<HardwareDatabase.CpuModelRule>>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var rule in HardwareDatabase.CpuRulesDatabase)
        {
            if (!index.TryGetValue(rule.Vendor, out var byFamily))
            {
                byFamily = new Dictionary<int, List<HardwareDatabase.CpuModelRule>>();
                index[rule.Vendor] = byFamily;
            }

            if (!byFamily.TryGetValue(rule.Family, out var list))
            {
                list = new List<HardwareDatabase.CpuModelRule>();
                byFamily[rule.Family] = list;
            }

            list.Add(rule);
        }

        return index;
    }

    /// <summary>
    /// Finds the first matching CPU rule for the given manufacturer, family, and model.
    /// Uses the pre-built index for O(1) vendor+family lookup, then scans the
    /// (usually tiny) per-family list for a model range match.
    /// </summary>
    private static HardwareDatabase.CpuModelRule? FindCpuRule(
        string manufacturer, int family, int model)
    {
        // Match any vendor keyword present in the manufacturer string.
        foreach (var (vendor, byFamily) in _cpuRuleIndex)
        {
            if (!manufacturer.Contains(vendor, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!byFamily.TryGetValue(family, out var rules))
            {
                return null;
            }

            foreach (var rule in rules)
            {
                if (model >= rule.MinModel && model <= rule.MaxModel)
                {
                    return rule;
                }
            }

            return null;
        }

        return null;
    }
}