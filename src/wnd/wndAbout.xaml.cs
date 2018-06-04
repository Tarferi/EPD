using System.Windows;

namespace StarcraftEPDTriggers {

    public partial class winAbout : Window {

        public static void OpenThread() {
            System.Diagnostics.Process.Start("http://rion.cz/epd/thread");
        }

        public winAbout() {
            InitializeComponent();
            lblVersion.Content = MainWindow.Version;
        }

        private void btnThread_Copy_Click(object sender, RoutedEventArgs e) {
            OpenThread();
        }

        private void btndonate_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("http://rion.cz/epd/donate");
        }
    }
}
