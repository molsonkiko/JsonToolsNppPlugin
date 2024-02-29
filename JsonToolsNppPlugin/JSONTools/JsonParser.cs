/*
A parser and linter for JSON.
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using JSON_Tools.Utils;

namespace JSON_Tools.JSON_Tools
{
    /// <summary>
    /// An exception that may be thrown when the parser encounters syntactically invalid JSON.
    /// Subclasses FormatException.
    /// </summary>
    public class JsonParserException : FormatException
    {
        public new string Message { get; set; }
        public char CurChar { get; set; }
        public int Position { get; set; }

        public JsonParserException(string Message, char c, int pos)
        {
            this.Message = Message;
            this.CurChar = c;
            this.Position = pos;
        }

        public override string ToString()
        {
            return $"{Message} at position {Position} (char {JsonLint.CharDisplay(CurChar)})";
        }
    }

    /// <summary>
    /// A syntax error caught and logged by the linter.
    /// </summary>
    public struct JsonLint
    {
        public string message;
        /// <summary>
        /// the position of the error in the UTF-8 encoding of the document
        /// </summary>
        public int pos;
        /// <summary>
        /// the UTF-16 character where the error began
        /// </summary>
        public char curChar;
        public ParserState severity;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="pos">the position of the error in the UTF8 encoding of the JSON document</param>
        /// <param name="curChar">the UTF-16 character where the error began</param>
        /// <param name="severity"></param>
        public JsonLint(string message, int pos, char curChar, ParserState severity)
        {
            this.message = message;
            this.pos = pos;
            this.curChar = curChar;
            this.severity = severity;
        }

        public override string ToString()
        {
            return $"Syntax error (severity = {severity}) at position {pos} (char {CharDisplay(curChar)}): {message}";
        }

        /// <summary>
        /// Display a char wrapped in singlequotes in a way that makes it easily recognizable.
        /// For example, '\n' is represented as '\n' and '\'' is represented as '\''.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string CharDisplay(char c)
        {
            switch (c)
            {
                case '\x00': return "'\\x00'";
                case '\t': return "'\\t'";
                case '\r': return "'\\r'";
                case '\n': return "'\\n'";
                case '\'': return "'\\''";
                default: return $"'{c}'";
            }
        }

        /// <summary>
        /// the object {"message": this.message, "position": this.pos, "severity": this.severity}
        /// </summary>
        public JNode ToJson()
        {
            return new JObject(0, new Dictionary<string, JNode>
            {
                ["message"] = new JNode(message),
                ["position"] = new JNode((long)pos),
                ["severity"] = new JNode(severity.ToString()),
            });
        }
    }

    /// <summary>
    /// Any errors above this level are reported by a JsonParser.<br></br>
    /// The integer value of a state reflects how seriously the input deviates from the original JSON spec.
    /// </summary>
    public enum LoggerLevel
    {
        /// <summary>
        /// Valid according to the <i>exact</i> original JSON specification.<br></br>
        /// This is pretty annoying to use because horizontal tabs ('\t') are forbidden.
        /// </summary>
        STRICT,
        /// <summary>
        /// Valid according to a <i>slightly relaxed</i> version of the JSON specification.<br></br>
        /// In addition to the JSON spec, it tolerates:<br></br>
        /// * control characters with ASCII codes below 0x20
        /// </summary>
        OK,
        /// <summary>
        /// Everything at the OK level plus NaN, Infinity, and -Infinity.
        /// </summary>
        NAN_INF,
        /// <summary>
        /// Everything at the NAN_INF level, plus JavaScript comments.<br></br>
        /// Note that this differs slightly from the standard JSONC spec
        /// because NaN and +/-Infinity are not part of that spec.
        /// </summary>
        JSONC,
        /// <summary>
        /// JSON that follows the specification described here: https://json5.org/<br></br>
        /// Includes everything at the JSONC level, plus various things, including:<br></br>
        /// * unquoted object keys<br></br>
        /// * comma after last element of iterable<br></br>
        /// * singlequoted strings
        /// </summary>
        JSON5,
    }

    /// <summary>
    /// the sequence of states the JSON parser can be in.<br></br>
    /// The first five states (STRICT, OK, NAN_INF, JSONC, JSON5) have the same
    /// meaning as in the LoggerLevel enum.
    /// The last two states (BAD and FATAL) reflect errors that are
    /// <i>always logged</i> and thus do not belong in the LoggerLevel enum.
    /// </summary>
    public enum ParserState
    {
        /// <summary>
        /// see LoggerLevel.STRICT
        /// </summary>
        STRICT,
        /// <summary>
        /// see LoggerLevel.OK
        /// </summary>
        OK,
        /// <summary>
        /// see LoggerLevel.NAN_INF
        /// </summary>
        NAN_INF,
        /// <summary>
        /// see LoggerLevel.JSONC
        /// </summary>
        JSONC,
        /// <summary>
        /// see LoggerLevel.JSON5
        /// </summary>
        JSON5,
        /// <summary>
        /// JSON with syntax errors that my parser can handle but that should always be logged, such as:<br></br>
        /// * unterminated strings<br></br>
        /// * missing commas after array elements<br></br>
        /// * Python-style single-line comments (start with '#')
        /// </summary>
        BAD,
        /// <summary>
        /// errors that are always fatal, such as:<br></br>
        /// * recursion depth hits the recursion limit<br></br>
        /// * empty input
        /// </summary>
        FATAL,
        /// <summary>
        /// reserved for JSON Schema validation errors
        /// </summary>
        SCHEMA
    }

    /// <summary>
    /// Parses a JSON document into a <seealso cref="JNode"/> tree.
    /// </summary>
    public class JsonParser
    {
        /// <summary>
        /// need to track recursion depth because stack overflow causes a panic that makes Notepad++ crash
        /// </summary>
        public const int MAX_RECURSION_DEPTH = 512;

        #region JSON_PARSER_ATTRS
        /// <summary>
        /// If true, any strings in the standard formats of ISO 8601 dates (yyyy-MM-dd) and datetimes (yyyy-MM-dd hh:mm:ss.sss)
        ///  will be automatically parsed as the appropriate type.
        ///  Not currently supported. May never be.
        /// </summary>
        public bool parseDatetimes;

        /// <summary>
        /// If line is not null, most forms of invalid syntax will not cause the parser to stop,<br></br>
        /// but instead the syntax error will be recorded in a list.
        /// </summary>
        public List<JsonLint> lint;

        /// <summary>
        /// position in JSON string
        /// </summary>
        public int ii;

        private bool _rememberComments;
        
        public bool rememberComments { 
            get { return _rememberComments; }
            set
            {
                _rememberComments = value;
                comments = value ? new List<Comment>() : null;
            }
        }

        public List<Comment> comments;

        /// <summary>
        /// the number of extra bytes in the UTF-8 encoding of the text consumed
        /// so far.<br></br>
        /// For example, if<br></br>
        /// "words": "Thế nào rồi?"<br></br>
        /// has been consumed, utf8ExtraBytes is 5 because all the characters
        /// are 1-byte ASCII<br></br>
        /// except 'ồ' and 'ế', which are both 3-byte characters<br></br>
        /// and 'à' which is a 2-byte character
        /// </summary>
        private int utf8ExtraBytes;

        public ParserState state { get; private set; }
        
        /// <summary>
        /// errors above this 
        /// </summary>
        public LoggerLevel loggerLevel;

        /// <summary>
        /// Any error above the logger level causes an error to be thrown.<br></br>
        /// If false, parse functions will return everything logged up until a fatal error
        /// and will parse everything if there were no fatal errors.<br></br>
        /// Present primarily for backwards compatibility.
        /// </summary>
        public bool throwIfLogged;

        public bool throwIfFatal;

        /// <summary>
        /// attach ExtraJNodeProperties to each JNode parsed
        /// </summary>
        public bool includeExtraProperties;
        
        /// <summary>
        /// the number of bytes in the utf-8 representation
        /// before the current position in the current document
        /// </summary>
        public int utf8Pos { get { return ii + utf8ExtraBytes; } }

        public bool fatal
        {
            get { return state == ParserState.FATAL; }
        }

        public bool hasLogged
        {
            get { return (int)state > (int)loggerLevel; }
        }

        public bool exitedEarly
        {
            get { return fatal || (throwIfLogged && hasLogged); }
        }

        /// <summary>
        /// if parsing failed, this will be the final error logged. If parsing succeeded, this is null.
        /// </summary>
        public JsonLint? fatalError
        {
            get
            {
                if (exitedEarly)
                    return lint[lint.Count - 1];
                return null;
            }
        }

        public JsonParser(LoggerLevel loggerLevel = LoggerLevel.NAN_INF, bool parseDatetimes = false, bool throwIfLogged = true, bool throwIfFatal = true, bool rememberComments = false)
            //, bool includeExtraProperties = false)
        {
            this.loggerLevel = loggerLevel;
            this.parseDatetimes = parseDatetimes;
            this.throwIfLogged = throwIfLogged;
            this.throwIfFatal = throwIfFatal;
            //this.includeExtraProperties = includeExtraProperties;
            ii = 0;
            lint = new List<JsonLint>();
            state = ParserState.STRICT;
            utf8ExtraBytes = 0;
            this.rememberComments = rememberComments;
        }

        #endregion
        #region HELPER_METHODS

        public static int ExtraUTF8Bytes(char c)
        {
            return (c < 128)
                ? 0
                : (c > 2047)
                    ? // check if it's in the surrogate pair region
                      (c >= 0xd800 && c <= 0xdfff)
                        ? 1 // each member of a surrogate pair counts as 2 bytes
                            // for a total of 4 bytes for the unicode characters over 65535
                        : 2 // other chars bigger than 2047 take up 3 bytes
                    : 1; // non-ascii chars less than 2048 take up 2 bytes
        }

        /// <summary>
        /// gets the number of extra bytes (greater than end - start) in inp
        /// beteeen 0-based index start (inclusive) and end (exclusive)
        /// </summary>
        public static int ExtraUTF8BytesBetween(string inp, int start, int end)
        {
            int count = 0;
            for (int ii = start; ii < end; ii++)
            {
                count += ExtraUTF8Bytes(inp[ii]);
            }
            return count;
        }

        /// <summary>
        /// Set the parser's state to severity, unless the state was already higher.<br></br>
        /// If the severity is above the parser's loggerLevel:<br></br>
        ///     * if throwIfLogged or (FATAL and throwIfFatal), throw a JsonParserException<br></br>
        ///     * otherwise, add new JsonLint with the appropriate message, position, curChar, and severity.<br></br>
        /// Return whether current state is FATAL.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inp"></param>
        /// <param name="pos"></param>
        /// <param name="severity"></param>
        /// <exception cref="JsonParserException"/>
        private bool HandleError(string message, string inp, int pos, ParserState severity)
        {
            if (state < severity)
                state = severity;
            bool fatal = this.fatal;
            if ((int)severity > (int)loggerLevel)
            {
                char c = (pos >= inp.Length)
                    ? '\x00'
                    : inp[pos];
                lint.Add(new JsonLint(message, utf8Pos, c, severity));
                if (throwIfLogged || (fatal && throwIfFatal))
                {
                    throw new JsonParserException(message, c, utf8Pos);
                }
            }
            return fatal;
        }

        /// <summary>
        /// consumes characters until a '\n' is found (and consumes that too)
        /// </summary>
        /// <param name="inp"></param>
        private void ConsumeLine(string inp)
        {
            while (ii < inp.Length)
            {
                char c = inp[ii++];
                if (c == '\n')
                    return;
                utf8ExtraBytes += ExtraUTF8Bytes(c);
            }
        }

        /// <summary>assumes that ii is at the start of a line or at the end of the document</summary>
        /// <param name="inp"></param>
        public static int EndOfPreviousLine(string inp, int ii, int start)
        {
            int pos = ii >= inp.Length ? inp.Length - 1 : ii - 1;
            if (pos <= start)
            {
                if (start == inp.Length - 1)
                    // we special case this so that a substring from start to EndOfPreviousLine
                    // will still have a length of 1
                    return inp.Length;
                return start;
            }
            char c = inp[pos];
            if (c != '\n') // end of line is the end of the document
                return pos + 1;
            if (pos >= 1 && inp[pos - 1] == '\r')
                return pos - 1; // last newline was CRLF
            return pos; // last newline was LF
        }

        /// <summary>
        /// Consume comments and whitespace until the next character that is not
        /// '#', '/', or whitespace.
        /// Return false if an unacceptable error occurred.
        /// </summary>
        /// <param name="inp"></param>
        /// <returns></returns>
        private bool ConsumeInsignificantChars(string inp)
        {
            while (ii < inp.Length)
            {
                char c = inp[ii];
                switch (c)
                {
                case ' ':
                case '\t':
                case '\r':
                case '\n': ii++; break;
                case '/':
                    int commentStartUtf8 = utf8Pos;
                    int commentContentStartII = ii + 2;
                    int commentContentEndII;
                    bool isMultiline;
                    ii++;
                    if (ii == inp.Length)
                    {
                        HandleError("Expected JavaScript comment after '/'", inp, inp.Length - 1, ParserState.FATAL);
                        return false;
                    }
                    HandleError("JavaScript comments are not part of the original JSON specification", inp, ii, ParserState.JSONC);
                    c = inp[ii];
                    if (c == '/')
                    {
                        isMultiline = false;
                        ConsumeLine(inp);
                        commentContentEndII = EndOfPreviousLine(inp, ii, commentContentStartII);
                    }
                    else if (c == '*')
                    {
                        isMultiline = true;
                        bool commentEnded = false;
                        while (ii < inp.Length - 1)
                        {
                            c = inp[ii++];
                            if (c == '*')
                            {
                                if (inp[ii] == '/')
                                {
                                    commentEnded = true;
                                    ii++;
                                    break;
                                }
                            }
                            else
                                utf8ExtraBytes += ExtraUTF8Bytes(c);
                        }
                        if (!commentEnded)
                        {
                            HandleError("Unterminated multi-line comment", inp, inp.Length - 1, ParserState.BAD);
                            ii++;
                            return false;
                        }
                        commentContentEndII = ii - 2;
                    }
                    else
                    {
                        HandleError("Expected JavaScript comment after '/'", inp, ii, ParserState.FATAL);
                        return false;
                    }
                    if (rememberComments)
                        comments.Add(new Comment(inp.Substring(commentContentStartII, commentContentEndII - commentContentStartII), isMultiline, commentStartUtf8));
                    break;
                case '#':
                    // Python-style single-line comment
                    commentStartUtf8 = utf8Pos;
                    commentContentStartII = ii + 1;
                    HandleError("Python-style '#' comments are not part of any well-accepted JSON specification",
                            inp, ii, ParserState.BAD);
                    ConsumeLine(inp);
                    commentContentEndII = EndOfPreviousLine(inp, ii, commentContentStartII);
                    if (rememberComments)
                        comments.Add(new Comment(inp.Substring(commentContentStartII, commentContentEndII - commentContentStartII), false, commentStartUtf8));
                    break;
                case '\u2028': // line separator
                case '\u2029': // paragraph separator
                case '\ufeff': // Byte-order mark
                // the next 16 (plus '\x20', normal whitespace) comprise the unicode space separator category
                case '\xa0': // non-breaking space
                case '\u1680': // Ogham Space Mark
                case '\u2000': // En Quad
                case '\u2001': // Em Quad
                case '\u2002': // En Space
                case '\u2003': // Em Space
                case '\u2004': // Three-Per-Em Space
                case '\u2005': // Four-Per-Em Space
                case '\u2006': // Six-Per-Em Space
                case '\u2007': // Figure Space
                case '\u2008': // Punctuation Space
                case '\u2009': // Thin Space
                case '\u200A': // Hair Space
                case '\u202F': // Narrow No-Break Space
                case '\u205F': // Medium Mathematical Space
                case '\u3000': // Ideographic Space
                    HandleError("Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5", inp, ii, ParserState.JSON5);
                    utf8ExtraBytes += ExtraUTF8Bytes(c);
                    ii++;
                    break;
                default: return true;
                }
            }
            return true;
        }

        /// <summary>
        /// read a hexadecimal integer representation of length `length` at position `index` in `inp`.
        /// sets the parser's state to FATAL if the integer is not valid hexadecimal
        /// or if `index` is less than `length` from the end of `inp`.
        /// </summary>
        private int ParseHexChar(string inp, int length)
        {
            if (ii >= inp.Length - length)
            {
                HandleError("Could not find valid hexadecimal of length " + length,
                                              inp, ii, ParserState.FATAL);
                return -1;
            }
            int end = ii + length > inp.Length
                ? inp.Length
                : ii + length;
            var hexNum = inp.Substring(ii, end - ii);
            ii = end - 1;
            // the -1 is because ParseString increments by 1 after every escaped sequence anyway
            int charval;
            try
            {
                charval = int.Parse(hexNum, NumberStyles.HexNumber);
            }
            catch
            {
                HandleError("Could not find valid hexadecimal of length " + length,
                                              inp, ii, ParserState.FATAL);
                return -1;
            }
            return charval;
        }

        /// <summary>
        /// check if char c is a control character (less than 0x20)
        /// and then check if it is '\n' or the null character or negative.
        /// Handle errors accordingly.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="inp"></param>
        /// <param name="ii"></param>
        /// <param name="startUtf8_pos"></param>
        /// <returns></returns>
        private bool HandleCharErrors(int c, string inp, int ii)
        {
            if (c < 0x20)
            {
                if (c == '\n')
                    return HandleError($"String literal contains newline", inp, ii, ParserState.BAD);
                if (c == 0)
                    return HandleError("'\\x00' is the null character, which is illegal in JsonTools", inp, ii, ParserState.FATAL);
                if (c < 0)
                    return true;
                return HandleError("Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    inp, ii, ParserState.OK);
            }
            return false;
        }

        public static Dictionary<char, char> ESCAPE_MAP = new Dictionary<char, char>
        {
            { '\\', '\\' },
            { 'n', '\n' },
            { 'r', '\r' },
            { 'b', '\b' },
            { 't', '\t' },
            { 'f', '\f' },
            { '/', '/' }, // the '/' char is often escaped in JSON
            { 'v', '\x0b' }, // vertical tab
            { '\'', '\'' },
            { '"', '"' },
        };

        #endregion
        #region PARSER_FUNCTIONS

        /// <summary>
        /// Parse a string literal in a JSON string.<br></br>
        /// Sets the parser's state to BAD if:<br></br>
        /// 1. The end of the input is reached before the closing quote char<br></br>
        /// 2. A '\n' is encountered before the closing quote char
        /// 3. Contains invalid hexadecimal<br></br>
        /// 4. Contains "\\" escaping a character other than 'n', 'b', 'r', '\', '/', '"', 'f', or 't'.
        /// </summary>
        /// <param name="inp">the json string</param>
        /// <returns>a JNode of type Dtype.STR, and the position of the end of the string literal</returns>
        /// </exception>
        public JNode ParseString(string inp)
        {
            int startUtf8Pos = ii + utf8ExtraBytes;
            char quoteChar = inp[ii++];
            if (quoteChar == '\'')
                HandleError("Singlequoted strings are only allowed in JSON5", inp, ii, ParserState.JSON5);
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (ii >= inp.Length)
                {
                    HandleError($"Unterminated string literal starting at position {startUtf8Pos}", inp, ii - 1, ParserState.BAD);
                    break;
                }
                char c = inp[ii];
                if (c == quoteChar)
                {
                    break;
                }
                else if (c == '\\')
                {
                    if (ii >= inp.Length - 2)
                    {
                        HandleError($"Unterminated string literal starting at position {startUtf8Pos}", inp, inp.Length - 1, ParserState.BAD);
                        ii++;
                        continue;
                    }
                    char nextChar = inp[ii + 1];
                    if (nextChar == quoteChar)
                    {
                        sb.Append(quoteChar);
                        ii += 1;
                    }
                    else if (ESCAPE_MAP.TryGetValue(nextChar, out char escapedChar))
                    {
                        sb.Append(escapedChar);
                        ii += 1;
                    }
                    else if (nextChar == 'u')
                    {
                        // 2-byte unicode of the form \uxxxx
                        ii += 2;
                        int nextHex = ParseHexChar(inp, 4);
                        if (HandleCharErrors(nextHex, inp, ii))
                            break;
                        sb.Append((char)nextHex);
                    }
                    else if (nextChar == '\n' || nextChar == '\r')
                    {
                        HandleError("Escaped newline characters are only allowed in JSON5", inp, ii + 1, ParserState.JSON5);
                        ii++;
                        if (nextChar == '\r'
                            && ii < inp.Length - 1 && inp[ii + 1] == '\n')
                            ii++;
                    }
                    else if (nextChar == 'x')
                    {
                        // 1-byte unicode (allowed only in JSON5)
                        ii += 2;
                        int nextHex = ParseHexChar(inp, 2);
                        if (HandleCharErrors(nextHex, inp, ii))
                            break;
                        HandleError("\\x escapes are only allowed in JSON5", inp, ii, ParserState.JSON5);
                        sb.Append((char)nextHex);
                    }
                    else HandleError($"Escaped char '{nextChar}' is only valid in JSON5", inp, ii + 1, ParserState.JSON5);
                }
                else
                {
                    if (HandleCharErrors(c, inp, ii))
                        break;
                    utf8ExtraBytes += ExtraUTF8Bytes(c);
                    sb.Append(c);
                }
                ii++;
            }
            ii++;
            if (parseDatetimes)
            {
                return TryParseDateOrDateTime(sb.ToString(), startUtf8Pos);
            }
            return new JNode(sb.ToString(), Dtype.STR, startUtf8Pos);
        }

        public string ParseKey(string inp)
        {
            char quoteChar = inp[ii];
            if (quoteChar == '\'')
                HandleError("Singlequoted strings are only allowed in JSON5", inp, ii, ParserState.JSON5);
            if (quoteChar != '\'' && quoteChar != '"')
            {
                return ParseUnquotedKey(inp);
            }
            ii++;
            var sb = new StringBuilder();
            while (true)
            {
                if (ii >= inp.Length)
                {
                    HandleError($"Unterminated object key", inp, ii - 1, ParserState.FATAL);
                    return null;
                }
                char c = inp[ii];
                if (c == quoteChar)
                {
                    break;
                }
                else if (c == '\\')
                {
                    if (ii >= inp.Length - 2)
                    {
                        HandleError($"Unterminated object key", inp, inp.Length - 1, ParserState.FATAL);
                        return null;
                    }
                    char nextChar = inp[ii + 1];
                    if (nextChar == quoteChar)
                    {
                        sb.Append(quoteChar);
                        ii++;
                    }
                    else if (ESCAPE_MAP.TryGetValue(nextChar, out char escapedChar))
                    {
                        sb.Append(escapedChar);
                        ii++;
                    }
                    else if (nextChar == 'u')
                    {
                        // 2-byte unicode of the form \uxxxx
                        // \x and \U escapes are not part of the JSON standard
                        ii += 2;
                        int nextHex = ParseHexChar(inp, 4);
                        if (HandleCharErrors(nextHex, inp, ii))
                            break;
                        sb.Append((char)nextHex);
                    }
                    else if (nextChar == '\n' || nextChar == '\r')
                    {
                        HandleError($"Escaped newline characters are only allowed in JSON5", inp, ii + 1, ParserState.JSON5);
                        ii++;
                        if (nextChar == '\r'
                            && ii < inp.Length - 1 && inp[ii + 1] == '\n')
                            ii++; // skip \r\n as one
                    }
                    else if (nextChar == 'x')
                    {
                        ii += 2;
                        int nextHex = ParseHexChar(inp, 2);
                        if (HandleCharErrors(nextHex, inp, ii))
                            break;
                        HandleError("\\x escapes are only allowed in JSON5", inp, ii, ParserState.JSON5);
                        sb.Append((char)nextHex);
                    }
                    else HandleError($"Escaped char '{nextChar}' is only valid in JSON5", inp, ii + 1, ParserState.JSON5);
                }
                else if (c < 0x20) // control characters
                {
                    if (c == '\n')
                        HandleError($"Object key contains newline", inp, ii, ParserState.BAD);
                    else
                        HandleError("Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification", inp, ii, ParserState.OK);
                    sb.Append(c);
                }
                else
                {
                    utf8ExtraBytes += ExtraUTF8Bytes(c);
                    sb.Append(c);
                }
                ii++;
            }
            ii++;
            return sb.ToString();
        }

        public const string UNQUOTED_START = @"(?:[_\$\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}]|\\u[\da-f]{4})";

        private static Regex UNICODE_ESCAPES = new Regex(@"(?<=\\u)[\da-f]{4}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static Regex UNQUOTED_KEY_REGEX = new Regex($@"{UNQUOTED_START}(?:[\p{{Mn}}\p{{Mc}}\p{{Nd}}\p{{Pc}}\u200c\u200d]|{UNQUOTED_START})*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string ParseUnquotedKey(string inp)
        {
            var match = UNQUOTED_KEY_REGEX.Match(inp, ii);
            if (!match.Success || match.Index != ii)
            {
                HandleError($"No valid unquoted key beginning at {ii}", inp, ii, ParserState.BAD);
                return null;
            }
            HandleError("Unquoted keys are only supported in JSON5", inp, ii, ParserState.JSON5);
            var result = match.Value;
            ii += result.Length;
            utf8ExtraBytes += ExtraUTF8BytesBetween(result, 0, result.Length);
            return ParseUnquotedKeyHelper(inp, result);
        }

        public string ParseUnquotedKeyHelper(string inp, string result)
        {
            if (result.Contains("\\u")) // fix unicode escapes
            {
                StringBuilder sb = new StringBuilder();
                Match m = UNICODE_ESCAPES.Match(result);
                int start = 0;
                while (m.Success)
                {
                    if (m.Index > start + 2)
                    {
                        sb.Append(result, start, m.Index - start - 2);
                    }
                    char hexval = (char)int.Parse(m.Value, NumberStyles.HexNumber);
                    if (HandleCharErrors(hexval, inp, ii))
                        return null;
                    sb.Append(hexval);
                    start = m.Index + 4;
                    m = m.NextMatch();
                }
                if (start < result.Length)
                    sb.Append(result, start, result.Length - start);
                result = sb.ToString();
            }
            return result;
        }

        private static Regex DATE_TIME_REGEX = new Regex(@"^\d{4}-\d\d-\d\d # date
                                                           (?:[T ](?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d # hours, minutes, seconds
                                                           (?:\.\d{1,3})?Z?)?$ # milliseconds",
                                                         RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        private JNode TryParseDateOrDateTime(string maybeDatetime, int startUtf8Pos)
        {
            Match mtch = DATE_TIME_REGEX.Match(maybeDatetime);
            int len = maybeDatetime.Length;
            if (mtch.Success)
            {
                try
                {
                    if (len == 10)
                    {
                        // yyyy-mm-dd dates have length 10
                        return new JNode(DateTime.Parse(maybeDatetime), Dtype.DATE, startUtf8Pos);
                    }
                    if (len >= 19 && len <= 23)
                    {
                        // yyyy-mm-dd hh:mm:ss has length 19, and yyyy-mm-dd hh:mm:ss.sss has length 23
                        return new JNode(DateTime.Parse(maybeDatetime), Dtype.DATETIME, startUtf8Pos);
                    }
                }
                catch { } // it was an invalid date, i guess
            }
            // it didn't match, so it's just a normal string
            return new JNode(maybeDatetime, Dtype.STR, startUtf8Pos);
        }

        /// <summary>
        /// Parse a number in a JSON string, including NaN or Infinity<br></br>
        /// Also parses null and the Python literals None (which we parse as null), nan, inf
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <returns>a JNode with type = Dtype.INT or Dtype.FLOAT, and the position of the end of the number.
        /// </returns>
        public JNode ParseNumber(string inp)
        {
            // parsed tracks which portions of a number have been parsed.
            // So if the int part has been parsed, it will be 1.
            // If the int and decimal point parts have been parsed, it will be 3.
            // If the int, decimal point, and scientific notation parts have been parsed, it will be 7
            int parsed = 1;
            int start = ii;
            int startUtf8Pos = start + utf8ExtraBytes;
            char c = inp[ii];
            bool negative = false;
            if (c < '0' || c > '9')
            {
                if (c == 'n')
                {
                    // try null
                    if (ii <= inp.Length - 4 && inp[ii + 1] == 'u' && inp[ii + 2] == 'l' && inp[ii + 3] == 'l')
                    {
                        ii += 4;
                        return new JNode(null, Dtype.NULL, startUtf8Pos);
                    }
                    if (ii <= inp.Length - 3 && inp[ii + 1] == 'a' && inp[ii + 2] == 'n')
                    {
                        HandleError("nan is not a valid representation of Not a Number in JSON", inp, ii, ParserState.BAD);
                        ii += 3;
                        return new JNode(NanInf.nan, Dtype.FLOAT, startUtf8Pos);
                    }
                    HandleError("Expected literal starting with 'n' to be null or nan", inp, ii + 1, ParserState.FATAL);
                    return new JNode(null, Dtype.NULL, startUtf8Pos);
                }
                if (c == '-' || c == '+')
                {
                    if (c == '+')
                        HandleError("Leading + signs in numbers are not allowed except in JSON5", inp, ii, ParserState.JSON5);
                    else negative = true;
                    ii++;
                    if (ii >= inp.Length)
                    {
                        HandleError($"'{c}' sign at end of document", inp, inp.Length - 1, ParserState.FATAL);
                        return new JNode(null, Dtype.NULL, startUtf8Pos);
                    }
                }
                c = inp[ii];
                if (c == 'I')
                {
                    // try Infinity
                    if (ii <= inp.Length - 8 && inp[ii + 1] == 'n' && inp.Substring(ii + 2, 6) == "finity")
                    {
                        HandleError("Infinity is not part of the original JSON specification", inp, ii, ParserState.NAN_INF);
                        ii += 8;
                        double infty = negative ? NanInf.neginf : NanInf.inf;
                        return new JNode(infty, Dtype.FLOAT, startUtf8Pos);
                    }
                    HandleError("Expected literal starting with 'I' to be Infinity",
                                                  inp, ii + 1, ParserState.FATAL);
                    return new JNode(null, Dtype.NULL, startUtf8Pos);
                }
                else if (c == 'N')
                {
                    // try NaN
                    if (ii <= inp.Length - 3 && inp[ii + 1] == 'a' && inp[ii + 2] == 'N')
                    {
                        HandleError("NaN is not part of the original JSON specification", inp, ii, ParserState.NAN_INF);
                        ii += 3;
                        return new JNode(NanInf.nan, Dtype.FLOAT, startUtf8Pos);
                    }
                    // try None
                    if (ii <= inp.Length - 4 && inp[ii + 1] == 'o' && inp[ii + 2] == 'n' && inp[ii + 3] == 'e')
                    {
                        ii += 4;
                        HandleError("None is not an accepted part of any JSON specification", inp, ii, ParserState.BAD);
                        return new JNode(null, Dtype.NULL, startUtf8Pos);
                    }
                    HandleError("Expected literal starting with 'N' to be NaN or None", inp, ii + 1, ParserState.FATAL);
                    return new JNode(null, Dtype.NULL, startUtf8Pos);
                }
                else if (c == 'i')
                {
                    if (ii <= inp.Length - 3 && inp[ii + 1] == 'n' && inp[ii + 2] == 'f')
                    {
                        HandleError("inf is not the correct representation of Infinity in JSON", inp, ii, ParserState.BAD);
                        ii += 3;
                        return new JNode(negative ? NanInf.neginf : NanInf.inf, Dtype.FLOAT, startUtf8Pos);
                    }
                    HandleError("Expected literal starting with 'i' to be inf", inp, ii, ParserState.FATAL);
                    return new JNode(null, Dtype.NULL, startUtf8Pos);
                }
            }
            if (c == '0' && ii < inp.Length  - 1)
            {
                char nextChar = inp[ii + 1];
                if (nextChar == 'x')
                {
                    HandleError("Hexadecimal numbers are only part of JSON5", inp, ii, ParserState.JSON5);
                    ii += 2;
                    start = ii;
                    while (ii < inp.Length)
                    {
                        c = inp[ii];
                        if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                            break;
                        ii++;
                    }
                    try
                    {
                        var hexnum = long.Parse(inp.Substring(start, ii - start), NumberStyles.HexNumber);
                        return new JNode(negative ? -hexnum : hexnum, Dtype.INT, startUtf8Pos);
                    }
                    catch
                    {
                        HandleError("Hex number too large for a 64-bit signed integer type", inp, start, ParserState.FATAL);
                        return new JNode(null, Dtype.NULL, startUtf8Pos);
                    }
                }
                else if (nextChar >= '0' && nextChar <= '9')
                    HandleError("Numbers with an unnecessary leading 0 (like \"01\") are not part of any JSON specification", inp, ii, ParserState.BAD);
            }
            while (ii < inp.Length)
            {
                c = inp[ii];
                if (c >= '0' && c <= '9')
                {
                    ii++;
                }
                else if (c == '.')
                {
                    if (parsed != 1)
                    {
                        HandleError("Number with a decimal point in the wrong place", inp, ii, ParserState.FATAL);
                        break;
                    }
                    if (ii == start)
                        HandleError("Numbers with a leading decimal point are only part of JSON5", inp, startUtf8Pos, ParserState.JSON5);
                    parsed = 3;
                    ii++;
                }
                else if (c == 'e' || c == 'E')
                {
                    if ((parsed & 4) != 0)
                    {
                        break;
                    }
                    if (ii >= 1 && inp[ii - 1] == '.')
                        HandleError("Numbers with a trailing decimal point are only part of JSON5", inp, startUtf8Pos, ParserState.JSON5);
                    parsed += 4;
                    ii++;
                    if (ii < inp.Length)
                    {
                        c = inp[ii];
                        if (c == '+' || c == '-')
                        {
                            ii++;
                        }
                    }
                    else
                    {
                        HandleError("Scientific notation 'e' with no number following", inp, inp.Length - 1, ParserState.FATAL);
                        return new JNode(null, Dtype.NULL, startUtf8Pos);
                    }
                }
                else if (c == '/' && ii < inp.Length - 1)
                {
                    char nextC = inp[ii + 1];
                    // make sure prospective denominator is also a number (we won't allow NaN or Infinity as denominator)
                    if (!((nextC >= '0' && nextC <= '9')
                        || nextC == '-' || nextC == '.' || nextC == '+'))
                    {
                        break;
                    }
                    HandleError("Fractions of the form 1/3 are not part of any JSON specification", inp, startUtf8Pos, ParserState.BAD);
                    double numer = double.Parse(inp.Substring(start, ii - start), JNode.DOT_DECIMAL_SEP);
                    JNode denomNode;
                    ii++;
                    denomNode = ParseNumber(inp);
                    if (fatal)
                    {
                        return new JNode(numer, Dtype.FLOAT, startUtf8Pos);
                    }
                    double denom = Convert.ToDouble(denomNode.value);
                    return new JNode(numer / denom, Dtype.FLOAT, startUtf8Pos);
                }
                else
                {
                    break;
                }
            }
            string numstr = inp.Substring(start, ii - start);
            if (parsed == 1)
            {
                try
                {
                    return new JNode(long.Parse(numstr), Dtype.INT, startUtf8Pos);
                }
                catch (Exception ex)
                {
                    if (!(ex is OverflowException))
                    {
                        HandleError($"Number string {JNode.StrToString(numstr, true)} had bad format", inp, startUtf8Pos, ParserState.BAD);
                        return new JNode(NanInf.nan, startUtf8Pos);
                    }
                    // overflow exceptions are OK,
                    // because doubles can represent much larger numbers than 64-bit ints,
                    // albeit with loss of precision
                }
            }
            double num;
            try
            {
                num = double.Parse(numstr, JNode.DOT_DECIMAL_SEP);
            }
            catch
            {
                HandleError($"Number string {JNode.StrToString(numstr, true)} had bad format", inp, startUtf8Pos, ParserState.BAD);
                num = NanInf.nan;
            }
            if (numstr[numstr.Length - 1] == '.')
                HandleError("Numbers with a trailing decimal point are only part of JSON5", inp, startUtf8Pos, ParserState.JSON5);
            return new JNode(num, Dtype.FLOAT, startUtf8Pos);
        }

        /// <summary>
        /// Parse an array in a JSON string.<br></br>
        /// Parsing may fail for any of the following reasons:<br></br>
        /// 1. The array is not terminated by ']'.<br></br>
        /// 2. The array is terminated with '}' instead of ']'.<br></br>
        /// 3. Two commas with nothing but whitespace in between.<br></br>
        /// 4. A comma before the first value.<br></br>
        /// 5. A comma after the last value.<br></br>
        /// 6. Two values with no comma in between.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <returns>a JArray, and the position of the end of the array.</returns>
        public JArray ParseArray(string inp, int recursionDepth)
        {
            var children = new List<JNode>();
            JArray arr = new JArray(ii + utf8ExtraBytes, children);
            bool alreadySeenComma = false;
            ii++;
            char curC;
            if (recursionDepth == MAX_RECURSION_DEPTH)
            {
                // Need to do this to avoid stack overflow when presented with unreasonably deep nesting.
                // Stack overflow causes an unrecoverable panic, and we would rather fail gracefully.
                HandleError($"Maximum recursion depth ({MAX_RECURSION_DEPTH}) reached", inp, ii, ParserState.FATAL);
                return arr;
            }
            while (ii < inp.Length)
            {
                if (!ConsumeInsignificantChars(inp))
                {
                    return arr;
                }
                if (ii >= inp.Length)
                {
                    break;
                }
                curC = inp[ii];
                if (curC == ',')
                {
                    if (alreadySeenComma)
                        HandleError($"Two consecutive commas after element {children.Count - 1} of array", inp, ii, ParserState.BAD);
                    alreadySeenComma = true;
                    if (children.Count == 0)
                        HandleError("Comma before first value in array", inp, ii, ParserState.BAD);
                    ii++;
                    continue;
                }
                else if (curC == ']')
                {
                    if (alreadySeenComma)
                    {
                        HandleError("Comma after last element of array", inp, ii, ParserState.JSON5);
                    }
                    ii++;
                    return arr;
                }
                else if (curC == '}')
                {
                    HandleError("Tried to terminate an array with '}'", inp, ii, ParserState.BAD);
                    if (alreadySeenComma)
                    {
                        HandleError("Comma after last element of array", inp, ii, ParserState.JSON5);
                    }
                    ii++;
                    return arr;
                }
                else
                {
                    if (children.Count > 0 && !alreadySeenComma)
                        HandleError("No comma between array members", inp, ii, ParserState.BAD);
                    // a new array member of some sort
                    alreadySeenComma = false;
                    JNode newObj;
                    int iiBeforeParse = ii;
                    int utf8ExtraBeforeParse = utf8ExtraBytes;
                    newObj = ParseSomething(inp, recursionDepth);
                    if (newObj.type == Dtype.STR && ii < inp.Length && inp[ii] == ':')
                    {
                        // maybe the user forgot the closing ']' of an array that's the child of an object.
                        HandleError("':' (key-value separator) where ',' between array members expected. Maybe you forgot to close the array?", inp, ii, ParserState.BAD);
                        ii = iiBeforeParse;
                        utf8ExtraBytes = utf8ExtraBeforeParse;
                        return arr;
                    }
                    //if (includeExtraProperties)
                    //{
                    //    newObj.extras = new ExtraJNodeProperties(arr, ii, children.Count);
                    //}
                    children.Add(newObj);
                    if (fatal)
                        return arr;
                }
            }
            ii++;
            HandleError("Unterminated array", inp, inp.Length - 1, ParserState.BAD);
            return arr;
        }

        /// <summary>
        /// Parse an object in a JSON string.<br></br>
        /// Parsing may fail for any of the following reasons:<br></br>
        /// 1. The object is not terminated by '}'.<br></br>
        /// 2. The object is terminated with ']' instead of '}'.<br></br>
        /// 3. Two commas with nothing but whitespace in between.<br></br>
        /// 4. A comma before the first key-value pair.<br></br>
        /// 5. A comma after the last key-value pair.<br></br>
        /// 6. Two key-value pairs with no comma in between.<br></br>
        /// 7. No ':' between a key and a value.<br></br>
        /// 8. A key that's not a string.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <returns>a JArray, and the position of the end of the array.</returns>
        public JObject ParseObject(string inp, int recursionDepth)
        {
            var children = new Dictionary<string, JNode>();
            JObject obj = new JObject(ii + utf8ExtraBytes, children);
            bool alreadySeenComma = false;
            ii++;
            char curC;
            if (recursionDepth == MAX_RECURSION_DEPTH)
            {
                HandleError($"Maximum recursion depth ({MAX_RECURSION_DEPTH}) reached", inp, ii, ParserState.FATAL);
                return obj;
            }
            while (ii < inp.Length)
            {
                if (!ConsumeInsignificantChars(inp))
                {
                    return obj;
                }
                if (ii >= inp.Length)
                {
                    break;
                }
                curC = inp[ii];
                if (curC == ',')
                {
                    if (alreadySeenComma)
                        HandleError($"Two consecutive commas after key-value pair {children.Count - 1} of object", inp, ii, ParserState.BAD);
                    alreadySeenComma = true;
                    if (children.Count == 0)
                        HandleError("Comma before first value in object", inp, ii, ParserState.BAD);
                    ii++;
                    continue;
                }
                else if (curC == '}')
                {
                    if (alreadySeenComma)
                        HandleError("Comma after last key-value pair of object", inp, ii, ParserState.JSON5);
                    ii++;
                    return obj;
                }
                else if (curC == ']')
                {
                    HandleError("Tried to terminate object with ']'", inp, ii, ParserState.BAD);
                    if (alreadySeenComma)
                        HandleError("Comma after last key-value pair of object", inp, ii, ParserState.JSON5);
                    ii++;
                    return obj;
                }
                else // expecting a key
                {
                    int childCount = children.Count;
                    if (childCount > 0 && !alreadySeenComma)
                    {
                        HandleError($"No comma after key-value pair {childCount - 1} in object", inp, ii, ParserState.BAD);
                        if (ii < inp.Length - 1 && curC == ':')
                        {
                            HandleError("':' found instead of comma after key-value pair", inp, ii, ParserState.BAD);
                            ii++;
                            ConsumeInsignificantChars(inp);
                            if (ii >= inp.Length)
                                break;
                        }
                    }
                    // a new key-value pair
                    int iiBeforeKey = ii;
                    int utf8ExtraBeforeKey = utf8ExtraBytes;
                    string key = ParseKey(inp);
                    if (fatal || key == null)
                    {
                        // key could be null if there's a valid JSON there that is not a valid key
                        // this covers the possibility that the user forgot to close the object before this (presumed) key, and in fact it's meant to be a value in a parent array
                        return obj;
                    }
                    if (ii >= inp.Length)
                    {
                        break;
                    }
                    if (inp[ii] == ':')
                        ii++;
                    else
                    {
                        if (!ConsumeInsignificantChars(inp))
                        {
                            return obj;
                        }
                        if (ii >= inp.Length)
                        {
                            break;
                        }
                        char c = inp[ii];
                        if (c == ':')
                        {
                            ii++;
                        }
                        else if (c == ',' || c == ']')
                        {
                            // comma or ']' after key instead of value could mean that this is supposed to be a value in a parent array,
                            // so we'll try bailing out here and reinterpreting the key as such
                            HandleError($"Found '{c}' after key {childCount} when colon expected", inp, ii, ParserState.BAD);
                            ii = iiBeforeKey;
                            utf8ExtraBytes = utf8ExtraBeforeKey;
                            return obj;
                        }
                        else HandleError($"No ':' between key {childCount} and value {childCount} of object", inp, ii, ParserState.BAD);
                    }
                    if (!ConsumeInsignificantChars(inp))
                    {
                        return obj;
                    }
                    if (ii >= inp.Length)
                    {
                        break;
                    }
                    JNode val = ParseSomething(inp, recursionDepth);
                    //if (includeExtraProperties)
                    //{
                    //    val.extras = new ExtraJNodeProperties(obj, ii, key);
                    //}
                    children[key] = val;
                    if (fatal)
                    {
                        return obj;
                    }
                    if (children.Count == childCount)
                    {
                        HandleError($"Object has multiple of key \"{key}\"", inp, ii, ParserState.BAD);
                    }
                    alreadySeenComma = false;
                }
            }
            ii++;
            HandleError("Unterminated object", inp, inp.Length - 1, ParserState.BAD);
            return obj;
        }

        /// <summary>
        /// Parse anything (a scalar, null, an object, or an array) in a JSON string.<br></br>
        /// Parsing may fail (causing this to return a null JNode) for any of the following reasons:<br></br>
        /// 1. Whatever reasons ParseObject, ParseArray, or ParseString might throw an error.<br></br>
        /// 2. An unquoted string other than true, false, null, NaN, Infinity, -Infinity.<br></br>
        /// 3. The JSON string contains only blankspace or is empty.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <returns>a JNode.</returns>
        public JNode ParseSomething(string inp, int recursionDepth)
        {
            int startUtf8Pos = ii + utf8ExtraBytes;
            if (ii >= inp.Length)
            {
                HandleError("Unexpected end of file", inp, inp.Length - 1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, startUtf8Pos);
            }
            char curC = inp[ii];
            if (curC == '"' || curC == '\'')
            {
                return ParseString(inp);
            }
            if (curC >= '0' && curC <= '9'
                || curC == '-' || curC == '+'
                || curC == 'n' // null and nan
                || curC == 'I' || curC == 'N' // Infinity, NaN and None
                || curC == '.' // leading decimal point JSON5 numbers
                || curC == 'i') // inf
            {
                return ParseNumber(inp);
            }
            if (curC == '[')
            {
                return ParseArray(inp, recursionDepth + 1);
            }
            if (curC == '{')
            {
                return ParseObject(inp, recursionDepth + 1);
            }
            char nextC;
            if (ii > inp.Length - 4)
            {
                HandleError("No valid literal possible", inp, ii, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, startUtf8Pos);
            }
            // misc literals. In strict JSON, only true or false
            nextC = inp[ii + 1];
            if (curC == 't')
            {
                // try true
                if (nextC == 'r' && inp[ii + 2] == 'u' && inp[ii + 3] == 'e')
                {
                    ii += 4;
                    return new JNode(true, Dtype.BOOL, startUtf8Pos);
                }
                HandleError("Expected literal starting with 't' to be true", inp, ii+1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, startUtf8Pos);
            }
            if (curC == 'f')
            {
                // try false
                if (ii <= inp.Length - 5 && nextC == 'a' && inp.Substring(ii + 2, 3) == "lse")
                {
                    ii += 5;
                    return new JNode(false, Dtype.BOOL, startUtf8Pos);
                }
                HandleError("Expected literal starting with 'f' to be false", inp, ii+1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, startUtf8Pos);
            }
            if (curC == 'T')
            {
                // try True from Python
                if (nextC == 'r' && inp[ii + 2] == 'u' && inp[ii + 3] == 'e')
                {
                    ii += 4;
                    HandleError("True is not an accepted part of any JSON specification", inp, ii, ParserState.BAD);
                    return new JNode(true, Dtype.BOOL, startUtf8Pos);
                }
                HandleError("Expected literal starting with 'T' to be True", inp, ii + 1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, startUtf8Pos);
            }
            if (curC == 'F')
            {
                // try False from Python
                if (ii <= inp.Length - 5 && nextC == 'a' && inp.Substring(ii + 2, 3) == "lse")
                {
                    ii += 5;
                    HandleError("False is not an accepted part of any JSON specification", inp, ii, ParserState.BAD);
                    return new JNode(false, Dtype.BOOL, startUtf8Pos);
                }
                HandleError("Expected literal starting with 'F' to be False", inp, ii + 1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, startUtf8Pos);
            }
            if (curC == 'u')
            {
                // try undefined, because apparently some people want that?
                // https://github.com/kapilratnani/JSON-Viewer/pull/146
                // it will be parsed as null
                if (ii <= inp.Length - 9 && nextC == 'n' && inp.Substring(ii + 2, 7) == "defined")
                {
                    ii += 9;
                    HandleError("undefined is not part of any JSON specification", inp, startUtf8Pos - utf8ExtraBytes, ParserState.BAD);
                }
                else HandleError("Expected literal starting with 'u' to be undefined",
                                              inp, ii + 1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, startUtf8Pos);
            }
            HandleError("Badly located character " + (ii >= inp.Length ? "\"\\x00\"" : JNode.StrToString(inp.Substring(ii, 1), true)), inp, ii, ParserState.FATAL);
            return new JNode(null, Dtype.NULL, startUtf8Pos);
        }

        /// <summary>
        /// Parse a JSON string and return a JNode representing the document.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <returns></returns>
        public JNode Parse(string inp)
        {
            Reset();
            if (inp.Length == 0)
            {
                HandleError("No input", inp, 0, ParserState.FATAL);
                return new JNode();
            }
            if (!ConsumeInsignificantChars(inp))
            {
                return new JNode();
            }
            if (ii >= inp.Length)
            {
                HandleError("Json string is only whitespace and maybe comments", inp, inp.Length - 1, ParserState.FATAL);
                return new JNode();
            }
            JNode json = ParseSomething(inp, 0);
            //if (includeExtraProperties)
            //{
            //    json.extras = new ExtraJNodeProperties(null, ii, null);
            //}
            if (fatal)
            {
                return json;
            }
            if (!ConsumeInsignificantChars(inp))
            {
                return json;
            }
            if (ii < inp.Length)
            {
                HandleError($"At end of valid JSON document, got {inp[ii]} instead of EOF", inp, ii, ParserState.BAD);
            }
            return json;
        }

        /// <summary>
        /// Parse a JSON Lines document (a text file containing one or more \n-delimited lines
        /// where each line contains its own valid JSON document)
        /// as an array where the i^th element is the document on the i^th line.<br></br>
        /// See https://jsonlines.org/
        /// </summary>
        /// <param name="inp"></param>
        /// <returns></returns>
        public JNode ParseJsonLines(string inp)
        {
            Reset();
            if (inp.Length == 0)
            {
                HandleError("No input", inp, 0, ParserState.FATAL);
                return new JNode();
            }
            if (!ConsumeInsignificantChars(inp))
            {
                return new JNode();
            }
            if (ii >= inp.Length)
            {
                HandleError("Json string is only whitespace and maybe comments", inp, inp.Length - 1, ParserState.FATAL);
                return new JNode();
            }
            int lastII = 0;
            JNode json;
            List<JNode> children = new List<JNode>();
            JArray arr = new JArray(0, children);
            int lineNum = 0;
            while (ii < inp.Length)
            {
                json = ParseSomething(inp, 0);
                ConsumeInsignificantChars(inp);
                children.Add(json);
                if (fatal)
                {
                    return arr;
                }
                int maxLastII = ii > inp.Length ? inp.Length : ii; 
                for (; lastII < maxLastII; lastII++)
                {
                    if (inp[lastII] == '\n')
                        lineNum++;
                }
                // make sure this document was all in one line
                if (!(lineNum == arr.Length
                    || (ii >= inp.Length && lineNum == arr.Length - 1)))
                {
                    if (ii >= inp.Length)
                        ii = inp.Length - 1;
                    HandleError(
                        "JSON Lines document does not contain exactly one JSON document per line",
                        inp, ii, ParserState.FATAL
                    );
                    return arr;
                }
                if (!ConsumeInsignificantChars(inp))
                {
                    return arr;
                }
            }
            return arr;
        }

        /// <summary>
        /// reset the lint, position, and utf8_extraBytes of this parser
        /// </summary>
        public void Reset()
        {
            lint.Clear();
            comments?.Clear();
            state = ParserState.STRICT;
            utf8ExtraBytes = 0;
            ii = 0;
        }

        /// <summary>
        /// create a new JsonParser with all the same settings as this one
        /// </summary>
        /// <returns></returns>
        public JsonParser Copy()
        {
            return new JsonParser(loggerLevel, parseDatetimes, throwIfLogged, throwIfFatal);//, includeExtraProperties);
        }
        #endregion
        #region MISC_OTHER_FUNCTIONS
        /// <summary>
        /// returns inp if start == 0 and end == inp.Length (to avoid wasteful copying), otherwise returns inp.Substring(start, end - start)
        /// </summary>
        /// <param name="inp"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static string SubstringUnlessAll(string inp, int start, int end)
        {
            return (start == 0 && end == inp.Length) ? inp : inp.Substring(start, end - start);
        }

        /// <summary>
        /// Try to parse inp[start:end] (start inclusive, end exclusive) as a number within the JSON5 specification, then return that number as a JNode<br></br>
        /// If inp[start:end] can't be parsed as a JSON5 number, return inp[start:end] as a JNode.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <param name="start">the position to start parsing from</param>
        /// <param name="end">the position to end parsing at</param>
        /// <param name="jnodePosition">the value to assign to the position attribute of the JNode returned</param>
        /// <returns>a JNode with type = Dtype.INT or Dtype.FLOAT, and the position of the end of the number.
        /// </returns>
        public static JNode TryParseNumber(string inp, int start, int end, int jnodePosition)
        {
            end = inp.Length < end ? inp.Length : end;
            if (start >= end)
                return new JNode("");
            // parsed tracks which portions of a number have been parsed.
            // So if the int part has been parsed, it will be 1.
            // If the int and decimal point parts have been parsed, it will be 3.
            // If the int, decimal point, and scientific notation parts have been parsed, it will be 7
            int parsed = 1;
            int ogStart = start;
            char c = inp[start];
            bool negative = false;
            if (c < '0' || c > '9')
            {
                if (c == '-' || c == '+')
                {
                    negative = c == '-';
                    start++;
                    if (start >= end)
                        return new JNode(SubstringUnlessAll(inp, start, end), jnodePosition);
                    c = inp[start];
                }
                if (start == end - 8 && c == 'I' && inp[start + 1] == 'n' && inp.Substring(start + 2, 6) == "finity")
                {
                    // try Infinity
                    return new JNode(negative ? NanInf.neginf : NanInf.inf, jnodePosition);
                }
                else if (start == end - 3 && c == 'N' && inp[start + 1] == 'a' && inp[start + 2] == 'N')
                {
                    // try NaN
                    return new JNode(NanInf.nan, jnodePosition);
                }
            }
            int ii = start;
            if (c == '0' && ii < end - 1 && inp[ii + 1] == 'x')
            {
                ii += 2;
                start = ii;
                while (ii < end)
                {
                    c = inp[ii];
                    if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                        return new JNode(SubstringUnlessAll(inp, ogStart, end), jnodePosition);
                    ii++;
                }
                try
                {
                    var hexnum = long.Parse(inp.Substring(start, end - start), NumberStyles.HexNumber);
                    return new JNode(negative ? -hexnum : hexnum, jnodePosition);
                }
                catch
                {
                    // probably overflow error
                    return new JNode(SubstringUnlessAll(inp, ogStart, end), jnodePosition);
                }
            }
            string numstr = SubstringUnlessAll(inp, ogStart, end);
            while (ii < end)
            {
                c = inp[ii];
                if (c >= '0' && c <= '9')
                {
                    ii++;
                }
                else if (c == '.')
                {
                    if (parsed != 1)
                    {
                        // two decimal places in the number
                        goto notANumber;
                    }
                    parsed = 3;
                    ii++;
                }
                else if (c == 'e' || c == 'E')
                {
                    if ((parsed & 4) != 0)
                    {
                        break;
                    }
                    parsed += 4;
                    ii++;
                    if (ii < end)
                    {
                        c = inp[ii];
                        if (c == '+' || c == '-')
                        {
                            ii++;
                        }
                    }
                    else
                    {
                        // Scientific notation 'e' with no number following
                        goto notANumber;
                    }
                }
                else
                    goto notANumber;
            }
            if (parsed == 1)
            {
                try
                {
                    long l = long.Parse(numstr);
                    return new JNode(l, jnodePosition);
                }
                catch (OverflowException)
                {
                    // doubles can represent much larger numbers than 64-bit ints,
                    // albeit with loss of precision
                }
            }
            try
            {
                double d = double.Parse(numstr, JNode.DOT_DECIMAL_SEP);
                return new JNode(d, jnodePosition);
            }
            catch
            {
                return new JNode(numstr);
            }
            notANumber:
            return new JNode(numstr, jnodePosition);
        }
        #endregion
    }
}