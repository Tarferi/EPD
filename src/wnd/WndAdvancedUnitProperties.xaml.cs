using System;
using System.Windows;
using System.Windows.Controls;

namespace StarcraftEPDTriggers {

    

    public partial class WndAdvancedUnitProperties : Window {

        private CheckBox[] _checks;

        private int defaultValue;

        private int getValueFromUI() {
            int currentValue = 0;
            currentValue |= (int)(((bool)txtBuilding.IsChecked) ? 0x00000001 : 0);
            currentValue |= (int)(((bool)txtAddon.IsChecked) ? 0x00000002 : 0);
            currentValue |= (int)(((bool)txtFlyer.IsChecked) ? 0x00000004 : 0);
            currentValue |= (int)(((bool)txtWorker.IsChecked) ? 0x00000008 : 0);
            currentValue |= (int)(((bool)txtSubunit.IsChecked) ? 0x00000010 : 0);
            currentValue |= (int)(((bool)txtFlyingBuilding.IsChecked) ? 0x00000020 : 0);
            currentValue |= (int)(((bool)txtHero.IsChecked) ? 0x00000040 : 0);
            currentValue |= (int)(((bool)txtRegeneratesHP.IsChecked) ? 0x00000080 : 0);
            currentValue |= (int)(((bool)txtAnimatedIdle.IsChecked) ? 0x00000100 : 0);

            currentValue |= (int)(((bool)txtCloakable.IsChecked) ? 0x00000200 : 0);
            currentValue |= (int)(((bool)txtTwoUnitsin1Egg.IsChecked) ? 0x00000400 : 0);
            currentValue |= (int)(((bool)txtSingleEntity.IsChecked) ? 0x00000800 : 0);
            currentValue |= (int)(((bool)txtResourceDepot.IsChecked) ? 0x00001000 : 0);
            currentValue |= (int)(((bool)txtResourceContainer.IsChecked) ? 0x00002000 : 0);
            currentValue |= (int)(((bool)txtRoboticUnit.IsChecked) ? 0x00004000 : 0);
            currentValue |= (int)(((bool)txtDetector.IsChecked) ? 0x00008000 : 0);
            currentValue |= (int)(((bool)txtOrganicUnit.IsChecked) ? 0x00010000 : 0);
            currentValue |= (int)(((bool)txtRequiresCreep.IsChecked) ? 0x00020000 : 0);
            currentValue |= (int)(((bool)txtUnused.IsChecked) ? 0x00040000 : 0);
            currentValue |= (int)(((bool)txtRequiresPsi.IsChecked) ? 0x00080000 : 0);
            currentValue |= (int)(((bool)txtBurrowable.IsChecked) ? 0x00100000 : 0);
            currentValue |= (int)(((bool)txtSpellcaster.IsChecked) ? 0x00200000 : 0);

            currentValue |= (int)(((bool)txtPermanentCloak.IsChecked) ? 0x00400000 : 0);
            currentValue |= (int)(((bool)txtPickupItem.IsChecked) ? 0x00800000 : 0);
            currentValue |= (int)(((bool)txtIgnoreSupplyCheck.IsChecked) ? 0x01000000 : 0);
            currentValue |= (int)(((bool)txtUseMediumOverlays.IsChecked) ? 0x02000000 : 0);
            currentValue |= (int)(((bool)txtUseLargeOverlays.IsChecked) ? 0x04000000 : 0);
            currentValue |= (int)(((bool)txtBattleReactions.IsChecked) ? 0x08000000 : 0);
            currentValue |= (int)(((bool)txtFullAutoAttack.IsChecked) ? 0x10000000 : 0);
            currentValue |= (int)(((bool)txtInvincible.IsChecked) ? 0x20000000 : 0);
            currentValue |= (int)(((bool)txtMechanicalUnit.IsChecked) ? 0x40000000 : 0);
            currentValue |= (int)(((bool)txtProducesUnits.IsChecked) ? 0x80000000 : 0);
            return currentValue;
        }

