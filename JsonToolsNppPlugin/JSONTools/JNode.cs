/*
A class for representing arbitrary JSON.
*/
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic; // for dictionary, list
using System.Globalization;
using System.Runtime.InteropServices;

namespace JSON_Tools.JSON_Tools
{
    /// <summary>
    /// JNode type indicator
    /// </summary>
    public enum Dtype : ushort
    {
        /// <summary>useful only in JSON schema</summary>
        TYPELESS = 0,
        /// <summary>represented as booleans</summary>
        BOOL = 1,
        /// <summary>represented as longs</summary>
        INT = 2,
        /// <summary>represented as doubles</summary>
        FLOAT = 4,
        /// <summary>represented as strings</summary>
        STR = 8,
        NULL = 16,
        /// <summary>JObject Dtype. Represented as Dictionary(string, JNode).</summary>
        OBJ = 32,
        /// <summary>JArray Dtype. Represented as List(JNode).</summary>
        ARR = 64,
        /// <summary>The type of a CurJson node in RemesPathFunctions with unknown type</summary>
        UNKNOWN = 128,
        /// <summary>A regular expression, made by RemesPath</summary>
        REGEX = 256,
        /// <summary>a string representing an array slice</summary>
        SLICE = 512,
        /// <summary>
        /// A YYYY-MM-DD date
        /// </summary>
        DATE = 1024,
        /// <summary>
        /// A YYYY-MM-DD hh:mm:ss.sss datetime
        /// </summary>
        DATETIME = 2048,
        ///// <summary>
        ///// An HH:MM:SS 24-hour time
        ///// </summary>
        //TIME = 4096,
        /* COMPOSITE TYPES */
        FLOAT_OR_INT = FLOAT | INT,
        /// <summary>
        /// a float, int, or bool
        /// </summary>
        NUM = FLOAT | INT | BOOL,
        ITERABLE = UNKNOWN | ARR | OBJ,
        STR_OR_REGEX = STR | REGEX,
        DATE_OR_DATETIME = DATE | DATETIME,
        INT_OR_SLICE = INT | SLICE,
        ARR_OR_OBJ = ARR | OBJ,
        SCALAR = FLOAT | INT | BOOL | STR | NULL | REGEX | DATETIME | DATE, // | TIME
        ANYTHING = SCALAR | ITERABLE,
    }

    /// <summary>
    /// The artistic style used for pretty-printing.<br></br>
    /// Controls things like whether the start bracket of an array/object are on the same line as the parent key.<br></br>
    /// See http://astyle.sourceforge.net/astyle.html#_style=google
    /// </summary>
    public enum PrettyPrintStyle
    {
        /// <summary>
        /// Formats
        /// <code>{"a": {"b": {"c": 2}, "d": [3, [4]]}}</code>
        /// like so:<br></br>
        /// <code>
        ///{
        ///    "a": {
        ///        "b": {
        ///            "c": 2
        ///        },
        ///        "d": [
        ///            3,
        ///            [
        ///                4
        ///            ]
        ///        ]
        ///    }
        ///}
        ///</code>
        /// </summary>
        Google,
        /// <summary>
        /// Formats
        /// <code>{"a": {"b": {"c": 2}, "d": [3, [4]]}}</code>
        /// like so:<br></br>
        /// <code>
        ///{
        ///"a":
        ///    {
        ///    "b":
        ///        {
        ///        "c": 2
        ///        },
        ///    "d":
        ///        [
        ///        3,
        ///            [
        ///            4
        ///            ]
        ///        ]
        ///    }
        ///}
        ///</code>
        /// This is a bit different from the Whitesmith style described on astyle.sourceforge.net,<br></br>
        /// but it's closer to that style than it is to anything else.
        /// </summary>
        Whitesmith,
    }

    /// <summary>
    /// Whether a given string can be accessed using dot syntax
    /// or square brackets and quotes, and what kind of quotes to use
    /// </summary>
    public enum KeyStyle
    {
        /// <summary>
        /// dot syntax for strings starting with _ or letters and containing only _, letters, and digits<br></br>
        /// else singlequotes if it doesn't contain '<br></br>
        /// else double quotes
        /// </summary>
        JavaScript,
        /// <summary>
        /// Singlequotes if it doesn't contain '<br></br>
        /// else double quotes
        /// </summary>
        Python,
        /// <summary>
        /// dot syntax for strings starting with _ or letters and containing only _, letters, and digits<br></br>
        /// else backticks
        /// </summary>
        RemesPath,
    }

