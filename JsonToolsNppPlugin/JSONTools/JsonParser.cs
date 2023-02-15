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
    /// An exception thrown when the parser encounters syntactically invalid JSON.
    /// Subclasses FormatException.
    /// </summary>
    public class JsonParserException : FormatException
    {
        public new string Message { get; set; }
        public char Cur_char { get; set; }
        public int Pos { get; set; }

        public JsonParserException(string Message, char cur_char, int pos)
        {
            this.Message = Message;
            this.Cur_char = cur_char;
            this.Pos = pos;
        }

        public JsonParserException(string Message)
        {
            this.Message=Message;
            this.Cur_char = '\x00';
            this.Pos = 0;
        }

        public override string ToString()
        {
            return $"{Message} at position {Pos} (char '{Cur_char}')";
        }
    }

    /// <summary>
    /// A syntax error caught and logged by the linter.
    /// </summary>
    public struct JsonLint
    {
        public string message;
        public int pos;
        public int line;
        public char cur_char;

        public JsonLint(string message, int pos, int line, char cur_char)
        {
            this.message = message;
            this.pos = pos;
            this.line = line;
            this.cur_char = cur_char;
        }

        public override string ToString()
        {
            return $"JsonLint({message}, {pos}, {line}, '{cur_char}')";
        }
    }

    /// <summary>
    /// Parses a JSON document into a <seealso cref="JNode"/> tree.
    /// </summary>
    public class JsonParser
    {
        // need to track recursion depth because stack overflow causes a panic that makes Notepad++ crash
        public const int MAX_RECURSION_DEPTH = 512;
        #region JSON_PARSER_ATTRS
        ///<summary>
        /// If true, float NaN (not a number) and float infinity and -infinity are allowed
        /// in JSON as the strings "NaN", "Infinity", and "-Infinity" respectively.
        /// These are not part of the official JSON spec, so they can be disallowed.
        ///</summary>
        public bool allow_nan_inf;
        /// <summary>
        /// If true, JavaScript single-line (preceded by "//") and multiline ("/* ... */") comments
        /// will be ignored rather than raising an exception.
        /// </summary>
        public bool allow_javascript_comments;
        /// <summary>
        /// If true, Strings enquoted with ' rather than " will be tolerated rather than raising an exception.
        /// </summary>
        public bool allow_singlequoted_str;
        /// <summary>
        /// If true, Strings consisting only of ASCII letters and underscores without any surrounding quotes will be tolerated.
        /// </summary>
        public bool allow_unquoted_str;
        /// <summary>
        /// If true, any strings in the standard formats of ISO 8601 dates (yyyy-MM-dd) and datetimes (yyyy-MM-dd hh:mm:ss.sss)
        ///  will be automatically parsed as the appropriate type.
        ///  Not currently supported. May never be.
        /// </summary>
        public bool allow_datetimes;
        /// <summary>
        /// If "linting" is true, most forms of invalid syntax will not cause the parser to stop, but instead the syntax error will be recorded in a list.
        /// </summary>
        public List<JsonLint> lint;
        //public Dictionary<char, char> escape_map 
        // no customization for date culture will be available - dates will be recognized as yyyy-mm-dd
        // and datetimes will be recognized as YYYY-MM-DDThh:mm:ss.sssZ (the Z at the end indicates that it's UTC)
        // see https://stackoverflow.com/questions/10286204/what-is-the-right-json-date-format
        /// <summary>
        /// position in JSON string
        /// </summary>
        public int ii = 0;
        public int line_num = 0;

        public JsonParser(bool allow_datetimes = false,
                          bool allow_singlequoted_str = false,
                          bool allow_javascript_comments = false,
                          bool linting = false,
                          bool allow_unquoted_str = false,
                          bool allow_nan_inf = true)
        {
            this.allow_javascript_comments = allow_javascript_comments;
            this.allow_singlequoted_str = allow_singlequoted_str;
            this.allow_unquoted_str = allow_unquoted_str; // not currently supported; may never be supported
            this.allow_datetimes = allow_datetimes;
            this.allow_nan_inf = allow_nan_inf;
            lint = linting ? new List<JsonLint>() : null;
        }

        #endregion
        #region HELPER_METHODS

        private void ConsumeWhiteSpace(string inp)
        {
            char c;
            while (ii < inp.Length)
            {
                c = inp[ii];
                if (c == ' ' || c == '\t' || c == '\r') { ii++; }
                else if (c == '\n') { ii++; line_num++; }
                else { return; }
            }
        }

        private void ConsumeComment(string inp)
        {
            char cur_c;
            char next_c;
            while (ii < inp.Length - 1 && inp[ii] == '/')
            {
                next_c = inp[ii + 1];
                if (next_c == '/')
                {
                    // one or more single-line comments
                    ii++;
                    while (ii < inp.Length)
                    {
                        cur_c = inp[ii];
                        if (cur_c == '\n')
                        {
                            ConsumeWhiteSpace(inp);
                            break;
                        }
                        ii++;
                    }
                }
                else if (next_c == '*')
                {
                    // a multi-line comment (/* ... */)
                    ii++;
                    bool comment_ended = false;
                    while (ii < inp.Length - 1)
                    {
                        cur_c = inp[ii++];
                        if (cur_c == '\n') { line_num++; }
                        else if (cur_c == '*')
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
                        throw new JsonParserException("Unterminated multi-line comment", inp[ii], ii);
                    }
                }
                ConsumeWhiteSpace(inp);
            }
        }

        private void MaybeConsumeComment(string inp)
        {
            if (allow_javascript_comments || lint != null)
            {
                if (lint != null)
                {
                    lint.Add(new JsonLint("JavaScript comments are not part of the original JSON specification", ii, line_num, '/'));
                }
                ConsumeComment(inp);
                return;
            }
            throw new JsonParserException("JavaScript comments are not part of the original JSON specification", inp[ii], ii);
        }

        /// <summary>
        /// read a hexadecimal integer representation of length `length` at position `index` in `inp`.
        /// Throws an JsonParserException of the integer is not valid hexadecimal
        /// or if `index` is less than `length` from the end of `inp`.
        /// </summary>
        /// <exception cref="JsonParserException"></exception>
        private int ParseHexadecimal(string inp, int length)
        {
            var sb = new StringBuilder();
            int end = ii + length > inp.Length ? inp.Length : ii + length;
            for ( ; ii < end; ii++)
            {
                sb.Append(inp[ii]);
            }
            string s = sb.ToString();
            // Npp.AddText($"hex of length {length} = {s}, will go to char {inp[index + length - 1]}");
            int charval;
            try
            {
                charval = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                if (0xd800 <= charval && charval <= 0xdbff
                    && inp[end - 1] == '\\' && inp[end] == 'u')
                {
                    // see https://github.com/python/cpython/blob/main/Lib/json/encoder.py
                    // Characters bigger than 0xffff are encoded as surrogate pairs
                    // of 2-byte characters, and this is a way to tell that you're going
                    // to see a surrogate pair
                    ii = end + 1;
                    int charval2 = ParseHexadecimal(inp, 4);
                    return 0x10000 + (((charval - 0xd800) << 10) | (charval2 - 0xdc00));
                }
                ii = end - 1;
                return charval;
                // the -1 is because ParseString increments by 1 after every escaped sequence anyway
            }
            catch
            {
                throw new JsonParserException("Could not find valid hexadecimal of length " + length,
                                              inp[end - 1], end - 1);
            }
        }

        private static Regex DATE_TIME_REGEX = new Regex(@"^\d{4}-\d\d-\d\d # date
                                                           (?:[T ](?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d # hours, minutes, seconds
                                                           (?:\.\d{1,3})?Z?)?$ # milliseconds",
                                                         RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        private static JNode TryParseDateOrDateTime(string maybe_datetime, int line_num)
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
                        return new JNode(DateTime.Parse(maybe_datetime), Dtype.DATE, line_num);
                    }
                    if (len >= 19 && len <= 23)
                    {
                        // yyyy-mm-dd hh:mm:ss has length 19, and yyyy-mm-dd hh:mm:ss.sss has length 23
                        return new JNode(DateTime.Parse(maybe_datetime), Dtype.DATETIME, line_num);
                    }
                }
                catch { } // it was an invalid date, i guess
            }
            // it didn't match, so it's just a normal string
            return new JNode(maybe_datetime, Dtype.STR, line_num);
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
        /// Throws a JsonParserException for any of the following reasons:<br></br>
        /// 1. Unterminated (no closing ")<br></br>
        /// 2. Contains invalid hexadecimal<br></br>
        /// 3. Contains "\\" escaping a character other than 'n', 'b', 'r', '\', '/', '"', 'f', or 't'.<br></br>
        /// </summary>
        /// <param name="inp">the json string</param>
        /// <param name="startpos">the current location in the string</param>
        /// <param name="line_num">the current line number in the string</param>
        /// <returns>a JNode of type Dtype.STR, and the position of the end of the string literal</returns>
        /// <exception cref="JsonParserException">
        /// </exception>
        public JNode ParseString(string inp, char quote_char = '"')
        {
            int start = ii++;
            char cur_c;
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (ii == inp.Length)
                {
                    if (lint == null) throw new JsonParserException("Unterminated string literal", inp[ii - 1], ii - 1);
                    lint.Add(new JsonLint($"Unterminated string literal starting at position {start}", ii - 1, line_num, inp[ii - 1]));
                    break;
                }
                cur_c = inp[ii];
                if (cur_c == '\n')
                {
                    // internal newlines are not allowed in JSON strings
                    if (lint == null) throw new JsonParserException("Unterminated string literal", cur_c, ii);
                    line_num++;
                    lint.Add(new JsonLint($"String literal starting at position {start} contains newline", ii, line_num, inp[ii]));
                }
                if (cur_c == quote_char)
                {
                    break;
                }
                else if (cur_c == '\\')
                {
                    if (ii >= inp.Length - 2)
                    {
                        if (lint == null) throw new JsonParserException("Unterminated string literal", cur_c, ii);
                        lint.Add(new JsonLint($"Unterminated string literal starting at position {start}", ii, line_num, inp[ii]));
                        break;
                    }
                    char next_char = inp[ii + 1];
                    if (next_char == quote_char)
                    {
                        sb.Append(next_char);
                        ii += 2;
                        continue;
                    }
                    if (ESCAPE_MAP.TryGetValue(next_char, out char escaped_char))
                    {
                        sb.Append(escaped_char);
                        ii += 2;
                        continue;
                    }
                    int next_hex;
                    if (next_char == 'u')
                    {
                        // 2-byte unicode of the form \uxxxx
                        // \x and \U escapes are not part of the JSON standard
                        try
                        {
                            ii += 2;
                            next_hex = ParseHexadecimal(inp, 4);
                            sb.Append((char)next_hex);
                        }
                        catch (Exception e)
                        {
                            if (lint != null && e is JsonParserException je)
                            {
                                lint.Add(new JsonLint($"Invalid hexadecimal ending at {ii}", je.Pos, line_num, je.Cur_char));
                                ii = je.Pos;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (lint == null) throw new JsonParserException("Invalidly escaped char", next_char, ii+1);
                        lint.Add(new JsonLint($"Invalidly escaped char {next_char}", ii+1, line_num, next_char));
                        ii++;
                        break;
                    }
                }
                else
                {
                    sb.Append(cur_c);
                }
                ii++;
            }
            ii++;
            if (allow_datetimes)
            {
                return TryParseDateOrDateTime(sb.ToString(), line_num);
            }
            return new JNode(sb.ToString(), Dtype.STR, line_num);
        }

        /// <summary>
        /// Parse a number in a JSON string.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <param name="startpos">The starting position</param>
        /// <param name="line_num">The starting line number</param>
        /// <returns>a JNode with type = Dtype.INT or Dtype.FLOAT, and the position of the end of the number.
        /// </returns>
        public JNode ParseNumber(string inp)
        {
            StringBuilder sb = new StringBuilder();
            // parsed tracks which portions of a number have been parsed.
            // So if the int part has been parsed, it will be 1.
            // If the int and decimal point parts have been parsed, it will be 3.
            // If the int, decimal point, and scientific notation parts have been parsed, it will be 7
            int parsed = 1;
            char c = inp[ii];
            if (c == '-' || c == '+')
            {
                sb.Append(c);
                ii++;
            }
            while (ii < inp.Length)
            {
                c = inp[ii];
                if (c >= '0' && c <= '9')
                {
                    sb.Append(c);
                    ii++;
                }
                else if (c == '.')
                {
                    if (parsed != 1)
                    {
                        if (lint == null) throw new JsonParserException("Number with two decimal points", c, ii);
                        lint.Add(new JsonLint("Number with two decimal points", ii, line_num, c));
                        break;
                    }
                    parsed = 3;
                    sb.Append('.');
                    ii++;
                }
                else if (c == 'e' || c == 'E')
                {
                    if ((parsed & 4) != 0)
                    {
                        break;
                    }
                    parsed += 4;
                    sb.Append('e');
                    if (ii < inp.Length - 1)
                    {
                        c = inp[++ii];
                        if (c == '+' || c == '-')
                        {
                            sb.Append(c);
                            ii++;
                        }
                    }
                }
                else if (c == '/')
                {
                    // fractions are part of the JSON language specification
                    double numer = double.Parse(sb.ToString(), JNode.DOT_DECIMAL_SEP);
                    JNode denom_node;
                    ii++;
                    denom_node = ParseNumber(inp);
                    double denom = Convert.ToDouble(denom_node.value);
                    return new JNode(numer / denom, Dtype.FLOAT, line_num);
                }
                else
                {
                    break;
                }
            }
            if (parsed == 1)
            {
                try
                {
                    return new JNode(long.Parse(sb.ToString()), Dtype.INT, line_num);
                }
                catch (OverflowException)
                {
                    // doubles can represent much larger numbers than 64-bit ints, albeit with loss of precision
                    return new JNode(double.Parse(sb.ToString()), Dtype.FLOAT, line_num);
                }
            }
            return new JNode(double.Parse(sb.ToString(), JNode.DOT_DECIMAL_SEP), Dtype.FLOAT, line_num);
        }

        /// <summary>
        /// Parse an array in a JSON string.<br></br>
        /// May raise a JsonParserException for any of the following reasons:<br></br>
        /// 1. The array is not terminated by ']'.<br></br>
        /// 2. The array is terminated with '}' instead of ']'.<br></br>
        /// 3. Two commas with nothing but whitespace in between.<br></br>
        /// 4. A comma before the first value.<br></br>
        /// 5. A comma after the last value.<br></br>
        /// 6. Two values with no comma in between.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <param name="startpos">The starting position</param>
        /// <param name="line_num">The starting line number</param>
        /// <returns>a JArray, and the position of the end of the array.</returns>
        /// <exception cref="JsonParserException"></exception>
        public JArray ParseArray(string inp, int recursion_depth)
        {
            var children = new List<JNode>();
            bool already_seen_comma = false;
            int start_line_num = line_num;
            char cur_c = inp[++ii];
            // need to do this to avoid stack overflow when presented with unreasonably deep nesting
            // stack overflow causes an unrecoverable panic, and we would rather fail gracefully
            if (recursion_depth == MAX_RECURSION_DEPTH)
                throw new JsonParserException($"Maximum recursion depth ({MAX_RECURSION_DEPTH}) reached", cur_c, ii);
            while (ii < inp.Length)
            {
                ConsumeWhiteSpace(inp);
                cur_c = inp[ii];
                if (cur_c == ',')
                {
                    if (already_seen_comma)
                    {
                        if (lint == null) throw new JsonParserException($"Two consecutive commas after element {children.Count - 1} of array", cur_c, ii);
                        lint.Add(new JsonLint($"Two consecutive commas after element {children.Count - 1} of array", ii, line_num, cur_c));
                    }
                    already_seen_comma = true;
                    if (children.Count == 0)
                    {
                        if (lint == null) throw new JsonParserException("Comma before first value in array", cur_c, ii);
                        lint.Add(new JsonLint("Comma before first value in array", ii, line_num, cur_c));
                    }
                    ii++;
                    continue;
                }
                else if (cur_c == ']')
                {
                    if (already_seen_comma)
                    {
                        if (lint == null) throw new JsonParserException("Comma after last element of array", cur_c, ii);
                        lint.Add(new JsonLint("Comma after last element of array", ii, line_num, cur_c));
                    }
                    JArray arr = new JArray(start_line_num, children);
                    // Npp.AddText("Returning array " + arr.ToString());
                    ii++;
                    return arr;
                }
                else if (cur_c == '}')
                {
                    if (lint == null) throw new JsonParserException("Tried to terminate an array with '}'", cur_c, ii);
                    if (already_seen_comma)
                    {
                        if (lint == null) throw new JsonParserException("Comma after last element of array", cur_c, ii);
                        lint.Add(new JsonLint("Comma after last element of array", ii, line_num, cur_c));
                    }
                    lint.Add(new JsonLint("Tried to terminate an array with '}'", ii, line_num, cur_c));
                    JArray arr = new JArray(start_line_num, children);
                    ii++;
                    return arr;
                }
                else if (cur_c == '/') MaybeConsumeComment(inp);
                else
                {
                    if (children.Count > 0 && !already_seen_comma)
                    {
                        if (lint == null) throw new JsonParserException("No comma between array members", cur_c, ii);
                        lint.Add(new JsonLint("No comma between array members", ii, line_num, cur_c));
                    }
                    // a new array member of some sort
                    already_seen_comma = false;
                    JNode new_obj;
                    new_obj = ParseSomething(inp, recursion_depth);
                    // Npp.AddText("\nobj = "+new_obj.ToString());
                    children.Add(new_obj);
                }
            }
            if (lint == null)
                throw new JsonParserException("Unterminated array", cur_c, ii);
            lint.Add(new JsonLint("Unterminated array", ii, line_num, cur_c));
            return new JArray(start_line_num, children);
        }

        /// <summary>
        /// Parse an object in a JSON string.<br></br>
        /// May raise a JsonParserException for any of the following reasons:<br></br>
        /// 1. The object is not terminated by ']'.<br></br>
        /// 2. The object is terminated with ']' instead of '}'.<br></br>
        /// 3. Two commas with nothing but whitespace in between.<br></br>
        /// 4. A comma before the first key-value pair.<br></br>
        /// 5. A comma after the last key-value pair.<br></br>
        /// 6. Two key-value pairs with no comma in between.<br></br>
        /// 7. No ':' between a key and a value.<br></br>
        /// 8. A key that's not a string.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <param name="startpos">The starting position</param>
        /// <param name="line_num">The starting line number</param>
        /// <returns>a JArray, and the position of the end of the array.</returns>
        /// <exception cref="JsonParserException"></exception>
        public JObject ParseObject(string inp, int recursion_depth)
        {
            int start_line_num = line_num;
            var children = new Dictionary<string, JNode>();
            bool already_seen_comma = false;
            char cur_c = inp[++ii];
            if (recursion_depth == MAX_RECURSION_DEPTH)
                throw new JsonParserException($"Maximum recursion depth ({MAX_RECURSION_DEPTH}) reached", cur_c, ii);
            string child_key;
            while (ii < inp.Length)
            {
                ConsumeWhiteSpace(inp);
                cur_c = inp[ii];
                if (cur_c == ',')
                {
                    if (already_seen_comma)
                    {
                        if (lint == null) throw new JsonParserException($"Two consecutive commas after key-value pair {children.Count - 1} of object", cur_c, ii);
                        lint.Add(new JsonLint($"Two consecutive commas after key-value pair {children.Count - 1} of object", ii, line_num, cur_c));
                    }
                    already_seen_comma = true;
                    if (children.Count == 0)
                    {
                        if (lint == null) throw new JsonParserException("Comma before first value in object", cur_c, ii);
                        lint.Add(new JsonLint("Comma before first value in object", ii, line_num, cur_c));
                    }
                    ii++;
                    continue;
                }
                else if (cur_c == '}')
                {
                    if (already_seen_comma)
                    {
                        if (lint == null) throw new JsonParserException("Comma after last key-value pair of object", cur_c, ii);
                        lint.Add(new JsonLint("Comma after last key-value pair of object", ii, line_num, cur_c));
                    }
                    JObject obj = new JObject(start_line_num, children);
                    // Npp.AddText("Returning array " + obj.ToString());
                    ii++;
                    return obj;
                }
                else if (cur_c == ']')
                {
                    if (lint == null) throw new JsonParserException("Tried to terminate object with ']'", cur_c, ii);
                    if (already_seen_comma)
                    {
                        if (lint == null) throw new JsonParserException("Comma after last key-value pair of object", cur_c, ii);
                        lint.Add(new JsonLint("Comma after last key-value pair of object", ii, line_num, cur_c));
                    }
                    lint.Add(new JsonLint("Tried to terminate object with ']'", ii, line_num, cur_c));
                    JObject obj = new JObject(start_line_num, children);
                    ii++;
                    return obj;
                }
                else if (cur_c == '"' 
                    || ((allow_singlequoted_str || lint != null) && cur_c == '\''))
                {
                    if (children.Count > 0 && !already_seen_comma)
                    {
                        if (lint == null) throw new JsonParserException($"No comma after key-value pair {children.Count - 1} in object", cur_c, ii);
                        lint.Add(new JsonLint($"No comma after key-value pair {children.Count - 1} in object", ii, line_num, cur_c));
                    }
                    if (cur_c == '\'' && lint != null)
                    {
                        lint.Add(new JsonLint("Strings must be quoted with \" rather than '", ii, line_num, cur_c));
                    }
                    // a new key-value pair
                    JNode keystring = ParseString(inp, cur_c);
                    //child_key = (string)keystring.value;
                    string child_keystr = keystring.ToString();
                    child_key = child_keystr.Substring(1, child_keystr.Length - 2);
                    if (inp[ii] != ':')
                    {
                        // avoid call overhead in most likely case where colon comes
                        // immediately after key
                        ConsumeWhiteSpace(inp);
                        if (cur_c == '/') MaybeConsumeComment(inp);
                    }
                    if (inp[ii] != ':')
                    {
                        if (lint == null) throw new JsonParserException($"No ':' between key {children.Count} and value {children.Count} of object", inp[ii], ii);
                        lint.Add(new JsonLint($"No ':' between key {children.Count} and value {children.Count} of object", ii, line_num, cur_c));
                        ii--;
                    }
                    ii++;
                    ConsumeWhiteSpace(inp);
                    if (inp[ii] == '/') MaybeConsumeComment(inp);
                    JNode new_obj = ParseSomething(inp, recursion_depth);
                    // Npp.AddText($"\nkey = {child_key}, obj = {new_obj.ToString()}");
                    children.Add(child_key, new_obj);
                    already_seen_comma = false;
                }
                else if (inp[ii] == '/') MaybeConsumeComment(inp);
                else
                {
                    if (lint == null) throw new JsonParserException($"Key in object (would be key {children.Count}) must be string", cur_c, ii);
                    lint.Add(new JsonLint($"Key in object (would be key {children.Count}) must be string", ii, line_num, cur_c));
                    ii++;
                }
            }
            if (lint == null)
                throw new JsonParserException("Unterminated object", cur_c, ii);
            lint.Add(new JsonLint($"Unterminated object", ii, line_num, cur_c));
            return new JObject(start_line_num, children);
        }

        /// <summary>
        /// Parse anything (a scalar, null, an object, or an array) in a JSON string.<br></br>
        /// May raise a JsonParserException for any of the following reasons:<br></br>
        /// 1. Whatever reasons ParseObject, ParseArray, or ParseString might throw an error.<br></br>
        /// 2. An unquoted string other than true, false, null, NaN, Infinity, -Infinity.<br></br>
        /// 3. The JSON string contains only blankspace or is empty.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <param name="startpos">The starting position</param>
        /// <param name="line_num">The starting line number</param>
        /// <returns>a JArray, and the position of the end of the array.</returns>
        /// <exception cref="JsonParserException"></exception>
        public JNode ParseSomething(string inp, int recursion_depth)
        {
            char cur_c = inp[ii]; // could throw IndexOutOfRangeException, but we'll handle that elsewhere
            char next_c;
            if (cur_c == '"' || ((allow_singlequoted_str || lint != null) && cur_c == '\''))
            {
                if (cur_c == '\'' && lint != null)
                {
                    lint.Add(new JsonLint("Strings must be quoted with \" rather than '", ii, line_num, cur_c));
                }
                return ParseString(inp, cur_c);
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
            // either a special scalar or a negative number
            next_c = inp[ii + 1];
            if (cur_c == '-' || (cur_c == '+' && lint != null))
            {
                // try +/-infinity or number
                if (next_c >= '0' && next_c <= '9')
                {
                    return ParseNumber(inp);
                }
                if (next_c == 'I' && inp.Substring(ii + 2, 7) == "nfinity")
                {
                    if (!allow_nan_inf)
                    {
                        throw new JsonParserException("Infinity is not part of the original JSON specification.", next_c, ii);
                    }
                    else if (lint != null)
                    {
                        lint.Add(new JsonLint("Infinity is not part of the original JSON specification", ii, line_num, cur_c));
                    }
                    ii += 9;
                    if (cur_c == '+')
                        return new JNode(NanInf.inf, Dtype.FLOAT, line_num);
                    return new JNode(NanInf.neginf, Dtype.FLOAT, line_num);
                }
                throw new JsonParserException("Expected literal starting with '-' or '+' to be a number",
                    next_c, ii+1);
            }
            if (ii == inp.Length - 2)
            {
                throw new JsonParserException("No valid literal", cur_c, ii);
            }
            // OK, so maybe it's a special scalar like null or true or NaN?
            if (cur_c == 'n')
            {
                // try null
                if (next_c == 'u' && inp.Substring(ii + 2, 2) == "ll")
                {
                    ii += 4;
                    return new JNode(null, Dtype.NULL, line_num);
                }
                throw new JsonParserException("Expected literal starting with 'n' to be null",
                                              next_c, ii+1);
            }
            if (cur_c == 'N')
            {
                // try NaN
                if (next_c == 'a' && inp[ii + 2] == 'N')
                {
                    if (!allow_nan_inf)
                    {
                        throw new JsonParserException("NaN is not part of the original JSON specification.", next_c, ii);
                    }
                    else if (lint != null)
                    {
                        lint.Add(new JsonLint("NaN is not part of the original JSON specification", ii, line_num, cur_c));
                    }
                    ii += 3;
                    return new JNode(NanInf.nan, Dtype.FLOAT, line_num);
                }
                throw new JsonParserException("Expected literal starting with 'N' to be NaN", next_c, ii+1);
            }
            if (cur_c == 'I')
            {
                // try Infinity
                if (next_c == 'n' && inp.Substring(ii + 2, 6) == "finity")
                {
                    if (!allow_nan_inf)
                    {
                        throw new JsonParserException("Infinity is not part of the original JSON specification.", next_c, ii);
                    }
                    else if (lint != null)
                    {
                        lint.Add(new JsonLint("Infinity is not part of the original JSON specification", ii, line_num, cur_c));
                    }
                    ii += 8;
                    return new JNode(NanInf.inf, Dtype.FLOAT, line_num);
                }
                throw new JsonParserException("Expected literal starting with 'I' to be Infinity",
                                              next_c, ii+1);
            }
            if (cur_c == 't')
            {
                // try true
                if (next_c == 'r' && inp[ii + 2] == 'u' && inp[ii + 3] == 'e')
                {
                    ii += 4;
                    return new JNode(true, Dtype.BOOL, line_num);
                }
                throw new JsonParserException("Expected literal starting with 't' to be true", next_c, ii+1);
            }
            if (cur_c == 'f')
            {
                // try false
                if (next_c == 'a' && inp.Substring(ii + 2, 3) == "lse")
                {
                    ii += 5;
                    return new JNode(false, Dtype.BOOL, line_num);
                }
                throw new JsonParserException("Expected literal starting with 'f' to be false",
                                              next_c, ii+1);
            }
            throw new JsonParserException("Badly located character", cur_c, ii);
        }

        /// <summary>
        /// Parse a JSON string and return a JNode representing the document.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <returns></returns>
        /// <exception cref="JsonParserException"></exception>
        public JNode Parse(string inp)
        {
            if (lint != null) lint.Clear();
            if (inp.Length == 0)
            {
                throw new JsonParserException("no input");
            }
            ii = 0;
            line_num = 0;
            ConsumeWhiteSpace(inp);
            if (inp[ii] == '/') MaybeConsumeComment(inp);
            if (ii >= inp.Length)
            {
                throw new JsonParserException("Json string is only whitespace");
            }
            try
            {
                JNode json = ParseSomething(inp, 0);
                ConsumeWhiteSpace(inp);
                if (ii < inp.Length && inp[ii] == '/') MaybeConsumeComment(inp);
                if (ii < inp.Length)
                {
                    throw new JsonParserException($"At end of valid JSON document, got {inp[ii]} instead of EOF", inp[ii], ii);
                }
                return json;
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException)
                {
                    throw new JsonParserException("Unexpected end of JSON", inp[inp.Length - 1], inp.Length - 1);
                }
                throw;
            }
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
            if (lint != null)
                lint.Clear();
            if (inp.Length == 0)
            {
                throw new JsonParserException("no input");
            }
            ii = 0;
            line_num = 0;
            JNode json = new JNode();
            List<JNode> arr = new List<JNode>();
            while (ii < inp.Length)
            {
                try
                {
                    json = ParseSomething(inp, 0);
                }
                catch (Exception e)
                {
                    if (e is IndexOutOfRangeException)
                    {
                        throw new JsonParserException("Unexpected end of JSON", inp[inp.Length - 1], inp.Length - 1);
                    }
                    throw;
                }
                arr.Add(json);
                // make sure this document was all in one line
                if (line_num != arr.Count - 1)
                {
                    if (ii > inp.Length - 1)
                        ii = inp.Length - 1;
                    throw new JsonParserException(
                        "JSON Lines document does not contain exactly one JSON document per line",
                        inp[ii], ii
                    );
                }
                ConsumeWhiteSpace(inp); // go to next line
            }
            // one last check to make sure the document has one JSON doc per line
            // it's fine for the document to have one empty line at the end
            if (line_num != arr.Count - 1 && line_num != arr.Count)
            {
                if (ii > inp.Length - 1)
                    ii = inp.Length - 1;
                throw new JsonParserException(
                    "JSON Lines document does not contain exactly one JSON document per line",
                    inp[ii], ii
                );
            }
            return new JArray(0, arr);
        }

        /// <summary>
        /// create a new JsonParser with all the same settings as this one
        /// </summary>
        /// <returns></returns>
        public JsonParser Copy()
        {
            bool linting = lint != null;
            return new JsonParser(
                allow_datetimes,
                allow_singlequoted_str,
                allow_javascript_comments,
                linting,
                allow_unquoted_str,
                allow_nan_inf
            );
        }
    }
    #endregion
        
}