namespace SYSi.Services.HardwareService;

/// <summary>
/// Centralized hardware lookup database.
/// Each region represents a hardware subsystem (CPU, GPU, RAM, Motherboard, Chipset, Storage).
/// To add support for new hardware, update the corresponding region only.
/// </summary>
public static class HardwareDatabase
{
    #region CPU
    public sealed record CpuModelRule(
        string Vendor,
        int Family,
        int MinModel,
        int MaxModel,
        string CodeName,
        string Socket);

    public enum CpuidRegister
    {
        Eax,
        Ebx,
        Ecx,
        Edx
    }

    public sealed record CpuFeature(
        int Leaf,
        int SubLeaf,
        CpuidRegister Register,
        int Bit,
        string Name);

    public record EsSampleRule(
        int Family,
        int MinModel,
        int MaxModel,
        int Stepping,       // -1 = any
        int CoreCount,      // -1 = any
        int ThreadCount,    // -1 = any
        string RetailName,
        string? ProcessorIdMask = null   // null = skip; '?' = wildcard per char
    );

    public static readonly Dictionary<ushort, string> CpuArchitecturesDatabase = new()
    {
        [0]  = "x86",
        [5]  = "ARM",
        [6]  = "Itanium",
        [9]  = "x64",
        [12] = "ARM64"
    };

    public static readonly Dictionary<string, string> CpuTdpDatabase = new(StringComparer.OrdinalIgnoreCase)
    {
        // Intel 13th/14th Gen desktop
        ["i9-14900K"] = "125 W (253 W PL2)",
        ["i9-13900K"] = "125 W (253 W PL2)",
        ["i7-14700K"] = "125 W (253 W PL2)",
        ["i7-13700K"] = "125 W (253 W PL2)",
        ["i5-14600K"] = "125 W (181 W PL2)",
        ["i5-13600K"] = "125 W (181 W PL2)",

        // AMD Ryzen 7000/9000 desktop
        ["7950X3D"] = "120 W",
        ["7950X"]   = "170 W",
        ["7800X3D"] = "120 W",
        ["7700X"]   = "105 W",
        ["7600X"]   = "105 W",
        ["9950X"]   = "170 W",
        ["9700X"]   = "65 W",
    };

    public static readonly CpuModelRule[] CpuRulesDatabase =
    [
        // Intel Family 6
        new("Intel", 6, 0xBA, 0xBA, "Raptor Lake", "LGA1700"),
        new("Intel", 6, 0xB7, 0xB7, "Raptor Lake", "LGA1700"),
        new("Intel", 6, 0xB5, 0xB5, "Raptor Lake", "LGA1700"),

        new("Intel", 6, 0x97, 0x97, "Alder Lake", "LGA1700"),
        new("Intel", 6, 0x9A, 0x9A, "Alder Lake", "LGA1700"),

        new("Intel", 6, 0x8F, 0x8F, "Sapphire Rapids", "LGA4677"),

        new("Intel", 6, 0x8C, 0x8C, "Tiger Lake", "BGA"),
        new("Intel", 6, 0x8D, 0x8D, "Tiger Lake", "BGA"),

        new("Intel", 6, 0xA5, 0xA5, "Comet Lake", "LGA1200"),
        new("Intel", 6, 0xA6, 0xA6, "Comet Lake", "LGA1200"),

        new("Intel", 6, 0x9E, 0x9E, "Coffee Lake / Kaby Lake", "LGA1151"),
        new("Intel", 6, 0x9D, 0x9D, "Coffee Lake / Kaby Lake", "LGA1151"),

        new("Intel", 6, 0x55, 0x55, "Skylake-X", "LGA2066"),

        new("Intel", 6, 0x4E, 0x4E, "Skylake", "LGA1151"),
        new("Intel", 6, 0x5E, 0x5E, "Skylake", "LGA1151"),

        new("Intel", 6, 0x3D, 0x3D, "Broadwell", "LGA1150"),
        new("Intel", 6, 0x47, 0x47, "Broadwell", "LGA1150"),

        new("Intel", 6, 0x3C, 0x3C, "Haswell", "LGA1150"),
        new("Intel", 6, 0x45, 0x45, "Haswell", "LGA1150"),
        new("Intel", 6, 0x46, 0x46, "Haswell", "LGA1150"),

        new("Intel", 6, 0x3A, 0x3A, "Ivy Bridge", "LGA1155"),
        new("Intel", 6, 0x3E, 0x3E, "Ivy Bridge", "LGA2011"),

        new("Intel", 6, 0x2A, 0x2A, "Sandy Bridge", "LGA1155"),
        new("Intel", 6, 0x2D, 0x2D, "Sandy Bridge", "LGA2011"),

        // AMD Family 1Ah
        new("AMD", 0x1A, 0x00, 0xFF, "Zen 5", "AM5"),

        // AMD Family 19h
        new("AMD", 0x19, 0x60, 0x6F, "Zen 4 (Mobile)", "AM5"),
        new("AMD", 0x19, 0x10, 0x1F, "Zen 4", "AM5"),
        new("AMD", 0x19, 0x40, 0x5F, "Zen 3+", "AM4"),

        // AMD Family 17h
        new("AMD", 0x17, 0x30, 0xFF, "Zen 2", "AM4"),
        new("AMD", 0x17, 0x00, 0x2F, "Zen / Zen+", "AM4"),
    ];

