using System;
using System.Collections.Generic;
using System.Text;

namespace SYSi.Models
{
    public class RamSlotInfo
    {
        public string BankLabel { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string CapacityText { get; set; } = string.Empty;
        public uint SpeedMHz { get; set; }
        public string MemoryType { get; set; } = string.Empty;
        public string FormFactor { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public ushort DataWidth { get; set; }
    }

    public class RamInfo
    {
        public string TotalText { get; set; } = string.Empty;
        public string AvailableText { get; set; } = string.Empty;
        public string UsedText { get; set; } = string.Empty;
        public double UsagePercent { get; set; }
        public string SpeedText { get; set; } = string.Empty;
        public string MemoryType { get; set; } = string.Empty;
        public List<RamSlotInfo> Slots { get; set; } = new();
    }
}
