using StarcraftEPDTriggers.src;
using StarcraftEPDTriggers.src.data;
using StarcraftEPDTriggers.src.ui;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static StarcraftEPDTriggers.src.TriggerContentType;

namespace StarcraftEPDTriggers {

    public partial class WndModify : Window {

        private void test() {
            tabs.SelectedIndex = 1;
            btnNewCond_Click(null, null);
        }

        private Trigger _originalTrigger;
        private StarcraftEPDTriggers.src.data.TriggerCollection _colection;

        private Trigger myTrigga;

        private List<PlayerDef> Affecteds { get { return myTrigga.getAffectedPlayers(); } }

        private CheckBox[] playersChecks;
        private MySelectableList lstCond;
        private MySelectableList lstAct;
        private Action<Trigger> _saveCallback;
        
        private void setup() {
            lstCond = new MySelectableList(lstCondRaw);
            lstAct = new MySelectableList(lstActionsRaw);

            playersChecks = new CheckBox[] {
                pl_0,
                pl_1,
                pl_2,
                pl_3,
                pl_4,
                pl_5,
                pl_6,
                pl_7,
                pl_8,
                pl_9,
                pl_10,
                pl_11,
                pl_12,
                pl_13,
                pl_14,
                pl_15,
                pl_16,
                pl_17,
                pl_18,
                pl_19,
                pl_20,
                pl_21,
                pl_22,
                pl_23,
                pl_24,
                pl_25,
                pl_26,
            };
            lstCond.SelectionChange += lstCond_SelectionChanged;
            lstAct.SelectionChange += lstAct_SelectionChanged;

        }

        private void updateValuesFromTrigger() {
            // Setup affecteds
            foreach (CheckBox cb in playersChecks) {
                cb.IsChecked = false;
            }
            foreach (PlayerDef affected in myTrigga.getAffectedPlayers()) {
                playersChecks[affected.getIndex()].IsChecked = true;
            }

            // Setup conditions
            lstCond.Clear();
            foreach (Condition cond in myTrigga.Conditions) {
                addVisualCondition(cond);
            }

            // Setup actions
            lstAct.Clear();
            foreach (Action action in myTrigga.Actions) {
                addVisualAction(action);
            }
        }