    public static readonly CpuFeature[] CpuFeaturesDatabase =
    [
        // Leaf 1 EDX
        new(1, 0, CpuidRegister.Edx, 23, "MMX"),
        new(1, 0, CpuidRegister.Edx, 25, "SSE"),
        new(1, 0, CpuidRegister.Edx, 26, "SSE2"),

        // Leaf 1 ECX
        new(1, 0, CpuidRegister.Ecx, 0,  "SSE3"),
        new(1, 0, CpuidRegister.Ecx, 9,  "SSSE3"),
        new(1, 0, CpuidRegister.Ecx, 12, "FMA3"),
        new(1, 0, CpuidRegister.Ecx, 19, "SSE4.1"),
        new(1, 0, CpuidRegister.Ecx, 20, "SSE4.2"),
        new(1, 0, CpuidRegister.Ecx, 25, "AES"),
        new(1, 0, CpuidRegister.Ecx, 28, "AVX"),

        // Leaf 7 EBX
        new(7, 0, CpuidRegister.Ebx, 3,  "BMI1"),
        new(7, 0, CpuidRegister.Ebx, 5,  "AVX2"),
        new(7, 0, CpuidRegister.Ebx, 8,  "BMI2"),
        new(7, 0, CpuidRegister.Ebx, 16, "AVX512F"),
        new(7, 0, CpuidRegister.Ebx, 17, "AVX512DQ"),
        new(7, 0, CpuidRegister.Ebx, 28, "AVX512CD"),
        new(7, 0, CpuidRegister.Ebx, 30, "AVX512BW"),
        new(7, 0, CpuidRegister.Ebx, 31, "AVX512VL"),

        // Leaf 7 ECX
        new(7, 0, CpuidRegister.Ecx, 8, "GFNI"),
        new(7, 0, CpuidRegister.Ecx, 9, "VAES"),

        // Leaf 7 EDX
        new(7, 0, CpuidRegister.Edx, 4, "AVX512VNNI"),
        new(7, 0, CpuidRegister.Edx, 8, "AVX512VP2INTERSECT"),

        // 0x80000001 ECX
        new(unchecked((int)0x80000001), 0, CpuidRegister.Ecx, 6,  "SSE4A"),
        new(unchecked((int)0x80000001), 0, CpuidRegister.Ecx, 16, "FMA4"),
        new(unchecked((int)0x80000001), 0, CpuidRegister.Ecx, 21, "TBM"),

        // 0x80000001 EDX
        new(unchecked((int)0x80000001), 0, CpuidRegister.Edx, 29, "x86-64"),
        new(unchecked((int)0x80000001), 0, CpuidRegister.Edx, 31, "3DNow!")
    ];

