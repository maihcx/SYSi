namespace SYSi.Services.HardwareService;

public sealed partial class HardwareService
{
    // ── SMBIOS cache ─────────────────────────────────────────────────────────

    private static byte[]? _smbiosCache;
    private static readonly object _smbiosLock = new();

    private static byte[] GetSmbiosData()
    {
        lock (_smbiosLock)
        {
            if (_smbiosCache != null) return _smbiosCache;

            const uint RSMB = 0x52534D42;
            uint size = NativeMethods.GetSystemFirmwareTable(RSMB, 0, IntPtr.Zero, 0);
            if (size == 0) return _smbiosCache = [];

            IntPtr buf = Marshal.AllocHGlobal((int)size);
            try
            {
                NativeMethods.GetSystemFirmwareTable(RSMB, 0, buf, size);
                _smbiosCache = new byte[size];
                Marshal.Copy(buf, _smbiosCache, 0, (int)size);
            }
            finally { Marshal.FreeHGlobal(buf); }

            return _smbiosCache;
        }
    }

    /// <summary>Yields every SMBIOS structure of the requested type.</summary>
    internal static IEnumerable<SmbiosStruct> ParseSmbios(byte type)
    {
        byte[] raw = GetSmbiosData();
        if (raw.Length < 8) yield break;  // 8-byte firmware table header

        int offset = 8;
        while (offset < raw.Length - 4)
        {
            byte curType = raw[offset];
            byte length = raw[offset + 1];
            if (length < 4 || offset + length > raw.Length) break;

            // Locate double-null string terminator
            int strStart = offset + length;
            int strEnd = strStart;
            while (strEnd < raw.Length - 1)
            {
                if (raw[strEnd] == 0 && raw[strEnd + 1] == 0) { strEnd += 2; break; }
                strEnd++;
            }

            if (curType == type)
                yield return new SmbiosStruct(raw, offset, length,
                    ParseSmbiosStrings(raw, strStart, strEnd));

            offset = strEnd;
        }
    }

    private static List<string> ParseSmbiosStrings(byte[] raw, int start, int end)
    {
        var list = new List<string> { "" };  // SMBIOS strings are 1-based
        int i = start;
        while (i < end - 1)
        {
            int j = i;
            while (j < end && raw[j] != 0) j++;
            list.Add(Encoding.UTF8.GetString(raw, i, j - i));
            i = j + 1;
        }
        return list;
    }

    // ── SmbiosStruct ─────────────────────────────────────────────────────────

    internal record SmbiosStruct(byte[] Raw, int Offset, int Length, List<string> Strings)
    {
        public byte Byte(int fieldOffset) => Raw[Offset + fieldOffset];
        public ushort Word(int fieldOffset) => BitConverter.ToUInt16(Raw, Offset + fieldOffset);

        public string Str(int fieldOffset)
        {
            int idx = Raw[Offset + fieldOffset];
            return idx > 0 && idx < Strings.Count ? Strings[idx] : "N/A";
        }
    }
}
