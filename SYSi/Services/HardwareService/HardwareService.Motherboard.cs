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
            ReadChipsetInfo(info);
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

    // ── CPU Microcode Revision (Registry) ───────────────────────────────────

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
            ref classGuid, IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT);

        if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(-1))
            return;

        try
        {
            var devData = new NativeMethods.SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.SP_DEVINFO_DATA>()
            };

            uint index = 0;
            while (NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, index++, ref devData))
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

                // Intel PCH — match trực tiếp, có codename + chipset SKU + bus specs
                if (venId.Equals("8086", StringComparison.OrdinalIgnoreCase)
                    && IntelPchLookup.TryGetValue(devId, out var intel))
                {
                    info.Chipset = intel.Codename;
                    info.Southbridge = intel.ChipsetName;
                    info.BusSpecs = intel.BusSpecs;
                    return;
                }

                // AMD FCH — chỉ suy ra codename + bus specs theo generation,
                // SKU chipset fallback từ tên board
                if (venId.Equals("1022", StringComparison.OrdinalIgnoreCase)
                    && AmdFchLookup.TryGetValue(devId, out var amd))
                {
                    info.Chipset = amd.Codename;
                    info.BusSpecs = amd.BusSpecs;
                    info.Southbridge = GuessAmdChipsetFromBoardName(info.Product);
                }
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

    // ── Intel PCH Lookup ─────────────────────────────────────────────────────

    private static readonly Dictionary<string, (string Codename, string ChipsetName, string BusSpecs)> IntelPchLookup = new()
    {
        // 700-series PCH — Raptor Lake / Raptor Lake Refresh — DMI 4.0 x8
        ["7A04"] = ("Intel Raptor Lake", "Intel Z790", "PCI-Express 4.0 (16.0 GT/s)"),
        ["7A06"] = ("Intel Raptor Lake", "Intel H770", "PCI-Express 4.0 (16.0 GT/s)"),
        ["7A08"] = ("Intel Raptor Lake", "Intel B760", "PCI-Express 4.0 (16.0 GT/s)"),
        ["7A0C"] = ("Intel Raptor Lake", "Intel Q670", "PCI-Express 4.0 (16.0 GT/s)"),
        ["7A14"] = ("Intel Raptor Lake", "Intel W680", "PCI-Express 4.0 (16.0 GT/s)"),

        // 600-series PCH — Alder Lake — DMI 4.0 x8
        ["7A84"] = ("Intel Alder Lake", "Intel Z690", "PCI-Express 4.0 (16.0 GT/s)"),
        ["7A86"] = ("Intel Alder Lake", "Intel H670", "PCI-Express 4.0 (16.0 GT/s)"),
        ["7A88"] = ("Intel Alder Lake", "Intel B660", "PCI-Express 4.0 (16.0 GT/s)"),
        ["7A8C"] = ("Intel Alder Lake", "Intel Q670", "PCI-Express 4.0 (16.0 GT/s)"),

        // 500-series PCH — Rocket Lake — DMI 3.0 x8
        ["A0FC"] = ("Intel Rocket Lake", "Intel Z590", "PCI-Express 3.0 (8.0 GT/s)"),
        ["A0FE"] = ("Intel Rocket Lake", "Intel H570", "PCI-Express 3.0 (8.0 GT/s)"),
        ["A0F0"] = ("Intel Rocket Lake", "Intel B560", "PCI-Express 3.0 (8.0 GT/s)"),
        ["A0F4"] = ("Intel Rocket Lake", "Intel Q570", "PCI-Express 3.0 (8.0 GT/s)"),

        // 400-series PCH — Comet Lake — DMI 3.0 x8
        ["0684"] = ("Intel Comet Lake", "Intel Z490", "PCI-Express 3.0 (8.0 GT/s)"),
        ["0687"] = ("Intel Comet Lake", "Intel H470", "PCI-Express 3.0 (8.0 GT/s)"),
        ["06A4"] = ("Intel Comet Lake", "Intel B460", "PCI-Express 3.0 (8.0 GT/s)"),
        ["06A1"] = ("Intel Comet Lake", "Intel Q470", "PCI-Express 3.0 (8.0 GT/s)"),
    };

    private static readonly Dictionary<string, (string Codename, string BusSpecs)> AmdFchLookup = new()
    {
        ["790B"] = ("AMD Zen FCH", "PCI-Express 4.0 (16.0 GT/s)"), // SMBus
        ["790E"] = ("AMD Zen FCH", "PCI-Express 4.0 (16.0 GT/s)"), // LPC Bridge
    };

    private static string GuessAmdChipsetFromBoardName(string? boardProduct)
    {
        if (string.IsNullOrWhiteSpace(boardProduct))
        {
            return "N/A";
        }

        string[] knownChipsets =
        [
            "X670E", "X670", "X870E", "X870",
            "B650E", "B650", "B850",
            "A620",
            "X570", "B550", "A520",
            "X470", "B450", "A320",
        ];

        foreach (var chip in knownChipsets)
        {
            if (boardProduct.Contains(chip, StringComparison.OrdinalIgnoreCase))
            {
                return $"AMD {chip}";
            }
        }

        return "N/A";
    }
}
