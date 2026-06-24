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
        int BaseClockMHz,    // -1 = any
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
        #region Intel
        // Intel Core Ultra 200 Desktop
        ["Core Ultra 9 285"] = "65 W (182 W Turbo)",
        ["Core Ultra 9 285T"] = "35 W (114 W Turbo)",
        ["Core Ultra 7 265"] = "65 W (182 W Turbo)",
        ["Core Ultra 7 265F"] = "65 W (182 W Turbo)",
        ["Core Ultra 7 265T"] = "35 W (114 W Turbo)",
        ["Core Ultra 5 245"] = "65 W (121 W Turbo)",
        ["Core Ultra 5 245F"] = "65 W (121 W Turbo)",
        ["Core Ultra 5 245T"] = "35 W (92 W Turbo)",

        // Intel Core Ultra 200S Desktop
        ["Core Ultra 9 285K"] = "125 W (250 W Turbo)",
        ["Core Ultra 7 265K"] = "125 W (250 W Turbo)",
        ["Core Ultra 7 265KF"] = "125 W (250 W Turbo)",
        ["Core Ultra 5 245K"] = "125 W (159 W Turbo)",
        ["Core Ultra 5 245KF"] = "125 W (159 W Turbo)",

        // Intel Core Ultra 200HX Laptop
        ["Core Ultra 9 285HX"] = "55 W (160 W Turbo)",
        ["Core Ultra 7 265HX"] = "55 W (160 W Turbo)",
        ["Core Ultra 7 255HX"] = "55 W (160 W Turbo)",
        ["Core Ultra 5 245HX"] = "55 W (160 W Turbo)",
        ["Core Ultra 5 235HX"] = "55 W (160 W Turbo)",

        // Intel Core Ultra 200H Laptop
        ["Core Ultra 9 285H"] = "28 W (115 W Turbo)",
        ["Core Ultra 7 265H"] = "28 W (115 W Turbo)",
        ["Core Ultra 7 255H"] = "28 W (115 W Turbo)",
        ["Core Ultra 5 235H"] = "28 W (115 W Turbo)",
        ["Core Ultra 5 225H"] = "28 W (115 W Turbo)",

        // Intel Core Ultra Series 2 Laptop
        ["Core Ultra 9 288V"] = "17 W (37 W Turbo)",
        ["Core Ultra 7 268V"] = "17 W (37 W Turbo)",
        ["Core Ultra 7 266V"] = "17 W (37 W Turbo)",
        ["Core Ultra 7 258V"] = "17 W (37 W Turbo)",
        ["Core Ultra 7 256V"] = "17 W (37 W Turbo)",
        ["Core Ultra 5 238V"] = "17 W (37 W Turbo)",
        ["Core Ultra 5 236V"] = "17 W (37 W Turbo)",
        ["Core Ultra 5 228V"] = "17 W (37 W Turbo)",
        ["Core Ultra 5 226V"] = "17 W (37 W Turbo)",

        // Intel Core Ultra Series 1 Laptop
        ["Core Ultra 9 185H"] = "28 W (115 W Turbo)",
        ["Core Ultra 7 165H"] = "28 W (115 W Turbo)",
        ["Core Ultra 7 155H"] = "28 W (115 W Turbo)",
        ["Core Ultra 7 165U"] = "15 W (57 W Turbo)",
        ["Core Ultra 7 155U"] = "15 W (57 W Turbo)",
        ["Core Ultra 5 135H"] = "28 W (115 W Turbo)",
        ["Core Ultra 5 125H"] = "28 W (115 W Turbo)",
        ["Core Ultra 5 135U"] = "15 W (57 W Turbo)",
        ["Core Ultra 5 125U"] = "15 W (57 W Turbo)",
        ["Core Ultra 3 100U"] = "15 W (57 W Turbo)",

        // Intel Core Series 1 Laptop
        ["Core 7 150U"] = "15 W (55 W Turbo)",
        ["Core 5 120U"] = "15 W (55 W Turbo)",
        ["Core 3 100U"] = "15 W (55 W Turbo)",

        // Intel 14th Gen desktop
        ["i9-14900KS"] = "150 W (253 W Turbo)",
        ["i9-14900K"]  = "125 W (253 W Turbo)",
        ["i9-14900KF"] = "125 W (253 W Turbo)",
        ["i9-14900"]   = "65 W (219 W Turbo)",
        ["i9-14900F"]  = "65 W (219 W Turbo)",
        ["i9-14900T"]  = "35 W (106 W Turbo)",
        ["i7-14700K"]  = "125 W (253 W Turbo)",
        ["i7-14700KF"] = "125 W (253 W Turbo)",
        ["i7-14700"]   = "65 W (219 W Turbo)",
        ["i7-14700F"]  = "65 W (219 W Turbo)",
        ["i7-14700T"]  = "35 W (106 W Turbo)",
        ["i5-14600K"]  = "125 W (181 W Turbo)",
        ["i5-14600KF"] = "125 W (181 W Turbo)",
        ["i5-14600"]   = "65 W (154 W Turbo)",
        ["i5-14500"]   = "65 W (154 W Turbo)",
        ["i5-14400"]   = "65 W (148 W Turbo)",
        ["i5-14400F"]  = "65 W (148 W Turbo)",
        ["i5-14500T"]  = "35 W (92 W Turbo)",
        ["i5-14400T"]  = "35 W (82 W Turbo)",
        ["i3-14100"]   = "60 W (110 W Turbo)",
        ["i3-14100F"]  = "58 W (110 W Turbo)",
        ["i3-14100T"]  = "35 W (69 W Turbo)",

        //Intel 14th Gen Laptop(HX Series)
        ["i9-14900HX"] = "55 W (157 W Turbo)",
        ["i7-14700HX"] = "55 W (157 W Turbo)",
        ["i7-14650HX"] = "55 W (157 W Turbo)",
        ["i5-14500HX"] = "55 W (157 W Turbo)",
        ["i5-14450HX"] = "55 W (157 W Turbo)",

        // Intel 13th Gen desktop
        ["i9-13900KS"] = "150 W (253 W Turbo)",
        ["i9-13900K"]  = "125 W (253 W Turbo)",
        ["i9-13900KF"] = "125 W (253 W Turbo)",
        ["i9-13900"]   = "65 W (219 W Turbo)",
        ["i9-13900F"]  = "65 W (219 W Turbo)",
        ["i9-13900T"]  = "35 W (106 W Turbo)",
        ["i7-13700K"]  = "125 W (253 W Turbo)",
        ["i7-13700KF"] = "125 W (253 W Turbo)",
        ["i7-13700"]   = "65 W (219 W Turbo)",
        ["i7-13700F"]  = "65 W (219 W Turbo)",
        ["i7-13700T"]  = "35 W (106 W Turbo)",
        ["i5-13600K"]  = "125 W (181 W Turbo)",
        ["i5-13600KF"] = "125 W (181 W Turbo)",
        ["i5-13600"]   = "65 W (154 W Turbo)",
        ["i5-13500"]   = "65 W (154 W Turbo)",
        ["i5-13400"]   = "65 W (148 W Turbo)",
        ["i5-13400F"]  = "65 W (148 W Turbo)",
        ["i5-13500T"]  = "35 W (92 W Turbo)",
        ["i5-13400T"]  = "35 W (82 W Turbo)",
        ["i3-13100"]   = "60 W (89 W Turbo)",
        ["i3-13100F"]  = "58 W (89 W Turbo)",
        ["i3-13100T"]  = "35 W (69 W Turbo)",

        // Intel 13th Gen laptop
        ["i9-13980HX"] = "55 W (157 W Turbo)",
        ["i9-13950HX"] = "55 W (157 W Turbo)",
        ["i9-13900HX"] = "55 W (157 W Turbo)",
        ["i9-13900HK"] = "45 W (115 W Turbo)",
        ["i9-13900H"]  = "45 W (115 W Turbo)",
        ["i7-13850HX"] = "55 W (157 W Turbo)",
        ["i7-13700HX"] = "55 W (157 W Turbo)",
        ["i7-13800H"]  = "45 W (115 W Turbo)",
        ["i7-13700H"]  = "45 W (115 W Turbo)",
        ["i7-13620H"]  = "45 W (115 W Turbo)",
        ["i7-1370P"]   = "28 W (64 W Turbo)",
        ["i7-1360P"]   = "28 W (64 W Turbo)",
        ["i7-1355U"]   = "15 W (55 W Turbo)",
        ["i7-1365U"]   = "15 W (55 W Turbo)",
        ["i5-13600HX"] = "55 W (157 W Turbo)",
        ["i5-13500HX"] = "55 W (157 W Turbo)",
        ["i5-13500H"]  = "45 W (95 W Turbo)",
        ["i5-13420H"]  = "45 W (95 W Turbo)",
        ["i5-1350P"]   = "28 W (64 W Turbo)",
        ["i5-1340P"]   = "28 W (64 W Turbo)",
        ["i5-1335U"]   = "15 W (55 W Turbo)",
        ["i5-1345U"]   = "15 W (55 W Turbo)",
        ["i3-1315U"]   = "15 W (55 W Turbo)",
        ["i3-1305U"]   = "15 W (55 W Turbo)",
        ["i3-N305"] = "15 W",
        ["i3-N300"] = "7 W",
        ["i3-N200"] = "6 W",
        ["i3-N100"] = "6 W",

        // Intel 12th Gen desktop
        ["i9-12900KS"] = "150 W (241 W Turbo)",
        ["i9-12900K"]  = "125 W (241 W Turbo)",
        ["i9-12900KF"] = "125 W (241 W Turbo)",
        ["i9-12900"]   = "65 W (202 W Turbo)",
        ["i9-12900F"]  = "65 W (202 W Turbo)",
        ["i9-12900T"]  = "35 W (106 W Turbo)",
        ["i7-12700K"]  = "125 W (190 W Turbo)",
        ["i7-12700KF"] = "125 W (190 W Turbo)",
        ["i7-12700"]   = "65 W (180 W Turbo)",
        ["i7-12700F"]  = "65 W (180 W Turbo)",
        ["i7-12700T"]  = "35 W (99 W Turbo)",
        ["i5-12600K"]  = "125 W (150 W Turbo)",
        ["i5-12600KF"] = "125 W (150 W Turbo)",
        ["i5-12600"]   = "65 W (117 W Turbo)",
        ["i5-12500"]   = "65 W (117 W Turbo)",
        ["i5-12400"]   = "65 W (117 W Turbo)",
        ["i5-12400F"]  = "65 W (117 W Turbo)",
        ["i5-12600T"]  = "35 W (74 W Turbo)",
        ["i5-12500T"]  = "35 W (74 W Turbo)",
        ["i5-12400T"]  = "35 W (74 W Turbo)",
        ["i3-12300"]   = "60 W (89 W Turbo)",
        ["i3-12100"]   = "60 W (89 W Turbo)",
        ["i3-12100F"]  = "58 W (89 W Turbo)",
        ["i3-12300T"]  = "35 W (69 W Turbo)",
        ["i3-12100T"]  = "35 W (69 W Turbo)",
        ["G7400"]      = "46 W",
        ["G7400T"]     = "35 W",
        ["G7405"]      = "65 W",
        ["G6900"]      = "46 W",
        ["G6900T"]     = "35 W",
        ["G6905"]      = "65 W",

        // Intel 12th Gen laptop
        ["i9-12900HK"]  = "45 W (115 W Turbo)",
        ["i9-12900H"]   = "45 W (115 W Turbo)",
        ["i9-12900HX"]  = "55 W (157 W Turbo)",
        ["i9-12900H"]   = "45 W (115 W Turbo)",
        ["i9-12900HE"]  = "45 W (115 W Turbo)",
        ["i7-12800HX"]  = "55 W (157 W Turbo)",
        ["i7-12850HX"]  = "55 W (157 W Turbo)",
        ["i7-12700H"]   = "45 W (115 W Turbo)",
        ["i7-12800H"]   = "45 W (115 W Turbo)",
        ["i7-12650H"]   = "45 W (115 W Turbo)",
        ["i7-1260P"]    = "28 W (64 W Turbo)",
        ["i7-1280P"]    = "28 W (64 W Turbo)",
        ["i7-1265U"]    = "15 W (55 W Turbo)",
        ["i7-1255U"]    = "15 W (55 W Turbo)",
        ["i5-12600HX"]  = "55 W (157 W Turbo)",
        ["i5-12500H"]   = "45 W (95 W Turbo)",
        ["i5-12450H"]   = "45 W (95 W Turbo)",
        ["i5-1240P"]    = "28 W (64 W Turbo)",
        ["i5-1250P"]    = "28 W (64 W Turbo)",
        ["i5-1235U"]    = "15 W (55 W Turbo)",
        ["i5-1245U"]    = "15 W (55 W Turbo)",
        ["i3-1220P"]    = "28 W (64 W Turbo)",
        ["i3-1215U"]    = "15 W (55 W Turbo)",
        ["i3-1210U"]    = "15 W (55 W Turbo)",
        ["Pentium 8505"] = "15 W (55 W Turbo)",
        ["Celeron 7305"] = "15 W (55 W Turbo)",

        // Intel 11th Gen Desktop (Rocket Lake)
        ["i9-11900K"]  = "125 W (250 W Turbo)",
        ["i9-11900KF"] = "125 W (250 W Turbo)",
        ["i9-11900"]   = "65 W (224 W Turbo)",
        ["i9-11900F"]  = "65 W (224 W Turbo)",
        ["i9-11900T"]  = "35 W (123 W Turbo)",
        ["i7-11700K"]  = "125 W (251 W Turbo)",
        ["i7-11700KF"] = "125 W (251 W Turbo)",
        ["i7-11700"]   = "65 W (219 W Turbo)",
        ["i7-11700F"]  = "65 W (219 W Turbo)",
        ["i7-11700T"]  = "35 W (123 W Turbo)",
        ["i5-11600K"]  = "125 W (251 W Turbo)",
        ["i5-11600KF"] = "125 W (251 W Turbo)",
        ["i5-11600"]   = "65 W (154 W Turbo)",
        ["i5-11500"]   = "65 W (154 W Turbo)",
        ["i5-11400"]   = "65 W (154 W Turbo)",
        ["i5-11400F"]  = "65 W (154 W Turbo)",
        ["i5-11500T"]  = "35 W (92 W Turbo)",
        ["i5-11400T"]  = "35 W (92 W Turbo)",
        ["i3-11300"]   = "60 W (89 W Turbo)",
        ["i3-11100"]   = "60 W (89 W Turbo)",
        ["i3-11100F"]  = "58 W (89 W Turbo)",
        ["i3-11100T"]  = "35 W (69 W Turbo)",

        // Intel 11th Gen Laptop (Tiger Lake)
        ["i9-11980HK"] = "45 W (65 W Turbo)",
        ["i9-11900H"]  = "45 W (65 W Turbo)",
        ["i7-11800H"]  = "45 W (109 W Turbo)",
        ["i7-11370H"]  = "35 W (64 W Turbo)",
        ["i7-1165G7"]  = "15 W (55 W Turbo)",
        ["i7-1185G7"]  = "15 W (55 W Turbo)",
        ["i5-11400H"]  = "45 W (95 W Turbo)",
        ["i5-11300H"]  = "35 W (64 W Turbo)",
        ["i5-1135G7"]  = "15 W (55 W Turbo)",
        ["i3-1115G4"]  = "15 W (28 W Turbo)",
        ["i3-1125G4"]  = "15 W (28 W Turbo)",

        // Intel 10th Gen Desktop (Comet Lake)
        ["i9-10900K"]  = "125 W (250 W Turbo)",
        ["i9-10900KF"] = "125 W (250 W Turbo)",
        ["i9-10900"]   = "65 W (224 W Turbo)",
        ["i9-10900F"]  = "65 W (224 W Turbo)",
        ["i9-10900T"]  = "35 W (123 W Turbo)",
        ["i7-10700K"]  = "125 W (229 W Turbo)",
        ["i7-10700KF"] = "125 W (229 W Turbo)",
        ["i7-10700"]   = "65 W (224 W Turbo)",
        ["i7-10700F"]  = "65 W (224 W Turbo)",
        ["i7-10700T"]  = "35 W (123 W Turbo)",
        ["i5-10600K"]  = "125 W (182 W Turbo)",
        ["i5-10600KF"] = "125 W (182 W Turbo)",
        ["i5-10600"]   = "65 W (134 W Turbo)",
        ["i5-10500"]   = "65 W (134 W Turbo)",
        ["i5-10400"]   = "65 W (134 W Turbo)",
        ["i5-10400F"]  = "65 W (134 W Turbo)",
        ["i5-10500T"]  = "35 W (74 W Turbo)",
        ["i5-10400T"]  = "35 W (74 W Turbo)",
        ["i3-10320"]   = "65 W (90 W Turbo)",
        ["i3-10100"]   = "65 W (90 W Turbo)",
        ["i3-10100F"]  = "65 W (90 W Turbo)",
        ["i3-10100T"]  = "35 W (58 W Turbo)",

        // Intel 10th Gen Laptop (Comet Lake / Ice Lake)
        ["i9-10980HK"] = "45 W (90 W Turbo)",
        ["i7-10875H"]  = "45 W (90 W Turbo)",
        ["i7-10750H"]  = "45 W (90 W Turbo)",
        ["i7-1065G7"]  = "15 W (25 W Turbo)",
        ["i5-10300H"]  = "45 W (90 W Turbo)",
        ["i5-1035G7"]  = "15 W (25 W Turbo)",
        ["i5-1035G1"]  = "15 W (25 W Turbo)",
        ["i3-1005G1"]  = "15 W (25 W Turbo)",

        // Intel 9th Gen Desktop (Coffee Lake Refresh)
        ["i9-9900KS"]  = "127 W (159 W Turbo)",
        ["i9-9900K"]   = "95 W (210 W Turbo)",
        ["i9-9900KF"]  = "95 W (210 W Turbo)",
        ["i9-9900"]    = "65 W (119 W Turbo)",
        ["i9-9900T"]   = "35 W (95 W Turbo)",
        ["i7-9700K"]   = "95 W (150 W Turbo)",
        ["i7-9700KF"]  = "95 W (150 W Turbo)",
        ["i7-9700"]    = "65 W (119 W Turbo)",
        ["i7-9700F"]   = "65 W (119 W Turbo)",
        ["i7-9700T"]   = "35 W (95 W Turbo)",
        ["i5-9600K"]   = "95 W (118 W Turbo)",
        ["i5-9600KF"]  = "95 W (118 W Turbo)",
        ["i5-9600"]    = "65 W (95 W Turbo)",
        ["i5-9500"]    = "65 W (95 W Turbo)",
        ["i5-9400"]    = "65 W (95 W Turbo)",
        ["i5-9400F"]   = "65 W (95 W Turbo)",
        ["i5-9500T"]   = "35 W (69 W Turbo)",
        ["i5-9400T"]   = "35 W (69 W Turbo)",
        ["i3-9350K"]   = "91 W (119 W Turbo)",
        ["i3-9320"]    = "62 W (89 W Turbo)",
        ["i3-9100"]    = "65 W (88 W Turbo)",
        ["i3-9100F"]   = "65 W (88 W Turbo)",
        ["i3-9100T"]   = "35 W (58 W Turbo)",

        // Intel 9th Gen Laptop (Coffee Lake-H)
        ["i9-9980HK"]  = "45 W (90 W Turbo)",
        ["i9-9880H"]   = "45 W (90 W Turbo)",
        ["i7-9850H"]   = "45 W (90 W Turbo)",
        ["i7-9750H"]   = "45 W (90 W Turbo)",
        ["i7-9700HF"]  = "45 W (90 W Turbo)",
        ["i5-9400H"]   = "45 W (90 W Turbo)",
        ["i5-9300H"]   = "45 W (90 W Turbo)",
        ["i3-9100HL"]  = "25 W (45 W Turbo)",

        // Intel 8th Gen Desktop (Coffee Lake)
        ["i7-8700K"]  = "95 W (118 W Turbo)",
        ["i7-8700"]   = "65 W (95 W Turbo)",
        ["i7-8700T"]  = "35 W (69 W Turbo)",
        ["i5-8600K"]  = "95 W (118 W Turbo)",
        ["i5-8600"]   = "65 W (95 W Turbo)",
        ["i5-8500"]   = "65 W (95 W Turbo)",
        ["i5-8400"]   = "65 W (95 W Turbo)",
        ["i5-8500T"]  = "35 W (69 W Turbo)",
        ["i5-8400T"]  = "35 W (69 W Turbo)",
        ["i3-8350K"]  = "91 W (119 W Turbo)",
        ["i3-8300"]   = "62 W (89 W Turbo)",
        ["i3-8100"]   = "65 W (65 W Turbo)",
        ["i3-8100T"]  = "35 W (35 W Turbo)",

        // Intel 8th Gen Laptop (Coffee Lake-H / Kaby Lake-R)
        ["i9-8950HK"] = "45 W (90 W Turbo)",
        ["i7-8850H"]  = "45 W (90 W Turbo)",
        ["i7-8750H"]  = "45 W (90 W Turbo)",
        ["i7-8650U"]  = "15 W (44 W Turbo)",
        ["i7-8550U"]  = "15 W (44 W Turbo)",
        ["i5-8400H"]  = "45 W (78 W Turbo)",
        ["i5-8300H"]  = "45 W (78 W Turbo)",
        ["i5-8350U"]  = "15 W (44 W Turbo)",
        ["i5-8250U"]  = "15 W (44 W Turbo)",
        ["i3-8130U"]  = "15 W (25 W Turbo)",
        ["i3-8145U"]  = "15 W (25 W Turbo)",

        // Intel 7th Gen Desktop (Kaby Lake)
        ["i7-7700K"]  = "91 W (112 W Turbo)",
        ["i7-7700"]   = "65 W (91 W Turbo)",
        ["i7-7700T"]  = "35 W (69 W Turbo)",
        ["i5-7600K"]  = "91 W (112 W Turbo)",
        ["i5-7600"]   = "65 W (91 W Turbo)",
        ["i5-7500"]   = "65 W (65 W Turbo)",
        ["i5-7400"]   = "65 W (65 W Turbo)",
        ["i5-7500T"]  = "35 W (35 W Turbo)",
        ["i5-7400T"]  = "35 W (35 W Turbo)",
        ["i3-7350K"]  = "60 W (91 W Turbo)",
        ["i3-7320"]   = "51 W (65 W Turbo)",
        ["i3-7100"]   = "51 W (51 W Turbo)",
        ["i3-7100T"]  = "35 W (35 W Turbo)",
        ["G4620"]     = "51 W",
        ["G4600"]     = "51 W",
        ["G4560"]     = "54 W",
        ["G3930"]     = "51 W",
        ["G3930T"]    = "35 W",

        // Intel 7th Gen Laptop (Kaby Lake)
        ["i7-7920HQ"] = "45 W (78 W Turbo)",
        ["i7-7820HK"] = "45 W (78 W Turbo)",
        ["i7-7700HQ"] = "45 W (78 W Turbo)",
        ["i7-7600U"]  = "15 W (25 W Turbo)",
        ["i7-7500U"]  = "15 W (25 W Turbo)",
        ["i5-7440HQ"] = "45 W (78 W Turbo)",
        ["i5-7300HQ"] = "45 W (78 W Turbo)",
        ["i5-7300U"]  = "15 W (25 W Turbo)",
        ["i5-7200U"]  = "15 W (25 W Turbo)",
        ["i3-7100H"]  = "35 W (51 W Turbo)",
        ["i3-7100U"]  = "15 W (25 W Turbo)",
        ["i3-7020U"]  = "15 W (25 W Turbo)",
        #endregion

        #region AMD
        // AMD Ryzen 9000 Desktop (Zen 5)
        ["9950X"]    = "170 W (230 W PPT)",
        ["9900X"]    = "120 W (162 W PPT)",
        ["9700X"]    = "65 W (88 W PPT)",
        ["9600X"]    = "65 W (88 W PPT)",
        ["9950X3D"]  = "170 W (230 W PPT)",
        ["9900X3D"]  = "120 W (162 W PPT)",
        ["9800X3D"]  = "120 W (162 W PPT)",

        // AMD Ryzen 8000G Desktop
        ["8700G"]    = "65 W (88 W PPT)",
        ["8600G"]    = "65 W (88 W PPT)",
        ["8500G"]    = "65 W (88 W PPT)",
        ["8300G"]    = "65 W (88 W PPT)",

        // AMD Ryzen 7000 Desktop (Zen 4)
        ["7950X"]    = "170 W (230 W PPT)",
        ["7900X"]    = "170 W (230 W PPT)",
        ["7950X3D"]  = "120 W (162 W PPT)",
        ["7900X3D"]  = "120 W (162 W PPT)",
        ["7800X3D"]  = "120 W (162 W PPT)",
        ["7900"]     = "65 W (88 W PPT)",
        ["7700"]     = "65 W (88 W PPT)",
        ["7600"]     = "65 W (88 W PPT)",
        ["7500F"]    = "65 W (88 W PPT)",
        ["7700X"]    = "105 W (142 W PPT)",
        ["7600X"]    = "105 W (142 W PPT)",

        // AMD Ryzen 5000 Desktop (Zen 3)
        ["5950X"]    = "105 W (142 W PPT)",
        ["5900X"]    = "105 W (142 W PPT)",
        ["5800XT"]   = "105 W (142 W PPT)",
        ["5800X3D"]  = "105 W (142 W PPT)",
        ["5800X"]    = "105 W (142 W PPT)",
        ["5700X3D"]  = "105 W (142 W PPT)",
        ["5700X"]    = "65 W (88 W PPT)",
        ["5700"]     = "65 W (88 W PPT)",
        ["5600X"]    = "65 W (88 W PPT)",
        ["5600"]     = "65 W (88 W PPT)",
        ["5500"]     = "65 W (88 W PPT)",
        ["5700G"]    = "65 W (88 W PPT)",
        ["5600G"]    = "65 W (88 W PPT)",
        ["5300G"]    = "65 W (88 W PPT)",

        // AMD Ryzen 4000G Desktop
        ["4750G"]    = "65 W (88 W PPT)",
        ["4650G"]    = "65 W (88 W PPT)",
        ["4350G"]    = "65 W (88 W PPT)",

        // AMD Ryzen 3000 Desktop (Zen 2)
        ["3950X"]    = "105 W (142 W PPT)",
        ["3900XT"]   = "105 W (142 W PPT)",
        ["3900X"]    = "105 W (142 W PPT)",
        ["3900"]     = "65 W (88 W PPT)",
        ["3800XT"]   = "105 W (142 W PPT)",
        ["3800X"]    = "105 W (142 W PPT)",
        ["3700X"]    = "65 W (88 W PPT)",
        ["3600XT"]   = "95 W (128 W PPT)",
        ["3600X"]    = "95 W (128 W PPT)",
        ["3600"]     = "65 W (88 W PPT)",
        ["3500X"]    = "65 W (88 W PPT)",
        ["3300X"]    = "65 W (88 W PPT)",
        ["3100"]     = "65 W (88 W PPT)",

        // AMD Ryzen Threadripper 7000
        ["7980X"]    = "350 W (400 W PPT)",
        ["7970X"]    = "350 W (400 W PPT)",
        ["7960X"]    = "350 W (400 W PPT)",

        // AMD Ryzen Threadripper PRO 7000 WX
        ["7995WX"]   = "350 W (400 W PPT)",
        ["7985WX"]   = "350 W (400 W PPT)",
        ["7975WX"]   = "350 W (400 W PPT)",
        ["7965WX"]   = "350 W (400 W PPT)",
        ["7955WX"]   = "350 W (400 W PPT)",
        ["7945WX"]   = "350 W (400 W PPT)",
        #endregion
    };

    public static readonly CpuModelRule[] CpuRulesDatabase =
    [
        #region Intel
        // Arrow Lake (Core Ultra 200 Desktop)
        new("Intel", 6, 0xC6, 0xC6, "Arrow Lake", "LGA1851"),

        // Lunar Lake (Core Ultra Series 2)
        new("Intel", 6, 0xBD, 0xBD, "Lunar Lake", "BGA"),

        // Meteor Lake (Core Ultra Series 1)
        new("Intel", 6, 0xAA, 0xAA, "Meteor Lake", "BGA"),
        new("Intel", 6, 0xAC, 0xAC, "Meteor Lake", "BGA"),

        // Raptor Lake Refresh / Raptor Lake
        new("Intel", 6, 0xBA, 0xBA, "Raptor Lake Refresh", "LGA1700"),
        new("Intel", 6, 0xB7, 0xB7, "Raptor Lake", "LGA1700"),
        new("Intel", 6, 0xB5, 0xB5, "Raptor Lake", "LGA1700"),

        // Alder Lake
        new("Intel", 6, 0x97, 0x97, "Alder Lake", "LGA1700"),
        new("Intel", 6, 0x9A, 0x9A, "Alder Lake", "LGA1700"),

        // Sapphire Rapids
        new("Intel", 6, 0x8F, 0x8F, "Sapphire Rapids", "LGA4677"),

        // Tiger Lake
        new("Intel", 6, 0x8C, 0x8C, "Tiger Lake", "BGA"),
        new("Intel", 6, 0x8D, 0x8D, "Tiger Lake", "BGA"),

        // Rocket Lake
        new("Intel", 6, 0xA7, 0xA7, "Rocket Lake", "LGA1200"),

        // Comet Lake
        new("Intel", 6, 0xA5, 0xA5, "Comet Lake", "LGA1200"),
        new("Intel", 6, 0xA6, 0xA6, "Comet Lake", "LGA1200"),

        // Coffee Lake Refresh / Coffee Lake
        new("Intel", 6, 0x9E, 0x9E, "Coffee Lake", "LGA1151"),
        new("Intel", 6, 0x9D, 0x9D, "Coffee Lake", "LGA1151"),

        // Skylake-X / Cascade Lake-X
        new("Intel", 6, 0x55, 0x55, "Skylake-X", "LGA2066"),

        // Skylake
        new("Intel", 6, 0x4E, 0x4E, "Skylake", "LGA1151"),
        new("Intel", 6, 0x5E, 0x5E, "Skylake", "LGA1151"),

        // Broadwell
        new("Intel", 6, 0x3D, 0x3D, "Broadwell", "LGA1150"),
        new("Intel", 6, 0x47, 0x47, "Broadwell", "LGA1150"),

        // Haswell
        new("Intel", 6, 0x3C, 0x3C, "Haswell", "LGA1150"),
        new("Intel", 6, 0x45, 0x45, "Haswell", "LGA1150"),
        new("Intel", 6, 0x46, 0x46, "Haswell", "LGA1150"),

        // Ivy Bridge
        new("Intel", 6, 0x3A, 0x3A, "Ivy Bridge", "LGA1155"),
        new("Intel", 6, 0x3E, 0x3E, "Ivy Bridge-E", "LGA2011"),

        // Sandy Bridge
        new("Intel", 6, 0x2A, 0x2A, "Sandy Bridge", "LGA1155"),
        new("Intel", 6, 0x2D, 0x2D, "Sandy Bridge-E", "LGA2011"),
        #endregion

        #region AMD
        // =========================
        // AMD
        // =========================

        // Zen 5
        new("AMD", 0x1A, 0x00, 0xFF, "Zen 5", "AM5"),

        // Zen 4
        new("AMD", 0x19, 0x60, 0x6F, "Zen 4", "AM5"),
        new("AMD", 0x19, 0x70, 0x7F, "Zen 4", "AM5"),
        new("AMD", 0x19, 0x10, 0x1F, "Zen 4", "AM5"),

        // Zen 3+
        new("AMD", 0x19, 0x40, 0x5F, "Zen 3+", "FP7"),

        // Zen 3
        new("AMD", 0x19, 0x20, 0x2F, "Zen 3", "AM4"),

        // Zen 2
        new("AMD", 0x17, 0x30, 0x3F, "Zen 2", "AM4"),
        new("AMD", 0x17, 0x60, 0x6F, "Zen 2", "AM4"),
        new("AMD", 0x17, 0x70, 0x7F, "Zen 2", "AM4"),

        // Zen+
        new("AMD", 0x17, 0x10, 0x1F, "Zen+", "AM4"),

        // Zen
        new("AMD", 0x17, 0x00, 0x0F, "Zen", "AM4"),
        #endregion
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
        #region Intel
        // ── Intel Arrow Lake — family 6, model 0xC5 ──────────────────────────────
        new(6, 0xC5, 0xC5,  0, -1, -1, -1, "Core Ultra 9 285K (ES)",
            ProcessorIdMask: "????????000C0C50"),
        new(6, 0xC5, 0xC5,  1, -1, -1, -1, "Core Ultra 9 285K (QS)",
            ProcessorIdMask: "????????000C0C51"),
        new(6, 0xC5, 0xC5,  2, -1, -1, -1, "Core Ultra 9 285K",
            ProcessorIdMask: "????????000C0C52"),

        // ── Intel Meteor Lake — family 6, model 0xAA ─────────────────────────────
        new(6, 0xAA, 0xAA,  0, -1, -1, -1, "Core Ultra 9 185H (ES)",
            ProcessorIdMask: "????????000A06A0"),
        new(6, 0xAA, 0xAA,  1, -1, -1, -1, "Core Ultra 9 185H (QS)",
            ProcessorIdMask: "????????000A06A1"),

        // ── Intel Raptor Lake — family 6, model 0xB7 ─────────────────────────────
        new(6, 0xB7, 0xB7,  0, 24, 32, -1, "13th Gen Intel(R) Core(TM) i9-13900K (ES)",
            ProcessorIdMask: "????????000906B0"),
        new(6, 0xB7, 0xB7,  0, 16, 24, -1, "13th Gen Intel(R) Core(TM) i7-13700K (ES)",
            ProcessorIdMask: "????????000906B0"),
        new(6, 0xB7, 0xB7, -1, 24, 32, 1600, "13th Gen Intel(R) Core(TM) i9-13900 (ES)"),

        // ── Intel Alder Lake — family 6, model 0x97 ──────────────────────────────
        new(6, 0x97, 0x97,  1, 16, 24, -1, "12th Gen Intel(R) Core(TM) i9-12900K (QS)",
            ProcessorIdMask: "????????00090671"),
        new(6, 0x97, 0x97,  0, 16, 24, -1, "12th Gen Intel(R) Core(TM) i9-12900K (ES)",
            ProcessorIdMask: "????????00090670"),
        new(6, 0x97, 0x97,  0, 6, 12, 800, "12th Gen Intel(R) Core(TM) i5-12500 (ES)",
            ProcessorIdMask: "????????00090670"),
        #endregion

        #region AMD
        // ── AMD Zen 4 — family 0x19, model range 0x10–0x1F ───────────────────────
        new(0x19, 0x10, 0x1F, -1, 16, 32, -1, "Ryzen 9 7950X (ES)"),
        new(0x19, 0x10, 0x1F, -1, 12, 24, -1, "Ryzen 9 7900X (ES)"),
        new(0x19, 0x10, 0x1F, -1,  8, 16, -1, "Ryzen 7 7700X (ES)"),
        new(0x19, 0x10, 0x1F, -1,  6, 12, -1, "Ryzen 5 7600X (ES)"),

        // ── AMD Zen 5 — family 0x1A ───────────────────────────────────────────────
        new(0x1A, 0x00, 0x0F, -1, 16, 32, -1, "Ryzen 9 9950X (ES)"),
        new(0x1A, 0x00, 0x0F, -1,  8, 16, -1, "Ryzen 7 9700X (ES)"),
        #endregion
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
            // Intel 800 Series (Arrow Lake)
            ["E104"] = ("Intel Arrow Lake", "Intel Z890", "PCI-Express 5.0 (32.0 GT/s)"),
            ["E106"] = ("Intel Arrow Lake", "Intel B860", "PCI-Express 5.0 (32.0 GT/s)"),
            ["E108"] = ("Intel Arrow Lake", "Intel H810", "PCI-Express 5.0 (32.0 GT/s)"),
            ["E10C"] = ("Intel Arrow Lake", "Intel Q870", "PCI-Express 5.0 (32.0 GT/s)"),
            ["E114"] = ("Intel Arrow Lake", "Intel W880", "PCI-Express 5.0 (32.0 GT/s)"),

            // Intel 700 Series
            ["7A04"] = ("Intel Raptor Lake", "Intel Z790", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A06"] = ("Intel Raptor Lake", "Intel B760", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A08"] = ("Intel Raptor Lake", "Intel H770", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A0C"] = ("Intel Raptor Lake", "Intel Q670", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A14"] = ("Intel Raptor Lake", "Intel W680", "PCI-Express 4.0 (16.0 GT/s)"),

            // Intel 600 Series
            ["7A84"] = ("Intel Alder Lake", "Intel Z690", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A86"] = ("Intel Alder Lake", "Intel H670", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A88"] = ("Intel Alder Lake", "Intel B660", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A8C"] = ("Intel Alder Lake", "Intel Q670", "PCI-Express 4.0 (16.0 GT/s)"),
            ["7A8E"] = ("Intel Alder Lake", "Intel H610", "PCI-Express 4.0 (16.0 GT/s)"),

            // Intel 500 Series
            ["A0FC"] = ("Intel Rocket Lake", "Intel Z590", "PCI-Express 3.0 (8.0 GT/s)"),
            ["A0FE"] = ("Intel Rocket Lake", "Intel H570", "PCI-Express 3.0 (8.0 GT/s)"),
            ["A0F0"] = ("Intel Rocket Lake", "Intel B560", "PCI-Express 3.0 (8.0 GT/s)"),
            ["A0F4"] = ("Intel Rocket Lake", "Intel Q570", "PCI-Express 3.0 (8.0 GT/s)"),
            ["A082"] = ("Intel Rocket Lake", "Intel H510", "PCI-Express 3.0 (8.0 GT/s)"),

            // Intel 400 Series
            ["0684"] = ("Intel Comet Lake", "Intel Z490", "PCI-Express 3.0 (8.0 GT/s)"),
            ["0687"] = ("Intel Comet Lake", "Intel H470", "PCI-Express 3.0 (8.0 GT/s)"),
            ["06A4"] = ("Intel Comet Lake", "Intel B460", "PCI-Express 3.0 (8.0 GT/s)"),
            ["06A1"] = ("Intel Comet Lake", "Intel Q470", "PCI-Express 3.0 (8.0 GT/s)"),
            ["06D2"] = ("Intel Comet Lake", "Intel H410", "PCI-Express 3.0 (8.0 GT/s)"),

            // Intel 300 Series
            ["A2C9"] = ("Intel Coffee Lake", "Intel Z390", "PCI-Express 3.0 (8.0 GT/s)"),
            ["A2CC"] = ("Intel Coffee Lake", "Intel Z370", "PCI-Express 3.0 (8.0 GT/s)"),
            ["A303"] = ("Intel Coffee Lake", "Intel B365", "PCI-Express 3.0 (8.0 GT/s)"),
            ["A30D"] = ("Intel Coffee Lake", "Intel B360", "PCI-Express 3.0 (8.0 GT/s)"),
            ["A30C"] = ("Intel Coffee Lake", "Intel H370", "PCI-Express 3.0 (8.0 GT/s)"),
            ["A31E"] = ("Intel Coffee Lake", "Intel H310", "PCI-Express 3.0 (8.0 GT/s)"),
        },

        ["1022"] = new()
        {
            // AM5
            ["14D8"] = ("AMD Zen 4/5", "X870E", "PCI-Express 5.0 (32.0 GT/s)"),
            ["14D9"] = ("AMD Zen 4/5", "X870", "PCI-Express 5.0 (32.0 GT/s)"),
            ["14DA"] = ("AMD Zen 4/5", "B850", "PCI-Express 5.0 (32.0 GT/s)"),
            ["14DB"] = ("AMD Zen 4/5", "B840", "PCI-Express 4.0 (16.0 GT/s)"),
            ["43B5"] = ("AMD Zen 4", "X670E", "PCI-Express 5.0 (32.0 GT/s)"),
            ["43B6"] = ("AMD Zen 4", "X670", "PCI-Express 5.0 (32.0 GT/s)"),
            ["43B7"] = ("AMD Zen 4", "B650E", "PCI-Express 5.0 (32.0 GT/s)"),
            ["43B8"] = ("AMD Zen 4", "B650", "PCI-Express 5.0 (32.0 GT/s)"),
            ["43B9"] = ("AMD Zen 4", "A620", "PCI-Express 4.0 (16.0 GT/s)"),

            // AM4
            ["790B"] = ("AMD Zen", "X570", "PCI-Express 4.0 (16.0 GT/s)"),
            ["790E"] = ("AMD Zen", "X570", "PCI-Express 4.0 (16.0 GT/s)"),
            ["43C5"] = ("AMD Zen", "B550", "PCI-Express 4.0 (16.0 GT/s)"),
            ["43C6"] = ("AMD Zen", "A520", "PCI-Express 3.0 (8.0 GT/s)"),
            ["1450"] = ("AMD Zen", "X470", "PCI-Express 3.0 (8.0 GT/s)"),
            ["1451"] = ("AMD Zen", "B450", "PCI-Express 3.0 (8.0 GT/s)"),
            ["1452"] = ("AMD Zen", "X370", "PCI-Express 3.0 (8.0 GT/s)"),
            ["1453"] = ("AMD Zen", "B350", "PCI-Express 3.0 (8.0 GT/s)"),
            ["1454"] = ("AMD Zen", "A320", "PCI-Express 3.0 (8.0 GT/s)"),
        },
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