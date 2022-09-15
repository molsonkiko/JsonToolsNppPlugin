/*
A class for representing arbitrary JSON.
*/
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic; // for dictionary, list

namespace JSON_Tools.JSON_Tools
{
    public struct Str_Line
    {
        public string str;
        public int line;
        
        public Str_Line(string str, int line)
        {
            this.str = str;
            this.line = line;
        }
    }
    /// <summary>
    /// JNode type indicator
    /// </summary>
    public enum Dtype : ushort
    {
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
    }

    /// <summary>
    /// JSON documents are parsed as JNodes, JObjects, and JArrays. JObjects and JArrays are subclasses of JNode.
    ///    A JSON node, for use in creating a drop-down tree
    ///    Here's an example of how a small JSON document (with newlines as shown)
    ///    would be parsed as JNodes:
    /// <example>
    ///example.json
    ///{
    ///"a": [
    ///    1,
    ///    true,
    ///        {"b": 0.5, "c": "a"},
    ///    null
    ///    ]
    ///}
    ///should be parsed as:
    ///    node1: JObject(type =  Dtype.OBJ, line_num = 1, children = Dictionary<string, JNode>{"a": node2})
    ///    node2: JArray(type =  Dtype.ARR, line_num = 2, children = List<JNode>{node3, node4, node5, node8})
    ///    node3: JNode(value = 1, type =  Dtype.INT, line_num = 3)
    ///    node4: JNode(value = true, type = Dtype.BOOL, line_num = 4)
    ///    node5: JObject(type = Dtype.OBJ, line_num = 5,
    ///                   children = Dictionary<string, JNode>{"b": node6, "c": node7})
    ///    node6: JNode(value = 0.5, type = Dtype.FLOAT, line_num = 5)
    ///    node7: JNode(value = "a", type = Dtype.STR, line_num = 5)
    ///    node8: JNode(value = null, type = Dtype.NULL, line_num = 6)
    /// </example>
    /// </summary>
    public class JNode : IComparable
    {
        public IComparable value; // null for arrays and objects
                                   // IComparable is good here because we want easy comparison of JNodes
        public Dtype type;
        public int line_num;

        public JNode(IComparable value,
                 Dtype type,
                 int line_num)
        {
            this.line_num = line_num;
            this.type = type;
            this.value = value;
        }

        /// <summary>
        /// instantiates a JNode with null value, type Dtype.NULL, and line number 0
        /// </summary>
        public JNode()
        {
            this.line_num = 0;
            this.type = Dtype.NULL;
            this.value = null;
        }

        public static Dictionary<char, string> TO_STRING_ESCAPE_MAP = new Dictionary<char, string>
        {
            ['\\'] = "\\\\",
            ['\n'] = "\\n",
            ['\r'] = "\\r",
            ['\b'] = "\\b",
            // ['/'] = "\\/", // the '/' char is often escaped in JSON
            ['\t'] = "\\t",
            ['"'] = "\\\"",
            ['\f'] = "\\f",
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
            int rem;
            for (int pow = out_len - 1; pow > -1; pow--)
            {
                x = Math.DivRem(x, 16, out rem);
                sb[pow] = HEX_CHARS[rem];
            }
            return new string(sb);
        }

