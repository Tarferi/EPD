using System;
using StarcraftEPDTriggers.src;
using System.Text;
using static StarcraftEPDTriggers.Action;
using static StarcraftEPDTriggers.src.TriggerContentType;
using StarcraftEPDTriggers.src.data;

namespace StarcraftEPDTriggers {

    public abstract class Action : TriggerContent {

        public Action(Parser parser, int num) : base(parser, num) { }

        public override string ToString() {
            return ToSaveString();
        }

        public virtual string ToSaveString() {
            StringBuilder sb = new StringBuilder();
            if(!this.isEnabled()) {
                sb.Append(";");
            }
            if (this is GenericAction) {
                SaveableItem[] parts = ((GenericAction)this).getUsefulDefinitionParts();
                sb.Append(((GenericAction)this).Name + "(");
                for (int i = 0; i < parts.Length; i++) {
                    SaveableItem part = parts[i];
                    sb.Append(part.ToSaveString());
                    if (i != parts.Length - 1) {
                        sb.Append(", ");
                    }
                }
                sb.Append(");");
            } else if(this is EPDAction) {
                EPDAction epd = (EPDAction) this;
                sb.Append(epd.ToString());
            } else {
                throw new NotImplementedException();
            }
            return sb.ToString();
        }
    }

    public class GenericAction : Action {

        private GeneralTriggerContentInternalCalculator _obj;

        public string Name { get { return _obj.Name; } }

        private static int getArgCount(TriggerContentTypeDescriptor[] types) {
            int count = 0;
            foreach (TriggerContentTypeDescriptor type in types) {
                if (!(type is TriggerContentTypeDescriptorVisual)) {
                    count++;
                }
            }
            return count;
        }

        public T get<T>(int index) where T : SaveableItem {
            object obj = _obj.getRawObject(index);
            if (obj is T) {
                return (T)obj;
            }
            throw new NotImplementedException();
        }

        public void set<T>(T obj, int index)where T:SaveableItem {
            object aobj = _obj.getRawObject(index);
            if(aobj is T) {
                _obj.setRawObject(obj, index);
                return;
            }
            throw new NotImplementedException();
        }


        public GenericAction(string name, int[] textMapping, Func<GenericAction, TriggerDefinitionPart[], TriggerDefinitionPart[]> remapVisuals, Func<GenericAction, SaveableItem[], SaveableItem[]> remapSaveables, GenericAction original) : base(null, 0) {
            _obj = new GeneralTriggerContentInternalCalculator(name, textMapping,(TriggerDefinitionPart[] input)=> remapVisuals(this, input),(SaveableItem[] input)=> remapSaveables(this, input), original._obj);
        }


        public GenericAction(Parser parser, string name, TriggerContentTypeDescriptor[] types, int[] usefulMapping, int[] textMapping, int[]trigMapping) : base(parser, getArgCount(types)) {
            _obj = new GeneralTriggerContentInternalCalculator(parser, name, types, usefulMapping, textMapping, trigMapping, getArgCount, this);
        }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            return _obj.getDefinitionParts();
        }

