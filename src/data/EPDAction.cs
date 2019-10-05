using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using static StarcraftEPDTriggers.WndModify;

namespace StarcraftEPDTriggers.src.data {

    public class StarcraftMemory {

        public static T getDefaultValue<T>(uint addr, uint elementSize, int offset) {
            byte[] defaultValueData = StarcraftDefaultMemory.GetDefaultMemoryBlock(addr, offset * ((int)elementSize), (int)elementSize);
            if (defaultValueData.Length == 1) {
                return (T)(object)(int)defaultValueData[0];
            } else if (defaultValueData.Length == 2 || defaultValueData.Length == 4) {
                int defaultValue = 0;
                for (int i = 0; i < defaultValueData.Length; i++) {
                    defaultValue <<= 8;
                    defaultValue |= defaultValueData[defaultValueData.Length - i - 1]; // Invert the byte order, because fuck you logic
                }
                return (T)(object)defaultValue;
            } else if (defaultValueData.Length == 8) {
                long defaultValue = 0;
                for (int i = 0; i < defaultValueData.Length; i++) {
                    defaultValue <<= 8;
                    defaultValue |= defaultValueData[defaultValueData.Length - i - 1]; // Invert the byte order, because fuck you logic
                }
                int defaultValueInt = (int)(defaultValue & 0xffffffff);
                return (T)(object)defaultValueInt;
            } else {
                throw new NotImplementedException();
            }
        }

        public static T getDefaultValue<T>(StarcraftMemory.MemoryPlace memory, int offset) {
            return getDefaultValue<T>(memory.Address, memory.ElementSize, offset);
        }

        public static readonly List<MemoryPlace> ActionMemories = new List<MemoryPlace>();
        public static readonly List<MemoryPlace> ConditionMemories = new List<MemoryPlace>();
        public static readonly List<MemoryPlace> ActionUnsafeMemories = new List<MemoryPlace>();
        public static readonly List<MemoryPlace> ConditionUnsafeMemories = new List<MemoryPlace>();

        public static MemoryPlace findConditionMemoryForAddress(uint address) {
            return findMemoryForAddress(address, ConditionMemories);
        }

        public static MemoryPlace findActionMemoryForAddress(uint address) {
            return findMemoryForAddress(address, ActionMemories);
        }

        private static MemoryPlace findMemoryForAddress(uint address, List<MemoryPlace> memories) {
            address = address - (address % 4);
            foreach (MemoryPlace place in memories) {
                if (place.Address <= address) {
                    uint offset = (address - place.Address) / place.ElementSize;
                    if (offset < place.ElementTotal) {
                        return place;
                    }
                }
            }
            return null;
        }

        public class MemoryPlace {

            private uint _address;
            private uint _elementSize;
            private uint _elementsTotal;
            private string[] _names;
            private bool _show;
            Func<RawActionSetDeaths, MemoryPlace, int, EPDAction> _constructor;
            Func<ConditionMemory, MemoryPlace, int, EPDCondition> _constructorCond;

            public static int getAlignedID(uint address, uint elementSize, int inputID) {
                uint finalAddress = address + (elementSize * (uint)inputID);
                uint newAddress = finalAddress - (finalAddress % 4);
                int newInputID = (int)((newAddress - address) / elementSize);
                return newInputID;
            }

            public int getAlignedID(int inputID) {
                return getAlignedID(_address, _elementSize, inputID);
            }

            public uint Address { get { return _address; } }
            public uint ElementSize { get { return _elementSize; } }
            public uint ElementTotal { get { return _elementsTotal; } }
            public uint LastAddress { get { return Address + (ElementSize * (ElementTotal - 1)); } }
            public string NameAction { get { return _names[0]; } }
            public string NameCondition { get { return _names[1]; } }

            public bool Show { get { return _show; } }

            private static void resort() {
                StarcraftMemory.ActionMemories.Sort((StarcraftMemory.MemoryPlace place, StarcraftMemory.MemoryPlace place2) => String.Compare(place.NameAction, place2.NameAction));
                StarcraftMemory.ActionUnsafeMemories.Sort((StarcraftMemory.MemoryPlace place, StarcraftMemory.MemoryPlace place2) => String.Compare(place.NameAction, place2.NameAction));
                StarcraftMemory.ConditionMemories.Sort((StarcraftMemory.MemoryPlace place, StarcraftMemory.MemoryPlace place2) => String.Compare(place.NameCondition, place2.NameCondition));
                StarcraftMemory.ConditionUnsafeMemories.Sort((StarcraftMemory.MemoryPlace place, StarcraftMemory.MemoryPlace place2) => String.Compare(place.NameCondition, place2.NameCondition));

            }

            private void checkAddress() {
                uint check = Address % 4;
                if(check != 0) {
                    throw new NotImplementedException();
                }
            }

            public MemoryPlace(string[] names, uint address, uint elementSize, int totalElements, Func<RawActionSetDeaths, MemoryPlace, int, EPDAction> constructor, Func<ConditionMemory, MemoryPlace, int, EPDCondition> constructorCond, bool canBeUnsafe, bool addToList) {
                _names = names;
                _address = address;
                _elementSize = elementSize;
                _elementsTotal = (uint)totalElements;
                _constructor = constructor;
                _constructorCond = constructorCond;
                _show = addToList;
                if (constructor != null) {
                    StarcraftMemory.ActionMemories.Add(this);
                    if (canBeUnsafe) {
                        StarcraftMemory.ActionUnsafeMemories.Add(this);
                    }
                }
                if(constructorCond != null) {
                    StarcraftMemory.ConditionMemories.Add(this);
                    if (canBeUnsafe) {
                        StarcraftMemory.ConditionUnsafeMemories.Add(this);
                    }
                }
                checkAddress();
                resort();
            }

            public MemoryPlace(uint address, int size, int length, Func<RawActionSetDeaths, MemoryPlace, int, EPDAction> constructor) {
                _names =new string[] { "Unidentifier memory block", "Unidentified memory block" };
                _address = address;
                _elementSize = (uint) size;
                _elementsTotal = (uint) length;
                _constructor = constructor;
                checkAddress();
            }

            public EPDAction construct(RawActionSetDeaths trigger, int ObjectID) {
                return _constructor(trigger, this, ObjectID);
            }

            public EPDCondition constructCond(ConditionMemory mem, int ObjectID) {
                return _constructorCond(mem, this, ObjectID);
            }

            internal void setAddress(uint v) {
                _address = v - (v % 4);
            }
        }

        private static MemoryPlace _get<TARGET_TYPE, VALUE_TYPE>(string name, uint address, int size, int length, string description, Func<int, TARGET_TYPE> typeGetter, Func<int, VALUE_TYPE> valueGetter, bool isAlsoUnsafe) where TARGET_TYPE : Gettable<TARGET_TYPE> where VALUE_TYPE : Gettable<VALUE_TYPE> {
            return new MemoryPlace(new string[] { name, null }, address, (uint)size, length, (RawActionSetDeaths trigger, MemoryPlace memory, int ObjectID) => {
                if (trigger.getSetQuantifier() == SetQuantifier.SetTo) {
                    return new LengthtyEPDAction<TARGET_TYPE, VALUE_TYPE>(memory, ObjectID, trigger, "Change " + description, description, typeGetter, valueGetter);
                } else {
                    return new LengthtyEPDAddSubActionStarcraftMemory<TARGET_TYPE, VALUE_TYPE>(memory, ObjectID, trigger, description, description, typeGetter, valueGetter);
                }
            },
            /*
            (ConditionMemory trigger, MemoryPlace memory, int ObjectID) => {
                if (trigger.getQuantifier() == Quantifier.Exactly) {
                    return new LengthlyEPDAddSubCondition<TARGET_TYPE, VALUE_TYPE>(memory, ObjectID, trigger, "Change " + description, description, typeGetter, valueGetter);
                } else {
                    return new LengthlyEPDAddSubCondition<TARGET_TYPE, VALUE_TYPE>(memory, ObjectID, trigger, description, description, typeGetter, valueGetter);
                }

            }
            */
            null
            
            ,/* isAlsoUnsafe*/ false, true);
        }

        private static MemoryPlace _get1__<VALUE_TYPE>(string name, uint address, int size, string description, Func<int, VALUE_TYPE> valueGetter, bool isAlsoUnsafe) where VALUE_TYPE : Gettable<VALUE_TYPE> {
            return new MemoryPlace(new string[] { name, null }, address, (uint)size, 1, (RawActionSetDeaths trigger, MemoryPlace memory, int ObjectID) => {
                if (trigger.getSetQuantifier() == SetQuantifier.SetTo) {
                    //return new LengthtyEPDActionSingle<VALUE_TYPE>(memory, ObjectID, trigger, "Change " + description, description, valueGetter);
                } else {
                    //return new LengthtyEPDAddSubActionStarcraftMemorySingle<VALUE_TYPE>(memory, ObjectID, trigger, description, description, valueGetter);
                }
                throw new NotImplementedException();
            }, (ConditionMemory trigger, MemoryPlace memory, int ObjectID) => {
                if (trigger.getQuantifier() == Quantifier.Exactly) {
                    //return new LengthlyEPDAddSubConditionSingle<VALUE_TYPE>(memory, ObjectID, trigger, "Change " + description, description, valueGetter);
                } else {
                    //return new LengthlyEPDAddSubConditionSingle<VALUE_TYPE>(memory, ObjectID, trigger, description, description, valueGetter);
                }
                throw new NotImplementedException();
            },/* isAlsoUnsafe */ false, true);
        }

        private static MemoryPlace _getTechUpg<TYPE>(string TechOrUpAct, string TechOrUpCond, uint[] address, int[] lengths, string description, Func<int, TYPE> getter) where TYPE :TechOrUpgradeDef<TYPE> {
            new MemoryPlace(new string[] { TechOrUpAct, TechOrUpCond }, address[0], 1, lengths[0], (RawActionSetDeaths trigger, MemoryPlace mem, int ObjectID) => {

                return new LenghtyEPDAction_UpgradesTech<TYPE>(address, 0, ObjectID, trigger, TechOrUpAct, description, getter);

            }, (ConditionMemory trigger, MemoryPlace mme, int ObjectID)=> {

                return new LenghtyEPDCondition_UpgradesTech<TYPE>(address, 0, ObjectID, trigger, TechOrUpCond, description, getter);

            }, false, true);
            return new MemoryPlace(new string[] { TechOrUpAct, TechOrUpCond }, address[1], 1, lengths[1], (RawActionSetDeaths trigger, MemoryPlace mem, int ObjectID) => {

                return new LenghtyEPDAction_UpgradesTech<TYPE>(address, 1, ObjectID, trigger, TechOrUpAct, description, getter);
            }, (ConditionMemory trigger, MemoryPlace mme, int ObjectID) => {

                return new LenghtyEPDCondition_UpgradesTech<TYPE>(address, 1, ObjectID, trigger, TechOrUpCond, description, getter);

            }, false, false);
        }