    public static readonly EsSampleRule[] EsSamplesDatabase =
    [
        // ── Intel Arrow Lake — family 6, model 0xC5 ──────────────────────────────
        new(6, 0xC5, 0xC5,  0, -1, -1, "Core Ultra 9 285K (ES)",
            ProcessorIdMask: "????????000C0C50"),
        new(6, 0xC5, 0xC5,  1, -1, -1, "Core Ultra 9 285K (QS)",
            ProcessorIdMask: "????????000C0C51"),
        new(6, 0xC5, 0xC5,  2, -1, -1, "Core Ultra 9 285K",
            ProcessorIdMask: "????????000C0C52"),

        // ── Intel Raptor Lake — family 6, model 0xB7 ─────────────────────────────
        new(6, 0xB7, 0xB7,  0, 24, 32, "13th Gen Intel(R) Core(TM) i9-13900K (ES)",
            ProcessorIdMask: "????????000906B0"),
        new(6, 0xB7, 0xB7,  0, 16, 24, "13th Gen Intel(R) Core(TM) i7-13700K (ES)",
            ProcessorIdMask: "????????000906B0"),
        new(6, 0xB7, 0xB7, -1, 24, -1, "13th Gen Intel(R) Core(TM) i9-13900 (ES)"),

        // ── Intel Alder Lake — family 6, model 0x97 ──────────────────────────────
        new(6, 0x97, 0x97,  0, -1, -1, "12th Gen Intel(R) Core(TM) i9-12900K (ES)",
            ProcessorIdMask: "????????00090670"),
        new(6, 0x97, 0x97,  1, -1, -1, "12th Gen Intel(R) Core(TM) i9-12900K (QS)",
            ProcessorIdMask: "????????00090671"),

        // ── Intel Meteor Lake — family 6, model 0xAA ─────────────────────────────
        new(6, 0xAA, 0xAA,  0, -1, -1, "Core Ultra 9 185H (ES)",
            ProcessorIdMask: "????????000A06A0"),
        new(6, 0xAA, 0xAA,  1, -1, -1, "Core Ultra 9 185H (QS)",
            ProcessorIdMask: "????????000A06A1"),

        // ── AMD Zen 4 — family 0x19, model range 0x10–0x1F ───────────────────────
        new(0x19, 0x10, 0x1F, -1, 16, 32, "Ryzen 9 7950X (ES)"),
        new(0x19, 0x10, 0x1F, -1, 12, 24, "Ryzen 9 7900X (ES)"),
        new(0x19, 0x10, 0x1F, -1,  8, 16, "Ryzen 7 7700X (ES)"),
        new(0x19, 0x10, 0x1F, -1,  6, 12, "Ryzen 5 7600X (ES)"),

        // ── AMD Zen 5 — family 0x1A ───────────────────────────────────────────────
        new(0x1A, 0x00, 0x0F, -1, 16, 32, "Ryzen 9 9950X (ES)"),
        new(0x1A, 0x00, 0x0F, -1,  8, 16, "Ryzen 7 9700X (ES)"),
    ];

    #endregion

    #region GPU
    public sealed record GpuArchitectureRule(
        string VendorId,
        int MinDeviceId,
        int MaxDeviceId,
        string Architecture);

    public static readonly GpuArchitectureRule[] GpuArchitectureDatabase =
    [
        // AMD
        new("1002", 0x7550, 0x7550, "RDNA 4"),
        new("1002", 0x7551, 0x7551, "RDNA 4"),
        new("1002", 0x7480, 0x7480, "RDNA 4"),
        new("1002", 0x7590, 0x7590, "RDNA 4"),
        new("1002", 0x75A0, 0x75A0, "RDNA 4"),

        new("1002", 0x1580, 0x15BF, "RDNA 3.5"),
        new("1002", 0x7440, 0x745F, "RDNA 3"),
        new("1002", 0x73A0, 0x73FF, "RDNA 2"),

        new("1002", 0x7310, 0x731F, "RDNA 1"),
        new("1002", 0x7340, 0x734F, "RDNA 1"),

        new("1002", 0x6860, 0x687F, "GCN 5 (Vega)"),
        new("1002", 0x66A0, 0x66AF, "GCN 5 (Vega)"),

        new("1002", 0x67C0, 0x67FF, "GCN 4 (Polaris)"),
        new("1002", 0x6980, 0x699F, "GCN 4 (Polaris)"),

        // NVIDIA
        new("10DE", 0x2600, 0x27FF, "Ada Lovelace"),

        new("10DE", 0x2200, 0x25FF, "Ampere"),
        new("10DE", 0x2480, 0x249F, "Ampere"),

        new("10DE", 0x1E00, 0x1FFF, "Turing"),
        new("10DE", 0x2180, 0x21FF, "Turing"),

        new("10DE", 0x1B00, 0x1B80, "Pascal"),
        new("10DE", 0x1C00, 0x1C8F, "Pascal"),

        // Intel
        new("8086", 0x4F80, 0x4F90, "Xe HPG (Arc)"),
        new("8086", 0x5690, 0x56BF, "Xe HPG (Arc)"),

        new("8086", 0x9A40, 0x9A7F, "Xe LP (Tiger Lake)"),
        new("8086", 0x4C8A, 0x4C9A, "Xe LP (Rocket Lake)")
    ];

