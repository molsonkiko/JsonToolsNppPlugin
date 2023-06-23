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
            var testcases = new (string input, List<object> toks, string msg)[]
            {
                ( "@ + 2", new List<object>{new CurJson(), Binop.BINOPS["+"], 2L}, "cur_json binop scalar" ),
                ( "2.5 7e5 3.2e4 ", new List<object>{2.5, 7e5, 3.2e4}, "all float formats" ),
                ( "abc_2 `ab\\`c` \\ud83d\\ude00_$\\u1ed3", new List<object>{"abc_2", "ab`c", "😀_$ồ"}, "unquoted and quoted strings" ),
                ( "len(null, Infinity)", new List<object>{"len", '(', null, ',', NanInf.inf, ')'}, "arg function, constants, delimiters" ),
                ( "j`[1,\"a\\`\"]`", new List<object>{jsonParser.Parse("[1,\"a`\"]")}, "json string" ),
                ( "g`a?[b\\`]`", new List<object>{new Regex(@"a?[b`]") }, "regex" ),
                ( " - /", new List<object>{Binop.BINOPS["-"], Binop.BINOPS["/"]}, "more binops" ),
                ( ". []", new List<object>{'.', '[', ']'}, "more delimiters" ),
                ( "3blue", new List<object>{3L, "blue"}, "number immediately followed by string" ),
                ( "2.5+-2", new List<object>{2.5, Binop.BINOPS["+"], Binop.BINOPS["-"], 2L}, "number binop binop number, no whitespace" ),
                ( "`a`+@", new List<object>{"a", Binop.BINOPS["+"], new CurJson()}, "quoted string binop curjson, no whitespace" ),
                ( "== in =~", new List<object>{Binop.BINOPS["=="], "in", Binop.BINOPS["=~"]}, "two-character binops and argfunction in" ),
                ( "@[1,2]/3", new List<object>{new CurJson(), '[', 1L, ',', 2L, ']', Binop.BINOPS["/"], 3L}, "numbers and delimiters then binop number, no whitespace" ),
                ( "2 <=3!=", new List<object>{2L, Binop.BINOPS["<="], 3L, Binop.BINOPS["!="] }, "!=" ),
                ( "8 = @ * 79 ==", new List<object>{ 8L, '=', new CurJson(), Binop.BINOPS["*"], 79L, Binop.BINOPS["=="] }, "assignment operator" ),
            };
            int tests_failed = 0;
            int ii = 0;
            foreach ((string input, List<object> toks, string msg) in testcases)
            {
                string correct_str = RemesPathLexer.TokensToString(toks);
                List<object> got_toks = null;
                ii++;
                try
                {
                    got_toks = lexer.Tokenize(input);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine($"Test {ii} (input \"{input}\", {msg}) failed:\r\n" +
                                $"Expected toks = {correct_str}\r\nInstead raised exception\r\n{ex}");
                    continue;
                }
                string got_str = RemesPathLexer.TokensToString(got_toks);
                if (correct_str != got_str)
                {

                    tests_failed++;
                    Npp.AddLine($"Test {ii} (input \"{input}\", {msg}) failed:\r\n" +
                                $"Expected toks = {correct_str}\r\nInstead got toks = {got_str}");
                }
            }
            // test exeptions related to unmatched brackets
            foreach (string paren in new string[] { "(", ")", "[", "]", "{", "}" })
            {
                ii++;
                try
                {
                    lexer.Tokenize(paren);
                    Npp.AddLine($"Test {ii} (query \"{paren}\") failed, expected exception due to unmatched '{paren}'");
                    tests_failed++;
                }
                catch { }
            }
            // test if two decimal points causes error
            string query;
            //ii++;
            //try
            //{
            //    query = "1.5.2";
            //    lexer.Tokenize(query);
            //    tests_failed++;
            //    Npp.AddLine($"Test {ii} (query \"{query}\") failed, expected exception due to number with two decimal points");
            //}
            //catch { }
            // test if recursion limit of 512 is enforced
            ii++;
            try
            {
                query = $"{new string('(', 1000)}1{new string(')', 1000)}";
                lexer.Tokenize(query);
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
                // subtraction
                new Query_DesiredResult("j`[ 1,   1,    1, 0.5,  0.5,  0.5, false, false, false]` - " +
                                        "j`[ 2, 2.0, true,   2,  2.0, true,     2,   2.0,  true]`",
                                          "[-1,-1.0,    0,-1.5, -1.5, -0.5,    -2,  -2.0,    -1]"),
                // addition
                new Query_DesiredResult("j`[ 1,   1,    1, 0.5,  0.5,  0.5, false, false, false,  \"a\"]` + " +
                                        "j`[ 2, 2.0, true,   2,  2.0, true,     2,   2.0,  true,  \"b\"]`",
                                          "[ 3, 3.0,    2, 2.5,  2.5,  1.5,     2,   2.0,     1, \"ab\"]"),
                // multiplication
                new Query_DesiredResult("j`[ 1,   1,    1, 0.5,  0.5,  0.5, false, false, false, \"a\", \"a\"]` * " +
                                        "j`[ 2, 2.0, true,   2,  2.0, true,     2,   2.0,  true,     2, false]`",
                                          "[ 2, 2.0,    1, 1.0,  1.0,  0.5,     0,   0.0,     0,\"aa\",  \"\"]"),
                // division
                new Query_DesiredResult("j`[  1,   1,    1, 0.5,  0.5,  0.5, false, false, false]` / " +
                                        "j`[  2, 2.0, true,   2,  2.0, true,     2,   2.0,  true]`",
                                          "[0.5, 0.5,  1.0,0.25, 0.25,  0.5,   0.0,   0.0,   0.0]"),
                // exponentiation
                new Query_DesiredResult("j`[  1,   1,    1, 0.5,  0.5,  0.5, false, false, false]` ** " +
                                        "j`[  2, 2.0, true,   2,  3.0, true,     2,   2.0,  true]`",
                                          "[1.0, 1.0,  1.0,0.25,0.125,  0.5,   0.0,   0.0,   0.0]"),
                // floor division
                new Query_DesiredResult("j`[  5,   6,    1, 0.5,  7.5,  6.2, false,  true, false]` // " +
                                        "j`[  2, 3.0, true,   2,  2.0, true,     2,   1.0,  true]`",
                                          "[  2,   2,    1,   0,    3,    6,     0,     1,     0]"),
                // modulo
                new Query_DesiredResult("j`[  5,   6,    1, 2.5,  0.5,  0.5,  true,  true, false]` % " +
                                        "j`[  2, 3.0, true,   1,  2.0, true, 0.625,   2.0,  true]`",
                                          "[  1, 0.0,    0, 0.5,  0.5,  0.5, 0.375,   1.0,     0]"),
                // XOR
                new Query_DesiredResult("j`[   4, false,  true, 3]` ^ " +
                                        "j`[true, false,     1, 1]`",
                                          "[   5, false,     0, 2]"),
                // AND
                new Query_DesiredResult("j`[   4, false,  true, 3]` & " +
                                        "j`[true, false,     1, 1]`",
                                          "[   0, false,     1, 1]"),
                // OR
                new Query_DesiredResult("j`[   4, false,  true, 3]` | " +
                                        "j`[true, false,     1, 1]`",
                                          "[   5, false,     1, 3]"),
                // comparison binops
                new Query_DesiredResult("j`[1, true, 1.0, 1.0]` == j`[1.0, -1, false, 2]`", "[true, false, false, false]"),
                new Query_DesiredResult("j`[1, true, 1.0, 1.0]` <= j`[1.0, -1, false, 2]`", "[true, false, false, true]"),
                new Query_DesiredResult("j`[1, true, 1.0, 1.0]` >= j`[1.0, -1, false, 2]`", "[true, true,  true,  false]"),
                new Query_DesiredResult("j`[1, true, 1.0, 1.0]` <  j`[1.0, -1, false, 2]`", "[false, false, false,  true]"),
                new Query_DesiredResult("j`[1, true, 1.0, 1.0]` >  j`[1.0, -1, false, 2]`", "[false, true, true,  false]"),
                new Query_DesiredResult("j`[1, true, 1.0, 1.0]` !=  j`[1.0, -1, false, 2]`", "[false, true, true,  true]"),
                new Query_DesiredResult("append(j`[]`{  abc,   bcd} =~ g`^a`, def =~ g)", // pattern matching
                                                    "[true,  false,           false]"),
                // binop precedence change tests (2 binops)
                new Query_DesiredResult("2 - 4 * 2", "-6"), // ascend
                new Query_DesiredResult("2 * 4 - 2", "6"), // descend
                new Query_DesiredResult("2 - 2 - 4", "-4"), // same
                // binop precedence change tests (3 binops)
                new Query_DesiredResult("1 - 1 * 2 ** 3", "-7.0"), // up up
                new Query_DesiredResult("1 + 2 / 2 * 4", "5.0"), // up same
                new Query_DesiredResult("1 + 2 / 2 - 3", "-1.0"), // up down
                new Query_DesiredResult("1 * 2 - 2 ** 3", "-6.0"), // down up
                new Query_DesiredResult("3 * 2 - 2 + 3", "7"), // down same
                new Query_DesiredResult("2 ** 2 * 3 + 3", "15.0"), // down down
                new Query_DesiredResult("1 / 2 * 3 ** 3", "13.5"), // same up
                new Query_DesiredResult("1 / 2 * 3 * 3", "4.5"), // same same
                new Query_DesiredResult("1 / 2 * 3 + 3", "4.5"), // same down
                new Query_DesiredResult("2 / 4 ** 2 ** 2", "0.0078125"), // up exp exp
                new Query_DesiredResult("4 ** 2 ** 2 / 4", "64.0"), // exp exp down
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
                new Query_DesiredResult("j`[\"a\", \"b\", \"c\"]` * 2", "[\"aa\", \"bb\", \"cc\"]"),
                new Query_DesiredResult("a * j`[1, 2, 3]`", "[\"a\", \"aa\", \"aaa\"]"),
                new Query_DesiredResult("ab * 2", "\"abab\""),
                new Query_DesiredResult("j`[\"a\", \"b\"]` * j`[1, 2]`", "[\"a\", \"bb\"]"),
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
                new Query_DesiredResult("-j`[1,2]`**-3", "[-1.0, -0.125]"), // check uminus comes after exponentiation
                new Query_DesiredResult("-@.foo[2]", "[-6.0, -7.0, -8.0]"),
                new Query_DesiredResult("2/--4", "0.5"),
                new Query_DesiredResult("---@.foo[2] + 3", "[-3.0, -4.0, -5.0]"), // check uminus precedes +
                new Query_DesiredResult("(-@.foo[0])[::2]", "[0.0, -2.0]"),
                // unary not tests
                new Query_DesiredResult("ifelse(not 1.5, a, b)", "\"b\""),
                new Query_DesiredResult("not not j`[2, 0.0, true]`", "[true, false, true]"),
                new Query_DesiredResult("not j`[true, false]` & j`[false, true]`", "[true, true]"),
                new Query_DesiredResult("- not -j`[true, false]`", "[0, -1]"),
                new Query_DesiredResult("1 + not j`[true, false]`", "[1, 2]"),
                new Query_DesiredResult("- not - not j`[\"a\", \"\", [], [1], {}, {\"a\": 1}, null]`",
                                                       "[-1,    0,   0,  -1,  0,    -1,        0]"),
                new Query_DesiredResult("(not @.foo[0])[:2]", "[true, false]"),
                new Query_DesiredResult("not not not not not @.foo[0]", "[true, false, false]"),
                new Query_DesiredResult("not not not not not @.foo[0][1] + 3", "false"),
                // unary plus tests
                new Query_DesiredResult("+ -2", "-2"),
                new Query_DesiredResult("str(+ range(-2, 3, 2))", "[\"-2\", \"0\", \"2\"]"),
                new Query_DesiredResult("not + j`[3, 0]`", "[false, true]"),
                new Query_DesiredResult("+ - + not not not @.foo[1][0] + 3", "true"), // equivalent to not ((-@.foo[1][0]) + 3), equivalent to not (-3 + 3), so true
                new Query_DesiredResult("not - + @.foo[0][0] ** 1.0", "true"),
                new Query_DesiredResult("- + (@{a, b} == a)", "[-1, 0]"),
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
                new Query_DesiredResult("min_by(@.foo[:]{a: @[0], b: @[1], c: @[2]}, a)", "{\"a\": 0, \"b\": 1, \"c\": 2}"),
                new Query_DesiredResult("s_sub(@.bar.b, g`a(\\`?)`, `$1z`)", "[\"`zg\", \"bzh\"]"), // regex to_replace
                new Query_DesiredResult("s_sub(j`[\"12.0\", \"1.5\", \"2\"]`, `.`, ``)", "[\"120\", \"15\", \"2\"]"), // string to_replace with special char
                new Query_DesiredResult("s_sub(j`[\"12.0\", \"1.5\", \"2\"]`, g`.`, ``)", "[\"\", \"\", \"\"]"), // regex to_replace with special char
                new Query_DesiredResult("isna(@.foo[0])", "[false, false, false]"),
                new Query_DesiredResult("s_slice(@.bar.b, 2)", "[\"g\", \"h\"]"),
                new Query_DesiredResult("s_slice(@.bar.b, ::2)", "[\"ag\", \"bh\"]"),
                new Query_DesiredResult("str(@.foo[2])", "[\"6.0\", \"7.0\", \"8.0\"]"),
                new Query_DesiredResult("int(@.foo[1])", "[3, 4, 5]"),
                new Query_DesiredResult("s_slice(str(@.foo[2]), 2:)", "[\"0\", \"0\", \"0\"]"),
                new Query_DesiredResult("s_slice(str(@.foo[2]), ::-1)", "[\"0.6\", \"0.7\", \"0.8\"]"),
                new Query_DesiredResult("sorted(flatten(@.guzo, 2))", "[1, 2, 3]"),
                new Query_DesiredResult("keys(@)", "[\"foo\", \"bar\", \"baz\", \"quz\", \"jub\", \"guzo\", \"7\", \"_\"]"),
                new Query_DesiredResult("values(@.bar)[:]", "[false, [\"a`g\", \"bah\"]]"),
                new Query_DesiredResult("s_join(`\t`, @.bar.b)", "\"a`g\\tbah\""),
                new Query_DesiredResult("sorted(unique(@.foo[1]), true)", "[5.0, 4.0, 3.0]"), // have to sort because this function involves a HashSet so order is random
                new Query_DesiredResult("unique(@.foo[0], true)", "[0, 1, 2]"),
                new Query_DesiredResult("sort_by(value_counts(@.foo[0]), 1)", "[[0, 1], [1, 1], [2, 1]]"), // function involves a Dictionary so order is inherently random
                new Query_DesiredResult("value_counts(j`[\"a\", \"b\", \"c\", \"c\", \"c\", \"b\"]`, true)", "[[\"c\", 3], [\"b\", 2], [\"a\", 1]]"),
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
                new Query_DesiredResult("enumerate(j`[\"a\", \"b\", \"c\"]`)", "[[0, \"a\"], [1, \"b\"], [2, \"c\"]]"),
                new Query_DesiredResult("iterable(j`[1,2,3]`)", "true"),
                new Query_DesiredResult("iterable(a)", "false"),
                new Query_DesiredResult("iterable(j`{\"a\": 1}`)", "true"),
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
                new Query_DesiredResult("max_by(@.foo[:]{-@}[0], 1)", "[0, -1, -2]"),
                new Query_DesiredResult("max_by(@.foo[:]{mx: max(@), first: @[0]}, mx)", "{\"mx\": 8.0, \"first\": 6.0}"),
                new Query_DesiredResult("@{`\t\b\x12`: 1}", "{\"\\t\\b\\u0012\": 1}"), // control characters in projection key
                new Query_DesiredResult("@{foo: 1, $baz: 2, 草: 2, _quЯ: 3, \\ud83d\\ude00_$\\u1ed3: 4, a\\uff6acf: 5, \\u0008\\u000a: 6}", // JSON5-compliant unquoted strings
                    "{\"foo\": 1, \"$baz\": 2, \"草\": 2, \"_quЯ\": 3, \"😀_$ồ\": 4, \"aｪcf\": 5, \"\\\\b\\\\n\": 6}"),
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
                new Query_DesiredResult("(6 // @.foo[1])[1]", "1"), // indexing on a paren-wrapped binop result
                new Query_DesiredResult("(3 * range(len(@.foo)))[2]", "6"), // indexing on a paren-wrapped result of a binop (scalar, non-vectorized arg function)
                new Query_DesiredResult("(2 | s_len(str(@.foo[0])))[2]", "3"), // indexing on a paren-wrapped result of a binop (scalar, vectorized arg function)
                new Query_DesiredResult("(range(len(@.foo)) ** 3)[2]", "8.0"), // indexing on a paren-wrapped result of a binop (non-vectorized arg function, scalar)
                new Query_DesiredResult("(s_len(str(@.foo[0])) ^ 3)[2]", "2"), // indexing on a paren-wrapped result of a binop (vectorized arg function, scalar)
                new Query_DesiredResult("j`{\"items\": [\"a\", 1]}`.items", "[\"a\", 1]"), // using an ArgFunction name as a string rather than an ArgFunction
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
                if (!(result.type == jdesired_result.type && result.Equals(jdesired_result)))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search({qd.query}, foo) to return {jdesired_result.ToString()}, " +
                                      $"but instead got {result.ToString()}.");
                }
            }
            // the rand() function requires a special test because its output is nondeterministic
            ii += 3;
            bool test_failed = false;
            for (int randNum = 0; randNum < 40 && !test_failed; randNum++)
            {
                // rand()
                try
                {
                    result = remesparser.Search("rand()", foo);
                    if (!(result.value is double d && d >= 0d && d < 1d))
                    {
                        test_failed = true;
                        tests_failed++;
                        Npp.AddLine($"Expected remesparser.Search(rand(), foo) to return a double between 0 and 1, but instead got {result.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    test_failed = true;
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search(rand(), foo) to return a double between 0 and 1 but instead threw" +
                                      $" an exception:\n{ex}");
                }
                // argfunction on rand()
                try
                {
                    result = remesparser.Search("ifelse(rand() < 0.5, a, b)", foo);
                    if (!(result.value is string s && (s == "a" || s == "b")))
                    {
                        test_failed = true;
                        tests_failed++;
                        Npp.AddLine($"Expected remesparser.Search(ifelse(rand(), a, b), foo) to return \"a\" or \"b\", but instead got {result.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    test_failed = true;
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search(ifelse(rand(), a, b), foo) to return \"a\" or \"b\" but instead threw" +
                                      $" an exception:\n{ex}");
                }
                // rand() in projection (make sure not all doubles are equal)
                try
                {
                    result = remesparser.Search("j`[1,2,3]`[:]{rand()}[0]", foo);
                    if (!(result is JArray arr) || arr.children.All(x => x == arr[0]))
                    {
                        test_failed = true;
                        tests_failed++;
                        Npp.AddLine($"Expected remesparser.Search(j`[1,2,3]`{{{{rand(@)}}}}[0], foo) to return array of doubles that aren't all equal" +
                            $", but instead got {result.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    test_failed = true;
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search(j`[1,2,3]`{{{{rand(@)}}}}[0] to return array of doubles that aren't equal, but instead threw" +
                                      $" an exception:\n{ex}");
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
                new [] {"concat(j`[1, 2]`, j`{\"a\": 2}`)", "[]"}, // concat throws with mixed arrays and objects
                new [] {"concat(@, 1)", "[1, 2]"}, // concat throws with non-iterables
                new [] {"concat(@, j`{\"b\": 2}`, 1)", "{\"a\": 1}"}, // concat throws with non-iterables
                new [] {"to_records(@, g)", "{\"a\": [1, 2]}" }, // to_records throws when second arg is not n, r, d, or s 
                new [] {"blahrurn(@, a)", "{}"}, // something that's not the name of an ArgFunction used where an ArgFunction was expected
                new []{"= a", "\"b\""}, // assignment with no LHS
                new []{"a = ", "\"b\""}, // assignment with no RHS
                new []{"a = = b", "\"b\""}, // two assignment operators
                new []{"+ a", "1"}, // unary functions with invalid args
                new []{"+[]", "1" },
                new []{"+{}", "1" }, 
                new []{"not g`a`", "1"},
                new []{"-a", "1"},
                new []{"-[[]]", "1" },
                new []{"-@", "[{}]"},
            });
            // test issue where sometimes a binop does not raise an error when it operates on two invalid types
            string[] invalid_others = new string[] { "{}", "[]", "\"1\"" };
            foreach (string other in invalid_others)
            {
                foreach (string bop in Binop.BINOPS.Keys)
                {
                    if (!(other == "\"1\"" && bop == "*"))
                    {
                        testcases.Add(new string[] { $"@ {bop} 1", $"[{other}]" });
                        testcases.Add(new string[] { $"@ {bop} true", $"[{other}]" });
                        testcases.Add(new string[] { $"j`[{other}]` {bop} true", "null" });
                        testcases.Add(new string[] { $"j`[{other}]` {bop} 1", "null" });
                    }
                    testcases.Add(new string[] { $"1 {bop} @", $"[{other}]" });
                    // now try it with JSON literals defined inside the query
                    testcases.Add(new string[] { $"1 {bop} j`[{other}]`", "null" });
                    // now do it for booleans
                    testcases.Add(new string[] { $"true {bop} @", $"[{other}]" });
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
                    JNode bad_result = remesParser.Search(query, inp);
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
                    result = remesParser.Search(query, inp);
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
            foostr = "{\"foo\": [1], \"bar\": {\"a\": 1}}";
            string[][] fail_cases = new string[][]
            {
                new string[]{"@.foo = len(@)", foostr},
                new string[]{"@.foo = @{a: len(@)}", foostr},
                new string[]{"@.foo[0] = j`[1]`", foostr},
                new string[]{"@.bar = len(@)", foostr},
                new string[]{"@.bar = j`[1]`", foostr},
                new string[]{"@.bar.a = j`[1]`", foostr},
            };

            foreach (string[] test in fail_cases)
            {
                ii++;
                try
                {
                    string query = test[0];
                    string inpstr = test[1];
                    JNode inp = jsonParser.Parse(inpstr);
                    JNode bad_result = remesParser.Search(query, inp);
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
            var testcases = new (List<JNode> args, ArgFunction f, JNode desired_output)[]
            {
                ( new List<JNode>{jsonParser.Parse("[1,2]")}, ArgFunction.FUNCTIONS["len"], new JNode(Convert.ToInt64(2), Dtype.INT, 0) ),
                ( new List<JNode>{jsonParser.Parse("[1,2]"), jtrue}, ArgFunction.FUNCTIONS["sorted"], jsonParser.Parse("[2,1]") ),
                ( new List<JNode>{jsonParser.Parse("[[1,2], [4, 1]]"), new JNode(Convert.ToInt64(1), Dtype.INT, 0), jfalse }, ArgFunction.FUNCTIONS["sort_by"], jsonParser.Parse("[[4, 1], [1, 2]]") ),
                ( new List<JNode>{jsonParser.Parse("[1, 3, 2]")}, ArgFunction.FUNCTIONS["mean"], new JNode(2.0, Dtype.FLOAT, 0) ),
            };
            int tests_failed = 0;
            int ii = 0;
            foreach ((List<JNode> args, ArgFunction f, JNode desired_output) in testcases)
            {
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
                string str_desired = desired_output.ToString();
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
