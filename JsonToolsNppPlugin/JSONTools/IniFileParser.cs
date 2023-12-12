using System.Collections.Generic;
using System;

namespace JSON_Tools.JSON_Tools
{
    public class IniParserException : FormatException
    {
        public new string Message { get; set; }
        public char CurChar { get; set; }
        public int Position { get; set; }
        public int LineNum { get; set; }

        /// <summary>lineNum arg is 0-based, but LineNum attribute will be 1-based</summary>
        /// <param name="Message"></param>
        /// <param name="c">character where error happened (approx)</param>
        /// <param name="pos">position in utf-8 encoding of input</param>
        /// <param name="lineNum">the 0-based line number (this will be incremented by 1 to the 1-based line number in the LineNum attribute)</param>
        public IniParserException(string Message, char c, int pos, int lineNum)
        {
            this.Message = Message;
            this.CurChar = c;
            this.Position = pos;
            this.LineNum = lineNum + 1;
        }

        public override string ToString()
        {
            return $"{Message} at line {LineNum}, position {Position} (char {JsonLint.CharDisplay(CurChar)})";
        }

        public JsonLint ToJsonLint()
        {
            return new JsonLint(Message, Position, CurChar, ParserState.FATAL);
        }
    }

    public class IniFileParser
    {
        ///// <summary>
        ///// if true, ${SECTION:KEY} is replaced with the value associated with key KEY in section SECTION<br></br>
        ///// ${KEY} is replaced with the value associated with key KEY <i>if KEY is in the same section</i>
        ///// </summary>
        //public bool UseInterpolation;
        
        public int utf8ExtraBytes { get; private set; }
        public int ii { get; private set; }
        public int lineNum { get; private set; }
        public int utf8Pos { get { return ii + utf8ExtraBytes; } }
        public List<Comment> comments;

        public IniFileParser(/*bool useInterpolation*/)
        {
            ii = 0;
            utf8ExtraBytes = 0;
            lineNum = 0;
            comments = new List<Comment>();
            //UseInterpolation = useInterpolation;
        }

        public void Reset()
        {
            ii = 0;
            lineNum = 0;
            utf8ExtraBytes = 0;
            comments.Clear();
        }

        public JObject Parse(string inp)
        {
            Reset();
            var doc = new Dictionary<string, JNode>();
            while (ii < inp.Length)
            {
                ConsumeInsignificantChars(inp);
                char c = inp[ii++];
                if (c == '[')
                {
                    // section header
                    int startOfHeader = ii;
                    int headerStartUtf8 = ii - 1 + utf8ExtraBytes;
                    int endOfHeader = -1;
                    // find end of header, then keep going to start of next line,
                    // throwing an exception if newline comes before end of header,
                    // or non-whitespace comes between end of header and newline
                    while (ii < inp.Length)
                    {
                        c = inp[ii];
                        switch (c)
                        {
                        case ']':
                            endOfHeader = ii;
                            break;
                        case ' ':
                        case '\t':
                        case '\r':
                            break;
                        case '\u2028':
                        case '\u2029':
                        case '\ufeff':
                        case '\xa0':
                        case '\u1680':
                        case '\u2000':
                        case '\u2001':
                        case '\u2002':
                        case '\u2003':
                        case '\u2004':
                        case '\u2005':
                        case '\u2006':
                        case '\u2007':
                        case '\u2008':
                        case '\u2009':
                        case '\u200A':
                        case '\u202F':
                        case '\u205F':
                        case '\u3000':
                            utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
                            break;
                        case '\n':
                            ii++;
                            lineNum++;
                            if (endOfHeader < 0)
                                throw new IniParserException("Opening '[' and closing ']' of section header were not on the same line", c, headerStartUtf8 - 1, lineNum);
                            else
                                goto headerFinished;
                        default:
                            if (endOfHeader > 0)
                                throw new IniParserException("Non-whitespace between end of section header and end of header line", '[', headerStartUtf8 - 1, lineNum);
                            utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
                            break;
                        }
                        ii++;
                    }
                headerFinished:
                    string header = inp.Substring(startOfHeader, endOfHeader - startOfHeader);
                    if (doc.ContainsKey(header))
                    {
                        throw new IniParserException($"Document has duplicate section header [{header}]", c, headerStartUtf8 - 1, lineNum);
                    }
                    JObject section = ParseSection(inp, header);
                    section.position = headerStartUtf8;
                    doc[header] = section;
                }
                else
                    throw new IniParserException("Expected section header", c, ii, lineNum);
            }
            return new JObject(0, doc);
        }

        public JObject ParseSection(string inp, string header)
        {
            var section = new Dictionary<string, JNode>();
            while (ii < inp.Length)
            {
                int indent = ConsumeInsignificantChars(inp);
                int keyStartUtf8 = utf8Pos;
                if (ii >= inp.Length)
                    break;
                char c = inp[ii];
                if (c == '[')
                    // start of the next section
                    break;
                KeyValuePair<string, JNode> kv = ParseKeyValuePair(inp, indent);
                if (section.ContainsKey(kv.Key))
                {
                    throw new IniParserException($"Section [{header}] has duplicate key \"{kv.Key}\"", c, keyStartUtf8, lineNum);
                }
                section[kv.Key] = kv.Value;
            }
            return new JObject(0, section);
        }

