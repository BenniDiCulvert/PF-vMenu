using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

using MenuAPI;

using vMenuClient.data;

using static vMenuShared.ConfigManager;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;
using vMenuClient.MenuAPIWrapper;

namespace vMenuClient.menus
{
    public class VehicleSpawner
    {
        public struct FilterItems
        {
            public WMenuItem Name;
            public WMenuItem Manufacturer;
            public WMenuItem CustomClass;
            public WMenuItem DefaultClass;
            public WMenuItem DefaultMods;

            public int Count => 2
                + (CustomClass != null ? 1 : 0)
                + (DefaultClass != null ? 1 : 0)
                + (DefaultMods != null ? 1 : 0);
        }

        // Variables
        private WMenu menu;


        public WMenu AllVehiclesMenu;
        private VehicleData.VehicleFilter filter;
        FilterItems filterItems;

        private bool searchingByName = false;

        private int prevIndex = 0;
        private int prevOffset = 0;

        public void ResetAllVehiclesFilter()
        {
            filter = new VehicleData.VehicleFilter();

            filterItems.Name.Label = "";
            filterItems.Manufacturer.AsListItem().ListIndex = 0;
            if (filterItems.CustomClass != null)
            {
                filterItems.CustomClass.AsListItem().ListIndex = 0;
            }
            if (filterItems.DefaultClass != null)
            {
                filterItems.DefaultClass.AsListItem().ListIndex = 0;
            }
            if (filterItems.DefaultMods != null)
            {
                filterItems.DefaultMods.AsListItem().ListIndex = 0;
            }
        }

        private int FilterAllVehiclesMenu(string name = null)
        {
            AllVehiclesMenu.Menu.ResetFilter();
            var countTotal = AllVehiclesMenu.Menu.Size;

            if (name != null)
            {
                filter.Name = name;
            }

            filterItems.Name.Label = $"~c~{filter.Name}~s~";

            AllVehiclesMenu.Menu.FilterMenuItems(mi =>
                mi.ItemData == null ||
                filter.IsMatching(mi.ItemData as VehicleData.VehicleModelInfo));
            var countFiltered = AllVehiclesMenu.Menu.Size;

            AllVehiclesMenu.ResetIncrement();

            return countFiltered - filterItems.Count;
        }

        public bool SpawnInVehicle { get; private set; } = UserDefaults.VehicleSpawnerSpawnInside;
        public bool SpawnUpgraded { get; private set; } = IsAllowed(Permission.VOMod) ? UserDefaults.VehicleSpawnerSpawnUpgraded : false;
        public bool ReplaceVehicle { get; private set; } = UserDefaults.VehicleSpawnerReplacePrevious;
        public bool SpawnDestructible { get; private set; } = UserDefaults.VehicleSpawnerSpawnDestructible;
        public bool SpawnWithSavedMods { get; private set; } = IsAllowed(Permission.VOSaveMods) ? UserDefaults.VehicleSpawnerSpawnWithSavedMods : false;

        private WMenuItem CreateSpawnVehicleButton(VehicleData.VehicleModelInfo vi)
        {
            var textColor = !vi.HasProperName ? "~y~" : vi.IsAddon ? "~q~" : "";
            var text = $"{textColor}{vi.Name}~s~";

            var manufacturerDescr = vi.Manufacturer != "NULL" ? $"~b~{vi.Manufacturer}~s~ " : "";
            var description = $"Spawn the {manufacturerDescr}~b~{vi.Name}~s~.";

            var btn = new MenuItem(text, description)
            {
                Label = $"~c~({vi.Shortname})~s~",
                ItemData = vi
            }.ToWrapped();
            btn.Selected += async (_s, _args) => await SpawnVehicle(
                vi.Shortname,
                SpawnInVehicle,
                ReplaceVehicle, destructible: SpawnDestructible,
                upgraded: SpawnUpgraded,
                withSavedModifications: SpawnWithSavedMods);

            return btn;
        }

        private static void ChangeThumbnail(MenuItem item, bool immediately)
        {
            if (item == null)
                return;

            var vi = item.ItemData as VehicleData.VehicleModelInfo;
            if (vi != null)
            {
                MainMenu.VehicleThumbnailDrawer?.SetThumbnail(vi.Shortname, immediately);
            }
            else
            {
                MainMenu.VehicleThumbnailDrawer?.HideThumbnail();
            }
        }

