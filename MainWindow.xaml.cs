using System.Windows;
using StarcraftEPDTriggers.src.data;
using System.Collections.Generic;
using System.Windows.Media;
using StarcraftEPDTriggers.src.ui;
using System.Windows.Controls;
using System.Text;
using System;
using StarcraftEPDTriggers.src;
using System.Diagnostics;
using Microsoft.Win32;
using System.Reflection;

namespace StarcraftEPDTriggers {

    public enum AppState {
        OpeningFile,
        MapOpened,
        NothingOpened
    }


    public partial class MainWindow : Window {

        public static readonly string Version = Assembly.GetEntryAssembly().GetName().Version.Major.ToString() + "." + Assembly.GetEntryAssembly().GetName().Version.Minor.ToString() + "." + Assembly.GetEntryAssembly().GetName().Version.Build.ToString() + "." + Assembly.GetEntryAssembly().GetName().Version.Revision.ToString() + " BETA";

        private AppState CurrentState;

        private void setState(AppState state) {
            CurrentState = state;
            if (state != AppState.MapOpened) {
                Triggers.AllTrigers.Clear();
                Triggers.TriggerData.Clear();
                lstPlayers.Clear();
                lstTriggers.Clear();
            }
            UnsavedChanges = false;
            btnNewTrigger.IsEnabled = false;
            btnClose.IsEnabled = false;
            btnDelete.IsEnabled = false;
            btnClose.IsEnabled = false;
            btnModify.IsEnabled = false;
            btnMoveDown.IsEnabled = false;
            btnMoveUp.IsEnabled = false;
            btnSave.IsEnabled = false;
            txtMapName.IsEnabled = false;
            UnsavedChanges = false;
            switch (CurrentState) {
                case AppState.MapOpened:
                    btnClose.Content = "Close";
                    btnClose.IsEnabled = true;
                    btnSave.IsEnabled = true;
                    btnNewTrigger.IsEnabled = true;
                    break;

                case AppState.NothingOpened:
                    btnClose.Content = "Open";
                    btnClose.IsEnabled = true;
                    txtMapName.IsEnabled = true;
                    break;

                case AppState.OpeningFile:
                    btnClose.Content = "Close";
                    break;
            }

        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e) {
            if (e.ExceptionObject is NotImplementedException) {
                new WndError(e.ExceptionObject as NotImplementedException).ShowDialog();
            }
            Process.GetCurrentProcess().Kill(); // Suicide
        }

        private string loadedMapPath { get { return txtMapName.Text; } set { txtMapName.Text = value; } }

        private bool _unsavedChanges = false;
        private bool UnsavedChanges { set { _unsavedChanges = value; } get { return _unsavedChanges; } }

        private static Func<bool> useCustomNames = () => false;
        private static Func<bool> ignoreComments = () => false;

        public static bool UseCustomName { get { return useCustomNames(); } }
        public static bool IgnoreComments { get { return ignoreComments(); } }

        public new src.data.TriggerCollection Triggers = new src.data.TriggerCollection();

        private MySelectableList lstTriggers;
        private MySelectableList lstPlayers;

        public MainWindow() {
#if DEBUG
#else
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
#endif
            InitializeComponent();

            useCustomNames = () => !((bool)txtShowNativeNames.IsChecked);
            ignoreComments = () => (bool)txtIgnoreComments.IsChecked;
            //loadMap("C:/Users/Tom/Desktop/Documents/Downloads/Chkdraft-master/TriggerProcessor/out.scx");

            lstTriggers = new MySelectableList(lstTriggers_raw, new Thickness(3, 2, 5, 3));
            lstTriggers.SelectionChange += lstTriggers_SelectionChanged;
            lstPlayers = new MySelectableList(lstPlayers_raw);
            lstPlayers.SelectionChange += lstPlayers_SelectionChanged;
            lstTriggers.DoubleClick += lstPlayer_DoubleClicked;

            setState(AppState.NothingOpened);

            //loadMap("C:/Users/Tom/Desktop/Documents/Starcraft/Maps/[EUD]Sniper Troy.scx");
#if DEBUG
            //loadMap("C:/Users/Tom/Desktop/5.scx");
            loadMap("C:/Users/Tom/Desktop/Documents/Starcraft/Maps/HGA.scx");
            //loadMap("C:/Users/Tom/Desktop/Documents/Starcraft/Maps/[1] Snipers Sanctuary - ban.scx");
            //loadMap("C:/Starcraft/ZWHS v1.1.scx");
#endif
            //loadMap("C:/Users/Tom/Desktop/Documents/Starcraft/Maps/moje mapy/majne Sniper STFU.scx");
            //loadMap("C:/Users/Tom/Desktop/Documents/Starcraft/Maps/moje mapy/MILES Laser Tag V8L ELITE.scx");

            updateHistory();
        }

