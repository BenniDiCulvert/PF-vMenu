using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.data;
using vMenuClient.MenuAPIWrapper;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;
using static vMenuShared.PermissionsManager;


namespace vMenuClient.menus
{
    public class VehicleCustomization
    {
        #region Variables
        // Menu variable, will be defined in CreateMenu()
        private Menu menu;
        public static Dictionary<uint, Dictionary<int, string>> VehicleExtras;

        // Submenus
        public Menu VehicleModMenu { get; private set; }
        public Menu VehicleComponentsMenu { get; private set; }
        public Menu VehicleLiveriesMenu { get; private set; }
        public Menu VehicleColorsMenu { get; private set; }
        public Menu UnderglowColorsMenu { get; private set; }

        // Public variables (getters only), return the private variables.
        public bool isCorrectVehicleType { get; private set; }
        public int RedPrimary { get; private set; } = 0;
        public int GreenPrimary { get; private set; } = 0;
        public int BluePrimary { get; private set; } = 0;
        public int FinishPrimary { get; private set; } = 0;
        public int RedSecondary { get; private set; } = 0;
        public int GreenSecondary { get; private set; } = 0;
        public int BlueSecondary { get; private set; } = 0;
        public int FinishSecondary { get; private set; } = 0;
        public object RedUnderglow { get; private set; }
        public object GreenUnderglow { get; private set; }
        public object BlueUnderglow { get; private set; }

        private static readonly LanguageManager Lm = new LanguageManager();

        private Dictionary<MenuItem, int> vehicleExtras = new Dictionary<MenuItem, int>();

        public class PlateDescriptor
        {
            public PlateDescriptor(string id, string defaultName, LicensePlateStyle style, int menuSort)
            {
                Id = id;
                DefaultName = defaultName;
                Style = style;
                MenuSort = menuSort;
            }

            public string Id { get; private set; }
            public string DefaultName { get; private set; }
            public LicensePlateStyle Style { get; private set; }
            public int MenuSort { get; private set; }
        }

        private static readonly List<PlateDescriptor> _licensePlateDescriptors = new()
        {
            new("plate01", GetLabelText("CMOD_PLA_0"), LicensePlateStyle.BlueOnWhite1, 0),
            new("plate05", GetLabelText("CMOD_PLA_4"), LicensePlateStyle.YellowOnBlack, 4),
            new("plate04", GetLabelText("CMOD_PLA_3"), LicensePlateStyle.YellowOnBlue, 3),
            new("plate02", GetLabelText("CMOD_PLA_1"), LicensePlateStyle.BlueOnWhite2, 1),
            new("plate03", GetLabelText("CMOD_PLA_2"), LicensePlateStyle.BlueOnWhite3, 2),
            new("yankton_plate", "North Yankton", LicensePlateStyle.NorthYankton, 5),
            new(null, "eCola", LicensePlateStyle.ECola, 11),
            new(null, "Las Venturas", LicensePlateStyle.LasVenturas, 6),
            new(null, "Liberty City", LicensePlateStyle.LibertyCity, 7),
            new(null, "LS Car Meet", LicensePlateStyle.LSCarMeet, 8),
            new(null, "LS Panic", LicensePlateStyle.LSPanic, 9),
            new(null, "LS Pounders", LicensePlateStyle.LSPounders, 10),
            new(null, "Sprunk", LicensePlateStyle.Sprunk, 12),
        };

        public static readonly List<PlateDescriptor> LicensePlateDescriptors =
            [.. _licensePlateDescriptors
                .OrderBy(pdscr => pdscr.MenuSort) ];

        public static readonly List<PlateDescriptor> LicensePlateDescriptorsWithId =
            [.. LicensePlateDescriptors.Where(pdscr => !string.IsNullOrEmpty(pdscr.Id))];

        public static readonly List<string> LicensePlateChoices =
            [.. LicensePlateDescriptors
                .Select(pdscr => pdscr.DefaultName)];

        public static MenuListItem SetLicensePlateType { get; private set; }

        #endregion

        /// <summary>
        /// Create menu creates the vehicle options menu.
        /// </summary>
        private void CreateMenu()
        {
            // Create the menu.
            menu = new Menu(MenuTitle, "Vehicle Customization");

            #region menu items variables
            // Create buttons.
            var setLicensePlateText = new MenuItem("License Plate Text", "Enter custom ~b~license plate text~s~.");
            var modMenuBtn = new MenuItem("Vehicle Mods", "Tune and customize your vehicle.")
            {
                Label = "→→→"
            };
            var componentsMenuBtn = new MenuItem("Vehicle Components & Extras", "Add or remove vehicle components and extras.")
            {
                Label = "→→→"
            };
            var liveriesMenuBtn = new MenuItem("Vehicle Liveries", "Apply liveries to your vehicle.")
            {
                Label = "→→→"
            };
            var colorsMenuBtn = new MenuItem("Vehicle Colors", "Paint your vehicle.")
            {
                Label = "→→→"
            };

            if (GetSettingsBool(Setting.vmenu_enable_replace_plates))
            {
                foreach (var plateDescr in LicensePlateDescriptorsWithId)
                {
                    var id = plateDescr.Id;

                    var stuff = GetConvar("vmenu_plate_override_" + id, "false");
                    if (!(stuff == "false" || stuff == null || stuff == ""))
                    {
                        var data = JsonConvert.DeserializeObject<PlateStruct>(stuff);
                        if (string.IsNullOrEmpty(data.fileName))
                        {
                            continue;
                        }

                        var index = LicensePlateDescriptors.FindIndex(pdscr => pdscr.Id == id);
                        LicensePlateChoices[index] = data.vMenuPlateName;
                    }
                }
            }

            var saveModifications = WMenuItem.CreateConfirmationButton(
                menu,
                MainMenu.MenuText["VEHICLE_CUSTOMIZATION__SAVE_AS_DEFAULT_MODS__ITEM"],
                MainMenu.MenuText["VEHICLE_CUSTOMIZATION__SAVE_AS_DEFAULT_MODS__DESC"]);
            saveModifications.Confirmed += (_s, e) =>
            {
                var vehicle = TryGetIntactDriverVehicle("do this");
                if (vehicle == null)
                {
                    return;
                }
                SaveVehicleMods(vehicle);
            };

            var deleteModifications = WMenuItem.CreateConfirmationButton(
                menu,
                MainMenu.MenuText["VEHICLE_CUSTOMIZATION__DELETE_DEFAULT_MODS__ITEM"],
                MainMenu.MenuText["VEHICLE_CUSTOMIZATION__DELETE_DEFAULT_MODS__DESC"]);
            deleteModifications.Confirmed += (_s, e) =>
            {
                var vehicle = TryGetIntactDriverVehicle("do this");
                if (vehicle == null)
                {
                    return;
                }
                DeleteSavedVehicleMods(vehicle);
            };

            SetLicensePlateType = new MenuListItem("License Plate Type", LicensePlateChoices, 0, "Select the ~b~license plate type~s~.");
            #endregion

            #region Submenus
            // Submenu's
            VehicleModMenu = Lm.GetMenu(new Menu(MenuTitle, "Vehicle Mods"));
            VehicleModMenu.InstructionalButtons.Add(Control.Jump, "Toggle Vehicle Doors");
            VehicleModMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.Jump, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((m, c) =>
            {
                var veh = GetVehicle();
                if (veh != null && veh.Exists() && !veh.IsDead && veh.Driver == Game.PlayerPed)
                {
                    var open = GetVehicleDoorAngleRatio(veh.Handle, 0) < 0.1f;
                    if (open)
                    {
                        for (var i = 0; i < 8; i++)
                        {
                            SetVehicleDoorOpen(veh.Handle, i, false, false);
                        }
                    }
                    else
                    {
                        SetVehicleDoorsShut(veh.Handle, false);
                    }
                }
            }), false));
            VehicleComponentsMenu = Lm.GetMenu(new Menu(MenuTitle, "Vehicle Components & Extras"));
            VehicleLiveriesMenu = Lm.GetMenu(new Menu(MenuTitle, "Vehicle Liveries"));
            VehicleColorsMenu = Lm.GetMenu(new Menu(MenuTitle, "Vehicle Colors"));

            MenuController.AddSubmenu(menu, VehicleModMenu);
            MenuController.AddSubmenu(menu, VehicleComponentsMenu);
            MenuController.AddSubmenu(menu, VehicleLiveriesMenu);
            MenuController.AddSubmenu(menu, VehicleColorsMenu);

            #endregion

            #region Add items to the menu.
            // Add everything to the menu. (based on permissions)
            if (IsAllowed(Permission.VOMod)) // MOD MENU
            {
                menu.AddMenuItem(modMenuBtn);
            }
            if (IsAllowed(Permission.VOColors)) // COLORS MENU
            {
                menu.AddMenuItem(colorsMenuBtn);
            }
            if (IsAllowed(Permission.VOLiveries)) // LIVERIES MENU
            {
                menu.AddMenuItem(liveriesMenuBtn);
            }
            if (IsAllowed(Permission.VOComponents)) // COMPONENTS MENU
            {
                menu.AddMenuItem(componentsMenuBtn);
            }
            if (IsAllowed(Permission.VOChangePlate))
            {
                menu.AddMenuItem(setLicensePlateText); // SET LICENSE PLATE TEXT
                menu.AddMenuItem(SetLicensePlateType); // SET LICENSE PLATE TYPE
            }
            if (IsAllowed(Permission.VOSaveMods))
            {
                menu.AddMenuItem(saveModifications.MenuItem);
                menu.AddMenuItem(deleteModifications.MenuItem);
            }
            #endregion

            #region Bind Submenus to their buttons.
            MenuController.BindMenuItem(menu, VehicleModMenu, modMenuBtn);
            MenuController.BindMenuItem(menu, VehicleComponentsMenu, componentsMenuBtn);
            MenuController.BindMenuItem(menu, VehicleLiveriesMenu, liveriesMenuBtn);
            MenuController.BindMenuItem(menu, VehicleColorsMenu, colorsMenuBtn);
            #endregion

