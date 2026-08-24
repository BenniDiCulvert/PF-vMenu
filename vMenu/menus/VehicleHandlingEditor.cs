using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.MenuAPIWrapper;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;


namespace vMenuClient.menus
{
    public class VehicleHandlingEditor
    {
        public static int? Vehicle
        {
            get
            {
                var player = Player.Local.Character.Handle;
                var vehicle = GetVehiclePedIsIn(player, false);

                return vehicle == 0 ? null : vehicle;
            }
        }

        const string KvsPrefix = "vmenu_handlingdata_";

        private static WMenuItem CreateInputItem(string name, Func<string, bool> setValue)
        {
            var item = new MenuItem(name).ToWrapped();
            item.Selected += async (_s, args) =>
            {
                var input = await GetUserInput(name, item.Label);
                if (input == null)
                {
                    return;
                }

                if (input.Length != 0)
                {
                    if (!setValue(input))
                    {
                        Notify.Error("The input was invalid.");
                    }
                }
                else
                {
                    Notify.Error("The input was empty.");
                }
            };

            return item;
        }

        public struct Flag
        {
            public Flag(string name, uint bit)
            {
                Name = name;
                Bit = bit;
            }

            public string Name { get; }
            public uint Bit { get; }
        }

        public abstract class Field
        {
            public string Section { get; }
            public string Name { get; }

            protected Field(string section, string name)
            {
                Section = section;
                Name = name;
            }

            protected int GetHandlingInt()
            {
                return GetVehicleHandlingInt(CurrentVehicle ?? 0, Section, Name);
            }
            protected void SetHandlingInt(int val)
            {
                SetVehicleHandlingInt(CurrentVehicle ?? 0, Section, Name, val);
            }

            protected float GetHandlingFloat()
            {
                return GetVehicleHandlingFloat(CurrentVehicle ?? 0, Section, Name);
            }
            protected void SetHandlingFloat(float val)
            {
                SetVehicleHandlingFloat(CurrentVehicle ?? 0, Section, Name, val);
            }

            protected Vector3 GetHandlingVector()
            {
                return GetVehicleHandlingVector(CurrentVehicle ?? 0, Section, Name);
            }
            protected void SetHandlingVector(Vector3 val)
            {
                SetVehicleHandlingVector(CurrentVehicle ?? 0, Section, Name, val);
            }

            public abstract void CreateMenuItem(WMenu menu);

            public void ApplyValue()
            {
                ApplyValueImpl();
            }
            public void ResetValue()
            {
                if (CurrentVehicle.HasValue)
                {
                    ResetValueImpl();
                }
            }

            public abstract string Serialize();

            public void DeserializeAndApply(string value)
            {
                Deserialize(value);
                ApplyValue();
            }
            protected abstract void Deserialize(string value);

            protected abstract void ApplyValueImpl();
            protected abstract void ResetValueImpl();

            private int? CurrentVehicle
            {
                get
                {
                    var vehicle = GetVehiclePedIsIn(Player.Local.Character.Handle, false);
                    return vehicle == 0 ? null : vehicle;
                }
            }
        }

        public class IntField : Field
        {
            private WMenuItem menuItem;

            public IntField(string section, string name) : base(section, name)
            {
                menuItem = CreateInputItem(
                    Name,
                    input =>
                    {
                        if (int.TryParse(input, out _))
                        {
                            DeserializeAndApply(input);
                            return true;
                        }
                        return false;
                    });
                menuItem.Label = $"{0}";
            }

            public override void CreateMenuItem(WMenu menu)
            {
                menu.AddItem(menuItem);
            }

            protected override void ApplyValueImpl()
            {
                SetHandlingInt(int.Parse(menuItem.Label));
            }

            protected override void ResetValueImpl()
            {
                menuItem.Label = $"{GetHandlingInt()}";
            }

            public override string Serialize()
            {
                return menuItem.Label;
            }

