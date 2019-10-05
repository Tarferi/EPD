using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcraftEPDTriggers.src.parser {
    class MyBetterReader {

        private string _str;

        private int pos;
        private int max;

        public string getSubstr(int length) {
            string ret = _str.Substring(pos, length);
            pos += length;
            return ret;
        }

        public string Past { get { return _str.Substring(0, pos); } }
        public string Future { get { return _str.Substring(pos); } }

        public MyBetterReader(string str) {
            _str = str;
            pos = 0;
            max = _str.Length - 1;
        }

        public int getPosition() {
            return pos;
        }

        public char read() {
            if (pos > max) {
                return (char) 0;
            } else {
                char chr = _str[pos];
                pos++;
                return chr;
            }
        }

        public void unread() {
            unread(1);
        }

        public void unread(int howmany) {
            pos -= howmany;
        }

        public void Close() {
            _str = null;
            pos = 0;
            max = 0;
        }
    }
}
