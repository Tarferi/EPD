using System;
using System.Windows;

namespace StarcraftEPDTriggers {

    public partial class WndStringPropertyEdit : Window {

        protected string HelpText {

            get {
                return @"<01><1> - Use Default
<02><2> - Pale Blue
<03><3> - Yellow
<04><4> - White
<05><5> - Grey
<06><6> - Red
<07><7> - Green
<08><8> - Red (P1)
<0B> - Invisible
<0C> - Remove beyond
<0E> - Blue (P2)
<0F> - Teal (P3)
<10> - Purple (P4)
<11> - Orange (P5)
<12><R> - Right Align
<13><C> - Center Align
<14> - Invisible
<15> - Brown (p6)
<16> - White (p7)
<17> - Yellow (p8)
<18> - Green (p9)
<19> - Brighter Yellow (p10)
<1A> - Cyan
<1B> - Pinkish (p11)
<1C> - Dark Cyan (p12)
<1D> - Greygreen
<1E> - Bluegrey
<1F> - Turquoise";
            }

        }

        private Action<string, bool> _setter;


        public WndStringPropertyEdit(Func<string> getterTxt, Func<bool> getterBool, Action<string, bool> setter) {
            InitializeComponent();
            _setter = setter;
            txtInput.Text = getterTxt();
            txtHelper.Text = HelpText;
            txtAlwaysDispaly.IsChecked = getterBool();
            ShowDialog();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e) {
            _setter(txtInput.Text.Replace("\"","\\\""), (bool)txtAlwaysDispaly.IsChecked);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
