namespace SYSi.Services.HardwareService;

/// <summary>
/// Centralized hardware lookup database.
/// Each region represents a hardware subsystem (CPU, GPU, RAM, Motherboard, Chipset, Storage).
/// To add support for new hardware, update the corresponding region only.
/// </summary>
public static class HardwareDatabase
{
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

    public static readonly Dictionary<ushort, string> CpuArchitectures = new()
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
}