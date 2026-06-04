using System;
using System.Collections.Generic;
using System.Text;

namespace SYSi.Models
{
    public class StorageDriveInfo
    {
        public string Letter { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string TotalText { get; set; } = string.Empty;
        public string FreeText { get; set; } = string.Empty;
        public string UsedText { get; set; } = string.Empty;
        public double UsagePercent { get; set; }
        public string FileSystem { get; set; } = string.Empty;
        public string Interface { get; set; } = string.Empty;
        public string DriveType { get; set; } = string.Empty;
        public string Firmware { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string VolumeLabel { get; set; } = string.Empty;
    }
}