            #region Handle button presses
            // Manage button presses.
            menu.OnItemSelect += (sender, item, index) =>
            {
                // If the player is actually in a vehicle, continue.
                if (GetVehicle() != null && GetVehicle().Exists())
                {
                    // Create a vehicle object.
                    var vehicle = GetVehicle();

                    // Check if the player is the driver of the vehicle, if so, continue.
                    if (vehicle.GetPedOnSeat(VehicleSeat.Driver) == new Ped(Game.PlayerPed.Handle))
                    {
                        // Set license plate text
                        if (item == setLicensePlateText)
                        {
                            SetLicensePlateCustomText();
                        }
                    }
                }
            };
            #endregion

            #region Handle List Changes.
            // Handle list changes.
            menu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
            {
                if (GetVehicle() != null && GetVehicle().Exists())
                {
                    var veh = GetVehicle();
                    if (item == SetLicensePlateType)
                    {
                        veh.Mods.LicensePlateStyle = LicensePlateDescriptors[newIndex].Style;
                    }
                }
            };
            #endregion

            #region Vehicle Colours Submenu Stuff
            // primary menu
            var primaryColorsMenu = Lm.GetMenu(new Menu(MenuTitle, "Primary Colors"));
            MenuController.AddSubmenu(VehicleColorsMenu, primaryColorsMenu);

            var primaryColorsBtn = new MenuItem("Primary Color") { Label = "→→→" };
            VehicleColorsMenu.AddMenuItem(primaryColorsBtn);
            MenuController.BindMenuItem(VehicleColorsMenu, primaryColorsMenu, primaryColorsBtn);

            // secondary menu
            var secondaryColorsMenu = Lm.GetMenu(new Menu(MenuTitle, "Secondary Colors"));
            MenuController.AddSubmenu(VehicleColorsMenu, secondaryColorsMenu);

            var secondaryColorsBtn = new MenuItem("Secondary Color") { Label = "→→→" };
            VehicleColorsMenu.AddMenuItem(secondaryColorsBtn);
            MenuController.BindMenuItem(VehicleColorsMenu, secondaryColorsMenu, secondaryColorsBtn);
            var ColorFinishes = new List<string>()
            {
                "Normal",
                "Metallic",
                "Pearl",
                "Matte",
                "Metal",
                "Chrome",
            };
            // color lists
            var classic = new List<string>();
            var matte = new List<string>();
            var metals = new List<string>();
            var util = new List<string>();
            var worn = new List<string>();
            var chameleon = new List<string>();
            var wheelColors = new List<string>() { "Default Alloy" };

            // Just quick and dirty solution to put this in a new enclosed section so that we can still use 'i' as a counter in the other code parts.
            {
                var i = 0;
                foreach (var vc in VehicleData.ClassicColors)
                {
                    classic.Add($"{GetLabelText(vc.label)} ({i + 1}/{VehicleData.ClassicColors.Count})");
                    i++;
                }

                i = 0;
                foreach (var vc in VehicleData.MatteColors)
                {
                    matte.Add($"{GetLabelText(vc.label)} ({i + 1}/{VehicleData.MatteColors.Count})");
                    i++;
                }

                i = 0;
                foreach (var vc in VehicleData.MetalColors)
                {
                    metals.Add($"{GetLabelText(vc.label)} ({i + 1}/{VehicleData.MetalColors.Count})");
                    i++;
                }

                i = 0;
                foreach (var vc in VehicleData.UtilColors)
                {
                    util.Add($"{GetLabelText(vc.label)} ({i + 1}/{VehicleData.UtilColors.Count})");
                    i++;
                }

                i = 0;
                foreach (var vc in VehicleData.WornColors)
                {
                    worn.Add($"{GetLabelText(vc.label)} ({i + 1}/{VehicleData.WornColors.Count})");
                    i++;
                }

                if (GetSettingsBool(Setting.vmenu_using_chameleon_colours))
                {
                    i = 0;
                    foreach (var vc in VehicleData.ChameleonColors)
                    {
                        chameleon.Add($"{GetLabelText(vc.label)} ({i + 1}/{VehicleData.ChameleonColors.Count})");
                        i++;
                    }
                }

                wheelColors.AddRange(classic);
            }

            var wheelColorsList = new MenuListItem("Wheel Color", wheelColors, 0);
            var dashColorList = new MenuListItem("Dashboard Color", classic, 0);
            var intColorList = new MenuListItem("Interior / Trim Color", classic, 0);
            var vehicleEnveffScale = new MenuSliderItem("Vehicle Enveff Scale", "This only works on certain vehicles. It fades some paint layers.", 0, 20, 10, true);

            VehicleColorsMenu.AddMenuItem(vehicleEnveffScale);

            VehicleColorsMenu.OnSliderPositionChange += (m, sliderItem, oldPosition, newPosition, itemIndex) =>
            {
                var veh = GetVehicle();
                if (veh != null && veh.Driver == Game.PlayerPed && !veh.IsDead)
                {
                    if (sliderItem == vehicleEnveffScale)
                    {
                        SetVehicleEnveffScale(veh.Handle, newPosition / 20f);
                    }
                }
                else
                {
                    Notify.Error("You need to be the driver of a driveable vehicle to change this slider.");
                }
            };
            VehicleColorsMenu.AddMenuItem(dashColorList);
            VehicleColorsMenu.AddMenuItem(intColorList);
            VehicleColorsMenu.AddMenuItem(wheelColorsList);

            // Underglow menu
            var UnderglowColorsMenu = Lm.GetMenu(new Menu(MenuTitle, "Underglow Colors"));
            MenuController.AddSubmenu(VehicleColorsMenu, UnderglowColorsMenu);

            var UnderglowColorsBtn = new MenuItem("Underglow Color") { Label = "→→→" };
            VehicleColorsMenu.AddMenuItem(UnderglowColorsBtn);
            MenuController.BindMenuItem(VehicleColorsMenu, UnderglowColorsMenu, UnderglowColorsBtn);

            MenuSliderItem RedSliderUnderglow = new MenuSliderItem($"Red Color ({RedUnderglow})", 0, 255, 0, false)
            {
                BarColor = System.Drawing.Color.FromArgb(155, 0, 0, 0),
                BackgroundColor = System.Drawing.Color.FromArgb(200, 79, 79, 79),
                Description = "Use the slider to pick a red color.",
            };

            MenuSliderItem GreenSliderUnderglow = new MenuSliderItem($"Green Color ({GreenUnderglow})", 0, 255, 0, false)
            {
                BarColor = System.Drawing.Color.FromArgb(155, 0, 0, 0),
                BackgroundColor = System.Drawing.Color.FromArgb(200, 79, 79, 79),
                Description = "Use the slider to pick a green color.",
            };

            MenuSliderItem BlueSliderUnderglow = new MenuSliderItem($"Blue Color ({BlueUnderglow})", 0, 255, 0, false)
            {
                BarColor = System.Drawing.Color.FromArgb(155, 0, 0, 0),
                BackgroundColor = System.Drawing.Color.FromArgb(200, 79, 79, 79),
                Description = "Use the slider to pick a blue color.",
            };



            VehicleColorsMenu.OnListIndexChange += HandleListIndexChanges;

            void HandleListIndexChanges(Menu sender, MenuListItem listItem, int oldIndex, int newIndex, int itemIndex)
            {
                var veh = GetVehicle();
                if (veh != null && veh.Exists() && !veh.IsDead && veh.Driver == Game.PlayerPed)
                {
                    var pearlColor = 0;
                    var wheelColor = 0;

                    GetVehicleExtraColours(veh.Handle, ref pearlColor, ref wheelColor);

                    if (sender == primaryColorsMenu || sender == secondaryColorsMenu)
                    {
                        if (itemIndex < 6) // normal paints
                        {
                            int color = 0;
                            switch (itemIndex)
                            {
                                case 0:
                                    color = VehicleData.ClassicColors[newIndex].id;
                                    break;
                                case 1:
                                    color = VehicleData.MatteColors[newIndex].id;
                                    break;
                                case 2:
                                    color = VehicleData.ClassicColors[newIndex].id;
                                    break;
                                case 3:
                                    color = VehicleData.MetalColors[newIndex].id;
                                    break;
                                case 4:
                                    color = VehicleData.WornColors[newIndex].id;
                                    break;
                                case 5:
                                    color = VehicleData.UtilColors[newIndex].id;
                                    break;
                            }

                            if (sender == primaryColorsMenu)
                            {
                                SetVehicleCustomPrimaryPaintType(veh, 0);
                                ClearVehicleCustomPrimaryColour(veh.Handle);

                                SetVehiclePrimaryColor(veh, color);
                            }
                            else
                            {
                                SetVehicleCustomSecondaryPaintType(veh, 0);
                                ClearVehicleCustomSecondaryColour(veh.Handle);

                                SetVehicleSecondaryColor(veh, color);
                            }
                        }
                        else if (itemIndex == 6) // chameleon
                        {
                            int color = VehicleData.ChameleonColors[newIndex].id;

                            SetVehicleCustomPrimaryPaintType(veh, 0);
                            ClearVehicleCustomPrimaryColour(veh.Handle);
                            SetVehicleCustomSecondaryPaintType(veh, 0);
                            ClearVehicleCustomSecondaryColour(veh.Handle);

                            SetVehicleColors(veh, color, color);
                        }
                        else if (itemIndex == 7) // pearl
                        {
                            pearlColor = VehicleData.ClassicColors[newIndex].id;
                        }
                    }
                    else if (sender == VehicleColorsMenu)
                    {
                        if (listItem == wheelColorsList)
                        {
                            if (newIndex == 0)
                            {
                                wheelColor = 156; // default alloy color.
                            }
                            else
                            {
                                wheelColor = VehicleData.ClassicColors[newIndex - 1].id;
                            }
                        }
                        else if (listItem == dashColorList)
                        {
                            var dashColor = VehicleData.ClassicColors[newIndex].id;
                            // sadly these native names are mixed up :/ but ofc it's impossible to fix due to backwards compatibility.
                            // this should actually be called SetVehicleDashboardColour
                            SetVehicleInteriorColour(veh.Handle, dashColor);
                        }
                        else if (listItem == intColorList)
                        {
                            var intColor = VehicleData.ClassicColors[newIndex].id;
                            // sadly these native names are mixed up :/ but ofc it's impossible to fix due to backwards compatibility.
                            // this should actually be called SetVehicleInteriorColour
                            SetVehicleDashboardColour(veh.Handle, intColor);
                        }
                    }

                    SetVehicleExtraColours(veh.Handle, pearlColor, wheelColor);
                }
                else
                {
                    Notify.Error("You must be the driver of a vehicle in order to change the vehicle's colors.");
                }
            }

