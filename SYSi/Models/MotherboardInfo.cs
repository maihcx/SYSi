using System;
using System.Collections.Generic;
using System.Text;

namespace SYSi.Models
{
    public class MotherboardInfo
    {
        public string Manufacturer { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string BiosManufacturer { get; set; } = string.Empty;
        public string BiosVersion { get; set; } = string.Empty;
        public string BiosDate { get; set; } = string.Empty;
        public string SystemFamily { get; set; } = string.Empty;
        public string SystemModel { get; set; } = string.Empty;
    }
}
