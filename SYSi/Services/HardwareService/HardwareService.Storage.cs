namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    public List<StorageDriveInfo> GetStorageInfo()
    {
        var drives = new List<StorageDriveInfo>();
        try
        {
            // Build basic info for all ready drives
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                drives.Add(BuildDriveInfo(drive));
            }

            // Enrich with model names — each drive opens a handle independently,
            // so they can run in parallel safely
            EnrichDrivesWithModel(drives);
        }
        catch { }
        return drives;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private static StorageDriveInfo BuildDriveInfo(DriveInfo drive)
    {
        string root = drive.Name;
        var volName = new StringBuilder(261);
        var fsName = new StringBuilder(261);

        NativeMethods.GetVolumeInformation(
            root, volName, 261, out _, out _, out _, fsName, 261);

        var info = new StorageDriveInfo
        {
            Letter      = root,
            VolumeLabel = volName.ToString(),
            FileSystem  = fsName.ToString(),
            DriveType   = drive.DriveType.ToString(),
            Status      = "OK",
            Interface   = drive.DriveType == DriveType.Fixed ? "SATA/NVMe" : drive.DriveType.ToString(),
        };

        if (NativeMethods.GetDiskFreeSpaceEx(root, out ulong freeAvail, out ulong total, out _))
        {
            long used = (long)(total - freeAvail);
            info.TotalText    = FormatBytes((long)total);
            info.FreeText     = FormatBytes((long)freeAvail);
            info.UsedText     = FormatBytes(used);
            info.UsagePercent = total > 0 ? Math.Round((double)used / total * 100, 1) : 0;
        }

        return info;
    }

    // ── Model name enrichment (parallel per-drive) ────────────────────────────

    private static void EnrichDrivesWithModel(List<StorageDriveInfo> drives)
    {
        // Resolve disk numbers in parallel — each call opens its own handle
        var diskNumbers = new int[drives.Count];
        Parallel.For(0, drives.Count, i =>
            diskNumbers[i] = GetDiskNumber(drives[i].Letter));

        // Model lookup by disk number — deduplicate with a shared cache
        // ConcurrentDictionary avoids locking on the common case (cache hit)
        var modelCache = new System.Collections.Concurrent.ConcurrentDictionary<int, string>();

        Parallel.For(0, drives.Count, i =>
        {
            int disk = diskNumbers[i];
            drives[i].Model = disk < 0
                ? "N/A"
                : modelCache.GetOrAdd(disk, GetDiskModel);
        });
    }

    private static int GetDiskNumber(string driveLetter)
    {
        try
        {
            using var handle = NativeMethods.CreateFile(
                @"\\.\" + driveLetter.TrimEnd('\\'),
                0, FileShare.ReadWrite, IntPtr.Zero,
                FileMode.Open, 0, IntPtr.Zero);

            if (handle.IsInvalid) return -1;

            int size = Marshal.SizeOf<NativeMethods.VOLUME_DISK_EXTENTS>();
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                if (!NativeMethods.DeviceIoControl(
                        handle,
                        NativeMethods.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS,
                        IntPtr.Zero, 0, buffer, (uint)size, out _, IntPtr.Zero))
                    return -1;

                return (int)Marshal.PtrToStructure<NativeMethods.VOLUME_DISK_EXTENTS>(buffer)
                                   .Extents.DiskNumber;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        catch { return -1; }
    }

    private static string GetDiskModel(int diskNumber)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\disk\Enum");

            string? value = key?.GetValue(diskNumber.ToString())?.ToString();

            return string.IsNullOrWhiteSpace(value) ? "N/A" : NormalizeModel(value);
        }
        catch { return "N/A"; }
    }

    private static string NormalizeModel(string pnpId)
    {
        if (string.IsNullOrWhiteSpace(pnpId)) return "N/A";

        int ven = pnpId.IndexOf("Ven_", StringComparison.OrdinalIgnoreCase);
        int prod = pnpId.IndexOf("&Prod_", StringComparison.OrdinalIgnoreCase);

        if (ven < 0 || prod < 0) return pnpId;

        string vendor = pnpId.Substring(ven + 4, prod - (ven + 4));
        string product = pnpId[(prod + 6)..];

        int slash = product.IndexOf('\\');
        if (slash >= 0) product = product[..slash];

        return $"{vendor} {product}".Replace('_', ' ').Trim();
    }
}