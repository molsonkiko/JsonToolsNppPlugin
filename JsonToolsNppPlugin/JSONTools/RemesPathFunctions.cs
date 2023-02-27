/*
A library of built-in functions for the RemesPath query language.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JSON_Tools.Utils;

namespace JSON_Tools.JSON_Tools
{
    #region BINOPS
    /// <summary>
    /// Binary operators, e.g., +, -, *, ==
    /// </summary>
    public class Binop
    {
        private Func<JNode, JNode, JNode> Function { get; }
        public float precedence;
        public string name;

        public Binop(Func<JNode, JNode, JNode> function, float precedence, string name)
        {
            Function = function;
            this.precedence = precedence;
            this.name = name;
        }

        public override string ToString() 
        { 
            return $"Binop(\"{this.name}\")";
        }

        public JNode Call(JNode left, JNode right)
        {
            if (left is CurJson && right is CurJson)
            {
                JNode ResolvedBinop(JNode json)
                {
                    return Function(((CurJson)left).function(json), ((CurJson)right).function(json));
                }
                return new CurJson(Dtype.UNKNOWN, ResolvedBinop);
            }
            if (right is CurJson)
            {
                JNode ResolvedBinop(JNode json)
                {
                    return Function(left, ((CurJson)right).function(json));
                }
                return new CurJson(Dtype.UNKNOWN, ResolvedBinop);
            }
            if (left is CurJson)
            {
                JNode ResolvedBinop(JNode json)
                {
                    return Function(((CurJson)left).function(json), right);
                }
                return new CurJson(Dtype.UNKNOWN, ResolvedBinop);
            }
            return Function(left, right);
        }

        public static JNode Add(JNode a, JNode b)
        {
            object aval = a.value; object bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (atype == Dtype.INT && btype == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(aval) + Convert.ToInt64(bval), Dtype.INT, 0);
            }
            if (((atype & Dtype.FLOAT_OR_INT) != 0) && ((btype & Dtype.FLOAT_OR_INT) != 0))
            {
                return new JNode(Convert.ToDouble(aval) + Convert.ToDouble(bval), Dtype.FLOAT, 0);
            }
            if (atype == Dtype.STR && btype == Dtype.STR)
            {
                return new JNode(Convert.ToString(aval) + Convert.ToString(bval), Dtype.STR, 0);
            }
            throw new RemesPathException($"Can't add objects of types {JNode.FormatDtype(atype)} and {JNode.FormatDtype(btype)}");
        }

        public static JNode Sub(JNode a, JNode b)
        {
            object aval = a.value; object bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (atype == Dtype.INT && btype == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(aval) - Convert.ToInt64(bval), Dtype.INT, 0);
            }
            if (((atype & Dtype.FLOAT_OR_INT) == 0) || ((btype & Dtype.FLOAT_OR_INT) == 0))
                throw new RemesPathException($"Can't subtract objects of type {JNode.FormatDtype(atype)} and {JNode.FormatDtype(btype)}");
            return new JNode(Convert.ToDouble(aval) - Convert.ToDouble(bval), Dtype.FLOAT, 0);
        }

        public static JNode Mul(JNode a, JNode b)
        {
            object aval = a.value; object bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (atype == Dtype.INT && btype == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(aval) * Convert.ToInt64(bval), Dtype.INT, 0);
            }
            if (((atype & Dtype.FLOAT_OR_INT) == 0) || ((btype & Dtype.FLOAT_OR_INT) == 0))
                throw new RemesPathException($"Can't multiply objects of type {JNode.FormatDtype(atype)} and {JNode.FormatDtype(btype)}");
            return new JNode(Convert.ToDouble(aval) * Convert.ToDouble(bval), Dtype.FLOAT, 0);
        }

        public static JNode Divide(JNode a, JNode b)
        {
            if (((a.type & Dtype.FLOAT_OR_INT) == 0) || ((b.type & Dtype.FLOAT_OR_INT) == 0))
                throw new RemesPathException($"Can't divide objects of types {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
            return new JNode(Convert.ToDouble(a.value) / Convert.ToDouble(b.value), Dtype.FLOAT, 0);
        }

        public static JNode FloorDivide(JNode a, JNode b)
        {
            if (((a.type & Dtype.FLOAT_OR_INT) == 0) || ((b.type & Dtype.FLOAT_OR_INT) == 0))
                throw new RemesPathException($"Can't floor divide objects of types {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
            return new JNode(Math.Floor(Convert.ToDouble(a.value) / Convert.ToDouble(b.value)), Dtype.INT, 0);
        }

        public static JNode Pow(JNode a, JNode b)
        {
            if (((a.type & Dtype.FLOAT_OR_INT) == 0) || ((b.type & Dtype.FLOAT_OR_INT) == 0))
                throw new RemesPathException($"Can't exponentiate objects of type {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
            return new JNode(Math.Pow(Convert.ToDouble(a.value), Convert.ToDouble(b.value)), Dtype.FLOAT, 0);
        }

        /// <summary>
        /// -a.value**b.value
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static JNode NegPow(JNode a, JNode b)
        {
            if (((a.type & Dtype.FLOAT_OR_INT) == 0) || ((b.type & Dtype.FLOAT_OR_INT) == 0))
                throw new RemesPathException($"Can't exponentiate objects of type {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
            return new JNode(-Math.Pow(Convert.ToDouble(a.value), Convert.ToDouble(b.value)), Dtype.FLOAT, 0);
        }

        public static JNode Mod(JNode a, JNode b)
        {
            object aval = a.value; object bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (atype == Dtype.INT && btype == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(aval) % Convert.ToInt64(bval), Dtype.INT, 0);
            }
            if (((atype & Dtype.FLOAT_OR_INT) == 0) || ((btype & Dtype.FLOAT_OR_INT) == 0))
                throw new RemesPathException($"Can't use modulo operator on objects of type {JNode.FormatDtype(atype)} and {JNode.FormatDtype(btype)}");
            return new JNode(Convert.ToDouble(aval) % Convert.ToDouble(bval), Dtype.FLOAT, 0);
        }

        public static JNode BitWiseOR(JNode a, JNode b)
        {
            if (a.type == Dtype.INT && b.type == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(a.value) | Convert.ToInt64(b.value), Dtype.INT, 0);
            }
            if ((a.type != Dtype.BOOL) || (b.type != Dtype.BOOL))
                throw new RemesPathException($"Can't bitwise OR objects of type {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
            return new JNode(Convert.ToBoolean(a.value) || Convert.ToBoolean(b.value), Dtype.BOOL, 0);
        }

        public static JNode BitWiseXOR(JNode a, JNode b)
        {
            if (a.type == Dtype.INT && b.type == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(a.value) ^ Convert.ToInt64(b.value), Dtype.INT, 0);
            }
            if ((a.type != Dtype.BOOL) || (b.type != Dtype.BOOL))
                throw new RemesPathException($"Can't bitwise XOR objects of type {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
            return new JNode((bool)a.value ^ (bool)b.value, Dtype.BOOL, 0);
        }

        public static JNode BitWiseAND(JNode a, JNode b)
        {
            if (a.type == Dtype.INT && b.type == Dtype.INT)
            {
                return new JNode(Convert.ToInt64(a.value) & Convert.ToInt64(b.value), Dtype.INT, 0);
            }
            if ((a.type != Dtype.BOOL) || (b.type != Dtype.BOOL))
                throw new RemesPathException($"Can't bitwise AND objects of type {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
            return new JNode((bool)a.value && (bool)b.value, Dtype.BOOL, 0);
        }

        /// <summary>
        /// Returns a boolean JNode with value true if node's string value contains the pattern sub.value.<br></br>
        /// E.g. HasPattern(JNode("abc"), JNode("ab+")) -> JNode(true)
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sub"></param>
        /// <returns></returns>
        public static JNode HasPattern(JNode node, JNode sub)
        {
            string s = (string)node.value;
            if (sub.type == Dtype.STR)
            {
                return new JNode(Regex.IsMatch(s, (string)sub.value), Dtype.BOOL, 0);
            }
            return new JNode((((JRegex)sub).regex).IsMatch(s), Dtype.BOOL, 0);
        }

        public static JNode LessThan(JNode a, JNode b)
        {
            return new JNode(a.LessThan(b), Dtype.BOOL, 0);
        }

        public static JNode GreaterThan(JNode a, JNode b)
        {
            return new JNode(a.GreaterThan(b), Dtype.BOOL, 0);
        }

        public static JNode GreaterThanOrEqual(JNode a, JNode b)
        {
            return new JNode(a.GreaterEquals(b), Dtype.BOOL, 0);
        }

        public static JNode LessThanOrEqual(JNode a, JNode b)
        {
            return new JNode(a.LessEquals(b), Dtype.BOOL, 0);
        }

        public static JNode IsEqual(JNode a, JNode b)
        {
            return new JNode(a.Equals(b), Dtype.BOOL, 0);
        }

        public static JNode IsNotEqual(JNode a, JNode b)
        {
            return new JNode(!a.Equals(b), Dtype.BOOL, 0);
        }

        public static Dictionary<string, Binop> BINOPS = new Dictionary<string, Binop>
        {
            ["&"] = new Binop(BitWiseAND, 0, "&"),
            ["|"] = new Binop(BitWiseOR, 0, "|"),
            ["^"] = new Binop(BitWiseXOR, 0, "^"),
            ["=~"] = new Binop(HasPattern, 1, "=~"),
            ["=="] = new Binop(IsEqual, 1, "=="),
            ["!="] = new Binop(IsNotEqual, 1, "!="),
            ["<"] = new Binop(LessThan, 1, "<"),
            [">"] = new Binop(GreaterThan, 1, ">"),
            [">="] = new Binop(GreaterThanOrEqual, 1, ">="),
            ["<="] = new Binop(LessThanOrEqual, 1, "<="),
            ["+"] = new Binop(Add, 2, "+"),
            ["-"] = new Binop(Sub, 2, "-"),
            ["//"] = new Binop(FloorDivide, 3, "//"),
            ["%"] =  new Binop(Mod, 3, "%"),
            ["*"] = new Binop(Mul, 3, "*"),
            ["/"] = new Binop(Divide, 3, "/"),
            // precedence of unary minus (e.g., 2 * -5) is between division's precedence
            // exponentiation's precedence
            ["**"] = new Binop(Pow, 5, "**"),
        };

        public static HashSet<string> BOOLEAN_BINOPS = new HashSet<string> { "==", ">", "<", "=~", "!=", ">=", "<=" };

        public static HashSet<string> BITWISE_BINOPS = new HashSet<string> { "^", "&", "|" };

        public static HashSet<string> FLOAT_RETURNING_BINOPS = new HashSet<string> { "/", "**" };

        public static HashSet<string> POLYMORPHIC_BINOPS = new HashSet<string> { "%", "+", "-", "*" };
    }


    public class BinopWithArgs
    {
        public Binop binop;
        public object left;
        public object right;

        public BinopWithArgs(Binop binop, object left, object right)
        {
            this.binop = binop;
            this.left = left;
            this.right = right;
        }

        public JNode Call()
        {
            if (left is BinopWithArgs)
            {
                left = ((BinopWithArgs)left).Call();
            }
            if (right is BinopWithArgs)
            {
                right = ((BinopWithArgs)right).Call();
            }
            return binop.Call((JNode)left, (JNode)right);
        }
    }
    #endregion

    /// <summary>
    /// functions with arguments in parens, e.g. mean(x), index(arr, elt), sort_by(arr, key, reverse)
    /// </summary>
    public class ArgFunction
    {
        private Func<List<JNode>, JNode> Function { get; }
        private Dtype[] Input_types;
        public string name;
        public Dtype type;
        public int max_args;
        public int min_args;
        public bool is_vectorized;

        /// <summary>
        /// A function whose arguments must be given in parentheses (e.g., len(x), concat(x, y), s_mul(abc, 3).<br></br>
        /// Input_types[ii] is the type that the i^th argument to a function must have,
        /// unless the function has arbitrarily many arguments (indicated by max_args = Int32.MaxValue),
        /// in which case the last value of Input_types is the type that all optional arguments must have.
        /// </summary>
        public ArgFunction(Func<List<JNode>, JNode> function,
            string name,
            Dtype type,
            int min_args,
            int max_args,
            bool is_vectorized,
            Dtype[] input_types)
        {
            Function = function;
            this.name = name;
            this.type = type;
            this.max_args = max_args;
            this.min_args = min_args;
            this.is_vectorized = is_vectorized;
            this.Input_types = input_types;
        }

        public Dtype[] input_types()
        {
            var intypes = new Dtype[Input_types.Length];
            for (int i = 0; i < intypes.Length; i++)
            {
                intypes[i] = Input_types[i];
            }
            return intypes;
        }

        public JNode Call(List<JNode> args)
        {
            return Function(args);
        }
        
        public override string ToString()
        {
            return $"ArgFunction({name}, {type})";
        }

        #region NON_VECTORIZED_ARG_FUNCTIONS

        /// <summary>
        /// First arg is any JNode.<br></br>
        /// Second arg is a JObject or JArray.<br></br>
        /// Returns:<br></br>
        /// If second arg is a JObject, returns true if first arg is a key in second arg.<br></br>
        ///     The first arg must have string value if second arg is JObject.<br></br>
        /// If second arg is a JArray, returns true if first arg is equal to any element in second arg.<br></br>
        /// Examples:<br></br>
        /// In(2, [1, 2, 3]) returns true.<br></br>
        /// In(4, [1, 2, 3]) returns false.<br></br>
        /// In("a", {"a": 1, "b": 2}) returns true.<br></br>
        /// In("c", {"a": 1, "b": 2}) returns false.<br></br>
        /// In(1, {"a": 1, "b": 2}) will throw an error because 1 is not a string.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode In(List<JNode> args)
        {
            JNode elt = args[0];
            JNode itbl = args[1];
            if (itbl is JArray)
            {
                foreach (JNode node in ((JArray)itbl).children)
                {
                    if (node.CompareTo(elt) == 0)
                    {
                        return new JNode(true, Dtype.BOOL, 0);
                    }
                }
                return new JNode(false, Dtype.BOOL, 0);
            }
            if (!(elt.value is string))
            {
                throw new RemesPathException("'in' function first argument must be string if second argument is an object.");
            }
            bool itbl_has_key = ((JObject)itbl).children.ContainsKey((string)elt.value);
            return new JNode(itbl_has_key, Dtype.BOOL, 0);
        }

        /// <summary>
        /// Assuming first arg is a dictionary or list, return the number of elements it contains.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Len(List<JNode> args)
        {
            var itbl = args[0];
            if (itbl is JArray)
            {
                return new JNode(Convert.ToInt64(((JArray)itbl).Length), Dtype.INT, 0);
            }
            return new JNode(Convert.ToInt64(((JObject)itbl).Length), Dtype.INT, 0);
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the sum, cast to a double.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Sum(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            double tot = 0;
            foreach (JNode child in itbl.children)
            {
                if ((child.type & Dtype.NUM) == 0)
                {
                    throw new RemesPathException("Function Sum requires an array of all numbers");
                }
                tot += Convert.ToDouble(child.value);
            }
            return new JNode(tot, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the arithmetic mean of that list.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Mean(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            double tot = 0;
            foreach (JNode child in itbl.children)
            {
                if ((child.type & Dtype.NUM) == 0)
                {
                    throw new RemesPathException("Function Mean requires an array of all numbers");
                }
                tot += Convert.ToDouble(child.value);
            }
            return new JNode(tot / itbl.Length, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// "Flattens" nested lists by adding all the elements of lists at depth 1 to a single
        /// list.<br></br>
        /// Example: Flatten({{1,2},3,{4,{5}}}) = {1,2,3,4,{5}}<br></br> 
        /// (except input is an array with one List<object> and output is a List<object>)<br></br>
        /// In the above example, everything at depth 0 is still at depth 0, and everything else
        /// has its depth reduced by 1.
        /// </summary>
        /// <param name="args">an array containng a single List<object></param>
        /// <returns>List<object> containing the flattened result</returns>
        public static JNode Flatten(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            var iterations = (long?)args[1].value;
            JArray flat;
            if (iterations is null || iterations == 1)
            {
                flat = new JArray();
                foreach (JNode child in itbl.children)
                {
                    if (child is JArray)
                    {
                        foreach (JNode grandchild in ((JArray)child).children)
                        {
                            flat.children.Add(grandchild);
                        }
                    }
                    else
                    {
                        flat.children.Add(child);
                    }
                }
                return flat;
            }
            flat = itbl;
            for (int ii = 0; ii < iterations; ii++)
            {
                flat = (JArray)Flatten(new List<JNode> { flat, new JNode() });
            }
            return flat;
        }

        /// <summary>
        /// first arg should be List&lt;object&gt;, second arg should be object,
        /// optional third arg should be bool.<br></br>
        /// If second arg (elt) is in first arg (itbl), return the index in itbl where
        /// elt first occurs.<br></br>
        /// If a third arg (reverse) is true, then instead return the index
        /// of the final occurence of elt in itbl.<br></br>
        /// If elt does not occur in itbl, throw a KeyNotFoundException.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public static JNode Index(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            var elt = args[1];
            var reverse = args[2].value;
            
            if (reverse != null && (bool)reverse == true)
            {
                for (int ii = itbl.Length - 1; ii >= 0; ii--)
                {
                    if (itbl[ii].CompareTo(elt) == 0) { return new JNode(Convert.ToInt64(ii), Dtype.INT, 0); }
                }
                throw new KeyNotFoundException($"Element {elt} not found in the array {itbl}");
            }
            for (int ii = 0; ii < itbl.Length; ii++)
            {
                if (itbl[ii].CompareTo(elt) == 0) { return new JNode(Convert.ToInt64(ii), Dtype.INT, 0); }
            }
            throw new KeyNotFoundException($"Element {elt} not found in the array {itbl}");
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the largest number in the list,
        /// cast to a double.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Max(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            JNode biggest = new JNode(NanInf.neginf, Dtype.FLOAT, 0);
            foreach (JNode child in itbl.children)
            {
                if ((child.type & Dtype.NUM) == 0)
                {
                    throw new RemesPathException("Function Max requires an array of all numbers");
                }
                if (Convert.ToDouble(child.value) > Convert.ToDouble(biggest.value)) { biggest = child; }
            }
            return biggest;
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the smallest number in the list,
        /// cast to a double.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Min(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            JNode smallest = new JNode(NanInf.inf, Dtype.FLOAT, 0);
            foreach (JNode child in itbl.children)
            {
                if ((child.type & Dtype.NUM) == 0)
                {
                    throw new RemesPathException("Function Min requires an array of all numbers");
                }
                if (Convert.ToDouble(child.value) < Convert.ToDouble(smallest.value)) { smallest = child; }
            }
            return smallest;
        }


        public static JNode Sorted(List<JNode> args)
        {
            var sorted = new JArray();
            sorted.children.AddRange(((JArray)args[0]).children);
            var reverse = args[1].value;
            sorted.children.Sort();
            if (reverse != null && (bool)reverse)
            {
                sorted.children.Reverse();
            }
            return sorted;
        }

        public static JNode SortBy(List<JNode> args)
        {
            var arr = (JArray)args[0];
            var sorted = new JArray();
            var key = args[1].value;
            var reverse = args[2].value;
            if (key is string)
            {
                string kstr = (string)key;
                foreach (JNode elt in arr.children.OrderBy(x => ((JObject)x).children[kstr]))
                {
                    sorted.children.Add(elt);
                }
            }
            else
            {
                int kint = Convert.ToInt32(key);
                foreach (JNode elt in arr.children.OrderBy(x => ((JArray)x).children[kint]))
                {
                    sorted.children.Add(elt);
                }
            }
            if (reverse != null && (bool)reverse)
            {
                sorted.children.Reverse();
            }
            return sorted;
        }

        public static JNode MaxBy(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            var key = args[1].value;
            if (itbl.Length == 0) return new JNode(null, Dtype.NULL, 0);
            if (key is string)
            {
                string keystr = (string)key;
                JObject max = (JObject)itbl[0];
                for (int ii = 1; ii < itbl.children.Count; ii++)
                {
                    JObject x = (JObject)itbl[ii];
                    if (x[keystr].CompareTo(max[keystr]) > 0)
                    {
                        max = x;
                    }
                }
                return max;
            }
            else
            {
                int kint = Convert.ToInt32(key);
                JArray max = (JArray)itbl[0];
                for (int ii = 1; ii < itbl.children.Count; ii++)
                {
                    JArray x = (JArray)itbl[ii];
                    if (x[kint].CompareTo(max[kint]) > 0)
                    {
                        max = x;
                    }
                }
                return max;
            }
        }

        public static JNode MinBy(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            var key = args[1].value;
            if (itbl.Length == 0) return new JNode();
            if (key is string)
            {
                string keystr = (string)key;
                JObject min = (JObject)itbl[0];
                for (int ii = 1; ii < itbl.children.Count; ii++)
                {
                    JObject x = (JObject)itbl[ii];
                    if (x[keystr].CompareTo(min[keystr]) < 0)
                    {
                        min = x;
                    }
                }
                return min;
            }
            else
            {
                int kint = Convert.ToInt32(key);
                JArray min = (JArray)itbl[0];
                for (int ii = 1; ii < itbl.children.Count; ii++)
                {
                    JArray x = (JArray)itbl[ii];
                    if (x[kint].CompareTo(min[kint]) < 0)
                    {
                        min = x;
                    }
                }
                return min;
            }
        }

        /// <summary>
        /// Three args, one required.<br></br>
        /// If only first arg provided: return all integers in [0, args[0])<br></br>
        /// If first and second arg provided: return all integers in [args[0], args[1])<br></br>
        /// If all three args provided: return all integers from args[0] to args[1],
        ///     incrementing by args[2] each time.<br></br>
        /// EXAMPLES:<br></br>
        /// Range(3) -> List&lt;long&gt;({0, 1, 2})<br></br>
        /// Range(3, 7) -> List&lt;long&gt;({3, 4, 5, 6})<br></br>
        /// Range(10, 4, -2) -> List&lt;long&gt;({10, 8, 6})
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Range(List<JNode> args)
        {
            var start = (long?)args[0].value;
            var end = (long?)args[1].value;
            var step = (long?)args[2].value;
            var nums = new JArray();
            if (start == null)
            {
                throw new RemesPathException("First argument for range function cannot be null.");
            }
            if (step == 0)
            {
                throw new RemesPathException("Can't have a step size of 0 for the range function");
            }
            if (end == null && start > 0)
            {
                for (long ii = 0; ii < start; ii++)
                {
                    nums.children.Add(new JNode(ii, Dtype.INT, 0));
                }
            }
            else if (step == null && start < end)
            {
                for (long ii = start.Value; ii < end; ii++)
                {
                    nums.children.Add(new JNode(ii, Dtype.INT, 0));
                }
            }
            else
            {
                if (start > end && step < 0)
                {
                    for (long ii = start.Value; ii > end; ii += step.Value)
                    {
                        nums.children.Add(new JNode(ii, Dtype.INT, 0));
                    }
                }
                else if (start < end && step > 0)
                {
                    for (long ii = start.Value; ii < end; ii += step.Value)
                    {
                        nums.children.Add(new JNode(ii, Dtype.INT, 0));
                    }
                }
            }
            return nums;
        }

        public static JNode Values(List<JNode> args)
        {
            var vals = new JArray();
            vals.children.AddRange(((JObject)args[0]).children.Values);
            return vals;
        }

        public static JNode Keys(List<JNode> args)
        {
            var ks = new JArray();
            foreach (string s in ((JObject)args[0]).children.Keys)
            {
                ks.children.Add(new JNode(s, Dtype.STR, 0));
            }
            return ks;
        }

        /// <summary>
        /// Takes a single JObject as argument.
        /// Returns an array of 2-arrays, 
        /// where the first element of each subarray is the key
        /// and the second element is the value.
        /// Example:
        /// Items({"a": 1, "b": 2, "c": 3}) = [["a", 1], ["b", 2], ["c", 3]]
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Items(List<JNode> args)
        {
            var its = new List<JNode>();
            JObject obj = (JObject)args[0];
            foreach (string k in obj.children.Keys)
            {
                JNode v = obj[k];
                JNode knode = new JNode(k, Dtype.STR, 0);
                var subarr = new List<JNode>();
                subarr.Add(knode);
                subarr.Add(v);
                its.Add(new JArray(0, subarr));
            }
            return new JArray(0, its);
        }

        public static JNode Unique(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            var is_sorted = args[1].value;
            var uniq = new HashSet<object>();
            foreach (JNode val in itbl.children)
            {
                uniq.Add(val.value);
            }
            var uniq_list = new List<JNode>();
            foreach (object val in uniq)
            {
                uniq_list.Add(ObjectsToJNode(val));
            }
            if (is_sorted != null && (bool)is_sorted)
            {
                uniq_list.Sort();
            }
            return new JArray(0, uniq_list);
        }

        /// <summary>
        /// The first arg (itbl) must be a list containing only numbers.<br></br>
        /// The second arg (qtile) to be a double in [0,1].<br></br>
        /// Returns the qtile^th quantile of itbl, as a double.<br></br>
        /// So Quantile(x, 0.5) returns the median, Quantile(x, 0.75) returns the 75th percentile, and so on.<br></br>
        /// Uses linear interpolation if the index found is not an integer.<br></br>
        /// For example, suppose that the 60th percentile is at index 6.6, and elements 6 and 7 are 8 and 10.<br></br>
        /// Then the returned value is 0.6*10 + 0.4*8 = 9.2
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Quantile(List<JNode> args)
        {
            var sorted = new List<double>();
            foreach (JNode node in ((JArray)args[0]).children)
            {
                if ((node.type & Dtype.NUM) == 0)
                {
                    throw new RemesPathException("Function Quantile requires an array of all numbers");
                }
                sorted.Add(Convert.ToDouble(node.value));
            }
            double quantile = Convert.ToDouble(args[1].value);
            sorted.Sort();
            if (sorted.Count == 0)
            {
                throw new RemesPathException("Cannot find quantiles of an empty array");
            }
            if (sorted.Count == 1)
            {
                return new JNode(sorted[0], Dtype.FLOAT, 0);
            }
            double ind = quantile * (sorted.Count - 1);
            int lower_ind = Convert.ToInt32(Math.Floor(ind));
            double weighted_avg;
            double lower_val = sorted[lower_ind];
            if (ind != lower_ind)
            {
                double upper_val = sorted[lower_ind + 1];
                double frac_upper = ind - lower_ind;
                weighted_avg = upper_val * frac_upper + lower_val * (1 - frac_upper);
            }
            else
            {
                weighted_avg = lower_val;
            }
            return new JNode(weighted_avg, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// args[0] should be a list of objects.<br></br>
        /// Finds an array of sub-arrays, where each sub-array is an element-count pair, where the count is of that element in args[0].<br></br>
        /// EXAMPLES:
        /// ValueCounts(JArray({1, "a", 2, "a", 1})) ->
        /// JArray(JArray({"a", 2}), JArray({1, 2}), JArray({2, 1}))
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode ValueCounts(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            var uniqs = new Dictionary<object, long>();
            foreach (JNode elt in itbl.children)
            {
                object val = elt.value;
                if (val == null)
                {
                    throw new RemesPathException("Can't count occurrences of objects with null values");
                }
                if (!uniqs.ContainsKey(val))
                    uniqs[val] = 0;
                uniqs[val]++;
            }
            var uniq_arr = new JArray();
            foreach (object elt in uniqs.Keys)
            {
                long ct = uniqs[elt];
                JArray elt_ct = new JArray();
                elt_ct.children.Add(ObjectsToJNode(elt));
                elt_ct.children.Add(new JNode(ct, Dtype.INT, 0));
                uniq_arr.children.Add(elt_ct);
            }
            return uniq_arr;
        }

        public static JNode StringJoin(List<JNode> args)
        {
            string sep = (string)args[0].value;
            var itbl = (JArray)args[1];
            var sb = new StringBuilder();
            sb.Append((string)itbl[0].value);
            for (int ii = 1; ii < itbl.Length; ii++)
            {
                sb.Append(sep);
                sb.Append((string)itbl[ii].value);
            }
            return new JNode(sb.ToString(), Dtype.STR, 0);
        }

        /// <summary>
        /// First arg (itbl) must be a JArray containing only other JArrays or only other JObjects.<br></br>
        /// Second arg (key) must be a JNode with a long value or a JNode with a string value.<br></br>
        /// Returns a new JObject where each entry in itbl is grouped into a separate JArray under the stringified form
        /// of the value associated with key/index key in itbl.<br></br>
        /// EXAMPLE<br></br>
        /// GroupBy([{"foo": 1, "bar": "a"}, {"foo": 2, "bar": "b"}, {"foo": 3, "bar": "a"}], "bar") returns:<br></br>
        /// {"a": [{"foo": 1, "bar": "a"}, {"foo": 3, "bar": "a"}], "b": [{"foo": 2, "bar": "b"}]}
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static JNode GroupBy(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            object key = args[1].value;
            if (!(key is string || key is long))
            {
                throw new ArgumentException("The GroupBy function can only group by string keys or int indices");
            }
            var gb = new Dictionary<string, JNode>();
            string vstr;
            if (key is long)
            {
                int ikey = Convert.ToInt32(key);
                foreach (JNode node in itbl.children)
                {
                    JArray subobj = (JArray)node;
                    JNode val = subobj[ikey];
                    vstr = val.type == Dtype.STR ? (string)val.value : val.ToString();
                    if (gb.ContainsKey(vstr))
                    {
                        ((JArray)gb[vstr]).children.Add(subobj);
                    }
                    else
                    {
                        gb[vstr] = new JArray(0, new List<JNode> { subobj });
                    }
                }
            }
            else
            {
                string skey = (string)key;
                foreach (JNode node in itbl.children)
                {
                    JObject subobj = (JObject)node;
                    JNode val = subobj[skey];
                    vstr = val.type == Dtype.STR ? (string)val.value : val.ToString();
                    if (gb.ContainsKey(vstr))
                    {
                        ((JArray)gb[vstr]).children.Add(subobj);
                    }
                    else
                    {
                        gb[vstr] = new JArray(0, new List<JNode> { subobj });
                    }
                }
            }
            return new JObject(0, gb);
        }

        ///// <summary>
        ///// Like GroupBy, the first argument is a JArray containing only JObjects or only JArrays, and the second arg is a string or int.<br></br>
        ///// The third argument is a function of the current JSON, e.g., sum(@[:].foo).
        ///// Returns a JObject mapping each distinct stringified value at the key^th index/key of each subobject in the original iterable
        ///// to the specified aggregation function of all of those iterables.<br></br>
        ///// EXAMPLE<br></br>
        ///// AggBy([{"foo": 1, "bar": "a"}, {"foo": 2, "bar": "b"}, {"foo": 3, "bar": "a"}], "bar", sum(@[:].foo)) returns <br></br>
        ///// {"a": 4.0, "b": 2.0}
        ///// </summary>
        ///// <param name="args"></param>
        ///// <returns></returns>
        //public static JNode AggBy(List<JNode> args)
        //{
        //    JObject gb = (JObject)GroupBy(args.Slice(":2").ToArray());
        //    CurJson fun = (CurJson)args[2];
        //    var aggs = new Dictionary<string, JNode>();
        //    foreach ((string key, JNode subitbl) in gb.children)
        //    {
        //        aggs[key] = fun.function(subitbl);
        //    }
        //    return new JObject(0, aggs);
        //}

        /// <summary>
        /// Takes 2-8 JArrays as arguments. They must be of equal length.
        /// Returns: one JArray in which the i^th element is an array containing the i^th elements of the input arrays.
        /// Example:
        /// Zip([1,2],["a", "b"], [true, false]) = [[1, "a", true], [2, "b", false]]
        /// </summary>
        public static JNode Zip(List<JNode> args)
        {
            int first_len = ((JArray)args[0]).Length;
            List<JArray> arrs = new List<JArray>();
            foreach (JNode arg in args)
            {
                if (arg is JArray) arrs.Add((JArray)arg);
            }
            // check equal lengths
            foreach (JArray arr in arrs)
            {
                if (arr.Length != first_len)
                {
                    throw new RemesPathException("The `zip` function expects all input arrays to have equal length");
                }
            }
            var rst = new List<JNode>();
            // zip them together
            for (int ii = 0; ii < first_len; ii++)
            {
                List<JNode> subarr = new List<JNode>();
                foreach (JArray arr in arrs)
                {
                    subarr.Add(arr[ii]);
                }
                rst.Add(new JArray(0, subarr));
            }
            return new JArray(0, rst);
        }

        /// <summary>
        /// Expects one argument:
        /// A JArray of 2-entry JArrays where:
        /// the first entry of each subarray is a string and the second entry is anything.
        /// Returns:
        /// A JObject where every first entry is a key and every second entry is the corresponding value.
        /// Example:
        /// Dict([["a", 1], ["b", 2], ["c", 3]]) = {"a": 1, "b": 2, "c": 3}
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Dict(List<JNode> args)
        {
            JArray pairs = (JArray)args[0];
            Dictionary<string, JNode> rst = new Dictionary<string, JNode>();
            for (int ii = 0; ii < pairs.Length; ii++)
            {
                JArray pair = (JArray)pairs[ii];
                JNode key = pair[0];
                JNode val = pair[1];
                if (!(key.value is string)) throw new RemesPathException("The Dict function's first argument must be an array of all strings");
                rst[(string)key.value] = val;
            }
            return new JObject(0, rst);
        }

        /// <summary>
        /// Takes 2-8 arguments, either all arrays or all objects.<br></br>
        /// If all args are arrays, returns an array that contains all elements of
        /// every array passed in, in the order they were passed.<br></br>
        /// If all args are objects, returns an object that contains all key-value pairs in
        /// all the objects passed in.<br></br>
        /// If multiple objects have the same keys, objects later in the arguments take precedence.<br></br>
        /// EXAMPLES<br></br>
        /// concat([1, 2], [3, 4], [5]) -> [1, 2, 3, 4, 5]<br></br>
        /// concat({"a": 1, "b": 2}, {"c": 3}, {"a": 4}) -> {"b": 2, "c": 3, "a": 4}<br></br>
        /// concat([1, 2], {"a": 2}) raises an exception because you can't concatenate arrays with objects.<br></br>
        /// concat(1, [1, 2]) raises an exception because you can't concatenate anything with non-iterables.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="RemesPathException"></exception>
        public static JNode Concat(List<JNode> args)
        {
            JNode first_itbl = args[0];
            if (first_itbl is JArray first_arr)
            {
                List<JNode> new_arr = new List<JNode>(first_arr.children);
                for (int ii = 1; ii < args.Count; ii++)
                {
                    if (!(args[ii] is JArray arr))
                        throw new RemesPathException("All arguments to the 'concat' function must the same type - either arrays or objects");
                    new_arr.AddRange(arr.children);
                }
                return new JArray(0, new_arr);
            }
            else if (first_itbl is JObject first_obj)
            {
                Dictionary<string, JNode> new_obj = new Dictionary<string, JNode>(first_obj.children);
                for (int ii = 1; ii < args.Count; ii++)
                {
                    if (!(args[ii] is JObject obj))
                        throw new RemesPathException("All arguments to the 'concat' function must the same type - either arrays or objects");
                    foreach (string key in obj.children.Keys)
                    {
                        // this can overwrite the same key in any earlier arg.
                        // TODO: maybe consider raising if multiple iterables have same key?
                        new_obj[key] = obj[key];
                    }
                }
                return new JObject(0, new_obj);
            }
            throw new RemesPathException("All arguments to the 'concat' function must the same type - either arrays or objects");
        }

        /// <summary>
        /// Takes an array and any number of other things (any JSON) and returns a <em>new array</em> with
        /// the other things added to the end of the first array.<br></br>
        /// Does not mutate the original array.<br></br>
        /// They are added in the order that they were passed as arguments.<br></br>
        /// EXAMPLES<br></br>
        /// - append([], 1, false, "a", [4]) -> [1, false, "a", [4]]
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="RemesPathException"></exception>
        public static JNode Append(List<JNode> args)
        {
            JArray arr = (JArray)args[0];
            List<JNode> new_arr = new List<JNode>(arr.children);
            for (int ii = 1; ii < args.Count; ii++)
            {
                JNode arg = args[ii];
                new_arr.Add(arg);
            }
            return new JArray(0, new_arr);
        }

        /// <summary>
        /// The 3+ arguments must have the types (obj: object, ...: string, anything alternating)<br></br>
        /// Returns a <em>new object</em> with the key-value pair(s) k_i-v_i added.<br></br>
        /// <em>Does not mutate the original object.</em><br></br>
        /// EXAMPLES<br></br>
        /// - add_items({}, "a", 1, "b", 2, "c", 3, "d", 4) -> {"a": 1, "b": 2, "c": 3, "d": 4}
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode AddItems(List<JNode> args)
        {
            JObject obj = (JObject)args[0];
            Dictionary<string, JNode> new_obj = new Dictionary<string, JNode>(obj.children);
            int ii = 1;
            while (ii < args.Count - 1)
            {
                JNode k = args[ii++];
                if (k.type != Dtype.STR)
                    throw new RemesPathException("Even-numbered args to 'add_items' function (new keys) must be strings");
                JNode v = args[ii++];
                new_obj[(string)k.value] = v;
            }
            return new JObject(0, new_obj);
        }

        public static JNode ToRecords(List<JNode> args)
        {
            JNode arg = args[0];
            Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(arg);
            char strat = args[1].value == null ? 'd' : ((string)args[1].value)[0];
            switch (strat)
            {
                case 'd': return new JsonTabularizer(JsonTabularizerStrategy.DEFAULT).BuildTable(arg, schema);
                case 'r': return new JsonTabularizer(JsonTabularizerStrategy.FULL_RECURSIVE).BuildTable(arg, schema);
                case 'n': return new JsonTabularizer(JsonTabularizerStrategy.NO_RECURSION).BuildTable(arg, schema);
                case 's': return new JsonTabularizer(JsonTabularizerStrategy.STRINGIFY_ITERABLES).BuildTable(arg, schema);
                default: throw new RemesPathException($"ToRecords second arg must be either blank or one of\n'd': default\n'r': full recursive\n'n': no recursion\n's': stringify iterables\nSee the JSON to CSV form documentation.");
            }
        }

        public static JNode Pivot(List<JNode> args)
        {
            JArray arr = (JArray)args[0];
            var piv = new Dictionary<string, JNode>();
            if (arr[0] is JArray)
            {
                int by = Convert.ToInt32(args[1].value);
                int val_col = Convert.ToInt32(args[2].value);
                foreach (JNode item in arr.children)
                {
                    JArray subarr = (JArray)item;
                    JNode bynode = subarr[by];
                    string key = bynode.type == Dtype.STR
                        ? JObject.FormatAsKey((string)bynode.value)
                        : bynode.ToString();
                    if (!piv.ContainsKey(key))
                        piv[key] = new JArray();
                    ((JArray)piv[key]).children.Add(subarr[val_col]);
                }
                int uniq_ct = piv.Count;
                for (int ii = 3; ii < args.Count; ii++)
                {
                    int idx_col = Convert.ToInt32(args[ii].value);
                    var new_subarr = new List<JNode>();
                    for (int jj = 0; jj < arr.children.Count; jj += uniq_ct)
                        new_subarr.Add(((JArray)arr[jj])[idx_col]);
                    piv[idx_col.ToString()] = new JArray(0, new_subarr);
                }
            }
            else if (arr[0] is JObject)
            {
                string by = (string)args[1].value;
                string val_col = (string)args[2].value;
                foreach (JNode item in arr.children)
                {
                    JObject subobj = (JObject)item;
                    JNode bynode = subobj[by];
                    string key = bynode.type == Dtype.STR
                        ? JObject.FormatAsKey((string)bynode.value)
                        : bynode.ToString();
                    if (!piv.ContainsKey(key))
                        piv[key] = new JArray();
                    ((JArray)piv[key]).children.Add(subobj[val_col]);
                }
                int uniq_ct = piv.Count;
                for (int ii = 3; ii < args.Count; ii++)
                {
                    string idx_col = (string)args[ii].value;
                    var new_subarr = new List<JNode>();
                    for (int jj = 0; jj < arr.children.Count; jj += uniq_ct)
                        new_subarr.Add(((JObject)arr[jj])[idx_col]);
                    piv[idx_col] = new JArray(0, new_subarr);
                }
            }
            else throw new RemesPathException("First argument to Pivot must be an array of arrays or an array of objects");
            return new JObject(0, piv);
        }

        /// <summary>
        /// Accepts one argument, an array of all booleans.<br></br>
        /// Returns true if ALL of the booleans in the array are true.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode All(List<JNode> args)
        {
            JArray arr = (JArray)args[0];
            foreach (JNode item in arr.children)
            {
                if (!(bool)item.value)
                    return new JNode(false, Dtype.BOOL, 0);
            }
            return new JNode(true, Dtype.BOOL, 0);
        }

        /// <summary>
        /// Accepts one argument, an array of all booleans.<br></br>
        /// Returns true if ANY of the values in the array are true.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode Any(List<JNode> args)
        {
            JArray arr = (JArray)args[0];
            foreach (JNode item in arr.children)
            {
                if ((bool)item.value)
                    return new JNode(true, Dtype.BOOL, 0);
            }
            return new JNode(false, Dtype.BOOL, 0);
        }
        #endregion
        #region VECTORIZED_ARG_FUNCTIONS
        /// <summary>
        /// Length of a string
        /// </summary>
        /// <param name="node">string</param>
        public static JNode StrLen(List<JNode> args)
        {
            JNode node = args[0];
            if (node.type != Dtype.STR)
            {
                throw new RemesPathException("StrLen only works for strings");
            }
            return new JNode(Convert.ToInt64(((string)node.value).Length), Dtype.INT, 0);
        }

        /// <summary>
        /// Returns a string made by joining one string to itself n times.<br></br>
        /// Thus StrMul("ab", 3) -> "ababab"
        /// </summary>
        /// <param name="node">string</param>
        /// <param name="n">number of times to repeat s</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static JNode StrMul(List<JNode> args)
        {
            JNode node = args[0];
            JNode n = args[1];
            string s = (string)node.value;
            var sb = new StringBuilder();
            for (int i = 0; i < Convert.ToInt32(n.value); i++)
            {
                sb.Append(s);
            }
            return new JNode(sb.ToString(), Dtype.STR, 0);
        }

        /// <summary>
        /// Return a JNode of type = Dtype.INT with value equal to the number of ocurrences of 
        /// pattern or substring sub in node.value.<br></br>
        /// So StrCount(JNode("ababa", Dtype.STR, 0), Regex("a?ba")) -> JNode(2, Dtype.INT, 0)
        /// because "a?ba" matches "aba" starting at position 0 and "ba" starting at position 3.
        /// </summary>
        /// <param name="node">a string in which to find pattern/substring sub</param>
        /// <param name="sub">a substring or Regex pattern</param>
        public static JNode StrCount(List<JNode> args)
        {
            JNode node = args[0];
            JNode sub = args[1];
            string s = (string)node.value;
            int ct;
            if (sub.type == Dtype.REGEX)
            {
                ct = (((JRegex)sub).regex).Matches(s).Count;
                
            }
            else
            {
                ct = Regex.Matches(s, (string)sub.value).Count;
            }
            return new JNode(Convert.ToInt64(ct), Dtype.INT, 0);
        }

        /// <summary>
        /// Get a List<object> containing all non-overlapping occurrences of regex pattern pat in
        /// string node
        /// </summary>
        /// <param name="node">string</param>
        /// <param name="sub">substring or Regex pattern to be found within node</param>
        /// <returns></returns>
        public static JNode StrFind(List<JNode> args)
        {
            JNode node = args[0];
            JNode pat = args[1];
            Regex rex = (pat as JRegex).regex;
            MatchCollection results = rex.Matches((string)node.value);
            var result_list = new List<JNode>();
            foreach (Match match in results)
            {
                result_list.Add(new JNode(match.Value, Dtype.STR, 0));
            }
            return new JArray(0, result_list);
        }

        public static JNode StrSplit(List<JNode> args)
        {
            JNode node = args[0];
            JNode sep = args[1];
            string s = (string)node.value;
            string[] parts = (sep.type == Dtype.STR) ? Regex.Split(s, (string)sep.value) : ((JRegex)sep).regex.Split(s);
            var out_nodes = new List<JNode>();
            foreach (string part in parts)
            {
                out_nodes.Add(new JNode(part, Dtype.STR, 0));
            }
            return new JArray(0, out_nodes);
        }

        public static JNode StrLower(List<JNode> args)
        {
            return new JNode(((string)args[0].value).ToLower(), Dtype.STR, 0);
        }

        public static JNode StrUpper(List<JNode> args)
        {
            return new JNode(((string)args[0].value).ToUpper(), Dtype.STR, 0);
        }

        public static JNode StrStrip(List<JNode> args)
        {
            return new JNode(((string)args[0].value).Trim(), Dtype.STR, 0);
        }

        public static JNode StrSlice(List<JNode> args)
        {
            string s = (string)args[0].value;
            JNode slicer_or_int = args[1];
            if (slicer_or_int is JSlicer)
            {
                return new JNode(s.Slice(((JSlicer)slicer_or_int).slicer), Dtype.STR, 0);
            }
            int index = Convert.ToInt32(slicer_or_int.value);
            // allow negative indices for consistency with slicing syntax
            index = index < s.Length ? index : s.Length + index;
            return new JNode(s.Substring(index, 1), Dtype.STR, 0);            
        }

        /// <summary>
        /// first arg: a string<br></br>
        /// second arg: a string or regex to be replaced<br></br>
        /// third arg: a string<br></br>
        /// Returns:<br></br>
        /// * if arg 2 is a string, a new string with all instances of arg 2 in arg 1 replaced with arg 3<br></br>
        /// * if arg 2 is a regex, a new string with all matches to that regex in arg 1 replaced with arg 3<br></br>
        /// EXAMPLES:<br></br>
        /// * StrSub("abbbbc", Regex("b+"), "z") -> "azc"<br></br>
        /// * StrSub("123 123", "1", "z") -> "z23 z23"<br></br>
        /// <i>NOTE: prior to JsonTools 4.10.1, it didn't matter whether arg 2 was a string or regex; it always treated it as a regex.</i> 
        /// </summary>
         /// <returns>new JNode of type = Dtype.STR with all replacements made</returns>
        public static JNode StrSub(List<JNode> args)
        {
            JNode node = args[0];
            JNode to_replace = args[1];
            JNode repl = args[2];
            string val = (string)node.value;
            if (to_replace.type == Dtype.STR)
            {
                return new JNode(val.Replace((string)to_replace.value, (string)repl.value), Dtype.STR, 0);
            }
            return new JNode(((JRegex)to_replace).regex.Replace(val, (string)repl.value), Dtype.STR, 0);
        }

        /// <summary>
        /// returns true is x is string
        /// </summary>
        public static JNode IsStr(List<JNode> args)
        {
            return new JNode(args[0].type == Dtype.STR, Dtype.BOOL, 0);
        }

        /// <summary>
        /// returns true is x is long, double, or bool
        /// </summary>
        public static JNode IsNum(List<JNode> args)
        {
            return new JNode((args[0].type & (Dtype.INT | Dtype.FLOAT | Dtype.BOOL)) != 0, Dtype.BOOL, 0);
        }

        /// <summary>
        /// returns true if x is JObject or JArray
        /// </summary>
        public static JNode IsExpr(List<JNode> args)
        {
            return new JNode((args[0].type & Dtype.ARR_OR_OBJ) != 0, Dtype.BOOL, 0);
        }

        public static JNode IsNull(List<JNode> args)
        {
            return new JNode(args[0].type == Dtype.NULL, Dtype.BOOL, 0);
        }

        /// <summary>
        /// if first arg is true, returns second arg. Else returns first arg.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode IfElse(List<JNode> args)
        {
            return (bool)args[0].value ? args[1] : args[2];
        }

        public static JNode Log(List<JNode> args)
        {
            double num = Convert.ToDouble(args[0].value);
            object Base = args[1].value;
            if (Base == null)
            {
                return new JNode(Math.Log(num), Dtype.FLOAT, 0);
            }
            return new JNode(Math.Log(num, Convert.ToDouble(Base)), Dtype.FLOAT, 0);
        }

        public static JNode Log2(List<JNode> args)
        {
            return new JNode(Math.Log(Convert.ToDouble(args[0].value), 2), Dtype.FLOAT, 0);
        }

        public static JNode Abs(List<JNode> args)
        {
            JNode val = args[0];
            if (val.type == Dtype.INT)
            {
                return new JNode(Math.Abs(Convert.ToInt64(val.value)), Dtype.INT, 0);
            }
            else if (val.type == Dtype.FLOAT)
            {
                return new JNode(Math.Abs(Convert.ToDouble(val.value)), Dtype.FLOAT, 0);
            }
            throw new ArgumentException("Abs can only be called on ints and floats");
        }

        public static JNode IsNa(List<JNode> args)
        {
            return new JNode(double.IsNaN(Convert.ToDouble(args[0].value)), Dtype.BOOL, 0);
        }

        public static JNode ToStr(List<JNode> args)
        {
            JNode val = args[0];
            if (val.type == Dtype.STR)
            {
                return new JNode(val.value, Dtype.STR, 0);
            }
            return new JNode(val.ToString(), Dtype.STR, 0);
        }

        public static JNode ToFloat(List<JNode> args)
        {
            JNode val = args[0];
            if (val.type == Dtype.STR)
            {
                return new JNode(double.Parse((string)val.value, JNode.DOT_DECIMAL_SEP), Dtype.FLOAT, 0);
            }
            return new JNode(Convert.ToDouble(val.value), Dtype.FLOAT, 0);
        }

        public static JNode ToInt(List<JNode> args)
        {
            JNode val = args[0];
            if (val.type == Dtype.STR)
            {
                return new JNode(long.Parse((string)val.value), Dtype.INT, 0);
            }
            return new JNode(Convert.ToInt64(val.value), Dtype.INT, 0);
        }

        /// <summary>
        /// If val is an int/long, return val because rounding does nothing to an int/long.<br></br>
        /// If val is a double:<br></br>
        ///     - if sigfigs is null, return val rounded to the nearest long.<br></br>
        ///     - else return val rounded to nearest double with sigfigs decimal places<br></br>
        /// If val's value is any other type, throw an ArgumentException
        /// </summary>
        public static JNode Round(List<JNode> args)
        {
            JNode val = args[0];
            JNode sigfigs = args[1];
            if (val.type == Dtype.INT)
            {
                return new JNode(val.value, Dtype.INT, 0);
            }
            else if (val.type == Dtype.FLOAT)
            {
                if (sigfigs == null)
                {
                    return new JNode(Convert.ToInt64(Math.Round(Convert.ToDouble(val.value))), Dtype.INT, 0);
                }
                return new JNode(Math.Round(Convert.ToDouble(val.value), Convert.ToInt32(sigfigs.value)), Dtype.FLOAT, 0);
            }
            throw new ArgumentException("Cannot round non-float, non-integer numbers");            
        }

        public static JNode Not(List<JNode> args)
        {
            return new JNode(!Convert.ToBoolean(args[0].value), Dtype.BOOL, 0);
        }

        public static JNode Uminus(List<JNode> args)
        {
            JNode val = args[0];
            if (val.type == Dtype.INT)
            {
                return new JNode(-Convert.ToInt64(val.value), Dtype.INT, 0);
            }
            if (val.type == Dtype.FLOAT)
            {
                return new JNode(-Convert.ToDouble(val.value), Dtype.FLOAT, 0);
            }
            throw new RemesPathException("Unary '-' can only be applied to ints and floats");
        }

        #endregion

        public static JNode ObjectsToJNode(object obj)
        {
            if (obj == null)
            {
                return new JNode();
            }
            if (obj is long)
            {
                return new JNode(Convert.ToInt64(obj), Dtype.INT, 0);
            }
            if (obj is double)
            {
                return new JNode((double)obj, Dtype.FLOAT, 0);
            }
            if (obj is string)
            {
                return new JNode((string)obj, Dtype.STR, 0);
            }
            if (obj is bool)
            {
                return new JNode((bool)obj, Dtype.BOOL, 0);
            }
            if (obj is List<object>)
            {
                var nodes = new List<JNode>();
                foreach (object child in (List<object>)obj)
                {
                    nodes.Add(ObjectsToJNode(child));
                }
                return new JArray(0, nodes);
            }
            else if (obj is Dictionary<string, object>)
            {
                var nodes = new Dictionary<string, JNode>();
                var dobj = (Dictionary<string, object>)obj;
                foreach (string key in dobj.Keys)
                {
                    nodes[key] = ObjectsToJNode(dobj[key]);
                }
                return new JObject(0, nodes);
            }
            throw new ArgumentException("Cannot convert any objects to JNode except null, long, double, bool, string, List<object>, or Dictionary<string, object");
        }

        /// <summary>
        /// Recursively extract the values from a JNode, converting JArrays into lists of objects,
        /// JObjects into Dictionaries mapping strings to objects, and everything else to its value.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static object JNodeToObjects(JNode node)
        {
            // if it's not an obj, arr, or unknown, just return its value
            if ((node.type & Dtype.ITERABLE) == 0)
            {
                return node.value;
            }
            if (node.type == Dtype.UNKNOWN)
            {
                return (CurJson)node;
            }
            if (node.type == Dtype.OBJ)
            {
                var dic = new Dictionary<string, object>();
                JObject onode = (JObject)node;
                foreach (string key in onode.children.Keys)
                {
                    dic[key] = JNodeToObjects(onode[key]);
                }
                return dic;
            }
            var arr = new List<object>();
            foreach (JNode val in ((JArray)node).children)
            {
                arr.Add(JNodeToObjects(val));
            }
            return arr;
        }

        // VectorizeArgFunction used to be here, but it's not necessary now that I reimplemented all the
        // vectorized ArgFunctions to take List<JNode> as arguments

        public static Dictionary<string, ArgFunction> FUNCTIONS =
        new Dictionary<string, ArgFunction>
        {
            // non-vectorized functions
            ["add_items"] = new ArgFunction(AddItems, "add_items", Dtype.OBJ, 3, Int32.MaxValue, false, new Dtype[] { Dtype.OBJ | Dtype.UNKNOWN, Dtype.STR, Dtype.ANYTHING, /* any # of args */ Dtype.ANYTHING }),
            ["all"] = new ArgFunction(All, "all", Dtype.BOOL, 1, 1, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN }),
            ["any"] = new ArgFunction(Any, "any", Dtype.BOOL, 1, 1, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN }),
            ["append"] = new ArgFunction(Append, "append", Dtype.ARR, 2, Int32.MaxValue, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN, Dtype.ANYTHING, /* any # of args */ Dtype.ANYTHING }),
            ["avg"] = new ArgFunction(Mean, "avg", Dtype.FLOAT, 1, 1, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN }),
            ["concat"] = new ArgFunction(Concat, "concat", Dtype.ARR_OR_OBJ, 2, Int32.MaxValue, false, new Dtype[] { Dtype.ITERABLE, Dtype.ITERABLE, /* any # of args */ Dtype.ITERABLE }),
            ["dict"] = new ArgFunction(Dict, "dict", Dtype.OBJ, 1, 1, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN }),
            ["flatten"] = new ArgFunction(Flatten, "flatten", Dtype.ARR, 1, 2, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN, Dtype.INT }),
            ["group_by"] = new ArgFunction(GroupBy, "group_by", Dtype.OBJ, 2, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT}),
            ["in"] = new ArgFunction(In, "in", Dtype.BOOL, 2, 2, false, new Dtype[] {Dtype.ANYTHING, Dtype.ITERABLE }),
            ["index"] = new ArgFunction(Index, "index", Dtype.INT, 2, 3, false, new Dtype[] {Dtype.ITERABLE, Dtype.SCALAR, Dtype.BOOL}),
            ["items"] = new ArgFunction(Items, "items", Dtype.ARR, 1, 1, false, new Dtype[] { Dtype.OBJ | Dtype.UNKNOWN }),
            ["keys"] = new ArgFunction(Keys, "keys", Dtype.ARR, 1, 1, false, new Dtype[] {Dtype.OBJ | Dtype.UNKNOWN}),
            ["len"] = new ArgFunction(Len, "len", Dtype.INT, 1, 1, false, new Dtype[] {Dtype.ITERABLE}),
            ["max"] = new ArgFunction(Max, "max", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["max_by"] = new ArgFunction(MaxBy, "max_by", Dtype.ARR_OR_OBJ, 2, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT}),
            ["mean"] = new ArgFunction(Mean, "mean", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["min"] = new ArgFunction(Min, "min", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["pivot"] = new ArgFunction(Pivot, "pivot", Dtype.OBJ, 3, Int32.MaxValue, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT | Dtype.UNKNOWN, Dtype.STR | Dtype.INT | Dtype.UNKNOWN, /* any # of args */ Dtype.STR | Dtype.INT | Dtype.UNKNOWN }),
            ["min_by"] = new ArgFunction(MinBy, "min_by", Dtype.ARR_OR_OBJ, 2, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT}),
            ["quantile"] = new ArgFunction(Quantile, "quantile", Dtype.FLOAT, 2, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.FLOAT}),
            ["range"] = new ArgFunction(Range, "range", Dtype.ARR, 1, 3, false, new Dtype[] {Dtype.INT | Dtype.UNKNOWN, Dtype.INT | Dtype.UNKNOWN, Dtype.INT | Dtype.UNKNOWN}),
            ["s_join"] = new ArgFunction(StringJoin, "s_join", Dtype.STR, 2, 2, false, new Dtype[] {Dtype.STR, Dtype.ARR | Dtype.UNKNOWN}),
            ["sort_by"] = new ArgFunction(SortBy, "sort_by", Dtype.ARR, 2, 3, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT, Dtype.BOOL }),
            ["sorted"] = new ArgFunction(Sorted, "sorted", Dtype.ARR, 1, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.BOOL}),
            ["sum"] = new ArgFunction(Sum, "sum", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["to_records"] = new ArgFunction(ToRecords, "to_records", Dtype.ARR, 1, 2, false, new Dtype[] { Dtype.ITERABLE, Dtype.STR | Dtype.UNKNOWN }),
            ["unique"] = new ArgFunction(Unique, "unique", Dtype.ARR, 1, 2, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.BOOL}),
            ["value_counts"] = new ArgFunction(ValueCounts, "value_counts",Dtype.ARR_OR_OBJ, 1, 1, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN}),
            ["values"] = new ArgFunction(Values, "values", Dtype.ARR, 1, 1, false, new Dtype[] {Dtype.OBJ | Dtype.UNKNOWN}),
            ["zip"] = new ArgFunction(Zip, "zip", Dtype.ARR, 2, Int32.MaxValue, false, new Dtype[] {Dtype.ARR | Dtype.UNKNOWN, Dtype.ARR | Dtype.UNKNOWN, /* any # of args */ Dtype.ARR | Dtype.UNKNOWN }),
            //["agg_by"] = new ArgFunction(AggBy, "agg_by", Dtype.OBJ, 3, 3, false, new Dtype[] { Dtype.ARR | Dtype.UNKNOWN, Dtype.STR | Dtype.INT, Dtype.ITERABLE | Dtype.SCALAR }),
            // vectorized functions
            ["abs"] = new ArgFunction(Abs, "abs", Dtype.FLOAT_OR_INT, 1, 1, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE}),
            ["float"] = new ArgFunction(ToFloat, "float", Dtype.FLOAT, 1, 1, true, new Dtype[] { Dtype.ANYTHING}),
            ["ifelse"] = new ArgFunction(IfElse, "ifelse", Dtype.UNKNOWN, 3, 3, true, new Dtype[] {Dtype.ANYTHING, Dtype.ITERABLE | Dtype.SCALAR, Dtype.ITERABLE | Dtype.SCALAR}),
            ["int"] = new ArgFunction(ToInt, "int", Dtype.INT, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["is_expr"] = new ArgFunction(IsExpr, "is_expr", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["is_num"] = new ArgFunction(IsNum, "is_num", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["is_str"] = new ArgFunction(IsStr, "is_str", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["isna"] = new ArgFunction(IsNa, "isna", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["isnull"] = new ArgFunction(IsNull, "is_null", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["log"] = new ArgFunction(Log, "log", Dtype.FLOAT, 1, 2, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE, Dtype.FLOAT_OR_INT}),
            ["log2"] = new ArgFunction(Log2, "log2", Dtype.FLOAT, 1, 1, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE}),
            ["__UMINUS__"] = new ArgFunction(Uminus, "-", Dtype.FLOAT_OR_INT, 1, 1, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE}),
            ["not"] = new ArgFunction(Not, "not", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.BOOL | Dtype.ITERABLE}),
            ["round"] = new ArgFunction(Round, "round", Dtype.FLOAT_OR_INT, 1, 2, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE, Dtype.INT}),
            ["s_count"] = new ArgFunction(StrCount, "s_count", Dtype.STR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX}),
            ["s_find"] = new ArgFunction(StrFind, "s_find", Dtype.ARR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.REGEX}),
            ["s_len"] = new ArgFunction(StrLen, "s_len", Dtype.INT, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_lower"] = new ArgFunction(StrLower, "s_lower", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_mul"] = new ArgFunction(StrMul, "s_mul", Dtype.STR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.INT | Dtype.UNKNOWN }),
            ["s_slice"] = new ArgFunction(StrSlice, "s_slice", Dtype.STR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.INT_OR_SLICE}),
            ["s_split"] = new ArgFunction(StrSplit, "s_split", Dtype.ARR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX}),
            ["s_strip"] = new ArgFunction(StrStrip, "s_strip", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_sub"] = new ArgFunction(StrSub, "s_sub", Dtype.STR, 3, 3, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX, Dtype.STR}),
            ["s_upper"] = new ArgFunction(StrUpper, "s_upper", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["str"] = new ArgFunction(ToStr, "str", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.ANYTHING})
        };
    }


    public class ArgFunctionWithArgs
    {
        public ArgFunction function;
        public List<JNode> args;

        public ArgFunctionWithArgs(ArgFunction function, List<JNode> args)
        {
            this.function = function;
            this.args = args;
        }

        public JNode Call()
        {
            return function.Call(args);
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

        public JNode Identity(JNode obj)
        {
            return obj;
        }
    }
}