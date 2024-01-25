/*
Breaks a Remespath query into tokens. 
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using JSON_Tools.Utils;
using System.Linq;

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
3.5>>>HERE>>>.2
             */
            string firstQueryPart = query.Substring(0, lexpos);
            string secondQueryPart = query.Substring(lexpos);
            return $"Syntax error at position {lexpos}: {msg}\r\n{firstQueryPart}>>>HERE>>>{secondQueryPart}";
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
            @"(->)|" + // delimiters containing characters that conflict with binops
            @"(&|\||\^|=~|[!=]=|<=?|>=?|\+|-|//?|%|\*\*?)|" + // binops
            @"([,\[\]\(\)\{\}\.:=!;])|" + // delimiters
            @"([gjf]?(?<!\\)`(?:\\\\|\\`|[^`\r\n])*`)|" + // backtick strings
            $@"({JsonParser.UNQUOTED_START}(?:[\p{{Mn}}\p{{Mc}}\p{{Nd}}\p{{Pc}}\u200c\u200d]|{JsonParser.UNQUOTED_START})*)|" + // unquoted strings
            @"\#([^\n]*)(?:\n|\z)|" + // comments (Python-style, single-line)
            @"(\S+)", // anything non-whitespace non-token stuff (will cause error)
            RegexOptions.Compiled
        );

        public List<object> Tokenize(string q)
        {
            MatchCollection regtoks = TOKEN_REGEX.Matches(q);
            int tokCount = regtoks.Count;
            if (tokCount == 0)
                throw new RemesLexerException("Empty query");
            var toks = new List<object>(tokCount);
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
                    else if (starter == 'g') // regex
                        toks.Add(new JRegex(new Regex(enquoted, RegexOptions.Compiled | RegexOptions.Multiline)));
                    else if (starter == 'f') // f-string
                        TokenizeFString(toks, enquoted, q, m.Index + 2);
                    else
                        toks.Add(new JNode(enquoted));
                    break;
                case 11: toks.Add(ParseUnquotedString(m.Value)); break;
                case 12: /* toks.Add(new Comment(m.Value, false, m.Index)); */ break; // comments; currently we won't add them to the tokens, but maybe later?
                default:
                    throw new RemesLexerException(m.Index, q, $"Invalid token \"{m.Value}\"");
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

        public static readonly string[] LOOP_VAR_KEYWORDS = new string[] { "for" };

        public static readonly string[] NON_LOOP_VAR_KEYWORDS = new string[] { "var" };

        public static readonly Dictionary<string, VariableAssignmentType> VAR_ASSIGN_KEYWORDS_TO_TYPES =
            LOOP_VAR_KEYWORDS.Select(x => (x, VariableAssignmentType.LOOP))
            .Concat(NON_LOOP_VAR_KEYWORDS.Select(x => (x, VariableAssignmentType.NORMAL)))
            .ToDictionary(x => x.Item1, x => x.Item2);

        public static readonly string LOOP_END_KEYWORD = "end";

        public static readonly string[] MISC_KEYWORDS = new string[] { "not", LOOP_END_KEYWORD };

        public static readonly HashSet<string> KEYWORDS =
            CONSTANTS.Keys
            .Concat(VAR_ASSIGN_KEYWORDS_TO_TYPES.Keys)
            .Concat(MISC_KEYWORDS)
            .ToHashSet();

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
                int regTokIndex = ii >= regtoks.Count ? regtoks.Count - 1 : ii;
                object tok = toks[ii];
                if (tok is char c)
                {
                    if (c == open)
                    {
                        if (++unclosedCount >= MAX_RECURSION_DEPTH)
                            throw new RemesLexerException(regtoks[regTokIndex].Index, q, $"Maximum recursion depth ({MAX_RECURSION_DEPTH}) in a RemesPath query reached");
                        else if (unclosedCount == 1)
                            lastUnclosed = regtoks[regTokIndex].Index;
                    }
                    else if (c == close && --unclosedCount < 0)
                        throw new RemesLexerException(regtoks[regTokIndex].Index, q, $"Unmatched '{close}'");
                }
            }
            if (unclosedCount > 0)
            {
                throw new RemesLexerException(lastUnclosed, q, $"Unclosed '{open}'");
            }
        }

        /// <summary>
        /// RemesPath backtick string that will be parsed as string s
        /// </summary>
        public static string StringToBacktickString(string s)
        {
            var sb = new StringBuilder();
            sb.Append('`');
            for (int ii = 0; ii < s.Length; ii++)
            {
                char c = s[ii];
                switch (c)
                {
                case '`': sb.Append("\\`"); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\r': sb.Append("\\r"); break;
                default: sb.Append(c); break;
                }
            }
            sb.Append('`');
            return sb.ToString();
        }

        /// <summary>
        /// Similar to Python and C#, RemesPath supports f-strings as of JsonTools v6.1.<br></br>
        /// For example, if the input is [1, [2, "a"], "foo"]<br></br>
        /// f`first element is {@[0]}, second is {@[1]}. Does the final element begin with f? {ifelse(s_slice(@[2], 0) == f, YES, NO)}! {{LITERAL CURLY}}`<br></br>
        /// returns "First element is 1, second is [2, \"a\"]. Does the final element begin with f? YES! {LITERAL CURLY}"<br></br>
        /// Notice that if you want a literal curlybrace in an f-string, you need to double up the curlybrace, as shown above.
        /// </summary>
        private void TokenizeFString(List<object> tokens, string fstring, string query, int startIndex)
        {
            int len = fstring.Length;
            // under the hood, we will interpret the f-string as a call to a non-vectorized function,
            // s_cat, which takes any nonzero number of arguments
            // and concatenates their string represenatations.
            tokens.Add(new UnquotedString("s_cat"));
            tokens.Add('(');
            int lastUnmatchedOpenCurly = 0;
            bool insideCurlyBraces = false;
            var sb = new StringBuilder();
            for (int ii = 0; ii < len; ii++)
            {
                char c = fstring[ii];
                switch (c)
                {
                case '{':
                    if (ii == len - 1)
                    {
                        if (!insideCurlyBraces)
                            lastUnmatchedOpenCurly = ii;
                        goto unmatchedOpenCurly;
                    }
                    if (fstring[ii + 1] == '{')
                    {
                        sb.Append(c); // {{ matching literal {
                        ii++;
                    }
                    else
                    {
                        if (insideCurlyBraces)
                            goto unmatchedOpenCurly;
                        lastUnmatchedOpenCurly = ii;
                        insideCurlyBraces = true;
                        if (sb.Length > 0)
                        {
                            string newTok = sb.ToString();
                            sb = new StringBuilder();
                            tokens.Add(new JNode(newTok));
                            tokens.Add(',');
                        }
                    }
                    break;
                case '}':
                    if (insideCurlyBraces)
                    {
                        insideCurlyBraces = false;
                        string interpolatedQuery = sb.ToString();
                        List<object> interpolatedTokens = Tokenize(interpolatedQuery);
                        tokens.AddRange(interpolatedTokens);
                        sb = new StringBuilder();
                        if (ii < len - 1)
                            tokens.Add(',');
                    }
                    else if (ii < len - 1 && fstring[ii + 1] == '}')
                    {
                        sb.Append(c); // }} representing literal }
                        ii++;
                    }
                    else
                        throw new RemesLexerException(startIndex + ii, query,
                            "'{' characters are not allowed in f-strings except to close an interpolated section or in the \"}}\" sequence that represents a literal '}' character.");
                    break;
                case '=':
                    if (insideCurlyBraces)
                        throw new RemesLexerException(startIndex + ii, query, "'=' tokens (signifying a mutation expression) are not allowed inside f-string interpolated sections");
                    sb.Append(c);
                    break;
                case ';':
                    if (insideCurlyBraces)
                        throw new RemesLexerException(startIndex + ii, query, "';' tokens (signifying the end of a statement) are not allowed inside f-string interpolated sections");
                    sb.Append(c);
                    break;
                default:
                    sb.Append(c);
                    break;
                }
            }
            if (insideCurlyBraces)
                goto unmatchedOpenCurly;
            if (sb.Length > 0)
                tokens.Add(new JNode(sb.ToString()));
            tokens.Add(')'); // close paren for the s_cat function added at the beginning
            return;
            unmatchedOpenCurly:
            throw new RemesLexerException(startIndex + lastUnmatchedOpenCurly, query, "unmatched '{' in f-string");
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
                    sb.Append('\'');
                    JNode.CharToSb(sb, c);
                    sb.Append('\'');
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