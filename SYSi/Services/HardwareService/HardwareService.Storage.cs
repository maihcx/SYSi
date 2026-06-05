namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    public List<StorageDriveInfo> GetStorageInfo()
    {
        var drives = new List<StorageDriveInfo>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;

                var info = BuildDriveInfo(drive);
                drives.Add(info);
            }

            EnrichDrivesWithModel(drives);
        }
        catch { }
        return drives;
    }

    // ── Build ────────────────────────────────────────────────────────────────

    private static StorageDriveInfo BuildDriveInfo(DriveInfo drive)
    {
        string root    = drive.Name;
        var    volName = new StringBuilder(261);
        var    fsName  = new StringBuilder(261);

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
            long used         = (long)(total - freeAvail);
            info.TotalText    = FormatBytes((long)total);
            info.FreeText     = FormatBytes((long)freeAvail);
            info.UsedText     = FormatBytes(used);
            info.UsagePercent = total > 0 ? Math.Round((double)used / total * 100, 1) : 0;
        }

        return info;
    }

    // ── Model name from Registry (HARDWARE\DEVICEMAP\Scsi) ──────────────────

    private static void EnrichDrivesWithModel(List<StorageDriveInfo> drives)
    {
        var models = ReadScsiModels();
        int idx    = 0;

        foreach (var d in drives.Where(d => d.DriveType == "Fixed"))
            d.Model = idx < models.Count ? models[idx++] : "N/A";

        foreach (var d in drives.Where(d => string.IsNullOrEmpty(d.Model)))
            d.Model = "N/A";
    }

    private static List<string> ReadScsiModels()
    {
        var models = new List<string>();
        for (int port = 0; port < 8; port++)
        {
            try
            {
                string path = $@"HARDWARE\DEVICEMAP\Scsi\Scsi Port {port}\Scsi Bus 0\Target Id 0\Logical Unit Id 0";
                using var key = Registry.LocalMachine.OpenSubKey(path);
                string? model = key?.GetValue("Identifier")?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(model) && model != "N/A")
                    models.Add(model);
            }
            catch { }
        }
        return models;
    }
}