        public WndModify(Trigger trig, StarcraftEPDTriggers.src.data.TriggerCollection colection, Action<Trigger> saveCallback) {
            InitializeComponent();
            setup();
            _originalTrigger = trig;
            _colection = colection;
            _saveCallback = saveCallback;
            myTrigga = new Trigger();
            TriggerDefinitionPartProperties def = new TriggerDefinitionPartProperties();
            myTrigga.reparseFromString(_originalTrigger.ToSaveString(), null, def);
            updateValuesFromTrigger();
            ShowDialog();
            setup();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (tabs.SelectedIndex >= 0) {
                switch (tabs.SelectedIndex) {
                    case 0:
                        wnd.Title = "Select players trigger should execute for.";
                        break;
                    case 1:
                        wnd.Title = "Trigger conditions (" + lstCond.GetAllItems().Count + "/16).";
                        break;
                    case 2:
                        wnd.Title = "Trigger actions (" + lstAct.GetAllItems().Count + "/64).";
                        break;
                    default:
                        wnd.Title = "Trigger editor";
                        break;
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private List<T> getFromSelLst<T>(List<MySelectableListItem> src) {
            List<T> lst = new List<T>();
            foreach (MySelectableListItem item in src) {
                if (((object[])item.CustomData)[1] is T) {
                    lst.Add((T)(((object[])item.CustomData)[1]));
                } else {
                    throw new NotImplementedException();
                }
            }
            return lst;
        }

        private MySelectableListItem addVisualCondition(Condition cond) {
            RichTextBox cb = new RichTextBox(); // Callback should change it, but it won't change the already added MySelectableListItem
            TriggerDefinitionPartProperties def = new TriggerDefinitionPartProperties();
            def.g = cb;
            Trigger.regenerateTextboxForTriggerDefinitionParts(def, cond.getDefinitionParts, (RichTextBox qcb) => { cb = qcb; });
            MySelectableListItem item = new MySelectableListItem(new Object[] { cb, cond, def }, cb);
            lstCond.Add(item);
            return item;
        }

        private MySelectableListItem addVisualAction(Action act) {
            RichTextBox cb = new RichTextBox(); // Callback should change it, but it won't change the already added MySelectableListItem
            TriggerDefinitionPartProperties def = new TriggerDefinitionPartProperties();
            def.g = cb;
            Trigger.regenerateTextboxForTriggerDefinitionParts(def, act.getDefinitionParts, (RichTextBox qcb) => { cb = qcb; });
            MySelectableListItem item = new MySelectableListItem(new Object[] { cb, act, def}, cb);
            lstAct.Add(item);
            return item;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e) {
            // First validate settings
            // At least 1 player is selected
            Affecteds.Clear();
            for (int i = 0; i < playersChecks.Length; i++) {
                if ((bool)playersChecks[i].IsChecked) {
                    Affecteds.Add(PlayerDef.AllPlayers[i]);
                }
            }
            if (Affecteds.Count == 0) {
                tabs.SelectedIndex = 0;
                MessageBox.Show("Trigger must affect at least 1 player.");
                return;
            }

            // At least 1 condition exists
            List<Condition> conds = getFromSelLst<Condition>(lstCond.GetAllItems());
            if (conds.Count == 0) {
                tabs.SelectedIndex = 1;
                MessageBox.Show("At least 1 condition must exist.");
                return;
            }
            if (conds.Count > 16) {
                tabs.SelectedIndex = 1;
                MessageBox.Show("There can only be at most 16 conditions.");
                return;
            }
            myTrigga.Conditions.Clear();
            foreach (Condition cond in conds) {
                myTrigga.Conditions.Add(cond);
            }


            // At least 1 action exists
            List<Action> actions = getFromSelLst<Action>(lstAct.GetAllItems());
            if (actions.Count == 0) {
                tabs.SelectedIndex = 2;
                MessageBox.Show("At least 1 action must exist.");
                return;
            }
            if (actions.Count > 64) {
                tabs.SelectedIndex = 1;
                MessageBox.Show("There can only be at most 64 actions.");
                return;
            }
            myTrigga.Actions.Clear();
            foreach (Action action in actions) {
                myTrigga.Actions.Add(action);
            }
            TriggerDefinitionPartProperties def = new TriggerDefinitionPartProperties();
            _originalTrigger.reparseFromString(myTrigga.ToSaveString(), _colection, def);
            _saveCallback(_originalTrigger);
            Close();
        }

        private void btnCondMoveUp_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstCond.CurrentlySelected;
            if (sel != null) {
                lstCond.MoveItemUp(sel);
            }
        }

        private void btnCondMoveDown_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstCond.CurrentlySelected;
            if (sel != null) {
                lstCond.MoveItemDown(sel);
            }
        }

        private void btnDelCond_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstCond.CurrentlySelected;
            if (sel != null) {
                int selectedIndex = lstCond.SelectedIndex;
                if (lstCond.isLastItemSelected()) {
                    selectedIndex--;
                }
                lstCond.Remove(sel);
                if (selectedIndex >= 0) { // Select next item
                    lstCond.SelectedIndex = selectedIndex;
                }
            }
            TabControl_SelectionChanged(null, null);
        }

        private void btnCondCopy_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstCond.CurrentlySelected;
            if (sel != null) {
                Condition cond = ((object[])sel.CustomData)[1] as Condition;
                Parser parser = new Parser(new Scanner(cond.ToSaveString()));
                Condition clone = parser.parseOnlyCondition();
                MySelectableListItem item = addVisualCondition(clone);
                lstCond.MoveItemBeforeItem(item, sel);
                lstCond.MoveItemBeforeItem(sel, item);
            }
            TabControl_SelectionChanged(null, null);
        }

        private static readonly Brush selected = new SolidColorBrush(Color.FromRgb(90, 153, 215));

        private void lstCond_SelectionChanged(MySelectableListItem lastSelected, MySelectableListItem newSelected) {
            TabControl_SelectionChanged(null, null);
            bool isSel = newSelected != null;
            btnCondCopy.IsEnabled = isSel;
            btnDelCond.IsEnabled = isSel;
            btnCondMoveDown.IsEnabled = isSel;
            btnCondMoveUp.IsEnabled = isSel;

            if (lstCond.isLastItemSelected()) {
                btnCondMoveDown.IsEnabled = false;
            }
            if (lstCond.isFirstItemSelected()) {
                btnCondMoveUp.IsEnabled = false;
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

        private void btnActMoveDown_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstAct.CurrentlySelected;
            if (sel != null) {
                lstAct.MoveItemDown(sel);
            }
        }

        private void btnActMoveUp_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstAct.CurrentlySelected;
            if (sel != null) {
                lstAct.MoveItemUp(sel);
            }
        }