        public void setValueToUI(int value) {
            txtBuilding.IsChecked = (value & 0x00000001) > 0;
            txtAddon.IsChecked = (value & 0x00000002) > 0;
            txtFlyer.IsChecked = (value & 0x00000004) > 0;
            txtWorker.IsChecked = (value & 0x00000008) > 0;
            txtSubunit.IsChecked = (value & 0x00000010) > 0;
            txtFlyingBuilding.IsChecked = (value & 0x00000020) > 0;
            txtHero.IsChecked = (value & 0x00000040) > 0;
            txtRegeneratesHP.IsChecked = (value & 0x00000080) > 0;
            txtAnimatedIdle.IsChecked = (value & 0x00000100) > 0;

            txtCloakable.IsChecked = (value & 0x00000200) > 0;
            txtTwoUnitsin1Egg.IsChecked = (value & 0x00000400) > 0;
            txtSingleEntity.IsChecked = (value & 0x00000800) > 0;
            txtResourceDepot.IsChecked = (value & 0x00001000) > 0;
            txtResourceContainer.IsChecked = (value & 0x00002000) > 0;
            txtRoboticUnit.IsChecked = (value & 0x00004000) > 0;
            txtDetector.IsChecked = (value & 0x00008000) > 0;
            txtOrganicUnit.IsChecked = (value & 0x00010000) > 0;
            txtRequiresCreep.IsChecked = (value & 0x00020000) > 0;
            txtUnused.IsChecked = (value & 0x00040000) > 0;
            txtRequiresPsi.IsChecked = (value & 0x00080000) > 0;
            txtBurrowable.IsChecked = (value & 0x00100000) > 0;
            txtSpellcaster.IsChecked = (value & 0x00200000) > 0;

            txtPermanentCloak.IsChecked = (value & 0x00400000) > 0;
            txtPickupItem.IsChecked = (value & 0x00800000) > 0;
            txtIgnoreSupplyCheck.IsChecked = (value & 0x01000000) > 0;
            txtUseMediumOverlays.IsChecked = (value & 0x02000000) > 0;
            txtUseLargeOverlays.IsChecked = (value & 0x04000000) > 0;
            txtBattleReactions.IsChecked = (value & 0x08000000) > 0;
            txtFullAutoAttack.IsChecked = (value & 0x10000000) > 0;
            txtInvincible.IsChecked = (value & 0x20000000) > 0;
            txtMechanicalUnit.IsChecked = (value & 0x40000000) > 0;
            txtProducesUnits.IsChecked = (value & 0x80000000) > 0;
        }

