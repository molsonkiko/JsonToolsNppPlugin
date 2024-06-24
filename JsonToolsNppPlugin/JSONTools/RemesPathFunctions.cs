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
        /// <summary>
        /// whether a op b op c should be evaluated as<br></br>
        /// a op (b op c) (right associative) or<br></br>
        /// (a op b) op c (left associative, default)
        /// </summary>
        public bool isRightAssociative;

        public Binop(Func<JNode, JNode, JNode> function, float precedence, string name, bool isRightAssociative = false)
        {
            Function = function;
            this.precedence = precedence;
            this.name = name;
            this.isRightAssociative = isRightAssociative;
        }

        public override string ToString()
        {
            return $"Binop(\"{name}\")";
        }

        /// <summary>
        /// whether this binop is evaluated before another binop or UnaryOp
        /// that is to the left.
        /// </summary>
        /// <param name="right"></param>
        /// <returns></returns>
        public bool PrecedesLeft(float leftPrecedence)
        {
            if (leftPrecedence > this.precedence)
                return false;
            if (leftPrecedence < this.precedence)
                return true;
            return isRightAssociative;
        }

        public JNode Call(JNode left, JNode right)
        {
            if (left is CurJson cjl)
            {
                if (right is CurJson cjr)
                {
                    return new CurJson(Dtype.UNKNOWN, (JNode json) =>
                        Function(cjl.function(json), cjr.function(json))
                    );
                }
                return new CurJson(Dtype.UNKNOWN,
                    json => Function(cjl.function(json), right));
            }
            else if (right is CurJson cjr_)
            {
                return new CurJson(Dtype.UNKNOWN,
                    json => Function(left, cjr_.function(json)));
            }
            return Function(left, right);
        }

        //public static void ThrowIfTypeNotIntersects(JNode arg, Dtype mustIntersect, bool argIsLHS, string binopName)
        //{
        //    if ((arg.type & mustIntersect) == 0)
        //    {
        //        string argSide = argIsLHS ? "left" : "right";
        //        throw new RemesPathException($"Invalid type {arg.type} for {argSide} argument to binop \"{binopName}\" (must be a type in {JNode.FormatDtype(mustIntersect)})");
        //    }
        //}

        public static JNode Add(JNode a, JNode b)
        {
            object aval = a.value; object bval = b.value;
            Dtype atype = a.type; Dtype btype = b.type;
            if (JNode.BothTypesIntersect(atype, btype, Dtype.INT_OR_BOOL))
            {
                return new JNode(Convert.ToInt64(aval) + Convert.ToInt64(bval), Dtype.INT, 0);
            }
            if (JNode.BothTypesIntersect(atype, btype, Dtype.NUM))
            {
                return new JNode(Convert.ToDouble(aval) + Convert.ToDouble(bval), Dtype.FLOAT, 0);
            }
            if (atype == Dtype.STR && btype == Dtype.STR)
            {
                return new JNode(Convert.ToString(aval) + Convert.ToString(bval), Dtype.STR, 0);
            }
            throw new RemesPathException($"Can't add objects of types {JNode.FormatDtype(atype)} and {JNode.FormatDtype(btype)}");
        }

        //// ADD VARIANTS WITH ONE SIDE CONSTANT

        //public static JNode AddRhsConstStr(JNode a, string b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.STR, true, "+");
        //    return new JNode(Convert.ToString(a.value) + b);
        //}

        //public static JNode AddLhsConstStr(string a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.STR, false, "+");
        //    return new JNode(a + Convert.ToString(b.value));
        //}

        //public static JNode AddRhsConstInt(JNode a, long b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "+");
        //    if (a.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(a.value) + Convert.ToDouble(b));
        //    return new JNode(Convert.ToInt64(a.value) + b);
        //}

        //public static JNode AddLhsConstInt(long a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "+");
        //    if (b.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(b.value) + Convert.ToDouble(a));
        //    return new JNode(Convert.ToInt64(b.value) + a);
        //}

        //public static JNode AddRhsConstBool(JNode a, bool b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "+");
        //    if (a.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(a.value) + (b ? 1d : 0d));
        //    return new JNode(Convert.ToInt64(a.value) + (b ? 1L : 0L));
        //}

        //public static JNode AddLhsConstBool(bool a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "+");
        //    if (b.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(b.value) + (a ? 1d : 0d));
        //    return new JNode(Convert.ToInt64(b.value) + (a ? 1L : 0L));
        //}

        //public static JNode AddRhsConstFloat(JNode a, double b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "+");
        //    return new JNode(Convert.ToDouble(a.value) + b);
        //}

        //public static JNode AddLhsConstFloat(double a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "+");
        //    return new JNode(Convert.ToDouble(b.value) + a);
        //}

        public static JNode Sub(JNode a, JNode b)
        {
            object aval = a.value, bval = b.value;
            Dtype atype = a.type, btype = b.type;
            if (JNode.BothTypesIntersect(atype, btype, Dtype.INT_OR_BOOL))
            {
                return new JNode(Convert.ToInt64(aval) - Convert.ToInt64(bval), Dtype.INT, 0);
            }
            if (JNode.BothTypesIntersect(atype, btype, Dtype.NUM))
                return new JNode(Convert.ToDouble(aval) - Convert.ToDouble(bval), Dtype.FLOAT, 0);
            throw new RemesPathException($"Can't subtract objects of type {JNode.FormatDtype(atype)} and {JNode.FormatDtype(btype)}");
        }

        //// SUB VARIANTS WITH ONE SIDE CONSTANT

        //public static JNode SubRhsConstInt(JNode a, long b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "-");
        //    if (a.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(a.value) - Convert.ToDouble(b));
        //    return new JNode(Convert.ToInt64(a.value) - b);
        //}

        //public static JNode SubLhsConstInt(long a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "-");
        //    if (b.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(a) - Convert.ToDouble(b.value));
        //    return new JNode(a - Convert.ToInt64(b.value));
        //}

        //public static JNode SubRhsConstBool(JNode a, bool b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "-");
        //    if (a.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(a.value) - (b ? 1d : 0d));
        //    return new JNode(Convert.ToInt64(a.value) - (b ? 1L : 0L));
        //}

        //public static JNode SubLhsConstBool(bool a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "-");
        //    if (b.type == Dtype.FLOAT)
        //        return new JNode((a ? 1d : 0d) - Convert.ToDouble(b.value));
        //    return new JNode((a ? 1L : 0L) - Convert.ToInt64(b.value));
        //}

        //public static JNode SubRhsConstFloat(JNode a, double b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "-");
        //    return new JNode(Convert.ToDouble(a.value) - b);
        //}

        //public static JNode SubLhsConstFloat(double a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "-");
        //    return new JNode(a - Convert.ToDouble(b.value));
        //}

        public static JNode Mul(JNode a, JNode b)
        {
            object aval = a.value, bval = b.value;
            Dtype atype = a.type, btype = b.type;
            if ((btype & Dtype.INT_OR_BOOL) != 0)
            {
                if ((atype & Dtype.INT_OR_BOOL) != 0)
                    return new JNode(Convert.ToInt64(aval) * Convert.ToInt64(bval), Dtype.INT, 0);
                else if (atype == Dtype.STR) // can multiply string by int, but not int by string
                    return new JNode(ArgFunction.StrMulHelper((string)a.value, Convert.ToInt32(b.value)));
            }
            if (JNode.BothTypesIntersect(atype, btype, Dtype.NUM))
                return new JNode(Convert.ToDouble(aval) * Convert.ToDouble(bval), Dtype.FLOAT, 0);
            throw new RemesPathException($"Can't multiply objects of type {JNode.FormatDtype(atype)} and {JNode.FormatDtype(btype)}");
        }

        //// MUL VARIANTS WITH ONE SIDE CONSTANT

        //public static JNode MulLhsConstStr(string a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.INT_OR_BOOL, false, "*");
        //    if (b.type == Dtype.BOOL)
        //        return new JNode(((bool)b.value) ? a : "");
        //    return new JNode(ArgFunction.StrMulHelper(a, (long)b.value));
        //}

        //public static JNode MulRhsConstInt(JNode a, long b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM |  Dtype.STR, true, "*");
        //    if (a.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(a.value) * Convert.ToDouble(b));
        //    if (a.type == Dtype.STR)
        //        return new JNode(ArgFunction.StrMulHelper((string)a.value, b));
        //    return new JNode(Convert.ToInt64(a.value) * b);
        //}

        //public static JNode MulLhsConstInt(long a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "*");
        //    if (b.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(a) * Convert.ToDouble(b.value));
        //    return new JNode(a * Convert.ToInt64(b.value));
        //}

        //public static JNode MulRhsConstBool(JNode a, bool b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM | Dtype.STR, true, "*");
        //    if (a.type == Dtype.FLOAT)
        //        return new JNode(Convert.ToDouble(a.value) * (b ? 1d : 0d));
        //    if (a.type == Dtype.STR)
        //        return new JNode(b ? (string)a.value : "");
        //    return new JNode(Convert.ToInt64(a.value) * (b ? 1L : 0L));
        //}

        //public static JNode MulLhsConstBool(bool a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "*");
        //    if (b.type == Dtype.FLOAT)
        //        return new JNode((a ? 1d : 0d) * Convert.ToDouble(b.value));
        //    return new JNode((a ? 1L : 0L) * Convert.ToInt64(b.value));
        //}

        //public static JNode MulRhsConstFloat(JNode a, double b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "*");
        //    return new JNode(Convert.ToDouble(a.value) * b);
        //}

        //public static JNode MulLhsConstFloat(double a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "*");
        //    return new JNode(a * Convert.ToDouble(b.value));
        //}

        public static JNode Divide(JNode a, JNode b)
        {
            if (JNode.BothTypesIntersect(a.type, b.type, Dtype.NUM))
                return new JNode(Convert.ToDouble(a.value) / Convert.ToDouble(b.value), Dtype.FLOAT, 0);
            throw new RemesPathException($"Can't divide objects of types {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
        }

        //// DIVIDE VARIANTS WITH ONE SIDE CONSTANT

        //public static JNode DivideRhsConstInt(JNode a, long b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "/");
        //    return new JNode(Convert.ToDouble(a.value) / Convert.ToDouble(b));
        //}

        //public static JNode DivideLhsConstInt(long a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "/");
        //    return new JNode(Convert.ToDouble(a) / Convert.ToDouble(b.value));
        //}

        //public static JNode DivideRhsConstBool(JNode a, bool b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "/");
        //    return new JNode(Convert.ToDouble(a.value) / (b ? 1d : 0d));
        //}

        //public static JNode DivideLhsConstBool(bool a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "/");
        //    return new JNode(a ? 1d/Convert.ToDouble(b.value) : 0d);
        //}

        //public static JNode DivideRhsConstFloat(JNode a, double b)
        //{
        //    ThrowIfTypeNotIntersects(a, Dtype.NUM, true, "/");
        //    return new JNode(Convert.ToDouble(a.value) / b);
        //}

        //public static JNode DivideLhsConstFloat(double a, JNode b)
        //{
        //    ThrowIfTypeNotIntersects(b, Dtype.NUM, true, "/");
        //    return new JNode(a / Convert.ToDouble(b.value));
        //}

        public static JNode FloorDivide(JNode a, JNode b)
        {
            if (JNode.BothTypesIntersect(a.type, b.type, Dtype.NUM))
                return new JNode(Convert.ToInt64(Math.Floor(Convert.ToDouble(a.value) / Convert.ToDouble(b.value))), Dtype.INT, 0);
            throw new RemesPathException($"Can't floor divide objects of types {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
        }

        public static JNode Pow(JNode a, JNode b)
        {
            if (JNode.BothTypesIntersect(a.type, b.type, Dtype.NUM))
                return new JNode(Math.Pow(Convert.ToDouble(a.value), Convert.ToDouble(b.value)), Dtype.FLOAT, 0);
            throw new RemesPathException($"Can't exponentiate objects of type {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
        }

        public static JNode Mod(JNode a, JNode b)
        {
            object aval = a.value, bval = b.value;
            Dtype atype = a.type, btype = b.type;
            if (JNode.BothTypesIntersect(atype, btype, Dtype.INT_OR_BOOL))
            {
                return new JNode(Convert.ToInt64(aval) % Convert.ToInt64(bval), Dtype.INT, 0);
            }
            if (JNode.BothTypesIntersect(atype, btype, Dtype.NUM))
                return new JNode(Convert.ToDouble(aval) % Convert.ToDouble(bval), Dtype.FLOAT, 0);
            throw new RemesPathException($"Can't use modulo operator on objects of type {JNode.FormatDtype(atype)} and {JNode.FormatDtype(btype)}");
        }

        public static JNode BitWiseOR(JNode a, JNode b)
        {
            Dtype atype = a.type, btype = b.type;
            if (a.value is bool abool && b.value is bool bbool)
                return new JNode(abool || bbool, Dtype.BOOL, 0);
            if (JNode.BothTypesIntersect(atype, btype, Dtype.INT_OR_BOOL))
            {
                return new JNode(Convert.ToInt64(a.value) | Convert.ToInt64(b.value), Dtype.INT, 0);
            }
            throw new RemesPathException($"Can't bitwise OR objects of type {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
        }

        public static JNode BitWiseXOR(JNode a, JNode b)
        {
            Dtype atype = a.type, btype = b.type;
            if (a.value is bool abool && b.value is bool bbool)
                return new JNode(abool ^ bbool, Dtype.BOOL, 0);
            if (JNode.BothTypesIntersect(atype, btype, Dtype.INT_OR_BOOL))
            {
                return new JNode(Convert.ToInt64(a.value) ^ Convert.ToInt64(b.value), Dtype.INT, 0);
            }
            throw new RemesPathException($"Can't bitwise XOR objects of type {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
        }

        public static JNode BitWiseAND(JNode a, JNode b)
        {
            Dtype atype = a.type, btype = b.type;
            if (a.value is bool abool && b.value is bool bbool)
                return new JNode(abool && bbool, Dtype.BOOL, 0);
            if (JNode.BothTypesIntersect(atype, btype, Dtype.INT_OR_BOOL))
            {
                return new JNode(Convert.ToInt64(a.value) & Convert.ToInt64(b.value), Dtype.INT, 0);
            }
            throw new RemesPathException($"Can't bitwise AND objects of type {JNode.FormatDtype(a.type)} and {JNode.FormatDtype(b.type)}");
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
            if (!(node.value is string s))
                throw new RemesPathException("For \"x =~ y\" expressions, x must be a string and y must be string or regex");
            if (sub is JRegex jregex)
            {
                return new JNode(jregex.regex.IsMatch(s), Dtype.BOOL, 0);
            }
            return new JNode(Regex.IsMatch(s, (string)sub.value), Dtype.BOOL, 0);
        }

        public static JNode LessThan(JNode a, JNode b)
        {
            return new JNode(a.CompareTo(b) < 0, Dtype.BOOL, 0);
        }

        public static JNode GreaterThan(JNode a, JNode b)
        {
            return new JNode(a.CompareTo(b) > 0, Dtype.BOOL, 0);
        }

        public static JNode GreaterThanOrEqual(JNode a, JNode b)
        {
            return new JNode(a.CompareTo(b) >= 0, Dtype.BOOL, 0);
        }

        public static JNode LessThanOrEqual(JNode a, JNode b)
        {
            return new JNode(a.CompareTo(b) <= 0, Dtype.BOOL, 0);
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
            ["%"] = new Binop(Mod, 3, "%"),
            ["*"] = new Binop(Mul, 3, "*"),
            ["/"] = new Binop(Divide, 3, "/"),
            ["**"] = new Binop(Pow, 5, "**", true),
        };

        public static HashSet<string> BOOLEAN_BINOPS = new HashSet<string> { "==", ">", "<", "=~", "!=", ">=", "<=" };

        public static HashSet<string> BITWISE_BINOPS = new HashSet<string> { "^", "&", "|" };

        public static HashSet<string> FLOAT_RETURNING_BINOPS = new HashSet<string> { "/", "**" };

        public static HashSet<string> POLYMORPHIC_BINOPS = new HashSet<string> { "%", "+", "-", "*" };

        /// <summary>
        /// return 2 if x is not an object or array<br></br>
        /// If it is an object or array:<br></br> 
        /// return 1 if its length is 0.<br></br>
        /// else return 0.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static int ObjectOrArrayEmpty(JNode x)
        {
            if (x is JObject obj) { return (obj.Length == 0) ? 1 : 0; }
            if (x is JArray arr) { return (arr.Length == 0) ? 1 : 0; }
            return 2;
        }

        public JNode BinopTwoJsons(JNode left, JNode right)
        {
            if (ObjectOrArrayEmpty(right) == 2)
            {
                if (ObjectOrArrayEmpty(left) == 2)
                {
                    return Call(left, right);
                }
                return BinopJsonScalar(left, right);
            }
            if (ObjectOrArrayEmpty(left) == 2)
            {
                return BinopScalarJson(left, right);
            }
            if (right is JObject robj)
            {
                var dic = new Dictionary<string, JNode>();
                var lobj = (JObject)left;
                if (robj.Length != lobj.Length)
                {
                    throw new VectorizedArithmeticException("Tried to apply a binop to two dicts with different sets of keys");
                }
                foreach (KeyValuePair<string, JNode> rkv in robj.children)
                {
                    bool leftHasKey = lobj.children.TryGetValue(rkv.Key, out JNode leftVal);
                    if (!leftHasKey)
                    {
                        throw new VectorizedArithmeticException("Tried to apply a binop to two dicts with different sets of keys");
                    }
                    dic[rkv.Key] = Call(leftVal, rkv.Value);
                }
                return new JObject(0, dic);
            }
            var rarr = (JArray)right;
            var larr = (JArray)left;
            int rarLen = rarr.Length;
            if (larr.Length != rarLen)
            {
                throw new VectorizedArithmeticException("Tried to perform vectorized arithmetic on two arrays of unequal length");
            }
            var arr = new List<JNode>(rarLen);
            for (int ii = 0; ii < rarLen; ii++)
            {
                arr.Add(Call(larr[ii], rarr[ii]));
            }
            return new JArray(0, arr);
        }

        public JNode BinopJsonScalar(JNode left, JNode right)
        {
            if (left is JObject lobj)
            {
                var dic = new Dictionary<string, JNode>();
                foreach (KeyValuePair<string, JNode> lkv in lobj.children)
                {
                    dic[lkv.Key] = Call(lkv.Value, right);
                }
                return new JObject(0, dic);
            }
            var larr = (JArray)left;
            var arr = larr.children
                .Select(x => Call(x, right))
                .ToList();
            return new JArray(0, arr);
        }

        public JNode BinopScalarJson(JNode left, JNode right)
        {
            if (right is JObject robj)
            {
                var dic = new Dictionary<string, JNode>();
                foreach (KeyValuePair<string, JNode> rkv in robj.children)
                {
                    dic[rkv.Key] = Call(left, rkv.Value);
                }
                return new JObject(0, dic);
            }
            var rarr = (JArray)right;
            var arr = rarr.children
                .Select(x => Call(left, x))
                .ToList();
            return new JArray(0, arr);
        }

        /// <summary>
        /// For a given binop and the types of two JNodes, determines the output's type.<br></br>
        /// Raises a RemesPathException if the types are inappropriate for that Binop.<br></br>
        /// EXAMPLES<br></br>
        /// BinopOutType(Binop.BINOPS["+"], Dtype.STR, Dtype.STR) -> Dtype.STR<br></br>
        /// BinopOutType(Binop.BINOPS["**"], Dtype.STR, Dtype.INT) -> throws RemesPathException<br></br>
        /// BinopOutType(Binop.BINOPS["*"], Dtype.INT, Dtype.FLOAT) -> Dtype.FLOAT
        /// </summary>
        /// <param name="b"></param>
        /// <param name="ltype"></param>
        /// <param name="rtype"></param>
        /// <returns></returns>
        /// <exception cref="RemesPathException"></exception>
        public Dtype BinopOutType(Dtype ltype, Dtype rtype)
        {
            if (ltype == Dtype.UNKNOWN || rtype == Dtype.UNKNOWN) { return Dtype.UNKNOWN; }
            if (ltype == Dtype.OBJ || rtype == Dtype.OBJ)
            {
                if (ltype == Dtype.ARR || rtype == Dtype.ARR)
                {
                    throw new RemesPathException("Cannot have a function of an array and an object");
                }
                return Dtype.OBJ;
            }
            if (ltype == Dtype.ARR || rtype == Dtype.ARR)
            {
                if (ltype == Dtype.OBJ || rtype == Dtype.OBJ)
                {
                    throw new RemesPathException("Cannot have a function of an array and an object");
                }
                return Dtype.ARR;
            }
            if (Binop.BOOLEAN_BINOPS.Contains(name)) { return Dtype.BOOL; }
            if (!JNode.BothTypesIntersect(ltype, rtype, Dtype.NUM))
            {
                if (name == "+" && ltype == Dtype.STR && rtype == Dtype.STR)
                    return Dtype.STR; // string addition
                if (name == "*" && ltype == Dtype.STR && (rtype & Dtype.INT_OR_BOOL) != 0)
                    return Dtype.STR; // string multiplication
                throw new RemesPathException($"Invalid argument types {JNode.FormatDtype(ltype)}" +
                                            $" and {JNode.FormatDtype(rtype)} for binop {name}");
            }
            if (Binop.BITWISE_BINOPS.Contains(name)) // ^, & , |
            {
                // return bool when acting on two bools, int if acting on int/bool, bool/int, or int/int
                if (ltype == Dtype.BOOL && rtype == Dtype.BOOL)
                    return Dtype.BOOL;
                if (JNode.BothTypesIntersect(ltype, rtype, Dtype.INT_OR_BOOL))
                {
                    return Dtype.INT;
                }
                throw new RemesPathException($"Incompatible types {JNode.FormatDtype(ltype)}" +
                                            $" and {JNode.FormatDtype(rtype)} for bitwise binop {name}");
            }
            // it's an arithmetic binop: %, *, +, /, -, //, **
            if (name == "//") { return Dtype.INT; }
            if (Binop.FLOAT_RETURNING_BINOPS.Contains(name)) { return Dtype.FLOAT; }
            // division and exponentiation always give doubles
            if (JNode.BothTypesIntersect(ltype, rtype, Dtype.INT))
            {
                return rtype & ltype;
            }
            return Dtype.FLOAT;
        }

        /// <summary>
        /// Handles all possible argument combinations for a Binop being called on two JNodes:<br></br>
        /// iterable and iterable, iterable and scalar, iterable that's a function of the current JSON and scalar 
        /// that's not, etc.<br></br>
        /// Throws a RemesPathException if an invalid combination of types is chosen.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public JNode Resolve(JNode left, JNode right)
        {
            bool leftItbl = (left.type & Dtype.ITERABLE) != 0;
            bool rightItbl = (right.type & Dtype.ITERABLE) != 0;
            Dtype outType = BinopOutType(left.type, right.type);
            if (left is CurJson lcur)
            {
                if (right is CurJson rcur_)
                {
                    if (leftItbl)
                    {
                        if (rightItbl)
                        {
                            // they're both iterables or unknown type
                            return new CurJson(outType, (JNode x) => BinopTwoJsons(lcur.function(x), rcur_.function(x)));
                        }
                        // only left is an iterable and unknown type
                        return new CurJson(outType, (JNode x) => BinopTwoJsons(lcur.function(x), rcur_.function(x)));
                    }
                    if (rightItbl)
                    {
                        // right is iterable or unknown, but left is not iterable
                        return new CurJson(outType, (JNode x) => BinopTwoJsons(lcur.function(x), rcur_.function(x)));
                    }
                    // they're both scalars
                    return new CurJson(outType, (JNode x) => Call(lcur.function(x), rcur_.function(x)));
                }
                // right is not a function of the current JSON, but left is
                if (leftItbl)
                {
                    if (rightItbl)
                    {
                        return new CurJson(outType, (JNode x) => BinopTwoJsons(lcur.function(x), right));
                    }
                    return new CurJson(outType, (JNode x) => BinopTwoJsons(lcur.function(x), right));
                }
                if (rightItbl)
                {
                    return new CurJson(outType, (JNode x) => BinopTwoJsons(lcur.function(x), right));
                }
                return new CurJson(outType, (JNode x) => Call(lcur.function(x), right));
            }
            if (right is CurJson rcur)
            {
                // left is not a function of the current JSON, but right is
                if (leftItbl)
                {
                    if (rightItbl)
                    {
                        return new CurJson(outType, (JNode x) => BinopTwoJsons(left, rcur.function(x)));
                    }
                    return new CurJson(outType, (JNode x) => BinopTwoJsons(left, rcur.function(x)));
                }
                if (rightItbl)
                {
                    return new CurJson(outType, (JNode x) => BinopTwoJsons(left, rcur.function(x)));
                }
                return new CurJson(outType, (JNode x) => Call(left, rcur.function(x)));
            }
            // neither is a function of the current JSON
            if (leftItbl)
            {
                if (rightItbl)
                {
                    return BinopTwoJsons(left, right);
                }
                return BinopJsonScalar(left, right);
            }
            if (rightItbl)
            {
                return BinopScalarJson(left, right);
            }
            return Call(left, right);
        }
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

        public override string ToString()
        {
            string leftStr;
            if (left == null)
                leftStr = "null";
            else if (left is BinopWithArgs lbwa)
                leftStr = lbwa.ToString();
            else
                leftStr = ((JNode)left).ToString();
            string rightStr;
            if (right == null)
                rightStr = "null";
            else if (right is BinopWithArgs rbwa)
                rightStr = rbwa.ToString();
            else
                rightStr = ((JNode)right).ToString();
            return $"BinopWithArgs(\"{binop.name}\", {leftStr}, {rightStr})";
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
            return binop.Resolve((JNode)left, (JNode)right);
        }

        public static object ResolveStack(List<BinopWithArgs> bwaStack, List<object> argStack)
        {
            if (argStack.Count == 0)
            {
                if (bwaStack.Count > 0)
                    throw new RemesPathException($"Binop {bwaStack.Last().binop.name} with no left argument");
                return null;
            }
            if (bwaStack.Count == 0)
            {
                if (argStack.Count >= 2)
                    throw new RemesPathException($"Two JNodes {argStack[0]} and {argStack[1]} with no binop in between");
                return (JNode)argStack[0];
            }
            BinopWithArgs bwa = bwaStack.Last();
            Binop bop = bwa.binop;
            if (bwaStack.Count == 1)
            {
                if (bwa.left == null)
                    bwa.left = argStack.Pop();
                if (argStack.Count >= 1)
                {
                    bwa.right = argStack.Pop();
                    JNode result = bop.Resolve((JNode)bwa.left, (JNode)bwa.right);
                    bwaStack.Clear();
                    return result;
                }
                return bwa;
            }
            // two or more binops; they can have a contest
            object lastArg = argStack.Pop();
            if (bwa.left != null)
            {
                bwa.right = lastArg;
                argStack.Add(bwa.Call());
                bwaStack.Pop();
                return ResolveStack(bwaStack, argStack);
            }
            BinopWithArgs leftBwa = bwaStack[bwaStack.Count - 2];
            float leftPrecedence = leftBwa.binop.precedence;
            if (bop.PrecedesLeft(leftPrecedence))
            {
                bwa.left = lastArg;
                return bwa;
            }
            // left wins, takes argument, is evaluated
            // binop left of left (if exists) and latest binop fight
            leftBwa.right = lastArg;
            bwaStack.RemoveAt(bwaStack.Count - 2);
            argStack.Add(leftBwa.Call());
            return ResolveStack(bwaStack, argStack);
        }
    }
    #endregion
    public class UnaryOp
    {
        private Func<JNode, JNode> Function { get; }
        public float precedence;
        public string name;
        /// <summary>
        /// whether op(op(a)) = a for all a
        /// </summary>
        public bool isSelfNegating;
        /// <summary>
        /// type of the output of the binop, given the type of x.<br></br>
        /// This function should only handle cases where x is a scalar of known type
        /// (if x is an iterable or unknown, the output type is always the same type as x)
        /// </summary>
        public Func<Dtype, Dtype> outputType;

        public UnaryOp(Func<JNode, JNode> function, float precedence, string name, bool isSelfNegating, Func<Dtype, Dtype> outputType)
        {
            this.Function = function;
            this.precedence = precedence;
            this.name = name;
            this.isSelfNegating = isSelfNegating;
            this.outputType = outputType;
        }

        public override string ToString()
        {
            return $"UnaryOp(\"{name}\")";
        }

        public JNode Call(JNode x)
        {
            if (x is CurJson cj)
            {
                Dtype outType = (cj.type & Dtype.ITERABLE) != 0
                    ? cj.type
                    : outputType(cj.type);
                if (outType == Dtype.TYPELESS)
                    throw new RemesPathException($"Invalid argument type {JNode.FormatDtype(cj.type)} for unary function '{name}'");
                return new CurJson(outType, json => Call(cj.function(json)));
            }
            if (x is JArray arr)
            {
                return new JArray(0,
                    arr.children.Select(Function).ToList()
                );
            }
            if (x is JObject obj)
            {
                var outObj = new Dictionary<string, JNode>(obj.Length);
                foreach (KeyValuePair<string, JNode> kv in obj.children)
                {
                    outObj[kv.Key] = Function(kv.Value);
                }
                return new JObject(0, outObj);
            }
            return Function(x);
        }

        public void AddToStack(List<UnaryOp> stack)
        {
            if (stack.Count > 0 && stack.Last().name == name && isSelfNegating)
                stack.RemoveAt(stack.Count - 1);
            // e.g., double negatives cancel
            else
                stack.Add(this);
        }

        public static Dictionary<string, UnaryOp> UNARY_OPS = new Dictionary<string, UnaryOp>
        {
            ["-"] = new UnaryOp(Uminus, 4, "-", true, NumericUnaryOpsOutputType),
            ["not"] = new UnaryOp(Not, -2, "not", false, NotUnaryOpOutputType),
            ["+"] = new UnaryOp(Uplus, 4, "+", true, NumericUnaryOpsOutputType),
        };

        public static Dtype NumericUnaryOpsOutputType(Dtype x)
        {
            if (x == Dtype.FLOAT)
                return Dtype.FLOAT;
            if ((x & Dtype.INT_OR_BOOL) != 0)
                return Dtype.INT;
            return Dtype.TYPELESS;
        }

        public static Dtype NotUnaryOpOutputType(Dtype x)
        {
            return (x & (Dtype.NULL | Dtype.STR | Dtype.NUM)) != 0
                ? Dtype.BOOL
                : Dtype.TYPELESS;
        }

        public static JNode Uminus(JNode x)
        {
            if (x.value is long l)
                return new JNode(-l);
            if (x.value is double d)
                return new JNode(-d);
            if (x.value is bool b)
                return new JNode(b ? -1L : 0L);
            throw new RemesPathException("Unary '-' can only be applied to ints, bools and floats");
        }

        /// <summary>
        /// returns a JNode containing the opposite of whatever ArgFunction.ToBoolHelper would return (see below)
        /// </summary>
        public static JNode Not(JNode x)
        {
            return new JNode(!ArgFunction.ToBoolHelper(x));
        }

        public static JNode Uplus(JNode x)
        {
            if (x.value is bool b)
                return new JNode(b ? 1L : 0L);
            if (x.value is long l)
                return new JNode(l);
            if (x.value is double d)
                return new JNode(d);
            throw new RemesPathException("Unary '-' can only be applied to ints, bools and floats");
        }
    }

    /// <summary>
    /// functions with arguments in parens, e.g. mean(x), index(arr, elt), sort_by(arr, key, reverse)
    /// </summary>
    public class ArgFunction
    {
        /// <summary>
        /// the function associated with this ArgFunction
        /// </summary>
        public Func<List<JNode>, JNode> Call { get; private set; }
        private Dtype[] inputTypes;
        public string name;
        public Dtype type;
        public int maxArgs;
        public int minArgs;
        /// <summary>
        /// the function is applied to all elements in the first argument
        /// if the first argument is an iterable.
        /// </summary>
        public bool isVectorized;
        /// <summary>
        /// if false, the function is random
        /// </summary>
        public bool isDeterministic;
        /// <summary>
        /// transforms arguments at compile time
        /// </summary>
        public ArgsTransform argsTransform;
        /// <summary>
        /// iff true, one or more of this function's arguments is evaluated after the function is called, not before<br></br>
        /// This allows for things like the ifelse function imitating the ternary operator<br></br>
        /// and the and() and or() functions short-circuiting the same way as the corresponding binops in Python.
        /// </summary>
        public bool conditionalExecution { get; private set; }

        /// <summary>
        /// A function whose arguments must be given in parentheses (e.g., len(x), concat(x, y), s_mul(abc, 3).<br></br>
        /// inputTypes[ii] is the type that the i^th argument to a function must have,
        /// unless the function has arbitrarily many arguments (indicated by max_args = int.MaxValue),
        /// in which case the last value of Input_types is the type that all optional arguments must have.
        /// </summary>
        /// <param name="function">the function associated with this argFunction</param>
        /// <param name="name">the name used in RemesPath to call it</param>
        /// <param name="type">the type(s) returned by the function</param>
        /// <param name="minArgs">the minimum number of args</param>
        /// <param name="maxArgs">the maximum number of args</param>
        /// <param name="isVectorized">if true, the function is applied to each value in an object or each child of an array, rather than to the object/array itself (this applies only to the first arg)</param>
        /// <param name="inputTypes">must have maxArgs values (the i^th value indicates the acceptable types for the i^th arg), or minArgs + 1 values if maxArgs is int.MaxValue</param>
        /// <param name="isDeterministic">if false, the function outputs a random value</param>
        /// <param name="argsTransform">transformations that are applied to any number of arguments at compile time</param>
        /// <param name="conditionalExecution">whether the function's excution of one or more arguments is conditional on something</param>
        public ArgFunction(Func<List<JNode>, JNode> function,
            string name,
            Dtype type,
            int minArgs,
            int maxArgs,
            bool isVectorized,
            Dtype[] inputTypes,
            bool isDeterministic = true,
            ArgsTransform argsTransform = null,
            bool conditionalExecution = false)
        {
            Call = function;
            this.name = name;
            this.type = type;
            this.maxArgs = maxArgs;
            this.minArgs = minArgs;
            this.isVectorized = isVectorized;
            this.inputTypes = inputTypes;
            this.isDeterministic = isDeterministic;
            this.argsTransform = argsTransform;
            this.conditionalExecution = conditionalExecution;
        }

        /// <summary>
        /// valid type options for a given argument number to the function
        /// </summary>
        /// <param name="argNum"></param>
        /// <returns></returns>
        public Dtype TypeOptions(int argNum)
        {
            return (argNum >= inputTypes.Length
                ? inputTypes[inputTypes.Length - 1] // last type in inputTypes applies to all optional args for funcs with arbitrary numbers of args
                : inputTypes[argNum])
                | Dtype.UNKNOWN; // UNKNOWN is always a valid argument type.
        }
        
        public override string ToString()
        {
            return $"ArgFunction({name}, {type})";
        }

        /// <summary>
        /// Vectorized functions take on the type of the iterable they're vectorized across,
        /// but they have a set type when operating on scalars<br></br>
        /// (e.g. s_len returns an array when acting on an array
        /// and an object when operating on an object,
        /// but s_len always returns an int when acting on a single string).<br></br>
        /// Non-vectorized functions always return the same type.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public Dtype OutputType(JNode x)
        {
            return isVectorized && ((x.type & Dtype.ITERABLE) != 0) ? x.type : type;
        }

        /// <summary>
        /// if x is null or its type is not one of the appropriate types for argument argNum to this function,
        /// throw a RemesPathArgumentException
        /// </summary>
        /// <param name="x"></param>
        /// <param name="argNum"></param>
        /// <exception cref="RemesPathArgumentException"></exception>
        public void CheckType(JNode x, int argNum)
        {
            Dtype typeOptions = TypeOptions(argNum);
            if (x == null || (x.type & typeOptions) == 0)
            {
                Dtype argType = x == null ? Dtype.NULL : x.type;
                throw new RemesPathArgumentException(null, argNum, this, argType);
            }
        }

        /// <summary>
        /// throw a RemesPathException because count arguments were given to this function,
        /// which requires between minArgs and maxArgs args.
        /// </summary>
        /// <param name="count"></param>
        /// <exception cref="RemesPathException"></exception>
        public void ThrowWrongArgCount(int count)
        {
            char correctCharAfterArg = count == maxArgs ? ')' : ',';
            string numArgsDescription = (minArgs == maxArgs) ? $"({maxArgs} args)" : $"({minArgs} - {maxArgs} args)";
            throw new RemesPathException($"Expected '{correctCharAfterArg}' after argument {count} of function {name} {numArgsDescription}");
        }

        /// <summary>
        /// for functions that have a fixed number of optional args, pad the unfilled args with null nodes
        /// </summary>
        public void PadToMaxArgs(List<JNode> args)
        {
            int argNum = args.Count;
            if (maxArgs < int.MaxValue)
            {
                for (int arg2 = argNum; arg2 < maxArgs; arg2++)
                {
                    args.Add(new JNode());
                }
            }
        }

        /// <summary>
        /// there are currently three mutable public global variables that are set in queries:<br></br>
        /// regexSearchResultsShouldBeCached, csvDelimiterInLastQuery, and csvQuoteCharInLastQuery.<br></br>
        /// These all relate to s_csv and s_fa.<br></br>
        /// This is basically a hack, because RemesPath does not currently support deep top-down introspection of a query's AST
        /// to determine if s_csv or s_fa has been called in a certain way.
        /// </summary>
        /// <param name="containsMutation"></param>
        public static void InitializeGlobals(bool containsMutation)
        {
            regexSearchResultsShouldBeCached = !containsMutation;
            csvDelimiterInLastQuery = '\x00';
            csvQuoteCharInLastQuery = '\x00';
        }

        #region NON_VECTORIZED_ARG_FUNCTIONS

        /// <summary>
        /// and(x: anything, y: anything, ...: anything) -> bool<br></br>
        /// requires at least two arguments.<br></br>
        /// If any of the arguments are not "truthy", returns false. Else returns true.<br></br>
        /// This function employs conditional execution, so for instance<br></br>
        /// and(is_str(@), s_len(@) &lt; 4) will work, because "s_len(@) &lt; 4" is only called if "is_str(@)" returns true (meaning the current json must be a string in that branch)
        /// </summary>
        public static JNode And(List<JNode> args)
        {
            int lastIdx = args.Count - 1;
            JNode curjson = args[lastIdx];
            for (int ii = 0; ii < lastIdx; ii++)
            {
                JNode val = args[ii];
                JNode resolvedVal = val is CurJson cj ? cj.function(curjson) : val;
                if (!ToBoolHelper(resolvedVal))
                    return new JNode(false);
            }
            return new JNode(true);
        }

        /// <summary>
        /// or(x: anything, y: anything, ...: anything) -> bool<br></br>
        /// requires at least two arguments.<br></br>
        /// If any of the arguments are "truthy", returns true. Else returns false.<br></br>
        /// This function employs conditional execution, so for instance<br></br>
        /// or(not is_str(@), s_len(@) &lt; 4) will work, because "s_len(@) &lt; 4" is not called if "not is_str(@)" returns true.
        /// </summary>
        public static JNode Or(List<JNode> args)
        {
            int lastIdx = args.Count - 1;
            JNode curjson = args[lastIdx];
            for (int ii = 0; ii < lastIdx; ii++)
            {
                JNode val = args[ii];
                JNode resolvedVal = val is CurJson cj ? cj.function(curjson) : val;
                if (ToBoolHelper(resolvedVal))
                    return new JNode(true);
            }
            return new JNode(false);
        }

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
        public static JNode In(List<JNode> args)
        {
            JNode elt = args[0];
            JNode itbl = args[1];
            if (itbl is JArray arr)
            {
                foreach (JNode node in arr.children)
                {
                    if (node.CompareTo(elt.value) == 0)
                    {
                        return new JNode(true);
                    }
                }
                return new JNode(false);
            }
            if (!(elt.value is string k))
            {
                throw new RemesPathException("'in' function first argument must be string if second argument is an object.");
            }
            bool itblHasKey = ((JObject)itbl).children.ContainsKey(k);
            return new JNode(itblHasKey);
        }

        private static (JNode elt, char printStyle, bool sortKeys, int indent, char indentChar) HandleStringifyArgs(List<JNode> args)
        {
            JNode elt = args[0];
            char printStyle = args[1].type == Dtype.NULL ? 'm' : ((string)args[1].value)[0];
            bool sortKeys = args[2].type == Dtype.NULL || (bool)args[2].value;
            int indent = 4;
            char indentChar = ' ';
            IComparable arg3val = args[3].value;
            if (arg3val is long l)
                indent = Convert.ToInt32(l);
            else if (arg3val is string s && s[0] is char c && c == '\t')
            {
                indent = 1;
                indentChar = c;
            }
            return (elt, printStyle, sortKeys, indent, indentChar);
        }

        /// <summary>
        /// stringify(elt: anything, print_style: string=m, sort_keys: bool=true, indent: int | str=4) -> str<br></br>
        /// For the pretty-printed forms, spaces are used to indent unless '\t' is passed as the indent argument.<br></br>
        /// returns the string representation of elt according to the following rules:<br></br>
        /// * if print_style is 'm', return the minimal-whitespace compact representation<br></br>
        /// * if print_style is 'c', return the Python-style compact representation (one space after ',' or ':')<br></br>
        /// * if print_style is 'g', return the Google-style pretty-printed representation<br></br>
        /// * if print_style is 'w', return the Whitesmith-style pretty-printed representation<br></br>
        /// * if print_style is 'p', return the PPrint-style pretty-printed representation<br></br>
        /// </summary>
        public static JNode Stringify(List<JNode> args)
        {
            (JNode elt, char printStyle, bool sortKeys, int indent, char indentChar) = HandleStringifyArgs(args);
            switch (printStyle)
            {
            case 'm': return new JNode(elt.ToString(sortKeys, ":", ","));
            case 'c': return new JNode(elt.ToString(sortKeys));
            case 'g': return new JNode(elt.PrettyPrint(indent, sortKeys, PrettyPrintStyle.Google, int.MaxValue, indentChar));
            case 'w': return new JNode(elt.PrettyPrint(indent, sortKeys, PrettyPrintStyle.Whitesmith, int.MaxValue, indentChar));
            case 'p': return new JNode(elt.PrettyPrint(indent, sortKeys, PrettyPrintStyle.PPrint, int.MaxValue, indentChar));
            default: throw new RemesPathArgumentException($"print_style (2nd arg of stringify) must be one of \"mcgwp\", got {JsonLint.CharDisplay(printStyle)}", 1, FUNCTIONS["stringify"]);
            }
        }

        /// <summary>
        /// type(elt: anything) -> str<br></br>
        /// get the JSON schema type name associated with a JSON variable (e.g. ints -> "integer", bools -> "boolean")
        /// </summary>
        public static JNode TypeOf(List<JNode> args)
        {
            return new JNode(JsonSchemaMaker.TypeName(args[0].type));
        }

        /// <summary>
        /// Assuming first arg is a dictionary or list, return the number of elements it contains.
        /// </summary>
        public static JNode Len(List<JNode> args)
        {
            var itbl = args[0];
            if (itbl is JArray arr)
            {
                return new JNode(Convert.ToInt64(arr.Length));
            }
            return new JNode(Convert.ToInt64(((JObject)itbl).Length));
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the sum, cast to a double.
        /// </summary>
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
            return new JNode(tot);
        }

        /// <summary>
        /// Assumes args has one element, a list of numbers. Returns the arithmetic mean of that list.
        /// </summary>
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
            return new JNode(tot / itbl.Length);
        }

        /// <summary>
        /// Requires array input.<br></br>
        /// Like the Python enumerate() function (except not lazy),<br></br>
        /// returns an array of subarrays where the first item is an index
        /// and the second item is the item at that index<br></br>
        /// Example:<br></br>
        /// enumerate(["a", "b", "c"]) returns [[0, "a"], [1, "b"], [2, "c"]]
        /// </summary>
        public static JNode Enumerate(List<JNode> args)
        {
            var arr = (JArray)args[0];
            var result = new List<JNode>(arr.Length);
            for (int ii = 0; ii < arr.Length; ii++)
            {
                var subarr = new JArray(0,
                    new List<JNode>
                    {
                        new JNode((long)ii),
                        arr[ii]
                    }
                );
                result.Add(subarr);
            }
            return new JArray(0, result);
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
                    if (child is JArray childArr)
                    {
                        foreach (JNode grandchild in childArr.children)
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
        /// <exception cref="KeyNotFoundException"></exception>
        public static JNode Index(List<JNode> args)
        {
            var itbl = ((JArray)args[0]).children;
            var elt = args[1];
            var reverse = args[2].value;
            (int start, int end, int increment) = (reverse != null && (bool)reverse)
                ? (itbl.Count - 1, -1, -1)
                : (0, itbl.Count, 1);
            for (int ii = start; ii != end; ii += increment)
            {
                if (itbl[ii].CompareTo(elt) == 0)
                { 
                    return new JNode(Convert.ToInt64(ii));
                }
            }
            throw new KeyNotFoundException($"Element {elt} not found in the array {itbl}");
        }

        /// <summary>
        /// max(x: array[float] -> float<br></br>
        /// Returns the largest number in x (an array of numbers) cast to a double.
        /// </summary>
        public static JNode Max(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            double biggest = NanInf.neginf;
            foreach (JNode child in itbl.children)
            {
                if ((child.type & Dtype.NUM) == 0)
                {
                    throw new RemesPathException("Function Max requires an array of all numbers");
                }
                double val = Convert.ToDouble(child.value);
                if (val > biggest)
                    biggest = val;
            }
            return new JNode(biggest);
        }

        /// <summary>
        /// max(x: array[float] -> float<br></br>
        /// Returns the smallest number in x (an array of numbers) cast to a double.
        /// </summary>
        public static JNode Min(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            double smallest = NanInf.inf;
            foreach (JNode child in itbl.children)
            {
                if ((child.type & Dtype.NUM) == 0)
                {
                    throw new RemesPathException("Function Min requires an array of all numbers");
                }
                double val = Convert.ToDouble(child.value);
                if (val < smallest)
                    smallest = val;
            }
            return new JNode(smallest);
        }

        /// <summary>
        /// sorted(x: array, reverse: boolean = false) -> array<br></br>
        /// return a copy of x, sorted ascending (or descending iff `reverse` is true)
        /// </summary>
        public static JNode Sorted(List<JNode> args)
        {
            var arr = ((JArray)args[0]).children;
            var sorted = new List<JNode>(arr);
            var reverse = args[1].value;
            sorted.Sort();
            if (reverse != null && (bool)reverse)
            {
                sorted.Reverse();
            }
            return new JArray(0, sorted);
        }

        /// <summary>
        /// x must be a JArray.<br></br>
        /// Get the element at x[idx] (allowing idx to be negative as normal for Python indices).<br></br>
        /// If idx is out of range (idx >= x.Length or idx < -x.Length), throw a RemesPathIndexOutOfRangeException.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        /// <exception cref="RemesPathIndexOutOfRangeException"></exception>
        public static JNode AtIndex(JNode x, int idx)
        {
            if (!((JArray)x).children.WrappedIndex(idx, out JNode atKint))
                throw new RemesPathIndexOutOfRangeException(idx, x);
            return atKint;
        }

        /// <summary>
        /// sort_by(arr: array, key: function | integer | string, reverse: bool=false) -> array<br></br>
        /// return a copy of arr sorted by one of the following:<br></br>
        /// * if key is an integer, assume that each element of arr is also an array, and sort by subarr[key] for each subarray subarr of arr<br></br>
        /// * if key is an string, assume that each element of arr is an object, and sort by subobj[key] for each subobject subobj of arr<br></br>
        /// * if key is a function (CurJson), sort by key(child) for each child of arr
        /// </summary>
        public static JNode SortBy(List<JNode> args)
        {
            var arr = ((JArray)args[0]).children;
            var reverse = args[2].value;
            var keyNode = args[1];
            List<JNode> sorted;
            if (keyNode is CurJson cj)
            {
                sorted = arr.OrderBy(cj.function).ToList();
            }
            else
            {
                var key = keyNode.value;
                if (key is string kstr)
                {
                    sorted = arr.OrderBy(x => ((JObject)x).children[kstr]).ToList();
                }
                else
                {
                    int kint = Convert.ToInt32(key);
                    sorted = arr.OrderBy(x => AtIndex(x, kint)).ToList();
                }
            }
            if (reverse != null && (bool)reverse)
            {
                sorted.Reverse();
            }
            return new JArray(0, sorted);
        }

        /// <summary>
        /// max_by(arr: array, key: function | integer | string) -> anything<br></br>
        /// return the largest element of arr according to some number-valued function of each element:<br></br>
        /// * if key is an integer, assume that each element of arr is also an array, and find the child subarr of arr for which subarr[key] is largest<br></br>
        /// * if key is an string, assume that each element of arr is an object, and find the child subobj of arr for which subobj[key] is largest<br></br>
        /// * if key is a function (CurJson), find child of arr such that key(child) is larges
        /// </summary>
        public static JNode MaxBy(List<JNode> args)
        {
            var itbl = ((JArray)args[0]).children;
            var max = new JNode();
            if (itbl.Count == 0)
                return max;
            var keyNode = args[1];
            double maxval = NanInf.neginf;
            if (keyNode is CurJson cj)
            {
                Func<JNode, JNode> fun = cj.function;
                foreach (JNode x in itbl)
                {
                    var xval = Convert.ToDouble(fun(x).value);
                    if (xval > maxval)
                    {
                        max = x;
                        maxval = xval;
                    }
                }
                return max;
            }
            var key = keyNode.value;
            if (key is string keystr)
            {
                for (int ii = 0; ii < itbl.Count; ii++)
                {
                    var x = (JObject)itbl[ii];
                    var xval = Convert.ToDouble(x[keystr].value);
                    if (xval > maxval)
                    {
                        max = x;
                        maxval = xval;
                    }
                }
            }
            else
            {
                int kint = Convert.ToInt32(key);
                for (int ii = 0; ii < itbl.Count; ii++)
                {
                    var x = (JArray)itbl[ii];
                    var xval = Convert.ToDouble(AtIndex(x, kint).value);
                    if (xval > maxval)
                    {
                        max = x;
                        maxval = xval;
                    }
                }
            }
            return max;
        }

        /// <summary>
        /// min_by(arr: array, key: function | integer | string) -> anything<br></br>
        /// as max_by, but replace "maximize" with "minimize" everywhere in the description
        /// </summary>
        public static JNode MinBy(List<JNode> args)
        {
            var itbl = ((JArray)args[0]).children;
            var min = new JNode();
            if (itbl.Count == 0)
                return min;
            var keyNode = args[1];
            double minval = NanInf.inf;
            if (keyNode is CurJson cj)
            {
                Func<JNode, JNode> fun = cj.function;
                foreach (JNode x in itbl)
                {
                    var xval = Convert.ToDouble(fun(x).value);
                    if (xval < minval)
                    {
                        min = x;
                        minval = xval;
                    }
                }
                return min;
            }
            var key = keyNode.value;
            if (key is string keystr)
            {
                for (int ii = 0; ii < itbl.Count; ii++)
                {
                    var x = (JObject)itbl[ii];
                    var xval = Convert.ToDouble(x[keystr].value);
                    if (xval < minval)
                    {
                        min = x;
                        minval = xval;
                    }
                }
            }
            else
            {
                int kint = Convert.ToInt32(key);
                for (int ii = 0; ii < itbl.Count; ii++)
                {
                    var x = (JArray)itbl[ii];
                    var xval = Convert.ToDouble(AtIndex(x, kint).value);
                    if (xval < minval)
                    {
                        min = x;
                        minval = xval;
                    }
                }
            }
            return min;
        }

        /// <summary>
        /// Returns a random JNode with a double from 0 (inclusive) to 1 (exclusive).
        /// </summary>
        public static JNode RandomFrom0To1(List<JNode> args)
        {
            double rand = RandomJsonFromSchema.random.NextDouble();
            return new JNode(rand, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// randint(start: int, end: int=null)  -> int<br></br>
        /// returns a random integer from start (inclusive) to end (exclusive)<br></br>
        /// If end is not provided, returns a random integer from 0 (inclusive) to start (exclusive)
        /// </summary>
        public static JNode RandomInteger(List<JNode> args)
        {
            JNode startNode = args[0];
            if (startNode.type != Dtype.INT)
                throw new RemesPathArgumentException("both args to randint must be integers", 0, FUNCTIONS["randint"]);
            int start = Convert.ToInt32(startNode.value);
            JNode endNode = args[1];
            if (endNode.type == Dtype.NULL)
                return new JNode((long)RandomJsonFromSchema.random.Next(start));
            if (endNode.type != Dtype.INT)
                throw new RemesPathArgumentException("both args to randint must be integers", 1, FUNCTIONS["randint"]);
            int end = Convert.ToInt32(endNode.value);
            return new JNode((long)RandomJsonFromSchema.random.Next(start, end));
        }

        /// <summary>
        /// this is the value returned by the LoopCount function below.<br></br>
        /// If you are writing a function that would allow users to call the loop() function<br></br>
        /// <i>do not just increment the value before every time a CurJson is called.</i><br></br>
        /// Instead, you should use the following pattern:<br></br>
        /// <b>var loopCountBeforeCall = loopCountValue;<br></br>
        /// var something = curjson.function(inp); // calling this function could mutate loopCountValue!<br></br>
        /// loopCountValue = loopCountBeforeCall + 1; // this ensures that the next time curjson.function(inp) is called,
        /// it sees loopCountValue = 1 + the last value it saw</b>
        /// </summary>
        private static long loopCountValue = -1;

        /// <summary>
        /// loop() -> int<br></br>
        /// returns 1 + the current number of iterations of some repetitive process<br></br>
        /// THIS FUNCTION ALWAYS RETURNS -1 UNLESS CALLED IN THE FOLLOWING CONTEXT(S):<br></br>
        /// * the callback (3rd argument) of the s_sub function, if the 2nd argument is a regex.
        /// EXAMPLE:<br></br>
        /// s_sub(`a b c` , g`[a-z]`, @[0]*loop()) returns "a bb ccc"<br></br>
        /// because there are three matches, "a", "b", and "c", and loop() returns 1 on the first match, 2 on the second, and 3 on the third 
        /// </summary>
        /// <exception cref="RemesPathException"></exception>
        public static JNode LoopCount(List<JNode> args)
        {
            return new JNode(loopCountValue);
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
            var vals = new JArray(0, ((JObject)args[0]).children.Values.ToList());
            return vals;
        }

        public static JNode Keys(List<JNode> args)
        {
            var ks = new JArray();
            foreach (string s in ((JObject)args[0]).children.Keys)
            {
                ks.children.Add(new JNode(s));
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
        public static JNode Items(List<JNode> args)
        {
            var its = new List<JNode>();
            JObject obj = (JObject)args[0];
            foreach (KeyValuePair<string, JNode> kv in obj.children)
            {
                JNode knode = new JNode(kv.Key, Dtype.STR, 0);
                var subarr = new List<JNode> { knode, kv.Value };
                its.Add(new JArray(0, subarr));
            }
            return new JArray(0, its);
        }

        /// <summary>
        /// set(x: array) -> object<br></br>
        /// returns an object where the keys are all the stringified unique values in x and the values are all null.
        /// </summary>
        public static JNode Set(List<JNode> args)
        {
            List<JNode> arr = ((JArray)args[0]).children;
            var set = new Dictionary<string, JNode>();
            foreach (JNode child in arr)
            {
                string key = child.ValueOrToString();
                set[key] = new JNode();
            }
            return new JObject(0, set);
        }

        /// <summary>
        /// unique(x: array, is_sorted: bool=false) -> array<br></br>
        /// returns an array of all the distinct values in x (they must all be scalars).<br></br>
        /// If is_sorted, the returned array is sorted. Otherwise the order is random.
        /// </summary>
        public static JNode Unique(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            var isSorted = args[1].value;
            var uniq = new HashSet<object>();
            foreach (JNode val in itbl.children)
            {
                if ((val.type & Dtype.SCALAR) == 0)
                    throw new RemesPathArgumentException("First argument to unique must be an array of all scalars.", 0, FUNCTIONS["unique"]);
                uniq.Add(val.value);
            }
            var uniqList = new List<JNode>();
            foreach (object val in uniq)
            {
                uniqList.Add(ObjectsToJNode(val));
            }
            if (isSorted != null && (bool)isSorted)
            {
                uniqList.Sort();
            }
            return new JArray(0, uniqList);
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
            int lowerInd = Convert.ToInt32(Math.Floor(ind));
            double weightedAvg;
            double lowerVal = sorted[lowerInd];
            if (ind != lowerInd)
            {
                double upperVal = sorted[lowerInd + 1];
                double fracUpper = ind - lowerInd;
                weightedAvg = upperVal * fracUpper + lowerVal * (1 - fracUpper);
            }
            else
            {
                weightedAvg = lowerVal;
            }
            return new JNode(weightedAvg, Dtype.FLOAT, 0);
        }

        /// <summary>
        /// args[0] should be an array of scalars.<br></br>
        /// args[1] should be a bool. If true, sort by count descending.<br></br>
        /// Finds an array of sub-arrays, where each sub-array is an element-count pair, where the count is of that element in args[0].<br></br>
        /// EXAMPLES:
        /// * ValueCounts([2, 1, "a", "a", 1]) returns [[2, 1], [1, 2], ["a", 2]]
        /// * ValueCounts(["a", "b", "c", "c", "c", "b"], true) returns [["c", 3], ["b", 2], ["a", 1]]
        /// </summary>
        public static JNode ValueCounts(List<JNode> args)
        {
            var arr = (JArray)args[0];
            var sortByCount = args.Count > 1 && (args[1].value is bool sbc)
                && sbc;
            var uniqs = new Dictionary<object, long>();
            foreach (JNode child in arr.children)
            {
                object val = child.value;
                if (val == null)
                {
                    throw new RemesPathException("Can't count occurrences of objects with null values");
                }
                if (!uniqs.ContainsKey(val))
                    uniqs[val] = 0L;
                uniqs[val]++;
            }
            var uniqArr = uniqs.Keys
                .Select(k => (JNode)new JArray(0, new List<JNode>
                {
                        ObjectsToJNode(k),
                        new JNode(uniqs[k], Dtype.INT, 0)
                }))
                .ToList();
            if (sortByCount)
            {
                uniqArr.Sort(
                    (a, b) => (((JArray)a)[1]).CompareTo(((JArray)b)[1])
                );
                uniqArr.Reverse();
            }
            return new JArray(0, uniqArr);
        }

        /// <summary>
        /// s_cat(x: anything, ...: anything) -> string<br></br>
        /// appends the string representations (or values, in the case of strings) of any number of JSON elements together. NOT VECTORIZED.<br></br>
        /// EXAMPLES:<br></br>
        /// * with input [[1, 2], {"b": "bar"}], s_cat(@[0], ` `, foo, bar, baz, @[1].b) returns "[1, 2] foobarbaz{\"b\": \"bar\"}"
        /// </summary>
        public static JNode StrCat(List<JNode> args)
        {
            var sb = new StringBuilder();
            foreach (JNode arg in args)
            {
                sb.Append(arg.ValueOrToString());
            }
            return new JNode(sb.ToString());
        }

        public static JNode StringJoin(List<JNode> args)
        {
            var itbl = (JArray)args[1];
            if (itbl.Length == 0)
                return new JNode("", Dtype.STR, 0);
            string sep = (string)args[0].value;
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
        /// Second arg (key) must be a JNode with integer or string value OR an array of such JNodes<br></br>
        /// <b>If key is an array of length greater than 1:</b><br></br>
        /// * Returns a new JObject where each value of the n^th subkey in key is mapped to itbl recursively grouped by subkeys n+1, n+2,... in key.<br></br>
        /// <b>If key is NOT an array</b> (or an array of length 1)<br></br>
        /// * Returns a new JObject of the form described for GroupBySingleKey below.<br></br>
        /// EXAMPLES<br></br>
        /// * See GroupBySingleKey examples for when key is integer or string.<br></br>
        /// * GroupBy([{"a": 1, "b": "x", "c": -0.5}, {"a": 1, "b": "y", "c": 0.0}, {"a": 2, "b": "x", "c": 0.5}], ["a", "b"]) returns:<br></br>
        /// {"1": {"x": [{"a": 1, "b": "x", "c": -0.5}], "y": [{"a": 1, "b": "y", "c": 0.0}]}, "2": {"x": [{"a": 2, "b": "x", "c": 0.5}]}}<br></br>
        /// * GroupBy([[1, "x", -0.5], [1, "y", 0.0], [2, "x", 0.5]], [1, 0]) returns:<br></br>
        /// {"x": {"1": [[1, "x", -0.5]], "2": [[2, "x", 0.5]]}, "y": {"1": [[1, "y", 0.0]]}}<br></br>
        /// * GroupBy([[1, 2, 2, 0.0], [1, 2, 3, -1.0], [1, 3, 3, -2.0], [1, 3, 4, -3.0], [2, 2, 2, -4.0]], [0, 1, 2]) returns:<br></br>
        /// {"1": {"2": {"2": [[1, 2, 2, 0.0]], "3": [[1, 2, 3, -1.0]]}, "3": {"3": [[1, 3, 3, -2.0]], "4": [[1, 3, 4, -3.0]]}}, "2": {"2": {"2": [[2, 2, 2, -4.0]]}}}
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static JNode GroupBy(List<JNode> args)
        {
            var itbl = (JArray)args[0];
            JNode keyNode = args[1];
            if (keyNode is JArray arr)
                return GroupByRecursionHelper(itbl, arr, 0);
            return GroupBySingleKey(itbl, keyNode);
        }

        public static JNode GroupByRecursionHelper(JArray itbl, JArray keys, int currentIdx)
        {
            JNode key = keys[currentIdx];
            if (currentIdx == keys.Length - 1)
                return GroupBySingleKey(itbl, key);
            var gb = new Dictionary<string, JNode>();
            if (key.value is long l)
            {
                int i = Convert.ToInt32(l);
                foreach (JNode child in itbl.children)
                {
                    AddToGroupByDict(gb, i, child);
                }
            }
            else if (key.value is string s)
            {
                foreach (JNode child in itbl.children)
                {
                    AddToGroupByDict(gb, s, child);
                }
            }
            var recursiveGb = new Dictionary<string, JNode>();
            foreach (string groupKey in gb.Keys)
            {
                JNode group = gb[groupKey];
                recursiveGb[groupKey] = GroupByRecursionHelper((JArray)group, keys, currentIdx + 1);
            }
            return new JObject(0, recursiveGb);
        }

        /// <summary>
        /// Returns a new JObject where each entry in itbl is grouped into a separate JArray under the stringified form
        /// of the value associated with key/index key in itbl.
        /// EXAMPLE<br></br>
        /// * GroupBy([{"foo": 1, "bar": "a"}, {"foo": 2, "bar": "b"}, {"foo": 3, "bar": "a"}], "bar") returns:<br></br>
        /// {"a": [{"foo": 1, "bar": "a"}, {"foo": 3, "bar": "a"}], "b": [{"foo": 2, "bar": "b"}]}<br></br>
        /// * GroupBy([[1, "a"], [2, "b"], [2, "c"], [3, "d"]], 0) returns:<br></br>
        /// {"1": [[1, "a"]], "2": [[2, "b"], [2, "c"]], "3": [[3, "d"]]}
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private static JNode GroupBySingleKey(JArray itbl, JNode keyNode)
        {
            object key = keyNode.value;
            if (!(key is string || key is long))
            {
                throw new ArgumentException("The GroupBy function can only group by string keys or int indices");
            }
            var gb = new Dictionary<string, JNode>();
            if (key is long l)
            {
                int ikey = Convert.ToInt32(l);
                foreach (JNode child in itbl.children)
                {
                    AddToGroupByDict(gb, ikey, child);
                }
            }
            else if (key is string skey)
            {
                foreach (JNode child in itbl.children)
                {
                    AddToGroupByDict(gb, skey, child);
                }
            }
            else
                throw new RemesPathArgumentException("group_by keys must be integers or strings (or an array thereof)", 1, FUNCTIONS["group_by"]);
            return new JObject(0, gb);
        }

        private static void AddToGroupByDict(Dictionary<string, JNode> gb, int ikey, JNode child)
        {
            JArray subobj = (JArray)child;
            JNode val = AtIndex(subobj, ikey);
            string vstr = val.type == Dtype.STR ? (string)val.value : val.ToString();
            if (gb.ContainsKey(vstr))
            {
                ((JArray)gb[vstr]).children.Add(subobj);
            }
            else
            {
                gb[vstr] = new JArray(0, new List<JNode> { subobj });
            }
        }

        private static void AddToGroupByDict(Dictionary<string, JNode> gb, string skey, JNode child)
        {
            JObject subobj = (JObject)child;
            JNode val = subobj[skey];
            string vstr = val.type == Dtype.STR ? (string)val.value : val.ToString();
            if (gb.ContainsKey(vstr))
            {
                ((JArray)gb[vstr]).children.Add(subobj);
            }
            else
            {
                gb[vstr] = new JArray(0, new List<JNode> { subobj });
            }
        }

        /// <summary>
        /// Takes 2-8 JArrays as arguments. They must be of equal length.
        /// Returns: one JArray in which the i^th element is an array containing the i^th elements of the input arrays.
        /// Example:
        /// Zip([1,2],["a", "b"], [true, false]) = [[1, "a", true], [2, "b", false]]
        /// </summary>
        public static JNode Zip(List<JNode> args)
        {
            var firstArr = ((JArray)args[0]);
            int firstLen = firstArr.Length;
            if (!args.All(x => x is JArray arr && arr.Length == firstLen))
                throw new RemesPathException("The `zip` function expects all input arrays to have equal length");
            var rst = new List<JNode>(firstLen);
            for (int ii = 0; ii < firstLen; ii++)
            {
                rst.Add(new JArray(0, args.Select(x => ((JArray)x)[ii]).ToList()));
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
        public static JNode Dict(List<JNode> args)
        {
            JArray pairs = (JArray)args[0];
            Dictionary<string, JNode> rst = new Dictionary<string, JNode>(pairs.Length);
            for (int ii = 0; ii < pairs.Length; ii++)
            {
                JArray pair = (JArray)pairs[ii];
                JNode key = pair[0];
                JNode val = pair[1];
                if (!(key.value is string k))
                    throw new RemesPathException("The Dict function's argument must be an array of two-item subarrays where the first item of each subarray is a string.");
                rst[k] = val;
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
        /// <exception cref="RemesPathException"></exception>
        public static JNode Concat(List<JNode> args)
        {
            JNode firstItbl = args[0];
            if (firstItbl is JArray firstArr)
            {
                List<JNode> newArr = new List<JNode>(firstArr.children);
                for (int ii = 1; ii < args.Count; ii++)
                {
                    if (!(args[ii] is JArray arr))
                        throw new RemesPathException("All arguments to the 'concat' function must the same type - either arrays or objects");
                    newArr.AddRange(arr.children);
                }
                return new JArray(0, newArr);
            }
            else if (firstItbl is JObject firstObj)
            {
                Dictionary<string, JNode> newObj = new Dictionary<string, JNode>(firstObj.children);
                for (int ii = 1; ii < args.Count; ii++)
                {
                    if (!(args[ii] is JObject obj))
                        throw new RemesPathException("All arguments to the 'concat' function must the same type - either arrays or objects");
                    foreach (KeyValuePair<string, JNode> kv in obj.children)
                    {
                        // this can overwrite the same key in any earlier arg.
                        // TODO: maybe consider raising if multiple iterables have same key?
                        newObj[kv.Key] = kv.Value;
                    }
                }
                return new JObject(0, newObj);
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
        /// <exception cref="RemesPathException"></exception>
        public static JNode Append(List<JNode> args)
        {
            JArray arr = (JArray)args[0];
            List<JNode> newArr = new List<JNode>(arr.children);
            for (int ii = 1; ii < args.Count; ii++)
            {
                JNode arg = args[ii];
                newArr.Add(arg);
            }
            return new JArray(0, newArr);
        }

        /// <summary>
        /// At(json: array | object, inds_or_keys: string | integer | array) -> anything<br></br>
        /// first arg must be an object or array.<br></br>
        /// second arg must be an integer, a string, an array of integers, or an array of strings.<br></br>
        /// Assuming json is an array, returns json[ind] for each integer index in inds_or_keys<br></br>
        /// Assuming json is an object, returns json[key] for each string key in inds_or_keys<br></br>
        /// EXAMPLES<br></br>
        /// at([1, 2, 3], 0) -> 1<br></br>
        /// at(["foo", "bar", "baz"], [-1, 0]) -> ["baz", "foo"]<br></br>
        /// at({"foo": 1, "bar": 2}, "bar") -> 2<br></br>
        /// at({"foo": 1, "bar": 2}, ["bar", "foo"]) -> [2, 1]
        /// </summary>
        public static JNode At(List<JNode> args)
        {
            JNode json = args[0];
            JNode indsOrKeys = args[1];
            if (json is JArray arr)
            {
                var children = arr.children;
                int ind;
                if (indsOrKeys is JArray indArr)
                {
                    var atList = new List<JNode>();
                    foreach (JNode indNode in indArr.children)
                    {
                        ind = Convert.ToInt32(indNode.value);
                        if (ArrayExtensions.WrappedIndex(children, ind, out JNode atInd))
                            atList.Add(atInd);
                        else
                            throw new RemesPathIndexOutOfRangeException(ind, arr);
                    }
                    return new JArray(0, atList);
                }
                ind = Convert.ToInt32(indsOrKeys.value);
                if (ArrayExtensions.WrappedIndex(children, ind, out JNode atIndsOrKeys))
                    return atIndsOrKeys;
                else
                    throw new RemesPathIndexOutOfRangeException(ind, arr);
            }
            else if (json is JObject obj)
            {
                var children = obj.children;
                if (indsOrKeys is JArray indArr)
                {
                    return new JArray(0, indArr.children.Select(key => children[(string)key.value]).ToList());
                }
                return obj[(string)indsOrKeys.value];
            }
            else throw new RemesPathArgumentException($"got type {json.type}", 0, ArgFunction.FUNCTIONS["at"]);
        }

        /// <summary>
        /// The 3+ arguments must have the types (obj: object, ...: string, anything alternating)<br></br>
        /// Returns a <em>new object</em> with the key-value pair(s) k_i-v_i added.<br></br>
        /// <em>Does not mutate the original object.</em><br></br>
        /// EXAMPLES<br></br>
        /// - add_items({}, "a", 1, "b", 2, "c", 3, "d", 4) -> {"a": 1, "b": 2, "c": 3, "d": 4}
        /// </summary>
        public static JNode AddItems(List<JNode> args)
        {
            JObject obj = (JObject)args[0];
            Dictionary<string, JNode> newObj = new Dictionary<string, JNode>(obj.children);
            int ii = 1;
            while (ii < args.Count - 1)
            {
                JNode k = args[ii++];
                if (k.type != Dtype.STR)
                    throw new RemesPathException("Even-numbered args to 'add_items' function (new keys) must be strings");
                JNode v = args[ii++];
                newObj[(string)k.value] = v;
            }
            return new JObject(0, newObj);
        }

        public static JNode ToRecords(List<JNode> args)
        {
            JNode arg = args[0];
            Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(arg);
            char strat = args[1].value is string s ? s[0] : 'd';
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
                int valCol = Convert.ToInt32(args[2].value);
                foreach (JNode item in arr.children)
                {
                    JArray subarr = (JArray)item;
                    JNode bynode = subarr[by];
                    string key = bynode.ValueOrToString();
                    if (!piv.ContainsKey(key))
                        piv[key] = new JArray();
                    ((JArray)piv[key]).children.Add(subarr[valCol]);
                }
                int uniqCt = piv.Count;
                for (int ii = 3; ii < args.Count; ii++)
                {
                    int idxCol = Convert.ToInt32(args[ii].value);
                    var newSubarr = new List<JNode>();
                    for (int jj = 0; jj < arr.children.Count; jj += uniqCt)
                        newSubarr.Add(((JArray)arr[jj])[idxCol]);
                    piv[idxCol.ToString()] = new JArray(0, newSubarr);
                }
            }
            else if (arr[0] is JObject)
            {
                string by = (string)args[1].value;
                string valCol = (string)args[2].value;
                foreach (JNode item in arr.children)
                {
                    JObject subobj = (JObject)item;
                    JNode bynode = subobj[by];
                    string key = bynode.ValueOrToString();
                    if (!piv.ContainsKey(key))
                        piv[key] = new JArray();
                    ((JArray)piv[key]).children.Add(subobj[valCol]);
                }
                int uniqCt = piv.Count;
                for (int ii = 3; ii < args.Count; ii++)
                {
                    string idxCol = (string)args[ii].value;
                    var newSubarr = new List<JNode>();
                    for (int jj = 0; jj < arr.children.Count; jj += uniqCt)
                        newSubarr.Add(((JObject)arr[jj])[idxCol]);
                    piv[idxCol] = new JArray(0, newSubarr);
                }
            }
            else throw new RemesPathException("First argument to Pivot must be an array of arrays or an array of objects");
            return new JObject(0, piv);
        }

        /// <summary>
        /// all(x: array[boolean]) -> boolean<br></br>
        /// Accepts one argument, an array of all booleans.<br></br>
        /// Returns true if ALL of the booleans in the array are true.
        /// </summary>
        public static JNode All(List<JNode> args)
        {
            JArray arr = (JArray)args[0];
            foreach (JNode item in arr.children)
            {
                if (!(bool)item.value)
                    return new JNode(false);
            }
            return new JNode(true);
        }

        /// <summary>
        /// all(x: array[boolean]) -> boolean<br></br>
        /// Accepts one argument, an array of all booleans.<br></br>
        /// Returns true if ANY of the values in the array are true.
        /// </summary>
        public static JNode Any(List<JNode> args)
        {
            JArray arr = (JArray)args[0];
            foreach (JNode item in arr.children)
            {
                if ((bool)item.value)
                    return new JNode(true);
            }
            return new JNode(false);
        }
        #endregion
        #region VECTORIZED_ARG_FUNCTIONS
        /// <summary>
        /// s_len(x: string) -> integer<br></br>
        /// Length of a string
        /// </summary>
        /// <param name="node">string</param>
        public static JNode StrLen(List<JNode> args)
        {
            JNode node = args[0];
            if (node.value is string str)
                return new JNode(Convert.ToInt64(str.Length));
            throw new RemesPathArgumentException(null, 0, FUNCTIONS["s_len"], node.type);
        }

        /// <summary>
        /// returns a new string that contains n consecutive instances of s with no break between
        /// </summary>
        public static string StrMulHelper(string s, int num)
        {
            if (num <= 0)
                return "";
            var sb = new StringBuilder(s.Length * num);
            for (int i = 0; i < num; i++)
            {
                sb.Append(s);
            }
            return sb.ToString();
        }

        /// <summary>
        /// s_mul(x: string, n: integer | boolean) -> string<br></br>
        /// Returns a string made by joining string x to itself n times.<br></br>
        /// Thus StrMul("ab", 3) -> "ababab"<br></br>
        /// THIS FUNCTION IS DEPRECATED AS OF v5.1.0, WHEN IT BECAME POSSIBLE TO USE THE * OPERATOR TO MULTIPLY A STRING BY AN INTEGER (Python style)
        /// </summary>
        public static JNode StrMul(List<JNode> args)
        {
            var arg2 = args[1];
            if ((arg2.type & Dtype.INT_OR_BOOL) == 0)
                throw new RemesPathArgumentException(null, 1, FUNCTIONS["s_mul"], arg2.type);
            return new JNode(StrMulHelper((string)args[0].value, Convert.ToInt32(arg2.value)));
        }

        /// <summary>
        /// s_count(x: string, sub: regex | string) -> integer<br></br>
        /// Return a JNode of type = Dtype.INT with value equal to the number of ocurrences of pattern sub (2nd arg) in node.value.<br></br>
        /// sub is treated as a regex even if a string is passed in.<br></br>
        /// So StrCount(JNode("ababa", Dtype.STR, 0), Regex("a?ba")) -> JNode(2, Dtype.INT, 0)
        /// because "a?ba" matches "aba" starting at position 0 and "ba" starting at position 3.
        /// </summary>
        public static JNode StrCount(List<JNode> args)
        {
            JNode node = args[0];
            JNode sub = args[1];
            string s = (string)node.value;
            int ct = (sub is JRegex jregex)
                ? jregex.regex.Matches(s).Count
                : Regex.Matches(s, (string)sub.value).Count;
            return new JNode(Convert.ToInt64(ct));
        }

        /// <summary>
        /// s_find(x: string, pat: string | regex) -> array[string]<br></br>
        /// Get an array containing all non-overlapping occurrences of regex pattern pat in string node<br></br>
        /// This function is largely deprecated in favor of s_fa,
        /// but it can still be useful if you always want the result to be a single string rather than an array of capture groups.
        /// </summary>
        public static JNode StrFind(List<JNode> args)
        {
            JNode node = args[0];
            Regex rex = (args[1] as JRegex).regex;
            MatchCollection results = rex.Matches((string)node.value);
            var resultList = new List<JNode>();
            foreach (Match match in results)
            {
                resultList.Add(new JNode(match.Value, Dtype.STR, 0));
            }
            return new JArray(0, resultList);
        }

        /// <summary>
        /// any document smaller than this isn't worth caching results for
        /// </summary>
        public const int MIN_DOC_SIZE_CACHE_REGEX_SEARCH = 100_000;

        /// <summary>
        /// any input larger than this (5 megabytes for 32bit, 10 megabytes for 64bit) will not have s_csv or s_fa outputs cached, to avoid eating too much memory
        /// </summary>
        private static readonly int MAX_DOC_SIZE_CACHE_REGEX_SEARCH = IntPtr.Size *  1_250_000;

        /// <summary>
        /// if false, do not cache for any reason. This is usually because the query involves mutation and there is some danger of mutating a value in the cache.
        /// </summary>
        public static bool regexSearchResultsShouldBeCached = true;

        private const int regexSearchResultCacheSize = 8;

        /// <summary>
        /// caches the results of executing s_fa or s_csv with a specific set of arguments on a specific input.<br></br>
        /// Only used if regexSearchResultsShouldBeCached and the size of the input is between MIN_DOC_SIZE_CACHE_REGEX_SEARCH and MAX_DOC_SIZE_CACHE_REGEX_SEARCH.
        /// </summary>
        private static LruCache<(string input, string argsAsJArrayString), JNode> regexSearchResultCache = new LruCache<(string input, string argsAsJArrayString), JNode>(regexSearchResultCacheSize);

        public static char csvDelimiterInLastQuery = '\x00';
        public static char csvQuoteCharInLastQuery = '\x00';

        /// <summary>
        /// parses a single CSV value (one column in one row) according to RFC 4180 (https://www.ietf.org/rfc/rfc4180.txt)
        /// </summary>
        public const string CSV_BASE_COLUMN_REGEX = "([^{DELIM}\\r\\n{QUOTE}]*|{QUOTE}(?:[^{QUOTE}]|{QUOTE}{2})*{QUOTE})";

        /// <summary>
        /// captures an integer (0x<HEX> or normal decimal) with leading + or - sign
        /// </summary>
        public const string CAPTURED_INT_REGEX_STR = "([+-]?(?:0x[\\da-fA-F]+|\\d+))";

        /// <summary>
        /// captures a floating point number or a hex integer. If not hex, scientific notation is allowed, as are trailing or leading decimal points 
        /// </summary>
        public const string CAPTURED_NUMBER_REGEX_STR = "([+-]?(?:0x[\\da-fA-F]+|\\d+(?:\\.\\d*)?|\\.\\d+)(?:[eE][+-]?\\d+)?)";

        // variants of the above two, but they don't capture
        public const string NONCAPTURING_INT_REGEX_STR = "(?:[+-]?(?:0x[\\da-fA-F]+|\\d+))";
        public const string NONCAPTURING_NUMBER_REGEX_STR = "(?:[+-]?(?:0x[\\da-fA-F]+|\\d+(?:\\.\\d*)?|\\.\\d+)(?:[eE][+-]?\\d+)?)";

        /// <summary>
        /// matches (but does not capture) anything that NONCAPTURING_NUMBER_REGEX_STR would match. 
        /// </summary>
        public static readonly Regex NUM_REGEX = new Regex(NONCAPTURING_NUMBER_REGEX_STR, RegexOptions.Compiled);

        /// <summary>
        /// converts the delimiter to a format suitable for use in regular expressions 
        /// </summary>
        public static string CsvCleanChar(char c)
        {
            return c == '\t' ? "\\t" : Regex.Escape(new string(c, 1));
        }

        public static string CsvColumnRegex(string delimiter, string quote)
        {
            return CSV_BASE_COLUMN_REGEX.Replace("{QUOTE}", quote).Replace("{DELIM}", delimiter);
        }

        /// <summary>
        /// returns a regex that matches a single row of a CSV file (formatted according to RFC 4180 (https://www.ietf.org/rfc/rfc4180.txt)) with:<br></br>
        /// - nColumns columns,<br></br>
        /// - delim as the column separator,<br></br>
        /// - newline as the line terminator (must be \r\n, \r, \n, or an escaped variant thereof)<br></br>
        /// - and quote as the quote character (used to enclose columns that include a delim or a newline in their text).<br></br>
        /// Thus we might call CsvRegex(5, '\t', '\n', '\'') to match a tab-separated variables file with 5 columns, UNIX LF newline, and "'" (single quote) as the quote character)
        /// </summary>
        /// <exception cref="ArgumentException">if newline is not a valid line end</exception>
        public static string CsvRowRegex(int nColumns, char delim=',', string newline="\r\n", char quote = '"')
        {
            string cleanDelim = CsvCleanChar(delim);
            string escapedQuote = CsvCleanChar(quote);
            string escapedNewline;
            switch (newline)
            {
            case "\r":
            case "\\r":
                escapedNewline = "\\r";
                break;
            case "\n":
            case "\\n":
                escapedNewline = "\\n";
                break;
            case "\r\n":
            case "\\r\\n":
                escapedNewline = "\\r\\n";
                break;
            default:
                throw new ArgumentException("newline must be one of [\\r\\n, \\r, \\n] or an escaped form of one of those", "newline");
            }
            string column = CsvColumnRegex(cleanDelim, escapedQuote);
            string startOfLine = $"(?:\\A|(?<={escapedNewline}))";
            string endOfLine = $"(?=\\Z|{escapedNewline})";
            var sb = new StringBuilder(startOfLine);
            for (int ii = 0; ii < nColumns - 1; ii++)
            {
                sb.Append(column);
                sb.Append(cleanDelim);
            }
            sb.Append(column);
            sb.Append(endOfLine);
            return sb.ToString();
        }

        /// <summary>
        /// `csv_regex(nColumns: int, delim: string=",", newline: string="\r\n", quote_char: string="\"")`
        /// Returns the regex that s_csv (see CsvRead function below) uses to match a single row of a CSV file
        /// with delimiter `delim`, `nColumns` columns, quote character `quote_char`, and newline `newline`.
        /// </summary>
        /// <exception cref="RemesPathArgumentException"></exception>
        public static JNode CsvRegexAsJNode(List<JNode> args)
        {
            JNode nColumnsNode = args[0];
            if (nColumnsNode.type != Dtype.INT)
                throw new RemesPathArgumentException($"first arg to csv_regex must be integer, not {nColumnsNode.type}", 0, FUNCTIONS["csv_regex"]);
            int nColumns = Convert.ToInt32(nColumnsNode.value);
            JNode delimNode = args[1];
            char delim = delimNode.value is string delimStr ? delimStr[0] : ',';
            JNode newlineNode = args[2];
            string newline = newlineNode.value is string newlineStr ? newlineStr : "\r\n";
            JNode quoteNode = args[3];
            char quote = quoteNode.value is string quoteStr ? quoteStr[0] : '"';
            Regex rex = new Regex(CsvRowRegex(nColumns, delim, newline, quote), RegexOptions.Compiled);
            return new JRegex(rex);
        }

        /// <summary>
        /// replace all instances of "(INT)" with a regex that captures a decimal or hex integer,<br></br>
        /// and all instances of "(NUMBER)" with a regex that captures a decimal floating point number<br></br>
        /// Also insert noncapturing versions of the int and float regexes respectively in place of each instance of "(?:INT)" or "(?:NUMBER)"
        /// </summary>
        /// <param name="regexOrString"></param>
        /// <returns></returns>
        public static JNode TransformRegex(JNode regexOrString)
        {
            if (regexOrString is CurJson cj)
            {
                JNode newRex(JNode x)
                {
                    return TransformRegex(cj.function(x));
                }
                return new CurJson(Dtype.REGEX, newRex);
            }
            string pat = regexOrString is JRegex jregex ? jregex.regex.ToString() : (string)regexOrString.value;
            string fixedPat = Regex.Replace(pat, @"(?<!\\)\((?:\?:)?(?:INT|NUMBER)\)", m =>
            {
                string mtch = m.Value;
                if (mtch[2] == ':')
                    return mtch[3] == 'I' ? NONCAPTURING_INT_REGEX_STR : NONCAPTURING_NUMBER_REGEX_STR;
                return mtch[1] == 'I' ? CAPTURED_INT_REGEX_STR : CAPTURED_NUMBER_REGEX_STR;
            });
            return new JRegex(new Regex(fixedPat, RegexOptions.Compiled | RegexOptions.Multiline));
        }

        /// <summary>
        /// assumes args[firstOptionalArgNum] and every element thereafter of args is an integer. If this is not true, throws a RemesPathArgumentException<br></br>
        /// returns all the integers from firstOptionalArgNum on, wrapped using Python-style negative indexing if needed
        /// </summary>
        /// <param name="firstOptionalArgNum">first arg to use</param>
        /// <param name="nColumns">number of columns in each row of a CSV (for s_csv only)</param>
        /// <param name="funcName">just for showing if a RemesPathArgumentException is thrown</param>
        /// <returns></returns>
        /// <exception cref="RemesPathArgumentException"></exception>
        private static int[] ColumnNumbersToParseAsNumber(List<JNode> args, int firstOptionalArgNum, int nColumns, string funcName)
        {
            return args.LazySlice($"{firstOptionalArgNum}:")
                .Select(x =>
            {
                if (x.type != Dtype.INT)
                    throw new RemesPathArgumentException($"Arguments {firstOptionalArgNum} onward to {funcName} must be integers", firstOptionalArgNum, FUNCTIONS[funcName]);
                int xVal = Convert.ToInt32(x.value);
                if (nColumns >= 1 && xVal < 0)
                    return nColumns + xVal;
                return xVal;
            })
                .ToArray<int>();
        }

        public enum HeaderHandlingInCsv
        {
            /// <summary>
            /// skip header, return array of arrays.<br></br>
            /// for example, returns [["1", "foo", "2.5"], ["2", "baz", "-3"]]<br></br>
            /// when parsing<br></br>
            /// a,b,c<br></br>
            /// 1,foo,2.5<br></br>
            /// 2,baz,-3
            /// </summary>
            SKIP_HEADER_ROWS_AS_ARRAYS,
            /// <summary>
            /// return array of arrays, including header as first row<br></br>
            /// for example, returns [["a", "b", "c"], ["1", "foo", "2.5"], ["2", "baz", "-3"]]<br></br>
            /// when parsing<br></br>
            /// a,b,c<br></br>
            /// 1,foo,2.5<br></br>
            /// 2,baz,-3
            /// </summary>
            INCLUDE_HEADER_ROWS_AS_ARRAYS,
            /// <summary>
            /// return array of objects, using header row as keys<br></br>
            /// for example, returns [{"a": "1", "b": "foo", "c": "2.5"}, {"a": "2", "b": "baz", "c": "-3"}]<br></br>
            /// when parsing<br></br>
            /// a,b,c<br></br>
            /// 1,foo,2.5<br></br>
            /// 2,baz,-3
            /// </summary>
            MAP_HEADER_TO_ROWS,
            /// <summary>
            /// not actually an option for CSV header handling, but rather includes the full text of the match as the first item of the returned array.<br></br>
            /// for example, returns [["a,b,c", "a", "b", "c"], ["1,foo,2.5", "1, "foo", "2.5"], ["2,baz,-3", "2", "baz", "-3"]]<br></br>
            /// when using s_fa with the csv-parser regex "^([^,\r\n]*),([^,\r\n]*),([^,\r\n]*)$ to parse<br></br>
            /// a,b,c<br></br>
            /// 1,foo,2.5<br></br>
            /// 2,baz,-3
            /// </summary>
            INCLUDE_FULL_MATCH_AS_FIRST_ITEM,
        }

        private static readonly Dictionary<string, HeaderHandlingInCsv> HEADER_HANDLING_ABBREVIATIONS = new Dictionary<string, HeaderHandlingInCsv>
        {
            ["n"] = HeaderHandlingInCsv.SKIP_HEADER_ROWS_AS_ARRAYS,
            ["h"] = HeaderHandlingInCsv.INCLUDE_HEADER_ROWS_AS_ARRAYS,
            ["d"] = HeaderHandlingInCsv.MAP_HEADER_TO_ROWS,
            //["i"] = HeaderHandlingInCsv.INCLUDE_FULL_MATCH_AS_FIRST_ITEM // this is not accessible, because it's not for CSV parsing
        };

        public static IEnumerable<JNode> EnumerateGroupsOfRegexMatch(string text, int matchStart, int utf8ExtraBytes, int minGroupNum, int maxGroupNum, int[] columnsToParseAsNumber, Match m, Func<string, bool, int, JNode> matchEvaluator)
        {
            int groupStart = matchStart;
            int nextColToParseAsNumber = columnsToParseAsNumber.Length == 0 ? -1 : columnsToParseAsNumber[0];
            int indexInColsToParseAsNumber = 0;
            for (int jj = minGroupNum; jj <= maxGroupNum; jj++)
            {
                Group grp = m.Groups[jj];
                string captured = grp.Value;
                utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(text, groupStart, grp.Index);
                groupStart = grp.Index;
                int jnodeIndex = groupStart + utf8ExtraBytes;
                JNode item;
                if (captured is null)
                {
                    item = new JNode("", jnodeIndex);
                }
                else
                {
                    if (nextColToParseAsNumber == jj - minGroupNum)
                    {
                        item = matchEvaluator(captured, true, jnodeIndex);
                        indexInColsToParseAsNumber++;
                        nextColToParseAsNumber = columnsToParseAsNumber.Length > indexInColsToParseAsNumber
                            ? columnsToParseAsNumber[indexInColsToParseAsNumber]
                            : -1;
                    }
                    else
                        item = matchEvaluator(captured, false, jnodeIndex);
                }
                yield return item;
            }
        }

        private static bool UseRegexSearchResultCache(string input)
        {
            return regexSearchResultsShouldBeCached && input.Length >= MIN_DOC_SIZE_CACHE_REGEX_SEARCH && input.Length <= MAX_DOC_SIZE_CACHE_REGEX_SEARCH;
        }

        private static string ArgsAsJArrayString(Regex rex, HeaderHandlingInCsv headerHandling, int[] columnsToParseAsNumber)
        {
            var sb = new StringBuilder("[");
            sb.Append(JNode.StrToString(rex.ToString(), true));
            sb.Append(",\"");
            sb.Append(headerHandling.ToString());
            sb.Append("\",");
            for (int ii = 0; ii < columnsToParseAsNumber.Length; ii++)
            {
                sb.Append(columnsToParseAsNumber[ii]);
                sb.Append(ii == columnsToParseAsNumber.Length - 1 ? ']' : ',');
            }
            return sb.ToString();
        }

        /// <summary>
        /// if the regexSearchResultCache is usable for this input at this time, check if this combination of (input, regex, HeaderHandlingInCsv, columnsToParseAsNumber)
        /// is in the regexSearchResultCache and return the cached value if so.
        /// </summary>
        private static bool TryGetCachedRegexSearchResults(string input, Regex rex, HeaderHandlingInCsv headerHandling, int[] columnsToParseAsNumber, out JNode cachedOutput)
        {
            cachedOutput = null;
            if (!UseRegexSearchResultCache(input))
                return false;
            string argsAsJArrayString = ArgsAsJArrayString(rex, headerHandling, columnsToParseAsNumber);
            return regexSearchResultCache.TryGetValue((input, argsAsJArrayString), out cachedOutput);
        }
        
        private static void CacheResultsOfRegexSearch(string input, Regex rex, HeaderHandlingInCsv headerHandling, int[] columnsToParseAsNumber, JNode output)
        {
            if (!UseRegexSearchResultCache(input))
                return;
            string argsAsJArrayString = ArgsAsJArrayString(rex, headerHandling, columnsToParseAsNumber);
            regexSearchResultCache[(input, argsAsJArrayString)] = output;
        }

        /// <summary>
        /// return an array of strings(if rex has 0 or 1 capture group(s)) or an array of arrays of strings (if there are multiple capture groups)<br></br> 
        /// The arguments in args at index firstOptionalArgNum onward will be treated as the 0-based indices of capture groups to parse as numbers<br></br>
        /// A negative number can be used for a columnsToParseAsNumber arg, and it can be wrapped according to standard Python negative indexing rules.<br></br>
        /// A failed attempt to parse a capture group as a number will return the string value of the capture group, or "" if that capture group was failed.
        /// </summary>
        /// <param name="text">text to search in</param>
        /// <param name="rex">regex to search with</param>
        /// <param name="args">the args originally passed to the function calling this</param>
        /// <param name="firstOptionalArgNum">every arg from here onwards is the 0-based index of a capture group to parse as a number</param>
        /// <param name="funcName">name of invoking function</param>
        /// <param name="maxGroupNum">highest capture group number (number of columns in s_csv)</param>
        /// <param name="headerHandling">how headers are used, see the HeaderHandlingInCsv enum (only for s_csv)</param>
        /// <param name="csvQuoteChar">the quote character (only used in s_csv)</param>
        /// <returns></returns>
        /// <exception cref="RemesPathArgumentException"></exception>
        public static JNode StrFindAllHelper(string text, Regex rex, List<JNode> args, int firstOptionalArgNum, string funcName, int maxGroupNum=-1, HeaderHandlingInCsv headerHandling = HeaderHandlingInCsv.INCLUDE_HEADER_ROWS_AS_ARRAYS, char csvQuoteChar = '\x00')
        {
            int[] columnsToParseAsNumber = ColumnNumbersToParseAsNumber(args, firstOptionalArgNum, maxGroupNum, funcName);
            Array.Sort(columnsToParseAsNumber);
            int nextMatchStart = 0;
            int matchEnd = 0;
            int utf8ExtraBytes = 0;
            var rows = new List<JNode>();
            List<string> header = null;
            bool isFirstRow = true;
            string doubleQuoteStr = csvQuoteChar > 0 ? new string(csvQuoteChar, 2) : "";
            string quoteStr = csvQuoteChar > 0 ? new string(csvQuoteChar, 1) : "";
            JNode matchEvaluator(string mValue, bool tryParseAsNumber, int jnodePosition)
            {
                if (tryParseAsNumber)
                {
                    JNode parsed = JsonParser.TryParseNumber(mValue, 0, mValue.Length, jnodePosition);
                    if (csvQuoteChar > 0 && parsed.value is string parsedStr && parsedStr.Length > 0 && parsedStr[0] == csvQuoteChar)
                        parsed.value = parsedStr.Substring(1, parsedStr.Length - 2).Replace(doubleQuoteStr, quoteStr);
                    return parsed;
                }
                if (csvQuoteChar > 0 && mValue.Length > 0 && mValue[0] == csvQuoteChar)
                    return new JNode(mValue.Substring(1, mValue.Length - 2).Replace(doubleQuoteStr, quoteStr), jnodePosition);
                return new JNode(mValue, jnodePosition);
            }
            int minGroupNum = headerHandling == HeaderHandlingInCsv.INCLUDE_FULL_MATCH_AS_FIRST_ITEM ? 0 : 1;
            int nColumns = minGroupNum >= maxGroupNum ? 1 : maxGroupNum + 1 - minGroupNum;
            JNode output;
            while (nextMatchStart < text.Length)
            {
                Match m = rex.Match(text, nextMatchStart);
                if (!m.Success)
                    break;
                int matchStart = m.Index;
                utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(text, matchEnd, matchStart);
                int jnodePos = matchStart + utf8ExtraBytes;
                matchEnd = matchStart + m.Length;
                if (maxGroupNum < 0)
                {
                    maxGroupNum = m.Groups.Count - 1;
                    nColumns = minGroupNum >= maxGroupNum ? 1 : maxGroupNum + 1 - minGroupNum;
                    for (int ii = 0; ii < columnsToParseAsNumber.Length; ii++)
                    {
                        if (columnsToParseAsNumber[ii] < 0)
                        {
                            columnsToParseAsNumber[ii] += maxGroupNum;
                        }
                    }
                    Array.Sort(columnsToParseAsNumber);
                }
                if (isFirstRow && TryGetCachedRegexSearchResults(text, rex, headerHandling, columnsToParseAsNumber, out output))
                    return output;
                bool parseMatchesAsRow = headerHandling == HeaderHandlingInCsv.INCLUDE_HEADER_ROWS_AS_ARRAYS || headerHandling == HeaderHandlingInCsv.INCLUDE_FULL_MATCH_AS_FIRST_ITEM || !isFirstRow;
                if (nColumns == 1)
                {
                    int nextColToParseAsNumber = columnsToParseAsNumber.Length == 0 ? -1 : columnsToParseAsNumber[0];
                    string mValue;
                    int childPos;
                    if (maxGroupNum == 1)
                    {
                        Group grp1 = m.Groups[1];
                        int grpIndex = grp1.Index;
                        utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(text, matchStart, grpIndex);
                        childPos = grpIndex + utf8ExtraBytes;
                        mValue = grp1.Value;
                        utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(text, grpIndex, matchEnd);
                    }
                    else
                    {
                        mValue = m.Value;
                        childPos = jnodePos;
                        utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(text, matchStart, matchEnd);
                    }
                    if (isFirstRow)
                    {
                        if (headerHandling == HeaderHandlingInCsv.MAP_HEADER_TO_ROWS)
                        {
                            header = new List<string> { mValue };  
                        }
                    }
                    if (parseMatchesAsRow)
                    {
                        JNode nodeToAdd = matchEvaluator(mValue, nextColToParseAsNumber >= 0, childPos);
                        if (headerHandling == HeaderHandlingInCsv.MAP_HEADER_TO_ROWS)
                        {
                            // if we have a 1-column file and we want to map the header to rows, we will map the one value
                            JObject thisRow = new JObject(jnodePos, new Dictionary<string, JNode> { [header[0]] = nodeToAdd });
                            rows.Add(thisRow);
                        }
                        else // if we have a 1-column file, the overall output will be an array of strings (or numbers if desired), so just add nodeToAdd
                            rows.Add(nodeToAdd);
                    }
                }
                else
                {
                    if (isFirstRow)
                    {
                        if (headerHandling == HeaderHandlingInCsv.MAP_HEADER_TO_ROWS)
                        {
                            header = new List<string>(nColumns);
                            for (int ii = minGroupNum; ii <= maxGroupNum; ii++)
                                header.Add(m.Groups[ii].Value);
                        }
                    }
                    if (parseMatchesAsRow)
                    {
                        int lastGroupIndex = m.Groups[maxGroupNum].Index;
                        if (headerHandling == HeaderHandlingInCsv.MAP_HEADER_TO_ROWS)
                        {
                            var rowObj = new Dictionary<string, JNode>(nColumns);
                            int ii = 0;
                            foreach (JNode val in EnumerateGroupsOfRegexMatch(text, matchStart, utf8ExtraBytes, minGroupNum, maxGroupNum, columnsToParseAsNumber, m, matchEvaluator))
                            {
                                rowObj[header[ii++]] = val;
                            }
                            utf8ExtraBytes = rowObj[header[header.Count - 1]].position - lastGroupIndex;
                            rows.Add(new JObject(jnodePos, rowObj));
                        }
                        else
                        {
                            var row = new List<JNode>(nColumns);
                            foreach (JNode val in EnumerateGroupsOfRegexMatch(text, matchStart, utf8ExtraBytes, minGroupNum, maxGroupNum, columnsToParseAsNumber, m, matchEvaluator))
                            {
                                row.Add(val);
                            }
                            utf8ExtraBytes = row[row.Count - 1].position - lastGroupIndex;
                            rows.Add(new JArray(jnodePos, row));
                        }
                        utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(text, lastGroupIndex, matchEnd);
                    }
                    else // still need to get utf8 extra bytes in the first row
                        utf8ExtraBytes += JsonParser.ExtraUTF8BytesBetween(text, matchStart, matchEnd);
                }
                nextMatchStart = matchEnd > matchStart ? matchEnd : matchStart + 1;
                isFirstRow = false;
            }
            output = new JArray(0, rows);
            CacheResultsOfRegexSearch(text, rex, headerHandling, columnsToParseAsNumber, output);
            return output;
        }

        /// <summary>
        /// s_csv(csv_text: string, nColumns: int, delimiter: string|null=",",
        /// newline: string|null="\r\n", quote: string|null="\"", has_header: bool |null = false,
        /// *columns_to_parse_as_number: int)<br></br>
        /// If delimiter, newline, or quote is null, their defaults are used.<br></br>
        /// returns an array of strings (if nColumns is 1)<br></br>
        /// or an array of arrays of strings (where the number of strings in each subarray is the 2nd arg nColumns)<br></br>
        /// Note that this method will ignore any rows that do not have exactly nColumns columns,<br></br>
        /// and you may get unexpected results if the delimiter or quote or newline are incorrectly specified.<br></br>
        /// if has_header, skip the first row.<br></br>
        /// See StrFindAllHelper for how the optional columns_to_parse_as_number args are handled
        /// </summary>
        /// <exception cref="RemesPathArgumentException"></exception>
        public static JNode CsvRead(List<JNode> args)
        {
            string text = (string)args[0].value;
            JNode arg2 = args[1];
            if (arg2.type != Dtype.INT)
                throw new RemesPathArgumentException(null, 1, FUNCTIONS["s_csv"], arg2.type);
            int nColumns = Convert.ToInt32(arg2.value);
            char delim = args.Count > 2 ? ((string)args[2].value)[0] : ',';
            csvDelimiterInLastQuery = delim;
            string newline = args.Count > 3 ? (string)args[3].value : "\r\n";
            char quote = args.Count > 4 ? ((string)args[4].value)[0] : '"';
            csvQuoteCharInLastQuery = quote;
            string headerHandlingAbbrev = args.Count > 5 ? (string)args[5].value : "n";
            if (!HEADER_HANDLING_ABBREVIATIONS.TryGetValue(headerHandlingAbbrev, out HeaderHandlingInCsv headerHandling))
                throw new RemesPathArgumentException("header_handling (6th argument, default 'n') must be 'n' (no header, rows as arrays), 'h' (include header, rows as arrays), or 'd' (rows as objects with header as keys)", 5, FUNCTIONS["s_csv"]);
            string rexPat = CsvRowRegex(nColumns, delim, newline, quote);
            return (JArray)StrFindAllHelper(text, new Regex(rexPat, RegexOptions.Compiled), args, 6, "s_csv", nColumns, headerHandling, quote);
        }

        /// <summary>
        /// to_csv(x: array, delimiter: string=",", newline: string="\r\n", quote_char: string="\"") -> string<br></br>
        /// returns x formatted as a CSV (RFC 4180 rules as normal), according to the following rules:<br></br>
        /// * if x is an array of non-iterables, each child is converted to a string on a separate line<br></br>
        /// * if x is an array of arrays, each subarray is converted to a row<br></br>
        /// * if x is an array of objects, the keys of the first subobject are converted to a header row, and the values of every subobject become their own row.
        /// </summary>
        public static JNode ToCsv(List<JNode> args)
        {
            JArray arr = (JArray)args[0];
            if (arr.Length == 0)
                return new JNode("");
            char delim = args[1].value is string s2 ? s2[0] : ',';
            string newline = args[2].value is string s3 ? s3 : "\r\n";
            char quote = args[3].value is string s4 ? s4[0] : '"';
            var sb = new StringBuilder();
            JNode firstRow = arr[0];
            Func<JNode, bool> rowFormatter;
            int nColumns = 0;
            if (firstRow is JObject obj)
            {
                // treat keys as header
                string[] keys = obj.children.Keys.ToArray();
                nColumns = keys.Length;
                for (int ii = 0; ii < nColumns; ii++)
                {
                    JsonTabularizer.ApplyQuotesIfNeeded(sb, keys[ii], delim, quote);
                    if (ii < keys.Length - 1)
                        sb.Append(delim);
                }
                sb.Append(newline);
                rowFormatter = x =>
                {
                    JObject o = (JObject)x;
                    int ii = 0;
                    foreach (JNode ochild in o.children.Values)
                    {
                        JsonTabularizer.CsvStringToSb(sb, ochild, delim, quote, false);
                        ii++;
                        if (ii < o.Length)
                            sb.Append(delim);
                    }
                    nColumns = ii;
                    sb.Append(newline);
                    return true;
                };
            }
            else
            {
                rowFormatter = x =>
                {
                    if (!(x is JArray a))
                    {
                        JsonTabularizer.CsvStringToSb(sb, x, delim, quote, false);
                        sb.Append(newline);
                        nColumns = 1;    
                        return true;
                    }
                    var children = a.children;
                    nColumns = children.Count;
                    for (int ii = 0; ii < nColumns; ii++)
                    {
                        JsonTabularizer.CsvStringToSb(sb, children[ii], delim, quote, false);
                        if (ii < a.Length - 1)
                            sb.Append(delim);
                    }
                    sb.Append(newline);
                    return true;
                };
            }
            foreach (JNode child in arr.children)
                rowFormatter(child);
            // include trailing newline unless it's a 1-column CSV (in which case the trailing newline would be interpreted as an empty final row)
            if (nColumns == 1)
                sb.Remove(sb.Length - newline.Length, newline.Length);
            return new JNode(sb.ToString());
        }

        /// <summary>
        /// s_fa(text: string, rex: regex | string, includeFullMatchAsFirstItem: bool = false, *colunsToParseAsNumber: int (optional))<br></br>
        /// If rex is a string, converts it to a regex<br></br>
        /// if includeFullMatchAsFirstItem and there is at least one capture group, the full match string will come before the capture groups as the first element of each subarray<br></br>
        /// columns_to_parse_as_number: any number of optional int args, the 0-based indices of capture groups to parse as numbers<br></br>
        /// see StrFindAllHelper for more
        /// </summary>
        public static JNode StrFindAll(List<JNode> args)
        {
            string text = (string)args[0].value;
            Regex rex = ((JRegex)args[1]).regex;
            bool includeFullMatchAsFirstItem = args.Count > 2 && (bool)args[2].value;
            HeaderHandlingInCsv headerHandling = includeFullMatchAsFirstItem ? HeaderHandlingInCsv.INCLUDE_FULL_MATCH_AS_FIRST_ITEM : HeaderHandlingInCsv.INCLUDE_HEADER_ROWS_AS_ARRAYS;
            return StrFindAllHelper(text, rex, args, 3, "s_fa", -1, headerHandling);
        }

        /// <summary>
        /// s_split(x: string, sep: string | regex = g`\s+`) -> array[string]<br></br>
        /// split x by each match to sep (which is always treated as a regex)<br></br>
        /// if sep is not provided, split on whitespace.<br></br>
        /// See https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.split?view=netframework-4.8#system-text-regularexpressions-regex-split(system-string) for implementation details.
        /// </summary>
        public static JNode StrSplit(List<JNode> args)
        {
            JNode node = args[0];
            JNode sepNode = args[1];
            string s = (string)node.value;
            if (sepNode.type == Dtype.NULL)
                return new JArray(0, Regex.Split(s, "\\s+").Select(x => new JNode(x)).ToList());
            string[] parts = (sepNode.value is string sep)
                ? Regex.Split(s, sep)
                : ((JRegex)sepNode).regex.Split(s);
            var outNodes = new List<JNode>(parts.Length);
            foreach (string part in parts)
            {
                outNodes.Add(new JNode(part));
            }
            return new JArray(0, outNodes);
        }

        /// <summary>
        /// s_lines(x: string) -> array[string]<br></br>
        /// splits x into an array of lines, treating '\r', '\n', and '\r\n' all as line terminators.<br></br>
        /// Use s_split(x, `\r\n`) or s_split(x, `\r`) or s_split(x, `\n`) instead if you only want to consider one type of line terminator.
        /// </summary>
        public static JNode StrSplitLines(List<JNode> args)
        {
            string s = (string)args[0].value;
            string[] lines = Regex.Split(s, @"\r\n?|\n");
            var lineArr = new List<JNode>(lines.Length);
            foreach (string line in lines)
                lineArr.Add(new JNode(line));
            return new JArray(0, lineArr);
        }

        /// <summary>
        /// s_lower(x: string) -> string<br></br>
        /// returns x converted to lowercase
        /// </summary>
        public static JNode StrLower(List<JNode> args)
        {
            return new JNode(((string)args[0].value).ToLower());
        }

        /// <summary>
        /// s_upper(x: string) -> string<br></br>
        /// returns x converted to uppercase
        /// </summary>
        public static JNode StrUpper(List<JNode> args)
        {
            return new JNode(((string)args[0].value).ToUpper());
        }

        /// <summary>
        /// s_strip(x: string) -> string<br></br>
        /// returns x with no leading or trailing whitespace
        /// </summary>
        public static JNode StrStrip(List<JNode> args)
        {
            return new JNode(((string)args[0].value).Trim());
        }

        
        /// <summary>
        /// s_slice(x: string, slicer_or_int: integer | slicer) -> string<br></br>
        /// Get a single character or slice from a string. E.g., "s_slice(abcd, -2)" returns "c", and "s_slice(abcd, :2)" returns "ab".<br></br> 
        /// See string.Slice extension method in ArrayExtensions.cs
        /// </summary>
        public static JNode StrSlice(List<JNode> args)
        {
            string s = (string)args[0].value;
            JNode slicerOrInt = args[1];
            if (slicerOrInt is JSlicer jslicer)
            {
                return new JNode(s.Slice(jslicer.slicer));
            }
            int index = Convert.ToInt32(slicerOrInt.value);
            // allow negative indices for consistency with slicing syntax
            if (!s.WrappedIndex(index, out string result))
                throw new RemesPathIndexOutOfRangeException(index, args[0]);
            return new JNode(result);
        }

        /// <summary>
        /// s_sub(s: string, to_replace: string | regex, repl: string | function) -> string<br></br> 
        /// first arg (s): a string<br></br>
        /// second arg (toReplace): a string or regex to be replaced<br></br>
        /// third arg (repl: a string (or function that returns a string)<br></br>
        /// Returns:<br></br>
        /// * if toReplace is a string, a new string with all instances of toReplace in s replaced with repl<br></br>
        /// * if toReplace is a regex and repl is a string, uses .NET regex-replacement syntax (see https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference#substitutions)<br></br>
        /// * if toReplace is a regex and repl is a function (must take an array as input and return a string), returns that function applied to the "match array" for each regex match. The "match array" always has the captured string as its first element and the n^th capture group as its (n+1)^th element. You can access the number of matches made so far within the callback using the loop() function.<br></br>
        /// EXAMPLES:<br></br>
        /// * s_sub(abbbbc, g`b+`, z) -> "azc"<br></br>
        /// * s_sub(`123 123`, `1`, `z`) -> "z23 z23"<br></br>
        /// * s_sub(`1a 2b 3c`, g`(\d)([a-z])`, (@[2]*loop()) + @[1]) -> "a1 bb2 ccc3" (the callback function (arg 3) returns any instance of a digit, then a letter letter with (that letter * (the number of matches made so far + 1), followed by the digit)<br></br>
        /// <i>NOTE: prior to JsonTools 4.10.1, it didn't matter whether arg 2 was a string or regex; it always treated it as a regex.</i> 
        /// </summary>
        /// <returns>new JNode of type = Dtype.STR with all replacements made</returns>
        public static JNode StrSub(List<JNode> args)
        {
            string val = (string)args[0].value;
            JNode toReplace = args[1];
            JNode repl = args[2];
            string replStr;
            if (toReplace is JRegex jRegex)
            {
                Regex regex = jRegex.regex;
                if (repl is CurJson cj)
                {
                    Func<JNode, JNode> fun = cj.function;
                    long previousLoopCountValue = loopCountValue;
                    loopCountValue = 0;
                    string replacementFunction(Match m)
                    {
                        int groupCount = m.Groups.Count;
                        var matchArrChildren = new List<JNode>(groupCount);
                        for (int ii = 0; ii < groupCount; ii++)
                        {
                            matchArrChildren.Add(new JNode(m.Groups[ii].Value));
                        }
                        long loopCountBeforeCall = loopCountValue;
                        var matchArr = new JArray(0, matchArrChildren);
                        loopCountValue = loopCountBeforeCall + 1;
                        return (string)fun(matchArr).value;
                    }
                    JNode resultNode = new JNode(regex.Replace(val, replacementFunction));
                    // reset to 0 so that it can't be used anywhere else
                    loopCountValue = previousLoopCountValue;
                    return resultNode;
                }
                else
                {
                    replStr = (string)repl.value;
                    return new JNode(regex.Replace(val, replStr));
                }
            }
            replStr = (string)repl.value;
            string toReplaceStr = (string)toReplace.value;
            return new JNode(val.Replace(toReplaceStr, replStr));
        }

        /// <summary>
        /// return a string that contains s padded on the left with enough repetitions of padWith
        /// to make a composite string with length at least padToLen<br></br>
        /// EXAMPLES:<br></br>
        /// LeftPadHelper("foo", "0", 5) returns "00foo"<br></br>
        /// LeftPadHelper("ab", "01", 5) returns "0101ab"
        /// </summary>
        public static string LeftPadHelper(string s, string padWith, long padToLen)
        {
            long lengthOfPad = padToLen - s.Length;
            var sb = new StringBuilder();
            long padWithCount = Math.DivRem(lengthOfPad, padWith.Length, out long mod);
            padWithCount = mod == 0 ? padWithCount : padWithCount + 1;
            for (long ii = 0; ii < padWithCount; ii++)
            {
                sb.Append(padWith);
            }
            sb.Append(s);
            return sb.ToString();
        }

        /// <summary>
        /// s_lpad(x: string, padWith: string, padToLen: int) -> string<br></br>
        /// see LeftPadHelper above
        /// </summary>
        public static JNode StrLeftPad(List<JNode> args)
        {
            string s = (string)args[0].value;
            string padWith = (string)args[1].value;
            long padToLen = (long)args[2].value;
            return new JNode(LeftPadHelper(s, padWith, padToLen));
        }

        /// <summary>
        /// zfill(x: anything, padToLen: int) -> string<br></br>
        /// returns x (or the string repr of x, if not a string) padded on the left with enough '0' chars to bring its total length to padToLen<br></br>
        /// EXAMPLES:<br></br>
        /// zfill(ab, 4) returns "00ab"<br></br>
        /// zfill(-10, 5) returns "00-10"<br></br>
        /// zfill(5.0, 4) returns "05.0"
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static JNode ZFill(List<JNode> args)
        {
            string s = args[0].ValueOrToString();
            int padToLen = Convert.ToInt32((long)args[1].value);
            return new JNode(s.PadLeft(padToLen, '0'));
        }

        /// <summary>
        /// return a string that contains s padded on the right with enough repetitions of padWith
        /// to make a composite string with length at least padToLen<br></br>
        /// EXAMPLES:<br></br>
        /// RightPadHelper("foo", "0", 5) returns "foo00"<br></br>
        /// RightPadHelper("ab", "01", 5) returns "ab0101"
        /// </summary>
        public static string RightPadHelper(string s, string padWith, long padToLen)
        {
            long lengthOfPad = padToLen - s.Length;
            var sb = new StringBuilder();
            long padWithCount = Math.DivRem(lengthOfPad, padWith.Length, out long mod);
            padWithCount = mod == 0 ? padWithCount : padWithCount + 1;
            sb.Append(s);
            for (long ii = 0; ii < padWithCount; ii++)
            {
                sb.Append(padWith);
            }
            return sb.ToString();
        }

        /// <summary>
        /// s_rpad(x: string, padWith: string, padToLen: int) -> string<br></br>
        /// see RightPadHelper above
        /// </summary>
        public static JNode StrRightPad(List<JNode> args)
        {
            string s = (string)args[0].value;
            string padWith = (string)args[1].value;
            long padToLen = (long)args[2].value;
            return new JNode(RightPadHelper(s, padWith, padToLen));
        }

        /// <summary>
        /// s_format(s: str, print_style: string=m, sort_keys: bool=true, indent: int | str=4, remember_comments: bool=false) -> str<br></br>
        /// If s is <i>not</i> valid JSON (according to the most permissive parsing rules, same as used by the parse() function),
        /// return a copy of s.<br></br>
        /// Otherwise, let <strong>elt</strong> be the JSON returned by parsing <strong>s</strong>.<br></br>
        /// For the pretty-printed forms, spaces are used to indent unless '\t' is passed as the indent argument.<br></br>
        /// <i>If remember comments</i>, use the following rules (see the docs/README.md#remember_comments in the GitHub repo):<br></br>
        /// * if print_style is 'm' or 'c', return the comment-remembering compact representation<br></br>
        /// * if print_style is 'g' or 'w', return the comment-remembering Google-style pretty-printed representation<br></br>
        /// * if print_style is 'p', return the comment-remembering PPrint-style pretty-printed representation<br></br>
        /// <i>If not remember_comments:</i><br></br>
        /// returns the string representation of elt according to the following rules:<br></br>
        /// * if print_style is 'm', return the minimal-whitespace compact representation<br></br>
        /// * if print_style is 'c', return the Python-style compact representation (one space after ',' or ':')<br></br>
        /// * if print_style is 'g', return the Google-style pretty-printed representation<br></br>
        /// * if print_style is 'w', return the Whitesmith-style pretty-printed representation<br></br>
        /// * if print_style is 'p', return the PPrint-style pretty-printed representation<br></br>
        /// </summary>
        public static JNode StrFormat(List<JNode> args)
        {
            (JNode elt, char printStyle, bool sortKeys, int indent, char indentChar) = HandleStringifyArgs(args);
            if (!(elt.value is string s))
                throw new RemesPathArgumentException(null, 0, FUNCTIONS["s_format"], elt.type);
            bool rememberComments = args[4].type != Dtype.NULL && (bool)args[4].value;
            var parser = new JsonParser(LoggerLevel.JSON5, false, false, rememberComments);
            JNode result = parser.Parse(s);
            if (parser.fatal)
                return new JNode(s);
            if (rememberComments)
            {
                switch (printStyle)
                {
                case 'm':
                case 'c':
                    return new JNode(result.ToStringWithComments(parser.comments, sortKeys));
                case 'g':
                case 'w':
                    return new JNode(result.PrettyPrintWithComments(parser.comments, indent, sortKeys, indentChar, PrettyPrintStyle.Google));
                case 'p':
                    return new JNode(result.PrettyPrintWithComments(parser.comments, indent, sortKeys, indentChar, PrettyPrintStyle.PPrint));
                default: throw new RemesPathArgumentException($"print_style (2nd arg of s_format) must be one of \"mcgwp\", got {JsonLint.CharDisplay(printStyle)}", 1, FUNCTIONS["s_format"]);
                }
            }
            switch (printStyle)
            {
            case 'm': return new JNode(result.ToString(sortKeys, ":", ","));
            case 'c': return new JNode(result.ToString(sortKeys));
            case 'g': return new JNode(result.PrettyPrint(indent, sortKeys, PrettyPrintStyle.Google, int.MaxValue, indentChar));
            case 'w': return new JNode(result.PrettyPrint(indent, sortKeys, PrettyPrintStyle.Whitesmith, int.MaxValue, indentChar));
            case 'p': return new JNode(result.PrettyPrint(indent, sortKeys, PrettyPrintStyle.PPrint, int.MaxValue, indentChar));
            default: throw new RemesPathArgumentException($"print_style (2nd arg of s_format) must be one of \"mcgwp\", got {JsonLint.CharDisplay(printStyle)}", 1, FUNCTIONS["s_format"]);
            }
        }

        /// <summary>
        /// is_str(x: anything) -> boolean<br></br>
        /// returns true iff is x is a string
        /// </summary>
        public static JNode IsStr(List<JNode> args)
        {
            return new JNode(args[0].type == Dtype.STR);
        }

        /// <summary>
        /// is_num(x: anything) -> boolean<br></br>
        /// returns true iff x is long, double, or bool
        /// </summary>
        public static JNode IsNum(List<JNode> args)
        {
            return new JNode((args[0].type & Dtype.NUM) != 0);
        }

        /// <summary>
        /// is_expr(x: anything) -> boolean<br></br>
        /// returns true iff x is JObject or JArray
        /// </summary>
        public static JNode IsExpr(List<JNode> args)
        {
            return new JNode((args[0].type & Dtype.ARR_OR_OBJ) != 0);
        }

        /// <summary>
        /// isnull(x: anything) -> boolean<br></br>
        /// returns true iff x is null
        /// </summary>
        public static JNode IsNull(List<JNode> args)
        {
            return new JNode(args[0].type == Dtype.NULL);
        }

        /// <summary>
        /// ifelse(x: anything, if_true: anything, if_false: anything) -> anything<br></br>
        /// if first arg is "truthy" (see ToBoolHelper for truthiness rules), returns second arg. Else returns third arg.<br></br>
        /// Beginning in JsonTools v7.0.0 (6.1.1.1 to be exact), this function uses conditional execution.<br></br>
        /// Prior to that version, something like ifelse(is_str(@), s_len(@), -1) would raise an error for non-string inputs,
        /// because it would always evaluate both branches regardless of the condition's value.
        /// </summary>
        public static JNode IfElse(List<JNode> args)
        {
            JNode val = ToBoolHelper(args[0]) ? args[1] : args[2];
            return val is CurJson cj
                ? cj.function(args[3]) // the optional 4th arg is secret, and exists only to provide a CurJson(function: Identity)
                                       // so that the chosen branch can be called on the current JSON
                : val;
        }


        /// <summary>
        /// log(x: integer | number, base: number | null = null) -> number<br></br>
        /// If base is null, returns the natural logarithm of x.<br></br>
        /// Otherwise, returns the logarithm base (base) of x.
        /// </summary>
        public static JNode Log(List<JNode> args)
        {
            var arg1 = args[0];
            if ((arg1.type & Dtype.FLOAT_OR_INT) == 0)
                throw new RemesPathArgumentException(null, 0, FUNCTIONS["log"], arg1.type);
            double num = Convert.ToDouble(arg1.value);
            object Base = args[1].value;
            if (Base is null)
            {
                return new JNode(Math.Log(num));
            }
            return new JNode(Math.Log(num, Convert.ToDouble(Base)));
        }

        /// <summary>
        /// log2(x: integer | number) -> number<br></br>
        /// Returns the logarithm (base 2) of x
        /// </summary>
        public static JNode Log2(List<JNode> args)
        {
            var arg1 = args[0];
            if ((arg1.type & Dtype.FLOAT_OR_INT) == 0)
                throw new RemesPathArgumentException(null, 0, FUNCTIONS["log"], arg1.type);
            return new JNode(Math.Log(Convert.ToDouble(arg1.value), 2));
        }

        /// <summary>
        /// log2(x: integer | number) -> integer | number | boolean<br></br>
        /// Returns the absolute value of x (converts false to 0 and 1 to true)
        /// </summary>
        public static JNode Abs(List<JNode> args)
        {
            JNode val = args[0];
            if ((val.type & Dtype.INT_OR_BOOL) != 0)
            {
                return new JNode(Math.Abs(Convert.ToInt64(val.value)));
            }
            else if (val.type == Dtype.FLOAT)
            {
                return new JNode(Math.Abs(Convert.ToDouble(val.value)));
            }
            throw new RemesPathArgumentException(null, 0, FUNCTIONS["abs"], val.type);
        }

        /// <summary>
        /// isnull(x: anything) -> boolean<br></br>
        /// returns true iff x is NaN (the special "Not a Number" double)
        /// </summary>
        public static JNode IsNa(List<JNode> args)
        {
            return new JNode(double.IsNaN(Convert.ToDouble(args[0].value)));
        }

        /// <summary>
        /// same "truthiness" rules as JavaScript and Python:<br></br>
        /// * if val is bool, return val<br></br>
        /// * if val is integer or number, return val != 0<br></br>
        /// * if val is string or array or object, return val.length > 0<br></br>
        /// * if val is null, return false<br></br>
        /// * if val is any other type (incl. regex, datetime), return true
        /// </summary>
        /// <param name="val"></param>
        public static bool ToBoolHelper(JNode val)
        {
            switch (val.type)
            {
            case Dtype.BOOL:  return (bool)val.value;
            case Dtype.INT:   return 0L != (long)val.value;
            case Dtype.FLOAT: return 0d != (double)val.value;
            case Dtype.STR:   return 0 < ((string)val.value).Length;
            case Dtype.ARR:   return 0 < ((JArray)val).Length;
            case Dtype.OBJ:   return 0 < ((JObject)val).Length;
            case Dtype.NULL:  return false;
            case Dtype.REGEX: return 0 < ((JRegex)val).regex.ToString().Length;
            default:          return true;
            }
        }

        /// <summary>
        /// bool(x: anything) -> boolean<br></br>
        /// see ArgFunction.ToBoolHelper for truthiness rules (similar to Python and JavaScript)
        /// </summary>
        public static JNode ToBool(List<JNode> args)
        {
            return new JNode(ToBoolHelper(args[0]));
        }

        /// <summary>
        /// str(x: anything) -> string<br></br>
        /// if x is a string, return a copy of x. Otherwise, return the string representation of x. 
        /// </summary>
        public static JNode ToStr(List<JNode> args)
        {
            return new JNode(args[0].ValueOrToString());
        }

        /// <summary>
        /// float(x: anything) -> float<br></br>
        /// converts all numeric JNodes to an equal double.<br></br>
        /// Converts all decimal string representations of floating point numbers to the corresponding double.
        /// </summary>
        public static JNode ToFloat(List<JNode> args)
        {
            JNode val = args[0];
            if (val.value is string s)
            {
                return new JNode(double.Parse(s, JNode.DOT_DECIMAL_SEP));
            }
            return new JNode(Convert.ToDouble(val.value));
        }

        /// <summary>
        /// int(x: anything) -> integer<br></br>
        /// rounds floating point numbers to the nearest integer (rounding to the nearest even integer if it's halfway between)<br></br>
        /// if x is boolean or integer, returns an equivalent int JNode<br></br>
        /// strings must be base 10
        /// </summary>
        public static JNode ToInt(List<JNode> args)
        {
            JNode val = args[0];
            if (val.value is string s)
            {
                return new JNode(long.Parse(s));
            }
            return new JNode(Convert.ToInt64(val.value));
        }

        /// <summary>
        /// if s is a hex number preceded by "0x" (with optional leading sign), parses the hex part of that number and converts it to a double.<br></br>
        /// Otherwise, expects a string representing decimal floating point number.
        /// </summary>
        public static double StrToNumHelper(string s)
        {
            char startChar = s[0];
            bool hasLeadingSign = startChar == '-' || startChar == '+';
            int xpos = hasLeadingSign ? 2 : 1;
            if (s.Length > xpos + 1 && s[xpos] == 'x')
            {
                double d = long.Parse(s.Substring(xpos + 1), System.Globalization.NumberStyles.HexNumber);
                return startChar == '-' ? -d : d;
            }
            return double.Parse(s, JNode.DOT_DECIMAL_SEP);
        }

        /// <summary>
        /// num(x: anything) -> float<br></br>
        /// as ToFloat above, but also handles hex integers preceded by "0x" (with optional +/- sign)<br></br>
        /// EXAMPLES:<br></br>
        /// * num(`+0xff`) returns 255.0<br></br>
        /// * num(`-0xa`) returns 10.0<br></br>
        /// * num(false) returns 0.0<br></br>
        /// </summary>
        public static JNode ToNum(List<JNode> args)
        {
            var valvalue = args[0].value;
            if (valvalue is string s)
                return new JNode(StrToNumHelper(s));
            return new JNode(Convert.ToDouble(valvalue));
        }

        /// <summary>
        /// round(x: number | integer, sigfigs: integer | null = null) -> integer | number<br></br>
        /// If val is an integer, return val because rounding does nothing to an integer.<br></br>
        /// If val is a double:<br></br>
        ///     - if sigfigs is null, return val rounded to the nearest integer.<br></br>
        ///     - else return val rounded to nearest double with sigfigs decimal places<br></br>
        /// If val's value is any other type, throw an ArgumentException
        /// </summary>
        public static JNode Round(List<JNode> args)
        {
            JNode val = args[0];
            IComparable valval = val.value;
            JNode sigfigs = args[1];
            if (valval is long valInt)
            {
                return new JNode(valInt);
            }
            else if (valval is double d)
            {
                if (sigfigs.type == Dtype.NULL)
                {
                    return new JNode(Convert.ToInt64(Math.Round(d)));
                }
                return new JNode(Math.Round(d, Convert.ToInt32(sigfigs.value)));
            }
            throw new RemesPathArgumentException(null, 0, FUNCTIONS["round"], val.type);            
        }

        /// <summary>
        /// parse(stringified: str) -> object<br></br>
        /// Parses args[0] as JSON. Uses the most permissive parser settings, but throws on fatal errors.<br></br>
        /// if parsing succeeded:<br></br>
        /// returns {"result": parsed JSON}<br></br>
        /// if there was a caught exception:<br></br>
        /// returns {"error": stringified exception}
        /// </summary>
        public static JNode Parse(List<JNode> args)
        {
            string stringified = (string)args[0].value;
            var output = new Dictionary<string, JNode>();
            var parser = new JsonParser(LoggerLevel.JSON5, false, false);
            JNode result = parser.Parse(stringified);
            if (parser.fatal)
            {
                JsonLint fatalError = parser.fatalError.Value;
                output["error"] = new JNode($"{fatalError.message} at position {fatalError.pos} (char {JsonLint.CharDisplay(fatalError.curChar)})");
            }
            else
                output["result"] = result;
            return new JObject(0, output);
        }

        #endregion

        /// <summary>
        /// recursively converts non-JNodes to JNodes. See the body of this function for how it's done
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static JNode ObjectsToJNode(object obj)
        {
            if (obj == null)
            {
                return new JNode();
            }
            if (obj is long l)
            {
                return new JNode(l);
            }
            if (obj is double d)
            {
                return new JNode(d);
            }
            if (obj is string s)
            {
                return new JNode(s);
            }
            if (obj is bool b)
            {
                return new JNode(b);
            }
            if (obj is Regex rex)
            {
                return new JRegex(rex);
            }
            if (obj is List<object> list)
            {
                var nodes = new List<JNode>();
                foreach (object child in list)
                {
                    nodes.Add(ObjectsToJNode(child));
                }
                return new JArray(0, nodes);
            }
            else if (obj is Dictionary<string, object> dobj)
            {
                var nodes = new Dictionary<string, JNode>();
                foreach (string key in dobj.Keys)
                {
                    nodes[key] = ObjectsToJNode(dobj[key]);
                }
                return new JObject(0, nodes);
            }
            throw new ArgumentException("Cannot convert any objects to JNode except null, long, double, bool, string, List<object>, or Dictionary<string, object");
        }

        public static Dictionary<string, ArgFunction> FUNCTIONS =
        new Dictionary<string, ArgFunction>
        {
            // non-vectorized functions
            ["add_items"] = new ArgFunction(AddItems, "add_items", Dtype.OBJ, 3, int.MaxValue, false, new Dtype[] { Dtype.OBJ, Dtype.STR, Dtype.ANYTHING, /* any # of args */ Dtype.ANYTHING }),
            ["all"] = new ArgFunction(All, "all", Dtype.BOOL, 1, 1, false, new Dtype[] { Dtype.ARR }),
            ["and"] = new ArgFunction(And, "and", Dtype.BOOL, 2, int.MaxValue, false, new Dtype[] {Dtype.ANYTHING | Dtype.FUNCTION, Dtype.ANYTHING | Dtype.FUNCTION, Dtype.ANYTHING | Dtype.FUNCTION}, conditionalExecution: true),
            ["any"] = new ArgFunction(Any, "any", Dtype.BOOL, 1, 1, false, new Dtype[] { Dtype.ARR }),
            ["append"] = new ArgFunction(Append, "append", Dtype.ARR, 2, int.MaxValue, false, new Dtype[] { Dtype.ARR, Dtype.ANYTHING, /* any # of args */ Dtype.ANYTHING }),
            ["at"] = new ArgFunction(At, "at", Dtype.ANYTHING, 2, 2, false, new Dtype[] {Dtype.ARR_OR_OBJ, Dtype.ARR | Dtype.INT | Dtype.STR}),
            ["avg"] = new ArgFunction(Mean, "avg", Dtype.FLOAT, 1, 1, false, new Dtype[] { Dtype.ARR }),
            ["concat"] = new ArgFunction(Concat, "concat", Dtype.ARR_OR_OBJ, 2, int.MaxValue, false, new Dtype[] { Dtype.ITERABLE, Dtype.ITERABLE, /* any # of args */ Dtype.ITERABLE }),
            ["csv_regex"] = new ArgFunction(CsvRegexAsJNode, "csv_regex", Dtype.REGEX, 1, 4, false, new Dtype[] {Dtype.INT, Dtype.STR, Dtype.STR, Dtype.STR}),
            ["dict"] = new ArgFunction(Dict, "dict", Dtype.OBJ, 1, 1, false, new Dtype[] { Dtype.ARR }),
            ["enumerate"] = new ArgFunction(Enumerate, "enumerate", Dtype.ARR, 1, 1, false, new Dtype[] { Dtype.ARR }),
            ["flatten"] = new ArgFunction(Flatten, "flatten", Dtype.ARR, 1, 2, false, new Dtype[] { Dtype.ARR, Dtype.INT }),
            ["group_by"] = new ArgFunction(GroupBy, "group_by", Dtype.OBJ, 2, 2, false, new Dtype[] {Dtype.ARR, Dtype.STR | Dtype.INT | Dtype.ARR}),
            ["in"] = new ArgFunction(In, "in", Dtype.BOOL, 2, 2, false, new Dtype[] {Dtype.ANYTHING, Dtype.ITERABLE }),
            ["index"] = new ArgFunction(Index, "index", Dtype.INT, 2, 3, false, new Dtype[] {Dtype.ITERABLE, Dtype.SCALAR, Dtype.BOOL}),
            ["items"] = new ArgFunction(Items, "items", Dtype.ARR, 1, 1, false, new Dtype[] { Dtype.OBJ }),
            ["iterable"] = new ArgFunction(IsExpr, "iterable", Dtype.BOOL, 1, 1, false, new Dtype[] {Dtype.ANYTHING}),
            ["keys"] = new ArgFunction(Keys, "keys", Dtype.ARR, 1, 1, false, new Dtype[] {Dtype.OBJ}),
            ["len"] = new ArgFunction(Len, "len", Dtype.INT, 1, 1, false, new Dtype[] {Dtype.ITERABLE}),
            ["loop"] = new ArgFunction(LoopCount, "loop", Dtype.INT, 0, 0, false, new Dtype[] {}, false),
            ["max"] = new ArgFunction(Max, "max", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR}),
            ["max_by"] = new ArgFunction(MaxBy, "max_by", Dtype.ANYTHING, 2, 2, false, new Dtype[] {Dtype.ARR, Dtype.STR | Dtype.INT | Dtype.FUNCTION}),
            ["mean"] = new ArgFunction(Mean, "mean", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR}),
            ["min"] = new ArgFunction(Min, "min", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR}),
            ["min_by"] = new ArgFunction(MinBy, "min_by", Dtype.ANYTHING, 2, 2, false, new Dtype[] {Dtype.ARR, Dtype.STR | Dtype.INT | Dtype.FUNCTION}),
            ["or"] = new ArgFunction(Or, "or", Dtype.BOOL, 2, int.MaxValue, false, new Dtype[] {Dtype.ANYTHING | Dtype.FUNCTION, Dtype.ANYTHING | Dtype.FUNCTION, Dtype.ANYTHING | Dtype.FUNCTION}, conditionalExecution: true),
            ["pivot"] = new ArgFunction(Pivot, "pivot", Dtype.OBJ, 3, int.MaxValue, false, new Dtype[] { Dtype.ARR, Dtype.STR | Dtype.INT, Dtype.STR | Dtype.INT, /* any # of args */ Dtype.STR | Dtype.INT }),
            ["quantile"] = new ArgFunction(Quantile, "quantile", Dtype.FLOAT, 2, 2, false, new Dtype[] {Dtype.ARR, Dtype.FLOAT}),
            ["rand"] = new ArgFunction(RandomFrom0To1, "rand", Dtype.FLOAT, 0, 0, false, new Dtype[] {}, false),
            ["randint"] = new ArgFunction(RandomInteger, "randint", Dtype.INT, 1, 2, false, new Dtype[] {Dtype.INT, Dtype.INT}, false),
            ["range"] = new ArgFunction(Range, "range", Dtype.ARR, 1, 3, false, new Dtype[] {Dtype.INT, Dtype.INT, Dtype.INT}),
            ["s_cat"] = new ArgFunction(StrCat, "s_cat", Dtype.STR, 1, int.MaxValue, false, new Dtype[] {Dtype.ANYTHING, /* any # of args */ Dtype.ANYTHING}),
            ["s_join"] = new ArgFunction(StringJoin, "s_join", Dtype.STR, 2, 2, false, new Dtype[] {Dtype.STR, Dtype.ARR}),
            ["set"] = new ArgFunction(Set, "set", Dtype.OBJ, 1, 1, false, new Dtype[] {Dtype.ARR}),
            ["sort_by"] = new ArgFunction(SortBy, "sort_by", Dtype.ARR, 2, 3, false, new Dtype[] { Dtype.ARR, Dtype.STR | Dtype.INT | Dtype.FUNCTION, Dtype.BOOL }),
            ["sorted"] = new ArgFunction(Sorted, "sorted", Dtype.ARR, 1, 2, false, new Dtype[] {Dtype.ARR, Dtype.BOOL}),
            ["stringify"] = new ArgFunction(Stringify, "stringify", Dtype.STR, 1, 4, false, new Dtype[] { Dtype.ANYTHING, Dtype.NULL | Dtype.STR, Dtype.BOOL | Dtype.STR, Dtype.INT | Dtype.STR | Dtype.NULL }),
            ["sum"] = new ArgFunction(Sum, "sum", Dtype.FLOAT, 1, 1, false, new Dtype[] {Dtype.ARR}),
            ["to_csv"] = new ArgFunction(ToCsv, "to_csv", Dtype.STR, 1, 4, false, new Dtype[] {Dtype.ARR, Dtype.STR, Dtype.STR, Dtype.STR}),
            ["to_records"] = new ArgFunction(ToRecords, "to_records", Dtype.ARR, 1, 2, false, new Dtype[] { Dtype.ITERABLE, Dtype.STR }),
            ["type"] = new ArgFunction(TypeOf, "type", Dtype.STR, 1, 1, false, new Dtype[] { Dtype.ANYTHING }),
            ["unique"] = new ArgFunction(Unique, "unique", Dtype.ARR, 1, 2, false, new Dtype[] {Dtype.ARR, Dtype.BOOL}),
            ["value_counts"] = new ArgFunction(ValueCounts, "value_counts",Dtype.ARR, 1, 2, false, new Dtype[] {Dtype.ARR, Dtype.BOOL}),
            ["values"] = new ArgFunction(Values, "values", Dtype.ARR, 1, 1, false, new Dtype[] {Dtype.OBJ}),
            ["zip"] = new ArgFunction(Zip, "zip", Dtype.ARR, 2, int.MaxValue, false, new Dtype[] {Dtype.ARR, Dtype.ARR, /* any # of args */ Dtype.ARR }),
            // VECTORIZED FUNCTIONS
            ["abs"] = new ArgFunction(Abs, "abs", Dtype.FLOAT_OR_INT, 1, 1, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE}),
            ["bool"] = new ArgFunction(ToBool, "bool", Dtype.BOOL, 1, 1, true, new Dtype[] { Dtype.ANYTHING }),
            ["float"] = new ArgFunction(ToFloat, "float", Dtype.FLOAT, 1, 1, true, new Dtype[] { Dtype.ANYTHING}),
            ["ifelse"] = new ArgFunction(IfElse, "ifelse", Dtype.UNKNOWN, 3, 3, true, new Dtype[] {Dtype.ANYTHING, Dtype.ANYTHING | Dtype.FUNCTION, Dtype.ANYTHING | Dtype.FUNCTION}, conditionalExecution: true),
            ["int"] = new ArgFunction(ToInt, "int", Dtype.INT, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["is_expr"] = new ArgFunction(IsExpr, "is_expr", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["is_num"] = new ArgFunction(IsNum, "is_num", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["is_str"] = new ArgFunction(IsStr, "is_str", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["isna"] = new ArgFunction(IsNa, "isna", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["isnull"] = new ArgFunction(IsNull, "isnull", Dtype.BOOL, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["log"] = new ArgFunction(Log, "log", Dtype.FLOAT, 1, 2, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE, Dtype.FLOAT_OR_INT}),
            ["log2"] = new ArgFunction(Log2, "log2", Dtype.FLOAT, 1, 1, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE}),
            ["num"] = new ArgFunction(ToNum, "num", Dtype.FLOAT, 1, 1, true, new Dtype[] { Dtype.ANYTHING}),
            ["parse"] = new ArgFunction(Parse, "parse", Dtype.OBJ, 1, 1, true, new Dtype[] { Dtype.STR | Dtype.ITERABLE }),
            ["round"] = new ArgFunction(Round, "round", Dtype.FLOAT_OR_INT, 1, 2, true, new Dtype[] {Dtype.FLOAT_OR_INT | Dtype.ITERABLE, Dtype.INT}),
            ["s_count"] = new ArgFunction(StrCount, "s_count", Dtype.INT, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX}),
            ["s_csv"] = new ArgFunction(CsvRead, "s_csv", Dtype.ARR, 2, int.MaxValue, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.INT, Dtype.STR | Dtype.NULL, Dtype.STR | Dtype.NULL, Dtype.STR | Dtype.NULL, Dtype.STR | Dtype.NULL, Dtype.INT}, true, new ArgsTransform((2, Dtype.NULL, x => new JNode(",")), (3, Dtype.NULL, x => new JNode("\r\n")), (4, Dtype.NULL, x => new JNode("\"")), (5, Dtype.NULL, x => new JNode("n")))),
            ["s_fa"] = new ArgFunction(StrFindAll, "s_fa", Dtype.ARR, 2, int.MaxValue, true, new Dtype[] { Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX, Dtype.BOOL | Dtype.NULL, Dtype.INT}, true, new ArgsTransform((1, Dtype.STR_OR_REGEX, TransformRegex), (2, Dtype.NULL, x => new JNode(false)))),
            ["s_find"] = new ArgFunction(StrFind, "s_find", Dtype.ARR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.REGEX}),
            ["s_format"] = new ArgFunction(StrFormat, "s_format", Dtype.STR, 1, 5, true, new Dtype[] { Dtype.ANYTHING, Dtype.NULL | Dtype.STR, Dtype.BOOL | Dtype.STR, Dtype.INT | Dtype.STR | Dtype.NULL, Dtype.BOOL | Dtype.NULL }),
            ["s_len"] = new ArgFunction(StrLen, "s_len", Dtype.INT, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_lines"] = new ArgFunction(StrSplitLines, "s_lines", Dtype.ARR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_lower"] = new ArgFunction(StrLower, "s_lower", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_lpad"] = new ArgFunction(StrLeftPad, "s_lpad", Dtype.STR, 3, 3, true, new Dtype[] { Dtype.STR | Dtype.ITERABLE, Dtype.STR | Dtype.ITERABLE, Dtype.INT | Dtype.ITERABLE}),
            ["s_mul"] = new ArgFunction(StrMul, "s_mul", Dtype.STR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.INT }),
            ["s_rpad"] = new ArgFunction(StrRightPad, "s_rpad", Dtype.STR, 3, 3, true, new Dtype[] { Dtype.STR | Dtype.ITERABLE, Dtype.STR | Dtype.ITERABLE, Dtype.INT | Dtype.ITERABLE}),
            ["s_slice"] = new ArgFunction(StrSlice, "s_slice", Dtype.STR, 2, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.INT_OR_SLICE}),
            ["s_split"] = new ArgFunction(StrSplit, "s_split", Dtype.ARR, 1, 2, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX}),
            ["s_strip"] = new ArgFunction(StrStrip, "s_strip", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["s_sub"] = new ArgFunction(StrSub, "s_sub", Dtype.STR, 3, 3, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE, Dtype.STR_OR_REGEX, Dtype.STR | Dtype.FUNCTION}, true, new ArgsTransform((1, Dtype.REGEX, TransformRegex))),
            ["s_upper"] = new ArgFunction(StrUpper, "s_upper", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.STR | Dtype.ITERABLE}),
            ["str"] = new ArgFunction(ToStr, "str", Dtype.STR, 1, 1, true, new Dtype[] {Dtype.ANYTHING}),
            ["zfill"] = new ArgFunction(ZFill, "zfill", Dtype.STR, 2, 2, true, new Dtype[] { Dtype.ANYTHING, Dtype.INT | Dtype.ITERABLE}),
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
    /// used to transform the args of an ArgFunction at compile time, before the function is ever called.<br></br>
    /// this may be useful for replacing null optional args with their default value, or performing expensive transformations<br></br>
    /// like the "(INT)" -> (integer-matching regex) transformation of regexes for s_sub and s_fa.
    /// </summary>
    public class ArgsTransform
    {
        private (int index, Dtype typesToTransform, Func<JNode, JNode> tranformer)[] transformers;

        public ArgsTransform(params (int index, Dtype typesToTransform, Func<JNode, JNode> transformer)[] args)
        {
            transformers = args;
        }

        /// <summary>
        /// if args[Index] has a type in TypesToTransform, replace args[Index] with Transformer(args[Index])
        /// </summary>
        public void Transform(List<JNode> args)
        {
            foreach ((int index, Dtype typesToTransform, Func<JNode, JNode> transformer) in transformers)
            {
                if (args.Count > index)
                {
                    var arg = args[index];
                    if ((arg.type & typesToTransform) != 0)
                        args[index] = transformer(arg);
                }
            }
        }
    }
}