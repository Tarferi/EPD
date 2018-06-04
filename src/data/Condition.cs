using System;
using StarcraftEPDTriggers.src;
using System.Text;
using static StarcraftEPDTriggers.src.TriggerContentType;
using StarcraftEPDTriggers.src.data;

namespace StarcraftEPDTriggers {

    public abstract class Condition : TriggerContent {

        public Condition(Parser parser, int num) : base(parser, num) { }

        public override string ToString() {
            return ToSaveString();
        }

        public virtual string ToSaveString() {
            StringBuilder sb = new StringBuilder();
            if (!this.isEnabled()) {
                sb.Append(";");
            }
            if (this is GenericCondition) {
                SaveableItem[] parts = ((GenericCondition)this).getUsefulDefinitionParts();
                sb.Append(((GenericCondition)this).Name + "(");
                for (int i = 0; i < parts.Length; i++) {
                    SaveableItem part = parts[i];
                    sb.Append(part.ToSaveString());
                    if (i != parts.Length - 1) {
                        sb.Append(", ");
                    }
                }
                sb.Append(");");
            } else if(this is EPDCondition) {
                EPDCondition epd = (EPDCondition)this;
                sb.Append(epd.ToString());
            } else {
                throw new NotImplementedException();
            }
            return sb.ToString();
        }
    }

    public class GeneralTriggerContentInternalCalculator {
        
        private TriggerDefinitionPart[] visualParts;
        private SaveableItem[] _contents;
        private string _name;
        private int[] _textMapping;

        public string Name { get { return _name; } }

        public GeneralTriggerContentInternalCalculator(Parser parser, string name, TriggerContentTypeDescriptor[] types, int[] usefulMapping, int[] atextMapping, int[] trigMapping, Func<TriggerContentTypeDescriptor[], int> getArgCount, TriggerContent content) {
            int count = getArgCount(types);
            int counter = 0;
            _textMapping = atextMapping;

            _contents = new SaveableItem[count];
            _name = name;
            visualParts = new TriggerDefinitionPart[types.Length];
            for (int i = 0; i < types.Length; i++) {
                int localCounter = counter;
                TriggerContentTypeDescriptor type = types[i];
                if (type is TriggerContentTypeDescriptorVisual) {
                    string label = ((TriggerContentTypeDescriptorVisual)type).Content;
                    if (label.Equals("\n")) {
                        visualParts[i] = new TriggerDefinitionNewLine();
                    } else {
                        visualParts[i] = (TriggerDefinitionPart)TriggerContentType.VISUAL_LABEL(label).Read(null, 0);
                    }
                } else {
                    _contents[_textMapping[counter]] = type.Read(content, _textMapping[counter]);
                    counter++;
                    visualParts[i] = type.GetDefinitionPart(() => _contents[_textMapping[localCounter]], (SaveableItem whatevs) => { _contents[_textMapping[localCounter]] = whatevs; });
                }
            }
        }

        internal void setRawObject<T>(T obj, int index) where T : SaveableItem {
            _contents[index] = obj;
        }

        internal object getRawObject(int index) {
            return _contents[index];
        }

        public GeneralTriggerContentInternalCalculator(string name,int[] textMapping, Func<TriggerDefinitionPart[], TriggerDefinitionPart[]> remapVisuals, Func<SaveableItem[], SaveableItem[]> remapSaveables, GeneralTriggerContentInternalCalculator theOther) {
            _name = name;
            _textMapping = textMapping;
            _contents = remapSaveables(theOther._contents);
            visualParts = remapVisuals(theOther.visualParts);
        }

        public TriggerDefinitionPart[] getDefinitionParts() {
            return visualParts;
        }

        public SaveableItem[] getUsefulDefinitionParts() {
            SaveableItem[] ret = new SaveableItem[_textMapping.Length];
            for (int i=0;i<_textMapping.Length;i++) {
                ret[i] = _contents[i];
            }
            return ret;
        }

        internal int[] getTextMapping() {
            return _textMapping;
        }
    }

    public class GenericCondition : Condition {

        private GeneralTriggerContentInternalCalculator _obj;

        public string Name { get { return _obj.Name; } }

        public T get<T>(int index) where T : SaveableItem {
            object obj = _obj.getRawObject(index);
            if (obj is T) {
                return (T)obj;
            }
            throw new NotImplementedException();
        }

