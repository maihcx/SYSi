namespace SYSi.Utils
{
    internal static class NativeMethods
    {
        // ── Constants ────────────────────────────────────────────────────────────

        public const uint DIGCF_PRESENT = 0x00000002;
        public const uint SPDRP_DEVICEDESC = 0x00000000;
        public const uint SPDRP_HARDWAREID = 0x00000001;
        public const uint SPDRP_DRIVER = 0x00000009;
        public const uint SPDRP_MFG = 0x0000000B;

        public const uint ENUM_CURRENT_SETTINGS = 0xFFFFFFFF;

        public const uint PDH_FMT_DOUBLE = 0x00000200;

        // ── kernel32 ────────────────────────────────────────────────────────────

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(
            out FILETIME idle, out FILETIME kernel, out FILETIME user);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetDiskFreeSpaceEx(
            string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetVolumeInformation(
            string lpRootPathName,
            System.Text.StringBuilder lpVolumeNameBuffer, uint nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            System.Text.StringBuilder lpFileSystemNameBuffer, uint nFileSystemNameSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetSystemFirmwareTable(
            uint FirmwareTableProviderSignature,
            uint FirmwareTableID,
            IntPtr pFirmwareTableBuffer,
            uint BufferSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetLogicalProcessorInformation(
            IntPtr Buffer, ref uint ReturnedLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetLogicalProcessorInformationEx(
            LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType,
            IntPtr Buffer,
            ref uint ReturnedLength);

        [DllImport("kernel32.dll")]
        public static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);

        // ── setupapi ────────────────────────────────────────────────────────────

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property, out uint PropertyRegDataType,
            byte[]? PropertyBuffer, uint PropertyBufferSize, out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        // ── user32 ──────────────────────────────────────────────────────────────

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplaySettings(
            string? lpszDeviceName, uint iModeNum, ref DEVMODE lpDevMode);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplayDevices(
            string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        // ── pdh ─────────────────────────────────────────────────────────────────

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        public static extern uint PdhOpenQuery(
            string? szDataSource, IntPtr dwUserData, out IntPtr phQuery);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        public static extern uint PdhAddCounter(
            IntPtr hQuery, string szFullCounterPath, IntPtr dwUserData, out IntPtr phCounter);

        [DllImport("pdh.dll")]
        public static extern uint PdhCollectQueryData(IntPtr hQuery);

        [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
        public static extern uint PdhGetFormattedCounterArray(
            IntPtr hCounter, uint dwFormat, ref uint lpdwBufferSize,
            out uint lpdwItemCount, IntPtr ItemBuffer);

        [DllImport("pdh.dll")]
        public static extern uint PdhCloseQuery(IntPtr hQuery);

        // ── Structs ─────────────────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
        {
            public UIntPtr ProcessorMask;
            public uint Relationship;  // 0 = RelationProcessorCore
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
            public uint Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CACHE_DESCRIPTOR
        {
            public byte Level;
            public byte Associativity;
            public ushort LineSize;
            public uint Size;
            public uint Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public ushort ProcessorArchitecture;
            public ushort Reserved;
            public uint PageSize;
            public IntPtr MinimumApplicationAddress;
            public IntPtr MaximumApplicationAddress;
            public IntPtr ActiveProcessorMask;
            public uint NumberOfProcessors;
            public uint ProcessorType;
            public uint AllocationGranularity;
            public ushort ProcessorLevel;
            public ushort ProcessorRevision;
        }

        /// <summary>DEVMODEW — 220 bytes, matches Windows DEVMODEW exactly.</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public ushort dmSpecVersion;
            public ushort dmDriverVersion;
            public ushort dmSize;
            public ushort dmDriverExtra;
            public uint dmFields;
            // Display union
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;
            // Shared
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public ushort dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;
            public uint dmDisplayFlags;
            public uint dmDisplayFrequency;
            // Extended (needed for correct dmSize = 220)
            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAY_DEVICE
        {
            public uint cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PDH_FMT_COUNTERVALUE
        {
            public uint CStatus;
            public double doubleValue;
        }

        // ── Enums ────────────────────────────────────────────────────────────────

        public enum LOGICAL_PROCESSOR_RELATIONSHIP
        {
            RelationProcessorCore = 0,
            RelationNumaNode = 1,
            RelationCache = 2,
            RelationProcessorPackage = 3,
            RelationGroup = 4,
            RelationAll = 0xFFFF,
        }
    }
}
