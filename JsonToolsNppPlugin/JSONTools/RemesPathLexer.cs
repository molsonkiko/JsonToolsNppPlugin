/*
Breaks a Remespath query into tokens. 
*/
using System;
using System.Collections.Generic;
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
        
        /// <summary>
        /// position in query string
        /// </summary>
        public int ii = 0;

        public RemesPathLexer() { }

        // note that '-' sign is not part of this num regex.
        // That's because '-' is its own token and is handled separately from numbers
        public static readonly Regex NUM_REGEX = new Regex(@"^(-?(?:0|[1-9]\d*))" + // any int or 0
                @"(\.\d+)?" + // optional decimal point and digits after
                @"([eE][-+]?\d+)?$", // optional scientific notation
                RegexOptions.Compiled);

        public static readonly HashSet<char> DELIMITERS = new HashSet<char> { ',', '[', ']', '(', ')', '{', '}', '.', ':'  };

        public static readonly HashSet<char> BINOP_START_CHARS = new HashSet<char> {'!', '%', '&', '*', '+', '-', '/', '<', '=', '>', '^', '|', };

        public static readonly HashSet<char> WHITESPACE = new HashSet<char> { ' ', '\t', '\r', '\n' };

        public static readonly Dictionary<string, object> CONSTANTS = new Dictionary<string, object>
        {
            ["null"] = null,
            ["NaN"] = NanInf.nan,
            ["Infinity"] = NanInf.inf,
            ["true"] = true,
            ["false"] = false
        };

        public JNode ParseNumber(string q)
        {
            StringBuilder sb = new StringBuilder();
            // parsed tracks which portions of a number have been parsed.
            // So if the int part has been parsed, it will be 1.
            // If the int and decimal point parts have been parsed, it will be 3.
            // If the int, decimal point, and scientific notation parts have been parsed, it will be 7
            int parsed = 1;
            char c;
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
                        throw new RemesLexerException(ii, q, "Number with two decimal points");
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
                else
                {
                    break;
                }
            }
            if (parsed == 1)
            {
                try
                {
                    return new JNode(long.Parse(sb.ToString()), Dtype.INT, 0);
                }
                catch (OverflowException)
                {
                    // doubles can represent much larger numbers than 64-bit ints, albeit with loss of precision
                    return new JNode(double.Parse(sb.ToString()), Dtype.FLOAT, 0);
                }
            }
            return new JNode(double.Parse(sb.ToString(), JNode.DOT_DECIMAL_SEP), Dtype.FLOAT, 0);
        }

        public JNode ParseQuotedString(string q)
        {
            bool escaped = false;
            char c;
            var sb = new StringBuilder();
            while (ii < q.Length)
            {
                c = q[ii++];
                if (c == '`')
                {
                    if (!escaped) {
                        return new JNode(sb.ToString(), Dtype.STR, 0);
                    }
                    sb.Append(c);
                    escaped = false;
                }
                else if (c == '\\')
                {
                    if (escaped)
                    {
                        sb.Append(c);
                    }
                    escaped = !escaped;
                }
                else
                {
                    sb.Append(c);
                }                
            }
            throw new RemesLexerException(ii, q, "Unterminated quoted string");
        }

        private static readonly Regex UNQUOTED_STRING_REGEX = new Regex(@"[a-z_][a-z_\d]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Parses a reference to a named function, or an unquoted string, or a reference to a constant like true or false or NaN
        /// </summary>
        /// <param name="q"></param>
        /// <param name="ii"></param>
        /// <returns></returns>
        public object ParseUnquotedString(string q)
        {
            Match m = UNQUOTED_STRING_REGEX.Match(q, ii);
            if (!m.Success)
                throw new RemesLexerException(ii, q, "Failed to match unquoted string");
            ii += m.Length;
            string uqs = m.Value;
            if (CONSTANTS.ContainsKey(uqs))
            {
                object con = CONSTANTS[uqs];
                if (con == null)
                {
                    return new JNode();
                }
                else if (con is double)
                {
                    return new JNode((double)con, Dtype.FLOAT, 0);
                }
                else
                {
                    return new JNode((bool)con, Dtype.BOOL, 0);
                }
            }
            else if (Binop.BINOPS.ContainsKey(uqs))
            {
                return Binop.BINOPS[uqs];
            }
            else
            {
                return new UnquotedString(uqs);
            }
        }

        public Binop ParseBinop(string q)
        {
            char c;
            string bs = "";
            string newbs;
            while (ii < q.Length)
            {
                c = q[ii];
                newbs = bs + c;
                if (Binop.BINOPS.ContainsKey(bs) && !Binop.BINOPS.ContainsKey(newbs))
                {
                    break;
                }
                bs = newbs;
                ii++;
            }
            return Binop.BINOPS[bs];
        }

        public (List<object> selector_toks, List<object> mutator_toks) Tokenize(string q)
        {
            var selector_toks = new List<object>();
            List<object> mutator_toks = null;
            List<object> toks = selector_toks;
            bool is_assignment_expr = false;
            JsonParser jsonParser = new JsonParser();
            ii = 0;
            char c;
            JNode quoted_string;
            object unquoted_string;
            Binop bop;
            int parens_opened = 0;
            int last_unclosed_paren = 0;
            int square_braces_opened = 0;
            int last_unclosed_squarebrace = 0;
            int curly_braces_opened = 0;
            int last_unclosed_curlybrace = 0;
            while (ii < q.Length)
            {
                c = q[ii++];
                if (WHITESPACE.Contains(c)) { continue; }
                else if (c == '@')
                {
                    toks.Add(new CurJson());
                } 
                else if (c >= '0' && c <= '9')
                {
                    ii--;
                    object curtok = ParseNumber(q);
                    toks.Add(curtok);
                }
                else if (DELIMITERS.Contains(c))
                {
                    toks.Add(c);
                    // check for unmatched parentheses, do counting
                    switch (c)
                    {
                        case '(':
                            if (parens_opened == 0)
                                last_unclosed_paren = ii;
                            else if (parens_opened == MAX_RECURSION_DEPTH)
                                throw new RemesLexerException(ii, q, $"Maximum recursion depth ({MAX_RECURSION_DEPTH}) exceeded");
                            parens_opened++; 
                            break;
                        case ')':
                            if (--parens_opened < 0)
                            {
                                throw new RemesLexerException(ii, q, "Unmatched ')'");
                            }
                            break;
                        case '[':
                            if (square_braces_opened == 0) { last_unclosed_squarebrace = ii; }
                            square_braces_opened++;
                            break;
                        case ']':
                            if (--square_braces_opened < 0)
                            {
                                throw new RemesLexerException(ii, q, "Unmatched ']'");
                            }
                            break;
                        case '{':
                            if (curly_braces_opened == 0) { last_unclosed_curlybrace = ii; }
                            curly_braces_opened++;
                            break;
                        case '}':
                            if (--curly_braces_opened < 0)
                            {
                                throw new RemesLexerException(ii, q, "Unmatched '}'");
                            }
                            break;
                    }
                }
                else if (c == 'g')
                {
                    if (q[ii] == '`')
                    {
                        ii++;
                        quoted_string = ParseQuotedString(q);
                        toks.Add(new JRegex(new Regex((string)quoted_string.value)));
                    }
                    else
                    {
                        ii--;
                        unquoted_string = ParseUnquotedString(q);
                        toks.Add(unquoted_string);
                    }
                }
                else if (c == 'j')
                {
                    if (q[ii] == '`')
                    {
                        ii++;
                        quoted_string = ParseQuotedString(q);
                        toks.Add(jsonParser.Parse((string)quoted_string.value));
                    }
                    else
                    {
                        ii--;
                        unquoted_string = ParseUnquotedString(q);
                        toks.Add(unquoted_string);
                    }
                }
                else if (c == '`')
                {
                    quoted_string = ParseQuotedString(q);
                    toks.Add(quoted_string);
                }
                else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c == '_'))
                {
                    ii--;
                    unquoted_string = ParseUnquotedString(q);
                    toks.Add(unquoted_string);
                }
                else if (BINOP_START_CHARS.Contains(c))
                {
                    if (c == '=')
                    {
                        // could be the assignment operator
                        c = q[ii];
                        if (c != '=' && c != '~')
                        {
                            // it's not the first token of "==" or "=~", so it's an assignment expression
                            if (is_assignment_expr)
                            {
                                throw new RemesLexerException(ii, q, "RemesPath queries can contain at most one assignment expression");
                            }
                            if (toks.Count == 0)
                            {
                                throw new RemesLexerException(ii, q, "Assignment expression with no left-hand side");
                            }
                            is_assignment_expr = true;
                            // all toks after the assignment operator are the tokens that will compile to the mutator function
                            mutator_toks = new List<object>();
                            toks = mutator_toks;
                            continue;
                        }
                    }
                    ii--;
                    bop = ParseBinop(q);
                    toks.Add(bop);
                }
                else
                {
                    throw new RemesLexerException(ii, q, "Unexpected character");
                }
            }
            if (parens_opened > 0)
            {
                throw new RemesLexerException(last_unclosed_paren, q, "Unclosed '('");
            }
            if (curly_braces_opened > 0)
            {
                throw new RemesLexerException(last_unclosed_curlybrace, q, "Unclosed '{");
            }
            if (square_braces_opened > 0)
            {
                throw new RemesLexerException(last_unclosed_squarebrace, q, "Unclosed '['");
            }
            if (is_assignment_expr && mutator_toks.Count == 0)
            {
                throw new RemesLexerException(q.Length - 1, q, "Assignment expression with no right-hand side");
            }
            if (selector_toks.Count == 0)
            {
                throw new RemesLexerException(q.Length - 1, q, "Invalid query");
            }
            return (selector_toks, mutator_toks);
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
                else if (tok is char c)
                {
                    sb.Append($"'{JNode.CharToString(c)}'");
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