    /// <summary>
    /// JSON documents are parsed as JNodes, JObjects, and JArrays. JObjects and JArrays are subclasses of JNode.
    ///    A JSON node, for use in creating a drop-down tree
    ///    Here's an example of how a small JSON document (with newlines as shown, and assuming `\r\n` newline)
    ///    would be parsed as JNodes:
    /// <example>
    ///example.json
    ///<code>
    ///{
    ///"a": [
    ///    1,
    ///    true,
    ///        {"b": 0.5, "c": "a"},
    ///    null
    ///    ]
    ///}
    ///</code>
    ///should be parsed as:
    ///    node1: JObject(type =  Dtype.OBJ, position = 0, children = Dictionary<string, JNode>{"a": node2})
    ///    node2: JArray(type =  Dtype.ARR, position = 8, children = List<JNode>{node3, node4, node5, node8})
    ///    node3: JNode(value = 1, type =  Dtype.INT, position = 15)
    ///    node4: JNode(value = true, type = Dtype.BOOL, position = 23)
    ///    node5: JObject(type = Dtype.OBJ, position = 38,
    ///                   children = Dictionary<string, JNode>{"b": node6, "c": node7})
    ///    node6: JNode(value = 0.5, type = Dtype.FLOAT, position = 44)
    ///    node7: JNode(value = "a", type = Dtype.STR, position = 55)
    ///    node8: JNode(value = null, type = Dtype.NULL, position = 65)
    /// </example>
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("JNode({ToString()})")]
    public class JNode : IComparable
    {
        public static string NL = Environment.NewLine;
        
        public IComparable value; // null for arrays and objects
                                   // IComparable is good here because we want easy comparison of JNodes
        public Dtype type;
        /// <summary>
        /// Start position of the JNode in a UTF-8 encoded source string.<br></br>
        /// Note that this could disagree its position in the C# source string.
        /// </summary>
        public int position;

        public JNode(IComparable value,
                 Dtype type,
                 int position)
        {
            this.position = position;
            this.type = type;
            this.value = value;
        }

        /// <summary>
        /// instantiates a JNode with null value, type Dtype.NULL, and position 0
        /// </summary>
        public JNode()
        {
            this.position = 0;
            this.type = Dtype.NULL;
            this.value = null;
        }

        // in some places like Germany they use ',' as the normal decimal separator.
        // need to override this to ensure that we parse JSON correctly
        public static readonly NumberFormatInfo DOT_DECIMAL_SEP = new NumberFormatInfo
        {
            NumberDecimalSeparator = "."
        };

        private const string HEX_CHARS = "0123456789abcdef";
        /// <summary>
        /// Return the hexadecimal string representation of an integer
        /// </summary>
        /// <param name="x">the int to represent</param>
        /// <param name="out_len">the number of zeros to left-pad the hex number with</param>
        /// <returns></returns> 
        public static string ToHex(int x, int out_len)
        {
            var sb = new char[out_len];
            for (int pow = out_len - 1; pow > -1; pow--)
            {
                x = Math.DivRem(x, 16, out int rem);
                sb[pow] = HEX_CHARS[rem];
            }
            return new string(sb);
        }

        /// <summary>
        /// string representation of any characters in JSON
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static string CharToString(char c)
        {
            switch (c)
            {
                case '\\':   return "\\\\";
                case '"':    return "\\\"";
                case '\x01': return "\\u0001";
                case '\x02': return "\\u0002";
                case '\x03': return "\\u0003";
                case '\x04': return "\\u0004";
                case '\x05': return "\\u0005";
                case '\x06': return "\\u0006";
                case '\x07': return "\\u0007";
                case '\x08': return "\\b";
                case '\x09': return "\\t";
                case '\x0A': return "\\n";
                case '\x0B': return "\\u000B";
                case '\x0C': return "\\f";
                case '\x0D': return "\\r";
                case '\x0E': return "\\u000E";
                case '\x0F': return "\\u000F";
                case '\x10': return "\\u0010";
                case '\x11': return "\\u0011";
                case '\x12': return "\\u0012";
                case '\x13': return "\\u0013";
                case '\x14': return "\\u0014";
                case '\x15': return "\\u0015";
                case '\x16': return "\\u0016";
                case '\x17': return "\\u0017";
                case '\x18': return "\\u0018";
                case '\x19': return "\\u0019";
                case '\x1A': return "\\u001A";
                case '\x1B': return "\\u001B";
                case '\x1C': return "\\u001C";
                case '\x1D': return "\\u001D";
                case '\x1E': return "\\u001E";
                case '\x1F': return "\\u001F";
                default: return new string(c, 1);
            }
        }

        /// <summary>
        /// Compactly prints the JSON.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.<br></br>
        /// key_value_sep (default ": ") is the separator between the key and the value in an object. Use ":" instead if you want minimal whitespace.<br></br>
        /// item_sep (default ", ") is the separator between key-value pairs in an object or items in an array. Use "," instead if you want minimal whitespace.
        /// </summary>
        /// <returns>The compressed form of the JSON.</returns>
        public virtual string ToString(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ")
        {
            switch (type)
            {
                case Dtype.STR:
                {
                    var sb = new StringBuilder();
                    // wrap it in double quotes
                    sb.Append('"');
                    foreach (char c in (string)value)
                        sb.Append(CharToString(c));
                    sb.Append('"');
                    return sb.ToString();
                }
                case Dtype.FLOAT:
                {
                    double v = (double)value;
                    if (double.IsInfinity(v))
                    {
                        return (v < 0) ? "-Infinity" : "Infinity";
                    }
                    if (double.IsNaN(v)) { return "NaN"; }
                    if (v == Math.Round(v) && !(v > Int64.MaxValue || v < Int64.MinValue))
                    {
                        // add ending ".0" to distinguish doubles equal to integers from actual integers
                        return v.ToString(DOT_DECIMAL_SEP) + ".0";
                    }
                    return v.ToString(DOT_DECIMAL_SEP);
                }
                case Dtype.INT: return Convert.ToInt64(value).ToString();
                case Dtype.NULL: return "null";
                case Dtype.BOOL: return (bool)value ? "true" : "false";
                case Dtype.REGEX: return new JNode(((JRegex)this).regex.ToString(), Dtype.STR, 0).ToString();
                case Dtype.DATETIME: return '"' + ((DateTime)value).ToString("yyyy-MM-dd hh:mm:ss") + '"';
                case Dtype.DATE: return '"' + ((DateTime)value).ToString("yyyy-MM-dd") + '"';
                default: return ((object)this).ToString(); // just show the type name for it
            }
        }

        internal virtual int ToStringHelper(bool sort_keys, string key_value_sep, string item_sep, StringBuilder sb, bool change_positions, int extra_utf8_bytes)
        {
            if (change_positions) position = sb.Length + extra_utf8_bytes;
            var str = ToString();
            sb.Append(str);
            if (type == Dtype.STR)
                return extra_utf8_bytes + JsonParser.ExtraUTF8BytesBetween(str, 0, str.Length);
            return extra_utf8_bytes; // only ASCII characters in non-strings
        }

        /// <summary>
        /// Pretty-prints the JSON with each array value and object key-value pair on a separate line,
        /// and indentation proportional to nesting depth.<br></br>
        /// For JNodes that are not JArrays or JObjects, the indent and depth arguments do nothing.<br></br>
        /// The indent argument sets the number of spaces per level of depth.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.<br></br>
        /// The style argument controls various stylistic details of pretty-printing.
        /// See the documentation for the PrettyPrintStyle enum and its members.<br></br>
        /// <b>The depth argument should never be used</b> - it is incremented when PrettyPrint recursively calls itself.
        /// </summary>
        /// <param name="indent">the number of spaces per level of nesting.</param>
        /// <param name="depth">the current depth of nesting.</param>
        /// <returns>a pretty-printed JSON string</returns>
        public virtual string PrettyPrint(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google)
        {
            return ToString();
        }

        /// <summary>
        /// Called by JArray.PrettyPrintAndChangePositions and JObject.PrettyPrintAndChangePositions during recursions.<br></br>
        /// Returns the number of the final line in this node's string representation and this JNode's PrettyPrint() string.<br></br>
        /// The style argument controls various stylistic details of pretty-printing.
        /// See the documentation for the PrettyPrintStyle enum and its members.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.<br></br>
        /// </summary>
        /// <param name="indent"></param>
        /// <param name="depth"></param>
        /// <param name="extra_utf8_bytes"></param>
        /// <returns></returns>
        internal virtual int PrettyPrintHelper(int indent, bool sort_keys, PrettyPrintStyle style, int depth, StringBuilder sb, bool change_positions, int extra_utf8_bytes)
        {
            return ToStringHelper(sort_keys, ": ", ", ", sb, change_positions, extra_utf8_bytes);
        }

        /// <summary>
        /// Compactly prints the JNode - see the documentation for ToString.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.
        /// key_value_sep (default ": ") is the separator between the key and the value in an object. Use ":" instead if you want minimal whitespace.<br></br>
        /// item_sep (default ", ") is the separator between key-value pairs in an object or items in an array. Use "," instead if you want minimal whitespace.
        /// </summary>
        /// <returns></returns>
        public virtual string ToStringAndChangePositions(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ")
        {
            return ToString();
        }

        /// <summary>
        /// Pretty-prints the JNode - see documentation for PrettyPrint.<br></br>
        /// Also changes the line numbers of all the JNodes that are pretty-printed.<br></br>
        /// If sort_keys is true, the keys of objects are printed in ASCIIbetical order.<br></br>
        /// The style argument controls various stylistic details of pretty-printing.
        /// See the documentation for the PrettyPrintStyle enum and its members.<br></br>
        /// EXAMPLE: TODO<br></br>
        /// </summary>
        /// <param name="indent"></param>
        /// <returns></returns>
        public virtual string PrettyPrintAndChangePositions(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google)
        {
            return ToString();
        }

        /// <summary>
        /// Create a preview of this JNode's string representation with length less than or equal to
        /// the "length" argument
        /// </summary>
        /// <param name="length">max length of this preview</param>
        /// <param name="sort_keys">whether to sort keys of objects</param>
        /// <param name="key_value_sep">separation between keys and values of objects</param>
        /// <param name="item_sep">separation between key-value pairs of objects</param>
        /// <returns></returns>
        public virtual string Preview(int length=100, bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ")
        {
            string str = ToString();
            return (str.Length <= length)
                ? str
                : str.Substring(0, length - 3) + "...";
        }

        public virtual void PreviewHelper(int length, bool sort_keys, string key_value_sep, string item_sep, StringBuilder sb)
        {
            string str = ToString();
            sb.Append((str.Length <= length)
                ? str
                : str.Substring(0, length - 3) + "...");
        }

        ///<summary>
        /// A magic method called behind the scenes when sorting things.<br></br>
        /// It only works if other also implements IComparable.<br></br>
        /// Assuming this and other are sorted ascending, CompareTo should do the following:<br></br>
        /// return a negative number if this &#60; other<br></br> 
        /// return 0 if this == other<br></br>
        /// return a positive number if this &#62; other
        /// <!--&#60; and &#62; are greater than and less than symbols-->
        ///</summary>
        ///<exception cref="ArgumentException">
        /// If an attempt is made to compare two things of different type.
        ///</exception>
        public int CompareTo(object other)
        {
            if (other is JNode)
            {
                return CompareTo(((JNode)other).value);
            }
            switch (type)
            {
                // we could simply say value.CompareTo(other) after checking if value is null.
                // It is more user-friendly to attempt to allow comparison of different numeric types, so we do this instead
                case Dtype.STR: return ((string)value).CompareTo(other);
                case Dtype.INT: // Convert.ToInt64 has some weirdness where it rounds half-integers to the nearest even
                                // so it is generally preferable to have ints and floats compared the same way
                                // this way e.g. "3.5 < 4" will be treated the same as "4 > 3.5",
                                // which is the same comparison but with different operand order.
                                // The only downside of this approach is that integers between
                                // 4.5036e15 (2 ^ 52) and 9.2234e18 (2 ^ 63)
                                // can be precisely represented by longs but not by doubles,
                                // so very large integers will have a loss of precision.
                case Dtype.FLOAT:
                    if (!(other is long || other is double || other is bool))
                        throw new ArgumentException("Can't compare numbers to non-numbers");
                    return Convert.ToDouble(value).CompareTo(Convert.ToDouble(other));
                case Dtype.BOOL:
                    if (!(other is long || other is double || other is bool))
                        throw new ArgumentException("Can't compare numbers to non-numbers");
                    if ((bool)value) return (1.0).CompareTo(Convert.ToDouble(other));
                    return (0.0).CompareTo(Convert.ToDouble(other));
                case Dtype.NULL:
                    if (other != null) { throw new ArgumentException("Cannot compare null to non-null"); }
                    return 0;
                case Dtype.DATE: // return ((DateOnly)value).CompareTo((DateOnly)other);
                case Dtype.DATETIME: return ((DateTime)value).CompareTo((DateTime)other);
                default: throw new ArgumentException($"Cannot compare JNodes of type {type}");
            }
        }

        public virtual bool Equals(JNode other)
        {
            return CompareTo(other) == 0;
        }

        public bool GreaterEquals(JNode other)
        {
            return CompareTo(other) >= 0;
        }

        public bool GreaterThan(JNode other)
        {
            return CompareTo(other) > 0;
        }

        public bool LessThan(JNode other)
        {
            return CompareTo(other) < 0;
        }

        public bool LessEquals(JNode other)
        {
            return CompareTo(other) <= 0;
        }

        /// <summary>
        /// return a deep copy of this JNode (same in every respect except memory location)<br></br>
        /// Also recursively copies all the children of a JArray or JObject.
        /// </summary>
        /// <returns></returns>
        public virtual JNode Copy()
        {
            if (value is DateTime dt)
            {
                // DateTimes are mutable, unlike all other valid JNode values. We need to deal with them separately
                return new JNode(new DateTime(dt.Ticks), type, position);
            }
            return new JNode(value, type, position);
        }

        #region HELPER_FUNCS
        private static readonly Regex DOT_COMPATIBLE_REGEX = new Regex("^[_a-zA-Z][_a-zA-Z\\d]*$");
        // "dot compatible" means a string that starts with a letter or underscore
        // and contains only letters, underscores, and digits

        /// <summary>
        /// Get the the path to the JNode that contains position pos in a UTF-8 encoded document.<br></br>
        /// See PathToTreeNode for information on how paths are formatted.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="pos"></param>
        /// <param name="style"></param>
        /// <param name="keys_so_far"></param>
        /// <returns></returns>
        public string PathToPosition(int pos, KeyStyle style = KeyStyle.Python)
        {
            return PathToPositionHelper(pos, style, new List<object>());
        }

        public string PathToPositionHelper(int pos, KeyStyle style, List<object> path)
        {
            string result;
            if (position == pos)
                return FormatPath(path, style);
            if (this is JArray arr)
            {
                int ii = 0;
                while (ii < arr.Length - 1 && arr[ii + 1].position <= pos)
                {
                    ii++;
                }
                JNode child = arr[ii];
                path.Add(ii);
                result = child.PathToPositionHelper(pos, style, path);
                if (result.Length > 0)
                    return result;
                path.RemoveAt(path.Count - 1);
                return "";
            }
            else if (this is JObject obj)
            {
                foreach (string key in obj.children.Keys)
                {
                    JNode val = obj[key];
                    path.Add(key);
                    result = val.PathToPositionHelper(pos, style, path);
                    if (result.Length > 0)
                        return result;
                    path.RemoveAt(path.Count - 1);
                }
                return "";
            }
            string str = ToString();
            int utf8len = (type == Dtype.STR)
                ? Encoding.UTF8.GetByteCount(str)
                : str.Length;
            if (pos > position && pos <= position + utf8len)
                return FormatPath(path, style);
            return "";
        }

        private static string FormatPath(List<object> path, KeyStyle style)
        {
            StringBuilder sb = new StringBuilder();
            foreach (object member in path)
            {
                if (member is int ii)
                {
                    sb.Append($"[{ii}]");
                }
                else if (member is string key)
                {
                    sb.Append(FormatKey(key, style));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get the key in square brackets or prefaced by a quote as determined by the style.<br></br>
        /// Style: one of 'p' (Python), 'j' (JavaScript), or 'r' (RemesPath)<br></br>
        /// EXAMPLES (using the JSON {"a b": [1, {"c": 2}], "d": [4]}<br></br>
        /// Using key "a b'":<br></br>
        /// - JavaScript style: ["a b'"]<br></br>
        /// - Python style: ["a b'"]<br></br>
        /// - RemesPath style: [`a b'`]<br></br>
        /// Using key "c":<br></br>
        /// - JavaScript style: .c<br></br>
        /// - RemesPath style: .c<br></br>
        /// - Python style: ['c']
        /// </summary>
        /// <param name="node"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string FormatKey(string key, KeyStyle style = KeyStyle.Python)
        {
            switch (style)
            {
                case KeyStyle.RemesPath:
                    {
                        if (DOT_COMPATIBLE_REGEX.IsMatch(key))
                            return $".{key}";
                        string key_dubquotes_unescaped = key.Replace("\\", "\\\\").Replace("`", "\\`");
                        return $"[`{key_dubquotes_unescaped}`]";
                    }
                case KeyStyle.JavaScript:
                    {
                        if (DOT_COMPATIBLE_REGEX.IsMatch(key))
                            return $".{key}";
                        if (key.Contains('\''))
                        {
                            return $"[\"{key}\"]";
                        }
                        string key_dubquotes_unescaped = key.Replace("\\\"", "\"");
                        return $"['{key_dubquotes_unescaped}']";
                    }
                case KeyStyle.Python:
                    {
                        if (key.Contains('\''))
                        {
                            return $"[\"{key}\"]";
                        }
                        string key_dubquotes_unescaped = key.Replace("\\\"", "\"");
                        return $"['{key_dubquotes_unescaped}']";
                    }
                default: throw new ArgumentException("style argument for PathToTreeNode must be a KeyStyle member");
            }
        }

        public static Dictionary<Dtype, string> DtypeStrings = new Dictionary<Dtype, string>
        {
            [Dtype.SCALAR] = "scalar",
            [Dtype.ITERABLE] = "iterable",
            [Dtype.NUM] = "numeric", // mixed types come first, so that they're checked before pure
            [Dtype.ARR] = "array",
            [Dtype.BOOL] = "boolean",
            [Dtype.FLOAT] = "float",
            [Dtype.INT] = "integer",
            [Dtype.NULL] = "null",
            [Dtype.OBJ] = "object",
            [Dtype.STR] = "string",
            [Dtype.UNKNOWN] = "unknown",
            [Dtype.SLICE] = "slice",
            [Dtype.DATE] = "date",
            [Dtype.REGEX] = "regex",
            [Dtype.DATETIME] = "datetime",
        };

        /// <summary>
        /// By default, a pure enum value (e.g., Dtype.INT) has INT as its string representation.<br></br>
        /// However, the bitwise OR of multiple enum values just has an integer as its string repr.<br></br>
        /// This function allows, e.g., Dtype.INT | Dtype.BOOL to be represented as "boolean|integer"
        /// rather than 3 (its numeric value)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string FormatDtype(Dtype type)
        {
            List<string> typestrs = new List<string>();
            // it's pure (or a mixture with a previously designated name)
            if (DtypeStrings.TryGetValue(type, out string val))
            {
                return val;
            }
            // it's an undesignated mixture.
            // Cut it apart by making a list of each designated type/mixture it contains. 
            ushort typeint = (ushort)type;
            foreach (Dtype typ in DtypeStrings.Keys)
            {
                ushort shortyp = (ushort)typ;
                if ((typeint & shortyp) == shortyp)
                {
                    typestrs.Add(DtypeStrings[typ]);
                    typeint -= shortyp;
                    // subtract to not double-count types that are in a designated mixture
                }
            }
            return string.Join("|", typestrs);
        }
        #endregion
    }

    /// <inheritdoc/>
    /// <summary>
    /// A class representing JSON objects.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("JObject({ToString()})")]
    public class JObject : JNode
    {
        public Dictionary<string, JNode> children;

        public int Length { get { return children.Count; } }

        public JObject(int position, Dictionary<string, JNode> children) : base(null, Dtype.OBJ, position)
        {
            this.children = children;
        }

        /// <summary>
        /// instantiates a new empty JObject
        /// </summary>
        public JObject() : base(null, Dtype.OBJ, 0)
        {
            children = new Dictionary<string, JNode>();
        }

        /// <summary>
        /// return the JNode associated with key
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public JNode this[string key]
        {
            get { return children[key]; }
            set { children[key] = value; }
        }

        /// <inheritdoc/>
        public override string ToString(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ")
        {
            var sb = new StringBuilder(7 * Length);
            ToStringHelper(sort_keys, key_value_sep, item_sep, sb, false, position);
            return sb.ToString();
        }

        internal override int ToStringHelper(bool sort_keys, string key_value_sep, string item_sep, StringBuilder sb, bool change_positions, int extra_utf8_bytes)
        {
            if (change_positions) position = sb.Length + extra_utf8_bytes;
            sb.Append('{');
            int ctr = 0;
            IEnumerable<string> keys;
            if (sort_keys)
            {
                keys = children.Keys.ToArray();
                Array.Sort((string[])keys, StringComparer.CurrentCultureIgnoreCase);
            }
            else keys = children.Keys;
            foreach (string k in keys)
            {
                JNode v = children[k];
                sb.Append($"\"{k}\"{key_value_sep}");
                extra_utf8_bytes += JsonParser.ExtraUTF8BytesBetween(k, 0, k.Length);
                extra_utf8_bytes = v.ToStringHelper(sort_keys, key_value_sep, item_sep, sb, change_positions, extra_utf8_bytes);
                if (++ctr < children.Count)
                    sb.Append(item_sep);
            }
            sb.Append('}');
            return extra_utf8_bytes;
        }

        /// <inheritdoc/>
        public override string PrettyPrint(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google)
        {
            var sb = new StringBuilder(8 * Length);
            PrettyPrintHelper(indent, sort_keys, style, 0, sb, false, position);
            return sb.ToString();
        }

        /// <inheritdoc/>
        internal override int PrettyPrintHelper(int indent, bool sort_keys, PrettyPrintStyle style, int depth, StringBuilder sb, bool change_positions, int extra_utf8_bytes)
        {
            string dent = new string(' ', indent * depth);
            int ctr = 0;
            IEnumerable<string> keys;
            if (sort_keys)
            {
                keys = children.Keys.ToArray();
                Array.Sort((string[])keys, StringComparer.CurrentCultureIgnoreCase);
            }
            else keys = children.Keys;
            if (style == PrettyPrintStyle.Whitesmith)
                /*
{
"a":
    {
    "b":
        {
        "c": 2
        },
    "d":
        [
        3,
            [
            4
            ]
        ]
    }
}
            */
            {
                sb.Append(dent);
                if (change_positions) position = sb.Length + extra_utf8_bytes;
                sb.Append('{');
                sb.Append(NL);
                foreach (string k in keys)
                {
                    JNode v = children[k];
                    extra_utf8_bytes += JsonParser.ExtraUTF8BytesBetween(k, 0, k.Length);
                    sb.Append($"{dent}\"{k}\":");
                    if (v is JObject || v is JArray)
                        sb.Append(NL);
                    else
                        sb.Append(' ');
                    extra_utf8_bytes = v.PrettyPrintHelper(indent, sort_keys, style, depth + 1, sb, change_positions, extra_utf8_bytes);
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}}}");
            }
            else // if (style == PrettyPrintStyle.GOOGLE)
            /*
{
    "a": {
        "b": {
            "c": 2
        },
        "d": [
            3,
            [
                4
            ]
        ]
    }
}
            */
            {
                if (change_positions) position = sb.Length + extra_utf8_bytes;
                sb.Append('{');
                sb.Append(NL);
                string extra_dent = new string(' ', indent);
                foreach (string k in keys)
                {
                    JNode v = children[k];
                    extra_utf8_bytes += JsonParser.ExtraUTF8BytesBetween(k, 0, k.Length);
                    sb.Append($"{dent}{extra_dent}\"{k}\": ");
                    extra_utf8_bytes = v.PrettyPrintHelper(indent, sort_keys, style, depth + 1, sb, change_positions, extra_utf8_bytes);
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}}}");
            }
            return extra_utf8_bytes;
        }

        /// <inheritdoc/>
        public override string ToStringAndChangePositions(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ")
        {
            var sb = new StringBuilder(7 * Length);
            ToStringHelper(sort_keys, key_value_sep, item_sep, sb, true, position);
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrintAndChangePositions(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google)
        {
            var sb = new StringBuilder(8 * Length);
            PrettyPrintHelper(indent, sort_keys, style, 0, sb, true, position);
            return sb.ToString();
        }

        /////<inheritdoc/>
        //public override string Preview(int length = 100, bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ")
        //{
        //    string str = ToString();
        //    return (str.Length <= length)
        //        ? str
        //        : str.Substring(0, length - 3) + "...";
        //}

        //public virtual void PreviewHelper(int length, bool sort_keys, string key_value_sep, string item_sep, StringBuilder sb)
        //{
        //    string str = ToString();
        //    sb.Append((str.Length <= length)
        //        ? str
        //        : str.Substring(0, length - 3) + "...");
        //}

        /// <summary>
        /// Returns true if and only if other is a JObject with all the same key-value pairs.<br></br>
        /// Throws an ArgumentException if other is not a JObject.
        /// </summary>
        /// <param name="other">Another JObject</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override bool Equals(JNode other)
        {
            if (other.type != Dtype.OBJ)
            {
                throw new ArgumentException($"Cannot compare object {ToString()} to non-object {other.ToString()}");
            }
            var othobj = (JObject)other;
            if (children.Count != othobj.children.Count)
            {
                return false;
            }
            foreach (string key in children.Keys)
            {
                JNode val = children[key];
                bool other_haskey = othobj.children.TryGetValue(key, out JNode valobj);
                if (!other_haskey || !val.Equals(valobj))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override JNode Copy()
        {
            JObject copy = new JObject();
            foreach (string key in children.Keys)
            {
                copy[key] = children[key].Copy();
            }
            return copy;
        }

        /// <summary>
        /// you need to make sure that a string has all characters properly escaped and whatnot
        /// before you can use it as a key in JSON
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public static string FormatAsKey(string k)
        {
            string result = new JNode(k, Dtype.STR, 0).ToString();
            return result.Substring(1, result.Length - 2);
        }
    }

    [System.Diagnostics.DebuggerDisplay("JArray({ToString()})")]
    public class JArray : JNode
    {
        public List<JNode> children;

        public int Length { get { return children.Count; } }

        public JArray(int position, List<JNode> children) : base(null, Dtype.ARR, position)
        {
            this.children = children;
        }

        /// <summary>
        /// instantiates a new empty JArray
        /// </summary>
        public JArray() : base(null, Dtype.ARR, 0)
        {
            children = new List<JNode>();
        }

        /// <summary>
        /// return the JNode associated with index
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public JNode this[int index]
        {
            get { return children[index]; }
            set { children[index] = value; }
        }

        /// <inheritdoc/>
        public override string ToString(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ")
        {
            var sb = new StringBuilder(4 * Length);
            ToStringHelper(sort_keys, key_value_sep, item_sep, sb, false, position);
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrint(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google)
        {
            var sb = new StringBuilder(6 * Length);
            PrettyPrintHelper(indent, sort_keys, style, 0, sb, false, 0);
            return sb.ToString();
        }

        internal override int ToStringHelper(bool sort_keys, string key_value_sep, string item_sep, StringBuilder sb, bool change_positions, int extra_utf8_bytes)
        {
            if (change_positions) position = sb.Length + extra_utf8_bytes;
            sb.Append('[');
            int ctr = 0;
            foreach (JNode v in children)
            {
                v.ToStringHelper(sort_keys, key_value_sep, item_sep, sb, change_positions, extra_utf8_bytes);
                if (++ctr < children.Count)
                    sb.Append(item_sep);
            }
            sb.Append(']');
            return extra_utf8_bytes;
        }

        /// <inheritdoc/>
        public override string ToStringAndChangePositions(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ")
        {
            var sb = new StringBuilder(4 * Length);
            ToStringHelper(sort_keys, key_value_sep, item_sep, sb, true, position);
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrintAndChangePositions(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google)
        {
            var sb = new StringBuilder(6 * Length);
            PrettyPrintHelper(indent, sort_keys, style, 0, sb, true, position);
            return sb.ToString();
        }

        /// <inheritdoc/>
        internal override int PrettyPrintHelper(int indent, bool sort_keys, PrettyPrintStyle style, int depth, StringBuilder sb, bool change_positions, int extra_utf8_bytes)
        {
            string dent = new string(' ', indent * depth);
            if (style == PrettyPrintStyle.Whitesmith)
            {
                sb.Append(dent);
                if (change_positions) position = sb.Length + extra_utf8_bytes;
                sb.Append('[');
                sb.Append(NL);
                int ctr = 0;
                foreach (JNode v in children)
                {
                    if (!(v is JObject || v is JArray))
                        sb.Append(dent);
                    extra_utf8_bytes = v.PrettyPrintHelper(indent, sort_keys, style, depth + 1, sb, change_positions, extra_utf8_bytes);
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}]");
            }
            else
            {
                if (change_positions) position = sb.Length + extra_utf8_bytes;
                sb.Append('[');
                sb.Append(NL);
                string extra_dent = new string(' ', indent);
                int ctr = 0;
                foreach (JNode v in children)
                {
                    // this child's string could be multiple lines, so we need to know what the final line of its string was.
                    sb.Append($"{dent}{extra_dent}");
                    extra_utf8_bytes = v.PrettyPrintHelper(indent, sort_keys, style, depth + 1, sb, change_positions, extra_utf8_bytes);
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}]");
            }
            return extra_utf8_bytes;
        }

        /// <summary>
        /// Returns true if and only if other is a JArray such that 
        /// other[i] == this[i] for all i &#60; this.Length.<br></br>
        /// Throws an ArgumentException if other is not a JArray.
        /// </summary>
        /// <param name="other">Another JArray</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override bool Equals(JNode other)
        {
            if (other.type != Dtype.ARR)
            {
                throw new ArgumentException($"Cannot compare array {ToString()} to non-array {other.ToString()}");
            }
            var otharr = (JArray)other;
            if (children.Count != otharr.children.Count)
            {
                return false;
            }
            for (int ii = 0; ii < children.Count; ii++)
            {
                JNode val = children[ii];
                JNode othval = otharr[ii];
                if (!val.Equals(othval))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override JNode Copy()
        {
            JArray copy = new JArray();
            foreach (JNode child in children)
            {
                copy.children.Add(child.Copy());
            }
            return copy;
        }

        /// <summary>
        /// Return a '\n'-delimited JSON Lines document
        /// where the i^th line has the i^th element in this JArray.
        /// </summary>
        /// <returns></returns>
        public string ToJsonLines(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ")
        {
            StringBuilder sb = new StringBuilder();
            for (int ii = 0; ii < children.Count; ii++)
            {
                sb.Append(children[ii].ToString(sort_keys, key_value_sep, item_sep));
                if (ii < children.Count - 1)
                    sb.Append('\n');
            }
            return sb.ToString();
        }
    }

    [System.Diagnostics.DebuggerDisplay("JRegex({regex})")]
    /// <summary>
    /// A holder for Regex objects (assigned to the regex property).<br></br>
    /// The value is always null and the type is always Dtype.REGEX.
    /// </summary>
    public class JRegex : JNode
    {
        // has to be a separate property because Regex objects do not implement IComparable
        public Regex regex;

        public JRegex(Regex regex) : base(null, Dtype.REGEX, 0)
        {
            this.regex = regex;
        }
    }

    [System.Diagnostics.DebuggerDisplay("JSlicer({slicer})")]
    /// <summary>
    /// A holder for arrays of 1-3 nullable ints. This is a convenience class for parsing of Remespath queries.
    /// </summary>
    public class JSlicer : JNode
    {
        // has to be a separate property because arrays don't implement IComparable
        public int?[] slicer;

        public JSlicer(int?[] slicer) : base(null, Dtype.SLICE, 0)
        {
            this.slicer = slicer;
        }
    }
}
