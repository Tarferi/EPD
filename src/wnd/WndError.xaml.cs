using StarcraftEPDTriggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StarcraftEPDTriggers {
    /// <summary>
    /// Interaction logic for WndError.xaml
    /// </summary>
    public partial class WndError : Window {
        public WndError(NotImplementedException exc) {
            InitializeComponent();
            txtExce.Text = exc.ToString();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) {
            Environment.Exit(0);
            //Close();
        }

        private void btnThr_Click(object sender, RoutedEventArgs e) {
            winAbout.OpenThread();
        }
    }
}