        public KeyValuePair<string, JNode> ParseKeyValuePair(string inp, int keyIndent)
        {
            if (ii >= inp.Length)
                throw new IniParserException("EOF when expecting key-value pair", inp[inp.Length - 1], inp.Length - 1, lineNum);
            int startOfKey = ii;
            char startC = inp[ii];
            int keyStartUtf8 = utf8Pos;
            int endOfKey = ii;
            bool foundKeyValueSep = false;
            while (ii < inp.Length)
            {
                char c = inp[ii];
                switch (c)
                {
                case '=':
                case ':':
                    foundKeyValueSep = true;
                    break;
                case ' ':
                case '\t':
                case '\r':
                    break;
                case '\u2028':
                case '\u2029':
                case '\ufeff':
                case '\xa0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                    utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
                    break;
                case '\n':
                    if (!foundKeyValueSep)
                        throw new IniParserException("No '=' or ':' on same line as key", startC, keyStartUtf8, lineNum);
                    // key with empty value
                    goto exitKeyLoop;
                default:
                    if (foundKeyValueSep)
                    {
                        // first non-whitespace char after ':' or '='
                        goto exitKeyLoop;
                    }
                    // last non-whitespace seen so far before ':' or '='
                    utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
                    endOfKey = ii;
                    break;
                }
                ii++;
            }
        exitKeyLoop:
            string key = inp.Substring(startOfKey, endOfKey + 1 - startOfKey);
            int startOfValue = ii;
            int endOfValue = ii;
            while (ii < inp.Length)
            {
                int lineStart = ii;
                ConsumeLine(inp);
                endOfValue = JsonParser.EndOfPreviousLine(inp, ii, lineStart);
                // tentatively ending value at end of last line
                // we'll check to see if the current line has non-whitespace, non-comment at a greater indent than the start of the key
                // if it does, the value will continue to the end of this line.
                int indent = ConsumeToIndent(inp);
                if (ii < inp.Length)
                {
                    if (indent > keyIndent)
                    {
                        char c = inp[ii];
                        if (c == ';' || c == '#')
                            // start of comment, so value is over
                            break;
                    }
                    else
                        break; // value ends with line that has indent <= keyIndent.
                }
            }
            string valueStr = inp.Substring(startOfValue, endOfValue - startOfValue);
            if (key.IndexOf('=') >= 0 || key.IndexOf(':') >= 0)
                throw new IniParserException("Invalid key is empty or contains ':' or '='", inp[startOfValue], startOfValue, lineNum);
            // node is assigned position at start of key (not the start of the line the key is on)
            JNode valueNode = new JNode(valueStr, keyStartUtf8);
            return new KeyValuePair<string, JNode>(key, valueNode);
        }

        //public static JObject Interpolate(JObject iniJson)
        //{
        //    return iniJson;
        //}

        /// <summary>
        /// Consume comments and whitespace until EOF or the next character that is not
        /// whitespace or the beginning of a comment.<br></br>
        /// Return the indent of the current position, as described by ConsumeToIndent below.
        /// </summary>
        /// <returns>the number of characters since the last newline</returns>
        private int ConsumeInsignificantChars(string inp)
        {
            int indent = 0;
            while (ii < inp.Length)
            {
                char c = inp[ii];
                switch (c)
                {
                case ' ':
                case '\t':
                case '\r':
                case '\u2028':
                case '\u2029':
                case '\ufeff':
                case '\xa0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                    indent++;
                    ii++;
                    utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
                    break;
                case '\n':
                    lineNum++;
                    ii++;
                    indent = 0;
                    break;
                case ';':
                case '#':
                    int commentStartUtf8 = utf8Pos;
                    int commentContentStartII = ii + 1;
                    ConsumeLine(inp);
                    int commentContentEndII = JsonParser.EndOfPreviousLine(inp, ii, commentContentStartII);
                    comments.Add(new Comment(inp.Substring(commentContentStartII, commentContentEndII - commentContentStartII), false, commentStartUtf8));
                    indent = 0;
                    break;
                default: return indent;
                }
            }
            return 0;
        }

        /// <summary>
        /// Move ii to the first character of the next line (after the next newline), or to EOF<br></br>
        /// note that both this and ConsumeLineAndGetLastNonWhitespace treat '\n' as the newline character,
        /// because this works for both Windows CRLF and Unix LF. However, Macintosh CR is not supported.
        /// </summary>
        /// <param name="inp"></param>
        private void ConsumeLine(string inp)
        {
            while (ii < inp.Length)
            {
                char c = inp[ii++];
                if (c == '\n')
                {
                    lineNum++;
                    return;
                }
                utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
            }
        }

        /// <summary>
        /// consume characters from this line until '\n', the next non-whitespace, or EOF.<br></br>
        /// Return 0 if we stopped on a newline or EOF.<br></br>
        /// If we stopped on non-whitespace, return the number of characters between our starting ii (the beginning of the line)
        /// and the current ii.<br></br>
        /// For example, if we start in position 1 of the string "\n \tf", we will get an indent of 2 because there are
        /// two whitespace chars (' ' and '\t' count equally) between our starting position and the "f".<br></br>
        /// But if we start at position 1 of '   \n', we will return 0 because the line had no non-whitespace.
        /// </summary>
        /// <returns></returns>
        private int ConsumeToIndent(string inp)
        {
            int indent = 0;
            while (ii < inp.Length)
            {
                char c = inp[ii];
                switch (c)
                {
                case ' ':
                case '\t':
                case '\r':
                    break;
                case '\u2028':
                case '\u2029':
                case '\ufeff':
                case '\xa0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                    utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
                    break;
                case '\n':
                    // consumed entire line without seeing non-whitespace
                    // we treat a line with no non-whitespace characters as having an indent of 0
                    // which terminates multi-line strings.
                    ii++;
                    lineNum++;
                    return 0;
                default:
                    // non-whitespace, return # chars since start of line
                    return indent;
                }
                ii++;
                indent++;
            }
            return 0;
        }
    }
}