        public void set<T>(T obj, int index) where T : SaveableItem {
            object aobj = _obj.getRawObject(index);
            if (aobj is T) {
                _obj.setRawObject(obj, index);
                return;
            }
            throw new NotImplementedException();
        }

        private static int getArgCount(TriggerContentTypeDescriptor[] types) {
            int count = 0;
            foreach (TriggerContentTypeDescriptor type in types) {
                if (!(type is TriggerContentTypeDescriptorVisual)) {
                    count++;
                }
            }
            return count;
        }

        public GenericCondition(Parser parser, string name, TriggerContentTypeDescriptor[] types, int[] usefulMapping, int[] textMapping, int[] trigMapping) : base(parser, getArgCount(types)) {
            _obj = new GeneralTriggerContentInternalCalculator(parser, name, types, usefulMapping, textMapping, trigMapping, getArgCount, this);
        }

        protected override TriggerDefinitionPart[] getInnerDefinitionParts() {
            return _obj.getDefinitionParts();
        }

        public SaveableItem[] getUsefulDefinitionParts() {
            return _obj.getUsefulDefinitionParts();
        }
    }

    class ConditionAccumulate : GenericCondition {

        public ConditionAccumulate(Parser parser) : base(parser, "Accumulate", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" accumulates "),
                TriggerContentType.QUANTIFIER,
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
            return new int[] { 0, 2, 4, 6 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1, 2, 3 };
        }

    }
    
    class ConditionAlways : GenericCondition {

        public ConditionAlways(Parser parser) : base(parser, "Always", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Always"),
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

    class ConditionBring : GenericCondition {

        public ConditionBring(Parser parser) : base(parser, "Bring", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" brings "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL(" at "),
                TriggerContentType.LOCATION,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 3, 4, 1, 2 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2, 4, 6, 8 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 3, 4, 1, 2 };
        }

    }

    class ConditionCommand : GenericCondition {

        public ConditionCommand(Parser parser) : base(parser, "Command", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" commands "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 2, 3, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2, 4, 6 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 3, 1, 2 };
        }

    }

    class ConditionCommandTheLeast : GenericCondition {

        public ConditionCommandTheLeast(Parser parser) : base(parser, "Command the Least", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player commands the least "),
                TriggerContentType.UNIT_TYPE,
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

    class ConditionCommandTheLeastAt : GenericCondition {

        public ConditionCommandTheLeastAt(Parser parser) : base(parser, "Command the Least At", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player commands the least "),
                TriggerContentType.UNIT_TYPE,
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

    class ConditionCommandTheMost : GenericCondition {

        public ConditionCommandTheMost(Parser parser) : base(parser, "Command the Most", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player commands the most "),
                TriggerContentType.UNIT_TYPE,
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

    class ConditionCommandsTheMostAt : GenericCondition {

        public ConditionCommandsTheMostAt(Parser parser) : base(parser, "Commands the Most At", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player commands the most "),
                TriggerContentType.UNIT_TYPE,
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

    class ConditionCountdownTimer : GenericCondition {

        public ConditionCountdownTimer(Parser parser) : base(parser, "Countdown Timer", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Countdown timer is "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" game seconds."),
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

    class ConditionDeaths : GenericCondition {

        public ConditionDeaths(Parser parser) : base(parser, "Deaths", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" has suffered "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" deaths of "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public UnitVanillaDef getUnitDef() {
            return get<UnitVanillaDef>(1);
        }

        public PlayerDef getPlayerDef() {
            return get<PlayerDef>(0);
        }

        public IntDef getAmount() {
            return get<IntDef>(3);
        }

        public Quantifier getQuantifier() {
            return get<Quantifier>(2);
        }


        public static int[] getTextMapping() {
            return new int[] { 0, 2, 3, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2, 4, 6 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 3, 1, 2 };
        }

    }

    class ConditionElapsedTime : GenericCondition {

        public ConditionElapsedTime(Parser parser) : base(parser, "Elapsed Time", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Alapsed scenario time is "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" game seconds."),
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

    class ConditionHighestScore : GenericCondition {

        public ConditionHighestScore(Parser parser) : base(parser, "Highest Score", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player has highest score "),
                TriggerContentType.SCOREBOARD,
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

    class ConditionKill : GenericCondition {

        public ConditionKill(Parser parser) : base(parser, "Kill", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" kills "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" of "),
                TriggerContentType.UNIT_TYPE,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 2, 3, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2, 4, 6 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 3, 1, 2 };
        }

    }

    class ConditionLeastKills : GenericCondition {

        public ConditionLeastKills(Parser parser) : base(parser, "Least Kills", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player has least kills of "),
                TriggerContentType.UNIT_TYPE,
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

    class ConditionLeastResources : GenericCondition {

        public ConditionLeastResources(Parser parser) : base(parser, "Least Resources", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player has least "),
                TriggerContentType.RESOURCES,
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

    class ConditionLowestScore : GenericCondition {

        public ConditionLowestScore(Parser parser) : base(parser, "Lowest Score", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player has lowest score "),
                TriggerContentType.SCOREBOARD,
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

    class ConditionMostKills : GenericCondition {

        public ConditionMostKills(Parser parser) : base(parser, "Most Kills", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player has most kills of "),
                TriggerContentType.UNIT_TYPE,
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

    class ConditionMostResources : GenericCondition {

        public ConditionMostResources(Parser parser) : base(parser, "Most Resources", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Current player has most "),
                TriggerContentType.RESOURCES,
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

    class ConditionNever : GenericCondition {

        public ConditionNever(Parser parser) : base(parser, "Never", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Never"),
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

    class ConditionOpponents : GenericCondition {

        public ConditionOpponents(Parser parser) : base(parser, "Opponents", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" has "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" opponents remaining in the game."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1, 2 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2, 4 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1, 2 };
        }

    }

    class ConditionScore : GenericCondition {

        public ConditionScore(Parser parser) : base(parser, "Score", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.PLAYER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.SCOREBOARD,
                TriggerContentType.VISUAL_LABEL(" score is "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1, 2, 3 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2, 4, 6 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1, 2, 3 };
        }

    }

    class ConditionSwitch : GenericCondition {

        public ConditionSwitch(Parser parser) : base(parser, "Switch", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.SWITCH_NAME,
                TriggerContentType.VISUAL_LABEL(" is "),
                TriggerContentType.SWITCH_STATE,
            };

        }

        public static int[] getTextMapping() {
            return new int[] { 0, 1 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1 };
        }

    }

    public class ConditionCustom : GenericCondition {

        
        public ConditionCustom(Parser parser) : base(parser, "Custom", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL(" ")
            };
        }


        public static int[] getTextMapping() {
            return new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 0, 2, 4, 6, 8, 10, 12, 14, 16 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        }


    }

    public class ConditionMemory : GenericCondition {

        public ConditionMemory(Parser parser) : base(parser, "Memory", getComponents(), getUsefulMapping(), getTextMapping(), getTrigMapping()) { }

        public static TriggerContentTypeDescriptor[] getComponents() {
            return new TriggerContentTypeDescriptor[] {
                TriggerContentType.VISUAL_LABEL("Modify memory value at address "),
                TriggerContentType.ADDRESS,
                TriggerContentType.VISUAL_LABEL(": "),
                TriggerContentType.QUANTIFIER,
                TriggerContentType.VISUAL_LABEL(" "),
                TriggerContentType.AMOUNT,
                TriggerContentType.VISUAL_LABEL("."),
            };

        }


        public Quantifier getQuantifier() {
            return get<Quantifier>(1);
        }

        public void setQuantifier(Quantifier quant) {
            set<Quantifier>(quant, 1);
        }

        public IntDef getAddress() {
            IntDef playerID = get<IntDef>(0);
            return new IntDef((playerID.getIndex() + 1452249) * 4, playerID.UseHex);
        }

        public void setAddress(IntDef address) {
            IntDef playerID = new IntDef((address.getIndex() / 4) - 1452249, address.UseHex);
            set<IntDef>(playerID, 0);
        }

        public IntDef getAmount() {
            return get<IntDef>(2);
        }

        public void setAmount(IntDef amount) {
            set<IntDef>(amount, 2);
        }


        public static int[] getTextMapping() {
            return new int[] { 0, 1, 2 };
        }


        public static int[] getUsefulMapping() {
            return new int[] { 1, 3, 5 };
        }


        public static int[] getTrigMapping() {
            return new int[] { 0, 1, 2 };
        }

    }


}