    public static readonly Dictionary<string, string> GpuArchitectureDatabaseFallbacks = new()
    {
        ["1002"] = "AMD GCN",
        ["10DE"] = "NVIDIA GPU",
        ["8086"] = "Intel Graphics"
    };

    public sealed record GpuVramRule(
        string VendorId,
        int MinDeviceId,
        int MaxDeviceId,
        string VramType);

    public static readonly GpuVramRule[] GpuVramDatabase =
    [
        // AMD
        new("1002", 0x7550, 0x7550, "GDDR6"),
        new("1002", 0x7551, 0x7551, "GDDR6"),
        new("1002", 0x7480, 0x7480, "GDDR6"),
        new("1002", 0x7590, 0x7590, "GDDR6"),
        new("1002", 0x75A0, 0x75A0, "GDDR6"),

        new("1002", 0x7440, 0x745F, "GDDR6"),
        new("1002", 0x73A0, 0x73FF, "GDDR6"),
        new("1002", 0x7310, 0x734F, "GDDR6"),

        new("1002", 0x6860, 0x687F, "HBM2"),
        new("1002", 0x66A0, 0x66AF, "HBM2"),

        new("1002", 0x67C0, 0x67FF, "GDDR5"),

        // NVIDIA
        new("10DE", 0x2600, 0x27FF, "GDDR6X"), // Ada

        new("10DE", 0x2200, 0x25FF, "GDDR6"),  // Ampere
        new("10DE", 0x1E00, 0x21FF, "GDDR6"),  // Turing

        new("10DE", 0x1B00, 0x1C8F, "GDDR5X"), // Pascal

        // Intel
        new("8086", 0x0000, 0xFFFF, "Shared")
    ];

    public static readonly Dictionary<string, string> GpuVramDatabaseFallbacks = new()
    {
        ["1002"] = "GDDR",
        ["10DE"] = "GDDR",
        ["8086"] = "Shared"
    };

    public static readonly Dictionary<int, string> MemoryTypeDatabase = new()
    {
        [1]  = "Other",
        [2]  = "Unknown",
        [3]  = "VRAM",
        [4]  = "DRAM",
        [5]  = "SRAM",
        [6]  = "WRAM",
        [7]  = "EDO RAM",
        [8]  = "Burst Synchronous DRAM",
        [9]  = "Pipelined Burst SRAM",
        [10] = "CDRAM",
        [11] = "3DRAM",
        [12] = "SDRAM",
        [13] = "SGRAM"
    };
    #endregion

    #region MOTHERBOARD
    public static readonly Dictionary<string, Dictionary<string, (string Codename, string? ChipsetName, string BusSpecs)>> MotherboardChipsetInfoDatabase = new()
    {
        ["8086"] = new()
        {
            ["7A04"] = ("Intel Raptor Lake", "Intel Z790", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A06"] = ("Intel Raptor Lake", "Intel B760", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A08"] = ("Intel Raptor Lake", "Intel H770", "PCI-Express 4.0 (16.0 GT/s)"),
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
        },

        ["1022"] = new()
        {
            ["790B"] = ("AMD Zen FCH", null, "PCI-Express 4.0 (16.0 GT/s)"), // SMBus
            ["790E"] = ("AMD Zen FCH", null, "PCI-Express 4.0 (16.0 GT/s)"), // LPC Bridge
        }
    };

    public static readonly string[] AmdChipsetDatabase =
    [
        "X670E",
        "X670",
        "X870E",
        "X870",
        "B650E",
        "B650",
        "B850",
        "A620",
        "X570",
        "B550",
        "A520",
        "X470",
        "B450",
        "A320"
    ];
    #endregion

    #region RAM
    public static readonly Dictionary<byte, string> MemoryTechnologyDatabase = new()
    {
        [0x12] = "DDR",
        [0x13] = "DDR2",
        [0x14] = "DDR2 FB-DIMM",
        [0x18] = "DDR3",
        [0x1A] = "DDR4",
        [0x20] = "LPDDR4",
        [0x22] = "DDR5",
        [0x23] = "LPDDR5"
    };

    public static readonly Dictionary<byte, string> MemoryFormFactorDatabase = new()
    {
        [0x09] = "DIMM",
        [0x0D] = "SO-DIMM",
        [0x0F] = "RIMM",
        [0x11] = "FB-DIMM"
    };
    #endregion
}