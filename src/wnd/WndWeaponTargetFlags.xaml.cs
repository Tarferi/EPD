using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StarcraftEPDTriggers.src.wnd {
    /// <summary>
    /// Interaction logic for WndWeaponTargetFlags.xaml
    /// </summary>
    public partial class WndWeaponTargetFlags : Window {

        private CheckBox[] _checks;
        private Action<WeaponTargetFlags> _setter;

        public void setValueToUI(int value) {
            txtAir.IsChecked = (value & 0x001) > 0;
            txtGround.IsChecked = (value & 0x002) > 0;
            txtMech.IsChecked = (value & 0x004) > 0;
            txtOrganic.IsChecked = (value & 0x008) > 0;
            txtNoBuilding.IsChecked = (value & 0x010) > 0;
            txtNoRobotic.IsChecked = (value & 0x020) > 0;
            txtTerrain.IsChecked = (value & 0x040) > 0;
            txtOrganicOrMEcha.IsChecked = (value & 0x080) > 0;
            txtOwn.IsChecked = (value & 0x100) > 0;
        }

        private int getValueFromUI() {
            int currentValue = 0;
            currentValue |= (int)(((bool)txtAir.IsChecked) ? 0x001 : 0);
            currentValue |= (int)(((bool)txtGround.IsChecked) ? 0x002 : 0);
            currentValue |= (int)(((bool)txtMech.IsChecked) ? 0x004 : 0);
            currentValue |= (int)(((bool)txtOrganic.IsChecked) ? 0x008 : 0);
            currentValue |= (int)(((bool)txtNoBuilding.IsChecked) ? 0x010 : 0);
            currentValue |= (int)(((bool)txtNoRobotic.IsChecked) ? 0x020 : 0);
            currentValue |= (int)(((bool)txtTerrain.IsChecked) ? 0x040 : 0);
            currentValue |= (int)(((bool)txtOrganicOrMEcha.IsChecked) ? 0x080 : 0);
            currentValue |= (int)(((bool)txtOwn.IsChecked) ? 0x100 : 0);
            return currentValue;
        }

        private int defaultValue;

        private void setup() {
            _checks = new CheckBox[] {
                txtAir,
                txtGround,
                txtMech,
                txtOrganic,
                txtNoBuilding,
                txtNoRobotic,
                txtTerrain,
                txtOrganicOrMEcha,
                txtOwn
            };
            foreach (CheckBox check in _checks) {
                check.Checked += delegate {
                    updateDefaults();
                };
                check.Unchecked += delegate {
                    updateDefaults();
                };
            }
        }


        private void updateDefaults() {
            int val = getValueFromUI();
            showDef(val == defaultValue);
        }

        private void showDef(bool isDefault) {
            lblDef.Visibility = isDefault ? Visibility.Visible : Visibility.Collapsed;
            lblUndef.Visibility = !isDefault ? Visibility.Visible : Visibility.Collapsed;
        }

        public WndWeaponTargetFlags(Func<WeaponTargetFlags> getter, Action<WeaponTargetFlags> setter, WeaponTargetFlags defaultValue ) {
            InitializeComponent();
            setup();
            _setter = setter;
            this.defaultValue = defaultValue.getIndex();
            setValueToUI(getter().getIndex());
            updateDefaults();
            ShowDialog();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e) {
            setValueToUI(defaultValue);
            updateDefaults();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e) {
            _setter(WeaponTargetFlags.getByIndex(getValueFromUI()));
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
