using StarcraftEPDTriggers.src;
using System.Windows;

namespace StarcraftEPDTriggers {
    
    public partial class WndPlayerQuant : Window {
        private TriggerDefinitionQuantAmount triggerPartQuantAmountCallback;

        public WndPlayerQuant(TriggerDefinitionQuantAmount triggerPartQuantAmountCallback, int initValue) {
            InitializeComponent();
            this.triggerPartQuantAmountCallback = triggerPartQuantAmountCallback;
            if (initValue == -1) {
                txtAll.IsChecked = true;
                txtInput.Text = "0";
            } else {
                txtAll.IsChecked = false;
                txtInput.Text = initValue.ToString();
            }
            txtInput.IsEnabled = (bool) !txtAll.IsChecked;
            this.ShowDialog();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e) {
            if ((bool)txtAll.IsChecked) {
                triggerPartQuantAmountCallback.set(-1);
                Close();
            } else {
                string text = txtInput.Text;
                int n;
                bool isNumeric = int.TryParse(text, out n);
                if (isNumeric) {
                    triggerPartQuantAmountCallback.set(n);
                    this.Close();
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            triggerPartQuantAmountCallback.set(null);
        }

        private void txtAll_Checked(object sender, RoutedEventArgs e) {
            txtInput.IsEnabled = false;
        }

        private void txtAll_Unchecked(object sender, RoutedEventArgs e) {
            txtInput.IsEnabled = true;
        }
    }
}
