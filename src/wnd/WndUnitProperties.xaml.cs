using System.Windows;
using StarcraftEPDTriggers.src;

namespace StarcraftEPDTriggers {

    public partial class WndUnitProperties : Window {
        private TriggerDefinitionPropertiesDef triggerDefinitionPropertiesCallback;

        public WndUnitProperties(TriggerDefinitionPropertiesDef triggerDefinitionPropertiesCallback) {
            this.triggerDefinitionPropertiesCallback = triggerDefinitionPropertiesCallback;
            InitializeComponent();
            this.ShowDialog();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e) {
            int hp, mana, shield, hangar, resources;
            if (int.TryParse(txtLife.Text, out hp) && int.TryParse(txtMana.Text, out mana) && int.TryParse(txtShield.Text, out shield) && int.TryParse(txtHangar.Text, out hangar) && int.TryParse(txtResources.Text, out resources)) {
                // All valid
                bool invincible, halluc, cloak, burrowed, lifted;
                invincible = (bool)txtInvincible.IsChecked;
                halluc = (bool)txtHaluc.IsChecked;
                cloak = (bool)txtCloaked.IsChecked;
                burrowed = (bool)txtBurrowed.IsChecked;
                lifted = (bool)txtLifted.IsChecked;

                int result = 1;
                triggerDefinitionPropertiesCallback.set(result);
                Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            triggerDefinitionPropertiesCallback.set(null);
            Close();
        }
    }
}
