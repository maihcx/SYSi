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
        info.CurrentClockGHz = GetCurrentCpuSpeedGHz();
    }

    /// <summary>Delta-based CPU usage via GetSystemTimes — no PerformanceCounter overhead.</summary>
    public double GetCpuUsage()
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
            if (total <= 0) return 0;
            return Math.Round(Math.Clamp((1.0 - (double)dIdle / total) * 100.0, 0, 100), 1);
        }
        catch { return 0; }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static void ReadBasicCpuInfo(CpuInfo info)
    {
        info.Name         = GetCpuBrandViaCpuid();
        info.ShortName    = ParseCpuName(info.Name);
        info.Manufacturer = GetCpuVendor();

        int baseMHz = GetCpuBaseSpeedViaCpuid();
        if (baseMHz == 0)
        {
            var searcher = new ManagementObjectSearcher("select CurrentClockSpeed from Win32_Processor");
            foreach (var item in searcher.Get())
            {
                baseMHz = Convert.ToInt32((uint)item["CurrentClockSpeed"]);
            }
        }

        info.BaseClockGHz = baseMHz > 0 ? $"{baseMHz / 1000.0:F2} GHz" : "N/A";
    }

    // ── PDH current clock ────────────────────────────────────────────────────

    private void InitCpuClockPdh()
    {
        try
        {
            _cpuBaseMHz = GetCpuBaseSpeedViaCpuid();
            if (_cpuBaseMHz == 0)
            {
                var searcher = new ManagementObjectSearcher("select CurrentClockSpeed from Win32_Processor");
                foreach (var item in searcher.Get())
                {
                    _cpuBaseMHz = Convert.ToInt32((uint)item["CurrentClockSpeed"]);
                }
            }

            if (_cpuBaseMHz == 0) return;

            lock (_cpuClockLock)
            {
                if (NativeMethods.PdhOpenQuery(null, 0, out _cpuClockQuery) != 0) return;
                NativeMethods.PdhAddEnglishCounter(
                    _cpuClockQuery,
                    @"\Processor Information(_Total)\% Processor Performance",
                    0, out _cpuClockCounter);
                NativeMethods.PdhCollectQueryData(_cpuClockQuery);
            }
        }
        catch { }
    }

    public string GetCurrentCpuSpeedGHz()
    {
        try
        {
            if (_cpuClockQuery == IntPtr.Zero || _cpuBaseMHz == 0) return string.Empty;

            lock (_cpuClockLock)
            {
                if (NativeMethods.PdhCollectQueryData(_cpuClockQuery) != 0) return string.Empty;

                var value = new NativeMethods.PDH_FMT_COUNTERVALUE();
                if (NativeMethods.PdhGetFormattedCounterValue(
                        _cpuClockCounter,
                        NativeMethods.PDH_FMT_DOUBLE,
                        out _, out value) != 0)
                {
                    return string.Empty;
                }

                // % Processor Performance × base = current MHz
                double currentMHz = value.doubleValue / 100.0 * _cpuBaseMHz;
                return $"{currentMHz / 1000.0:F2} GHz";
            }
        }
        catch { return string.Empty; }
    }

    public void DisposeCpuClockPdh()
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
        if (maxLeaf < 0x16) return 0;
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
            sb.Append(b == 0 ? ' ' : (char)b);
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
            if (len == 0) return Environment.ProcessorCount;

            IntPtr buf = Marshal.AllocHGlobal((int)len);
            try
            {
                if (!NativeMethods.GetLogicalProcessorInformation(buf, ref len))
                    return Environment.ProcessorCount;

                int size = Marshal.SizeOf<NativeMethods.SYSTEM_LOGICAL_PROCESSOR_INFORMATION>();
                int count = 0;
                for (int i = 0; i + size <= (int)len; i += size)
                {
                    var item = Marshal.PtrToStructure<NativeMethods.SYSTEM_LOGICAL_PROCESSOR_INFORMATION>(buf + i);
                    if (item.Relationship == 0) count++;
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
            if (len == 0) return ("N/A", "N/A", "N/A");

            IntPtr buf = Marshal.AllocHGlobal((int)len);
            try
            {
                if (!NativeMethods.GetLogicalProcessorInformationEx(
                    NativeMethods.LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache, buf, ref len))
                    return ("N/A", "N/A", "N/A");

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
        if (!X86Base.IsSupported) return (0, 0, 0, "N/A");

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
            if (s.Length > 0x08) info.Socket = s.Str(0x04);
            break;
        }

        (info.L1Cache, info.L2Cache, info.L3Cache) = GetCpuCaches();
        info.Architecture = GetCpuArchitecture();

        var sig = GetCpuSignature();
        info.Family      = sig.Family   > 0 ? $"{sig.Family:X}" : "N/A";
        info.Model       = sig.Model    > 0 ? $"{sig.Model:X}" : "N/A";
        info.Stepping    = $"{sig.Stepping:X}";
        info.ProcessorId = sig.ProcessorId;
    }

    // ── Virtualization ───────────────────────────────────────────────────────

    private static bool GetVirtualizationEnabled()
    {
        if (!X86Base.IsSupported) return false;

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
}