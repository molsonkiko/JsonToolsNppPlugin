using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using static JSON_Tools.Tests.RemesParserTester;

namespace JSON_Tools.Tests
{
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

        public static readonly string fooStr = "{\"foo\": [[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]], " +
            "\"bar\": {\"a\": false, \"b\": [\"a`g\", \"bah\"]}, \"baz\": \"z\", " +
            "\"quz\": {}, \"jub\": [], \"guzo\": [[[1]], [[2], [3]]], \"7\": [{\"foo\": 2}, 1], \"_\": {\"0\": 0}}";
        public static readonly JNode foo = new JsonParser(LoggerLevel.JSON5).Parse(fooStr);
        public static readonly JObject fooObj = (JObject)foo;
        public static readonly int fooLen = fooObj.Length;

        public static bool Test()
        {
            JsonParser jsonParser = new JsonParser();
            RemesParser remesparser = new RemesParser();
            Npp.AddLine($"The queried JSON in the RemesParser tests is named foo:{fooStr}");
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
                // binops with uminus
                new Query_DesiredResult("5 * -1/4", "-1.25"),
                new Query_DesiredResult("-5 * 1/4", "-1.25"),
                new Query_DesiredResult("@.foo[1] / -@.foo[0] ** s_len(foo)", "[-Infinity, -4.0, -0.625]"),
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
                new Query_DesiredResult("@.*", fooStr),
                new Query_DesiredResult("@.foo[*]", "[[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]"),
                new Query_DesiredResult("@[*]", fooStr),
                new Query_DesiredResult("@.foo[:2][2*@[0] >= @[1]]", "[[3.0, 4.0, 5.0]]"),
                new Query_DesiredResult("@.foo[-1]", "[6.0, 7.0, 8.0]"),
                // out-of-bounds index tests
                new Query_DesiredResult("@.foo[-4]", "[]"),
                new Query_DesiredResult("@.foo[1, -7, 5]", "[[3.0, 4.0, 5.0]]"),
                new Query_DesiredResult("@.foo[:][-9]", "[]"),
                new Query_DesiredResult("@.foo[:][5]", "[]"),
                new Query_DesiredResult("@.g`[a-z]oo`", "{\"foo\": [[0, 1, 2], [3.0, 4.0, 5.0], [6.0, 7.0, 8.0]]}"),
                // boolean indexing tests
                new Query_DesiredResult("@.foo[:][:][@ % 3 == 1]", "[[1], [4.0], [7.0]]"),
                new Query_DesiredResult("j`[{\"a\": 1, \"b\": [\"a\", \"b\"]}, {\"a\": 2, \"b\": [\"c\", \"d\", \"e\"]}, {\"a\": 3, \"b\": null}]`" +
                                        "[:][@.a < 3]" +
                                        ".b[:2][@ =~ g`[a-e]`]",
                    "[[\"a\", \"b\"], [\"c\", \"d\"]]"),
                new Query_DesiredResult("j`{\"a\": 1, \"b\": 2}`[@ < 2]", "{\"a\": 1}"),
                new Query_DesiredResult("j`{\"a\": 1, \"b\": 2}`[@ < 0]", "{}"),
                new Query_DesiredResult("j`[1,2,3]`[@ < 0]", "[]"),
                new Query_DesiredResult("j`[1,2,3]`[false]", "[]"),
                new Query_DesiredResult("j`{\"a\": 1, \"b\": 2}`[false]", "{}"),
                new Query_DesiredResult("j`{\"a\": 1, \"b\": 2}`[len(@) > 0]", "{\"a\": 1, \"b\": 2}"),
                new Query_DesiredResult("j`{\"a\": 1, \"b\": 2}`[true]", "{\"a\": 1, \"b\": 2}"),
                new Query_DesiredResult("@.foo[:]{a: @[0], b: @[1], c: @[2], d: @[2] / @[0]}[@.a * @.c < @.b][@ > 1]", "[{\"c\": 2, \"d\": Infinity}]"),
                // negated varname list tests
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}!.e", "{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4}"),
                new Query_DesiredResult("j`{a: 1, b: 2, c: 3, d: 4, e: 5}`!.d", "{\"a\": 1, \"b\": 2, \"c\": 3, \"e\": 5}"),
                new Query_DesiredResult("j`{a: 1, b: 2, c: 3, d: 4, e: 5}`!.g`[a-d]`", "{\"e\": 5}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![e]", "{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![f]", "{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4, \"e\": 5}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}!.g`f`", "{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4, \"e\": 5}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}!.g`e`", "{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![g`[de]`, c]", "{\"a\": 1, \"b\": 2}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![g`[de]`, g`[ac]`, g`f`]", "{\"b\": 2}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![g`[de]`, g`[acd]`, g`[fe]`]", "{\"b\": 2}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![a, b, d, e]", "{\"c\": 3}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![a, b, c, d, e]", "{}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![a, b, g`[bc]`]", "{\"d\": 4, \"e\": 5}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![a, b, g`c`, g`d`]", "{\"e\": 5}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![a, b, f, g`[cd]`]", "{\"e\": 5}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![a, g`[b]`, g`[cd]`]", "{\"e\": 5}"),
                new Query_DesiredResult("@{a: 1, b: 2, c: 3, d: 4, e: 5}![f, g`[b]`, g`[cd]`]", "{\"a\": 1, \"e\": 5}"),
                new Query_DesiredResult("@{a: @{1, 2}, b: @{3}, c: 3, d: 4, e: 5}![c, d, e][:]{@, str(@)}", "{\"a\": [[1,\"1\"],[2,\"2\"]], \"b\": [[3,\"3\"]]}"),
                // negated slicer list tests
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![0]", "[1, 2, 3, 4]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![1:]", "[0]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![:2, -2]", "[2, 4]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![:2, -4]", "[2, 3, 4]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![:2, 4]", "[2, 3]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![:3:2]", "[1, 3, 4]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![:3:2, -1]", "[1, 3]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![:3:2, -2:]", "[1]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![:3, -2:]", "[]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![0, 1, 2, 4]", "[3]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![0, -3, 2, -1]", "[1, 3]"),
                new Query_DesiredResult("j`[0, 1, 2, 3, 4]`![2, 1::3]", "[0, 3]"),
                new Query_DesiredResult("j`[[0,-1], 1, 2, [3,-4], 4]`![2, 1::3][1]", "[-1,-4]"),
                new Query_DesiredResult("j`[{\"a\": 0,\"b\":-1}, {\"c\":1}, {\"a\":1}, {\"a\":3,\"b\":-4}, 4]`[:3]!.a", "[{\"b\": -1},{\"c\":1}]"),
                // ufunction tests
                new Query_DesiredResult("len(@)", fooLen.ToString()),
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
                new Query_DesiredResult("min_by(@.foo, -3)", "[0, 1, 2]"),
                new Query_DesiredResult("min_by(@.foo, -1)", "[0, 1, 2]"),
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
                new Query_DesiredResult("s_slice(str(@.foo[2]), -3)", "[\"6\", \"7\", \"8\"]"),
                new Query_DesiredResult("s_slice(j`[\"abc\", \"bced\", \"g\"]`, -1)", "[\"c\", \"d\", \"g\"]"),
                new Query_DesiredResult("s_slice(j`[\"ca\", \"bcddddd\", \"efg\"]`, -2)", "[\"c\", \"d\", \"f\"]"),
                new Query_DesiredResult("sorted(flatten(@.guzo, 2))", "[1, 2, 3]"),
                new Query_DesiredResult("keys(@)", "[\"foo\", \"bar\", \"baz\", \"quz\", \"jub\", \"guzo\", \"7\", \"_\"]"),
                new Query_DesiredResult("values(@.bar)[:]", "[false, [\"a`g\", \"bah\"]]"),
                new Query_DesiredResult("s_join(`\t`, @.bar.b)", "\"a`g\\tbah\""),
                new Query_DesiredResult("sorted(unique(@.foo[1]), true)", "[5.0, 4.0, 3.0]"), // have to sort because this function involves a HashSet so order is random
                new Query_DesiredResult("unique(@.foo[0], true)", "[0, 1, 2]"),
                new Query_DesiredResult("sort_by(value_counts(@.foo[0]), 1)", "[[0, 1], [1, 1], [2, 1]]"), // function involves a Dictionary so order is inherently random
                new Query_DesiredResult("value_counts(j`[\"a\", \"b\", \"c\", \"c\", \"c\", \"b\"]`, true)", "[[\"c\", 3], [\"b\", 2], [\"a\", 1]]"),
                new Query_DesiredResult("sort_by(value_counts(j`[1, 2, 1, 3, 1]`), 0)", "[[1, 3], [2, 1], [3, 1]]"),
                new Query_DesiredResult("sort_by(j`[[1, 2], [3, 1], [4, -1]]`, -1)", "[[4, -1], [3, 1], [1, 2]]"),
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
                new Query_DesiredResult("group_by(j`[{\"a\": 1, \"b\": \"x\", \"c\": -0.5}, {\"a\": 1, \"b\": \"y\", \"c\": 0.0}, {\"a\": 2, \"b\": \"x\", \"c\": 0.5}]`, j`[\"a\", \"b\"]`)", "{\"1\": {\"x\": [{\"a\": 1, \"b\": \"x\", \"c\": -0.5}], \"y\": [{\"a\": 1, \"b\": \"y\", \"c\": 0.0}]}, \"2\": {\"x\": [{\"a\": 2, \"b\": \"x\", \"c\": 0.5}]}}"),
                new Query_DesiredResult("group_by(j`[[1, \"x\", -0.5], [1, \"y\", 0.0], [2, \"x\", 0.5]]`, j`[1, 0]`)", "{\"x\": {\"1\": [[1, \"x\", -0.5]], \"2\": [[2, \"x\", 0.5]]}, \"y\": {\"1\": [[1, \"y\", 0.0]]}}"),
                new Query_DesiredResult("group_by(j`[[1, 2, 2, 0.0], [1, 2, 3, -1.0], [1, 3, 3, -2.0], [1, 3, 4, -3.0], [2, 2, 2, -4.0]]`, j`[0, 1, 2]`)", "{\"1\": {\"2\": {\"2\": [[1, 2, 2, 0.0]], \"3\": [[1, 2, 3, -1.0]]}, \"3\": {\"3\": [[1, 3, 3, -2.0]], \"4\": [[1, 3, 4, -3.0]]}}, \"2\": {\"2\": {\"2\": [[2, 2, 2, -4.0]]}}}"),
                //("agg_by(@.foo, 0, sum(flatten(@)))", "{\"0\": 3.0, \"3.0\": 11.0, \"6.0\": 21.0}"),
                new Query_DesiredResult("index(j`[1,3,2,3,1]`, max(j`[1,3,2,3,1]`), true)", "3"),
                new Query_DesiredResult("index(@.foo[0], min(@.foo[0]))", "0"),
                new Query_DesiredResult("zip(j`[1,2,3]`, j`[\"a\", \"b\", \"c\"]`)", "[[1, \"a\"], [2, \"b\"], [3, \"c\"]]"),
                new Query_DesiredResult("zip(@.foo[0], @.foo[1], @.foo[2], j`[-20, -30, -40]`)", "[[0, 3.0, 6.0, -20], [1, 4.0, 7.0, -30], [2, 5.0, 8.0, -40]]"),
                new Query_DesiredResult("dict(zip(keys(@.bar), j`[1, 2]`))", "{\"a\": 1, \"b\": 2}"),
                new Query_DesiredResult("dict(items(@))", fooStr),
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
                new Query_DesiredResult("-len(@) * len(@)", (-(fooLen * fooLen)).ToString()),
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
                new Query_DesiredResult("parse(j`[\"[1,2,NaN,null,\\\"\\\"]\", \"{\\\"foo\\\": false}\", \"u\"]`)", "[{\"result\": [1,2,NaN,null,\"\"]}, {\"result\": {\"foo\": false}}, {\"error\": \"No valid literal possible at position 0 (char 'u')\"}]"),
                new Query_DesiredResult("stringify(j`[1,2,\"foo\"]`)", "\"[1,2,\\\"foo\\\"]\""),
                new Query_DesiredResult("j`[true,null,1,NaN,{},[], \"\"]`[:]{type(@)}[0]", "[\"boolean\", \"null\", \"integer\", \"number\", \"object\", \"array\", \"string\"]"),
                new Query_DesiredResult("at(@.foo[1], -@.foo[0] - 1)", "[5.0, 4.0, 3.0]"),
                new Query_DesiredResult("at(@.foo[1], @.foo[0][2])", "5.0"),
                new Query_DesiredResult("at(@, keys(@)[:][s_slice(@, 0) == b])", "[{\"a\": false, \"b\": [\"a`g\", \"bah\"]}, \"z\"]"),
                new Query_DesiredResult("at(@, ifelse(@.bar.a, bar, baz))", "\"z\""),
                // '*' spread operator for array arguments to arg functions
                new Query_DesiredResult("zip(*@.foo)", "[[0, 3.0, 6.0], [1, 4.0, 7.0], [2, 5.0, 8.0]]"),
                new Query_DesiredResult("zip(@.foo[2] * 3, *@.foo[:]->(@ ** 2))", "[[18.0, 0, 9.0, 36.0], [21.0, 1, 16.0, 49.0], [24.0, 4, 25.0, 64.0]]"),
                new Query_DesiredResult("concat(j`[\"a\", \"b\", \"c\"]`, *j`[[1, 2], [3, 4]]`)", "[\"a\", \"b\", \"c\", 1, 2, 3, 4]"),
                new Query_DesiredResult("j`{\"a\": [[[1, 0], [0, 1]], 1], \"b\": [[[1, 0], [0, 1]], 0]}`.*->max_by(*@)", "{\"a\": [0, 1], \"b\": [1, 0]}"),
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
                new Query_DesiredResult("max_by(@.foo[:]{-@}[0], -2)", "[0, -1, -2]"),
                new Query_DesiredResult("max_by(@.foo[:]{mx: max(@), first: @[0]}, mx)", "{\"mx\": 8.0, \"first\": 6.0}"),
                new Query_DesiredResult("@{`\t\b\x12`: 1}", "{\"\\t\\b\\u0012\": 1}"), // control characters in projection key
                new Query_DesiredResult("@{foo: .125E3, $baz: 0x2eFb, 草: 2, _quЯ: 3, \\ud83d\\ude00_$\\u1ed3: 4, a\\uff6acf: 5, \\u0008\\u000a: 0xabcdefABCDEF}", // JSON5-compliant unquoted strings
                    "{\"foo\": 125, \"$baz\": 12027, \"草\": 2, \"_quЯ\": 3, \"😀_$ồ\": 4, \"aｪcf\": 5, \"\\\\b\\\\n\": 188900977659375}"),
                new Query_DesiredResult("1{1, 2}", "[1, 2]"), // projections off of scalars
                new Query_DesiredResult("baz{a: 1}", "{\"a\": 1}"),
                new Query_DesiredResult("null{foo: 1.5->1, b: true->9.75, c: b{9}, d: 1{bar: baz}}",
                    "{\"foo\": 1, \"b\": 9.75, \"c\": [9], \"d\": {\"bar\": \"baz\"}}"),
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
                // making sure various escapes work in backtickstrings
                new Query_DesiredResult("`\\t\\r\\n`", "\"\t\\r\\n\""),
                // "->" (map) operator
                new Query_DesiredResult("@ -> len(@)", fooLen.ToString()),
                new Query_DesiredResult("@.foo[:] -> stringify(@)", "[\"[0,1,2]\", \"[3.0,4.0,5.0]\", \"[6.0,7.0,8.0]\"]"),
                new Query_DesiredResult("@.* -> 3", "{\"foo\": 3, \"bar\": 3, \"baz\": 3, \"quz\": 3, \"jub\": 3, \"guzo\": 3, \"7\": 3, \"_\": 3}"),
                new Query_DesiredResult("@.bar.* -> type(@)", "{\"a\": \"boolean\", \"b\": \"array\"}"),
                new Query_DesiredResult("@.`7`[:] -> s_len(stringify(@)) {s: str(@), gt1: @ > 1}", "[{\"s\": \"9\", \"gt1\": true}, {\"s\": \"1\", \"gt1\": false}]"),
                new Query_DesiredResult("j`{\"a\": [\"1\", \"2\"], \"b\": [\"x\", \"yz\"]}`.* -> s_join(_, @) -> (s_split(@, ``)[1:-1]) -> (@ * range(1, 1 + len(@)))",
                      "{\"a\": [\"1\", \"__\", \"222\"], \"b\": [\"x\", \"__\", \"yyy\", \"zzzz\"]}"),
                new Query_DesiredResult("j`[1,2,3]` -> @ * 3", "[3, 6, 9]"),
                new Query_DesiredResult("j`[{\"a\": [[1], [2, 3]], \"b\": [null]}, {\"a\": [[4, 5], [6], []], \"b\": [1.5, {}]}]`" +
                                        "[:]{al: @.a[:]->len(@)*2, bt: @.b[:]->type(@)}",
                                        "[{\"al\": [2, 4], \"bt\": [\"null\"]}, {\"al\": [4, 2, 0], \"bt\": [\"number\", \"object\"]}]"),
                new Query_DesiredResult("concat(@.foo[0][:]->(str(@)*(@+1)), keys(@.`7`[0])[:]->(@ + s_slice(@, ::-1)))",
                    "[\"0\", \"11\", \"222\", \"foooof\"]"),
                // s_csv CSV parser
                // 3-column 14 rows, ',' delimiter, CRLF newline, '"' quote character, newline before EOF
                new Query_DesiredResult("s_csv(`nums,names,cities\\r\\n" +
                                                "nan,Bluds,BUS\\r\\n" +
                                                ",,\\r\\n" +
                                                "nan,\"\",BUS" +
                                                "\\r\\n0.5,\"df\\r\\ns \\\"d\\\" \",FUDG\\r\\n" +
                                                "0.5,df\"sd,FUDG\\r\\n" + // valid; unescaped quotes are fine on a quoted line
                                                "\"\",,FUDG\\r\\n" +
                                                "0.5,df\\ns\\rd,\"\"\\r\\n" + // valid; neither \n nor \r is the designated newline
                                                "1.2,qere,GOLAR\\r\\n" +
                                                "1.2,qere,GOLAR\\r\\n" +
                                                "3.4,flodt,\"q,tun\"\\r\\n" +
                                                "4.6,Kjond,YUNOB\\r\\n" +
                                                "4.6,Kjond,YUNOB\\r\\n" +
                                                "7,Unyir,\\r\\n`" +
                                                ", 3)", // only 3 columns and string need to be specified; comma delim, CRLF newline, and '"' quote are defaults
                    "[[\"nums\",\"names\",\"cities\"],[\"nan\",\"Bluds\",\"BUS\"],[\"\",\"\",\"\"],[\"nan\",\"\\\"\\\"\",\"BUS\"],[\"0.5\",\"\\\"df\\r\\ns \\\\\\\"d\\\\\\\" \\\"\",\"FUDG\"],[\"0.5\",\"df\\\"sd\",\"FUDG\"],[\"\\\"\\\"\",\"\",\"FUDG\"],[\"0.5\",\"df\\ns\\rd\",\"\\\"\\\"\"],[\"1.2\",\"qere\",\"GOLAR\"],[\"1.2\",\"qere\",\"GOLAR\"],[\"3.4\",\"flodt\",\"\\\"q,tun\\\"\"],[\"4.6\",\"Kjond\",\"YUNOB\"],[\"4.6\",\"Kjond\",\"YUNOB\"],[\"7\",\"Unyir\",\"\"]]"),
                // 7 columns, 8 rows '\t' delimiter, LF newline, '\'' quote character, no newline before EOF
                new Query_DesiredResult("s_csv(`nums\\tnames\\tcities\\tdate\\tzone\\tsubzone\\tcontaminated\\n" +
                                                "nan\\tBluds\\tBUS\\t\\t1\\t''\\tTRUE\\n" +
                                                "0.5\\tdfsd\\tFUDG\\t12/13/2020 0:00\\t2\\tc\\tTRUE\\n" +
                                                "\\tqere\\tGOLAR\\t\\t3\\tf\\t\\n" +
                                                "1.2\\tqere\\t'GOL\\\\'AR'\\t\\t3\\th\\tTRUE\\n" +
                                                "''\\tflodt\\t'q\\ttun'\\t\\t4\\tq\\tFALSE\\n" +
                                                "4.6\\tKjond\\t\\t\\t\\tw\\t''\\n" +
                                                "4.6\\t'Kj\\nond'\\tYUNOB\\t10/17/2014 0:00\\t5\\tz\\tFALSE`" +
                                                ", 7, `\t`, `\n`, `'`)",
                    "[[\"nums\",\"names\",\"cities\",\"date\",\"zone\",\"subzone\",\"contaminated\"],[\"nan\",\"Bluds\",\"BUS\",\"\",\"1\",\"''\",\"TRUE\"],[\"0.5\",\"dfsd\",\"FUDG\",\"12/13/2020 0:00\",\"2\",\"c\",\"TRUE\"],[\"\",\"qere\",\"GOLAR\",\"\",\"3\",\"f\",\"\"],[\"1.2\",\"qere\",\"'GOL\\\\'AR'\",\"\",\"3\",\"h\",\"TRUE\"],[\"''\",\"flodt\",\"'q\\ttun'\",\"\",\"4\",\"q\",\"FALSE\"],[\"4.6\",\"Kjond\",\"\",\"\",\"\",\"w\",\"''\"],[\"4.6\",\"'Kj\\nond'\",\"YUNOB\",\"10/17/2014 0:00\",\"5\",\"z\",\"FALSE\"]]"),
                // 1-column, '^' delimiter, '$' quote character, '\r' newline, 7 valid rows with 2 invalid rows at the end
                new Query_DesiredResult("s_csv(`a\\r" +
                                               "$b^c$\\r" +
                                               "$new\\r\\$line\\$$\\r" +
                                               "\\r" +
                                               "\\r" +
                                               "$$\\r" +
                                               "d$\ne\\r" +
                                               "$ $ $\\r" + // invalid because there's an unescaped quote character on a quoted line
                                               "f^g`" + // invalid because there's a delimiter on an unquoted line
                                               ", 1, `^`, `\\r`, `$`)",
                    "[\"a\",\"$b^c$\",\"$new\\r\\\\$line\\\\$$\",\"\",\"\",\"$$\",\"d$\\ne\"]"),
            };
            int ii = 0;
            int tests_failed = 0;
            JNode result;
            foreach (Query_DesiredResult qd in testcases)
            {
                ii++;
                JNode jdesired_result;
                try
                {
                    jdesired_result = jsonParser.Parse(qd.desired_result);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Got an error while parsing {qd.desired_result}:\n{ex}");
                    continue;
                }
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
                if (!result.TryEquals(jdesired_result, out _))
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
            string onetofive_str = "[1,2,3,4,5]";
            JNode onetofive = jsonParser.Parse(onetofive_str);
            (string query, JArray desired_result)[] slice_testcases = SliceTester.str_testcases
                .Select(testcase => ( // convert int[] into JArray of JNode with long value and Dtype.INT
                    $"@[{testcase.slicer}]",
                    new JArray(0,
                        testcase.desired
                        .Select(i => new JNode((long)i))
                        .ToList())
                ))
                .ToArray();
            foreach ((string query, JArray desired_result) in slice_testcases)
            {
                ii++;
                try
                {
                    result = remesparser.Search(query, onetofive);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search({query}, {onetofive_str}) to return {desired_result.ToString()}, but instead threw" +
                                      $" an exception:\n{ex}");
                    continue;
                }
                if (!result.TryEquals(desired_result, out _))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search({query}, {onetofive_str}) to return {desired_result.ToString()}, " +
                                      $"but instead got {result.ToString()}.");
                }
            }
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }
    }

    class RemesPathThrowsWhenAppropriateTester
    {
        public static bool Test()
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
                new []{"max_by(@, -4)", "[[1, 2], [2, 3]]"},
                new []{"sort_by(@, -4)", "[[1, 2], [2, 3]]"},
                new []{"min_by(@, -4)", "[[1, 2], [2, 3]]"},
                new []{"group_by(@, -4)", "[[1, 2], [2, 3]]"},
                new []{"s_slice(@, -4)", "[\"abc\", \"cde\"]"},
                new []{"s_slice(@, 7)", "[\"abc\", \"cde\"]"},
                new []{"s_slice(*@)", "[\"abc\", 1, 2]"}, // * spread operator spreading an array that would cause function to have too many args
                new []{"s_slice(*@)", "[\"abc\"]"}, // * spread operator spreading an array that would cause function to have too few args
                new []{"s_slice(*@)", "[\"abc\", \"b\"]"}, // * spread operator spreading an array that would cause function to have arg of wrong type
                // disallowed negated indices (recursive, star, boolean, projection)
                new []{"@!..a", "{\"a\": 1, \"b\": [{\"a\": 2, \"c\": 3}]}"},
                new []{"@!..[a, c]", "{\"a\": 1, \"b\": [{\"a\": 2, \"c\": 3}]}"},
                new []{"@!..[0, -1:]", "[1, 2, 3, 4]"},
                new []{"@!.*", "[1, 2, 3, 4]"},
                new []{"@!..*", "[1, 2, 3, 4]"},
                new []{"@![@ > 3]", "[1, 2, 3, 4]"},
                new []{"@!{@ > 3}", "[1, 2, 3, 4]"},
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
            return tests_failed > 0;
        }
    }

    class RemesPathAssignmentTester
    {
        public static bool Test()
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
                new string[]{ foostr, "@!.foo = s_len(@)",
                    "{\"foo\": [-1, 2, 3], \"bar\": 3, \"baz\": 2}" },
                new string[]{ "[1, 2, 3, 4, 5]", "@![:1, -1] = @ * -2",
                    "[1, -4, -6, -8, 5]" },
                new string[]{"2", "2 = 3", "2"}, // mutating something that's not a function of input; the input is still returned
            };
            foreach (string[] test in testcases)
            {
                string inpstr = test[0];
                JNode inp = JsonParserTester.TryParse(test[0], jsonParser);
                string query = test[1];
                JNode jdesired_result = JsonParserTester.TryParse(test[2], jsonParser);
                if (inp is null || jdesired_result is null)
                    continue;
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
                if (!inp.TryEquals(jdesired_result, out _))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search({query}, {inpstr}) to mutate {inpstr} into {jdesired_result.ToString()}, " +
                                      $"but instead got {inp.ToString()}.");
                }
                if (!result.TryEquals(jdesired_result, out _))
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
            return tests_failed > 0;
        }
    }

    /// <summary>
    /// test multi-statement queries
    /// </summary>
    public class RemesPathComplexQueryTester
    {
        public static bool Test()
        {
            JsonParser jsonParser = new JsonParser();
            RemesParser remesparser = new RemesParser();
            Npp.AddLine($"The queried JSON in the RemesParser complex query tests is named foo:{fooStr}");
            var testcases = new Query_DesiredResult[]
            {
                new Query_DesiredResult("var a = 1; " +
                                        "var b = @.foo[0]; " +
                                        "var c = a + 2; " +
                                        "b = @ * c; " + // not reassigning b (use "var b = @ * c" for that), but rather mutating it
                                        "@.foo[:][1]",
                    "[3, 4.0, 7.0]"),
                new Query_DesiredResult("var two = @.foo[1]; " + // [3.0, 4.0, 5.0]
                                        "var one = @.foo[0]; " + // [0, 1, 2]
                                        "var one_two = one[::-1] + two; " + // [5.0, 5.0, 5.0]
                                        "two = @ + one_two[1]; " + // [8.0, 9.0, 10.0]
                                        "two",
                    "[8.0, 9.0, 10.0]"),
                new Query_DesiredResult("var two = @.foo[1]; " + // [3.0, 4.0, 5.0]
                                        "var one = @.foo[0]; " + // [0, 1, 2]
                                        "var mintwo = min(two); " + // 3.0
                                        "var z = one[:]{@, @ - mintwo};", // [[0, -3.0], [1, -2.0], [2, -1.0]]
                    "[[0, -3.0], [1, -2.0], [2, -1.0]]"),
                new Query_DesiredResult("var ifelse = blah; var s_len = s_len(ifelse); ifelse(s_len < 3, foo, bar)",
                    "\"bar\""), // variables with same names as argfunctions
                new Query_DesiredResult("var bar_a = @.bar.a; " + // false
                                        "var baz = @.baz; " + // "z"
                                        "var bazbar = str(bar_a) + baz; " + // "falsez"
                                        "var baz = @{baz, bazbar}; " +
                                        "var baz = @{bar_a, baz, bazbar}; " +
                                        "baz",
                    "[false, [\"z\", \"falsez\"], \"falsez\"]"), // redefining variables
                new Query_DesiredResult("var foobar = @{foo, bar}; " +
                                        "var foobar_keys = sorted(keys(@)[:][in(@, foobar)], true); " +
                                        "var o_a = s_slice(foobar_keys, 1){@ * 2, @[::-1]}; " +
                                        "o_a[:]->s_sub(foobar_keys, *@)",
                    "[[\"faa\", \"bar\"], [\"foo\", \"bor\"]]"), // '*' spread operator for array arguments to arg functions
                new Query_DesiredResult("for f = @.foo; " +
                                        "  for g = range(len(f)); " +
                                        "    at(f, g) = @ + g;", // for each subarray in foo, increase each value in that subarray by its index in the subarray
                    "[[0, 2, 4], [3.0, 5.0, 7.0], [6.0, 8.0, 10.0]]"),
                new Query_DesiredResult("var a = @.foo[0][2:0:-1] + 1;\r\n" + // [3, 2]
                                        "var b = @.bar.b;\r\n" +   // ["a`g", "bah"]
                                        "var b_maxlen = ``;\r\n" +
                                        "for i = range(len(a));\r\n" +
                                        "    var bval = at(b, i);\r\n" +
                                        "    bval = @ * at(a, i);\r\n" + // string-multiply each element in b by the corresponding element in a
                                        "    b_maxlen = ifelse(s_len(bval) > s_len(b_maxlen), bval, b_maxlen);\r\n" + // if bval is longer than b_maxlen, set b_maxlen = bval
                                        "end for;\r\n" + // at this point @.bar.b has been mutated to ["a`ga`ga`g", "bahbah"]
                                        "b_maxlen", // return the longest value in b (after it's been mutated by the above loop)
                    "\"a`ga`ga`g\""
                ),
                new Query_DesiredResult("var foo = @.foo;\r\n" +
                                        "for z = zip(foo[0], foo[2]);\r\n" +
                                        "    z[0] = z[1];\r\n" +
                                        "end for;\r\n" +
                                        "foo[0, -1]",
                    "[[6.0, 7.0, 8.0], [6.0, 7.0, 8.0]]"
                ),
                new Query_DesiredResult("var numStrMap = j`[\"foo\", \"bar\", \"baz\"]`;\r\n" +
                                        "for ii_numStr = enumerate(numStrMap);\r\n" +
                                        "    var ii = ii_numStr[0];\r\n" +
                                        "    var numStr = ii_numStr[1];\r\n" +
                                        "    at(@.foo[1], ii) = numStr;\r\n" +
                                        "end for;\r\n" +
                                        "@.foo[1]",
                    "[\"foo\", \"bar\", \"baz\"]"
                ),
                new Query_DesiredResult("var cumsum = 0;\r\n" + // replace each value of @.foo with the cumulative sum of mutfoo up to that point
                                                                // where mutfoo is a mutated version of @.foo where the first value of each subarray is reduced by 1
                                        "for foo = @.foo;\r\n" +
                                        "    foo[0] = @ - 1;" +
                                        "    for a = foo;\r\n" +
                                        "        cumsum = @ + a;\r\n" +
                                        "        a = cumsum;\r\n" +
                                        "    end for;" +
                                        "end for", // test that the return value of a query that ends on an "end for;" statement
                    // [[-1, 1, 2], [2.0, 4.0, 5.0], [5.0, 7.0, 8.0]]
                    "[[-1, 0, 2], [4.0, 8.0, 13.0], [18.0, 25.0, 33.0]]"
                ),
                new Query_DesiredResult("for f = @.foo;\r\n" +
                                        "    for b = f;\r\n" +
                                        "        b = str(@);\r\n" +
                                        "    end for;", // test that nested for loops where only one of the loops is closed still work fine
                    "[[\"0\", \"1\", \"2\"], [\"3.0\", \"4.0\", \"5.0\"], [\"6.0\", \"7.0\", \"8.0\"]]"
                ),
                new Query_DesiredResult("for f = @.foo[0]", "[0, 1, 2]" // make sure that for loops with no body don't cause an infinite loop
                ),
                new Query_DesiredResult("for f = j`[0, 1, 2]`", "[0, 1, 2]" // make sure that for loops that aren't functions of input with no body don't cause an infinite loop
                ),
                new Query_DesiredResult("var z = @.foo[:]{@, 1}; z[1][1] = @ + 1; z[:][1]",
                    "[1, 2, 1]"
                    // make sure that if something returns an array of projections where one value in the projection is a compile-time constant,
                    // mutating one instance of the projection doesn't mutate all the instances
                ),
                new Query_DesiredResult(
                    "var y = 1; var x = y; for z  = j`[1, 2, 3]`; x = @ * z; end for; y",
                    "6" // x is a reference to y, and x is mutated; make sure mutations propagate to y
                ),
                new Query_DesiredResult(
                    "var x = 3; var xarr = append(j`[1,2]`, x); for f = xarr; var x = f; f = @ * x; end for; @{x, xarr}",
                    "[9, [1, 4, 9]]"
                ),
            };
            int ii = 0;
            int tests_failed = 0;
            JNode result;
            foreach (Query_DesiredResult qd in testcases)
            {
                ii++;
                JNode jdesired_result;
                JNode foo = RemesParserTester.foo.Copy(); // need a fresh copy each time b/c queries could mutate it
                try
                {
                    jdesired_result = jsonParser.Parse(qd.desired_result);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Got an error while parsing {qd.desired_result}:\n{ex}");
                    continue;
                }
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
                if (!result.TryEquals(jdesired_result, out _))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected remesparser.Search({qd.query}, foo) to return {jdesired_result.ToString()}, " +
                                      $"but instead got {result.ToString()}.");
                }
            }
            ii = testcases.Length;
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }
    }

    public class RemesPathFuzzTester
    {
        public static readonly string target = "{\"a\": [-4, -3., -2., -1, 0, 1, 2., 3., 4], " +
                                                "\"bc\": NaN," +
                                                "\"c`d\": \"df\", " +
                                                "\"q\": [\"\", \"a\", \"jk\", \"ian\", \"\", \"32\", \"u\", \"aa\", \"moun\"]," +
                                                "\"f\": 1," +
                                                "\"g\": 1," +
                                                "\"h\": 1," +
                                                "\"i\": 1," +
                                                "\"j\": 1}";
        public static readonly (string tok, Dtype type)[] operands = new (string tok, Dtype type)[]
        {
            ("@.`c\\`d`", Dtype.STR),
            ("foo", Dtype.STR),
            ("@.q", Dtype.STR | Dtype.ARR),
            ("keys(@)", Dtype.STR | Dtype.ARR),
            ("@.bc", Dtype.NUM),
            ("@.a", Dtype.NUM | Dtype.ARR),
            ("range(9)", Dtype.NUM | Dtype.ARR),
            ("-2.0", Dtype.NUM),
            ("-0.0732", Dtype.NUM),
            ("0", Dtype.NUM),
            ("4", Dtype.NUM),
            ("0x5", Dtype.NUM),
            ("true", Dtype.NUM),
            ("false", Dtype.NUM),
            ("783.0", Dtype.NUM),
            ("-0x9872b1", Dtype.NUM),
            ("-0x12", Dtype.NUM),
            ("j`[-3,-2,-1,0,1,2,3,4,5]`", Dtype.NUM | Dtype.ARR),
            //("s", Dtype.NUM), // if this is uncommented, it should cause fuzz tests to fail; use this to verify you haven't broken the tests
        };
        public static readonly string[] unops = new string[] { "not", "-", "+", "" };
        public static readonly string[] num_funcs = new string[] { "int", "float" };
        public static readonly string[] arr_funcs = new string[] { "len", "iterable", "is_num" };
        public static readonly string[] str_funcs = new string[] { "s_len", "s_count" };
        public static readonly string[] binops = new string[] { "-", "+", "*", "/", "//", "**", ">", "<", "==", "!=", ">=", "<=", "%" };
        public static Random random = RandomJsonFromSchema.random;

        public static bool CoinFlip()
        {
            return random.NextDouble() < 0.5;
        }

        public static T RandomChoice<T>(T[] things)
        {
            return things[random.Next(things.Length)];
        }

        public static string ChooseFunc(string[] funcs1, string[] funcs2 = null)
        {
            if (funcs2 == null)
                return RandomChoice(funcs1);
            if (CoinFlip())
                return RandomChoice(funcs1);
            return RandomChoice(funcs2);
        }

        public static string GenerateOperand()
        {
            (string tok, Dtype type) = RandomChoice(operands);
            string unop = RandomChoice(unops);
            bool apply_argfunc = CoinFlip();
            bool is_str = (type & Dtype.STR) != 0;
            bool is_arr = (type & Dtype.ARR) != 0;
            bool apply_unop = unop != "" && (!is_str || unop == "not");
            bool unop_on_tok = CoinFlip();
            if (apply_unop && unop_on_tok)
            {
                if (is_str)
                    tok = $"({unop} {tok})";
                else
                    tok = $"{unop} {tok}";
                is_str = false; // if tok was string, the unop was not, and the output was bool
            }
            string operand;
            if (is_str)
            {
                string func = (is_arr && apply_argfunc)
                    ? ChooseFunc(str_funcs, arr_funcs)
                    : ChooseFunc(str_funcs);
                operand = (func == "s_count")
                    ? $"s_count({tok}, a)"
                    : $"{func}({tok})";
                if (apply_unop && !unop_on_tok)
                    operand = $"{unop} {operand}";
            }
            else if (apply_argfunc)
            {
                string func = (is_arr)
                    ? ChooseFunc(arr_funcs, num_funcs)
                    : ChooseFunc(num_funcs);
                operand = $"{func}({tok})";
                if (apply_unop && !unop_on_tok)
                    operand = $"{unop} {operand}";
            }
            else
                operand = tok;
            return CoinFlip()
                ? $"ifelse({operand} > 0, 1, -1)"
                : operand;
        }

        /// <summary>
        /// Perform many randomly generated tests with the expectations that:<br></br>
        /// 1. There will be no error (each test input is syntactically valid and should not cause runtime errors)<br></br>
        /// 2. The output will be either a number or an array of numbers
        /// </summary>
        /// <param name="n_tests"></param>
        /// <param name="max_failures"></param>
        public static bool Test(int n_tests, int max_failures)
        {
            JNode targetNode;
            try
            {
                targetNode = new JsonParser(LoggerLevel.JSON5, false, false, false).Parse(target);
            }
            catch (Exception ex)
            {
                Npp.AddLine($"Expected successful parsing of\r\n{target}\r\ninstead got error\r\n{ex}");
                return true;
            }
            Npp.AddLine($"Fuzz tests query\r\n{target}");
            RemesParser parser = new RemesParser();
            int failures = 0;
            int ii = 0;
            for (; ii < n_tests && failures < max_failures; ii++)
            {
                string v1 = GenerateOperand();
                string binop1 = RandomChoice(binops);
                string v2 = GenerateOperand();
                string binop2 = RandomChoice(binops);
                string v3 = GenerateOperand();
                string binop3 = RandomChoice(binops);
                string v4 = GenerateOperand();

                string query;
                if (CoinFlip())
                {
                    query = CoinFlip()
                        ? $"({v1} {binop1} {v2}) {binop2} ({v3} {binop3} {v4})"
                        : CoinFlip()
                            ? $"{v1} {binop1} {v2} {binop2} ({v3} {binop3} {v4})"
                            : $"({v1} {binop1} {v2}) {binop2} {v3} {binop3} {v4}";
                }
                else
                    query = $"{v1} {binop1} {v2} {binop2} {v3} {binop3} {v4}";
                JNode result;
                try
                {
                    result = parser.Search(query, targetNode);
                }
                catch (Exception ex)
                {
                    if (!(ex is OverflowException || ex is DivideByZeroException))
                    {
                        failures++;
                        Npp.AddLine($"Expected parsing of query\r\n{query}\r\nto not raise an exception, but got exception\r\n{ex}");
                    }
                    continue;
                }
                if (result is JArray arr)
                {
                    foreach (JNode child in arr.children)
                    {
                        if (!(child.value is bool || child.value is long || child.value is double))
                        {
                            failures++;
                            Npp.AddLine($"If result is an array, expected all members of result to be integers, floats, or bools, instead got {child}");
                            break;
                        }
                    }
                }
                else
                {
                    if (!(result.value is bool || result.value is long || result.value is double))
                    {
                        failures++;
                        Npp.AddLine($"If result is a scalar, expected result to be integer, float, or bool, instead got {result}");
                    }
                }
            }
            Npp.AddLine($"Ran {ii} fuzz tests");
            Npp.AddLine($"Failed {failures} fuzz tests");
            return failures > 0;
        }
    }
}