            protected override void Deserialize(string value)
            {
                menuItem.Label = value;
            }
        }

        public class FloatField : Field
        {
            private WMenuItem menuItem;

            public FloatField(string section, string name) : base(section, name)
            {
                menuItem = CreateInputItem(
                    Name,
                    input =>
                    {
                        if (float.TryParse(input, out _))
                        {
                            DeserializeAndApply(input);
                            return true;
                        }

                        return false;
                    });
                menuItem.Label = $"{0.0}";
            }

            public override void CreateMenuItem(WMenu menu)
            {
                menu.AddItem(menuItem);
            }

            protected override void ApplyValueImpl()
            {
                SetHandlingFloat(float.Parse(menuItem.Label));
            }

            protected override void ResetValueImpl()
            {
                menuItem.Label = $"{GetHandlingFloat()}";
            }

            public override string Serialize()
            {
                return menuItem.Label;
            }

            protected override void Deserialize(string value)
            {
                menuItem.Label = value;
            }
        }

        public class VectorField : Field
        {
            WMenuItem xMenuItem;
            WMenuItem yMenuItem;
            WMenuItem zMenuItem;
            WMenu vectorMenu;


            private Func<string, bool> ParseAndSet(Func<WMenuItem> getItem)
            {
                return input =>
                {
                    if (float.TryParse(input, out _))
                    {
                        getItem().Label = input;
                        ApplyValue();
                        return true;
                    }

                    return false;
                };
            }

            public VectorField(string section, string name) : base(section, name)
            {
                vectorMenu = new WMenu(MenuTitle, name);
                xMenuItem = CreateInputItem("x", ParseAndSet(() => xMenuItem));
                yMenuItem = CreateInputItem("y", ParseAndSet(() => yMenuItem));
                zMenuItem = CreateInputItem("z", ParseAndSet(() => zMenuItem));

                xMenuItem.Label = $"{0.0}";
                yMenuItem.Label = $"{0.0}";
                zMenuItem.Label = $"{0.0}";

                vectorMenu.AddItems([xMenuItem, yMenuItem, zMenuItem]);
            }

            public override void CreateMenuItem(WMenu menu)
            {
                menu.AddSubmenu(vectorMenu);
            }

            private Vector3 GetVectorValue()
            {
                return new Vector3(
                    float.Parse(xMenuItem.Label),
                    float.Parse(yMenuItem.Label),
                    float.Parse(zMenuItem.Label));
            }

            private void SetVectorValue(Vector3 vec)
            {
                xMenuItem.Label = $"{vec.X}";
                yMenuItem.Label = $"{vec.Y}";
                zMenuItem.Label = $"{vec.Z}";
            }

            protected override void ApplyValueImpl()
            {
                SetHandlingVector(GetVectorValue());
            }

            protected override void ResetValueImpl()
            {
                SetVectorValue(GetHandlingVector());
            }

            public override string Serialize()
            {
                return JsonConvert.SerializeObject(GetVectorValue());
            }

            protected override void Deserialize(string value)
            {
                var vec = JsonConvert.DeserializeObject<Vector3>(value);
                SetVectorValue(vec);
            }
        }

        public class FlagsField : Field
        {
            List<WMenuItem> flagItems = new List<WMenuItem>();
            WMenu flagsMenu;

            uint curr = 0;

            public FlagsField(string section, string name, List<Flag> flags) : base(section, name)
            {
                foreach (var flag in flags)
                {
                    var flagCb = new MenuCheckboxItem(flag.Name, false).ToWrapped();
                    flagCb.ItemData = new { bit = flag.Bit };
                    flagCb.CheckboxChanged += (_s, args) =>
                    {
                        if (args.Checked)
                        {
                            curr |= flag.Bit;
                        }
                        else
                        {
                            curr &= ~flag.Bit;
                        }

                        ApplyValue();
                    };
                    flagItems.Add(flagCb);
                }
            }