        private static void SetIndexPastFilters(WMenu menu, FilterItems filterItems)
        {
            if (menu.Menu.CurrentIndex < filterItems.Count && menu.Count > filterItems.Count)
            {
                menu.Menu.RefreshIndex(filterItems.Count, 0);
                ChangeThumbnail(menu.Menu.GetMenuItems()[menu.CurrentIndex], true);
            }
        }

        private void AddFilterItems(WMenu vehiclesMenu)
        {
            filter = new VehicleData.VehicleFilter();
            filterItems = new FilterItems();

            int currentFilterItemIndex = 0;

            {
                var filterItemIndex = currentFilterItemIndex++;

                var nameFilter = new MenuItem("~b~Filter By Name~s~", "Filter vehicles by (model) name or reset the filter.").ToWrapped();
                nameFilter.Selected += async (_s, _args) =>
                {
                    var input = await GetUserInput("Enter filter text. Leave empty to reset the filter", 20);
                    if (input == null)
                        return;

                    filter.Name = input;
                    FilterAllVehiclesMenu();
                    vehiclesMenu.Menu.RefreshIndex(filterItemIndex);
                };

                filterItems.Name = nameFilter;
                vehiclesMenu.AddItem(nameFilter);
            }

            {
                var filterItemIndex = currentFilterItemIndex++;

                var manufacturers = VehicleData.DisplayVehicles
                    .Select(veh => veh.Manufacturer)
                    .Distinct()
                    .OrderBy(s => s, Comparer<string>.Create(VehicleData.CompareManufacturers))
                    .Select(s => s == "NULL" ? "~italic~Unknown~italic~" : s);

                var manufacturerFilterOptions = Enumerable.Concat(["~italic~All~italic~"], manufacturers).ToList();
                var manufacturerFilter = new MenuListItem("~b~Filter By Manufacturer~s~", manufacturerFilterOptions, 0, "Filter vehicles by manufacturer. Click to reset the filter.").ToWrapped();
                manufacturerFilter.ListChanged += (_s, args) =>
                {
                    if (args.ListIndexNew == 0)
                    {
                        filter.Manufacturer = null;
                    }
                    else if (args.ListIndexNew == manufacturerFilterOptions.Count - 1)
                    {
                        filter.Manufacturer = "NULL";
                    }
                    else
                    {
                        filter.Manufacturer = manufacturerFilterOptions[args.ListIndexNew];
                    }
                    FilterAllVehiclesMenu();
                    vehiclesMenu.Menu.RefreshIndex(filterItemIndex);
                };
                manufacturerFilter.ListSelected += (_s, _args) =>
                {
                    manufacturerFilter.AsListItem().ListIndex = 0;
                    filter.Manufacturer = null;
                    FilterAllVehiclesMenu();
                    vehiclesMenu.Menu.RefreshIndex(filterItemIndex);
                };

                filterItems.Manufacturer = manufacturerFilter;
                vehiclesMenu.AddItem(manufacturerFilter);
            }

            bool customClassesOnly = GetSettingsBool(Setting.vmenu_only_custom_classes);

            var customClasses = VehicleData.CustomVehicleClasses
                .Select(c => c.Name)
                .ToList();
            if (customClasses.Count > 0)
            {
                var filterItemIndex = currentFilterItemIndex++;

                var customClassesOptions = Enumerable.Concat(["~italic~All~italic~"], customClasses).ToList();
                var customClassesFilter = new MenuListItem(
                    $"~b~Filter By {(customClassesOnly ? "" : "Custom ")}Class~s~",
                    customClassesOptions,
                    0,
                    "Filter vehicles by custom class. Click to reset the filter.").ToWrapped();
                customClassesFilter.ListChanged += (_s, args) =>
                {
                    if (args.ListIndexNew == 0)
                    {
                        filter.CustomClass = null;
                    }
                    else
                    {
                        filter.CustomClass = customClassesOptions[args.ListIndexNew];
                    }
                    FilterAllVehiclesMenu();
                    vehiclesMenu.Menu.RefreshIndex(filterItemIndex);
                };
                customClassesFilter.ListSelected += (_s, _args) =>
                {
                    customClassesFilter.AsListItem().ListIndex = 0;
                    filter.CustomClass = null;
                    FilterAllVehiclesMenu();
                    vehiclesMenu.Menu.RefreshIndex(filterItemIndex);
                };

                filterItems.CustomClass = customClassesFilter;
                vehiclesMenu.AddItem(customClassesFilter);
            }

            if (customClasses.Count == 0 || !customClassesOnly)
            {
                var filterItemIndex = currentFilterItemIndex++;

                var defaultClasses = VehicleData.DisplayVehicles
                    .Select(veh => veh.Class)
                    .OrderBy(c => c, Comparer<int>.Create(VehicleData.CompareClasses))
                    .Distinct()
                    .Select(c => VehicleData.ClassIdToName[c]);

                var rockstarClassesOptions = Enumerable.Concat(["~italic~All~italic~"], defaultClasses).ToList();
                var rockstarClassesFilter = new MenuListItem(
                    $"~b~Filter By {(customClasses.Count == 0 ? "" : "Rockstar ")}Class~s~",
                    rockstarClassesOptions,
                    0,
                    "Filter vehicles by Rockstar class. Click to reset the filter.").ToWrapped();
                rockstarClassesFilter.ListChanged += (_s, args) =>
                {
                    if (args.ListIndexNew == 0)
                    {
                        filter.RockstarClass = null;
                    }
                    else
                    {
                        filter.RockstarClass = rockstarClassesOptions[args.ListIndexNew];
                    }
                    FilterAllVehiclesMenu();
                    vehiclesMenu.Menu.RefreshIndex(filterItemIndex);
                };
                rockstarClassesFilter.ListSelected += (_s, _args) =>
                {
                    rockstarClassesFilter.AsListItem().ListIndex = 0;
                    filter.RockstarClass = null;
                    FilterAllVehiclesMenu();
                    vehiclesMenu.Menu.RefreshIndex(filterItemIndex);
                };

                filterItems.DefaultClass = rockstarClassesFilter;
                vehiclesMenu.AddItem(rockstarClassesFilter);
            }

            if (IsAllowed(Permission.VOSaveMods))
            {
                var filterItemIndex = currentFilterItemIndex++;

                var defaultModsFilter = new MenuListItem(
                    MainMenu.MenuText["VEHICLES_LIST__DEFAULT_MODS_FILTER__ITEM"],
                    new List<string> { "~italic~All~italic~", "With", "Without" },
                    0,
                    MainMenu.MenuText["VEHICLES_LIST__DEFAULT_MODS_FILTER__DESC"]).ToWrapped();
                defaultModsFilter.ListChanged += (_, args) =>
                {
                    filter.FilterDefaultMods = (VehicleData.VehicleFilterFilterDefaultMods)args.ListIndexNew;

                    FilterAllVehiclesMenu();
                    vehiclesMenu.Menu.RefreshIndex(filterItemIndex);
                };
                defaultModsFilter.ListSelected += (_, args) =>
                {
                    defaultModsFilter.AsListItem().ListIndex = 0;
                    filter.FilterDefaultMods = VehicleData.VehicleFilterFilterDefaultMods.All;

                    FilterAllVehiclesMenu();
                    vehiclesMenu.Menu.RefreshIndex(filterItemIndex);
                };

                vehiclesMenu.AddItem(defaultModsFilter);
                filterItems.DefaultMods = defaultModsFilter;

                currentFilterItemIndex++;
            }

            vehiclesMenu.AddItem(WMenuItem.CreateSeparatorItem("Vehicles"));

            vehiclesMenu.Menu.InstructionalButtons.Add(Control.SelectWeapon, "Filter Vehicles");
            vehiclesMenu.Menu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(
                Control.SelectWeapon,
                Menu.ControlPressCheckType.JUST_RELEASED,
                (m, _c) =>
                {
                    if (vehiclesMenu.CurrentIndex < filterItems.Count)
                    {
                        SetIndexPastFilters(vehiclesMenu, filterItems);
                    }
                    else
                    {
                        vehiclesMenu.Menu.RefreshIndex();
                        vehiclesMenu.ResetIncrement();
                        MainMenu.VehicleThumbnailDrawer?.HideThumbnail();
                    }
                },
                true));
        }

