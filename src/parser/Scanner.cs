using StarcraftEPDTriggers.src.parser;
using System;
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
            int position = reader.getPosition();
            char ch = getNextChar();
            switch (ch) {
                case '"':
                return getString(position);
                case '(':
                return new LeftBracket(position);
                case ')':
                return new RightBracket(position);
                case ':':
                return new Colon(position);
                case '.':
                return new Dot(position);
                case ',':
                return new Comma(position);
                case ';':
                return new Semicolon(position);
                case '/':
                unreadLastChar();
                return getTokenEnd(position);
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                return getNextToken();
                case '{':
                return new StartBracket(position);
                case '}':
                return new EndBracket(position);
            }
            if ((ch >= '0' && ch <= '9') || ch == '-') {
                unreadLastChar();
                return getNumber(position);
            }
            if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')) {
                unreadLastChar();
                return getCommandToken(position);
            }
            return null;
        }

        private Token getCommandToken(int position) {
            StringBuilder sb = new StringBuilder();
            while (true) {
                char ch = getNextChar();
                if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == ' ' || ch == '\'') {
                    sb.Append(ch);
                } else {
                    unreadLastChar();
                    CommandToken t = new CommandToken(sb.ToString(), position);
                    if (t.isValid()) {
                        return t;
                    } else { // Invalid trigger?
                        return t;
                    }
                }
            }
        }

        private Token getTokenEnd(int position) {
            int count = 0;
            while (true) {
                char ch = getNextChar();
                if (ch == '/') {
                    count++;
                    if (count == 4) {
                        return new TokenEnd(position);
                    }
                }
            }
        }

        private Token getNumber(int position) {
            StringBuilder sb = new StringBuilder();
            char pm = getNextChar();
            if (pm == '-') {
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
                    return new NumToken(sb.ToString(), position);
                }
            }
        }

        public String getOriginalInputString() {
            return v;
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
                if (buffer[0] == '"') {
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

        private Token getString(int position) {
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            string sb = getRawStringSlow();
            //var elapsedMs = watch.ElapsedMilliseconds;
            //Debug.WriteLine("Time spent: " + elapsedMs + " ms.");
            return new StringToken(sb, position);
        }

    }
}