        private void btnActDel_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstAct.CurrentlySelected;
            if (sel != null) {
                int selectedIndex = lstAct.SelectedIndex;
                if (lstAct.isLastItemSelected()) {
                    selectedIndex--;
                }
                lstAct.Remove(sel);
                if (selectedIndex >= 0) { // Select next item
                    lstAct.SelectedIndex = selectedIndex;
                }
            }
            TabControl_SelectionChanged(null, null);
        }

        private void btnActCopy_Click(object sender, RoutedEventArgs e) {
            MySelectableListItem sel = lstAct.CurrentlySelected;
            if (sel != null) {
                Action act = ((object[])sel.CustomData)[1] as Action;
                Parser parser = new Parser(new Scanner(act.ToSaveString()));
                Action clone = parser.parseOnlyAction();
                MySelectableListItem item = addVisualAction(clone);
                lstAct.MoveItemBeforeItem(item, sel);
                lstAct.MoveItemBeforeItem(sel, item);
            }
            TabControl_SelectionChanged(null, null);
        }

        private void lstAct_SelectionChanged(MySelectableListItem lastSelected, MySelectableListItem newSelected) {
            TabControl_SelectionChanged(null, null);
            bool isSel = newSelected != null;
            btnActCopy.IsEnabled = isSel;
            btnActDel.IsEnabled = isSel;
            btnActMoveDown.IsEnabled = isSel;
            btnActMoveUp.IsEnabled = isSel;

            if (lstAct.isLastItemSelected()) {
                btnActMoveDown.IsEnabled = false;
            }
            if (lstAct.isFirstItemSelected()) {
                btnActMoveUp.IsEnabled = false;
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

        private void btnNewCond_Click(object sender, RoutedEventArgs e) {
             new AddCondAct("condition", getConditionTemplates(), (TriggerContent c) => {
                if (c is Condition) {
                    Condition a = c as Condition;
                    addVisualCondition(a);
                     TabControl_SelectionChanged(null, null);
                     return;
                }
                throw new NotImplementedException();
            });
        }

        private void btnActNew_Click(object sender, RoutedEventArgs e) {
            new AddCondAct("action", getActionTemplates(), (TriggerContent c) => {
                if (c is Action) {
                    Action a = c as Action;
                    addVisualAction(a);
                    TabControl_SelectionChanged(null, null);
                    return;
                }
                throw new NotImplementedException();
            });
           
        }

        private class ConstrucableTriggerTemplate : ConstrucableTrigger {

            private string _name;
            private Func<TriggerContent> _construct;

            public ConstrucableTriggerTemplate(string name, Func<TriggerContent> construct) {
                _name = name;
                _construct = construct;
            }

            public TriggerContent construct() {
                return _construct();
            }

            public string getTriggerName() {
                return _name;
            }
        }

        private List<ConstrucableTrigger> getActionTemplates() {
            List<ConstrucableTrigger> lst = new List<ConstrucableTrigger>();
            lst.Add(new ConstrucableTriggerTemplate("Center View", () => new ActionCenterView(new WeakParser(ActionCenterView.getComponents(), ActionCenterView.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Comment", () => new ActionComment(new WeakParser(ActionComment.getComponents(), ActionComment.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Create Unit", () => new ActionCreateUnit(new WeakParser(ActionCreateUnit.getComponents(), ActionCreateUnit.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Create Unit With Properties", () => new ActionCreateUnitWithProperties(new WeakParser(ActionCreateUnitWithProperties.getComponents(), ActionCreateUnitWithProperties.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Defeat", () => new ActionDefeat(new WeakParser(ActionDefeat.getComponents(), ActionDefeat.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Display Text Message", () => new ActionDisplayTextMessage(new WeakParser(ActionDisplayTextMessage.getComponents(), ActionDisplayTextMessage.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Draw", () => new ActionDraw(new WeakParser(ActionDraw.getComponents(), ActionDraw.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Give Units To Player", () => new ActionGiveUnitsToPlayer(new WeakParser(ActionGiveUnitsToPlayer.getComponents(), ActionGiveUnitsToPlayer.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Kill Unit", () => new ActionKillUnit(new WeakParser(ActionKillUnit.getComponents(), ActionKillUnit.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Kill Unit At Location", () => new ActionKillUnitAtLocation(new WeakParser(ActionKillUnitAtLocation.getComponents(), ActionKillUnitAtLocation.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Control At Location", () => new ActionLeaderBoardControlAtLocation(new WeakParser(ActionLeaderBoardControlAtLocation.getComponents(), ActionLeaderBoardControlAtLocation.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Control", () => new ActionLeaderBoardControl(new WeakParser(ActionLeaderBoardControl.getComponents(), ActionLeaderBoardControl.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Deaths", () => new ActionLeaderBoardDeaths(new ActionLeaderBoardKills(new WeakParser(ActionLeaderBoardKills.getComponents(), ActionLeaderBoardKills.getTextMapping())))));
            lst.Add(new ConstrucableTriggerTemplate("Leader board Greed", () => new ActionLeaderboardGreed(new WeakParser(ActionLeaderboardGreed.getComponents(), ActionLeaderboardGreed.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Kills", () => new ActionLeaderBoardKills(new WeakParser(ActionLeaderBoardKills.getComponents(), ActionLeaderBoardKills.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Points", () => new ActionLeaderBoardPoints(new WeakParser(ActionLeaderBoardPoints.getComponents(), ActionLeaderBoardPoints.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Resources", () => new ActionLeaderBoardResources(new WeakParser(ActionLeaderBoardResources.getComponents(), ActionLeaderBoardResources.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Computer Players", () => new ActionLeaderboardComputerPlayers(new WeakParser(ActionLeaderboardComputerPlayers.getComponents(), ActionLeaderboardComputerPlayers.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Goal Control At Location", () => new ActionLeaderboardGoalControlAtLocation(new WeakParser(ActionLeaderboardGoalControlAtLocation.getComponents(), ActionLeaderboardGoalControlAtLocation.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Goal Control", () => new ActionLeaderboardGoalControl(new WeakParser(ActionLeaderboardGoalControl.getComponents(), ActionLeaderboardGoalControl.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Goal Kills", () => new ActionLeaderboardGoalKills(new WeakParser(ActionLeaderboardGoalKills.getComponents(), ActionLeaderboardGoalKills.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Goal Points", () => new ActionLeaderboardGoalPoints(new WeakParser(ActionLeaderboardGoalPoints.getComponents(), ActionLeaderboardGoalPoints.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Leader Board Goal Resources", () => new ActionLeaderboardGoalResources(new WeakParser(ActionLeaderboardGoalResources.getComponents(), ActionLeaderboardGoalResources.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Minimap Ping", () => new ActionMinimapPing(new WeakParser(ActionMinimapPing.getComponents(), ActionMinimapPing.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Modify Unit Energy", () => new ActionModifyUnitEnergy(new WeakParser(ActionModifyUnitEnergy.getComponents(), ActionModifyUnitEnergy.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Modify Unit Hanger Count", () => new ActionModifyUnitHangerCount(new WeakParser(ActionModifyUnitHangerCount.getComponents(), ActionModifyUnitHangerCount.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Modify Unit Hit Points", () => new ActionModifyUnitHitPoints(new WeakParser(ActionModifyUnitHitPoints.getComponents(), ActionModifyUnitHitPoints.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Modify Unit Resource Amount", () => new ActionModifyUnitResourceAmount(new WeakParser(ActionModifyUnitResourceAmount.getComponents(), ActionModifyUnitResourceAmount.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Modify Unit Shield Points", () => new ActionModifyUnitShieldPoints(new WeakParser(ActionModifyUnitShieldPoints.getComponents(), ActionModifyUnitShieldPoints.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Move Location", () => new ActionMoveLocation(new WeakParser(ActionMoveLocation.getComponents(), ActionMoveLocation.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Play WAV", () => new ActionPlayWAV(new WeakParser(ActionPlayWAV.getComponents(), ActionPlayWAV.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Move Unit", () => new ActionMoveUnit(new WeakParser(ActionMoveUnit.getComponents(), ActionMoveUnit.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Mute Unit Speech", () => new ActionMuteUnitSpeech(new WeakParser(ActionMuteUnitSpeech.getComponents(), ActionMuteUnitSpeech.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Order", () => new ActionOrder(new WeakParser(ActionOrder.getComponents(), ActionOrder.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Pause Game", () => new ActionPauseGame(new WeakParser(ActionPauseGame.getComponents(), ActionPauseGame.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Pause Timer", () => new ActionPauseTimer(new WeakParser(ActionPauseTimer.getComponents(), ActionPauseTimer.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Preserve Trigger", () => new ActionPreserveTrigger(new WeakParser(ActionPreserveTrigger.getComponents(), ActionPreserveTrigger.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Remove Unit", () => new ActionRemoveUnit(new WeakParser(ActionRemoveUnit.getComponents(), ActionRemoveUnit.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Remove Unit At Location", () => new ActionRemoveUnitAtLocation(new WeakParser(ActionRemoveUnitAtLocation.getComponents(), ActionRemoveUnitAtLocation.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Run AI Script", () => new ActionRunAIScript(new WeakParser(ActionRunAIScript.getComponents(), ActionRunAIScript.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Run AI Script At Location", () => new ActionRunAIScriptAtLocation(new WeakParser(ActionRunAIScriptAtLocation.getComponents(), ActionRunAIScriptAtLocation.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Set Alliance Status", () => new ActionSetAllianceStatus(new WeakParser(ActionSetAllianceStatus.getComponents(), ActionSetAllianceStatus.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Set Countdown Timer", () => new ActionSetCountdownTimer(new WeakParser(ActionSetCountdownTimer.getComponents(), ActionSetCountdownTimer.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Set Doodad State", () => new ActionSetDoodadState(new WeakParser(ActionSetDoodadState.getComponents(), ActionSetDoodadState.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Set Deaths", () => new ActionSetDeaths(new RawActionSetDeaths(new WeakParser(RawActionSetDeaths.getComponents(), RawActionSetDeaths.getTextMapping())))));
            lst.Add(new ConstrucableTriggerTemplate("Set Invincibility", () => new ActionSetInvincibility(new WeakParser(ActionSetInvincibility.getComponents(), ActionSetInvincibility.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Set Mission Objectives", () => new ActionSetMissionObjectives(new WeakParser(ActionSetMissionObjectives.getComponents(), ActionSetMissionObjectives.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Set Next Scenario", () => new ActionSetNextScenario(new WeakParser(ActionSetNextScenario.getComponents(), ActionSetNextScenario.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Set Resources", () => new ActionSetResources(new WeakParser(ActionSetResources.getComponents(), ActionSetResources.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Set Score", () => new ActionSetScore(new WeakParser(ActionSetScore.getComponents(), ActionSetScore.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Set Switch", () => new ActionSetSwitch(new WeakParser(ActionSetSwitch.getComponents(), ActionSetSwitch.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Talking Portrait", () => new ActionTalkingPortrait(new WeakParser(ActionTalkingPortrait.getComponents(), ActionTalkingPortrait.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Transmission", () => new ActionTransmission(new WeakParser(ActionTransmission.getComponents(), ActionTransmission.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Unmute Unit Speech", () => new ActionUnmuteUnitSpeech(new WeakParser(ActionUnmuteUnitSpeech.getComponents(), ActionUnmuteUnitSpeech.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Unpause Game", () => new ActionUnpauseGame(new WeakParser(ActionUnpauseGame.getComponents(), ActionUnpauseGame.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Unpause Timer", () => new ActionUnpauseTimer(new WeakParser(ActionUnpauseTimer.getComponents(), ActionUnpauseTimer.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Victory", () => new ActionVictory(new WeakParser(ActionVictory.getComponents(), ActionVictory.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Wait", () => new ActionWait(new WeakParser(ActionWait.getComponents(), ActionWait.getTextMapping()))));
            EPDAction.fillConstructables(lst);
            return lst;
        }

        internal class WeakParser : Parser {

            public WeakParser(TriggerContentTypeDescriptor[] fields, int[] textMapping) : base(getScanner(fields, textMapping)) { }

            private static src.Scanner getScanner(TriggerContentTypeDescriptor[] fields, int[] textMappingI) {
                int[] textMapping = new int[textMappingI.Length];
                for(int i=0;i<textMapping.Length;i++) {
                    textMapping[textMappingI[i]] = i;
                }


                StringBuilder sb = new StringBuilder();
                sb.Append("(");
                List<TriggerContentTypeDescriptor> usefulFields = new List<TriggerContentTypeDescriptor>();
                for (int i = 0; i < fields.Length; i++) {
                    if(!(fields[i] is TriggerContentTypeDescriptorVisual)) {
                        usefulFields.Add(fields[i]);
                    }
                }

                for (int i=0;i< usefulFields.Count;i++) {
                    TriggerContentTypeDescriptor whatNeedsToBeHere = usefulFields[textMapping[i]];
                    sb.Append(whatNeedsToBeHere.getDefaultValue().ToSaveString());
                    if(i != usefulFields.Count - 1) {
                        sb.Append(", ");
                    }
                }

                sb.Append(")");
                return new src.Scanner(sb.ToString());
            }

        }

        private List<ConstrucableTrigger> getConditionTemplates() {
            List<ConstrucableTrigger> lst = new List<ConstrucableTrigger>();
            lst.Add(new ConstrucableTriggerTemplate("Accumulate", () => new ConditionAccumulate(new WeakParser(ConditionAccumulate.getComponents(), ConditionAccumulate.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Always", () => new ConditionAlways(new WeakParser(ConditionAlways.getComponents(), ConditionAlways.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Bring", () => new ConditionBring(new WeakParser(ConditionBring.getComponents(), ConditionBring.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Commands", () => new ConditionCommand(new WeakParser(ConditionCommand.getComponents(), ConditionCommand.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Commands The Least", () => new ConditionCommandTheLeast(new WeakParser(ConditionCommandTheLeast.getComponents(), ConditionCommandTheLeast.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Commands The Least At", () => new ConditionCommandTheLeastAt(new WeakParser(ConditionCommandTheLeastAt.getComponents(), ConditionCommandTheLeastAt.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Commands The Most", () => new ConditionCommandTheMost(new WeakParser(ConditionCommandTheMost.getComponents(), ConditionCommandTheMost.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Commands The Most At", () => new ConditionCommandsTheMostAt(new WeakParser(ConditionCommandsTheMostAt.getComponents(), ConditionCommandsTheMostAt.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Countdown Timer", () => new ConditionCountdownTimer(new WeakParser(ConditionCountdownTimer.getComponents(), ConditionCountdownTimer.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Deaths", () => new ConditionDeaths(new WeakParser(ConditionDeaths.getComponents(), ConditionDeaths.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Elapsed Time", () => new ConditionElapsedTime(new WeakParser(ConditionElapsedTime.getComponents(), ConditionElapsedTime.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Highes tScore", () => new ConditionHighestScore(new WeakParser(ConditionHighestScore.getComponents(), ConditionHighestScore.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Kill", () => new ConditionKill(new WeakParser(ConditionKill.getComponents(), ConditionKill.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Least Kills", () => new ConditionLeastKills(new WeakParser(ConditionLeastKills.getComponents(), ConditionLeastKills.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Least Resources", () => new ConditionLeastResources(new WeakParser(ConditionLeastResources.getComponents(), ConditionLeastResources.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Lowest Score", () => new ConditionLowestScore(new WeakParser(ConditionLowestScore.getComponents(), ConditionLowestScore.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Most Kills", () => new ConditionMostKills(new WeakParser(ConditionMostKills.getComponents(), ConditionMostKills.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Most Resources", () => new ConditionMostResources(new WeakParser(ConditionMostResources.getComponents(), ConditionMostResources.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Never", () => new ConditionNever(new WeakParser(ConditionNever.getComponents(), ConditionNever.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Opponents", () => new ConditionOpponents(new WeakParser(ConditionOpponents.getComponents(), ConditionOpponents.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Score", () => new ConditionScore(new WeakParser(ConditionScore.getComponents(), ConditionScore.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Switch", () => new ConditionSwitch(new WeakParser(ConditionSwitch.getComponents(), ConditionSwitch.getTextMapping()))));
            lst.Add(new ConstrucableTriggerTemplate("Memory", () => new ConditionMemory(new WeakParser(ConditionMemory.getComponents(), ConditionMemory.getTextMapping()))));
            EPDCondition.fillConstructables(lst);
            return lst;
        }

    }

}
