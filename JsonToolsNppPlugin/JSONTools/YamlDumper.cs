/*
Reads a JSON document and outputs YAML that can be serialized back to 
equivalent (or very nearly equivalent) JSON.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JSON_Tools.Utils;

namespace JSON_Tools.JSON_Tools
{
    public class YamlDumper
    {
        // NonAscii functions from https://stackoverflow.com/questions/1615559/convert-a-unicode-string-to-an-escaped-ascii-string
        //static string EncodeNonAsciiCharacters(string value)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (char c in value)
        //    {
        //        if (c > 127)
        //        {
        //            // This character is too big for ASCII
        //            string encodedValue = "\\u" + ((int)c).ToString("x4");
        //            sb.Append(encodedValue);
        //        }
        //        else
        //        {
        //            sb.Append(c);
        //        }
        //    }
        //    return sb.ToString();
        //}

        //static string DecodeEncodedNonAsciiCharacters(string value)
        //{
        //    return Regex.Replace(
        //        value,
        //        @"\\u(?<Value>[a-zA-Z0-9]{4})",
        //        m => {
        //            return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
        //        });
        //}

        private bool IsIterable(JNode x)
        {
            return x != null && x is JObject || x is JArray;
        }

        private bool StartsOrEndsWith(string x, string sufOrPref)
        {
            return (x.Length > 0) && (sufOrPref.Contains(x[0]) || sufOrPref.Contains(x[x.Length - 1]));
        }

        private string EscapeBackslash(string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append(@"\\t"); break;
                    case '\\': sb.Append(@"\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\'': sb.Append(@"\'"); break;
                    case '\f': sb.Append(@"\\f"); break;
                    case '\b': sb.Append(@"\\b"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private string EscapeBackslashKey(string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append(@"\\t"); break;
                    case '\\': sb.Append(@"\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\'': sb.Append("''"); break;
                    case '\f': sb.Append(@"\\f"); break;
                    case '\b': sb.Append(@"\\b"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private string YamlKeyRepr(string k)
        {
            bool isFloatStr = false;
            try
            {
                double.Parse(k, JNode.DOT_DECIMAL_SEP);
                isFloatStr = true;
            }
            catch { }
            if (isFloatStr)
            {
                // k is a string representing a number; we need to enquote it so that
                // a YAML parser will recognize that it is not actually a number.
                return "'" + k + "'";
            }
            Regex forbiddenKeyChars = new Regex(@"[\t :]");
            if (forbiddenKeyChars.IsMatch(k))
            {
                // '\t', ' ', and ':' are all illegal inside a YAML key. We will escape those out
                return EscapeBackslashKey(k);
            }
            return k;
        }

        private string YamlValRepr(JNode v)
        {
            if (v.value == null)
            {
                return "null";
            }
            string strv = v.value.ToString();
            if (v.type == Dtype.STR)
            {
                try
                {
                    _ = double.Parse(strv, JNode.DOT_DECIMAL_SEP);
                    return $"'{strv}'";
                    // enquote stringified numbers to avoid confusion with actual numbers
                }
                catch { }
                if (StartsOrEndsWith(strv, " "))
                {
                    return $"\"{strv}\"";
                }
                Regex backslash = new Regex("([\\\\:\"'\r\t\n\f\b])");
                if (backslash.IsMatch(strv))
                {
                    return EscapeBackslash(strv);
                }
            }
            else if (v.type == Dtype.FLOAT)
            {
                // k is a number
                double d = (double)v.value;
                if (d == NanInf.inf) return ".inf";
                if (d == NanInf.neginf) return "-.inf";
                if (double.IsNaN(d))
                {
                    return ".nan";
                }
                if (d % 1 == 0)
                    return $"{d}.0"; // add .0 at end of floats that are equal to ints
                return d.ToString(JNode.DOT_DECIMAL_SEP);
            }
            return strv;
        }

        private void BuildYaml(StringBuilder sb, JNode tok, int depth, int indent)
        {
            // start the line with depth * indent blank spaces
            string indentSpace = new string(' ', indent * depth);
            if (tok is JObject o)
            {
                if (o.children.Count == 0)
                {
                    sb.Append(indentSpace + "{}\n");
                    return;
                }
                foreach (KeyValuePair<string, JNode> kv in o.children)
                {
                    // Console.WriteLine("k = " + k);
                    if (IsIterable(kv.Value))
                    {
                        sb.Append(String.Format("{0}{1}:\n", indentSpace, YamlKeyRepr(kv.Key)));
                        BuildYaml(sb, kv.Value, depth + 1, indent);
                    }
                    else
                    {
                        sb.Append(String.Format("{0}{1}: {2}\n",
                                             indentSpace,
                                             YamlKeyRepr(kv.Key),
                                             YamlValRepr(kv.Value)));
                    }
                }
            }
            else if (tok is JArray a)
            {
                if (a.children.Count == 0)
                {
                    sb.Append(indentSpace + "[]\n");
                    return;
                }
                foreach (JNode child in a.children)
                {
                    if (IsIterable(child))
                    {
                        sb.Append(String.Format("{0}-\n", indentSpace));
                        BuildYaml(sb, child, depth + 1, indent);
                    }
                    else
                    {
                        sb.Append(String.Format("{0}- {1}\n",
                            indentSpace, YamlValRepr(child)));
                    }
                }
            }
            else
            {
                sb.Append(String.Format("{0}\n", YamlValRepr(tok)));
            }
        }
        public string Dump(JNode json, int indent)
        {
            //MessageBox.Show(obj);
            var sb = new StringBuilder();
            BuildYaml(sb, json, 0, indent);
            return sb.ToString();
        }

        public string Dump(JNode json)
        {
            return Dump(json, 4);
        }

        //public void DumpJsonOrYaml(string outType, string fname)
        //{
        //    JsonParser jsonParser = new JsonParser();
        //    outType = outType.ToLower();
        //    StreamReader streamReader = new StreamReader(fname);
        //    string jsonstr = streamReader.ReadToEnd();
        //    JNode json = jsonParser.Parse(jsonstr);
        //    streamReader.Close();
        //    notepad.FileNew();
        //    // sw.WriteLine(EncodeNonAsciiCharacters(dumper.Dump(json, 2)));
        //    // the above line would convert UTF-16 characters to \uxxxx format.
        //    // That may be desirable, but in my experience it is unnecessary.
        //    if (outType[0] == 'j')
        //    {
        //        sw.WriteLine((outType.Length == 2 && outType[1] == 'p') ? json.PrettyPrint(4) : json.ToString());
        //    }
        //    else
        //    {
        //        sw.WriteLine(yamlDumper.Dump(json, 2));
        //    }
        //}
    }
}
