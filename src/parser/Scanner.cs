using StarcraftEPDTriggers.src.parser;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace StarcraftEPDTriggers.src {

    public class Scanner {
        private string v;
        MyBetterReader reader;

        public Scanner(string v) {
            this.v = v;
            reader = new MyBetterReader(v);
        }

        public void close() {
            reader.Close();
        }

        private char getNextChar() {
            return reader.read();
        }

        private void unreadLastChar() {
            reader.unread();
        }
        

        public Token getNextToken() {
            char ch = getNextChar();
            switch (ch) {
                case '"':
                    return getString();
                case '(':
                    return new LeftBracket();
                case ')':
                    return new RightBracket();
                case ':':
                    return new Colon();
                case '.':
                    return new Dot();
                case ',':
                    return new Comma();
                case ';':
                    return new Semicolon();
                case '/':
                    unreadLastChar();
                    return getTokenEnd();
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    return getNextToken();
                case '{':
                    return new StartBracket();
                case '}':
                    return new EndBracket();
            }
            if ((ch >= '0' && ch <= '9') || ch == '-') {
                unreadLastChar();
                return getNumber();
            }
            if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')) {
                unreadLastChar();
                return getCommandToken();
            }
            return null;
        }

        private Token getCommandToken() {
            StringBuilder sb = new StringBuilder();
            while (true) {
                char ch = getNextChar();
                if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == ' ' || ch == '\'') {
                    sb.Append(ch);
                } else {
                    unreadLastChar();
                    CommandToken t = new CommandToken(sb.ToString());
                    if (t.isValid()) {
                        return t;
                    } else { // Invalid trigger?
                        return t;
                    }
                }
            }
        }

        private Token getTokenEnd() {
            int count = 0;
            while (true) {
                char ch = getNextChar();
                if (ch == '/') {
                    count++;
                    if (count == 4) {
                        return new TokenEnd();
                    }
                }
            }
        }

        private Token getNumber() {
            StringBuilder sb = new StringBuilder();
            char pm = getNextChar();
            if(pm == '-') {
                sb.Append("-");
            } else {
                unreadLastChar();
            }
            while (true) {
                char ch = getNextChar();
                if (ch >= '0' && ch <= '9') {
                    sb.Append(ch);
                } else {
                    unreadLastChar();
                    return new NumToken(sb.ToString());
                }
            }
        }

        private string getRawStringSlow() {
            StringBuilder sb = new StringBuilder();
            while (true) {
                char ch = getNextChar();
                if (ch == '\\') {
                    char b = getNextChar();
                    sb.Append("\\");
                    sb.Append(b);
                } else if (ch != '"') {
                    sb.Append(ch);
                } else {
                    return sb.ToString();
                }
            }
        }

        private string getRawStringFast() {
            StringBuilder sb = new StringBuilder();
            bool expectLiteral = false;
            while (true) {
                string buffer = reader.getSubstr(64);
                if(buffer[0] == '"') {
                    if (!expectLiteral) { // Empty buffer
                        reader.unread(buffer.Length - 2); // Exclude the " from next reading
                        return sb.ToString();
                    }
                }
                if (buffer.Contains("\"")) { // String contains ", which can be terminator, or literal
                    int lastOffset = 0;
                    while (true) { // For all occurances of that string, check if that is the terminator
                        int currentIndex = buffer.IndexOf("\"", lastOffset + 1);
                        if (currentIndex == -1) {
                            break;
                        }
                        lastOffset = currentIndex;
                        if (currentIndex > 0) { // Not first character, previous says if this is literal or not
                            if (buffer[currentIndex - 1] != '\"') { // Not literal, this is the end of string
                                string resultBuffer = buffer.Substring(0, currentIndex); // Exclude the final " char
                                sb.Append(resultBuffer);
                                reader.unread(buffer.Length - resultBuffer.Length - 1); // Include the final " char
                                return sb.ToString();
                            }
 
                        }
                    }
                    sb.Append(buffer);
                } else { // Safe, if last char is not "\"
                    sb.Append(buffer);
                    expectLiteral = buffer[buffer.Length - 1] == '\\';
                }
            }
        }

        private Token getString() {
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            string sb = getRawStringSlow();
            //var elapsedMs = watch.ElapsedMilliseconds;
            //Debug.WriteLine("Time spent: " + elapsedMs + " ms.");
            return new StringToken(sb);
        }
    }
}