            private void UpdateMenuItems()
            {
                foreach (var menuItem in flagItems)
                {
                    var cb = menuItem.AsCheckboxItem();
                    uint bit = cb.ItemData.bit;

                    cb.Checked = (curr & bit) != 0;
                }
            }

            public override void CreateMenuItem(WMenu menu)
            {
                flagsMenu = new WMenu(MenuTitle, Name);
                flagsMenu.AddItems(flagItems);

                menu.AddSubmenu(flagsMenu);
            }

            protected override void ApplyValueImpl()
            {
                SetHandlingInt((int)curr);
            }

            protected override void ResetValueImpl()
            {
                curr = (uint)GetHandlingInt();
                UpdateMenuItems();
            }

            public override string Serialize()
            {
                return $"{curr}";
            }

            protected override void Deserialize(string value)
            {
                curr = uint.Parse(value);
                UpdateMenuItems();
            }
        }

        public static List<Flag> ModelFlags = new List<Flag>
        {
             new Flag("MF_IS_VAN", 0x1),
             new Flag("MF_IS_BUS", 0x2),
             new Flag("MF_IS_LOW", 0x4),
             new Flag("MF_IS_BIG", 0x8),
             new Flag("MF_ABS_STD", 0x10),
             new Flag("MF_ABS_OPTION", 0x20),
             new Flag("MF_ABS_ALT_STD", 0x40),
             new Flag("MF_ABS_ALT_OPTION", 0x80),
             new Flag("MF_NO_DOORS", 0x100),
             new Flag("MF_TANDEM_SEATING", 0x200),
             new Flag("MF_SIT_IN_BOAT", 0x400),
             new Flag("MF_HAS_TRACKS", 0x800),
             new Flag("MF_NO_EXHAUST", 0x1000),
             new Flag("MF_DOUBLE_EXHAUST", 0x2000),
             new Flag("MF_NO_1STPERSON_LOOKBEHIND", 0x4000),
             new Flag("MF_CAN_ENTER_IF_NO_DOOR", 0x8000),
             new Flag("MF_AXLE_F_TORSION", 0x10000),
             new Flag("MF_AXLE_F_SOLID", 0x20000),
             new Flag("MF_AXLE_F_MCPHERSON", 0x40000),
             new Flag("MF_ATTACH_PED_TO_BODYSHELL", 0x80000),
             new Flag("MF_AXLE_R_TORSION", 0x100000),
             new Flag("MF_AXLE_R_SOLID", 0x200000),
             new Flag("MF_AXLE_R_MCPHERSON", 0x400000),
             new Flag("MF_DONT_FORCE_GRND_CLEARANCE", 0x800000),
             new Flag("MF_DONT_RENDER_STEER", 0x1000000),
             new Flag("MF_NO_WHEEL_BURST", 0x2000000),
             new Flag("MF_INDESTRUCTIBLE", 0x4000000),
             new Flag("MF_DOUBLE_FRONT_WHEELS", 0x8000000),
             new Flag("MF_IS_RC", 0x10000000),
             new Flag("MF_DOUBLE_REAR_WHEELS", 0x20000000),
             new Flag("MF_NO_WHEEL_BREAK", 0x40000000),
             new Flag("MF_IS_HATCHBACK", 0x80000000),
        };

