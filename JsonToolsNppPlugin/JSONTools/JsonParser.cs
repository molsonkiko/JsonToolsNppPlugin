/*
A parser and linter for JSON.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JSON_Tools.Utils;
using static System.Windows.Forms.LinkLabel;

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

        public JsonParserException(string Message)
        {
            this.Message = Message;
            this.CurChar = '\x00';
            this.Position = 0;
        }

        public override string ToString()
        {
            return $"{Message} at position {Position} (char '{CurChar}')";
        }
    }

    /// <summary>
    /// A syntax error caught and logged by the linter.
    /// </summary>
    public struct JsonLint
    {
        public string message;
        public int pos;
        public char curChar;
        public ParserState severity;

        public JsonLint(string message, int pos, char curChar, ParserState severity)
        {
            this.message = message;
            this.pos = pos;
            this.curChar = curChar;
            this.severity = severity;
        }

        public override string ToString()
        {

            string charDisplay;
            switch (curChar)
            {
                case '\x00': charDisplay = "''";    break; 
                case '\t':   charDisplay = "'\\t'"; break;
                case '\r':   charDisplay = "'\\r'"; break;
                case '\n':   charDisplay = "'\\n'"; break;
                case '\'':   charDisplay = "'\\''"; break;
                default:     charDisplay = $"'{curChar}'"; break;
            }
            return $"Syntax error (severity = {severity}) at position {pos} (char {charDisplay}): {message}";
        }
    }

    /// <summary>
    /// Any errors above this level are reported by a JsonParser.<br></br>
    /// The integer value of a state reflects how seriously the input deviates from the original JSON spec.
    /// </summary>
    public enum LoggerLevel
    {
        /// <summary>
        /// Valid according to the original JSON specification
        /// </summary>
        OK,
        /// <summary>
        /// Standard JSON plus NaN, Infinity, and -Infinity.
        /// </summary>
        NAN_INF,
        /// <summary>
        /// JSON with JavaScript comments.<br></br>
        /// NaN and +/-Infinity are also allowed.<br></br>
        /// Note that this differs slightly from the standard JSONC spec
        /// because NaN and +/-Infinity are not part of that spec.
        /// </summary>
        JSONC,
        /// <summary>
        /// JSON that follows the specification described here: https://json5.org/<br></br>
        /// The most notable deviations from main spec include:<br></br>
        /// * unquoted object keys<br></br>
        /// * comma after last element of iterable<br></br>
        /// * singlequoted strings
        /// </summary>
        JSON5,
        ///// <summary>
        ///// JSON with syntax errors that my parser can handle, such as:<br></br>
        ///// * unterminated strings<br></br>
        ///// * missing commas after array elements<br></br>
        ///// * Python-style single-line comments (start with '#')
        ///// </summary>
        //BAD
    }

    /// <summary>
    /// the sequence of states the JSON parser can be in.<br></br>
    /// The first four states (OK, NAN_INF, JSONC, JSON5) have the same
    /// meaning as in the LoggerLevel enum.
    /// The last two states (BAD and FATAL) reflect errors that are
    /// <i>always logged</i> and thus do not belong in the LoggerLevel enum.
    /// </summary>
    public enum ParserState
    {
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
        /// * recursion depth hits the recursion limit
        /// * empty input
        /// </summary>
        FATAL
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
        public bool parse_datetimes;

        /// <summary>
        /// If line is not null, most forms of invalid syntax will not cause the parser to stop,<br></br>
        /// but instead the syntax error will be recorded in a list.
        /// </summary>
        public List<JsonLint> lint;

        /// <summary>
        /// position in JSON string
        /// </summary>
        private int ii;
        
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
        private int utf8_extra_bytes;

        private ParserState state;
        
        /// <summary>
        /// errors above this 
        /// </summary>
        public LoggerLevel logger_level;

        /// <summary>
        /// Any error above the logger level causes an error to be thrown.<br></br>
        /// If false, parse functions will return everything logged up until a fatal error
        /// and will parse everything if there were no fatal errors.<br></br>
        /// Present primarily for backwards compatibility.
        /// </summary>
        private bool throw_if_logged;

        private bool throw_if_fatal;
        
        /// <summary>
        /// the number of bytes in the utf-8 representation
        /// before the current position in the current document
        /// </summary>
        public int utf8_pos { get { return ii + utf8_extra_bytes; } }

        public bool fatal
        {
            get { return state == ParserState.FATAL; }
        }

        /// <summary>
        /// if parsing failed, this will be the final error logged. If parsing succeeded, this is null.
        /// </summary>
        public JsonLint? fatal_error
        {
            get
            {
                if (fatal)
                    return lint[lint.Count - 1];
                return null;
            }
        }

        public JsonParser(LoggerLevel logger_level = LoggerLevel.NAN_INF, bool parse_datetimes = false, bool throw_if_logged = true, bool throw_if_fatal = true)
        {
            this.logger_level = logger_level;
            this.parse_datetimes = parse_datetimes;
            this.throw_if_logged = throw_if_logged;
            this.throw_if_fatal = throw_if_fatal;
            ii = 0;
            lint = new List<JsonLint>();
            state = ParserState.OK;
            utf8_extra_bytes = 0;
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
        /// If the severity is above the parser's logger_level:<br></br>
        ///     * if throw_if_logged or (FATAL and throw_if_fatal), throw a JsonParserException<br></br>
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
            if ((int)state > (int)logger_level)
            {
                char c = (pos >= inp.Length)
                    ? '\x00'
                    : inp[pos];
                lint.Add(new JsonLint(message, utf8_pos, c, severity));
                if (throw_if_logged || (fatal && throw_if_fatal))
                {
                    throw new JsonParserException(message, c, utf8_pos);
                }
            }
            return fatal;
        }

        private void ConsumeWhiteSpace(string inp)
        {
            while (ii < inp.Length)
            {
                char c = inp[ii];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    ii++;
                else { return; }
            }
        }

        private void ConsumeLine(string inp)
        {
            while (ii < inp.Length && inp[ii] != '\n')
            {
                ii++;
            }
            ii++;
        }

        /// <summary>
        /// return false if the comment could not be consumed.<br></br>
        /// return true if it was consumed and the parser is still in
        /// an acceptable state.
        /// </summary>
        /// <param name="inp"></param>
        /// <returns></returns>
        private bool MaybeConsumeComment(string inp)
        {
            if (HandleError("Comments are not part of the original JSON specification", inp, ii, ParserState.JSONC))
                return false;
            char cur_c;
            char next_c;
            while (ii < inp.Length - 1 && (inp[ii] == '/' || inp[ii] == '#'))
            {
                next_c = inp[ii + 1];
                if (next_c == '/')
                {
                    // JavaScript-style single-line comment
                    ii++;
                    ConsumeLine(inp);
                }
                else if (next_c == '#')
                {
                    // Python-style single-line comment
                    if (HandleError("Python-style '#' comments are not part of any well-accepted JSON specification.",
                        inp, ii, ParserState.BAD))
                    {
                        return false;
                    }
                    ii++;
                    ConsumeLine(inp);
                }
                else if (next_c == '*')
                {
                    // a multi-line comment (/* ... */)
                    ii++;
                    bool comment_ended = false;
                    while (ii < inp.Length - 1)
                    {
                        cur_c = inp[ii++];
                        if (cur_c == '*')
                        {
                            if (inp[ii] == '/')
                            {
                                comment_ended = true;
                                ii++;
                                break;
                            }
                        }
                    }
                    if (!comment_ended)
                    {
                        HandleError("Unterminated multi-line comment", inp, inp.Length - 1, ParserState.FATAL);
                        return false;
                    }
                }
                ConsumeWhiteSpace(inp);
            }
            return true;
        }

        /// <summary>
        /// read a hexadecimal integer representation of length `length` at position `index` in `inp`.
        /// sets the parser's state to FATAL if the integer is not valid hexadecimal
        /// or if `index` is less than `length` from the end of `inp`.
        /// </summary>
        private int ParseHexadecimal(string inp, int length)
        {
            int end = ii + length > inp.Length ? inp.Length : ii + length;
            var hexNum = inp.Substring(ii, end - ii);
            ii = end - 1;
            // the -1 is because ParseString increments by 1 after every escaped sequence anyway
            int charval;
            try
            {
                charval = int.Parse(hexNum, System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                HandleError("Could not find valid hexadecimal of length " + length,
                                              inp, ii, ParserState.BAD);
                return -1;
            }
            if (0xd800 <= charval && charval <= 0xdbff
                && inp[ii] == '\\' && inp[end] == 'u')
            {
                // see https://github.com/python/cpython/blob/main/Lib/json/encoder.py
                // Characters bigger than 0xffff are encoded as surrogate pairs
                // of 2-byte characters, and this is a way to tell that you're going
                // to see a surrogate pair
                ii = end + 1;
                int charval2 = ParseHexadecimal(inp, 4);
                if (charval2 == -1)
                    return charval; 
                return 0x10000 + (((charval - 0xd800) << 10) | (charval2 - 0xdc00));
            }
            return charval;
        }

        public static Dictionary<char, char> ESCAPE_MAP = new Dictionary<char, char>
        {
            { '\\', '\\' },
            { 'n', '\n' },
            { 'r', '\r' },
            { 'b', '\b' },
            { 't', '\t' },
            // { '"', '"' }, // handled separately inside of ToString
            { 'f', '\f' },
            { '/', '/' }, // the '/' char is often escaped in JSON
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
            int start_utf8_pos = ii + utf8_extra_bytes;
            char quote_char = inp[ii++];
            if (quote_char == '\'' && HandleError("Singlequoted strings are only allowed in JSON5", inp, ii, ParserState.JSON5))
            {
                return new JNode("", Dtype.STR, utf8_pos);
            }
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (ii >= inp.Length)
                {
                    HandleError($"Unterminated string literal starting at position {start_utf8_pos}", inp, ii - 1, ParserState.BAD);
                    break;
                }
                char c = inp[ii];
                if (c == '\n')
                {
                    // internal newlines are not allowed in JSON strings
                    if (HandleError($"String literal starting at position {start_utf8_pos} contains newline", inp, ii, ParserState.BAD))
                    {
                        break;
                    }
                }
                if (c == quote_char)
                {
                    break;
                }
                else if (c == '\\')
                {
                    if (ii >= inp.Length - 2)
                    {
                        HandleError($"Unterminated string literal starting at position {start_utf8_pos}", inp, inp.Length - 1, ParserState.BAD);
                        break;
                    }
                    char next_char = inp[ii + 1];
                    if (next_char == quote_char)
                    {
                        sb.Append(next_char);
                        ii += 1;
                    }
                    else if (ESCAPE_MAP.TryGetValue(next_char, out char escaped_char))
                    {
                        sb.Append(escaped_char);
                        ii += 1;
                    }
                    else if (next_char == 'u')
                    {
                        // 2-byte unicode of the form \uxxxx
                        // \x and \U escapes are not part of the JSON standard
                        ii += 2;
                        int next_hex = ParseHexadecimal(inp, 4);
                        if (next_hex < 0)
                        {
                            // if next_hex is -1, that means it was invalid
                            break;
                        }
                        sb.Append((char)next_hex);
                    }
                    //else if (next_char == '\n'
                    //     && HandleError($"Escaped newline characters are only allowed in JSON5", inp, ii + 1, (int)ParserState.JSON5))
                    //{
                    //    break;
                    //}
                    else if (HandleError("Invalidly escaped char", inp, ii + 1, ParserState.BAD))
                        break;
                }
                else
                {
                    utf8_extra_bytes += ExtraUTF8Bytes(c);
                    sb.Append(c);
                }
                ii++;
            }
            ii++;
            if (parse_datetimes)
            {
                return TryParseDateOrDateTime(sb.ToString(), start_utf8_pos);
            }
            return new JNode(sb.ToString(), Dtype.STR, start_utf8_pos);
        }

        private static Regex DATE_TIME_REGEX = new Regex(@"^\d{4}-\d\d-\d\d # date
                                                           (?:[T ](?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d # hours, minutes, seconds
                                                           (?:\.\d{1,3})?Z?)?$ # milliseconds",
                                                         RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        private JNode TryParseDateOrDateTime(string maybe_datetime, int start_utf8_pos)
        {
            Match mtch = DATE_TIME_REGEX.Match(maybe_datetime);
            int len = maybe_datetime.Length;
            if (mtch.Success)
            {
                try
                {
                    if (len == 10)
                    {
                        // yyyy-mm-dd dates have length 10
                        return new JNode(DateTime.Parse(maybe_datetime), Dtype.DATE, start_utf8_pos);
                    }
                    if (len >= 19 && len <= 23)
                    {
                        // yyyy-mm-dd hh:mm:ss has length 19, and yyyy-mm-dd hh:mm:ss.sss has length 23
                        return new JNode(DateTime.Parse(maybe_datetime), Dtype.DATETIME, start_utf8_pos);
                    }
                }
                catch { } // it was an invalid date, i guess
            }
            // it didn't match, so it's just a normal string
            return new JNode(maybe_datetime, Dtype.STR, start_utf8_pos);
        }

        /// <summary>
        /// Parse a number in a JSON string.
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
            int start_utf8_pos = start + utf8_extra_bytes;
            char c = inp[ii];
            if (c == '-' || c == '+')
            {
                if (c == '+' && HandleError("Leading + signs in numbers are not allowed except in JSON5.", inp, ii, ParserState.JSON5))
                {
                    return new JNode(null, Dtype.NULL, start_utf8_pos);
                }
                ii++;
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
                    if (ii < inp.Length - 1)
                    {
                        c = inp[++ii];
                        if (c == '+' || c == '-')
                        {
                            ii++;
                        }
                    }
                }
                else if (c == '/')
                {
                    // fractions are part of the JSON language specification
                    double numer = double.Parse(inp.Substring(start, ii - start), JNode.DOT_DECIMAL_SEP);
                    JNode denom_node;
                    ii++;
                    denom_node = ParseNumber(inp);
                    double denom = Convert.ToDouble(denom_node.value);
                    return new JNode(numer / denom, Dtype.FLOAT, start_utf8_pos);
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
                    return new JNode(long.Parse(numstr), Dtype.INT, start_utf8_pos);
                }
                catch (OverflowException)
                {
                    // doubles can represent much larger numbers than 64-bit ints,
                    // albeit with loss of precision
                    return new JNode(double.Parse(numstr), Dtype.FLOAT, start_utf8_pos);
                }
            }
            return new JNode(double.Parse(numstr, JNode.DOT_DECIMAL_SEP), Dtype.FLOAT, start_utf8_pos);
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
        public JArray ParseArray(string inp, int recursion_depth)
        {
            var children = new List<JNode>();
            JArray arr = new JArray(ii + utf8_extra_bytes, children);
            bool already_seen_comma = false;
            if (ii >= inp.Length - 1)
            {
                HandleError("Unterminated array", inp, inp.Length - 1, ParserState.BAD);
                return arr;
            }
            ii++;
            char cur_c;
            if (recursion_depth == MAX_RECURSION_DEPTH)
            {
                // Need to do this to avoid stack overflow when presented with unreasonably deep nesting.
                // Stack overflow causes an unrecoverable panic, and we would rather fail gracefully.
                HandleError($"Maximum recursion depth ({MAX_RECURSION_DEPTH}) reached", inp, ii, ParserState.FATAL);
                return arr;
            }
            while (ii < inp.Length)
            {
                ConsumeWhiteSpace(inp);
                cur_c = inp[ii];
                if (cur_c == ',')
                {
                    if (already_seen_comma
                        && HandleError($"Two consecutive commas after element {children.Count - 1} of array", inp, ii, ParserState.BAD))
                    {
                        return arr;
                    }
                    already_seen_comma = true;
                    if (children.Count == 0
                        && HandleError("Comma before first value in array", inp, ii, ParserState.BAD))
                    {
                        return arr;
                    }
                    ii++;
                    continue;
                }
                else if (cur_c == ']')
                {
                    if (already_seen_comma)
                    {
                        HandleError("Comma after last element of array", inp, ii, ParserState.JSON5);
                    }
                    ii++;
                    return arr;
                }
                else if (cur_c == '}')
                {
                    HandleError("Tried to terminate an array with '}'", inp, ii, ParserState.BAD);
                    if (already_seen_comma)
                    {
                        HandleError("Comma after last element of array", inp, ii, ParserState.JSON5);
                    }
                    ii++;
                    return arr;
                }
                else if (cur_c == '/' || cur_c == '#'
                    && !MaybeConsumeComment(inp))
                {
                    return arr;
                }
                else
                {
                    if (children.Count > 0 && !already_seen_comma
                        && HandleError("No comma between array members", inp, ii, ParserState.BAD))
                    {
                        return arr;
                    }
                    // a new array member of some sort
                    already_seen_comma = false;
                    JNode new_obj;
                    new_obj = ParseSomething(inp, recursion_depth);
                    children.Add(new_obj);
                    if (fatal)
                        return arr;
                }
            }
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
        public JObject ParseObject(string inp, int recursion_depth)
        {
            var children = new Dictionary<string, JNode>();
            JObject obj = new JObject(ii + utf8_extra_bytes, children);
            bool already_seen_comma = false;
            if (ii >= inp.Length - 1)
            {
                HandleError("Unterminated object", inp, inp.Length - 1, ParserState.BAD);
                return obj;
            }
            char cur_c = inp[++ii];
            if (recursion_depth == MAX_RECURSION_DEPTH)
            {
                HandleError($"Maximum recursion depth ({MAX_RECURSION_DEPTH}) reached", inp, ii, ParserState.FATAL);
                return obj;
            }
            while (ii < inp.Length)
            {
                ConsumeWhiteSpace(inp);
                cur_c = inp[ii];
                if (cur_c == ',')
                {
                    if (already_seen_comma
                        && HandleError($"Two consecutive commas after key-value pair {children.Count - 1} of object", inp, ii, ParserState.BAD))
                    {
                        return obj;
                    }
                    already_seen_comma = true;
                    if (children.Count == 0
                        && HandleError("Comma before first value in object", inp, ii, ParserState.BAD))
                    {
                        return obj;
                    }
                    ii++;
                    continue;
                }
                else if (cur_c == '}')
                {
                    if (already_seen_comma)
                        HandleError("Comma after last key-value pair of object", inp, ii, ParserState.JSON5);
                    ii++;
                    return obj;
                }
                else if (cur_c == ']')
                {
                    HandleError("Tried to terminate object with ']'", inp, ii, ParserState.BAD);
                    if (already_seen_comma)
                        HandleError("Comma after last key-value pair of object", inp, ii, ParserState.JSON5);
                    ii++;
                    return obj;
                }
                else if (cur_c == '"' || state >= ParserState.JSON5)
                {
                    if (children.Count > 0 && !already_seen_comma
                        && HandleError($"No comma after key-value pair {children.Count - 1} in object", inp, ii, ParserState.BAD))
                    {
                        return obj;
                    }
                    // a new key-value pair
                    JNode keystring = ParseString(inp);
                    //child_key = (string)keystring.value;
                    string child_keystr = keystring.ToString();
                    string key = child_keystr.Substring(1, child_keystr.Length - 2);
                    if (inp[ii] != ':')
                    {
                        // avoid call overhead in most likely case where colon comes
                        // immediately after key
                        ConsumeWhiteSpace(inp);
                        cur_c = inp[ii];
                        if (cur_c == '/' || cur_c == '#'
                            && !MaybeConsumeComment(inp))
                        {
                            return obj;
                        }
                    }
                    if (inp[ii] != ':')
                    {
                        if (HandleError($"No ':' between key {children.Count} and value {children.Count} of object", inp, ii, ParserState.BAD))
                        {
                            return obj;
                        }
                        else
                        {
                            ii--;
                        }
                    }
                    ii++;
                    ConsumeWhiteSpace(inp);
                    cur_c = inp[ii];
                    if (cur_c == '/' || cur_c == '#'
                        && !MaybeConsumeComment(inp))
                    {
                        return obj;
                    }
                    JNode val = ParseSomething(inp, recursion_depth);
                    children.Add(key, val);
                    if (fatal)
                    {
                        return obj;
                    }
                    already_seen_comma = false;
                }
                else if (cur_c == '/' || cur_c == '#'
                    && MaybeConsumeComment(inp))
                {
                    return obj;
                }
                else // some inappropriate character
                {
                    HandleError($"Key in object (would be key {children.Count}) must be string", inp, ii, ParserState.FATAL);
                    return obj;
                }
            }
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
        public JNode ParseSomething(string inp, int recursion_depth)
        {
            int start_utf8_pos = ii + utf8_extra_bytes;
            if (ii >= inp.Length)
            {
                HandleError("Unexpected end of file", inp, inp.Length - 1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, start_utf8_pos);
            }
            char cur_c = inp[ii];
            char next_c;
            if (cur_c == '"' || cur_c == '\'')
            {
                return ParseString(inp);
            }
            if (cur_c >= '0' && cur_c <= '9')
            {
                return ParseNumber(inp);
            }
            if (cur_c == '[')
            {
                return ParseArray(inp, recursion_depth + 1);
            }
            if (cur_c == '{')
            {
                return ParseObject(inp, recursion_depth + 1);
            }
            // no valid JSON other than a 1-digit number (which was already covered) can be one character
            // either a special scalar or a negative number
            next_c = inp[ii + 1];
            if (cur_c == '-' || cur_c == '+')
            {
                // try +/-infinity or number
                if (next_c >= '0' && next_c <= '9')
                {
                    return ParseNumber(inp);
                }
                if (next_c == 'I' && inp.Substring(ii + 2, 7) == "nfinity")
                {
                    if (HandleError("Infinity is not part of the original JSON specification.", inp, ii, ParserState.NAN_INF))
                    {
                        return new JNode(null, Dtype.NULL, start_utf8_pos);
                    }
                    ii += 9;
                    if (cur_c == '+')
                        return new JNode(NanInf.inf, Dtype.FLOAT, start_utf8_pos);
                    return new JNode(NanInf.neginf, Dtype.FLOAT, start_utf8_pos);
                }
                HandleError("Expected literal starting with '-' or '+' to be a number",
                    inp, ii+1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, start_utf8_pos);
            }
            if (ii >= inp.Length - 3)
            {
                HandleError("No valid literal possible", inp, ii, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, start_utf8_pos);
            }
            // OK, so maybe it's a special scalar like null or true or NaN?
            if (cur_c == 'n')
            {
                // try null
                if (ii < inp.Length - 3 && next_c == 'u' && inp[ii + 2] == 'l' && inp[ii + 3] == 'l')
                {
                    ii += 4;
                    return new JNode(null, Dtype.NULL, start_utf8_pos);
                }
                HandleError("Expected literal starting with 'n' to be null",
                                              inp, ii+1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, start_utf8_pos);
            }
            if (cur_c == 't')
            {
                // try true
                if (ii < inp.Length - 3 && next_c == 'r' && inp[ii + 2] == 'u' && inp[ii + 3] == 'e')
                {
                    ii += 4;
                    return new JNode(true, Dtype.BOOL, start_utf8_pos);
                }
                HandleError("Expected literal starting with 't' to be true", inp, ii+1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, start_utf8_pos);
            }
            if (cur_c == 'f')
            {
                // try false
                if (ii < inp.Length - 4 && next_c == 'a' && inp.Substring(ii + 2, 3) == "lse")
                {
                    ii += 5;
                    return new JNode(false, Dtype.BOOL, start_utf8_pos);
                }
                HandleError("Expected literal starting with 'f' to be false",
                                              inp, ii+1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, start_utf8_pos);
            }
            if (cur_c == 'N')
            {
                // try NaN
                if (next_c == 'a' && inp[ii + 2] == 'N')
                {
                    if (HandleError("NaN is not part of the original JSON specification.", inp, ii, ParserState.NAN_INF))
                    {
                        return new JNode(null, Dtype.NULL, start_utf8_pos);
                    }
                    ii += 3;
                    return new JNode(NanInf.nan, Dtype.FLOAT, start_utf8_pos);
                }
                HandleError("Expected literal starting with 'N' to be NaN", inp, ii+1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, start_utf8_pos);
            }
            if (cur_c == 'I')
            {
                // try Infinity
                if (ii < inp.Length - 6 && next_c == 'n' && inp.Substring(ii + 2, 6) == "finity")
                {
                    if (HandleError("Infinity is not part of the original JSON specification.", inp, ii, ParserState.NAN_INF))
                    {
                        return new JNode(null, Dtype.NULL, start_utf8_pos);
                    }
                    ii += 8;
                    return new JNode(NanInf.inf, Dtype.FLOAT, start_utf8_pos);
                }
                HandleError("Expected literal starting with 'I' to be Infinity",
                                              inp, ii+1, ParserState.FATAL);
                return new JNode(null, Dtype.NULL, start_utf8_pos);
            }
            HandleError("Badly located character", inp, ii, ParserState.FATAL);
            return new JNode(null, Dtype.NULL, start_utf8_pos);
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
            ConsumeWhiteSpace(inp);
            if (ii >= inp.Length)
            {
                HandleError("Json string is only whitespace", inp, inp.Length - 1, ParserState.FATAL);
                return new JNode();
            }
            char cur_c = inp[ii];
            if ((cur_c == '/' || cur_c == '#')
                && !MaybeConsumeComment(inp))
            {
                return new JNode();
            }
            if (ii >= inp.Length)
            {
                HandleError("Json string is only whitespace and maybe comments", inp, inp.Length - 1, ParserState.FATAL);
                return new JNode();
            }
            JNode json = ParseSomething(inp, 0);
            ConsumeWhiteSpace(inp);
            if (ii < inp.Length)
            {
                cur_c = inp[ii];
                if (cur_c == '/' || cur_c == '#') MaybeConsumeComment(inp);
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
            int last_ii = 0;
            JNode json;
            List<JNode> children = new List<JNode>();
            JArray arr = new JArray(0, children);
            int line_num = 0;
            while (ii < inp.Length)
            {
                json = ParseSomething(inp, 0);
                children.Add(json);
                if (fatal)
                {
                    return arr;
                }
                for (; last_ii < ii; last_ii++)
                {
                    if (inp[ii] == '\n')
                        line_num++;
                }
                // make sure this document was all in one line
                if (line_num != arr.Length - 1)
                {
                    if (ii > inp.Length - 1)
                        ii = inp.Length - 1;
                    HandleError(
                        "JSON Lines document does not contain exactly one JSON document per line",
                        inp, ii, ParserState.FATAL
                    );
                    return arr;
                }
                ConsumeWhiteSpace(inp); // go to next line
            }
            for (; last_ii < ii; last_ii++)
            {
                if (inp[ii] == '\n')
                    line_num++;
            }
            // one last check to make sure the document has one JSON doc per line
            // it's fine for the document to have one empty line at the end
            if (line_num != arr.Length - 1 && line_num != arr.Length)
            {
                if (ii > inp.Length - 1)
                    ii = inp.Length - 1;
                HandleError(
                    "JSON Lines document does not contain exactly one JSON document per line",
                    inp, ii, ParserState.FATAL
                );
            }
            return arr;
        }

        /// <summary>
        /// reset the lint, position, and utf8_extra_bytes of this parser
        /// </summary>
        public void Reset()
        {
            lint.Clear();
            utf8_extra_bytes = 0;
            ii = 0;
        }

        /// <summary>
        /// create a new JsonParser with all the same settings as this one
        /// </summary>
        /// <returns></returns>
        public JsonParser Copy()
        {
            return new JsonParser(logger_level, parse_datetimes, throw_if_logged);
        }
    }
    #endregion
        
}