        public SaveableItem[] getUsefulDefinitionParts() {
            return _obj.getUsefulDefinitionParts();
        }

    }

    public class RawActionSetDeaths : GenericAction {

        public RawActionSetDeaths(Parser parser) : base(parser, "Set Deaths", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }


        public SetQuantifier getSetQuantifier() {
            return get<SetQuantifier>(2);
        }

        public void setSetQuantifier(SetQuantifier sq) {
            set<SetQuantifier>(sq, 2);
        }

        public IntDef getPlayerID() {
            return get<IntDef>(0);
        }

        public void setPlayerID(IntDef playerID) {
            set<IntDef>(playerID, 0);
        }

        public IntDef getAmount() {
            return get<IntDef>(3);
        }

        public void setAmmount(IntDef amount) {
            set<IntDef>(amount, 3);
        }

        public IntDef getUnitID() {
            return get<IntDef>(1);
        }

        public void setUnitID(IntDef unitID) {
            set<IntDef>(unitID, 1);
        }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Modify death counts for "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(": "),
                TriggerContentType.SET_QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL("."),
            };
        }

        public static int[] getTextMapping() {
            return new int[] { 0, 2, 3, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1, 2, 3 };
        }


    }

    public class ActionLeaderBoardDeaths : Action {

        private Func<UnitVanillaDef> _unitTypeGetter;
        private Action<UnitVanillaDef> _unitTypeSetter;
        private Func<StringDef> _titleGetter;
        private Action<StringDef> _titleSetter;
        private ActionLeaderBoardKills _action;

        public ActionLeaderBoardDeaths(ActionLeaderBoardKills action) : base(null, 0) {
            _action = action;
            int[] mapping = ActionLeaderBoardKills.getTextMapping();
            int unitIndex = mapping[0];
            int titleIndex = mapping[1];
            int offset = UnitVanillaDef.AllUnits.Length;
            offset = UnitDef.AllUnits.Length;


            Func<UnitVanillaDef> unitTypeGetter = () => _action.get<UnitVanillaDef>(unitIndex);
            Action<UnitVanillaDef> unitTypeSetter = (UnitVanillaDef unit) => _action.set<UnitVanillaDef>(unit, unitIndex);
            Func<StringDef> titleGetter = () => _action.get<StringDef>(titleIndex);
            Action<StringDef> titleSetter = (StringDef str) => _action.set<StringDef>(str, titleIndex);
       
            _unitTypeGetter = () => {
                int killsIndex = unitTypeGetter().getIndex();
                int index = killsIndex < offset ? killsIndex : killsIndex - offset;
                return UnitVanillaDef.getByIndex( index );
            };
            _unitTypeSetter = (UnitVanillaDef unit)=> { unitTypeSetter(UnitVanillaDef.getByIndex(unit.getIndex() + offset)); };
            _titleGetter = titleGetter;
            _titleSetter = titleSetter;
       
    }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            UnitVanillaDef[] lst = new UnitVanillaDef[UnitVanillaDef.AllUnits.Length - 5];
            for(int i = 0; i < lst.Length; i++) {
                lst[i] = UnitVanillaDef.AllUnits[i + 5];
            }

            return new TriggerDefinitionPart[] {
                new TriggerDefinitionLabel("Show LeaderBoard for most deaths of "),
                new TriggerDefinitionGeneralDef<UnitVanillaDef>(_unitTypeGetter, _unitTypeSetter, ()=>UnitVanillaDef.AllUnits[5], lst),
                new TriggerDefinitionLabel(" .\nDisplay label: "),
                new TriggerDefinitionInputText(_titleGetter,_titleSetter)
            };
        }

        public override string ToSaveString() {
            return _action.ToSaveString();
        }
    }

    class ActionSetDeaths : GenericAction {

        public ActionSetDeaths(RawActionSetDeaths trigger) : base("Set deaths", new int[] { 1, 3, 5, 7 }, (GenericAction instance, TriggerDefinitionPart[] arg) => remapper2(instance, arg), (GenericAction instance, SaveableItem[] arg) => remapper1(instance, arg), trigger ) { }
        /*
        private PlayerDef pd;
        private UnitVanillaDef ud;
        */

        private static SaveableItem[] remapper1(GenericAction genericInstance, SaveableItem[] original) {
           /*
            ActionSetDeaths instance = (ActionSetDeaths)genericInstance;
            instance.pd = PlayerDef.getByIndex((original[0] as IntDef).getIndex());
            instance.ud = UnitVanillaDef.getByIndex((original[1] as IntDef).getIndex());

            original[0] = instance.pd;
            original[1] = instance.ud;
            */
            return original;
        }

        private static TriggerDefinitionPart[] remapper2(GenericAction genericInstance, TriggerDefinitionPart[] remoriginal) {
            TriggerDefinitionIntAmount rawPlayer = (TriggerDefinitionIntAmount)remoriginal[1];
            TriggerDefinitionIntAmount rawUnit = (TriggerDefinitionIntAmount)remoriginal[7];

            ActionSetDeaths instance = (ActionSetDeaths)genericInstance;

            //remoriginal[1] = new TriggerDefinitionGeneralDef<PlayerDef>(()=>instance.pd, (PlayerDef pd)=> { instance.pd = pd; }, PlayerDef.getDefaultValue, PlayerDef.AllPlayers);
            //remoriginal[7] = new TriggerDefinitionGeneralDef<UnitVanillaDef>(() => instance.ud, (UnitVanillaDef ud) => { instance.ud = ud; }, UnitVanillaDef.getDefaultValue, UnitVanillaDef.AllUnits);


            remoriginal[1] = new TriggerDefinitionGeneralDef<PlayerDef>(() => {
                return PlayerDef.getByIndex(rawPlayer.getValue());
            }, (PlayerDef pd) => { rawPlayer.setValue(pd.getIndex()); }, PlayerDef.getDefaultValue, PlayerDef.AllPlayers);
            remoriginal[7] = new TriggerDefinitionGeneralDef<UnitVanillaDef>(() => UnitVanillaDef.getByIndex(rawUnit.getValue()), (UnitVanillaDef ud) => { rawUnit.setValue(ud.getIndex()); }, UnitVanillaDef.getDefaultValue, UnitVanillaDef.AllUnits);
            remoriginal[1].updatePropertiesFromAnotherSource(rawPlayer);
            remoriginal[7].updatePropertiesFromAnotherSource(rawUnit);
            return remoriginal;
        }
    }

    class ActionCenterView : GenericAction {

        public ActionCenterView(Parser parser) : base(parser, "Center View", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Center view for current player at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0 };
        }

    }

    class ActionComment : GenericAction {

        public ActionComment(Parser parser) : base(parser, "Comment", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Comment: "),
                TriggerContentType.COMMENT,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0 };
        }

    }

    class ActionCreateUnit : GenericAction {

        public ActionCreateUnit(Parser parser) : base(parser, "Create Unit", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Create "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 3, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 3, 1, 0, 2 };
        }

    }

    class ActionCreateUnitWithProperties : GenericAction {

        public ActionCreateUnitWithProperties(Parser parser) : base(parser, "Create Unit with Properties", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Create "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(".\nApply "),
                TriggerContentType.PROPERTIES,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 3, 0, 4 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7, 9 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 3, 1, 0, 2, 4 };
        }

    }

    class ActionDefeat : GenericAction {

        public ActionDefeat(Parser parser) : base(parser, "Defeat", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("End scenario in defeat for current player."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionDisplayTextMessage : GenericAction {

        public ActionDisplayTextMessage(Parser parser) : base(parser, "Display Text Message", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Display "),
                TriggerContentType.MESSAGE_TYPE,
                TriggerContentType.VISUAL_LABEL(" for current player:\n"),
                TriggerContentType.MESSAGE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1 };
        }

    }

    class ActionDraw : GenericAction {

        public ActionDraw(Parser parser) : base(parser, "Draw", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("End the scenario in a draw for all players."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionGiveUnitsToPlayer : GenericAction {

        public ActionGiveUnitsToPlayer(Parser parser) : base(parser, "Give Units to Player", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Give "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" owned by "),
                TriggerContentType.SOURCE_PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(" to "),
                TriggerContentType.TARGET_PLAYER,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 3, 2, 0, 4, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7, 9 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 4, 1, 0, 3 };
        }

    }

    class ActionKillUnit : GenericAction {

        public ActionKillUnit(Parser parser) : base(parser, "Kill Unit", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Kill all "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 1, 0 };
        }

    }

    class ActionKillUnitAtLocation : GenericAction {

        public ActionKillUnitAtLocation(Parser parser) : base(parser, "Kill Unit At Location", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Kill "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 0, 3 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 0, 3 };
        }

    }

    class ActionLeaderBoardControlAtLocation : GenericAction {

        public ActionLeaderBoardControlAtLocation(Parser parser) : base(parser, "Leader Board Control At Location", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show LeaderBoard for most control of "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(".\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 2, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 0, 1 };
        }

    }

    class ActionLeaderBoardControl : GenericAction {

        public ActionLeaderBoardControl(Parser parser) : base(parser, "Leader Board Control", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show LeaderBoard for most control of "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(".\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 1, 0 };
        }

    }

    class ActionLeaderboardGreed : GenericAction {

        public ActionLeaderboardGreed(Parser parser) : base(parser, "Leader Board Greed", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show Greed LeaderBoard for player closest to accumulation of "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" ore and gas."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0 };
        }

    }

    public class ActionLeaderBoardKills : GenericAction {

        public ActionLeaderBoardKills(Parser parser) : base(parser, "Leader Board Kills", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show Leader Board for most kills of "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" .\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 1, 0 };
        }

    }

    class ActionLeaderBoardPoints : GenericAction {

        public ActionLeaderBoardPoints(Parser parser) : base(parser, "Leader Board Points", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show LeaderBoard for most "),
                TriggerContentType.SCOREBOARD,
                TriggerContentType.VISUAL_LABEL(".\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 1, 0 };
        }

    }

    class ActionLeaderBoardResources : GenericAction {

        public ActionLeaderBoardResources(Parser parser) : base(parser, "Leader Board Resources", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show LeaderBoard for accumulating of most "),
                TriggerContentType.RESOURCES,
                TriggerContentType.VISUAL_LABEL(".\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 1, 0 };
        }

    }

    class ActionLeaderboardComputerPlayers : GenericAction {

        public ActionLeaderboardComputerPlayers(Parser parser) : base(parser, "Leader Board Computer Players", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.ENABLE_STATE,
                TriggerContentType.VISUAL_LABEL(" use of computer players in leaderboard calculations."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0 };
        }

    }

    class ActionLeaderboardGoalControlAtLocation : GenericAction {

        public ActionLeaderboardGoalControlAtLocation(Parser parser) : base(parser, "Leader board Goal Control At Location", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show LeaderBoard for player closest to control of "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" of "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(".\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 3, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 3, 1, 0, 2 };
        }

    }

    class ActionLeaderboardGoalControl : GenericAction {

        public ActionLeaderboardGoalControl(Parser parser) : base(parser, "Leader board Goal Control", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show LeaderBoard for player closest to control of "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" of "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(".\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 0 };
        }

    }

    class ActionLeaderboardGoalKills : GenericAction {

        public ActionLeaderboardGoalKills(Parser parser) : base(parser, "Leader board Goal Kills", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show LeaderBoard for player closest to "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" kills of "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(".\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 0 };
        }

    }

    class ActionLeaderboardGoalPoints : GenericAction {

        public ActionLeaderboardGoalPoints(Parser parser) : base(parser, "Leader board Goal Points", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show LeaderBoard for player closest to "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.SCOREBOARD,
                TriggerContentType.VISUAL_LABEL(".\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 0 };
        }

    }

    class ActionLeaderboardGoalResources : GenericAction {

        public ActionLeaderboardGoalResources(Parser parser) : base(parser, "Leader board Goal Resources", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show LeaderBoard for player closest to accumulation of "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.RESOURCES,
                TriggerContentType.VISUAL_LABEL(".\nDisplay label: "),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 2, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 0, 1 };
        }

    }

    class ActionMinimapPing : GenericAction {

        public ActionMinimapPing(Parser parser) : base(parser, "Minimap Ping", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show minimap ping for current player at "),
                TriggerContentType.LOCATION,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0 };
        }

    }

    class ActionModifyUnitEnergy : GenericAction {

        public ActionModifyUnitEnergy(Parser parser) : base(parser, "Modify Unit Energy", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Set energy points for "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" owned by "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(" to "),
                TriggerContentType.PERCENTAGE,
                TriggerContentType.VISUAL_LABEL("%."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 3, 1, 0, 4, 2 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7, 9 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 4, 0, 3 };
        }

    }

    class ActionModifyUnitHangerCount : GenericAction {

        public ActionModifyUnitHangerCount(Parser parser) : base(parser, "Modify Unit Hanger Count", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Add at most "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" to hanger for "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(" owned by "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 3, 1, 4, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7, 9 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 4, 2, 0, 1, 3 };
        }

    }

    class ActionModifyUnitHitPoints : GenericAction {

        public ActionModifyUnitHitPoints(Parser parser) : base(parser, "Modify Unit Hit Points", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Set hit points for "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" owned by "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(" to "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL("%."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 3, 1, 0, 4, 2 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7, 9 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 4, 0, 3 };
        }

    }

    class ActionModifyUnitResourceAmount : GenericAction {

        public ActionModifyUnitResourceAmount(Parser parser) : base(parser, "Modify Unit Resource Amount", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Set resources amount for "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" resource sources owned by "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(" to "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL("%."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 0, 3, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 1, 3, 0, 2 };
        }

    }

    class ActionModifyUnitShieldPoints : GenericAction {

        public ActionModifyUnitShieldPoints(Parser parser) : base(parser, "Modify Unit Shield Points", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Set shield points for "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" owned by "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(" to "),
                TriggerContentType.PERCENTAGE,
                TriggerContentType.VISUAL_LABEL("%."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 3, 1, 0, 4, 2 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7, 9 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 4, 0, 3 };
        }

    }

    class ActionMoveLocation : GenericAction {

        public ActionMoveLocation(Parser parser) : base(parser, "Move Location", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Center location labeled "),
                TriggerContentType.SOURCE_LOCATION,
                TriggerContentType.VISUAL_LABEL(" on "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" owned by "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.TARGET_LOCATION,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 0, 3 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 0, 3 };
        }

    }

    class ActionPlayWAV : GenericAction {

        public ActionPlayWAV(Parser parser) : base(parser, "Play WAV", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Play wav file "),
                TriggerContentType.WAVPATH,
                TriggerContentType.VISUAL_LABEL(" with "),
                TriggerContentType.WAVPATH_PARAM,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1 };
        }

    }

    class ActionMoveUnit : GenericAction {

        public ActionMoveUnit(Parser parser) : base(parser, "Move Unit", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Move "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.SOURCE_LOCATION,
                TriggerContentType.VISUAL_LABEL(" to "),
                TriggerContentType.TARGET_LOCATION,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 0, 3, 4 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7, 9 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 0, 3, 4 };
        }

    }

    class ActionMuteUnitSpeech : GenericAction {

        public ActionMuteUnitSpeech(Parser parser) : base(parser, "Mute Unit Speech", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Mute all non-trigger unit sounds for current player."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionOrder : GenericAction {

        public ActionOrder(Parser parser) : base(parser, "Order", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Issue order to all "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" owned by "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.SOURCE_LOCATION,
                TriggerContentType.VISUAL_LABEL(":\n"),
                TriggerContentType.ORDER,
                TriggerContentType.VISUAL_LABEL(" to "),
                TriggerContentType.TARGET_LOCATION,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 0, 2, 4, 3 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7, 9 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 1, 0, 2, 4, 3 };
        }

    }

    class ActionPauseGame : GenericAction {

        public ActionPauseGame(Parser parser) : base(parser, "Pause Game", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Pause the game."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionPauseTimer : GenericAction {

        public ActionPauseTimer(Parser parser) : base(parser, "Pause Timer", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Pause the countdown timer."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionPreserveTrigger : GenericAction {

        public ActionPreserveTrigger(Parser parser) : base(parser, "Preserve Trigger", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Preserve trigger."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionRemoveUnit : GenericAction {

        public ActionRemoveUnit(Parser parser) : base(parser, "Remove Unit", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Remove all "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 1, 0 };
        }

    }

    class ActionRemoveUnitAtLocation : GenericAction {

        public ActionRemoveUnitAtLocation(Parser parser) : base(parser, "Remove Unit At Location", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Remove "),
                TriggerContentType.UNITS_QUANTITY,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 2, 1, 0, 3 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 0, 3 };
        }

    }

    class ActionRunAIScript : GenericAction {

        public ActionRunAIScript(Parser parser) : base(parser, "Run AI Script", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Execute AI script "),
                TriggerContentType.AISCRIPT,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0 };
        }

    }

    class ActionRunAIScriptAtLocation : GenericAction {

        public ActionRunAIScriptAtLocation(Parser parser) : base(parser, "Run AI Script At Location", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Execute AI script "),
                TriggerContentType.AISCRIPTAT,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1 };
        }

    }

    class ActionSetAllianceStatus : GenericAction {

        public ActionSetAllianceStatus(Parser parser) : base(parser, "Set Alliance Status", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Set "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" to "),
                TriggerContentType.ALLIANCE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1 };
        }

    }

    class ActionSetCountdownTimer : GenericAction {

        public ActionSetCountdownTimer(Parser parser) : base(parser, "Set Countdown Timer", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Modify Countdown Timer: "),
                TriggerContentType.SET_QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" seconds."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1 };
        }

    }

    class ActionSetDoodadState : GenericAction {

        public ActionSetDoodadState(Parser parser) : base(parser, "Set Doodad State", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.ENABLE_STATE,
                TriggerContentType.VISUAL_LABEL(" doodad state for "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 3, 1, 0, 2 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2, 4, 6 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 3, 0 };
        }

    }

    class ActionSetInvincibility : GenericAction {

        public ActionSetInvincibility(Parser parser) : base(parser, "Set Invincibility", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.ENABLE_STATE,
                TriggerContentType.VISUAL_LABEL(" invincibility for "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 3, 1, 0, 2 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2, 4, 6 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 2, 1, 3, 0 };
        }

    }

    class ActionSetMissionObjectives : GenericAction {

        public ActionSetMissionObjectives(Parser parser) : base(parser, "Set Mission Objectives", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Set Mission Objective to:\n"),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0 };
        }

    }

    class ActionSetNextScenario : GenericAction {

        public ActionSetNextScenario(Parser parser) : base(parser, "Set Next Scenario", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Load "),
                TriggerContentType.TITLE,
                TriggerContentType.VISUAL_LABEL(" after completion of current game."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0 };
        }

    }

    class ActionSetResources : GenericAction {

        public ActionSetResources(Parser parser) : base(parser, "Set Resources", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Modify resources for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(": "),
                TriggerContentType.SET_QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.RESOURCES,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1, 2, 3 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1, 2, 3 };
        }

    }

    class ActionSetScore : GenericAction {

        public ActionSetScore(Parser parser) : base(parser, "Set Score", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Modify score for "),
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(": "),
                TriggerContentType.SET_QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.SCOREBOARD,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1, 2, 3 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1, 2, 3 };
        }

    }

    class ActionSetSwitch : GenericAction {

        public ActionSetSwitch(Parser parser) : base(parser, "Set Switch", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.SWITCH_SET_STATE,
                TriggerContentType.VISUAL_LABEL(" switch '"),
                TriggerContentType.SWITCH_NAME,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 1, 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 1, 0 };
        }

    }

    class ActionTalkingPortrait : GenericAction {

        public ActionTalkingPortrait(Parser parser) : base(parser, "Talking Portrait", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Show "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" talking to current player for "),
                TriggerContentType.TIMEOUT,
                TriggerContentType.VISUAL_LABEL(" miliseconds."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1 };
        }

    }

    class ActionTransmission : GenericAction {

        public ActionTransmission(Parser parser) : base(parser, "Transmission", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Send transmission to current player from "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL(".\nPlay "),
                TriggerContentType.WAVPATH,
                TriggerContentType.VISUAL_LABEL(".\nModify transmission duration: "),
                TriggerContentType.SET_QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" miliseconds.\nDisplay "),
                TriggerContentType.MESSAGE_TYPE,
                TriggerContentType.VISUAL_LABEL(" the following text:\n"),
                TriggerContentType.TITLE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 5, 2, 3, 6, 4, 7, 0, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5, 7, 9, 11, 13, 15 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 6, 7, 1, 2, 4, 0, 3, 5 };
        }

    }

    class ActionUnmuteUnitSpeech : GenericAction {

        public ActionUnmuteUnitSpeech(Parser parser) : base(parser, "Unmute Unit Speech", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Unmute all non-trigger unit sounds for current player."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionUnpauseGame : GenericAction {

        public ActionUnpauseGame(Parser parser) : base(parser, "Unpause Game", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Unapuse the game."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionUnpauseTimer : GenericAction {

        public ActionUnpauseTimer(Parser parser) : base(parser, "Unpause Timer", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Unapuse the countdown timer."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionVictory : GenericAction {

        public ActionVictory(Parser parser) : base(parser, "Victory", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("End scenario in victory for current player."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { };
        }


        public static int[] getUsefulMapping() {
            return new int[] { };
        }


        public static int[] getTrigMapping() {
            return new int[] { };
        }

    }

    class ActionWait : GenericAction {

        public ActionWait(Parser parser) : base(parser, "Wait", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Wait for "),
                TriggerContentType.TIMEOUT,
                TriggerContentType.VISUAL_LABEL(" miliseconds."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0 };
        }

    }

}