        private static MemoryPlace _getKey(string Name, uint address, int size, int length) {
            return new MemoryPlace(new String[] { null, Name }, address, (uint) size, length, null,
                (ConditionMemory trigger, MemoryPlace mem, int ObjectID)=>{
                    return new LengthyEPDCondition_KeyPress(mem, ObjectID, trigger);
                }, false, true );
        }

        private static MemoryPlace _getSetKills(string Name, uint address, int size, int length) {
            return new MemoryPlace(new String[] { Name, null }, address, (uint)size, length,
               (RawActionSetDeaths trigger, MemoryPlace mem, int ObjectID) => {
                   return new LengthEPPAction_SetKills(mem, trigger, ObjectID);
               }, null, false, true);
        }

        private static MemoryPlace _getSetPlayerColor(string Name, uint address, int size, int length) {
            return new MemoryPlace(new String[] { Name, null }, address, (uint)size, length,
               (RawActionSetDeaths trigger, MemoryPlace mem, int ObjectID) => {
                   return new LengthEPPAction_SetPlayerColor(mem, trigger, ObjectID);
               }, null, false, true);
        }

        public static readonly MemoryPlace PlayerColorSelCircle = _getSetPlayerColor("Set Player Color (Experimental)", 0x581D74, 8, IngamePlayerDef.AllIngamePlayers.Length);
      