        private void setup() {
            _checks = new CheckBox[] {
                txtBuilding,
                txtAddon,
                txtFlyer,
                txtWorker,
                txtSubunit,
                txtFlyingBuilding,
                txtHero,
                txtRegeneratesHP,
                txtAnimatedIdle,
                txtCloakable,
                txtTwoUnitsin1Egg,
                txtSingleEntity,
                txtResourceDepot,
                txtResourceContainer,
                txtRoboticUnit,
                txtDetector,
                txtOrganicUnit,
                txtRequiresCreep,
                txtUnused,
                txtRequiresPsi,
                txtBurrowable,
                txtSpellcaster,
                txtPermanentCloak,
                txtPickupItem,
                txtIgnoreSupplyCheck,
                txtUseMediumOverlays,
                txtUseLargeOverlays,
                txtBattleReactions,
                txtFullAutoAttack,
                txtInvincible,
                txtMechanicalUnit,
                txtProducesUnits
            };

            txtBuilding.ToolTip = "Unit is considered as a building for targeting purposes, and affects Addons-related properties. Also allows for other units to be built/trained 'inside' the unit and makes the unit placement dependent on the \"Placement Box\" property instead of the \"Unit Dimensions\".";
            txtAddon.ToolTip = "Makes the building placeable in a special way, dependent on the main building. If unchecked, the building will still use the standard way of placing the addon, but after lift-off, it will be inaccessible.";
            txtFlyer.ToolTip = "If unchecked, the unit is targeted by Ground weapons. If checked, unit can be targeted by Air weapons. It also makes the unit \"fly\" in the way that it chooses the shortest path to its destination, moving over terrain and other units.";
            txtWorker.ToolTip = "Unit can gather/return resources. Does NOT affect building construction abilities (except a tiny Drone glitch if you cancel a building morph). Requires a .LOO file pointed from the Special Overlay property in Images.dat to work. Vespene Gas harvesting seems good for all units, but Minerals may cause crashes, depending on the unit you use (e.g. Marine is OK, but the Firebat will crash)";
            txtSubunit.ToolTip = "Main subunit to the unit. Various turrets mostly. [pointer to units.dat]";
            txtFlyingBuilding.ToolTip = "Allows/Disallows the unit to be in the \"In Transit\" (or \"Flying\") state both in the game and in StarEdit, but it will crash if the unit does not have a Lift-off and Landing animations (in Iscript.bin). Does not affect buttons available for the unit.";
            txtHero.ToolTip = "Unit has all its upgrades researched from the start and receives a white wireframe box (instead of the standard blue one). If a unit is also a spellcaster, it will have 250 energy points, regardless if there is an energy upgrade for it or not. ";
            txtRegeneratesHP.ToolTip = "Unit will slowly regain Hit Points, until its full HP capacity.";
            txtAnimatedIdle.ToolTip = "Unknown.";
            txtCloakable.ToolTip = "Allows/Disallows the unit to use the Cloak technology. It does NOT give/remove the \"Cloak\" button but allows the unit to display the \"Group(Cloakers)\" button set when selected in a group.";
            txtTwoUnitsin1Egg.ToolTip = "2 units will come out of one Zerg Egg instead of just one. The cost for morphing will NOT be doubled, but the amount of the used supplies will equal 2 times the normal amount. To accomplish the full effect you will also have to add a \"Construction\" graphics and set a \"Landing Dust\" overlay to it.";
            txtSingleEntity.ToolTip = "nit cannot be selected in a group, but only as a single unit. Unit cannot be selected by dragging a selection box, by a SHIFT-Click nor a double-click.";
            txtResourceDepot.ToolTip = "Makes a building (and ONLY a building) a place workers can return resources to. Also makes it impossible to place/land the building next to a unit with the \"Resource Container\" property.";
            txtResourceContainer.ToolTip = "Unit does not generate an error message if targeted for Gathering by a worker. Allows/Disallows to set unit's resources capacity in StarEdit, but does not make a unit other than original resource actually store resources in-game. Unchecked, makes the original resources capacity equal to 0, although workers will still try to harvest it. Prevents \"Resource Depots\" from being located next to it.";
            txtRoboticUnit.ToolTip = "Unit is treated as a robotic-type target for weapons and abilities (e.g. it cannot be targeted with Spawn Broodlings)";
            txtDetector.ToolTip = "Unit can detect cloaked enemy units and receives the \"Detector\" title under its name.";
            txtOrganicUnit.ToolTip = "Unit is treated as an organic-type target for weapons and abilities (e.g. Maelstrom).";
            txtRequiresCreep.ToolTip = "Building MUST be built on creep. It will also get a creep outline around it.";
            txtUnused.ToolTip = "Unknown";
            txtRequiresPsi.ToolTip = "Unit must be built within a PSI field, like that produced by pylons. If it is not within a PSI field, it will become \"Disabled\" and not function. Can be given to any unit. You can also disable it on Protoss buildings so they can be built anywhere.";
            txtBurrowable.ToolTip = "Allows/Disallows the unit to use the Burrow technology. It does NOT give/remove the \"Burrow\" button.";
            txtSpellcaster.ToolTip = "Unit receives an energy bar and will slowly regain energy points through time. Combined with the Permanent Cloak property, will prevent unit from regaining energy.";
            txtPermanentCloak.ToolTip = "Unit is constantly cloaked. If the unit is also a Spellcaster, giving this property will make it lose energy until 0.";
            txtPickupItem.ToolTip = "Related to powerups. Not completely understood.";
txtIgnoreSupplyCheck.ToolTip = "Even if you don't have the supply available to build the unit, the engine will still build it and add that to your supply.";
            txtUseMediumOverlays.ToolTip = "Unit will use medium spell overlay graphics.";
            txtUseLargeOverlays.ToolTip = "Unit will use large spell overlay graphics.";
            txtBattleReactions.ToolTip = "Unit will show battle reactions, i.e. if it sees an enemy it will move to it and attack, if it is attacked by an unreachable enemy (e.g. an Air unit and it doesn't have an Air weapon) it will run away. Works ONLY if the unit's Idle AI Orders are set to Guard.";
            txtFullAutoAttack.ToolTip = "Unit will attack any enemy that enters its firing range. If unchecked, unit will attack the enemy ONLY if it is facing its direction, e.g. because it has an animated idle state. ";
            txtInvincible.ToolTip = "Unit cannot be a target of any sort of weapons or abilities. It also hides the unit's Hit Points value.";
            txtMechanicalUnit.ToolTip = "Unit is treated as a mechanical-type target for weapons and abilities (e.g. Lockdown). It can also be repaired by SCVs.";
            txtProducesUnits.ToolTip = "Unknown.";

            foreach (CheckBox check in _checks) {
                check.Checked += delegate {
                    updateDefaults();
                };
                check.Unchecked += delegate {
                    updateDefaults();
                };

                ToolTip t = new ToolTip();
                t.Content = new TextBlock() { Text = check.ToolTip.ToString(), MaxWidth = 400, TextWrapping = TextWrapping.Wrap };
                check.ToolTip = t;
            }
        }

        private Action<AdvancedPropertiesDef> _setter;

        private void updateDefaults() {
            int val = getValueFromUI();
            showDef(val == defaultValue);
        }

        private void showDef(bool isDefault) {
            lblDef.Visibility = isDefault ? Visibility.Visible : Visibility.Collapsed;
            lblUndef.Visibility = !isDefault ? Visibility.Visible : Visibility.Collapsed;
        }

        public WndAdvancedUnitProperties(Func<AdvancedPropertiesDef> getter, Action<AdvancedPropertiesDef> setter, int defaultValue) {
            InitializeComponent();
            _setter = setter;
            this.defaultValue = defaultValue;
            setup();
            setValueToUI(getter().getIndex());
            ShowDialog();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e) {
            int val = getValueFromUI();
            _setter(new AdvancedPropertiesDef(val));
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e) {
            setValueToUI(defaultValue);
        }
    }
}