        public static List<Flag> HandlingFlags = new List<Flag>
        {
            new Flag("HF_SMOOTHED_COMPRESSION", 0x1),
            new Flag("HF_REDUCED_MOD_MASS", 0x2),
            new Flag("HF_HAS_KERS", 0x4),
            new Flag("HF_HAS_RALLY_TYRE", 0x8),
            new Flag("HF_NO_HANDBRAKE", 0x10),
            new Flag("HF_STEER_REARWHEELS", 0x20),
            new Flag("HF_HANDBRAKE_REARWHEELSTEER", 0x40),
            new Flag("HF_STEER_ALL_WHEELS", 0x80),
            new Flag("HF_FREEWHEEL_NO_GAS", 0x100),
            new Flag("HF_NO_REVERSE", 0x200),
            new Flag("HF_REDUCED_RIGHTING_FORC", 0x400),
            new Flag("HF_STEER_NO_WHEELS", 0x800),
            new Flag("HF_CVT", 0x1000),
            new Flag("HF_ALT_EXT_WHEEL_BOUNDS_BEH", 0x2000),
            new Flag("HF_DONT_RAISE_BOUNDS_AT_SPEED", 0x4000),
            new Flag("HF_EXT_WHEEL_BOUNDS_COL", 0x8000),
            new Flag("HF_LESS_SNOW_SINK", 0x10000),
            new Flag("HF_TYRES_CAN_CLIP", 0x20000),
            new Flag("HF_REDUCED_DRIVE_OVER_DAMAG", 0x40000),
            new Flag("HF_ALT_EXT_WHEEL_BOUNDS_SHRIN", 0x80000),
            new Flag("HF_OFFROAD_ABILITIES", 0x100000),
            new Flag("HF_OFFROAD_ABILITIES_X2", 0x200000),
            new Flag("HF_TYRES_RAISE_SIDE_IMPACT_THRESHOLD", 0x400000),
            new Flag("HF_OFFROAD_INCREASED_GRAVITY_NO_FOLIAGE_DRA", 0x800000),
            new Flag("HF_ENABLE_LEAN", 0x1000000),
            new Flag("HF_FORCE_NO_TC_OR_S", 0x2000000),
            new Flag("HF_HEAVYARMOUR", 0x4000000),
            new Flag("HF_ARMOURED", 0x8000000),
            new Flag("HF_SELF_RIGHTING_IN_WATER", 0x10000000),
            new Flag("HF_IMPROVED_RIGHTING_FORCE", 0x20000000),
            new Flag("HF_LOW_SPEED_WHEELIE", 0x40000000),
            new Flag("HF_LAST_AVAILABLE_FLAG", 0x80000000),
        };

        public static List<Flag> DamageFlags = new List<Flag>
        {
            new Flag("DF_DRIVER_SIDE_FRONT_DOOR", 0x1),
            new Flag("DF_DRIVER_SIDE_REAR_DOOR", 0x2),
            new Flag("DF_DRIVER_PASSENGER_SIDE_FRONT_DOOR", 0x4),
            new Flag("DF_DRIVER_PASSENGER_SIDE_REAR_DOOR", 0x8),
            new Flag("DF_BONNET", 0x10),
            new Flag("DF_BOOT", 0x20),
        };

        public static List<Flag> AdvancedHandlingFlags = new List<Flag>
        {
            new Flag("CF_DIFF_FRONT", 0x1),
            new Flag("CF_DIFF_REAR", 0x2),
            new Flag("CF_DIFF_CENTRE", 0x4),
            new Flag("CF_DIFF_LIMITED_FRONT", 0x8),
            new Flag("CF_DIFF_LIMITED_REAR", 0x10),
            new Flag("CF_DIFF_LIMITED_CENTRE", 0x20),
            new Flag("CF_DIFF_LOCKING_FRONT", 0x40),
            new Flag("CF_DIFF_LOCKING_REAR", 0x80),
            new Flag("CF_DIFF_LOCKING_CENTRE", 0x100),
            new Flag("CF_GEARBOX_FULL_AUTO", 0x200),
            new Flag("CF_GEARBOX_MANUAL", 0x400),
            new Flag("CF_GEARBOX_DIRECT_SHIFT", 0x800),
            new Flag("CF_GEARBOX_ELECTRIC", 0x1000),
            new Flag("CF_ASSIST_TRACTION_CONTROL", 0x2000),
            new Flag("CF_ASSIST_STABILITY_CONTROL", 0x4000),
            new Flag("CF_ALLOW_REDUCED_SUSPENSION_FORCE", 0x8000),
            new Flag("CF_HARD_REV_LIMIT", 0x10000),
            new Flag("CF_HOLD_GEAR_WITH_WHEELSPIN", 0x20000),
            new Flag("CF_INCREASE_SUSPENSION_FORCE_WITH_SPEED", 0x40000),
            new Flag("CF_BLOCK_INCREASED_ROT_VELOCITY_WITH_DRIVE_FORCE", 0x80000),
            new Flag("CF_REDUCED_SELF_RIGHTING_SPEED", 0x100000),
            new Flag("CF_CLOSE_RATIO_GEARBOX", 0x200000),
            new Flag("CF_FORCE_SMOOTH_RPM", 0x400000),
            new Flag("CF_ALLOW_TURN_ON_SPOT", 0x800000),
            new Flag("CF_CAN_WHEELIE", 0x1000000),
            new Flag("CF_ENABLE_WHEEL_BLOCKER_SIDE_IMPACTS", 0x2000000),
            new Flag("CF_FIX_OLD_BUGS", 0x4000000),
            new Flag("CF_USE_DOWNFORCE_BIAS", 0x8000000),
            new Flag("CF_REDUCE_BODY_ROLL_WITH_SUSPENSION_MODS", 0x10000000),
            new Flag("CF_ALLOWS_EXTENDED_MODS", 0x20000000),
        };

