/*
Breaks a Remespath query into tokens. 
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using JSON_Tools.Utils;

namespace JSON_Tools.JSON_Tools
{
    public class RemesLexerException : Exception
    {
        public int lexpos { get; set; }
        public string query { get; set; }
        public string msg { get; set; }

        public RemesLexerException(int lexpos, string query, string msg)
        {
            this.lexpos = lexpos;
            this.query = query;
            this.msg = msg;
        }

        public RemesLexerException(string msg)
        {
            this.msg = msg;
            query = "";
            lexpos = 0;
        }

        public override string ToString()
        {
            /* Looks like this:
Syntax error at position 3: Number with two decimal points
3.5.2
   ^
             */
            string caret = new string(' ', lexpos) + '^';
            return $"Syntax error at position {lexpos}: {msg}{System.Environment.NewLine}{query}{System.Environment.NewLine}{caret}";
        }
    }

    public struct UnquotedString
    {
        public string value;

        public UnquotedString(string value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return JNode.StrToString(value, true);
        }
    }

    public class RemesPathLexer
    {
        public const int MAX_RECURSION_DEPTH = JsonParser.MAX_RECURSION_DEPTH;

        public JsonParser jsonParser;

        public RemesPathLexer()
        {
            jsonParser = new JsonParser(LoggerLevel.JSON5, false, false, false);
        }

        public static readonly Regex TOKEN_REGEX = new Regex(
            "(@)|" + // CurJson
            @"(0x[\da-fA-F]+)|" + // hex numbers
            @"(0|[1-9]\d*)(\.\d*)?([eE][-+]?\d+)?|" + // numbers
            @"(\.\d+(?:[eE][-+]?\d+)?)|" + // numbers with leading decimal point
            @"(->)|" + // map operator
            @"(&|\||\^|=~|[!=]=|<=?|>=?|\+|-|//?|%|\*\*?)|" + // binops
            @"([,\[\]\(\)\{\}\.:=])|" + // delimiters
            @"([gj]?(?<!\\)`(?:\\`|[^`])*(?<!\\)`)|" + // backtick strings
            $@"({JsonParser.UNQUOTED_START}(?:[\p{{Mn}}\p{{Mc}}\p{{Nd}}\p{{Pc}}\u200c\u200d]|{JsonParser.UNQUOTED_START})*)|" + // unquoted strings
            @"(\S+)", // anything non-whitespace non-token stuff (will cause error)
            RegexOptions.Compiled
        );

        public List<object> Tokenize(string q)
        {
            MatchCollection regtoks = TOKEN_REGEX.Matches(q);
            var toks = new List<object>(regtoks.Count);
            foreach (Match m in regtoks)
            {
                int successfulGroup = 1;
                for (; successfulGroup < m.Groups.Count && !m.Groups[successfulGroup].Success; successfulGroup++) { }
                switch (successfulGroup)
                {
                case 1: toks.Add(new CurJson()); break;
                case 2: toks.Add(new JNode(long.Parse(m.Value.Substring(2), NumberStyles.HexNumber), Dtype.INT, 0)); break; // hex number
                case 3: // number
                    if (!(m.Groups[4].Success || m.Groups[5].Success))
                    {
                        // base 10 integer
                        try
                        {
                            toks.Add(new JNode(long.Parse(m.Value), Dtype.INT, 0));
                        }
                        catch (OverflowException)
                        {
                            toks.Add(new JNode(double.Parse(m.Value, JNode.DOT_DECIMAL_SEP), Dtype.FLOAT, 0));
                        }
                    }
                    else toks.Add(new JNode(double.Parse(m.Value, JNode.DOT_DECIMAL_SEP), Dtype.FLOAT, 0));
                    break;
                case 6: toks.Add(new JNode(double.Parse(m.Value, JNode.DOT_DECIMAL_SEP), Dtype.FLOAT, 0)); break; // number with leading decimal point
                case 7: // map operator "->" (any token(s) that would otherwise be tokenized as multiple binops)
                    switch (m.Value)
                    {
                    case "->": toks.Add('>'); break;
                    }
                    break;
                case 8: toks.Add(Binop.BINOPS[m.Value]); break;
                case 9: toks.Add(m.Value[0]); break; // delimiters
                case 10: // backtick strings
                    char starter = m.Value[0];
                    StringBuilder sb = new StringBuilder();
                    bool escaped = false;
                    int startIdx = starter == '`' ? 1 : 2;
                    for (int ii = startIdx; ii < m.Value.Length - 1; ii++)
                    {
                        char c = m.Value[ii];
                        if (c == '\\')
                        {
                            if (escaped)
                            {
                                sb.Append('\\');
                            }
                            escaped = !escaped;
                        }
                        else if (c == '`')
                        {
                            if (!escaped)
                                break;
                            sb.Append('`');
                            escaped = false;
                        }
                        else if (escaped)
                        {
                            // \r, \n, \t are the only escapes we'll give special treatment
                            if (c == 'r')
                                sb.Append('\r');
                            else if (c == 'n')
                                sb.Append('\n');
                            else if (c == 't')
                                sb.Append('\t');
                            else
                            {
                                sb.Append('\\');
                                sb.Append(c);
                            }
                            escaped = false;
                        }
                        else sb.Append(c);
                    }
                    string enquoted = sb.ToString();
                    if (starter == 'j') // JSON literal
                        toks.Add(jsonParser.Parse(enquoted));
                    else if (starter == 'g')
                        toks.Add(new JRegex(new Regex(enquoted, RegexOptions.Compiled)));
                    else
                        toks.Add(new JNode(enquoted, Dtype.STR, 0));
                    break;
                case 11: toks.Add(ParseUnquotedString(m.Value)); break;
                default:
                    throw new RemesLexerException($"Invalid token \"{m.Value}\" at position {m.Index}");
                }
            }
            BraceMatchCheck(q, regtoks, toks, '(', ')');
            BraceMatchCheck(q, regtoks, toks, '[', ']');
            BraceMatchCheck(q, regtoks, toks, '{', '}');
            return toks;
        }

        public static readonly Dictionary<string, object> CONSTANTS = new Dictionary<string, object>
        {
            ["null"] = null,
            ["NaN"] = NanInf.nan,
            ["Infinity"] = NanInf.inf,
            ["true"] = true,
            ["false"] = false
        };

        /// <summary>
        /// Parses a reference to a named function, or an unquoted string, or a reference to a constant like true or false or NaN
        /// </summary>
        /// <param name="q"></param>
        /// <param name="ii"></param>
        /// <returns></returns>
        public object ParseUnquotedString(string unquoted)
        {
            string uqs = jsonParser.ParseUnquotedKeyHelper(unquoted, unquoted);
            if (CONSTANTS.ContainsKey(uqs))
            {
                object con = CONSTANTS[uqs];
                if (con == null)
                {
                    return new JNode();
                }
                if (con is double d)
                {
                    return new JNode(d, Dtype.FLOAT, 0);
                }
                return new JNode((bool)con, Dtype.BOOL, 0);
            }
            if (Binop.BINOPS.TryGetValue(uqs, out Binop bop))
                return bop;
            if (UnaryOp.UNARY_OPS.TryGetValue(uqs, out UnaryOp unop))
                return unop;
            return new UnquotedString(uqs);
        }

        public static void BraceMatchCheck(string q, MatchCollection regtoks, List<object> toks, char open, char close)
        {
            int lastUnclosed = -1;
            int unclosedCount = 0;
            for (int ii = 0; ii < toks.Count; ii++)
            {
                object tok = toks[ii];
                if (tok is char c)
                {
                    if (c == open)
                    {
                        if (++unclosedCount >= MAX_RECURSION_DEPTH)
                            throw new RemesLexerException($"Maximum recuresion depth ({MAX_RECURSION_DEPTH}) in a RemesPath query reached");
                        else if (unclosedCount == 1)
                            lastUnclosed = regtoks[ii].Index;
                    }
                    else if (c == close && --unclosedCount < 0)
                        throw new RemesLexerException(regtoks[ii].Index, q, $"Unmatched '{close}'");
                }
            }
            if (unclosedCount > 0)
            {
                throw new RemesLexerException(lastUnclosed, q, $"Unclosed '{open}'");
            }
        }

        public static string TokensToString(List<object> tokens)
        {
            if (tokens == null)
                return "null";
            var sb = new StringBuilder();
            foreach (object tok in tokens)
            {
                if (tok is int || tok is long || tok is double || tok is string || tok == null || tok is Regex)
                {
                    sb.Append(ArgFunction.ObjectsToJNode(tok).ToString());
                }
                else if (tok is char c)
                {
                    sb.Append($"'{JNode.CharToString(c)}'");
                }
                else if (tok is UnquotedString uqs)
                {
                    sb.Append(uqs.ToString());
                }
                else if (tok is JNode node)
                {
                    sb.Append(node.ToString());
                }
                else if (tok is Binop bop)
                {
                    sb.Append(bop.ToString());
                }
                else if (tok is UnaryOp unop)
                {
                    sb.Append(unop.ToString());
                }
                else
                {
                    sb.Append(tok);
                }
                sb.Append(", ");
            }
            return sb.ToString();
        }
    }
}