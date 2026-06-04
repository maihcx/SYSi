using System;
using System.Collections.Generic;
using System.Text;

namespace SYSi.Models
{
    public class GpuInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string VramText { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public string DriverDate { get; set; } = string.Empty;
        public string VideoProcessor { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public string RefreshRate { get; set; } = string.Empty;
        public string BitsPerPixel { get; set; } = string.Empty;
        public string VideoArchitecture { get; set; } = string.Empty;
        public string VideoMemoryType { get; set; } = string.Empty;
        public string PnpDeviceId { get; set; } = string.Empty;
        public double UsagePercent { get; set; }
    }
}