        public static readonly Dictionary<string, Field> HandlingFields = new List<Field>
        {
            new FloatField("CHandlingData", "fMass"),
            new FloatField("CHandlingData", "fInitialDragCoeff"),
            new FloatField("CHandlingData", "fDownForceModifier"),
            new FloatField("CHandlingData", "fPopUpLightRotation"),
            new FloatField("CHandlingData", "fPercentSubmerged"),
            new VectorField("CHandlingData", "vecCentreOfMassOffset"),
            new VectorField("CHandlingData", "vecInertiaMultiplier"),

            new FloatField("CHandlingData", "fDriveBiasFront"),
            new IntField("CHandlingData", "nInitialDriveGears"),
            new FloatField("CHandlingData", "fInitialDriveForce"),
            new FloatField("CHandlingData", "fDriveInertia"),
            new FloatField("CHandlingData", "fClutchChangeRateScaleUpShift"),
            new FloatField("CHandlingData", "fClutchChangeRateScaleDownShift"),
            new FloatField("CHandlingData", "fInitialDriveMaxFlatVel"),
            new FloatField("CHandlingData", "fBrakeForce"),
            new FloatField("CHandlingData", "fBrakeBiasFront"),
            new FloatField("CHandlingData", "fHandBrakeForce"),
            new FloatField("CHandlingData", "fSteeringLock"),

            new FloatField("CHandlingData", "fTractionCurveMax"),
            new FloatField("CHandlingData", "fTractionCurveMin"),
            new FloatField("CHandlingData", "fTractionCurveLateral"),
            new FloatField("CHandlingData", "fTractionSpringDeltaMax"),
            new FloatField("CHandlingData", "fLowSpeedTractionLossMult"),
            new FloatField("CHandlingData", "fCamberStiffnesss"),
            new FloatField("CHandlingData", "fTractionBiasFront"),
            new FloatField("CHandlingData", "fTractionLossMult"),

            new FloatField("CHandlingData", "fSuspensionForce"),
            new FloatField("CHandlingData", "fSuspensionCompDamp"),
            new FloatField("CHandlingData", "fSuspensionReboundDamp"),
            new FloatField("CHandlingData", "fSuspensionUpperLimit"),
            new FloatField("CHandlingData", "fSuspensionLowerLimit"),
            new FloatField("CHandlingData", "fSuspensionRaise"),
            new FloatField("CHandlingData", "fSuspensionBiasFront"),
            new FloatField("CHandlingData", "fAntiRollBarForce"),
            new FloatField("CHandlingData", "fAntiRollBarBiasFront"),
            new FloatField("CHandlingData", "fRollCentreHeightFront"),
            new FloatField("CHandlingData", "fRollCentreHeightRear"),

            new FloatField("CHandlingData", "fCollisionDamageMult"),
            new FloatField("CHandlingData", "fWeaponDamageMult"),
            new FloatField("CHandlingData", "fDeformationDamageMult"),
            new FloatField("CHandlingData", "fEngineDamageMult"),
            new FloatField("CHandlingData", "fPetrolTankVolume"),
            new FloatField("CHandlingData", "fOilVolume"),
            new FloatField("CHandlingData", "fPetrolConsumptionRate"),

            new FloatField("CHandlingData", "fSeatOffsetDistX"),
            new FloatField("CHandlingData", "fSeatOffsetDistY"),
            new FloatField("CHandlingData", "fSeatOffsetDistZ"),
            new IntField("CHandlingData", "nMonetaryValue"),

            new FlagsField("CHandlingData", "strModelFlags", ModelFlags),
            new FlagsField("CHandlingData", "strHandlingFlags", HandlingFlags),
            new FlagsField("CHandlingData", "strDamageFlags", DamageFlags),

            new FloatField("CCarHandlingData", "fBackEndPopUpCarImpulseMult"),
            new FloatField("CCarHandlingData", "fBackEndPopUpBuildingImpulseMult"),
            new FloatField("CCarHandlingData", "fBackEndPopUpMaxDeltaSpeed"),
            new FloatField("CCarHandlingData", "fToeFront"),
            new FloatField("CCarHandlingData", "fToeRear"),
            new FloatField("CCarHandlingData", "fCamberFront"),
            new FloatField("CCarHandlingData", "fCamberRear"),
            new FloatField("CCarHandlingData", "fCastor"),
            new FloatField("CCarHandlingData", "fEngineResistance"),
            new FloatField("CCarHandlingData", "fMaxDriveBiasTransfer"),
            new FloatField("CCarHandlingData", "fJumpForceScale"),
            new FloatField("CCarHandlingData", "fIncreasedRammingForceScale"),

            new FlagsField("CCarHandlingData", "strAdvancedFlags", AdvancedHandlingFlags),
        }.ToDictionary(f => f.Name);

