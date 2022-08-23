/*
Breaks a Remespath query into tokens. 
*/
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace JSON_Viewer.JSONViewer
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
        //public static readonly Regex MASTER_REGEX = new Regex(
        //@"(&|\||\^|\+|-|/{1,2}|\*{1,2}|%|" + // most arithmetic and bitwise operators
        //@"[=!]=|[><]=?|=~|" + // comparison operators
        //@"[gj]?`(?:[^`]|(?<=\\)`)*(?<!\\)`|" + // backtick string with optional g or j prefix
        //                                       // all '`' in the string must be escaped by '\\'
        //@"\[|\]|\(|\)|\{|\}|" + // close/open parens, squarebrackets and curly brackets
        //@"(?:0|[1-9]\\d*)(?:\\.\\d+)?(?:[eE][-+]?\\d+)?|" + // numbers with optional exponent and decimal point
        //@",|:|\.{1,2}|@|" + // commas, colons, periods, double dots, '@'
        //@"[a-zA-Z_][a-zA-Z_0-9]*)", // unquoted string
        //RegexOptions.Compiled);

        // note that '-' sign is not part of this num regex. That's because '-' is its own token and is handled
        // separately from numbers
        public static readonly Regex NUM_REGEX = new Regex(@"^(-?(?:0|[1-9]\d*))" + // any int or 0
                @"(\.\d+)?" + // optional decimal point and digits after
                @"([eE][-+]?\d+)?$", // optional scientific notation
                RegexOptions.Compiled);

        //public static readonly HashSet<string> DELIMITERS = new HashSet<string>
        //    {",", "[", "]", "(", ")", "{", "}", ".", ":", ".."};

        public static readonly string DELIMITERS = ",[](){}.:";

        public static readonly string BINOP_START_CHARS = "!%&*+-/<=>^|";

        public static readonly string WHITESPACE = " \t\r\n";

        public static readonly Dictionary<string, object?> CONSTANTS = new Dictionary<string, object?>
        {
            ["null"] = null,
            ["NaN"] = double.NaN,
            ["Infinity"] = double.PositiveInfinity,
            ["true"] = true,
            ["false"] = false
        };

        public RemesPathLexer()
        {
        }

        public static (JNode num, int ii) ParseNumber(string q, int ii)
        {
            StringBuilder sb = new StringBuilder();
            // parsed tracks which portions of a number have been parsed.
            // So if the int part has been parsed, it will be "i".
            // If the int and decimal point parts have been parsed, it will be "id".
            // If the int, decimal point, and scientific notation parts have been parsed, it will be "ide"
            string parsed = "i";
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
                    if (parsed != "i")
                    {
                        throw new RemesLexerException(ii, q, "Number with two decimal points");
                    }
                    parsed = "id";
                    sb.Append('.');
                    ii++;
                }
                else if (c == 'e' || c == 'E')
                {
                    if (parsed.Contains('e'))
                    {
                        break;
                    }
                    parsed += 'e';
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
            if (parsed == "i")
            {
                return (new JNode(long.Parse(sb.ToString()), Dtype.INT, 0), ii);
            }
            return (new JNode(double.Parse(sb.ToString()), Dtype.FLOAT, 0), ii);
        }

        public static (JNode s, int ii) ParseQuotedString(string q, int ii)
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
                        return (new JNode(sb.ToString(), Dtype.STR, 0), ii);
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
        public static (object s, int ii) ParseUnquotedString(string q, int ii)
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
                object? con = CONSTANTS[uqs];
                if (con == null)
                {
                    return (new JNode(null, Dtype.NULL, 0), ii);
                }
                else if (con is double)
                {
                    return (new JNode((double)con, Dtype.FLOAT, 0), ii);
                }
                else
                {
                    return (new JNode((bool)con, Dtype.BOOL, 0), ii);
                }
            }
            else if (Binop.BINOPS.ContainsKey(uqs))
            {
                return (Binop.BINOPS[uqs], ii);
            }
            else if (ArgFunction.FUNCTIONS.ContainsKey(uqs))
            {
                return (ArgFunction.FUNCTIONS[uqs], ii);
            }
            else
            {
                return (new JNode(uqs, Dtype.STR, 0), ii);
            }
        }

        public static (Binop bop, int ii) ParseBinop(string q, int ii)
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
            return (Binop.BINOPS[bs], ii);
        }

        public static List<object> Tokenize(string q)
        {
            JsonParser jsonParser = new JsonParser();
            var tokens = new List<object>();
            int ii = 0;
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
                    (curtok, ii) = ParseNumber(q, ii-1);
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
                        (quoted_string, ii) = ParseQuotedString(q, ii+1);
                        tokens.Add(new JRegex(new Regex((string)quoted_string.value)));
                    }
                    else
                    {
                        (unquoted_string, ii) = ParseUnquotedString(q, ii-1);
                        tokens.Add(unquoted_string);
                    }
                }
                else if (c == 'j')
                {
                    if (q[ii] == '`')
                    {
                        (quoted_string, ii) = ParseQuotedString(q, ii+1);
                        tokens.Add(jsonParser.Parse((string)quoted_string.value));
                    }
                    else
                    {
                        (unquoted_string, ii) = ParseUnquotedString(q, ii-1);
                        tokens.Add(unquoted_string);
                    }
                }
                else if (c == '`')
                {
                    (quoted_string, ii) = ParseQuotedString(q, ii);
                    tokens.Add(quoted_string);
                }
                else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c == '_'))
                {
                    (unquoted_string, ii) = ParseUnquotedString(q, ii-1);
                    tokens.Add(unquoted_string);
                }
                else if (BINOP_START_CHARS.Contains(c))
                {
                    (bop, ii) = ParseBinop(q, ii-1);
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
            var testcases = new (string input, List<object?> desired, string msg)[]
            {
                ("@ + 2", new List<object?>(new object?[]{new CurJson(), Binop.BINOPS["+"], (long)2}), "cur_json binop scalar"),
                ("2.5 7e5 3.2e4 ", new List<object?>(new object?[]{2.5, 7e5, 3.2e4}), "all float formats"),
                ("abc_2 `ab\\`c`", new List<object?>(new object?[]{"abc_2", "ab`c"}), "unquoted and quoted strings"),
                ("len(null, Infinity)", new List<object?>(new object?[]{ArgFunction.FUNCTIONS["len"], '(', null, ',', Double.PositiveInfinity, ')'}), "arg function, constants, delimiters"),
                ("j`[1,\"a\\`\"]`", new List<object?>(new object?[]{jsonParser.Parse("[1,\"a`\"]")}), "json string"),
                ("g`a?[b\\`]`", new List<object?>(new object?[]{new Regex(@"a?[b`]") }), "regex"),
                (" - /", new List<object?>(new object?[]{Binop.BINOPS["-"], Binop.BINOPS["/"]}), "more binops"),
                (". []", new List<object?>(new object?[]{'.', '[', ']'}), "more delimiters"),
                ("3blue", new List<object?>(new object?[]{(long)3, "blue"}), "number immediately followed by string"),
                ("2.5+-2", new List<object?>(new object?[]{2.5, Binop.BINOPS["+"], Binop.BINOPS["-"], (long)2}), "number binop binop number, no whitespace"),
                ("`a`+@", new List<object?>(new object?[]{"a", Binop.BINOPS["+"], new CurJson()}), "quoted string binop curjson, no whitespace"),
                ("== in =~", new List<object?>(new object?[]{Binop.BINOPS["=="], ArgFunction.FUNCTIONS["in"], Binop.BINOPS["=~"]}), "two-character binops and argfunction in"),
                ("@[1,2]/3", new List<object?>(new object?[]{new CurJson(), '[', (long)1, ',', (long)2, ']', Binop.BINOPS["/"], (long)3}), "numbers and delimiters then binop number, no whitespace"),
                ("2 <=3!=", new List<object?>(new object?[]{(long)2, Binop.BINOPS["<="], (long)3, Binop.BINOPS["!="]}), "binop where a substring is also a binop")
            };
            int tests_failed = 0;
            int ii = 0;
            foreach ((string input, List<object?> desired, string msg) in testcases)
            {
                List<object> output = RemesPathLexer.Tokenize(input);
                var sb_desired = new StringBuilder();
                foreach (object? desired_value in desired)
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
                foreach (object? value in output)
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
                    RemesPathLexer.Tokenize(paren);
                    Console.WriteLine($"Test {ii} failed, expected exception due to unmatched '{paren}'");
                    tests_failed++;
                }
                catch { }
                ii++;
            }
            ii++;
            try
            {
                RemesPathLexer.Tokenize("1.5.2");
                tests_failed++;
                Console.WriteLine($"Test {ii} failed, expected exception due to number with two decimal points");
            }
            catch {}

            Console.WriteLine($"Failed {tests_failed} tests.");
            Console.WriteLine($"Passed {ii - tests_failed} tests.");
        }
    }
}