        /// <summary>
        /// Compactly prints the JSON. One space between consecutive key-value pairs and array members.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.
        /// </summary>
        /// <returns>The compressed form of the JSON.</returns>
        public virtual string ToString(bool sort_keys = true)
        {
            switch (type)
            {
                case Dtype.STR:
                {
                    var sb = new StringBuilder();
                    // wrap it in double quotes
                    sb.Append('"');
                    foreach (char c in (string)value)
                    {
                        if (c > 0x7f)
                        {
                            // unfortunately things like y with umlaut (char 0xff)
                            // confuse a lot of text editors because they can be
                            // composed in different ways using Unicode.
                            // The safest thing to do is to use \u notation for everything
                            // that's not in standard 7-bit ASCII
                            if (c < 0x10000)
                                sb.Append($"\\u{ToHex(c, 4)}");
                            else
                            {
                                    // make a surrogate pair for chars bigger
                                    // than 0xffff
                                    // see https://github.com/python/cpython/blob/main/Lib/json/decoder.py
                                    int n = c - 0x10000;
                                int s1 = 0xd800 | ((n >> 10) & 0x3ff);
                                int s2 = 0xdc00 | (n & 0x3ff);
                                return $"\\u{ToHex(s1, 4)}\\u{ToHex(s2, 4)}";
                            }
                        }
                        else if (TO_STRING_ESCAPE_MAP.TryGetValue(c, out string escape))
                        {
                            sb.Append(escape);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                    }
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
                    if (v == Math.Round(v))
                    {
                        // add ending ".0" to distinguish doubles equal to integers from actual integers
                        return v.ToString() + ".0";
                    }

                    return v.ToString();
                }
                case Dtype.INT: return Convert.ToInt64(value).ToString();
                case Dtype.NULL: return "null";
                case Dtype.BOOL: return value.ToString().ToLower();
                case Dtype.REGEX: return ((JRegex)this).regex.ToString();
                case Dtype.DATETIME: return '"' + ((DateTime)value).ToString("yyyy-MM-dd hh:mm:ss") + '"';
                case Dtype.DATE: return '"' + ((DateTime)value).ToString("yyyy-MM-dd") + '"';
                default: return ((object)this).ToString(); // just show the type name for it
            }
        }

        /// <summary>
        /// Pretty-prints the JSON with each array value and object key-value pair on a separate line,
        /// and indentation proportional to nesting depth.<br></br>
        /// For JNodes that are not JArrays or JObjects, the indent and depth arguments do nothing.<br></br>
        /// The indent argument sets the number of spaces per level of depth.<br></br>
        /// The depth argument should generally never be used - it is incremented when PrettyPrint recursively calls itself.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.
        /// </summary>
        /// <param name="indent">the number of spaces per level of nesting.</param>
        /// <param name="depth">the current depth of nesting.</param>
        /// <returns>a pretty-printed JSON string</returns>
        public virtual string PrettyPrint(int indent = 4, bool sort_keys = true, int depth = 0)
        {
            return ToString();
        }

        /// <summary>
        /// Compactly prints the JNode - see the documentation for ToString.<br></br>
        /// Also sets the line number of every child node equal to the line number of the root node,<br></br>
        /// because compactly printed JSON has every node on the same line.<br></br>
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.
        /// </summary>
        /// <param name="cur_line_num"></param>
        /// <returns></returns>
        public virtual string ToStringAndChangeLineNumbers(bool sort_keys = true, int? cur_line_num = null)
        {
            if (cur_line_num != null) { line_num = (int)cur_line_num; }
            return ToString();
        }

        /// <summary>
        /// Pretty-prints the JNode - see documentation for PrettyPrint.<br></br>
        /// Also changes the line numbers of all the JNodes that are pretty-printed.<br></br>
        /// The optional depth and cur_line_num arguments are only for recursive self-calling.<br></br>
        /// If sort_keys is true, the keys of objects are printed in ASCIIbetical order.
        /// EXAMPLE<br></br>
        /// PrettyPrintAndChangeLineNumbers(JArray(children = List({JNode(1), JNode(2), JNode(3)})))<br></br>
        /// returns "[\n    1,\n    2,\n    3]"<br></br>
        /// Assuming the root JNode (the JArray) has a line number of 0, the first element's line number becomes 1,
        /// the second element's line number becomes 2, and the third elements line number becomes 2.
        /// </summary>
        /// <param name="indent"></param>
        /// <param name="depth"></param>
        /// <param name="cur_line_number"></param>
        /// <returns></returns>
        public virtual string PrettyPrintAndChangeLineNumbers(int indent = 4, bool sort_keys = true, int depth = 0, int? cur_line_num = null)
        {
            if (cur_line_num != null) line_num = (int)cur_line_num;
            return ToString();
        }

        /// <summary>
        /// Called by JArray.PrettyPrintAndChangeLineNumbers and JObject.PrettyPrintAndChangeLineNumbers during recursions.<br></br>
        /// Returns the number of the final line in this node's string representation and this JNode's PrettyPrint() string.
        /// If sort_keys is true, the keys of objects are printed in alphabetical order.<br></br>
        /// So for example, JArray(List({JNode(1), JNode(2), JNode(3)})).PrettyPrintChangeLinesHelper(4, 0, 0)<br></br>
        /// would change the JArray's line_num to 0, the first element's line_num to 1, the second element's line_num to 2,<br></br>
        /// the third element's line_num to 3, and return 4.<br></br>
        /// </summary>
        /// <param name="indent"></param>
        /// <param name="depth"></param>
        /// <param name="curline"></param>
        /// <returns></returns>
        public virtual Str_Line PrettyPrintChangeLinesHelper(int indent, bool sort_keys, int depth, int curline)
        {
            line_num = curline;
            return new Str_Line(ToString(), curline);
        }

        ///// <summary>
        ///// Method for searching with JMESPath or similar.
        ///// </summary>
        ///// <param name="path">A RemesPath (see RemesPath.cs in this package)</param>
        ///// <returns>The JNode that was found by the search, or null if no match</returns>
        //public virtual JNode Search(string query)
        //{
        //    return this;
        //}

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
                case Dtype.FLOAT: return Convert.ToDouble(value).CompareTo(Convert.ToDouble(other));
                case Dtype.BOOL: return ((bool)value).CompareTo(Convert.ToBoolean(other));
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
                return new JNode(new DateTime(dt.Ticks), type, line_num);
            }
            return new JNode(value, type, line_num);
        }
    }

    /// <inheritdoc/>
    /// <summary>
    /// A class representing JSON objects.
    /// </summary>
    public class JObject : JNode
    {
        public Dictionary<string, JNode> children;

        public int Length { get { return children.Count; } }

        public JObject(int line_num, Dictionary<string, JNode> children) : base(null, Dtype.OBJ, line_num)
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
        public override string ToString(bool sort_keys = true)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            int ctr = 0;
            string[] keys = children.Keys.ToArray();
            if (sort_keys) Array.Sort(keys, (x, y) => x.ToLower().CompareTo(y.ToLower()));
            foreach (string k in keys)
            {
                JNode v = children[k];
                string vstr = v.ToString(sort_keys);
                if (++ctr == children.Count)
                {
                    sb.Append($"\"{k}\": {vstr}");
                }
                else
                {
                    sb.Append($"\"{k}\": {vstr}, ");
                }
            }
            sb.Append('}');
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrint(int indent = 4, bool sort_keys = true, int depth = 0)
        {
            string dent = new string(' ', indent * depth);
            var sb = new StringBuilder();
            sb.Append($"{dent}{{{Environment.NewLine}");
            int ctr = 0;
            string[] keys = children.Keys.ToArray();
            if (sort_keys) Array.Sort(keys, (x, y) => x.ToLower().CompareTo(y.ToLower()));
            foreach (string k in keys)
            {
                JNode v = children[k];
                string vstr = v.PrettyPrint(indent, sort_keys, depth + 1);
                if (v is JObject || v is JArray)
                {
                    sb.Append($"{dent}\"{k}\":{Environment.NewLine}{vstr}");
                }
                else
                {
                    sb.Append($"{dent}\"{k}\": {vstr}");
                }
                sb.Append((++ctr == children.Count) ? Environment.NewLine : "," + Environment.NewLine);
            }
            sb.Append($"{dent}}}");
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string ToStringAndChangeLineNumbers(bool sort_keys = true, int? cur_line_num = null)
        {
            int curline = (cur_line_num == null) ? line_num : (int)cur_line_num;
            line_num = curline;
            var sb = new StringBuilder();
            sb.Append('{');
            int ctr = 0;
            string[] keys = children.Keys.ToArray();
            if (sort_keys) Array.Sort(keys, (x, y) => x.ToLower().CompareTo(y.ToLower()));
            foreach (string k in keys)
            {
                JNode v = children[k];
                string vstr = v.ToStringAndChangeLineNumbers(sort_keys, curline);
                if (++ctr == children.Count)
                {
                    sb.Append($"\"{k}\": {vstr}");
                }
                else
                {
                    sb.Append($"\"{k}\": {vstr}, ");
                }
            }
            sb.Append('}');
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrintAndChangeLineNumbers(int indent = 4, bool sort_keys = true, int depth = 0, int? cur_line_num = null)
        {
            // the cur_line_num is based off of the root node, whichever node originally called
            // PrettyPrintAndChangeLineNumbers. If this is the root node, everything else's line number is based on this one's.
            int curline = (cur_line_num == null) ? line_num : (int)cur_line_num;
            return PrettyPrintChangeLinesHelper(indent, sort_keys, depth, curline).str;
        }

        /// <inheritdoc/>
        public override Str_Line PrettyPrintChangeLinesHelper(int indent, bool sort_keys, int depth, int curline)
        {
            line_num = curline;
            string dent = new string(' ', indent * depth);
            var sb = new StringBuilder();
            sb.Append($"{dent}{{{Environment.NewLine}");
            int ctr = 0;
            string[] keys = children.Keys.ToArray();
            if (sort_keys) Array.Sort(keys, (x, y) => x.ToLower().CompareTo(y.ToLower()));
            foreach (string k in keys)
            {
                JNode v = children[k];
                Str_Line sline;
                if (v is JObject || v is JArray)
                {
                    sline = v.PrettyPrintChangeLinesHelper(indent, sort_keys, depth + 1, curline + 2);
                    sb.Append($"{dent}\"{k}\":{Environment.NewLine}{sline.str}");
                }
                else
                {
                    sline = v.PrettyPrintChangeLinesHelper(indent, sort_keys, depth + 1, curline + 1);
                    sb.Append($"{dent}\"{k}\": {sline.str}");
                }
                curline = sline.line;
                sb.Append((++ctr == children.Count) ? Environment.NewLine : "," + Environment.NewLine);
            }
            sb.Append($"{dent}}}");
            return new Str_Line(sb.ToString(), curline + 1);
        }

        ///// <inheritdoc/>
        //public override JNode Search(string query)
        //{
        //    return new JNode();
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
    }

    public class JArray : JNode
    {
        public List<JNode> children;

        public int Length { get { return children.Count; } }

        public JArray(int line_num, List<JNode> children) : base(null, Dtype.ARR, line_num)
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
        public override string ToString(bool sort_keys = true)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            int ctr = 0;
            foreach (JNode v in children)
            {
                string vstr = v.ToString(sort_keys);
                if (++ctr == children.Count)
                {
                    sb.Append(vstr);
                }
                else
                {
                    sb.Append($"{vstr}, ");
                }
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrint(int indent = 4, bool sort_keys = true, int depth = 0)
        {
            string dent = new string(' ', indent * depth);
            var sb = new StringBuilder();
            sb.Append($"{dent}[" + Environment.NewLine);
            int ctr = 0;
            foreach (JNode v in children)
            {
                string vstr = v.PrettyPrint(indent, sort_keys, depth + 1);
                if (v is JObject || v is JArray)
                {
                    sb.Append(vstr);
                }
                else
                {
                    sb.Append($"{dent}{vstr}");
                }
                sb.Append((++ctr == children.Count) ? Environment.NewLine : "," + Environment.NewLine);
            }
            sb.Append($"{dent}]");
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string ToStringAndChangeLineNumbers(bool sort_keys = true, int? cur_line_num = null)
        {
            int curline = (cur_line_num == null) ? line_num : (int)cur_line_num;
            line_num = curline;
            var sb = new StringBuilder();
            sb.Append('[');
            int ctr = 0;
            foreach (JNode v in children)
            {
                string vstr = v.ToStringAndChangeLineNumbers(sort_keys, curline);
                if (++ctr == children.Count)
                {
                    sb.Append(vstr);
                }
                else
                {
                    sb.Append($"{vstr}, ");
                }
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <inheritdoc/>
        public override string PrettyPrintAndChangeLineNumbers(int indent = 4, bool sort_keys = true, int depth = 0, int? cur_line_num = null)
        {
            // the cur_line_num is based off of the root node, whichever node originally called
            // PrettyPrintAndChangeLineNumbers. If this is the root node, everything else's line number is based on this one's.
            int curline = (cur_line_num == null) ? line_num : (int)cur_line_num;
            return PrettyPrintChangeLinesHelper(indent, sort_keys, depth, curline).str;
        }

        /// <inheritdoc/>
        public override Str_Line PrettyPrintChangeLinesHelper(int indent, bool sort_keys, int depth, int curline)
        {
            line_num = curline;
            string dent = new string(' ', indent * depth);
            var sb = new StringBuilder();
            sb.Append($"{dent}[" + Environment.NewLine);
            int ctr = 0;
            foreach (JNode v in children)
            {
                Str_Line sline = v.PrettyPrintChangeLinesHelper(indent, sort_keys, depth + 1, ++curline);
                curline = sline.line;
                // this child's string could be multiple lines, so we need to know what the final line of its string was.
                if (v is JObject || v is JArray)
                {
                    sb.Append(sline.str);
                }
                else
                {
                    sb.Append($"{dent}{sline.str}");
                }
                curline = sline.line;
                sb.Append((++ctr == children.Count) ? Environment.NewLine : "," + Environment.NewLine);
            }
            sb.Append($"{dent}]");
            return new Str_Line(sb.ToString(), curline + 1);
        }

        ///// <inheritdoc/>
        //public override JNode Search(string query)
        //{
        //    return new JNode();
        //}

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
    }

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
