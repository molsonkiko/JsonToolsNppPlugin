using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    class RemesPathLexerTester
    {
        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            RemesPathLexer lexer = new RemesPathLexer();
            var testcases = new object[][]
            {
                new object[] { "@ + 2", new List<object>(new object[]{new CurJson(), Binop.BINOPS["+"], 2L}), "cur_json binop scalar" },
                new object[] { "2.5 7e5 3.2e4 ", new List<object>(new object[]{2.5, 7e5, 3.2e4}), "all float formats" },
                new object[] { "abc_2 `ab\\`c`", new List<object>(new object[]{"abc_2", "ab`c"}), "unquoted and quoted strings" },
                new object[] { "len(null, Infinity)", new List<object>(new object[]{ArgFunction.FUNCTIONS["len"], '(', null, ',', NanInf.inf, ')'}), "arg function, constants, delimiters" },
                new object[] { "j`[1,\"a\\`\"]`", new List<object>(new object[]{jsonParser.Parse("[1,\"a`\"]")}), "json string" },
                new object[] { "g`a?[b\\`]`", new List<object>(new object[]{new Regex(@"a?[b`]") }), "regex" },
                new object[] { " - /", new List<object>(new object[]{Binop.BINOPS["-"], Binop.BINOPS["/"]}), "more binops" },
                new object[] { ". []", new List<object>(new object[]{'.', '[', ']'}), "more delimiters" },
                new object[] { "3blue", new List<object>(new object[]{3L, "blue"}), "number immediately followed by string" },
                new object[] { "2.5+-2", new List<object>(new object[]{2.5, Binop.BINOPS["+"], Binop.BINOPS["-"], 2L}), "number binop binop number, no whitespace" },
                new object[] { "`a`+@", new List<object>(new object[]{"a", Binop.BINOPS["+"], new CurJson()}), "quoted string binop curjson, no whitespace" },
                new object[] { "== in =~", new List<object>(new object[]{Binop.BINOPS["=="], ArgFunction.FUNCTIONS["in"], Binop.BINOPS["=~"]}), "two-character binops and argfunction in" },
                new object[] { "@[1,2]/3", new List<object>(new object[]{new CurJson(), '[', 1L, ',', 2L, ']', Binop.BINOPS["/"], 3L}), "numbers and delimiters then binop number, no whitespace" },
                new object[] { "2 <=3!=", new List < object >(new object[] {2L, Binop.BINOPS["<="], 3L, Binop.BINOPS["!="] }), "!=" },
                new object[] { "8 = @ * 79 ==", new List<object>(new object[] {8L, '=', new CurJson(), Binop.BINOPS["*"], 79L, Binop.BINOPS["=="]}), "assignment operator" },
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (object[] test in testcases)
            {
                //(string input, List<object> desired, string msg)
                ii++;
                string input = (string)test[0], msg = (string)test[2];
                List<object> desired = (List<object>)test[1];
                var sb_desired = new StringBuilder();
                bool should_be_assignment_expr = false;
                bool is_assignment_expr = false;
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
                        if (desired_value is char c && c == '=') should_be_assignment_expr = true;
                        sb_desired.Append(desired_value.ToString());
                    }
                    sb_desired.Append(", ");
                }
                string str_desired = sb_desired.ToString();
                List<object> output = null;
                try
                {
                    output = lexer.Tokenize(input, out is_assignment_expr);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} (input \"{1}\", {2}) failed:\n" +
                                            "Expected\n{3}\nInstead raised exception\n{4}",
                                            ii, input, msg, str_desired, ex));
                    continue;
                }
                if (is_assignment_expr != should_be_assignment_expr)
                {
                    Npp.AddLine(String.Format("Test {0} (input \"{1}\", {2}) failed:\n" +
                                            "Expected is_assignment_expr = {3} but instead got is_assignment_expr = {4}",
                                            ii, input, msg, str_desired, should_be_assignment_expr, is_assignment_expr));
                }
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
                    Npp.AddLine(String.Format("Test {0} (input \"{1}\", {2}) failed:\n" +
                                                    "Expected\n{3}\nGot\n{4}",
                                                    ii, input, msg, str_desired, str_output));
                }
            }
            // test exeptions related to unmatched brackets
            ii = testcases.Length;
            foreach (string paren in new string[] { "(", ")", "[", "]", "{", "}" })
            {
                ii++;
                try
                {
                    lexer.Tokenize(paren, out bool _);
                    Npp.AddLine($"Test {ii} (query \"{paren}\") failed, expected exception due to unmatched '{paren}'");
                    tests_failed++;
                }
                catch { }
            }
            // test if two decimal points causes error
            ii++;
            string query;
            try
            {
                query = "1.5.2";
                lexer.Tokenize(query, out bool _);
                tests_failed++;
                Npp.AddLine($"Test {ii} (query \"{query}\") failed, expected exception due to number with two decimal points");
            }
            catch { }
            // test if two assignment expressions causes error
            ii++;
            try
            {
                query = "@[@ =~ a] = 5 = 7";
                lexer.Tokenize(query, out bool _);
                tests_failed++;
                Npp.AddLine($"Test {ii} (query \"{query}\") failed, expected exception due to more than one assignment expression");
            }
            catch { }
            // test if '=' with no RHS causes error
            ii++;
            try
            {
                query = "@ *3 = ";
                lexer.Tokenize(query, out bool _);
                tests_failed++;
                Npp.AddLine($"Test {ii} (query \"{query}\") failed, expected exception due to assignment expression with no RHS");
            }
            catch { }
            // test if '=' with no LHS causes error
            ii++;
            try
            {
                query = "= @ *3";
                lexer.Tokenize(query, out bool _);
                tests_failed++;
                Npp.AddLine($"Test {ii} (query \"{query}\") failed, expected exception due to assignment expression with no LHS");
            }
            catch { }
            // test if recursion limit of 512 is enforced
            ii++;
            try
            {
                query = $"{new string('(', 1000)}1{new string(')', 1000)}";
                lexer.Tokenize(query, out bool _);
                tests_failed++;
                Npp.AddLine($"Test {ii} (query \"{query}\") failed, expected exception due to exceeding recursion limit with too many unclosed parentheses");
            }
            catch { }
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }

    public class RemesParserTester
    {
        public struct Query_DesiredResult
        {
            public string query;
            public string desired_result;

            public Query_DesiredResult(string query, string desired_result)
            {
                this.query = query;
                this.desired_result = desired_result;
            }
        }

        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            JNode foo = jsonParser.Parse("{\"foo\": [[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]], " +
                                           "\"bar\": {\"a\": false, \"b\": [\"a`g\", \"bah\"]}, \"baz\": \"z\", " +
                                           "\"quz\": {}, \"jub\": [], \"guzo\": [[[1]], [[2], [3]]], \"7\": [{\"foo\": 2}, 1], \"_\": {\"0\": 0}}");
            RemesParser remesparser = new RemesParser();
            Npp.AddLine($"The queried JSON in the RemesParser tests is:{foo.ToString()}");
            var testcases = new Query_DesiredResult[]
            {
                // binop precedence tests
                new Query_DesiredResult("2 - 4 * 3.5", "-12.0"),
                new Query_DesiredResult("2 / 3 - 4 * 5 ** 1", "-58/3"),
                new Query_DesiredResult("5 ** (6 - 2)", "625.0"),
                // binop two jsons, binop json scalar, binop scalar json tests
                new Query_DesiredResult("@.foo[0] + @.foo[1]", "[3.0, 5.0, 7.0]"),
                new Query_DesiredResult("@.foo[0] + j`[3.0, 4.0, 5.0]`", "[3.0, 5.0, 7.0]"),
                new Query_DesiredResult("j`[0, 1, 2]` + @.foo[1]", "[3.0, 5.0, 7.0]"),
                new Query_DesiredResult("1 + @.foo[0]", "[1, 2, 3]"),
                new Query_DesiredResult("@.foo[0] + 1", "[1, 2, 3]"),
                new Query_DesiredResult("1 + j`[0, 1, 2]`", "[1, 2, 3]"),
                new Query_DesiredResult("j`[0, 1, 2]` + 1", "[1, 2, 3]"),
                new Query_DesiredResult("`a` + str(range(3))", "[\"a0\", \"a1\", \"a2\"]"),
                new Query_DesiredResult("str(range(3)) + `a`", "[\"0a\", \"1a\", \"2a\"]"),
                new Query_DesiredResult("str(@.foo[0]) + `a`", "[\"0a\", \"1a\", \"2a\"]"),
                new Query_DesiredResult("`a` + str(@.foo[0])", "[\"a0\", \"a1\", \"a2\"]"),
                // comparison tests
                // comparisons of booleans
                new Query_DesiredResult("2 < true", "false"),
                new Query_DesiredResult("2 > true", "true"),
                new Query_DesiredResult("2 == true", "false"),
                new Query_DesiredResult("true < 2", "true"),
                new Query_DesiredResult("true > 2", "false"),
                new Query_DesiredResult("true == 2", "false"),
                new Query_DesiredResult("0.5 > true", "false"),
                new Query_DesiredResult("0.5 < true", "true"),
                new Query_DesiredResult("0.5 == true", "false"),
                new Query_DesiredResult("1.0 == true", "true"),
                new Query_DesiredResult("1.0 < true", "false"),
                new Query_DesiredResult("1.0 > true", "false"),
                new Query_DesiredResult("true == 1.0", "true"),
                new Query_DesiredResult("true < 1.0", "false"),
                new Query_DesiredResult("true > 1.0", "false"),
                new Query_DesiredResult("0.0 == false", "true"),
                new Query_DesiredResult("0.0 < false", "false"),
                new Query_DesiredResult("0.0 > false", "false"),
                new Query_DesiredResult("false == 0.0", "true"),
                new Query_DesiredResult("false < 0.0", "false"),
                new Query_DesiredResult("false > 0.0", "false"),
                new Query_DesiredResult("false > true", "false"),
                new Query_DesiredResult("false < true", "true"),
                new Query_DesiredResult("false == true", "false"),
                new Query_DesiredResult("-1 > false", "false"),
                new Query_DesiredResult("-1 < false", "true"),
                new Query_DesiredResult("-1 == false", "false"),
                new Query_DesiredResult("false < -1", "false"),
                new Query_DesiredResult("false > -1", "true"),
                new Query_DesiredResult("false == -1", "false"),
                // comparisons between ints and floats
                new Query_DesiredResult("-1 < -0.6", "true"),
                new Query_DesiredResult("-1 > -0.6", "false"),
                new Query_DesiredResult("17 < 17.4", "true"),
                new Query_DesiredResult("17.4 > 17", "true"),
                new Query_DesiredResult("17 == 17.4", "false"),
                new Query_DesiredResult("17.4 == 17", "false"),
                new Query_DesiredResult("17 > 16.6", "true"),
                new Query_DesiredResult("17 == 16.6", "false"),
                new Query_DesiredResult("16.6 < 17", "true"),
                new Query_DesiredResult("16.6 == 17", "false"),
                // uminus tests
                new Query_DesiredResult("-j`[1]`", "[-1]"),
                new Query_DesiredResult("-j`[1,2]`**-3", "[-1.0, -1/8]"),
                new Query_DesiredResult("-@.foo[2]", "[-6.0, -7.0, -8.0]"),
                new Query_DesiredResult("2/--3", "2/3"),
                // indexing tests
                new Query_DesiredResult("@.baz", "\"z\""),
                new Query_DesiredResult("@.foo[0]", "[0, 1, 2]"),
                new Query_DesiredResult("@[g`^b`]", "{\"bar\": {\"a\": false, \"b\": [\"a`g\", \"bah\"]}, \"baz\": \"z\"}"),
                new Query_DesiredResult("@.foo[1][@ > 3.5]", "[4.0, 5.0]"),
                new Query_DesiredResult("@.foo[-2:]", "[[3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]"),
                new Query_DesiredResult("@.foo[:3:2]", "[[0, 1, 2], [6.0, 7.0, 8.0]]"),
                new Query_DesiredResult("@[foo, jub]", "{\"foo\": [[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]], \"jub\": []}"),
                new Query_DesiredResult("@[foo, jub][2]", "{\"foo\": [6.0, 7.0, 8.0]}"),
                new Query_DesiredResult("@[foo][0][0,2]", "[0, 2]"),
                new Query_DesiredResult("@[foo][0][0, 2:]", "[0, 2]"),
                new Query_DesiredResult("@[foo][0][2:, 0]", "[2, 0]"),
                new Query_DesiredResult("@[foo][0][0, 2:, 1] ", "[0, 2, 1]"),
                new Query_DesiredResult("@[foo][0][:1, 2:]", "[0, 2]"),
                new Query_DesiredResult("@[foo][0][0, 2:4]", "[0, 2]"),
                new Query_DesiredResult("@[foo][0][3:, 0]", "[0]"),
                new Query_DesiredResult("@.*", foo.ToString()),
                new Query_DesiredResult("@.foo[*]", "[[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]"),
                new Query_DesiredResult("@.foo[:2][2*@[0] >= @[1]]", "[[3.0, 4.0, 5.0]]"),
                new Query_DesiredResult("@.foo[-1]", "[6.0, 7.0, 8.0]"),
                new Query_DesiredResult("@.g`[a-z]oo`", "{\"foo\": [[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]}"),
                // ufunction tests
                new Query_DesiredResult("len(@)", ((JObject)foo).Length.ToString()),
                new Query_DesiredResult("s_mul(@.bar.b, 2)", "[\"a`ga`g\", \"bahbah\"]"),
                new Query_DesiredResult("in(1, @.foo[0])", "true"),
                new Query_DesiredResult("in(4.0, @.foo[0])", "false"),
                new Query_DesiredResult("in(`foo`, @)", "true"),
                new Query_DesiredResult("in(`fjdkfjdkuren`, @)", "false"),
                new Query_DesiredResult("range(2, len(@)) * 3", "[6, 9, 12, 15, 18, 21]"),
                new Query_DesiredResult("sort_by(@.foo, 0, true)[:2]", "[[6.0, 7.0, 8.0], [3.0, 4.0, 5.0]]"),
                new Query_DesiredResult("mean(flatten(@.foo[0]))", "1.0"),
                new Query_DesiredResult("flatten(@.foo)[:4]", "[0, 1, 2, 3.0]"),
                new Query_DesiredResult("flatten(@.guzo, 2)", "[1, 2, 3]"),
                new Query_DesiredResult("min_by(@.foo, 1)", "[0, 1, 2]"),
                new Query_DesiredResult("s_sub(@.bar.b, g`a(\\`?)`, `$1z`)", "[\"`zg\", \"bzh\"]"), // regex to_replace
                new Query_DesiredResult("s_sub(j`[\"12.0\", \"1.5\", \"2\"]`, `.`, ``)", "[\"120\", \"15\", \"2\"]"), // string to_replace with special char
                new Query_DesiredResult("s_sub(j`[\"12.0\", \"1.5\", \"2\"]`, g`.`, ``)", "[\"\", \"\", \"\"]"), // regex to_replace with special char
                new Query_DesiredResult("isna(@.foo[0])", "[false, false, false]"),
                new Query_DesiredResult("s_slice(@.bar.b, 2)", "[\"g\", \"h\"]"),
                new Query_DesiredResult("s_slice(@.bar.b, ::2)", "[\"ag\", \"bh\"]"),
                new Query_DesiredResult("str(@.foo[2])", "[\"6.0\", \"7.0\", \"8.0\"]"),
                new Query_DesiredResult("int(@.foo[1])", "[3, 4, 5]"),
                new Query_DesiredResult("s_slice(str(@.foo[2]), 2:)", "[\"0\", \"0\", \"0\"]"),
                new Query_DesiredResult("sorted(flatten(@.guzo, 2))", "[1, 2, 3]"),
                new Query_DesiredResult("keys(@)", "[\"foo\", \"bar\", \"baz\", \"quz\", \"jub\", \"guzo\", \"7\", \"_\"]"),
                new Query_DesiredResult("values(@.bar)[:]", "[false, [\"a`g\", \"bah\"]]"),
                new Query_DesiredResult("s_join(`\t`, @.bar.b)", "\"a`g\tbah\""),
                new Query_DesiredResult("sorted(unique(@.foo[1]), true)", "[5.0, 4.0, 3.0]"), // have to sort because this function involves a HashSet so order is random
                new Query_DesiredResult("unique(@.foo[0], true)", "[0, 1, 2]"),
                new Query_DesiredResult("sort_by(value_counts(@.foo[0]), 1)", "[[0, 1], [1, 1], [2, 1]]"), // function involves a Dictionary so order is inherently random
                new Query_DesiredResult("sort_by(value_counts(j`[1, 2, 1, 3, 1]`), 0)", "[[1, 3], [2, 1], [3, 1]]"),
                new Query_DesiredResult("quantile(flatten(@.foo[1:]), 0.5)", "5.5"),
                new Query_DesiredResult("float(@.foo[0])[:1]", "[0.0]"),
                new Query_DesiredResult("not(is_expr(values(@.bar)))", "[true, false]"),
                new Query_DesiredResult("round(@.foo[0] * 1.66)", "[0, 2, 3]"),
                new Query_DesiredResult("round(@.foo[0] * 1.66, 1)", "[0.0, 1.7, 3.3]"),
                new Query_DesiredResult("round(@.foo[0] * 1.66, 2)", "[0.0, 1.66, 3.32]"),
                new Query_DesiredResult("s_find(@.bar.b, g`[a-z]+`)", "[[\"a\", \"g\"], [\"bah\"]]"),
                new Query_DesiredResult("s_count(@.bar.b, `a`)", "[1, 1]"),
                new Query_DesiredResult("s_count(@.bar.b, g`[a-z]`)", "[2, 3]"),
                new Query_DesiredResult("ifelse(@.foo[0] > quantile(@.foo[0], 0.5), `big`, `small`)", "[\"small\", \"small\", \"big\"]"),
                new Query_DesiredResult("ifelse(is_num(j`[1, \"a\", 2.0]`), isnum, notnum)", "[\"isnum\", \"notnum\", \"isnum\"]"),
                new Query_DesiredResult("s_upper(j`[\"hello\", \"world\"]`)", "[\"HELLO\", \"WORLD\"]"),
                new Query_DesiredResult("s_strip(` a dog!\t`)", "\"a dog!\""),
                new Query_DesiredResult("log(2.718281828459045 ** @.foo[0])", $"[0.0, 1.0, 2.0]"),
                new Query_DesiredResult("log(j`[10, 100]`, 10)", $"[1.0, 2.0]"),
                new Query_DesiredResult("log2(j`[1, 4, 8]`)", $"[0, 2, 3]"),
                new Query_DesiredResult("abs(j`[-1, 0, 1]`)", "[1, 0, 1]"),
                new Query_DesiredResult("is_str(@.bar.b)", "[true, true]"),
                new Query_DesiredResult("s_split(@.bar.b[0], g`[^a-z]+`)", "[\"a\", \"g\"]"),
                new Query_DesiredResult("s_split(@.bar.b, `a`)", "[[\"\", \"`g\"], [\"b\", \"h\"]]"),
                new Query_DesiredResult("group_by(@.foo, 0)", "{\"0\": [[0, 1, 2]], \"3.0\": [[3.0, 4.0, 5.0]], \"6.0\": [[6.0, 7.0, 8.0]]}"),
                new Query_DesiredResult("group_by(j`[{\"foo\": 1, \"bar\": \"a\"}, {\"foo\": 2, \"bar\": \"b\"}, {\"foo\": 3, \"bar\": \"b\"}]`, bar).*{`sum`: sum(@[:].foo), `count`: len(@)}", "{\"a\": {\"sum\": 1.0, \"count\": 1}, \"b\": {\"sum\": 5.0, \"count\": 2}}"),
                //("agg_by(@.foo, 0, sum(flatten(@)))", "{\"0\": 3.0, \"3.0\": 11.0, \"6.0\": 21.0}"),
                new Query_DesiredResult("index(j`[1,3,2,3,1]`, max(j`[1,3,2,3,1]`), true)", "3"),
                new Query_DesiredResult("index(@.foo[0], min(@.foo[0]))", "0"),
                new Query_DesiredResult("zip(j`[1,2,3]`, j`[\"a\", \"b\", \"c\"]`)", "[[1, \"a\"], [2, \"b\"], [3, \"c\"]]"),
                new Query_DesiredResult("zip(@.foo[0], @.foo[1], @.foo[2], j`[-20, -30, -40]`)", "[[0, 3.0, 6.0, -20], [1, 4.0, 7.0, -30], [2, 5.0, 8.0, -40]]"),
                new Query_DesiredResult("dict(zip(keys(@.bar), j`[1, 2]`))", "{\"a\": 1, \"b\": 2}"),
                new Query_DesiredResult("dict(items(@))", foo.ToString()),
                new Query_DesiredResult("dict(j`[[\"a\", 1], [\"b\", 2], [\"c\", 3]]`)", "{\"a\": 1, \"b\": 2, \"c\": 3}"),
                new Query_DesiredResult("items(j`{\"a\": 1, \"b\": 2, \"c\": 3}`)", "[[\"a\", 1], [\"b\", 2], [\"c\", 3]]"),
                new Query_DesiredResult("isnull(@.foo)", "[false, false, false]"),
                new Query_DesiredResult("int(isnull(j`[1, 1.5, [], \"a\", \"2000-07-19\", \"1975-07-14 01:48:21\", null, false, {}]`))",
                    "[0, 0, 0, 0, 0, 0, 1, 0, 0]"),
                new Query_DesiredResult("range(len(@), 0, -1)", 
                    "[8, 7, 6, 5, 4, 3, 2, 1]"),
                new Query_DesiredResult("range(-10)", "[]"),
                new Query_DesiredResult("range(-3, -5, -1)", "[-3, -4]"),
                new Query_DesiredResult("range(2, 19, -5)", "[]"),
                new Query_DesiredResult("range(2, 19, 5)", "[2, 7, 12, 17]"),
                new Query_DesiredResult("range(3)", "[0, 1, 2]"),
                new Query_DesiredResult("range(3, 5)", "[3, 4]"),
                new Query_DesiredResult("range(-len(@))", "[]"),
                new Query_DesiredResult("range(0, -len(@))", "[]"),
                new Query_DesiredResult("range(0, len(@) - len(@))", "[]"),
                new Query_DesiredResult("range(0, -len(@) + len(@))", "[]"), 
                // uminus'd CurJson appears to be causing problems with other arithmetic binops as the second arg to the range function
                new Query_DesiredResult("range(0, -len(@) - len(@))", "[]"),
                new Query_DesiredResult("range(0, -len(@) * len(@))", "[]"),
                new Query_DesiredResult("range(0, 5, -len(@))", "[]"),
                new Query_DesiredResult("-len(@) + len(@)", "0"), // see if binops of uminus'd CurJson are also causing problems when they're not the second arg to the range function
                new Query_DesiredResult("-len(@) * len(@)", (-(((JObject)foo).Length * ((JObject)foo).Length)).ToString()),
                new Query_DesiredResult("abs(-len(@) + len(@))", "0"), // see if other functions (not just range) of binops of uminus'd CurJson cause problems
                new Query_DesiredResult("range(0, abs(-len(@) + len(@)))", "[]"),
                new Query_DesiredResult("range(0, -abs(-len(@) + len(@)))", "[]"),
                new Query_DesiredResult("add_items(j`{}`, a, 1, b, 2, c, 3, d, 4)", "{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4}"),
                new Query_DesiredResult("add_items(j`{}`, a, null, b, 2, c, null)", "{\"a\": null, \"b\": 2, \"c\": null}"), // null values in add_items
                new Query_DesiredResult("append(j`[]`, 1, false, a, null, j`[4]`, null, 2.5)", "[1, false, \"a\", null, [4], null, 2.5]"),
                new Query_DesiredResult("concat(j`[1, 2]`, j`[3, 4]`, j`[5]`)", "[1, 2, 3, 4, 5]"),
                new Query_DesiredResult("concat(j`{\"a\": 1, \"b\": 2}`, j`{\"c\": 3}`, j`{\"a\": 4}`)", "{\"b\": 2, \"c\": 3, \"a\": 4}"),
                new Query_DesiredResult("pivot(j`[[\"foo\", 2, 3, true], [\"bar\", 3, 3, true], [\"foo\", 4, 4, false], [\"bar\", 5, 4, false]]`, 0, 1, 2, 3)", "{\"foo\": [2, 4], \"bar\": [3, 5], \"2\": [3, 4], \"3\": [true, false]}"),
                new Query_DesiredResult("pivot(j`[{\"a\": true, \"b\": 2, \"c\": 3}, {\"a\": false, \"b\": 3, \"c\": 3}, {\"a\": true, \"b\": 4, \"c\": 4}, {\"a\": false, \"b\": 5, \"c\": 4}]`, a, b, c)", "{\"true\": [2, 4], \"false\": [3, 5], \"c\": [3, 4]}"),
                new Query_DesiredResult("pivot(j`[{\"a\": \"foo\", \"b\": 2, \"c\": 3}, {\"a\": \"bar\", \"b\": 3, \"c\": 3}, {\"a\": \"foo\", \"b\": 4, \"c\": 4}, {\"a\": \"bar\", \"b\": 5, \"c\": 4}]`, a, b, c)", "{\"foo\": [2, 4], \"bar\": [3, 5], \"c\": [3, 4]}"),
                new Query_DesiredResult("to_records(j`{\"a\": [1, 2], \"b\": [2, 3]}`)", "[{\"a\": 1, \"b\": 2}, {\"a\": 2, \"b\": 3}]"),
                new Query_DesiredResult("to_records(j`{\"a\": [1, 2], \"b\": [2, 3]}`, d)", "[{\"a\": 1, \"b\": 2}, {\"a\": 2, \"b\": 3}]"),
                new Query_DesiredResult("to_records(j`{\"a\": [1, 2], \"b\": [2, 3]}`, n)", "[{\"a\": 1, \"b\": 2}, {\"a\": 2, \"b\": 3}]"),
                new Query_DesiredResult("to_records(j`[[1, 2, [3]], [2, 3, [4]]]`, r)", "[{\"col1\": 1, \"col2\": 2, \"col3.col1\": 3}, {\"col1\": 2, \"col2\": 3, \"col3.col1\": 4}]"),
                new Query_DesiredResult("to_records(j`[[1, 2, [3]], [2, 3, [4]]]`, s)", "[{\"col1\": 1, \"col2\": 2, \"col3\": \"[3]\"}, {\"col1\": 2, \"col2\": 3, \"col3\": \"[4]\"}]"),
                new Query_DesiredResult("all(j`[true, true, false]`)", "false"),
                new Query_DesiredResult("all(j`[true, true]`)", "true"),
                new Query_DesiredResult("any(j`[true, true, false]`)", "true"),
                new Query_DesiredResult("any(j`[true, true]`)", "true"),
                new Query_DesiredResult("all(j`[false, false]`)", "false"),
                // parens tests
                new Query_DesiredResult("(@.foo[:2])", "[[0, 1, 2], [3.0, 4.0, 5.0]]"),
                new Query_DesiredResult("(@.foo)[0]", "[0, 1, 2]"),
                // projection tests
                new Query_DesiredResult("@{@.jub, @.quz}", "[[], {}]"),
                new Query_DesiredResult("@.foo{foo: @[0], bar: @[1][:2]}", "{\"foo\": [0, 1, 2], \"bar\": [3.0, 4.0]}"),
                new Query_DesiredResult("sorted(flatten(@.guzo, 2)){`min`: @[0], `max`: @[-1], `tot`: sum(@)}", "{\"min\": 1, \"max\": 3, \"tot\": 6}"),
                new Query_DesiredResult("(@.foo[:]{`max`: max(@), `min`: min(@)})[0]", "{\"max\": 2.0, \"min\": 0.0}"),
                new Query_DesiredResult("len(@.foo[:]{blah: 1})", "3"),
                new Query_DesiredResult("str(@.foo[0]{a: @[0], b: @[1]})", "{\"a\": \"0\", \"b\": \"1\"}"),
                new Query_DesiredResult("max_by(@.foo[:]{mx: max(@), first: @[0]}, mx)", "{\"mx\": 8.0, \"first\": 6.0}"),
                // recursive search
                new Query_DesiredResult("@..g`\\\\d`", "[[{\"foo\": 2}, 1], 0]"),
                new Query_DesiredResult("@..[foo,`0`]", "[[[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]], 2, 0]"),
                new Query_DesiredResult("@..`7`[0].foo", "[2]"),
                new Query_DesiredResult("@._..`0`", "[0]"),
                new Query_DesiredResult("@.bar..[a, b]", "[false, [\"a`g\", \"bah\"]]"),
                new Query_DesiredResult("@.bar..c", "[]"),
                new Query_DesiredResult("@.bar..[a, c]", "[false]"),
                new Query_DesiredResult("@.`7`..foo", "[2]"),
                new Query_DesiredResult("j`{\"a\": [true, 2, [3]], \"b\": {\"c\": [\"d\", \"e\"], \"f\": null}}`..*", "[true, 2, 3, \"d\", \"e\", null]"),
                new Query_DesiredResult("j`{\"a\": 1, \"b\": {\"c\": 2}}`..g`zzz`", "[]"), // return empty array if no keys match
                new Query_DesiredResult("(range(len(@.foo)))[0]", "0"), // indexing on a paren-wrapped non-vectorized arg function result
                new Query_DesiredResult("(2 // @.foo[0])[1]", "2"), // indexing on a paren-wrapped binop result
                new Query_DesiredResult("(3 * range(len(@.foo)))[2]", "6"), // indexing on a paren-wrapped result of a binop (scalar, non-vectorized arg function)
                new Query_DesiredResult("(2 | s_len(str(@.foo[0])))[2]", "3"), // indexing on a paren-wrapped result of a binop (scalar, vectorized arg function)
                new Query_DesiredResult("(range(len(@.foo)) ** 3)[2]", "8.0"), // indexing on a paren-wrapped result of a binop (non-vectorized arg function, scalar)
                new Query_DesiredResult("(s_len(str(@.foo[0])) ^ 3)[2]", "2"), // indexing on a paren-wrapped result of a binop (vectorized arg function, scalar)
            };
            int ii = 0;
            int tests_failed = 0;
            JNode result;
            foreach (Query_DesiredResult qd in testcases)
            {
                ii++;
                JNode jdesired_result = jsonParser.Parse(qd.desired_result);
                try
                {
                    result = remesparser.Search(qd.query, foo, out bool _);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search({qd.query}, foo) to return {jdesired_result.ToString()}, but instead threw" +
                                      $" an exception:\n{ex}");
                    continue;
                }
                if (result.type != jdesired_result.type || !result.Equals(jdesired_result))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search({qd.query}, foo) to return {jdesired_result.ToString()}, " +
                                      $"but instead got {result.ToString()}.");
                }
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }

    class RemesPathThrowsWhenAppropriateTester
    {
        public static void Test()
        {
            int ii = 0;
            int tests_failed = 0;
            JsonParser jsonParser = new JsonParser();
            RemesParser remesParser = new RemesParser();
            List<string[]> testcases = new List<string[]>(new string[][]
            {
                new string[] {"concat(j`[1, 2]`, j`{\"a\": 2}`)", "[]"}, // concat throws with mixed arrays and objects
                new string[] {"concat(@, 1)", "[1, 2]"}, // concat throws with non-iterables
                new string[] {"concat(@, j`{\"b\": 2}`, 1)", "{\"a\": 1}"}, // concat throws with non-iterables
                new string[] {"to_records(@, g)", "{\"a\": [1, 2]}" }, // to_records throws when second arg is not n, r, d, or s 
});
            // test issue where sometimes a binop does not raise an error when it operates on two invalid types
            string[] invalid_others = new string[] { "{}", "[]", "\"1\"" };
            foreach (string other in invalid_others)
            {
                foreach (string bop in Binop.BINOPS.Keys)
                {
                    testcases.Add(new string[] { $"@ {bop} 1", $"[{other}]" });
                    testcases.Add(new string[] { $"1 {bop} @", $"[{other}]" });
                    // now try it with JSON literals defined inside the query
                    testcases.Add(new string[] { $"j`[{other}]` {bop} 1", "null" });
                    testcases.Add(new string[] { $"1 {bop} j`[{other}]`", "null" });
                    // now do it for booleans
                    testcases.Add(new string[] { $"@ {bop} true", $"[{other}]" });
                    testcases.Add(new string[] { $"true {bop} @", $"[{other}]" });
                    testcases.Add(new string[] { $"j`[{other}]` {bop} true", "null" });
                    testcases.Add(new string[] { $"true {bop} j`[{other}]`", "null" });
                    // now do it for doubles
                    testcases.Add(new string[] { $"@ {bop} 1.0", $"[{other}]" });
                    testcases.Add(new string[] { $"1.0 {bop} @", $"[{other}]" });
                    testcases.Add(new string[] { $"j`[{other}]` {bop} 1.0", "null" });
                    testcases.Add(new string[] { $"1.0 {bop} j`[{other}]`", "null" });
                }
            }
            foreach (string[] test in testcases)
            {
                ii++;
                string query = test[0];
                string inpstr = test[1];
                JNode inp = null;
                try
                {
                    inp = jsonParser.Parse(inpstr);
                }
                catch
                {
                    tests_failed++;
                    Npp.AddLine($"Got error while trying to parse input {inpstr}");
                    continue;
                }
                try
                {
                    JNode bad_result = remesParser.Search(query, inp, out bool _);
                    tests_failed++;
                    Npp.AddLine($"Expected Search({query}, {inpstr}) to raise exception, but instead returned {bad_result.ToString()}");
                }
                catch { }
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }

    class RemesPathAssignmentTester
    {
        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            RemesParser remesParser = new RemesParser();
            int ii = 0;
            int tests_failed = 0;
            JNode result;
            string foostr = "{\"foo\": [-1, 2, 3], \"bar\": \"abc\", \"baz\": \"de\"}";
            string[][] testcases = new string[][] // input, query, input after mutation
            {
                new string[]{ "2", "@ = 3", "3" },
                new string[]{ "[2, 3]", "@ = @ * 3", "[6, 9]" },
                new string[]{ "[\"a\", \"b\"]", "@[0] = s_mul(@, 3)", "[\"aaa\", \"b\"]"},
                new string[]{ "[{\"a\": [1, 3]}]", "@[0].a[@ > 2] = @ - 1", "[{\"a\": [1, 2]}]"},
                new string[]{foostr, "@.foo[@ < 0] = @ + 1",
                    "{\"foo\": [0, 2, 3], \"bar\": \"abc\", \"baz\": \"de\"}"},
                new string[]{foostr, "@.bar = s_slice(@, :2)",
                    "{\"foo\": [-1, 2, 3], \"bar\": \"ab\", \"baz\": \"de\"}" },
                new string[]{ foostr, "@.g`b` = s_len(@)",
                    "{\"foo\": [-1, 2, 3], \"bar\": 3, \"baz\": 2}" },
            };
            foreach (string[] test in testcases)
            {
                string inpstr = test[0];
                JNode inp = jsonParser.Parse(test[0]);
                string query = test[1];
                JNode jdesired_result = jsonParser.Parse(test[2]);
                ii += 2;
                try
                {
                    result = remesParser.Search(query, inp, out bool _);
                }
                catch (Exception ex)
                {
                    tests_failed += 2;
                    Npp.AddLine($"Expected remesparser.Search({query}, {inpstr}) to mutate {inpstr} into {jdesired_result.ToString()}, but instead threw" +
                                      $" an exception:\n{ex}");
                    continue;
                }
                if (inp.type != jdesired_result.type || !inp.Equals(jdesired_result))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search({query}, {inpstr}) to mutate {inpstr} into {jdesired_result.ToString()}, " +
                                      $"but instead got {inp.ToString()}.");
                }
                if (result.type != jdesired_result.type || !result.Equals(jdesired_result))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search({query}, {inpstr}) to return {jdesired_result.ToString()}, " +
                                      $"but instead got {result.ToString()}.");
                }
            }

            // test throws errors when expected
            foostr = "{\"foo\": [1]}";
            string[][] fail_cases = new string[][]
            {
                new string[]{"@.foo = len(@)", foostr},
                new string[]{"@.foo = @{a: len(@)}", foostr},
                new string[]{"@.foo[0] = j`[1]`", foostr},
            };

            foreach (string[] test in fail_cases)
            {
                ii++;
                try
                {
                    string query = test[0];
                    string inpstr = test[1];
                    JNode inp = jsonParser.Parse(inpstr);
                    JNode bad_result = remesParser.Search(query, inp, out bool _);
                    tests_failed++;
                    Npp.AddLine($"Expected Search({query}, {inpstr}) to raise exception, but instead returned {bad_result.ToString()}");
                }
                catch { }
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }

    class BinopTester
    {
        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            JNode jtrue = jsonParser.Parse("true"); JNode jfalse = jsonParser.Parse("false");
            var testcases = new object[][]
            {
                new object[]{ jsonParser.Parse("1"), jsonParser.Parse("3"), Binop.BINOPS["-"], jsonParser.Parse("-2"), "subtraction of ints" },
                new object[]{ jsonParser.Parse("2.5"), jsonParser.Parse("5"), Binop.BINOPS["/"], jsonParser.Parse("0.5"), "division of float by int" },
                new object[]{ jsonParser.Parse("\"a\""), jsonParser.Parse("\"b\""), Binop.BINOPS["+"], jsonParser.Parse("\"ab\""), "addition of strings" },
                new object[]{ jsonParser.Parse("3"), jsonParser.Parse("4"), Binop.BINOPS[">="], jfalse, "comparison ge" },
                new object[]{ jsonParser.Parse("7"), jsonParser.Parse("9"), Binop.BINOPS["<"], jtrue, "comparison lt" },
                new object[]{ jsonParser.Parse("\"abc\""), jsonParser.Parse("\"ab+\""), Binop.BINOPS["=~"], jtrue, "has_pattern" },
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (object[] test in testcases)
            {
                JNode x = (JNode)test[0], y = (JNode)test[1], desired = (JNode)test[3];
                Binop bop = (Binop)test[2];
                string msg = (string)test[4];
                JNode output = bop.Call(x, y);
                string str_desired = desired.ToString();
                string str_output = output.ToString();
                if (str_desired != str_output)
                {

                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} (input \"{1}({2}, {3})\", {4}) failed:\n" +
                                                    "Expected\n{5}\nGot\n{6}",
                                                    ii + 1, bop, x.ToString(), y.ToString(), msg, str_desired, str_output));
                }
                ii++;
            }
            ii = testcases.Length;
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }

    class ArgFunctionTester
    {
        public static void Test()
        {
            JsonParser jsonParser = new JsonParser();
            JNode jtrue = jsonParser.Parse("true");
            JNode jfalse = jsonParser.Parse("false");
            var testcases = new object[][]
            {
                new object[]{ new List<JNode>{jsonParser.Parse("[1,2]")}, ArgFunction.FUNCTIONS["len"], new JNode(Convert.ToInt64(2), Dtype.INT, 0) },
                new object[]{ new List<JNode>{jsonParser.Parse("[1,2]"), jtrue}, ArgFunction.FUNCTIONS["sorted"], jsonParser.Parse("[2,1]") },
                new object[]{ new List<JNode>{jsonParser.Parse("[[1,2], [4, 1]]"), new JNode(Convert.ToInt64(1), Dtype.INT, 0), jfalse }, ArgFunction.FUNCTIONS["sort_by"], jsonParser.Parse("[[4, 1], [1, 2]]") },
                new object[]{ new List<JNode>{jsonParser.Parse("[1, 3, 2]")}, ArgFunction.FUNCTIONS["mean"], new JNode(2.0, Dtype.FLOAT, 0) },
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (object[] test in testcases)
            {
                List<JNode> args = (List<JNode>)test[0];
                ArgFunction f = (ArgFunction)test[1];
                JNode desired = (JNode)test[2];
                JNode output = f.Call(args);
                var sb = new StringBuilder();
                sb.Append('{');
                int argnum = 0;
                while (argnum < args.Count)
                {
                    sb.Append(args[argnum++].ToString());
                    if (argnum < (args.Count - 1)) { sb.Append(", "); }
                }
                sb.Append('}');
                string argstrings = sb.ToString();
                string str_desired = desired.ToString();
                string str_output = output.ToString();
                if (str_desired != str_output)
                {

                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} (input \"{1}({2}) failed:\nExpected\n{3}\nGot\n{4}",
                                                    ii + 1, f, argstrings, str_desired, str_output));
                }
                ii++;
            }
            ii = testcases.Length;
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }
}
