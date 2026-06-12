namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    public RamInfo GetRamInfo()
    {
        var info = new RamInfo();
        try
        {
            ReadRamUsage(info);
            ReadRamSlots(info);
        }
        catch { }
        return info;
    }

    public void RefreshRamInfo(RamInfo info)
    {
        ReadRamUsage(info);
    }

    // ── Usage (GlobalMemoryStatusEx) ─────────────────────────────────────────

    private static void ReadRamUsage(RamInfo info)
    {
        var status = new NativeMethods.MEMORYSTATUSEX
        { dwLength = (uint)Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>() };

        if (!NativeMethods.GlobalMemoryStatusEx(ref status))
        {
            return;
        }

        ulong total = status.ullTotalPhys;
        ulong avail = status.ullAvailPhys;
        ulong used = total - avail;

        info.TotalText     = FormatBytes((long)total);
        info.AvailableText = FormatBytes((long)avail);
        info.UsedText      = FormatBytes((long)used);
        info.UsagePercent  = total > 0 ? Math.Round((double)used / total * 100, 1) : 0;
    }

    // ── Per-slot info (SMBIOS Type 17 — Memory Device) ───────────────────────

    private static void ReadRamSlots(RamInfo info)
    {
        var speeds = new List<string>();
        int total = 0;
        int occupied = 0;

        foreach (var s in ParseSmbios(17))
        {
            total++;

            if (s.Length < 0x1C)
            {
                continue;
            }

            ushort size = s.Word(0x0C);
            if (size == 0 || size == 0xFFFF)
            {
                continue;
            }

            occupied++;

            long capBytes = size == 0x7FFF
                ? (long)s.Word(0x1C) * 1024L * 1024L
                : (long)size         * 1024L * 1024L;

            ushort speedMhz = s.Word(0x15);

            // Configured (actual running) speed — SMBIOS 2.7+ offset 0x20
            ushort configuredSpeed = s.Length > 0x21 ? s.Word(0x20) : (ushort)0;

            // Voltage fields — SMBIOS 2.8+ (offsets 0x22, 0x23, 0x25 — each 2 bytes)
            ushort configuredVoltage = s.Length > 0x27 ? s.Word(0x26) : (ushort)0;
            ushort minVoltage = s.Length > 0x23 ? s.Word(0x22) : (ushort)0;
            ushort maxVoltage = s.Length > 0x25 ? s.Word(0x24) : (ushort)0;

            byte rank = s.Length > 0x1B ? (byte)(s.Byte(0x1B) & 0x0F) : (byte)0;

            string deviceLocator = s.Str(0x10);
            string label = deviceLocator.Contains('-')
                ? deviceLocator.Split('-').Last()
                : deviceLocator;

            var slot = new RamSlotInfo
            {
                CapacityText       = FormatBytes(capBytes),
                SpeedMHz           = speedMhz,
                ConfiguredSpeedMHz = configuredSpeed > 0 ? configuredSpeed : speedMhz,
                ConfiguredVoltage  = configuredVoltage,
                MinVoltage         = minVoltage,
                MaxVoltage         = maxVoltage,
                Rank               = rank,
                MemoryType         = ParseMemoryType(s.Byte(0x12)),
                FormFactor         = ParseFormFactor(s.Byte(0x0E)),
                BankLabel          = label != "N/A" ? label : $"Slot {info.Slots.Count + 1}",
                Manufacturer       = s.Length > 0x17 ? s.Str(0x17) : "N/A",
                PartNumber         = s.Length > 0x1A ? s.Str(0x1A) : "N/A",
                SerialNumber       = s.Length > 0x18 ? s.Str(0x18) : "N/A",
                DataWidth          = s.Length > 0x0B ? s.Word(0x0A) : (ushort)0,
            };

            if (speedMhz > 0)
            {
                speeds.Add((configuredSpeed > 0 ? configuredSpeed : speedMhz).ToString());
            }

            info.Slots.Add(slot);

            if (string.IsNullOrEmpty(info.MemoryType))
            {
                info.MemoryType = slot.MemoryType;
            }
        }

        info.TotalSlots     = total;
        info.PopulatedSlots = occupied;
        info.ChannelMode    = DetectChannelMode(occupied);

        info.SpeedText = speeds.Count switch
        {
            0 => string.Empty,
            1 => $"{speeds[0]} MHz",
            _ => $"{string.Join("/", speeds.Distinct())} MHz",
        };
    }

    private static string DetectChannelMode(int populated) => populated switch
    {
        <= 0 => string.Empty,
        1 => "Single",
        2 => "Dual",
        4 => "Quad",
        _ => $"{populated}-channel?",
    };

    // ── SMBIOS decode tables ─────────────────────────────────────────────────

    private static string ParseMemoryType(byte t) => t switch
    {
        0x12 => "DDR",
        0x13 => "DDR2",
        0x14 => "DDR2 FB-DIMM",
        0x18 => "DDR3",
        0x1A => "DDR4",
        0x20 => "LPDDR4",
        0x22 => "DDR5",
        0x23 => "LPDDR5",
        _ => t > 0 ? $"Type {t}" : "N/A",
    };

    private static string ParseFormFactor(byte f) => f switch
    {
        0x09 => "DIMM",
        0x0D => "SO-DIMM",
        0x0F => "RIMM",
        0x11 => "FB-DIMM",
        _ => f > 0 ? $"Type {f}" : "N/A",
    };
}
