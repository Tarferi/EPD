using StarcraftEPDTriggers.src.data;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StarcraftEPDTriggers.src {
    public class Parser {
        private Scanner scanner;

        public Parser(Scanner s) {
            this.scanner = s;
        }

        public void close() {
            scanner.close();
        }

        public bool parse(ref int lastReadPosition, ref int lastReadPositionEnd) {
            LocationDef.__setLocationsCountDoNotUseOutsideOfParser(256); // Safe keeping
            return parse(false, ref lastReadPosition, ref lastReadPositionEnd);
        }

        public bool parse(bool onlyTriggers, ref int lastReadPosition, ref int lastReadPositionEnd) {
            allTriggers = new List<Trigger>();
            return parseTokens(onlyTriggers, ref lastReadPosition, ref lastReadPositionEnd);
        }

        public List<Trigger> getTriggers() {
            return allTriggers;
        }

        Token lastToken;

        public virtual Token getNextToken() {
            lastToken = scanner.getNextToken();
            if (currentTrigger != null) {
                currentTrigger.addToken(lastToken);
            }
            return lastToken;
        }

        private List<Trigger> allTriggers = new List<Trigger>();

        private Trigger currentTrigger;

        private bool parseStringTable() {
            Token qt = getNextToken();
            if (qt is CommandToken) {
                CommandToken ct = (CommandToken) qt;
                if (ct.isStrings()) {
                    if (getNextToken() is LeftBracket) {
                        qt = getNextToken();
                        if (qt is NumToken) {
                            int stringTableLength = qt.toInt();
                            if (getNextToken() is RightBracket) {
                                if (getNextToken() is Colon) {
                                    string[] stringTable = new string[stringTableLength];
                                    for (int i = 0; i < stringTable.Length; i++) {
                                        qt = getNextToken();
                                        if (qt is StringToken) {
                                            stringTable[i] = qt.getContent();
                                            //Debug.Write("0");
                                        } else {
                                            throw new NotImplementedException();
                                        }
                                    }
                                    Token.StringTable = stringTable;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }

        private bool parseExtendedStringTable() {
            Token qt = getNextToken();
            if (qt is CommandToken) {
                CommandToken ct = (CommandToken) qt;
                if (getNextToken() is LeftBracket) {
                    if (ct.isExtendedStrings()) {
                        qt = getNextToken();
                        if (qt is NumToken) {
                            int extendedStringsTableLength = ((NumToken) qt).toInt();
                            if (getNextToken() is RightBracket) {
                                if (getNextToken() is Colon) {
                                    string[] extendedStringTable = new string[extendedStringsTableLength];
                                    for (int i = 0; i < extendedStringTable.Length; i++) {
                                        qt = getNextToken();
                                        if (qt is StringToken) {
                                            extendedStringTable[i] = ((StringToken) qt).ToString();
                                        } else {
                                            throw new NotImplementedException();
                                        }
                                    }
                                    Token.ExtendedStringTable = extendedStringTable;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }

        private bool parseTriggers() {
            while (true) {
                currentTrigger = new Trigger();
                Token t = getNextToken();
                if (t == null) {
                    return true;
                } else if (t is CommandToken) {
                    CommandToken ct = t as CommandToken;
                    if (ct.isTrigger()) {
                        if (getNextToken() is LeftBracket) {
                            Token affectName = getNextToken();
                            allTriggers.Add(currentTrigger);
                            Token nt = affectName;
                            if (affectName is NumToken || affectName is RightBracket) { // This is fuckery
                                if (affectName is NumToken) { // This trigger belongs to somebody
                                    nt = getNextToken();
                                    currentTrigger.addAffectName(affectName);
                                    while (nt is Comma) {
                                        affectName = getNextToken();
                                        if (affectName is NumToken) {
                                            currentTrigger.addAffectName(affectName);
                                            nt = getNextToken();
                                        } else {
                                            throw new NotImplementedException();
                                        }
                                    }
                                } else { // This trigger doesn't belong to anyone

                                }
                                if (nt is RightBracket) {
                                    if (getNextToken() is StartBracket) {
                                        t = getNextToken();
                                        if (t is CommandToken) {
                                            ct = t as CommandToken;
                                            if (ct.isConditions()) {
                                                if (getNextToken() is Colon) {
                                                    if (parseConditions()) {
                                                        if (parseActions()) {
                                                            if (getNextToken() is TokenEnd) {
                                                                continue;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    } else {
                        return true;
                    }
                }
                throw new NotImplementedException();
            }
        }

        private bool parseLocations() {
            if (lastToken is CommandToken) {
                CommandToken ct = (CommandToken) lastToken;
                if (ct.isLocations()) {
                    if (getNextToken() is LeftBracket) {
                        Token num = getNextToken();
                        if (num is NumToken && getNextToken() is RightBracket && getNextToken() is Colon) {
                            int locationsCount = num.toInt();
                            LocationDef.__setLocationsCountDoNotUseOutsideOfParser(locationsCount);
                            for (int i = 0; i < locationsCount; i++) {
                                Token locName = getNextToken();
                                if (locName is NumToken) { // Unnamed location
                                    LocationDef.__setLocationNameDontUseOutsideOfParser(i, "Location " + ((NumToken) locName).toInt());
                                } else if (locName is StringToken) {
                                    LocationDef.__setLocationNameDontUseOutsideOfParser(i, ((StringToken) locName).toStringDef().ToString());
                                } else if (locName is CommandToken) {
                                    if (((CommandToken) locName).isNoLocation()) {
                                        LocationDef.__setLocationNameDontUseOutsideOfParser(i, "No Location");
                                    } else {
                                        throw new NotImplementedException();
                                    }
                                } else {
                                    throw new NotImplementedException();
                                }
                            }
                            return true;
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }

        private bool parsePlayerNames() {
            Token t = getNextToken();
            if (t is CommandToken) {
                CommandToken ct = t as CommandToken;
                if (ct.isPlayers()) {
                    if (getNextToken() is LeftBracket) {
                        Token num = getNextToken();
                        if (num is NumToken) {
                            if (getNextToken() is RightBracket) {
                                if (getNextToken() is Colon) {
                                    int playersCount = num.toInt();
                                    for (int i = 0; i < playersCount; i++) {
                                        Token playerName = getNextToken();
                                        if (playerName is StringToken) {
                                            PlayerDef.__setPlayerNameDontUseOutsideOfParser(i, ((StringToken) playerName).toStringDef().ToString());
                                        } else {
                                            throw new NotImplementedException();
                                        }
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }

        private bool parseSwitchNames() {
            Token t = getNextToken();
            if (t is CommandToken) {
                CommandToken ct = t as CommandToken;
                if (ct.isSwitches()) {
                    if (getNextToken() is LeftBracket) {
                        Token num = getNextToken();
                        if (num is NumToken) {
                            if (getNextToken() is RightBracket) {
                                if (getNextToken() is Colon) {
                                    int switchesCount = num.toInt();
                                    for (int i = 0; i < switchesCount; i++) {
                                        Token switchName = getNextToken();
                                        if (switchName is StringToken) {
                                            SwitchNameDef.__setSwitchNameDontUseOutsideOfParser(i, ((StringToken) switchName).toStringDef().ToString());
                                        } else {
                                            throw new NotImplementedException();
                                        }
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }

        private bool parseUnitNames() {
            Token t = getNextToken();
            if (t is CommandToken) {
                CommandToken ct = t as CommandToken;
                if (ct.isUnits())
                    if (getNextToken() is LeftBracket) {
                        Token num = getNextToken();
                        if (num is NumToken) {
                            if (getNextToken() is RightBracket) {
                                if (getNextToken() is Colon) {
                                    int unitsCount = num.toInt();
                                    for (int i = 0; i < unitsCount; i++) {
                                        Token unitName = getNextToken();
                                        if (unitName is StringToken) {
                                            UnitVanillaDef.__setUnitNameDontUseOutsideOfParser(i, ((StringToken) unitName).toStringDef().ToString());
                                        } else {
                                            throw new NotImplementedException();
                                        }
                                    }
                                    return true;
                                }
                            }
                        }
                    }
            }
            throw new NotImplementedException();
        }

        private void setLastTokenData(Token lastToken, ref int lastReadPosition, ref int lastReadPositionEnd) {
            if (lastToken == null) {
                lastReadPosition = 0;
                lastReadPositionEnd = 0;
            } else {
                lastReadPosition = lastToken.getPosition();
                lastReadPositionEnd = lastReadPosition + lastToken.getRawToken().Length;
            }
        }

        private bool parseTokens(bool onlyTriggers, ref int lastReadPosition, ref int lastReadPositionEnd) {
            if (onlyTriggers) {
                try {
                    if (parseTriggers()) {
                        setLastTokenData(lastToken, ref lastReadPosition, ref lastReadPositionEnd);
                        return true;
                    } else {
                        setLastTokenData(lastToken, ref lastReadPosition, ref lastReadPositionEnd);
                        throw new NotImplementedException();
                    }
                } catch (NotImplementedException) {
                    setLastTokenData(lastToken, ref lastReadPosition, ref lastReadPositionEnd);
                    throw;
                }
            } else {
                if (parseStringTable()) {
                    if (parseExtendedStringTable()) {
                        if (parseTriggers()) {
                            if (parseLocations()) {
                                if (parseUnitNames()) {
                                    if (parseSwitchNames()) {
                                        if (parsePlayerNames()) {
                                            lastReadPosition = lastToken.getPosition();
                                            lastReadPositionEnd = lastReadPosition + lastToken.getRawToken().Length;
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            lastReadPosition = lastToken == null ? 0 : lastToken.getPosition();
            lastReadPositionEnd = lastToken == null ? 0 : lastReadPosition + lastToken.getRawToken().Length;
            throw new NotImplementedException();
        }

        public Action parseOnlyAction() {
            Token t = getNextToken();
            bool enabled = true;
            if (t is Semicolon) {
                enabled = false;
                t = getNextToken();
            }
            if (t is CommandToken) {
                Action c = getAction((CommandToken) t);
                c.setEnabled(enabled);
                return c;
            }
            throw new NotImplementedException();
        }

        public Condition parseOnlyCondition() {
            Token t = getNextToken();
            bool enabled = true;
            if (t is Semicolon) {
                enabled = false;
                t = getNextToken();
            }
            if (t is CommandToken) {
                Condition c = GetCondition((CommandToken) t);
                c.setEnabled(enabled);
                return c;
            }
            throw new NotImplementedException();
        }

        class DummyParser : Parser {

            private Token[] _ret;
            private int ct = 0;

            public DummyParser(Token[] toReturn) : base(null) {
                _ret = toReturn;
            }

            public override Token getNextToken() {
                Token t = _ret[ct];
                ct++;
                return t;
            }
        }

        private Condition GetCondition(CommandToken ct) {
            int position = ct.getPosition();
            if (ct.isAccumulate()) {
                return new ConditionAccumulate(this);
            } else if (ct.isAlways()) {
                return new ConditionAlways(this);
            } else if (ct.isBring()) {
                return new ConditionBring(this);
            } else if (ct.isCommand()) {
                return new ConditionCommand(this);
            } else if (ct.isCommandTheLeast()) {
                return new ConditionCommandTheLeast(this);
            } else if (ct.isCommandTheLeastAt()) {
                return new ConditionCommandTheLeastAt(this);
            } else if (ct.isCommandTheMost()) {
                return new ConditionCommandTheMost(this);
            } else if (ct.isCommandsTheMostAt()) {
                return new ConditionCommandsTheMostAt(this);
            } else if (ct.isCountdownTimer()) {
                return new ConditionCountdownTimer(this);
            } else if (ct.isDeaths()) {
                ConditionDeaths d = new ConditionDeaths(this);
                UnitVanillaDef unit = d.getUnitDef();
                PlayerDef player = d.getPlayerDef();
                IntDef amount = d.getAmount();
                Quantifier q = d.getQuantifier();
                if (unit.getIndex() > unit.getMaxValue()) { // EUD Condition
                    int pid = player.getIndex();
                    int uid = unit.getIndex();
                    int oid = amount.getIndex();
                    int addr = (uid * 12) + pid;

                    ConditionMemory cm = new ConditionMemory(new DummyParser(new Token[] { new LeftBracket(position), new NumToken(addr.ToString(), position), new Comma(position), new CommandToken(q.ToString(), position), new Comma(position), new NumToken(oid.ToString(), position), new RightBracket(position) }));
                    EPDCondition cond = EPDCondition.get(cm);
                    if (cond != null) {
                        return cond;
                    }

                    return cm;
                }

                return d;
            } else if (ct.isElapsedTime()) {
                return new ConditionElapsedTime(this);
            } else if (ct.isHighestScore()) {
                return new ConditionHighestScore(this);
            } else if (ct.isKill()) {
                return new ConditionKill(this);
            } else if (ct.isLeastKills()) {
                return new ConditionLeastKills(this);
            } else if (ct.isLeastResources()) {
                return new ConditionLeastResources(this);
            } else if (ct.isLowestScore()) {
                return new ConditionLowestScore(this);
            } else if (ct.isMostKills()) {
                return new ConditionMostKills(this);
            } else if (ct.isMostResources()) {
                return new ConditionMostResources(this);
            } else if (ct.isNever()) {
                return new ConditionNever(this);
            } else if (ct.isOpponents()) {
                return new ConditionOpponents(this);
            } else if (ct.isScore()) {
                return new ConditionScore(this);
            } else if (ct.isSwitch()) {
                return new ConditionSwitch(this);
            } else if (ct.isMemory()) {
                ConditionMemory cm = new ConditionMemory(this);
                EPDCondition cond = EPDCondition.get(cm);
                if (cond != null) {
                    return cond;
                }
                return cm;
            } else if (ct.isCustom()) {
                return new ConditionCustom(this);
            }
            throw new NotImplementedException();
        }

        private Action getAction(CommandToken ct) {
            if (ct.isCenterView()) {
                return new ActionCenterView(this);
            } else if (ct.isComment()) {
                return new ActionComment(this);
            } else if (ct.isCreateUnit()) {
                return new ActionCreateUnit(this);
            } else if (ct.isCreateUnitWithProperties()) {
                return new ActionCreateUnitWithProperties(this);
            } else if (ct.isDefeat()) {
                return new ActionDefeat(this);
            } else if (ct.isDisplayTextMessage()) {
                return new ActionDisplayTextMessage(this);
            } else if (ct.isDraw()) {
                return new ActionDraw(this);
            } else if (ct.isGiveUnitsToPlayer()) {
                return new ActionGiveUnitsToPlayer(this);
            } else if (ct.isKillUnit()) {
                return new ActionKillUnit(this);
            } else if (ct.isKillUnitAtLocation()) {
                return new ActionKillUnitAtLocation(this);
            } else if (ct.isLeaderBoardControlAtLocation()) {
                return new ActionLeaderBoardControlAtLocation(this);
            } else if (ct.isLeaderBoardControl()) {
                return new ActionLeaderBoardControl(this);
            } else if (ct.isLeaderboardGreed()) {
                return new ActionLeaderboardGreed(this);
            } else if (ct.isLeaderBoardKills()) {
                ActionLeaderBoardKills act = new ActionLeaderBoardKills(this);
                int[] mapping = ActionLeaderBoardKills.getTextMapping();
                int unitIndex = mapping[0];
                UnitVanillaDef unit = act.get<UnitVanillaDef>(unitIndex);
                if (unit.getIndex() > unit.getMaxValue()) {
                    return new ActionLeaderBoardDeaths(act);
                }
                return act;
            } else if (ct.isLeaderBoardPoints()) {
                return new ActionLeaderBoardPoints(this);
            } else if (ct.isLeaderBoardResources()) {
                return new ActionLeaderBoardResources(this);
            } else if (ct.isLeaderboardComputerPlayers()) {
                return new ActionLeaderboardComputerPlayers(this);
            } else if (ct.isLeaderboardGoalControlAtLocation()) {
                return new ActionLeaderboardGoalControlAtLocation(this);
            } else if (ct.isLeaderboardGoalControl()) {
                return new ActionLeaderboardGoalControl(this);
            } else if (ct.isLeaderboardGoalKills()) {
                return new ActionLeaderboardGoalKills(this);
            } else if (ct.isLeaderboardGoalPoints()) {
                return new ActionLeaderboardGoalPoints(this);
            } else if (ct.isLeaderboardGoalResources()) {
                return new ActionLeaderboardGoalResources(this);
            } else if (ct.isMinimapPing()) {
                return new ActionMinimapPing(this);
            } else if (ct.isModifyUnitEnergy()) {
                return new ActionModifyUnitEnergy(this);
            } else if (ct.isModifyUnitHangerCount()) {
                return new ActionModifyUnitHangerCount(this);
            } else if (ct.isModifyUnitHitPoints()) {
                return new ActionModifyUnitHitPoints(this);
            } else if (ct.isModifyUnitResourceAmount()) {
                return new ActionModifyUnitResourceAmount(this);
            } else if (ct.isModifyUnitShieldPoints()) {
                return new ActionModifyUnitShieldPoints(this);
            } else if (ct.isMoveLocation()) {
                return new ActionMoveLocation(this);
            } else if (ct.isPlayWAV()) {
                return new ActionPlayWAV(this);
            } else if (ct.isMoveUnit()) {
                return new ActionMoveUnit(this);
            } else if (ct.isMuteUnitSpeech()) {
                return new ActionMuteUnitSpeech(this);
            } else if (ct.isOrder()) {
                return new ActionOrder(this);
            } else if (ct.isPauseGame()) {
                return new ActionPauseGame(this);
            } else if (ct.isPauseTimer()) {
                return new ActionPauseTimer(this);
            } else if (ct.isPreserveTrigger()) {
                return new ActionPreserveTrigger(this);
            } else if (ct.isRemoveUnit()) {
                return new ActionRemoveUnit(this);
            } else if (ct.isRemoveUnitAtLocation()) {
                return new ActionRemoveUnitAtLocation(this);
            } else if (ct.isRunAIScript()) {
                return new ActionRunAIScript(this);
            } else if (ct.isRunAIScriptAtLocation()) {
                return new ActionRunAIScriptAtLocation(this);
            } else if (ct.isSetAllianceStatus()) {
                return new ActionSetAllianceStatus(this);
            } else if (ct.isSetCountdownTimer()) {
                return new ActionSetCountdownTimer(this);
            } else if (ct.isSetDeaths()) {
                RawActionSetDeaths rawDeaths = new RawActionSetDeaths(this);
                Action epd = EPDAction.getFromDeathAction(rawDeaths);
                if (epd != null) {
                    return epd;
                } else {
                    try {
                        return new ActionSetDeaths(rawDeaths);
                    } catch (NotImplementedException) {
                        return rawDeaths;
                    }
                    //return rawDeaths;
                }
            } else if (ct.isSetDoodadState()) {
                return new ActionSetDoodadState(this);
            } else if (ct.isSetInvincibility()) {
                return new ActionSetInvincibility(this);
            } else if (ct.isSetMissionObjectives()) {
                return new ActionSetMissionObjectives(this);
            } else if (ct.isSetNextScenario()) {
                return new ActionSetNextScenario(this);
            } else if (ct.isSetResources()) {
                return new ActionSetResources(this);
            } else if (ct.isSetScore()) {
                return new ActionSetScore(this);
            } else if (ct.isSetSwitch()) {
                return new ActionSetSwitch(this);
            } else if (ct.isTalkingPortrait()) {
                return new ActionTalkingPortrait(this);
            } else if (ct.isTransmission()) {
                return new ActionTransmission(this);
            } else if (ct.isUnmuteUnitSpeech()) {
                return new ActionUnmuteUnitSpeech(this);
            } else if (ct.isUnpauseGame()) {
                return new ActionUnpauseGame(this);
            } else if (ct.isUnpauseTimer()) {
                return new ActionUnpauseTimer(this);
            } else if (ct.isVictory()) {
                return new ActionVictory(this);
            } else if (ct.isWait()) {
                return new ActionWait(this);
            }
            throw new NotImplementedException();
        }

        private bool parseFlags() {
            if (getNextToken() is Colon) {
                Token t = getNextToken();
                if (t is NumToken) {
                    int flags = t.toBinaryInt();
                    currentTrigger.setFlags(flags);
                    if (getNextToken() is Semicolon) {
                        return true;
                    }
                }
            }
            throw new NotImplementedException();
        }

        private bool parseConditions() {
            Token t = getNextToken();
            bool commented = false;
            if (t is Semicolon) { // Comment
                commented = true;
                t = getNextToken();
            }
            if (t is CommandToken) {
                CommandToken ct = t as CommandToken;
                if (ct.isActions()) {
                    if (!commented) {
                        if (getNextToken() is Colon) {
                            return true;
                        }
                    }
                } else {
                    Condition c = GetCondition(ct);
                    c.setEnabled(!commented);
                    currentTrigger.addCondition(c);
                    t = getNextToken();
                    if (t is Semicolon) {
                        return parseConditions();
                    }
                }
            }
            throw new NotImplementedException();
        }

        private bool parseActions() {
            Token t = getNextToken();
            bool commented = false;
            if (t is Semicolon) { // Comment
                commented = true;
                t = getNextToken();
            } else {
                if (t is EndBracket) {
                    return true;
                }
            }

            if (t is CommandToken) {
                CommandToken ct = t as CommandToken;
                if (ct.isFlags()) {
                    if (parseFlags()) {
                        if (getNextToken() is EndBracket) {
                            return true;
                        }
                    }
                } else {
                    Action a = getAction(ct);
                    a.setEnabled(!commented);
                    currentTrigger.addAction(a);
                    t = getNextToken();
                    if (t is Semicolon) {
                        return parseActions();
                    }
                }
            }
            throw new NotImplementedException();
        }
    }
}
