using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using SYSi.Models;

namespace SYSi.Services;

public class HardwareService
{
    private readonly PerformanceCounter _cpuCounter;

    private readonly List<PerformanceCounter> _gpuCounters = [];

    public HardwareService()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _cpuCounter.NextValue(); // first call returns 0

        InitializeGpuCounters();
    }

    private void InitializeGpuCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");

            foreach (string instance in category.GetInstanceNames())
            {
                if (!instance.Contains("engtype"))
                    continue;

                var counter = new PerformanceCounter(
                    "GPU Engine",
                    "Utilization Percentage",
                    instance);

                counter.NextValue();

                _gpuCounters.Add(counter);
            }
        }
        catch
        {
        }
    }

    // ─── CPU ────────────────────────────────────────────────────────────────

    public CpuInfo GetCpuInfo()
    {
        var info = new CpuInfo();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                info.Name = obj["Name"]?.ToString()?.Trim() ?? "N/A";
                info.Manufacturer = obj["Manufacturer"]?.ToString() ?? "N/A";
                info.PhysicalCores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                info.LogicalProcessors = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                info.BaseSpeedGHz = Convert.ToDouble(obj["MaxClockSpeed"] ?? 0) / 1000.0;
                info.MaxSpeedGHz = info.BaseSpeedGHz;
                info.Socket = obj["SocketDesignation"]?.ToString() ?? "N/A";
                info.Architecture = ParseCpuArchitecture(Convert.ToUInt16(obj["Architecture"] ?? 0));
                info.ProcessorId = obj["ProcessorId"]?.ToString() ?? "N/A";
                info.Stepping = obj["Stepping"]?.ToString() ?? "N/A";
                info.Family = obj["Family"]?.ToString() ?? "N/A";
                info.Model = obj["Name"]?.ToString()?.Trim() ?? "N/A";
                info.VirtualizationEnabled = Convert.ToBoolean(obj["VirtualizationFirmwareEnabled"] ?? false);

                // Cache
                uint l2 = Convert.ToUInt32(obj["L2CacheSize"] ?? 0);
                uint l3 = Convert.ToUInt32(obj["L3CacheSize"] ?? 0);
                info.L1Cache = "N/A"; // WMI doesn't expose L1 easily
                info.L2Cache = l2 > 0 ? $"{l2} KB" : "N/A";
                info.L3Cache = l3 > 0 ? FormatCacheSize(l3) : "N/A";
                break;
            }
        }
        catch { /* WMI error fallback */ }

        info.UsagePercent = GetCpuUsage();
        return info;
    }

    public double GetCpuUsage()
    {
        try { return Math.Round(_cpuCounter.NextValue(), 1); }
        catch { return 0; }
    }

    private static string ParseCpuArchitecture(ushort arch) => arch switch
    {
        0 => "x86",
        1 => "MIPS",
        2 => "Alpha",
        3 => "PowerPC",
        5 => "ARM",
        6 => "ia64",
        9 => "x64",
        12 => "ARM64",
        _ => "Unknown"
    };

    private static string FormatCacheSize(uint kb)
    {
        if (kb >= 1024) return $"{kb / 1024} MB";
        return $"{kb} KB";
    }

    // ─── GPU ────────────────────────────────────────────────────────────────

    public List<GpuInfo> GetGpuInfoList()
    {
        var list = new List<GpuInfo>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                var gpu = new GpuInfo
                {
                    Name = obj["Caption"]?.ToString()?.Trim() ?? "N/A",
                    Manufacturer = ParseGpuManufacturer(obj["AdapterCompatibility"]?.ToString() ?? ""),
                    DriverVersion = obj["DriverVersion"]?.ToString() ?? "N/A",
                    DriverDate = ParseWmiDate(obj["DriverDate"]?.ToString()),
                    VideoProcessor = obj["VideoProcessor"]?.ToString() ?? "N/A",
                    Resolution = $"{obj["CurrentHorizontalResolution"]}×{obj["CurrentVerticalResolution"]}",
                    RefreshRate = $"{obj["CurrentRefreshRate"]} Hz",
                    BitsPerPixel = $"{obj["CurrentBitsPerPixel"]} bit",
                    VideoArchitecture = ParseVideoArchitecture(Convert.ToUInt16(obj["VideoArchitecture"] ?? 0)),
                    VideoMemoryType = ParseVideoMemoryType(Convert.ToUInt16(obj["VideoMemoryType"] ?? 0)),
                    PnpDeviceId = obj["PNPDeviceID"]?.ToString() ?? "N/A",
                };

                ulong adapterRam = Convert.ToUInt64(obj["AdapterRAM"] ?? 0);
                gpu.VramText = adapterRam > 0 ? FormatBytes((long)adapterRam) : "N/A";
                list.Add(gpu);
            }
        }
        catch { }
        return list;
    }

    private static string ParseGpuManufacturer(string raw)
    {
        if (raw.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
        if (raw.Contains("AMD", StringComparison.OrdinalIgnoreCase) || raw.Contains("ATI", StringComparison.OrdinalIgnoreCase)) return "AMD";
        if (raw.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
        return string.IsNullOrWhiteSpace(raw) ? "N/A" : raw;
    }

    private static string ParseVideoArchitecture(ushort arch) => arch switch
    {
        1 => "Other", 2 => "Unknown", 3 => "CGA", 4 => "EGA",
        5 => "VGA", 6 => "SVGA", 7 => "MDA", 8 => "HGC",
        9 => "MCGA", 10 => "8514A", 11 => "XGA", 12 => "Linear Frame Buffer",
        160 => "PC-98", _ => "N/A"
    };

    private static string ParseVideoMemoryType(ushort t) => t switch
    {
        2 => "DRAM", 3 => "VRAM", 4 => "SRAM", 5 => "WRAM",
        6 => "EDO RAM", 7 => "Burst Synchronous DRAM", 8 => "Pipelined Burst SRAM",
        9 => "CDRAM", 10 => "3DRAM", 11 => "SDRAM", 12 => "SGRAM",
        13 => "RDRAM", 14 => "DDR", 15 => "DDR2", 16 => "GDDR2",
        17 => "GDDR3", 18 => "GDDR4", 19 => "DDR3", 20 => "GDDR5",
        21 => "HBM", 22 => "HBM2", 23 => "GDDR5X", 24 => "GDDR6",
        25 => "GDDR6X", _ => "N/A"
    };

    // ─── RAM ────────────────────────────────────────────────────────────────

    public RamInfo GetRamInfo()
    {
        var info = new RamInfo();
        try
        {
            // Physical memory total from OS
            using var csSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            ulong totalBytes = 0;
            foreach (ManagementObject obj in csSearcher.Get())
                totalBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"] ?? 0);

            // Available from perf
            using var osSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            ulong availKb = 0;
            foreach (ManagementObject obj in osSearcher.Get())
                availKb = Convert.ToUInt64(obj["FreePhysicalMemory"] ?? 0);

            ulong availBytes = availKb * 1024;
            ulong usedBytes = totalBytes - availBytes;
            double pct = totalBytes > 0 ? (double)usedBytes / totalBytes * 100 : 0;

            info.TotalText = FormatBytes((long)totalBytes);
            info.AvailableText = FormatBytes((long)availBytes);
            info.UsedText = FormatBytes((long)usedBytes);
            info.UsagePercent = Math.Round(pct, 1);

            // Per-slot info
            using var ramSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            List<string> speeds = new List<string>();
            string memoryType = string.Empty;

            foreach (ManagementObject obj in ramSearcher.Get())
            {
                var slot = new RamSlotInfo
                {
                    BankLabel = obj["BankLabel"]?.ToString() ?? "N/A",
                    Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "N/A",
                    SpeedMHz = Convert.ToUInt32(obj["Speed"] ?? 0),
                    MemoryType = ParseMemoryType(Convert.ToUInt16(obj["SMBIOSMemoryType"] ?? 0)),
                    FormFactor = ParseFormFactor(Convert.ToUInt16(obj["FormFactor"] ?? 0)),
                    PartNumber = obj["PartNumber"]?.ToString()?.Trim() ?? "N/A",
                    SerialNumber = obj["SerialNumber"]?.ToString()?.Trim() ?? "N/A",
                    DataWidth = Convert.ToUInt16(obj["DataWidth"] ?? 0),
                };

                speeds.Add(slot.SpeedMHz.ToString());

                if (string.IsNullOrEmpty(memoryType))
                {
                    memoryType = slot.MemoryType;
                }

                ulong cap = Convert.ToUInt64(obj["Capacity"] ?? 0);
                slot.CapacityText = FormatBytes((long)cap);
                info.Slots.Add(slot);
            }

            info.SpeedText = speeds.Count switch
            {
                0 => string.Empty,
                1 => $"{speeds[0]} MHz",
                _ => $"{string.Join("/", speeds)} MHz"
            };

            info.MemoryType = memoryType;
        }

        catch { }
        return info;
    }

    private static string ParseMemoryType(ushort t) => t switch
    {
        20 => "DDR", 21 => "DDR2", 22 => "DDR2 FB-DIMM",
        24 => "DDR3", 26 => "DDR4", 34 => "DDR5",
        _ => t > 0 ? $"Type {t}" : "N/A"
    };

    private static string ParseFormFactor(ushort f) => f switch
    {
        8 => "DIMM", 12 => "SO-DIMM", 13 => "RIMM",
        14 => "SODIMM", 15 => "SRIMM", _ => f > 0 ? $"Type {f}" : "N/A"
    };

    // ─── STORAGE ────────────────────────────────────────────────────────────

    public List<StorageDriveInfo> GetStorageInfo()
    {
        var drives = new List<StorageDriveInfo>();
        try
        {
            // Logical disks
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                var info = new StorageDriveInfo
                {
                    Letter = drive.Name,
                    VolumeLabel = drive.VolumeLabel,
                    FileSystem = drive.DriveFormat,
                    DriveType = drive.DriveType.ToString(),
                };
                long total = drive.TotalSize;
                long free = drive.AvailableFreeSpace;
                long used = total - free;
                info.TotalText = FormatBytes(total);
                info.FreeText = FormatBytes(free);
                info.UsedText = FormatBytes(used);
                info.UsagePercent = total > 0 ? Math.Round((double)used / total * 100, 1) : 0;
                drives.Add(info);
            }

            // Enrich with WMI disk model info (map by device ID)
            using var diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            var diskModels = new Dictionary<string, string>();
            var diskSerial = new Dictionary<string, string>();
            foreach (ManagementObject obj in diskSearcher.Get())
            {
                string? model = obj["Model"]?.ToString()?.Trim();
                string? serial = obj["SerialNumber"]?.ToString()?.Trim();
                string deviceId = obj["DeviceID"]?.ToString() ?? "";
                if (model != null) diskModels[deviceId] = model;
                if (serial != null) diskSerial[deviceId] = serial;
            }

            // Associate logical → physical via partition
            using var partSearcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_LogicalDiskToPartition");
            using var partDiskSearcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_DiskDriveToDiskPartition");

            var partitionToDisk = new Dictionary<string, string>();
            foreach (ManagementObject rel in partDiskSearcher.Get())
            {
                string? disk = rel["Antecedent"]?.ToString();
                string? part = rel["Dependent"]?.ToString();
                if (disk == null || part == null) continue;
                string diskId = ExtractWmiId(disk);
                string partId = ExtractWmiId(part);
                partitionToDisk[partId] = diskId;
            }

            var logicalToPartition = new Dictionary<string, string>();
            foreach (ManagementObject rel in partSearcher.Get())
            {
                string? part = rel["Antecedent"]?.ToString();
                string? logical = rel["Dependent"]?.ToString();
                if (part == null || logical == null) continue;
                string partId = ExtractWmiId(part);
                string logicalId = ExtractWmiId(logical);
                logicalToPartition[logicalId] = partId;
            }

            foreach (var d in drives)
            {
                string letter = d.Letter.TrimEnd('\\');
                if (logicalToPartition.TryGetValue(letter, out string? partId) &&
                    partitionToDisk.TryGetValue(partId, out string? diskId))
                {
                    d.Model = diskModels.GetValueOrDefault(diskId, "N/A");
                    d.SerialNumber = diskSerial.GetValueOrDefault(diskId, "N/A");
                }
                else
                {
                    d.Model = "N/A";
                    d.SerialNumber = "N/A";
                }
                d.Status = "OK";
                d.Interface = d.DriveType == "Fixed" ? "SATA/NVMe" : d.DriveType;
            }
        }
        catch { }
        return drives;
    }

    // ─── MOTHERBOARD ────────────────────────────────────────────────────────

    public MotherboardInfo GetMotherboardInfo()
    {
        var info = new MotherboardInfo();
        try
        {
            using var mbSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (ManagementObject obj in mbSearcher.Get())
            {
                info.Manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "N/A";
                info.Product = obj["Product"]?.ToString()?.Trim() ?? "N/A";
                info.Version = obj["Version"]?.ToString()?.Trim() ?? "N/A";
                info.SerialNumber = obj["SerialNumber"]?.ToString()?.Trim() ?? "N/A";
                break;
            }

            using var biosSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            foreach (ManagementObject obj in biosSearcher.Get())
            {
                info.BiosManufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "N/A";
                info.BiosVersion = obj["SMBIOSBIOSVersion"]?.ToString()?.Trim() ?? "N/A";
                info.BiosDate = ParseWmiDate(obj["ReleaseDate"]?.ToString());
                break;
            }

            using var csSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in csSearcher.Get())
            {
                info.SystemFamily = obj["SystemFamily"]?.ToString()?.Trim() ?? "N/A";
                info.SystemModel = obj["Model"]?.ToString()?.Trim() ?? "N/A";
                break;
            }
        }
        catch { }
        return info;
    }

    // ─── NETWORK ────────────────────────────────────────────────────────────

    public List<NetworkAdapterInfo> GetNetworkInfo()
    {
        var adapters = new List<NetworkAdapterInfo>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (nic.OperationalStatus == OperationalStatus.Unknown) continue;

                var props = nic.GetIPProperties();
                var info = new NetworkAdapterInfo
                {
                    Name = nic.Name,
                    AdapterType = nic.NetworkInterfaceType.ToString(),
                    MacAddress = FormatMac(nic.GetPhysicalAddress().ToString()),
                    IsConnected = nic.OperationalStatus == OperationalStatus.Up,
                    LinkSpeed = nic.Speed > 0 ? FormatSpeed(nic.Speed) : "N/A",
                };

                // IP
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        info.IpAddress = ua.Address.ToString();
                        info.SubnetMask = ua.IPv4Mask.ToString();
                    }
                    else if (ua.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        info.Ipv6Address = ua.Address.ToString();
                    }
                }

                // Gateway
                foreach (var gw in props.GatewayAddresses)
                    info.Gateway = gw.Address.ToString();

                // DNS
                info.DnsServers = string.Join(", ",
                    props.DnsAddresses.Select(a => a.ToString()));

                adapters.Add(info);
            }
        }
        catch { }
        return adapters;
    }

    // ─── HELPERS ────────────────────────────────────────────────────────────

    public static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "N/A";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double val = bytes;
        int idx = 0;
        while (val >= 1024 && idx < units.Length - 1) { val /= 1024; idx++; }
        return $"{val:F2} {units[idx]}";
    }

    private static string FormatSpeed(long bitsPerSec)
    {
        if (bitsPerSec >= 1_000_000_000) return $"{bitsPerSec / 1_000_000_000.0:F0} Gbps";
        if (bitsPerSec >= 1_000_000) return $"{bitsPerSec / 1_000_000:F0} Mbps";
        return $"{bitsPerSec / 1000} Kbps";
    }

    private static string FormatMac(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length != 12) return raw;
        return string.Join(":", Enumerable.Range(0, 6).Select(i => raw.Substring(i * 2, 2)));
    }

    private static string ParseWmiDate(string? wmiDate)
    {
        if (string.IsNullOrWhiteSpace(wmiDate) || wmiDate.Length < 8) return "N/A";
        try
        {
            string year = wmiDate[..4];
            string month = wmiDate[4..6];
            string day = wmiDate[6..8];
            return $"{day}/{month}/{year}";
        }
        catch { return wmiDate; }
    }

    private static string ExtractWmiId(string wmiPath)
    {
        // e.g. \\PC\root\cimv2:Win32_LogicalDisk.DeviceID="C:"
        int eq = wmiPath.LastIndexOf('=');
        if (eq >= 0) return wmiPath[(eq + 1)..].Trim('"');
        return wmiPath;
    }
}
