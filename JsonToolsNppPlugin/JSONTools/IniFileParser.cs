using System.Collections.Generic;
using System;


namespace JSON_Tools.JSON_Tools
{
    public class IniParserException : Exception
    {
        public IniParserException(string message) : base(message) { }
    }

    public class IniFileParser
    {
        /// <summary>
        /// if true, ${SECTION:KEY} is replaced with the value associated with key KEY in section SECTION<br></br>
        /// ${KEY} is replaced with the value associated with key KEY <i>if KEY is in the same section</i>
        /// </summary>
        public bool UseInterpolation;
        public int utf8ExtraBytes { get; private set; }
        public int ii { get; private set; }
        public int utf8Pos { get { return ii + utf8ExtraBytes; } }
        public List<Comment> comments;

        public IniFileParser(bool useInterpolation)
        {
            ii = 0;
            utf8ExtraBytes = 0;
            comments = new List<Comment>();
            UseInterpolation = useInterpolation;
        }

        public void Reset()
        {
            ii = 0;
            utf8ExtraBytes = 0;
            comments.Clear();
        }

        //public static readonly Regex INI_TOKENIZER = new Regex(
        //    @"^\s*\[([^\r\n]+)\]\s*$|" + // section name
        //    @"^\s*([^\r\n:=]+)\s*[:=](.+)$|" + // key and value within section
        //    @"^\s*[;#](.+)$|" + // comment
        //    @"^\s*(?![;#])(\S+)$" // multi-line continuation of value
        //);

        public JObject Parse(string text)
        {
            Reset();
            var doc = new Dictionary<string, JNode>();
            return new JObject();
            while (ii < text.Length)
            {

            }
        }

        public JObject ParseSection(string text, int sectionIndent)
        {
            var section = new Dictionary<string, JNode>();
            return new JObject();
            while (ii < text.Length)
            {
                int curIndent = ConsumeInsignificantChars(text);
                char c = text[ii];
            }
            int startOfSectionName = ii;
        }

        public static JObject Interpolate(JObject iniJson)
        {
            return iniJson;
        }

        /// <summary>
        /// Consume comments and whitespace until the next character that is not
        /// '#', ';', or whitespace.
        /// </summary>
        /// <returns>the number of characters since the last newline</returns>
        private int ConsumeInsignificantChars(string inp)
        {
            bool isFirstNonWhiteSpaceCharOfLine = false;
            int charsSinceLastNewline = -1;
            while (ii < inp.Length)
            {
                char c = inp[ii];
                charsSinceLastNewline++;
                switch (c)
                {
                case '\n':
                    ii++;
                    charsSinceLastNewline = 0;
                    isFirstNonWhiteSpaceCharOfLine = true;
                    break;
                case ';':
                case '#':
                    if (!isFirstNonWhiteSpaceCharOfLine)
                        throw new IniParserException("JsonTools cannot parse ini file comments where the comment start character ('#' or ';') is not the first non-whitespace character of the line");
                    int commentStartUtf8 = utf8Pos;
                    int commentContentStartII = ii + 1;
                    ConsumeLine(inp);
                    int commentContentEndII = JsonParser.EndOfPreviousLine(inp, ii, commentContentStartII);
                    comments.Add(new Comment(inp.Substring(commentContentStartII, commentContentEndII - commentContentStartII), false, commentStartUtf8));
                    break;
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
                    utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
                    ii++;
                    break;
                default: return charsSinceLastNewline;
                }
            }
            return charsSinceLastNewline;
        }

        /// <summary>
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
                    return;
                utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
            }
        }

        private int ConsumeLineAndGetLastNonWhitespace(string inp)
        {
            int lastNonWhitespace = ii;
            return 0;
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
                    utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(c);
                    break;
                case '\n':
                    ii++;
                    return lastNonWhitespace;
                default:
                    lastNonWhitespace = ii;
                    break;
                }
                ii++;
            }
        }
    }
}
