using System;
using System.Collections.Generic;

namespace StarcraftEPDTriggers.src.data {
    class History {

        public static List<String> getHistory() {
            List<String> lst = new List<String>();
            try {
                String[] history = System.IO.File.ReadAllLines("history.log");
                foreach(String item in history) {
                    if(System.IO.File.Exists(item)) {
                        lst.Add(item);
                    }
                }
            } catch (Exception) {
            }
            return lst;
        }

        public static void addToHistory(String str) {
            if (System.IO.File.Exists(str)) {
                List<String> history = getHistory();
                if (!history.Contains(str)) {
                    history.Insert(0, str);
                    System.IO.File.WriteAllLines("history.log", history);
                } else {
                    if (!history[0].Equals(str)) {
                        history.Remove(str);
                        history.Insert(0, str);
                        System.IO.File.WriteAllLines("history.log", history);
                    }
                }
            }
        }

    }
}