        private WMenu menu = null;

        private WMenu editHandlingMenu = new WMenu(MenuTitle, "Edit Handling Data");
        private WMenu savedHandlingDataMenu = new WMenu(MenuTitle, "Saved Handling Data");

        private WMenuItem topSpeedModifier =
            new MenuItem("Top Speed Modifier", "Directly modify's a vehicle's top speed.")
            { Label = $"{0.0}" }
            .ToWrapped();

        private void SaveHandlingDataToKvs(string key)
        {
            var handlingData = new Dictionary<string, string>();
            foreach (var handlingField in HandlingFields)
            {
                handlingData.Add(handlingField.Key, handlingField.Value.Serialize());
            }

            var json = JsonConvert.SerializeObject(handlingData);
            KeyValueStore.Set(key, json);
        }

        private void CreateManageSavedHandlingDataMenu(string name, string key)
        {
            var menu = new WMenu(MenuTitle, name);

            var applyBtn = new MenuItem("Apply", "Apply the handling data to the current vehicle").ToWrapped();
            applyBtn.Selected += (_s, _args) =>
            {
                var json = KeyValueStore.GetString(key);
                var handlingData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                foreach (var handlingField in HandlingFields)
                {
                    if (handlingData.TryGetValue(handlingField.Key, out var value))
                    {
                        handlingField.Value.DeserializeAndApply(value);
                    }
                }
            };

            var overwriteBtn = WMenuItem.CreateConfirmationButton("Overwrite", "Overwrite the saved handling data");
            overwriteBtn.Confirmed += (_s, _args) => SaveHandlingDataToKvs(key);

            var deleteBtn = WMenuItem.CreateConfirmationButton("Delete", "Delete the saved handling data");
            deleteBtn.Confirmed += (_s, _args) =>
            {
                MenuController.CloseAllMenus();

                savedHandlingDataMenu.RemoveSubmenu(menu);
                KeyValueStore.Remove(key);

                savedHandlingDataMenu.Menu.OpenMenu();
            };

            menu.AddItems([applyBtn, overwriteBtn, deleteBtn]);

            savedHandlingDataMenu.AddSubmenu(menu, $"Load {name}");
            savedHandlingDataMenu.Menu.SortMenuItems((MenuItem a, MenuItem b) => string.Compare(a.Text, b.Text));
        }

