/*
Breaks a Remespath query into tokens. 
*/
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

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

    //public struct Delimiter
    //{
    //    public string value;

    //    public Delimiter(string value) { this.value = value; }

    //    public override string ToString() { return $"Delimiter(\"{value}\")"; }
    //}

    public class RemesPathLexer
    {
        /// <summary>
        /// position in query string
        /// </summary>
        public int ii = 0;

        public RemesPathLexer() { }

        // note that '-' sign is not part of this num regex. That's because '-' is its own token and is handled
        // separately from numbers
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
            //["NaN"] = double.NaN,
            //["Infinity"] = double.PositiveInfinity,
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
                return new JNode(long.Parse(sb.ToString()), Dtype.INT, 0);
            }
            return new JNode(double.Parse(sb.ToString()), Dtype.FLOAT, 0);
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

        /// <summary>
        /// Parses a reference to a named function, or an unquoted string, or a reference to a constant like true or false or NaN
        /// </summary>
        /// <param name="q"></param>
        /// <param name="ii"></param>
        /// <returns></returns>
        public object ParseUnquotedString(string q)
        {
            char c = q[ii++];
            StringBuilder sb = new StringBuilder();
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c == '_'))
            {
                sb.Append(c);
            }
            while (ii < q.Length)
            {
                c = q[ii];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c == '_') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                    ii++;
                }
                else { break; }
            }
            string uqs = sb.ToString();
            if (CONSTANTS.ContainsKey(uqs))
            {
                object con = CONSTANTS[uqs];
                if (con == null)
                {
                    return new JNode(null, Dtype.NULL, 0);
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
            else if (ArgFunction.FUNCTIONS.ContainsKey(uqs))
            {
                return ArgFunction.FUNCTIONS[uqs];
            }
            else
            {
                return new JNode(uqs, Dtype.STR, 0);
            }
        }

        public Binop ParseBinop(string q)
        {
            char c;
            string bs = "";
            string newbs = "";
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

        public List<object> Tokenize(string q)
        {
            JsonParser jsonParser = new JsonParser();
            var tokens = new List<object>();
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
                    tokens.Add(new CurJson());
                } 
                else if (c >= '0' && c <= '9')
                {
                    object curtok;
                    ii--;
                    curtok = ParseNumber(q);
                    tokens.Add(curtok);
                }
                else if (DELIMITERS.Contains(c))
                {
                    tokens.Add(c);
                    // check for unmatched parentheses, do counting
                    switch (c)
                    {
                        case '(':
                            if (parens_opened == 0) { last_unclosed_paren = ii; }
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
                        tokens.Add(new JRegex(new Regex((string)quoted_string.value)));
                    }
                    else
                    {
                        ii--;
                        unquoted_string = ParseUnquotedString(q);
                        tokens.Add(unquoted_string);
                    }
                }
                else if (c == 'j')
                {
                    if (q[ii] == '`')
                    {
                        ii++;
                        quoted_string = ParseQuotedString(q);
                        tokens.Add(jsonParser.Parse((string)quoted_string.value));
                    }
                    else
                    {
                        ii--;
                        unquoted_string = ParseUnquotedString(q);
                        tokens.Add(unquoted_string);
                    }
                }
                else if (c == '`')
                {
                    quoted_string = ParseQuotedString(q);
                    tokens.Add(quoted_string);
                }
                else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c == '_'))
                {
                    ii--;
                    unquoted_string = ParseUnquotedString(q);
                    tokens.Add(unquoted_string);
                }
                else if (BINOP_START_CHARS.Contains(c))
                {
                    ii--;
                    bop = ParseBinop(q);
                    tokens.Add(bop);
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
            return tokens;
        }
    }

    class RemesPathLexerTester
    {
        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            RemesPathLexer lexer = new RemesPathLexer();
            double inf = 1d;// double.PositiveInfinity;
            var testcases = new object[][]
            {
                new object[] { "@ + 2", new List<object>(new object[]{new CurJson(), Binop.BINOPS["+"], (long)2}), "cur_json binop scalar" },
                new object[] { "2.5 7e5 3.2e4 ", new List<object>(new object[]{2.5, 7e5, 3.2e4}), "all float formats" },
                new object[] { "abc_2 `ab\\`c`", new List<object>(new object[]{"abc_2", "ab`c"}), "unquoted and quoted strings" },
                new object[] { "len(null, Infinity)", new List<object>(new object[]{ArgFunction.FUNCTIONS["len"], '(', null, ',', inf, ')'}), "arg function, constants, delimiters" },
                new object[] { "j`[1,\"a\\`\"]`", new List<object>(new object[]{jsonParser.Parse("[1,\"a`\"]")}), "json string" },
                new object[] { "g`a?[b\\`]`", new List<object>(new object[]{new Regex(@"a?[b`]") }), "regex" },
                new object[] { " - /", new List<object>(new object[]{Binop.BINOPS["-"], Binop.BINOPS["/"]}), "more binops" },
                new object[] { ". []", new List<object>(new object[]{'.', '[', ']'}), "more delimiters" },
                new object[] { "3blue", new List<object>(new object[]{(long)3, "blue"}), "number immediately followed by string" },
                new object[] { "2.5+-2", new List<object>(new object[]{2.5, Binop.BINOPS["+"], Binop.BINOPS["-"], (long)2}), "number binop binop number, no whitespace" },
                new object[] { "`a`+@", new List<object>(new object[]{"a", Binop.BINOPS["+"], new CurJson()}), "quoted string binop curjson, no whitespace" },
                new object[] { "== in =~", new List<object>(new object[]{Binop.BINOPS["=="], ArgFunction.FUNCTIONS["in"], Binop.BINOPS["=~"]}), "two-character binops and argfunction in" },
                new object[] { "@[1,2]/3", new List<object>(new object[]{new CurJson(), '[', (long)1, ',', (long)2, ']', Binop.BINOPS["/"], (long)3}), "numbers and delimiters then binop number, no whitespace" },
                new object[] { "2 <=3!=", new List < object >(new object[] {(long) 2, Binop.BINOPS["<="],(long) 3, Binop.BINOPS["!="] }), "!=" } 
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (object[] test in testcases)
            {
                //(string input, List<object> desired, string msg)
                string input = (string)test[0], msg = (string)test[2];
                List<object> desired = (List<object>)test[1];
                List<object> output = lexer.Tokenize(input);
                var sb_desired = new StringBuilder();
                foreach (object desired_value in desired)
                {
                    if (desired_value is int || desired_value is long || desired_value is double || desired_value is string || desired_value == null)
                    {
                        sb_desired.Append(ArgFunction.ObjectsToJNode(desired_value).ToString());
                    }
                    else if (desired_value is JNode)
                    {
                        sb_desired.Append(((JNode)desired_value).ToString());
                    }
                    else
                    {
                        sb_desired.Append(desired_value.ToString());
                    }
                    sb_desired.Append(", ");
                }
                string str_desired = sb_desired.ToString();
                var sb_output = new StringBuilder();
                foreach (object value in output)
                {
                    if (value is JNode && !(value is CurJson))
                    {
                        sb_output.Append(((JNode)value).ToString());
                    }
                    else
                    {
                        sb_output.Append((value == null) ? "null" : value.ToString());
                    }
                    sb_output.Append(", ");
                }
                string str_output = sb_output.ToString();
                if (str_output != str_desired)
                {

                    tests_failed++;
                    Console.WriteLine(String.Format("Test {0} (input \"{1}\", {2}) failed:\n" +
                                                    "Expected\n{3}\nGot\n{4}",
                                                    ii+1, input, msg, str_desired, str_output));
                }
                ii++;
            }
            ii = testcases.Length;
            foreach (string paren in new string[] { "(", ")", "[", "]", "{", "}" })
            {
                try
                {
                    lexer.Tokenize(paren);
                    Console.WriteLine($"Test {ii} failed, expected exception due to unmatched '{paren}'");
                    tests_failed++;
                }
                catch { }
                ii++;
            }
            ii++;
            try
            {
                lexer.Tokenize("1.5.2");
                tests_failed++;
                Console.WriteLine($"Test {ii} failed, expected exception due to number with two decimal points");
            }
            catch {}

            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }
    }
}