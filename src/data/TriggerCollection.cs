using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

/*
#define getResultTextSize YoMommaIsSoFatThatSheCausesStackOverflow
#define SCXToTXT YoMomaIsSoUglyThatSheScaresBlindPeople
#define TXTToSCX YoMomaIsSoOldThatHerFirstChristmasWasFirstChristmasEver
#define SCXToTXTUnsafe YoMommaIsSoPoorSheCantEvenPayAttention
#define SCXToTXTUnsafeFree YoMommaIsSoFatWhenSheTookABusSheSatNextToEveryone
*/

namespace StarcraftEPDTriggers.src.data {

    public class TriggerCollection {

        public readonly Dictionary<PlayerDef, List<Trigger>> TriggerData = new Dictionary<PlayerDef, List<Trigger>>();

        public readonly List<Trigger> AllTrigers = new List<Trigger>();

        public TriggerCollection() {

        }

        [DllImport("TriggerMaster.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "#3")]
        public static extern int getResultTextSize(string inputFileNameoutputString);

        
        [DllImport("TriggerMaster.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "#2")]
        unsafe public static extern bool SCXToTXT(string inputFileName, char* result, int resultLength);


        [DllImport("TriggerMaster.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "#1")]
        public static extern bool TXTToSCX(string outputFileNanem, string triggers, bool append);

        [DllImport("TriggerMaster.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "#5")]
        public static extern IntPtr SCXToTXTUnsafe(string inputFilename);

        [DllImport("TriggerMaster.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "#4")]
        public static extern bool SCXToTXTUnsafeFree(IntPtr outputFileNanem);

        private string runExtractionSlow(string path) {
            int len = getResultTextSize(path);
            if (len == 0) {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            bool ok = false;
            unsafe
            {
                byte[] myArray = new byte[len];
                fixed (byte* ptr = &myArray[0]) {
                    ok = SCXToTXT(path, (char*) ptr, len);
                }
                if (ok) {
                    for(int i=0;i<len;i++) {
                        sb.Append((char)myArray[i]);
                    }
                }

            }
            if(ok) {
                return sb.ToString();
            } else {
                return null;
            }
        }

        private string runExtractionFast(string path) {
            string returner = null;
            unsafe
            {
                IntPtr ret = SCXToTXTUnsafe(path);
                if(ret != IntPtr.Zero) {
                    string str = Marshal.PtrToStringAnsi(ret);
                    SCXToTXTUnsafeFree((IntPtr)ret);
                    returner = str;
                }
            }
            return returner;
        }

        public bool load(string filename) {
            string triggersText = runExtractionFast(filename);
            if(triggersText != null) {
                triggersText = triggersText.Replace("\n", "\r\n");
                //File.WriteAllText("loaded.txt", triggersText);
                Scanner scanner = new src.Scanner(triggersText);
                Parser parser = new src.Parser(scanner);
                AllTrigers.Clear();
                TriggerData.Clear();
                if (parser.parse()) {
                    List<Trigger> triggers = parser.getTriggers();
                    foreach (Trigger trigger in triggers) {
                        AllTrigers.Add(trigger);
                        foreach (PlayerDef affected in trigger.getAffectedPlayers()) {
                            if (!TriggerData.ContainsKey(affected)) {
                                TriggerData[affected] = new List<Trigger>();
                            }
                            TriggerData[affected].Add(trigger);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public bool save(string thr_path, string triggersText) {
            //File.WriteAllText("saved.txt", triggersText);
            return TXTToSCX(thr_path, triggersText, false);
        }

        public void UpdateAffecteds(Trigger trigger) {
            TriggerCreated(trigger);
            
            List<PlayerDef> affecteds = trigger.getAffectedPlayers();
            foreach (KeyValuePair<PlayerDef, List<Trigger>> subList in TriggerData) {
                if(subList.Value.Contains(trigger)) { // Player owns it
                    if(!affecteds.Contains(subList.Key)) { // Player shouldn't own it
                        subList.Value.Remove(trigger); // Remove it from player
                    }
                } else { // Player doesn't own it
                    if (affecteds.Contains(subList.Key)) { // Player should own it
                        subList.Value.Add(trigger); // Add it to player
                    }
                }
            }
            
        }

        public Trigger loadAndInsertAfter(string str, Trigger trigger) {
            Scanner scanner = new src.Scanner(str);
            Parser parser = new src.Parser(scanner);
            if (parser.parse(true)) {
                List<Trigger> triggers = parser.getTriggers();
                if(triggers.Count == 1) {
                    Trigger trig = triggers[0];
                    InsertAfter(AllTrigers, trigger, trig);
                    foreach (KeyValuePair<PlayerDef, List<Trigger>> subList in TriggerData) {
                        InsertAfter(subList.Value, trigger, trig);
                    }
                    return trig;
                }
            }
            throw new NotImplementedException();
        }

        public void Delete(Trigger trigger) {
            AllTrigers.Remove(trigger);
            foreach(KeyValuePair<PlayerDef, List<Trigger>> subList in TriggerData) {
                subList.Value.Remove(trigger);
            }
        }

        private void InsertAfter(List<Trigger> lst, Trigger trigInTheList, Trigger trigBeingInserted) {
            int index = lst.IndexOf(trigInTheList);
            if(index >=0 && index < lst.Count) {
                //lst.Add(trigBeingInserted);
                //Move(lst, trigBeingInserted, index + 1);
                lst.Insert(index + 1, trigBeingInserted);
            } else {
                lst.Add(trigBeingInserted);
            }
        }

        public static void Move<T>(List<T> lst, T trigger, int indexOffset) {
            int index = lst.IndexOf(trigger);
            if (index >= 0 && index < lst.Count) {
                if (indexOffset < 0) { // Move up
                    if (index >=  -indexOffset && index < lst.Count) { // indexOffset < 0 !
                        lst[index] = lst[index + indexOffset];
                        lst[index + indexOffset] = trigger;
                    }
                } else  // Move down
                    if (index >= 0 && index < lst.Count - indexOffset) {
                    lst[index] = lst[index + indexOffset];
                    lst[index + indexOffset] = trigger;
                }
            }
        }

        public void MoveUp(Trigger trigger) {
            Move<Trigger>(AllTrigers, trigger, -1);
            foreach (KeyValuePair<PlayerDef, List<Trigger>> subList in TriggerData) {
                Move(subList.Value, trigger, -1);
            }
        }

        public void MoveDown(Trigger trigger) {
            Move<Trigger>(AllTrigers, trigger, 1);
            foreach (KeyValuePair<PlayerDef, List<Trigger>> subList in TriggerData) {
                Move(subList.Value, trigger, 1);
            }
        }

        public void TriggerCreated(Trigger trig) {
            if (!AllTrigers.Contains(trig)) {
                AllTrigers.Add(trig);
            }
            foreach (PlayerDef affected in trig.getAffectedPlayers()) {
                if (!TriggerData.ContainsKey(affected)) {
                    TriggerData[affected] = new List<Trigger>();
                }
                if (!TriggerData[affected].Contains(trig)) {
                    TriggerData[affected].Add(trig);
                }
            }
        }
    }
}
