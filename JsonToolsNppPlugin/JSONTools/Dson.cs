using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.JSON_Tools
{
    class DsonDumpException : Exception
    {
        public new string Message;

        public DsonDumpException(string message)
        {
            Message = message;
        }

        public override string ToString() => $"DSON dump error: {Message}";
    }

    /// <summary>
    /// wow such data very readable<br></br>
    /// such docs at https://dogeon.xyz/ plz read
    /// </summary>
    class Dson
    {
        public static readonly string KeyValuePairDelims = ",.!?";

        public static readonly string[] ArrayPairDelims = new string[] { "and", "also" };

        private static string FormatInteger(BigInteger val)
        {
            if (val == 0)
                return "0";
            bool negative = val < 0;
            if (negative)
                val = -val;
            // build the string representation backwards and then reverse it
            var sb = new StringBuilder();
            while (val > 0)
            {
                val = BigInteger.DivRem(val, 8, out BigInteger rem);
                sb.Append((int)rem);
            }
            int sblen = sb.Length;
            string s;
            if (sblen > 1)
            {
                var chars = new char[sblen];
                for (int ii = 0; ii < sblen; ii++)
                    chars[ii] = sb[sblen - 1 - ii];
                s = new string(chars);
            }
            else
                s = sb.ToString();
            return negative ? "-" + s : s;
        }

        /// <summary>
        /// plz stringify json as DSON<br></br>
        /// wow so amaze
        /// </summary>
        public static string Dump(JNode json)
        {
            StringBuilder sb;
            switch (json.type)
            {
                case Dtype.ARR:
                    sb = new StringBuilder();
                    sb.Append("so ");
                    JArray arr = (JArray)json;
                    bool useAndDelim = true;
                    for (int ii = 0; ii < arr.Length; ii++)
                    {
                        sb.Append(Dump(arr[ii]));
                        sb.Append(useAndDelim ? " and " : " also ");
                        useAndDelim = !useAndDelim;
                    }
                    if (arr.Length > 0)
                    {
                        // trim off the last delimiter if the array had items
                        int lengthToRemove = useAndDelim ? 5 : 4;
                        sb.Remove(sb.Length - lengthToRemove - 1, lengthToRemove);
                    }
                    sb.Append("many");
                    return sb.ToString();
                case Dtype.OBJ:
                    // {"a": 13.51e25, "b": true} becomes
                    // such "a" is 15.63very31, "b" is yes wow
                    sb = new StringBuilder();
                    sb.Append("such ");
                    int delimIdx = 0;
                    JObject obj = (JObject)json;
                    foreach (KeyValuePair<string, JNode> kv in obj.children)
                    {
                        sb.Append($"{JNode.StrToString(kv.Key, true)} is ");
                        sb.Append(Dump(kv.Value));
                        sb.Append(KeyValuePairDelims[delimIdx % 4] + " ");
                        delimIdx++;
                    }
                    if (obj.Length > 0)
                        sb.Remove(sb.Length - 2, 1); // remove the last delimter if there were items
                    sb.Append("wow");
                    return sb.ToString();
                case Dtype.BOOL:
                    return (bool)json.value ? "yes" : "no";
                case Dtype.NULL:
                    return "empty";
                case Dtype.INT:
                    return FormatInteger((BigInteger)json.value);
                case Dtype.FLOAT:
                    // floating point numbers are formatted
                    // such that fractional part, exponent, and integer part are all octal
                    double val = (double)json.value;
                    string valstr = val.ToString(JNode.DOT_DECIMAL_SEP);
                    if (double.IsInfinity(val) || double.IsNaN(val))
                        // Infinity and NaN are not in the DSON specification
                        throw new DsonDumpException($"{valstr} is fake number, can't understand. So silly, wow");
                    StringBuilder partSb = new StringBuilder();
                    sb = new StringBuilder();
                    BigInteger part;
                    foreach (char c in valstr)
                    {
                        if ('0' <= c && c <= '9' || c == '+' || c == '-')
                        {
                            partSb.Append(c);
                        }
                        else
                        {
                            part = BigInteger.Parse(partSb.ToString());
                            partSb = new StringBuilder();
                            sb.Append(FormatInteger(part));
                            if (c == '.')
                                sb.Append(".");
                            else if (c == 'e')
                                sb.Append("very");
                            else if (c == 'E')
                                sb.Append("VERY");
                        }
                    }
                    part = BigInteger.Parse(partSb.ToString());
                    sb.Append(FormatInteger(part));
                    return sb.ToString();
                case Dtype.STR:
                    return json.ToString();
                default: throw new NotSupportedException();
            }
        }
    }
}
