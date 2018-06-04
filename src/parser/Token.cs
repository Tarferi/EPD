using System;
using System.Windows;

namespace StarcraftEPDTriggers.src {
    public class Token {

        private string content;

        public static string[] StringTable;
        public static string[] ExtendedStringTable;
       
        public Token(string content) {
            this.content = content;
        }

        public string getContent() {
            return content;
        }

        private bool iss(string s) {
            return content.ToLower().Equals(s.ToLower());
        }

        private bool issn(int s) {
            return content.Equals(s.ToString());
        }

        public int toInt() {
            if (this is NumToken) {
                uint out1;
                if(uint.TryParse(content, out out1)) {
                    return (int)out1;
                } else {
                    return int.Parse(content);
                }
            } else if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isAll()) {
                    return -1;
                }
            }
            throw new NotImplementedException();
        }

        public EnableState toEnableState() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isDisabled()) {
                    return EnableState.Disable;
                } else if (ct.isEnabled()) {
                    return EnableState.Enable;
                } else if (ct.isToggle()) {
                    return EnableState.Toggle;
                }
            }
            throw new NotImplementedException();
        }

        public SwitchSetState toSwitchSetState() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isClear()) {
                    return SwitchSetState.Clear;
                } else if (ct.isRandomize()) {
                    return SwitchSetState.Randomize;
                } else if (ct.isSet()) {
                    return SwitchSetState.Set;
                } else if (ct.isToggle()) {
                    return SwitchSetState.Toggle;
                }
            }
            throw new NotImplementedException();
        }

        public Resources toResources() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isOre()) {
                    return Resources.Ore;
                } else if (ct.isGas()) {
                    return Resources.Gas;
                } else if (ct.isOreAndGas()) {
                    return Resources.OreAndGas;
                }
            }
            throw new NotImplementedException();
        }

        public AllianceDef toAlliance() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isAlliedVictory()) {
                    return AllianceDef.AlliedVictory;
                } else if (ct.isAlly()) {
                    return AllianceDef.Ally;
                } else if (ct.isEnemy()) {
                    return AllianceDef.Enemy;
                }
            }
            throw new NotImplementedException();
        }

        public MessageType getMessageType() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isAlwaysDisplay()) {
                    return MessageType.ALwaysDisplay;
                } else if (ct.isDontAlwaysDisplay()) {
                    return MessageType.DontAlwaysDisplay;
                }
            }
            throw new NotImplementedException();
        }

        public Order toOrder() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isMove()) {
                    return Order.Move;
                } else if (ct.isAttack()) {
                    return Order.Attack;
                } else if (ct.isPatrol()) {
                    return Order.Patrol;
                }
            }
            throw new NotImplementedException();
        }

        public SetQuantifier toSetQuantifier() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isAdd()) {
                    return SetQuantifier.Add;
                } else if (ct.isSubtract()) {
                    return SetQuantifier.Subtract;
                } else if (ct.isSetTo()) {
                    return SetQuantifier.SetTo;
                }
            }
            throw new NotImplementedException();
        }

        public StringDef toStringDef() {
            if (this is StringToken) {
                string dtr = content;
                return new StringDef(dtr);
            } else if(this is NumToken) { // Index to string table
                int index = this.toInt();
                if (index >= 0 && index < StringTable.Length) {
                    return new StringDef(StringTable[index]);
                } else {
                    string undef = "<Undefined>";
                    MessageBox.Show("Invalid string found.\nString at index " + index + " is no within range of defined strings.\nString changed to \""+ undef + "\"", "Trigger Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return new StringDef(undef);
                }
            }
            throw new NotImplementedException();
        }

        public Quantifier toQuantifier() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isAtLeast()) {
                    return Quantifier.AtLeast;
                } else if (ct.isAtMost()) {
                    return Quantifier.AtMost;
                } else if (ct.isExactly()) {
                    return Quantifier.Exactly;
                }
            }
            throw new NotImplementedException();
        }

        public LocationDef toLocationDef() {
            if (this is NumToken) {
                int index = this.toInt();
                return LocationDef.getByIndex(index);
            }
            throw new NotImplementedException();
        }

        public PlayerDef toPlayerDef() {
            if (this is NumToken) {
                int index = this.toInt();
                return PlayerDef.getByIndex(index);
            }
            throw new NotImplementedException();
        }

        public ScoreBoard toScoreBoard() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isBuildings()) {
                    return ScoreBoard.Buildings;
                } else if (ct.isCustom()) {
                    return ScoreBoard.Custom;
                } else if (ct.isKills()) {
                    return ScoreBoard.Kills;
                } else if (ct.isKillsAndRazings()) {
                    return ScoreBoard.KillsAndRazings;
                } else if (ct.isRazings()) {
                    return ScoreBoard.Razings;
                } else if (ct.isTotal()) {
                    return ScoreBoard.Total;
                } else if (ct.isUnits()) {
                    return ScoreBoard.Units;
                } else if (ct.isUnitsAndBuildings()) {
                    return ScoreBoard.UnitsAndBuildings;
                }
            }
            throw new NotImplementedException();
        }

        public SwitchState toSwitchState() {
            if (this is CommandToken) {
                CommandToken ct = this as CommandToken;
                if (ct.isSet()) {
                    return SwitchState.Set;
                } else if (ct.isNotSet()) {
                    return SwitchState.NotSet;
                }
            }
            throw new NotImplementedException();
        }

        internal int toBinaryInt() {
            if (content.Length == 32) {
                int result = 0;
                foreach(char chr in content) {
                    byte b;
                    if(chr == '0') {
                        b = 0;
                    } else if(chr == '1') {
                        b = 1;
                    } else {
                        throw new NotImplementedException();
                    }
                    result <<= 1;
                    result |= b;
                }
                return result;
            }
            throw new NotImplementedException();
        }
    }

    class LeftBracket : Token { public LeftBracket() : base("(") { } }

    class RightBracket : Token { public RightBracket() : base(")") { } }

    class Semicolon : Token { public Semicolon() : base(";") { } }

    class Comma : Token { public Comma() : base(",") { } }

    class Dot : Token { public Dot() : base(".") { } }

    class Colon : Token { public Colon() : base(":") { } }

    class StartBracket : Token { public StartBracket() : base("{") { } }

    class EndBracket : Token { public EndBracket() : base("}") { } }


    class StringToken : Token { public StringToken(string s) : base(s) { } }

    class NumToken : Token { public NumToken(string s) : base(s) { } }

    class TokenEnd : Token { public TokenEnd() : base("") { } }

    class CommandToken : Token {
        public CommandToken(string command) : base(command) {

        }

        public static readonly string[] specials = new string[] {
            "Trigger",
            "Conditions",
            "Actions",
        };

        public static readonly string[] conditions = new string[] {
            "Accumulate",
            "Always",
            "Bring",
            "Command",
            "Command the Least",
            "Command the Least At",
            "Command the Most",
            "Commands the Most At",
            "Countdown Timer",
            "Deaths",
            "Elapsed Time",
            "Highest Score",
            "Kill",
            "Least Kills",
            "Least Resources",
            "Lowest Score",
            "Most Kills",
            "Most Resources",
            "Never",
            "Opponents",
            "Score",
            "Switch",
        };

        public static readonly string[] actions = new string[] {
            "Center View",
            "Comment",
            "Create Unit",
            "Create Unit with Properties",
            "Defeat",
            "Display Text Message",
            "Draw",
            "Give Units to Player",
            "Kill Unit",
            "Kill Unit At Location",
            "Leader Board Control At Location",
            "Leader Board Control",
            "Leader board Greed",
            "Leader Board Kills",
            "Leader Board Points",
            "Leader Board Resources",
            "Leader board Computer Players",
            "Leader board Goal Control At Location",
            "Leader board Goal Control",
            "Leader board Goal Kills",
            "Leader board Goal Points",
            "Leader board Goal Resources",
            "Minimap Ping",
            "Modify Unit Energy",
            "Modify Unit Hanger Count",
            "Modify Unit Hit Points",
            "Modify Unit Resource Amount",
            "Modify Unit Shield Points",
            "Move Location",
            "Play WAV",
            "Move Unit",
            "Mute Unit Speech",
            "Order",
            "Pause Game",
            "Pause Timer",
            "Preserve Trigger",
            "Remove Unit",
            "Remove Unit At Location",
            "Run AI Script",
            "Run AI Script At Location",
            "Set Alliance Status",
            "Set Countdown Timer",
            "Set Deaths",
            "Set Doodad State",
            "Set Invincibility",
            "Set Mission Objectives",
            "Set Next Scenario",
            "Set Resources",
            "Set Score",
            "Set Switch",
            "Talking Portrait",
            "Transmission",
            "Unmute Unit Speech",
            "Unpause Game",
            "Unpause Timer",
            "Victory",
            "Wait",

        };

        public static readonly string[] parameters = new string[] {
            "At most",
            "At least",
            "Exactly",
            "ore",
            "gas",
            "ore and gas",
            "patrol",
            "add",
            "subtract",
            "set to",
            "set",
            "clear",
            "randomize",
            "not set",
            "buildings",
            "custom",
            "kills",
            "kills and razings",
            "razings",
            "total",
            "units",
            "units and buildings",
            "All",
            "enabled",
            "disabled",
            "Toggle",
            "attack",
            "move",
            "patrol",
            "Ally",
            "Enemy",
            "Allied Victory",
            "Don't Always Display",
            "Always Display",
        };

        private bool isSomething(string what) {
            return base.getContent().ToLower().Equals(what.ToLower());
        }

        public bool isOneSpecial() {
            string f = base.getContent().ToLower();
            foreach (string special in specials) {
                string c = special.ToLower();
                if (f.Equals(c)) {
                    return true;
                }
            }
            return false;
        }

        public bool isOneParameter() {
            string f = base.getContent().ToLower();
            foreach (string parameter in parameters) {
                string c = parameter.ToLower();
                if (f.Equals(c)) {
                    return true;
                }
            }
            return false;
        }

        public bool isOneCondition() {
            string f = base.getContent().ToLower();
            foreach (string condition in conditions) {
                string c = condition.ToLower();
                if (f.Equals(c)) {
                    return true;
                }
            }
            return false;
        }

        public bool isOneAction() {
            string f = base.getContent().ToLower();
            foreach (string action in actions) {
                string c = action.ToLower();
                if (f.Equals(c)) {
                    return true;
                }
            }
            return false;
        }

        public bool isValid() {
            return isOneAction() || isOneCondition() || isOneSpecial() || isOneParameter();

        }

        public bool isAccumulate() {
            return isSomething("Accumulate");
        }

        public bool isActions() {
            return isSomething("Actions");
        }

        public bool isAdd() {
            return isSomething("Add");
        }

        public bool isAll() {
            return isSomething("All");
        }

        public bool isAlliedVictory() {
            return isSomething("Allied Victory");
        }

        public bool isAlly() {
            return isSomething("Ally");
        }

        public bool isAlways() {
            return isSomething("Always");
        }

        public bool isNoLocation() {
            return isSomething("No Location");
        }

        public bool isAlwaysDisplay() {
            return isSomething("Always Display");
        }

        public bool isAtLeast() {
            return isSomething("At Least");
        }

        public bool isAtMost() {
            return isSomething("At Most");
        }

        public bool isAttack() {
            return isSomething("Attack");
        }

        public bool isBring() {
            return isSomething("Bring");
        }

        public bool isBuildings() {
            return isSomething("Buildings");
        }

        public bool isCenterView() {
            return isSomething("Center View");
        }

        public bool isCommand() {
            return isSomething("Command");
        }

        public bool isCommandsTheMostAt() {
            return isSomething("Commands The Most At");
        }

        public bool isCommandTheLeast() {
            return isSomething("Command The Least");
        }

        public bool isCommandTheLeastAt() {
            return isSomething("Command The Least At");
        }

        public bool isCommandTheMost() {
            return isSomething("Command The Most");
        }

        public bool isComment() {
            return isSomething("Comment");
        }

        public bool isConditions() {
            return isSomething("Conditions");
        }

        public bool isCountdownTimer() {
            return isSomething("Countdown Timer");
        }

        public bool isCreateUnit() {
            return isSomething("Create Unit");
        }

        public bool isCreateUnitWithProperties() {
            return isSomething("Create Unit With Properties");
        }

        public bool isCustom() {
            return isSomething("Custom");
        }

        public bool isClear() {
            return isSomething("Clear");
        }

        public bool isDeaths() {
            return isSomething("Deaths");
        }

        public bool isDefeat() {
            return isSomething("Defeat");
        }

        public bool isDisabled() {
            return isSomething("Disabled");
        }

        public bool isDisplayTextMessage() {
            return isSomething("Display Text Message");
        }

        public bool isDontAlwaysDisplay() {
            return isSomething("Don't Always Display");
        }

        public bool isDraw() {
            return isSomething("Draw");
        }

        public bool isElapsedTime() {
            return isSomething("Elapsed Time");
        }

        public bool isEnabled() {
            return isSomething("Enabled");
        }

        public bool isEnemy() {
            return isSomething("Enemy");
        }

        public bool isExactly() {
            return isSomething("Exactly");
        }

        public bool isGas() {
            return isSomething("Gas");
        }

        public bool isGiveUnitsToPlayer() {
            return isSomething("Give Units To Player");
        }

        public bool isHighestScore() {
            return isSomething("Highest Score");
        }

        public bool isKill() {
            return isSomething("Kill");
        }

        public bool isKills() {
            return isSomething("Kills");
        }

        public bool isKillsAndRazings() {
            return isSomething("Kills And Razings");
        }

        public bool isKillUnit() {
            return isSomething("Kill Unit");
        }

        public bool isKillUnitAtLocation() {
            return isSomething("Kill Unit At Location");
        }

        public bool isLeaderboardComputerPlayers() {
            return isSomething("Leader board Computer Players");
        }

        public bool isLeaderBoardControl() {
            return isSomething("Leader Board Control");
        }

        public bool isLeaderBoardControlAtLocation() {
            return isSomething("Leader Board Control At Location");
        }

        public bool isLeaderboardGoalControl() {
            return isSomething("Leader board Goal Control");
        }

        public bool isLeaderboardGoalControlAtLocation() {
            return isSomething("Leader board Goal Control At Location");
        }

        public bool isLeaderboardGoalKills() {
            return isSomething("Leader board Goal Kills");
        }

        public bool isLeaderboardGoalPoints() {
            return isSomething("Leader board Goal Points");
        }

        public bool isLeaderboardGoalResources() {
            return isSomething("Leader board Goal Resources");
        }

        public bool isLeaderboardGreed() {
            return isSomething("Leader board Greed");
        }

        public bool isLeaderBoardKills() {
            return isSomething("Leader Board Kills");
        }

        public bool isLeaderBoardPoints() {
            return isSomething("Leader Board Points");
        }

        public bool isLeaderBoardResources() {
            return isSomething("Leader Board Resources");
        }

        public bool isLeastKills() {
            return isSomething("Least Kills");
        }

        public bool isLeastResources() {
            return isSomething("Least Resources");
        }

        public bool isLowestScore() {
            return isSomething("Lowest Score");
        }

        public bool isMinimapPing() {
            return isSomething("Minimap Ping");
        }

        public bool isModifyUnitEnergy() {
            return isSomething("Modify Unit Energy");
        }

        public bool isModifyUnitHangerCount() {
            return isSomething("Modify Unit Hanger Count");
        }

        public bool isModifyUnitHitPoints() {
            return isSomething("Modify Unit Hit Points");
        }

        public bool isModifyUnitResourceAmount() {
            return isSomething("Modify Unit Resource Amount");
        }

        public bool isModifyUnitShieldPoints() {
            return isSomething("Modify Unit Shield Points");
        }

        public bool isMostKills() {
            return isSomething("Most Kills");
        }

        public bool isMostResources() {
            return isSomething("Most Resources");
        }

        public bool isMove() {
            return isSomething("Move");
        }

        public bool isMoveLocation() {
            return isSomething("Move Location");
        }

        public bool isMoveUnit() {
            return isSomething("Move Unit");
        }

        public bool isMuteUnitSpeech() {
            return isSomething("Mute Unit Speech");
        }

        public bool isNever() {
            return isSomething("Never");
        }

        public bool isNotSet() {
            return isSomething("Cleared") || isSomething("Not Set");
        }

        public bool isOpponents() {
            return isSomething("Opponents");
        }

        public bool isOrder() {
            return isSomething("Order");
        }

        public bool isOre() {
            return isSomething("Ore");
        }

        public bool isOreAndGas() {
            return isSomething("Ore And Gas");
        }

        public bool isPatrol() {
            return isSomething("Patrol");
        }

        public bool isPauseGame() {
            return isSomething("Pause Game");
        }

        public bool isPauseTimer() {
            return isSomething("Pause Timer");
        }

        public bool isPlayWAV() {
            return isSomething("Play WAV");
        }

        public bool isPreserveTrigger() {
            return isSomething("Preserve Trigger");
        }

        public bool isRandomize() {
            return isSomething("Randomize");
        }

        public bool isRazings() {
            return isSomething("Razings");
        }

        public bool isRemoveUnit() {
            return isSomething("Remove Unit");
        }

        public bool isRemoveUnitAtLocation() {
            return isSomething("Remove Unit At Location");
        }

        public bool isRunAIScript() {
            return isSomething("Run AI Script");
        }

        public bool isRunAIScriptAtLocation() {
            return isSomething("Run AI Script At Location");
        }

        public bool isScore() {
            return isSomething("Score");
        }

        public bool isSet() {
            return isSomething("Set");
        }

        public bool isSetAllianceStatus() {
            return isSomething("Set Alliance Status");
        }

        public bool isSetCountdownTimer() {
            return isSomething("Set Countdown Timer");
        }

        public bool isSetDeaths() {
            return isSomething("Set Deaths");
        }

        public bool isSetDoodadState() {
            return isSomething("Set Doodad State");
        }

        public bool isSetInvincibility() {
            return isSomething("Set Invincibility");
        }

        public bool isSetMissionObjectives() {
            return isSomething("Set Mission Objectives");
        }

        public bool isSetNextScenario() {
            return isSomething("Set Next Scenario");
        }

        public bool isSetResources() {
            return isSomething("Set Resources");
        }

        public bool isSetScore() {
            return isSomething("Set Score");
        }

        public bool isSetSwitch() {
            return isSomething("Set Switch");
        }

        public bool isSetTo() {
            return isSomething("Set To");
        }

        public bool isSubtract() {
            return isSomething("Subtract");
        }

        public bool isSwitch() {
            return isSomething("Switch");
        }

        public bool isMemory() {
            return isSomething("Memory");
        }

        public bool isTalkingPortrait() {
            return isSomething("Talking Portrait");
        }

        public bool isToggle() {
            return isSomething("Toggle");
        }

        public bool isTotal() {
            return isSomething("Total");
        }

        public bool isTransmission() {
            return isSomething("Transmission");
        }

        public bool isTrigger() {
            return isSomething("Trigger");
        }

        public bool isLocations() {
            return isSomething("Locations");
        }

        public bool isSwitches() {
            return isSomething("Switches");
        }

        public bool isPlayers() {
            return isSomething("Players");
        }

        public bool isUnits() {
            return isSomething("Units");
        }

        public bool isUnitsAndBuildings() {
            return isSomething("Units And Buildings");
        }

        public bool isUnmuteUnitSpeech() {
            return isSomething("Unmute Unit Speech");
        }

        public bool isUnpauseGame() {
            return isSomething("Unpause Game");
        }

        public bool isUnpauseTimer() {
            return isSomething("Unpause Timer");
        }

        public bool isVictory() {
            return isSomething("Victory");
        }

        public bool isWait() {
            return isSomething("Wait");
        }

        public bool isFlags() {
            return isSomething("Flags");
        }

        public bool isStrings() {
            return isSomething("Strings");
        }

        public bool isExtendedStrings() {
            return isSomething("ExtendedStrings");
        }
    }
}
