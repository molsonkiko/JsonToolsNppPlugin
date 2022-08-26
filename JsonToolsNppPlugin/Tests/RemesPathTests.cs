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
                new object[] { "@ + 2", new List<object>(new object[]{new CurJson(), Binop.BINOPS["+"], (long)2}), "cur_json binop scalar" },
                new object[] { "2.5 7e5 3.2e4 ", new List<object>(new object[]{2.5, 7e5, 3.2e4}), "all float formats" },
                new object[] { "abc_2 `ab\\`c`", new List<object>(new object[]{"abc_2", "ab`c"}), "unquoted and quoted strings" },
                new object[] { "len(null, Infinity)", new List<object>(new object[]{ArgFunction.FUNCTIONS["len"], '(', null, ',', NanInf.inf, ')'}), "arg function, constants, delimiters" },
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
                    Npp.AddLine(String.Format("Test {0} (input \"{1}\", {2}) failed:\n" +
                                                    "Expected\n{3}\nGot\n{4}",
                                                    ii + 1, input, msg, str_desired, str_output));
                }
                ii++;
            }
            ii = testcases.Length;
            foreach (string paren in new string[] { "(", ")", "[", "]", "{", "}" })
            {
                try
                {
                    lexer.Tokenize(paren);
                    Npp.AddLine($"Test {ii} failed, expected exception due to unmatched '{paren}'");
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
                Npp.AddLine($"Test {ii} failed, expected exception due to number with two decimal points");
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
                new Query_DesiredResult("s_sub(@.bar.b, g`a(\\`?)`, `$1z`)", "[\"`zg\", \"bzh\"]"),
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
                new Query_DesiredResult("log(@.foo[0] + 1)", $"[0.0, {Math.Log(2)}, {Math.Log(3)}]"),
                new Query_DesiredResult("log2(@.foo[1])", $"[{Math.Log(3, 2)}, 2.0, {Math.Log(5, 2)}]"),
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
                new Query_DesiredResult("@.bar..c", "{}"),
                new Query_DesiredResult("@.bar..[a, c]", "[false]"),
                new Query_DesiredResult("@.`7`..foo", "[2]"),
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
                    result = remesparser.Search(qd.query, foo);
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
                new object[]{ new JNode[]{jsonParser.Parse("[1,2]")}, ArgFunction.FUNCTIONS["len"], new JNode(Convert.ToInt64(2), Dtype.INT, 0) },
                new object[]{ new JNode[]{jsonParser.Parse("[1,2]"), jtrue}, ArgFunction.FUNCTIONS["sorted"], jsonParser.Parse("[2,1]") },
                new object[]{ new JNode[]{jsonParser.Parse("[[1,2], [4, 1]]"), new JNode(Convert.ToInt64(1), Dtype.INT, 0), jfalse }, ArgFunction.FUNCTIONS["sort_by"], jsonParser.Parse("[[4, 1], [1, 2]]") },
                new object[]{ new JNode[]{jsonParser.Parse("[1, 3, 2]")}, ArgFunction.FUNCTIONS["mean"], new JNode(2.0, Dtype.FLOAT, 0) },
                //(new JNode[]{jsonParser.Parse("[{\"a\": 1, \"b\": 2}, {\"a\": 3, \"b\": 1}]"), new JNode("b", Dtype.STR, 0)}, ArgFunction.FUNCTIONS["min_by"], jsonParser.Parse("{\"a\": 3, \"b\": 1}")),
                //(new JNode[]{jsonParser.Parse("[\"ab\", \"bca\", \"\"]")}, ArgFunction.FUNCTIONS["s_len"], jsonParser.Parse("[2, 3, 0]")),
                //(new JNode[]{jsonParser.Parse("[\"ab\", \"bca\", \"\"]"), new JNode("a", Dtype.STR, 0), new JNode("z", Dtype.STR, 0)}, ArgFunction.FUNCTIONS["s_sub"], jsonParser.Parse("[\"zb\", \"bcz\", \"\"]")),
                //(new JNode[]{jsonParser.Parse("[\"ab\", \"bca\", \"\"]"), new JRegex(new Regex(@"a+")), new JNode("z", Dtype.STR, 0)}, ArgFunction.FUNCTIONS["s_sub"], jsonParser.Parse("[\"zb\", \"bcz\", \"\"]")),
                //(new JNode[]{jsonParser.Parse("[\"ab\", \"bca\", \"\"]"), new JSlicer(new int?[] {1, null, -1})}, ArgFunction.FUNCTIONS["s_slice"], jsonParser.Parse("[\"ba\", \"cb\", \"\"]")),
                //(new JNode[]{jsonParser.Parse("{\"a\": \"2\", \"b\": \"1.5\"}")}, ArgFunction.FUNCTIONS["float"], jsonParser.Parse("{\"a\": 2.0, \"b\": 1.5}")),
                //(new JNode[]{jsonParser.Parse("{\"a\": \"a\", \"b\": \"b\"}"), new JNode(3, Dtype.INT, 0)}, ArgFunction.FUNCTIONS["s_mul"], jsonParser.Parse("{\"a\": \"aaa\", \"b\": \"bbb\"}"))
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (object[] test in testcases)
            {
                JNode[] args = (JNode[])test[0];
                ArgFunction f = (ArgFunction)test[1];
                JNode desired = (JNode)test[2];
                JNode output = f.Call(args);
                var sb = new StringBuilder();
                sb.Append('{');
                int argnum = 0;
                while (argnum < args.Length)
                {
                    sb.Append(args[argnum++].ToString());
                    if (argnum < (args.Length - 1)) { sb.Append(", "); }
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