        void ApplyTopSpeedModifier()
        {
            if (Vehicle.HasValue)
            {
                ModifyVehicleTopSpeed(Vehicle.Value, float.Parse(topSpeedModifier.Label));
            }
        }

        public async Task SaveHandling()
        {
            var input = await GetUserInput("Handling Data Save Name", 64);

            if (input == null)
            {
                return;
            }

            if (input.Length == 0)
            {
                Notify.Error("Name cannot be empty.");
                return;
            }

            var kvsKey = $"{KvsPrefix}{input}";

            if (!string.IsNullOrEmpty(KeyValueStore.GetString(kvsKey)))
            {
                Notify.Error("Saved handling data with the given name already exists.");
                return;
            }

            SaveHandlingDataToKvs(kvsKey);
            CreateManageSavedHandlingDataMenu(input, kvsKey);
        }

        public void LoadAllSavedHandling()
        {
            var keys = KeyValueStore.GetAllWithPrefixString(KvsPrefix).Keys;
            foreach (var key in keys)
            {
                var name = key.Substring(KvsPrefix.Length);
                CreateManageSavedHandlingDataMenu(name, key);
            }
        }

        private void CreateMenu()
        {
            menu = new WMenu(MenuTitle, "Vehicle Handling Editor");

            var applyBtn = new MenuItem("Apply", "Apply the current handling data your vehicle.").ToWrapped();
            applyBtn.Selected += (_s, _args) =>
            {
                foreach (var handlingField in HandlingFields)
                {
                    handlingField.Value.ApplyValue();
                }

                ApplyTopSpeedModifier();
            };

            var resetBtn = WMenuItem.CreateConfirmationButton("Reset", "Reset the handling data to the values of your current vehicle.");
            resetBtn.Confirmed += (_s, _args) =>
            {
                foreach (var handlingField in HandlingFields)
                {
                    handlingField.Value.ResetValue();
                }

                if (Vehicle.HasValue)
                {
                    topSpeedModifier.Label = $"{GetVehicleTopSpeedModifier(Vehicle.Value)}";
                }
            };

            topSpeedModifier = CreateInputItem(
                "Top Speed Modifier",
                (input) =>
                {
                    if (float.TryParse(input, out var value))
                    {
                        topSpeedModifier.Label = $"{value}";
                        ApplyTopSpeedModifier();
                        return true;
                    }

                    return false;
                });
            topSpeedModifier.MenuItem.Description = "Directly modify's a vehicle's top speed (MODIFY_VEHICLE_TOP_SPEED).";
            topSpeedModifier.MenuItem.Label = $"{0.0}";

            var saveBtn = new MenuItem("Save", "Save the current handling data so you can load and edit it later.").ToWrapped();
            saveBtn.Selected += async (_s, _args) =>
            {
                await SaveHandling();
            };

            foreach (var field in HandlingFields.Values)
            {
                field.CreateMenuItem(editHandlingMenu);
            }

            LoadAllSavedHandling();

            menu.AddItems([applyBtn, resetBtn]);
            menu.AddSubmenu(editHandlingMenu, "Edit (your current vehicle's) handling data.");

            menu.AddItems([topSpeedModifier, saveBtn]);
            menu.AddSubmenu(savedHandlingDataMenu, "Load and manage saved handling data.", addEmpty: true);
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
            return menu.Menu;
        }
    }
}
