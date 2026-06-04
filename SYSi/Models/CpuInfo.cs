using System;
using System.Collections.Generic;
using System.Text;

namespace SYSi.Models
{
    public class CpuInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public int PhysicalCores { get; set; }
        public int LogicalProcessors { get; set; }
        public double BaseSpeedGHz { get; set; }
        public double MaxSpeedGHz { get; set; }
        public string L1Cache { get; set; } = string.Empty;
        public string L2Cache { get; set; } = string.Empty;
        public string L3Cache { get; set; } = string.Empty;
        public string Socket { get; set; } = string.Empty;
        public bool VirtualizationEnabled { get; set; }
        public string ProcessorId { get; set; } = string.Empty;
        public string Stepping { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public double UsagePercent { get; set; }
        public double TemperatureCelsius { get; set; }
    }
}