        public static readonly MemoryPlace UnitGraphics = _get("Set Unit Graphics", 0x006644F8, 1, UnitDef.AllUnits.Length, "unit graphics for ", UnitDef.getByIndex, FlingyImageDef.getByIndex, true);
        public static readonly MemoryPlace UnitMineralCost = _get("Set Unit Mineral Cost", 0x00663888, 2, UnitDef.AllUnits.Length, "unit mineral cost for ", UnitDef.getByIndex, (int index)=>Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace UnitGasCost = _get("Set Unit Gas Cost", 0x0065FD00, 2, UnitDef.AllUnits.Length, "the gas cost for ", UnitDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace AdvancedUnitFlags = _get("Set Unit Advanced Properties", 0x00664080, 4, UnitDef.AllUnits.Length, "advanced properties for ", UnitDef.getByIndex, AdvancedPropertiesDef.getByIndex, false);
        public static readonly MemoryPlace UnitBuildTime = _get("Set Unit Build Time", 0x00660428, 2, UnitDef.AllUnits.Length, "unit build time for ", UnitDef.getByIndex, (int index) => IntDef.getByIndex(index, false), true);
        public static readonly MemoryPlace UnitMaxHelath = _get("Set Unit Maximum Health (Experimental)", 0x00662350, 4, UnitDef.AllUnits.Length, "unit maximum health for ", UnitDef.getByIndex, UnitHPDef.getByIndex, true);
        public static readonly MemoryPlace UnitGroundWeapon = _get("Set Unit Ground Weapon", 0x006636B8, 1, UnitDef.AllUnits.Length, "ground weapon for ", UnitDef.getByIndex, WeaponDef.getByIndex, true);
        public static readonly MemoryPlace UnitAirWeapon = _get("Set Unit Air Weapon", 0x006616E0, 1, UnitDef.AllUnits.Length, "air weapon for ", UnitDef.getByIndex, WeaponDef.getByIndex, true);
        public static readonly MemoryPlace UnitArmor = _get("Set Unit Armor", 0x0065FEC8, 1, UnitDef.AllUnits.Length, "armor for ", UnitDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);
        public static readonly MemoryPlace UnitArmorUpgrade = _get("Set Unit Armor Upgrade", 0x006635D0, 1, UnitDef.AllUnits.Length, "which upgrade applies to the units armor. ", UnitDef.getByIndex, UpgradeDef.getByIndex, true);
        //public static readonly MemoryPlace UnitUnitDimensions = _get("Set Unit Dimensions", 0x006617C8, 8, UnitDef.AllUnits.Length, "unit demensions ", UnitDef.getByIndex, IntDef.getByIndex, true);
        public static readonly MemoryPlace UnitSightRange = _get("Set Unit Sight Range", 0x00663238, 1, UnitDef.AllUnits.Length, "the site range for ", UnitDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);
        public static readonly MemoryPlace UnitSupplyUsed = _get("Set Unit Supply Used", 0x00663CE8, 1, UnitDef.AllUnits.Length, "unit supply used for ", UnitDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);
        public static readonly MemoryPlace UnitKillScore = _get("Set Unit Kill Score", 0x00663EB8, 2, UnitDef.AllUnits.Length, "kill score for ", UnitDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace UnitElevationLevel = _get("Set Unit Elevation Level (Experimental)", 0x00663150, 1, UnitDef.AllUnits.Length, "unit elevation for ", UnitDef.getByIndex, ElevationLevelDef.getByIndex, true);
        public static readonly MemoryPlace UnitTransportRequired = _get("Set Unit Transport Space Required", 0x00664410, 1, UnitDef.AllUnits.Length, "required transport space for ", UnitDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);
        public static readonly MemoryPlace UnitTransportProvided = _get("Set Unit Transport Space Provided", 0x00660988, 1, UnitDef.AllUnits.Length, "provided transport space for ", UnitDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);

        //public static readonly MemoryPlace UnitButtonSet = _get1("Set Unit Button Set (Experimental)", 0x0059CD3C, 4, 1, "button set for ", UnitDef.getByIndex, (int index) => Int32Def.getByIndex(index, false), true);
        public static readonly MemoryPlace UnitRightAction = _get("Set Unit Right-Click Action (Experimental)", 0x00662098, 1, UnitDef.AllUnits.Length, "right-click action for ", UnitDef.getByIndex, RightClickAction.getByIndex, true);
        public static readonly MemoryPlace UnitSubUnit1 = _get("Set Unit Subunit 1 (Experimental)", 0x006607C0, 1, UnitDef.AllUnits.Length, "subunit for ", UnitDef.getByIndex, UnitDef.getByIndex, true);
        public static readonly MemoryPlace UnitSubUnit2 = _get("Set Unit Subunit 2 (Experimental)", 0x00660C38, 1, UnitDef.AllUnits.Length, "subunit for ", UnitDef.getByIndex, UnitDef.getByIndex, true);
        public static readonly MemoryPlace UnitPortrait = _get("Set Unit Portrait (Experimental)", 0x00662F88, 2, UnitDef.AllUnits.Length, "portrait for ", UnitDef.getByIndex, PortraitIdleAvatarsDef.getByIndex, true);
        //public static readonly MemoryPlace UnitBuildingDimensions = _get("Set Unit Dimensions (Buildings)", 0x006617C8, 4, UnitDef.AllUnits.Length, "building demensions ", UnitDef.getByIndex, Int32Def.getByIndex, true);
        public static readonly MemoryPlace UnitShieldsToggle = _get("Set Unit Shields", 0x006647B0, 1, UnitDef.AllUnits.Length, "shield for ", UnitDef.getByIndex, BoolDef.getByIndex, false);
        public static readonly MemoryPlace UnitMovementFlags = _get("Set Unit Movement Flags (Experimental)", 0x00660FC8, 1, UnitDef.AllUnits.Length, "unit movement flags for ", UnitDef.getByIndex, (int index) => Int32Def.getByIndex(index, false), true);



        public static readonly MemoryPlace WeaponGraphics = _get("Set Weapon Graphic", 0x00656CA8, 4, WeaponDef.AllWeapons.Length, "graphic for weapon ", WeaponDef.getByIndex, FlingyImageDef.getByIndex, true);
        public static readonly MemoryPlace WeaponIcon = _get("Set Weapon Icon", 0x00656780, 2, WeaponDef.AllWeapons.Length, "icon for weapon ", WeaponDef.getByIndex, UnitAvatarDef.getByIndex, true);
        public static readonly MemoryPlace WeaponCooldown = _get("Set Weapon Cooldown", 0x00656FB8, 1, WeaponDef.AllWeapons.Length, "weapon cooldown for ", WeaponDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);
        public static readonly MemoryPlace WeaponMinRAnge = _get("Set Weapon Minimum Range", 0x00656A18, 4, WeaponDef.AllWeapons.Length, "weapon minimum range for ", WeaponDef.getByIndex, (int index) => Int32Def.getByIndex(index, false), true);
        public static readonly MemoryPlace WeaponMaxRAnge = _get("Set Weapon Maximum Range", 0x00657470, 4, WeaponDef.AllWeapons.Length, "weapon maximum range for ", WeaponDef.getByIndex, (int index) => Int32Def.getByIndex(index, false), true);
        public static readonly MemoryPlace WeaponDamageFactor = _get("Set Weapon Damage Factor", 0x006564E0, 1, WeaponDef.AllWeapons.Length, "weapon damage factor for ", WeaponDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);
        public static readonly MemoryPlace WeaponDamageAmount = _get("Set Weapon Damage Amount", 0x00656EB0, 2, WeaponDef.AllWeapons.Length, "weapon damage amount for ", WeaponDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace WeaponTargetFlagsd = _get("Set Weapon Target Flags", 0x00657998, 2, WeaponDef.AllWeapons.Length, "weapon target flags for ", WeaponDef.getByIndex, WeaponTargetFlags.getByIndex, true);
        public static readonly MemoryPlace Behavior = _get("Set Weapon Behavior", 0x00656670, 1, WeaponDef.AllWeapons.Length, "behavior for weapon ", WeaponDef.getByIndex, BehaviorDef.getByIndex, true);
        public static readonly MemoryPlace WeaponLabel = _get("Set Weapon Label", 0x006572E0, 2, WeaponDef.AllWeapons.Length, "label for weapon ", WeaponDef.getByIndex, StatsDef.getByIndex, true);
        public static readonly MemoryPlace WeaponEffect = _get("Set Weapon Effect", 0x006566F8, 1, WeaponDef.AllWeapons.Length, "the weapon effect for ", WeaponDef.getByIndex, WeaponEffectDef.getByIndex, true);
        public static readonly MemoryPlace WeaponSplashInnerRadius = _get("Set Weapon Splash Inner Radius", 0x00656888, 2, WeaponDef.AllWeapons.Length, "weapon splash inner radius for ", WeaponDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace WeaponSplashMiddleRadius = _get("Set Weapon Splash Middle Radius", 0x006570C8, 2, WeaponDef.AllWeapons.Length, "weapon splash middle radius for ", WeaponDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace WeaponSplashOuterRadius = _get("Set Weapon Splash Outer Radius", 0x00657780, 2, WeaponDef.AllWeapons.Length, "weapon splash outer radius for ", WeaponDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace WeaponUpgrade = _get("Set Weapon Upgrade", 0x006571D0, 1, WeaponDef.AllWeapons.Length, "which upgrade applies to this weapon (Weapon/Upgrade ID). ", WeaponDef.getByIndex, UpgradeDef.getByIndex, true);
        public static readonly MemoryPlace WeaponDamageType = _get("Set Weapon Damage Type", 0x00657258, 1, 130, "the damage type for ", WeaponDef.getByIndex, WeaponDamageTypeDef.getByIndex, true);
        public static readonly MemoryPlace WeaponRemoveAfter = _get("Set Weapon Remove After", 0x00657040, 1, WeaponDef.AllWeapons.Length, "weapon remove after time for ", WeaponDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);


        public static readonly MemoryPlace WeaponYOffset = _get("Set Weapon Graphics Y Offset (Experimental)", 0x00656C20, 1, WeaponDef.AllWeapons.Length, "graphics Y offset for ", WeaponDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);
        public static readonly MemoryPlace WeaponXOffset = _get("Set Weapon Graphics X Offset (Experimental)", 0x00657910, 1, WeaponDef.AllWeapons.Length, "graphics X offset for ", WeaponDef.getByIndex, (int index) => Int8Def.getByIndex(index, false), true);

        public static readonly MemoryPlace SpriteVisible = _get("Set Sprite Visible (Experimental)", 0x00665C48, 1, SpriteDef.AllSprites.Length, "sprite visibility for ", SpriteDef.getByIndex, BoolDef.getByIndex, false);
        //public static readonly MemoryPlace SpriteSelectionCircle = _get("Set Sprite Selection Circle (Experimental)", 0x00665AC0, 1, Sprite130Def.AllSprites.Length, "sprite selection circle for ", Sprite130Def.getByIndex, Image561Def.getByIndex, true);



        public static readonly MemoryPlace TechResearched = _getTechUpg("Set Technologies Researched", "Researched Technologies (Experimental)", new uint[] { 0x0058CF44, 0x0058F140 }, new int[] { SCTechDef.AllTechs.Length * IngamePlayerDef.AllIngamePlayers.Length, BWTechDef.AllTechs.Length * IngamePlayerDef.AllIngamePlayers.Length }, "researched technologies for ", TechDef.getByIndex);
        public static readonly MemoryPlace TechAvailable = _getTechUpg("Set Technologies Available", "Technologies Available (Experimental)", new uint[] { 0x0058CE24, 0x0058F050 }, new int[] { SCTechDef.AllTechs.Length * IngamePlayerDef.AllIngamePlayers.Length, BWTechDef.AllTechs.Length * IngamePlayerDef.AllIngamePlayers.Length }, "available technologies for ", TechDef.getByIndex);
        public static readonly MemoryPlace UpgradesReached = _getTechUpg("Set Upgrade Researhed Level", "Upgrades Level (Experimental)", new uint[] { 0x0058D2B0, 0x0058F32C }, new int[] { SCUpgradeDef.AllUpgrades.Length * IngamePlayerDef.AllIngamePlayers.Length, BWUpgradeDef.AllUpgrades.Length * IngamePlayerDef.AllIngamePlayers.Length }, "researched upgrade level for ", UpgradeDef.getByIndex);
        public static readonly MemoryPlace UpgradesAvailable = _getTechUpg("Set Upgrade Available Level", "Upgrades Available (Experimental)", new uint[] { 0x0058D088, 0x0058F278 }, new int[] { SCUpgradeDef.AllUpgrades.Length * IngamePlayerDef.AllIngamePlayers.Length, BWUpgradeDef.AllUpgrades.Length * IngamePlayerDef.AllIngamePlayers.Length }, "available upgrade level for ", UpgradeDef.getByIndex);

        public static readonly MemoryPlace UpgradeIcon = _get("Set Upgrade Icon", 0x00655AC0, 2, UpgradeDef.AllUpgrades.Length, "upgrade icon for ", UpgradeDef.getByIndex, UnitAvatarDef.getByIndex, true);
        public static readonly MemoryPlace UpgradeLabel = _get("Set Upgrade Label", 0x00655A40, 2, UpgradeDef.AllUpgrades.Length, "label for upgrade ", UpgradeDef.getByIndex, StatsDef.getByIndex, true);
        public static readonly MemoryPlace UpgradeGasFactor = _get("Set Upgrade Gas Factor", 0x006557C0, 2, UpgradeDef.AllUpgrades.Length, "gas factor for ", UpgradeDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace UpgradeMineralFactor = _get("Set Upgrade Mineral Factor", 0x006559C0, 2, UpgradeDef.AllUpgrades.Length, "mineral factor for ", UpgradeDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace UpgradeTimeFactorS = _get("Set Upgrade Time Factor", 0x00655940, 2, UpgradeDef.AllUpgrades.Length, "time factor for ", UpgradeDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);


        public static readonly MemoryPlace Acceleration = _get("Set Acceleration", 0x006C9C78, 2, FlingyDef.AllFlingys.Length, "acceleration for ", FlingyDef.getByIndex, (int index) => Int16Def.getByIndex(index, false), true);
        public static readonly MemoryPlace TopSpeed = _get("Set Top Speed", 0x006C9EF8, 4, FlingyDef.AllFlingys.Length, "top speed for ", FlingyDef.getByIndex, (int index) => Int32Def.getByIndex(index, false), true);
        public static readonly MemoryPlace MovementControl = _get("Set Movement Control", 0x0006C9858, 1, FlingyDef.AllFlingys.Length, "movement control for ", FlingyDef.getByIndex, MovementControlDef.getByIndex, true);
        public static readonly MemoryPlace PlayerColor = _get("Set Player Color (Unknown)", 0x0057F21C, 4, 8, "color for ", PlayerDef.getByIndex, (int index) => Int32Def.getByIndex(index, false), true);


        public static readonly MemoryPlace KeyPress = _getKey("Key Pressed", 0x00596A18, 1, KeyDef.AllKeys.Length);
        public static readonly MemoryPlace SetKills = _getSetKills("Set Kills (Experimental)", 0x005878A4, 4, UnitDef.AllUnits.Length * IngamePlayerDef.AllIngamePlayers.Length);

        public static readonly MemoryPlace FlingySpriteIndex = _get("Set Flingy Sprite Index", 0x006CA318, 2, FlingyDef.AllFlingys.Length, "sprite ID for Flingy ", FlingyDef.getByIndex, SpriteDef.getByIndex, true);
        public static readonly MemoryPlace ImageScriptID = _get("Set Image IScript ID", 0x0066EC48, 4, ImageDef.AllImages.Length, "IScript ID for Image ", ImageDef.getByIndex, IScriptDef.getByIndex, true);
        public static readonly MemoryPlace ImageDrawingFunction = _get("Set Image Drawing Function", 0x00669E28, 1, ImageDef.AllImages.Length, "remapping function for Image ", ImageDef.getByIndex, DrawingFunctionDef.getByIndex, true);


        public static readonly MemoryPlace ImageRemappingIndex = _get("Set Image Remapping Index (Experimental)", 0x00669A40, 1, ImageDef.AllImages.Length, "remapping index for ", ImageDef.getByIndex, RemappingIndexDef.getByIndex, true);
        public static readonly MemoryPlace TurnRadius = _get("Set Turn Radius (Experimental)", 0x006C9E20, 1, FlingyDef.AllFlingys.Length, "turn radius for ", FlingyDef.getByIndex, (int index) => Int32Def.getByIndex(index, false), true);


        //public static readonly MemoryPlace Latency = _get1("Set Game Latency", 0x0051CE84, 4, "game latency to ", LatencyDef.getByIndex, true);
        //public static readonly MemoryPlace CenterViewX = _get1("Set Center View X-Axis", 0x00628448, 4, "screen position of the X-axis for ", Int32Def.getByIndex, true);
        //public static readonly MemoryPlace CenterViewY = _get1("Center View Y-Axis", 0x00628470, 4, "screen position of the Y-axis for ", Int32Def.getByIndex, true);

    }

    public abstract class EPDAction : Action {

        private class ConstrucableEPDTriggerTemplate : ConstrucableTrigger {

            private StarcraftMemory.MemoryPlace _mem;

            public ConstrucableEPDTriggerTemplate(StarcraftMemory.MemoryPlace mem) {
                _mem = mem;
            }


            public TriggerContent construct() {
                RawActionSetDeaths rd = new RawActionSetDeaths(new WeakParser(RawActionSetDeaths.getComponents(), RawActionSetDeaths.getTextMapping()));
                rd.setSetQuantifier(SetQuantifier.SetTo);
                int defaultValueIndex = 0;
                int defaultValue = StarcraftMemory.getDefaultValue<int>(_mem, defaultValueIndex);
                rd.setAmmount(new IntDef(defaultValue, false)); // Default value for memory block
                rd.setPlayerID(new IntDef(((int)((((_mem.Address + (defaultValueIndex * _mem.ElementSize)) / 4) - 1452249))), true));
                rd.setUnitID(new IntDef(0, false));
                EPDAction act = _mem.construct(rd, 0);
                return act;
            }

            public string getTriggerName() {
                return _mem.NameAction;
            }
        }

        private class ConstrucableEPDGeneralTriggerTemplate : ConstrucableTrigger {

            public ConstrucableEPDGeneralTriggerTemplate() {
            }


            public TriggerContent construct() {
                RawActionSetDeaths rd = new RawActionSetDeaths(new WeakParser(RawActionSetDeaths.getComponents(), RawActionSetDeaths.getTextMapping()));
                rd.setSetQuantifier(SetQuantifier.SetTo);
                int defaultValueIndex = 0;
                rd.setAmmount(new IntDef(0, false)); // Default value for memory block
                rd.setPlayerID(new IntDef(((int)((((0 + (defaultValueIndex * 4)) / 4) - 1452249))), true));
                rd.setUnitID(new IntDef(0, false));
                EPDAction act = new EPDGeneralAction(rd);
                return act;
            }

            public string getTriggerName() {
                return "[Unsafe] Set Memory At (By Memory)";
            }
        }

        private class ConstrucableEPDTriggerTemplateUnsafe : ConstrucableTrigger {

            private StarcraftMemory.MemoryPlace _mem;

            public ConstrucableEPDTriggerTemplateUnsafe(StarcraftMemory.MemoryPlace mem) {
                _mem = mem;
            }


            public TriggerContent construct() {
                RawActionSetDeaths rd = new RawActionSetDeaths(new WeakParser(RawActionSetDeaths.getComponents(), RawActionSetDeaths.getTextMapping()));
                rd.setSetQuantifier(SetQuantifier.Add);
                int defaultValueIndex = 0;
                int defaultValue = StarcraftMemory.getDefaultValue<int>(_mem, defaultValueIndex);
                rd.setAmmount(new IntDef(defaultValue, false)); // Default value for memory block
                rd.setPlayerID(new IntDef(((int)((((_mem.Address + (defaultValueIndex * _mem.ElementSize)) / 4) - 1452249))), true));
                rd.setUnitID(new IntDef(0, false));
                EPDAction act = _mem.construct(rd, 0);
                return act;
            }

            public string getTriggerName() {
                return "[Unsafe] " + _mem.NameAction;
            }
        }

        public static void fillConstructables(List<ConstrucableTrigger> lst) {

            foreach (StarcraftMemory.MemoryPlace mem in StarcraftMemory.ActionMemories) {
                if (mem.Show) {
                    lst.Add(new ConstrucableEPDTriggerTemplate(mem));
                }
            }
            
            foreach (StarcraftMemory.MemoryPlace mem in StarcraftMemory.ActionUnsafeMemories) {
                if (mem.Show) {
                    lst.Add(new ConstrucableEPDTriggerTemplateUnsafe(mem));
                }
            }
            lst.Add(new ConstrucableEPDGeneralTriggerTemplate());
        }

        private StarcraftMemory.MemoryPlace _memory;
        private RawActionSetDeaths _trigger;

        public StarcraftMemory.MemoryPlace Memory { get { return _memory; } }

        public RawActionSetDeaths Trigger { get { return _trigger; } }

        public EPDAction(StarcraftMemory.MemoryPlace memory, RawActionSetDeaths trigger) : base(null, 0) {
            _memory = memory;
            _trigger = trigger;
        }

        public static int InvalidTriggers = 0;

        public static Action getFromDeathAction(RawActionSetDeaths deaths) {
            int playerID = deaths.getPlayerID().getIndex();
            int unitID = deaths.getUnitID().getIndex();
            if (unitID >= UnitDef.AllUnits.Length && playerID >= -12 && playerID <= 12 ) { // EUD => EPD
                playerID = (unitID * 12) + playerID;
                unitID = 0;
                deaths.setPlayerID(new IntDef((int)playerID, deaths.getPlayerID().UseHex));
                deaths.setUnitID(new IntDef((int)unitID, deaths.getUnitID().UseHex));
            }
            if(playerID < 0) { // Shitty EGD, no idea how to calculate this
                playerID = (unitID * 12) + playerID;
                unitID = 0;
                deaths.setPlayerID(new IntDef((int)playerID, deaths.getPlayerID().UseHex));
                deaths.setUnitID(new IntDef((int)unitID, deaths.getUnitID().UseHex));
            }


            int num = 1452249;
            if (unitID == 0 && (playerID > PlayerDef.AllPlayers.Length || playerID < 0)) { // EPD
                uint base_address = (uint)(4 * (playerID + num));
                StarcraftMemory.MemoryPlace memory = StarcraftMemory.findActionMemoryForAddress(base_address);
                if (memory != null) { // Woohoo!
                    int offset = ((int)base_address) - ((int)memory.Address);
                    int ObjectID = (int)(offset / memory.ElementSize);
#if !DEBUG
                    try {
#endif
                    return memory.construct(deaths, ObjectID);
#if !DEBUG
                    } catch(NotImplementedException) {
                        InvalidTriggers++;
                        return new EPDGeneralAction(deaths, "Invalid");
                    }
#endif
                }
                return new EPDGeneralAction(deaths);
            }
            return null;
        }

        public override string ToString() {
            return Trigger.ToString();
        }
    }

    public abstract class EPDCondition : Condition {

        protected StarcraftMemory.MemoryPlace Memory;
        protected ConditionMemory Trigger;

        private class ConstrucableEPDTriggerTemplate : ConstrucableTrigger {

            private StarcraftMemory.MemoryPlace _mem;

            public ConstrucableEPDTriggerTemplate(StarcraftMemory.MemoryPlace mem) {
                _mem = mem;
            }


            public TriggerContent construct() {
                ConditionMemory rd = new ConditionMemory(new WeakParser(ConditionMemory.getComponents(), ConditionMemory.getTextMapping()));
                rd.setQuantifier(Quantifier.Exactly);
                int defaultValueIndex = 0;
                int defaultValue = StarcraftMemory.getDefaultValue<int>(_mem, defaultValueIndex);
                rd.setAmount(new IntDef(defaultValue, false)); // Default value for memory block
                rd.setAddress(new IntDef((int)_mem.Address, true));
                EPDCondition cond = _mem.constructCond(rd, 0);
                return cond;
            }

            public string getTriggerName() {
                return _mem.NameCondition;
            }
        }

        public static void fillConstructables(List<ConstrucableTrigger> lst) {

            foreach (StarcraftMemory.MemoryPlace mem in StarcraftMemory.ConditionMemories) {
                if (mem.Show) {
                    lst.Add(new ConstrucableEPDTriggerTemplate(mem));
                }
            }
      
        }

        public EPDCondition(StarcraftMemory.MemoryPlace memory, ConditionMemory trigger) : base(null, 0) {
            Memory = memory;
            Trigger = trigger;
        }

        public static EPDCondition get(ConditionMemory mem) {
            uint base_address = (uint)mem.getAddress().getIndex();
            StarcraftMemory.MemoryPlace memory = StarcraftMemory.findConditionMemoryForAddress(base_address);
            if (memory != null) { // Woohoo!
                int offset = ((int)base_address) - ((int)memory.Address);
                int ObjectID = (int)(offset / memory.ElementSize);
                return memory.constructCond(mem, ObjectID);
            }
            return null;
        }

        public override string ToString() {
            return Trigger.ToString();
        }
    }

    class EPDGeneralAction : EPDAction {

        private string _unsafeLabel;

        public EPDGeneralAction(RawActionSetDeaths trigger): this(trigger, "Unsafe") { }

        public EPDGeneralAction(RawActionSetDeaths trigger, string invalidText) : base(new StarcraftMemory.MemoryPlace((uint)(4 * (trigger.getPlayerID().getIndex() + 1452249)), 4, 1, (RawActionSetDeaths trig, StarcraftMemory.MemoryPlace memory, int ObjectID) => new EPDGeneralAction(trig, invalidText)), trigger) {
            _unsafeLabel = invalidText;
        }

        private bool isHex = true;
        private IntDef idef { get { return new IntDef((int)Memory.Address, isHex); } set {

                isHex = value.UseHex;
                Memory.setAddress((uint)value.getIndex());
                int finalAddress = (int) Memory.Address;
                int playerID = (finalAddress / 4) - 1452249;
                Trigger.setPlayerID(new IntDef(playerID, Trigger.getPlayerID().UseHex));
                redraw();
            } }

        protected void redraw() {
            if (_trgs != null) {
                foreach (TriggerDefinitionPart part in _trgs) {
                    part.ValueChanged(false);
                }
            }
        }

        private TriggerDefinitionPart[] _trgs;

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            _trgs = new TriggerDefinitionPart[] {
                new TriggerDefinitionLabel("["+_unsafeLabel+"] ", 0xff0000),
                new TriggerDefinitionLabel("Modify memory at "),
                new TriggerDefinitionIntAmount(()=>idef, (IntDef obj)=> {idef=obj; }, ()=>new IntDef(0, isHex)).setEditable(true),
                //new TriggerDefinitionLabel(" (Player ID "+Trigger.getPlayerID()+") "),
                new TriggerDefinitionLabel(" "),
                new TriggerDefinitionGeneralDef<SetQuantifier>(()=>Trigger.getSetQuantifier(), (SetQuantifier tmp) => { Trigger.setSetQuantifier(tmp); }, SetQuantifier.getDefaultValue, SetQuantifier.AllQuantifiers),
                new TriggerDefinitionLabel(" "),
                new TriggerDefinitionIntAmount(()=>Trigger.getAmount(), (IntDef obj)=> { Trigger.setAmmount(obj); },()=>new IntDef(0, Trigger.getAmount().UseHex)),
                new TriggerDefinitionLabel("."),
            };
            return _trgs;
        }
    }

    public interface Gettable<T> {

        int getMaxValue();

        int getIndex();

        TriggerDefinitionPart getTriggerPart(Func<T> getter, Action<T> setter, Func<T> getDefault);

    }

    public interface GettableImage {

        BitmapImageX getImage();

    }

    public interface TechOrUpgradeDef<T> :Gettable<T> {

        int getMemoryIndex();

        int getFirstPartLength();
    }

    class LengthtyEPDAddSubActionStarcraftMemory<TARGET_TYPE, VALUE_TYPE> : EPDAction where TARGET_TYPE : Gettable<TARGET_TYPE> where VALUE_TYPE : Gettable<VALUE_TYPE> {

        private string _settingWhat;
        private string _settingWhat2;

        private Func<int, TARGET_TYPE> _typeGetter;
        private Func<int, VALUE_TYPE> _valueGetter;

        private TARGET_TYPE[] _targets;
        private VALUE_TYPE[] _values;
        private VALUE_TYPE[] _baseValues;

        private StarcraftMemory.MemoryPlace _memory;

        private TriggerDefinitionPart _lastGlow;

        public override string ToString() {
            int factor = Trigger.getSetQuantifier() == SetQuantifier.Add ? 1 : -1;
            int[] values = new int[_values.Length];
            for (int i = 0; i < values.Length; i++) {
                int baseVal = _baseValues[i].getIndex();
                int val = _values[i].getIndex();
                if (val == baseVal) {
                    values[i] = 0;
                } else if (val > baseVal) {
                    if (factor != 1) {
                        throw new NotImplementedException();
                    }
                    values[i] = val - baseVal;
                } else { // baseVal > val
                    if (factor != -1) {
                        throw new NotImplementedException();
                    }
                    values[i] = baseVal - val;
                }
            }
            int resultValue = 0;
            if (values.Length == 1) {
                resultValue = (values[0] << 0);
            } else if (values.Length == 2) {
                resultValue = (values[1] << 16) | (values[0] << 0);
            } else if (values.Length == 4) {
                resultValue = (values[3] << 24) | (values[2] << 16) | (values[1] << 8) | (values[0] << 0);
            } else {
                throw new NotImplementedException();
            }
            int objectID = _targets[0].getIndex();
            int baseAddress = (int)Memory.Address;
            int elementSize = (int)Memory.ElementSize;
            int finalAddress = baseAddress + (elementSize * objectID);
            int playerID = (finalAddress / 4) - 1452249;
            Trigger.setPlayerID(new IntDef(playerID, Trigger.getPlayerID().UseHex));
            Trigger.setAmmount(new IntDef(resultValue, Trigger.getAmount().UseHex));
            return base.ToString();
        }

        protected VALUE_TYPE getDefault(TARGET_TYPE value) {
            int offset = value.getIndex();
            int defaultValue = StarcraftMemory.getDefaultValue<int>(_memory, offset);
            return _valueGetter(defaultValue);
        }

        private int ObjectID;

        public LengthtyEPDAddSubActionStarcraftMemory(StarcraftMemory.MemoryPlace memory, int ObjectID, RawActionSetDeaths trigger, string settingWhat, string settingsWhat2, Func<int, TARGET_TYPE> typeGetter, Func<int, VALUE_TYPE> valueGetter) : base(memory, trigger) {
            this.ObjectID = ObjectID;
            _memory = memory;
            _settingWhat = settingWhat;
            _settingWhat2 = settingsWhat2;
            _typeGetter = typeGetter;
            _valueGetter = valueGetter;
            int size = (int)memory.ElementSize;
            int value = trigger.getAmount().getIndex();
            List<int> values = new List<int>();
            if (size == 1) {
                values.Add(value & 0xff);
                values.Add((value & 0xff00) >> 8);
                values.Add((value & 0xff0000) >> 16);
                values.Add((int)((value & 0xff000000) >> 24));
            } else if (size == 2) {
                values.Add(value & 0xffff);
                values.Add((int)((value & 0xffff0000) >> 16));
            } else if (size == 4) {
                values.Add(value);
            } else {
                throw new NotImplementedException();
            }
            size = values.Count;
            _targets = new TARGET_TYPE[size];
            _values = new VALUE_TYPE[size];
            _baseValues = new VALUE_TYPE[size];
            int factor = trigger.getSetQuantifier() == SetQuantifier.Add ? 1 : -1;
            for (int i = 0; i < values.Count; i++) {
                _targets[i] = _typeGetter(ObjectID + i);
                VALUE_TYPE defaultValueT = getDefault(_targets[i]);
                int defaultValue = defaultValueT.getIndex();
                int maxValue = defaultValueT.getMaxValue();

                values[i] *= factor;
                if (values[i] + defaultValue < 0) {
                    defaultValue -= values[i];
                } else if (values[i] + defaultValue > maxValue) {
                    defaultValue = maxValue - values[i];
                }
                if (defaultValue < 0 || defaultValue > maxValue) {
                    string details = "Default value (" + defaultValue + ") for memory address at " + memory.Address + " is " + (defaultValue < 0 ? "< 0" : "> " + maxValue) + " which is not allowed.";
                    MessageBoxResult rs = MessageBox.Show("Invalid EUD trigger found!\nProceeding with this value will cause starcraft to crash.\nCorrect the value?\n(Select \"YES\" to correct the value or \"No\" to exit.\nDetails: "+details, "Trigger Editor", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (rs == MessageBoxResult.No) {
                        throw new NotImplementedException();
                    } else {
                        defaultValue = defaultValueT.getIndex();
                        value = defaultValue;
                    }
                }
                _baseValues[i] = _valueGetter(defaultValue);
                _values[i] = _valueGetter(defaultValue + values[i]);
            }
        }

        private void recalculateFromCurrentValue(int index, VALUE_TYPE obj) {
            int factor = Trigger.getSetQuantifier() == SetQuantifier.Add ? 1 : -1;
            int baseValue = _baseValues[index].getIndex();
            int max = _values[index].getMaxValue();
            int newBaseValue = obj.getIndex();
            int value = _values[index].getIndex();
            if (baseValue > value) {
                int diffValue = baseValue - value;
                if (newBaseValue < diffValue) {
                    newBaseValue = diffValue;
                }
                value = newBaseValue - diffValue;
            } else {
                int diffValue = value - baseValue;
                if (newBaseValue + diffValue > max) {
                    newBaseValue = max - diffValue - 1;
                }
                value = newBaseValue + diffValue;
            }
            _baseValues[index] = _valueGetter(newBaseValue);
            _values[index] = _valueGetter(value);
            redraw();
        }

        private void recalculateFromTarget(int index, TARGET_TYPE value) {
            TARGET_TYPE target = value;
            int targetBase = Memory.getAlignedID(target.getIndex());
            int changeOffset = target.getIndex() - targetBase;
            bool hasChanged = changeOffset != 0;
            for (int i = 0; i < _targets.Length; i++) {
                _targets[i] = _typeGetter(targetBase + i);
            }
            if (hasChanged) {
                int glowIndex = index + changeOffset;
                if (glowIndex == 1) {
                    glowIndex = 11;
                } else if (glowIndex == 2) {
                    glowIndex = 20;
                } else if (glowIndex == 3) {
                    glowIndex = 29;
                } else {
                    throw new NotImplementedException();
                }
                TriggerDefinitionPart glowTarget = _trgs[glowIndex];
                if (_lastGlow != null) {
                    _lastGlow.unglow();
                }
                _lastGlow = glowTarget;
                _lastGlow.glow();
            } else if (_lastGlow != null) {
                _lastGlow.unglow();
                _lastGlow = null;
            }
            redraw();
        }

        private void requestValueChange(int index, VALUE_TYPE obj) {
            int factor = Trigger.getSetQuantifier() == SetQuantifier.Add ? 1 : -1;
            int baseValue = _baseValues[index].getIndex();
            int value = obj.getIndex();
            if (baseValue > value && factor == 1) { // Changing from "add" to "sub"
                bool allGood = true;
                for (int i = 0; i < _values.Length; i++) {
                    if (i != index) {
                        if (_values[i].getIndex() != _baseValues[i].getIndex()) {
                            allGood = false;
                        }
                    }
                }
                if (!allGood) {
                    MessageBoxResult rs = MessageBox.Show("Changing the value will most likely mess other values up (Add->Sub)\n(Select \"YES\" to commit the change or \"No\" to cancel.", "Trigger Editor", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (rs == MessageBoxResult.No) {
                        return;
                    }
                }
                Trigger.setSetQuantifier(SetQuantifier.Subtract);
                for (int i = 0; i < _values.Length; i++) { // base+val -> base-val
                    int bas = _baseValues[i].getIndex();
                    int valDiff = _values[i].getIndex() - bas; // Val difference
                    int max = _values[i].getMaxValue() - 1;
                    int val = bas - valDiff; // New val
                    if (val < 0) {
                        bas = valDiff;
                        val = 0;
                    }
                    if (val > max || bas > max) {
                        bas = max - (bas - val);
                        val = max;
                    }
                    _baseValues[i] = _valueGetter(bas);
                    _values[i] = _valueGetter(val);
                }
                _values[index] = obj;
            } else if (baseValue < value && factor == -1) { // Changing from "sub" to "add"
                bool allGood = true;
                for (int i = 0; i < _values.Length; i++) {
                    if (i != index) {
                        if (_values[i].getIndex() != _baseValues[i].getIndex()) {
                            allGood = false;
                        }
                    }
                }
                if (!allGood) {
                    MessageBoxResult rs = MessageBox.Show("Changing the value will most likely mess other values up (Sub->Add)\n(Select \"YES\" to commit the change or \"No\" to cancel.", "Trigger Editor", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (rs == MessageBoxResult.No) {
                        return;
                    }
                }
                Trigger.setSetQuantifier(SetQuantifier.Add);
                for (int i = 0; i < _values.Length; i++) { // base-val => base+val
                    int bas = _baseValues[i].getIndex();
                    int valDiff = bas - _values[i].getIndex(); // Val diff
                    int max = _values[i].getMaxValue() - 1;
                    int val = bas + valDiff; // Val new value
                    if (val < 0) {
                        bas = valDiff;
                        val = 0;
                    }
                    if (bas + valDiff > max) {
                        bas = max - valDiff;
                        val = max;
                    }
                    _baseValues[i] = _valueGetter(bas);
                    _values[i] = _valueGetter(val);
                }
                _values[index] = obj;
            } else if (factor == 1) { // "Add->Add", simply add the current value
                _values[index] = obj;

            } else if (factor == -1) { // "Sub"->"Sub", simply sub the current value
                _values[index] = obj;
            }
            redraw();
        }

        private void redraw() {
            if (_trgs != null) {
                foreach (TriggerDefinitionPart part in _trgs) {
                    part.ValueChanged(false);
                }
            }
        }

        private TriggerDefinitionPart[] _trgs;

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            List<TriggerDefinitionPart> lst = new List<TriggerDefinitionPart>();
            for (int i = 0; i < _targets.Length; i++) {
                int ii = i;
                lst.Add(new TriggerDefinitionLabel("[Unsafe]", 0xff0000));
                lst.Add(new TriggerDefinitionLabel(" Given current value of " + _settingWhat + " "));
                lst.Add(_targets[0].getTriggerPart(() => _targets[ii], (TARGET_TYPE obj) => { recalculateFromTarget(ii, obj); }, () => { throw new NotImplementedException(); }).setEditable(i == 0));
                lst.Add(new TriggerDefinitionLabel(" "));
                lst.Add(_values[0].getTriggerPart(() => _baseValues[ii], (VALUE_TYPE obj) => { recalculateFromCurrentValue(ii, obj); }, () => getDefault(_targets[ii])).SetResetable(true));
                lst.Add(new TriggerDefinitionLabel(", change this value to "));
                lst.Add(_values[0].getTriggerPart(() => _values[ii], (VALUE_TYPE obj) => { requestValueChange(ii, obj); }, () => getDefault(_targets[ii])).SetResetable(true));
                lst.Add(new TriggerDefinitionLabel("."));
                if (i != _targets.Length - 1) {
                    lst.Add(new TriggerDefinitionNewLine());
                }
            }
            _trgs = new TriggerDefinitionPart[lst.Count];
            for (int i = 0; i < _trgs.Length; i++) {
                _trgs[i] = lst[i];
            }
            return _trgs;
        }
    }

    class LenghtyEPDAction_UpgradesTech<TYPE>: EPDAction where TYPE : TechOrUpgradeDef<TYPE> {

        protected string _settingWhat;
        protected string _settingWhat2;

        protected Func<int, TYPE> _typeGetter;
        protected Func<int, Int8Def> _valueGetter;

        protected TechOrUpgradeDef<TYPE>[] _targets;
        protected IngamePlayerDef _player;
        protected Int8Def[] _values;

        protected uint[] _addresses;

        private TriggerDefinitionPart _lastGlow;

        public LenghtyEPDAction_UpgradesTech(uint[] addresses, int addressIndex, int ObjectID, RawActionSetDeaths trigger, string settingWhat, string settingsWhat2, Func<int, TYPE> typeGetter) : base(null, trigger) {
            _addresses = addresses;
            _settingWhat = settingWhat;
            _settingWhat2 = settingsWhat2;
            _valueGetter = (int index) => Int8Def.getByIndex(index, false);
            _typeGetter = typeGetter;
            int am = trigger.getAmount().getIndex();
            _values = new Int8Def[4];
            _targets = new TechOrUpgradeDef<TYPE>[4];
            int realAddress = (int) addresses[addressIndex];
            int max = typeGetter(0).getMaxValue();
            int off = typeGetter(0).getFirstPartLength();
            if (addressIndex == 1) {
                max = max - off;
            } else {
                max = off;
            }
            int playerID = ObjectID / (max+1);
            ObjectID = ObjectID % (max+1);
            if (addressIndex == 1) {
                ObjectID += off;
            }
            _targets[0] = typeGetter(ObjectID);
            _player = IngamePlayerDef.getByIndex(playerID);
            _values[3] = _valueGetter((int)((am >> 24) & 0xff));
            _values[2] = _valueGetter((int)((am >> 16) & 0xff));
            _values[1] = _valueGetter((int)((am >> 8) & 0xff));
            _values[0] = _valueGetter((int)((am >> 0) & 0xff));
            recalculateOthers();
        }

        public override string ToString() {
            int[] values = new int[_values.Length];
            for (int i = 0; i < values.Length; i++) {
                values[i] = _values[i].getIndex();
            }
            int resultValue = 0;
            resultValue = (values[3] << 24) | (values[2] << 16) | (values[1] << 8) | (values[0] << 0);
            int memi = _targets[0].getMemoryIndex();
            int objectID = _targets[0].getIndex();
            int max = _targets[0].getFirstPartLength();
            if (memi == 1) {
                objectID -= _targets[0].getFirstPartLength(); // Local index to that address
                max = _targets[0].getMaxValue() - max;
            }
            
            int baseAddress = (int)_addresses[memi];
            int elementSize = 1;

            objectID += (max+1) * _player.getIndex();

            int finalAddress = baseAddress + (elementSize * objectID);
            int playerID = (finalAddress / 4) - 1452249;
            Trigger.setPlayerID(new IntDef(playerID, Trigger.getPlayerID().UseHex));
            Trigger.setAmmount(new IntDef(resultValue, Trigger.getAmount().UseHex));
            return base.ToString();
        }

        private void recalculateFromTarget(int index, TYPE value) {
            if(index != 0) {
                throw new NotImplementedException();
            }
            uint addr = (uint) _addresses[value.getMemoryIndex()];
            index = value.getIndex();
            if(value.getMemoryIndex() == 1) {
                index -= value.getFirstPartLength(); // Local index to that address
            }

            int newValue = StarcraftMemory.MemoryPlace.getAlignedID(addr, 1, index);
            int changeOffset = index - newValue;
            bool hasChanged = changeOffset != 0;
            if (value.getMemoryIndex() == 1) {
                newValue += value.getFirstPartLength(); // Local index to that address
            }
            _targets[0] = _typeGetter(newValue);
            recalculateOthers();
            if (hasChanged) {
                int glowIndex = changeOffset;
                if (glowIndex == 1) {
                    glowIndex = 11;
                } else if (glowIndex == 2) {
                    glowIndex = 18;
                } else if (glowIndex == 3) {
                    glowIndex = 25;
                } else {
                    throw new NotImplementedException();
                }
                TriggerDefinitionPart glowTarget = _trgs[glowIndex];
                if (_lastGlow != null) {
                    _lastGlow.unglow();
                }
                _lastGlow = glowTarget;
                _lastGlow.glow();
            } else if (_lastGlow != null) {
                _lastGlow.unglow();
                _lastGlow = null;
            }
            resetValuesIfDefaultIsKept();
            redraw();
        }

        protected void redraw() {
            if (_trgs != null) {
                foreach (TriggerDefinitionPart part in _trgs) {
                    part.ValueChanged(false);
                }
            }
        }

        private void resetValuesIfDefaultIsKept() {
            if (_trgs != null) {
                TriggerDefinitionPart part = _trgs[0];
                if (part.isDefaultKept()) {
                    for (int i = 0; i < _values.Length; i++) {
                        Int8Def value = _values[i];
                        TYPE target = (TYPE) _targets[i];
                        Int8Def def = getDefault(target, value.UseHex);
                        if (target.getIndex() != value.getIndex()) {
                            _values[i] = def;
                        }
                    }
                }
            }
        }

        private void recalculateOthers() {
            int index = _targets[0].getIndex();
            for (int i = 1; i < _targets.Length; i++) {
                _targets[i] = _typeGetter(_targets[0].getIndex() + i);
            }
            if (_trgs != null) {
                for (int i = 0; i < _trgs.Length; i++) {
                    _trgs[i].ValueChanged(false);
                }
            }
            resetValuesIfDefaultIsKept();
            redraw();
        }

        protected TriggerDefinitionPart[] _trgs;

        protected Int8Def getDefault(TYPE value, bool usehex) {
            uint addr = (uint)_addresses[value.getMemoryIndex()];
            int index = value.getIndex();
            if (value.getMemoryIndex() == 1) {
                index -= value.getFirstPartLength(); // Local index to that address
            }
            int defaultValue = StarcraftMemory.getDefaultValue<int>(addr, 1, index);
            return new Int8Def(defaultValue, usehex);
        }

        private void updatePlayers(IngamePlayerDef pl) {
            _player = pl;
            redraw();
        }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            int len = 9 + (3 * 7);
            TriggerDefinitionPart[] trgs = new TriggerDefinitionPart[len];
            _trgs = trgs;
            int trgI = 0;
            trgs[trgI++] = new TriggerDefinitionLabel("Change "+_settingWhat2);
            trgs[trgI++] = _targets[0].getTriggerPart(() => (TYPE) _targets[0], (TYPE obj) => { recalculateFromTarget(0, obj); recalculateOthers(); }, () => { throw new NotImplementedException(); }).setRegenerateAfterChange((bool b) => recalculateOthers());
            trgs[trgI++] = new TriggerDefinitionLabel(" for ");
            trgs[trgI++] = _player.getTriggerPart(()=>_player, (IngamePlayerDef pl)=> { _player = pl; updatePlayers(pl);  },()=>IngamePlayerDef.AllIngamePlayers[0]);
            trgs[trgI++] = new TriggerDefinitionLabel(" to ");

            trgs[trgI++] = _values[0].getTriggerPart(() => _values[0], (Int8Def obj) => { _values[0] = obj; }, () => getDefault((TYPE)_targets[0], _values[0].UseHex)).SetResetable(true);
            trgs[trgI++] = new TriggerDefinitionLabel(".");
            trgs[trgI++] = new TriggerDefinitionNewLine();
            trgs[trgI++] = new TriggerDefinitionLabel("Which subsequently changes also " + _settingWhat2);
            for (int i = 1; i < _targets.Length; i++) {
                int ii = i; // Scope guard
                int mi = _targets.Length - 1;
                trgs[trgI++] = new TriggerDefinitionNewLine();
                trgs[trgI++] = new TriggerDefinitionLabel("\t");
                trgs[trgI++] = _targets[ii].getTriggerPart(() => (TYPE) _targets[ii], (TYPE obj) => { _targets[ii] = obj; }, () => { throw new NotImplementedException(); }).setEditable(false);
                trgs[trgI++] = new TriggerDefinitionLabel(" for ");
                trgs[trgI++] = _player.getTriggerPart(() => _player, (IngamePlayerDef pl) => { _player = pl; updatePlayers(pl); }, () => IngamePlayerDef.AllIngamePlayers[0]);
                trgs[trgI++] = new TriggerDefinitionLabel(" to ");
                trgs[trgI++] = _values[ii].getTriggerPart(() => _values[ii], (Int8Def obj) => { _values[ii] = obj; }, () => getDefault((TYPE)_targets[ii], _values[ii].UseHex)).SetResetable(true);
            }
            return trgs;
        }

    }

    class LenghtyEPDCondition_UpgradesTech<TYPE> : EPDCondition where TYPE : TechOrUpgradeDef<TYPE> {

        protected string _settingWhat;
        protected string _settingWhat2;

        protected Func<int, TYPE> _typeGetter;
        protected Func<int, Int8Def> _valueGetter;

        protected TechOrUpgradeDef<TYPE>[] _targets;
        protected IngamePlayerDef _player;
        protected Int8Def[] _values;

        protected uint[] _addresses;

        private TriggerDefinitionPart _lastGlow;

        public LenghtyEPDCondition_UpgradesTech(uint[] addresses, int addressIndex, int ObjectID, ConditionMemory trigger, string settingWhat, string settingsWhat2, Func<int, TYPE> typeGetter) : base(null, trigger) {
            if(trigger.getQuantifier() != Quantifier.Exactly) {
                throw new NotImplementedException();
            }

            _addresses = addresses;
            _settingWhat = settingWhat;
            _settingWhat2 = settingsWhat2;
            _valueGetter = (int index) => Int8Def.getByIndex(index, false);
            _typeGetter = typeGetter;
            int am = trigger.getAmount().getIndex();
            _values = new Int8Def[4];
            _targets = new TechOrUpgradeDef<TYPE>[4];
            int realAddress = (int)addresses[addressIndex];
            int max = typeGetter(0).getMaxValue();
            int off = typeGetter(0).getFirstPartLength();
            if (addressIndex == 1) {
                max = max - off;
            } else {
                max = off;
            }
            int playerID = ObjectID / (max+1);
            ObjectID = ObjectID % (max+1);
            if (addressIndex == 1) {
                ObjectID += off;
            }
            _targets[0] = typeGetter(ObjectID);
            _player = IngamePlayerDef.getByIndex(playerID);
            _values[3] = _valueGetter((int)((am >> 24) & 0xff));
            _values[2] = _valueGetter((int)((am >> 16) & 0xff));
            _values[1] = _valueGetter((int)((am >> 8) & 0xff));
            _values[0] = _valueGetter((int)((am >> 0) & 0xff));
            recalculateOthers();
        }

        public override string ToString() {
            int[] values = new int[_values.Length];
            for (int i = 0; i < values.Length; i++) {
                values[i] = _values[i].getIndex();
            }
            int resultValue = 0;
            resultValue = (values[3] << 24) | (values[2] << 16) | (values[1] << 8) | (values[0] << 0);
            int memi = _targets[0].getMemoryIndex();
            int objectID = _targets[0].getIndex();
            int max = _targets[0].getFirstPartLength();
            if (memi == 1) {
                objectID -= _targets[0].getFirstPartLength(); // Local index to that address
                max = _targets[0].getMaxValue() - max;
            }

            int baseAddress = (int)_addresses[memi];
            int elementSize = 1;
            objectID += (max+1) * _player.getIndex();
            int finalAddress = baseAddress + (elementSize * objectID);

            Trigger.setAddress(new IntDef(finalAddress, Trigger.getAddress().UseHex));
            Trigger.setAmount(new IntDef(resultValue, Trigger.getAmount().UseHex));
            return base.ToString();
        }

        private void recalculateFromTarget(int index, TYPE value) {
            if (index != 0) {
                throw new NotImplementedException();
            }
            uint addr = (uint)_addresses[value.getMemoryIndex()];
            index = value.getIndex();
            if (value.getMemoryIndex() == 1) {
                index -= value.getFirstPartLength(); // Local index to that address
            }

            int newValue = StarcraftMemory.MemoryPlace.getAlignedID(addr, 1, index);
            int changeOffset = index - newValue;
            bool hasChanged = changeOffset != 0;
            if (value.getMemoryIndex() == 1) {
                newValue += value.getFirstPartLength(); // Local index to that address
            }
            _targets[0] = _typeGetter(newValue);
            recalculateOthers();
            if (hasChanged) {
                int glowIndex = changeOffset;
                if (glowIndex == 1) {
                    glowIndex = 11;
                } else if (glowIndex == 2) {
                    glowIndex = 17;
                } else if (glowIndex == 3) {
                    glowIndex = 23;
                } else {
                    throw new NotImplementedException();
                }
                TriggerDefinitionPart glowTarget = _trgs[glowIndex];
                if (_lastGlow != null) {
                    _lastGlow.unglow();
                }
                _lastGlow = glowTarget;
                _lastGlow.glow();
            } else if (_lastGlow != null) {
                _lastGlow.unglow();
                _lastGlow = null;
            }
            resetValuesIfDefaultIsKept();
            redraw();
        }

        protected void redraw() {
            if (_trgs != null) {
                foreach (TriggerDefinitionPart part in _trgs) {
                    part.ValueChanged(false);
                }
            }
        }

        private void resetValuesIfDefaultIsKept() {
            if (_trgs != null) {
                TriggerDefinitionPart part = _trgs[0];
                if (part.isDefaultKept()) {
                    for (int i = 0; i < _values.Length; i++) {
                        Int8Def value = _values[i];
                        TYPE target = (TYPE)_targets[i];
                        Int8Def def = getDefault(target, value.UseHex);
                        if (target.getIndex() != value.getIndex()) {
                            _values[i] = def;
                        }
                    }
                }
            }
        }

        private void recalculateOthers() {
            int index = _targets[0].getIndex();
            for (int i = 1; i < _targets.Length; i++) {
                _targets[i] = _typeGetter(_targets[0].getIndex() + i);
            }
            if (_trgs != null) {
                for (int i = 0; i < _trgs.Length; i++) {
                    _trgs[i].ValueChanged(false);
                }
            }
            resetValuesIfDefaultIsKept();
            redraw();
        }

        protected TriggerDefinitionPart[] _trgs;

        protected Int8Def getDefault(TYPE value, bool usehex) {
            uint addr = (uint)_addresses[value.getMemoryIndex()];
            int index = value.getIndex();
            if (value.getMemoryIndex() == 1) {
                index -= value.getFirstPartLength(); // Local index to that address
            }
            int defaultValue = StarcraftMemory.getDefaultValue<int>(addr, 1, index);
            return new Int8Def(defaultValue, usehex);
        }

        private void updatePlayers(IngamePlayerDef pl) {
            _player = pl;
            redraw();
        }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            int len = 9 + ((_targets.Length - 1) * 6);
            TriggerDefinitionPart[] trgs = new TriggerDefinitionPart[len];
            int trgi = 0;
            trgs[trgi++] = new TriggerDefinitionLabel("Player ");
            trgs[trgi++] = _player.getTriggerPart(() => _player, (IngamePlayerDef pl) => { _player = pl; updatePlayers(pl); }, () => IngamePlayerDef.AllIngamePlayers[0]);
            trgs[trgi++] = new TriggerDefinitionLabel(" has following " + _settingWhat2 + ":");
            trgs[trgi++] = new TriggerDefinitionNewLine();
            trgs[trgi++] = new TriggerDefinitionLabel("\t");
            trgs[trgi++] = _targets[0].getTriggerPart(() => (TYPE)_targets[0], (TYPE obj) => { recalculateFromTarget(0, obj); recalculateOthers(); }, () => { throw new NotImplementedException(); }).setRegenerateAfterChange((bool b) => recalculateOthers());
            trgs[trgi++] = new TriggerDefinitionLabel(" at level ");
            trgs[trgi++] = _values[0].getTriggerPart(() => _values[0], (Int8Def obj) => { _values[0] = obj; }, () => getDefault((TYPE)_targets[0], _values[0].UseHex)).SetResetable(true);
            trgs[trgi++] = new TriggerDefinitionLabel(".");
 
            for (int i = 1; i < _targets.Length; i++) {
                int ii = i; // Scope guard
                int mi = _targets.Length - 1;
                trgs[trgi++] = new TriggerDefinitionNewLine();
                trgs[trgi++] = new TriggerDefinitionLabel("\t");
                trgs[trgi++] = _targets[ii].getTriggerPart(() => (TYPE)_targets[ii], (TYPE obj) => { _targets[ii] = obj; }, () => { throw new NotImplementedException(); }).setEditable(false);
                trgs[trgi++] = new TriggerDefinitionLabel(" at level ");
                trgs[trgi++] = _values[ii].getTriggerPart(() => _values[ii], (Int8Def obj) => { _values[ii] = obj; }, () => getDefault((TYPE)_targets[ii], _values[ii].UseHex)).SetResetable(true);
                trgs[trgi++] =new TriggerDefinitionLabel(".");
            }
            _trgs = trgs;
            return trgs;
        }

    }

    class LengthyEPDCondition_KeyPress : EPDCondition {

        private KeyDef[] keys;
        private bool[] isPressed;
        private TriggerDefinitionPart[] _trgs;

        public LengthyEPDCondition_KeyPress(StarcraftMemory.MemoryPlace memory, int ObjectID, ConditionMemory trigger) : base(memory, trigger) {
            keys = new KeyDef[4];
            isPressed = new bool[keys.Length];
            int val = trigger.getAmount().getIndex();
            isPressed[0] = (val & 0xff) > 0;
            isPressed[1] = (val & 0xff00) > 0;
            isPressed[2] = (val & 0xff0000) > 0;
            isPressed[3] = (val & 0xff000000) > 0;
            recalculate(KeyDef.getByIndex(ObjectID));
        }

        private TriggerDefinitionPart _lastGlow = null;

        public override string ToString() {
            int[] values = new int[isPressed.Length];
            for (int i = 0; i < values.Length; i++) {
                values[i] = isPressed[i] ? 1 : 0;
            }
            int resultValue = 0;
            resultValue = (values[3] << 24) | (values[2] << 16) | (values[1] << 8) | (values[0] << 0);
            int objectID = keys[0].getIndex();
            int baseAddress = (int)Memory.Address;
            int elementSize = 1;
            int finalAddress = baseAddress + (elementSize * objectID);
            Trigger.setAddress(new IntDef(finalAddress, Trigger.getAddress().UseHex));
            Trigger.setAmount(new IntDef(resultValue, Trigger.getAmount().UseHex));
            return base.ToString();
        }

        protected void redraw() {
            if (_trgs != null) {
                foreach (TriggerDefinitionPart part in _trgs) {
                    part.ValueChanged(false);
                }
            }
        }

        private void recalculate(KeyDef newFirst) {
            KeyDef first = newFirst;
            int index = first.getIndex();
            int newIndex = Memory.getAlignedID(index);
            keys[0] = KeyDef.getByIndex(newIndex);
            keys[1] = KeyDef.getByIndex(newIndex + 1);
            keys[2] = KeyDef.getByIndex(newIndex + 2);
            keys[3] = KeyDef.getByIndex(newIndex + 3);
            if (index != newIndex) {
                int glowIndex = index - newIndex;
                if (glowIndex == 1) {
                    glowIndex = 8;
                } else if (glowIndex == 2) {
                    glowIndex = 13;
                } else if (glowIndex == 3) {
                    glowIndex = 18;
                } else {
                    throw new NotImplementedException();
                }
                TriggerDefinitionPart glowTarget = _trgs[glowIndex];
                if (_lastGlow != null) {
                    _lastGlow.unglow();
                }
                _lastGlow = glowTarget;
                _lastGlow.glow();
            } else if (_lastGlow != null) {
                _lastGlow.unglow();
                _lastGlow = null;
            }
            redraw();
        }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            _trgs = new TriggerDefinitionPart[] {
                new TriggerDefinitionLabel("Current player is holding following keys:"),
                new TriggerDefinitionNewLine(),
                new TriggerDefinitionLabel("\t"),
                keys[0].getTriggerPart(()=>keys[0], (KeyDef set)=> { keys[0]=set; recalculate(set); }, ()=>KeyDef.getByIndex(0)).setEditable(true),
                new TriggerDefinitionLabel(" is pressed: "),
                new TriggerDefinitionResetableCheckbox(()=>isPressed[0], (bool p)=> { isPressed[0]=p; }, ()=>false).setEditable(true).SetResetable(true),

                new TriggerDefinitionNewLine(),
                new TriggerDefinitionLabel("\t"),
                keys[0].getTriggerPart(()=>keys[1], (KeyDef set)=>keys[1]=set, ()=>KeyDef.getByIndex(0)).setEditable(false),
                new TriggerDefinitionLabel(" is pressed: "),
                new TriggerDefinitionResetableCheckbox(()=>isPressed[1], (bool p)=> { isPressed[1]=p; }, ()=>false).setEditable(true).SetResetable(true),

                new TriggerDefinitionNewLine(),
                new TriggerDefinitionLabel("\t"),
                keys[0].getTriggerPart(()=>keys[2], (KeyDef set)=>keys[2]=set, ()=>KeyDef.getByIndex(0)).setEditable(false),
                new TriggerDefinitionLabel(" is pressed: "),
                new TriggerDefinitionResetableCheckbox(()=>isPressed[2], (bool p)=> { isPressed[2]=p; }, ()=>false).setEditable(true).SetResetable(true),

                new TriggerDefinitionNewLine(),
                new TriggerDefinitionLabel("\t"),
                keys[0].getTriggerPart(()=>keys[3], (KeyDef set)=>keys[3]=set, ()=>KeyDef.getByIndex(0)).setEditable(false),
                new TriggerDefinitionLabel(" is pressed: "),
                new TriggerDefinitionResetableCheckbox(()=>isPressed[3], (bool p)=> { isPressed[3]=p; }, ()=>false).setEditable(true).SetResetable(true),

            };
            return _trgs;
        }
    }

    class LengthEPPAction_SetPlayerColor : EPDAction {

        public LengthEPPAction_SetPlayerColor(StarcraftMemory.MemoryPlace memory, RawActionSetDeaths trigger, int ObjectID) : base(memory, trigger) {
            int playerID = ObjectID;
            int colorID = (trigger.getAmount().getIndex() >> 16) & 0xff;
            _pl = IngamePlayerDef.getByIndex(playerID);
            _cl = PlayerColorDef.getByIndex(colorID);
        }

        private IngamePlayerDef _pl;
        private PlayerColorDef _cl;

        public override string ToString() {
            int baseAddress = (int)Memory.Address;
            int elementSize = (int)Memory.ElementSize;
            int objectID = _pl.getIndex();
            int resultValue = _cl.getIndex() << 16;
            int finalAddress = baseAddress + (elementSize * objectID);
            int playerID = (finalAddress / 4) - 1452249;
            Trigger.setPlayerID(new IntDef(playerID, Trigger.getPlayerID().UseHex));
            Trigger.setAmmount(new IntDef(resultValue, Trigger.getAmount().UseHex));
            Trigger.setSetQuantifier(SetQuantifier.SetTo);
            return base.ToString();
        }

        private PlayerColorDef getDefault() {
            return PlayerColorDef.getByPlayerIndex(_pl.getIndex());
        }

        private TriggerDefinitionPart[] _trgs;

        private void redraw() {
            if(_trgs != null) {
                foreach(TriggerDefinitionPart part in _trgs) {
                    part.ValueChanged(false);
                }
            }
        }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            _trgs = new TriggerDefinitionPart[] {
                new TriggerDefinitionLabel("Set color for "),
                new TriggerDefinitionGeneralDef<IngamePlayerDef>(()=>_pl, (IngamePlayerDef pl)=> { _pl=pl;redraw(); }, ()=>IngamePlayerDef.AllIngamePlayers[0], IngamePlayerDef.AllIngamePlayers).setEditable(true),
                new TriggerDefinitionLabel(" to "),
                _cl.getTriggerPart(()=>_cl,(PlayerColorDef def)=>_cl=def, getDefault).setEditable(true).SetResetable(true),
                new TriggerDefinitionLabel("."),
            };
            return _trgs;
        }
    }

    class LengthEPPAction_SetKills : EPDAction {

        public LengthEPPAction_SetKills(StarcraftMemory.MemoryPlace memory, RawActionSetDeaths trigger, int ObjectID) : base(memory, trigger) {
            _am = trigger.getAmount();
            int max = UnitDef.AllUnits[0].getMaxValue();
            int playerID = ObjectID % (max + 1);
            int unitID = ObjectID / (max + 1);
            _pl = IngamePlayerDef.getByIndex(playerID);
            _quant = trigger.getSetQuantifier();
            _un = UnitDef.getByIndex(unitID);
        }

        private IngamePlayerDef _pl;
        private SetQuantifier _quant;
        private IntDef _am;
        private UnitDef _un;

        public override string ToString() {
            int resultValue = _am.getIndex();
            int _unitID = _un.getIndex();
            int _playerID = _pl.getIndex();
            int objectID = _playerID;
            int max = _un.getMaxValue();
            int baseAddress = (int)Memory.Address;
            int elementSize = (int)Memory.ElementSize;
            objectID += (max + 1) * _unitID;
            int finalAddress = baseAddress + (elementSize * objectID);
            int playerID = (finalAddress / 4) - 1452249;
            Trigger.setPlayerID(new IntDef(playerID, Trigger.getPlayerID().UseHex));
            Trigger.setAmmount(new IntDef(resultValue, Trigger.getAmount().UseHex));
            Trigger.setSetQuantifier(_quant);
            return base.ToString();
        }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            return new TriggerDefinitionPart[] {
                new TriggerDefinitionLabel("Modify kills count for "),
                new TriggerDefinitionGeneralDef<IngamePlayerDef>(()=>_pl, (IngamePlayerDef pl)=>_pl=pl, ()=>IngamePlayerDef.AllIngamePlayers[0], IngamePlayerDef.AllIngamePlayers).setEditable(true),
                new TriggerDefinitionLabel(": "),
                new TriggerDefinitionGeneralDef<SetQuantifier>(()=>_quant,(SetQuantifier qu)=>_quant=qu, SetQuantifier.getDefaultValue, SetQuantifier.AllQuantifiers).setEditable(true),
                new TriggerDefinitionLabel(": "),
                new TriggerDefinitionIntAmount(()=>_am, (IntDef ina)=>_am=ina, ()=>IntDef.getDefaultValue(false)).setEditable(true),
                new TriggerDefinitionLabel(" for "),
                new TriggerDefinitionGeneralDef<UnitDef>(()=>_un, (UnitDef unit)=>_un=unit, UnitDef.getDefaultValue, UnitDef.AllUnits).setEditable(true),
                new TriggerDefinitionLabel("."),
            };
        }
    }

    class LengthtyEPDAction<TARGET_TYPE, VALUE_TYPE> : EPDAction where TARGET_TYPE : Gettable<TARGET_TYPE> where VALUE_TYPE : Gettable<VALUE_TYPE> {

        protected string _settingWhat;
        protected string _settingWhat2;

        protected Func<int, TARGET_TYPE> _typeGetter;
        protected Func<int, VALUE_TYPE> _valueGetter;

        protected int size;

        protected TARGET_TYPE[] _targets;
        protected VALUE_TYPE[] _values;

        protected StarcraftMemory.MemoryPlace _memory;

        private TriggerDefinitionPart _lastGlow;

        public LengthtyEPDAction(StarcraftMemory.MemoryPlace memory, int ObjectID, RawActionSetDeaths trigger, string settingWhat, string settingsWhat2, Func<int, TARGET_TYPE> typeGetter, Func<int, VALUE_TYPE> valueGetter) : base(memory, trigger) {
            _memory = memory;
            _settingWhat = settingWhat;
            _settingWhat2 = settingsWhat2;
            _typeGetter = typeGetter;
            _valueGetter = valueGetter;
            size = 4 / ((int)Memory.ElementSize); // <1,4>
            _targets = new TARGET_TYPE[size];
            _values = new VALUE_TYPE[size];
            _targets[0] = _typeGetter(ObjectID);
            int offset = (32 / size);
            uint am = (uint)Trigger.getAmount().getIndex();
            if (_values.Length == 4) {
                _values[3] = _valueGetter((int)((am >> 24) & 0xff));
                _values[2] = _valueGetter((int)((am >> 16) & 0xff));
                _values[1] = _valueGetter((int)((am >> 8) & 0xff));
                _values[0] = _valueGetter((int)((am >> 0) & 0xff));
            } else if (_values.Length == 2) {
                _values[1] = _valueGetter((int)((am >> 16) & 0xffff));
                _values[0] = _valueGetter((int)((am >> 0) & 0xffff));
            } else if (_values.Length == 1) {
                _values[0] = _valueGetter((int)am);
            } else {
                throw new NotImplementedException();
            }
            recalculateOthers();
        }

        public override string ToString() {
            int[] values = new int[_values.Length];
            for (int i = 0; i < values.Length; i++) {
                values[i] = _values[i].getIndex();
            }
            int resultValue = 0;

            
            if (values.Length == 1) {
                resultValue = (values[0] << 0);
            } else if (values.Length == 2) {
                resultValue = (values[1] << 16) | (values[0] << 0);
            } else if (values.Length == 4) {
                resultValue = (values[3] << 24) | (values[2] << 16) | (values[1] << 8) | (values[0] << 0);
            } else {
                throw new NotImplementedException();
            }
            
            /*
            if (values.Length == 1) {
                resultValue = (values[0] << 0);
            } else if (values.Length == 2) {
                resultValue = (values[0] << 16) | (values[1] << 0);
            } else if (values.Length == 4) {
                resultValue = (values[0] << 24) | (values[1] << 16) | (values[2] << 8) | (values[3] << 0);
            } else {
                throw new NotImplementedException();
            }
            */

            int objectID = _targets[0].getIndex();
            int baseAddress = (int)Memory.Address;
            int elementSize = (int)Memory.ElementSize;
            int finalAddress = baseAddress + (elementSize * objectID);
            int playerID = (finalAddress / 4) - 1452249;
            Trigger.setPlayerID(new IntDef(playerID, Trigger.getPlayerID().UseHex));
            Trigger.setAmmount(new IntDef(resultValue, Trigger.getAmount().UseHex));
            return base.ToString();
        }

        private void resetValuesIfDefaultIsKept() {
            if (_trgs != null) {
                TriggerDefinitionPart part = _trgs[0];
                if (part.isDefaultKept()) {
                    for (int i = 0; i < _values.Length; i++) {
                        VALUE_TYPE value = _values[i];
                        TARGET_TYPE target = _targets[i];
                        VALUE_TYPE def = getDefault(target);
                        if(target.getIndex() != value.getIndex()) {
                            _values[i] = def;
                        }
                    }
                }
            }
        }

        private void recalculateFromTarget(int index, TARGET_TYPE value) {
            TARGET_TYPE target = value;
            int targetBase = Memory.getAlignedID(target.getIndex());
            int max = target.getMaxValue();
            if (targetBase + 4 > max) {
                targetBase -= 4;
                target = _typeGetter(target.getIndex() - 4);
            }

            int changeOffset = target.getIndex() - targetBase;
            bool hasChanged = changeOffset != 0;
            target = _typeGetter(targetBase);
            _targets[index]= target;
            recalculateOthers();
            if(hasChanged) {
                int glowIndex = index + changeOffset;
                if(glowIndex == 1) {
                    glowIndex = 9;
                } else if(glowIndex == 2) {
                    glowIndex = 14;
                } else if(glowIndex == 3) {
                    glowIndex = 19;
                } else  {
                    throw new NotImplementedException();
                }
                TriggerDefinitionPart glowTarget = _trgs[glowIndex];
                if(_lastGlow!=null) {
                    _lastGlow.unglow();
                }
                _lastGlow = glowTarget;
                _lastGlow.glow();
            } else if(_lastGlow != null) {
                _lastGlow.unglow();
                _lastGlow = null;
            }
            resetValuesIfDefaultIsKept();
            redraw();
        }

        protected void redraw() {
            if (_trgs != null) {
                foreach (TriggerDefinitionPart part in _trgs) {
                    part.ValueChanged(false);
                }
            }
        }

        private void recalculateOthers() {
            if (_targets[0].getIndex() > _targets[0].getMaxValue() - (size - 1)) { // Last weapon
                _targets[0] = _typeGetter(_targets[0].getMaxValue() - (size - 1)); // Previous weapon
            }
            for (int i = 1; i < _targets.Length; i++) {
                _targets[i] = _typeGetter(_targets[0].getIndex() + i);
            }
            if (_trgs != null) {
                for (int i = 0; i < _trgs.Length; i++) {
                    _trgs[i].ValueChanged(false);
                }
            }
            resetValuesIfDefaultIsKept();
            redraw();
        }

        protected TriggerDefinitionPart[] _trgs;

        protected VALUE_TYPE getDefault(TARGET_TYPE value) {
            int offset = value.getIndex();
            int defaultValue = StarcraftMemory.getDefaultValue<int>(_memory, offset);
            return _valueGetter(defaultValue);
        }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            int len = size == 1 ? 5 : 7 + ((size - 1) * 5);
            TriggerDefinitionPart[] trgs = new TriggerDefinitionPart[len];
            _trgs = trgs;
            int trgI = 0;
            trgs[trgI++] = new TriggerDefinitionLabel(_settingWhat);
            trgs[trgI++] = _targets[0].getTriggerPart(() => _targets[0], (TARGET_TYPE obj) => { recalculateFromTarget(0, obj); recalculateOthers(); }, () => { throw new NotImplementedException(); }).setRegenerateAfterChange((bool b) => recalculateOthers());
            trgs[trgI++] = new TriggerDefinitionLabel(" to ");
            trgs[trgI++] = _values[0].getTriggerPart(() => _values[0], (VALUE_TYPE obj) => { _values[0] = obj; }, () => getDefault(_targets[0])).SetResetable(true);
            trgs[trgI++] = new TriggerDefinitionLabel(".");
            if (size > 1) {
                trgs[trgI++] = new TriggerDefinitionNewLine();
                trgs[trgI++] = new TriggerDefinitionLabel("Which subsequently changes also " + _settingWhat2);
                for (int i = 1; i < _targets.Length; i++) {
                    int ii = i; // Scope guard
                    int mi = _targets.Length - 1;
                    trgs[trgI++] = new TriggerDefinitionNewLine();
                    trgs[trgI++] = new TriggerDefinitionLabel("\t");
                    trgs[trgI++] = _targets[ii].getTriggerPart(() => _targets[ii], (TARGET_TYPE obj) => { _targets[ii] = obj; }, () => { throw new NotImplementedException(); }).setEditable(false);
                    trgs[trgI++] = new TriggerDefinitionLabel(" to ");
                    trgs[trgI++] = _values[ii].getTriggerPart(() => _values[ii], (VALUE_TYPE obj) => { _values[ii] = obj; }, () => getDefault(_targets[ii])).SetResetable(true);

                }
            }
            return trgs;
        }

    }


    class LengthlyEPDAddSubCondition<TARGET_TYPE, VALUE_TYPE> : EPDCondition where TARGET_TYPE : Gettable<TARGET_TYPE> where VALUE_TYPE : Gettable<VALUE_TYPE> {

        public LengthlyEPDAddSubCondition(StarcraftMemory.MemoryPlace memory, int ObjectID, ConditionMemory trigger, string settingWhat, string settingsWhat2, Func<int, TARGET_TYPE> typeGetter, Func<int, VALUE_TYPE> valueGetter) : base(memory, trigger) {

        }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            throw new NotImplementedException();
        }
    }
}
