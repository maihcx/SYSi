namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    public MotherboardInfo GetMotherboardInfo()
    {
        var info = new MotherboardInfo();
        try
        {
            ReadBaseboard(info);
            ReadBios(info);
            ReadSystemInfo(info);
            ReadBiosMicrocode(info);
        }
        catch { }
        return info;
    }

    // ── SMBIOS Type 2 — Base Board ───────────────────────────────────────────

    private static void ReadBaseboard(MotherboardInfo info)
    {
        foreach (var s in ParseSmbios(2))
        {
            info.Manufacturer = s.Str(0x04);
            info.Product      = s.Str(0x05);
            info.Version      = s.Str(0x06);
            info.SerialNumber = s.Str(0x07);
            break;
        }
    }

    // ── SMBIOS Type 0 — BIOS Information ────────────────────────────────────

    private static void ReadBios(MotherboardInfo info)
    {
        foreach (var s in ParseSmbios(0))
        {
            info.BiosManufacturer = s.Str(0x04);
            info.BiosVersion      = s.Str(0x05);
            info.BiosDate         = s.Length > 0x08 ? s.Str(0x08) : "N/A";
            break;
        }
    }

    // ── SMBIOS Type 1 — System Information ──────────────────────────────────

    private static void ReadSystemInfo(MotherboardInfo info)
    {
        foreach (var s in ParseSmbios(1))
        {
            info.SystemModel  = s.Str(0x05);
            info.SystemFamily = s.Length > 0x1A ? s.Str(0x1A) : "N/A";
            break;
        }
    }

    public static void ReadBiosMicrocode(MotherboardInfo info)
    {
        string microcode = "N/A";
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");

            if (key?.GetValue("Update Revision") is byte[] data && data.Length >= 4)
            {
                uint revision = BitConverter.ToUInt32(data, 0);
                microcode = $"0x{revision:X}";
            }
        }
        catch { }

        info.Microcode = microcode;
    }
}