        private WMenu CreateVehiclesMenu(string subtitle, List<VehicleData.VehicleModelInfo> vehicles, bool addFilters = false)
        {
            var vehiclesMenu = new WMenu(MenuTitle, subtitle);

            if (addFilters)
            {
                AddFilterItems(vehiclesMenu);
            }

            if (vehicles.Count > 10)
            {
                vehiclesMenu.AddIncrementToggle(Control.NextCamera);

                vehiclesMenu.Closed += (_s, _args) => vehiclesMenu.ResetIncrement();
            }

            foreach (var vehicle in vehicles)
            {
                var btn = CreateSpawnVehicleButton(vehicle);
                vehiclesMenu.AddItem(btn);
            }

            vehiclesMenu.IndexChanged += (_, args) =>
            {
                ChangeThumbnail(args.ItemNew.MenuItem, false);
            };

            vehiclesMenu.Opened += (s, args) =>
            {
                // Filter on open because vehicles with/without default mods may have changed
                FilterAllVehiclesMenu();

                // Restore prev position in menu after filter
                var index = Math.Min(prevIndex, vehiclesMenu.Count - 1);
                var maxOffset = Math.Max(0, vehiclesMenu.Count - vehiclesMenu.Menu.MaxItemsOnScreen);
                var offset = Math.Min(prevOffset, maxOffset);
                vehiclesMenu.Menu.RefreshIndex(index, offset);

                SetIndexPastFilters(vehiclesMenu, filterItems);
                ChangeThumbnail(vehiclesMenu.Menu.GetCurrentMenuItem(), true);
            };

            vehiclesMenu.Closed += (_s, _args) =>
            {
                if (searchingByName)
                {
                    ResetAllVehiclesFilter();
                    FilterAllVehiclesMenu();
                }
                searchingByName = false;
                prevIndex = vehiclesMenu.Menu.CurrentIndex;
                prevOffset = vehiclesMenu.Menu.ViewIndexOffset;
            };

            if (addFilters && IsAllowed(Permission.VOMenu))
            {
                vehiclesMenu.Menu.InstructionalButtons.Add(Control.LookBehind, "Vehicle Customization");
                vehiclesMenu.Menu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(
                    Control.LookBehind,
                    Menu.ControlPressCheckType.JUST_RELEASED,
                    (m, _c) =>
                    {
                        if (MainMenu.VehicleCustomizationMenu == null)
                        {
                            return;
                        }

                        var customizationMenu = MainMenu.VehicleCustomizationMenu.GetMenu();
                        MenuController.AddSubmenu(vehiclesMenu.Menu, customizationMenu);
                        MenuController.CloseAllMenus();
                        customizationMenu.OpenMenu();
                    },
                    true));
            }