            for (int i = 0; i < 2; i++)
            {
                var pearlescentList = new MenuListItem("Pearlescent", classic, 0);
                var classicList = new MenuListItem("Classic", classic, 0);
                var metallicList = new MenuListItem("Metallic", classic, 0);
                var matteList = new MenuListItem("Matte", matte, 0);
                var metalList = new MenuListItem("Metals", metals, 0);
                var utilList = new MenuListItem("Util", util, 0);
                var wornList = new MenuListItem("Worn", worn, 0);

                var colorsMenu = i == 0 ? primaryColorsMenu : secondaryColorsMenu;

                colorsMenu.AddMenuItem(classicList);
                colorsMenu.AddMenuItem(matteList);
                colorsMenu.AddMenuItem(metallicList);
                colorsMenu.AddMenuItem(metalList);
                colorsMenu.AddMenuItem(wornList);
                colorsMenu.AddMenuItem(utilList);
                if (i == 0)
                {
                    if (GetSettingsBool(Setting.vmenu_using_chameleon_colours))
                    {
                        var chameleonList = new MenuListItem("Chameleon", chameleon, 0);
                        colorsMenu.AddMenuItem(chameleonList);
                    }
                    colorsMenu.AddMenuItem(pearlescentList);
                }

                colorsMenu.OnListIndexChange += HandleListIndexChanges;
            }

            var primaryColorsMenuRGB = Lm.GetMenu(new Menu(MenuTitle, "Custom"));
            MenuController.AddSubmenu(primaryColorsMenu, primaryColorsMenuRGB);

            var primaryColorsRGBBtn = new MenuItem("Custom") { Label = "→→→" };
            primaryColorsMenu.AddMenuItem(primaryColorsRGBBtn);
            MenuController.BindMenuItem(primaryColorsMenu, primaryColorsMenuRGB, primaryColorsRGBBtn);
            MenuSliderItem RedSliderPrimary = new MenuSliderItem($"Red Color ({RedPrimary})", 0, 255, 0, false)
            {
                BarColor = System.Drawing.Color.FromArgb(155, 0, 0, 0),
                BackgroundColor = System.Drawing.Color.FromArgb(200, 79, 79, 79),
                Description = "Use the slider to pick a red color.",

            };

            MenuSliderItem GreenSliderPrimary = new MenuSliderItem($"Green Color ({GreenPrimary})", 0, 255, 0, false)
            {
                BarColor = System.Drawing.Color.FromArgb(155, 0, 0, 0),
                BackgroundColor = System.Drawing.Color.FromArgb(200, 79, 79, 79),
                Description = "Use the slider to pick a green color.",

            };

            MenuSliderItem BlueSliderPrimary = new MenuSliderItem($"Blue Color ({BluePrimary})", 0, 255, 0, false)
            {
                BarColor = System.Drawing.Color.FromArgb(155, 0, 0, 0),
                BackgroundColor = System.Drawing.Color.FromArgb(200, 79, 79, 79),
                Description = "Use the slider to pick a blue color.",

            };
            var pearlescentListPrimary = new MenuListItem("Pearlescent", classic, 0);
            var HexColorPrimary = new MenuItem("Primary Hex", "Set primary color with hex code.");
            MenuListItem FinishListPrimary = new MenuListItem($"Paint Finish", ColorFinishes, 0, "Select a finish for your primary paint.");
            var SecondaryMatchColorPrimary = new MenuItem("Copy Secondary Paint", "Copy the secondary paint.");
            primaryColorsMenuRGB.AddMenuItem(RedSliderPrimary);
            primaryColorsMenuRGB.AddMenuItem(GreenSliderPrimary);
            primaryColorsMenuRGB.AddMenuItem(BlueSliderPrimary);
            primaryColorsMenuRGB.AddMenuItem(HexColorPrimary);
            primaryColorsMenuRGB.AddMenuItem(FinishListPrimary);
            primaryColorsMenuRGB.AddMenuItem(pearlescentListPrimary);
            primaryColorsMenuRGB.AddMenuItem(SecondaryMatchColorPrimary);

            void CopyPrimaryToRgb()
            {
                var veh = GetVehicle();

                if (GetIsVehiclePrimaryColourCustom(veh.Handle))
                    return;

                var primaryColorred = 0;
                var primaryColorgreen = 0;
                var primaryColorblue = 0;

                GetVehicleCustomPrimaryColour(veh.Handle, ref primaryColorred, ref primaryColorgreen, ref primaryColorblue);

                RedPrimary = primaryColorred;
                GreenPrimary = primaryColorgreen;
                BluePrimary = primaryColorblue;
                FinishPrimary = 0;
            }

            void UpdatePrimaryChange(bool changePaint = true)
            {
                RedSliderPrimary.Position = RedPrimary;
                GreenSliderPrimary.Position = GreenPrimary;
                BlueSliderPrimary.Position = BluePrimary;
                RedSliderPrimary.BarColor = System.Drawing.Color.FromArgb(255, RedPrimary, GreenPrimary, BluePrimary);
                GreenSliderPrimary.BarColor = System.Drawing.Color.FromArgb(255, RedPrimary, GreenPrimary, BluePrimary);
                BlueSliderPrimary.BarColor = System.Drawing.Color.FromArgb(255, RedPrimary, GreenPrimary, BluePrimary);
                RedSliderPrimary.Text = $"Red Color ({RedPrimary})";
                GreenSliderPrimary.Text = $"Green Color ({GreenPrimary})";
                BlueSliderPrimary.Text = $"Blue Color ({BluePrimary})";
                FinishListPrimary.ListIndex = FinishPrimary;

                string hexValue = RedPrimary.ToString("X2") + GreenPrimary.ToString("X2") + BluePrimary.ToString("X2");
                HexColorPrimary.Label = $"#{hexValue}";

                if (changePaint)
                {
                    var veh = GetVehicle();
                    SetVehicleCustomPrimaryColour(veh.Handle, RedPrimary, GreenPrimary, BluePrimary);
                    SetVehicleCustomPrimaryPaintType(veh, FinishPrimary);
                }
            }

            primaryColorsMenu.OnItemSelect += (sender, item, index) =>
            {
                CopyPrimaryToRgb();
                UpdatePrimaryChange(false);
            };

            primaryColorsMenuRGB.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
            {
                if (item == pearlescentListPrimary)
                {
                    var veh = GetVehicle();
                    var pearlColorReset = 0;
                    var wheelColorReset = 0;
                    GetVehicleExtraColours(veh.Handle, ref pearlColorReset, ref wheelColorReset);
                    SetVehicleExtraColours(veh.Handle, VehicleData.ClassicColors[newIndex].id, wheelColorReset);
                }
            };

            primaryColorsMenuRGB.OnItemSelect += async (sender, item, index) =>
            {
                if (item == HexColorPrimary)
                {
                    var result = await GetUserInput(windowTitle: "Enter Color Hex", defaultText: (HexColorPrimary.Label).Replace("#", ""), maxInputLength: 6);
                    if (!string.IsNullOrEmpty(result))
                    {
                        if (IsHex(result))
                        {
                            int RGBint = Convert.ToInt32(result, 16);
                            RedPrimary = (byte)((RGBint >> 16) & 255);
                            GreenPrimary = (byte)((RGBint >> 8) & 255);
                            BluePrimary = (byte)(RGBint & 255);

                            UpdatePrimaryChange();
                        }
                        else
                            Notify.Error($"#{result} is not a valid hex code!");
                    }
                }
                if (item == SecondaryMatchColorPrimary)
                {
                    var veh = GetVehicle();
                    CopySecondaryToRgb();

                    if (!GetIsVehiclePrimaryColourCustom(veh.Handle))
                    {
                        SetVehicleCustomPrimaryPaintType(veh, 0);
                        ClearVehicleCustomPrimaryColour(veh.Handle);
                        SetVehiclePrimaryColor(veh, GetVehicleSecondaryColor(veh));
                        CopyPrimaryToRgb();
                        UpdatePrimaryChange(false);
                    }
                    else
                    {
                        RedPrimary = RedSecondary;
                        GreenPrimary = GreenSecondary;
                        BluePrimary = BlueSecondary;
                        FinishPrimary = FinishSecondary;

                        UpdatePrimaryChange();
                    }
                }
            };

            primaryColorsMenuRGB.OnSliderPositionChange += (m, sliderItem, oldPosition, newPosition, itemIndex) =>
            {
                if (sliderItem == RedSliderPrimary)
                {
                    RedPrimary = newPosition;
                    UpdatePrimaryChange();
                }
                if (sliderItem == GreenSliderPrimary)
                {
                    GreenPrimary = newPosition;
                    UpdatePrimaryChange();
                }
                if (sliderItem == BlueSliderPrimary)
                {
                    BluePrimary = newPosition;
                    UpdatePrimaryChange();
                }

            };
            primaryColorsMenuRGB.OnListIndexChange += (m, listItem, oldLi, newLi, itemIx) =>
            {
                if (listItem == FinishListPrimary)
                {
                    FinishPrimary = newLi;
                    UpdatePrimaryChange();
                }
            };

