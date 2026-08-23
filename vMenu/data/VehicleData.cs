using System;
using System.Collections.Generic;
using System.Linq;

using CitizenFX.Core;

using Newtonsoft.Json;

using static CitizenFX.Core.Native.API;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.data
{
    public static class VehicleData
    {
        public readonly struct VehicleColor
        {
            public readonly int id;
            public readonly string label;

            public VehicleColor(int id, string label)
            {
                if (label == "veh_color_taxi_yellow")
                {
                    if (GetLabelText("veh_color_taxi_yellow") == "NULL")
                    {
                        AddTextEntry("veh_color_taxi_yellow", $"Taxi {GetLabelText("IEC_T20_2")}");
                    }
                }
                else if (label == "veh_color_off_white")
                {
                    if (GetLabelText("veh_color_off_white") == "NULL")
                    {
                        AddTextEntry("veh_color_off_white", "Off White");
                    }
                }
                else if (label == "VERY_DARK_BLUE")
                {
                    if (GetLabelText("VERY_DARK_BLUE") == "NULL")
                    {
                        AddTextEntry("VERY_DARK_BLUE", "Very Dark Blue");
                    }
                }

                this.label = label;
                this.id = id;
            }
        }

        public static readonly List<VehicleColor> ClassicColors = new()
        {
            new VehicleColor(0, "BLACK"),
            new VehicleColor(1, "GRAPHITE"),
            new VehicleColor(2, "BLACK_STEEL"),
            new VehicleColor(3, "DARK_SILVER"),
            new VehicleColor(4, "SILVER"),
            new VehicleColor(5, "BLUE_SILVER"),
            new VehicleColor(6, "ROLLED_STEEL"),
            new VehicleColor(7, "SHADOW_SILVER"),
            new VehicleColor(8, "STONE_SILVER"),
            new VehicleColor(9, "MIDNIGHT_SILVER"),
            new VehicleColor(10, "CAST_IRON_SIL"),
            new VehicleColor(11, "ANTHR_BLACK"),

            new VehicleColor(27, "RED"),
            new VehicleColor(28, "TORINO_RED"),
            new VehicleColor(29, "FORMULA_RED"),
            new VehicleColor(30, "BLAZE_RED"),
            new VehicleColor(31, "GRACE_RED"),
            new VehicleColor(32, "GARNET_RED"),
            new VehicleColor(33, "SUNSET_RED"),
            new VehicleColor(34, "CABERNET_RED"),
            new VehicleColor(35, "CANDY_RED"),
            new VehicleColor(36, "SUNRISE_ORANGE"),
            new VehicleColor(37, "GOLD"),
            new VehicleColor(38, "ORANGE"),

            new VehicleColor(49, "DARK_GREEN"),
            new VehicleColor(50, "RACING_GREEN"),
            new VehicleColor(51, "SEA_GREEN"),
            new VehicleColor(52, "OLIVE_GREEN"),
            new VehicleColor(53, "BRIGHT_GREEN"),
            new VehicleColor(54, "PETROL_GREEN"),

            new VehicleColor(61, "GALAXY_BLUE"),
            new VehicleColor(62, "DARK_BLUE"),
            new VehicleColor(63, "SAXON_BLUE"),
            new VehicleColor(64, "BLUE"),
            new VehicleColor(65, "MARINER_BLUE"),
            new VehicleColor(66, "HARBOR_BLUE"),
            new VehicleColor(67, "DIAMOND_BLUE"),
            new VehicleColor(68, "SURF_BLUE"),
            new VehicleColor(69, "NAUTICAL_BLUE"),
            new VehicleColor(70, "ULTRA_BLUE"),
            new VehicleColor(71, "PURPLE"),
            new VehicleColor(72, "SPIN_PURPLE"),
            new VehicleColor(73, "RACING_BLUE"),
            new VehicleColor(74, "LIGHT_BLUE"),

            new VehicleColor(88, "YELLOW"),
            new VehicleColor(89, "RACE_YELLOW"),
            new VehicleColor(90, "BRONZE"),
            new VehicleColor(91, "FLUR_YELLOW"),
            new VehicleColor(92, "LIME_GREEN"),

            new VehicleColor(94, "UMBER_BROWN"),
            new VehicleColor(95, "CREEK_BROWN"),
            new VehicleColor(96, "CHOCOLATE_BROWN"),
            new VehicleColor(97, "MAPLE_BROWN"),
            new VehicleColor(98, "SADDLE_BROWN"),
            new VehicleColor(99, "STRAW_BROWN"),
            new VehicleColor(100, "MOSS_BROWN"),
            new VehicleColor(101, "BISON_BROWN"),
            new VehicleColor(102, "WOODBEECH_BROWN"),
            new VehicleColor(103, "BEECHWOOD_BROWN"),
            new VehicleColor(104, "SIENNA_BROWN"),
            new VehicleColor(105, "SANDY_BROWN"),
            new VehicleColor(106, "BLEECHED_BROWN"),
            new VehicleColor(107, "CREAM"),

            new VehicleColor(111, "WHITE"),
            new VehicleColor(112, "FROST_WHITE"),

            new VehicleColor(135, "HOT PINK"),
            new VehicleColor(136, "SALMON_PINK"),
            new VehicleColor(137, "PINK"),
            new VehicleColor(138, "BRIGHT_ORANGE"),

            new VehicleColor(141, "MIDNIGHT_BLUE"),
            new VehicleColor(142, "MIGHT_PURPLE"),
            new VehicleColor(143, "WINE_RED"),

            new VehicleColor(145, "BRIGHT_PURPLE"),
            new VehicleColor(146, "VERY_DARK_BLUE"),
            new VehicleColor(147, "BLACK_GRAPHITE"),

            new VehicleColor(150, "LAVA_RED"),
        };

        public static readonly List<VehicleColor> MatteColors = new()
        {
            new VehicleColor(12, "BLACK"),
            new VehicleColor(13, "GREY"),
            new VehicleColor(14, "LIGHT_GREY"),

            new VehicleColor(39, "RED"),
            new VehicleColor(40, "DARK_RED"),
            new VehicleColor(41, "ORANGE"),
            new VehicleColor(42, "YELLOW"),

            new VehicleColor(55, "LIME_GREEN"),

            new VehicleColor(82, "DARK_BLUE"),
            new VehicleColor(83, "BLUE"),
            new VehicleColor(84, "MIDNIGHT_BLUE"),

            new VehicleColor(128, "GREEN"),

            new VehicleColor(148, "Purple"),
            new VehicleColor(149, "MIGHT_PURPLE"),

            new VehicleColor(151, "MATTE_FOR"),
            new VehicleColor(152, "MATTE_OD"),
            new VehicleColor(153, "MATTE_DIRT"),
            new VehicleColor(154, "MATTE_DESERT"),
            new VehicleColor(155, "MATTE_FOIL"),
        };

        public static readonly List<VehicleColor> MetalColors = new()
        {
            new VehicleColor(117, "BR_STEEL"),
            new VehicleColor(118, "BR BLACK_STEEL"),
            new VehicleColor(119, "BR_ALUMINIUM"),
            new VehicleColor(120, "CHROME"),
            new VehicleColor(158, "GOLD_P"),
            new VehicleColor(159, "GOLD_S"),
        };

        public static readonly List<VehicleColor> UtilColors = new()
        {
            new VehicleColor(15, "BLACK"),
            new VehicleColor(16, "FMMC_COL1_1"),
            new VehicleColor(17, "DARK_SILVER"),
            new VehicleColor(18, "SILVER"),
            new VehicleColor(19, "BLACK_STEEL"),
            new VehicleColor(20, "SHADOW_SILVER"),

            new VehicleColor(43, "DARK_RED"),
            new VehicleColor(44, "RED"),
            new VehicleColor(45, "GARNET_RED"),

            new VehicleColor(56, "DARK_GREEN"),
            new VehicleColor(57, "GREEN"),

            new VehicleColor(75, "DARK_BLUE"),
            new VehicleColor(76, "MIDNIGHT_BLUE"),
            new VehicleColor(77, "SAXON_BLUE"),
            new VehicleColor(78, "NAUTICAL_BLUE"),
            new VehicleColor(79, "BLUE"),
            new VehicleColor(80, "FMMC_COL1_13"),
            new VehicleColor(81, "BRIGHT_PURPLE"),

            new VehicleColor(93, "STRAW_BROWN"),

            new VehicleColor(108, "UMBER_BROWN"),
            new VehicleColor(109, "MOSS_BROWN"),
            new VehicleColor(110, "SANDY_BROWN"),

            new VehicleColor(122, "veh_color_off_white"),

            new VehicleColor(125, "BRIGHT_GREEN"),

            new VehicleColor(127, "HARBOR_BLUE"),

            new VehicleColor(134, "FROST_WHITE"),

            new VehicleColor(139, "LIME_GREEN"),
            new VehicleColor(140, "ULTRA_BLUE"),

            new VehicleColor(144, "GREY"),

            new VehicleColor(157, "LIGHT_BLUE"),

            new VehicleColor(160, "YELLOW")
        };

        public static readonly List<VehicleColor> WornColors = new()
        {
            new VehicleColor(21, "BLACK"),
            new VehicleColor(22, "GRAPHITE"),
            new VehicleColor(23, "LIGHT_GREY"),
            new VehicleColor(24, "SILVER"),
            new VehicleColor(25, "BLUE_SILVER"),
            new VehicleColor(26, "SHADOW_SILVER"),

            new VehicleColor(46, "RED"),
            new VehicleColor(47, "SALMON_PINK"),
            new VehicleColor(48, "DARK_RED"),

            new VehicleColor(58, "DARK_GREEN"),
            new VehicleColor(59, "GREEN"),
            new VehicleColor(60, "SEA_GREEN"),

            new VehicleColor(85, "DARK_BLUE"),
            new VehicleColor(86, "BLUE"),
            new VehicleColor(87, "LIGHT_BLUE"),

            new VehicleColor(113, "SANDY_BROWN"),
            new VehicleColor(114, "BISON_BROWN"),
            new VehicleColor(115, "CREEK_BROWN"),
            new VehicleColor(116, "BLEECHED_BROWN"),

            new VehicleColor(121, "veh_color_off_white"),

            new VehicleColor(123, "ORANGE"),
            new VehicleColor(124, "SUNRISE_ORANGE"),

            new VehicleColor(126, "veh_color_taxi_yellow"),

            new VehicleColor(129, "RACING_GREEN"),
            new VehicleColor(130, "ORANGE"),
            new VehicleColor(131, "WHITE"),
            new VehicleColor(132, "FROST_WHITE"),
            new VehicleColor(133, "OLIVE_GREEN"),
        };

        public static readonly List<VehicleColor> ChameleonColors = new()
        {
            new VehicleColor(161, "ANOD_RED"),
            new VehicleColor(162, "ANOD_WINE"),
            new VehicleColor(163, "ANOD_PURPLE"),
            new VehicleColor(164, "ANOD_BLUE"),
            new VehicleColor(165, "ANOD_GREEN"),
            new VehicleColor(166, "ANOD_LIME"),
            new VehicleColor(167, "ANOD_COPPER"),
            new VehicleColor(168, "ANOD_BRONZE"),
            new VehicleColor(169, "ANOD_CHAMPAGNE"),
            new VehicleColor(170, "ANOD_GOLD"),
            new VehicleColor(171, "GREEN_BLUE_FLIP"),
            new VehicleColor(172, "GREEN_RED_FLIP"),
            new VehicleColor(173, "GREEN_BROW_FLIP"),
            new VehicleColor(174, "GREEN_TURQ_FLIP"),
            new VehicleColor(175, "GREEN_PURP_FLIP"),
            new VehicleColor(176, "TEAL_PURP_FLIP"),
            new VehicleColor(177, "TURQ_RED_FLIP"),
            new VehicleColor(178, "TURQ_PURP_FLIP"),
            new VehicleColor(179, "CYAN_PURP_FLIP"),
            new VehicleColor(180, "BLUE_PINK_FLIP"),
            new VehicleColor(181, "BLUE_GREEN_FLIP"),
            new VehicleColor(182, "PURP_RED_FLIP"),
            new VehicleColor(183, "PURP_GREEN_FLIP"),
            new VehicleColor(184, "MAGEN_GREE_FLIP"),
            new VehicleColor(185, "MAGEN_YELL_FLIP"),
            new VehicleColor(186, "BURG_GREEN_FLIP"),
            new VehicleColor(187, "MAGEN_CYAN_FLIP"),
            new VehicleColor(188, "COPPE_PURP_FLIP"),
            new VehicleColor(189, "MAGEN_ORAN_FLIP"),
            new VehicleColor(190, "RED_ORANGE_FLIP"),
            new VehicleColor(191, "ORANG_PURP_FLIP"),
            new VehicleColor(192, "ORANG_BLUE_FLIP"),
            new VehicleColor(193, "WHITE_PURP_FLIP"),
            new VehicleColor(194, "RED_RAINBO_FLIP"),
            new VehicleColor(195, "BLU_RAINBO_FLIP"),
            new VehicleColor(196, "DARKGREENPEARL"),
            new VehicleColor(197, "DARKTEALPEARL"),
            new VehicleColor(198, "DARKBLUEPEARL"),
            new VehicleColor(199, "DARKPURPLEPEARL"),
            new VehicleColor(200, "OIL_SLICK_PEARL"),
            new VehicleColor(201, "LIT_GREEN_PEARL"),
            new VehicleColor(202, "LIT_BLUE_PEARL"),
            new VehicleColor(203, "LIT_PURP_PEARL"),
            new VehicleColor(204, "LIT_PINK_PEARL"),
            new VehicleColor(205, "OFFWHITE_PRISMA"),
            new VehicleColor(206, "PINK_PEARL"),
            new VehicleColor(207, "YELLOW_PEARL"),
            new VehicleColor(208, "GREEN_PEARL"),
            new VehicleColor(209, "BLUE_PEARL"),
            new VehicleColor(210, "CREAM_PEARL"),
            new VehicleColor(211, "WHITE_PRISMA"),
            new VehicleColor(212, "GRAPHITE_PRISMA"),
            new VehicleColor(213, "DARKBLUEPRISMA"),
            new VehicleColor(214, "DARKPURPPRISMA"),
            new VehicleColor(215, "HOT_PINK_PRISMA"),
            new VehicleColor(216, "RED_PRISMA"),
            new VehicleColor(217, "GREEN_PRISMA"),
            new VehicleColor(218, "BLACK_PRISMA"),
            new VehicleColor(219, "OIL_SLIC_PRISMA"),
            new VehicleColor(220, "RAINBOW_PRISMA"),
            new VehicleColor(221, "BLACK_HOLO"),
            new VehicleColor(222, "WHITE_HOLO"),
            new VehicleColor(223, "YKTA_MONOCHROME"),
            new VehicleColor(224, "YKTA_NITE_DAY"),
            new VehicleColor(225, "YKTA_VERLIERER2"),
            new VehicleColor(226, "YKTA_SPRUNK_EX"),
            new VehicleColor(227, "YKTA_VICE_CITY"),
            new VehicleColor(228, "YKTA_SYNTHWAVE"),
            new VehicleColor(229, "YKTA_FOUR_SEASO"),
            new VehicleColor(230, "YKTA_M9_THROWBA"),
            new VehicleColor(231, "YKTA_BUBBLEGUM"),
            new VehicleColor(232, "YKTA_FULL_RBOW"),
            new VehicleColor(233, "YKTA_SUNSETS"),
            new VehicleColor(234, "YKTA_THE_SEVEN"),
            new VehicleColor(235, "YKTA_KAMENRIDER"),
            new VehicleColor(236, "YKTA_CHROMABERA"),
            new VehicleColor(237, "YKTA_CHRISTMAS"),
            new VehicleColor(238, "YKTA_TEMPERATUR"),
            new VehicleColor(239, "YKTA_HSW"),
            new VehicleColor(240, "YKTA_ELECTRO"),
            new VehicleColor(241, "YKTA_MONIKA"),
            new VehicleColor(242, "YKTA_FUBUKI"),

        };

        public class CustomVehicleClassJson
        {
            public string name = "";
            public List<string> vehicles = new List<string>();
        }
        public class VehicleInfoJson
        {
            public List<string> addons = new List<string>();
            public List<string> blacklisted = new List<string>();
            public List<string> hidden = new List<string>();
            public List<string> sporty = new List<string>();
            public List<CustomVehicleClassJson> customClasses = new List<CustomVehicleClassJson>();
        }

        private static HashSet<string> GetVehicleInfoSet(string listName, List<string> vehicles)
        {
            HashSet<string> set = new HashSet<string>();

            foreach (var vehicle in vehicles)
            {
                if (!IsModelInCdimage((uint)GetHashKey(vehicle)))
                {
                    Debug.WriteLine($"^3[vMenu] [WARNING]^7 Vehicle \"{vehicle}\" in vehicleInfo[{listName}] not available");
                }
                else
                {
                    set.Add(vehicle.ToLower());
                }
            }

            return set;
        }

        public static void InitVehicleInfo(VehicleInfoJson info)
        {
            AddonVehicles = GetVehicleInfoSet("addons", info.addons);
            VehicleBlacklist = GetVehicleInfoSet("blacklisted", info.blacklisted);
            VehicleDisablelist = GetVehicleInfoSet("hidden", info.hidden);
            SportyVehicles = GetVehicleInfoSet("sporty", info.sporty);

            var customClasses = info.customClasses;
            HashSet<string> seenClassNames = new HashSet<string>();
            foreach (var customClass in customClasses)
            {
                var className = customClass.name;
                if (seenClassNames.Contains(className))
                {
                    Debug.WriteLine($"[WARNING] Duplicate custom vehicle class \"{className}\" was not added.");
                    continue;
                }
                seenClassNames.Add(className);

                var classVehiclesSet = GetVehicleInfoSet("customClasses", customClass.vehicles);
                if (classVehiclesSet.Count == 0)
                    continue;

                CustomVehicleClasses.Add(new CustomVehicleClass
                {
                    Name = className,
                    Vehicles = classVehiclesSet
                });
            }
        }

        public static Dictionary<int, string> ClassIdToName { get; } =
            Enumerable
                .Range(0, 23)
                .ToDictionary(c => c, c => GetLabelText($"VEH_CLASS_{c}"));

        public class CustomVehicleClass
        {
            public string Name { get; set; }
            public HashSet<string> Vehicles { get; set; }
        }

        public static HashSet<string> AddonVehicles { get; private set; } = null;
        public static HashSet<string> VehicleBlacklist { get; private set; } = null;
        public static HashSet<string> VehicleDisablelist { get; private set; } = null;
        public static HashSet<string> SportyVehicles { get; private set; } = null;
        public static List<CustomVehicleClass> CustomVehicleClasses { get; private set; } =
            new List<CustomVehicleClass>();

        private static List<bool> allowedClasses;
        public static List<bool> AllowedClasses
        {
            get
            {
                if (allowedClasses != null)
                    return allowedClasses;

                allowedClasses = new List<bool>()
                {
                    IsAllowed(Permission.VSCompacts, checkAnyway: true),
                    IsAllowed(Permission.VSSedans, checkAnyway: true),
                    IsAllowed(Permission.VSSUVs, checkAnyway: true),
                    IsAllowed(Permission.VSCoupes, checkAnyway: true),
                    IsAllowed(Permission.VSMuscle, checkAnyway: true),
                    IsAllowed(Permission.VSSportsClassic, checkAnyway: true),
                    IsAllowed(Permission.VSSports, checkAnyway: true),
                    IsAllowed(Permission.VSSuper, checkAnyway: true),
                    IsAllowed(Permission.VSMotorcycles, checkAnyway: true),
                    IsAllowed(Permission.VSOffRoad, checkAnyway: true),
                    IsAllowed(Permission.VSIndustrial, checkAnyway: true),
                    IsAllowed(Permission.VSUtility, checkAnyway: true),
                    IsAllowed(Permission.VSVans, checkAnyway: true),
                    IsAllowed(Permission.VSCycles, checkAnyway: true),
                    IsAllowed(Permission.VSBoats, checkAnyway: true),
                    IsAllowed(Permission.VSHelicopters, checkAnyway: true),
                    IsAllowed(Permission.VSPlanes, checkAnyway: true),
                    IsAllowed(Permission.VSService, checkAnyway: true),
                    IsAllowed(Permission.VSEmergency, checkAnyway: true),
                    IsAllowed(Permission.VSMilitary, checkAnyway: true),
                    IsAllowed(Permission.VSCommercial, checkAnyway: true),
                    IsAllowed(Permission.VSTrains, checkAnyway: true),
                    IsAllowed(Permission.VSOpenWheel, checkAnyway: true)
                };
                return allowedClasses;
            }
        }

        public class VehicleModelInfo
        {
            public static string GetName(uint hash, string shortname, out bool hasProperName)
            {
                string displayname = GetDisplayNameFromVehicleModel(hash);
                if (string.IsNullOrWhiteSpace(displayname) || displayname == "CARNOTFOUND")
                {
                    hasProperName = false;
                    return shortname.ToUpper();
                }

                string labelname = GetLabelText(displayname);
                if (labelname == "NULL")
                {
                    hasProperName = false;
                    return displayname;
                }

                hasProperName = true;
                return labelname;
            }

            public static string GetManufacturer(uint hash)
            {
                string makename = GetMakeNameFromVehicleModel(hash);
                if (string.IsNullOrWhiteSpace(makename) || makename == "CARNOTFOUND" || makename == "(null)")
                    return "NULL";

                string labelname = GetLabelText(makename);
                if (labelname == "NULL" || labelname == "(null)")
                    return makename;

                return labelname;
            }

            public VehicleModelInfo(string shortname)
            {
                ShortnameCaseSensitive = shortname;
                Shortname = shortname.ToLower();
                Hash = (uint)GetHashKey(shortname);
            }

            public uint Hash { get; }
            public string ShortnameCaseSensitive { get; }
            public string Shortname { get; }
            public string Name => GetName(Hash, Shortname, out var _);
            public bool HasProperName
            {
                get
                {
                    GetName(Hash, Shortname, out var hasProperName);
                    return hasProperName;
                }
            }
            public string Manufacturer => GetManufacturer(Hash);
            public int Class => GetVehicleClassFromName(Hash);
            public string ClassName => GetLabelText($"VEH_CLASS_{Class}");
            public bool IsAddon
            {
                get => AddonVehicles.Contains(Shortname);
            }

            private HashSet<string> customVehicleClasses = null;
            public HashSet<string> CustomVehicleClasses
            {
                get
                {
                    if (customVehicleClasses != null)
                        return customVehicleClasses;

                    customVehicleClasses = [..VehicleData.CustomVehicleClasses
                        .Where(c => c.Vehicles.Contains(Shortname))
                        .Select(c => c.Name)];

                    return customVehicleClasses;
                }
            }

            public bool IsAllowed =>
                AllowedClasses[Class] &&
                (!IsAddon || IsAllowed(Permission.VSAddon)) &&
                (!IsBlacklisted || IsAllowed(Permission.VOVehiclesBlacklist)) &&
                (!IsHidden || IsAllowed(Permission.VODisableFromDefaultList));

            public bool DisplayVehicle => IsAllowed && !IsHidden;

            public bool IsBlacklisted => VehicleBlacklist.Contains(Shortname);
            public bool IsHidden => VehicleDisablelist.Contains(Shortname);
            public bool IsSporty => SportyVehicles.Contains(Shortname);
        }

        private static Dictionary<string, VehicleModelInfo> allVehicles = null;

        public static Dictionary<string, VehicleModelInfo> AllVehicles
        {
            get
            {
                if (allVehicles != null)
                    return allVehicles;

                allVehicles = ((List<object>)GetAllVehicleModels())
                    .Select(shortname => new VehicleModelInfo((string)shortname))
                    .ToDictionary(vi => vi.Shortname, vi => vi);
                return allVehicles;
            }
        }

        public static Dictionary<uint, VehicleModelInfo> hashToVehicle;
        public static Dictionary<uint, VehicleModelInfo> HashToVehicle
        {
            get
            {
                if (hashToVehicle != null)
                    return hashToVehicle;

                hashToVehicle = AllVehicles.ToDictionary(kv => kv.Value.Hash, kv => kv.Value);
                return hashToVehicle;
            }
        }

        private static HashSet<VehicleModelInfo> allowedVehicles;
        public static HashSet<VehicleModelInfo> AllowedVehicles
        {
            get
            {
                if (allowedVehicles != null)
                    return allowedVehicles;

                allowedVehicles = [.. AllVehicles.Values.Where(vi => vi.IsAllowed)];
                return allowedVehicles;
            }
        }


        private static HashSet<VehicleModelInfo> displayVehicles;

        public static HashSet<VehicleModelInfo> DisplayVehicles
        {
            get
            {
                if (displayVehicles != null)
                    return displayVehicles;

                displayVehicles = [.. AllowedVehicles.Where(vi => vi.DisplayVehicle)];
                return displayVehicles;
            }
        }

        public static Tuple<string, string, string> GetDigitsNondigitsRest(string s)
        {
            int i = 0;
            for (; i < s.Length && char.IsDigit(s[i]); ++i) ;

            string digits = s.Substring(0, i);

            int k = i;
            for (; i < s.Length && !char.IsDigit(s[i]); ++i) ;

            string nondigits = s.Substring(k, i - k);
            string rest = s.Substring(i);

            return new Tuple<string, string, string>(digits, nondigits, rest);
        }

        public static int StringCompareWithNumbers(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return s1.CompareTo(s2);

            var dndr1 = GetDigitsNondigitsRest(s1);
            var dndr2 = GetDigitsNondigitsRest(s2);

            var digits1 = dndr1.Item1;
            var digits2 = dndr2.Item1;

            int ret;
            if (!string.IsNullOrEmpty(digits1) && !string.IsNullOrEmpty(digits2))
            {
                int num1 = int.Parse(digits1);
                int num2 = int.Parse(digits2);

                ret = num1.CompareTo(num2);
                if (ret != 0)
                    return ret;

                // If both digit strings parse to the same number, order the one with (more) leading zeros before
                ret = digits2.Length.CompareTo(digits1.Length);
                if (ret != 0)
                    return ret;
            }
            else if (!string.IsNullOrEmpty(digits1) && string.IsNullOrEmpty(digits2))
            {
                return -1;
            }
            else if (string.IsNullOrEmpty(digits1) && !string.IsNullOrEmpty(digits2))
            {
                return 1;
            }

            var nondigits1 = dndr1.Item2;
            var nondigits2 = dndr2.Item2;

            ret = nondigits1.CompareTo(nondigits2);
            if (ret != 0)
                return ret;

            var rest1 = dndr1.Item3;
            var rest2 = dndr2.Item3;

            return StringCompareWithNumbers(rest1, rest2);
        }

        public static Tuple<string, string> GetYearModel(string name)
        {
            if (name.Length <= 5)
                return new Tuple<string, string>("", name);

            bool hasYear = true;
            for (int i = 0; i < 4; i++)
            {
                if (!char.IsDigit(name[i]))
                {
                    hasYear = false;
                    break;
                }
            }
            if (name[4] != ' ')
                hasYear = false;

            if (!hasYear)
                return new Tuple<string, string>("", name);

            return new Tuple<string, string>(name.Substring(0, 4), name.Substring(5));
        }

        public static int CompareVehicleNames(string name1, string name2)
        {
            var yearModel1 = GetYearModel(name1);
            var yearModel2 = GetYearModel(name2);

            var year1Str = yearModel1.Item1;
            year1Str = string.IsNullOrEmpty(year1Str) ? "0000" : year1Str;
            var year2Str = yearModel2.Item1;
            year2Str = string.IsNullOrEmpty(year2Str) ? "0000" : year2Str;

            var year1 = int.Parse(year1Str);
            var year2 = int.Parse(year2Str);

            var model1 = yearModel1.Item2;
            var model2 = yearModel2.Item2;

            int ret = StringCompareWithNumbers(model1, model2);
            if (ret != 0)
                return ret;

            return year1.CompareTo(year2);
        }


        public static int CompareManufacturers(string s1, string s2)
        {
            if (s1 == "NULL" && s2 != "NULL")
            {
                return 1;
            }
            else if (s1 != "NULL" && s2 == "NULL")
            {
                return -1;
            }
            else
            {
                return string.Compare(s1, s2);
            }
        }

        private static Dictionary<int, int> compareVehicleClassDict =
            new int[] { 0, 3, 4, 9, 22, 1, 6, 5, 7, 2, 12, 8, 15, 16, 14, 20, 18, 10, 19, 17, 11, 13, 21 }
                .Select((num, ix) => new KeyValuePair<int, int>(num, ix))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        public static int CompareClasses(int i1, int i2) =>
            compareVehicleClassDict[i1].CompareTo(compareVehicleClassDict[i2]);


        public enum VehicleFilterFilterDefaultMods
        {
            All,
            With,
            Without,
        };

        public struct VehicleFilter
        {
            public string Name;
            public string Manufacturer;
            public string CustomClass;
            public string RockstarClass;
            public VehicleFilterFilterDefaultMods FilterDefaultMods;

            private bool IsNameMatching(VehicleModelInfo info, string name)
            {
                if (string.IsNullOrEmpty(Name))
                    return true;

                return
                    info.Name.ToLower().Contains(Name.ToLower()) ||
                    info.Shortname.ToLower().Contains(Name.ToLower()) ||
                    (!string.IsNullOrEmpty(name) && name.ToLower().Contains(Name.ToLower()));
            }

            private bool IsManufacturerMatching(VehicleModelInfo info)
            {
                if (string.IsNullOrEmpty(Manufacturer))
                    return true;

                return info.Manufacturer == Manufacturer;
            }

            private bool IsCustomClassMatching(VehicleModelInfo info)
            {
                if (string.IsNullOrEmpty(CustomClass))
                    return true;

                return info.CustomVehicleClasses.Contains(CustomClass);
            }

            private bool IsRockstarClassMatching(VehicleModelInfo info)
            {
                if (string.IsNullOrEmpty(RockstarClass))
                    return true;

                return info.ClassName == RockstarClass;
            }

            private bool IsDefaultModsMatching(VehicleModelInfo info)
            {
                if (FilterDefaultMods == VehicleFilterFilterDefaultMods.All)
                {
                    return true;
                }
                return StorageManager.TryGetSavedVehicleMods(info) != null
                    ? FilterDefaultMods == VehicleFilterFilterDefaultMods.With
                    : FilterDefaultMods == VehicleFilterFilterDefaultMods.Without;
            }

            public bool IsMatching(VehicleModelInfo info, string name = null) =>
                info != null &&
                IsNameMatching(info, name) &&
                IsManufacturerMatching(info) &&
                IsCustomClassMatching(info) &&
                IsRockstarClassMatching(info) &&
                IsDefaultModsMatching(info);
        }
    }
}
