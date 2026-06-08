using System.Runtime.Intrinsics.X86;

namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    // ── State ────────────────────────────────────────────────────────────────

    private long _prevIdle, _prevKernel, _prevUser;
    private readonly object _cpuLock = new();

    // Constructor initialises the first timing sample so the first delta is valid.
    public HardwareService() => ReadSystemTimes(out _prevIdle, out _prevKernel, out _prevUser);

    // ── Public API ───────────────────────────────────────────────────────────

    public CpuInfo GetCpuInfo()
    {
        var info = new CpuInfo();
        try
        {
            ReadBasicCpuInfo(info);
            info.LogicalProcessors = Environment.ProcessorCount;
            info.PhysicalCores     = GetPhysicalCoreCount();
            EnrichCpuFromSmbios(info);
        }
        catch { }

        info.UsagePercent = GetCpuUsage();
        return info;
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
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
            @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        if (key == null) return;

        info.Name         = ParseCpuName(key.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "N/A");
        info.Manufacturer = key.GetValue("VendorIdentifier")?.ToString() ?? "N/A";
        double mhz        = Convert.ToDouble(key.GetValue("~MHz") ?? 0);
        info.BaseSpeedGHz = $"{Math.Round(mhz / 1000.0, 2)} GHz";
        info.MaxSpeedGHz  = info.BaseSpeedGHz;
    }

    private static void ReadSystemTimes(out long idle, out long kernel, out long user)
    {
        NativeMethods.GetSystemTimes(out var fi, out var fk, out var fu);
        idle   = ToLong(fi);
        kernel = ToLong(fk);
        user   = ToLong(fu);
    }

    private static long ToLong(NativeMethods.FILETIME ft)
        => ((long)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

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

                int size  = Marshal.SizeOf<NativeMethods.SYSTEM_LOGICAL_PROCESSOR_INFORMATION>();
                int count = 0;
                for (int i = 0; i + size <= (int)len; i += size)
                {
                    var item = Marshal.PtrToStructure<NativeMethods.SYSTEM_LOGICAL_PROCESSOR_INFORMATION>(buf + i);
                    if (item.Relationship == 0) count++;  // RelationProcessorCore
                }
                return count > 0 ? count : Environment.ProcessorCount;
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        catch { return Environment.ProcessorCount; }
    }

    private static string GetCpuArchitecture()
    {
        NativeMethods.GetNativeSystemInfo(out var si);
        return si.ProcessorArchitecture switch
        {
            0  => "x86",
            5  => "ARM",
            6  => "Itanium",
            9  => "x64",
            12 => "ARM64",
            _  => $"Unknown ({si.ProcessorArchitecture})",
        };
    }

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
                    IntPtr cur    = IntPtr.Add(buf, (int)offset);
                    var    header = Marshal.PtrToStructure<NativeMethods.SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(cur);

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

    private static (int Family, int Model, int Stepping, string ProcessorId) GetCpuSignature()
    {
        if (!X86Base.IsSupported) return (0, 0, 0, "N/A");

        var cpuid = X86Base.CpuId(1, 0);
        int eax   = cpuid.Eax;

        int stepping  = eax & 0xF;
        int model     = (eax >> 4)  & 0xF;
        int family    = (eax >> 8)  & 0xF;
        int extModel  = (eax >> 16) & 0xF;
        int extFamily = (eax >> 20) & 0xFF;

        int displayFamily = family == 0xF ? family + extFamily : family;
        int displayModel  = (family == 0x6 || family == 0xF)
            ? model + (extModel << 4) : model;

        return (displayFamily, displayModel, stepping,
            $"{(uint)cpuid.Edx:X8}{(uint)cpuid.Eax:X8}");
    }

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
        info.Family      = sig.Family    > 0 ? $"{sig.Family:X}"    : "N/A";
        info.Model       = sig.Model     > 0 ? $"{sig.Model:X}"     : "N/A";
        info.Stepping    = sig.Stepping  > 0 ? $"{sig.Stepping:X}"  : "N/A";
        info.ProcessorId = sig.ProcessorId;
    }

    private static string ParseCpuName(string cpuName)
    {
        return cpuName.Replace("Intel(R) Core(TM)", "Core");
    }
}
