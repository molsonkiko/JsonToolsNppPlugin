/*
A parser and linter for JSON.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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
        #region JSON_PARSER_ATTRS
        ///<summary>
        /// If true, float NaN (not a number) and float infinity and -infinity are allowed
        /// in JSON as the strings "NaN", "Infinity", and "-Infinity" respectively.
        /// These are not part of the official JSON spec, so they can be disallowed.
        ///</summary>
        public bool allow_nan_inf = true;
        /// <summary>
        /// If true, JavaScript single-line (preceded by "//") and multiline ("/* ... */") comments
        /// will be ignored rather than raising an exception.
        /// </summary>
        public bool allow_javascript_comments = false;
        /// <summary>
        /// If true, Strings enquoted with ' rather than " will be tolerated rather than raising an exception.
        /// </summary>
        public bool allow_singlequoted_str = false;
        /// <summary>
        /// If true, Strings consisting only of ASCII letters and underscores without any surrounding quotes will be tolerated.
        /// </summary>
        public bool allow_unquoted_str = false;
        /// <summary>
        /// If true, any strings in the standard formats of ISO 8601 dates (yyyy-MM-dd) and datetimes (yyyy-MM-dd hh:mm:ss.sss)
        ///  will be automatically parsed as the appropriate type.
        ///  Not currently supported. May never be.
        /// </summary>
        public bool allow_datetimes = false;
        /// <summary>
        /// If "linting" is true, most forms of invalid syntax will not cause the parser to stop, but instead the syntax error will be recorded in a list.
        /// </summary>
        public List<JsonLint> lint = null;
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
            if (linting) lint = new List<JsonLint>();
            //this.escape_map = ESCAPE_MAP;
        }

        #endregion
        #region HELPER_METHODS

        //public static bool IsWhiteSpace(char c)
        //{
        //    return (c == ' ' || c == '\t' || c == '\n' || c == '\r');
        //}

        private void ConsumeWhiteSpace(string inp)
        {
            char c;
            while (ii < inp.Length - 1)
            {
                c = inp[ii];
                // tried using if/else if, but it's slower
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
                    ii++;
                    while (ii < inp.Length)
                    {
                        cur_c = inp[ii++];
                        if (cur_c == '\n')
                        {
                            ConsumeWhiteSpace(inp);
                            return;
                        }
                    }
                }
                else if (next_c == '*')
                {
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
                                ii += 1;
                                return;
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
            for ( ; ii < ii + length && ii < inp.Length; ii++)
            {
                sb.Append(inp[ii]);
            }
            string s = sb.ToString();
            // Console.WriteLine($"hex of length {length} = {s}, will go to char {inp[index + length - 1]}");
            int charval;
            try
            {
                charval = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                ii += length - 1;
                return charval;
                // the -1 is because ParseString increments by 1 after every escaped sequence anyway
            }
            catch
            {
                throw new JsonParserException("Could not find valid hexadecimal of length " + length,
                                              inp[ii + length], ii + length);
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
            ii++;
            char cur_c;
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (ii == inp.Length)
                {
                    if (lint == null) throw new JsonParserException("Unterminated string literal", inp[ii], ii);
                    lint.Add(new JsonLint($"Unterminated string literal starting at position {ii}", ii, line_num, inp[ii]));
                    break;
                }
                cur_c = inp[ii];
                if (cur_c == '\n')
                {
                    // internal newlines are not allowed in JSON strings
                    if (lint == null) throw new JsonParserException("Unterminated string literal", cur_c, ii);
                    line_num++;
                    lint.Add(new JsonLint($"String literal starting at position {ii} contains newline", ii, line_num, inp[ii]));
                }
                if (cur_c == quote_char)
                {
                    break;
                }
                else if (cur_c == '\\')
                {
                    if (ii == inp.Length - 2)
                    {
                        if (lint == null) throw new JsonParserException("Unterminated string literal", cur_c, ii);
                        lint.Add(new JsonLint($"Unterminated string literal starting at position {ii}", ii, line_num, inp[ii]));
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
                            if (lint != null && e is JsonParserException)
                            {
                                JsonParserException je = (JsonParserException)e;
                                lint.Add(new JsonLint($"Invalid hexadecimal starting at {ii}", je.Pos, line_num, je.Cur_char));
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

        //public static readonly Regex num_regex = new Regex(@"(-?(?:0|[1-9]\d*))(\.\d+)?([eE][-+]?\d+)?");
        /// <summary>
        /// Parse a number in a JSON string.
        /// </summary>
        /// <param name="inp">the JSON string</param>
        /// <param name="startpos">The starting position</param>
        /// <param name="line_num">The starting line number</param>
        /// <returns>a JNode with type = Dtype.INT or Dtype.FLOAT, and the position of the end of the number.
        /// </returns>
        public JNode ParseNumber(string q)
        {
            StringBuilder sb = new StringBuilder();
            // parsed tracks which portions of a number have been parsed.
            // So if the int part has been parsed, it will be 1.
            // If the int and decimal point parts have been parsed, it will be 3.
            // If the int, decimal point, and scientific notation parts have been parsed, it will be 7
            int parsed = 1;
            char c = q[ii];
            if (c == '-' || c == '+')
            {
                sb.Append(c);
                ii++;
            }
            while (ii < q.Length)
            {
                c = q[ii];
                if (c >= '0' && c <= '9')
                {
                    sb.Append(c);
                    ii++;
                }
                else if (c == '.')
                {
                    if (parsed != 1)
                    {
                        if (lint == null) throw new RemesLexerException(ii, q, "Number with two decimal points");
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
                    if (ii < q.Length - 1)
                    {
                        c = q[++ii];
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
                    double numer = double.Parse(sb.ToString());
                    JNode denom_node;
                    denom_node = ParseNumber(q);
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
                return new JNode(long.Parse(sb.ToString()), Dtype.INT, line_num);
            }
            return new JNode(double.Parse(sb.ToString()), Dtype.FLOAT, line_num);
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
        public JArray ParseArray(string inp)
        {
            var children = new List<JNode>();
            bool already_seen_comma = false;
            int start_line_num = line_num;
            char cur_c = inp[++ii];
            while (ii < inp.Length)
            {
                ConsumeWhiteSpace(inp);
                cur_c = inp[ii];
                // tried using a switch statement instead of chained if/else if, but it's actually much slower
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
                    // Console.WriteLine("Returning array " + arr.ToString());
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
                    new_obj = ParseSomething(inp);
                    // Console.WriteLine("\nobj = "+new_obj.ToString());
                    children.Add(new_obj);
                }
            }
            throw new JsonParserException("Unterminated array", cur_c, ii);
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
        public JObject ParseObject(string inp)
        {
            int start_line_num = line_num;
            var children = new Dictionary<string, JNode>();
            bool already_seen_comma = false;
            char cur_c = inp[++ii];
            string child_key;
            while (ii < inp.Length)
            {
                ConsumeWhiteSpace(inp);
                cur_c = inp[ii];
                // tried using a switch statement here - it turns out to be much slower
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
                    // Console.WriteLine("Returning array " + obj.ToString());
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
                    return obj;
                }
                else if (cur_c == '"' 
                    || ((allow_singlequoted_str || lint != null) && cur_c == '\'')
                    //|| (allow_unquoted_str && (('a' <= cur_c && cur_c <= 'z') || ('A' <= cur_c && cur_c <= 'Z') || cur_c == '_'))
                    //// unquoted strings may someday be allowed so long as they start with letters or underscore and contain only letters, underscore, and digits
                    )
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
                    ConsumeWhiteSpace(inp);
                    if (inp[ii] == '/') MaybeConsumeComment(inp);
                    JNode new_obj = ParseSomething(inp);
                    // Console.WriteLine($"\nkey = {child_key}, obj = {new_obj.ToString()}");
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
            throw new JsonParserException("Unterminated object", cur_c, ii);
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
        public JNode ParseSomething(string inp)
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
                return ParseArray(inp);
            }
            if (cur_c == '{')
            {
                return ParseObject(inp);
            }
            // either a special scalar or a negative number
            next_c = inp[ii + 1];
            if (cur_c == '-')
            {
                // try negative infinity or number
                if (next_c >= '0' && next_c <= '9')
                {
                    return ParseNumber(inp);
                }
                if (next_c == 'I' && inp.Substring(ii + 2, 7) == "nfinity")
                {
                    if (!allow_nan_inf)
                    {
                        throw new JsonParserException("-Infinity is not part of the original JSON specification.", next_c, ii);
                    }
                    else if (lint != null)
                    {
                        lint.Add(new JsonLint("-Infinity is not part of the original JSON specification", ii, line_num, cur_c));
                    }
                    ii += 9;
                    return new JNode(1d, Dtype.FLOAT, line_num); // should return -inf
                }
                throw new JsonParserException("Expected literal starting with '-' to be negative number",
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
                    return new JNode(0d, Dtype.FLOAT, line_num); // should return NaN
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
                    //double nan = double.NaN;
                    //double neginf = double.NegativeInfinity;
                    double inf = 1d; // double.PositiveInfinity;
                    //if (nan == inf || inf == neginf) { }
                    return new JNode(inf, Dtype.FLOAT, line_num); // should return inf
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
            throw new JsonParserException("Badly located character", cur_c, inp.Length - 1);
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
                return ParseSomething(inp);
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
    }
        #endregion
        #region TEST_FUNCTIONS

    public class JsonParserTester
    {
        public static void Test()
        {
            JsonParser parser = new JsonParser();
            // includes:
            // 1. hex
            // 2. all other backslash escape sequences
            // 3. Empty arrays and objects
            // 4. empty strings
            // 5. space before and after commas
            // 6. no space between comma and next value
            // 7. space before colon and variable space after colon
            // 8. hex and escape sequences in keys
            // 9. all special scalars (nan, null, inf, -inf, true, false)
            // 10. all forms of whitespace
            string NL = Environment.NewLine;
            string example = "{\"a\":[-1, true, {\"b\" :  0.5, \"c\": \"\\uae77\"},null],\n"
                    + "\"a\\u10ff\":[true, false, NaN, Infinity,-Infinity, {},\t\"\\u043ea\", []], "
                    + "\"back'slas\\\"h\": [\"\\\"'\\f\\n\\b\\t/\", -0.5, 23, \"\"]} ";
            string norm_example = "{\"a\": [-1, true, {\"b\": 0.5, \"c\": \"\\uae77\"}, null], "
                    + "\"a\\u10ff\": [true, false, NaN, Infinity, -Infinity, {}, \"\\u043ea\", []], "
                    + "\"back'slas\\\"h\": [\"\\\"'\\f\\n\\b\\t/\", -0.5, 23, \"\"]}";
            string pprint_example = "{" +
                                    NL + "\"a\":" +
                                    NL + "    [" +
                                    NL + "    -1," +
                                    NL + "    true," +
                                    NL + "        {" +
                                    NL + "        \"b\": 0.5," +
                                    NL + "        \"c\": \"\\uae77\"" +
                                    NL + "        }," +
                                    NL + "    null" +
                                    NL + "    ]," +
                                    NL + "\"a\\u10ff\":" +
                                    NL + "    [" +
                                    NL + "    true," +
                                    NL + "    false," +
                                    NL + "    NaN," +
                                    NL + "    Infinity," +
                                    NL + "    -Infinity," +
                                    NL + "        {" +
                                    NL + "        }," +
                                    NL + "    \"\\u043ea\"," +
                                    NL + "        [" +
                                    NL + "        ]" +
                                    NL + "    ]," +
                                    NL + "\"back'slas\\\"h\":" +
                                    NL + "    [" +
                                    NL + "    \"\\\"'\\f\\n\\b\\t/\"," +
                                    NL + "    -0.5," +
                                    NL + "    23," +
                                    NL + "    \"\"" +
                                    NL + "    ]" +
                                    NL + "}";
            var testcases = new string[][]
            {
                new string[]{ example, norm_example, pprint_example, "general parsing" },
                new string[] { "1/2", "0.5", "0.5", "fractions" },
                new string[] { "[[]]", "[[]]", "[" + NL + "    [" + NL + "    ]" + NL + "]", "empty lists" },
                new string[] { "\"abc\"", "\"abc\"", "\"abc\"", "scalar string" },
                new string[] { "1", "1", "1", "scalar int" },
                new string[] { "-1.0", "-1.0", "-1.0", "negative scalar float" },
                new string[] { "3.5", "3.5", "3.5", "scalar float" },
                new string[] { "-4", "-4", "-4", "negative scalar int" },
                new string[] { "[{\"FullName\":\"C:\\\\123467756\\\\Z\",\"LastWriteTimeUtc\":\"\\/Date(1600790697130)\\/\"}," +
                            "{\"FullName\":\"C:\\\\123467756\\\\Z\\\\B\",\"LastWriteTimeUtc\":\"\\/Date(1618852147285)\\/\"}]",
                             "[{\"FullName\": \"C:\\\\123467756\\\\Z\", \"LastWriteTimeUtc\": \"/Date(1600790697130)/\"}, " +
                            "{\"FullName\": \"C:\\\\123467756\\\\Z\\\\B\", \"LastWriteTimeUtc\": \"/Date(1618852147285)/\"}]",
                             "[" +
                             NL + "    {" +
                             NL + "    \"FullName\": \"C:\\\\123467756\\\\Z\"," +
                             NL + "    \"LastWriteTimeUtc\": \"/Date(1600790697130)/\"" +
                             NL + "    }," +
                             NL + "    {" +
                             NL + "    \"FullName\": \"C:\\\\123467756\\\\Z\\\\B\"," +
                             NL + "    \"LastWriteTimeUtc\": \"/Date(1618852147285)/\"" +
                             NL + "    }" +
                             NL + "]",
                             "open issue in Kapilratnani's JSON-Viewer regarding forward slashes having '/' stripped" },
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (string[] test in testcases)
            {
                //(string input, string norm_input, string pprint_desired, string msg)
                string input = test[0], norm_input = test[1], pprint_desired = test[2], msg = test[3];
                JNode json = parser.Parse(input);
                string norm_str_out = json.ToString();
                string pprint_out = json.PrettyPrint(4);
                if (norm_str_out != norm_input)
                {
                    tests_failed++;
                    Console.WriteLine(String.Format(@"Test {0} ({1}) failed:
Expected
{2}
Got
{3} ",
                                     ii + 1, msg, norm_input, norm_str_out));
                }
                ii++;
                if (pprint_out != pprint_desired)
                {
                    tests_failed++;
                    Console.WriteLine(String.Format(@"Test {0} (pretty-print {1}) failed:
Expected
{2}
Got
{3} ",
                                     ii + 1, msg, pprint_desired, pprint_out));
                }
                ii++;
            }

            string objstr = "{\"a\": [1, 2, 3], \"b\": {}, \"c\": [], \"d\": 3}";
            JObject obj = (JObject)parser.Parse(objstr);
            string pp = obj.PrettyPrint();
            string pp_ch_line = obj.PrettyPrintAndChangeLineNumbers();
            ii++;
            if (pp != pp_ch_line)
            {
                tests_failed++;
                Console.WriteLine(String.Format(@"Test {0} failed:
Expected PrettyPrintAndChangeLineNumbers({1}) to return
{2}
instead got
{3}",
                                                ii + 1, objstr, pp, pp_ch_line));
            }

            var keylines = new object[][]
            {
                new object[]{"a", 2 },
                new object[]{ "b", 8 },
                new object[]{ "c", 11 },
                new object[]{"d", 13 }
            };
            foreach (object[] kl in keylines)
            {
                string key = (string)kl[0];
                int expected_line = (int)kl[1];
                ii++;
                int true_line = obj.children[key].line_num;
                if (true_line != expected_line)
                {
                    tests_failed++;
                    Console.WriteLine($"After PrettyPrintAndChangeLineNumbers({objstr}), expected the line of child {key} to be {expected_line}, got {true_line}.");
                }
            }

            string tostr = obj.ToString();
            string tostr_ch_line = obj.ToStringAndChangeLineNumbers();
            ii++;
            if (tostr != tostr_ch_line)
            {
                tests_failed++;
                Console.WriteLine(String.Format(@"Test {0} failed:
Expected ToStringAndChangeLineNumbers({1}) to return
{2}
instead got
{3}",
                                                ii+1, objstr, tostr, tostr_ch_line));
            }
            foreach (object[] kl in keylines)
            {
                string key = (string)kl[0];
                ii++;
                int true_line = obj.children[key].line_num;
                if (true_line != 0)
                {
                    tests_failed++;
                    Console.WriteLine($"After ToStringAndChangeLineNumbers({objstr}), expected the line of child {key} to be 0, got {true_line}.");
                }
            }

            // test if the parser correctly counts line numbers in nested JSON
            JObject pp_obj = (JObject)parser.Parse(pp_ch_line);
            foreach (object[] kl in keylines)
            {
                string key = (string)kl[0];
                int expected_line = (int)kl[1];
                int true_line = pp_obj.children[key].line_num;
                if (true_line != expected_line)
                {
                    tests_failed++;
                    Console.WriteLine($"After PrettyPrintAndChangeLineNumbers({pp}), expected the line of child {key} to be {expected_line}, got {true_line}.");
                }
            }

            var equality_testcases = new object[][]
            {
                new object[] { "1", "2", false },
                new object[] { "1", "1", true },
                new object[] { "2.5e3", "2.5e3", true },
                new object[] { "2.5e3", "2.2e3", false },
                new object[] { "\"a\"", "\"a\"", true },
                new object[] { "\"a\"", "\"b\"", false },
                new object[] { "[[1, 2], [3, 4]]", "[[1,2],[3,4]]", true },
                new object[] { "[1, 2, 3, 4]", "[[1,2], [3,4]]", false },
                new object[] { "{\"a\": 1, \"b\": Infinity, \"c\": 0.5}", "{\"b\": Infinity, \"a\": 1, \"c\": 1/2}", true },
                new object[] { "[\"z\\\"\"]", "[\"z\\\"\"]", true },
                new object[] { "{}", "{" + NL + "   }", true },
                new object[] { "[]", "[ ]", true },
                new object[] { "[]", "[1, 2]", false }
            };
            foreach (object[] test in equality_testcases)
            {
                string astr = (string)test[0];
                string bstr = (string)test[1];
                bool a_equals_b = (bool)test[2];
                ii++;
                JNode a = parser.Parse(astr);
                JNode b = parser.Parse(bstr);
                bool result = a.Equals(b);
                if (result != a_equals_b)
                {
                    tests_failed++;
                    Console.WriteLine($"Expected {a.ToString()} == {b.ToString()} to be {a_equals_b}, but it was called {result}");
                }
            }


            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }

        public static void TestSpecialParserSettings()
        {
            JsonParser simpleparser = new JsonParser();
            JsonParser parser = new JsonParser(true, true, true);
            var testcases = new object[][]
            {
                new object[]{ "{\"a\": 1, // this is a comment\n\"b\": 2}", simpleparser.Parse("{\"a\": 1, \"b\": 2}") },
                new object[]{ @"[1,
/* this is a
multiline comment
*/
2]",
                    simpleparser.Parse("[1, 2]")
                },
                new object[]{ "\"2022-06-04\"", new JNode(new DateTime(2022, 6, 4), Dtype.DATE, 0) },
                new object[]{ "\"1956-11-13 11:17:56.123\"", new JNode(new DateTime(1956, 11, 13, 11, 17, 56, 123), Dtype.DATETIME, 0) },
                new object[]{ "\"1956-13-12\"", new JNode("1956-13-12", Dtype.STR, 0) }, // bad date- month too high
                new object[]{ "\"1956-11-13 25:56:17\"", new JNode("1956-11-13 25:56:17", Dtype.STR, 0) }, // bad datetime- hour too high
                new object[]{ "\"1956-11-13 \"", new JNode("1956-11-13 ", Dtype.STR, 0) }, // bad date- has space at end
                new object[]{ "['abc', 2, '1999-01-03']", // single-quoted strings 
                    new JArray(0, new List<JNode>(new JNode[]{new JNode("abc", Dtype.STR, 0),
                                                          new JNode(Convert.ToInt64(2), Dtype.INT, 0),
                                                          new JNode(new DateTime(1999, 1, 3), Dtype.DATE, 0)}))},
                new object[]{ "{'a': \"1\", \"b\": 2}", // single quotes and double quotes in same thing
                    simpleparser.Parse("{\"a\": \"1\", \"b\": 2}") },
                new object[]{ @"{'a':
                  // one comment
                  // wow, another single-line comment?
                  // go figure
                  [2]}",
                simpleparser.Parse("{\"a\": [2]}")},
                new object[]{ "{'a': [ /* internal comment */ 2 ]}", simpleparser.Parse("{\"a\": [2]}") },
                new object[]{ "[1, 2] // trailing comment", simpleparser.Parse("[1, 2]") },
                new object[]{ "// the comments return!\n[2]", simpleparser.Parse("[2]") },
                new object[]{ @"
                  /* multiline comment 
                   */
                  /* followed by another multiline comment */
                 // followed by a single line comment 
                 /* and then a multiline comment */ 
                 [1, 2]
                 /* and one last multiline comment */", simpleparser.Parse("[1, 2]") }
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (object[] test in testcases)
            {
                string inp = (string)test[0];
                JNode desired_out = (JNode)test[1];
                ii++;
                JNode result = new JNode(null, Dtype.NULL, 0);
                string base_message = $"Expected JsonParser(true, true, true, true).Parse({inp})\nto return\n{desired_out.ToString()}\n";
                try
                {
                    result = parser.Parse(inp);
                    try
                    {
                        if (!desired_out.Equals(result))
                        {
                            tests_failed++;
                            Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
                        }
                    }
                    catch
                    {
                        tests_failed++;
                        Console.WriteLine($"{base_message}Instead returned\n{result.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Console.WriteLine($"{base_message}Instead threw exception\n{ex}");
                }
            }

            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }

        public static void TestLinter()
        {
            JsonParser simpleparser = new JsonParser();
            JsonParser parser = new JsonParser(true, true, true, true);
            var testcases = new object[][]
            {
                new object[]{ "[1, 2]", "[1, 2]", new string[]{ } }, // syntactically valid JSON
                new object[]{ "[1 2]", "[1, 2]", new string[]{"No comma between array members" } },
                new object[]{ "[1, , 2]", "[1, 2]", new string[]{$"Two consecutive commas after element 0 of array"} },
                new object[]{ "[1, 2,]", "[1, 2]", new string[]{"Comma after last element of array"} },
                new object[]{ "[1 2,]", "[1, 2]", new string[]{"No comma between array members", "Comma after last element of array"} },
                new object[]{ "{\"a\" 1}", "{\"a\": 1}", new string[]{"No ':' between key 0 and value 0 of object"} },
                new object[]{ "{\"a\": 1 \"b\": 2}", "{\"a\": 1, \"b\": 2}", new string[]{ "No comma after key-value pair 0 in object" } },
                new object[]{ "[1  \"a\n\"]", "[1, \"a\\n\"]", new string[]{"No comma between array members", "String literal starting at position 4 contains newline"} },
                new object[]{ "[NaN, -Infinity, Infinity]", "[NaN, -Infinity, Infinity]",
                    new string[]{ "NaN is not part of the original JSON specification",
                                  "-Infinity is not part of the original JSON specification",
                                  "Infinity is not part of the original JSON specification" } },
                new object[]{ "{'a\n':[1,2,},]", "{\"a\\n\": [1,2]}", new string[]{"Strings must be quoted with \" rather than '",
                                                         "String literal starting at position 1 contains newline",
                                                         "Comma after last element of array",
                                                         "Tried to terminate an array with '}'",
                                                         "Comma after last key-value pair of object",
                                                         "Tried to terminate object with ']'"} },
            };

            int tests_failed = 0;
            int ii = 0;
            foreach (object[] test in testcases)
            {
                string inp = (string)test[0], desired_out = (string)test[1];
                string[] expected_lint = (string[])test[2];
                ii++;
                JNode jdesired = simpleparser.Parse(desired_out);
                JNode result = new JNode(null, Dtype.NULL, 0);
                string expected_lint_str = "[" + string.Join(", ", expected_lint) + "]";
                string base_message = $"Expected JsonParser(true, true, true, true).Parse({inp})\nto return\n{desired_out} and have lint {expected_lint_str}\n";
                try
                {
                    result = parser.Parse(inp);
                    if (parser.lint == null)
                    {
                        tests_failed++;
                        Console.WriteLine(base_message + "Lint was null");
                        continue;
                    }
                    StringBuilder lint_sb = new StringBuilder();
                    lint_sb.Append('[');
                    for (int jj = 0; jj < parser.lint.Count; jj++)
                    {
                        lint_sb.Append(parser.lint[jj].message);
                        if (jj < parser.lint.Count - 1) lint_sb.Append(", ");
                    }
                    lint_sb.Append("]");
                    string lint_str = lint_sb.ToString();
                    try
                    {
                        if (!jdesired.Equals(result) || lint_str != expected_lint_str)
                        {
                            tests_failed++;
                            Console.WriteLine($"{base_message}Instead returned\n{result.ToString()} and had lint {lint_str}");
                        }
                    }
                    catch
                    {
                        tests_failed++;
                        Console.WriteLine($"{base_message}Instead returned\n{result.ToString()} and had lint {lint_str}");
                    }
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Console.WriteLine($"{base_message}Instead threw exception\n{ex}");
                }
            }

            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }

        #endregion
    }
}