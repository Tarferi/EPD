using StarcraftEPDTriggers.src;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static StarcraftEPDTriggers.src.TriggerDefinitionNewLine;

namespace StarcraftEPDTriggers {

    public interface ConstrucableTrigger {

        string getTriggerName();

        TriggerContent construct();

    }

    public class MyComboItem {

        private ConstrucableTrigger _trg;

         public ConstrucableTrigger getConstructable() {
            return _trg;
        }

        public MyComboItem(ConstrucableTrigger trg) {
            _trg = trg;
        }

        public override string ToString() {
            return _trg.getTriggerName();
        }
    }

    public partial class AddCondAct: Window {

        Action<TriggerContent> _setter;

        private bool KeepDefault { get { return (bool)txtDef.IsChecked; } }

        public AddCondAct(string what, List<ConstrucableTrigger> comboInsides, Action<TriggerContent> setter)  {
            InitializeComponent();
            _setter = setter;
            lblSelWhat.Content= "Select "+what;
            lstCombo.Items.Clear();
            foreach(ConstrucableTrigger obj in comboInsides) {
                lstCombo.Items.Add(new MyComboItem(obj));
            }
            Loaded += delegate {
                lstCombo.SelectedIndex = 1;
                lstCombo.Focus();
            };
            wnd.Title = "Add " + what;
            ShowDialog();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private TriggerContent content;

        private void lstCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            grd.Children.Clear();
            MyComboItem sel = lstCombo.SelectedItem as MyComboItem;
            RichTextBox rc = new RichTextBox();
            grd.Children.Add(rc);
            content = sel.getConstructable().construct();
            TriggerDefinitionPartProperties def = new TriggerDefinitionPartProperties(()=>KeepDefault);
            def.HideCheckBox = true;
            def.g = rc;
            Trigger.regenerateTextboxForTriggerDefinitionParts(def, content.getDefinitionParts, (RichTextBox rb) => { rc = rb; });
        }

        private void btnOk_Click(object sender, RoutedEventArgs e) {
            if (grd.Children.Count != 0) {
                _setter(content);
                Close();
            }
        }
    }
}