            MainMenu.VehicleThumbnailDrawer?.AddMenu(vehiclesMenu.Menu);
            return vehiclesMenu;
        }

        private Random random = new Random();

        private List<string> randomLandVehiclesList;
        private List<string> randomWaterVehiclesList;
        public async Task SpawnRandomVehicle()
        {
            List<string> randomVehiclesList;

            var playerId = PlayerPedId();

            if (IsPedSwimming(playerId) || IsPedSwimmingUnderWater(playerId))
            {
                randomVehiclesList = randomWaterVehiclesList;
            }
            else
            {
                randomVehiclesList = randomLandVehiclesList;
            }

            if (randomVehiclesList == null || randomVehiclesList.Count == 0)
            {
                Notify.Error("You are not able to spawn any random vehicles, sorry");
                return;
            }
            var veh = randomVehiclesList[random.Next(0, randomVehiclesList.Count)];
            await SpawnVehicle(
                veh,
                SpawnInVehicle,
                ReplaceVehicle,
                destructible: SpawnDestructible,
                upgraded: SpawnUpgraded,
                withSavedModifications: SpawnWithSavedMods);
        }

        private List<string> randomSportyVehiclesList;
        public async Task SpawnRandomSportyVehicle()
        {
            if (randomSportyVehiclesList.Count == 0)
            {
                Notify.Error("You are not able to spawn any random sporty vehicles, sorry");
                return;
            }
            var veh = randomSportyVehiclesList[random.Next(0, randomSportyVehiclesList.Count)];
            await SpawnVehicle(
                veh,
                SpawnInVehicle,
                ReplaceVehicle,
                destructible: SpawnDestructible,
                upgraded: SpawnUpgraded,
                withSavedModifications: SpawnWithSavedMods);
        }


        private void CreateMenu()
        {
            var allowedVehiclesList = VehicleData.AllVehicles.Values
                .Where(vi => vi.IsAllowed)
                .OrderBy(vi => vi.Name, Comparer<string>.Create(VehicleData.CompareVehicleNames))
                .ToList();

            var allowedDisplayVehiclesList = allowedVehiclesList.Where(vi => vi.DisplayVehicle).ToList();

            var possibleRandomVehicles = VehicleData.DisplayVehicles.Where(veh => !veh.IsBlacklisted);

            randomLandVehiclesList = possibleRandomVehicles
                .Where(veh =>
                {
                    var hash = veh.Hash;
                    return
                        IsThisModelABicycle(hash) ||
                        IsThisModelABike(hash) ||
                        IsThisModelACar(hash) ||
                        IsThisModelAnAmphibiousCar(hash) ||
                        IsThisModelAnAmphibiousQuadbike((int)hash) ||
                        IsThisModelAQuadbike(hash);
                })
                .Select(veh => veh.Shortname)
                .ToList();
            randomWaterVehiclesList = possibleRandomVehicles
                .Where(veh =>
                {
                    var hash = veh.Hash;
                    return
                        IsThisModelABoat(hash) ||
                        IsThisModelAJetski(hash) ||
                        IsThisModelASubmersible(hash) ||
                        IsThisModelAnAmphibiousCar(hash) ||
                        IsThisModelAnAmphibiousQuadbike((int)hash) ||
                        IsThisModelAnEmergencyBoat(hash);
                })
                .Select(veh => veh.Shortname)
                .ToList();

            randomSportyVehiclesList = VehicleData.DisplayVehicles
                .Where(veh => veh.IsSporty)
                .Select(veh => veh.Shortname)
                .ToList();

            // Create the menu.
            menu = new WMenu(MenuTitle, "Spawn Vehicles");

            if (IsAllowed(Permission.VSSpawnByName))
            {
                var spawnVehicleByName = new MenuItem("Spawn Vehicle By Model Name", "Spawn a vehicle by its exact model name.").ToWrapped();
                spawnVehicleByName.Selected += async (_s, _args) => await SpawnVehicle(
                    "custom",
                    SpawnInVehicle,
                    ReplaceVehicle,
                    SpawnDestructible,
                    SpawnUpgraded,
                    SpawnWithSavedMods);

                menu.AddItem(spawnVehicleByName);
            }

            {
                var searchByName = new MenuItem("Search Vehicle By Name", "Search a vehicle by its (model) name").ToWrapped();
                searchByName.Selected += async (_s, _args) =>
                {
                    var input = await GetUserInput("Enter search text", 20);
                    if (string.IsNullOrEmpty(input))
                        return;

                    ResetAllVehiclesFilter();
                    int count = FilterAllVehiclesMenu(input);
                    if (count == 0)
                    {
                        Notify.Info("No vehicles found matching this search.");

                        ResetAllVehiclesFilter();
                        FilterAllVehiclesMenu();
                    }
                    else
                    {
                        searchingByName = true;
                        MenuController.CloseAllMenus();
                        MenuController.AddSubmenu(menu.Menu, AllVehiclesMenu.Menu);
                        AllVehiclesMenu.Menu.OpenMenu();
                    }
                };

                menu.AddItem(searchByName);
            }

            {
                AllVehiclesMenu = CreateVehiclesMenu("Vehicles List", allowedDisplayVehiclesList, addFilters: true);
                menu.AddSubmenu(AllVehiclesMenu, "A list of all vehicles that you can also filter.");
            }

            {
                var spawnRandom = new MenuItem("Spawn Random Vehicle", "Spawn a random land-based vehicle.").ToWrapped();
                spawnRandom.Selected += async (_s, _args) => await SpawnRandomVehicle();

                menu.AddItem(spawnRandom);
            }

            if (randomSportyVehiclesList.Count > 0)
            {
                var spawnRandomSporty = new MenuItem("Spawn Random Sporty Vehicle", "Spawn a random, but sporty land-based vehicle.").ToWrapped();
                spawnRandomSporty.Selected += async (_s, _args) => await SpawnRandomSportyVehicle();

                menu.AddItem(spawnRandomSporty);
            }

            {
                var spawnOptionsMenu = new Menu(MenuTitle, "Spawn Options");
                var spawnOptionsBtn = new MenuItem("Spawn Options", "Change vehicle spawn options.");

                var spawnInVeh = new MenuCheckboxItem("Spawn Inside Vehicle", "If enabled, you will automatically spawn into the spawned vehicles.", SpawnInVehicle);
                var replacePrev = new MenuCheckboxItem("Replace Previous Vehicle", "If enabled, the newly spawned vehicle will replace your old one.", ReplaceVehicle);
                var spawnWithSavedMods = new MenuCheckboxItem(
                    MainMenu.MenuText["VEHICLE_SPAWNER__SPAWN_WITH_DEFAULT_MODS__ITEM"],
                    MainMenu.MenuText["VEHICLE_SPAWNER__SPAWN_WITH_DEFAULT_MODS__DESC"],
                    SpawnWithSavedMods);
                var spawnUpgraded = new MenuCheckboxItem("Spawn Upgraded Vehicle", "If enabled and you don't have custom mods for the vehicle, performance upgrades will be applied to the spawned vehicle.", SpawnUpgraded);
                var spawnDestructible = new MenuCheckboxItem("Spawn Traffic-Style Vehicle", "If enabled, spawned vehicles can despawn when too far away and explode on impact.", SpawnDestructible);

                if (IsAllowed(Permission.VOSaveMods))
                {
                    spawnOptionsMenu.AddMenuItem(spawnWithSavedMods);
                }
                if (IsAllowed(Permission.VOMod))
                {
                    spawnOptionsMenu.AddMenuItem(spawnUpgraded);
                }
                spawnOptionsMenu.AddMenuItem(spawnInVeh);
                if (IsAllowed(Permission.VSDisableReplacePrevious))
                {
                    spawnOptionsMenu.AddMenuItem(replacePrev);
                }
                else
                {
                    replacePrev = null;
                    ReplaceVehicle = true;
                }
                spawnOptionsMenu.AddMenuItem(spawnDestructible);

                menu.AddSubmenu(spawnOptionsMenu);

                spawnOptionsMenu.OnCheckboxChange += (sender, item, index, _checked) =>
                {
                    if (item == spawnInVeh)
                    {
                        UserDefaults.VehicleSpawnerSpawnInside = SpawnInVehicle = _checked;
                    }
                    else if (item == spawnUpgraded)
                    {
                        UserDefaults.VehicleSpawnerSpawnUpgraded = SpawnUpgraded = _checked;
                    }
                    else if (item == replacePrev)
                    {
                        UserDefaults.VehicleSpawnerReplacePrevious = ReplaceVehicle = _checked;
                    }
                    else if (item == spawnDestructible)
                    {
                        UserDefaults.VehicleSpawnerSpawnDestructible = SpawnDestructible = _checked;
                    }
                    else if (item == spawnWithSavedMods)
                    {
                        UserDefaults.VehicleSpawnerSpawnWithSavedMods = SpawnWithSavedMods = _checked;
                    }
                };
            }

            if (VehicleData.VehicleDisablelist.Count > 0 && IsAllowed(Permission.VODisableFromDefaultList))
            {
                var allowedDisabledVehicles = allowedVehiclesList
                    .Where(vi => vi.IsHidden)
                    .ToList();

                if (allowedDisabledVehicles.Count > 0)
                {
                    var disabledVehiclesMenu = CreateVehiclesMenu("Hidden Vehicles", allowedDisabledVehicles, addFilters: false);

                    WMenuItem button = new MenuItem(
                        "~y~Hidden Vehicles~s~",
                        "~y~These vehicles will not show in other vehicle lists and can only be spawned by players with the ~o~VODisableFromDefaultList~y~ permission.~s~")
                        .ToWrapped();
                    menu.BindSubmenu(disabledVehiclesMenu, button);

                    menu.AddItem(button);
                }
            }

            if (VehicleData.VehicleBlacklist.Count > 0 && IsAllowed(Permission.VOVehiclesBlacklist))
            {
                var allowedBlacklistedVehicles = allowedVehiclesList
                    .Where(vi => VehicleData.VehicleBlacklist.Contains(vi.Shortname))
                    .ToList();

                if (allowedBlacklistedVehicles.Count > 0)
                {
                    var disabledVehiclesMenu = CreateVehiclesMenu("Blacklisted Vehicles", allowedBlacklistedVehicles, addFilters: false);

                    WMenuItem button = new MenuItem(
                        "~y~Blacklisted Vehicles~s~",
                        "~y~These vehicles ~italic~will~italic~ show in other vehicle lists, but can only be spawned by players with the ~o~VOVehiclesBlacklist~y~ permission.~s~")
                        .ToWrapped();
                    menu.BindSubmenu(disabledVehiclesMenu, button);

                    menu.AddItem(button);
                }
            }
        }

        /// <summary>
        /// Create the menu if it doesn't exist, and then returns it.
        /// </summary>
        /// <returns>The Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
            }
            return menu.Menu;
        }
    }
}
