using StarcraftEPDTriggers.src.data;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;

namespace StarcraftEPDTriggers {
    /// <summary>
    /// Interaction logic for WndUpdate.xaml
    /// </summary>
    public partial class WndUpdate : Window {

        public WndUpdate() {
            InitializeComponent();
            txtCurrentVersion.Text = MainWindow.Version;

            Loaded += delegate {
                new AsyncWorker(txtCurrentVersion.Text, (object o) => {
                    string txt = o.ToString();
                    string html = string.Empty;
                    string url = @"https://rion.cz/epd/thread/update.php?rv=1&ver=" + txt;
                    try {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.AutomaticDecompression = DecompressionMethods.GZip;

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream)) {
                            html = reader.ReadToEnd();
                        }
                    } catch (Exception) {
                        return null;
                    }
                    return html;
                }, (object result) => {
                    rect1.Visibility = Visibility.Collapsed;
                    if(result == null) {
                        MessageBox.Show("Failed getting latest version number", "Trigger Editor", MessageBoxButton.OK, MessageBoxImage.Error);
                    } else {
                        string html = result.ToString();
                        txtLatestVersion.Text = html;
                        if (!txtLatestVersion.Text.Equals(txtCurrentVersion.Text)) {
                            btnGet.IsEnabled = true;
                            btnGet.Visibility = Visibility.Visible;
                        } else {
                            check.Visibility = Visibility.Visible;
                        }
                    }
                });
            };
        }

        private void btnGet_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("https://rion.cz/epd/thread/EPD.zip");
        }
    }
}
