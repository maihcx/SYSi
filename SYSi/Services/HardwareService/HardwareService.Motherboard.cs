namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    public MotherboardInfo GetMotherboardInfo()
    {
        var info = new MotherboardInfo();

        ReadBaseboard(info);
        ReadBios(info);
        ReadSystemInfo(info);
        ReadBiosMicrocode(info);
        ReadChipsetInfo(info);

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

    // ── CPU Microcode Revision (Registry) ───────────────────────────────────

    public static void ReadBiosMicrocode(MotherboardInfo info)
    {
        string microcode = "N/A";

        using var key = Registry.LocalMachine.OpenSubKey(
            @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");

        if (key?.GetValue("Update Revision") is byte[] data && data.Length >= 4)
        {
            uint revision = BitConverter.ToUInt32(data, 0);
            microcode = $"0x{revision:X}";
        }

        info.Microcode = microcode;
    }

    // ── Chipset / Southbridge (PCI Device Enumeration) ──────────────────────

    private static readonly Guid GUID_DEVCLASS_SYSTEM =
        new("4d36e97d-e325-11ce-bfc1-08002be10318");

    private static void ReadChipsetInfo(MotherboardInfo info)
    {
        info.Chipset = "N/A";
        info.Southbridge = "N/A";
        info.BusSpecs = "N/A";

        var classGuid = GUID_DEVCLASS_SYSTEM;
        IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(
            ref classGuid,
            IntPtr.Zero,
            IntPtr.Zero,
            NativeMethods.DIGCF_PRESENT);

        if (deviceInfoSet == IntPtr.Zero ||
            deviceInfoSet == new IntPtr(-1))
        {
            return;
        }

        try
        {
            var devData = new NativeMethods.SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.SP_DEVINFO_DATA>()
            };

            uint index = 0;

            while (NativeMethods.SetupDiEnumDeviceInfo(
                       deviceInfoSet,
                       index++,
                       ref devData))
            {
                string? hwId = GetHardwareId(deviceInfoSet, ref devData);
                if (hwId == null)
                {
                    continue;
                }

                string? venId = ExtractField(hwId, "VEN_");
                string? devId = ExtractField(hwId, "DEV_");

                if (venId == null || devId == null)
                {
                    continue;
                }

                if (!HardwareDatabase.MotherboardChipsetInfoDatabase.TryGetValue(
                        venId,
                        out var vendorDatabase))
                {
                    continue;
                }

                if (!vendorDatabase.TryGetValue(
                        devId,
                        out var chipset))
                {
                    continue;
                }

                info.Chipset = chipset.Codename;
                info.BusSpecs = chipset.BusSpecs;

                info.Southbridge = chipset.ChipsetName ?? GuessAmdChipsetFromBoardName(info.Product);

                return;
            }
        }
        finally
        {
            NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }

    // ── PCI Helpers ──────────────────────────────────────────────────────────

    private static string? GetHardwareId(
        IntPtr deviceInfoSet, ref NativeMethods.SP_DEVINFO_DATA devData)
    {
        NativeMethods.SetupDiGetDeviceRegistryProperty(
            deviceInfoSet, ref devData, NativeMethods.SPDRP_HARDWAREID,
            out _, null, 0, out uint required);

        if (required == 0)
        {
            return null;
        }

        var buffer = new byte[required];
        if (!NativeMethods.SetupDiGetDeviceRegistryProperty(
                deviceInfoSet, ref devData, NativeMethods.SPDRP_HARDWAREID,
                out _, buffer, required, out _))
        {
            return null;
        }

        string full = System.Text.Encoding.Unicode.GetString(buffer);
        int nullIdx = full.IndexOf('\0');
        return nullIdx >= 0 ? full[..nullIdx] : full;
    }

    private static string? ExtractField(string hwId, string prefix)
    {
        int start = hwId.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        start += prefix.Length;

        int end = start;
        while (end < hwId.Length && Uri.IsHexDigit(hwId[end]))
        {
            end++;
        }

        return end > start ? hwId[start..end].ToUpperInvariant() : null;
    }

    private static string GuessAmdChipsetFromBoardName(string? boardProduct)
    {
        if (string.IsNullOrWhiteSpace(boardProduct))
        {
            return "N/A";
        }

        foreach (string chipset in HardwareDatabase.AmdChipsetDatabase)
        {
            if (boardProduct.Contains(chipset, StringComparison.OrdinalIgnoreCase))
            {
                return $"AMD {chipset}";
            }
        }

        return "N/A";
    }
}