        private void updateHistory() {
            List<String> data = History.getHistory();
            this.txtMapName.Items.Clear();
            foreach(String str in data) {
                this.txtMapName.Items.Add(str);
            }
            if (txtMapName.Items.Count > 0) {
                txtMapName.SelectedIndex = 0;
            }
        }

        private void saveToFile(string path) {
            string trigs = getSaveString();
            lstPlayers.isEnabled = false;
            lstTriggers.isEnabled = false;
            new AsyncWorker(new object[] { path, trigs }, (object obj) => {
                string thr_path = (string)((object[])obj)[0];
                string thr_trigs = (string)((object[])obj)[1];
                return Triggers.save(thr_path, thr_trigs);
            }, (object result) => {
                bool rb = (bool)result;
                if (rb) {
                    lstPlayers.isEnabled = true;
                    lstTriggers.isEnabled = true;
                } else {
                    MessageBox.Show("Failed to save map", "Trigger editor", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
            });
        }

        private void loadMap(string path) {
            setState(AppState.OpeningFile);
            EPDAction.InvalidTriggers = 0;
            loadedMapPath = path;
            new AsyncWorker(path, (object pth) => { return Triggers.load((string)pth); }, (object result) => { bool rb = (bool)result; mapLoaded(rb); });
        }

        private void mapLoaded(bool loadedOk) {
            if (loadedOk) {
                History.addToHistory(loadedMapPath);
                updateHistory();
                setState(AppState.MapOpened);
                updateLists();
                if(EPDAction.InvalidTriggers != 0) {
                    MessageBox.Show("Encountered " + EPDAction.InvalidTriggers + " invalid EUD/EPD trigger" + (EPDAction.InvalidTriggers > 1 ? "s" : "") + ".\n" + (EPDAction.InvalidTriggers > 1 ? "Those triggers were" : "This trigger was") + " marked as [Invalid].", "Trigger Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                    EPDAction.InvalidTriggers = 0;
                }
            } else {
                setState(AppState.NothingOpened);
                MessageBox.Show("Failed to open given file.", "Trigger Editor", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }

        }

        private string getSaveString() {
            StringBuilder sb = new StringBuilder();
            foreach (Trigger trigger in Triggers.AllTrigers) {
                sb.Append(trigger.ToSaveString());
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        private void updateLists() {
            lstPlayers.Clear();
            List<MySelectableListItem> pls = new List<MySelectableListItem>();
            foreach (KeyValuePair<PlayerDef, List<Trigger>> subList in Triggers.TriggerData) {
                PlayerDef key = subList.Key;
                List<Trigger> value = subList.Value;
                if (value.Count > 0) { // Category not empty
                    Label lbl = new Label();
                    lbl.Content = key.ToString();
                    pls.Add(new MySelectableListItem(new object[] { key, lbl }, lbl));
                }
            }
            pls.Sort((MySelectableListItem item1, MySelectableListItem item2) => String.Compare(item1.ToString(), item2.ToString()));
            foreach(MySelectableListItem item in pls) {
                lstPlayers.Add(item);
            }
            //File.WriteAllText("gen.txt", getSaveString());
        }

        private void lstPlayers_SelectionChanged(MySelectableListItem lastSelected, MySelectableListItem newSelected) {
            if (lastSelected != newSelected) {
                if (lastSelected != null) {
                    object[] data = lastSelected.CustomData as object[];
                    Label lbl = data[1] as Label;
                    lbl.Background = Brushes.Transparent;
                    lbl.InvalidateVisual();
                }
                if (newSelected != null) {
                    object[] data = newSelected.CustomData as object[];
                    Label lbl = data[1] as Label;
                    lbl.Background = selected;
                    lbl.InvalidateVisual();
                    PlayerDef pd = data[0] as PlayerDef;

                    using (var d = lstTriggers.Dispatcher.DisableProcessing()) {
                        lstTriggers.Clear();
                        List<UIElement> items = new List<UIElement>();
                        List<Trigger> trigs = Triggers.TriggerData[pd];
                        foreach (Trigger trig in trigs) {
                            InsertTriggerIntoTheVisualList(trig);
                        }
                    }
                }
            }
        }

        private MySelectableListItem InsertTriggerIntoTheVisualList(Trigger trig) {
            Brush brush = new SolidColorBrush(Colors.Red);
            TriggerDefinitionPartProperties def = new TriggerDefinitionPartProperties();
            FrameworkElement g = trig.getTrigerListItem(def);
            def.g = (RichTextBox)g;
            MySelectableListItem item = new MySelectableListItem(new object[] { g, trig, def }, g);
            lstTriggers.Add(item);
            return item;
        }

        private static readonly Brush transparentBrush = new SolidColorBrush(Colors.White);
        private static readonly Brush selected = new SolidColorBrush(Color.FromRgb(90, 153, 215));

        private void lstTriggers_SelectionChanged(MySelectableListItem lastSelected, MySelectableListItem newSelected) {
            bool isSel = newSelected != null;
            btnCopy.IsEnabled = isSel;
            btnDelete.IsEnabled = isSel;
            btnModify.IsEnabled = isSel;
            btnMoveDown.IsEnabled = isSel;
            btnMoveUp.IsEnabled = isSel;

            if (lstTriggers.isLastItemSelected()) {
                btnMoveDown.IsEnabled = false;
            }
            if (lstTriggers.isFirstItemSelected()) {
                btnMoveUp.IsEnabled = false;
            }

            if (lastSelected != newSelected) { // Ignore the crap of this shit
                if (lastSelected != null) {
                    FrameworkElement ls = ((object[])lastSelected.CustomData)[0] as FrameworkElement;
                    TriggerDefinitionPartProperties props = ((object[])lastSelected.CustomData)[2] as TriggerDefinitionPartProperties;
                    props.FireSelectionChange(false);
                    if (ls is RichTextBox) {
                        ((RichTextBox)ls).Background = Brushes.White;
                        ((RichTextBox)ls).Foreground = Brushes.Black;
                        ls.InvalidateVisual();
                    }
                }
                if (newSelected != null) {
                    FrameworkElement ls = ((object[])newSelected.CustomData)[0] as FrameworkElement;
                    TriggerDefinitionPartProperties props = ((object[])newSelected.CustomData)[2] as TriggerDefinitionPartProperties;
                    props.FireSelectionChange(true);
                    if (ls is RichTextBox) {
                        ((RichTextBox)ls).Background = selected;
                        ((RichTextBox)ls).Foreground = Brushes.White;
                        ls.InvalidateVisual();
                    }
                }
            }
        }

        private void lstPlayer_DoubleClicked(MySelectableListItem item) {
            btnModify_Click(null, null);
        }

        private void btnMoveUp_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstTriggers.CurrentlySelected;
            if (sel != null) {
                lstTriggers.MoveItemUp(sel);
                Trigger trigger = ((object[])sel.CustomData)[1] as Trigger;
                Triggers.MoveUp(trigger);
                UnsavedChanges = true;
            }
        }

        private void btnMoveDown_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstTriggers.CurrentlySelected;
            if (sel != null) {
                lstTriggers.MoveItemDown(sel);
                Trigger trigger = ((object[])sel.CustomData)[1] as Trigger;
                Triggers.MoveDown(trigger);
                UnsavedChanges = true;
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstTriggers.CurrentlySelected;
            if (sel != null) {
                Trigger trigger = ((object[])sel.CustomData)[1] as Trigger;
                int selectedIndex = lstTriggers.SelectedIndex;
                if (lstTriggers.isLastItemSelected()) {
                    selectedIndex--;
                }
                lstTriggers.Remove(sel);
                Triggers.Delete(trigger);
                if (selectedIndex >= 0) { // Select next item
                    lstTriggers.SelectedIndex = selectedIndex;
                } else { // Category empty, regenerate catagories
                    updateLists();
                }
                UnsavedChanges = true;
            }
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstTriggers.CurrentlySelected;
            if (sel != null) {
                Trigger trigger = ((object[])sel.CustomData)[1] as Trigger;
                string textTrig = trigger.ToSaveString();
                Trigger trig = Triggers.loadAndInsertAfter(textTrig, trigger);
                MySelectableListItem item = InsertTriggerIntoTheVisualList(trig);
                lstTriggers.MoveItemBeforeItem(item, sel);
                lstTriggers.MoveItemBeforeItem(sel, item);
                UnsavedChanges = true;
            }
        }

        private void btnModify_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstTriggers.CurrentlySelected;
            if (sel != null) {
                Trigger trigger = ((object[])sel.CustomData)[1] as Trigger;
                new WndModify(trigger, Triggers, (Trigger trig) => {
                    Triggers.UpdateAffecteds(trig);
                    refershListAndKeepSelection();
                    UnsavedChanges = true;
                });
            }
        }

        private void refershListAndKeepSelection() {
            MySelectableListItem selectedTrigger = lstTriggers.CurrentlySelected;
            MySelectableListItem selectedPlayer = lstPlayers.CurrentlySelected;

            if (selectedTrigger != null) {
                Trigger trigDef = ((object[])selectedTrigger.CustomData)[1] as Trigger;
                PlayerDef pselPld = null;
                if (selectedPlayer == null) {
                    if (trigDef.getAffectedPlayers().Count == 0) {
                        updateLists();
                        lstTriggers.Clear();
                        return;
                    }
                    pselPld = trigDef.getAffectedPlayers()[0];
                } else {
                    pselPld = ((object[])selectedPlayer.CustomData)[0] as PlayerDef;
                }
                refershListAndKeepSelection(trigDef, pselPld);
            } else { // No selected trigger
                if (selectedPlayer == null) { // No selected player
                    updateLists();
                    lstTriggers.Clear();
                    return;
                }
                PlayerDef pselPld = ((object[])selectedPlayer.CustomData)[0] as PlayerDef;
                refershListAndKeepSelection(null, pselPld);
            }
        }

        private void refershListAndKeepSelection(Trigger trigDef, PlayerDef pselPld) {

            updateLists();
            int index = 0;
            bool found = false;
            // Check if trigger is still under selected player list
            if (trigDef != null) {
                if (!trigDef.getAffectedPlayers().Contains(pselPld)) { // Select another player who has this trigger (the first one)
                    pselPld = trigDef.getAffectedPlayers()[0];
                }
            }

            // Find the player and activate it
            foreach (MySelectableListItem pl in lstPlayers.GetAllItems()) {
                PlayerDef pld = ((object[])pl.CustomData)[0] as PlayerDef;
                if (pld == pselPld) {
                    lstPlayers.SelectedIndex = index;
                    found = true;
                    break;
                }
                index++;
            }
            if (!found) {
                // Should never happen
                if (lstPlayers.GetAllItems().Count == 0) {
                    lstTriggers.Clear();
                    return;
                } else {
                    lstPlayers.SelectedIndex = 0;
                    lstTriggers.Clear();
                    return;
                }
            } else {
                // Select trigger now
                index = 0;
                foreach (MySelectableListItem trg in lstTriggers.GetAllItems()) {
                    Trigger trig = ((object[])trg.CustomData)[1] as Trigger;
                    lstTriggers.SelectedIndex = index;
                    if (trig == trigDef) {

                        break;
                    }
                    index++;
                }
            }
        }

        private void btnNewTrigger_Click(object sender, RoutedEventArgs e) {
            Trigger newTrig = new StarcraftEPDTriggers.Trigger();
            MySelectableListItem item = lstPlayers.CurrentlySelected;
            if (item != null) {
                PlayerDef pd = ((object[])item.CustomData)[0] as PlayerDef;
                newTrig.getAffectedPlayers().Add(pd);
            }
            new WndModify(newTrig, Triggers, (Trigger trig) => {
                Triggers.TriggerCreated(trig);
                MySelectableListItem timSel = lstPlayers.CurrentlySelected;
                PlayerDef pd = null;
                if (timSel != null) {
                    pd = ((object[])timSel.CustomData)[0] as PlayerDef;
                }
                refershListAndKeepSelection(trig, pd);
                UnsavedChanges = true;
            });
        }

        private void txtShowNativeNames_Checked(object sender, RoutedEventArgs e) {
            refershListAndKeepSelection();
        }

        private void txtShowNativeNames_Unchecked(object sender, RoutedEventArgs e) {
            refershListAndKeepSelection();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e) {
            saveToFile(loadedMapPath);
            UnsavedChanges = false;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) {
            if (CurrentState == AppState.NothingOpened) { // Open a map
                loadedMapPath = txtMapName.Text;
                if (loadedMapPath.Length > 0) { // Have something in a history
                    loadMap(loadedMapPath);
                } else { // Have nothing in a history, open a dialog
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Title = "Open Starcraft Map file";
                    openFileDialog.Filter = "Starcraft Map|*.scm;*.scx";
                    if ((bool)openFileDialog.ShowDialog()) {
                        loadMap(openFileDialog.FileName);
                    }
                }
            } else if (CurrentState == AppState.MapOpened) { // Close the map
                if (UnsavedChanges) {
                    MessageBoxResult res = MessageBox.Show("Close with file without saving changes?", "Trigger Editor", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    if (res != MessageBoxResult.Yes) {
                        return;
                    }
                }
                setState(AppState.NothingOpened);
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(files.Length == 1) {
                if (CurrentState == AppState.MapOpened) {
                    if (UnsavedChanges) {
                        MessageBoxResult res = MessageBox.Show("Close with file without saving changes?", "Trigger Editor", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (res != MessageBoxResult.Yes) {
                            return;
                        }
                    }
                    setState(AppState.NothingOpened);
                } else if(CurrentState == AppState.OpeningFile) {
                    return;
                }
                string path = files[0];
                loadMap(path);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            refershListAndKeepSelection();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            refershListAndKeepSelection();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e) {
            new winAbout().ShowDialog();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e) {
            new WndUpdate().ShowDialog();
        }

        private void btnClr_Click(object sender, RoutedEventArgs e) {
            loadedMapPath = "";
        }
    }
}