            primaryColorsMenuRGB.OnMenuOpen += (m) =>
            {
                CopyPrimaryToRgb();
                UpdatePrimaryChange(false);
            };


            var secondaryColorsMenuRGB = Lm.GetMenu(new Menu(MenuTitle, "Custom"));
            MenuController.AddSubmenu(secondaryColorsMenu, secondaryColorsMenuRGB);

            var SecondaryColorsRGBBtn = new MenuItem("Custom") { Label = "→→→" };
            secondaryColorsMenu.AddMenuItem(SecondaryColorsRGBBtn);
            MenuController.BindMenuItem(secondaryColorsMenu, secondaryColorsMenuRGB, SecondaryColorsRGBBtn);
            MenuSliderItem RedSliderSecondary = new MenuSliderItem($"Red Color ({RedSecondary})", 0, 255, 0, false)
            {
                BarColor = System.Drawing.Color.FromArgb(155, 0, 0, 0),
                BackgroundColor = System.Drawing.Color.FromArgb(200, 79, 79, 79),
                Description = "Use the slider to pick a red color.",

            };

            MenuSliderItem GreenSliderSecondary = new MenuSliderItem($"Green Color ({GreenSecondary})", 0, 255, 0, false)
            {
                BarColor = System.Drawing.Color.FromArgb(155, 0, 0, 0),
                BackgroundColor = System.Drawing.Color.FromArgb(200, 79, 79, 79),
                Description = "Use the slider to pick a green color.",

            };

            MenuSliderItem BlueSliderSecondary = new MenuSliderItem($"Blue Color ({BlueSecondary})", 0, 255, 0, false)
            {
                BarColor = System.Drawing.Color.FromArgb(155, 0, 0, 0),
                BackgroundColor = System.Drawing.Color.FromArgb(200, 79, 79, 79),
                Description = "Use the slider to pick a blue color.",

            };
            var HexColorSecondary = new MenuItem("Secondary Hex", "Set secondary color with hex code.");
            MenuListItem FinishListSecondary = new MenuListItem($"Paint Finish", ColorFinishes, 0, "Select a finish for your secondary paint.");
            var PrimaryMatchColorSecondary = new MenuItem("Copy Primary Paint", "Copy the primary paint.");


            void CopySecondaryToRgb()
            {
                var veh = GetVehicle();

                if (GetIsVehicleSecondaryColourCustom(veh.Handle))
                    return;

                var secondaryColorred = 0;
                var secondaryColorgreen = 0;
                var secondaryColorblue = 0;

                GetVehicleCustomSecondaryColour(veh.Handle, ref secondaryColorred, ref secondaryColorgreen, ref secondaryColorblue);

                RedSecondary = secondaryColorred;
                GreenSecondary = secondaryColorgreen;
                BlueSecondary = secondaryColorblue;
                FinishSecondary = 0;
            }

            void UpdateSecondaryChange(bool changePaint = true)
            {
                RedSliderSecondary.Position = RedSecondary;
                GreenSliderSecondary.Position = GreenSecondary;
                BlueSliderSecondary.Position = BlueSecondary;
                RedSliderSecondary.BarColor = System.Drawing.Color.FromArgb(255, RedSecondary, GreenSecondary, BlueSecondary);
                GreenSliderSecondary.BarColor = System.Drawing.Color.FromArgb(255, RedSecondary, GreenSecondary, BlueSecondary);
                BlueSliderSecondary.BarColor = System.Drawing.Color.FromArgb(255, RedSecondary, GreenSecondary, BlueSecondary);
                RedSliderSecondary.Text = $"Red Color ({RedSecondary})";
                GreenSliderSecondary.Text = $"Green Color ({GreenSecondary})";
                BlueSliderSecondary.Text = $"Blue Color ({BlueSecondary})";
                FinishListSecondary.ListIndex = FinishSecondary;

                if (changePaint)
                {
                    var veh = GetVehicle();

                    SetVehicleCustomSecondaryColour(veh.Handle, RedSecondary, GreenSecondary, BlueSecondary);
                    SetVehicleCustomSecondaryPaintType(veh, FinishSecondary);
                    string hexValue = RedSecondary.ToString("X2") + GreenSecondary.ToString("X2") + BlueSecondary.ToString("X2");
                    HexColorSecondary.Label = $"#{hexValue}";
                }
            }

            secondaryColorsMenu.OnItemSelect += (sender, item, index) =>
            {
                CopySecondaryToRgb();
                UpdateSecondaryChange(false);
            };
            secondaryColorsMenuRGB.OnItemSelect += async (sender, item, index) =>
            {
                if (item == HexColorSecondary)
                {
                    var result = await GetUserInput(windowTitle: "Enter Color Hex", defaultText: (HexColorSecondary.Label).Replace("#", ""), maxInputLength: 6);
                    if (!string.IsNullOrEmpty(result))
                    {
                        if (IsHex(result))
                        {
                            int RGBint = Convert.ToInt32(result, 16);
                            RedSecondary = (byte)((RGBint >> 16) & 255);
                            GreenSecondary = (byte)((RGBint >> 8) & 255);
                            BlueSecondary = (byte)(RGBint & 255);

                            UpdateSecondaryChange();
                        }
                        else
                            Notify.Error($"{result} is not a valid hex code!");

                    }

                }
                if (item == PrimaryMatchColorSecondary)
                {
                    var veh = GetVehicle();
                    CopyPrimaryToRgb();

                    if (!GetIsVehiclePrimaryColourCustom(veh.Handle))
                    {
                        SetVehicleCustomSecondaryPaintType(veh, 0);
                        ClearVehicleCustomSecondaryColour(veh.Handle);
                        SetVehicleSecondaryColor(veh, GetVehiclePrimaryColor(veh));
                        CopySecondaryToRgb();
                        UpdateSecondaryChange(false);
                    }
                    else
                    {
                        RedSecondary = RedPrimary;
                        GreenSecondary = GreenPrimary;
                        BlueSecondary = BluePrimary;
                        FinishSecondary = FinishPrimary;

                        UpdateSecondaryChange();
                    }
                }
            };

            secondaryColorsMenuRGB.AddMenuItem(RedSliderSecondary);
            secondaryColorsMenuRGB.AddMenuItem(GreenSliderSecondary);
            secondaryColorsMenuRGB.AddMenuItem(BlueSliderSecondary);
            secondaryColorsMenuRGB.AddMenuItem(HexColorSecondary);
            secondaryColorsMenuRGB.AddMenuItem(FinishListSecondary);

            secondaryColorsMenuRGB.AddMenuItem(PrimaryMatchColorSecondary);

            secondaryColorsMenuRGB.OnSliderPositionChange += (m, sliderItem, oldPosition, newPosition, itemIndex) =>
            {
                if (sliderItem == RedSliderSecondary)
                {
                    RedSecondary = newPosition;
                    UpdateSecondaryChange();
                }
                if (sliderItem == GreenSliderSecondary)
                {
                    GreenSecondary = newPosition;
                    UpdateSecondaryChange();
                }
                if (sliderItem == BlueSliderSecondary)
                {
                    BlueSecondary = newPosition;
                    UpdateSecondaryChange();
                }
            };
            secondaryColorsMenuRGB.OnListIndexChange += (m, listItem, oldLi, newLi, itemIx) =>
            {
                if (listItem == FinishListSecondary)
                {
                    FinishSecondary = newLi;
                    UpdateSecondaryChange();
                }
            };
            secondaryColorsMenuRGB.OnMenuOpen += (m) =>
            {
                CopySecondaryToRgb();
                UpdateSecondaryChange(false);
            };
            #endregion


            #region Vehicle Liveries Submenu Stuff
            menu.OnItemSelect += (sender, item, idex) =>
            {
                // If the liverys menu button is selected.
                if (item == liveriesMenuBtn)
                {
                    // Get the player's vehicle.
                    var veh = GetVehicle();
                    // If it exists, isn't dead and the player is in the drivers seat continue.
                    if (veh != null && veh.Exists() && !veh.IsDead)
                    {
                        if (veh.Driver == Game.PlayerPed)
                        {
                            VehicleLiveriesMenu.ClearMenuItems();
                            SetVehicleModKit(veh.Handle, 0);
                            var liveryCount = GetVehicleLiveryCount(veh.Handle);

                            if (liveryCount > 0)
                            {
                                var liveryList = new List<string>();
                                for (var i = 0; i < liveryCount; i++)
                                {
                                    var livery = GetLiveryName(veh.Handle, i);
                                    livery = GetLabelText(livery) != "NULL" ? GetLabelText(livery) : $"Livery #{i}";
                                    liveryList.Add(livery);
                                }
                                var liveryListItem = new MenuListItem("Set Livery", liveryList, GetVehicleLivery(veh.Handle), "Choose a livery for this vehicle.");
                                VehicleLiveriesMenu.AddMenuItem(liveryListItem);
                                VehicleLiveriesMenu.OnListIndexChange += (_menu, listItem, oldIndex, newIndex, itemIndex) =>
                                {
                                    if (listItem == liveryListItem)
                                    {
                                        veh = GetVehicle();
                                        SetVehicleLivery(veh.Handle, newIndex);
                                    }
                                };
                                VehicleLiveriesMenu.RefreshIndex();
                                //VehicleLiveriesMenu.UpdateScaleform();
                            }
                            else
                            {
                                VehicleLiveriesMenu.CloseMenu();
                                menu.OpenMenu();
                                var backBtn = new MenuItem("No Liveries Available.");
                                VehicleLiveriesMenu.AddMenuItem(backBtn);

                                VehicleLiveriesMenu.RefreshIndex();
                                //VehicleLiveriesMenu.UpdateScaleform();
                            }
                        }
                        else
                        {
                            Notify.Error("You have to be the driver of a vehicle to access this menu.");
                        }
                    }
                    else
                    {
                        Notify.Error("You have to be the driver of a vehicle to access this menu.");
                    }
                }
            };
            #endregion

