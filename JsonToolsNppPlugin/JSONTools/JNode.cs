/*
A class for representing arbitrary JSON.
*/
using JSON_Tools.Utils;
using System;
using System.Collections.Generic; // for dictionary, list
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        /// <summary>
        /// NO JNODES ACTUALLY HAVE THE FUNCTION TYPE.<br></br>
        /// It is used in RemesPath to distinguish CurJson from non-CurJson
        /// </summary>
        FUNCTION = 4096,
        /********* COMPOSITE TYPES *********/
        FLOAT_OR_INT = FLOAT | INT,
        INT_OR_BOOL = INT | BOOL,
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
        /// <summary>
        /// <code>
        ///{
        ///    "algorithm": [
        ///        ["start", "each", "child", "on", "a", "new", "line"],
        ///        ["if", "the", "line", "would", "have", "length", "at", "least", 80],
        ///        [
        ///            "follow",
        ///            "this",
        ///            "algorithm",
        ///            ["starting", "from", "the", "beginning"]
        ///        ],
        ///        ["else", "print", "it", "out", "on", 1, "line"]
        ///    ],
        ///    "style": "PPrint",
        ///    "useful": true
        ///}
        ///</code>
        ///</summary>
        PPrint,
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
    /// types of documents that can be parsed in JsonTools
    /// </summary>
    public enum DocumentType
    {
        /// <summary>null value for use in some functions</summary>
        NONE,
        JSON,
        /// <summary>JSON Lines documents</summary>
        JSONL,
        /// <summary>ini files</summary>
        INI,
        /// <summary>regex search results</summary>
        REGEX,
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
        public const int PPRINT_LINE_LENGTH = 79;

        public const string NL = "\r\n";
        
        public IComparable value; // null for arrays and objects
                                   // IComparable is good here because we want easy comparison of JNodes
        public Dtype type;
        /// <summary>
        /// Start position of the JNode in a UTF-8 encoded source string.<br></br>
        /// Note that this could disagree its position in the C# source string.
        /// </summary>
        public int position;

        //public ExtraJNodeProperties? extras;

        public JNode(IComparable value,
                 Dtype type,
                 int position)
        {
            this.position = position;
            this.type = type;
            this.value = value;
            //extras = null;
        }

        /// <summary>
        /// instantiates a JNode with null value, type Dtype.NULL, and position 0
        /// </summary>
        public JNode()
        {
            this.position = 0;
            this.type = Dtype.NULL;
            this.value = null;
            //extras = null;
        }

        public JNode(long value, int position = 0)
        {
            this.position = position;
            this.type = Dtype.INT;
            this.value = value;
        }

        public JNode(string value, int position = 0)
        {
            this.position = position;
            this.type = Dtype.STR;
            this.value = value;
        }

        public JNode(double value, int position = 0)
        {
            this.position = position;
            this.type = Dtype.FLOAT;
            this.value = value;
        }

        public JNode(bool value, int position = 0)
        {
            this.position = position;
            this.type = Dtype.BOOL;
            this.value = value;
        }

        // in some places like Germany they use ',' as the normal decimal separator.
        // need to override this to ensure that we parse JSON correctly
        public static readonly NumberFormatInfo DOT_DECIMAL_SEP = new NumberFormatInfo
        {
            NumberDecimalSeparator = "."
        };
        #region TOSTRING_FUNCS
        /// <summary>
        /// appends the JSON representation of char c to a StringBuilder.<br></br>
        /// for most characters, this just means appending the character itself, but for example '\n' would become "\\n", '\t' would become "\\t",<br></br>
        /// and most other chars less than 32 would be appended as "\\u00{char value in hex}" (e.g., '\x14' becomes "\\u0014")
        /// </summary>
        public static void CharToSb(StringBuilder sb, char c)
        {
            switch (c)
            {
            case '\\':   sb.Append("\\\\"   ); break;
            case '"':    sb.Append("\\\""   ); break;
            case '\x01': sb.Append("\\u0001"); break;
            case '\x02': sb.Append("\\u0002"); break;
            case '\x03': sb.Append("\\u0003"); break;
            case '\x04': sb.Append("\\u0004"); break;
            case '\x05': sb.Append("\\u0005"); break;
            case '\x06': sb.Append("\\u0006"); break;
            case '\x07': sb.Append("\\u0007"); break;
            case '\x08': sb.Append("\\b"    ); break;
            case '\x09': sb.Append("\\t"    ); break;
            case '\x0A': sb.Append("\\n"    ); break;
            case '\x0B': sb.Append("\\v"    ); break;
            case '\x0C': sb.Append("\\f"    ); break;
            case '\x0D': sb.Append("\\r"    ); break;
            case '\x0E': sb.Append("\\u000E"); break;
            case '\x0F': sb.Append("\\u000F"); break;
            case '\x10': sb.Append("\\u0010"); break;
            case '\x11': sb.Append("\\u0011"); break;
            case '\x12': sb.Append("\\u0012"); break;
            case '\x13': sb.Append("\\u0013"); break;
            case '\x14': sb.Append("\\u0014"); break;
            case '\x15': sb.Append("\\u0015"); break;
            case '\x16': sb.Append("\\u0016"); break;
            case '\x17': sb.Append("\\u0017"); break;
            case '\x18': sb.Append("\\u0018"); break;
            case '\x19': sb.Append("\\u0019"); break;
            case '\x1A': sb.Append("\\u001A"); break;
            case '\x1B': sb.Append("\\u001B"); break;
            case '\x1C': sb.Append("\\u001C"); break;
            case '\x1D': sb.Append("\\u001D"); break;
            case '\x1E': sb.Append("\\u001E"); break;
            case '\x1F': sb.Append("\\u001F"); break;
            default:     sb.Append(c);         break;
            }
        }

        /// <summary>
        /// the string representation of a JNode with value s,
        /// with or without the enclosing quotes that a Dtype.STR JNode normally has
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string StrToString(string s, bool quoted)
        {
            var sb = new StringBuilder();
            if (quoted)
                sb.Append('"');
            foreach (char c in s)
                CharToSb(sb, c);
            if (quoted)
                sb.Append('"');
            return sb.ToString();
        }

        /// <summary>
        /// mostly useful for quickly converting a JObject key (which must be escaped during parsing) into the raw string that it represents<br></br>
        /// Choose strAlreadyQuoted = true only if str is already wrapped in double quotes.
        /// </summary>
        public static string UnescapedJsonString(string str, bool strAlreadyQuoted)
        {
            return (string)new JsonParser().ParseString(strAlreadyQuoted ? str : $"\"{str}\"").value;
        }

        /// <summary>
        /// Compactly prints the JSON.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.<br></br>
        /// key_value_sep (default ": ") is the separator between the key and the value in an object. Use ":" instead if you want minimal whitespace.<br></br>
        /// item_sep (default ", ") is the separator between key-value pairs in an object or items in an array. Use "," instead if you want minimal whitespace.
        /// </summary>
        /// <returns>The compressed form of the JSON.</returns>
        public virtual string ToString(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ", int max_length = int.MaxValue)
        {
            switch (type)
            {
                case Dtype.STR:
                {
                    var sb = new StringBuilder();
                    sb.Append('"');
                    foreach (char c in (string)value)
                        CharToSb(sb, c);
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
                    string dubstring = v.ToString(DOT_DECIMAL_SEP);
                    if (v == Math.Round(v) && !(v > long.MaxValue || v < long.MinValue) && dubstring.IndexOf('E') < 0)
                    {
                        // add ending ".0" to distinguish doubles equal to integers from actual integers
                        // unless they use exponential notation, in which case you mess things up
                        // by turning something like 3.123E+15 into 3.123E+15.0 (a non-JSON number representation)
                        return dubstring + ".0";
                    }
                    return dubstring;
                }
                case Dtype.INT: return Convert.ToInt64(value).ToString();
                case Dtype.NULL: return "null";
                case Dtype.BOOL: return (bool)value ? "true" : "false";
                case Dtype.REGEX: return StrToString(((JRegex)this).regex.ToString(), true);
                case Dtype.DATETIME: return '"' + ((DateTime)value).ToString("yyyy-MM-dd hh:mm:ss") + '"';
                case Dtype.DATE: return '"' + ((DateTime)value).ToString("yyyy-MM-dd") + '"';
                default: return ((object)this).ToString(); // just show the type name for it
            }
        }

        internal virtual int ToStringHelper(bool sort_keys, string key_value_sep, string item_sep, StringBuilder sb, bool change_positions, int extra_utf8_bytes, int max_length)
        {
            if (change_positions)
                position = sb.Length + extra_utf8_bytes;
            var str = ToString();
            sb.Append(str);
            if (type == Dtype.STR)
                return extra_utf8_bytes + JsonParser.ExtraUTF8BytesBetween(str, 1, str.Length - 1);
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
        public virtual string PrettyPrint(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google, int max_length = int.MaxValue, char indent_char = ' ')
        {
            return ToString();
        }

        /// <summary>
        /// Called by JArray.PrettyPrintAndChangePositions and JObject.PrettyPrintAndChangePositions during recursions.<br></br>
        /// Returns the number of the final line in this node's string representation and this JNode's PrettyPrint() string.<br></br>
        /// The style argument controls various stylistic details of pretty-printing.
        /// See the documentation for the PrettyPrintStyle enum and its members.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.<br></br>
        /// max_length is the maximum length of the string representation
        /// </summary>
        /// <param name="indent"></param>
        /// <param name="depth"></param>
        /// <param name="extra_utf8_bytes"></param>
        /// <returns></returns>
        internal virtual int PrettyPrintHelper(int indent, bool sort_keys, PrettyPrintStyle style, int depth, StringBuilder sb, bool change_positions, int extra_utf8_bytes, int max_length, char indent_char)
        {
            return ToStringHelper(sort_keys, ": ", ", ", sb, change_positions, extra_utf8_bytes, max_length);
        }

        /// <summary>
        /// Compactly prints the JNode - see the documentation for ToString.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.
        /// key_value_sep (default ": ") is the separator between the key and the value in an object. Use ":" instead if you want minimal whitespace.<br></br>
        /// item_sep (default ", ") is the separator between key-value pairs in an object or items in an array. Use "," instead if you want minimal whitespace.<br></br>
        /// max_length is the maximum length that this string representation can have.
        /// </summary>
        /// <returns></returns>
        public virtual string ToStringAndChangePositions(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ", int max_length = int.MaxValue)
        {
            position = 0;
            return ToString();
        }

        /// <summary>
        /// Pretty-prints the JNode - see documentation for PrettyPrint.<br></br>
        /// Also changes the line numbers of all the JNodes that are pretty-printed.<br></br>
        /// If sort_keys is true, the keys of objects are printed in ASCIIbetical order.<br></br>
        /// The style argument controls various stylistic details of pretty-printing.
        /// See the documentation for the PrettyPrintStyle enum and its members.<br></br>
        /// max_length is the maximum length that this string representation can have.
        /// EXAMPLE: TODO<br></br>
        /// </summary>
        /// <param name="indent"></param>
        /// <returns></returns>
        public virtual string PrettyPrintAndChangePositions(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google, int max_length = int.MaxValue, char indent_char = ' ')
        {
            position = 0;
            return ToString();
        }

        public virtual int PPrintHelper(int indent, int depth, bool sort_keys, StringBuilder sb, bool change_positions, int extra_utf8_bytes, int max_line_end, int max_length, char indent_char)
        {
            return ToStringHelper(sort_keys, ":", ",", sb, change_positions, extra_utf8_bytes, int.MaxValue);
        }

        /// <summary>
        /// All JSON elements follow the same algorithm when compactly printing with comments:<br></br>
        /// All comments come before all the JSON, and the JSON is compactly printed with non-minimal whitespace
        /// (i.e., one space after colons in objects and one space after commas in iterables)
        /// </summary>
        public string ToStringWithComments(List<Comment> comments, bool sort_keys = true)
        {
            comments.Sort((x, y) => x.position.CompareTo(y.position));
            var sb = new StringBuilder();
            Comment.AppendAllCommentsBeforePosition(sb, comments, 0, 0, int.MaxValue, "");
            ToStringHelper(sort_keys, ": ", ", ", sb, false, 0, int.MaxValue);
            return sb.ToString();
        }

        /// <summary>
        /// As ToStringWithComments, but changes each JSON element's position to reflect where it is in the UTF-8 encoded form of the generated string.
        /// </summary>
        public string ToStringWithCommentsAndChangePositions(List<Comment> comments, bool sort_keys = true)
        {
            comments.Sort((x, y) => x.position.CompareTo(y.position));
            var sb = new StringBuilder();
            (_, int extra_utf8_bytes) = Comment.AppendAllCommentsBeforePosition(sb, comments, 0, 0, int.MaxValue, "");
            position = extra_utf8_bytes;
            ToStringHelper(sort_keys, ": ", ", ", sb, true, extra_utf8_bytes, int.MaxValue);
            return sb.ToString();
        }

        /// <summary>
        /// for non-iterables, pretty-printing with comments is the same as compactly printing with comments.<br></br>
        /// For iterables, pretty-printing with comments means Google-style pretty-printing,<br></br>
        /// with each comment having the same position relative to each JSON element and each other comment 
        /// that those comments did when the JSON was parsed.
        /// </summary>
        public string PrettyPrintWithComments(List<Comment> comments, int indent = 4, bool sort_keys = true, char indent_char = ' ')
        {
            return PrettyPrintWithComentsStarter(comments, false, indent, sort_keys, indent_char);
        }

        /// <summary>
        /// As PrettyPrintWithComments, but changes the position of each JNode to wherever it is in the UTF-8 encoded form of the returned string
        /// </summary>
        public string PrettyPrintWithCommentsAndChangePositions(List<Comment> comments, int indent = 4, bool sort_keys = true, char indent_char = ' ')
        {
            return PrettyPrintWithComentsStarter(comments, true, indent, sort_keys, indent_char);
        }

        private string PrettyPrintWithComentsStarter(List<Comment> comments, bool change_positions, int indent, bool sort_keys, char indent_char)
        {
            comments.Sort((x, y) => x.position.CompareTo(y.position));
            var sb = new StringBuilder();
            (int comment_idx, int extra_utf8_bytes) = Comment.AppendAllCommentsBeforePosition(sb, comments, 0, 0, position, "");
            (comment_idx, _) = PrettyPrintWithCommentsHelper(comments, comment_idx, indent, sort_keys, 0, sb, change_positions, extra_utf8_bytes, indent_char);
            sb.Append(NL);
            Comment.AppendAllCommentsBeforePosition(sb, comments, comment_idx, 0, int.MaxValue, ""); // add all comments after the last JNode
            return sb.ToString();
        }

        /// <summary>
        /// goal is to look like this EXAMPLE:
        /// <code>
        /// //comment at the beginning
        /// /* multiline start comment*/
        ///{
        ///    /* every comment has a newline
        ///    after it, even multiliners */
        ///    "a": {
        ///        "b": {
        ///            "c": 2
        ///        },
        ///        "d": [
        ///            // comment indentation is the same as
        ///            // whatever it comes before
        ///            3,
        ///            [
        ///                4
        ///            ]
        ///        ]
        ///    }
        ///}
        /// // comment at the end
        ///</code>
        ///</summary>
        public virtual (int comment_idx, int extra_utf8_bytes) PrettyPrintWithCommentsHelper(List<Comment> comments, int comment_idx, int indent, bool sort_keys, int depth, StringBuilder sb, bool change_positions, int extra_utf8_bytes, char indent_char)
        {
            return (comment_idx, ToStringHelper(sort_keys, ": ", ", ", sb, change_positions, extra_utf8_bytes, int.MaxValue));
        }
        #endregion
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
            if (other is JNode jother)
            {
                return CompareTo(jother.value);
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
                    if (other != null)
                        throw new ArgumentException("Cannot compare null to non-null");
                    return 0;
                case Dtype.DATE: // return ((DateOnly)value).CompareTo((DateOnly)other);
                case Dtype.DATETIME: return ((DateTime)value).CompareTo((DateTime)other);
                case Dtype.ARR:
                case Dtype.OBJ: throw new ArgumentException("Cannot compare JArrays or JObjects");
                default: throw new ArgumentException($"Cannot compare JNodes of type {type}");
            }
        }

        public virtual bool Equals(JNode other)
        {
            return CompareTo(other) == 0;
        }

        /// <summary>
        /// Returns false and sets errorMessage to ex.ToString()
        /// if calling this.Equals(other) throws exception ex.<br></br>
        /// If Equals throws no exception or showErrorMessage is false, errorMessage is null.
        /// </summary>
        public bool TryEquals(JNode other, out string errorMessage, bool showErrorMessage = false)
        {
            errorMessage = null;
            try
            {
                return Equals(other);
            }
            catch (Exception ex)
            {
                if (showErrorMessage)
                    errorMessage = ex.ToString();
                return false;
            }
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

        #region MISC_FUNCS
        /// <summary>
        /// get the parent of this JNode and this JNode's parent
        /// assuming that this JNode is in the tree rooted at root
        /// </summary>
        /// <returns></returns>
        public (object keyInParent, JNode parent) ParentAndKey(JNode root)
        {
            //if (extras is ExtraJNodeProperties ext
            //    && ext.parent != null
            //    && ext.parent.TryGetTarget(out JNode parent))
            //{
            //    return (ext.keyInParent, parent);
            //}
            return ParentHierarchy(root).Last();
        }

        public List<(object keyInParent, JNode parent)> ParentHierarchy(JNode root)
        {
            var parents = new List<JNode>();
            var keys = new List<object>();
            //if (extras is ExtraJNodeProperties ext)
            //    ParentHierarchyHelperWithExtras(keys, parents);
            //else
            ParentHierarchyHelper(root, this, keys, parents);
            return keys.Zip(parents, (k, p) => (k, p)).Reverse().ToList();
        }

        //public void ParentHierarchyHelperWithExtras(List<object> keys, List<JNode> parents)
        //{
        //    if (extras is ExtraJNodeProperties ext
        //        && ext.parent != null
        //        && ext.parent.TryGetTarget(out JNode parent))
        //    {
        //        keys.Add(ext.keyInParent);
        //        parents.Add(parent);
        //        ParentHierarchyHelperWithExtras(keys, parents);
        //    }
        //}

        public bool ParentHierarchyHelper(JNode root, JNode current, List<object> keys, List<JNode> parents)
        {
            if (object.ReferenceEquals(current, this))
                return true;
            if (current is JArray arr)
            {
                for (int ii = 0; ii < arr.children.Count; ii++)
                {
                    JNode child = arr.children[ii];
                    parents.Add(arr);
                    keys.Add(ii);
                    if (ParentHierarchyHelper(root, child, keys, parents))
                        return true;
                    keys.RemoveAt(keys.Count - 1);
                    parents.RemoveAt(parents.Count - 1);
                }
            }
            else if (current is JObject obj)
            {
                foreach (KeyValuePair<string, JNode> kv in obj.children)
                {
                    parents.Add(obj);
                    keys.Add(kv.Key);
                    if (ParentHierarchyHelper(root, kv.Value, keys, parents))
                        return true;
                    keys.RemoveAt(keys.Count - 1);
                    parents.RemoveAt(parents.Count - 1);
                }
            }
            return false;
        }

        private static readonly Regex DOT_COMPATIBLE_REGEX = new Regex("^[_a-zA-Z][_a-zA-Z\\d]*$");
        // "dot compatible" means a string that starts with a letter or underscore
        // and contains only letters, underscores, and digits

        public bool ContainsPosition(int pos)
        {
            if (position == pos)
                return true;
            if ((type & Dtype.ARR_OR_OBJ) != 0)
                return false;
            //if (extras is ExtraJNodeProperties ext)
            //    return pos > position && pos <= ext.endPosition;
            string str = ToString();
            int utf8len = (type == Dtype.STR)
                ? Encoding.UTF8.GetByteCount(str)
                : str.Length;
            return pos > position && pos <= position + utf8len;
        }

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
            if (ContainsPosition(pos))
                return FormatPath(path, style);
            if (this is JArray arr)
            {
                if (arr.Length == 0)
                    return "";
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
            }
            else if (this is JObject obj)
            {
                foreach (KeyValuePair<string, JNode> kv in obj.children)
                {
                    path.Add(kv.Key);
                    result = kv.Value.PathToPositionHelper(pos, style, path);
                    if (result.Length > 0)
                        return result;
                    path.RemoveAt(path.Count - 1);
                }
            }
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

        public static bool BothTypesIntersect(Dtype atype, Dtype btype, Dtype shouldMatch)
        {
            return (atype & shouldMatch) != 0 && (btype & shouldMatch) != 0;
        }

        /// <summary>
        /// does this embody a function that mutates JNode input?
        /// </summary>
        public virtual bool IsMutator => false;

        /// <summary>
        /// does this embody a function that can operate on JNode input?<br></br>
        /// if not, this.Operate MUST throw a NotImplementedException
        /// </summary>
        public virtual bool CanOperate => false;

        /// <summary>
        /// If this is a JNode that operates on input, return the result of this JNode's default operation on inp.<br></br>
        /// If this does not operate on input, throw NotImplementedException<br></br>
        /// Currently the JNode types that <i>can operate on input</i> (and their default operations) are:<br></br>
        /// * CurJson cj (cj.function)<br></br>
        /// * JMutator jm (jm.Mutate)<br></br>
        /// * JQueryContext jq (jq.Evaluate)
        /// </summary>
        /// <returns>the result of operating on input</returns>
        /// <exception cref="NotImplementedException">if this has no function of JNode input</exception>
        public virtual JNode Operate(JNode inp)
        {
            throw new NotImplementedException("This type of JNode cannot operate on input");
        }

        /// <summary>
        /// Changes the type and value of this to the type and value of vnew.<br></br>
        /// This and vnew are still separate objects.<br></br>
        /// Cannot change a non-iterable into an iterable.<br></br>
        /// Cannot change an array into a non-array.<br></br>
        /// Cannot change an object into a non-object.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="vnew"></param>
        /// <exception cref="InvalidMutationException">if this or vnew is a JArray or JObject</exception>
        public void MutateInto(JNode vnew)
        {
            if (type == Dtype.ARR)
            {
                throw new InvalidMutationException("Can't mutate an array.");
            }
            if (type == Dtype.OBJ)
            {
                throw new InvalidMutationException("Can't mutate an object.");
            }
            // v is a scalar
            if ((vnew.type & Dtype.ARR_OR_OBJ) != 0)
                throw new InvalidMutationException("Can't convert a scalar to an array or object.");
            type = vnew.type;
            value = vnew.value;
        }
        #endregion
    }

    /// <inheritdoc/>
    /// <summary>
    /// A class representing JSON objects.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("JObject({ToString(max_length: 200)})")]
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
        public override string ToString(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ", int max_length = int.MaxValue)
        {
            var sb = new StringBuilder(7 * Length);
            ToStringHelper(sort_keys, key_value_sep, item_sep, sb, false, position, max_length);
            if (sb.Length >= max_length)
                sb.Append("...");
            return sb.ToString();
        }

        internal override int ToStringHelper(bool sort_keys, string key_value_sep, string item_sep, StringBuilder sb, bool change_positions, int extra_utf8_bytes, int max_length)
        {
            if (sb.Length >= max_length)
                return -1;
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
                extra_utf8_bytes = v.ToStringHelper(sort_keys, key_value_sep, item_sep, sb, change_positions, extra_utf8_bytes, max_length);
                if (sb.Length >= max_length)
                    return -1;
                if (++ctr < children.Count)
                    sb.Append(item_sep);
            }
            sb.Append('}');
            return extra_utf8_bytes;
        }

        /// <inheritdoc/>
        public override string PrettyPrint(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google, int max_length = int.MaxValue, char indent_char = ' ')
        {
            var sb = new StringBuilder(8 * Length);
            PrettyPrintHelper(indent, sort_keys, style, 0, sb, false, position, max_length, indent_char);
            if (sb.Length >= max_length)
                sb.Append("...");
            return sb.ToString();
        }

        /// <inheritdoc/>
        internal override int PrettyPrintHelper(int indent, bool sort_keys, PrettyPrintStyle style, int depth, StringBuilder sb, bool change_positions, int extra_utf8_bytes, int max_length, char indent_char)
        {
            if (sb.Length >= max_length)
                return -1;
            string dent = new string(indent_char, indent * depth);
            int ctr = 0;
            IEnumerable<string> keys;
            if (sort_keys)
            {
                keys = children.Keys.ToArray();
                Array.Sort((string[])keys, StringComparer.CurrentCultureIgnoreCase);
            }
            else keys = children.Keys;
            switch (style)
            {
            case PrettyPrintStyle.Whitesmith:
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
                    extra_utf8_bytes = v.PrettyPrintHelper(indent, sort_keys, style, depth + 1, sb, change_positions, extra_utf8_bytes, max_length, indent_char);
                    if (sb.Length >= max_length)
                        return -1;
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}}}");
                break;
            case PrettyPrintStyle.Google:
                if (change_positions) position = sb.Length + extra_utf8_bytes;
                sb.Append('{');
                sb.Append(NL);
                string extra_dent = new string(indent_char, (depth + 1) * indent);
                foreach (string k in keys)
                {
                    JNode v = children[k];
                    extra_utf8_bytes += JsonParser.ExtraUTF8BytesBetween(k, 0, k.Length);
                    sb.Append($"{extra_dent}\"{k}\": ");
                    extra_utf8_bytes = v.PrettyPrintHelper(indent, sort_keys, style, depth + 1, sb, change_positions, extra_utf8_bytes, max_length, indent_char);
                    if (sb.Length >= max_length)
                        return -1;
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}}}");
                break;
            case PrettyPrintStyle.PPrint:
                if (change_positions) position = sb.Length + extra_utf8_bytes;
                int child_dent_len = (depth + 1) * indent;
                sb.Append('{');
                sb.Append(NL);
                extra_dent = new string(indent_char, child_dent_len);
                foreach (string k in keys)
                {
                    int max_line_end = sb.Length + PPRINT_LINE_LENGTH;
                    JNode v = children[k];
                    extra_utf8_bytes += JsonParser.ExtraUTF8BytesBetween(k, 0, k.Length);
                    sb.Append($"{extra_dent}\"{k}\": ");
                    extra_utf8_bytes = v.PPrintHelper(indent, depth, sort_keys, sb, change_positions, extra_utf8_bytes, max_line_end, max_length, indent_char);
                    if (sb.Length >= max_length)
                        return -1;
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}}}");
                break;
            default: throw new ArgumentOutOfRangeException("style");
            }
            return extra_utf8_bytes;
        }

        /// <inheritdoc/>
        public override string ToStringAndChangePositions(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ", int max_length = int.MaxValue)
        {
            var sb = new StringBuilder(7 * Length);
            ToStringHelper(sort_keys, key_value_sep, item_sep, sb, true, 0, max_length);
            if (sb.Length >= max_length)
                sb.Append("...");
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrintAndChangePositions(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google, int max_length = int.MaxValue, char indent_char = ' ')
        {
            var sb = new StringBuilder(8 * Length);
            PrettyPrintHelper(indent, sort_keys, style, 0, sb, true, 0, max_length, indent_char);
            if (sb.Length >= max_length)
                sb.Append("...");
            return sb.ToString();
        }

        public override int PPrintHelper(int indent, int depth, bool sort_keys, StringBuilder sb, bool change_positions, int extra_utf8_bytes, int max_line_end, int max_length, char indent_char)
        {
            if (Length > PPRINT_LINE_LENGTH / 8) // an non-minimal-whitespace-compressed object has at least 8 chars per element ("\"a\": 1, ")
                return PrettyPrintHelper(indent, sort_keys, PrettyPrintStyle.PPrint, depth + 1, sb, change_positions, extra_utf8_bytes, max_length, indent_char);
            int og_sb_len = sb.Length;
            int child_utf8_extra = ToStringHelper(sort_keys, ": ", ", ", sb, change_positions, extra_utf8_bytes, max_line_end);
            if (child_utf8_extra == -1)
            {
                // child is too long, so we do PPrint-style printing of it
                sb.Length = og_sb_len;
                return PrettyPrintHelper(indent, sort_keys, PrettyPrintStyle.PPrint, depth + 1, sb, change_positions, extra_utf8_bytes, max_length, indent_char);
            }
            // child is small enough when compact, so use compact repr
            return child_utf8_extra;
        }

        /// <inheritdoc/>
        public override (int comment_idx, int extra_utf8_bytes) PrettyPrintWithCommentsHelper(List<Comment> comments, int comment_idx, int indent, bool sort_keys, int depth, StringBuilder sb, bool change_positions, int extra_utf8_bytes, char indent_char)
        {
            if (change_positions) position = sb.Length + extra_utf8_bytes;
            int ctr = 0;
            IEnumerable<string> keys;
            if (sort_keys)
            {
                keys = children.Keys.ToArray();
                Array.Sort((string[])keys, StringComparer.CurrentCultureIgnoreCase);
            }
            else keys = children.Keys;
            sb.Append('{');
            sb.Append(NL);
            string dent = new string(indent_char, indent * depth);
            string extra_dent = new string(indent_char, (depth + 1) * indent);
            foreach (string k in keys)
            {
                JNode v = children[k];
                (comment_idx, extra_utf8_bytes) = Comment.AppendAllCommentsBeforePosition(sb, comments, comment_idx, extra_utf8_bytes, v.position, extra_dent);
                extra_utf8_bytes += JsonParser.ExtraUTF8BytesBetween(k, 0, k.Length);
                sb.Append($"{extra_dent}\"{k}\": ");
                (comment_idx, extra_utf8_bytes) = v.PrettyPrintWithCommentsHelper(comments, comment_idx, indent, sort_keys, depth + 1, sb, change_positions, extra_utf8_bytes, indent_char);
                if (++ctr < children.Count)
                    sb.Append(',');
                sb.Append(NL);
            }
            sb.Append($"{dent}}}");
            return (comment_idx, extra_utf8_bytes);
        }

        /// <summary>
        /// if this JObject represents an ini file, all the values must be strings. Calling this method ensures that this is so.
        /// </summary>
        /// <returns></returns>
        public void StringifyAllValuesInIniFile()
        {
            var sectionKeysToChange = new List<(string sectionName, string key, string newValue)>();
            foreach (KeyValuePair<string, JNode> kv in children)
            {
                if (!(kv.Value is JObject section))
                {
                    throw new InvalidOperationException("Only objects where all children are objects with only string values can be converted to ini files");
                }
                foreach (KeyValuePair<string, JNode> sectKv in section.children)
                {
                    string key = sectKv.Key;
                    JNode value = sectKv.Value;
                    if (!(value.value is string))
                    {
                        sectionKeysToChange.Add((kv.Key, key, value.ToString()));
                    }
                }
            }
            foreach ((string sectionName, string key, string newValue) in sectionKeysToChange)
            {
                JObject section = (JObject)this[sectionName];
                section[key] = new JNode(newValue);
            }
        }

        /// <summary>
        /// dump this JObject as an ini file
        /// </summary>
        public string ToIniFile(List<Comment> comments)
        {
            var sb = new StringBuilder();
            int positionInComments = 0;
            int utf8ExtraBytes = 0;
            foreach (KeyValuePair<string, JNode> kv in children)
            {
                string header = kv.Key;
                if (!(kv.Value is JObject section))
                {
                    throw new InvalidOperationException("Only objects where all children are objects with only string values can be converted to ini files");
                }
                // section is treated as beginning just before the open squarebrace of the header
                (positionInComments, utf8ExtraBytes) = Comment.AppendAllCommentsBeforePosition(sb, comments, positionInComments, utf8ExtraBytes, section.position, "", DocumentType.INI);
                string unescapedHeader = UnescapedJsonString(header, false);
                sb.Append($"[{unescapedHeader}]\r\n");
                utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(unescapedHeader, 0, unescapedHeader.Length);
                foreach (KeyValuePair<string, JNode> sectKv in section.children)
                {
                    string key = sectKv.Key;
                    JNode value = sectKv.Value;
                    if (!(value.value is string valueStr))
                    {
                        throw new InvalidOperationException("Only objects where all children are objects with only string values can be converted to ini files");
                    }
                    string unescapedKey = UnescapedJsonString(key, false);
                    (positionInComments, utf8ExtraBytes) = Comment.AppendAllCommentsBeforePosition(sb, comments, positionInComments, utf8ExtraBytes, value.position, "", DocumentType.INI);
                    sb.Append($"{unescapedKey}={valueStr}\r\n");
                    utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(unescapedKey, 0, unescapedKey.Length);
                    utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(valueStr, 0, valueStr.Length);
                }
            }
            Comment.AppendAllCommentsBeforePosition(sb, comments, positionInComments, utf8ExtraBytes, int.MaxValue, "", DocumentType.INI);
            return sb.ToString();
        }

        /// <summary>
        /// Returns true if and only if other is a JObject with all the same key-value pairs.<br></br>
        /// Throws an ArgumentException if other is not a JObject.
        /// </summary>
        /// <param name="other">Another JObject</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override bool Equals(JNode other)
        {
            if (!(other is JObject othobj))
            {
                throw new ArgumentException($"Cannot compare object {ToString()} to non-object {other.ToString()}");
            }
            if (children.Count != othobj.children.Count)
            {
                return false;
            }
            foreach (KeyValuePair<string, JNode> kv in children)
            {
                bool other_haskey = othobj.children.TryGetValue(kv.Key, out JNode valobj);
                if (!other_haskey || !kv.Value.Equals(valobj))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override JNode Copy()
        {
            JObject copy = new JObject(position, new Dictionary<string, JNode>(children.Count));
            foreach (KeyValuePair<string, JNode> kv in children)
            {
                copy[kv.Key] = kv.Value.Copy();
            }
            return copy;
        }
    }

    [System.Diagnostics.DebuggerDisplay("JArray({ToString(max_length: 200)})")]
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
        public override string ToString(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ", int max_length = int.MaxValue)
        {
            var sb = new StringBuilder(4 * Length);
            ToStringHelper(sort_keys, key_value_sep, item_sep, sb, false, 0, max_length);
            if (sb.Length >= max_length)
                sb.Append("...");
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrint(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google, int max_length = int.MaxValue, char indent_char = ' ')
        {
            var sb = new StringBuilder(6 * Length);
            PrettyPrintHelper(indent, sort_keys, style, 0, sb, false, 0, max_length, indent_char);
            if (sb.Length >= max_length)
                sb.Append("...");
            return sb.ToString();
        }

        internal override int ToStringHelper(bool sort_keys, string key_value_sep, string item_sep, StringBuilder sb, bool change_positions, int extra_utf8_bytes, int max_length)
        {
            if (sb.Length >= max_length)
                return -1;
            if (change_positions) position = sb.Length + extra_utf8_bytes;
            sb.Append('[');
            int ctr = 0;
            foreach (JNode v in children)
            {
                extra_utf8_bytes = v.ToStringHelper(sort_keys, key_value_sep, item_sep, sb, change_positions, extra_utf8_bytes, max_length);
                if (sb.Length >= max_length)
                    return -1;
                if (++ctr < children.Count)
                    sb.Append(item_sep);
            }
            sb.Append(']');
            return extra_utf8_bytes;
        }

        /// <inheritdoc/>
        public override string ToStringAndChangePositions(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ", int max_length = int.MaxValue)
        {
            var sb = new StringBuilder(4 * Length);
            ToStringHelper(sort_keys, key_value_sep, item_sep, sb, true, 0, max_length);
            if (sb.Length >= max_length)
                sb.Append("...");
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrintAndChangePositions(int indent = 4, bool sort_keys = true, PrettyPrintStyle style = PrettyPrintStyle.Google, int max_length = int.MaxValue, char indent_char = ' ')
        {
            var sb = new StringBuilder(6 * Length);
            PrettyPrintHelper(indent, sort_keys, style, 0, sb, true, 0, max_length, indent_char);
            if (sb.Length >= max_length)
                sb.Append("...");
            return sb.ToString();
        }

        /// <inheritdoc/>
        internal override int PrettyPrintHelper(int indent, bool sort_keys, PrettyPrintStyle style, int depth, StringBuilder sb, bool change_positions, int extra_utf8_bytes, int max_length, char indent_char)
        {
            if (sb.Length >= max_length)
                return -1;
            string dent = new string(indent_char, indent * depth);
            switch (style)
            {
            case PrettyPrintStyle.Whitesmith:
                sb.Append(dent);
                if (change_positions) position = sb.Length + extra_utf8_bytes;
                sb.Append('[');
                sb.Append(NL);
                int ctr = 0;
                foreach (JNode v in children)
                {
                    if (!(v is JObject || v is JArray))
                        sb.Append(dent);
                    extra_utf8_bytes = v.PrettyPrintHelper(indent, sort_keys, style, depth + 1, sb, change_positions, extra_utf8_bytes, max_length, indent_char);
                    if (sb.Length >= max_length)
                        return -1;
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}]");
                break;
            case PrettyPrintStyle.Google:
                if (change_positions) position = sb.Length + extra_utf8_bytes;
                sb.Append('[');
                sb.Append(NL);
                string extra_dent = new string(indent_char, (depth + 1) * indent);
                ctr = 0;
                foreach (JNode v in children)
                {
                    sb.Append(extra_dent);
                    extra_utf8_bytes = v.PrettyPrintHelper(indent, sort_keys, style, depth + 1, sb, change_positions, extra_utf8_bytes, max_length, indent_char);
                    if (sb.Length >= max_length)
                        return -1;
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}]");
                break;
            case PrettyPrintStyle.PPrint:
                if (change_positions) position = sb.Length + extra_utf8_bytes;
                int child_dent_len = (depth + 1) * indent;
                sb.Append('[');
                sb.Append(NL);
                extra_dent = new string(indent_char, child_dent_len);
                ctr = 0;
                foreach (JNode v in children)
                {
                    int max_line_end = sb.Length + PPRINT_LINE_LENGTH;
                    sb.Append(extra_dent);
                    extra_utf8_bytes = v.PPrintHelper(indent, depth, sort_keys, sb, change_positions, extra_utf8_bytes, max_line_end, max_length, indent_char);
                    if (sb.Length >= max_length)
                        return -1;
                    if (++ctr < children.Count)
                        sb.Append(',');
                    sb.Append(NL);
                }
                sb.Append($"{dent}]");
                break;
            default: throw new ArgumentOutOfRangeException("style");
            }
            return extra_utf8_bytes;
        }

        public override int PPrintHelper(int indent, int depth, bool sort_keys, StringBuilder sb, bool change_positions, int extra_utf8_bytes, int max_line_end, int max_length, char indent_char)
        {
            if (Length > PPRINT_LINE_LENGTH / 3) // an non-minimal-whitespace-compressed array has at least 3 chars per element ("1, ")
                return PrettyPrintHelper(indent, sort_keys, PrettyPrintStyle.PPrint, depth + 1, sb, change_positions, extra_utf8_bytes, max_length, indent_char);
            int og_sb_len = sb.Length;
            int child_utf8_extra = ToStringHelper(sort_keys, ": ", ", ", sb, change_positions, extra_utf8_bytes, max_line_end);
            if (child_utf8_extra == -1)
            {
                // child is too long, so we do PPrint-style printing of it
                sb.Length = og_sb_len;
                return PrettyPrintHelper(indent, sort_keys, PrettyPrintStyle.PPrint, depth + 1, sb, change_positions, extra_utf8_bytes, max_length, indent_char);
            }
            // child is small enough when compact, so use compact repr
            return child_utf8_extra;
        }

        public override (int comment_idx, int extra_utf8_bytes) PrettyPrintWithCommentsHelper(List<Comment> comments, int comment_idx, int indent, bool sort_keys, int depth, StringBuilder sb, bool change_positions, int extra_utf8_bytes, char indent_char)
        {
            if (change_positions) position = sb.Length + extra_utf8_bytes;
            sb.Append('[');
            sb.Append(NL);
            string dent = new string(indent_char, indent * depth);
            string extra_dent = new string(indent_char, (depth + 1) * indent);
            int ctr = 0;
            foreach (JNode v in children)
            {
                (comment_idx, extra_utf8_bytes) = Comment.AppendAllCommentsBeforePosition(sb, comments, comment_idx, extra_utf8_bytes, v.position, extra_dent);
                sb.Append(extra_dent);
                (comment_idx, extra_utf8_bytes) = v.PrettyPrintWithCommentsHelper(comments, comment_idx, indent, sort_keys, depth + 1, sb, change_positions, extra_utf8_bytes, indent_char);
                if (++ctr < children.Count)
                    sb.Append(',');
                sb.Append(NL);
            }
            sb.Append($"{dent}]");
            return (comment_idx, extra_utf8_bytes);
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
            if (!(other is JArray otharr))
            {
                throw new ArgumentException($"Cannot compare array {ToString()} to non-array {other.ToString()}");
            }
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
            JArray copy = new JArray(position, new List<JNode>(children.Count));
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

    /// <summary>
    /// A stand-in for an object that is a function of the user's input.
    /// This can take on any value, including a scalar (e.g., len(@) is CurJson of type Dtype.INT).
    /// The Function field must take any object or null as input and return an object of the type reflected
    /// in the CurJson's type field.
    /// So for example, a CurJson node standing in for len(@) would be initialized as follows:
    /// CurJson(Dtype.INT, obj => obj.Length)
    /// </summary>
    public class CurJson : JNode
    {
        public Func<JNode, JNode> function;
        public override bool CanOperate => true;

        public CurJson(Dtype type, Func<JNode, JNode> function) : base(null, type, 0)
        {
            this.function = function;
        }

        /// <summary>
        /// A CurJson node that simply stands in for the current json itself (represented by @)
        /// Its function is the identity function.
        /// </summary>
        public CurJson() : base(null, Dtype.UNKNOWN, 0)
        {
            function = Identity;
        }

        public override string ToString(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ", int max_length = int.MaxValue)
        {
            return $"CurJson(type = {type}, function = {function.Method.Name})";
        }

        public static JNode Identity(JNode obj)
        {
            return obj;
        }

        /// <inheritdoc/>
        public override JNode Operate(JNode inp)
        {
            return function(inp);
        }
    }

    /// <summary>
    /// A JNode that is produced by compilation of an assignment expression,
    /// e.g. "@[@ < 2] = @ + 3"
    /// In this example above, the Mutator produced would have
    /// selector: CurJson(function=(function that selects all direct children with numeric values less than 2)),
    /// mutator: CurJson(function=(function that adds 3 to the value of a numeric JNode))
    /// The type of a Mutator is always UNKNOWN, the value is always null, and the position is always 0.
    /// </summary>
    public class JMutator : JNode
    {
        /// <summary>
        /// One of the following:<br></br>
        /// 1. A constant value that is transformed by mutator<br></br>
        /// 2. a function that selects children to be mutated from the input JSON
        /// </summary>
        public JNode selector;

        /// <summary>
        /// One of the following:<br></br>
        /// 1. A constant value that all children of selector are turned into<br></br>
        /// 2. a function that mutates each child that was selected by selector
        /// </summary>
        public JNode mutator;

        public override bool CanOperate => true;
        public override bool IsMutator => true;

        public JMutator(JNode selector, JNode mutator) : base(null, Dtype.UNKNOWN, 0)
        {
            this.selector = selector;
            this.mutator = mutator;
        }

        public override string ToString(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ", int max_length = int.MaxValue)
        {
            return $"JMutator(selector = {selector.ToString()}, mutator = {mutator.ToString()})";
        }

        /// <summary>
        /// Performs an in-place mutation of inp by selecting children with selector and mutating each selected child with mutator.<br></br>
        /// Returns inp, the JNode that was passed in, after mutation.<br></br>
        /// Assignment expressions are vectorized,<br></br>
        /// so @ = @ * 2 will:<br></br>
        /// * change [1, 2, 3] to [2, 4, 6]<br></br>
        /// * change {"a": 1.5, "b": -4.6} to {"a": 3.0, "b": -9.2}<br></br>
        /// * change 2 to 4
        /// </summary>
        public JNode Mutate(JNode inp)
        {
            JNode selected = selector is CurJson cjsel
                ? cjsel.function(inp) // selector filters input
                : selector;   // selector is a constant JNode indepenedent of input
            if (mutator is CurJson cjmut)
            {
                var func = cjmut.function;
                if (selected is JObject xobj_)
                {
                    foreach (JNode v in xobj_.children.Values)
                    {
                        v.MutateInto(func(v));
                    }
                }
                else if (selected is JArray xarr)
                {
                    foreach (JNode v in xarr.children)
                    {
                        v.MutateInto(func(v));
                    }
                }
                else // x is a scalar
                {
                    selected.MutateInto(func(selected));
                }
                return inp;
            }
            // mutator is an unchanging value, so just perform the same change x or all children of x
            if (selected is JObject xobj)
            {
                foreach (JNode v in xobj.children.Values)
                {
                    v.MutateInto(mutator);
                }
            }
            else if (selected is JArray xarr)
            {
                foreach (JNode v in xarr.children)
                {
                    v.MutateInto(mutator);
                }
            }
            else
            {
                selected.MutateInto(mutator);
            }
            return inp;
        }

        /// <inheritdoc/>
        public override JNode Operate(JNode inp)
        {
            return Mutate(inp);
        }
    }

    /// <summary>
    /// Contains the necessary context to repeatedly execute a multi-statement RemesPath query,<br></br>
    /// possibly including variable assignments or multiple mutations.<br></br>
    /// The type is always UNKNOWN, the position is always 0, and the value is always null.
    /// </summary>
    public class JQueryContext : JNode
    {
        public int indexInStatements { get; private set; } = 0;
        /// <summary>
        /// Will the query mutate input?
        /// </summary>
        private bool mutatesInput = false;
        public override bool CanOperate => true;
        public override bool IsMutator => mutatesInput;
        private List<JNode> statements;
        /// <summary>
        /// variables not dependent on input are stored as JNodes here,<br></br>
        /// but any variables that depend on input as stored as CurJson
        /// </summary>
        private Dictionary<string, JNode> locals;
        /// <summary>
        /// variables <i>not dependent on input</i> are not found in here.<br></br>
        /// variables dependent on input are stored as the result of execution of the CurJson stored under the same name in locals.
        /// </summary>
        private Dictionary<string, JNode> cachedLocals;
        /// <summary>
        /// when a loop variable (declared with "for" instead of "var") is assigned, it is added to the stack.<br></br>
        /// when the loop is ended with "end for;", it is popped off the stack.
        /// </summary>
        private List<VarAssign> loopVariableAssignmentStack;
        private Dictionary<int, string> tokenIndicesOfVariableReferences;

        public JQueryContext() : base(null, Dtype.UNKNOWN, 0)
        {
            statements = new List<JNode>();
            locals = new Dictionary<string, JNode>();
            cachedLocals = new Dictionary<string, JNode>();
            loopVariableAssignmentStack = new List<VarAssign>();
            tokenIndicesOfVariableReferences = new Dictionary<int, string>();
        }

        /// <inheritdoc/>
        public override JNode Operate(JNode inp)
        {
            return Evaluate(inp);
        }

        /// <summary>
        /// a "simple query" is a query with one statement and no variable assignments.<br></br>
        /// This definition is useful because up until JsonTools 5.7 it was the only kind of query possible.<br></br>
        /// If true, you can keep the first statement and discard the context.
        /// </summary>
        public bool IsSimpleQuery
        {
            get
            {
                if (statements.Count > 1)
                    return false;
                JNode firstStatement = statements[0];
                return !(firstStatement is VarAssign);
            }
        }

        public string[] Varnames()
        {
            return locals.Keys.ToArray();
        }

        /// <summary>
        /// add a statement to the end of this.statements and do all the necessary operations after a statement was added, including:<br></br>
        /// 1. incrementing this.indexInStatements<br></br>
        /// 2. updating this.locals<br></br>
        /// 3. if the statement is a loop variable assignment, pushing it onto the loopVariableAssignmentStack<br></br>
        /// 4. if the statement is a loop end, updating the IndexOfLoopEnd for the popped variable assignment<br></br>
        /// 5. if the statement is a JMutator, updating this.mutatesInput to true
        /// </summary>
        /// <param name="statement"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddStatement(JNode statement)
        {
            statements.Add(statement);
            indexInStatements = statements.Count;
            if (statement is VarAssign va)
            {
                if (va.AssignmentType == VariableAssignmentType.LOOP)
                {
                    loopVariableAssignmentStack.Add(va);
                }
                locals[va.Name] = va.GetInitialValueOfVariable(true);
            }
            else if (statement is LoopEnd)
            {
                if (loopVariableAssignmentStack.Count == 0)
                {
                    throw new InvalidOperationException("No loop variable was assigned when a LoopEnd was created");
                }
                var lastAssignedLoopVar = loopVariableAssignmentStack.Pop();
                lastAssignedLoopVar.IndexOfLoopEnd = indexInStatements;
            }
            else if (statement is JMutator)
                mutatesInput = true;
        }

        /// <summary>
        /// gets the value of a variable declared in a previous statement in the query.
        /// </summary>
        /// <param name="varname"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        /// <exception cref="RemesPathNameException">if varname is the name of a variable that is declared in a subsequent statment</exception>
        public bool TryGetValue(int tokenIndex, string varname, out JNode val)
        {
            val = null;
            if (!locals.TryGetValue(varname, out JNode locval))
                return false;
            if (locval is null) // placeholder for undeclared variables
            {
                int indexOfDeclaration = statements.FindIndex(x => x is VarAssign va && va.Name == varname);
                throw new RemesPathNameException(varname, indexInStatements, indexOfDeclaration);
            }
            if (locval is CurJson cj)
            {
                // the variable is a function of input, so use the cached result of evaluating it
                tokenIndicesOfVariableReferences[tokenIndex] = varname;
                Func<JNode, JNode> getCachedValueForVarname = x => cachedLocals[varname];
                val = new CurJson(cj.type, getCachedValueForVarname);
                return true;
            }
            val = locval;
            return true;
        }

        /// <summary>
        /// return true if and only if a variable is referenced at any index ii in tokens such that ii &gt;= startIndex and ii &lt; endIndex
        /// </summary>
        public bool AnyVariableReferencedInRange(int startIndex, int endIndex)
        {
            return tokenIndicesOfVariableReferences.Keys.Any(x => x >= startIndex && x < endIndex);
        }

        /// <summary>
        /// if this is a simple query (one statement that isn't a variable assignment)
        /// return the first statement.<br></br>
        /// otherwise, return this
        /// </summary>
        /// <returns></returns>
        public JNode GetQuery()
        {
            if (IsSimpleQuery)
                return statements[0];
            // clear locals because it was necessary to use actual values while building for propagation
            Reset();
            return this;
        }

        /// <summary>
        /// run all the statements
        /// </summary>
        /// <param name="inp"></param>
        /// <returns></returns>
        public JNode Evaluate(JNode inp)
        {
            JNode lastStatement = EvaluateStatementsFromStartToEnd(inp, 0, statements.Count);
            Reset();
            return lastStatement;
        }

        /// <summary>
        /// called during the EvaluateStatementsFromStartToEnd loop.<br></br>
        /// NOT called when the VarAssign is first created.<br></br>
        /// 1. resets va.value to va.OriginalValue (if needed)<br></br>
        /// 2. adds va.value to locals<br></br>
        /// 3a. if va.value is a function of input:<br></br>
        ///     evaluates va.value on inp and caches the result in cachedLocals<br></br>
        /// 3b. else:<br></br>
        ///     returns va.value
        /// </summary>
        /// <param name="va"></param>
        /// <param name="inp"></param>
        /// <returns></returns>
        private JNode AssignVariable(VarAssign va, JNode inp)
        {
            JNode vaval = va.GetInitialValueOfVariable(false);
            string varname = va.Name;
            locals[varname] = null;
            // important: add varname to locals AFTER evaluating cj.function(inp) and setting locals[varname] to null
            // because this way we correctly deal with issues where a variable is referenced
            // in the expression that first assigns it. (e.g. "var a = a + 3")
            JNode cachedVal = null;
            if (vaval is CurJson cj)
            {
                cachedVal = cj.function(inp);
                cachedLocals[varname] = cachedVal;
            }
            locals[varname] = vaval;
            return cachedVal is null ? vaval : cachedVal;
        }

        /// <summary>
        /// Given a loop variable va that is either a constant array or a function of input that evaluates to an array<br></br>
        /// run through every statement between va's declaration and the end of the loop (an "end for" or the end of the query)<br></br>
        /// once for every value in va's value.<br></br>
        /// Thus if va's value is [1, 2] for some input,<br></br>
        /// and there are three statements between va's declaration and the end of the query,<br></br>
        /// those three statements are evaluated once for va = 1 and once for va = 2.<br></br>
        /// When this function is finished running, this.indexInStatements will equal va.IndexOfLoopEnd or this.statements.Count, whichever is less 
        /// </summary>
        /// <param name="va"></param>
        /// <param name="inp"></param>
        /// <returns></returns>
        /// <exception cref="RemesPathLoopVarNotAnArrayException">if va does not evaluate to an array</exception>
        private JNode EvaluateForLoop(VarAssign va, JNode inp)
        {
            if (!cachedLocals.TryGetValue(va.Name, out JNode toLoopOver))
                throw new RemesPathNameException(va.Name, indexInStatements, va.IndexOfDeclaration);
            if (!(toLoopOver is JArray arr))
            {
                throw new RemesPathLoopVarNotAnArrayException(va, toLoopOver.type);
            }
            int endOfLoop = va.IndexOfLoopEnd > statements.Count ? statements.Count : va.IndexOfLoopEnd;
            int startOfLoop = va.IndexOfDeclaration + 1;
            if (startOfLoop >= endOfLoop)
            {
                // no loop body; just return toLoopOver and don't waste time looping over its values
                indexInStatements = endOfLoop;
                return toLoopOver;
            }
            foreach (JNode child in arr.children)
            {
                cachedLocals[va.Name] = child;
                EvaluateStatementsFromStartToEnd(inp, startOfLoop, endOfLoop);
            }
            return toLoopOver;
        }

        /// <summary>
        /// loop through statements from index start to end.<br></br>
        /// when this function returns, this.indexInStatements will be equal to end.
        /// </summary>
        /// <param name="inp"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private JNode EvaluateStatementsFromStartToEnd(JNode inp, int start, int end)
        {
            JNode lastStatement = null;
            indexInStatements = start;
            while (indexInStatements < end)
            {
                JNode statement = statements[indexInStatements];
                if (statement is VarAssign va)
                {
                    lastStatement = AssignVariable(va, inp);
                    if (va.AssignmentType == VariableAssignmentType.LOOP)
                        EvaluateForLoop(va, inp);
                    else
                        indexInStatements++;
                }
                else if (statement is LoopEnd)
                    indexInStatements++;
                else
                {
                    lastStatement = statement.CanOperate ? statement.Operate(inp) : statement;
                    indexInStatements++;
                }
            }
            return lastStatement;
        }

        /// <summary>
        /// set all locals to null as placeholder and clear cached values of CurJson variables.
        /// </summary>
        private void Reset()
        {
            foreach (string varname in Varnames())
                locals[varname] = null;
            cachedLocals.Clear();
        }
    }

    public enum VariableAssignmentType
    {
        INVALID,
        NORMAL,
        LOOP,
    }

    /// <summary>
    /// represents a variable assignment in RemesPath.<br></br>
    /// The value field is the JNode associated with the Name field by a "(var|for) &lt;Name&gt; = &lt;value&gt;;" expression.<br></br>
    /// The AssignmentType field is true if and only if the variable was declared using "for" instead of "var".<br></br>
    /// The type is always UNKNOWN and the position is always 0.
    /// </summary>
    public class VarAssign : JNode
    {
        public string Name { get; private set; }
        public VariableAssignmentType AssignmentType { get; private set; }
        /// <summary>
        /// the 0-based index of the statement where this variable was declared
        /// </summary>
        public int IndexOfDeclaration { get; private set; }
        /// <summary>
        /// the 0-based index of the statement where this variable's loop ends.<br></br>
        /// default int.MaxValue, so that by default the loop goes until the last statement
        /// </summary>
        public int IndexOfLoopEnd = int.MaxValue;
        /// <summary>
        /// a copy of the value of the variable when it was originally declared.<br></br>
        /// this.value can be mutated, but OriginalValue stays constant.
        /// </summary>
        private JNode OriginalValue;
        /// <summary>
        /// true if:<br></br>
        /// * this is originally declared as a function of input<br></br>
        /// * this is mutated at some point<br></br>
        /// * this is originally declared as a constant, but then redefined as a function of input
        /// </summary>
        public bool IsFunctionOfInput;

        public VarAssign(string name, JNode json, int indexOfDeclaration = 0, VariableAssignmentType assignmentType = VariableAssignmentType.NORMAL, bool isFunctionOfInput = false) : base(json, Dtype.UNKNOWN, 0)
        {
            Name = name;
            if (assignmentType == VariableAssignmentType.INVALID)
                throw new ArgumentException($"assignment type {assignmentType} is not valid", "assignmentType");
            IndexOfDeclaration = indexOfDeclaration;
            AssignmentType = assignmentType;
            OriginalValue = json is CurJson ? json : json.Copy();
            IsFunctionOfInput = isFunctionOfInput;
        }

        /// <summary>
        /// if this.value is CurJson OR NOT this.IsFunctionOfInput, returns this and mutates no state<br></br>
        /// Otherwise, reset this.value to a copy of this.OriginalValue and returns a CurJson that returns the copy when called.
        /// </summary>
        /// <returns></returns>
        public JNode GetInitialValueOfVariable(bool isCompiling)
        {
            bool valueNeedsReset = !(value is CurJson) && IsFunctionOfInput;
            if (valueNeedsReset && !isCompiling) // if it's compiling, this was just initialized so its value doesn't need to be reset
                value = OriginalValue.Copy(); 
            JNode vaval = (JNode)value;
            if (valueNeedsReset)
                return new CurJson(vaval.type, x => vaval);
            return vaval;
        }

        public override string ToString()
        {
            return $"VarAssign(name =\"{Name}\", value={((JNode)value).ToString()}, indexOfDeclaration = {IndexOfDeclaration}, indexOfCompletion = {IndexOfLoopEnd}, assignmentType = {AssignmentType})";
        }
    }

    public class LoopEnd : JNode
    {
        public LoopEnd() : base() { }

        public override string ToString(bool sort_keys = true, string key_value_sep = ": ", string item_sep = ", ", int max_length = int.MaxValue)
        {
            return "LoopEnd()";
        }
    }

    public readonly struct Comment
    {
        public readonly string content;
        public readonly bool isMultiline;
        public readonly int position;

        public Comment(string content, bool isMultiline, int position)
        {
            this.content = content;
            this.isMultiline = isMultiline;
            this.position = position;
        }

        public override string ToString()
        {
            return $"Comment(content=\"{content}\", isMultiline={isMultiline}, position={position})";
        }

        /// <summary>
        /// appends the comment representation (on its own line) of a comment to sb<br></br>
        /// (e.g., "//" + comment.content for single-line comments in JSON, "/*" + comment.content + "*/\r\n" for multiline comments<br></br>
        /// and returns the number of extra utf8 bytes in the comment's content.
        /// </summary>
        public static int ToStringHelper(StringBuilder sb, Comment comment, string singleLineCommentStart)
        {
            sb.Append(comment.isMultiline ? "/*" : singleLineCommentStart);
            sb.Append(comment.content);
            if (comment.isMultiline)
                sb.Append("*/");
            sb.Append(JNode.NL);
            return JsonParser.ExtraUTF8BytesBetween(comment.content, 0, comment.content.Length);
        }

        public static readonly Dictionary<DocumentType, string> singleLineCommentStarts = new Dictionary<DocumentType, string>
        {
            [DocumentType.JSON] = "//",
            [DocumentType.JSONL] = "//",
            [DocumentType.INI] = ";",
        };

        /// <summary>
        /// assumes that comments are sorted by comment.position ascending<br></br>
        /// Keeps appending the JSON comment representations of the comments (as discussed in ToStringHelper above), each on a separate line preceded by dent<br></br>
        /// to sb while comment_idx is less than comments.Count
        /// and comments[comment_idx].position &lt; position
        /// </summary>
        public static (int comment_idx, int extra_utf8_bytes) AppendAllCommentsBeforePosition(StringBuilder sb, List<Comment> comments, int comment_idx, int extra_utf8_bytes, int position, string dent, DocumentType commentType = DocumentType.JSON)
        {
            if (!singleLineCommentStarts.TryGetValue(commentType, out string singleLineCommentStart))
                throw new ArgumentException($"{commentType} is not a valid comment type");
            for (; comment_idx < comments.Count; comment_idx++)
            {
                Comment comment = comments[comment_idx];
                if (comment.position > position)
                    break;
                sb.Append(dent);
                extra_utf8_bytes += ToStringHelper(sb, comment, singleLineCommentStart);
            }
            return (comment_idx, extra_utf8_bytes);
        }
    }

    /// <summary>
    /// Extra properties that can improve the performance of certain methods
    /// and that the parser may choose to add when parsing a document.
    /// Currently not in use
    /// </summary>
    public class ExtraJNodeProperties
    {
        /// <summary>
        /// null if this is the root
        /// </summary>
        public WeakReference<JNode> parent;

        /// <summary>
        /// The end position of this JNode.<br></br>
        /// In most cases this will just be its position
        /// plus the length of its string representation.
        /// </summary>
        public int endPosition;

        /// <summary>
        /// either an int (index in parent array)
        /// or string (key in parent object)
        /// or null (no parent)
        /// </summary>
        public object keyInParent;

        public ExtraJNodeProperties(JNode parent, int endPosition, object keyInParent)
        {
            this.parent = new WeakReference<JNode>(parent);
            this.endPosition = endPosition;
            this.keyInParent = keyInParent;
        }
    }
}
