namespace SYSi.Services.HardwareService;

/// <summary>
/// Reads hardware information via Win32 API / P/Invoke.
/// Split across partial files by hardware category:
///   HardwareService.Cpu.cs
///   HardwareService.Gpu.cs
///   HardwareService.Ram.cs
///   HardwareService.Storage.cs
///   HardwareService.Motherboard.cs
///   HardwareService.Network.cs
/// </summary>
public sealed partial class HardwareService : IDisposable
{
    public void Dispose() => DisposeGpuPdh();

    // ── Registry helper ──────────────────────────────────────────────────────

    internal static string GetDeviceProperty(
        IntPtr devInfo, ref NativeMethods.SP_DEVINFO_DATA devData, uint property)
    {
        NativeMethods.SetupDiGetDeviceRegistryProperty(
            devInfo, ref devData, property, out _, null, 0, out uint needed);

        if (needed == 0) return "N/A";

        var buf = new byte[needed];
        NativeMethods.SetupDiGetDeviceRegistryProperty(
            devInfo, ref devData, property, out _, buf, needed, out _);

        return Encoding.Unicode.GetString(buf).TrimEnd('\0', ' ');
    }

    // ── Format helpers ───────────────────────────────────────────────────────

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "N/A";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        return $"{v:F2} {units[i]}";
    }

    internal static string FormatBytes(uint bytes) => FormatBytes((long)bytes);

    internal static string FormatNetworkSpeed(long bps)
    {
        if (bps >= 1_000_000_000) return $"{bps / 1_000_000_000.0:F0} Gbps";
        if (bps >= 1_000_000)     return $"{bps / 1_000_000:F0} Mbps";
        return $"{bps / 1000} Kbps";
    }

    internal static string FormatMac(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length != 12) return raw;
        return string.Join(":", Enumerable.Range(0, 6).Select(i => raw.Substring(i * 2, 2)));
    }

    /// <summary>
    /// Reads a registry value that may be stored as string, byte[] (Unicode), or int/long.
    /// Returns "N/A" when absent or unrecognised.
    /// </summary>
    internal static string RegistryString(object? value) => value switch
    {
        string s  => s,
        byte[] b  => Encoding.Unicode.GetString(b).TrimEnd('\0'),
        int i     => i.ToString(),
        long l    => l.ToString(),
        _         => "N/A",
    };
}
