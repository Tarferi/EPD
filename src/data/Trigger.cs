using System;
using System.Collections.Generic;
using StarcraftEPDTriggers.src;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows;
using StarcraftEPDTriggers.src.data;

namespace StarcraftEPDTriggers {

    public class Trigger {

        private int index;
        private static int _instances = 0;

        private List<Condition> conditions = new List<Condition>();
        private List<Action> actions = new List<Action>();
        private List<PlayerDef> affecteds = new List<PlayerDef>();
        private int flags = 0;

        private RichTextBox triggerListItem;

        public List<PlayerDef> getAffectedPlayers() {
            return affecteds;
        }

        public List<Condition> Conditions {
            get { return conditions; }
        }

        public List<Action> Actions {
            get { return actions; }
        }

        public Trigger() {
            index = _instances++;
        }

        public void reparseFromString(string str, src.data.TriggerCollection triggers, TriggerDefinitionPartProperties props) {
            Scanner s = new Scanner(str);
            Parser p = new Parser(s);
            if(p.parse(true)) {
                List<Trigger> trgs = p.getTriggers();
                if(trgs.Count == 1) {
                    Trigger t = trgs[0];
                    triggerListItem = (RichTextBox) t.getTrigerListItem(props);
                    conditions = t.conditions;
                    actions = t.actions;
                    affecteds = t.affecteds;
                    sb = t.sb;
                    return;
                }
            }
            throw new NotImplementedException();
        }

        public FrameworkElement getTrigerListItem(TriggerDefinitionPartProperties props) {
            //if (triggerListItem == null) {
                regenerateTriggerListItem(props); // Nasty fuck around
            //}
            return triggerListItem;
        }

        public void regenerateTriggerListItem(TriggerDefinitionPartProperties props) {
            props.g = new RichTextBox();
            regenerateTextboxForTriggerDefinitionParts(props, getParts, (RichTextBox gg) => { triggerListItem = gg; });
        }

        public static void regenerateTextboxForTriggerDefinitionParts(TriggerDefinitionPartProperties props, Func<List<TriggerDefinitionPart>> getPartsF, Action<RichTextBox> triggerListItemSetter) {
            RichTextBox g = props.g;
            triggerListItemSetter(g);
            g.Document.Blocks.Clear();
            g.Document.Blocks.Add(new Paragraph());
            g.IsDocumentEnabled = true;
            g.IsReadOnly = true;
            g.Cursor = Cursors.Arrow;
            Brush[] backgrounds = new Brush[] { new SolidColorBrush(Color.FromRgb(240,240,240)), new SolidColorBrush(Color.FromRgb(220, 220, 220)) };
            Brush backgroundBrush = backgrounds[0];
            g.SetValue(Paragraph.LineHeightProperty, 1.0);
            List<TriggerDefinitionPart> parts = getPartsF();
            Func<Func<object>, bool, string> placeHolderToString = (Func<object> placeHolder, bool allowNewline) => {
                object output = placeHolder();
                if(output is string) {
                    string text = output as string;
                    if(!allowNewline) {
                        text = text.Replace("\r", "\\r").Replace("\n", "\\n");
                    }
                    return text;
                } else {
                    return output.ToString();
                }
            };

            foreach (TriggerDefinitionPart part in parts) {
                part.render(props);
            }
        }

        public void addAffectName(Token affectName) {
            this.affecteds.Add(affectName.toPlayerDef());
        }

        public void addCondition(Condition c) {
            conditions.Add(c);
        }

        public void addAction(Action a) {
            actions.Add(a);
        }

        public List<Action> getActions() {
            return actions;
        }

        private StringBuilder sb = new StringBuilder();

        public void addToken(Token t) {
            if (t != null) {
                sb.Append(t.getContent());
            }
        }

        public List<TriggerDefinitionPart> getParts() {
            List<TriggerDefinitionPart> parts = new List<TriggerDefinitionPart>();
            parts.Add(new TriggerDefinitionLabel("Conditions:"));
            int conditionsCounter = 0;
            int conditionsTotal = conditions.Count;
            foreach (Condition cond in conditions) {
                conditionsCounter++;
                parts.Add(new TriggerDefinitionParagraphStart());
                foreach (TriggerDefinitionPart condPart in cond.getDefinitionParts()) {
                    parts.Add(condPart);
                }
                if (conditionsCounter == conditionsTotal) {
                    parts.Add(new TriggerDefinitionParagraphEnd());
                }
            }
            parts.Add(new TriggerDefinitionLabel("Actions:"));
            int actionCounter = 0;
            int actionsTotal = actions.Count;
            foreach (Action action in actions) {
                actionCounter++;
                parts.Add(new TriggerDefinitionParagraphStart());
                if (action is ActionComment && !MainWindow.IgnoreComments) {
                    string title = action.ToString();
                    parts.Clear();
                    parts.Add(new TriggerDefinitionLabel(title));
                    return parts;
                }
                foreach (TriggerDefinitionPart condPart in action.getDefinitionParts()) {
                    parts.Add(condPart);
                }
                if (actionCounter == actionsTotal) {
                    parts.Add(new TriggerDefinitionParagraphEnd());
                }
            }
            return parts;
        }

        public override string ToString() {
            return "Trigger " + index;
        }

        public string ToSaveString() {
            StringBuilder sb = new StringBuilder();

            // Part 1
            sb.Append("Trigger(");
            int affectedCount = affecteds.Count;
            int counter = 0;
            if (affectedCount != 1) {
                foreach(PlayerDef affected in affecteds) {
                    counter++;
                    sb.Append(affected.GroupIndex);
                    if(counter!=affectedCount) {
                        sb.Append(", ");
                    }
                }
            } else {
                sb.Append(affecteds[0].GroupIndex);
            }
            sb.Append("){\r\n");

            // Part 2
            sb.Append("Conditions:\r\n");
            foreach (Condition cond in conditions) {
                sb.Append("\t"+cond.ToSaveString() + "\r\n");
            }
            sb.Append("\r\n");

            // Part 3
            sb.Append("Actions:\r\n");
            foreach (Action action in actions) {
                sb.Append("\t"+action.ToSaveString() + "\r\n");
            }
            if(flags != 0) {
                StringBuilder bsb = new StringBuilder();
                for(int i = 31; i >= 0; i--) {
                    char flag = (flags & (1 << i)) > 0 ? '1' : '0';
                    bsb.Append(flag);
                }
                sb.Append("Flags:\r\n" + bsb.ToString() + ";\r\n");
            }
            sb.Append("}\r\n\r\n");
            sb.Append("//-----------------------------------------------------------------//");
            sb.Append("\r\n");
            return sb.ToString();
        }

        public List<Condition> getConditoins() {
            return conditions;
        }

        public void setFlags(int flags) {
            this.flags = flags;
        }
    }

    public interface SaveableItem {

        string ToSaveString();

    }

    public class AdvancedPropertiesDef : Gettable<AdvancedPropertiesDef>, SaveableItem {

        private int _value;

        public AdvancedPropertiesDef(int number) {
            _value = number;
        }

        public static AdvancedPropertiesDef getByIndex(int arg) {
            return new AdvancedPropertiesDef(arg);
        }

        public int getIndex() {
            return _value;
        }

        public override string ToString() {
            return "Advanced properties";
        }

        public int getMaxValue() {
            return int.MaxValue;
        }

        public TriggerDefinitionPart getTriggerPart(Func<AdvancedPropertiesDef> getter, Action<AdvancedPropertiesDef> setter, Func<AdvancedPropertiesDef> getDefault) {
            return new TriggerDefinitionAdvancedPropertiesDef(getter, setter, getDefault);
        }

        public string ToSaveString() {
            throw new NotImplementedException();
        }
    }

    public class AIScriptDef : SaveableItem, Gettable<AIScriptDef> {

        private string _value;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return "\"" + ToString() + "\"";
        }

        private AIScriptDef(string value) {
            _value = value;
        }

        public static AIScriptDef getByStringValue(string value) {
            foreach (AIScriptDef script in AllScripts) {
                if (script.Equals(value)) {
                    return script;
                }
            }
            return new AIScriptDef(value); // This is so wrong
        }


        public static readonly AIScriptDef SendAllUnitsonStrategicSuicideMissions = new AIScriptDef("Send All Units on Strategic Suicide Missions");
        public static readonly AIScriptDef SendAllUnitsonRandomSuicideMissions = new AIScriptDef("Send All Units on Random Suicide Missions");
        public static readonly AIScriptDef SwitchComputerPlayertoRescuePassive = new AIScriptDef("Switch Computer Player to Rescue Passive");
        public static readonly AIScriptDef TurnONSharedVisionforPlayer1 = new AIScriptDef("Turn ON Shared Vision for Player 1");
        public static readonly AIScriptDef TurnONSharedVisionforPlayer2 = new AIScriptDef("Turn ON Shared Vision for Player 2");
        public static readonly AIScriptDef TurnONSharedVisionforPlayer3 = new AIScriptDef("Turn ON Shared Vision for Player 3");
        public static readonly AIScriptDef TurnONSharedVisionforPlayer4 = new AIScriptDef("Turn ON Shared Vision for Player 4");
        public static readonly AIScriptDef TurnONSharedVisionforPlayer5 = new AIScriptDef("Turn ON Shared Vision for Player 5");
        public static readonly AIScriptDef TurnONSharedVisionforPlayer6 = new AIScriptDef("Turn ON Shared Vision for Player 6");
        public static readonly AIScriptDef TurnONSharedVisionforPlayer7 = new AIScriptDef("Turn ON Shared Vision for Player 7");
        public static readonly AIScriptDef TurnONSharedVisionforPlayer8 = new AIScriptDef("Turn ON Shared Vision for Player 8");
        public static readonly AIScriptDef TurnOFFSharedVisionforPlayer1 = new AIScriptDef("Turn OFF Shared Vision for Player 1");
        public static readonly AIScriptDef TurnOFFSharedVisionforPlayer2 = new AIScriptDef("Turn OFF Shared Vision for Player 2");
        public static readonly AIScriptDef TurnOFFSharedVisionforPlayer3 = new AIScriptDef("Turn OFF Shared Vision for Player 3");
        public static readonly AIScriptDef TurnOFFSharedVisionforPlayer4 = new AIScriptDef("Turn OFF Shared Vision for Player 4");
        public static readonly AIScriptDef TurnOFFSharedVisionforPlayer5 = new AIScriptDef("Turn OFF Shared Vision for Player 5");
        public static readonly AIScriptDef TurnOFFSharedVisionforPlayer6 = new AIScriptDef("Turn OFF Shared Vision for Player 6");
        public static readonly AIScriptDef TurnOFFSharedVisionforPlayer7 = new AIScriptDef("Turn OFF Shared Vision for Player 7");
        public static readonly AIScriptDef TurnOFFSharedVisionforPlayer8 = new AIScriptDef("Turn OFF Shared Vision for Player 8");

        public static AIScriptDef getDefaultValue() {
            return AIScriptDef.SendAllUnitsonRandomSuicideMissions;
        }

        public int getMaxValue() {
            return AllScripts.Length - 1;
        }

        public int getIndex() {
            int index = 0;
            foreach (AIScriptDef script in AllScripts) {
                if (script == this) {
                    return index;
                }
                index++;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<AIScriptDef> getter, Action<AIScriptDef> setter, Func<AIScriptDef> getDefault) {
            return new TriggerDefinitionGeneralDef<AIScriptDef>(getter, setter, getDefault, AllScripts);
        }

        public static readonly AIScriptDef[] AllScripts = new AIScriptDef[] {
            SendAllUnitsonStrategicSuicideMissions,
            SendAllUnitsonRandomSuicideMissions,
            SwitchComputerPlayertoRescuePassive,
            TurnONSharedVisionforPlayer1,
            TurnONSharedVisionforPlayer2,
            TurnONSharedVisionforPlayer3,
            TurnONSharedVisionforPlayer4,
            TurnONSharedVisionforPlayer5,
            TurnONSharedVisionforPlayer6,
            TurnONSharedVisionforPlayer7,
            TurnONSharedVisionforPlayer8,
            TurnOFFSharedVisionforPlayer1,
            TurnOFFSharedVisionforPlayer2,
            TurnOFFSharedVisionforPlayer3,
            TurnOFFSharedVisionforPlayer4,
            TurnOFFSharedVisionforPlayer5,
            TurnOFFSharedVisionforPlayer6,
            TurnOFFSharedVisionforPlayer7,
            TurnOFFSharedVisionforPlayer8,
        };
    }

    public class AIScriptAtDef : SaveableItem, Gettable<AIScriptAtDef> {

        private string _value;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return "\"" + ToString() + "\"";
        }

        private AIScriptAtDef(string value) {
            _value = value;
        }

        public static AIScriptAtDef getByStringValue(string value) {
            foreach (AIScriptAtDef script in AllScripts) {
                if (script.Equals(value)) {
                    return script;
                }
            }
            return new AIScriptAtDef(value); // This is so wrong
        }

        public static AIScriptAtDef getDefaultValue() {
            return AIScriptAtDef.AINukeHere;
        }

        public int getMaxValue() {
            return AllScripts.Length - 1;
        }

        public int getIndex() {
            int index = 0;
            foreach (AIScriptAtDef script in AllScripts) {
                if (script == this) {
                    return index;
                }
                index++;
            }
            throw new NotImplementedException();
        }


        public TriggerDefinitionPart getTriggerPart(Func<AIScriptAtDef> getter, Action<AIScriptAtDef> setter, Func<AIScriptAtDef> getDefault) {
            return new TriggerDefinitionGeneralDef<AIScriptAtDef>(getter, setter, getDefault, AllScripts);
        }

        public static readonly AIScriptAtDef Terran3ZergTown = new AIScriptAtDef("Terran 3 Zerg Town");
        public static readonly AIScriptAtDef Terran5TerranMainTown = new AIScriptAtDef("Terran 5 Terran Main Town");
        public static readonly AIScriptAtDef Terran5TerranHarvestTown = new AIScriptAtDef("Terran 5 Terran Harvest Town");
        public static readonly AIScriptAtDef Terran6AirAttackZerg = new AIScriptAtDef("Terran 6 Air Attack Zerg");
        public static readonly AIScriptAtDef Terran6GroundAttackZerg = new AIScriptAtDef("Terran 6 Ground Attack Zerg");
        public static readonly AIScriptAtDef Terran6ZergSupportTown = new AIScriptAtDef("Terran 6 Zerg Support Town");
        public static readonly AIScriptAtDef Terran7BottomZergTown = new AIScriptAtDef("Terran 7 Bottom Zerg Town");
        public static readonly AIScriptAtDef Terran7RightZergTown = new AIScriptAtDef("Terran 7 Right Zerg Town");
        public static readonly AIScriptAtDef Terran7MiddleZergTown = new AIScriptAtDef("Terran 7 Middle Zerg Town");
        public static readonly AIScriptAtDef Terran8ConfederateTown = new AIScriptAtDef("Terran 8 Confederate Town");
        public static readonly AIScriptAtDef Terran9LightAttack = new AIScriptAtDef("Terran 9 Light Attack");
        public static readonly AIScriptAtDef Terran9HeavyAttack = new AIScriptAtDef("Terran 9 Heavy Attack");
        public static readonly AIScriptAtDef Terran10ConfederateTowns = new AIScriptAtDef("Terran 10 Confederate Towns");
        public static readonly AIScriptAtDef Terran11ZergTown = new AIScriptAtDef("Terran 11 Zerg Town");
        public static readonly AIScriptAtDef Terran11LowerProtossTown = new AIScriptAtDef("Terran 11 Lower Protoss Town");
        public static readonly AIScriptAtDef Terran11UpperProtossTown = new AIScriptAtDef("Terran 11 Upper Protoss Town");
        public static readonly AIScriptAtDef Terran12NukeTown = new AIScriptAtDef("Terran 12 Nuke Town");
        public static readonly AIScriptAtDef Terran12PhoenixTown = new AIScriptAtDef("Terran 12 Phoenix Town");
        public static readonly AIScriptAtDef Terran12TankTown = new AIScriptAtDef("Terran 12 Tank Town");
        public static readonly AIScriptAtDef Terran1ElectronicDistribution = new AIScriptAtDef("Terran 1 Electronic Distribution");
        public static readonly AIScriptAtDef Terran2ElectronicDistribution = new AIScriptAtDef("Terran 2 Electronic Distribution");
        public static readonly AIScriptAtDef Terran3ElectronicDistribution = new AIScriptAtDef("Terran 3 Electronic Distribution");
        public static readonly AIScriptAtDef Terran1Shareware = new AIScriptAtDef("Terran 1 Shareware");
        public static readonly AIScriptAtDef Terran2Shareware = new AIScriptAtDef("Terran 2 Shareware");
        public static readonly AIScriptAtDef Terran3Shareware = new AIScriptAtDef("Terran 3 Shareware");
        public static readonly AIScriptAtDef Terran4Shareware = new AIScriptAtDef("Terran 4 Shareware");
        public static readonly AIScriptAtDef Terran5Shareware = new AIScriptAtDef("Terran 5 Shareware");
        public static readonly AIScriptAtDef Zerg1TerranTown = new AIScriptAtDef("Zerg 1 Terran Town");
        public static readonly AIScriptAtDef Zerg2ProtossTown = new AIScriptAtDef("Zerg 2 Protoss Town");
        public static readonly AIScriptAtDef Zerg3TerranTown = new AIScriptAtDef("Zerg 3 Terran Town");
        public static readonly AIScriptAtDef Zerg4RightTerranTown = new AIScriptAtDef("Zerg 4 Right Terran Town");
        public static readonly AIScriptAtDef Zerg4LowerTerranTown = new AIScriptAtDef("Zerg 4 Lower Terran Town");
        public static readonly AIScriptAtDef Zerg6ProtossTown = new AIScriptAtDef("Zerg 6 Protoss Town");
        public static readonly AIScriptAtDef Zerg7AirTown = new AIScriptAtDef("Zerg 7 Air Town");
        public static readonly AIScriptAtDef Zerg7GroundTown = new AIScriptAtDef("Zerg 7 Ground Town");
        public static readonly AIScriptAtDef Zerg7SupportTown = new AIScriptAtDef("Zerg 7 Support Town");
        public static readonly AIScriptAtDef Zerg8ScoutTown = new AIScriptAtDef("Zerg 8 Scout Town");
        public static readonly AIScriptAtDef Zerg8TemplarTown = new AIScriptAtDef("Zerg 8 Templar Town");
        public static readonly AIScriptAtDef Zerg9TealProtoss = new AIScriptAtDef("Zerg 9 Teal Protoss");
        public static readonly AIScriptAtDef Zerg9LeftYellowProtoss = new AIScriptAtDef("Zerg 9 Left Yellow Protoss");
        public static readonly AIScriptAtDef Zerg9RightYellowProtoss = new AIScriptAtDef("Zerg 9 Right Yellow Protoss");
        public static readonly AIScriptAtDef Zerg9LeftOrangeProtoss = new AIScriptAtDef("Zerg 9 Left Orange Protoss");
        public static readonly AIScriptAtDef Zerg9RightOrangeProtoss = new AIScriptAtDef("Zerg 9 Right Orange Protoss");
        public static readonly AIScriptAtDef Zerg10LeftTealAttack = new AIScriptAtDef("Zerg 10 Left Teal (Attack)");
        public static readonly AIScriptAtDef Zerg10RightTealSupport = new AIScriptAtDef("Zerg 10 Right Teal (Support)");
        public static readonly AIScriptAtDef Zerg10LeftYellowSupport = new AIScriptAtDef("Zerg 10 Left Yellow (Support)");
        public static readonly AIScriptAtDef Zerg10RightYellowAttack = new AIScriptAtDef("Zerg 10 Right Yellow (Attack)");
        public static readonly AIScriptAtDef Zerg10RedProtoss = new AIScriptAtDef("Zerg 10 Red Protoss");
        public static readonly AIScriptAtDef Protoss1ZergTown = new AIScriptAtDef("Protoss 1 Zerg Town");
        public static readonly AIScriptAtDef Protoss2ZergTown = new AIScriptAtDef("Protoss 2 Zerg Town");
        public static readonly AIScriptAtDef Protoss3AirZergTown = new AIScriptAtDef("Protoss 3 Air Zerg Town");
        public static readonly AIScriptAtDef Protoss3GroundZergTown = new AIScriptAtDef("Protoss 3 Ground Zerg Town");
        public static readonly AIScriptAtDef Protoss4ZergTown = new AIScriptAtDef("Protoss 4 Zerg Town");
        public static readonly AIScriptAtDef Protoss5ZergTownIsland = new AIScriptAtDef("Protoss 5 Zerg Town Island");
        public static readonly AIScriptAtDef Protoss5ZergTownBase = new AIScriptAtDef("Protoss 5 Zerg Town Base");
        public static readonly AIScriptAtDef Protoss7LeftProtossTown = new AIScriptAtDef("Protoss 7 Left Protoss Town");
        public static readonly AIScriptAtDef Protoss7RightProtossTown = new AIScriptAtDef("Protoss 7 Right Protoss Town");
        public static readonly AIScriptAtDef Protoss7ShrineProtoss = new AIScriptAtDef("Protoss 7 Shrine Protoss");
        public static readonly AIScriptAtDef Protoss8LeftProtossTown = new AIScriptAtDef("Protoss 8 Left Protoss Town");
        public static readonly AIScriptAtDef Protoss8RightProtossTown = new AIScriptAtDef("Protoss 8 Right Protoss Town");
        public static readonly AIScriptAtDef Protoss8ProtossDefenders = new AIScriptAtDef("Protoss 8 Protoss Defenders");
        public static readonly AIScriptAtDef Protoss9GroundZerg = new AIScriptAtDef("Protoss 9 Ground Zerg");
        public static readonly AIScriptAtDef Protoss9AirZerg = new AIScriptAtDef("Protoss 9 Air Zerg");
        public static readonly AIScriptAtDef Protoss9SpellZerg = new AIScriptAtDef("Protoss 9 Spell Zerg");
        public static readonly AIScriptAtDef Protoss10MiniTowns = new AIScriptAtDef("Protoss 10 Mini-Towns");
        public static readonly AIScriptAtDef Protoss10MiniTownMaster = new AIScriptAtDef("Protoss 10 Mini-Town Master");
        public static readonly AIScriptAtDef Protoss10OvermindDefenders = new AIScriptAtDef("Protoss 10 Overmind Defenders");
        public static readonly AIScriptAtDef TerranCustomLevel = new AIScriptAtDef("Terran Custom Level");
        public static readonly AIScriptAtDef ZergCustomLevel = new AIScriptAtDef("Zerg Custom Level");
        public static readonly AIScriptAtDef ProtossCustomLevel = new AIScriptAtDef("Protoss Custom Level");
        public static readonly AIScriptAtDef TerranCampaignEasy = new AIScriptAtDef("Terran Campaign Easy");
        public static readonly AIScriptAtDef TerranCampaignMedium = new AIScriptAtDef("Terran Campaign Medium");
        public static readonly AIScriptAtDef TerranCampaignDifficult = new AIScriptAtDef("Terran Campaign Difficult");
        public static readonly AIScriptAtDef TerranCampaignInsane = new AIScriptAtDef("Terran Campaign Insane");
        public static readonly AIScriptAtDef TerranCampaignAreaTown = new AIScriptAtDef("Terran Campaign Area Town");
        public static readonly AIScriptAtDef ZergCampaignEasy = new AIScriptAtDef("Zerg Campaign Easy");
        public static readonly AIScriptAtDef ZergCampaignMedium = new AIScriptAtDef("Zerg Campaign Medium");
        public static readonly AIScriptAtDef ZergCampaignDifficult = new AIScriptAtDef("Zerg Campaign Difficult");
        public static readonly AIScriptAtDef ZergCampaignInsane = new AIScriptAtDef("Zerg Campaign Insane");
        public static readonly AIScriptAtDef ZergCampaignAreaTown = new AIScriptAtDef("Zerg Campaign Area Town");
        public static readonly AIScriptAtDef ProtossCampaignEasy = new AIScriptAtDef("Protoss Campaign Easy");
        public static readonly AIScriptAtDef ProtossCampaignMedium = new AIScriptAtDef("Protoss Campaign Medium");
        public static readonly AIScriptAtDef ProtossCampaignDifficult = new AIScriptAtDef("Protoss Campaign Difficult");
        public static readonly AIScriptAtDef ProtossCampaignInsane = new AIScriptAtDef("Protoss Campaign Insane");
        public static readonly AIScriptAtDef ProtossCampaignAreaTown = new AIScriptAtDef("Protoss Campaign Area Town");
        public static readonly AIScriptAtDef MoveDarkTemplarstoRegion = new AIScriptAtDef("Move Dark Templars to Region");
        public static readonly AIScriptAtDef ClearPreviousCombatData = new AIScriptAtDef("Clear Previous Combat Data");
        public static readonly AIScriptAtDef SetPlayertoEnemy = new AIScriptAtDef("Set Player to Enemy");
        public static readonly AIScriptAtDef SetPlayertoAlly = new AIScriptAtDef("Set Player to Ally");
        public static readonly AIScriptAtDef ValueThisAreaHigher = new AIScriptAtDef("Value This Area Higher");
        public static readonly AIScriptAtDef EnterClosestBunker = new AIScriptAtDef("Enter Closest Bunker");
        public static readonly AIScriptAtDef SetGenericCommandTarget = new AIScriptAtDef("Set Generic Command Target");
        public static readonly AIScriptAtDef MakeTheseUnitsPatrol = new AIScriptAtDef("Make These Units Patrol");
        public static readonly AIScriptAtDef EnterTransport = new AIScriptAtDef("Enter Transport");
        public static readonly AIScriptAtDef ExitTransport = new AIScriptAtDef("Exit Transport");
        public static readonly AIScriptAtDef AINukeHere = new AIScriptAtDef("AI Nuke Here");
        public static readonly AIScriptAtDef AIHarassHere = new AIScriptAtDef("AI Harass Here");
        public static readonly AIScriptAtDef SetUnitOrderToJunkYardDog = new AIScriptAtDef("Set Unit Order To: Junk Yard Dog");
        public static readonly AIScriptAtDef BroodWarsProtoss1TownA = new AIScriptAtDef("Brood Wars Protoss 1 Town A");
        public static readonly AIScriptAtDef BroodWarsProtoss1TownB = new AIScriptAtDef("Brood Wars Protoss 1 Town B");
        public static readonly AIScriptAtDef BroodWarsProtoss1TownC = new AIScriptAtDef("Brood Wars Protoss 1 Town C");
        public static readonly AIScriptAtDef BroodWarsProtoss1TownD = new AIScriptAtDef("Brood Wars Protoss 1 Town D");
        public static readonly AIScriptAtDef BroodWarsProtoss1TownE = new AIScriptAtDef("Brood Wars Protoss 1 Town E");
        public static readonly AIScriptAtDef BroodWarsProtoss1TownF = new AIScriptAtDef("Brood Wars Protoss 1 Town F");
        public static readonly AIScriptAtDef BroodWarsProtoss2TownA = new AIScriptAtDef("Brood Wars Protoss 2 Town A");
        public static readonly AIScriptAtDef BroodWarsProtoss2TownB = new AIScriptAtDef("Brood Wars Protoss 2 Town B");
        public static readonly AIScriptAtDef BroodWarsProtoss2TownC = new AIScriptAtDef("Brood Wars Protoss 2 Town C");
        public static readonly AIScriptAtDef BroodWarsProtoss2TownD = new AIScriptAtDef("Brood Wars Protoss 2 Town D");
        public static readonly AIScriptAtDef BroodWarsProtoss2TownE = new AIScriptAtDef("Brood Wars Protoss 2 Town E");
        public static readonly AIScriptAtDef BroodWarsProtoss2TownF = new AIScriptAtDef("Brood Wars Protoss 2 Town F");
        public static readonly AIScriptAtDef BroodWarsProtoss3TownA = new AIScriptAtDef("Brood Wars Protoss 3 Town A");
        public static readonly AIScriptAtDef BroodWarsProtoss3TownB = new AIScriptAtDef("Brood Wars Protoss 3 Town B");
        public static readonly AIScriptAtDef BroodWarsProtoss3TownC = new AIScriptAtDef("Brood Wars Protoss 3 Town C");
        public static readonly AIScriptAtDef BroodWarsProtoss3TownD = new AIScriptAtDef("Brood Wars Protoss 3 Town D");
        public static readonly AIScriptAtDef BroodWarsProtoss3TownE = new AIScriptAtDef("Brood Wars Protoss 3 Town E");
        public static readonly AIScriptAtDef BroodWarsProtoss3TownF = new AIScriptAtDef("Brood Wars Protoss 3 Town F");
        public static readonly AIScriptAtDef BroodWarsProtoss4TownA = new AIScriptAtDef("Brood Wars Protoss 4 Town A");
        public static readonly AIScriptAtDef BroodWarsProtoss4TownB = new AIScriptAtDef("Brood Wars Protoss 4 Town B");
        public static readonly AIScriptAtDef BroodWarsProtoss4TownC = new AIScriptAtDef("Brood Wars Protoss 4 Town C");
        public static readonly AIScriptAtDef BroodWarsProtoss4TownD = new AIScriptAtDef("Brood Wars Protoss 4 Town D");
        public static readonly AIScriptAtDef BroodWarsProtoss4TownE = new AIScriptAtDef("Brood Wars Protoss 4 Town E");
        public static readonly AIScriptAtDef BroodWarsProtoss4TownF = new AIScriptAtDef("Brood Wars Protoss 4 Town F");
        public static readonly AIScriptAtDef BroodWarsProtoss5TownA = new AIScriptAtDef("Brood Wars Protoss 5 Town A");
        public static readonly AIScriptAtDef BroodWarsProtoss5TownB = new AIScriptAtDef("Brood Wars Protoss 5 Town B");
        public static readonly AIScriptAtDef BroodWarsProtoss5TownC = new AIScriptAtDef("Brood Wars Protoss 5 Town C");
        public static readonly AIScriptAtDef BroodWarsProtoss5TownD = new AIScriptAtDef("Brood Wars Protoss 5 Town D");
        public static readonly AIScriptAtDef BroodWarsProtoss5TownE = new AIScriptAtDef("Brood Wars Protoss 5 Town E");
        public static readonly AIScriptAtDef BroodWarsProtoss5TownF = new AIScriptAtDef("Brood Wars Protoss 5 Town F");
        public static readonly AIScriptAtDef BroodWarsProtoss6TownA = new AIScriptAtDef("Brood Wars Protoss 6 Town A");
        public static readonly AIScriptAtDef BroodWarsProtoss6TownB = new AIScriptAtDef("Brood Wars Protoss 6 Town B");
        public static readonly AIScriptAtDef BroodWarsProtoss6TownC = new AIScriptAtDef("Brood Wars Protoss 6 Town C");
        public static readonly AIScriptAtDef BroodWarsProtoss6TownD = new AIScriptAtDef("Brood Wars Protoss 6 Town D");
        public static readonly AIScriptAtDef BroodWarsProtoss6TownE = new AIScriptAtDef("Brood Wars Protoss 6 Town E");
        public static readonly AIScriptAtDef BroodWarsProtoss6TownF = new AIScriptAtDef("Brood Wars Protoss 6 Town F");
        public static readonly AIScriptAtDef BroodWarsProtoss7TownA = new AIScriptAtDef("Brood Wars Protoss 7 Town A");
        public static readonly AIScriptAtDef BroodWarsProtoss7TownB = new AIScriptAtDef("Brood Wars Protoss 7 Town B");
        public static readonly AIScriptAtDef BroodWarsProtoss7TownC = new AIScriptAtDef("Brood Wars Protoss 7 Town C");
        public static readonly AIScriptAtDef BroodWarsProtoss7TownD = new AIScriptAtDef("Brood Wars Protoss 7 Town D");
        public static readonly AIScriptAtDef BroodWarsProtoss7TownE = new AIScriptAtDef("Brood Wars Protoss 7 Town E");
        public static readonly AIScriptAtDef BroodWarsProtoss7TownF = new AIScriptAtDef("Brood Wars Protoss 7 Town F");
        public static readonly AIScriptAtDef BroodWarsProtoss8TownA = new AIScriptAtDef("Brood Wars Protoss 8 Town A");
        public static readonly AIScriptAtDef BroodWarsProtoss8TownB = new AIScriptAtDef("Brood Wars Protoss 8 Town B");
        public static readonly AIScriptAtDef BroodWarsProtoss8TownC = new AIScriptAtDef("Brood Wars Protoss 8 Town C");
        public static readonly AIScriptAtDef BroodWarsProtoss8TownD = new AIScriptAtDef("Brood Wars Protoss 8 Town D");
        public static readonly AIScriptAtDef BroodWarsProtoss8TownE = new AIScriptAtDef("Brood Wars Protoss 8 Town E");
        public static readonly AIScriptAtDef BroodWarsProtoss8TownF = new AIScriptAtDef("Brood Wars Protoss 8 Town F");
        public static readonly AIScriptAtDef BroodWarsTerran1TownA = new AIScriptAtDef("Brood Wars Terran 1 Town A");
        public static readonly AIScriptAtDef BroodWarsTerran1TownB = new AIScriptAtDef("Brood Wars Terran 1 Town B");
        public static readonly AIScriptAtDef BroodWarsTerran1TownC = new AIScriptAtDef("Brood Wars Terran 1 Town C");
        public static readonly AIScriptAtDef BroodWarsTerran1TownD = new AIScriptAtDef("Brood Wars Terran 1 Town D");
        public static readonly AIScriptAtDef BroodWarsTerran1TownE = new AIScriptAtDef("Brood Wars Terran 1 Town E");
        public static readonly AIScriptAtDef BroodWarsTerran1TownF = new AIScriptAtDef("Brood Wars Terran 1 Town F");
        public static readonly AIScriptAtDef BroodWarsTerran2TownA = new AIScriptAtDef("Brood Wars Terran 2 Town A");
        public static readonly AIScriptAtDef BroodWarsTerran2TownB = new AIScriptAtDef("Brood Wars Terran 2 Town B");
        public static readonly AIScriptAtDef BroodWarsTerran2TownC = new AIScriptAtDef("Brood Wars Terran 2 Town C");
        public static readonly AIScriptAtDef BroodWarsTerran2TownD = new AIScriptAtDef("Brood Wars Terran 2 Town D");
        public static readonly AIScriptAtDef BroodWarsTerran2TownE = new AIScriptAtDef("Brood Wars Terran 2 Town E");
        public static readonly AIScriptAtDef BroodWarsTerran2TownF = new AIScriptAtDef("Brood Wars Terran 2 Town F");
        public static readonly AIScriptAtDef BroodWarsTerran3TownA = new AIScriptAtDef("Brood Wars Terran 3 Town A");
        public static readonly AIScriptAtDef BroodWarsTerran3TownB = new AIScriptAtDef("Brood Wars Terran 3 Town B");
        public static readonly AIScriptAtDef BroodWarsTerran3TownC = new AIScriptAtDef("Brood Wars Terran 3 Town C");
        public static readonly AIScriptAtDef BroodWarsTerran3TownD = new AIScriptAtDef("Brood Wars Terran 3 Town D");
        public static readonly AIScriptAtDef BroodWarsTerran3TownE = new AIScriptAtDef("Brood Wars Terran 3 Town E");
        public static readonly AIScriptAtDef BroodWarsTerran3TownF = new AIScriptAtDef("Brood Wars Terran 3 Town F");
        public static readonly AIScriptAtDef BroodWarsTerran4TownA = new AIScriptAtDef("Brood Wars Terran 4 Town A");
        public static readonly AIScriptAtDef BroodWarsTerran4TownB = new AIScriptAtDef("Brood Wars Terran 4 Town B");
        public static readonly AIScriptAtDef BroodWarsTerran4TownC = new AIScriptAtDef("Brood Wars Terran 4 Town C");
        public static readonly AIScriptAtDef BroodWarsTerran4TownD = new AIScriptAtDef("Brood Wars Terran 4 Town D");
        public static readonly AIScriptAtDef BroodWarsTerran4TownE = new AIScriptAtDef("Brood Wars Terran 4 Town E");
        public static readonly AIScriptAtDef BroodWarsTerran4TownF = new AIScriptAtDef("Brood Wars Terran 4 Town F");
        public static readonly AIScriptAtDef BroodWarsTerran5TownA = new AIScriptAtDef("Brood Wars Terran 5 Town A");
        public static readonly AIScriptAtDef BroodWarsTerran5TownB = new AIScriptAtDef("Brood Wars Terran 5 Town B");
        public static readonly AIScriptAtDef BroodWarsTerran5TownC = new AIScriptAtDef("Brood Wars Terran 5 Town C");
        public static readonly AIScriptAtDef BroodWarsTerran5TownD = new AIScriptAtDef("Brood Wars Terran 5 Town D");
        public static readonly AIScriptAtDef BroodWarsTerran5TownE = new AIScriptAtDef("Brood Wars Terran 5 Town E");
        public static readonly AIScriptAtDef BroodWarsTerran5TownF = new AIScriptAtDef("Brood Wars Terran 5 Town F");
        public static readonly AIScriptAtDef BroodWarsTerran6TownA = new AIScriptAtDef("Brood Wars Terran 6 Town A");
        public static readonly AIScriptAtDef BroodWarsTerran6TownB = new AIScriptAtDef("Brood Wars Terran 6 Town B");
        public static readonly AIScriptAtDef BroodWarsTerran6TownC = new AIScriptAtDef("Brood Wars Terran 6 Town C");
        public static readonly AIScriptAtDef BroodWarsTerran6TownD = new AIScriptAtDef("Brood Wars Terran 6 Town D");
        public static readonly AIScriptAtDef BroodWarsTerran6TownE = new AIScriptAtDef("Brood Wars Terran 6 Town E");
        public static readonly AIScriptAtDef BroodWarsTerran6TownF = new AIScriptAtDef("Brood Wars Terran 6 Town F");
        public static readonly AIScriptAtDef BroodWarsTerran7TownA = new AIScriptAtDef("Brood Wars Terran 7 Town A");
        public static readonly AIScriptAtDef BroodWarsTerran7TownB = new AIScriptAtDef("Brood Wars Terran 7 Town B");
        public static readonly AIScriptAtDef BroodWarsTerran7TownC = new AIScriptAtDef("Brood Wars Terran 7 Town C");
        public static readonly AIScriptAtDef BroodWarsTerran7TownD = new AIScriptAtDef("Brood Wars Terran 7 Town D");
        public static readonly AIScriptAtDef BroodWarsTerran7TownE = new AIScriptAtDef("Brood Wars Terran 7 Town E");
        public static readonly AIScriptAtDef BroodWarsTerran7TownF = new AIScriptAtDef("Brood Wars Terran 7 Town F");
        public static readonly AIScriptAtDef BroodWarsTerran8TownA = new AIScriptAtDef("Brood Wars Terran 8 Town A");
        public static readonly AIScriptAtDef BroodWarsTerran8TownB = new AIScriptAtDef("Brood Wars Terran 8 Town B");
        public static readonly AIScriptAtDef BroodWarsTerran8TownC = new AIScriptAtDef("Brood Wars Terran 8 Town C");
        public static readonly AIScriptAtDef BroodWarsTerran8TownD = new AIScriptAtDef("Brood Wars Terran 8 Town D");
        public static readonly AIScriptAtDef BroodWarsTerran8TownE = new AIScriptAtDef("Brood Wars Terran 8 Town E");
        public static readonly AIScriptAtDef BroodWarsTerran8TownF = new AIScriptAtDef("Brood Wars Terran 8 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg1TownA = new AIScriptAtDef("Brood Wars Zerg 1 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg1TownB = new AIScriptAtDef("Brood Wars Zerg 1 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg1TownC = new AIScriptAtDef("Brood Wars Zerg 1 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg1TownD = new AIScriptAtDef("Brood Wars Zerg 1 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg1TownE = new AIScriptAtDef("Brood Wars Zerg 1 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg1TownF = new AIScriptAtDef("Brood Wars Zerg 1 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg2TownA = new AIScriptAtDef("Brood Wars Zerg 2 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg2TownB = new AIScriptAtDef("Brood Wars Zerg 2 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg2TownC = new AIScriptAtDef("Brood Wars Zerg 2 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg2TownD = new AIScriptAtDef("Brood Wars Zerg 2 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg2TownE = new AIScriptAtDef("Brood Wars Zerg 2 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg2TownF = new AIScriptAtDef("Brood Wars Zerg 2 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg3TownA = new AIScriptAtDef("Brood Wars Zerg 3 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg3TownB = new AIScriptAtDef("Brood Wars Zerg 3 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg3TownC = new AIScriptAtDef("Brood Wars Zerg 3 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg3TownD = new AIScriptAtDef("Brood Wars Zerg 3 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg3TownE = new AIScriptAtDef("Brood Wars Zerg 3 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg3TownF = new AIScriptAtDef("Brood Wars Zerg 3 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg4TownA = new AIScriptAtDef("Brood Wars Zerg 4 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg4TownB = new AIScriptAtDef("Brood Wars Zerg 4 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg4TownC = new AIScriptAtDef("Brood Wars Zerg 4 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg4TownD = new AIScriptAtDef("Brood Wars Zerg 4 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg4TownE = new AIScriptAtDef("Brood Wars Zerg 4 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg4TownF = new AIScriptAtDef("Brood Wars Zerg 4 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg5TownA = new AIScriptAtDef("Brood Wars Zerg 5 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg5TownB = new AIScriptAtDef("Brood Wars Zerg 5 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg5TownC = new AIScriptAtDef("Brood Wars Zerg 5 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg5TownD = new AIScriptAtDef("Brood Wars Zerg 5 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg5TownE = new AIScriptAtDef("Brood Wars Zerg 5 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg5TownF = new AIScriptAtDef("Brood Wars Zerg 5 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg6TownA = new AIScriptAtDef("Brood Wars Zerg 6 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg6TownB = new AIScriptAtDef("Brood Wars Zerg 6 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg6TownC = new AIScriptAtDef("Brood Wars Zerg 6 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg6TownD = new AIScriptAtDef("Brood Wars Zerg 6 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg6TownE = new AIScriptAtDef("Brood Wars Zerg 6 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg6TownF = new AIScriptAtDef("Brood Wars Zerg 6 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg7TownA = new AIScriptAtDef("Brood Wars Zerg 7 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg7TownB = new AIScriptAtDef("Brood Wars Zerg 7 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg7TownC = new AIScriptAtDef("Brood Wars Zerg 7 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg7TownD = new AIScriptAtDef("Brood Wars Zerg 7 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg7TownE = new AIScriptAtDef("Brood Wars Zerg 7 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg7TownF = new AIScriptAtDef("Brood Wars Zerg 7 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg8TownA = new AIScriptAtDef("Brood Wars Zerg 8 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg8TownB = new AIScriptAtDef("Brood Wars Zerg 8 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg8TownC = new AIScriptAtDef("Brood Wars Zerg 8 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg8TownD = new AIScriptAtDef("Brood Wars Zerg 8 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg8TownE = new AIScriptAtDef("Brood Wars Zerg 8 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg8TownF = new AIScriptAtDef("Brood Wars Zerg 8 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg9TownA = new AIScriptAtDef("Brood Wars Zerg 9 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg9TownB = new AIScriptAtDef("Brood Wars Zerg 9 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg9TownC = new AIScriptAtDef("Brood Wars Zerg 9 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg9TownD = new AIScriptAtDef("Brood Wars Zerg 9 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg9TownE = new AIScriptAtDef("Brood Wars Zerg 9 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg9TownF = new AIScriptAtDef("Brood Wars Zerg 9 Town F");
        public static readonly AIScriptAtDef BroodWarsZerg10TownA = new AIScriptAtDef("Brood Wars Zerg 10 Town A");
        public static readonly AIScriptAtDef BroodWarsZerg10TownB = new AIScriptAtDef("Brood Wars Zerg 10 Town B");
        public static readonly AIScriptAtDef BroodWarsZerg10TownC = new AIScriptAtDef("Brood Wars Zerg 10 Town C");
        public static readonly AIScriptAtDef BroodWarsZerg10TownD = new AIScriptAtDef("Brood Wars Zerg 10 Town D");
        public static readonly AIScriptAtDef BroodWarsZerg10TownE = new AIScriptAtDef("Brood Wars Zerg 10 Town E");
        public static readonly AIScriptAtDef BroodWarsZerg10TownF = new AIScriptAtDef("Brood Wars Zerg 10 Town F");
        public static readonly AIScriptAtDef TerranExpansionCustomLevel = new AIScriptAtDef("Terran Expansion Custom Level");
        public static readonly AIScriptAtDef ZergExpansionCustomLevel = new AIScriptAtDef("Zerg Expansion Custom Level");
        public static readonly AIScriptAtDef ProtossExpansionCustomLevel = new AIScriptAtDef("Protoss Expansion Custom Level");
        public static readonly AIScriptAtDef ExpansionTerranCampaignEasy = new AIScriptAtDef("Expansion Terran Campaign Easy");
        public static readonly AIScriptAtDef ExpansionTerranCampaignMedium = new AIScriptAtDef("Expansion Terran Campaign Medium");
        public static readonly AIScriptAtDef ExpansionTerranCampaignDifficult = new AIScriptAtDef("Expansion Terran Campaign Difficult");
        public static readonly AIScriptAtDef ExpansionTerranCampaignInsane = new AIScriptAtDef("Expansion Terran Campaign Insane");
        public static readonly AIScriptAtDef ExpansionTerranCampaignAreaTown = new AIScriptAtDef("Expansion Terran Campaign Area Town");
        public static readonly AIScriptAtDef ExpansionZergCampaignEasy = new AIScriptAtDef("Expansion Zerg Campaign Easy");
        public static readonly AIScriptAtDef ExpansionZergCampaignMedium = new AIScriptAtDef("Expansion Zerg Campaign Medium");
        public static readonly AIScriptAtDef ExpansionZergCampaignDifficult = new AIScriptAtDef("Expansion Zerg Campaign Difficult");
        public static readonly AIScriptAtDef ExpansionZergCampaignInsane = new AIScriptAtDef("Expansion Zerg Campaign Insane");
        public static readonly AIScriptAtDef ExpansionZergCampaignAreaTown = new AIScriptAtDef("Expansion Zerg Campaign Area Town");
        public static readonly AIScriptAtDef ExpansionProtossCampaignEasy = new AIScriptAtDef("Expansion Protoss Campaign Easy");
        public static readonly AIScriptAtDef ExpansionProtossCampaignMedium = new AIScriptAtDef("Expansion Protoss Campaign Medium");
        public static readonly AIScriptAtDef ExpansionProtossCampaignDifficult = new AIScriptAtDef("Expansion Protoss Campaign Difficult");
        public static readonly AIScriptAtDef ExpansionProtossCampaignInsane = new AIScriptAtDef("Expansion Protoss Campaign Insane");
        public static readonly AIScriptAtDef ExpansionProtossCampaignAreaTown = new AIScriptAtDef("Expansion Protoss Campaign Area Town");



        public static readonly AIScriptAtDef[] AllScripts = new AIScriptAtDef[] {
            Terran3ZergTown,
            Terran5TerranMainTown,
            Terran5TerranHarvestTown,
            Terran6AirAttackZerg,
            Terran6GroundAttackZerg,
            Terran6ZergSupportTown,
            Terran7BottomZergTown,
            Terran7RightZergTown,
            Terran7MiddleZergTown,
            Terran8ConfederateTown,
            Terran9LightAttack,
            Terran9HeavyAttack,
            Terran10ConfederateTowns,
            Terran11ZergTown,
            Terran11LowerProtossTown,
            Terran11UpperProtossTown,
            Terran12NukeTown,
            Terran12PhoenixTown,
            Terran12TankTown,
            Terran1ElectronicDistribution,
            Terran2ElectronicDistribution,
            Terran3ElectronicDistribution,
            Terran1Shareware,
            Terran2Shareware,
            Terran3Shareware,
            Terran4Shareware,
            Terran5Shareware,
            Zerg1TerranTown,
            Zerg2ProtossTown,
            Zerg3TerranTown,
            Zerg4RightTerranTown,
            Zerg4LowerTerranTown,
            Zerg6ProtossTown,
            Zerg7AirTown,
            Zerg7GroundTown,
            Zerg7SupportTown,
            Zerg8ScoutTown,
            Zerg8TemplarTown,
            Zerg9TealProtoss,
            Zerg9LeftYellowProtoss,
            Zerg9RightYellowProtoss,
            Zerg9LeftOrangeProtoss,
            Zerg9RightOrangeProtoss,
            Zerg10LeftTealAttack,
            Zerg10RightTealSupport,
            Zerg10LeftYellowSupport,
            Zerg10RightYellowAttack,
            Zerg10RedProtoss,
            Protoss1ZergTown,
            Protoss2ZergTown,
            Protoss3AirZergTown,
            Protoss3GroundZergTown,
            Protoss4ZergTown,
            Protoss5ZergTownIsland,
            Protoss5ZergTownBase,
            Protoss7LeftProtossTown,
            Protoss7RightProtossTown,
            Protoss7ShrineProtoss,
            Protoss8LeftProtossTown,
            Protoss8RightProtossTown,
            Protoss8ProtossDefenders,
            Protoss9GroundZerg,
            Protoss9AirZerg,
            Protoss9SpellZerg,
            Protoss10MiniTowns,
            Protoss10MiniTownMaster,
            Protoss10OvermindDefenders,
            TerranCustomLevel,
            ZergCustomLevel,
            ProtossCustomLevel,
            TerranCampaignEasy,
            TerranCampaignMedium,
            TerranCampaignDifficult,
            TerranCampaignInsane,
            TerranCampaignAreaTown,
            ZergCampaignEasy,
            ZergCampaignMedium,
            ZergCampaignDifficult,
            ZergCampaignInsane,
            ZergCampaignAreaTown,
            ProtossCampaignEasy,
            ProtossCampaignMedium,
            ProtossCampaignDifficult,
            ProtossCampaignInsane,
            ProtossCampaignAreaTown,
            MoveDarkTemplarstoRegion,
            ClearPreviousCombatData,
            SetPlayertoEnemy,
            SetPlayertoAlly,
            ValueThisAreaHigher,
            EnterClosestBunker,
            SetGenericCommandTarget,
            MakeTheseUnitsPatrol,
            EnterTransport,
            ExitTransport,
            AINukeHere,
            AIHarassHere,
            SetUnitOrderToJunkYardDog,
            BroodWarsProtoss1TownA,
            BroodWarsProtoss1TownB,
            BroodWarsProtoss1TownC,
            BroodWarsProtoss1TownD,
            BroodWarsProtoss1TownE,
            BroodWarsProtoss1TownF,
            BroodWarsProtoss2TownA,
            BroodWarsProtoss2TownB,
            BroodWarsProtoss2TownC,
            BroodWarsProtoss2TownD,
            BroodWarsProtoss2TownE,
            BroodWarsProtoss2TownF,
            BroodWarsProtoss3TownA,
            BroodWarsProtoss3TownB,
            BroodWarsProtoss3TownC,
            BroodWarsProtoss3TownD,
            BroodWarsProtoss3TownE,
            BroodWarsProtoss3TownF,
            BroodWarsProtoss4TownA,
            BroodWarsProtoss4TownB,
            BroodWarsProtoss4TownC,
            BroodWarsProtoss4TownD,
            BroodWarsProtoss4TownE,
            BroodWarsProtoss4TownF,
            BroodWarsProtoss5TownA,
            BroodWarsProtoss5TownB,
            BroodWarsProtoss5TownC,
            BroodWarsProtoss5TownD,
            BroodWarsProtoss5TownE,
            BroodWarsProtoss5TownF,
            BroodWarsProtoss6TownA,
            BroodWarsProtoss6TownB,
            BroodWarsProtoss6TownC,
            BroodWarsProtoss6TownD,
            BroodWarsProtoss6TownE,
            BroodWarsProtoss6TownF,
            BroodWarsProtoss7TownA,
            BroodWarsProtoss7TownB,
            BroodWarsProtoss7TownC,
            BroodWarsProtoss7TownD,
            BroodWarsProtoss7TownE,
            BroodWarsProtoss7TownF,
            BroodWarsProtoss8TownA,
            BroodWarsProtoss8TownB,
            BroodWarsProtoss8TownC,
            BroodWarsProtoss8TownD,
            BroodWarsProtoss8TownE,
            BroodWarsProtoss8TownF,
            BroodWarsTerran1TownA,
            BroodWarsTerran1TownB,
            BroodWarsTerran1TownC,
            BroodWarsTerran1TownD,
            BroodWarsTerran1TownE,
            BroodWarsTerran1TownF,
            BroodWarsTerran2TownA,
            BroodWarsTerran2TownB,
            BroodWarsTerran2TownC,
            BroodWarsTerran2TownD,
            BroodWarsTerran2TownE,
            BroodWarsTerran2TownF,
            BroodWarsTerran3TownA,
            BroodWarsTerran3TownB,
            BroodWarsTerran3TownC,
            BroodWarsTerran3TownD,
            BroodWarsTerran3TownE,
            BroodWarsTerran3TownF,
            BroodWarsTerran4TownA,
            BroodWarsTerran4TownB,
            BroodWarsTerran4TownC,
            BroodWarsTerran4TownD,
            BroodWarsTerran4TownE,
            BroodWarsTerran4TownF,
            BroodWarsTerran5TownA,
            BroodWarsTerran5TownB,
            BroodWarsTerran5TownC,
            BroodWarsTerran5TownD,
            BroodWarsTerran5TownE,
            BroodWarsTerran5TownF,
            BroodWarsTerran6TownA,
            BroodWarsTerran6TownB,
            BroodWarsTerran6TownC,
            BroodWarsTerran6TownD,
            BroodWarsTerran6TownE,
            BroodWarsTerran6TownF,
            BroodWarsTerran7TownA,
            BroodWarsTerran7TownB,
            BroodWarsTerran7TownC,
            BroodWarsTerran7TownD,
            BroodWarsTerran7TownE,
            BroodWarsTerran7TownF,
            BroodWarsTerran8TownA,
            BroodWarsTerran8TownB,
            BroodWarsTerran8TownC,
            BroodWarsTerran8TownD,
            BroodWarsTerran8TownE,
            BroodWarsTerran8TownF,
            BroodWarsZerg1TownA,
            BroodWarsZerg1TownB,
            BroodWarsZerg1TownC,
            BroodWarsZerg1TownD,
            BroodWarsZerg1TownE,
            BroodWarsZerg1TownF,
            BroodWarsZerg2TownA,
            BroodWarsZerg2TownB,
            BroodWarsZerg2TownC,
            BroodWarsZerg2TownD,
            BroodWarsZerg2TownE,
            BroodWarsZerg2TownF,
            BroodWarsZerg3TownA,
            BroodWarsZerg3TownB,
            BroodWarsZerg3TownC,
            BroodWarsZerg3TownD,
            BroodWarsZerg3TownE,
            BroodWarsZerg3TownF,
            BroodWarsZerg4TownA,
            BroodWarsZerg4TownB,
            BroodWarsZerg4TownC,
            BroodWarsZerg4TownD,
            BroodWarsZerg4TownE,
            BroodWarsZerg4TownF,
            BroodWarsZerg5TownA,
            BroodWarsZerg5TownB,
            BroodWarsZerg5TownC,
            BroodWarsZerg5TownD,
            BroodWarsZerg5TownE,
            BroodWarsZerg5TownF,
            BroodWarsZerg6TownA,
            BroodWarsZerg6TownB,
            BroodWarsZerg6TownC,
            BroodWarsZerg6TownD,
            BroodWarsZerg6TownE,
            BroodWarsZerg6TownF,
            BroodWarsZerg7TownA,
            BroodWarsZerg7TownB,
            BroodWarsZerg7TownC,
            BroodWarsZerg7TownD,
            BroodWarsZerg7TownE,
            BroodWarsZerg7TownF,
            BroodWarsZerg8TownA,
            BroodWarsZerg8TownB,
            BroodWarsZerg8TownC,
            BroodWarsZerg8TownD,
            BroodWarsZerg8TownE,
            BroodWarsZerg8TownF,
            BroodWarsZerg9TownA,
            BroodWarsZerg9TownB,
            BroodWarsZerg9TownC,
            BroodWarsZerg9TownD,
            BroodWarsZerg9TownE,
            BroodWarsZerg9TownF,
            BroodWarsZerg10TownA,
            BroodWarsZerg10TownB,
            BroodWarsZerg10TownC,
            BroodWarsZerg10TownD,
            BroodWarsZerg10TownE,
            BroodWarsZerg10TownF,
            TerranExpansionCustomLevel,
            ZergExpansionCustomLevel,
            ProtossExpansionCustomLevel,
            ExpansionTerranCampaignEasy,
            ExpansionTerranCampaignMedium,
            ExpansionTerranCampaignDifficult,
            ExpansionTerranCampaignInsane,
            ExpansionTerranCampaignAreaTown,
            ExpansionZergCampaignEasy,
            ExpansionZergCampaignMedium,
            ExpansionZergCampaignDifficult,
            ExpansionZergCampaignInsane,
            ExpansionZergCampaignAreaTown,
            ExpansionProtossCampaignEasy,
            ExpansionProtossCampaignMedium,
            ExpansionProtossCampaignDifficult,
            ExpansionProtossCampaignInsane,
            ExpansionProtossCampaignAreaTown,
        };

    }

    public class AllianceDef : SaveableItem, Gettable<AllianceDef> {

        private string _value;

        private AllianceDef(string value) {
            _value = value;
        }

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return ToString();
        }

        public static readonly AllianceDef Ally = new AllianceDef("Ally");
        public static readonly AllianceDef Enemy = new AllianceDef("Enemy");
        public static readonly AllianceDef AlliedVictory = new AllianceDef("Allied Victory");

        public static AllianceDef getDefaultValue() {
            return AllianceDef.Ally;
        }

        public int getMaxValue() {
            return AllAlliances.Length - 1;
        }

        public int getIndex() {
            if(this == Ally) {
                return 0;
            } else if(this == Enemy) {
                return 1;
            } else if(this==AlliedVictory) {
                return 2;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<AllianceDef> getter, Action<AllianceDef> setter, Func<AllianceDef> getDefault) {
            return new TriggerDefinitionGeneralDef<AllianceDef>(getter, setter, getDefault, AllAlliances);
        }

        public static AllianceDef[] AllAlliances = new AllianceDef[] { Ally, Enemy, AlliedVictory };

    }

    public class BehaviorDef : Gettable<BehaviorDef> {

        private int _index;
        private string _name;

        public int BehaviorIndex { get { return _index; } }

        public string BehaviorName { get { return _name; } }

        public static BehaviorDef getByIndex(int index) {
            if (index < AllBehaviors.Length) {
                return AllBehaviors[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return BehaviorName;
        }

        public int getMaxValue() {
            return AllBehaviors.Length - 1;
        }

        public int getIndex() {
            return BehaviorIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<BehaviorDef> getter, Action<BehaviorDef> setter, Func<BehaviorDef> getDefault) {
            return new TriggerDefinitionGeneralDef<BehaviorDef>(getter, setter, getDefault, AllBehaviors);
        }

        private BehaviorDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid behavior";
            }
        }

        public static BehaviorDef[] AllBehaviors {
            get {
                if (_allBehaviors == null) {
                    _allBehaviors = new BehaviorDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allBehaviors[i] = new BehaviorDef(i);
                    }
                }
                return _allBehaviors;
            }
        }

        private static BehaviorDef[] _allBehaviors;

        private static readonly string[] _Defs = {
            "Fly & Don't Follow Target",
            "Fly & Follow Target",
            "Appear on Target Unit",
            "Persist on Target Site",
            "Appear on Target Site",
            "Appear on Attacker",
            "Attack & Self-Destruct",
            "Bounce",
            "Attack Target 3x3 Area",
            "Go to Max. Range",
        };
    }

    public class BoolDef : Gettable<BoolDef> {

        private int _value;

        public int getIndex() {
            return _value;
        }

        public int getMaxValue() {
            return 1;
        }

        public static BoolDef getByIndex(int index) {
            if(index == 0) {
                return BoolFalse;
            } else if(index == 1) {
                return BoolTrue;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<BoolDef> getter, Action<BoolDef> setter, Func<BoolDef> getDefault) {
            return new TriggerDefinitionResetableCheckbox(() => getter() == BoolTrue, (bool b) => { setter(b ? BoolTrue : BoolFalse); }, ()=>getDefault() == BoolTrue);
        }

        private BoolDef(int val) {
            _value = val;
        }

        private static readonly BoolDef BoolFalse = new BoolDef(0);
        private static readonly BoolDef BoolTrue = new BoolDef(1);

    }

    public class BWTechDef {

        public static BWTechDef[] AllTechs {
            get {
                if (_allTechs == null) {
                    _allTechs = new BWTechDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allTechs[i] = new BWTechDef();
                    }
                }
                return _allTechs;
            }
        }

        private static BWTechDef[] _allTechs;

        private static readonly string[] _Defs = {
            "Restoration",
            "Disruption Web",
            "Unused ",
            "Mind Control",
            "Dark Archon Meld",
            "Feedback",
            "Optical Flare",
            "Maelstorm",
            "Lurker Aspect",
            "Unused ",
            "Healing",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
        };

    }

    public class BWUpgradeDef {

        public static BWUpgradeDef[] AllUpgrades {
            get {
                if (_allUpgrades == null) {
                    _allUpgrades = new BWUpgradeDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allUpgrades[i] = new BWUpgradeDef();
                    }
                }
                return _allUpgrades;
            }
        }

        private static BWUpgradeDef[] _allUpgrades;

        private static readonly string[] _Defs = {
            "Unknown Upgrade46",
            "Argus Jewel (Corsair +50)",
            "Unknown Upgrade48",
            "Argus Talisman (DA +50)",
            "Unknown upgrade50",
            "Caduceus Reactor (Medic +50)",
            "Chtinous Plating",
            "Anabolic Synthesis",
            "Charon Booster",
            "Unknown Upgrade55",
            "Unknown Upgrade56",
            "Unknown Upgrade57",
            "Unknown Upgrade58",
            "Unknown Upgrade59",
            "Nothing"
        };

    }

    public class EnableState : SaveableItem, Gettable<EnableState> {

        private string _value;
        private string _saveValue;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return _saveValue;
        }

        private EnableState(string value, string saveValue) {
            _value = value;
            _saveValue = saveValue;
        }

        public static readonly EnableState Enable = new EnableState("Enable", "Enabled");
        public static readonly EnableState Disable = new EnableState("Disable", "Disabled");
        public static readonly EnableState Toggle = new EnableState("Toggle", "Toggle");

        public static EnableState getDefaultValue() {
            return EnableState.Enable;
        }

        public int getMaxValue() {
            return AllStates.Length - 1;
        }

        public int getIndex() {
            if(this == Enable) {
                return 0;
            } else if(this == Disable) {
                return 1;
            } else if(this == Toggle) {
                return 2;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<EnableState> getter, Action<EnableState> setter, Func<EnableState> getDefault) {
            return new TriggerDefinitionGeneralDef<EnableState>(getter, setter, getDefault, AllStates);
        }

        public static EnableState[] AllStates = new EnableState[] { Enable, Disable, Toggle };
    }

    public class ElevationLevelDef : Gettable<ElevationLevelDef> {

        private int _index;
        private string _name;

        public int ElevationIndex { get { return _index; } }

        public string ElevationName { get { return _name; } }

        public static ElevationLevelDef getByIndex(int index) {
            if (index < AllElevations.Length) {
                return AllElevations[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return ElevationName;
        }

        public int getMaxValue() {
            return AllElevations.Length - 1;
        }

        public int getIndex() {
            return ElevationIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<ElevationLevelDef> getter, Action<ElevationLevelDef> setter, Func<ElevationLevelDef> getDefault) {
            return new TriggerDefinitionGeneralDef<ElevationLevelDef>(getter, setter, getDefault, AllElevations);
        }

        protected ElevationLevelDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid elevation";
            }
        }

        public static ElevationLevelDef[] AllElevations {
            get {
                if (_allElevations == null) {
                    _allElevations = new ElevationLevelDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allElevations[i] = new ElevationLevelDef(i);
                    }
                }
                return _allElevations;
            }
        }

        private static ElevationLevelDef[] _allElevations;

        protected static readonly string[] _Defs = {
            "Below Ground (0)",
            "Below Ground (1)",
            "Ground Level (2)",
            "Ground Level (3)",
            "Ground Level (4)",
            "Ground Level (5)",
            "Ground Level (6)",
            "Ground Level (7)",
            "Ground Level (8)",
            "Ground Level (9)",
            "Ground Level (10)",
            "Ground Level (11)",
            "Air (12)",
            "Air (13)",
            "Air (14)",
            "Air (15)",
            "Air (16)",
            "Air (17)",
            "Air (18)",
            "Air (19)",

        };
    }

    public class FlingyDef : Gettable<FlingyDef> {

        private int _index;
        private string _name;

        public int FlingyIndex { get { return _index; } }

        public string FlingyName { get { return _name; } }

        public static FlingyDef getByIndex(int index) {
            if (index < AllFlingys.Length) {
                return AllFlingys[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return FlingyName;
        }

        public int getMaxValue() {
            return AllFlingys.Length - 1;
        }

        public int getIndex() {
            return FlingyIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<FlingyDef> getter, Action<FlingyDef> setter, Func<FlingyDef> getDefault) {
            return new TriggerDefinitionGeneralDef<FlingyDef>(getter, setter, getDefault, AllFlingys);
        }

        protected FlingyDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid flingy";
            }
        }

        public static FlingyDef[] AllFlingys {
            get {
                if (_allFlingys == null) {
                    _allFlingys = new FlingyDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allFlingys[i] = new FlingyDef(i);
                    }
                }
                return _allFlingys;
            }
        }

        private static FlingyDef[] _allFlingys;

        protected static readonly string[] _Defs = {
            "Scourge",
            "Broodling",
            "Infested Terran",
            "Guardian Cocoon",
            "Defiler",
            "Drone",
            "Egg",
            "Guardian",
            "Hydralisk",
            "Infested Kerrigan",
            "Larva",
            "Mutalisk",
            "Overlord",
            "Queen",
            "Ultralisk",
            "Zergling",
            "Cerebrate",
            "Infested Command Center",
            "Spawning Pool",
            "Mature Crysalis",
            "Evolution Chamber",
            "Creep Colony",
            "Hatchery",
            "Hive",
            "Lair",
            "Sunken Colony",
            "Greater Spine",
            "Defiler Mound",
            "Queen's Nest",
            "Nydus Canal",
            "Overmind With Shell",
            "Overmind Without shell",
            "Ultralisk Cavern",
            "Extractor",
            "Hydralisk Den",
            "Spire",
            "Spore Colony",
            "Arbiter",
            "Archon Energy",
            "Carrier",
            "Dragoon",
            "Interceptor",
            "Probe",
            "Scout",
            "Shuttle",
            "High Templar",
            "High Templar (Hero)",
            "Reaver",
            "Scarab",
            "Zealot",
            "Observer",
            "Templar Archives",
            "Assimilator",
            "Observatory",
            "Citadel of Adun",
            "Forge",
            "Gateway",
            "Cybernetics Core",
            "KhaydarinCrystal Formation",
            "Nexus",
            "Photon Cannon",
            "Arbiter Tribunal",
            "Pylon",
            "Robotics Facility",
            "Shield Battery",
            "Stargate",
            "Statis Cell/Prison",
            "Robotics Support Bay",
            "Protoss Temple",
            "Fleet Beacon",
            "Battlecruiser",
            "Civilian",
            "Dropship",
            "Firebat",
            "Ghost",
            "Goliath Base",
            "Goliath Turret",
            "Sarah Kerrigan",
            "Marine",
            "Scanner Sweep",
            "Wraith",
            "SCV",
            "Siege Tank (Tank) Base",
            "Siege Tank (Tank) Turret",
            "Siege tank (Siege) Base",
            "Siege Tank (Siege) Turret",
            "Science Vessel (Base)",
            "Science Vessel (Turret)",
            "Vulture",
            "Spider Mine",
            "Academy",
            "Barracks",
            "Armory",
            "Comsat Station",
            "Command Center",
            "Supply Depot",
            "Control Tower",
            "Factory",
            "Covert Ops",
            "Ion Cannon",
            "Machine Shop",
            "Missile Turret (Base)",
            "Crashed Battlecruiser",
            "Physics Lab",
            "Bunker",
            "Rafinery",
            "Immobile Baracks",
            "Science Facility",
            "Nuke Silo",
            "Nuclear Missile",
            "Starport",
            "Engineering Bay",
            "Terran Construction (Large)",
            "Terran Construction (Small)",
            "Ragnasaur (Ashworld)",
            "Rhynadon (Badlands)",
            "Bengalaas (Jungle)",
            "Vespene Geyser",
            "Mineral Field Type1",
            "Mineral Field Type2",
            "Mineral Field Type3",
            "Independent Starport (Unused)",
            "Zerg Beacon",
            "Terran Beacon",
            "Protoss Beacon",
            "Dark Swarm",
            "Flag",
            "Young Chrysalis",
            "Psi Emitter",
            "Data Disc",
            "Khaydarin Crystal",
            "Mineral Chunk Type1",
            "Mineral Chunk Type2",
            "Protoss Gas Orb Type1",
            "Protoss Gas Obr Type2",
            "Zerg Gas Sac Type1",
            "Zerg Gas Sac Type2",
            "Terran Gas Tank Type1",
            "Terran Gas Tank Type2",
            "Map Revealer",
            "Start Location",
            "Fusion Cutter Hit",
            "Gaus Rifle Hit",
            "C-10 Canister Rifle Hit",
            "Gemini Missiles",
            "Fragmentation Grenade",
            "Lockdown/LongBolt/Hellfire Missile",
            "Unused Lockdown",
            "ATS/ATA Laser Battery",
            "Burst Lasers",
            "Arclite Shock Cannon Hit",
            "EMP Missile",
            "Dual Photon Blasters Hit",
            "Particle Beam Hit",
            "Anti-Matter Missile",
            "Pulse Cannon",
            "Psionic Shockwave Hit",
            "Psionic Storm",
            "Yamato Gun",
            "Phase Disruptor",
            "STA/STS Cannon Overlay",
            "Sunken Colony Tentacle",
            "Venom (Unused Zerg Weapon)",
            "Acid Spore",
            "Plasme Drip Hit (Unused)",
            "Glave Wurm",
            "Seeker Spores",
            "Queen Spell Carrier",
            "Plague Cloud",
            "Consume",
            "Ensnare",
            "Needle Spine Hit",
            "White CCircle (Invisible)",
            "Left Upper Level Door",
            "Right Upper Level Door",
            "Substructure Left Door",
            "Substructure Right Door",
            "Substructure Opening Hole",
            "Floor Gun Trap",
            "Floor Missile Trap",
            "Wall Missile Trap",
            "Wall Missile Trap2",
            "Wall Flame Trap",
            "Wall Flame Trap2",
            "Lurker Egg",
            "Devourer",
            "Lurker",
            "Dark Archon Energy",
            "Dark Templar (Unit)",
            "Medic",
            "Valkyrie",
            "Corsair",
            "Disruption Web",
            "sOvermind Cocoon",
            "Psi Distupter",
            "Warp Gate",
            "Power Generator",
            "Xel'Naga Temple",
            "Scantic (Desert)",
            "Kakaru (Twilight)",
            "Ursadon (Ice)",
            "Optical Flare Grenade",
            "Halo Rockets",
            "Subterranean Spines",
            "Corrosive Acid Shot",
            "Corrosive Acid Hit",
            "Neutron Flare",
            "Uraj",
            "Khalis",
        };
    }

    public class FlingyImageDef : FlingyDef, Gettable<FlingyImageDef>, GettableImage {

        private BitmapImageX _image;

        public BitmapImageX FlingyImage { get { return _image; } }


        public static new FlingyImageDef getByIndex(int index) {
            if (index < AllFlingys.Length) {
                return AllFlingys[index];
            }
            throw new NotImplementedException();
        }

        private FlingyImageDef(int index) : base(index) {

        }

        public static new FlingyImageDef[] AllFlingys {
            get {
                return Application.Current.Dispatcher.Invoke(() => {
                    if (_allFlingys == null) {
                        _allFlingys = new FlingyImageDef[_Defs.Length];
                        _allFlingyImages = new BitmapImageX[_Defs.Length];
                        for (int i = 0; i < _Defs.Length; i++) {
                            _allFlingys[i] = new FlingyImageDef(i);
                            _allFlingyImages[i] = new BitmapImageX("flingy/flingy_" + i + ".png", _allFlingys[i]);
                            _allFlingys[i]._image = _allFlingyImages[i];
                        }
                    }
                    return _allFlingys;
                });
            }
        }

        private static FlingyImageDef[] _allFlingys;


        private static BitmapImageX[] _allFlingyImages;

        public static BitmapImageX[] AllFlingyImages { get { return _allFlingyImages; } }

        public TriggerDefinitionPart getTriggerPart(Func<FlingyImageDef> getter, Action<FlingyImageDef> setter, Func<FlingyImageDef> getDefault) {
            return new TriggerDefinitionGeneralIconsList<FlingyImageDef>(getter, setter, getDefault, AllFlingyImages, 3);
        }

        public BitmapImageX getImage() {
            return FlingyImage;
        }
    }

    public class Image561Def :Gettable<Image561Def> {

        private int _index;
        private string _name;

        public int SpriteIndex { get { return _index; } }

        public string SpriteName { get { return _name; } }

        public static Image561Def getByIndex(int index) {
            if (index < AllImages.Length) {
                return AllImages[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return SpriteName;
        }

        public int getMaxValue() {
            return AllImages.Length - 1;
        }

        public int getIndex() {
            return SpriteIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<Image561Def> getter, Action<Image561Def> setter, Func<Image561Def> getDefault) {
            return new TriggerDefinitionGeneralDef<Image561Def>(getter, setter, getDefault, AllImages);
        }

        private Image561Def(int index, ImageDef source) {
            _index = index;
            _name = source.ImageName;
        }

        public static Image561Def[] AllImages {
            get {
                if (_allImages == null) {
                    int off = 561;
                    int max = 581;
                    int len = max-off;
                    _allImages = new Image561Def[len];
                    for (int i = 0; i < _allImages.Length; i++) {
                        _allImages[i] = new Image561Def(i, ImageDef.AllImages[off + i]);
                    }
                }
                return _allImages;
            }
        }

        private static Image561Def[] _allImages;

    }

    public class ImageDef : Gettable<ImageDef> {

        private int _index;
        private string _name;

        public int ImageIndex { get { return _index; } }

        public string ImageName { get { return _name; } }

        public static ImageDef getByIndex(int index) {
            if (index < AllImages.Length) {
                return AllImages[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return ImageName;
        }

        public int getMaxValue() {
            return AllImages.Length - 1;
        }

        public int getIndex() {
            return ImageIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<ImageDef> getter, Action<ImageDef> setter, Func<ImageDef> getDefault) {
            return new TriggerDefinitionGeneralDef<ImageDef>(getter, setter, getDefault, AllImages);
        }

        private ImageDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid image";
            }
        }

        public static ImageDef[] AllImages {
            get {
                if (_allImages == null) {
                    _allImages = new ImageDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allImages[i] = new ImageDef(i);
                    }
                }
                return _allImages;
            }
        }

        private static ImageDef[] _allImages;

        private static readonly string[] _Defs = {
            "Scourge",
            "Scourge Shadow",
            "Scourge Birth",
            "Scourge Death",
            "Scourge Explosion",
            "Broodling",
            "Broodling Shadow",
            "Broodling Remnants",
            "Infested Terran",
            "Infested Terran Shadow",
            "Infested Terran Explosion",
            "Guardian Cocoon",
            "Guardian Cocoon Shadow",
            "Defiler",
            "Defiler Shadow",
            "Defiler Birth",
            "Defiler Remnants",
            "Drone",
            "Drone Shadow",
            "Drone Birth",
            "Drone Remnants",
            "Egg",
            "Egg Shadow",
            "Egg Spawn",
            "Egg Remnants",
            "Guardian",
            "Guardian Shadow",
            "Guardian Birth",
            "Guardian Death",
            "Hydralisk",
            "Hydralisk Shadow",
            "Hydralisk Birth",
            "Hydralisk Remnants",
            "Infested Kerrigan",
            "Infested Kerrigan Shadow",
            "Needle Spines",
            "Larva",
            "Larva Remnants",
            "Mutalisk",
            "Mutalisk Shadow",
            "Mutalisk Birth",
            "Mutalisk Death",
            "Overlord",
            "Overlord Shadow",
            "Overlord Birth",
            "Overlord Death",
            "Queen",
            "Queen Shadow",
            "Queen Death",
            "Queen Birth",
            "Ultralisk",
            "Ultralisk Shadow",
            "Ultralisk Birth",
            "Ultralisk Remnants",
            "Zergling",
            "Zergling Shadow",
            "Zergling Birth",
            "Zergling Remnants",
            "Zerg Air Death Explosion (Large)",
            "Zerg Air Death Explosion (Small)",
            "Zerg Building Explosion",
            "Cerebrate",
            "Cerebrate Shadow",
            "Infested Command Center",
            "Spawning Pool",
            "Spawning Pool Shadow",
            "Evolution Chamber",
            "Evolution Chamber Shadow",
            "Creep Colony",
            "Creep Colony Shadow",
            "Hatchery",
            "Hatchery Shadow",
            "Hive",
            "Hive Shadow",
            "Lair",
            "Lair Shadow",
            "Sunken Colony",
            "Sunken Colony Shadow",
            "Mature Chrysalis",
            "Mature Chrysalis Shadow",
            "Greater Spire",
            "Greater Spire Shadow",
            "Defiler Mound",
            "Defiler Mound Shadow",
            "Queen's Nest",
            "Queen Nest Shadow",
            "Nydus Canal",
            "Nydus Canal Shadow",
            "Overmind With Shell",
            "Overmind Remnants",
            "Overmind Without Shell",
            "Ultralisk Cavern",
            "Ultralisk Cavern Shadow",
            "Extractor",
            "Extractor Shadow",
            "Hydralisk Den",
            "Hydralisk Den Shadow",
            "Spire",
            "Spire Shadow",
            "Spore Colony",
            "Spore Colony Shadow",
            "Infested Command Center Overlay",
            "Zerg Construction (Large)",
            "Zerg Building Morph",
            "Zerg Construction (Medium)",
            "Zerg Construction (Small)",
            "Zerg Building Construction Shadow",
            "Zerg Building Spawn (Small)",
            "Zerg Building Spawn (Medium)",
            "Zerg Building Spawn (Large)",
            "Zerg Building Rubble (Small)",
            "Zerg Building Rubble (Large)",
            "Carrier",
            "Carrier Shadow",
            "Carrier Engines",
            "Carrier Warp Flash",
            "Interceptor",
            "Interceptor Shadow",
            "Shuttle",
            "Shuttle Shadow",
            "Shuttle Engines",
            "Shuttle Warp Flash",
            "Dragoon",
            "Dragoon Shadow",
            "Dragoon Remnants",
            "Dragoon Warp Flash",
            "High Templar",
            "High Templar Shadow",
            "High Templar Warp Flash",
            "Dark Templar (Hero)",
            "Arbiter",
            "Arbiter Shadow",
            "Arbiter Engines",
            "Arbiter Warp Flash",
            "Archon Energy",
            "Archon Being",
            "Archon Swirl",
            "Probe",
            "Probe Shadow",
            "Probe Warp Flash",
            "Scout",
            "Scout Shadow",
            "Scout Engines",
            "Scout Warp Flash",
            "Reaver",
            "Reaver Shadow",
            "Reaver Warp Flash",
            "Scarab",
            "Observer",
            "Observer Shadow",
            "Observer Warp Flash",
            "Zealot",
            "Zealot Shadow",
            "Zealot Death",
            "Zealot Warp Flash",
            "Templar Archives",
            "Templar Archives Warp Flash",
            "Templar Archives Shadow",
            "Assimilator",
            "Assimilator Warp Flash",
            "Assimilator Shadow",
            "Observatory",
            "Observatory Warp Flash",
            "Observatory Shadow",
            "Citadel of Adun",
            "Citadel of Adun Warp Flash",
            "Citadel of Adun Shadow",
            "Forge",
            "Forge Overlay",
            "Forge Warp Flash",
            "Forge Shadow",
            "Gateway",
            "Gateway Warp Flash",
            "Gateway Shadow",
            "Cybernetics Core",
            "Cybernetics Core Warp Flash",
            "Cybernetics Core Overlay",
            "Cybernetics Core Shadow",
            "Khaydarin Crystal Formation",
            "Nexus",
            "Nexus Warp Flash",
            "Nexus Overlay",
            "Nexus Shadow",
            "Photon Cannon",
            "Photon Cannon Shadow",
            "Photon Cannon Warp Flash",
            "Arbiter Tribunal",
            "Arbiter Tribunal Warp Flash",
            "Arbiter Tribunal Shadow",
            "Pylon",
            "Pylon Warp Flash",
            "Pylon Shadow",
            "Robotics Facility",
            "Robotics Facility Warp Flash",
            "Robotics Facility Shadow",
            "Shield Battery",
            "Shield Battery Overlay",
            "Shileld Battery Warp Flash",
            "Shield Battery Shadow",
            "Stargate",
            "Stargate Overlay",
            "Stargate Warp Flash",
            "Stargate Shadow",
            "Stasis Cell/Prison",
            "Robotics Support Bay",
            "Robotics Support Bay Warp Flash",
            "Robotics Support Bay Shadow",
            "Protoss Temple",
            "Fleet Beacon",
            "Fleet Beacon Warp Flash",
            "Warp Texture",
            "Warp Anchor",
            "Fleet Beacon Shadow",
            "Explosion1 (Small)",
            "Explosion1 (Medium)",
            "Explosion (Large)",
            "Protoss Building Rubble (Small)",
            "Protoss Building Rubble (Large)",
            "Battlecruiser",
            "Battlecruiser Shadow",
            "Battlecruiser Engines",
            "Civilian",
            "Civilian Shadow",
            "Dropship",
            "Dropship Shadow",
            "Dropship Engines",
            "Firebat",
            "Firebat Shadow",
            "Ghost",
            "Ghost Shadow",
            "Ghost Remnants",
            "Ghost Death",
            "Nuke Beam",
            "Nuke Target Dot",
            "Goliath Base",
            "Goliath Turret",
            "Goliath Shadow",
            "Sarah Kerrigan",
            "Sarah Kerrigan Shadow",
            "Marine",
            "Marine Shadow",
            "Marine Remnants",
            "Marine Death",
            "Wraith",
            "Wraith Shadow",
            "Wraith Engines",
            "Scanner Sweep",
            "SCV",
            "SCV Shadow",
            "SCV Glow",
            "Siege Tank (Tank) Base",
            "Siege Tank (Tank) Turret",
            "Siege Tank (Tank) Base Shadow",
            "Siege Tank (Siege) Base",
            "Siege Tank (Siege) Turret",
            "Siege Tank (Siege) Base Shadow",
            "Vulture",
            "Vulture Shadow",
            "Spider Mine",
            "Spider Mine Shadow",
            "Science Vessel (Base)",
            "Science Vessel (Turret)",
            "Science Vessel Shadow",
            "Terran Academy",
            "Academy Overlay",
            "Academy Shadow",
            "Barracks",
            "Barracks Shadow",
            "Armory",
            "Armory Overlay",
            "Armory Shadow",
            "Comsat Station",
            "Comsat Station Connector",
            "Comsat Station Overlay",
            "Comsat Station Shadow",
            "Command Center",
            "Command Center Overlay",
            "Command Center Shadow",
            "Supply Depot",
            "Supply Depot Overlay",
            "Supply Depot Shadow",
            "Control Tower",
            "Control Tower Connector",
            "Control Tower Overlay",
            "Control Tower Shadow",
            "Factory",
            "Factory Overlay",
            "Factory Shadow",
            "Covert Ops",
            "Covert Ops Connector",
            "Covert Ops Overlay",
            "Covert Ops Shadow",
            "Ion Cannon",
            "Machine Shop",
            "Machine Shop Connector",
            "Machine Shop Shadow",
            "Missile Turret (Base)",
            "Missile Turret (Turret)",
            "Missile Turret (Base) Shadow",
            "Crashed Batlecruiser",
            "Crashed Battlecruiser Shadow",
            "Physics Lab",
            "Physics Lab Connector",
            "Physics Lab Shadow",
            "Bunker",
            "Bunker Shadow",
            "Bunker Overlay",
            "Refinery",
            "Refinery Shadow",
            "Science Facility",
            "Science Facility Overlay",
            "Science Facility Shadow",
            "Nuclear Silo",
            "Nuclear Silo Connector",
            "Nuclear Silo Overlay",
            "Nuclear Silo Shadow",
            "Nuclear Missile",
            "Nuclear Missile Shadow",
            "Nuke Hit",
            "Starport",
            "Starport Overlay",
            "Starport Shadow",
            "Engineering Bay",
            "Engineering Bay Overlay",
            "Engineering Bay Shadow",
            "Terran Construction (Large)",
            "Terran Construction (Large) Shadow",
            "Terran Construction (Medium)",
            "Terran Construction (Medium) Shadow",
            "Addon Construction",
            "Terran Construction (Small)",
            "Terran Construction (Small) Shadow",
            "Explosion2 (Small)",
            "Explosion2 (Medium)",
            "Building Explosion (Large)",
            "Terran Building Rubble (Small)",
            "Terran Building Rubble (Large)",
            "Dark Swarm",
            "Ragnasaur (Ash)",
            "Ragnasaur (Ash) Shadow",
            "Rhynadon (Badlands)",
            "Rhynadon (Badlands) Shadow",
            "Bengalaas (Jungle)",
            "Bengalaas (Jungle) Shadow",
            "Vespene Geyser",
            "Vespene Geyser2",
            "Vespene Geyser Shadow",
            "Mineral Field Type1",
            "Mineral Field Type1 Shadow",
            "Mineral Field Type2",
            "Mineral Field Type2 Shadow",
            "Mineral Field Type3",
            "Mineral Field Type3 Shadow",
            "Independent Starport (Unused)",
            "Zerg Beacon",
            "Zerg Beacon Overlay",
            "Terran Beacon",
            "Terran Beacon Overlay",
            "Protoss Beacon",
            "Protoss Beacon Overlay",
            "Magna Pulse (Unused)",
            "Lockdown Field (Small)",
            "Lockdown Field (Medium)",
            "Lockdown Field (Large)",
            "Stasis Field Hit",
            "Stasis Field (Small)",
            "Stasis Field (Medium)",
            "Stasis Field (Large)",
            "Recharge Shields (Small)",
            "Recharge Shields (Medium)",
            "Recharge Shields (Large)",
            "Defensive Matrix Front (Small)",
            "Defensive Matrix Front (Medium)",
            "Defensive Matrix Front (Large)",
            "Defensive Matrix Back (Small)",
            "Defensive Matrix Back (Medium)",
            "Defensive Matrix Back (Large)",
            "Defensive Matrix Hit (Small)",
            "Defensive Matrix Hit (Medium)",
            "Defensive Matrix Hit (Large)",
            "Irradiate (Small)",
            "Irradiate (Medium)",
            "Irradiate (Large)",
            "Ensnare Cloud",
            "Ensnare Overlay (Small)",
            "Ensnare Overlay (Medium)",
            "Ensnare Overlay (Large)",
            "Plague Cloud",
            "Plague Overlay (Small)",
            "Plague Overlay (Medium)",
            "Plague Overlay (Large)",
            "Recall Field",
            "Flag",
            "Young Chrysalis",
            "Psi Emitter",
            "Data Disc",
            "Khaydarin Crystal",
            "Mineral Chunk Type1",
            "Mineral Chunk Type2",
            "Protoss Gas Orb Type1",
            "Protoss Gas Orb Type2",
            "Zerg Gas Sac Type1",
            "Zerg Gas Sac Type2",
            "Terran Gas Tank Type1",
            "Terran Gas Tank Type2",
            "Mineral Chunk Shadow",
            "Protoss Gas Orb Shadow",
            "Zerg Gas Sac Shadow",
            "Terran Gas Tank Shadow",
            "Data Disk Shadow (Ground)",
            "Data Disk Shadow (Carried)",
            "Flag Shadow (Ground)",
            "Flag Shadow (Carried)",
            "Crystal Shadow (Ground)",
            "Crystal Shadow (Carried)",
            "Young Chrysalis Shadow (Ground)",
            "Young Chrysalis Shadow (Carried)",
            "Psi Emitter Shadow (Ground)",
            "Psi Emitter Shadow (Carried)",
            "Acid Spray (Unused)",
            "Plasma Drip (Unused)",
            "FlameThrower",
            "Longbolt/Gemini Missiles Trail",
            "Burrowing Dust",
            "Shield Overlay",
            "Small Explosion (Unused)",
            "Double Explosion",
            "Phase Disruptor Hit",
            "Nuclear Missile Death",
            "Spider Mine Death",
            "Vespene Geyser Smoke1",
            "Vespene Geyser Smoke2",
            "Vespene Geyser Smoke3",
            "Vespene Geyser Smoke4",
            "Vespene Geyser Smoke5",
            "Vespene Geyser Smoke1 Overlay",
            "Vespene Geyser Smoke2 Overlay",
            "Vespene Geyser Smoke3 Overlay",
            "Vespene Geyser Smoke4 Overlay",
            "Vespene Geyser Smoke5 Overlay",
            "Fragmentation Grenade Hit",
            "Grenade Shot Smoke",
            "Anti-Matter Missile Hit",
            "Scarab/Anti-Matter Missile Overlay",
            "Scarab Hit",
            "Cursor Marker",
            "Battlecruiser Attack Overlay",
            "Burst Lasers Hit",
            "Burst Lasers Overlay (Unused)",
            "High Templar Glow",
            "Flames1 Type1 (Small)",
            "Flames1 Type2 (Small)",
            "Flames1 Type3 (Small)",
            "Flames2 Type3 (Small)",
            "Flames3 Type3 (Small)",
            "Flames4 Type3 (Small)",
            "Flames5 Type3 (Small)",
            "Flames6 Type3 (Small)",
            "Bleeding Variant1 Type1 (Small)",
            "Bleeding Variant1 Type2 (Small)",
            "Bleeding Variant1 Type3 (Small)",
            "Bleeding Variant1 Type4 (Small)",
            "Bleeding Variant2 Type1 (Small)",
            "Bleeding Variant2 Type2 (Small)",
            "Bleeding Variant2 Type3 (Small)",
            "Bleeding Variant2 Type4 (Small)",
            "Flames2 Type1 (Small)",
            "Flames2 Type2 (Small)",
            "Flames7 Type3 (Small)",
            "Flames3 Type1 (Small)",
            "Flames3 Type2 (Small)",
            "Flames8 Type3 (Small)",
            "Flames1 Type1 (Large)",
            "Flames1 Type2 (Large)",
            "Flames1 Type3 (Large)",
            "Flames2 Type3 (Large)",
            "Flames3 Type3 (Large)",
            "Flames4 Type3 (Large)",
            "Flames5 Type3 (Large)",
            "Flames6 Type3 (Large)",
            "Bleeding Variant1 Type1 (Large)",
            "Bleeding Variant1 Type2 (Large)",
            "Bleeding Variant1 Type3 (Large)",
            "Bleeding Variant1 Type4 (Large)",
            "Bleeding Variant2 Type1 (Large)",
            "Bleeding Variant2 Type3 (Large)",
            "Bleeding Variant3 Type3 (Large)",
            "Bleeding Variant2 Type4 (Large)",
            "Flames2 Type1 (Large)",
            "Flames2 Type2 (Large)",
            "Flames7 Type3 (Large)",
            "Flames3 Type1 (Large)",
            "Flames3 Type2 (Large)",
            "Flames8 Type3 (Large)",
            "Building Landing Dust Type1",
            "Building Landing Dust Type2",
            "Building Landing Dust Type3",
            "Building Landing Dust Type4",
            "Building Landing Dust Type5",
            "Building Lifting Dust Type1",
            "Building Lifting Dust Type2",
            "Building Lifting Dust Type3",
            "Building Lifting Dust Type4",
            "White Circle",
            "Needle Spine Hit",
            "Plasma Drip Hit (Unused)",
            "Sunken Colony Tentacle",
            "Venom (Unused Zerg Weapon)",
            "Venom Hit (Unused)",
            "Acid Spore",
            "Acid Spore Hit",
            "Glave Wurm",
            "Glave Wurm/Seeker Spores Hit",
            "Glave Wurm Trail",
            "Seeker Spores Overlay",
            "Seeker Spores",
            "Queen Spell Holder",
            "Consume",
            "Guardian Attack Overlay",
            "Dual Photon Blasters Hit",
            "Particle Beam Hit",
            "Anti-Matter Missile",
            "Pulse Cannon",
            "Phase Disruptor",
            "STA/STS Photon Cannon Overlay",
            "Psionic Storm",
            "Fusion Cutter Hit",
            "Gauss Rifle Hit",
            "Gemini Missiles",
            "Lockdown/LongBolt/Hellfire Missile",
            "Gemini Missiles Explosion",
            "C-10 Canister Rifle Hit",
            "Fragmentation Grenade",
            "Arclite Shock Cannon Hit",
            "ATS/ATA Laser Battery",
            "Burst Lasers",
            "Siege Tank(Tank) Turret Attack Overlay",
            "Siege Tank(Siege) Turret Attack Overlay",
            "Science Vessel Overlay (Part1)",
            "Science Vessel Overlay (Part2)",
            "Science Vessel Overlay (Part3)",
            "Yamato Gun",
            "Yamato Gun Trail",
            "Yamato Gun Overlay",
            "Yamato Gun Hit",
            "Hallucination Hit",
            "Scanner Sweep Hit",
            "Archon Birth (Unused)",
            "Psionic Shockwave Hit",
            "Archon Beam",
            "Psionic Storm Part1",
            "Psionic Storm Part2",
            "Psionic Storm Part3",
            "Psionic Storm Part4",
            "EMP Shockwave Missile",
            "EMP Shockwave Hit (Part1)",
            "EMP Shockwave Hit (Part2)",
            "Hallucination Death1",
            "Hallucination Death2",
            "Hallucination Death3",
            "Circle Marker1",
            "Selection Circle (22pixels)",
            "Selection Circle (32pixels)",
            "Selection Circle (48pixels)",
            "Selection Circle (62pixels)",
            "Selection Circle (72pixels)",
            "Selection Circle (94pixels)",
            "Selection Circle (110pixels)",
            "Selection Circle (122pixels)",
            "Selection Circle (146pixels)",
            "Selection Circle (224pixels)",
            "Selection Circle Dashed (22pixels)",
            "Selection Circle Dashed (32pixels)",
            "Selection Circle Dashed (48pixels)",
            "Selection Circle Dashed (62pixels)",
            "Selection Circle Dashed (72pixels)",
            "Selection Circle Dashed (94pixels)",
            "Selection Circle Dashed (110pixels)",
            "Selection Circle Dashed (122pixels)",
            "Selection Circle Dashed (146pixels)",
            "Selection Circle Dashed (224pixels)",
            "Circle Marker2",
            "Map Revealer",
            "Circle Marker3",
            "Psi Field1 (Right Upper)",
            "Psi Field1 (Right Lower)",
            "Psi Field2 (Right Upper)",
            "Psi Field2 (Right Lower)",
            "Start Location",
            "2/38 Ash",
            "2/38 Ash Shadow",
            "2/39 Ash",
            "2/39 Ash Shadow",
            "2/41 Ash",
            "2/41 Ash Shadow",
            "2/40 Ash",
            "2/40 Ash Shadow",
            "2/42 Ash",
            "2/42 Ash Shadow",
            "2/43 Ash",
            "2/44 Ash ",
            "2/1 Ash",
            "2/4 Ash",
            "2/5 Ash",
            "2/30 Ash",
            "2/28 Ash",
            "2/29 Ash",
            "4/1 Ash ",
            "4/2 Ash",
            "4/3 Ash",
            "4/56 Jungle",
            "4/56 Jungle Shadow",
            "4/57 Jungle",
            "4/57 Jungle Shadow",
            "4/58 Jungle",
            "4/58 Jungle Shadow",
            "4/59 Jungle",
            "4/59 Jungle Shadow",
            "9/5 Jungle",
            "9/5 Jungle Shadow",
            "9/6 Jungle",
            "9/6 Jungle Shadow",
            "9/7 Jungle",
            "9/7 Jungle Shadow",
            "4/51 Jungle",
            "4/51 Jungle Shadow",
            "4/52 Jungle",
            "4/52 Jungle Shadow",
            "4/54 Jungle",
            "4/54 Jungle Shadow",
            "4/53 Jungle",
            "4/53 Jungle Shadow",
            "9/1 Jungle",
            "9/1 Jungle Shadow",
            "9/2 Jungle",
            "9/2 Jungle Shadow",
            "9/3 Jungle",
            "9/3 Jungle Shadow",
            "9/4 Jungle",
            "9/4 Jungle Shadow",
            "4/12 Jungle",
            "4/13 Jungle",
            "4/1 Jungle",
            "4/3 Jungle",
            "4/2 Jungle",
            "4/5 Jungle",
            "4/4 Jungle",
            "4/9 Jungle",
            "4/10 Jungle",
            "5/5 Jungle",
            "5/7 Jungle",
            "5/6 Jungle",
            "5/9 Jungle",
            "5/8 Jungle",
            "4/6 Jungle",
            "4/7 Jungle",
            "4/17 Jungle",
            "13/4 Jungle",
            "11/5 Jungle",
            "11/6 Jungle",
            "11/7 Jungle",
            "11/8 Jungle",
            "11/10 Jungle",
            "11/11 Jungle",
            "11/12 Jungle",
            "7/4 Platform",
            "7/4 Platform Shadow",
            "7/5 Platform",
            "7/5 Platform Shadow",
            "7/6 Platform",
            "7/6 Platform Shadow",
            "7/1 Platform",
            "7/1 Platform Shadow",
            "7/2 Platform",
            "7/2 Platform Shadow",
            "7/3 Platform",
            "7/3 Platform Shadow",
            "7/9 Platform",
            "7/10 Platform",
            "7/8 Platform",
            "7/7 Platform",
            "7/26 Platform",
            "7/24 Platform",
            "7/28 Platform",
            "7/27 Platform",
            "7/25 Platform",
            "7/29 Platform",
            "7/30 Platform",
            "7/31 Platform",
            "12/1 Platform",
            "9/27 Platform",
            "5/54 Badlands",
            "5/54 Badlands Shadow",
            "5/55 Badlands",
            "5/55 Badlands Shadow",
            "5/56 Badlands",
            "5/56 Badlands Shadow",
            "5/57 Badlands",
            "5/57 Badlands Shadow",
            "6/16 Badlands",
            "6/17 Badlands",
            "6/20 Badlands",
            "6/21 Badlands",
            "5/10 Badlands",
            "5/50 Badlands",
            "5/50 Badlands Shadow",
            "5/52 Badlands",
            "5/52 Badlands Shadow",
            "5/53 Badlands",
            "5/53 Badlands Shadow",
            "5/51 Badlands",
            "5/51 Badlands Shadow",
            "6/3 Badlands",
            "11/3 Badlands",
            "11/8 Badlands",
            "11/6 Badlands",
            "11/7 Badlands",
            "11/9 Badlands",
            "11/10 Badlands",
            "11/11 Badlands",
            "11/12 Badlands",
            "11/13 Badlands",
            "11/14 Badlands",
            "1/13 Badlands",
            "1/9 Badlands",
            "1/11 Badlands",
            "1/14 Badlands",
            "1/10 Badlands",
            "1/12 Badlands",
            "1/15 Badlands",
            "1/7 Badlands",
            "1/5 Badlands",
            "1/16 Badlands",
            "1/8 Badlands",
            "1/6 Badlands",
            "Floor Gun Trap",
            "Floor Missile Trap",
            "Floor Missile Trap Turret",
            "Wall Missile Trap",
            "Wall Missile Trap2",
            "Wall Flame Trap",
            "Wall Flame Trap2",
            "Left Upper Level Door",
            "Right Upper Level Door",
            "4/15 Installation1",
            "4/15 Installation2",
            "3/9 Installation",
            "3/10 Installation",
            "3/11 Installation",
            "3/12 Installation",
            "Substructure Left Door",
            "Substructure Right Door",
            "3/1 Installation",
            "3/2 Installation",
            "Substructure Opening Hole",
            "7/21 Twilight",
            "Unknown Twilight",
            "7/13 Twilight",
            "7/14 Twilight",
            "7/16 Twilight",
            "7/15 Twilight",
            "7/19 Twilight",
            "7/20 Twilight",
            "7/17 Twilight",
            "6/1 Twilight",
            "6/2 Twilight",
            "6/3 Twilight",
            "6/4 Twilight",
            "6/5 Twilight",
            "8/3 Twilight1",
            "8/3 Twilight2",
            "9/29 Ice",
            "9/29 Ice Shadow",
            "9/28 Ice",
            "9/28 Ice Shadow",
            "12/38 Ice ",
            "12/38 Ice Shadow",
            "12/37 Ice",
            "12/37 Ice Shadow",
            "12/33 Ice1",
            "12/33 Ice1 Shadow",
            "9/21 Ice",
            "9/21 Ice Shadow",
            "9/15 Ice",
            "9/15 Ice Shadow",
            "9/16 Ice",
            "9/16 Ice Shadow",
            "Unknown787",
            "Unknown788",
            "12/9 Ice1",
            "12/10 Ice1",
            "9/24 Ice",
            "9/24 Ice Shadow",
            "9/23 Ice",
            "9/23 Ice Shadow",
            "Unknown795",
            "Unknown Ice Shadow",
            "12/7 Ice",
            "12/7 Ice Shadow",
            "12/8 Ice",
            "12/8 Ice Shadow",
            "12/9 Ice2",
            "12/9 Ice2 Shadow",
            "12/10 Ice2",
            "12/10 Ice2 Shadow",
            "12/40 Ice",
            "12/40 Ice Shadow",
            "12/41 Ice",
            "12/41 Ice Shadow",
            "12/42 Ice",
            "12/42 Ice Shadow",
            "12/5 Ice",
            "12/5 Ice Shadow",
            "12/6 Ice",
            "12/6 Ice Shadow",
            "12/36 Ice",
            "12/36 Ice Shadow",
            "12/32 Ice",
            "12/32 Ice Shadow",
            "12/33 Ice2",
            "12/33 Ice2 Shadow",
            "12/34 Ice",
            "12/34 Ice Shadow",
            "12/24 Ice1",
            "12/24 Ice1 Shadow",
            "12/25 Ice1",
            "12/25 Ice1 Shadow",
            "12/30 Ice1",
            "12/30 Ice1 Shadow",
            "12/31 Ice",
            "12/31 Ice Shadow",
            "12/20 Ice",
            "12/30 Ice2",
            "12/30 Ice2 Shadow",
            "9/22 Ice",
            "9/22 Ice Shadow",
            "12/24 Ice2",
            "12/24 Ice2 Shadow",
            "12/25 Ice2",
            "12/25 Ice2 Shadow",
            "Unknown840",
            "4/1 Ice",
            "6/1 Ice",
            "5/6 Ice ",
            "5/6 Ice Shadow",
            "5/7 Ice ",
            "5/7 Ice Shadow",
            "5/8 Ice ",
            "5/8 Ice Shadow",
            "5/9 Ice",
            "5/9 Ice Shadow",
            "10/10 Desert1",
            "10/12 Desert1",
            "10/12 Desert1 Shadow",
            "10/8 Desert1",
            "10/8 Desert1 Shadow",
            "10/9 Desert1",
            "10/9 Desert1 Shadow",
            "6/10 Desert",
            "6/10 Desert Shadow",
            "6/13 Desert1",
            "6/13 Desert1 Shadow",
            "Unknown Desert",
            "Unknown Desert Shadow",
            "10/12 Desert2",
            "10/12 Desert2 Shadow",
            "10/9 Desert2",
            "10/9 Desert2 Shadow",
            "10/10 Desert2",
            "10/10 Desert2 Shadow",
            "10/11 Desert",
            "10/11 Desert Shadow",
            "10/14 Desert",
            "10/14 Desert Shadow",
            "10/41 Desert",
            "10/41 Desert Shadow",
            "10/39 Desert",
            "1/39 Desert Shadow",
            "10/8 Desert2",
            "10/8 Desert2 Shadow",
            "10/6 Desert",
            "10/7 Desert",
            "10/7 Desert Shadow",
            "4/6 Desert",
            "4/6 Desert Shadow",
            "4/11 Desert",
            "4/11 Desert Shadow",
            "4/10 Desert",
            "4/10 Desert Shadow",
            "4/9 Desert",
            "4/7 Desert",
            "4/7 Desert Shadow",
            "4/12 Desert",
            "4/12 Desert Shadow",
            "4/8 Desert",
            "4/13 Desert",
            "4/13 Desert Shadow",
            "4/17 Desert",
            "4/15 Desert1",
            "4/15 Desert1 Shadow",
            "10/23 Desert",
            "10/23 Desert Shadow",
            "10/5 Desert",
            "10/5 Desert Shadow",
            "6/13 Desert2",
            "6/13 Desert2 Shadow",
            "6/20 Desert",
            "4/15 Desert2",
            "4/15 Desert2 Shadow",
            "8/23 Desert",
            "8/23 Desert Shadow",
            "12/1 Desert Overlay",
            "11/3 Desert",
            "Unknown913",
            "Lurker Egg",
            "Devourer",
            "Devourer Shadow",
            "Devourer Birth",
            "Devourer Death",
            "Lurker Birth",
            "Lurker Remnants",
            "Lurker",
            "Lurker Shadow",
            "Overmind Cocoon",
            "Overmind Cocoon Shadow",
            "Dark Archon Energy",
            "Dark Archon Being",
            "Dark Archon Swirl",
            "Dark Archon Death",
            "Corsair",
            "Corsair Shadow",
            "Corsair Engines",
            "Neutron Flare Overlay (Unused)",
            "Dark Templar (Unit)",
            "Warp Gate",
            "Warp Gate Shadow",
            "Warp Gate Overlay",
            "Xel'Naga Temple",
            "Xel'Naga Temple Shadow",
            "Valkyrie",
            "Valkyrie Shadow",
            "Valkyrie Engines",
            "Valkyrie Engines2 (Unused)",
            "Valkyrie Afterburners (Unused)",
            "Medic",
            "Medic Shadow",
            "Medic Remnants",
            "Psi Disrupter",
            "Psi Disrupter Shadow",
            "Power Generator",
            "Power Generator Shadow",
            "Disruption Web",
            "Scantid (Desert)",
            "Scantid (Desert) Shadow",
            "Kakaru (Twilight)",
            "Kakaru (Twilight) Shadow",
            "Ursadon (Ice)",
            "Ursadon (Ice) Shadow",
            "Uraj",
            "Khalis",
            "Halo Rockets Trail",
            "Subterranean Spines",
            "Corrosive Acid Shot",
            "Corrosive Acid Hit",
            "Neutron Flare",
            "Halo Rockets",
            "Optical Flare Grenade",
            "Restoration Hit (Small)",
            "Restoration Hit (Medium)",
            "Restoration Hit (Large)",
            "Unused Heal (Small)",
            "Unused Heal (Medium)",
            "Unused Heal (Large)",
            "Mind Control Hit (Small)",
            "Mind Control Hit (Medium)",
            "Mind Control Hit (Large)",
            "Optical Flare Hit (Small)",
            "Optical Flare Hit (Medium)",
            "Optical Flare Hit (Large)",
            "Feedback (Small)",
            "Feedback (Medium)",
            "Feedback Hit (Large)",
            "Maelstorm Overlay (Small)",
            "Maelstorm Overlay (Medium)",
            "Maelstorm Overlay (Large)",
            "Subterranean Spines Hit",
            "Acid Spores (1) Overlay (Small)",
            "Acid Spores (2-3) Overlay (Small)",
            "Acid Spores (4-5) Overlay (Small)",
            "Acid Spores (6-9) Overlay (Small)",
            "Acid Spores (1) Overlay (Medium)",
            "Acid Spores (2-3) Overlay (Medium)",
            "Acid Spores (4-5) Overlay (Medium)",
            "Acid Spores (6-9) Overlay (Medium)",
            "Acid Spores (1) Overlay (Large)",
            "Acid Spores (2-3) Overlay (Large)",
            "Acid Spores (4-5) Overlay (Large)",
            "Acid Spores (6-9) Overlay (Large)",
            "Maelstorm Hit"
        };


    }
    
    public class IngamePlayerDef : Gettable<IngamePlayerDef> {

        private int _index;
        private string _name;

        public int IngamePlayerIndex { get { return _index; } }

        public string IngamePlayerName { get { return _name; } }

        public static IngamePlayerDef getByIndex(int index) {
            if (index < AllIngamePlayers.Length) {
                return AllIngamePlayers[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return IngamePlayerName;
        }

        public int getMaxValue() {
            return AllIngamePlayers.Length - 1;
        }

        public int getIndex() {
            return IngamePlayerIndex; ;
        }

        public TriggerDefinitionPart getTriggerPart(Func<IngamePlayerDef> getter, Action<IngamePlayerDef> setter, Func<IngamePlayerDef> getDefault) {
            return new TriggerDefinitionGeneralDef<IngamePlayerDef>(getter, setter, getDefault, AllIngamePlayers);
        }

        private IngamePlayerDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid behavior";
            }
        }

        public static IngamePlayerDef[] AllIngamePlayers {
            get {
                if (_allIngamePlayers == null) {
                    _allIngamePlayers = new IngamePlayerDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allIngamePlayers[i] = new IngamePlayerDef(i);
                    }
                }
                return _allIngamePlayers;
            }
        }

        private static IngamePlayerDef[] _allIngamePlayers;

        private static readonly string[] _Defs = {
            "Player 1",
            "Player 2",
            "Player 3",
            "Player 4",
            "Player 5",
            "Player 6",
            "Player 7",
            "Player 8",
            "Unknown 1",
            "Unknown 2",
            "Unknown 3",
            "Unknown 4",
        };
    }
    
    public class IntDef : Gettable<IntDef>, SaveableItem {

        private int _value;

        public bool UseHex = false;

        public int getIndex() {
            return _value;
        }

        public virtual int getMaxValue() {
            return int.MaxValue;
        }

        public IntDef(int number, bool useHex) {
            _value = number > getMaxValue() ? getMaxValue() : number;
            UseHex = useHex;
        }

        public static IntDef getDefaultValue(bool usehex) {
            IntDef def = IntDef.getByIndex(0, usehex);
            return def;
        }

        public static IntDef getByIndex(int index, bool useHex) {
            return new IntDef(index, useHex);
        }

        public virtual TriggerDefinitionPart getTriggerPart(Func<IntDef> getter, Action<IntDef> setter, Func<IntDef> getDefault) {
            return new TriggerDefinitionIntAmount(() => getter(), (IntDef def) => { if (def.getIndex() <= getMaxValue()) { setter(def); } }, getDefault);
        }

        public override string ToString() {
            return UseHex ? "0x" + _value.ToString("X") : _value.ToString();
        }

        public string ToSaveString() {
            return _value.ToString();
        }
    }

    public class Int8Def : IntDef, Gettable<Int8Def> {

        public Int8Def(int number, bool usehex) : base(number, usehex) { }

        public override int getMaxValue() {
            return 255;
        }

        public static new Int8Def getByIndex(int num, bool useHex) {
            return new Int8Def(num, useHex);
        }

        public TriggerDefinitionPart getTriggerPart(Func<Int8Def> getter, Action<Int8Def> setter, Func<Int8Def> getDefault) {
            return base.getTriggerPart(getter, (IntDef d) => { setter(new Int8Def(d.getIndex(), d.UseHex)); }, getDefault);
        }
    }

    public class Int16Def : IntDef, Gettable<Int16Def> {

        public Int16Def(int number, bool usehex) : base(number, usehex) { }

        public override int getMaxValue() {
            return 255*255;
        }

        public static new Int16Def getByIndex(int num, bool usehex) {
            return new Int16Def(num, usehex);
        }

        public TriggerDefinitionPart getTriggerPart(Func<Int16Def> getter, Action<Int16Def> setter, Func<Int16Def> getDefault) {
            return base.getTriggerPart(getter,(IntDef d) => { setter(new Int16Def(d.getIndex(), d.UseHex)); }, getDefault);
        }
    }

    public class Int32Def : IntDef, Gettable<Int32Def> {

        public Int32Def(int number, bool usehex) : base(number, usehex) { }

        public override int getMaxValue() {
            return int.MaxValue;
        }

        public static new Int32Def getByIndex(int num, bool usehex) {
            return new Int32Def(num, usehex);
        }

        public TriggerDefinitionPart getTriggerPart(Func<Int32Def> getter, Action<Int32Def> setter, Func<Int32Def> getDefault) {
            return base.getTriggerPart(getter, (IntDef d) => { setter(new Int32Def(d.getIndex(), d.UseHex)); }, getDefault);
        }
    }

    public class KeyDef : Gettable<KeyDef> {

        private int _value;
        private string _name;

        public override string ToString() {
            return _name;
        }

        public int getIndex() {
            return _value;
        }

        private KeyDef(int value, string name) {
            _value = value;
            _name = name;
        }

        public int getMaxValue() {
            return AllKeys.Length - 1;
        }

        public TriggerDefinitionPart getTriggerPart(Func<KeyDef> getter, Action<KeyDef> setter, Func<KeyDef> getDefault) {
            return new TriggerDefinitionGeneralDef<KeyDef>(getter, setter, getDefault, AllKeys);
        }

        public static KeyDef getByIndex(int index) {
            if (index < AllKeys.Length) {
                return AllKeys[index];
            }
            throw new NotImplementedException();
        }

        public static KeyDef[] AllKeys = new KeyDef[] {
            new KeyDef(0, "VK_NONE (No button pressed)"),
            new KeyDef(1, "VK_LBUTTON (Left mouse button)"),
            new KeyDef(2, "VK_RBUTTON (Right mouse button)"),
            new KeyDef(3, "VK_CANCEL (Control-break processing)"),
            new KeyDef(4, "VK_MBUTTON (Middle mouse button)"),
            new KeyDef(5, "VK_XBUTTON1 (X1 mouse button)"),
            new KeyDef(6, "VK_XBUTTON2 (X2 mouse button)"),
            new KeyDef(7, "0x07 (Undefined)"),
            new KeyDef(8, "VK_BACK (BACKSPACE key)"),
            new KeyDef(9, "VK_TAB (TAB key)"),
            new KeyDef(10, "0x0A (Reserved)"),
            new KeyDef(11, "0x0B (Reserved)"),
            new KeyDef(12, "VK_CLEAR (CLEAR key)"),
            new KeyDef(13, "VK_RETURN (ENTER key)"),
            new KeyDef(14, "0x0E (Undefined)"),
            new KeyDef(15, "0x0F (Undefined)"),
            new KeyDef(16, "VK_SHIFT (SHIFT key)"),
            new KeyDef(17, "VK_CONTROL (CTRL key)"),
            new KeyDef(18, "VK_MENU (ALT key)"),
            new KeyDef(19, "VK_PAUSE (PAUSE key)"),
            new KeyDef(20, "VK_CAPITAL (CAPS LOCK key)"),
            new KeyDef(21, "VK_KANA (IME Kana mode)"),
            new KeyDef(22, "0x16 (Undefined)"),
            new KeyDef(23, "VK_JUNJA (IME Junja mode)"),
            new KeyDef(24, "VK_FINAL (IME final mode)"),
            new KeyDef(25, "VK_HANJA (IME Hanja mode)"),
            new KeyDef(26, "0x1A (Undefined)"),
            new KeyDef(27, "VK_ESCAPE (ESC key)"),
            new KeyDef(28, "VK_CONVERT (IME convert)"),
            new KeyDef(29, "VK_NONCONVERT (IME nonconvert)"),
            new KeyDef(30, "VK_ACCEPT (IME accept)"),
            new KeyDef(31, "VK_MODECHANGE (IME mode change request)"),
            new KeyDef(32, "VK_SPACE (SPACEBAR)"),
            new KeyDef(33, "VK_PRIOR (PAGE UP key)"),
            new KeyDef(34, "VK_NEXT (PAGE DOWN key)"),
            new KeyDef(35, "VK_END (END key)"),
            new KeyDef(36, "VK_HOME (HOME key)"),
            new KeyDef(37, "VK_LEFT (LEFT ARROW key)"),
            new KeyDef(38, "VK_UP (UP ARROW key)"),
            new KeyDef(39, "VK_RIGHT (RIGHT ARROW key)"),
            new KeyDef(40, "VK_DOWN (DOWN ARROW key)"),
            new KeyDef(41, "VK_SELECT (SELECT key)"),
            new KeyDef(42, "VK_PRINT (PRINT key)"),
            new KeyDef(43, "VK_EXECUTE (EXECUTE key)"),
            new KeyDef(44, "VK_SNAPSHOT (PRINT SCREEN key)"),
            new KeyDef(45, "VK_INSERT (INS key)"),
            new KeyDef(46, "VK_DELETE (DEL key)"),
            new KeyDef(47, "VK_HELP (HELP key)"),
            new KeyDef(48, "VK_0 (0 key)"),
            new KeyDef(49, "VK_1 (1 key)"),
            new KeyDef(50, "VK_2 (2 key)"),
            new KeyDef(51, "VK_3 (3 key)"),
            new KeyDef(52, "VK_4 (4 key)"),
            new KeyDef(53, "VK_5 (5 key)"),
            new KeyDef(54, "VK_6 (6 key)"),
            new KeyDef(55, "VK_7 (7 key)"),
            new KeyDef(56, "VK_8 (8 key)"),
            new KeyDef(57, "VK_9 (9 key)"),
            new KeyDef(58, "0x3A (Undefined)"),
            new KeyDef(59, "0x3B (Undefined)"),
            new KeyDef(60, "0x3C (Undefined)"),
            new KeyDef(61, "0x3D (Undefined)"),
            new KeyDef(62, "0x3E (Undefined)"),
            new KeyDef(63, "0x3F (Undefined)"),
            new KeyDef(64, "0x40 (Undefined)"),
            new KeyDef(65, "VK_A (A key)"),
            new KeyDef(66, "VK_B (B key)"),
            new KeyDef(67, "VK_C (C key)"),
            new KeyDef(68, "VK_D (D key)"),
            new KeyDef(69, "VK_E (E key)"),
            new KeyDef(70, "VK_F (F key)"),
            new KeyDef(71, "VK_G (G key)"),
            new KeyDef(72, "VK_H (H key)"),
            new KeyDef(73, "VK_I (I key)"),
            new KeyDef(74, "VK_J (J key)"),
            new KeyDef(75, "VK_K (K key)"),
            new KeyDef(76, "VK_L (L key)"),
            new KeyDef(77, "VK_M (M key)"),
            new KeyDef(78, "VK_N (N key)"),
            new KeyDef(79, "VK_O (O key)"),
            new KeyDef(80, "VK_P (P key)"),
            new KeyDef(81, "VK_Q (Q key)"),
            new KeyDef(82, "VK_R (R key)"),
            new KeyDef(83, "VK_S (S key)"),
            new KeyDef(84, "VK_T (T key)"),
            new KeyDef(85, "VK_U (U key)"),
            new KeyDef(86, "VK_V (V key)"),
            new KeyDef(87, "VK_W (W key)"),
            new KeyDef(88, "VK_X (X key)"),
            new KeyDef(89, "VK_Y (Y key)"),
            new KeyDef(90, "VK_Z (Z key)"),
            new KeyDef(91, "VK_LWIN (Left Windows key)"),
            new KeyDef(92, "VK_RWIN (Right Windows key)"),
            new KeyDef(93, "VK_APPS (Applications key)"),
            new KeyDef(94, "0x5E (Reserved)"),
            new KeyDef(95, "VK_SLEEP (Computer Sleep key)"),
            new KeyDef(96, "VK_NUMPAD0 (Numeric keypad 0 key)"),
            new KeyDef(97, "VK_NUMPAD1 (Numeric keypad 1 key)"),
            new KeyDef(98, "VK_NUMPAD2 (Numeric keypad 2 key)"),
            new KeyDef(99, "VK_NUMPAD3 (Numeric keypad 3 key)"),
            new KeyDef(100, "VK_NUMPAD4 (Numeric keypad 4 key)"),
            new KeyDef(101, "VK_NUMPAD5 (Numeric keypad 5 key)"),
            new KeyDef(102, "VK_NUMPAD6 (Numeric keypad 6 key)"),
            new KeyDef(103, "VK_NUMPAD7 (Numeric keypad 7 key)"),
            new KeyDef(104, "VK_NUMPAD8 (Numeric keypad 8 key)"),
            new KeyDef(105, "VK_NUMPAD9 (Numeric keypad 9 key)"),
            new KeyDef(106, "VK_MULTIPLY (Multiply key)"),
            new KeyDef(107, "VK_ADD (Add key)"),
            new KeyDef(108, "VK_SEPARATOR (Separator key)"),
            new KeyDef(109, "VK_SUBTRACT (Subtract key)"),
            new KeyDef(110, "VK_DECIMAL (Decimal key)"),
            new KeyDef(111, "VK_DIVIDE (Divide key)"),
            new KeyDef(112, "VK_F1 (F1 key)"),
            new KeyDef(113, "VK_F2 (F2 key)"),
            new KeyDef(114, "VK_F3 (F3 key)"),
            new KeyDef(115, "VK_F4 (F4 key)"),
            new KeyDef(116, "VK_F5 (F5 key)"),
            new KeyDef(117, "VK_F6 (F6 key)"),
            new KeyDef(118, "VK_F7 (F7 key)"),
            new KeyDef(119, "VK_F8 (F8 key)"),
            new KeyDef(120, "VK_F9 (F9 key)"),
            new KeyDef(121, "VK_F10 (F10 key)"),
            new KeyDef(122, "VK_F11 (F11 key)"),
            new KeyDef(123, "VK_F12 (F12 key)"),
            new KeyDef(124, "VK_F13 (F13 key)"),
            new KeyDef(125, "VK_F14 (F14 key)"),
            new KeyDef(126, "VK_F15 (F15 key)"),
            new KeyDef(127, "VK_F16 (F16 key)"),
            new KeyDef(128, "VK_F17 (F17 key)"),
            new KeyDef(129, "VK_F18 (F18 key)"),
            new KeyDef(130, "VK_F19 (F19 key)"),
            new KeyDef(131, "VK_F20 (F20 key)"),
            new KeyDef(132, "VK_F21 (F21 key)"),
            new KeyDef(133, "VK_F22 (F22 key)"),
            new KeyDef(134, "VK_F23 (F23 key)"),
            new KeyDef(135, "VK_F24 (F24 key)"),
            new KeyDef(136, "0x88 (Unassigned)"),
            new KeyDef(137, "0x89 (Unassigned)"),
            new KeyDef(138, "0x8A (Unassigned)"),
            new KeyDef(139, "0x8B (Unassigned)"),
            new KeyDef(140, "0x8C (Unassigned)"),
            new KeyDef(141, "0x8D (Unassigned)"),
            new KeyDef(142, "0x8E (Unassigned)"),
            new KeyDef(143, "0x8F (Unassigned)"),
            new KeyDef(144, "VK_NUMLOCK (NUM LOCK key)"),
            new KeyDef(145, "VK_SCROLL (SCROLL LOCK key)"),
            new KeyDef(146, "0x92 (OEM specific)"),
            new KeyDef(147, "0x93 (OEM specific)"),
            new KeyDef(148, "0x94 (OEM specific)"),
            new KeyDef(149, "0x95 (OEM specific)"),
            new KeyDef(150, "0x96 (OEM specific)"),
            new KeyDef(151, "0x97 (Unassigned)"),
            new KeyDef(152, "0x98 (Unassigned)"),
            new KeyDef(153, "0x99 (Unassigned)"),
            new KeyDef(154, "0x9A (Unassigned)"),
            new KeyDef(155, "0x9B (Unassigned)"),
            new KeyDef(156, "0x9C (Unassigned)"),
            new KeyDef(157, "0x9D (Unassigned)"),
            new KeyDef(158, "0x9E (Unassigned)"),
            new KeyDef(159, "0x9F (Unassigned)"),
            new KeyDef(160, "VK_LSHIFT (Left SHIFT key)"),
            new KeyDef(161, "VK_RSHIFT (Right SHIFT key)"),
            new KeyDef(162, "VK_LCONTROL (Left CONTROL key)"),
            new KeyDef(163, "VK_RCONTROL (Right CONTROL key)"),
            new KeyDef(164, "VK_LMENU (Left MENU key)"),
            new KeyDef(165, "VK_RMENU (Right MENU key)"),
            new KeyDef(166, "VK_BROWSER_BACK (Browser Back key)"),
            new KeyDef(167, "VK_BROWSER_FORWARD (Browser Forward key)"),
            new KeyDef(168, "VK_BROWSER_REFRESH (Browser Refresh key)"),
            new KeyDef(169, "VK_BROWSER_STOP (Browser Stop key)"),
            new KeyDef(170, "VK_BROWSER_SEARCH (Browser Search key)"),
            new KeyDef(171, "VK_BROWSER_FAVORITES (Browser Favorites key)"),
            new KeyDef(172, "VK_BROWSER_HOME (Browser Start and Home key)"),
            new KeyDef(173, "VK_VOLUME_MUTE (Volume Mute key)"),
            new KeyDef(174, "VK_VOLUME_DOWN (Volume Down key)"),
            new KeyDef(175, "VK_VOLUME_UP (Volume Up key)"),
            new KeyDef(176, "VK_MEDIA_NEXT_TRACK (Next Track key)"),
            new KeyDef(177, "VK_MEDIA_PREV_TRACK (Previous Track key)"),
            new KeyDef(178, "VK_MEDIA_STOP (Stop Media key)"),
            new KeyDef(179, "VK_MEDIA_PLAY_PAUSE (Play/Pause Media key)"),
            new KeyDef(180, "VK_LAUNCH_MAIL (Start Mail key)"),
            new KeyDef(181, "VK_LAUNCH_MEDIA_SELECT (Select Media key)"),
            new KeyDef(182, "VK_LAUNCH_APP1 (Start Application 1 key)"),
            new KeyDef(183, "VK_LAUNCH_APP2 (Start Application 2 key)"),
            new KeyDef(184, "0xB8 (Reserved)"),
            new KeyDef(185, "0xB9 (Reserved)"),
            new KeyDef(186, "VK_OEM_1 (Used for miscellaneous characters; it can vary by keyboard.)"),
            new KeyDef(187, "VK_OEM_PLUS (For any country/region, the '+' key)"),
            new KeyDef(188, "VK_OEM_COMMA (For any country/region, the ',' key)"),
            new KeyDef(189, "VK_OEM_MINUS (For any country/region, the '-' key)"),
            new KeyDef(190, "VK_OEM_PERIOD (For any country/region, the '.' key)"),
            new KeyDef(191, "VK_OEM_2 (Used for miscellaneous characters; it can vary by keyboard.)"),
            new KeyDef(192, "VK_OEM_3 (Used for miscellaneous characters; it can vary by keyboard.)"),
            new KeyDef(193, "0xC1 (Reserved)"),
            new KeyDef(194, "0xC2 (Reserved)"),
            new KeyDef(195, "0xC3 (Reserved)"),
            new KeyDef(196, "0xC4 (Reserved)"),
            new KeyDef(197, "0xC5 (Reserved)"),
            new KeyDef(198, "0xC6 (Reserved)"),
            new KeyDef(199, "0xC7 (Reserved)"),
            new KeyDef(200, "0xC8 (Reserved)"),
            new KeyDef(201, "0xC9 (Reserved)"),
            new KeyDef(202, "0xCA (Reserved)"),
            new KeyDef(203, "0xCB (Reserved)"),
            new KeyDef(204, "0xCC (Reserved)"),
            new KeyDef(205, "0xCD (Reserved)"),
            new KeyDef(206, "0xCE (Reserved)"),
            new KeyDef(207, "0xCF (Reserved)"),
            new KeyDef(208, "0xD0 (Reserved)"),
            new KeyDef(209, "0xD1 (Reserved)"),
            new KeyDef(210, "0xD2 (Reserved)"),
            new KeyDef(211, "0xD3 (Reserved)"),
            new KeyDef(212, "0xD4 (Reserved)"),
            new KeyDef(213, "0xD5 (Reserved)"),
            new KeyDef(214, "0xD6 (Reserved)"),
            new KeyDef(215, "0xD7 (Reserved)"),
            new KeyDef(216, "0xD8 (Unassigned)"),
            new KeyDef(217, "0xD9 (Unassigned)"),
            new KeyDef(218, "0xDA (Unassigned)"),
            new KeyDef(219, "VK_OEM_4 (Used for miscellaneous characters; it can vary by keyboard.)"),
            new KeyDef(220, "VK_OEM_5 (Used for miscellaneous characters; it can vary by keyboard.)"),
            new KeyDef(221, "VK_OEM_6 (Used for miscellaneous characters; it can vary by keyboard.)"),
            new KeyDef(222, "VK_OEM_7 (Used for miscellaneous characters; it can vary by keyboard.)"),
            new KeyDef(223, "VK_OEM_8 (Used for miscellaneous characters; it can vary by keyboard.)"),
            new KeyDef(224, "0xE0 (Reserved)"),
            new KeyDef(225, "0xE1 (OEM Specific)"),
            new KeyDef(226, "VK_OEM_102 (Either the angle bracket key or the backslash key on the RT 102-key keyboard)"),
            new KeyDef(227, "0xE3 (OEM Specific)"),
            new KeyDef(228, "0xE4 (OEM Specific)"),
            new KeyDef(229, "VK_PROCESSKEY (IME PROCESS key)"),
            new KeyDef(230, "0xE6 (OEM Specific)"),
            new KeyDef(231, "VK_PACKET (Used to pass Unicode characters as if they were keystrokes.)"),
            new KeyDef(232, "0xE8 (Unassigned)"),
            new KeyDef(233, "0xE9 (OEM Specific)"),
            new KeyDef(234, "0xEA (OEM Specific)"),
            new KeyDef(235, "0xEB (OEM Specific)"),
            new KeyDef(236, "0xEC (OEM Specific)"),
            new KeyDef(237, "0xED (OEM Specific)"),
            new KeyDef(238, "0xEE (OEM Specific)"),
            new KeyDef(239, "0xEF (OEM Specific)"),
            new KeyDef(240, "0xF0 (OEM Specific)"),
            new KeyDef(241, "0xF1 (OEM Specific)"),
            new KeyDef(242, "0xF2 (OEM Specific)"),
            new KeyDef(243, "0xF3 (OEM Specific)"),
            new KeyDef(244, "0xF4 (OEM Specific)"),
            new KeyDef(245, "0xF5 (OEM Specific)"),
            new KeyDef(246, "VK_ATTN (Attn key)"),
            new KeyDef(247, "VK_CRSEL (CrSel key)"),
            new KeyDef(248, "VK_EXSEL (ExSel key)"),
            new KeyDef(249, "VK_EREOF (Erase EOF key)"),
            new KeyDef(250, "VK_PLAY (Play key)"),
            new KeyDef(251, "VK_ZOOM (Zoom key)"),
            new KeyDef(252, "VK_NONAME (Reserved)"),
            new KeyDef(253, "VK_PA1 (PA1 key)"),
            new KeyDef(254, "VK_OEM_CLEAR (Clear key)"),
            new KeyDef(255, "0xFF (Undefined)")
        };
    }

    public class LatencyDef : SaveableItem, Gettable<LatencyDef> {

        private string _value;
        private int _index;

        public override string ToString() {
            return _value;
        }

        public static LatencyDef getByIndex(int index) {
            if (index == 0) {
                return SinglePlayer;
            } else if (index == 1) {
                return Low;
            } else if (index == 2) {
                return High;
            } else if (index == 3) {
                return ExtraHigh;
            }
            throw new NotImplementedException();
        }

        public string ToSaveString() {
            return ToString();
        }

        public int getMaxValue() {
            return 3;
        }

        public int getIndex() {
            return _index;
        }

        public TriggerDefinitionPart getTriggerPart(Func<LatencyDef> getter, Action<LatencyDef> setter, Func<LatencyDef> getDefault) {
            return new TriggerDefinitionGeneralDef<LatencyDef>(getter, setter, getDefault, new LatencyDef[] { SinglePlayer, Low, High, ExtraHigh });
        }

        private LatencyDef(string value, int index) {
            _value = value;
            _index = index;
        }

        public static readonly LatencyDef SinglePlayer = new LatencyDef("Single Player", 0);
        public static readonly LatencyDef Low = new LatencyDef("Low", 1);
        public static readonly LatencyDef High = new LatencyDef("High", 2);
        public static readonly LatencyDef ExtraHigh = new LatencyDef("Extra High", 3);

        public static SaveableItem getDefaultValue() {
            return LatencyDef.Low;
        }
    }

    public class LocationDef : SaveableItem, Gettable<LocationDef> {

        private int _index;
        private string _name;
        private string _customName;

        public int LocationIndex { get { return _index; } }

        public string LocationName { get { return MainWindow.UseCustomName ? _name : _customName; } }

        public static void __setLocationsCountDoNotUseOutsideOfParser(int locationsNumber) {
            _allLocations = new LocationDef[locationsNumber];
            for (int i = 0; i < locationsNumber; i++) {
                AllLocations[i] = new LocationDef(i);
            }
        }


        public static void __setLocationNameDontUseOutsideOfParser(int locationIndex, string locationName) {
            AllLocations[locationIndex]._customName = locationName;
        }

        public static LocationDef getByIndex(int index) {
            if (index < AllLocations.Length) {
                return AllLocations[index];
            }
            throw new NotImplementedException();
        }

        public static LocationDef getDefaultValue() {
            return LocationDef.getByIndex(64);
        }

        private LocationDef(int index) {
            _index = index;
            _name = index == 64 ? "Anywhere" : "Location " + index;
            _customName = _name;
        }

        public override string ToString() {
            return LocationName.ToString();
        }

        public string ToSaveString() {
            return _index.ToString();
        }

        public int getMaxValue() {
            return AllLocations.Length;
        }

        public int getIndex() {
            return _index;
        }

        public TriggerDefinitionPart getTriggerPart(Func<LocationDef> getter, Action<LocationDef> setter, Func<LocationDef> getDefault) {
            return new TriggerDefinitionGeneralDef<LocationDef>(getter, setter, getDefault, AllLocations);
        }

        public static LocationDef[] AllLocations {
            get {
                if (_allLocations == null) {
                    _allLocations = new LocationDef[256];
                    for (int i = 0; i < 256; i++) {
                        AllLocations[i] = new LocationDef(i);
                    }
                }
                return _allLocations;
            }
        }

        private static LocationDef[] _allLocations;
    }

    public class MessageType : SaveableItem, Gettable<MessageType> {

        private string _value;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return ToString();
        }

        private MessageType(string value) {
            _value = value;
        }

        public static readonly MessageType ALwaysDisplay = new MessageType("Always Display");
        public static readonly MessageType DontAlwaysDisplay = new MessageType("Don't Always Display");

        public static MessageType getDefaultValue() {
            return MessageType.ALwaysDisplay;
        }

        public int getMaxValue() {
            return AllDisplays.Length - 1;
        }

        public int getIndex() {
            if (this == ALwaysDisplay) {
                return 0;
            } else if (this == DontAlwaysDisplay) {
                return 1;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<MessageType> getter, Action<MessageType> setter, Func<MessageType> getDefault) {
            return new TriggerDefinitionGeneralDef<MessageType>(getter, setter, getDefault, AllDisplays);
        }

        public static MessageType[] AllDisplays = new MessageType[] { ALwaysDisplay, DontAlwaysDisplay };
    }

    public class Order : SaveableItem, Gettable<Order> {

        private string _value;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return ToString();
        }

        private Order(string value) {
            _value = value;
        }

        public static readonly Order Attack = new Order("Attack");
        public static readonly Order Move = new Order("Move");
        public static readonly Order Patrol = new Order("Patrol");

        public static Order getDefaultValue() {
            return Order.Attack;
        }

        public int getMaxValue() {
            return AllOrders.Length - 1;
        }

        public int getIndex() {
            if (this == Attack) {
                return 0;
            } else if (this == Move) {
                return 1;
            } else if (this == Patrol) {
                return 2;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<Order> getter, Action<Order> setter, Func<Order> getDefault) {
            return new TriggerDefinitionGeneralDef<Order>(getter, setter, getDefault, AllOrders);
        }

        public static Order[] AllOrders = new Order[] { Attack, Move, Patrol };
    }

    public class PercentageDef : IntDef {
        public PercentageDef(int number) : base(number % 101, false) {
        }
    }

    public class PlayerColorDef : Gettable<PlayerColorDef> {

        private int _index;
        private string _name;

        public override string ToString() {
            return _name;
        }

        public int getIndex() {
            return _index;
        }

        private PlayerColorDef(int index, string name) {
            _index = index;
            _name = name;
        }

        public int getMaxValue() {
            return AllPlayerColors.Length - 1;
        }

        public static PlayerColorDef getByIndex(int index) {
            if(index < AllPlayerColors.Length) {
                return AllPlayerColors[index];
            }
            throw new NotImplementedException();
        }

        public static PlayerColorDef getByPlayerIndex(int index) {
            if(index < AllPlayerColorsSorted.Length) {
                return AllPlayerColorsSorted[index];
            }
            throw new NotImplementedException();
        }
        public TriggerDefinitionPart getTriggerPart(Func<PlayerColorDef> getter, Action<PlayerColorDef> setter, Func<PlayerColorDef> getDefault) {
            return new TriggerDefinitionGeneralDef<PlayerColorDef>(getter, setter, getDefault, AllPlayerColorsSorted);
        }

        public static PlayerColorDef[] AllPlayerColors { get {
                if (_allPlayerColors == null) {
                    _allPlayerColors = new PlayerColorDef[AllPlayerColorsSorted.Length];
                    foreach(PlayerColorDef cd in AllPlayerColorsSorted) {
                        _allPlayerColors[cd._index] = cd;
                    }
                }
                return _allPlayerColors;
            } }

        private static PlayerColorDef[] _allPlayerColors;

        public static PlayerColorDef[] AllPlayerColorsSorted = new PlayerColorDef[] {
            new PlayerColorDef(111, "Red (Player 1)"),
            new PlayerColorDef(165, "Blue (Player 2)"),
            new PlayerColorDef(159, "Teal (Player 3)"),
            new PlayerColorDef(164, "Purple (Player 4)"),
            new PlayerColorDef(156, "Orange (Player 5)"),
            new PlayerColorDef(19, "Brown (Player 6)"),
            new PlayerColorDef(84, "White (Player 7)"),
            new PlayerColorDef(135, "Yellow (Player 8)"),
            new PlayerColorDef(185, "Green (Player 9)"),
            new PlayerColorDef(136, "Pale Yellow (Player 10)"),
            new PlayerColorDef(134, "Tan (Player 11)"),
            new PlayerColorDef(51, "Dark Aqua (Player 12)"),


            new PlayerColorDef(0, "_Color 0"),
            new PlayerColorDef(1, "_Color 1"),
            new PlayerColorDef(2, "_Color 2"),
            new PlayerColorDef(3, "_Color 3"),
            new PlayerColorDef(4, "_Color 4"),
            new PlayerColorDef(5, "_Color 5"),
            new PlayerColorDef(6, "_Color 6"),
            new PlayerColorDef(7, "_Color 7"),
            new PlayerColorDef(8, "_Color 8"),
            new PlayerColorDef(9, "_Color 9"),
            new PlayerColorDef(10, "_Color 10"),
            new PlayerColorDef(11, "_Color 11"),
            new PlayerColorDef(12, "_Color 12"),
            new PlayerColorDef(13, "_Color 13"),
            new PlayerColorDef(14, "_Color 14"),
            new PlayerColorDef(15, "_Color 15"),
            new PlayerColorDef(16, "_Color 16"),
            new PlayerColorDef(17, "_Color 17"),
            new PlayerColorDef(18, "_Color 18"),
            new PlayerColorDef(20, "_Color 20"),
            new PlayerColorDef(21, "_Color 21"),
            new PlayerColorDef(22, "_Color 22"),
            new PlayerColorDef(23, "_Color 23"),
            new PlayerColorDef(24, "_Color 24"),
            new PlayerColorDef(25, "_Color 25"),
            new PlayerColorDef(26, "_Color 26"),
            new PlayerColorDef(27, "_Color 27"),
            new PlayerColorDef(28, "_Color 28"),
            new PlayerColorDef(29, "_Color 29"),
            new PlayerColorDef(30, "_Color 30"),
            new PlayerColorDef(31, "_Color 31"),
            new PlayerColorDef(32, "_Color 32"),
            new PlayerColorDef(33, "_Color 33"),
            new PlayerColorDef(34, "_Color 34"),
            new PlayerColorDef(35, "_Color 35"),
            new PlayerColorDef(36, "_Color 36"),
            new PlayerColorDef(37, "_Color 37"),
            new PlayerColorDef(38, "_Color 38"),
            new PlayerColorDef(39, "_Color 39"),
            new PlayerColorDef(40, "_Color 40"),
            new PlayerColorDef(41, "_Color 41"),
            new PlayerColorDef(42, "_Color 42"),
            new PlayerColorDef(43, "_Color 43"),
            new PlayerColorDef(44, "_Color 44"),
            new PlayerColorDef(45, "_Color 45"),
            new PlayerColorDef(46, "_Color 46"),
            new PlayerColorDef(47, "_Color 47"),
            new PlayerColorDef(48, "_Color 48"),
            new PlayerColorDef(49, "_Color 49"),
            new PlayerColorDef(50, "_Color 50"),
            new PlayerColorDef(52, "_Color 52"),
            new PlayerColorDef(53, "_Color 53"),
            new PlayerColorDef(54, "_Color 54"),
            new PlayerColorDef(55, "_Color 55"),
            new PlayerColorDef(56, "_Color 56"),
            new PlayerColorDef(57, "_Color 57"),
            new PlayerColorDef(58, "_Color 58"),
            new PlayerColorDef(59, "_Color 59"),
            new PlayerColorDef(60, "_Color 60"),
            new PlayerColorDef(61, "_Color 61"),
            new PlayerColorDef(62, "_Color 62"),
            new PlayerColorDef(63, "_Color 63"),
            new PlayerColorDef(64, "_Color 64"),
            new PlayerColorDef(65, "_Color 65"),
            new PlayerColorDef(66, "_Color 66"),
            new PlayerColorDef(67, "_Color 67"),
            new PlayerColorDef(68, "_Color 68"),
            new PlayerColorDef(69, "_Color 69"),
            new PlayerColorDef(70, "_Color 70"),
            new PlayerColorDef(71, "_Color 71"),
            new PlayerColorDef(72, "_Color 72"),
            new PlayerColorDef(73, "_Color 73"),
            new PlayerColorDef(74, "_Color 74"),
            new PlayerColorDef(75, "_Color 75"),
            new PlayerColorDef(76, "_Color 76"),
            new PlayerColorDef(77, "_Color 77"),
            new PlayerColorDef(78, "_Color 78"),
            new PlayerColorDef(79, "_Color 79"),
            new PlayerColorDef(80, "_Color 80"),
            new PlayerColorDef(81, "_Color 81"),
            new PlayerColorDef(82, "_Color 82"),
            new PlayerColorDef(83, "_Color 83"),
            new PlayerColorDef(85, "_Color 85"),
            new PlayerColorDef(86, "_Color 86"),
            new PlayerColorDef(87, "_Color 87"),
            new PlayerColorDef(88, "_Color 88"),
            new PlayerColorDef(89, "_Color 89"),
            new PlayerColorDef(90, "_Color 90"),
            new PlayerColorDef(91, "_Color 91"),
            new PlayerColorDef(92, "_Color 92"),
            new PlayerColorDef(93, "_Color 93"),
            new PlayerColorDef(94, "_Color 94"),
            new PlayerColorDef(95, "_Color 95"),
            new PlayerColorDef(96, "_Color 96"),
            new PlayerColorDef(97, "_Color 97"),
            new PlayerColorDef(98, "_Color 98"),
            new PlayerColorDef(99, "_Color 99"),
            new PlayerColorDef(100, "_Color 100"),
            new PlayerColorDef(101, "_Color 101"),
            new PlayerColorDef(102, "_Color 102"),
            new PlayerColorDef(103, "_Color 103"),
            new PlayerColorDef(104, "_Color 104"),
            new PlayerColorDef(105, "_Color 105"),
            new PlayerColorDef(106, "_Color 106"),
            new PlayerColorDef(107, "_Color 107"),
            new PlayerColorDef(108, "_Color 108"),
            new PlayerColorDef(109, "_Color 109"),
            new PlayerColorDef(110, "_Color 110"),
            new PlayerColorDef(112, "_Color 112"),
            new PlayerColorDef(113, "_Color 113"),
            new PlayerColorDef(114, "_Color 114"),
            new PlayerColorDef(115, "_Color 115"),
            new PlayerColorDef(116, "_Color 116"),
            new PlayerColorDef(117, "_Color 117"),
            new PlayerColorDef(118, "_Color 118"),
            new PlayerColorDef(119, "_Color 119"),
            new PlayerColorDef(120, "_Color 120"),
            new PlayerColorDef(121, "_Color 121"),
            new PlayerColorDef(122, "_Color 122"),
            new PlayerColorDef(123, "_Color 123"),
            new PlayerColorDef(124, "_Color 124"),
            new PlayerColorDef(125, "_Color 125"),
            new PlayerColorDef(126, "_Color 126"),
            new PlayerColorDef(127, "_Color 127"),
            new PlayerColorDef(128, "_Color 128"),
            new PlayerColorDef(129, "_Color 129"),
            new PlayerColorDef(130, "_Color 130"),
            new PlayerColorDef(131, "_Color 131"),
            new PlayerColorDef(132, "_Color 132"),
            new PlayerColorDef(133, "_Color 133"),
            new PlayerColorDef(137, "_Color 137"),
            new PlayerColorDef(138, "_Color 138"),
            new PlayerColorDef(139, "_Color 139"),
            new PlayerColorDef(140, "_Color 140"),
            new PlayerColorDef(141, "_Color 141"),
            new PlayerColorDef(142, "_Color 142"),
            new PlayerColorDef(143, "_Color 143"),
            new PlayerColorDef(144, "_Color 144"),
            new PlayerColorDef(145, "_Color 145"),
            new PlayerColorDef(146, "_Color 146"),
            new PlayerColorDef(147, "_Color 147"),
            new PlayerColorDef(148, "_Color 148"),
            new PlayerColorDef(149, "_Color 149"),
            new PlayerColorDef(150, "_Color 150"),
            new PlayerColorDef(151, "_Color 151"),
            new PlayerColorDef(152, "_Color 152"),
            new PlayerColorDef(153, "_Color 153"),
            new PlayerColorDef(154, "_Color 154"),
            new PlayerColorDef(155, "_Color 155"),
            new PlayerColorDef(157, "_Color 157"),
            new PlayerColorDef(158, "_Color 158"),
            new PlayerColorDef(160, "_Color 160"),
            new PlayerColorDef(161, "_Color 161"),
            new PlayerColorDef(162, "_Color 162"),
            new PlayerColorDef(163, "_Color 163"),
            new PlayerColorDef(166, "_Color 166"),
            new PlayerColorDef(167, "_Color 167"),
            new PlayerColorDef(168, "_Color 168"),
            new PlayerColorDef(169, "_Color 169"),
            new PlayerColorDef(170, "_Color 170"),
            new PlayerColorDef(171, "_Color 171"),
            new PlayerColorDef(172, "_Color 172"),
            new PlayerColorDef(173, "_Color 173"),
            new PlayerColorDef(174, "_Color 174"),
            new PlayerColorDef(175, "_Color 175"),
            new PlayerColorDef(176, "_Color 176"),
            new PlayerColorDef(177, "_Color 177"),
            new PlayerColorDef(178, "_Color 178"),
            new PlayerColorDef(179, "_Color 179"),
            new PlayerColorDef(180, "_Color 180"),
            new PlayerColorDef(181, "_Color 181"),
            new PlayerColorDef(182, "_Color 182"),
            new PlayerColorDef(183, "_Color 183"),
            new PlayerColorDef(184, "_Color 184"),
            new PlayerColorDef(186, "_Color 186"),
            new PlayerColorDef(187, "_Color 187"),
            new PlayerColorDef(188, "_Color 188"),
            new PlayerColorDef(189, "_Color 189"),
            new PlayerColorDef(190, "_Color 190"),
            new PlayerColorDef(191, "_Color 191"),
            new PlayerColorDef(192, "_Color 192"),
            new PlayerColorDef(193, "_Color 193"),
            new PlayerColorDef(194, "_Color 194"),
            new PlayerColorDef(195, "_Color 195"),
            new PlayerColorDef(196, "_Color 196"),
            new PlayerColorDef(197, "_Color 197"),
            new PlayerColorDef(198, "_Color 198"),
            new PlayerColorDef(199, "_Color 199"),
            new PlayerColorDef(200, "_Color 200"),
            new PlayerColorDef(201, "_Color 201"),
            new PlayerColorDef(202, "_Color 202"),
            new PlayerColorDef(203, "_Color 203"),
            new PlayerColorDef(204, "_Color 204"),
            new PlayerColorDef(205, "_Color 205"),
            new PlayerColorDef(206, "_Color 206"),
            new PlayerColorDef(207, "_Color 207"),
            new PlayerColorDef(208, "_Color 208"),
            new PlayerColorDef(209, "_Color 209"),
            new PlayerColorDef(210, "_Color 210"),
            new PlayerColorDef(211, "_Color 211"),
            new PlayerColorDef(212, "_Color 212"),
            new PlayerColorDef(213, "_Color 213"),
            new PlayerColorDef(214, "_Color 214"),
            new PlayerColorDef(215, "_Color 215"),
            new PlayerColorDef(216, "_Color 216"),
            new PlayerColorDef(217, "_Color 217"),
            new PlayerColorDef(218, "_Color 218"),
            new PlayerColorDef(219, "_Color 219"),
            new PlayerColorDef(220, "_Color 220"),
            new PlayerColorDef(221, "_Color 221"),
            new PlayerColorDef(222, "_Color 222"),
            new PlayerColorDef(223, "_Color 223"),
            new PlayerColorDef(224, "_Color 224"),
            new PlayerColorDef(225, "_Color 225"),
            new PlayerColorDef(226, "_Color 226"),
            new PlayerColorDef(227, "_Color 227"),
            new PlayerColorDef(228, "_Color 228"),
            new PlayerColorDef(229, "_Color 229"),
            new PlayerColorDef(230, "_Color 230"),
            new PlayerColorDef(231, "_Color 231"),
            new PlayerColorDef(232, "_Color 232"),
            new PlayerColorDef(233, "_Color 233"),
            new PlayerColorDef(234, "_Color 234"),
            new PlayerColorDef(235, "_Color 235"),
            new PlayerColorDef(236, "_Color 236"),
            new PlayerColorDef(237, "_Color 237"),
            new PlayerColorDef(238, "_Color 238"),
            new PlayerColorDef(239, "_Color 239"),
            new PlayerColorDef(240, "_Color 240"),
            new PlayerColorDef(241, "_Color 241"),
            new PlayerColorDef(242, "_Color 242"),
            new PlayerColorDef(243, "_Color 243"),
            new PlayerColorDef(244, "_Color 244"),
            new PlayerColorDef(245, "_Color 245"),
            new PlayerColorDef(246, "_Color 246"),
            new PlayerColorDef(247, "_Color 247"),
            new PlayerColorDef(248, "_Color 248"),
            new PlayerColorDef(249, "_Color 249"),
            new PlayerColorDef(250, "_Color 250"),
            new PlayerColorDef(251, "_Color 251"),
            new PlayerColorDef(252, "_Color 252"),
            new PlayerColorDef(253, "_Color 253"),
            new PlayerColorDef(254, "_Color 254"),
            new PlayerColorDef(255, "_Color 255"),
        };
    }

    public class PlayerDef : Gettable<PlayerDef>, SaveableItem {

        private int _index;
        private string _name;
        private string _customName;

        public int GroupIndex { get { return _index; } }

        public string GroupName { get { return MainWindow.UseCustomName ? _name : _customName; } }

        public static PlayerDef getByIndex(int index) {
            if (index < AllPlayers.Length) {
                return AllPlayers[index];
            }
            throw new NotImplementedException();
        }

        public static PlayerDef getDefaultValue() {
            return PlayerDef.getByIndex(13);
        }

        public override string ToString() {
            return GroupName.ToString();
        }

        public int getMaxValue() {
            return AllPlayers.Length - 1;
        }

        public int getIndex() {
            return GroupIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<PlayerDef> getter, Action<PlayerDef> setter, Func<PlayerDef> getDefault) {
            return new TriggerDefinitionGeneralDef<PlayerDef>(getter, setter, getDefault, AllPlayers);
        }

        public string ToSaveString() {
            return GroupIndex.ToString();
        }

        internal static void __setPlayerNameDontUseOutsideOfParser(int i, string v) {
            AllPlayers[i]._customName = v;
        }

        private PlayerDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid player";
            }
            _customName = _name;
        }

        public static PlayerDef[] AllPlayers {
            get {
                if (_allPlayers == null) {
                    _allPlayers = new PlayerDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allPlayers[i] = new PlayerDef(i);
                    }
                }
                return _allPlayers;
            }
        }

        private static PlayerDef[] _allPlayers;

        private static readonly string[] _Defs = {
            "Player 1",
            "Player 2",
            "Player 3",
            "Player 4",
            "Player 5",
            "Player 6",
            "Player 7",
            "Player 8",
            "Player 9",
            "Player 10",
            "Player 11",
            "Player 12",
            "Unknown",
            "Current Player",
            "Foes",
            "Allies",
            "Neutral Players",
            "All Players",
            "Force 1",
            "Force 2",
            "Force 3",
            "Force 4",
            "Unused 1",
            "Unused 2",
            "Unused 3",
            "Unused 4",
            "Non Allied Victory Players",
            "unknown/unused"
        };
    }

    public class PortraitIdleAvatarsDef: Gettable<PortraitIdleAvatarsDef>, GettableImage {

        private int _index;
        private string _name;
        private BitmapImageX _image;

        public int PortraitIndex { get { return _index; } }

        public BitmapImageX PortraitImage { get { return _image; } }

        public int getIndex() {
            return PortraitIndex;
        }

        public static PortraitIdleAvatarsDef getByIndex(int index) {
            return AllPortraits[index];
        }

        public int getMaxValue() {
            return AllPortraits.Length;
        }

        public TriggerDefinitionPart getTriggerPart(Func<PortraitIdleAvatarsDef> getter, Action<PortraitIdleAvatarsDef> setter, Func<PortraitIdleAvatarsDef> getDefault) {
            return new TriggerDefinitionGeneralIconsList<PortraitIdleAvatarsDef>(getter, setter, getDefault, AllPortraitImages, 5);
        }

        public override string ToString() {
            return _name;
        }

        public BitmapImageX getImage() {
            return PortraitImage;
        }

        private PortraitIdleAvatarsDef(int index) {
            _index = index;
            _name = PortraitDef.AllProtraits[index].PortraitName;
            if (index < AllPortraits.Length) {
                _image = new BitmapImageX("portraits/portrait_" + index+".png", this);
            } else {
                throw new NotImplementedException();
            }
        }

        public static PortraitIdleAvatarsDef[] AllPortraits {
            get {
                return Application.Current.Dispatcher.Invoke(() => {
                    if (_allPortraits == null) {
                        int idlePortraitsCount = 110;
                        _allPortraits = new PortraitIdleAvatarsDef[idlePortraitsCount];
                        AllPortraitImages = new BitmapImageX[idlePortraitsCount];
                        for (int i = 0; i < idlePortraitsCount; i++) {
                            _allPortraits[i] = new PortraitIdleAvatarsDef(i);
                            AllPortraitImages[i] = _allPortraits[i].PortraitImage;
                        }
                    }
                    return _allPortraits;
                });
            }
        }

        private static PortraitIdleAvatarsDef[] _allPortraits;

        private static BitmapImageX[] AllPortraitImages;

    }

    public class PortraitDef : Gettable<PortraitDef> {


        private int _index;
        private string _name;

        public int PortraitIndex { get { return _index; } }

        public string PortraitName { get { return _name; } }

        public static PortraitDef getByIndex(int index) {
            if (index < AllProtraits.Length) {
                return AllProtraits[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return PortraitName.ToString();
        }

        public int getMaxValue() {
            return AllProtraits.Length - 1;
        }

        public int getIndex() {
            return PortraitIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<PortraitDef> getter, Action<PortraitDef> setter, Func<PortraitDef> getDefault) {
            return new TriggerDefinitionGeneralDef<PortraitDef>(getter, setter, getDefault, AllProtraits);
        }

        private PortraitDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid portrait";
            }
        }

        public static PortraitDef[] AllProtraits {
            get {
                if (_allPortraits == null) {
                    _allPortraits = new PortraitDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allPortraits[i] = new PortraitDef(i);
                    }
                }
                return _allPortraits;
            }
        }

        private static PortraitDef[] _allPortraits;

        private static readonly string[] _Defs = {
            "Marine - Idle Portrait",
            "Ghost - Idle Portrait",
            "Firebat - Idle Portrait ",
            "Vulture - Idle Portrait ",
            "Spider Mine - Idle Portrait",
            "Goliath - Idle Portrait",
            "Seige Tank - Idle Portrait",
            "SCV - Idle Portrait",
            "Wraith - Idle Portrait",
            "Science Vessel - Idle Portrait",
            "Dropship - Idle Portrait",
            "Battlecruiser - Idle Portrait",
            "Sarah Kerrigan - Idle Portrait",
            "Jim Raynor  - Idle Portrait",
            "Edmund Duke - Idle Portrait",
            "Civilian - Idle Portrait",
            "Arcturus Mengsk - Idle Portrait",
            "Terran Advisor - Idle Portrait",
            "Larva - Idle Portrait",
            "Egg - Idle Portrait",
            "Zergling - Idle Portrait",
            "Hydralisk - Idle Portrait",
            "Ultralisk - Idle Portrait",
            "Broodling - Idle Portrait",
            "Drone - Idle Portrait",
            "Overlord - Idle Portrait",
            "Mutalisk - Idle Portrait",
            "Guardian - Idle Portrait",
            "Queen - Idle Portrait",
            "Defiler - Idle Portrait",
            "Scourge - Idle Portrait",
            "Cocoon - Idle Portrait",
            "Mature Chrysalis - Idle Portrait",
            "Infested Terran - Idle Portrait",
            "Zasz - Idle Portrait",
            "Daggoth - Idle Portrait",
            "Infested Kerrigan - Idle Portrait",
            "Hunter Killer - Idle Portrait",
            "Overmind - Idle Portrait",
            "Probe - Idle Portrait",
            "Zealot - Idle Portrait",
            "Dragoon - Idle Portrait",
            "High Templar - Idle Portrait",
            "Archon - Idle Portrait",
            "Shuttle - Idle Portrait",
            "Scout - Idle Portrait",
            "Arbiter - Idle Portrait",
            "Carrier - Idle Portrait",
            "Interceptor - Idle Portrait",
            "Dark Templar - Idle Portrait",
            "Fenix (Zealot) - Idle Portrait",
            "Fenix (Dragoon) - Idle Portrait",
            "Zeratul - Idle Portrait",
            "Tassadar - Idle Portrait",
            "Gantrithor - Idle Portrait",
            "Observer - Idle Portrait",
            "Reaver - Idle Portrait",
            "Scarab - Idle Portrait",
            "Khaydarin Crystal Formation - Idle Portrait",
            "Aldaris - Idle Portrait",
            "Protoss Advisor - Idle Portrait",
            "Merc Biker - Idle Portrait",
            "Rhynadon - Idle Portrait",
            "Bengalaas - Idle Portrait",
            "Ragnasaur - Idle Portrait",
            "Cargo Ship - Idle Portrait",
            "Merc Gunship - Idle Portrait",
            "Raider - Idle Portrait",
            "Grom Biker - Idle Portrait",
            "Space Critter - Idle Portrait",
            "Sally - Idle Portrait",
            "Greedo - Idle Portrait",
            "Boskk - Idle Portrait",
            "Peter - Idle Portrait",
            "Independant Advisor - Idle Portrait",
            "Terran Gas Tank - Idle Portrait",
            "Protoss Gas Orb - Idle Portrait",
            "Zerg Gas Sac - Idle Portrait",
            "Mineral Cluster - Idle Portrait",
            "Data Disc - Idle Portrait",
            "Psi Emitter - Idle Portrait",
            "Khaydarin Crystal - Idle Portrait",
            "Flag (Red) - Idle Portrait (Pl.1)",
            "Flag (Blue) - Idle Portrait (Pl.2)",
            "Flag (Teal) - Idle Portrait (Pl.3)",
            "Flag (Purple) - Idle Portrait (Pl.4)",
            "Flag (Orange) - Idle Portrait (Pl.5)",
            "Flag (Brown) - Idle Portrait (Pl.6)",
            "Flag (White) - Idle Portrait (Pl.7)",
            "Flag (Yellow) - Idle Portrait (Pl.8)",
            "Medic - Idle Portrait",
            "Valkyrie - Idle Portrait",
            "Dugalle - Idle Portrait",
            "Stukov - Idle Portrait",
            "Duran - Idle Portrait",
            "Artanis - Idle Portrait",
            "Raszagal - Idle Portrait",
            "Devourer - Idle Portrait",
            "Lurker - Idle Portrait",
            "Dark Archon - Idle Portrait",
            "Corsair - Idle Portrait",
            "Scantid - Idle Portrait",
            "Kakaru - Idle Portrait",
            "Ursadon - Idle Portrait",
            "Uraj Crystal - Idle Portrait",
            "Khalis Crystal - Idle Portrait",
            "Flag (Green) - Idle Portrait (Pl.9)",
            "Flag (Bright Yellow) - Idle Portrait (Pl.10)",
            "Flag (Tan) - Idle Portrait (Pl.11)",
            "Flag (Blue) - Idle Portrait (Pl.12)",
            "Marine - Talking Portrait",
            "Ghost - Talking Portrait",
            "Firebat - Talking Portrait",
            "Vulture - Talking Portrait",
            "Spider Mine - Talking Portrait",
            "Goliath - Talking Portrait",
            "Seige Tank - Talking Portrait",
            "SCV - Talking Portrait",
            "Wraith - Talking Portrait",
            "Science Vessel - Talking Portrait",
            "Dropship - Talking Portrait",
            "Battlecruiser - Talking Portrait",
            "Sarah Kerrigan - Talking Portrait",
            "Jim Raynor  - Talking Portrait",
            "Edmund Duke - Talking Portrait",
            "Civilian - Talking Portrait",
            "Arcturus Mengsk - Talking Portrait",
            "Terran Advisor - Talking Portrait",
            "Larva - Talking Portrait",
            "Egg - Talking Portrait",
            "Zergling - Talking Portrait",
            "Hydralisk - Talking Portrait",
            "Ultralisk - Talking Portrait",
            "Broodling - Talking Portrait",
            "Drone - Talking Portrait",
            "Overlord - Talking Portrait",
            "Mutalisk - Talking Portrait",
            "Guardian - Talking Portrait",
            "Queen - Talking Portrait",
            "Defiler - Talking Portrait",
            "Scourge - Talking Portrait",
            "Cocoon - Talking Portrait",
            "Mature Chrysalis - Talking Portrait",
            "Infested Terran - Talking Portrait",
            "Zasz - Talking Portrait",
            "Daggoth - Talking Portrait",
            "Infested Kerrigan - Talking Portrait",
            "Hunter Killer - Talking Portrait",
            "Overmind - Talking Portrait",
            "Probe - Talking Portrait",
            "Zealot - Talking Portrait",
            "Dragoon - Talking Portrait",
            "High Templar - Talking Portrait",
            "Archon - Talking Portrait",
            "Shuttle - Talking Portrait",
            "Scout - Talking Portrait",
            "Arbiter - Talking Portrait",
            "Carrier - Talking Portrait",
            "Interceptor - Talking Portrait",
            "Dark Templar - Talking Portrait",
            "Fenix (Zealot) - Talking Portrait",
            "Fenix (Dragoon) - Talking Portrait",
            "Zeratul - Talking Portrait",
            "Tassadar - Talking Portrait",
            "Gantrithor - Talking Portrait",
            "Observer - Talking Portrait",
            "Reaver - Talking Portrait",
            "Scarab - Talking Portrait",
            "Khaydarin Crystal Formation - Talking Portrait",
            "Aldaris - Talking Portrait",
            "Protoss Advisor - Talking Portrait",
            "Merc Biker - Talking Portrait",
            "Rhynadon - Talking Portrait",
            "Bengalaas - Talking Portrait",
            "Ragnasaur - Talking Portrait",
            "Cargo Ship - Talking Portrait",
            "Merc Gunship - Talking Portrait",
            "Raider - Talking Portrait",
            "Grom Biker - Talking Portrait",
            "Space Critter - Talking Portrait",
            "Sally - Talking Portrait",
            "Greedo - Talking Portrait",
            "Boskk - Talking Portrait",
            "Peter - Talking Portrait",
            "Independant Advisor - Talking Portrait",
            "Terran Gas Tank - Talking Portrait",
            "Protoss Gas Orb - Talking Portrait",
            "Zerg Gas Sac - Talking Portrait",
            "Mineral Cluster - Talking Portrait",
            "Data Disc - Talking Portrait",
            "Psi Emitter - Talking Portrait",
            "Khaydarin Crystal - Talking Portrait",
            "Flag (Red) - Talking Portrait (Pl.1)",
            "Flag (Blue) - Talking Portrait (Pl.2)",
            "Flag (Teal) - Talking Portrait (Pl.3)",
            "Flag (Purple) - Talking Portrait (Pl.4)",
            "Flag (Orange) - Talking Portrait (Pl.5)",
            "Flag (Brown) - Talking Portrait (Pl.6)",
            "Flag (White) - Talking Portrait (Pl.7)",
            "Flag (Yellow) - Talking Portrait (Pl.8)",
            "Medic - Talking Portrait",
            "Valkyrie - Talking Portrait",
            "Dugalle - Talking Portrait",
            "Stukov - Talking Portrait",
            "Duran - Talking Portrait",
            "Artanis - Talking Portrait",
            "Raszagal - Talking Portrait",
            "Devourer - Talking Portrait",
            "Lurker - Talking Portrait",
            "Dark Archon - Talking Portrait",
            "Corsair - Talking Portrait",
            "Scantid - Talking Portrait",
            "Kakaru - Talking Portrait",
            "Ursadon - Talking Portrait",
            "Uraj Crystal - Talking Portrait",
            "Khalis Crystal - Talking Portrait",
            "Flag (Green) - Talking Portrait (Pl.9)",
            "Flag (Bright Yellow) - Talking Portrait (Pl.10)",
            "Flag (Tan) - Talking Portrait (Pl.11)",
            "Flag (Blue) - Talking Portrait (Pl.12)",
            "None"
        };


    }

    public class PropertiesDef : Gettable<PropertiesDef>, SaveableItem {

        private int _value;

        public PropertiesDef(int number) {
            _value = number;
        }

        public int getIndex() {
            return _value;
        }

        public override string ToString() {
            return "Properties";
        }

        public int getMaxValue() {
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<PropertiesDef> getter, Action<PropertiesDef> setter, Func<PropertiesDef> getDefault) {
            return new TriggerDefinitionPropertiesDef(getter, setter);
        }

        public string ToSaveString() {
            return _value.ToString();
        }

        internal static SaveableItem getDefaultValue() {
            return new PropertiesDef(0);
        }
    }

    public class Quantifier : SaveableItem, Gettable<Quantifier> {

        private string _value;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return ToString();
        }

        private Quantifier(string value) {
            _value = value;
        }

        public static readonly Quantifier AtMost = new Quantifier("At Most");
        public static readonly Quantifier AtLeast = new Quantifier("At Least");
        public static readonly Quantifier Exactly = new Quantifier("Exactly");

        public static Quantifier getDefaultValue() {
            return Quantifier.AtLeast;
        }

        public int getMaxValue() {
            return AllQuantifieres.Length - 1;
        }

        public int getIndex() {
            if (this == AtMost) {
                return 0;
            } else if (this == AtLeast) {
                return 1;
            } else if (this == Exactly) {
                return 2;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<Quantifier> getter, Action<Quantifier> setter, Func<Quantifier> getDefault) {
            return new TriggerDefinitionGeneralDef<Quantifier>(getter, setter, getDefault, AllQuantifieres);
        }

        public static Quantifier[] AllQuantifieres = new Quantifier[] { AtMost, AtLeast, Exactly };
    }

    public class Resources : SaveableItem, Gettable<Resources> {

        private string _value;
        private string _saveValue;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return _saveValue;
        }

        private Resources(string value, string saveValue) {
            _value = value;
            _saveValue = saveValue;
        }

        public static readonly Resources Ore = new Resources("Minerals", "ore");
        public static readonly Resources Gas = new Resources("Gas", "gas");
        public static readonly Resources OreAndGas = new Resources("Minerals And Gas", "ore and gas");

        public static Resources getDefaultValue() {
            return Resources.Ore;
        }

        public int getMaxValue() {
            return AllResources.Length - 1;
        }

        public int getIndex() {
            if(this == Ore) {
                return 0;
            } else if(this == Gas) {
                return 1;
            } else if(this == OreAndGas) {
                return 2;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<Resources> getter, Action<Resources> setter, Func<Resources> getDefault) {
            return new TriggerDefinitionGeneralDef<Resources>(getter, setter, getDefault, AllResources);
        }

        public static Resources[] AllResources = new Resources[] { Ore, Gas, OreAndGas};

    }

    public class RemappingIndexDef : Gettable<RemappingIndexDef> {

        private int _index;
        private string _name;

        public override string ToString() {
            return _name;
        }

        public int getIndex() {
            return _index;
        }

        public static RemappingIndexDef getByIndex(int index) {
            if(index < AllRemappingIndexes.Length) {
                return AllRemappingIndexes[index];
            }
            throw new NotImplementedException();
        }

        public int getMaxValue() {
            return AllRemappingIndexes.Length-1;
        }

        private RemappingIndexDef(int index, string name) {
            _index = index;
            _name = name;
        }

        public TriggerDefinitionPart getTriggerPart(Func<RemappingIndexDef> getter, Action<RemappingIndexDef> setter, Func<RemappingIndexDef> getDefault) {
            return new TriggerDefinitionGeneralDef<RemappingIndexDef>(getter, setter, getDefault, AllRemappingIndexes);
        }

        public static RemappingIndexDef[] AllRemappingIndexes = new RemappingIndexDef[] {
            new RemappingIndexDef(0, "No Remapping"),
            new RemappingIndexDef(1, "ofire.pcx (Orange)"),
            new RemappingIndexDef(2, "gfire.pcx (Green)"),
            new RemappingIndexDef(3, "bfire.pcx (Blue)"),
            new RemappingIndexDef(4, "bexpl.pcx (Blue 2)"),
            new RemappingIndexDef(5, "Special (Self cloaking)"),
            new RemappingIndexDef(6, "(crash)"),
            new RemappingIndexDef(7, "(crash)"),
            new RemappingIndexDef(8, "Unknown 8"),
            new RemappingIndexDef(9, "Unknown 9")

        };
    }

    public class RightClickAction :Gettable<RightClickAction> {


        private int _index;
        private string _name;

        public int ActionIndex { get { return _index; } }

        public string ActionName { get { return _name; } }

        public static RightClickAction getByIndex(int index) {
            if (index < AllActions.Length) {
                return AllActions[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return ActionName.ToString();
        }

        public int getMaxValue() {
            return AllActions.Length - 1;
        }

        public int getIndex() {
            return ActionIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<RightClickAction> getter, Action<RightClickAction> setter, Func<RightClickAction> getDefault) {
            return new TriggerDefinitionGeneralDef<RightClickAction>(getter, setter, getDefault, AllActions);
        }

        private RightClickAction(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid action";
            }
        }

        public static RightClickAction[] AllActions {
            get {
                if (_allActions == null) {
                    _allActions = new RightClickAction[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allActions[i] = new RightClickAction(i);
                    }
                }
                return _allActions;
            }
        }

        private static RightClickAction[] _allActions;

        private static readonly string[] _Defs = {
            "No commands/Auto Attack",
            "Normal movement/Normal Attack",
            "Normal movement/No Attack",
            "No movement/Normal Attack",
            "Harvest",
            "Harvest & Repair",
            "Nothing (with indicator)",
        };

    }

    public class ScoreBoard : SaveableItem, Gettable<ScoreBoard> {

        private string _value;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return ToString();
        }

        private ScoreBoard(string value) {
            _value = value;
        }

        public static readonly ScoreBoard Buildings = new ScoreBoard("Buildings");
        public static readonly ScoreBoard Custom = new ScoreBoard("Custom");
        public static readonly ScoreBoard Kills = new ScoreBoard("Kills");
        public static readonly ScoreBoard KillsAndRazings = new ScoreBoard("Kills And Razings");
        public static readonly ScoreBoard Razings = new ScoreBoard("Razings");
        public static readonly ScoreBoard Total = new ScoreBoard("Total");
        public static readonly ScoreBoard Units = new ScoreBoard("Units");
        public static readonly ScoreBoard UnitsAndBuildings = new ScoreBoard("Units And Buildings");

        public static ScoreBoard getDefaultValue() {
            return ScoreBoard.Buildings;
        }

        public int getMaxValue() {
            throw new NotImplementedException();
        }

        public int getIndex() {
            for(int i = 0; i < AllScoreBoards.Length; i++) {
                if(AllScoreBoards[i]== this) {
                    return i;
                }
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<ScoreBoard> getter, Action<ScoreBoard> setter, Func<ScoreBoard> getDefault) {
            return new TriggerDefinitionGeneralDef<ScoreBoard>(getter, setter, getDefault, AllScoreBoards);
        }

        public static ScoreBoard[] AllScoreBoards = new ScoreBoard[] { Buildings, Custom, Kills, KillsAndRazings, Razings, Total, Units, UnitsAndBuildings};
    }

    public class SCTechDef {

        public static SCTechDef[] AllTechs {
            get {
                if (_allTechs == null) {
                    _allTechs = new SCTechDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allTechs[i] = new SCTechDef();
                    }
                }
                return _allTechs;
            }
        }

        private static SCTechDef[] _allTechs;

        private static readonly string[] _Defs = {
            "Stim Packs",
            "Lockdown",
            "EMP Shockwave",
            "Spider Mines",
            "Scanner Sweep",
            "Tank Siege Mode",
            "Defensive Matrix",
            "Irradiate",
            "Yamato Gun",
            "Cloaking Field",
            "Personnel Cloaking",
            "Burrowing",
            "Infestation",
            "Spawn Broodlings",
            "Dark Swarm",
            "Plague",
            "Consume",
            "Ensnare",
            "Parasite",
            "Psionic Storm",
            "Hallucination",
            "Recall",
            "Stasis Field",
            "Archon Warp"
        };

    }

    public class SCUpgradeDef {

        public static SCUpgradeDef[] AllUpgrades {
            get {
                if (_allUpgrades == null) {
                    _allUpgrades = new SCUpgradeDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allUpgrades[i] = new SCUpgradeDef();
                    }
                }
                return _allUpgrades;
            }
        }

        private static SCUpgradeDef[] _allUpgrades;

        private static readonly string[] _Defs = {
            "Terran Infantry Armor",
            "Terran Vehicle Plating",
            "Terran Ship Plating",
            "Zerg Caraspace",
            "Zerg Flyer Caraspace",
            "Protoss Armor",
            "Protoss Plating",
            "Terran Infantry Weapons",
            "Terran Vehicle Weapons",
            "Terran Ship Weapons",
            "Zerg Melee Attacks",
            "Zerg Missile Attacks",
            "Zerg Flyer Attacks",
            "Protoss Ground Weapons",
            "Protoss Air Weapons",
            "Protoss Plasma Shields",
            "U-238 Shells",
            "Ion Thrusters",
            "Burst Lasers (unused)",
            "Titan Reactor (SV+50)",
            "Ocular Implantats",
            "Moebius Reactor (Ghost +50)",
            "Apollo Reactor (Wraith +50)",
            "Colossus Reactor (BC +50)",
            "Ventral Sacs",
            "Anennae",
            "Pneumatized Caraspace",
            "Metabolic Boost",
            "Adrenal Glads",
            "Muscular Augments",
            "Grooved Spines",
            "Gamete Meiosis (Queen +50)",
            "Metasynaptic Node (Defiler +50)",
            "Singularity Charge",
            "Leg Enhancements",
            "Scarab Damage",
            "Reaver Capacity",
            "Gravitic Drive",
            "Sensor Array",
            "Gravity Boosters",
            "Khaydarin Amulet (HT +50)",
            "Apial Sensors",
            "Gravitic Thrusters",
            "Carrier Capacity",
            "Khaydarin Core (Arbiter +50)",
            "Unknown upgrade45",
        };
    }

    public class SetQuantifier : SaveableItem, Gettable<SetQuantifier> {

        private string _value;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return ToString();
        }

        private SetQuantifier(string value) {
            _value = value;
        }

        public static readonly SetQuantifier Add = new SetQuantifier("Add");
        public static readonly SetQuantifier Subtract = new SetQuantifier("Subtract");
        public static readonly SetQuantifier SetTo = new SetQuantifier("Set To");

        public static SetQuantifier getDefaultValue() {
            return SetQuantifier.Add;
        }

        public int getMaxValue() {
            return AllQuantifiers.Length - 1;
        }

        public int getIndex() {
            if (this == Add) {
                return 0;
            } else if (this == Subtract) {
                return 1;
            } else if (this == SetTo) {
                return 2;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<SetQuantifier> getter, Action<SetQuantifier> setter, Func<SetQuantifier> getDefault) {
            return new TriggerDefinitionGeneralDef<SetQuantifier>(getter, setter, getDefault, AllQuantifiers);
        }

        public static SetQuantifier[] AllQuantifiers = new SetQuantifier[] { Add, Subtract, SetTo };


    }

    public class StatsDef : Gettable<StatsDef> {

        private int _index;
        private string _name;

        public int StatsIndex { get { return _index; } }

        public string StatsName { get { return _name; } }

        public static StatsDef getByIndex(int index) {
            if (index < AllStats.Length) {
                return AllStats[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return StatsName;
        }

        public int getMaxValue() {
            return AllStats.Length - 1;
        }

        public int getIndex() {
            return StatsIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<StatsDef> getter, Action<StatsDef> setter, Func<StatsDef> getDefault) {
            return new TriggerDefinitionGeneralDef<StatsDef>(getter, setter, getDefault, AllStats);
        }

        private StatsDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid player";
            }
        }

        public static StatsDef[] AllStats {
            get {
                if (_allStats == null) {
                    _allStats = new StatsDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allStats[i] = new StatsDef(i);
                    }
                }
                return _allStats;
            }
        }

        private static StatsDef[] _allStats;

        private static readonly string[] _Defs = {
            "None",
            "Terran Marine<0>*<0>Ground Units<0>",
            "Terran Ghost<0>*<0>Ground Units<0>",
            "Terran Vulture<0>*<0>Ground Units<0>",
            "Terran Goliath<0>*<0>Ground Units<0>",
            "Goliath Turret<0>*<0>*<0>",
            "Terran Siege Tank<0>Tank Mode<0>Ground Units<0>",
            "Tank Turret<0>*<0>*<0>",
            "Terran SCV<0>*<0>Ground Units<0>",
            "Terran Wraith<0>*<0>Air Units<0>",
            "Terran Science Vessel<0>*<0>Air Units<0>",
            "Gui Montag<0>Firebat<0>Heroes<0>",
            "Terran Dropship<0>*<0>Air Units<0>",
            "Terran Battlecruiser<0>*<0>Air Units<0>",
            "Vulture Spider Mine<0>*<0>Special<0>",
            "Nuclear Missile<0>*<0>Special<0>",
            "Terran Civilian<0>*<0>Heroes<0>",
            "Sarah Kerrigan<0>Ghost<0>Heroes<0>",
            "Alan Schezar<0>Goliath<0>Heroes<0>",
            "Alan Turret<0>*<0>*<0>",
            "Jim Raynor<0>Vulture<0>Heroes<0>",
            "Jim Raynor<0>Marine<0>Heroes<0>",
            "Tom Kazansky<0>Wraith<0>Heroes<0>",
            "Magellan<0>Science Vessel<0>Heroes<0>",
            "Edmund Duke<0>Siege Tank<0>Heroes<0>",
            "Duke Turret<0>*<0>*<0>",
            "Edmund Duke<0>Siege Mode<0>Heroes<0>",
            "Duke Turret<0>*<0>*<0>",
            "Arcturus Mengsk<0>Battlecruiser<0>Heroes<0>",
            "Hyperion<0>Battlecruiser<0>Heroes<0>",
            "Norad II<0>Battlecruiser<0>Heroes<0>",
            "Terran Siege Tank<0>Siege Mode<0>Ground Units<0>",
            "Tank Turret<0>*<0>*<0>",
            "Terran Firebat<0>*<0>Ground Units<0>",
            "Scanner Sweep<0>*<0>*<0>",
            "Terran Medic<0>*<0>Ground Units<0>",
            "Zerg Larva<0>*<0>*<0>",
            "Zerg Egg<0>*<0>*<0>",
            "Zerg Zergling<0>*<0>Ground Units<0>",
            "Zerg Hydralisk<0>*<0>Ground Units<0>",
            "Zerg Ultralisk<0>*<0>Ground Units<0>",
            "Zerg Broodling<0>*<0>Ground Units<0>",
            "Zerg Drone<0>*<0>Ground Units<0>",
            "Zerg Overlord<0>*<0>Air Units<0>",
            "Zerg Mutalisk<0>*<0>Air Units<0>",
            "Zerg Guardian<0>*<0>Air Units<0>",
            "Zerg Queen<0>*<0>Air Units<0>",
            "Zerg Defiler<0>*<0>Ground Units<0>",
            "Zerg Scourge<0>*<0>Air Units<0>",
            "Torrasque<0>Ultralisk<0>Heroes<0>",
            "Matriarch<0>Queen<0>Heroes<0>",
            "Infested Terran<0>*<0>Ground Units<0>",
            "Infested Kerrigan<0>Infested Terran<0>Heroes<0>",
            "Unclean One<0>Defiler<0>Heroes<0>",
            "Hunter Killer<0>Hydralisk<0>Heroes<0>",
            "Devouring One<0>Zergling<0>Heroes<0>",
            "Kukulza<0>Mutalisk<0>Heroes<0>",
            "Kukulza<0>Guardian<0>Heroes<0>",
            "Yggdrasill<0>Overlord<0>Heroes<0>",
            "Terran Valkyrie<0>*<0>Air Units<0>",
            "Cocoon<0>*<0>*<0>",
            "Protoss Corsair<0>*<0>Air Units<0>",
            "Protoss Dark Templar<0>*<0>Ground Units<0>",
            "Zerg Devourer<0>*<0>Air Units<0>",
            "Protoss Dark Archon<0>*<0>Ground Units<0>",
            "Protoss Probe<0>*<0>Ground Units<0>",
            "Protoss Zealot<0>*<0>Ground Units<0>",
            "Protoss Dragoon<0>*<0>Ground Units<0>",
            "Protoss High Templar<0>*<0>Ground Units<0>",
            "Protoss Archon<0>*<0>Ground Units<0>",
            "Protoss Shuttle<0>*<0>Air Units<0>",
            "Protoss Scout<0>*<0>Air Units<0>",
            "Protoss Arbiter<0>*<0>Air Units<0>",
            "Protoss Carrier<0>*<0>Air Units<0>",
            "Protoss Interceptor<0>*<0>Air Units<0>",
            "Dark Templar<0>Hero<0>Heroes<0>",
            "Zeratul<0>Dark Templar<0>Heroes<0>",
            "Tassadar/Zeratul<0>Archon<0>Heroes<0>",
            "Fenix<0>Zealot<0>Heroes<0>",
            "Fenix<0>Dragoon<0>Heroes<0>",
            "Tassadar<0>Templar<0>Heroes<0>",
            "Mojo<0>Scout<0>Heroes<0>",
            "Warbringer<0>Reaver<0>Heroes<0>",
            "Gantrithor<0>Carrier<0>Heroes<0>",
            "Protoss Reaver<0>*<0>Ground Units<0>",
            "Protoss Observer<0>*<0>Air Units<0>",
            "Protoss Scarab<0>*<0>Ground Units<0>",
            "Danimoth<0>Arbiter<0>Heroes<0>",
            "Aldaris<0>Templar<0>Heroes<0>",
            "Artanis<0>Scout<0>Heroes<0>",
            "Rhynadon<0>Badlands<0>Critters<0>",
            "Bengalaas<0>Jungle<0>Critters<0>",
            "Unused<0>*<0>*<0>",
            "Unused<0>*<0>*<0>",
            "Scantid<0>Desert<0>Critters<0>",
            "Kakaru<0>Twilight<0>Critters<0>",
            "Ragnasaur<0>Ash World<0>Critters<0>",
            "Ursadon<0>Ice World<0>Critters<0>",
            "Zerg Lurker Egg<0>*<0>*<0>",
            "Raszagal<0>Dark Templar<0>Heroes<0>",
            "Samir Duran<0>Ghost<0>Heroes<0>",
            "Alexei Stukov<0>Ghost<0>Heroes<0>",
            "Map Revealer<0>*<0>Special<0>",
            "Gerard DuGalle<0>Ghost<0>Heroes<0>",
            "Zerg Lurker<0>*<0>Ground Units<0>",
            "Infested Duran<0>*<0>Heroes<0>",
            "Disruption Field<0>*<0>Protoss<0>",
            "Terran Command Center<0>*<0>Buildings<0>",
            "Terran Comsat Station<0>*<0>Addons<0>",
            "Terran Nuclear Silo<0>*<0>Addons<0>",
            "Terran Supply Depot<0>*<0>Buildings<0>",
            "Terran Refinery<0>*<0>Buildings<0>",
            "Terran Barracks<0>*<0>Buildings<0>",
            "Terran Academy<0>*<0>Buildings<0>",
            "Terran Factory<0>*<0>Buildings<0>",
            "Terran Starport<0>*<0>Buildings<0>",
            "Terran Control Tower<0>*<0>Addons<0>",
            "Terran Science Facility<0>*<0>Buildings<0>",
            "Terran Covert Ops<0>*<0>Addons<0>",
            "Terran Physics Lab<0>*<0>Addons<0>",
            "Unused Terran Bldg<0>*<0>*<0>",
            "Terran Machine Shop<0>*<0>Addons<0>",
            "Unused Terran Bldg<0>*<0>Terran<0>",
            "Terran Engineering Bay<0>*<0>Buildings<0>",
            "Terran Armory<0>*<0>Buildings<0>",
            "Terran Missile Turret<0>*<0>Buildings<0>",
            "Terran Bunker<0>*<0>Buildings<0>",
            "Norad II<0>Crashed Battlecruiser<0>Special Buildings<0>",
            "Ion Cannon<0>*<0>Special Buildings<0>",
            "Uraj Crystal<0>*<0>Powerups<0>",
            "Khalis Crystal<0>*<0>Powerups<0>",
            "Infested Command Center<0>*<0>Buildings<0>",
            "Zerg Hatchery<0>*<0>Buildings<0>",
            "Zerg Lair<0>*<0>Buildings<0>",
            "Zerg Hive<0>*<0>Buildings<0>",
            "Zerg Nydus Canal<0>*<0>Buildings<0>",
            "Zerg Hydralisk Den<0>*<0>Buildings<0>",
            "Zerg Defiler Mound<0>*<0>Buildings<0>",
            "Zerg Greater Spire<0>*<0>Buildings<0>",
            "Zerg Queen's Nest<0>*<0>Buildings<0>",
            "Zerg Evolution Chamber<0>*<0>Buildings<0>",
            "Zerg Ultralisk Cavern<0>*<0>Buildings<0>",
            "Zerg Spire<0>*<0>Buildings<0>",
            "Zerg Spawning Pool<0>*<0>Buildings<0>",
            "Zerg Creep Colony<0>*<0>Buildings<0>",
            "Zerg Spore Colony<0>*<0>Buildings<0>",
            "Unused Zerg Bldg<0>*<0>Zerg<0>",
            "Zerg Sunken Colony<0>*<0>Buildings<0>",
            "Zerg Overmind<0>With Shell<0>Special Buildings<0>",
            "Zerg Overmind<0>*<0>Special Buildings<0>",
            "Zerg Extractor<0>*<0>Buildings<0>",
            "Mature Crysalis<0>*<0>Special Buildings<0>",
            "Zerg Cerebrate<0>*<0>Special Buildings<0>",
            "Zerg Cerebrate Daggoth<0>*<0>Special Buildings<0>",
            "Unused Zerg Bldg 5<0>*<0>Zerg<0>",
            "Protoss Nexus<0>*<0>Buildings<0>",
            "Protoss Robotics Facility<0>*<0>Buildings<0>",
            "Protoss Pylon<0>*<0>Buildings<0>",
            "Protoss Assimilator<0>*<0>Buildings<0>",
            "Protoss Unused<0>*<0>Protoss<0>",
            "Protoss Observatory<0>*<0>Buildings<0>",
            "Protoss Gateway<0>*<0>Buildings<0>",
            "Protoss Unused<0>*<0>Protoss<0>",
            "Protoss Photon Cannon<0>*<0>Buildings<0>",
            "Protoss Citadel of Adun<0>*<0>Buildings<0>",
            "Protoss Cybernetics Core<0>*<0>Buildings<0>",
            "Protoss Templar Archives<0>*<0>Buildings<0>",
            "Protoss Forge<0>*<0>Buildings<0>",
            "Protoss Stargate<0>*<0>Buildings<0>",
            "Stasis Cell/Prison<0>*<0>Special Buildings<0>",
            "Protoss Fleet Beacon<0>*<0>Buildings<0>",
            "Protoss Arbiter Tribunal<0>*<0>Buildings<0>",
            "Protoss Robotics Support Bay<0>*<0>Buildings<0>",
            "Protoss Shield Battery<0>*<0>Buildings<0>",
            "Khaydarin Crystal Formation<0>*<0>Special Buildings<0>",
            "Protoss Temple<0>*<0>Special Buildings<0>",
            "Xel'Naga Temple<0>*<0>Special Buildings<0>",
            "Mineral Field<0>Type 1<0>Resources<0>",
            "Mineral Field<0>Type 2<0>Resources<0>",
            "Mineral Field<0>Type 3<0>Resources<0>",
            "Cave<0>*<0>Neutral<0>",
            "Cave-in<0>*<0>Neutral<0>",
            "Cantina<0>*<0>Neutral<0>",
            "Mining Platform<0>*<0>Neutral<0>",
            "Independent Command Center<0>*<0>Independent<0>",
            "Independent Starport<0>*<0>Independent<0>",
            "Jump Gate<0>*<0>Neutral<0>",
            "Ruins<0>*<0>Neutral<0>",
            "Kyadarin Crystal Formation<0>*<0>Neutral<0>",
            "Vespene Geyser<0>*<0>Resources<0>",
            "Warp Gate<0>*<0>Special Buildings<0>",
            "Psi Disrupter<0>*<0>Special Buildings<0>",
            "Zerg Marker<0>*<0>Special<0>",
            "Terran Marker<0>*<0>Special<0>",
            "Protoss Marker<0>*<0>Special<0>",
            "Zerg Beacon<0>*<0>Special<0>",
            "Terran Beacon<0>*<0>Special<0>",
            "Protoss Beacon<0>*<0>Special<0>",
            "Zerg Flag Beacon<0>*<0>Special<0>",
            "Terran Flag Beacon<0>*<0>Special<0>",
            "Protoss Flag Beacon<0>*<0>Special<0>",
            "Power Generator<0>*<0>Special Buildings<0>",
            "Overmind Cocoon<0>*<0>Special Buildings<0>",
            "Dark Swarm<0>*<0>Zerg<0>",
            "Floor Missile Trap<0>*<0>Doodads<0>",
            "Floor Hatch (UNUSED)<0>*<0>Doodads<0>",
            "Left Upper Level Door<0>*<0>Doodads<0>",
            "Right Upper Level Door<0>*<0>Doodads<0>",
            "Left Pit Door<0>*<0>Doodads<0>",
            "Right Pit Door<0>*<0>Doodads<0>",
            "Floor Gun Trap<0>*<0>Doodads<0>",
            "Left Wall Missile Trap<0>*<0>Doodads<0>",
            "Left Wall Flame Trap<0>*<0>Doodads<0>",
            "Right Wall Missile Trap<0>*<0>Doodads<0>",
            "Right Wall Flame Trap<0>*<0>Doodads<0>",
            "Start Location<0>*<0>Start Location<0>",
            "Flag<0>*<0>Special<0>",
            "Young Chrysalis<0>*<0>Powerups<0>",
            "Psi Emitter<0>*<0>Powerups<0>",
            "Data Disc<0>*<0>Powerups<0>",
            "Khaydarin Crystal<0>*<0>Powerups<0>",
            "Mineral Chunk<0>Type 1<0>Resources<0>",
            "Mineral Chunk<0>Type 2<0>Resources<0>",
            "Vespene Orb<0>Protoss Type 1<0>Resources<0>",
            "Vespene Orb<0>Protoss Type 2<0>Resources<0>",
            "Vespene Sac<0>Zerg Type 1<0>Resources<0>",
            "Vespene Sac<0>Zerg Type 2<0>Resources<0>",
            "Vespene Tank<0>Terran Type 1<0>Resources<0>",
            "Vespene Tank<0>Terran Type 2<0>Resources<0>",
            "Gauss Rifle<0>",
            "Gauss Rifle<0>",
            "C-10 Canister Rifle<0>",
            "C-10 Canister Rifle<0>",
            "Fragmentation Grenade<0>",
            "Fragmentation Grenade<0>",
            "Twin Autocannons<0>",
            "Hellfire Missile Pack<0>",
            "Twin Autocannons<0>",
            "Hellfire Missile Pack<0>",
            "Arclite Cannon<0>",
            "Arclite Cannon<0>",
            "Fusion Cutter<0>",
            "Fusion Cutter<0>",
            "Gemini Missiles<0>",
            "Burst Lasers<0>",
            "Gemini Missiles<0>",
            "Burst Lasers<0>",
            "ATS Laser Battery<0>",
            "ATA Laser Battery<0>",
            "ATS Laser Battery<0>",
            "ATA Laser Battery<0>",
            "ATS Laser Battery<0>",
            "ATA Laser Battery<0>",
            "Flame Thrower<0>",
            "Flame Thrower<0>",
            "Arclite Shock Cannon<0>",
            "Arclite Shock Cannon<0>",
            "Longbolt Missile<0>",
            "Nuclear Strike<0>",
            "EMP Shockwave<0>",
            "Claws<0>",
            "Claws<0>",
            "Claws<0>",
            "Needle Spines<0>",
            "Needle Spines<0>",
            "Kaiser Blades<0>",
            "Kaiser Blades<0>",
            "Toxic Spores<0>",
            "Spines<0>",
            "Acid Spray<0>",
            "Acid Spray<0>",
            "Acid Spore<0>",
            "Acid Spore<0>",
            "Glave Wurm<0>",
            "Glave Wurm<0>",
            "Venom<0>",
            "Venom<0>",
            "Suicide<0>",
            "Seeker Spores<0>",
            "Subterranean Tentacle<0>",
            "Suicide<0>",
            "Particle Beam<0>",
            "Psi Blades<0>",
            "Psi Blades<0>",
            "Warp Blades<0>",
            "Warp Blades<0>",
            "Phase Disruptor<0>",
            "Phase Disruptor<0>",
            "Psi Assault<0>",
            "Psi Assault<0>",
            "Psionic Shockwave<0>",
            "Psionic Shockwave<0>",
            "Unused<0>",
            "Dual Photon Blasters<0>",
            "Anti-matter Missiles<0>",
            "Dual Photon Blasters<0>",
            "Anti-matter Missiles<0>",
            "Phase Disruptor Cannon<0>",
            "Phase Disruptor Cannon<0>",
            "Pulse Cannon<0>",
            "STS Photon Cannon<0>",
            "STA Photon Cannon<0>",
            "Scarab<0>",
            "Missiles<0>",
            "Laser Battery<0>",
            "Tormentor Missiles<0>",
            "Bombs<0>",
            "Raider Gun<0>",
            "Flechette Grenade<0>",
            "Twin Autocannons<0>",
            "Hellfire Missile Pack<0>",
            "Flame Thrower<0>",
            "Undefined Weapon Name<0>",
            "Stim Packs<0>",
            "Lockdown<0>",
            "EMP Shockwave<0>",
            "Spider Mines<0>",
            "Scanner Sweep<0>",
            "Tank Siege Mode<0>",
            "Defensive Matrix<0>",
            "Irradiate<0>",
            "Yamato Gun<0>",
            "Cloaking Field<0>",
            "Personnel Cloaking<0>",
            "t<4>Research S<3>t<1>im Pack Tech<10>(Marine and Firebat ability)<0>",
            "l<4>Research <3>L<1>ockdown<10>(Ghost ability)<0>",
            "e<4>Research <3>E<1>MP Shockwave<10>(Science Vessel ability)<0>",
            "m<4>Research Spider <3>M<1>ines<10>(Vulture ability)<0>",
            "s<4>Research <3>S<1>iege Tech<10>(Siege Tank ability)<0>",
            "m<4>Research Defensive <3>M<1>atrix<10>(Science Vessel ability)<0>",
            "i<4>Research <3>I<1>rradiate<10>(Science Vessel ability)<0>",
            "y<4>Research <3>Y<1>amato Gun<10>(Battlecruiser ability)<0>",
            "c<4>Research <3>C<1>loaking Field<10>(Wraith ability)<0>",
            "c<4>Research Personnel <3>C<1>loaking<10>(Ghost ability)<0>",
            "t<3>Use S<3>t<1>im Packs<0>",
            "l<3><3>L<1>ockdown<0>",
            "i<3>Use Spider M<3>i<1>nes<0>",
            "s<3><3>S<1>canner Sweep (Detector)<0>",
            "o<3>Siege M<3>o<1>de<0>",
            "o<0>Tank M<3>o<1>de<0>",
            "d<3>Activate <3>D<1>efensive Matrix<0>",
            "e<3>Activate <3>E<1>MP Shockwave<0>",
            "i<3><3>I<1>rradiate<0>",
            "y<3><3>Y<1>amato Gun<0>",
            "c<3><3>C<1>loak<0>",
            "c<0>De<3>c<1>loak<0>",
            "Stim Packs:<10> Research at Academy<0>",
            "Lockdown:<10> Research at Covert Ops<0>",
            "Spider Mines:<10> Research at Machine Shop<0>",
            "Siege Mode:<10> Research at Machine Shop<0>",
            "Defensive Matrix:<10> Research at Science Facility<0>",
            "EMP Shockwave:<10> Research at Science Facility<0>",
            "Irradiate:<10> Research at Science Facility<0>",
            "Yamato Gun:<10> Research at Physics Lab<0>",
            "Cloaking Field:<10> Research at Control Tower<0>",
            "Personnel Cloaking:<10> Research at Covert Ops<0>",
            "Burrowing<0>",
            "Infestation<0>",
            "Spawn Broodling<0>",
            "Dark Swarm<0>",
            "Parasite<0>",
            "Plague<0>",
            "Consume<0>",
            "Ensnare<0>",
            "b<4>Evolve <3>B<1>urrow<10>(Ground unit ability)<0>",
            "i<4>Evolve <3>I<1>nfestation<10>(Queen ability)<0>",
            "b<4>Evolve Spawn <3>B<1>roodling<10>(Queen ability)<0>",
            "s<4>Evolve Dark <3>S<1>warm<10>(Defiler ability)<0>",
            "r<4>Evolve Pa<3>r<1>asite<10>(Queen ability)<0>",
            "g<4>Evolve Pla<3>g<1>ue<10>(Defiler ability)<0>",
            "e<4>Evolve <3>E<1>nsnare<10>(Queen ability)<0>",
            "c<4>Evolve <3>C<1>onsume<10>(Defiler ability)<0>",
            "u<3>B<3>u<1>rrow<0>",
            "u<0><3>U<1>nburrow<0>",
            "i<0><3>I<1>nfest Terran Command Center<0>",
            "b<3>Spawn <3>B<1>roodlings<0>",
            "w<3>Dark S<3>w<1>arm<0>",
            "r<3>Pa<3>r<1>asite<0>",
            "g<3>Pla<3>g<1>ue<0>",
            "c<3><3>C<1>onsume<0>",
            "u<3>Cons<3>u<1>me<0>",
            "e<3><3>E<1>nsnare<0>",
            "Burrow:<10> Evolve at Hatchery<0>",
            "Infest:<10> Evolve at Queen's Nest<0>",
            "Spawn Broodlings:<10> Evolve at Queen's Nest<0>",
            "Dark Swarm:<10> Evolve at Defiler Mound<0>",
            "Parasite:<10> Evolve at Queen's Nest<0>",
            "Plague:<10> Evolve at Defiler Mound<0>",
            "Consume:<10> Evolve at Defiler Mound<0>",
            "Ensnare:<10> Evolve at Queen's Nest<0>",
            "Psionic Storm<0>",
            "Hallucination<0>",
            "Recall<0>",
            "Stasis Field<0>",
            "Archon Warp<0>",
            "p<4>Develop <3>P<1>sionic Storm<10>(Templar ability)<0>",
            "h<4>Develop <3>H<1>allucination<10>(Templar ability)<0>",
            "r<4>Develop <3>R<1>ecall<10>(Arbiter ability)<0>",
            "s<4>Develop <3>S<1>tasis Field<10>(Arbiter ability)<0>",
            "a<4>Develop <3>A<1>rchon Warp<10>(Templar ability)<0>",
            "t<3>Psionic S<3>t<1>orm<0>",
            "l<3>Ha<3>l<1>lucination<0>",
            "r<3><3>R<1>ecall<0>",
            "t<3>S<3>t<1>asis Field<0>",
            "r<1>A<3>r<1>chon Warp<0>",
            "Psionic Storm:<10> Develop at Templar Archives<0>",
            "Hallucination:<10> Develop at Templar Archives<0>",
            "Recall:<10> Develop at Arbiter Tribunal<0>",
            "Stasis Field:<10> Develop at Arbiter Tribunal<0>",
            "Archon Warp:<10> Research at Templar Archives<0>",
            "Archon Warp:<10> Select 2 or more Templars<0>",
            "Terran Infantry Armor<0>",
            "Terran Vehicle Plating<0>",
            "Terran Ship Plating<0>",
            "Zerg Carapace<0>",
            "Zerg Flyer Carapace<0>",
            "Protoss Armor<0>",
            "Protoss Plating<0>",
            "Terran Infantry Weapons<0>",
            "Terran Vehicle Weapons<0>",
            "Terran Ship Weapons<0>",
            "Zerg Melee Attacks<0>",
            "Zerg Missile Attacks<0>",
            "Zerg Flyer Attacks<0>",
            "Protoss Ground Weapons<0>",
            "Protoss Air Weapons<0>",
            "Protoss Plasma Shields<0>",
            "U-238 Shells<0>",
            "Ion Thrusters<0>",
            "Burst Lasers<0>",
            "Titan Reactor<0>",
            "Ocular Implants<0>",
            "Moebius Reactor<0>",
            "Apollo Reactor<0>",
            "Colossus Reactor<0>",
            "Ventral Sacs<0>",
            "Antennae<0>",
            "Pneumatized Carapace<0>",
            "Metabolic Boost<0>",
            "Adrenal Glands<0>",
            "Muscular Augments<0>",
            "Grooved Spines<0>",
            "Gamete Meiosis<0>",
            "Metasynaptic Node<0>",
            "Singularity Charge<0>",
            "Leg Enhancements<0>",
            "Scarab Damage<0>",
            "Reaver Capacity<0>",
            "Gravitic Drive<0>",
            "Sensor Array<0>",
            "Gravitic Boosters<0>",
            "Khaydarin Amulet<0>",
            "Apial Sensors<0>",
            "Gravitic Thrusters<0>",
            "Carrier Capacity<0>",
            "Khaydarin Core<0>",
            "a<2>Upgrade Infantry <3>A<1>rmor<0>",
            "p<2>Upgrade Vehicle <3>P<1>lating<0>",
            "h<2>Upgrade S<3>h<1>ip Plating<0>",
            "c<2>Evolve <3>C<1>arapace<0>",
            "c<2>Evolve Flyer <3>C<1>arapace<0>",
            "a<2>Upgrade Ground <3>A<1>rmor<0>",
            "a<2>Upgrade Air <3>A<1>rmor<0>",
            "w<2>Upgrade Infantry <3>W<1>eapons<0>",
            "w<2>Upgrade Vehicle <3>W<1>eapons<0>",
            "s<2>Upgrade <3>S<1>hip Weapons<0>",
            "m<2>Upgrade <3>M<1>elee Attacks<0>",
            "a<2>Upgrade Missile <3>A<1>ttacks<0>",
            "a<2>Upgrade Flyer <3>A<1>ttacks<0>",
            "w<2>Upgrade Ground <3>W<1>eapons<0>",
            "w<2>Upgrade Air <3>W<1>eapons<0>",
            "s<2>Upgrade Plasma <3>S<1>hields<0>",
            "u<2>Research <3>U<1>-238 Shells<10>(Increase Marine attack range)<0>",
            "i<2>Research <3>I<1>on Thrusters<10>(Faster Vulture movement)<0>",
            "b<2>Research <3>B<1>urst Lasers<10>(Wraith Weapon)<0>",
            "t<2>Research <3>T<1>itan Reactor<10>(+50 Science Vessel energy)<0>",
            "o<2>Research <3>O<1>cular Implants<10>(Increase Ghost sight range)<0>",
            "m<2>Research <3>M<1>oebius Reactor<10>(+50 Ghost energy)<0>",
            "a<2>Research <3>A<1>pollo Reactor<10>(+50 Wraith energy)<0>",
            "c<2>Research <3>C<1>olossus Reactor<10>(+50 Battlecruiser energy)<0>",
            "v<2>Evolve <3>V<1>entral Sacs<10>(Transporting for Overlord)<0>",
            "a<2>Evolve <3>A<1>ntennae<10>(Increase Overlord sight range)<0>",
            "p<2>Evolve <3>P<1>neumatized Carapace<10>(Faster Overlord movement)<0>",
            "m<2>Evolve <3>M<1>etabolic Boost<10>(Faster Zergling movement)<0>",
            "a<2>Evolve <3>A<1>drenal Glands<10>(Faster Zergling Attack)<0>",
            "m<2>Evolve <3>M<1>uscular Augments<10>(Faster Hydralisk movement)<0>",
            "g<2>Evolve <3>G<1>rooved Spines<10>(Increase Hydralisk attack range)<0>",
            "g<2>Evolve <3>G<1>amete Meiosis<10>(+50 Queen energy)<0>",
            "m<2>Evolve <3>M<1>etasynaptic Node<10>(+50 Defiler energy)<0>",
            "s<2>Develop <3>S<1>ingularity Charge<10>(Increase Dragoon attack range)<0>",
            "l<2>Develop <3>L<1>eg Enhancements<10>(Faster Zealot movement)<0>",
            "s<2>Upgrade <3>S<1>carab Damage<0>",
            "c<2>Increase Reaver <3>C<1>apacity<10>(+5 Max Scarabs)<0>",
            "g<2>Develop <3>G<1>ravitic Drive<10>(Faster Shuttle movement)<0>",
            "s<2>Develop <3>S<1>ensor Array<10>(Increase Observer sight range)<0>",
            "g<2>Develop <3>G<1>ravitic Booster<10>(Faster Observer movement)<0>",
            "k<2>Develop <3>K<1>haydarin Amulet<10>(+50 Templar energy)<0>",
            "a<2>Develop <3>A<1>pial Sensors<10>(Increase Scout sight range)<0>",
            "g<2>Develop <3>G<1>ravitic Thrusters<10>(Faster Scout movement)<0>",
            "c<2>Increase <3>C<1>arrier Capacity<10>(+4 Max Interceptors)<0>",
            "k<2>Develop <3>K<1>haydarin Core<10>(+50 Arbiter energy)<0>",
            "Infantry Armor L1 Require:<10> Nothing<0>",
            "Infantry Armor L2 Require:<10> Science Facility<0>",
            "Infantry Armor L3 Require:<10> Science Facility<0>",
            "Infantry Weapons L1 Require:<10> Nothing<0>",
            "Infantry Weapons L2 Require:<10> Science Facility<0>",
            "Infantry Weapons L3 Require:<10> Science Facility<0>",
            "Vehicle Weapons L1 Require:<10> Nothing<0>",
            "Vehicle Weapons L2 Require:<10> Science Facility<0>",
            "Vehicle Weapons L3 Require:<10> Science Facility<0>",
            "Vehicle Plating L1 Requires:<10> Nothing<0>",
            "Vehicle Plating L2 Requires:<10> Science Facility<0>",
            "Vehicle Plating L3 Requires:<10> Science Facility<0>",
            "Ship Weapons L1 Require:<10> Nothing<0>",
            "Ship Weapons L2 Require:<10> Science Facility<0>",
            "Ship Weapons L3 Require:<10> Science Facility<0>",
            "Ship Plating L1 Requires:<10> Nothing<0>",
            "Ship Plating L2 Requires:<10> Science Facility<0>",
            "Ship Plating L3 Requires:<10> Science Facility<0>",
            "Melee Attacks L1 Require:<10> Nothing<0>",
            "Melee Attacks L2 Require:<10> Lair<0>",
            "Melee Attacks L3 Require:<10> Hive<0>",
            "Missile Attacks L1 Require:<10> Nothing<0>",
            "Missile Attacks L2 Require:<10> Lair<0>",
            "Missile Attacks L3 Require:<10> Hive<0>",
            "Carapace L1 Requires:<10> Nothing<0>",
            "Carapace L2 Requires:<10> Lair<0>",
            "Carapace L3 Requires:<10> Hive<0>",
            "Flyer Attacks L1 Require:<10> Nothing<0>",
            "Flyer Attacks L2 Require:<10> Lair<0>",
            "Flyer Attacks L3 Require:<10> Hive<0>",
            "Flyer Carapace L1 Requires:<10> Nothing<0>",
            "Flyer Carapace L2 Requires:<10> Lair<0>",
            "Flyer Carapace L3 Requires:<10> Hive<0>",
            "Adrenal Glands Require:<10> Hive<0>",
            "Ground Weapons L1 Require:<10> Nothing<0>",
            "Ground Weapons L2 Require:<10> Templar Archives<0>",
            "Ground Weapons L3 Require:<10> Templar Archives<0>",
            "Air Weapons L1 Require:<10> Nothing<0>",
            "Air Weapons L2 Require:<10> Fleet Beacon<0>",
            "Air Weapons L3 Require:<10> Fleet Beacon<0>",
            "Ground Armor L1 Requires:<10> Nothing<0>",
            "Ground Armor L2 Requires:<10> Templar Archives<0>",
            "Ground Armor L3 Requires:<10> Templar Archives<0>",
            "Air Armor L1 Requires:<10> Nothing<0>",
            "Air Armor L2 Requires:<10> Fleet Beacon<0>",
            "Air Armor L3 Requires:<10> Fleet Beacon<0>",
            "Shields L1 Require:<10> Nothing<0>",
            "Shields L2 Require:<10> Cybernetics Core<0>",
            "Shields L3 Require:<10> Cybernetics Core<0>",
            "Recruit<0>",
            "Private<0>",
            "Private<0>",
            "Corporal<0>",
            "Specialist<0>",
            "Sergeant<0>",
            "First Sergeant<0>",
            "Master Sergeant<0>",
            "Warrant Officer<0>",
            "Captain<0>",
            "Major<0>",
            "Lt Commander<0>",
            "First Sergeant<0>",
            "Sergeant Major<0>",
            "Colonel<0>",
            "Commodore<0>",
            "Marshall<0>",
            "Lieutenant<0>",
            "General<0>",
            "Captain Raynor<0>",
            "General Duke<0>",
            "Admiral<0>",
            "Tassadar<0>",
            "Interceptors<0>",
            "ESC<0>",
            "z<1>Morph to <3>Z<1>erglings<0>",
            "h<1>Morph to <3>H<1>ydralisk<0>",
            "u<1>Morph to <3>U<1>ltralisk<0>",
            "d<1>Morph to <3>D<1>rone<0>",
            "o<1>Morph to <3>O<1>verlord<0>",
            "m<1>Morph to <3>M<1>utalisk<0>",
            "g<5><3>G<1>uardian Aspect<0>",
            "q<1>Morph to <3>Q<1>ueen<0>",
            "f<1>Morph to De<3>f<1>iler<0>",
            "s<1>Morph to <3>S<1>courge<0>",
            "i<1>Train <3>I<1>nfested Terran<0>",
            "m<1>Train <3>M<1>arine<0>",
            "g<1>Train <3>G<1>host<0>",
            "f<1>Train <3>F<1>irebat<0>",
            "v<1>Build <3>V<1>ulture<0>",
            "g<1>Build <3>G<1>oliath<0>",
            "t<1>Build Siege <3>T<1>ank<0>",
            "s<1>Build <3>S<1>CV<0>",
            "w<1>Build <3>W<1>raith<0>",
            "v<1>Build Science <3>V<1>essel<0>",
            "d<1>Build <3>D<1>ropship<0>",
            "b<1>Build <3>B<1>attlecruiser<0>",
            "n<1>Arm <3>N<1>uclear Silo<0>",
            "o<1>Build <3>O<1>bserver<0>",
            "p<1>Build <3>P<1>robe<0>",
            "z<1>Warp in <3>Z<1>ealot<0>",
            "d<1>Warp in <3>D<1>ragoon<0>",
            "t<1>Warp in High <3>T<1>emplar<0>",
            "s<1>Build <3>S<1>huttle<0>",
            "s<1>Warp in <3>S<1>cout<0>",
            "a<1>Warp in <3>A<1>rbiter<0>",
            "c<1>Warp in <3>C<1>arrier<0>",
            "i<1>Build <3>I<1>nterceptor<0>",
            "v<1>Build Rea<3>v<1>er<0>",
            "r<1>Build Sca<3>r<1>ab<0>",
            "b<1>Hire Merc <3>B<1>iker<0>",
            "g<1>Hire Merc <3>G<1>unship<0>",
            "r<1>Hire <3>R<1>aider<0>",
            "h<1>Mutate into <3>H<1>atchery<0>",
            "c<1>Mutate into <3>C<1>reep Colony<0>",
            "e<1>Mutate into <3>E<1>xtractor<0>",
            "s<1>Mutate into <3>S<1>pawning Pool<0>",
            "v<1>Mutate into E<3>v<1>olution Chamber<0>",
            "d<1>Mutate into Hydralisk <3>D<1>en<0>",
            "n<1>Mutate into <3>N<1>ydus Canal<0>",
            "s<1>Mutate into <3>S<1>pire<0>",
            "q<1>Mutate into <3>Q<1>ueen's Nest<0>",
            "u<1>Mutate into <3>U<1>ltralisk Cavern<0>",
            "d<1>Mutate into <3>D<1>efiler Mound<0>",
            "l<1>Mutate into <3>L<1>air<0>",
            "h<1>Mutate into <3>H<1>ive<0>",
            "g<1>Mutate into <3>G<1>reater Spire<0>",
            "s<1>Mutate into <3>S<1>pore Colony<0>",
            "u<1>Mutate into S<3>u<1>nken Colony<0>",
            "n<0>Place <3>N<1>ydus Canal Exit<0>",
            "n<1>Warp in <3>N<1>exus<0>",
            "p<1>Warp in <3>P<1>ylon<0>",
            "a<1>Warp in <3>A<1>ssimilator<0>",
            "g<1>Warp in <3>G<1>ateway<0>",
            "f<1>Warp in <3>F<1>orge<0>",
            "c<1>Warp in Photon <3>C<1>annon<0>",
            "y<1>Warp in C<3>y<1>bernetics Core<0>",
            "b<1>Warp in Shield <3>B<1>attery<0>",
            "r<1>Warp in <3>R<1>obotics Facility<0>",
            "o<1>Warp in <3>O<1>bservatory<0>",
            "c<1>Warp in <3>C<1>itadel of Adun<0>",
            "t<1>Warp in <3>T<1>emplar Archives<0>",
            "s<1>Warp in <3>S<1>targate<0>",
            "f<1>Warp in <3>F<1>leet Beacon<0>",
            "a<1>Warp in <3>A<1>rbiter Tribunal<0>",
            "b<1>Warp in Robotics Support <3>B<1>ay<0>",
            "c<1>Build <3>C<1>ommand Center<0>",
            "s<1>Build <3>S<1>upply Depot<0>",
            "r<1>Build <3>R<1>efinery<0>",
            "b<1>Build <3>B<1>arracks<0>",
            "e<1>Build <3>E<1>ngineering Bay<0>",
            "t<1>Build Missile <3>T<1>urret<0>",
            "a<1>Build <3>A<1>cademy<0>",
            "u<1>Build B<3>u<1>nker<0>",
            "f<1>Build <3>F<1>actory<0>",
            "s<1>Build <3>S<1>tarport<0>",
            "i<1>Build Sc<3>i<1>ence Facility<0>",
            "a<1>Build <3>A<1>rmory<0>",
            "c<1>Build <3>C<1>omsat Station<0>",
            "n<1>Build <3>N<1>uclear Silo<0>",
            "c<1>Build <3>C<1>ontrol Tower<0>",
            "c<1>Build <3>C<1>overt Ops<0>",
            "p<1>Build <3>P<1>hysics Lab<0>",
            "c<1>Build Ma<3>c<1>hine Shop<0>",
            "m<0><3>M<1>ove<0>",
            "s<0><3>S<1>top<0>",
            "a<0><3>A<1>ttack<0>",
            "p<0><3>P<1>atrol<0>",
            "h<0><3>H<1>old Position<0>",
            "w<0><3>W<1>ay Points<0>",
            "l<0><3>L<1>and<0>",
            "l<0><3>L<1>iftoff<0>",
            "r<0>Set <3>R<1>ally Point<0>",
            "r<0><3>R<1>echarge Shields<0>",
            "s<0><3>S<1>elect Larva<0>",
            "g<0><3>G<1>ather<0>",
            "c<0>Return <3>C<1>argo<0>",
            "r<0><3>R<1>epair<0>",
            "b<0><3>B<1>uild Structure<0>",
            "v<0>Build Ad<3>v<1>anced Structure<0>",
            "b<0><3>B<1>asic Mutation<0>",
            "v<0>Ad<3>v<1>anced Mutation<0>",
            "v<0>Ad<3>v<1>anced Morph<0>",
            "l<0><3>L<1>oad<0>",
            "u<0><3>U<1>nload All<0>",
            "n<0><3>N<1>uclear Strike<0>",
            "p<0><3>P<1>lace COP<0>",
            "<27><0><3>ESC<1> - Cancel<0>",
            "<27><0><3>ESC<1> - Cancel<0>",
            "<27><0><3>ESC<1> - Cancel Construction<0>",
            "<27><0><3>ESC<1> - Cancel Unit Training<0>",
            "<27><0><3>ESC<1> - Cancel Upgrade<0>",
            "<27><0><3>ESC<1> - Cancel Research<0>",
            "<27><0><3>ESC<1> - Cancel Last<0>",
            "<27><0><3>ESC<1> - Cancel Addon<0>",
            "<27><0><3>ESC<1> - Cancel Morph<0>",
            "<27><0><3>ESC<1> - Cancel Mutation<0>",
            "<27><0><3>ESC<1> - Cancel Infestation<0>",
            "<27><0><3>ESC<1> - Cancel Mutation<0>",
            "<27><0><3>ESC<1> - Cancel Nuclear Strike<0>",
            "<27><0><3>ESC<1> - Halt Construction<0>",
            "Ghost Requires:<10> Academy<10> Science Facility with<10> attached Covert Ops<0>",
            "Firebat Requires:<10> Academy<0>",
            "Goliath Requires:<10> Armory<0>",
            "Siege Tank Requires:<10> Attached Machine Shop<0>",
            "Science Vessel Requires:<10> Attached Control Tower<10> Science Facility<0>",
            "Dropship Requires:<10> Attached Control Tower<0>",
            "Battlecruiser Requires:<10> Attached Control Tower<10> Science Facility with<10> attached Physics Lab<0>",
            "Comsat Station Requires:<10> Academy<0>",
            "Nuclear Silo Requires:<10> Science Facility with<10> attached Covert Ops<0>",
            "Barracks Requires:<10> Command Center<0>",
            "Academy Requires:<10> Barracks<0>",
            "Factory Requires:<10> Barracks<0>",
            "Starport Requires:<10> Factory<0>",
            "Science Facility Requires:<10> Starport<0>",
            "Engineering Bay Requires:<10> Command Center<0>",
            "Armory Requires:<10> Factory<0>",
            "Missile Turret Requires:<10> Engineering Bay<0>",
            "Bunker Requires:<10> Barracks<0>",
            "Dragoon Requires:<10> Cybernetics Core<0>",
            "Templar Requires:<10> Templar Archives<0>",
            "Arbiter Requires:<10> Arbiter Tribunal<0>",
            "Carrier Requires:<10> Fleet Beacon<0>",
            "Reaver Requires:<10> Robotics Support Bay<0>",
            "Observer Requires:<10> Observatory<0>",
            "Gateway Requires:<10> Nexus<0>",
            "Forge Requires:<10> Nexus<0>",
            "Photon Cannon Requires:<10> Forge<0>",
            "Shield Battery Requires:<10> Gateway<0>",
            "Cybernetics Core Requires:<10> Gateway<0>",
            "Robotics Facility Requires:<10> Cybernetics Core<0>",
            "Stargate Requires:<10> Cybernetics Core<0>",
            "Citadel of Adun Requires:<10> Cybernetics Core<0>",
            "Observatory Requires:<10> Robotics Facility<0>",
            "Robotics Support Bay Requires:<10> Robotics Facility<0>",
            "Fleet Beacon Requires:<10> Stargate<0>",
            "Templar Archives Require:<10> Citadel of Adun<0>",
            "Arbiter Tribunal Requires:<10> Stargate<10> Templar Archives<0>",
            "Zerglings Require:<10> Spawning Pool<0>",
            "Hydralisk Requires:<10> Hydralisk Den<0>",
            "Ultralisk Requires:<10> Ultralisk Cavern<0>",
            "Mutalisk Requires:<10> Spire<0>",
            "Guardian Aspect Requires:<10> Greater Spire<0>",
            "Queen Requires:<10> Queen's Nest<0>",
            "Defiler Requires:<10> Defiler Mound<0>",
            "Scourge Require:<10> Spire<0>",
            "Lair Requires:<10> Spawning Pool<0>",
            "Hive Requires:<10> Queen's Nest<0>",
            "Nydus Canal Requires:<10> Hive<0>",
            "Hydralisk Den Requires:<10> Spawning Pool<0>",
            "Spire Requires:<10> Lair<0>",
            "Greater Spire Requires:<10> Hive<0>",
            "Queen's Nest Requires:<10> Lair<0>",
            "Evolution Chamber Requires:<10> Hatchery<0>",
            "Ultralisk Cavern Requires:<10> Hive<0>",
            "Defiler Mound Requires:<10> Hive<0>",
            "Spawning Pool Requires:<10> Hatchery<0>",
            "Spore Colony Requires:<10> Evolution Chamber<0>",
            "Sunken Colony Requires:<10> Spawning Pool<0>",
            "Carrier Attack:<10> Build Interceptors<0>",
            "Reaver Attack:<10> Build Scarabs<0>",
            "Nuclear Strike Requires:<10> Armed Nuclear Silo<0>",
            "Not Implemented<0>",
            "mk<0>",
            "Kills:<0>",
            "Evolving<0>",
            "Upgrading<0>",
            "Upgrading<0>",
            "Evolving<0>",
            "Researching<0>",
            "Developing<0>",
            "Morphing<0>",
            "Building<0>",
            "Opening Warp Gate<0>",
            "Morphing<0>",
            "Building<0>",
            "Building<0>",
            "Damage:<0>",
            "Armor:<0>",
            "Shields:<0>",
            "Mining Delay:<0>",
            "Production <0>",
            "Unit Ptr:<0>",
            "Current Order:<0>",
            "Next Order:<0>",
            "% Complete<0>",
            "Order:<0>",
            "Mutating<0>",
            "Adding On<0>",
            "Opening Warp Rift<0>",
            "Summoning<0>",
            "Interceptors<0>",
            "Scarabs<0>",
            "Nukes<0>",
            "Spider Mines<0>",
            "Next Level:<0>",
            "Minerals:<0>",
            "Vespene Gas:<0>",
            "Depleted<0>",
            "Mutating<0>",
            "Under Construction<0>",
            "Opening Warp Rift<0>",
            "Cancel Morph (%s)<0>",
            "Cancel Construction (%s)<0>",
            "Cancel Warp (%s)<0>",
            "Cancel Upgrade<0>",
            "Cancel Research<0>",
            "Cancel Morph<0>",
            "Unload Unit (%s)<0>",
            "Click: Select Unit<10>Shift+Click: Deselect Unit<10>Ctrl+Click: Select Unit Type<0>",
            "Show Terrain in Minimap (<3>Tab<1>)<0>",
            "Hide Terrain in Minimap (<3>Tab<1>)<0>",
            "Diplomacy<0>",
            "Messaging<0>",
            "Game Menu (<3>F10<1>)<0>",
            "Control Provided:<0>",
            "Supplies Provided:<0>",
            "Psi Provided:<0>",
            "Total Control:<0>",
            "Total Supplies:<0>",
            "Total Psi:<0>",
            "Control Used:<0>",
            "Supplies Used:<0>",
            "Psi Used:<0>",
            "Control Max:<0>",
            "Supplies Max:<0>",
            "Psi Max:<0>",
            "<7>Parasite Detected<0>",
            "<6>Disabled<0>",
            "<6>Disabled<0>",
            "<6>Unpowered<0>",
            "<3>Detector<0>",
            "<4>Hallucination<0>",
            "Units<0>",
            "Units<0>",
            "Resources<0>",
            "Kills<0>",
            "Score<0>",
            "Units to go<0>",
            "Units to go<0>",
            "Resources to go<0>",
            "Kills to go<0>",
            "Score to go<0>",
            "Nowhere to return to...can't return.<0>",
            "Too many underlings...create more Overlords.<0>",
            "Not enough supplies...build more Supply Depots.<0>",
            "Not enough psi...build more Pylons.<0>",
            "Underling limit exceeded.<0>",
            "Supply limit exceeded.<0>",
            "Psi limit exceeded.<0>",
            "Not enough minerals...mine more minerals.<0>",
            "Not enough Vespene gases....harvest more gas.<0>",
            "You can't build next to minerals or geysers.<0>",
            "An undetected unit is in the way.<0>",
            "You can't build off the map.<0>",
            "You can't build near the edge of the map.<0>",
            "You can't build there.<0>",
            "You must explore there first.<0>",
            "You must currently be able to see the location.<0>",
            "Could not build there, find a Vespene Geyser to build on.<0>",
            "You must build on the Creep.<0>",
            "You must build near a Pylon.<0>",
            "Couldn't reach the building site.<0>",
            "You can't land there.<0>",
            "Not enough energy.<0>",
            "Not enough energy.<0>",
            "Not enough energy.<0>",
            "Nothing to harvest. Find a Mine or a Vespene Geyser.<0>",
            "Nothing to harvest. Find a Mine, or build a Refinery at a Vespene Geyser.<0>",
            "Nothing to harvest. Find a Mine, or build an Assimilator at a Vespene Geyser.<0>",
            "Must target severely damaged Terran Command Center.<0>",
            "Unit's waypoint list is full.<0>",
            "Unable to add order.<0>",
            "Running low on orders, your last order was not processed.<0>",
            "Not enough life remaining.<0>",
            "Vespene Geyser depleted.<0>",
            "Invalid target.<0>",
            "Unable to target structure.<0>",
            "Must target units.<0>",
            "Unable to attack target.<0>",
            "Invalid target.<0>",
            "Must target mechanical units.<0>",
            "Invalid target.<0>",
            "Must target damaged mechanical units or damaged complete buildings.<0>",
            "Must target Terran units.<0>",
            "Invalid target.<0>",
            "Must target non-robotic ground units.<0>",
            "Must target ground.<0>",
            "Target out of range.<0>",
            "Target is too close.<0>",
            "Can only pick up your own transportable units.<0>",
            "Not enough room for unit.<0>",
            "Must gather from a Mineral Field or Vespene Geyser.<0>",
            "Morph an Extractor there first.<0>",
            "Build a Refinery there first.<0>",
            "Build an Assimilator there first.<0>",
            "Must gather gases from your own geyser.<0>",
            "Invalid target.<0>",
            "Invalid target.<0>",
            "Invalid target.<0>",
            "Units in stasis can't be targeted.<0>",
            "Must target non-hovering ground units.<0>",
            "Must target passable terrain.<0>",
            "Die<0>",
            "Fizzle<0>",
            "Stop<0>",
            "Guard<0>",
            "Player Guard<0>",
            "Turret Guard<0>",
            "Bunker Guard<0>",
            "Ignore<0>",
            "Carrier Ignore<0>",
            "Carrier Stop<0>",
            "Reaver Stop<0>",
            "Attack<0>",
            "Attack Unit<0>",
            "Attack Fixed Range<0>",
            "Move Attack Unit<0>",
            "Attack Tile<0>",
            "Hover<0>",
            "Attack Move<0>",
            "Atk Move EP<0>",
            "Harass Move<0>",
            "AI Patrol<0>",
            "Tower<0>",
            "Vulture Mine<0>",
            "Carrier Attack<0>",
            "Carrier Attack Move<0>",
            "Stay In Range<0>",
            "Turret Attack<0>",
            "Nothing<0>",
            "Drone Start Build<0>",
            "Drone Build<0>",
            "Drone Attack Unit<0>",
            "Infest Mine<0>",
            "Build<0>",
            "Build Protoss<0>",
            "Pylon Build<0>",
            "Construct Building<0>",
            "Repair<0>",
            "Place Add-On<0>",
            "Build Add-On<0>",
            "Train<0>",
            "Zerg Birth<0>",
            "Morph<0>",
            "Zerg Building Morph<0>",
            "Build Self<0>",
            "Zerg Build Self<0>",
            "Enter Nydus Canal<0>",
            "Protoss Build Self<0>",
            "Follow<0>",
            "Carrier<0>",
            "Carrier Fight<0>",
            "Reaver<0>",
            "Reaver Attack<0>",
            "Reaver Fight<0>",
            "Reaver Hold<0>",
            "Train Fighter<0>",
            "Strafe Unit<0>",
            "Scarab<0>",
            "Return<0>",
            "Drone Land<0>",
            "Building Land<0>",
            "Building Lift Off<0>",
            "Drone Lift Off<0>",
            "Lift Off<0>",
            "Reasearch Tech<0>",
            "Upgrade<0>",
            "Larva<0>",
            "Spawning Larva<0>",
            "Harvest<0>",
            "Harvest Gas<0>",
            "Return Gas<0>",
            "Harvest Minerals<0>",
            "Return Minerals<0>",
            "Enter Transport<0>",
            "Pick Up<0>",
            "Powerup<0>",
            "Siege Mode<0>",
            "Tank Mode<0>",
            "Watch Target<0>",
            "Initing Creep Growth<0>",
            "Stopping Creep Growth<0>",
            "Spread Creep<0>",
            "Guardian Aspect<0>",
            "Warping Archon<0>",
            "Completing Archon Summon<0>",
            "Hold Position<0>",
            "Cloak<0>",
            "Decloak<0>",
            "Unload<0>",
            "Move Unload<0>",
            "Fire Yamato Gun<0>",
            "Magna Pulse<0>",
            "Burrow<0>",
            "Burrowed<0>",
            "Unburrow<0>",
            "Cast Parasite<0>",
            "Summon Broodlings<0>",
            "EMP Shockwave<0>",
            "Lockdown<0>",
            "Nuke Wait<0>",
            "Nuke Train<0>",
            "Nuke Launch<0>",
            "Nuke Paint<0>",
            "Nuke Unit<0>",
            "Nuke Ground<0>",
            "Nuke Track<0>",
            "Initializing Arbiter<0>",
            "Cloaking nearby units<0>",
            "Place Mine<0>",
            "Right Click Action<0>",
            "Left Click Action<0>",
            "Sap Unit<0>",
            "Sap Location<0>",
            "Teleport<0>",
            "Teleport to Location<0>",
            "Place Scanner<0>",
            "Scanner<0>",
            "Defensive Matrix<0>",
            "Reset Collision<0>",
            "Reset Collision<0>",
            "Patrol<0>",
            "Computer AI<0>",
            "Guard Post<0>",
            "Rescue Passive<0>",
            "Neutral<0>",
            "Computer Return<0>",
            "Initing Psi Provider<0>",
            "Self Destructing<0>",
            "Decaying creep<0>",
            "Recharge Shields<0>",
            "Shield Battery<0>",
            "Rally Point<0>",
            "CTF COP Init<0>",
            "CTF COP<0>",
            "Critter<0>",
            "Hidden Gun<0>",
            "Open Door<0>",
            "Close Door<0>",
            "Hide Trap<0>",
            "Reveal Trap<0>",
            "Enable Doodad<0>",
            "Disable Doodad<0>",
            "Warp In<0>",
            "Hide and Suicide<0>",
            "Fatal<0>",
            "Nuclear launch detected.<0>",
            "Terran Custom Level<0>",
            "Terran Campaign Easy<0>",
            "Terran Campaign Medium<0>",
            "Terran Campaign Difficult<0>",
            "Terran Campaign Area Town<0>",
            "Terran 3 - Zerg Town<0>",
            "Terran 5 - Terran Main Town<0>",
            "Terran 5 - Terran Harvest Town<0>",
            "Terran 6 - Air Attack Zerg<0>",
            "Terran 6 - Ground Attack Zerg<0>",
            "Terran 6 - Zerg Support Town<0>",
            "Terran 7 - Bottom Zerg Town<0>",
            "Terran 7 - Right Zerg Town<0>",
            "Terran 7 - Middle Zerg Town<0>",
            "Terran 8 - Confederate Town<0>",
            "Terran 9 - Light Attack<0>",
            "Terran 9 - Heavy Attack<0>",
            "Terran 10 - Confederate Towns<0>",
            "Terran 11 - Zerg Town<0>",
            "Terran 11 - Lower Protoss Town<0>",
            "Terran 11 - Upper Protoss Town<0>",
            "Terran 12 - Nuke Town<0>",
            "Terran 12 - Phoenix Town<0>",
            "Terran 12 - Tank Town<0>",
            "Protoss Custom Level<0>",
            "Protoss Campaign Easy<0>",
            "Protoss Campaign Medium<0>",
            "Protoss Campaign Difficult<0>",
            "Protoss Campaign Area Town<0>",
            "Protoss 1 - Zerg Town<0>",
            "Protoss 2 - Zerg Town<0>",
            "Protoss 3 - Unused<0>",
            "Protoss 3 - Air Zerg Town<0>",
            "Protoss 3 - Ground Zerg Town<0>",
            "Protoss 4 - Zerg Town<0>",
            "Protoss 4 - Zerg Town (unused?)<0>",
            "Protoss 5 - Zerg Town Island<0>",
            "Protoss 5 - Zerg Town Base<0>",
            "Protoss 6 - incomplete<0>",
            "Protoss 7 - Left Protoss Town<0>",
            "Protoss 7 - Right Protoss Town<0>",
            "Protoss 7 - Shrine Protoss<0>",
            "Protoss 8 - Left Protoss Town<0>",
            "Protoss 8 - Right Protoss Town<0>",
            "Protoss 8 - Protoss Defenders<0>",
            "Protoss 9 - Ground Zerg<0>",
            "Protoss 9 - Air Zerg<0>",
            "Protoss 9 - Spell Zerg<0>",
            "Protoss 10 - Mini-Towns<0>",
            "Protoss 10 - Mini-Town Master<0>",
            "Protoss 10 - Overmind Defenders<0>",
            "Zerg Custom Level<0>",
            "Zerg Campaign Easy<0>",
            "Zerg Campaign Medium<0>",
            "Zerg Campaign Difficult<0>",
            "Zerg Campaign Area Town<0>",
            "Zerg 1 - Terran Town<0>",
            "Zerg 2 - Protoss Town<0>",
            "Zerg 3 - Terran Town<0>",
            "Zerg 4 - Right Terran Town<0>",
            "Zerg 4 - Lower Terran Town<0>",
            "Zerg 5 - incomplete<0>",
            "Zerg 6 - Protoss Town<0>",
            "Zerg 7 - Ground Town<0>",
            "Zerg 7 - Air Town<0>",
            "Zerg 7 - Support Town<0>",
            "Zerg 8 - Scout Town<0>",
            "Zerg 8 - Templar Town<0>",
            "Zerg 9 - Teal Protoss<0>",
            "Zerg 9 - Left Yellow Protoss<0>",
            "Zerg 9 - Right Yellow Protoss<0>",
            "Zerg 9 - Left Orange Protoss<0>",
            "Zerg 9 - Right Orange Protoss<0>",
            "Zerg 10 - Left Teal (Attack)<0>",
            "Zerg 10 - Right Teal (Support)<0>",
            "Zerg 10 - Left Yellow (Support)<0>",
            "Zerg 10 - Right Yellow (Attack)<0>",
            "Zerg 10 - Red Protoss<0>",
            "Set a Default Staging Area<0>",
            "Send All Units on Strategic Suicide Missions<0>",
            "Send All Units on Random Suicide Missions<0>",
            "Set Player to Enemy<0>",
            "Set Player to Ally<0>",
            "Move Dark Templars to Region<0>",
            "Switch Computer Player to Rescue Passive<0>",
            "Enter Closest Bunker<0>",
            "Value This Area Higher<0>",
            "Clear Previous Combat Data<0>",
            "Debug Script 1 (general)<0>",
            "Debug Script 2 (general)<0>",
            "Debug Script 3 (general)<0>",
            "Debug Script 4 (general)<0>",
            "Debug Script 5 (general)<0>",
            "Debug Script 1 (location)<0>",
            "Debug Script 2 (location)<0>",
            "Debug Script 3 (location)<0>",
            "Debug Script 4 (location)<0>",
            "Debug Script 5 (location)<0>",
            "Structure<0>",
            "Structure Wall<0>",
            "Cliff<0>",
            "High Dirt<0>",
            "Grass<0>",
            "High Grass<0>",
            "Dirt<0>",
            "Rocky Ground<0>",
            "Asphalt<0>",
            "Water<0>",
            "Coastal Cliff<0>",
            "Jungle<0>",
            "High Jungle<0>",
            "Ruins<0>",
            "High Ruins<0>",
            "Temple Wall<0>",
            "High Temple Wall<0>",
            "Dark Platform<0>",
            "High Plating<0>",
            "Plating<0>",
            "Platform<0>",
            "Platform Wall<0>",
            "Low Platform<0>",
            "Low Platform Wall<0>",
            "Rusty Pit Wall<0>",
            "Rusty Pit<0>",
            "Solar Array<0>",
            "Dirt<0>",
            "High Dirt<0>",
            "Shale<0>",
            "Cliff<0>",
            "Floor<0>",
            "Wall<0>",
            "Substructure<0>",
            "Substructure Wall<0>",
            "Plating<0>",
            "Bridges<0>",
            "Bridges<0>",
            "Elevated Catwalk Ramps<0>",
            "Bridges<0>",
            "Tar<0>",
            "Tar Cliff<0>",
            "Dirt<0>",
            "Dried Mud<0>",
            "Sand Dunes<0>",
            "Rocky Ground<0>",
            "Crags<0>",
            "Sandy Sunken Pit<0>",
            "Compound<0>",
            "High Dirt<0>",
            "High Sand Dunes<0>",
            "High Crags<0>",
            "High Sandy Sunken Pit<0>",
            "High Compound<0>",
            "Water<0>",
            "Snow<0>",
            "Moguls<0>",
            "Dirt<0>",
            "Rocky Snow<0>",
            "Grass<0>",
            "Ice<0>",
            "Outpost<0>",
            "High Snow<0>",
            "High Dirt<0>",
            "High Grass<0>",
            "High Ice<0>",
            "High Outpost<0>",
            "Water<0>",
            "Dirt<0>",
            "Mud<0>",
            "Crushed Rock<0>",
            "Crevices<0>",
            "Flagstones<0>",
            "Sunken Ground<0>",
            "Basilica<0>",
            "High Dirt<0>",
            "High Crushed Rock<0>",
            "High Flagstones<0>",
            "High Sunken Ground<0>",
            "High Basilica<0>",
            "Terran Campaign Insane<0>",
            "Protoss Campaign Insane<0>",
            "Zerg Campaign Insane<0>",
            "Terran 1 - Electronic Distribution<0>",
            "Terran 2 - Electronic Distribution<0>",
            "Terran 3 - Electronic Distribution<0>",
            "Terran 1 - Shareware<0>",
            "Terran 2 - Shareware<0>",
            "Terran 3 - Shareware<0>",
            "Terran 4 - Shareware<0>",
            "Terran 5 - Shareware<0>",
            "Halo Rockets<0>",
            "Corrosive Acid<0>",
            "Subterranean Spines<0>",
            "Neutron Flare<0>",
            "Mind Control<0>",
            "Healing<0>",
            "Restoration<0>",
            "Optical Flare<0>",
            "r<4>Research <3>R<1>estoration<10>(Medic ability)<0>",
            "f<4>Research Optical <3>F<1>lare<10>(Medic ability)<0>",
            "a<3>He<3>a<1>l<0>",
            "r<3><3>R<1>estoration<0>",
            "f<3>Optical <3>F<1>lare<0>",
            "Restoration:<10> Research at Academy<0>",
            "Optical Flare:<10> Research at Academy<0>",
            "Lurker Aspect<0>",
            "l<4>Evolve <3>L<1>urker Aspect<10>(Hydralisk ability)<0>",
            "Lurker Aspect:<10> Requires Lair<0>",
            "Disruption Web<0>",
            "Mind Control<0>",
            "Dark Archon Meld<0>",
            "Feedback<0>",
            "Maelstrom <0>",
            "d<4>Develop <3>D<1>isruption Web<10>(Corsair ability)<0>",
            "m<4>Develop <3>M<1>ind Control<10>(Dark Archon ability)<0>",
            "f<4>Develop <3>F<1>eedback<10>(Dark Archon ability)<0>",
            "e<4>Develop Ma<3>e<1>lstrom<10>(Dark Archon ability)<0>",
            "r<1>Da<3>r<1>k Archon Meld<0>",
            "d<3><3>D<1>isruption Web<0>",
            "c<3>Mind <3>C<1>ontrol<0>",
            "f<3><3>F<1>eedback<0>",
            "e<3>Ma<3>e<1>lstrom<0>",
            "Disruption Web:<10> Develop at Fleet Beacon<0>",
            "Mind Control:<10> Research at Templar Archives<0>",
            "Dark Archon Meld:<10> Research at Templar Archives<0>",
            "Dark Archon Warp:<10> Select 2 or more Dark Templars<0>",
            "Feedback:<10> No requirement<0>",
            "Maelstrom:<10> Develop at Templar Archives<0>",
            "Caduceus Reactor<0>",
            "Charon Booster<0>",
            "Anabolic Synthesis<0>",
            "Chitinous Plating<0>",
            "Argus Jewel<0>",
            "Argus Talisman<0>",
            "d<2>Research Ca<3>d<1>uceus Reactor<10>(+50 Medic energy)<0>",
            "c<2>Research <3>C<1>haron Boosters<10>(Increased Goliath Missile Range)<0>",
            "a<2>Evolve <3>A<1>nabolic Synthesis<10>(Faster Ultralisk Movement)<0>",
            "c<2>Evolve <3>C<1>hitinous Plating<10>(Improved Ultralisk Armor)<0>",
            "j<2>Develop Argus <3>J<1>ewel<10>(+50 Corsair energy)<0>",
            "t<2>Develop Argus <3>T<1>alisman<10>(+50 Dark Archon energy)<0>",
            "Research Charon Booster:<10> Requires Armory<0>",
            "d<5><3>D<1>evourer Aspect<0>",
            "l<1>Morph to <3>L<1>urker<0>",
            "c<1>Train Medi<3>c<1><0>",
            "y<1>Build Valk<3>y<1>rie<0>",
            "o<1>Warp in C<3>o<1>rsair<0>",
            "k<1>Warp in Dar<3>k<1> Templar<0>",
            "Medic Requires:<10> Academy<0>",
            "Valkyrie Requires:<10> Attached Control Tower<10> Armory<0>",
            "Dark Templar Requires:<10> Templar Archives<0>",
            "Devourer Aspect Requires:<10> Greater Spire<0>",
            "Lurker Aspect:<10> Evolve at Hydralisk Den<0>",
            "<7>Blind<0>",
            "<7>Acid Spores<0>",
            "per rocket<0>",
            "Recruit<0>",
            "Private<0>",
            "Private<0>",
            "1st Lieutenant<0>",
            "Corporal<0>",
            "Specialist<0>",
            "Sergeant<0>",
            "First Sergeant<0>",
            "Master Sergeant<0>",
            "Warrant Officer<0>",
            "Captain<0>",
            "Major<0>",
            "First Sergeant<0>",
            "Sergeant Major<0>",
            "Colonel<0>",
            "Commodore<0>",
            "Lt Commander<0>",
            "Marshall<0>",
            "Lieutenant<0>",
            "General<0>",
            "Captain Raynor<0>",
            "General Duke<0>",
            "Admiral<0>",
            "Must target injured non-mechanical ground units<0>",
            "Invalid target.<0>",
            "Must target enemy units<0>",
            "Must target non-mechanical units.<0>",
            "Unable to target structure.<0>",
            "Must target units with energy.<0>",
            "Medic<0>",
            "Medic Heal<0>",
            "Restoration<0>",
            "Cast Disruption Web<0>",
            "Cast Mind Control<0>",
            "Warping Dark Archon<0>",
            "Cast Feedback<0>",
            "Cast Optical Flare<0>",
            "Cast Shockwave<0>",
            "Heal Move<0>",
            "Medic Hold Position<0>",
            "Junk Yard Dog<0>",
            "Terran Expansion Custom Level<0>",
            "Protoss Expansion Custom Level<0>",
            "Zerg Expansion Custom Level<0>",
            "Brood Wars Protoss 1 - Town A<0>",
            "Brood Wars Protoss 1 - Town B<0>",
            "Brood Wars Protoss 1 - Town C<0>",
            "Brood Wars Protoss 1 - Town D<0>",
            "Brood Wars Protoss 1 - Town E<0>",
            "Brood Wars Protoss 1 - Town F<0>",
            "Brood Wars Protoss 2 - Town A<0>",
            "Brood Wars Protoss 2 - Town B<0>",
            "Brood Wars Protoss 2 - Town C<0>",
            "Brood Wars Protoss 2 - Town D<0>",
            "Brood Wars Protoss 2 - Town E<0>",
            "Brood Wars Protoss 2 - Town F<0>",
            "Brood Wars Protoss 3 - Town A<0>",
            "Brood Wars Protoss 3 - Town B<0>",
            "Brood Wars Protoss 3 - Town C<0>",
            "Brood Wars Protoss 3 - Town D<0>",
            "Brood Wars Protoss 3 - Town E<0>",
            "Brood Wars Protoss 3 - Town F<0>",
            "Brood Wars Protoss 4 - Town A<0>",
            "Brood Wars Protoss 4 - Town B<0>",
            "Brood Wars Protoss 4 - Town C<0>",
            "Brood Wars Protoss 4 - Town D<0>",
            "Brood Wars Protoss 4 - Town E<0>",
            "Brood Wars Protoss 4 - Town F<0>",
            "Brood Wars Protoss 5 - Town A<0>",
            "Brood Wars Protoss 5 - Town B<0>",
            "Brood Wars Protoss 5 - Town C<0>",
            "Brood Wars Protoss 5 - Town D<0>",
            "Brood Wars Protoss 5 - Town E<0>",
            "Brood Wars Protoss 5 - Town F<0>",
            "Brood Wars Protoss 6 - Town A<0>",
            "Brood Wars Protoss 6 - Town B<0>",
            "Brood Wars Protoss 6 - Town C<0>",
            "Brood Wars Protoss 6 - Town D<0>",
            "Brood Wars Protoss 6 - Town E<0>",
            "Brood Wars Protoss 6 - Town F<0>",
            "Brood Wars Protoss 7 - Town A<0>",
            "Brood Wars Protoss 7 - Town B<0>",
            "Brood Wars Protoss 7 - Town C<0>",
            "Brood Wars Protoss 7 - Town D<0>",
            "Brood Wars Protoss 7 - Town E<0>",
            "Brood Wars Protoss 7 - Town F<0>",
            "Brood Wars Protoss 8 - Town A<0>",
            "Brood Wars Protoss 8 - Town B<0>",
            "Brood Wars Protoss 8 - Town C<0>",
            "Brood Wars Protoss 8 - Town D<0>",
            "Brood Wars Protoss 8 - Town E<0>",
            "Brood Wars Protoss 8 - Town F<0>",
            "Brood Wars Terran 1 - Town A<0>",
            "Brood Wars Terran 1 - Town B<0>",
            "Brood Wars Terran 1 - Town C<0>",
            "Brood Wars Terran 1 - Town D<0>",
            "Brood Wars Terran 1 - Town E<0>",
            "Brood Wars Terran 1 - Town F<0>",
            "Brood Wars Terran 2 - Town A<0>",
            "Brood Wars Terran 2 - Town B<0>",
            "Brood Wars Terran 2 - Town C<0>",
            "Brood Wars Terran 2 - Town D<0>",
            "Brood Wars Terran 2 - Town E<0>",
            "Brood Wars Terran 2 - Town F<0>",
            "Brood Wars Terran 3 - Town A<0>",
            "Brood Wars Terran 3 - Town B<0>",
            "Brood Wars Terran 3 - Town C<0>",
            "Brood Wars Terran 3 - Town D<0>",
            "Brood Wars Terran 3 - Town E<0>",
            "Brood Wars Terran 3 - Town F<0>",
            "Brood Wars Terran 4 - Town A<0>",
            "Brood Wars Terran 4 - Town B<0>",
            "Brood Wars Terran 4 - Town C<0>",
            "Brood Wars Terran 4 - Town D<0>",
            "Brood Wars Terran 4 - Town E<0>",
            "Brood Wars Terran 4 - Town F<0>",
            "Brood Wars Terran 5 - Town A<0>",
            "Brood Wars Terran 5 - Town B<0>",
            "Brood Wars Terran 5 - Town C<0>",
            "Brood Wars Terran 5 - Town D<0>",
            "Brood Wars Terran 5 - Town E<0>",
            "Brood Wars Terran 5 - Town F<0>",
            "Brood Wars Terran 6 - Town A<0>",
            "Brood Wars Terran 6 - Town B<0>",
            "Brood Wars Terran 6 - Town C<0>",
            "Brood Wars Terran 6 - Town D<0>",
            "Brood Wars Terran 6 - Town E<0>",
            "Brood Wars Terran 6 - Town F<0>",
            "Brood Wars Terran 7 - Town A<0>",
            "Brood Wars Terran 7 - Town B<0>",
            "Brood Wars Terran 7 - Town C<0>",
            "Brood Wars Terran 7 - Town D<0>",
            "Brood Wars Terran 7 - Town E<0>",
            "Brood Wars Terran 7 - Town F<0>",
            "Brood Wars Terran 8 - Town A<0>",
            "Brood Wars Terran 8 - Town B<0>",
            "Brood Wars Terran 8 - Town C<0>",
            "Brood Wars Terran 8 - Town D<0>",
            "Brood Wars Terran 8 - Town E<0>",
            "Brood Wars Terran 8 - Town F<0>",
            "Brood Wars Zerg 1 - Town A<0>",
            "Brood Wars Zerg 1 - Town B<0>",
            "Brood Wars Zerg 1 - Town C<0>",
            "Brood Wars Zerg 1 - Town D<0>",
            "Brood Wars Zerg 1 - Town E<0>",
            "Brood Wars Zerg 1 - Town F<0>",
            "Brood Wars Zerg 2 - Town A<0>",
            "Brood Wars Zerg 2 - Town B<0>",
            "Brood Wars Zerg 2 - Town C<0>",
            "Brood Wars Zerg 2 - Town D<0>",
            "Brood Wars Zerg 2 - Town E<0>",
            "Brood Wars Zerg 2 - Town F<0>",
            "Brood Wars Zerg 3 - Town A<0>",
            "Brood Wars Zerg 3 - Town B<0>",
            "Brood Wars Zerg 3 - Town C<0>",
            "Brood Wars Zerg 3 - Town D<0>",
            "Brood Wars Zerg 3 - Town E<0>",
            "Brood Wars Zerg 3 - Town F<0>",
            "Brood Wars Zerg 4 - Town A<0>",
            "Brood Wars Zerg 4 - Town B<0>",
            "Brood Wars Zerg 4 - Town C<0>",
            "Brood Wars Zerg 4 - Town D<0>",
            "Brood Wars Zerg 4 - Town E<0>",
            "Brood Wars Zerg 4 - Town F<0>",
            "Brood Wars Zerg 5 - Town A<0>",
            "Brood Wars Zerg 5 - Town B<0>",
            "Brood Wars Zerg 5 - Town C<0>",
            "Brood Wars Zerg 5 - Town D<0>",
            "Brood Wars Zerg 5 - Town E<0>",
            "Brood Wars Zerg 5 - Town F<0>",
            "Brood Wars Zerg 6 - Town A<0>",
            "Brood Wars Zerg 6 - Town B<0>",
            "Brood Wars Zerg 6 - Town C<0>",
            "Brood Wars Zerg 6 - Town D<0>",
            "Brood Wars Zerg 6 - Town E<0>",
            "Brood Wars Zerg 6 - Town F<0>",
            "Brood Wars Zerg 7 - Town A<0>",
            "Brood Wars Zerg 7 - Town B<0>",
            "Brood Wars Zerg 7 - Town C<0>",
            "Brood Wars Zerg 7 - Town D<0>",
            "Brood Wars Zerg 7 - Town E<0>",
            "Brood Wars Zerg 7 - Town F<0>",
            "Brood Wars Zerg 8 - Town A<0>",
            "Brood Wars Zerg 8 - Town B<0>",
            "Brood Wars Zerg 8 - Town C<0>",
            "Brood Wars Zerg 8 - Town D<0>",
            "Brood Wars Zerg 8 - Town E<0>",
            "Brood Wars Zerg 8 - Town F<0>",
            "Brood Wars Zerg 9 - Town A<0>",
            "Brood Wars Zerg 9 - Town B<0>",
            "Brood Wars Zerg 9 - Town C<0>",
            "Brood Wars Zerg 9 - Town D<0>",
            "Brood Wars Zerg 9 - Town E<0>",
            "Brood Wars Zerg 9 - Town F<0>",
            "Brood Wars Zerg 10 - Town A<0>",
            "Brood Wars Zerg 10 - Town B<0>",
            "Brood Wars Zerg 10 - Town C<0>",
            "Brood Wars Zerg 10 - Town D<0>",
            "Brood Wars Zerg 10 - Town E<0>",
            "Brood Wars Zerg 10 - Town F<0>",
            "Expansion Zerg Campaign Easy<0>",
            "Expansion Zerg Campaign Medium<0>",
            "Expansion Zerg Campaign Difficult<0>",
            "Expansion Zerg Campaign Area Town<0>",
            "Expansion Protoss Campaign Easy<0>",
            "Expansion Protoss Campaign Medium<0>",
            "Expansion Protoss Campaign Difficult<0>",
            "Expansion Protoss Campaign Area Town<0>",
            "Expansion Terran Campaign Easy<0>",
            "Expansion Terran Campaign Medium<0>",
            "Expansion Terran Campaign Difficult<0>",
            "Expansion Terran Campaign Area Town<0>",
            "Expansion Terran Campaign Insane<0>",
            "Expansion Protoss Campaign Insane<0>",
            "Expansion Zerg Campaign Insane<0>",
            "Set Generic Command Target<0>",
            "Make These Units Patrol<0>",
            "Enter Transport<0>",
            "Exit Transport<0>",
            "Turn ON Shared Vision for Player 1<0>",
            "Turn ON Shared Vision for Player 2<0>",
            "Turn ON Shared Vision for Player 3<0>",
            "Turn ON Shared Vision for Player 4<0>",
            "Turn ON Shared Vision for Player 5<0>",
            "Turn ON Shared Vision for Player 6<0>",
            "Turn ON Shared Vision for Player 7<0>",
            "Turn ON Shared Vision for Player 8<0>",
            "Turn OFF Shared Vision for Player 1<0>",
            "Turn OFF Shared Vision for Player 2<0>",
            "Turn OFF Shared Vision for Player 3<0>",
            "Turn OFF Shared Vision for Player 4<0>",
            "Turn OFF Shared Vision for Player 5<0>",
            "Turn OFF Shared Vision for Player 6<0>",
            "Turn OFF Shared Vision for Player 7<0>",
            "Turn OFF Shared Vision for Player 8<0>",
            "AI Nuke Here<0>",
            "AI Harass Here<0>",
            "Set Unit Order To: Junk Yard Dog<0>",
            "View Players<0>",
            "u<0>Speed <3>U<1>p<0>",
            "p<0><3>P<1>lay<0>",
            "p<0><3>P<1>ause<0>",
            "d<0>Slow <3>D<1>own<0>",
            "Replay Progress<0>",
            "Paused<0>",
            "Unlimited<0>",
            "Unknown(Or invalid)",
        };
    }

    public class Sprite130Def :Gettable<Sprite130Def> {
        private int _index;
        private string _name;

        public int SpriteIndex { get { return _index; } }

        public string SpriteName { get { return _name; } }

        public static Sprite130Def getByIndex(int index) {
            if (index < AllSprites.Length) {
                return AllSprites[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return SpriteName;
        }

        public int getMaxValue() {
            return AllSprites.Length - 1;
        }

        public int getIndex() {
            return SpriteIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<Sprite130Def> getter, Action<Sprite130Def> setter, Func<Sprite130Def> getDefault) {
            return new TriggerDefinitionGeneralDef<Sprite130Def>(getter, setter, getDefault, AllSprites);
        }

        private Sprite130Def(int index, SpriteDef source) {
            _index = index;
            _name = source.SpriteName;
        }

        public static Sprite130Def[] AllSprites {
            get {
                if (_allSprites == null) {
                    int off = 130;
                    _allSprites = new Sprite130Def[SpriteDef.AllSprites.Length-off];
                    for (int i = 0; i < _allSprites.Length; i++) {
                        _allSprites[i] = new Sprite130Def(i, SpriteDef.AllSprites[off+i]);
                    }
                }
                return _allSprites;
            }
        }

        private static Sprite130Def[] _allSprites;
    }

    public class SpriteDef : Gettable<SpriteDef> {

        private int _index;
        private string _name;

        public int SpriteIndex { get { return _index; } }

        public string SpriteName { get { return _name; } }

        public static SpriteDef getByIndex(int index) {
            if (index < AllSprites.Length) {
                return AllSprites[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return SpriteName;
        }

        public int getMaxValue() {
            return AllSprites.Length - 1;
        }

        public int getIndex() {
            return SpriteIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<SpriteDef> getter, Action<SpriteDef> setter, Func<SpriteDef> getDefault) {
            return new TriggerDefinitionGeneralDef<SpriteDef>(getter, setter, getDefault, AllSprites);
        }

        private SpriteDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid sprite";
            }
        }

        public static SpriteDef[] AllSprites {
            get {
                if (_allSprites == null) {
                    _allSprites = new SpriteDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allSprites[i] = new SpriteDef(i);
                    }
                }
                return _allSprites;
            }
        }

        private static SpriteDef[] _allSprites;

        private static readonly string[] _Defs = {
            "2/38 Ash",
            "2/39 Ash",
            "2/41 Ash",
            "2/40 Ash",
            "2/42 Ash",
            "2/43 Ash",
            "2/44 Ash ",
            "2/1 Ash",
            "2/4 Ash",
            "2/5 Ash",
            "2/30 Ash",
            "2/28 Ash",
            "2/29 Ash",
            "4/1 Ash ",
            "4/2 Ash",
            "4/3 Ash",
            "4/56 Jungle",
            "4/57 Jungle",
            "4/58 Jungle",
            "4/59 Jungle",
            "9/5 Jungle",
            "9/6 Jungle",
            "9/7 Jungle",
            "4/51 Jungle",
            "4/52 Jungle",
            "4/54 Jungle",
            "4/53 Jungle",
            "9/1 Jungle",
            "9/2 Jungle",
            "9/3 Jungle",
            "9/4 Jungle",
            "4/12 Jungle",
            "4/13 Jungle",
            "4/1 Jungle",
            "4/3 Jungle",
            "4/2 Jungle",
            "4/5 Jungle",
            "4/4 Jungle",
            "4/9 Jungle",
            "4/10 Jungle",
            "5/5 Jungle",
            "5/7 Jungle",
            "5/6 Jungle",
            "5/9 Jungle",
            "5/8 Jungle",
            "4/6 Jungle",
            "4/7 Jungle",
            "4/17 Jungle",
            "13/4 Jungle",
            "11/5 Jungle",
            "11/6 Jungle",
            "11/7 Jungle",
            "11/8 Jungle",
            "11/10 Jungle",
            "11/11 Jungle",
            "11/12 Jungle",
            "7/4 Platform",
            "7/5 Platform",
            "7/6 Platform",
            "7/1 Platform",
            "7/2 Platform",
            "7/3 Platform",
            "7/9 Platform",
            "7/10 Platform",
            "7/8 Platform",
            "7/7 Platform",
            "7/26 Platform",
            "7/24 Platform",
            "7/28 Platform",
            "7/27 Platform",
            "7/25 Platform",
            "7/29 Platform",
            "7/30 Platform",
            "7/31 Platform",
            "12/1 Platform",
            "9/27 Platform",
            "5/54 Badlands",
            "5/55 Badlands",
            "5/56 Badlands",
            "5/57 Badlands",
            "6/16 Badlands",
            "6/17 Badlands",
            "6/20 Badlands",
            "6/21 Badlands",
            "5/10 Badlands",
            "5/50 Badlands",
            "5/52 Badlands",
            "5/53 Badlands",
            "5/51 Badlands",
            "6/3 Badlands",
            "11/3 Badlands",
            "11/8 Badlands",
            "11/6 Badlands",
            "11/7 Badlands",
            "11/9 Badlands",
            "11/10 Badlands",
            "11/11 Badlands",
            "11/12 Badlands",
            "11/13 Badlands",
            "11/14 Badlands",
            "1/13 Badlands",
            "1/9 Badlands",
            "1/11 Badlands",
            "1/14 Badlands",
            "1/10 Badlands",
            "1/12 Badlands",
            "1/15 Badlands",
            "1/7 Badlands",
            "1/5 Badlands",
            "1/16 Badlands",
            "1/8 Badlands",
            "1/6 Badlands1",
            "1/6 Badlands2",
            "1/6 Badlands3",
            "1/6 Badlands4",
            "1/6 Badlands5",
            "1/6 Badlands6",
            "1/6 Badlands7",
            "1/6 Badlands8",
            "4/15 Installation1",
            "4/15 Installation2",
            "3/9 Installation",
            "3/10 Installation",
            "3/11 Installation",
            "3/12 Installation",
            "1/6 Badlands9",
            "1/6 Badlands10",
            "3/1 Installation",
            "3/2 Installation",
            "1/6 Badlands11",
            "Scourge",
            "Scourge Death",
            "Scourge Explosion",
            "Broodling",
            "Broodling Remnants",
            "Infested Terran",
            "Infested Terran Explosion",
            "Guardian Cocoon",
            "Defiler",
            "Defiler Remnants",
            "Drone",
            "Drone Remnants",
            "Egg",
            "Egg Remnants",
            "Guardian",
            "Guardian Death",
            "Hydralisk",
            "Hydralisk Remnants",
            "Infested Kerrigan",
            "Larva",
            "Larva Remnants",
            "Mutalisk",
            "Mutalisk Death",
            "Overlord",
            "Overlord Death",
            "Queen",
            "Queen Death",
            "Ultralisk",
            "Ultralisk Remnants",
            "Zergling",
            "Zergling Remnants",
            "Cerebrate",
            "Infested Command Center",
            "Spawning Pool",
            "Mature Chrysalis",
            "Evolution Chamber",
            "Creep Colony",
            "Hatchery",
            "Hive",
            "Lair",
            "Sunken Colony",
            "Greater Spire",
            "Defiler Mound",
            "Queen's Nest",
            "Nydus Canal",
            "Overmind With Shell",
            "Overmind Without Shell",
            "Ultralisk Cavern",
            "Extractor",
            "Hydralisk Den",
            "Spire",
            "Spore Colony",
            "Zerg Building Spawn (Small)",
            "Zerg Building Spawn (Medium)",
            "Zerg Building Spawn (Large)",
            "Zerg Building Explosion",
            "Zerg Building Rubble (Small)",
            "Zerg Building Rubble (Large)",
            "Arbiter",
            "Archon Energy",
            "Carrier",
            "Dragoon",
            "Dragoon Remnants",
            "Interceptor",
            "Probe",
            "Scout",
            "Shuttle",
            "High Templar",
            "Dark Templar (Hero)",
            "Reaver",
            "Scarab",
            "Zealot",
            "Observer",
            "Templar Archives",
            "Assimilator",
            "Observatory",
            "Citadel of Adun",
            "Forge",
            "Gateway",
            "Cybernetics Core",
            "Khaydarin Crystal Formation",
            "Nexus",
            "Photon Cannon",
            "Arbiter Tribunal",
            "Pylon",
            "Robotics Facility",
            "Shield Battery",
            "Stargate",
            "Stasis Cell/Prison",
            "Robotics Support Bay",
            "Protoss Temple",
            "Fleet Beacon",
            "Explosion (Large)",
            "Protoss Building Rubble (Small)",
            "Protoss Building Rubble (Large)",
            "Battlecruiser",
            "Civilian",
            "Dropship",
            "Firebat",
            "Ghost",
            "Ghost Remnants",
            "Nuke Target Dot",
            "Goliath Base",
            "Goliath Turret",
            "Sarah Kerrigan",
            "Marine",
            "Marine Remnants",
            "Scanner Sweep",
            "Wraith",
            "SCV",
            "Siege Tank (Tank) Base",
            "Siege Tank (Tank) Turret",
            "Siege Tank (Siege) Base",
            "Siege Tank (Siege) Turret",
            "Vulture",
            "Spider Mine",
            "Science Vessel (Base)",
            "Science Vessel (Turret)",
            "Terran Academy",
            "Barracks",
            "Armory",
            "Comsat Station",
            "Command Center",
            "Supply Depot",
            "Control Tower",
            "Factory",
            "Covert Ops",
            "Ion Cannon",
            "Machine Shop",
            "Missile Turret (Base)",
            "Crashed Batlecruiser",
            "Physics Lab",
            "Bunker",
            "Refinery",
            "Science Facility",
            "Nuclear Silo",
            "Nuclear Missile",
            "Nuke Hit",
            "Starport",
            "Engineering Bay",
            "Terran Construction (Large)",
            "Terran Construction (Small)",
            "Building Explosion (Large)",
            "Terran Building Rubble (Small)",
            "Terran Building Rubble (Large)",
            "Vespene Geyser",
            "Ragnasaur (Ash)",
            "Rhynadon (Badlands)",
            "Bengalaas (Jungle)",
            "Mineral Field Type1",
            "Mineral Field Type2",
            "Mineral Field Type3",
            "Independent Starport (Unused)",
            "Zerg Beacon",
            "Terran Beacon",
            "Protoss Beacon",
            "Dark Swarm",
            "Flag",
            "Young Chrysalis",
            "Psi Emitter",
            "Data Disc",
            "Khaydarin Crystal",
            "Mineral Chunk Type1",
            "Mineral Chunk Type2",
            "Protoss Gas Orb Type1",
            "Protoss Gas Orb Type2",
            "Zerg Gas Sac Type1",
            "Zerg Gas Sac Type2",
            "Terran Gas Tank Type1",
            "Terran Gas Tank Type2",
            "White Circle (Invisible)",
            "Start Location",
            "Map Revealer",
            "Floor Gun Trap",
            "Wall Missile Trap",
            "Wall Missile Trap2",
            "Wall Flame Trap",
            "Wall Flame Trap2",
            "Floor Missile Trap",
            "Longbolt/Gemini Missiles Trail",
            "Grenade Shot Smoke",
            "Vespene Geyser Smoke1",
            "Vespene Geyser Smoke2",
            "Vespene Geyser Smoke3",
            "Vespene Geyser Smoke4",
            "Vespene Geyser Smoke5",
            "Small Explosion (Unused)",
            "Double Explosion",
            "Cursor Marker",
            "Egg Spawn",
            "High Templar Glow",
            "Psi Field (Right Upper)",
            "Burrowing Dust",
            "Building Landing Dust Type1",
            "Building Landing Dust Type2",
            "Building Landing Dust Type3",
            "Building Landing Dust Type4",
            "Building Landing Dust Type5",
            "Building Lifting Dust Type1",
            "Building Lifting Dust Type2",
            "Building Lifting Dust Type3",
            "Building Lifting Dust Type4",
            "Needle Spines",
            "Dual Photon Blasters Hit",
            "Particle Beam Hit",
            "Anti-Matter Missile",
            "Pulse Cannon",
            "Phase Disruptor",
            "STA/STS Photon Cannon Overlay",
            "Psionic Storm",
            "Fusion Cutter Hit",
            "Gauss Rifle Hit",
            "Gemini Missiles",
            "Fragmentation Grenade",
            "Magna Pulse (Unused)",
            "Lockdown/LongBolt/Hellfire Missile",
            "C-10 Canister Rifle Hit",
            "ATS/ATA Laser Battery",
            "Burst Lasers",
            "Arclite Shock Cannon Hit",
            "Yamato Gun",
            "Yamato Gun Trail",
            "EMP Shockwave Missile",
            "Needle Spine Hit",
            "Plasma Drip Hit (Unused)",
            "Sunken Colony Tentacle",
            "Venom (Unused Zerg Weapon)",
            "Acid Spore",
            "Glave Wurm",
            "Seeker Spores",
            "Queen Spell Holder",
            "Stasis Field Hit",
            "Plague Cloud",
            "Consume",
            "Ensnare",
            "Glave Wurm/Seeker Spores Hit",
            "Psionic Shockwave Hit",
            "Glave Wurm Trail",
            "Seeker Spores Overlay",
            "Phase Disruptor (Unused)",
            "White Circle",
            "Acid Spray (Unused)",
            "Plasma Drip (Unused)",
            "Scarab/Anti-Matter Missile Overlay",
            "Hallucination Death1",
            "Hallucination Death2",
            "Hallucination Death3",
            "Bunker Overlay",
            "FlameThrower",
            "Recall Field",
            "Scanner Sweep Hit",
            "Left Upper Level Door",
            "Right Upper Level Door",
            "Substructure Left Door",
            "Substructure Right Door",
            "Substructure Opening Hole",
            "7/13 Twilight",
            "7/14 Twilight",
            "7/16 Twilight",
            "7/15 Twilight",
            "7/19 Twilight",
            "7/20 Twilight",
            "7/21 Twilight",
            "Unknown Twilight",
            "7/17 Twilight",
            "6/1 Twilight",
            "6/2 Twilight",
            "6/3 Twilight",
            "6/4 Twilight",
            "6/5 Twilight",
            "8/3 Twilight",
            "8/3 Twilight",
            "9/29 Ice",
            "9/28 Ice",
            "12/38 Ice ",
            "12/37 Ice",
            "12/33 Ice",
            "9/21 Ice",
            "9/15 Ice",
            "9/16 Ice",
            "Unknown410",
            "Unknown411",
            "12/9 Ice1",
            "12/10 Ice",
            "9/24 Ice ",
            "9/23 Ice",
            "Unknown416",
            "12/7 Ice",
            "12/8 Ice",
            "12/9 Ice2",
            "12/10 Ice",
            "12/40 Ice",
            "12/41 Ice",
            "12/42 Ice",
            "12/5 Ice",
            "12/6 Ice ",
            "12/36 Ice",
            "12/32 Ice",
            "12/33 Ice",
            "12/34 Ice",
            "12/24 Ice",
            "12/25 Ice",
            "9/22 Ice",
            "12/31 Ice",
            "12/20 Ice",
            "12/24 Ice",
            "12/25 Ice",
            "12/30 Ice",
            "12/30 Ice",
            "Unknown439",
            "4/1 Ice",
            "6/1 Ice",
            "5/6 Ice ",
            "5/7 Ice ",
            "5/8 Ice ",
            "5/9 Ice",
            "10/10 Desert1",
            "10/12 Desert1",
            "10/8 Desert",
            "10/9 Desert1",
            "6/10 Desert",
            "6/13 Desert",
            "Unknown Desert",
            "10/12 Desert2",
            "10/9 Desert2",
            "10/10 Desert2",
            "10/11 Desert",
            "10/14 Desert",
            "10/41 Desert",
            "10/39 Desert",
            "10/8 Desert",
            "10/6 Desert",
            "10/7 Desert",
            "4/6 Desert",
            "4/11 Desert",
            "4/10 Desert",
            "4/9 Desert",
            "4/7 Desert",
            "4/12 Desert",
            "4/8 Desert",
            "4/13 Desert",
            "6/13 Desert",
            "4/17 Desert",
            "6/20 Desert",
            "4/15 Desert1",
            "4/15 Desert2",
            "10/23 Desert",
            "8/23 Desert",
            "10/5 Desert",
            "12/1 Desert Overlay",
            "11/3 Desert",
            "Lurker Egg",
            "Devourer",
            "Devourer Death",
            "Lurker Remnants",
            "Lurker",
            "Dark Archon Energy",
            "Corsair",
            "Dark Templar (Unit)",
            "Medic",
            "Medic Remnants",
            "Valkyrie",
            "Scantid (Desert)",
            "Kakaru (Twilight)",
            "Ursadon (Ice)",
            "Overmind Cocoon",
            "Power Generator",
            "Xel'Naga Temple",
            "Psi Disrupter",
            "Warp Gate",
            "Feedback Hit (Small)",
            "Feedback Hit (Medium)",
            "Feedback Hit (Large)",
            "Disruption Web",
            "White Circle",
            "Halo Rockets Trail",
            "Neutron Flare",
            "Neutron Flare Overlay (Unused)",
            "Optical Flare Grenade",
            "Halo Rockets",
            "Subterranean Spines Hit",
            "Subterranean Spines",
            "Corrosive Acid Shot",
            "Corrosive Acid Hit",
            "Maelstrom Hit",
            "Uraj",
            "Khalis"
        };
    }

    public class StringDef : Gettable<StringDef>, SaveableItem {

        private string _value;

        public int getIndex() {
            throw new NotImplementedException();
        }

        public static SaveableItem getDefaultValue() {
            return new StringDef("");
        }

        public string getValue() {
            return _value;
        }

        public int getMaxValue() {
            throw new NotImplementedException();
        }

        public StringDef(string str) {
            _value = str;
        }

        public TriggerDefinitionPart getTriggerPart(Func<StringDef> getter, Action<StringDef> setter, Func<StringDef> getDefault) {
            return new TriggerDefinitionInputText(getter, setter);
        }

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return "\"" + ToString() + "\"";
        }
    }

    public class SwitchNameDef : SaveableItem, Gettable<SwitchNameDef> {

        private int _index;
        private string _name;
        private string _customName;

        public int SwitchIndex { get { return _index; } }

        public string SwitchName { get { return MainWindow.UseCustomName ? _name : _customName; } }

        public static void __setSwitchNameDontUseOutsideOfParser(int switchIndex, string switchName) {
            AllSwitches[switchIndex]._customName = switchName;
        }

        public static SwitchNameDef getByIndex(int index) {
            if (index < AllSwitches.Length) {
                return AllSwitches[index];
            }
            throw new NotImplementedException();
        }

        public static SwitchNameDef getDefaultValue() {
            return SwitchNameDef.getByIndex(0);
        }

        private SwitchNameDef(int index) {
            _index = index;
            _name = "Switch " + index;
            _customName = _name;
        }

        public override string ToString() {
            return SwitchName.ToString();
        }

        public string ToSaveString() {
            return _index.ToString();
        }

        public int getMaxValue() {
            return AllSwitches.Length;
        }

        public int getIndex() {
            return _index;
        }

        public TriggerDefinitionPart getTriggerPart(Func<SwitchNameDef> getter, Action<SwitchNameDef> setter, Func<SwitchNameDef> getDefault) {
            return new TriggerDefinitionGeneralDef<SwitchNameDef>(getter, setter, getDefault, AllSwitches);
        }

        public static SwitchNameDef[] AllSwitches {
            get {
                if (_allSwitches == null) {
                    _allSwitches = new SwitchNameDef[256];
                    for (int i = 0; i < 256; i++) {
                        _allSwitches[i] = new SwitchNameDef(i);
                    }
                }
                return _allSwitches;
            }
        }

        private static SwitchNameDef[] _allSwitches;
    }

    public class SwitchSetState : SaveableItem, Gettable<SwitchSetState> {

        private string _value;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return ToString();
        }

        private SwitchSetState(string value) {
            _value = value;
        }

        public static readonly SwitchSetState Set = new SwitchSetState("Set");
        public static readonly SwitchSetState Clear = new SwitchSetState("Clear");
        public static readonly SwitchSetState Randomize = new SwitchSetState("Randomize");
        public static readonly SwitchSetState Toggle = new SwitchSetState("Toggle");

        public static SwitchSetState getDefaultValue() {
            return SwitchSetState.Set;
        }

        public int getMaxValue() {
            return AllStates.Length - 1;
        }

        public int getIndex() {
            if (this == Set) {
                return 0;
            } else if (this == Clear) {
                return 1;
            } else if (this == Randomize) {
                return 2;
            } else if (this == Toggle) {
                return 3;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<SwitchSetState> getter, Action<SwitchSetState> setter, Func<SwitchSetState> getDefault) {
            return new TriggerDefinitionGeneralDef<SwitchSetState>(getter, setter, getDefault, AllStates);
        }

        public static SwitchSetState[] AllStates = new SwitchSetState[] { Set, Clear, Randomize, Toggle };
    }

    public class SwitchState : SaveableItem, Gettable<SwitchState> {

        private string _value;

        public override string ToString() {
            return _value;
        }

        public string ToSaveString() {
            return ToString();
        }

        private SwitchState(string value) {
            _value = value;
        }

        public static readonly SwitchState Set = new SwitchState("Set");
        public static readonly SwitchState NotSet = new SwitchState("Cleared");

        public static SwitchState getDefaultValue() {
            return SwitchState.Set;
        }

        public int getMaxValue() {
            return AllStates.Length - 1;
        }

        public int getIndex() {
            if (this == Set) {
                return 0;
            } else if (this == NotSet) {
                return 1;
            }
            throw new NotImplementedException();
        }

        public TriggerDefinitionPart getTriggerPart(Func<SwitchState> getter, Action<SwitchState> setter, Func<SwitchState> getDefault) {
            return new TriggerDefinitionGeneralDef<SwitchState>(getter, setter, getDefault, AllStates);
        }

        public static SwitchState[] AllStates = new SwitchState[] { Set, NotSet };
    }

    public class TechDef : Gettable<TechDef>, TechOrUpgradeDef<TechDef> {

        private int _index;
        private string _name;

        public int TechIndex { get { return _index; } }

        public string TechName { get { return _name; } }

        public static TechDef getByIndex(int index) {
            if (index < AllTechs.Length) {
                return AllTechs[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return TechName;
        }

        public virtual int getMaxValue() {
            return AllTechs.Length - 1;
        }

        public virtual int getIndex() {
            return TechIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<TechDef> getter, Action<TechDef> setter, Func<TechDef> getDefault) {
            return new TriggerDefinitionGeneralDef<TechDef>(getter, setter, getDefault, AllTechs);
        }

        public int getMemoryIndex() {
            if (_index >= 0 && _index < SCTechDef.AllTechs.Length) {
                return 0;
            } else {
                return 1;
            }
        }

        public int getFirstPartLength() {
            return SCTechDef.AllTechs.Length;
        }

        protected TechDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid tech";
            }
        }

        public static TechDef[] AllTechs {
            get {
                if (_allTechs == null) {
                    _allTechs = new TechDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allTechs[i] = new TechDef(i);
                    }
                }
                return _allTechs;
            }
        }

        private static TechDef[] _allTechs;

        private static readonly string[] _Defs = {
            "Stim Packs",
            "Lockdown",
            "EMP Shockwave",
            "Spider Mines",
            "Scanner Sweep",
            "Tank Siege Mode",
            "Defensive Matrix",
            "Irradiate",
            "Yamato Gun",
            "Cloaking Field",
            "Personnel Cloaking",
            "Burrowing",
            "Infestation",
            "Spawn Broodlings",
            "Dark Swarm",
            "Plague",
            "Consume",
            "Ensnare",
            "Parasite",
            "Psionic Storm",
            "Hallucination",
            "Recall",
            "Stasis Field",
            "Archon Warp",
            "Restoration",
            "Disruption Web",
            "Unused ",
            "Mind Control",
            "Dark Archon Meld",
            "Feedback",
            "Optical Flare",
            "Maelstorm",
            "Lurker Aspect",
            "Unused ",
            "Healing",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
            "Unused ",
        };

    }

    public class UnitAvatarDef : Gettable<UnitAvatarDef>, GettableImage {

        private int _index;
        private BitmapImageX _image;

        public int AvatarIndex { get { return _index; } }

        public BitmapImageX UnitAvatarImage { get { return _image; } }

        public static UnitAvatarDef getByIndex(int index) {
            if (index < AllUnitAvatars.Length) {
                return AllUnitAvatars[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return "";
        }

        public int getMaxValue() {
            return AllUnitAvatars.Length - 1;
        }

        public int getIndex() {
            return AvatarIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<UnitAvatarDef> getter, Action<UnitAvatarDef> setter, Func<UnitAvatarDef> getDefault) {
            return new TriggerDefinitionGeneralIconsList<UnitAvatarDef>(getter, setter, getDefault, AllUnitAvatarImages, 10);
        }

        public BitmapImageX getImage() {
            return UnitAvatarImage;
        }

        private UnitAvatarDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                string path = _Defs[index];
                _image = new BitmapImageX("icons/" + path, this);
            } else {
                throw new NotImplementedException();
            }
        }

        public static UnitAvatarDef[] AllUnitAvatars {
            get {
                return Application.Current.Dispatcher.Invoke(() => {
                    if (_allUnitAvatars == null) {
                        _allUnitAvatars = new UnitAvatarDef[_Defs.Length];
                        _allUnitAvatarImages = new BitmapImageX[_Defs.Length];
                        for (int i = 0; i < _Defs.Length; i++) {
                            _allUnitAvatars[i] = new UnitAvatarDef(i);
                            _allUnitAvatarImages[i] = _allUnitAvatars[i].UnitAvatarImage;
                        }
                    }
                    return _allUnitAvatars;
                });
            }
        }

        private static UnitAvatarDef[] _allUnitAvatars;

        private static readonly string[] _Defs = {
            "icon_0.png",
            "icon_1.png",
            "icon_2.png",
            "icon_3.png",
            "icon_4.png",
            "icon_5.png",
            "icon_6.png",
            "icon_7.png",
            "icon_8.png",
            "icon_9.png",
            "icon_10.png",
            "icon_11.png",
            "icon_12.png",
            "icon_13.png",
            "icon_14.png",
            "icon_15.png",
            "icon_16.png",
            "icon_17.png",
            "icon_18.png",
            "icon_19.png",
            "icon_20.png",
            "icon_21.png",
            "icon_22.png",
            "icon_23.png",
            "icon_24.png",
            "icon_25.png",
            "icon_26.png",
            "icon_27.png",
            "icon_28.png",
            "icon_29.png",
            "icon_30.png",
            "icon_31.png",
            "icon_32.png",
            "icon_33.png",
            "icon_34.png",
            "icon_35.png",
            "icon_36.png",
            "icon_37.png",
            "icon_38.png",
            "icon_39.png",
            "icon_40.png",
            "icon_41.png",
            "icon_42.png",
            "icon_43.png",
            "icon_44.png",
            "icon_45.png",
            "icon_46.png",
            "icon_47.png",
            "icon_48.png",
            "icon_49.png",
            "icon_50.png",
            "icon_51.png",
            "icon_52.png",
            "icon_53.png",
            "icon_54.png",
            "icon_55.png",
            "icon_56.png",
            "icon_57.png",
            "icon_58.png",
            "icon_59.png",
            "icon_60.png",
            "icon_61.png",
            "icon_62.png",
            "icon_63.png",
            "icon_64.png",
            "icon_65.png",
            "icon_66.png",
            "icon_67.png",
            "icon_68.png",
            "icon_69.png",
            "icon_70.png",
            "icon_71.png",
            "icon_72.png",
            "icon_73.png",
            "icon_74.png",
            "icon_75.png",
            "icon_76.png",
            "icon_77.png",
            "icon_78.png",
            "icon_79.png",
            "icon_80.png",
            "icon_81.png",
            "icon_82.png",
            "icon_83.png",
            "icon_84.png",
            "icon_85.png",
            "icon_86.png",
            "icon_87.png",
            "icon_88.png",
            "icon_89.png",
            "icon_90.png",
            "icon_91.png",
            "icon_92.png",
            "icon_93.png",
            "icon_94.png",
            "icon_95.png",
            "icon_96.png",
            "icon_97.png",
            "icon_98.png",
            "icon_99.png",
            "icon_100.png",
            "icon_101.png",
            "icon_102.png",
            "icon_103.png",
            "icon_104.png",
            "icon_105.png",
            "icon_106.png",
            "icon_107.png",
            "icon_108.png",
            "icon_109.png",
            "icon_110.png",
            "icon_111.png",
            "icon_112.png",
            "icon_113.png",
            "icon_114.png",
            "icon_115.png",
            "icon_116.png",
            "icon_117.png",
            "icon_118.png",
            "icon_119.png",
            "icon_120.png",
            "icon_121.png",
            "icon_122.png",
            "icon_123.png",
            "icon_124.png",
            "icon_125.png",
            "icon_126.png",
            "icon_127.png",
            "icon_128.png",
            "icon_129.png",
            "icon_130.png",
            "icon_131.png",
            "icon_132.png",
            "icon_133.png",
            "icon_134.png",
            "icon_135.png",
            "icon_136.png",
            "icon_137.png",
            "icon_138.png",
            "icon_139.png",
            "icon_140.png",
            "icon_141.png",
            "icon_142.png",
            "icon_143.png",
            "icon_144.png",
            "icon_145.png",
            "icon_146.png",
            "icon_147.png",
            "icon_148.png",
            "icon_149.png",
            "icon_150.png",
            "icon_151.png",
            "icon_152.png",
            "icon_153.png",
            "icon_154.png",
            "icon_155.png",
            "icon_156.png",
            "icon_157.png",
            "icon_158.png",
            "icon_159.png",
            "icon_160.png",
            "icon_161.png",
            "icon_162.png",
            "icon_163.png",
            "icon_164.png",
            "icon_165.png",
            "icon_166.png",
            "icon_167.png",
            "icon_168.png",
            "icon_169.png",
            "icon_170.png",
            "icon_171.png",
            "icon_172.png",
            "icon_173.png",
            "icon_174.png",
            "icon_175.png",
            "icon_176.png",
            "icon_177.png",
            "icon_178.png",
            "icon_179.png",
            "icon_180.png",
            "icon_181.png",
            "icon_182.png",
            "icon_183.png",
            "icon_184.png",
            "icon_185.png",
            "icon_186.png",
            "icon_187.png",
            "icon_188.png",
            "icon_189.png",
            "icon_190.png",
            "icon_191.png",
            "icon_192.png",
            "icon_193.png",
            "icon_194.png",
            "icon_195.png",
            "icon_196.png",
            "icon_197.png",
            "icon_198.png",
            "icon_199.png",
            "icon_200.png",
            "icon_201.png",
            "icon_202.png",
            "icon_203.png",
            "icon_204.png",
            "icon_205.png",
            "icon_206.png",
            "icon_207.png",
            "icon_208.png",
            "icon_209.png",
            "icon_210.png",
            "icon_211.png",
            "icon_212.png",
            "icon_213.png",
            "icon_214.png",
            "icon_215.png",
            "icon_216.png",
            "icon_217.png",
            "icon_218.png",
            "icon_219.png",
            "icon_220.png",
            "icon_221.png",
            "icon_222.png",
            "icon_223.png",
            "icon_224.png",
            "icon_225.png",
            "icon_226.png",
            "icon_227.png",
            "icon_228.png",
            "icon_229.png",
            "icon_230.png",
            "icon_231.png",
            "icon_232.png",
            "icon_233.png",
            "icon_234.png",
            "icon_235.png",
            "icon_236.png",
            "icon_237.png",
            "icon_238.png",
            "icon_239.png",
            "icon_240.png",
            "icon_241.png",
            "icon_242.png",
            "icon_243.png",
            "icon_244.png",
            "icon_245.png",
            "icon_246.png",
            "icon_247.png",
            "icon_248.png",
            "icon_249.png",
            "icon_250.png",
            "icon_251.png",
            "icon_252.png",
            "icon_253.png",
            "icon_254.png",
            "icon_255.png",
            "icon_256.png",
            "icon_257.png",
            "icon_258.png",
            "icon_259.png",
            "icon_260.png",
            "icon_261.png",
            "icon_262.png",
            "icon_263.png",
            "icon_264.png",
            "icon_265.png",
            "icon_266.png",
            "icon_267.png",
            "icon_268.png",
            "icon_269.png",
            "icon_270.png",
            "icon_271.png",
            "icon_272.png",
            "icon_273.png",
            "icon_274.png",
            "icon_275.png",
            "icon_276.png",
            "icon_277.png",
            "icon_278.png",
            "icon_279.png",
            "icon_280.png",
            "icon_281.png",
            "icon_282.png",
            "icon_283.png",
            "icon_284.png",
            "icon_285.png",
            "icon_286.png",
            "icon_287.png",
            "icon_288.png",
            "icon_289.png",
            "icon_290.png",
            "icon_291.png",
            "icon_292.png",
            "icon_293.png",
            "icon_294.png",
            "icon_295.png",
            "icon_296.png",
            "icon_297.png",
            "icon_298.png",
            "icon_299.png",
            "icon_300.png",
            "icon_301.png",
            "icon_302.png",
            "icon_303.png",
            "icon_304.png",
            "icon_305.png",
            "icon_306.png",
            "icon_307.png",
            "icon_308.png",
            "icon_309.png",
            "icon_310.png",
            "icon_311.png",
            "icon_312.png",
            "icon_313.png",
            "icon_314.png",
            "icon_315.png",
            "icon_316.png",
            "icon_317.png",
            "icon_318.png",
            "icon_319.png",
            "icon_320.png",
            "icon_321.png",
            "icon_322.png",
            "icon_323.png",
            "icon_324.png",
            "icon_325.png",
            "icon_326.png",
            "icon_327.png",
            "icon_328.png",
            "icon_329.png",
            "icon_330.png",
            "icon_331.png",
            "icon_332.png",
            "icon_333.png",
            "icon_334.png",
            "icon_335.png",
            "icon_336.png",
            "icon_337.png",
            "icon_338.png",
            "icon_339.png",
            "icon_340.png",
            "icon_341.png",
            "icon_342.png",
            "icon_343.png",
            "icon_344.png",
            "icon_345.png",
            "icon_346.png",
            "icon_347.png",
            "icon_348.png",
            "icon_349.png",
            "icon_350.png",
            "icon_351.png",
            "icon_352.png",
            "icon_353.png",
            "icon_354.png",
            "icon_355.png",
            "icon_356.png",
            "icon_357.png",
            "icon_358.png",
            "icon_359.png",
            "icon_360.png",
            "icon_361.png",
            "icon_362.png",
            "icon_363.png",
            "icon_364.png",
            "icon_365.png",
            "icon_366.png",
            "icon_367.png",
            "icon_368.png",
            "icon_369.png",
            "icon_370.png",
            "icon_371.png",
            "icon_372.png",
            "icon_373.png",
            "icon_374.png",
            "icon_375.png",
            "icon_376.png",
            "icon_377.png",
            "icon_378.png",
            "icon_379.png",
            "icon_380.png",
            "icon_381.png",
            "icon_382.png",
            "icon_383.png",
            "icon_384.png",
            "icon_385.png"
        };

        private static BitmapImageX[] _allUnitAvatarImages;

        public static BitmapImageX[] AllUnitAvatarImages { get { return _allUnitAvatarImages; } }
    }

    public class UnitDef : Gettable<UnitDef>, SaveableItem {

        private int _id;
        private string _name;
        private string _customName;

        public int UnitID { get { return _id; } }
        public string UnitName { get { return MainWindow.UseCustomName ? _name : _customName; } }

        private static readonly Dictionary<int, UnitDef> _defs = new Dictionary<int, UnitDef>();

        public static UnitDef getByIndex(int index) {
            if (index < AllUnits.Length) {
                return AllUnits[index];
            }
            return new UnitDef(index);
        }

        public static void __setUnitNameDontUseOutsideOfParser(int index, string unitName) {
            AllUnits[index]._customName = unitName;
        }

        public override string ToString() {
            return UnitName.ToString();
        }

        public static UnitDef getDefaultValue() {
            return UnitDef.getByIndex(0);
        }

        public int getMaxValue() {
            return AllUnits.Length - 1;
        }

        public int getIndex() {
            return UnitID;
        }

        public TriggerDefinitionPart getTriggerPart(Func<UnitDef> getter, Action<UnitDef> setter, Func<UnitDef> getDefault) {
            return new TriggerDefinitionGeneralDef<UnitDef>(getter, setter,getDefault, AllUnits);
        }

        public string ToSaveString() {
            return UnitID.ToString();
        }

        private UnitDef(int id) {
            _id = id;
            if (id < _Defs.Length) {
                _name = _Defs[id];
            } else {
                _name = "EUD (" + id + ")";
            }
            _customName = _name;
        }

        public static UnitDef[] AllUnits {
            get {
                if (_allUnits == null) {
                    _allUnits = new UnitDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        AllUnits[i] = new UnitDef(i);
                    }
                }
                return _allUnits;
            }
        }

        private static UnitDef[] _allUnits;

        private static readonly string[] _Defs = {
            "Terran Marine",
            "Terran Ghost",
            "Terran Vulture",
            "Terran Goliath",
            "Goliath Turret",
            "Terran Siege Tank (Tank Mode)",
            "Tank Turret(Tank Mode)",
            "Terran SCV",
            "Terran Wraith",
            "Terran Science Vessel",
            "Gui Montang (Firebat)",
            "Terran Dropship",
            "Terran Battlecruiser",
            "Vulture Spider Mine",
            "Nuclear Missile",
            "Terran Civilian",
            "Sarah Kerrigan (Ghost)",
            "Alan Schezar (Goliath)",
            "Alan Schezar Turret",
            "Jim Raynor (Vulture)",
            "Jim Raynor (Marine)",
            "Tom Kazansky (Wraith)",
            "Magellan (Science Vessel)",
            "Edmund Duke (Siege Tank)",
            "Edmund Duke Turret",
            "Edmund Duke (Siege Mode)",
            "Edmund Duke Turret",
            "Arcturus Mengsk (Battlecruiser)",
            "Hyperion (Battlecruiser)",
            "Norad II (Battlecruiser)",
            "Terran Siege Tank (Siege Mode)",
            "Tank Turret (Siege Mode)",
            "Firebat",
            "Scanner Sweep",
            "Terran Medic",
            "Zerg Larva",
            "Zerg Egg",
            "Zerg Zergling",
            "Zerg Hydralisk",
            "Zerg Ultralisk",
            "Zerg Broodling",
            "Zerg Drone",
            "Zerg Overlord",
            "Zerg Mutalisk",
            "Zerg Guardian",
            "Zerg Queen",
            "Zerg Defiler",
            "Zerg Scourge",
            "Torrarsque (Ultralisk)",
            "Matriarch (Queen)",
            "Infested Terran",
            "Infested Kerrigan",
            "Unclean One (Defiler)",
            "Hunter Killer (Hydralisk)",
            "Devouring One (Zergling)",
            "Kukulza (Mutalisk)",
            "Kukulza (Guardian)",
            "Yggdrasill (Overlord)",
            "Terran Valkyrie Frigate",
            "Mutalisk/Guardian Cocoon",
            "Protoss Corsair",
            "Protoss Dark Templar(Unit)",
            "Zerg Devourer",
            "Protoss Dark Archon",
            "Protoss Probe",
            "Protoss Zealot",
            "Protoss Dragoon",
            "Protoss High Templar",
            "Protoss Archon",
            "Protoss Shuttle",
            "Protoss Scout",
            "Protoss Arbiter",
            "Protoss Carrier",
            "Protoss Interceptor",
            "Dark Templar(Hero)",
            "Zeratul (Dark Templar)",
            "Tassadar/Zeratul (Archon)",
            "Fenix (Zealot)",
            "Fenix (Dragoon)",
            "Tassadar (Templar)",
            "Mojo (Scout)",
            "Warbringer (Reaver)",
            "Gantrithor (Carrier)",
            "Protoss Reaver",
            "Protoss Observer",
            "Protoss Scarab",
            "Danimoth (Arbiter)",
            "Aldaris (Templar)",
            "Artanis (Scout)",
            "Rhynadon (Badlands Critter)",
            "Bengalaas (Jungle Critter)",
            "Unused - Was Cargo Ship",
            "Unused - Was Mercenary Gunship",
            "Scantid (Desert Critter)",
            "Kakaru (Twilight Critter)",
            "Ragnasaur (Ashworld Critter)",
            "Ursadon (Ice World Critter)",
            "Lurker Egg",
            "Raszagal",
            "Samir Duran (Ghost)",
            "Alexei Stukov (Ghost)",
            "Map Revealer",
            "Gerard DuGalle",
            "Zerg Lurker",
            "Infested Duran",
            "Disruption Web",
            "Terran Command Center",
            "Terran Comsat Station",
            "Terran Nuclear Silo",
            "Terran Supply Depot",
            "Terran Refinery",
            "Terran Barracks",
            "Terran Academy",
            "Terran Factory",
            "Terran Starport",
            "Terran Control Tower",
            "Terran Science Facility",
            "Terran Covert Ops",
            "Terran Physics Lab",
            "Unused - Was Starbase?",
            "Terran Machine Shop",
            "Unused - Was Repair Bay?",
            "Terran Engineering Bay",
            "Terran Armory",
            "Terran Missile Turret",
            "Terran Bunker",
            "Norad II",
            "Ion Cannon",
            "Uraj Crystal",
            "Khalis Crystal",
            "Infested Command Center",
            "Zerg Hatchery",
            "Zerg Lair",
            "Zerg Hive",
            "Zerg Nydus Canal",
            "Zerg Hydralisk Den",
            "Zerg Defiler Mound",
            "Zerg Greater Spire",
            "Zerg Queen's Nest",
            "Zerg Evolution Chamber",
            "Zerg Ultralisk Cavern",
            "Zerg Spire",
            "Zerg Spawning Pool",
            "Zerg Creep Colony",
            "Zerg Spore Colony",
            "Unused Zerg Building",
            "Zerg Sunken Colony",
            "Zerg Overmind (With Shell)",
            "Zerg Overmind",
            "Zerg Extractor",
            "Mature Chrysalis",
            "Zerg Cerebrate",
            "Zerg Cerebrate Daggoth",
            "Unused Zerg Building 5",
            "Protoss Nexus",
            "Protoss Robotics Facility",
            "Protoss Pylon",
            "Protoss Assimilator",
            "Unused Protoss Building",
            "Protoss Observatory",
            "Protoss Gateway",
            "Unused Protoss Building",
            "Protoss Photon Cannon",
            "Protoss Citadel of Adun",
            "Protoss Cybernetics Core",
            "Protoss Templar Archives",
            "Protoss Forge",
            "Protoss Stargate",
            "Stasis Cell/Prison",
            "Protoss Fleet Beacon",
            "Protoss Arbiter Tribunal",
            "Protoss Robotics Support Bay",
            "Protoss Shield Battery",
            "Khaydarin Crystal Formation",
            "Protoss Temple",
            "Xel'Naga Temple",
            "Mineral Field (Type 1)",
            "Mineral Field (Type 2)",
            "Mineral Field (Type 3)",
            "Cave",
            "Cave-in",
            "Cantina",
            "Mining Platform",
            "Independant Command Center",
            "Independant Starport",
            "Independant Jump Gate",
            "Ruins",
            "Kyadarin Crystal Formation",
            "Vespene Geyser",
            "Warp Gate",
            "PSI Disruptor",
            "Zerg Marker",
            "Terran Marker",
            "Protoss Marker",
            "Zerg Beacon",
            "Terran Beacon",
            "Protoss Beacon",
            "Zerg Flag Beacon",
            "Terran Flag Beacon",
            "Protoss Flag Beacon",
            "Power Generator",
            "Overmind Cocoon",
            "Dark Swarm",
            "Floor Missile Trap",
            "Floor Hatch",
            "Left Upper Level Door",
            "Right Upper Level Door",
            "Left Pit Door",
            "Right Pit Door",
            "Floor Gun Trap",
            "Left Wall Missile Trap",
            "Left Wall Flame Trap",
            "Right Wall Missile Trap",
            "Right Wall Flame Trap",
            "Start Location",
            "Flag",
            "Young Chrysalis",
            "Psi Emitter",
            "Data Disc",
            "Khaydarin Crystal",
            "Mineral Cluster Type 1",
            "Mineral Cluster Type 2",
            "Protoss Vespene Gas Orb Type 1",
            "Protoss Vespene Gas Orb Type 2",
            "Zerg Vespene Gas Sac Type 1",
            "Zerg Vespene Gas Sac Type 2",
            "Terran Vespene Gas Tank Type 1",
            "Terran Vespene Gas Tank Type 2",
        };
    }

    public class UnitHPDef : Gettable<UnitHPDef>, SaveableItem {

        private int _value;
        
        public int getIndex() {
            return _value;
        }

        public virtual int getMaxValue() {
            return int.MaxValue;
        }

        public UnitHPDef(int number) {
            _value = number > getMaxValue() ? getMaxValue() : number;
        }

        public static UnitHPDef getDefaultValue() {
            UnitHPDef def = UnitHPDef.getByIndex(0);
            return def;
        }

        public static UnitHPDef getByIndex(int index) {
            return new UnitHPDef(index);
        }

        public virtual TriggerDefinitionPart getTriggerPart(Func<UnitHPDef> getter, Action<UnitHPDef> setter, Func<UnitHPDef> getDefault) {
            return new TriggerDefinitionIntAmount(
                () => new IntDef(getter().getIndex() / 256, false),
                (IntDef def) => {
                    if (def.getIndex() <= getMaxValue()) {
                        setter(new UnitHPDef(def.getIndex() * 256));
                    }
                },
                () => new IntDef(getDefault().getIndex() / 256, false)
           );
        }

        public override string ToString() {
            return (_value / 256).ToString();
        }

        public string ToSaveString() {
            return _value.ToString();
        }
    }

    public class UnitVanillaDef : SaveableItem, Gettable<UnitVanillaDef> {

        private int _id;
        private string _name;
        private string _customName;

        public int UnitID { get { return _id; } }
        public string UnitName { get { return MainWindow.UseCustomName ? _name : _customName; } }

        private static readonly Dictionary<int, UnitVanillaDef> _defs = new Dictionary<int, UnitVanillaDef>();

        public static UnitVanillaDef getByIndex(int index) {
            if (index < AllUnits.Length) {
                return AllUnits[index];
            }
            return new UnitVanillaDef(index);
        }

        public static void __setUnitNameDontUseOutsideOfParser(int index, string unitName) {
            AllUnits[index]._customName = unitName;
        }

        public override string ToString() {
            return UnitName.ToString();
        }

        public static UnitVanillaDef getDefaultValue() {
            return UnitVanillaDef.getByIndex(0);
        }

        public int getMaxValue() {
            return AllUnits.Length - 1;
        }

        public int getIndex() {
            return UnitID;
        }

        public TriggerDefinitionPart getTriggerPart(Func<UnitVanillaDef> getter, Action<UnitVanillaDef> setter, Func<UnitVanillaDef> getDefault) {
            return new TriggerDefinitionGeneralDef<UnitVanillaDef>(getter, setter, getDefault, AllUnits);
        }

        public string ToSaveString() {
            return UnitID.ToString();
        }

        private UnitVanillaDef(int id) {
            _id = id;
            if (id < _Defs.Length) {
                _name = _Defs[id];
            } else {
                _name = "EUD (" + id + ")";
            }
            _customName = _name;
        }

        public static UnitVanillaDef[] AllUnits {
            get {
                if (_allUnits == null) {
                    _allUnits = new UnitVanillaDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        AllUnits[i] = new UnitVanillaDef(i);
                    }
                }
                return _allUnits;
            }
        }

        private static UnitVanillaDef[] _allUnits;

        private static readonly string[] _Defs = {
            "Terran Marine",
            "Terran Ghost",
            "Terran Vulture",
            "Terran Goliath",
            "Goliath Turret",
            "Terran Siege Tank (Tank Mode)",
            "Tank Turret(Tank Mode)",
            "Terran SCV",
            "Terran Wraith",
            "Terran Science Vessel",
            "Gui Montang (Firebat)",
            "Terran Dropship",
            "Terran Battlecruiser",
            "Vulture Spider Mine",
            "Nuclear Missile",
            "Terran Civilian",
            "Sarah Kerrigan (Ghost)",
            "Alan Schezar (Goliath)",
            "Alan Schezar Turret",
            "Jim Raynor (Vulture)",
            "Jim Raynor (Marine)",
            "Tom Kazansky (Wraith)",
            "Magellan (Science Vessel)",
            "Edmund Duke (Siege Tank)",
            "Edmund Duke Turret",
            "Edmund Duke (Siege Mode)",
            "Edmund Duke Turret",
            "Arcturus Mengsk (Battlecruiser)",
            "Hyperion (Battlecruiser)",
            "Norad II (Battlecruiser)",
            "Terran Siege Tank (Siege Mode)",
            "Tank Turret (Siege Mode)",
            "Firebat",
            "Scanner Sweep",
            "Terran Medic",
            "Zerg Larva",
            "Zerg Egg",
            "Zerg Zergling",
            "Zerg Hydralisk",
            "Zerg Ultralisk",
            "Zerg Broodling",
            "Zerg Drone",
            "Zerg Overlord",
            "Zerg Mutalisk",
            "Zerg Guardian",
            "Zerg Queen",
            "Zerg Defiler",
            "Zerg Scourge",
            "Torrarsque (Ultralisk)",
            "Matriarch (Queen)",
            "Infested Terran",
            "Infested Kerrigan",
            "Unclean One (Defiler)",
            "Hunter Killer (Hydralisk)",
            "Devouring One (Zergling)",
            "Kukulza (Mutalisk)",
            "Kukulza (Guardian)",
            "Yggdrasill (Overlord)",
            "Terran Valkyrie Frigate",
            "Mutalisk/Guardian Cocoon",
            "Protoss Corsair",
            "Protoss Dark Templar(Unit)",
            "Zerg Devourer",
            "Protoss Dark Archon",
            "Protoss Probe",
            "Protoss Zealot",
            "Protoss Dragoon",
            "Protoss High Templar",
            "Protoss Archon",
            "Protoss Shuttle",
            "Protoss Scout",
            "Protoss Arbiter",
            "Protoss Carrier",
            "Protoss Interceptor",
            "Dark Templar(Hero)",
            "Zeratul (Dark Templar)",
            "Tassadar/Zeratul (Archon)",
            "Fenix (Zealot)",
            "Fenix (Dragoon)",
            "Tassadar (Templar)",
            "Mojo (Scout)",
            "Warbringer (Reaver)",
            "Gantrithor (Carrier)",
            "Protoss Reaver",
            "Protoss Observer",
            "Protoss Scarab",
            "Danimoth (Arbiter)",
            "Aldaris (Templar)",
            "Artanis (Scout)",
            "Rhynadon (Badlands Critter)",
            "Bengalaas (Jungle Critter)",
            "Unused - Was Cargo Ship",
            "Unused - Was Mercenary Gunship",
            "Scantid (Desert Critter)",
            "Kakaru (Twilight Critter)",
            "Ragnasaur (Ashworld Critter)",
            "Ursadon (Ice World Critter)",
            "Lurker Egg",
            "Raszagal",
            "Samir Duran (Ghost)",
            "Alexei Stukov (Ghost)",
            "Map Revealer",
            "Gerard DuGalle",
            "Zerg Lurker",
            "Infested Duran",
            "Disruption Web",
            "Terran Command Center",
            "Terran Comsat Station",
            "Terran Nuclear Silo",
            "Terran Supply Depot",
            "Terran Refinery",
            "Terran Barracks",
            "Terran Academy",
            "Terran Factory",
            "Terran Starport",
            "Terran Control Tower",
            "Terran Science Facility",
            "Terran Covert Ops",
            "Terran Physics Lab",
            "Unused - Was Starbase?",
            "Terran Machine Shop",
            "Unused - Was Repair Bay?",
            "Terran Engineering Bay",
            "Terran Armory",
            "Terran Missile Turret",
            "Terran Bunker",
            "Norad II",
            "Ion Cannon",
            "Uraj Crystal",
            "Khalis Crystal",
            "Infested Command Center",
            "Zerg Hatchery",
            "Zerg Lair",
            "Zerg Hive",
            "Zerg Nydus Canal",
            "Zerg Hydralisk Den",
            "Zerg Defiler Mound",
            "Zerg Greater Spire",
            "Zerg Queen's Nest",
            "Zerg Evolution Chamber",
            "Zerg Ultralisk Cavern",
            "Zerg Spire",
            "Zerg Spawning Pool",
            "Zerg Creep Colony",
            "Zerg Spore Colony",
            "Unused Zerg Building",
            "Zerg Sunken Colony",
            "Zerg Overmind (With Shell)",
            "Zerg Overmind",
            "Zerg Extractor",
            "Mature Chrysalis",
            "Zerg Cerebrate",
            "Zerg Cerebrate Daggoth",
            "Unused Zerg Building 5",
            "Protoss Nexus",
            "Protoss Robotics Facility",
            "Protoss Pylon",
            "Protoss Assimilator",
            "Unused Protoss Building",
            "Protoss Observatory",
            "Protoss Gateway",
            "Unused Protoss Building",
            "Protoss Photon Cannon",
            "Protoss Citadel of Adun",
            "Protoss Cybernetics Core",
            "Protoss Templar Archives",
            "Protoss Forge",
            "Protoss Stargate",
            "Stasis Cell/Prison",
            "Protoss Fleet Beacon",
            "Protoss Arbiter Tribunal",
            "Protoss Robotics Support Bay",
            "Protoss Shield Battery",
            "Khaydarin Crystal Formation",
            "Protoss Temple",
            "Xel'Naga Temple",
            "Mineral Field (Type 1)",
            "Mineral Field (Type 2)",
            "Mineral Field (Type 3)",
            "Cave",
            "Cave-in",
            "Cantina",
            "Mining Platform",
            "Independant Command Center",
            "Independant Starport",
            "Independant Jump Gate",
            "Ruins",
            "Kyadarin Crystal Formation",
            "Vespene Geyser",
            "Warp Gate",
            "PSI Disruptor",
            "Zerg Marker",
            "Terran Marker",
            "Protoss Marker",
            "Zerg Beacon",
            "Terran Beacon",
            "Protoss Beacon",
            "Zerg Flag Beacon",
            "Terran Flag Beacon",
            "Protoss Flag Beacon",
            "Power Generator",
            "Overmind Cocoon",
            "Dark Swarm",
            "Floor Missile Trap",
            "Floor Hatch",
            "Left Upper Level Door",
            "Right Upper Level Door",
            "Left Pit Door",
            "Right Pit Door",
            "Floor Gun Trap",
            "Left Wall Missile Trap",
            "Left Wall Flame Trap",
            "Right Wall Missile Trap",
            "Right Wall Flame Trap",
            "Start Location",
            "Flag",
            "Young Chrysalis",
            "Psi Emitter",
            "Data Disc",
            "Khaydarin Crystal",
            "Mineral Cluster Type 1",
            "Mineral Cluster Type 2",
            "Protoss Vespene Gas Orb Type 1",
            "Protoss Vespene Gas Orb Type 2",
            "Zerg Vespene Gas Sac Type 1",
            "Zerg Vespene Gas Sac Type 2",
            "Terran Vespene Gas Tank Type 1",
            "Terran Vespene Gas Tank Type 2",
            "ID:228",
            "Any unit",
            "Men",
            "Buildings",
            "Factories",
        };
    }

    public class UnitsQuantity : SaveableItem {

        private int _amount;

        public string Amount { get { return _amount == -1 ? "All Units" : _amount.ToString(); } }

        public int RawAmount { get { return _amount; } }

        public override string ToString() {
            return _amount.ToString();
        }

        public string ToSaveString() {
            return _amount == -1 ? "All" : _amount.ToString();
        }

        public UnitsQuantity(int amount) {
            _amount = amount;
        }

        public static SaveableItem getDefaultValue() {
            return new UnitsQuantity(-1);
        }
    }

    public class UpgradeDef : Gettable<UpgradeDef>, TechOrUpgradeDef<UpgradeDef> {

        private int _index;
        private string _name;

        public int UpgradeIndex { get { return _index; } }

        public string UpgradeName { get { return _name; } }

        public static UpgradeDef getByIndex(int index) {
            if (index < AllUpgrades.Length) {
                return AllUpgrades[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return UpgradeName;
        }

        public int getMaxValue() {
            return AllUpgrades.Length - 1;
        }

        public int getIndex() {
            return UpgradeIndex;
        }


        public TriggerDefinitionPart getTriggerPart(Func<UpgradeDef> getter, Action<UpgradeDef> setter, Func<UpgradeDef> getDefault) {
            return new TriggerDefinitionGeneralDef<UpgradeDef>(getter, setter, getDefault, AllUpgrades);
        }

        public int getMemoryIndex() {
            if (_index >= 0 && _index < SCUpgradeDef.AllUpgrades.Length) {
                return 0;
            } else {
                return 1;
            }
        }

        public int getFirstPartLength() {
            return SCUpgradeDef.AllUpgrades.Length;
        }

        private UpgradeDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid upgrade";
            }
        }

        public static UpgradeDef[] AllUpgrades {
            get {
                if (_allUpgrades == null) {
                    _allUpgrades = new UpgradeDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allUpgrades[i] = new UpgradeDef(i);
                    }
                }
                return _allUpgrades;
            }
        }

        private static UpgradeDef[] _allUpgrades;

        private static readonly string[] _Defs = {
            "Terran Infantry Armor",
            "Terran Vehicle Plating",
            "Terran Ship Plating",
            "Zerg Caraspace",
            "Zerg Flyer Caraspace",
            "Protoss Armor",
            "Protoss Plating",
            "Terran Infantry Weapons",
            "Terran Vehicle Weapons",
            "Terran Ship Weapons",
            "Zerg Melee Attacks",
            "Zerg Missile Attacks",
            "Zerg Flyer Attacks",
            "Protoss Ground Weapons",
            "Protoss Air Weapons",
            "Protoss Plasma Shields",
            "U-238 Shells",
            "Ion Thrusters",
            "Burst Lasers (unused)",
            "Titan Reactor (SV+50)",
            "Ocular Implantats",
            "Moebius Reactor (Ghost +50)",
            "Apollo Reactor (Wraith +50)",
            "Colossus Reactor (BC +50)",
            "Ventral Sacs",
            "Anennae",
            "Pneumatized Caraspace",
            "Metabolic Boost",
            "Adrenal Glads",
            "Muscular Augments",
            "Grooved Spines",
            "Gamete Meiosis (Queen +50)",
            "Metasynaptic Node (Defiler +50)",
            "Singularity Charge",
            "Leg Enhancements",
            "Scarab Damage",
            "Reaver Capacity",
            "Gravitic Drive",
            "Sensor Array",
            "Gravity Boosters",
            "Khaydarin Amulet (HT +50)",
            "Apial Sensors",
            "Gravitic Thrusters",
            "Carrier Capacity",
            "Khaydarin Core (Arbiter +50)",
            "Unknown upgrade45",
            "Unknown Upgrade46",
            "Argus Jewel (Corsair +50)",
            "Unknown Upgrade48",
            "Argus Talisman (DA +50)",
            "Unknown upgrade50",
            "Caduceus Reactor (Medic +50)",
            "Chtinous Plating",
            "Anabolic Synthesis",
            "Charon Booster",
            "Unknown Upgrade55",
            "Unknown Upgrade56",
            "Unknown Upgrade57",
            "Unknown Upgrade58",
            "Unknown Upgrade59",
            "Nothing"
        };
    }

    public class WeaponDamageTypeDef : Gettable<WeaponDamageTypeDef> {

        private string _name;

        private int _index;

        public int TypeIndex { get { return _index; } }

        public string TypeName { get { return _name; } }

        public static WeaponDamageTypeDef getByIndex(int index) {
            if (index < AllTypes.Length) {
                return AllTypes[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return TypeName;
        }

        public int getMaxValue() {
            return AllTypes.Length - 1;
        }

        public int getIndex() {
            return TypeIndex;
        }


        public TriggerDefinitionPart getTriggerPart(Func<WeaponDamageTypeDef> getter, Action<WeaponDamageTypeDef> setter, Func<WeaponDamageTypeDef> getDefault) {
            return new TriggerDefinitionGeneralDef<WeaponDamageTypeDef>(getter, setter, getDefault, AllTypes);
        }


        private WeaponDamageTypeDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid upgrade";
            }
        }

        public static WeaponDamageTypeDef[] AllTypes {
            get {
                if (_allTypes == null) {
                    _allTypes = new WeaponDamageTypeDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allTypes[i] = new WeaponDamageTypeDef(i);
                    }
                }
                return _allTypes;
            }
        }

        private static WeaponDamageTypeDef[] _allTypes;

        private static readonly string[] _Defs = {
            "Independent",
            "Explosive",
            "Concussive",
            "Normal",
            "Ignore Armor"
        };

    }

    public class WeaponDef :Gettable<WeaponDef> {

        private int _index;
        private string _name;

        public int WeaponIndex { get { return _index; } }

        public string WeaponName { get { return _name; } }

        public static WeaponDef getByIndex(int index) {
            if (index < AllWeapons.Length) {
                return AllWeapons[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return WeaponName;
        }

        public int getMaxValue() {
            return AllWeapons.Length - 1;
        }

        public int getIndex() {
            return WeaponIndex;
        }

        public TriggerDefinitionPart getTriggerPart(Func<WeaponDef> getter, Action<WeaponDef> setter, Func<WeaponDef> getDefault) {
            return new TriggerDefinitionGeneralDef<WeaponDef>(getter, setter, getDefault, AllWeapons);
        }

        private WeaponDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid weapon";
            }
        }

        public static WeaponDef[] AllWeapons {
            get {
                if (_allWeapons == null) {
                    _allWeapons = new WeaponDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allWeapons[i] = new WeaponDef(i);
                    }
                }
                return _allWeapons;
            }
        }

        private static WeaponDef[] _allWeapons;

        private static readonly string[] _Defs = {
            "Gauss Rifle (Normal)",
            "Gauss Rifle (Jim Raynor-Marine)",
            "C-10 Concussion Rifle (Normal)",
            "C-10 Concusion Rifle (Sarah Kerrigan)",
            "Fragmentation Grenade (Normal)",
            "Fragmentation Grenade (Jim Raynor-Vulture)",
            "Spider Mines",
            "Twin Autocannons (Normal)",
            "Hellfire Missile Pack (Normal)",
            "Twin Autocanons (Normal)",
            "Hellfire missile Pack (Alan Schezar)",
            "Arclite Cannon (Normal)",
            "Arclite Cannon (Admunt Duke)",
            "Fusion Cutter",
            "Fusion Cutter (Harvest)",
            "Gemini Missiles (Normal)",
            "Burst Lasers (Normal)",
            "Gemini Missiles (Tom Kazansky)",
            "Burst Lasers (Tom Kazansky)",
            "ATS Laser Battery (Normal)",
            "ATA Laser Battery (Normal)",
            "ATS Laser Battery (Norad II+Mengsk+Dugaile)",
            "ATA Laser Battery (Norad II+Mengsk+Dugaile)",
            "ATS Laser Battery (Hyperion)",
            "ATA Laser Battery (Hyperion) ",
            "Flame Thrower (Normal)",
            "Flame Thrower (Gui Montag)",
            "Arclite Shock Cannon (Normal)",
            "ARclite Shock Cannon (Admund Duke)",
            "Longbolt Missiles",
            "Yamato Gun",
            "Nuclear Missile",
            "Lockdown",
            "EMP Shockwave",
            "Irradiate",
            "Claws (Normal)",
            "Claws (Devouring one)",
            "Claws (Infested Kerrigan)",
            "Needle Spines (Normal)",
            "Needle Spines (Hunter Killer)",
            "Kaiser Blades (Normal)",
            "Kaiser Blades (Torrasque)",
            "Toxic Spores (Broodling)",
            "Spines",
            "Spines (Harvest)",
            "Acid Spray (Unused)",
            "Acid Spore (Normal)",
            "Acid Spore (Kukulza-Guardian)",
            "Glave Wurm (Normal)",
            "Glave Wurm (Kukulza-Mutalisk)",
            "Venom (Unused-Defiler)",
            "Venom (Unused-Defiler Hero)",
            "Seeker Spores",
            "Subterranean Tentacle",
            "Suicide (Infested Terran)",
            "Suicide (Scourge)",
            "Parasite",
            "Spawn Broodlings",
            "Ensnare",
            "Dark Swarm",
            "Plague",
            "Consume",
            "Particle Beam",
            "Particle Beam (Harvest)",
            "Psi Blades (Normal)",
            "Psi Blades (Fenix-Zealot)",
            "Phase Disruptor (Normal)",
            "Phase Disruptor (Fenix-Dragoon)",
            "Psi Assault",
            "Psi Assault (TassadarAldaris)",
            "Psionic Shockwave (Normal)",
            "Psionic Shockwave (Tassadar/Zeratul Archon)",
            "Unknown 72",
            "Dual Photon Blasters (Normal)",
            "Anti-Matter Missiles (Normal)",
            "Dual Photon Blasters (Mojo)",
            "Anti-Matter Missiles (Mojo)",
            "Phase Disruptor Cannon (Normal)",
            "Phase Disruptor Cannon (Danimoth)",
            "Pulse Cannon",
            "STS Photon Cannon",
            "STA Photon Cannon",
            "Scarab",
            "Statis Field",
            "Psi Storm",
            "Warp Blades (Zeratul)",
            "Warp Blades (Dark Templar Hero)",
            "Missiles (Unused)",
            "Laser Batteryl (Unused)",
            "Tormentor Missiles (Unused)",
            "Bombs (Unused)",
            "Raider Gun (Unused)",
            "Laser Battery2 (unused)",
            "Laser Battery3 (Unused)",
            "Dual Photon Blasters (unused)",
            "Flechette Grenade (Unused)",
            "Twin Autocannons (Floor Trap)",
            "Hellfire Missile Pack (Wall Trap)",
            "Flame Thrower (Wall Trap)",
            "Hellfire Missile Pack (Floor Trap)",
            "Neutron Flare",
            "Disruption Web",
            "Restoration",
            "Halo Rockets",
            "Corrosive Acid",
            "Mind Control",
            "Feedback",
            "Optical Flare",
            "MaelStrom",
            "Subterranean Spines",
            "Gaus Rifle0 (Unused)",
            "Warp Blades (Normal)",
            "C-10 Concusion Rifle (Samir Duran)",
            "C-10 Concusion Rifle (Infested Duran)",
            "Dual Photon Blasters (Artanis)",
            "Anti-matter Missiles (Artanis)",
            "C-10 Concusion Rifle (Alexei Stukov)",
            "Gauss Rifle1 (Unused)",
            "Gauss Rifle2 (Unused)",
            "Gauss Rifle3 (Unused)",
            "Gauss Rifle4 (Unused)",
            "Gauss Rifle5 (Unused)",
            "Gauss Rifle6 (Unused)",
            "Gauss Rifle7 (Unused)",
            "Gauss Rifle8 (Unused)",
            "Gauss Rifle9 (Unused)",
            "Gauss Rifle10 (Unused)",
            "Gauss Rifle11 (Unused)",
            "Gauss Rifle12 (Unused)",
            "Gauss Rifle13 (Unused)",
            "[None]"
        };
    }

    public class WeaponEffectDef : Gettable<WeaponEffectDef> {

        private int _index;
        private string _name;

        public int EffectIndex { get { return _index; } }

        public string EffectName { get { return _name; } }

        public static WeaponEffectDef getByIndex(int index) {
            if (index < AllEffects.Length) {
                return AllEffects[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return EffectName;
        }

        public int getMaxValue() {
            return AllEffects.Length - 1;
        }

        public int getIndex() {
            return EffectIndex;
        }


        public TriggerDefinitionPart getTriggerPart(Func<WeaponEffectDef> getter, Action<WeaponEffectDef> setter, Func<WeaponEffectDef> getDefault) {
            return new TriggerDefinitionGeneralDef<WeaponEffectDef>(getter, setter, getDefault, AllEffects);
        }

        private WeaponEffectDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid effect";
            }
        }

        public static WeaponEffectDef[] AllEffects {
            get {
                if (_allEffects == null) {
                    _allEffects = new WeaponEffectDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allEffects[i] = new WeaponEffectDef(i);
                    }
                }
                return _allEffects;
            }
        }

        private static WeaponEffectDef[] _allEffects;

        private static readonly string[] _Defs = {
            "None ",
            "Normal Hit",
            "Splash (Radial)",
            "Splash (Enemy)",
            "Lockdown",
            "Nuclear Missile",
            "Parasite",
            "Broodlings",
            "EMP Shockwave",
            "Irradiate",
            "Ensnare",
            "Plague",
            "Stasis Field",
            "Dark Swarm",
            "Consume",
            "Yamato Gun",
            "Restoration",
            "Disruption Web",
            "Corrosive Acid",
            "Mind Control",
            "Feedback",
            "Optical Flare",
            "Maelstrom",
            "Unknown (Crash)",
            "Splash (Air)",
        };
    }

    public class WeaponTargetFlags : Gettable<WeaponTargetFlags>, SaveableItem {

        private int _value;

        public int getIndex() {
            return _value;
        }

        public int getMaxValue() {
            return int.MaxValue;
        }

        public static WeaponTargetFlags getByIndex(int index) {
            return new WeaponTargetFlags(index);
        }

        private WeaponTargetFlags(int value) {
            _value = value;
        }

        public TriggerDefinitionPart getTriggerPart(Func<WeaponTargetFlags> getter, Action<WeaponTargetFlags> setter, Func<WeaponTargetFlags> getDefault) {
            return new TriggerDefinitionWeaponTargetFlagsDef(getter, setter, getDefault);
        }

        public string ToSaveString() {
            throw new NotImplementedException();
        }

        public override string ToString() {
            return "Target Flags";
        }
    }

    public class IScriptDef : Gettable<IScriptDef> {

        private int _index;
        private string _name;

        public int IScriptID { get { return _index; } }

        public string IScriptName { get { return _name; } }

        public static IScriptDef getByIndex(int index) {
            if (index < AllIScripts.Length) {
                return AllIScripts[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return IScriptName;
        }

        public int getMaxValue() {
            return AllIScripts.Length - 1;
        }

        public int getIndex() {
            return IScriptID;
        }

        public TriggerDefinitionPart getTriggerPart(Func<IScriptDef> getter, Action<IScriptDef> setter, Func<IScriptDef> getDefault) {
            return new TriggerDefinitionGeneralDef<IScriptDef>(getter, setter, getDefault, AllIScripts);
        }

        private IScriptDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid IScript";
            }
        }

        public static IScriptDef[] AllIScripts {
            get {
                if (_allScripts == null) {
                    _allScripts = new IScriptDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allScripts[i] = new IScriptDef(i);
                    }
                }
                return _allScripts;
            }
        }

        private static IScriptDef[] _allScripts;

        private static readonly string[] _Defs = {
            "Scourge",
            "Unknown1",
            "Scourge Death",
            "Scourge Explosion",
            "Broodling",
            "Broodling Remnants",
            "Infested Terran",
            "Infested Terran Explosion",
            "Guardian Cocoon",
            "Defiler",
            "Defiler Remnants",
            "Drone",
            "Drone Remnants",
            "Egg",
            "Egg Remnants",
            "Guardian",
            "Guardian Birth",
            "Guardian Death",
            "Hydralisk",
            "Hydralisk Remnants",
            "Infested Kerrigan",
            "Larva",
            "Larva Remnants",
            "Mutalisk",
            "Mutalisk Death",
            "Overlord",
            "Overlord Death",
            "Queen",
            "Queen Death",
            "Ultralisk",
            "Ultralisk Remnants",
            "Zergling",
            "Zergling Remnants",
            "Zerg Air Death Explosion",
            "Zerg Building Explosion",
            "Unknown35",
            "Zerg Birth",
            "Egg Spawn",
            "Cerebrate",
            "Infested Command Center",
            "Spawning Pool",
            "Evolution Chamber",
            "Creep Colony",
            "Hatchery",
            "Hive",
            "Lair",
            "Sunken Colony",
            "Mature Chrysalis",
            "Greater Spire",
            "Defiler Mound",
            "Queen Nest",
            "Nydus Canal",
            "Overmind(with Shell)",
            "Overmind Remnants",
            "Overmind(without Shell)",
            "Ultralisk Cavern",
            "Extractor",
            "Hydralisk Den",
            "Spire",
            "Spore Colony",
            "Infested Command Center Overlay",
            "Zerg Construction(Small)",
            "Zerg Construction(Medium)",
            "Zerg Building Morph",
            "Zerg Construction(Large)",
            "Zerg Building Spawn",
            "Battlecruiser",
            "Civilian",
            "Dropship",
            "Firebat",
            "Ghost",
            "Ghost Remnants",
            "Ghost Death",
            "Nuke Beam",
            "Nuke Target Dot",
            "Goliath(Base)",
            "Goliath(Turret)",
            "Sarah Kerrigan",
            "Marine",
            "Marine Remnants",
            "Marine Death",
            "Scanner Sweep",
            "Wraith",
            "Wraith Afterburners",
            "SCV",
            "Unknown85",
            "Vulture",
            "Spider Mine",
            "Science Vessel(Base)",
            "Science Vessel(Turret)",
            "Siege Tank(Tank) Base",
            "Siege Tank(Tank) Turret",
            "Siege Tank(Siege) Base",
            "Siege Tank(Siege) Turret",
            "Academy",
            "Academy Overlay",
            "Barracks",
            "Armory",
            "Armory Overlay",
            "Comsat Station",
            "Comsat Connector",
            "Comsat Overlay",
            "Command Center",
            "Command Center Overlay",
            "Crashed Battlecruiser",
            "Supply Depot",
            "Supply Depot Overlay",
            "Control Tower",
            "Control Tower Connector",
            "Control Tower Overlay",
            "Unknown110",
            "Factory",
            "Factory Overlay",
            "Covert Ops",
            "Covert Ops Connector",
            "Covert Ops Overlay",
            "Ion Cannon",
            "Machine Shop",
            "Machine Shop Connector",
            "Missile Turret(Base)",
            "Missile Turret(Turret)",
            "Physics Lab",
            "Physics Lab Connector",
            "Bunker",
            "Bunker Overlay",
            "Refinery",
            "Science Facility",
            "Science Facility Overlay",
            "Nuclear Silo",
            "Nuclear Silo Connector",
            "Nuclear Silo Overlay",
            "Nuclear Missile",
            "Unknown132",
            "Nuke Explosion",
            "Starport",
            "Starport Overlay",
            "Engineering Bay",
            "Engineering Bay Overlay",
            "Terran Construction(Large)",
            "Terran Construction(Medium)",
            "Terran Construction(Small)",
            "Addon Construction",
            "Explosion(Small)",
            "Explosion(Medium)",
            "Explosion(Large)",
            "Building Rubble Header",
            "Arbiter",
            "Archon Energy",
            "Archon Being",
            "Archon Swirl",
            "Unknown150",
            "Carrier",
            "Dark Templar(Hero)",
            "Dragoon",
            "Dragoon Remnants",
            "Interceptor",
            "Probe",
            "Shuttle",
            "High Templar",
            "Reaver",
            "Scarab",
            "Scout",
            "Scout Engines",
            "Zealot",
            "Zealot Death",
            "Observer",
            "Templar Archives",
            "Assimilator",
            "Observatory",
            "Unknown169",
            "Citadel Of Adun",
            "Forge",
            "Forge Overlay",
            "Gateway",
            "Cybernetics Core",
            "Cybrnetics Core Overlay",
            "Khaydarin Crystal Formation",
            "Nexus",
            "Nexus Overlay",
            "Photon Cannon",
            "Arbiter Tribunal",
            "Pylon",
            "Robotics Facility",
            "Shield Battery",
            "Shield Battery Overlay",
            "Stargate",
            "Stargate Overlay",
            "Stasis Cell/Prison",
            "Robotics Support Bay",
            "Temple",
            "Fleet Beacon",
            "Warp Anchor",
            "Warp Flash Header",
            "Warp Texture",
            "Unknown194",
            "Unknown195",
            "Unknown196",
            "Unknown197",
            "Ragnasaur(Ashworld Critter)",
            "Rhynadon(Badlands Critter)",
            "Bengalaas(Jungle Critter)",
            "Vespene Geyser",
            "Vespene Geyser2",
            "Vespene Geyser Shadow",
            "Mineral Field Type1",
            "Mineral Field Type2",
            "Mineral Field Type3",
            "Unknown207",
            "Zerg Beacon Overlay",
            "Terran Beacon Overlay",
            "Protoss Beacon Overlay",
            "Zerg Beacon",
            "Protoss Beacon",
            "Terran Beacon",
            "Unknwon214",
            "Powerups Shadow Header",
            "Flag",
            "Psi Emitter",
            "Data Disk",
            "Crystals Shadows",
            "Young Chrysalis",
            "Ore Chunk",
            "Ore Chunk2",
            "Gas Sac",
            "Gas Sac2",
            "Gas Orb",
            "Gas Orb2",
            "Gas Tank",
            "Gas Tank2",
            "Archon Overlay",
            "Particle Beam Hit",
            "Dual Photon Blaster Hit",
            "Anti-Matter Missile",
            "Pulse Cannon",
            "Phase Disruptor",
            "STA/STS Photon Cannon Overlay",
            "Psionic Storm",
            "Fusion Cutter Hit",
            "Gauss Rifle Hit",
            "Gemini Missiles",
            "Longbolt Missile",
            "C-10 Canister Rifle Hit",
            "Fragmentation Grenade",
            "ATA/ATS Laser Battery/Burst Lasers",
            "Unknown244",
            "Lockdown Hit",
            "Arclite Shock Cannon Hit",
            "Yamato Gun",
            "Yamato Gun Trail",
            "Lockdown/EMP Shockwave Missile",
            "Siege Tank(Tank) Turret Overlay",
            "Siege Tank(Siege) Turret Overlay",
            "Science Vessel Overlay",
            "Hallucination Hit",
            "Unknown254",
            "Unknown255",
            "Needle Spines Hit",
            "Venom (Unused)",
            "Subterranean Tentacle",
            "Venom Hit (Unused)",
            "Acid Spore",
            "Acid Spore Hit",
            "Guardian Attack Overlay",
            "Unknown263",
            "Glave Wurm",
            "Glave Wurm Hit",
            "Seeker Spores",
            "Queen Spell Holder",
            "Psionic Shockwave Hit",
            "Glave Wurm Trail",
            "Seeker Spores Overlay",
            "Acid Spray",
            "Unknown272",
            "Longbolt\\Halo\\Gemini Missiles Trail",
            "Burowing Dust",
            "Shadow Header",
            "Shield Overlay",
            "Unknown277",
            "Double Explosion",
            "Nuclear Missile Death",
            "Spider Mine Explosion",
            "Vespene Geyser Smokes",
            "Unknown282",
            "Fragmentation Grenade Hit",
            "Grenade Shot Smoke",
            "Phase Disruptor/Anti-Matter Missile Hit",
            "Scarab/Anti-Matter Missile Overlay",
            "Scarab Hit",
            "Cursor Marker",
            "Circle Marker",
            "Carrier Engines",
            "Engines/Glow Header",
            "White Circle",
            "Battlecruiser Attack Overlay",
            "ATA/ATS Laser Battery/Burst Lasers Hit",
            "Unknown295",
            "Plague Cloud",
            "Plague Overlay",
            "Consume",
            "Dark Swarm",
            "Defensive Matrix Overlay",
            "Defensive Matrix Hit",
            "Ensnare",
            "Ensnare Overlay",
            "Irradiate",
            "Recall Field",
            "Stasis Field Overlay",
            "Stasis Field Hit",
            "Recharge Shields(Large)",
            "Recharge Shields(Small)",
            "High Templar Glow",
            "Needle Spines Overlay",
            "Flamethrower",
            "Gemini Missiles Explosion",
            "Yamato Gun Overlay",
            "Yamato Gun Hit",
            "Unknown316",
            "Psionic Storm Part Variant1",
            "Psionic Storm Part Variant2",
            "EMP Shockwave Hit(Part1)",
            "EMP Shockwave Hit(Part2)",
            "Hallucination Death",
            "Flames(Small)",
            "Unknown323",
            "Bleeding(Small) Variant1",
            "Bleeding(Small) Variant2",
            "Flames(Large)",
            "Unknown327",
            "Bleeding(Large) Variant1",
            "Bleeding(Large) Variant2",
            "Dust Variant1",
            "Dust Variant2",
            "Confirm Circle",
            "Psi Field Type1",
            "Psi Field Type2",
            "Start Location",
            "Doodad Header",
            "Doodad Header(secondary)",
            "Space Platform Doodad",
            "Space Platform Doodad2",
            "Installation Doodad",
            "Installation Doodad2",
            "Installation Right Wall Fans",
            "Installation Left Wall Fans",
            "Installation Gear",
            "Floor Missile Trap",
            "Floor Missile Trap Turret",
            "Floor Gun Trap",
            "Wall Missile Trap Type1",
            "Wall Missile Trap Typet2",
            "Wall Flame Trap Type1",
            "Wall Flame Trap Type2",
            "Map Revealer",
            "Lurker Egg",
            "Lurker",
            "Unknown355",
            "Lurker Remnants",
            "Devourer",
            "Devourer Birth",
            "Devourer Death",
            "Medic",
            "Medic Remnants",
            "Valkyrie",
            "Unknown363",
            "Unknown364",
            "Dark Archon Energy",
            "Dark Archon Being",
            "Dark Archon Swirl",
            "Dark Archon Death",
            "Corsair",
            "Corsair Engines",
            "Unknown371",
            "Dark Templar(Unit)",
            "Neutron Flare",
            "Disruption Web",
            "Scantid(Desert Critter)",
            "Kakaru(Twilight Critter)",
            "Ursadon(Ice Critter)",
            "Halo Rocket",
            "Optical Flare",
            "Subterranean Spines",
            "Corrosive Acid",
            "Corrosive Acid Hit",
            "Acid Spores (1) Overlay",
            "Acid Spores (2-3) Overlay",
            "Acid Spores (4-5) Overlay",
            "Acid Spores (6-9) Overlay",
            "Ice Doodad",
            "Doodad Shadows Header (BW)",
            "Restoration Hit",
            "Mind Control Hit",
            "Optical Flare Hit",
            "Feedback",
            "Maelstorm Overlay",
            "Unknown394",
            "Unknown395",
            "Desert Doodad",
            "Desert Doodad2",
            "Desert Doodad3",
            "Desert Doodad Overlay",
            "Desert Doodad",
            "Twilight Doodad",
            "Twilight Doodad",
            "Twilight Doodad",
            "Twilight Doodad",
            "Overmind Cocoon",
            "Warp Gate",
            "Psi Disrupter",
            "Power Generator",
            "Warp Gate Overlay",
            "Xel'Naga Temple",
            "Maelstrom Hit"
        };
    }

    public class MovementControlDef : Gettable<MovementControlDef> {

        private int _index;
        private string _name;

        public int MovementControlID { get { return _index; } }

        public string MovementControlName { get { return _name; } }

        public static MovementControlDef getByIndex(int index) {
            if (index < AllMovementControls.Length) {
                return AllMovementControls[index];
            }
            throw new NotImplementedException();
        }

        public override string ToString() {
            return MovementControlName;
        }

        public int getMaxValue() {
            return AllMovementControls.Length - 1;
        }

        public int getIndex() {
            return MovementControlID;
        }


        public TriggerDefinitionPart getTriggerPart(Func<MovementControlDef> getter, Action<MovementControlDef> setter, Func<MovementControlDef> getDefault) {
            return new TriggerDefinitionGeneralDef<MovementControlDef>(getter, setter, getDefault, AllMovementControls);
        }

        private MovementControlDef(int index) {
            _index = index;
            if (index < _Defs.Length) {
                _name = _Defs[index];
            } else {
                _name = "Invalid effect";
            }
        }

        public static MovementControlDef[] AllMovementControls {
            get {
                if (_allMovementControls == null) {
                    _allMovementControls = new MovementControlDef[_Defs.Length];
                    for (int i = 0; i < _Defs.Length; i++) {
                        _allMovementControls[i] = new MovementControlDef(i);
                    }
                }
                return _allMovementControls;
            }
        }

        private static MovementControlDef[] _allMovementControls;

        private static readonly string[] _Defs = {
            "Flingy.dat Control",
            "Partially Mobile, Weapon",
            "IScript.bin Control"
        };
    }
}