            #region Vehicle Mod Submenu Stuff
            menu.OnItemSelect += (sender, item, index) =>
            {
                // When the mod submenu is openend, reset all items in there.
                if (item == modMenuBtn)
                {
                    if (Game.PlayerPed.IsInVehicle())
                    {
                        UpdateMods();
                    }
                    else
                    {
                        VehicleModMenu.CloseMenu();
                        menu.OpenMenu();
                    }

                }
            };
            #endregion

            #region Vehicle Components Submenu
            // when the components menu is opened.
            menu.OnItemSelect += (sender, item, index) =>
            {
                // If the components menu is opened.
                if (item == componentsMenuBtn)
                {
                    // Empty the menu in case there were leftover buttons from another vehicle.
                    if (VehicleComponentsMenu.Size > 0)
                    {
                        VehicleComponentsMenu.ClearMenuItems();
                        vehicleExtras.Clear();
                        VehicleComponentsMenu.RefreshIndex();
                        //VehicleComponentsMenu.UpdateScaleform();
                    }

                    // Get the vehicle.
                    var veh = GetVehicle();

                    // Check if the vehicle exists, it's actually a vehicle, it's not dead/broken and the player is in the drivers seat.
                    if (veh != null && veh.Exists() && !veh.IsDead && veh.Driver == Game.PlayerPed)
                    {
                        Dictionary<int, string> extraLabels;
                        if (!VehicleExtras.TryGetValue((uint)veh.Model.Hash, out extraLabels))
                        {
                            extraLabels = new Dictionary<int, string>();
                        }
                        //List<int> extraIds = new List<int>();
                        // Loop through all possible extra ID's (AFAIK: 0-14).
                        for (var extra = 0; extra < 14; extra++)
                        {
                            // If this extra exists...
                            if (veh.ExtraExists(extra))
                            {
                                // Add it's ID to the list.
                                //extraIds.Add(extra);

                                // Create a checkbox for it.
                                string extraLabel;
                                if (!extraLabels.TryGetValue(extra, out extraLabel))
                                    extraLabel = $"Extra #{extra}";
                                var extraCheckbox = new MenuCheckboxItem(extraLabel, extra.ToString(), veh.IsExtraOn(extra));
                                // Add the checkbox to the menu.
                                VehicleComponentsMenu.AddMenuItem(extraCheckbox);

                                // Add it's ID to the dictionary.
                                vehicleExtras[extraCheckbox] = extra;
                            }
                        }



                        if (vehicleExtras.Count > 0)
                        {
                            var backBtn = new MenuItem("Go Back", "Go back to the Vehicle Options menu.");
                            VehicleComponentsMenu.AddMenuItem(backBtn);
                            VehicleComponentsMenu.OnItemSelect += (sender3, item3, index3) =>
                            {
                                VehicleComponentsMenu.GoBack();
                            };
                        }
                        else
                        {
                            var noExtrasBtn = new MenuItem("No Extras Available.");
                            VehicleComponentsMenu.AddMenuItem(noExtrasBtn);

                        }
                        // And update the submenu to prevent weird glitches.
                        VehicleComponentsMenu.RefreshIndex();
                        //VehicleComponentsMenu.UpdateScaleform();

                    }
                }
            };
            // when a checkbox in the components menu changes
            VehicleComponentsMenu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                // When a checkbox is checked/unchecked, get the selected checkbox item index and use that to get the component ID from the list.
                // Then toggle that extra.
                if (vehicleExtras.TryGetValue(item, out int extra))
                {
                    var veh = GetVehicle();
                    veh.ToggleExtra(extra, _checked);
                }
            };
            #endregion

            #region Underglow Submenu
            var underglowFront = new MenuCheckboxItem("Enable Front Light", "Enable or disable the underglow on the front side of the vehicle. Note not all vehicles have lights.", false);
            var underglowBack = new MenuCheckboxItem("Enable Rear Light", "Enable or disable the underglow on the left side of the vehicle. Note not all vehicles have lights.", false);
            var underglowLeft = new MenuCheckboxItem("Enable Left Light", "Enable or disable the underglow on the right side of the vehicle. Note not all vehicles have lights.", false);
            var underglowRight = new MenuCheckboxItem("Enable Right Light", "Enable or disable the underglow on the back side of the vehicle. Note not all vehicles have lights.", false);
            var underglowColorsList = new List<string>();
            for (int i = 0; i < 13; i++)
            {
                underglowColorsList.Add(GetLabelText($"CMOD_NEONCOL_{i}"));
            }
            var underglowColor = new MenuListItem("Underglow preset", underglowColorsList, 0, "Preset underglow colors.");
            var HexColorUnderglow = new MenuItem("Underglow Hex", "Set Underglow color with hex code.");
            var syncprimaryUnderglow = new MenuItem("Match Primary Paint", "Use the primary paint's color for the underglow.");
            var syncsecondaryUnderglow = new MenuItem("Match Secondary Paint", "Use the primary paint's color for the underglow.");
            UnderglowColorsMenu.AddMenuItem(underglowFront);
            UnderglowColorsMenu.AddMenuItem(underglowBack);
            UnderglowColorsMenu.AddMenuItem(underglowLeft);
            UnderglowColorsMenu.AddMenuItem(underglowRight);

            UnderglowColorsMenu.AddMenuItem(underglowColor);

            UnderglowColorsMenu.AddMenuItem(RedSliderUnderglow);
            UnderglowColorsMenu.AddMenuItem(GreenSliderUnderglow);
            UnderglowColorsMenu.AddMenuItem(BlueSliderUnderglow);
            UnderglowColorsMenu.AddMenuItem(HexColorUnderglow);
            UnderglowColorsMenu.AddMenuItem(syncprimaryUnderglow);
            UnderglowColorsMenu.AddMenuItem(syncsecondaryUnderglow);
            UnderglowColorsMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == HexColorUnderglow)
                {
                    var result = await GetUserInput(windowTitle: "Enter Color Hex", defaultText: (HexColorUnderglow.Label).Replace("#", ""), maxInputLength: 6);
                    if (!string.IsNullOrEmpty(result))
                    {
                        if (IsHex(result))
                        {
                            int RGBint = Convert.ToInt32(result, 16);
                            byte Red = (byte)((RGBint >> 16) & 255);
                            byte Green = (byte)((RGBint >> 8) & 255);
                            byte Blue = (byte)(RGBint & 255);


                            var veh = GetVehicle();
                            RedSliderUnderglow.Position = Red;
                            GreenSliderUnderglow.Position = Green;
                            BlueSliderUnderglow.Position = Blue;
                            RedSliderUnderglow.Text = $"Red Color ({Red})";
                            GreenSliderUnderglow.Text = $"Green Color ({Green})";
                            BlueSliderUnderglow.Text = $"Blue Color ({Blue})";
                            RedSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, Red, Green, Blue);
                            GreenSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, Red, Green, Blue);
                            BlueSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, Red, Green, Blue);
                            string hexValue = Red.ToString("X2") + Green.ToString("X2") + Blue.ToString("X2");
                            HexColorUnderglow.Label = $"#{hexValue}";
                            SetVehicleNeonLightsColour(veh.Handle, RedSliderUnderglow.Position, GreenSliderUnderglow.Position, BlueSliderUnderglow.Position);

                        }
                        else
                            Notify.Error($"#{result} is not a valid hex code!");
                    }
                }
                if (item == syncprimaryUnderglow)
                {
                    var Red = 0;
                    var Green = 0;
                    var Blue = 0;
                    var veh = GetVehicle();
                    GetVehicleCustomPrimaryColour(veh.Handle, ref Red, ref Green, ref Blue);
                    RedSliderUnderglow.Position = Red;
                    GreenSliderUnderglow.Position = Green;
                    BlueSliderUnderglow.Position = Blue;
                    RedSliderUnderglow.Text = $"Red Color ({Red})";
                    GreenSliderUnderglow.Text = $"Green Color ({Green})";
                    BlueSliderUnderglow.Text = $"Blue Color ({Blue})";
                    RedSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, Red, Green, Blue);
                    GreenSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, Red, Green, Blue);
                    BlueSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, Red, Green, Blue);
                    string hexValue = Red.ToString("X2") + Green.ToString("X2") + Blue.ToString("X2");
                    HexColorUnderglow.Label = $"#{hexValue}";
                    SetVehicleNeonLightsColour(veh.Handle, RedSliderUnderglow.Position, GreenSliderUnderglow.Position, BlueSliderUnderglow.Position);
                }
                if (item == syncsecondaryUnderglow)
                {
                    var Red = 0;
                    var Green = 0;
                    var Blue = 0;
                    var veh = GetVehicle();
                    GetVehicleCustomSecondaryColour(veh.Handle, ref Red, ref Green, ref Blue);
                    RedSliderUnderglow.Position = Red;
                    GreenSliderUnderglow.Position = Green;
                    BlueSliderUnderglow.Position = Blue;
                    RedSliderUnderglow.Text = $"Red Color ({Red})";
                    GreenSliderUnderglow.Text = $"Green Color ({Green})";
                    BlueSliderUnderglow.Text = $"Blue Color ({Blue})";
                    RedSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, Red, Green, Blue);
                    GreenSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, Red, Green, Blue);
                    BlueSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, Red, Green, Blue);
                    string hexValue = Red.ToString("X2") + Green.ToString("X2") + Blue.ToString("X2");
                    HexColorUnderglow.Label = $"#{hexValue}";
                    SetVehicleNeonLightsColour(veh.Handle, RedSliderUnderglow.Position, GreenSliderUnderglow.Position, BlueSliderUnderglow.Position);
                }
            };

            VehicleColorsMenu.OnItemSelect += (sender, item, index) =>
            {
                #region reset checkboxes state when opening the menu.
                var veh = GetVehicle();
                var redneon = 0;
                var greenneon = 0;
                var blueneon = 0;
                GetVehicleNeonLightsColour(veh.Handle, ref redneon, ref greenneon, ref blueneon);
                RedSliderUnderglow.Position = redneon;
                GreenSliderUnderglow.Position = greenneon;
                BlueSliderUnderglow.Position = blueneon;
                RedSliderUnderglow.Text = $"Red Color ({redneon})";
                GreenSliderUnderglow.Text = $"Green Color ({greenneon})";
                BlueSliderUnderglow.Text = $"Blue Color ({blueneon})";
                RedSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, redneon, greenneon, blueneon);
                GreenSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, redneon, greenneon, blueneon);
                BlueSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, redneon, greenneon, blueneon);
                string hexValue = redneon.ToString("X2") + greenneon.ToString("X2") + blueneon.ToString("X2");
                HexColorUnderglow.Label = $"#{hexValue}";
                if (veh != null)
                {
                    if (veh.Mods.HasNeonLights)
                    {
                        underglowFront.Checked = veh.Mods.HasNeonLight(VehicleNeonLight.Front) && veh.Mods.IsNeonLightsOn(VehicleNeonLight.Front);
                        underglowBack.Checked = veh.Mods.HasNeonLight(VehicleNeonLight.Back) && veh.Mods.IsNeonLightsOn(VehicleNeonLight.Back);
                        underglowLeft.Checked = veh.Mods.HasNeonLight(VehicleNeonLight.Left) && veh.Mods.IsNeonLightsOn(VehicleNeonLight.Left);
                        underglowRight.Checked = veh.Mods.HasNeonLight(VehicleNeonLight.Right) && veh.Mods.IsNeonLightsOn(VehicleNeonLight.Right);

                        underglowFront.Enabled = true;
                        underglowBack.Enabled = true;
                        underglowLeft.Enabled = true;
                        underglowRight.Enabled = true;

                        underglowFront.LeftIcon = MenuItem.Icon.NONE;
                        underglowBack.LeftIcon = MenuItem.Icon.NONE;
                        underglowLeft.LeftIcon = MenuItem.Icon.NONE;
                        underglowRight.LeftIcon = MenuItem.Icon.NONE;
                    }
                    else
                    {
                        underglowFront.Checked = false;
                        underglowBack.Checked = false;
                        underglowLeft.Checked = false;
                        underglowRight.Checked = false;

                        underglowFront.Enabled = false;
                        underglowBack.Enabled = false;
                        underglowLeft.Enabled = false;
                        underglowRight.Enabled = false;

                        underglowFront.LeftIcon = MenuItem.Icon.LOCK;
                        underglowBack.LeftIcon = MenuItem.Icon.LOCK;
                        underglowLeft.LeftIcon = MenuItem.Icon.LOCK;
                        underglowRight.LeftIcon = MenuItem.Icon.LOCK;
                    }
                }
                else
                {
                    underglowFront.Checked = false;
                    underglowBack.Checked = false;
                    underglowLeft.Checked = false;
                    underglowRight.Checked = false;

                    underglowFront.Enabled = false;
                    underglowBack.Enabled = false;
                    underglowLeft.Enabled = false;
                    underglowRight.Enabled = false;

                    underglowFront.LeftIcon = MenuItem.Icon.LOCK;
                    underglowBack.LeftIcon = MenuItem.Icon.LOCK;
                    underglowLeft.LeftIcon = MenuItem.Icon.LOCK;
                    underglowRight.LeftIcon = MenuItem.Icon.LOCK;
                }

                underglowColor.ListIndex = GetIndexFromColor();

                #endregion
            };
            // handle item selections
            UnderglowColorsMenu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (Game.PlayerPed.IsInVehicle())
                {
                    var veh = GetVehicle();
                    if (veh.Mods.HasNeonLights)
                    {
                        veh.Mods.NeonLightsColor = GetColorFromIndex(underglowColor.ListIndex);
                        if (item == underglowLeft)
                        {
                            veh.Mods.SetNeonLightsOn(VehicleNeonLight.Left, veh.Mods.HasNeonLight(VehicleNeonLight.Left) && _checked);
                        }
                        else if (item == underglowRight)
                        {
                            veh.Mods.SetNeonLightsOn(VehicleNeonLight.Right, veh.Mods.HasNeonLight(VehicleNeonLight.Right) && _checked);
                        }
                        else if (item == underglowBack)
                        {
                            veh.Mods.SetNeonLightsOn(VehicleNeonLight.Back, veh.Mods.HasNeonLight(VehicleNeonLight.Back) && _checked);
                        }
                        else if (item == underglowFront)
                        {
                            veh.Mods.SetNeonLightsOn(VehicleNeonLight.Front, veh.Mods.HasNeonLight(VehicleNeonLight.Front) && _checked);
                        }
                    }
                }
            };

            UnderglowColorsMenu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
            {
                if (item == underglowColor)
                {
                    if (Game.PlayerPed.IsInVehicle())
                    {
                        Vehicle veh = GetVehicle();
                        if (veh.Mods.HasNeonLights)
                        {
                            veh.Mods.NeonLightsColor = GetColorFromIndex(newIndex);
                            var redneon = 0;
                            var greenneon = 0;
                            var blueneon = 0;
                            GetVehicleNeonLightsColour(veh.Handle, ref redneon, ref greenneon, ref blueneon);

                            RedSliderUnderglow.Position = redneon;
                            GreenSliderUnderglow.Position = greenneon;
                            BlueSliderUnderglow.Position = blueneon;
                            RedSliderUnderglow.Text = $"Red Color ({redneon})";
                            GreenSliderUnderglow.Text = $"Green Color ({greenneon})";
                            BlueSliderUnderglow.Text = $"Blue Color ({blueneon})";
                            RedSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, redneon, greenneon, blueneon);
                            GreenSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, redneon, greenneon, blueneon);
                            BlueSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, redneon, greenneon, blueneon);
                            string hexValue = redneon.ToString("X2") + greenneon.ToString("X2") + blueneon.ToString("X2");
                            HexColorUnderglow.Label = $"#{hexValue}";
                        }
                    }
                }
            };
            UnderglowColorsMenu.OnSliderPositionChange += (m, sliderItem, oldPosition, newPosition, itemIndex) =>
            {
                var red = 0;
                var green = 0;
                var blue = 0;
                if (sliderItem == RedSliderUnderglow)
                {
                    //RedSliderUnderglow.Position;
                    RedSliderUnderglow.Text = $"Red Color ({newPosition})";
                    red = newPosition;
                    green = GreenSliderUnderglow.Position;
                    blue = BlueSliderUnderglow.Position;
                    RedSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, newPosition, GreenSliderUnderglow.Position, BlueSliderUnderglow.Position);
                    GreenSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, newPosition, GreenSliderUnderglow.Position, BlueSliderUnderglow.Position);
                    BlueSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, newPosition, GreenSliderUnderglow.Position, BlueSliderUnderglow.Position);
                }
                if (sliderItem == GreenSliderUnderglow)
                {
                    red = RedSliderUnderglow.Position;
                    green = newPosition;
                    blue = BlueSliderUnderglow.Position;
                    //GreenSliderUnderglow.Position = newPosition;
                    GreenSliderUnderglow.Text = $"Green Color ({newPosition})";
                    RedSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, RedSliderUnderglow.Position, newPosition, BlueSliderUnderglow.Position);
                    GreenSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, RedSliderUnderglow.Position, newPosition, BlueSliderUnderglow.Position);
                    BlueSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, RedSliderUnderglow.Position, newPosition, BlueSliderUnderglow.Position);
                }
                if (sliderItem == BlueSliderUnderglow)
                {
                    red = RedSliderUnderglow.Position;
                    green = GreenSliderUnderglow.Position;
                    blue = newPosition;
                    // BlueSliderUnderglow.Position = newPosition;
                    BlueSliderUnderglow.Text = $"Blue Color ({newPosition})";
                    RedSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, RedSliderUnderglow.Position, GreenSliderUnderglow.Position, newPosition);
                    GreenSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, RedSliderUnderglow.Position, GreenSliderUnderglow.Position, newPosition);
                    BlueSliderUnderglow.BarColor = System.Drawing.Color.FromArgb(255, RedSliderUnderglow.Position, GreenSliderUnderglow.Position, newPosition);
                }
                Vehicle veh = GetVehicle();
                string hexValue = red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
                HexColorUnderglow.Label = $"#{hexValue}";
                SetVehicleNeonLightsColour(veh.Handle, red, green, blue);
            };
            #endregion

            #region Handle menu-opening refreshing license plate
            menu.OnMenuOpen += (sender) =>
            {
                menu.GetMenuItems().ForEach((item) =>
                {
                    var veh = GetVehicle(true);

                    if (item == SetLicensePlateType && item != null)
                    {
                        var index = LicensePlateDescriptors
                            .FindIndex(pdscr => pdscr.Style == veh.Mods.LicensePlateStyle);
                        SetLicensePlateType.ListIndex = Math.Max(0, index);
                    }
                });
            };
            #endregion

        }

        /// <summary>
        /// Public get method for the menu. Checks if the menu exists, if not create the menu first.
        /// </summary>
        /// <returns>Returns the Vehicle Options menu.</returns>
        public Menu GetMenu()
        {
            // If menu doesn't exist. Create one.
            if (menu == null)
            {
                CreateMenu();
            }
            // Return the menu.
            return menu;
        }

        #region Update Vehicle Mods Menu
        /// <summary>
        /// Refreshes the mods page. The selectedIndex allows you to go straight to a specific index after refreshing the menu.
        /// This is used because when the wheel type is changed, the menu is refreshed to update the available wheels list.
        /// </summary>
        /// <param name="selectedIndex">Pass this if you want to go straight to a specific mod/index.</param>
        public void UpdateMods(int selectedIndex = 0)
        {
            // If there are items, remove all of them.
            if (VehicleModMenu.Size > 0)
            {
                if (selectedIndex != 0)
                {
                    VehicleModMenu.ClearMenuItems(true);
                }
                else
                {
                    VehicleModMenu.ClearMenuItems(false);
                }

            }

            // Get the vehicle.
            var veh = GetVehicle();

            // Check if the vehicle exists, is still drivable/alive and it's actually a vehicle.
            if (veh != null && veh.Exists() && !veh.IsDead)
            {
                #region initial setup & dynamic vehicle mods setup
                // Set the modkit so we can modify the car.
                SetVehicleModKit(veh.Handle, 0);

                // Get all mods available on this vehicle.
                var mods = veh.Mods.GetAllMods();

                // Loop through all the mods.
                foreach (var mod in mods)
                {
                    veh = GetVehicle();

                    // Get the proper localized mod type (suspension, armor, etc) name.
                    var typeName = mod.LocalizedModTypeName;
                    if (typeName == null)
                        continue;

                    if (typeName == "Front Wheel" && !veh.Model.IsBike)
                    {
                        typeName = "Wheel";
                    }

                    // Create a list to all available upgrades for this modtype.
                    var modlist = new List<string>();

                    // Get the current item index ({current}/{max upgrades})
                    var currentItem = $"[1/{mod.ModCount + 1}]";

                    // Add the stock value for this mod.
                    var name = $"Stock {typeName} {currentItem}";
                    modlist.Add(name);

                    // Loop through all available upgrades for this specific mod type.
                    for (var x = 0; x < mod.ModCount; x++)
                    {
                        // Create the item index.
                        currentItem = $"[{2 + x}/{mod.ModCount + 1}]";

                        // Create the name (again, converting to proper case), then add the name.
                        name = mod.GetLocalizedModName(x) != "" ? $"{ToProperString(mod.GetLocalizedModName(x))} {currentItem}" : $"{typeName} #{x} {currentItem}";

                        if (name == "Engine #4 [6/6]")
                            modlist.Add("EMS Upgrade, Level 5 [6/6]");
                        else
                            modlist.Add(name);
                    }

                    // Create the MenuListItem for this mod type.
                    var currIndex = GetVehicleMod(veh.Handle, (int)mod.ModType) + 1;
                    var modTypeListItem = new MenuListItem(
                        typeName,
                        modlist,
                        currIndex,
                        $"Upgrade or change the ~b~{typeName.ToLower()}~s~ modification. Changes will automatically be applied."
                    )
                    {
                        ItemData = (int)mod.ModType
                    };

                    // Add the list item to the menu.
                    VehicleModMenu.AddMenuItem(modTypeListItem);
                }
                #endregion

                #region more variables and setup
                veh = GetVehicle();
                // Create the wheel types list & listitem and add it to the menu.
                var wheelTypes = new List<string>()
                {
                    "Sports",       // 0
                    "Muscle",       // 1
                    "Lowrider",     // 2
                    "SUV",          // 3
                    "Offroad",      // 4
                    "Tuner",        // 5
                    "Bike Wheels",  // 6
                    "High End",     // 7
                    "Benny's (1)",  // 8
                    "Benny's (2)",  // 9
                    "Open Wheel",   // 10
                    "Street",       // 11
                    "Track"         // 12
                };
                var vehicleWheelType = new MenuListItem("Wheel Type", wheelTypes, MathUtil.Clamp(GetVehicleWheelType(veh.Handle), 0, 12), $"Choose the ~b~wheel type~s~ of your vehicle.");
                if (!veh.Model.IsBoat && !veh.Model.IsHelicopter && !veh.Model.IsPlane && !veh.Model.IsBicycle && !veh.Model.IsTrain)
                {
                    VehicleModMenu.AddMenuItem(vehicleWheelType);
                }

                // Create the checkboxes for some options.
                var toggleCustomWheels = new MenuCheckboxItem("Toggle Custom Wheels", "Toggle between stock and ~b~custom~s~ tire sidewalls.", GetVehicleModVariation(veh.Handle, 23));
                var xenonHeadlights = new MenuCheckboxItem("Xenon Headlights", "Enable or disable ~b~xenon~s~ headlights.", IsToggleModOn(veh.Handle, 22));
                var turbo = new MenuCheckboxItem("Turbo", "Enable or disable the ~b~turbo~s~.", IsToggleModOn(veh.Handle, 18));
                var bulletProofTires = new MenuCheckboxItem("Bulletproof Tires", "Enable or disable ~b~bulletproof tires~s~.", !GetVehicleTyresCanBurst(veh.Handle));
                var driftTires = new MenuCheckboxItem("Low-Grip Tires", "Enable or disable ~b~low-grip tires~s~.", GetDriftTyresEnabled(veh.Handle));

                // Add the checkboxes to the menu.
                VehicleModMenu.AddMenuItem(toggleCustomWheels);
                VehicleModMenu.AddMenuItem(xenonHeadlights);
                var currentHeadlightColor = GetHeadlightsColorForVehicle(veh);
                if (currentHeadlightColor < 0 || currentHeadlightColor > 12)
                {
                    currentHeadlightColor = 13;
                }
                var headlightColor = new MenuListItem("Headlight Color", new List<string>() { "White", "Blue", "Electric Blue", "Mint Green", "Lime Green", "Yellow", "Golden Shower", "Orange", "Red", "Pony Pink", "Hot Pink", "Purple", "Blacklight", "Default Xenon" }, currentHeadlightColor, "Select the ~b~headlight color~s~. ~y~You must enable Xenon Headlights first.~s~");
                VehicleModMenu.AddMenuItem(headlightColor);
                VehicleModMenu.AddMenuItem(turbo);
                VehicleModMenu.AddMenuItem(bulletProofTires);
                VehicleModMenu.AddMenuItem(driftTires);
                // Create a list of tire smoke options.
                var tireSmokes = new List<string>() { "Red", "Orange", "Yellow", "Gold", "Light Green", "Dark Green", "Light Blue", "Dark Blue", "Purple", "Pink", "Black" };
                var tireSmokeColors = new Dictionary<string, int[]>()
                {
                    ["Red"] = new int[] { 244, 65, 65 },
                    ["Orange"] = new int[] { 244, 167, 66 },
                    ["Yellow"] = new int[] { 244, 217, 65 },
                    ["Gold"] = new int[] { 181, 120, 0 },
                    ["Light Green"] = new int[] { 158, 255, 84 },
                    ["Dark Green"] = new int[] { 44, 94, 5 },
                    ["Light Blue"] = new int[] { 65, 211, 244 },
                    ["Dark Blue"] = new int[] { 24, 54, 163 },
                    ["Purple"] = new int[] { 108, 24, 192 },
                    ["Pink"] = new int[] { 192, 24, 172 },
                    ["Black"] = new int[] { 1, 1, 1 }
                };
                int smoker = 0, smokeg = 0, smokeb = 0;
                GetVehicleTyreSmokeColor(veh.Handle, ref smoker, ref smokeg, ref smokeb);
                var item = tireSmokeColors.ToList().Find((f) => { return (f.Value[0] == smoker && f.Value[1] == smokeg && f.Value[2] == smokeb); });
                var index = tireSmokeColors.ToList().IndexOf(item);
                if (index < 0)
                {
                    index = 0;
                }

                var tireSmoke = new MenuListItem("Tire Smoke Color", tireSmokes, index, $"Select the ~b~tire smoke color~s~.");
                VehicleModMenu.AddMenuItem(tireSmoke);

                // Create the checkbox to enable/disable the tiresmoke.
                var tireSmokeEnabled = new MenuCheckboxItem("Tire Smoke", "Enable or disable ~b~tire smoke~s~. ~y~When disabling tire smoke, you will need to drive around before it takes effect.~s~", IsToggleModOn(veh.Handle, 20));
                VehicleModMenu.AddMenuItem(tireSmokeEnabled);

                // Create list for window tint
                var windowTints = new List<string>() { "Stock [1/7]", "None [2/7]", "Limo [3/7]", "Light Smoke [4/7]", "Dark Smoke [5/7]", "Pure Black [6/7]", "Green [7/7]" };
                var currentTint = GetVehicleWindowTint(veh.Handle);
                if (currentTint == -1)
                {
                    currentTint = 4; // stock
                }

                // Convert window tint to the correct index of the list above.
                switch (currentTint)
                {
                    case 0:
                        currentTint = 1; // None
                        break;
                    case 1:
                        currentTint = 5; // Pure Black
                        break;
                    case 2:
                        currentTint = 4; // Dark Smoke
                        break;
                    case 3:
                        currentTint = 3; // Light Smoke
                        break;
                    case 4:
                        currentTint = 0; // Stock
                        break;
                    case 5:
                        currentTint = 2; // Limo
                        break;
                    case 6:
                        currentTint = 6; // Green
                        break;
                    default:
                        break;
                }

                var windowTint = new MenuListItem("Window Tint", windowTints, currentTint, "Change the ~b~window tint~s~.");
                VehicleModMenu.AddMenuItem(windowTint);

                #endregion

                #region Checkbox Changes
                // Handle checkbox changes.
                VehicleModMenu.OnCheckboxChange += (sender2, item2, index2, _checked) =>
                {
                    veh = GetVehicle();

                    // Xenon Headlights
                    if (item2 == xenonHeadlights)
                    {
                        ToggleVehicleMod(veh.Handle, 22, _checked);
                    }
                    // Turbo
                    else if (item2 == turbo)
                    {
                        ToggleVehicleMod(veh.Handle, 18, _checked);
                    }
                    // Bullet Proof Tires
                    else if (item2 == bulletProofTires)
                    {
                        SetVehicleTyresCanBurst(veh.Handle, !_checked);
                    }
                    // Low Grip Tires
                    else if (item2 == driftTires)
                    {
                        SetDriftTyresEnabled(veh.Handle, _checked);
                    }
                    // Custom Wheels
                    else if (item2 == toggleCustomWheels)
                    {
                        SetVehicleMod(veh.Handle, 23, GetVehicleMod(veh.Handle, 23), !GetVehicleModVariation(veh.Handle, 23));

                        // If the player is on a motorcycle, also change the back wheels.
                        if (IsThisModelABike((uint)GetEntityModel(veh.Handle)))
                        {
                            SetVehicleMod(veh.Handle, 24, GetVehicleMod(veh.Handle, 24), GetVehicleModVariation(veh.Handle, 23));
                        }
                    }
                    // Toggle Tire Smoke
                    else if (item2 == tireSmokeEnabled)
                    {
                        // If it should be enabled:
                        if (_checked)
                        {
                            // Enable it.
                            ToggleVehicleMod(veh.Handle, 20, true);
                            // Get the selected color values.
                            var r = tireSmokeColors[tireSmokes[tireSmoke.ListIndex]][0];
                            var g = tireSmokeColors[tireSmokes[tireSmoke.ListIndex]][1];
                            var b = tireSmokeColors[tireSmokes[tireSmoke.ListIndex]][2];
                            // Set the color.
                            SetVehicleTyreSmokeColor(veh.Handle, r, g, b);
                        }
                        // If it should be disabled:
                        else
                        {
                            // Set the smoke to white.
                            SetVehicleTyreSmokeColor(veh.Handle, 255, 255, 255);
                            // Disable it.
                            ToggleVehicleMod(veh.Handle, 20, false);
                            // Remove the mod.
                            RemoveVehicleMod(veh.Handle, 20);
                        }
                    }
                };
                #endregion

                #region List Changes
                // Handle list selections
                VehicleModMenu.OnListIndexChange += (sender2, item2, oldIndex, newIndex, itemIndex) =>
                {
                    // Get the vehicle and set the mod kit.
                    veh = GetVehicle();
                    SetVehicleModKit(veh.Handle, 0);

                    #region handle the dynamic (vehicle-specific) mods
                    // If the affected list is actually a "dynamically" generated list, continue. If it was one of the manual options, go to else.
                    if (item2.ItemData is int modType)
                    {
                        int selectedUpgrade = item2.ListIndex - 1;
                        bool customWheels = GetVehicleModVariation(veh.Handle, 23);

                        SetVehicleMod(veh.Handle, modType, selectedUpgrade, customWheels);
                    }
                    #endregion
                    // If it was not one of the lists above, then it was one of the manual lists/options selected,
                    // either: vehicle Wheel Type, tire smoke color, or window tint:
                    #region Handle the items available on all vehicles.
                    // Wheel types
                    else if (item2 == vehicleWheelType)
                    {
                        var vehicleClass = GetVehicleClass(veh.Handle);
                        var isBikeOrOpenWheel = (newIndex == 6 && veh.Model.IsBike);
                        var isNotBikeNorOpenWheel = newIndex != 6 && !veh.Model.IsBike;
                        var isCorrectVehicleType = isBikeOrOpenWheel || isNotBikeNorOpenWheel;
                        if (!isCorrectVehicleType)
                        {
                            // Go past the index if it's not a bike.
                            if (!veh.Model.IsBike)
                            {
                                if (newIndex > oldIndex)
                                {
                                    item2.ListIndex++;
                                }
                                else
                                {
                                    item2.ListIndex--;
                                }
                            }
                            // Reset the index to 6 if it is a bike
                            else
                            {
                                item2.ListIndex = veh.Model.IsBike ? 6 : 6;
                            }
                        }
                        // Set the wheel type
                        SetVehicleWheelType(veh.Handle, item2.ListIndex);

                        var customWheels = GetVehicleModVariation(veh.Handle, 23);

                        // Reset the wheel mod index for front wheels
                        SetVehicleMod(veh.Handle, 23, -1, customWheels);

                        // If the model is a bike, do the same thing for the rear wheels.
                        if (veh.Model.IsBike)
                        {
                            SetVehicleMod(veh.Handle, 24, -1, customWheels);
                        }

                        // Refresh the menu with the item index so that the view doesn't change
                        UpdateMods(selectedIndex: itemIndex);
                    }
                    // Tire smoke
                    else if (item2 == tireSmoke)
                    {
                        // Get the selected color values.
                        var r = tireSmokeColors[tireSmokes[newIndex]][0];
                        var g = tireSmokeColors[tireSmokes[newIndex]][1];
                        var b = tireSmokeColors[tireSmokes[newIndex]][2];

                        // Set the color.
                        SetVehicleTyreSmokeColor(veh.Handle, r, g, b);
                    }
                    // Window Tint
                    else if (item2 == windowTint)
                    {
                        // Stock = 4,
                        // None = 0,
                        // Limo = 5,
                        // LightSmoke = 3,
                        // DarkSmoke = 2,
                        // PureBlack = 1,
                        // Green = 6,

                        switch (newIndex)
                        {
                            case 1:
                                SetVehicleWindowTint(veh.Handle, 0); // None
                                break;
                            case 2:
                                SetVehicleWindowTint(veh.Handle, 5); // Limo
                                break;
                            case 3:
                                SetVehicleWindowTint(veh.Handle, 3); // Light Smoke
                                break;
                            case 4:
                                SetVehicleWindowTint(veh.Handle, 2); // Dark Smoke
                                break;
                            case 5:
                                SetVehicleWindowTint(veh.Handle, 1); // Pure Black
                                break;
                            case 6:
                                SetVehicleWindowTint(veh.Handle, 6); // Green
                                break;
                            case 0:
                            default:
                                SetVehicleWindowTint(veh.Handle, 4); // Stock
                                break;
                        }
                    }
                    else if (item2 == headlightColor)
                    {
                        if (newIndex == 13) // default
                        {
                            SetHeadlightsColorForVehicle(veh, 255);
                        }
                        else if (newIndex > -1 && newIndex < 13)
                        {
                            SetHeadlightsColorForVehicle(veh, newIndex);
                        }
                    }
                    #endregion
                };

                #endregion
            }
            // Refresh Index and update the scaleform to prevent weird broken menus.
            if (selectedIndex == 0)
            {
                VehicleModMenu.RefreshIndex();
            }

            //VehicleModMenu.UpdateScaleform();

            // Set the selected index to the provided index (0 by default)
            // Used for example, when the wheelstype is changed, the menu is refreshed and we want to set the
            // selected item back to the "wheelsType" list so the user doesn't have to scroll down each time they
            // change the wheels type.
            //VehicleModMenu.CurrentIndex = selectedIndex;
        }

        internal static void SetHeadlightsColorForVehicle(Vehicle veh, int newIndex)
        {

            if (veh != null && veh.Exists() && veh.Driver == Game.PlayerPed)
            {
                if (newIndex is > (-1) and < 13)
                {
                    SetVehicleHeadlightsColour(veh.Handle, newIndex);
                }
                else
                {
                    SetVehicleHeadlightsColour(veh.Handle, -1);
                }
            }
        }
        private bool IsHex(IEnumerable<char> chars)
        {
            bool isHex;
            foreach (var c in chars)
            {
                isHex = ((c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F'));

                if (!isHex)
                    return false;
            }
            return true;
        }
        internal static int GetHeadlightsColorForVehicle(Vehicle vehicle)
        {
            if (vehicle != null && vehicle.Exists())
            {
                if (IsToggleModOn(vehicle.Handle, 22))
                {
                    var val = GetVehicleHeadlightsColour(vehicle.Handle);
                    if (val is > (-1) and < 13)
                    {
                        return val;
                    }
                    return -1;
                }
            }
            return -1;
        }

        #endregion

        #region GetColorFromIndex function (underglow)

        private readonly List<int[]> _VehicleNeonLightColors = new List<int[]>()
        {
            { new int[3] { 255, 255, 255 } },   // White
            { new int[3] { 2, 21, 255 } },      // Blue
            { new int[3] { 3, 83, 255 } },      // Electric blue
            { new int[3] { 0, 255, 140 } },     // Mint Green
            { new int[3] { 94, 255, 1 } },      // Lime Green
            { new int[3] { 255, 255, 0 } },     // Yellow
            { new int[3] { 255, 150, 5 } },     // Golden Shower
            { new int[3] { 255, 62, 0 } },      // Orange
            { new int[3] { 255, 0, 0 } },       // Red
            { new int[3] { 255, 50, 100 } },    // Pony Pink
            { new int[3] { 255, 5, 190 } },     // Hot Pink
            { new int[3] { 35, 1, 255 } },      // Purple
            { new int[3] { 15, 3, 255 } },      // Blacklight
        };

        /// <summary>
        /// Converts a list index to a <see cref="System.Drawing.Color"/> struct.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private System.Drawing.Color GetColorFromIndex(int index)
        {
            if (index >= 0 && index < 13)
            {
                return System.Drawing.Color.FromArgb(_VehicleNeonLightColors[index][0], _VehicleNeonLightColors[index][1], _VehicleNeonLightColors[index][2]);
            }
            return System.Drawing.Color.FromArgb(255, 255, 255);
        }

        /// <summary>
        /// Returns the color index that is applied on the current vehicle.
        /// If a color is active on the vehicle which is not in the list, it'll return the default index 0 (white).
        /// </summary>
        /// <returns></returns>
        private int GetIndexFromColor()
        {
            var veh = GetVehicle();

            if (veh == null || !veh.Exists() || !veh.Mods.HasNeonLights)
            {
                return 0;
            }

            int r = 255, g = 255, b = 255;

            GetVehicleNeonLightsColour(veh.Handle, ref r, ref g, ref b);

            if (r == 255 && g == 0 && b == 255) // default return value when the vehicle has no neon kit selected.
            {
                return 0;
            }

            if (_VehicleNeonLightColors.Any(a => { return a[0] == r && a[1] == g && a[2] == b; }))
            {
                return _VehicleNeonLightColors.FindIndex(a => { return a[0] == r && a[1] == g && a[2] == b; });
            }

            return 0;
        }
        #endregion
    }
}
