using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class JsonParserTester
    {
        public static readonly string NL = System.Environment.NewLine;

        public static JNode TryParse(string input, JsonParser parser, bool is_json_lines = false)
        {
            try
            {
                if (is_json_lines)
                    return parser.ParseJsonLines(input);
                return parser.Parse(input);
            }
            catch (Exception e)
            {
                Npp.AddLine($"While parsing input {input}\nthrew error {e}");
                return null;
            }
        }

        public static bool TestJNodeCopy()
        {
            int ii = 0;
            int tests_failed = 0;
            JsonParser parser = new JsonParser(LoggerLevel.JSON5, true);
            string[] tests = new string[]
            {
                "2",
                "true",
                "\"abc\"",
                "3.5",
                "[1]",
                "{\"a\": 1}",
                "null",
                "\"2000-01-01\"",
                "\"1995-03-04 03:48:52\"",
                "[null, [1, 2, {\"abc\": 3.5, \"d\": {\"e\": [true]}}]]",
                "NaN",
                "Infinity",
            };

            foreach (string test in tests)
            {
                ii++;
                JNode node = TryParse(test, parser);
                if (node == null)
                {
                    tests_failed++;
                    continue;
                }
                JNode cp = node.Copy();
                if (!node.Equals(cp))
                {
                    tests_failed++;
                    Npp.AddLine($"Expected Copy({node.ToString()}) to return\n{node.ToString()}\nInstead got {cp.ToString()}");
                    continue;
                }
                // now test if mutating the node mutates the copy
                if (node is JArray arr)
                    arr.children.Clear();
                else if (node is JObject obj)
                    obj.children.Clear();
                else if (node.value == null)
                {
                    node.value = long.MaxValue;
                    node.type = Dtype.INT;
                }
                else
                {
                    node.value = null;
                    node.type = Dtype.NULL;
                }
                try
                {
                    node.Equals(cp);
                    if (!(node is JObject || node is JArray || node.type == Dtype.INT))
                    {
                        tests_failed++;
                        Npp.AddLine($"Expected mutating {node.ToString()}\nto not mutate Copy({node.ToString()})\nbut it did.");
                        continue;
                    }
                    if (node.Equals(cp))
                    {
                        tests_failed++;
                        Npp.AddLine($"Expected mutating {node.ToString()}\nto not mutate Copy({node.ToString()})\nbut it did.");
                        continue;
                    }
                }
                catch { }
            }
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }

        public static bool Test()
        {
            JsonParser parser = new JsonParser(LoggerLevel.JSON5, true, true, true);
            // includes:
            // 1. hex
            // 2. all other backslash escape sequences
            // 3. Empty arrays and objects
            // 4. empty strings
            // 5. space before and after commas
            // 6. no space between comma and next value
            // 7. space before colon and variable space after colon
            // 8. hex and escape sequences in keys
            // 9. all special scalars (nan, null, inf, -inf, true, false)
            // 10. all forms of whitespace
            #region ParserTests
            string example = "{\"a\":[-1, true, {\"b\" :  0.5, \"c\": \"\\uae77\"},null],\n"
                    + "\"a\\u10ff\":[true, false, NaN, Infinity,-Infinity, {},\t\"\\u043ea\", []], "
                    + "\"back'slas\\\"h\": [\"\\\"'\\f\\n\\b\\t/\", -0.5, 23, \"\"]} ";
            string norm_example = "{"
                + "\"a\u10ff\": [true, false, NaN, Infinity, -Infinity, {}, \"\u043ea\", []], "
                + "\"a\": [-1, true, {\"b\": 0.5, \"c\": \"\uae77\"}, null], "
                + "\"back'slas\\\"h\": [\"\\\"'\\f\\n\\b\\t/\", -0.5, 23, \"\"]}";
            string pprint_example = "{" +
                                    NL + "\"a\u10ff\":" +
                                    NL + "    [" +
                                    NL + "    true," +
                                    NL + "    false," +
                                    NL + "    NaN," +
                                    NL + "    Infinity," +
                                    NL + "    -Infinity," +
                                    NL + "        {" +
                                    NL + "        }," +
                                    NL + "    \"\u043ea\"," +
                                    NL + "        [" +
                                    NL + "        ]" +
                                    NL + "    ]," +
                                    NL + "\"a\":" +
                                    NL + "    [" +
                                    NL + "    -1," +
                                    NL + "    true," +
                                    NL + "        {" +
                                    NL + "        \"b\": 0.5," +
                                    NL + "        \"c\": \"\uae77\"" +
                                    NL + "        }," +
                                    NL + "    null" +
                                    NL + "    ]," +
                                    NL + "\"back'slas\\\"h\":" +
                                    NL + "    [" +
                                    NL + "    \"\\\"'\\f\\n\\b\\t/\"," +
                                    NL + "    -0.5," +
                                    NL + "    23," +
                                    NL + "    \"\"" +
                                    NL + "    ]" +
                                    NL + "}";
            var testcases = new (string, string, string, string)[]
            {
                (example, norm_example, pprint_example, "general parsing"),
                ("[[]]", "[[]]", "[" + NL + "    [" + NL + "    ]" + NL + "]", "empty lists" ),
                ("\"abc\"", "\"abc\"", "\"abc\"", "scalar string" ),
                ("1", "1", "1", "scalar int" ),
                ("-1.0", "-1.0", "-1.0", "negative scalar float" ),
                ("3.5", "3.5", "3.5", "scalar float" ),
                ("-4", "-4", "-4", "negative scalar int" ),
                ("[{\"FullName\":\"C:\\\\123467756\\\\Z\",\"LastWriteTimeUtc\":\"\\/Date(1600790697130)\\/\"}," +
                            "{\"FullName\":\"C:\\\\123467756\\\\Z\\\\B\",\"LastWriteTimeUtc\":\"\\/Date(1618852147285)\\/\"}]",
                             "[{\"FullName\": \"C:\\\\123467756\\\\Z\", \"LastWriteTimeUtc\": \"/Date(1600790697130)/\"}, " +
                            "{\"FullName\": \"C:\\\\123467756\\\\Z\\\\B\", \"LastWriteTimeUtc\": \"/Date(1618852147285)/\"}]",
                             "[" +
                             NL + "    {" +
                             NL + "    \"FullName\": \"C:\\\\123467756\\\\Z\"," +
                             NL + "    \"LastWriteTimeUtc\": \"/Date(1600790697130)/\"" +
                             NL + "    }," +
                             NL + "    {" +
                             NL + "    \"FullName\": \"C:\\\\123467756\\\\Z\\\\B\"," +
                             NL + "    \"LastWriteTimeUtc\": \"/Date(1618852147285)/\"" +
                             NL + "    }" +
                             NL + "]",
                             "open issue in Kapilratnani's JSON-Viewer regarding forward slashes having '/' stripped" ),
                ("111111111111111111111111111111", $"1.11111111111111E+29", $"1.11111111111111E+29",
                    "auto-conversion of int64 overflow to double" ),
                ("{ \"a\"\r\n:1, \"b\" : 1, \"c\"       :1}", "{\"a\": 1, \"b\": 1, \"c\": 1}",
                "{"+ NL + "\"a\": 1,"+ NL + "\"b\": 1,"+ NL + "\"c\": 1" + NL + "}",
                "weirdly placed colons"),
                (                    "{\"basst\": 1, \"baßk\": 1, \"blue\": 1, \"bLue\": 1, \"blve\": 1, \"blüe\": 1, \"oyster\": 1, \"spb\": 1, \"Spb\": 1, \"spä\": 1, \"öyster\": 1}",
                    "{\"baßk\": 1, \"basst\": 1, \"bLue\": 1, \"blue\": 1, \"blüe\": 1, \"blve\": 1, \"oyster\": 1, \"öyster\": 1, \"spä\": 1, \"spb\": 1, \"Spb\": 1}",
                    "{"
                    + NL + "\"baßk\": 1,"
                    + NL + "\"basst\": 1,"
                    + NL + "\"bLue\": 1,"
                    + NL + "\"blue\": 1,"
                    + NL + "\"blüe\": 1,"
                    + NL + "\"blve\": 1,"
                    + NL + "\"oyster\": 1,"
                    + NL + "\"öyster\": 1,"
                    + NL + "\"spä\": 1,"
                    + NL + "\"spb\": 1,"
                    + NL + "\"Spb\": 1"
                    + NL + "}",
                    "culture-sensitive sorting of keys (e.g., 'baßk' should sort before 'basst')"),
            };
            int tests_failed = 0;
            int ii = 0;
            foreach ((string input, string norm_input, string pprint_desired, string msg_) in testcases)
            {
                string msg = msg_;
                JNode json = TryParse(input, parser);
                if (json == null)
                {
                    ii += 4;
                    tests_failed += 4;
                    continue;
                }
                string norm_str_out = json.ToString();
                if (norm_str_out != norm_input)
                {
                    tests_failed++;
                    string fullMsg = string.Format(@"Test {0} ({1}) failed:
Expected
{2}
Got
{3}
", ii + 1, msg, norm_input, norm_str_out);
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(fullMsg), fullMsg);
                }
                ii++;
                JNode json_from_norm_str_out = parser.Parse(norm_str_out);
                if (!json_from_norm_str_out.Equals(json)
                    && input != "111111111111111111111111111111") // skip b/c floating-point imprecision
                {
                    tests_failed++;
                    msg = string.Format(@"Test {0} (parsing ToString result returns original) failed:
Expected Parse(Parse({1}).toString()) to return
{1}
Got
{2}
", ii + 1, norm_str_out, json_from_norm_str_out.ToString());
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(msg), msg);
                }
                ii++;
                string pprint_out = json.PrettyPrint(4, true, PrettyPrintStyle.Whitesmith);
                if (pprint_out != pprint_desired)
                {
                    tests_failed++;
                    msg = string.Format(@"Test {0} (pretty-print {1}) failed:
Expected
{2}
Got
{3}
", ii + 1, msg, pprint_desired, pprint_out);
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(msg), msg);
                }
                ii++;
                JNode json_from_pprint_out = parser.Parse(pprint_out);
                if (!json_from_pprint_out.Equals(json)
                    && input != "111111111111111111111111111111") // skip b/c floating-point imprecision
                {
                    tests_failed++;
                    msg = string.Format(@"Test {0} (parsing PrettyPrint result returns original) failed:
Expected Parse(Parse({1}).PrettyPrint()) to return
{1}
Got
{2}
", ii + 1, norm_str_out, json_from_pprint_out.ToString());
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(msg), msg);
                }
                ii++;
            }
            #endregion
            #region OtherPrintStyleTests
            var minimal_whitespace_testcases = new string[][]
            {
                new string[]
                {
                    "[1, {\"a\": 2, \"b\": 3}, [4, 5]]",
                    "[1,{\"a\":2,\"b\":3},[4,5]]"
                },
            };
            foreach (string[] test in minimal_whitespace_testcases)
            {
                string inp = test[0];
                string compact_desired = test[1];
                JNode json = TryParse(inp, parser);
                if (json == null)
                {
                    tests_failed++;
                    continue;
                }
                string compact_true = json.ToString(true, ":", ",");
                if (compact_true != compact_desired)
                {
                    tests_failed++;
                    Npp.AddLine(string.Format(@"Test {0} (minimal whitespace printing) failed:
Expected
{1}
Got
{2} ",
                                     ii + 1, compact_desired, compact_true));
                }
                ii++;
            }
            var dont_sort_keys_testcases = new string[][]
            {
                new string[]
                {
                    "{\"a\": 2,  \"c\": 4,   \"b\": [{\"e\":   1,\"d\":2}]}",
                    "{\"a\": 2, \"c\": 4, \"b\": [{\"e\": 1, \"d\": 2}]}"
                },
            };
            foreach (string[] test in dont_sort_keys_testcases)
            {
                string inp = test[0];
                string unsorted_desired = test[1];
                JNode json = TryParse(inp, parser);
                ii++;
                if (json == null)
                {
                    tests_failed++;
                    continue;
                }
                string unsorted_true = json.ToString(false);
                if (unsorted_true != unsorted_desired)
                {
                    tests_failed++;
                    Npp.AddLine(string.Format(@"Test {0} (minimal whitespace printing) failed:
Expected
{1}
Got
{2} ",
                                     ii + 1, unsorted_desired, unsorted_true));
                }
            }
            #endregion
            #region JNode Position Tests
            string objstr = "{\"a\": \u205F[1, 2, 3], \xa0\"b\": {}, \u3000\"Я草\": [], \"😀\": [[100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112], [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113], [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, [113, 114]]],/*comment*/\"d\":[{\"o\":\"öyster\"},\"cät\",\"dog\"],\"e\":false,\"f\":null}";
            // pprint-style leads to this:
            /*
{
    "a": [1, 2, 3],
    "b": {},
    "Я草": [],
    "😀": [
        [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112],
        [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113],
        [
            100,
            101,
            102,
            103,
            104,
            105,
            106,
            107,
            108,
            109,
            110,
            111,
            112,
            [113, 114]
        ]
    ],
    "d": [{"o": "öyster"}, "cät", "dog"],
    "e": false,
    "f": null
}
            */
            // also includes some weird space like '\xa0' only allowed in JSON5
            JNode onode = TryParse(objstr, parser);
            if (onode != null && onode is JObject obj)
            {
                Npp.AddLine($"obj =\r\n{objstr}\r\n");
                string correct_pprint_objstr = "{\r\n    \"a\": [1, 2, 3],\r\n    \"b\": {},\r\n    \"Я草\": [],\r\n    \"😀\": [\r\n        [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112],\r\n        [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113],\r\n        [\r\n            100,\r\n            101,\r\n            102,\r\n            103,\r\n            104,\r\n            105,\r\n            106,\r\n            107,\r\n            108,\r\n            109,\r\n            110,\r\n            111,\r\n            112,\r\n            [113, 114]\r\n        ]\r\n    ],\r\n    \"d\": [{\"o\": \"öyster\"}, \"cät\", \"dog\"],\r\n    \"e\": false,\r\n    \"f\": null\r\n}";
                ii++;
                string actual_pprint_objstr = obj.PrettyPrint(4, false, PrettyPrintStyle.PPrint);
                if (actual_pprint_objstr != correct_pprint_objstr)
                {
                    tests_failed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
    Expected PPrint-style PrettyPrintAndChangePositions(obj) to return
    {1}
    instead got
    {2}",
                        ii + 1, correct_pprint_objstr, actual_pprint_objstr));
                }
                ii++;
                // the formatting with '\t' indent is actually significantly different
                // because having only 1 char of indent makes it so all the sub-children fit on a single line 
                string correct_pprint_objstr_tab_indent = "{\r\n\t\"a\": [1, 2, 3],\r\n\t\"b\": {},\r\n\t\"Я草\": [],\r\n\t\"😀\": [\r\n\t\t[100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112],\r\n\t\t[100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113],\r\n\t\t[100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, [113, 114]]\r\n\t],\r\n\t\"d\": [{\"o\": \"öyster\"}, \"cät\", \"dog\"],\r\n\t\"e\": false,\r\n\t\"f\": null\r\n}";
                string actual_pprint_objstr_tab_indent = obj.PrettyPrint(1, false, PrettyPrintStyle.PPrint, int.MaxValue, '\t');
                if (actual_pprint_objstr_tab_indent != correct_pprint_objstr_tab_indent)
                {
                    tests_failed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
    Expected PPrint-style PrettyPrintAndChangePositions(obj) with tab indentation to return
    {1}
    instead got
    {2}",
                        ii + 1, correct_pprint_objstr_tab_indent, actual_pprint_objstr_tab_indent));
                }
                var keylines = new (string key,
                    int original_pos, int whitesmith_pos, int google_pos,
                    int tostring_miniwhite_pos, int tostring_pos, int pprint_pos)[]
                {
                    ("a", 9, 13, 12, 5, 6, 12),
                    ("b", 27, 57, 67, 17, 22, 33),
                    ("Я草", 43, 82, 91, 28, 35, 51),
                    ("😀", 55, 106, 114, 38, 47, 68),
                    ("d", 289, 818, 993, 220, 272, 525),
                    ("e", 324, 905, 1096, 255, 312, 570),
                    ("f", 334, 918, 1113, 265, 324, 587),
                };
                foreach ((string key, int original_position, _, _, _, _, _) in keylines)
                {
                    ii++;
                    int got_pos = obj[key].position;
                    if (got_pos != original_position)
                    {
                        tests_failed++;
                        Npp.AddLine($"After parsing of obj, expected the position of child {key} to be {original_position}, got {got_pos}.");
                    }
                }
                foreach (PrettyPrintStyle style in new[] {PrettyPrintStyle.Whitesmith, PrettyPrintStyle.Google, PrettyPrintStyle.PPrint })
                {
                    string pp = obj.PrettyPrint(4, false, style);
                    string pp_ch_line = obj.PrettyPrintAndChangePositions(4, false, style);
                    if (pp != pp_ch_line)
                    {
                        tests_failed++;
                        Npp.AddLine(string.Format(@"Test {0} failed:
    Expected {1}-style PrettyPrintAndChangePositions(obj) to return
    {2}
    instead got
    {3}",
                                                        ii + 1, style, pp, pp_ch_line));
                    }
                    ii++;
                }

                foreach ((string key, _, int whitesmith_pos, int google_pos, int tostring_miniwhite_pos, int tostring_pos, int pprint_pos) in keylines)
                {
                    obj.PrettyPrintAndChangePositions(4, false, PrettyPrintStyle.Whitesmith);
                    int got_pos = obj[key].position;
                    if (got_pos != whitesmith_pos)
                    {
                        tests_failed++;
                        Npp.AddLine($"After Whitesmith-style PrettyPrintAndChangePositions(obj), expected the position of child {key} to be {whitesmith_pos}, got {got_pos}.");
                    }
                    ii++;
                    obj.PrettyPrintAndChangePositions(4, false, PrettyPrintStyle.Google);
                    got_pos = obj[key].position;
                    if (got_pos != google_pos)
                    {
                        tests_failed++;
                        Npp.AddLine($"After Google-style PrettyPrintAndChangePositions(obj), expected the position of child {key} to be {google_pos}, got {got_pos}.");
                    }
                    ii++;
                    obj.ToStringAndChangePositions(false);
                    got_pos = obj[key].position;
                    if (got_pos != tostring_pos)
                    {
                        tests_failed++;
                        Npp.AddLine($"After ToStringAndChangePositions(obj), expected the position of child {key} to be {tostring_pos}, got {got_pos}.");
                    }
                    ii++;
                    obj.ToStringAndChangePositions(false, ":", ",");
                    got_pos = obj[key].position;
                    if (got_pos != tostring_miniwhite_pos)
                    {
                        tests_failed++;
                        Npp.AddLine($"After minimal-whitespace ToStringAndChangePositions(obj), expected the position of child {key} to be {tostring_miniwhite_pos}, got {got_pos}.");
                    }
                    ii++;
                    obj.PrettyPrintAndChangePositions(4, false, PrettyPrintStyle.PPrint);
                    got_pos = obj[key].position;
                    if (got_pos != pprint_pos)
                    {
                        tests_failed++;
                        Npp.AddLine($"After PPrint-style PrettyPrintAndChangePositions(obj), expected the position of child {key} to be {pprint_pos}, got {got_pos}.");
                    }
                    ii++;
                }
                string tostr = obj.ToString(false);
                string tostr_ch_line = obj.ToStringAndChangePositions(false);
                ii++;
                if (tostr != tostr_ch_line)
                {
                    tests_failed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
    Expected ToStringAndChangePositions({1}) to return
    {2}
    instead got
    {3}",
                                                    ii + 1, objstr, tostr, tostr_ch_line));
                }
            }
            #endregion
            #region EqualityTests
            var equality_testcases = new object[][]
            {
                new object[] { "1", "2", false },
                new object[] { "1", "1", true },
                new object[] { "2.5e3", "2.5e3", true },
                new object[] { "2.5e3", "2.2e3", false },
                new object[] { "\"a\"", "\"a\"", true },
                new object[] { "\"a\"", "\"b\"", false },
                new object[] { "[[1, 2], [3, 4]]", "[[1,2],[3,4]]", true },
                new object[] { "[1, 2, 3, 4]", "[[1,2], [3,4]]", false },
                new object[] { "{\"a\": 1, \"b\": Infinity, \"c\": 0.5}", "{\"b\": Infinity, \"a\": 1, \"c\": 0.5}", true },
                new object[] { "[\"z\\\"\"]", "[\"z\\\"\"]", true },
                new object[] { "{}", "{" + NL + "   }", true },
                new object[] { "[]", "[ ]", true },
                new object[] { "[]", "[1, 2]", false },
                new object[] { "1" + new string('0', 400), "NaN", true }, // really really big int representations
            };
            bool oldThrowIfLogged = parser.throw_if_logged;
            parser.throw_if_logged = false;
            foreach (object[] test in equality_testcases)
            {
                string astr = (string)test[0];
                string bstr = (string)test[1];
                bool a_equals_b = (bool)test[2];
                ii++;
                JNode a = parser.Parse(astr);
                JNode b = parser.Parse(bstr);
                bool result = a.Equals(b);
                if (result != a_equals_b)
                {
                    tests_failed++;
                    Npp.AddLine($"Expected {a.ToString()} == {b.ToString()} to be {a_equals_b}, but it was called {result}");
                }
            }
            parser.throw_if_logged = oldThrowIfLogged;
            #endregion
            #region TestJRegexToString
            var arrayOfJRegexes = new JArray(0, new JNode[]{ 
                new JRegex(new Regex(".")),
                new JRegex(new Regex("(\")")),
                new JRegex(new Regex("\\d+[a-z]\\\\"))
            }.ToList());
            var correctJRegexArrayRepr = "[\".\", \"(\\\")\", \"\\\\d+[a-z]\\\\\\\\\"]";
            ii++;
            string gotRepr = correctJRegexArrayRepr;
            try
            {
                gotRepr = arrayOfJRegexes.ToString();
            }
            catch (Exception ex)
            {
                tests_failed++;
                Npp.AddLine($"While trying to get the string representation of JRegex array {correctJRegexArrayRepr}\r\ngot error\r\n{ex}");
            }
            if (gotRepr != correctJRegexArrayRepr)
            {
                tests_failed++;
                Npp.AddLine($"JRegex ToString() should return a string that would reproduce the original regex.\r\nExpected\r\n{correctJRegexArrayRepr}\r\nGot\r\n{gotRepr}");
            }
            #endregion

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }

        public static bool TestThrowsWhenAppropriate()
        {
            int ii = 0, tests_failed = 0;
            JsonParser parser = new JsonParser();
            string[] testcases = new string[]
            {
                "\"abc\\", // escape at end of unterminated string
                "\"abc\" d", // something other than EOF after string document
                "[1] [1]", // something other than EOF after string document
                $"{new string('[', 1000)}1{new string(']', 1000)}", // too much recursion in array
                $"{new string('{', 1000)}\"a\": 1{new string('}', 1000)}", // too much recursion in object
            };

            foreach (string test in testcases)
            {
                ii++;
                try
                {
                    JNode json = parser.Parse(test);
                    tests_failed++;
                    Npp.AddLine($"Expected default settings JSON parser to throw an error on input\n{test}\nbut instead returned {json.ToString()}");
                }
                catch { }
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }

        public static bool TestSpecialParserSettings()
        {
            JsonParser simpleparser = new JsonParser();
            JsonParser parser = new JsonParser(LoggerLevel.JSON5, true, false);
            var testcases = new (string inp, JNode desired_out)[]
            {
                ("{\"a\": 1, // this is a comment\n\"b\": 2}", simpleparser.Parse("{\"a\": 1, \"b\": 2}")),
                (@"[1,
/* this is a
multiline comment
*/
2]",
                    simpleparser.Parse("[1, 2]")
                ),
                ("[.75, 0xff, +9]", simpleparser.Parse("[0.75, 255, 9]")), // leading decimal points, hex numbers, leading + sign
                ("\"2022-06-04\"", new JNode(new DateTime(2022, 6, 4), Dtype.DATE, 0)),
                ("\"1956-11-13 11:17:56.123\"", new JNode(new DateTime(1956, 11, 13, 11, 17, 56, 123), Dtype.DATETIME, 0)),
                ("\"1956-13-12\"", new JNode("1956-13-12", Dtype.STR, 0)), // bad date- month too high
                ("\"1956-11-13 25:56:17\"", new JNode("1956-11-13 25:56:17", Dtype.STR, 0)), // bad datetime- hour too high
                ("\"1956-11-13 \"", new JNode("1956-11-13 ", Dtype.STR, 0)), // bad date- has space at end
                ("['abc', 2, '1999-01-03']", // single-quoted strings 
                    new JArray(0, new List<JNode>(new JNode[]{new JNode("abc", Dtype.STR, 0),
                                                          new JNode(Convert.ToInt64(2), Dtype.INT, 0),
                                                          new JNode(new DateTime(1999, 1, 3), Dtype.DATE, 0)}))),
                ("{'a': \"1\", \"b\": 2}", // single quotes and double quotes in same thing
                    simpleparser.Parse("{\"a\": \"1\", \"b\": 2}")),
                (@"{'a':
                  // one comment
                  // wow, another single-line comment?
                  // go figure
                  [2]}",
                simpleparser.Parse("{\"a\": [2]}")),
                ("{'a': [ /* internal comment */ 2 ]}", TryParse("{\"a\": [2]}", simpleparser)),
                ("[1, 2] // trailing comment", TryParse("[1, 2]", simpleparser)),
                ("// the comments return!\n[2]", TryParse("[2]", simpleparser)),
                ("# python comment at start of file\n[2]", TryParse("[2]", simpleparser)),
                ("[1, 2] # python comment at end of file", TryParse("[1, 2]", simpleparser)),
                ("[1, 2]\r\n# python comment\r\n# another python comment", TryParse("[1, 2]", simpleparser)),
                (@"
                  /* multiline comment 
                   */
                  /* followed by another multiline comment */
                 // followed by a single line comment 
                 /* and then a multiline comment */ 
                 [1, 2]
                 /* and one last multiline comment */", TryParse("[1, 2]", simpleparser)),
                (@"
                  /* multiline comment 
                   */
                   # and a Python-style comment
                // and a JavaScript-style single-line comment
                 # then Python comment
                 /* and then a multiline comment */ 
            # another python comment
                  # another python comment
                 [1, 2]
                 /* and one last multiline comment */", TryParse("[1, 2]", simpleparser)),
                 ("{//\n}", new JObject()), // empty single-line comment at end of object
                 ("[//\n]", new JArray()), // empty single-line comment at end of array
                 ("{//\n'a'//\n://\n [//\n1 2 {\"a\" //\n1//\n} \n4//\n\n//\n5]//\n]//", // empty single-line comments everywhere
                    TryParse("{\"a\":[1,2,{\"a\":1},4,5]}", simpleparser)),
                 ("[1// single-line comment immediately after number\n,2]", TryParse("[1,2]", simpleparser)),
                 ("[1/* multiline comment immediately after number*/,2]", TryParse("[1,2]", simpleparser)),
            };
            int tests_failed = 0;
            int ii = 0;
            foreach ((string inp, JNode desired_out) in testcases)
            {
                if (desired_out == null)
                {
                    ii += 1;
                    tests_failed += 1;
                    continue;
                }
                ii++;
                JNode result = new JNode();
                string base_message = $"Expected JsonParser(ParserState.JSON5).Parse({inp})\nto return\n{desired_out.ToString()}\n";
                try
                {
                    result = parser.Parse(inp);
                    try
                    {
                        if (!desired_out.Equals(result))
                        {
                            tests_failed++;
                            Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
                        }
                    }
                    catch
                    {
                        tests_failed++;
                        Npp.AddLine($"{base_message}Instead returned\n{result.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine($"{base_message}Instead threw exception\n{ex}");
                }
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }

        public static bool TestLinter()
        {
            JsonParser simpleparser = new JsonParser();
            JsonParser parser = new JsonParser(LoggerLevel.STRICT, true, false, false);
            var testcases = new (string inp, string desired_out, string[] desired_lint)[]
            {
                ("[1, 2]", "[1, 2]", new string[]{ } ), // syntactically valid JSON
                ("[1 2]", "[1, 2]", new string[]{"No comma between array members" }),
                ("[1, , 2]", "[1, 2]", new string[]{$"Two consecutive commas after element 0 of array"}),
                ("[1, 2,]", "[1, 2]", new string[]{"Comma after last element of array"}),
                ("[1 2,]", "[1, 2]", new string[]{"No comma between array members", "Comma after last element of array"}),
                ("{\"a\" 1}", "{\"a\": 1}", new string[]{"No ':' between key 0 and value 0 of object"}),
                ("{\"a\": 1 \"b\": 2}", "{\"a\": 1, \"b\": 2}", new string[]{ "No comma after key-value pair 0 in object" }),
                ("[1  \"a\n\"]", "[1, \"a\\n\"]", new string[]{"No comma between array members", "String literal contains newline"}),
                ("[NaN, -Infinity, Infinity]", "[NaN, -Infinity, Infinity]",
                    new string[]{ "NaN is not part of the original JSON specification",
                                  "Infinity is not part of the original JSON specification",
                                  "Infinity is not part of the original JSON specification" }),
                ("{'a\n':[1,2,},]", "{\"a\\n\": [1,2]}", new string[]{"Singlequoted strings are only allowed in JSON5", "Object key contains newline", "Tried to terminate an array with '}'", "Comma after last element of array", "Tried to terminate object with ']', Comma after last key-value pair of object" }),
                ("[1, 2", "[1, 2]", new string[]{ "Unterminated array" }),
                ("{\"a\": 1", "{\"a\": 1}", new string[]{ "Unterminated object" }),
                ("{\"a\": [1, {\"b\": 2", "{\"a\": [1, {\"b\": 2}]}", new string[] { "Unterminated object",
                                                                                                "Unterminated array", 
                                                                                                "Unterminated object" }),
                ("{", "{}", new string[] { "Unterminated object" }),
                ("[", "[]", new string[] { "Unterminated array" }),
                ("[+1.5, +2e3, +Infinity, +7.5/-3]", "[1.5, 2000.0, Infinity, -2.5]", new string[]{ "Leading + signs in numbers are not allowed except in JSON5", "Leading + signs in numbers are not allowed except in JSON5", "Leading + signs in numbers are not allowed except in JSON5", "Infinity is not part of the original JSON specification", "Leading + signs in numbers are not allowed except in JSON5", "Fractions of the form 1/3 are not part of any JSON specification" }),
                ("[1] // comment", "[1]", new string[] { "JavaScript comments are not part of the original JSON specification" }),
                ("{\"a\": 1,,\"b\": 2}", "{\"a\": 1, \"b\": 2}", new string[] { "Two consecutive commas after key-value pair 0 of object" }),
                ("[1]\r\n# Python comment", "[1]", new string[] { "Python-style '#' comments are not part of any well-accepted JSON specification" }),
                ("[1] /* unterminated multiline", "[1]", new string[] { "JavaScript comments are not part of the original JSON specification", "Unterminated multi-line comment" }),
                ("\"\\u043\"", "\"\"", new string[] { "Could not find valid hexadecimal of length 4" }),
                ("'abc'", "\"abc\"", new string[] { "Singlequoted strings are only allowed in JSON5" }),
                ("  \"a\n\"", "\"a\\n\"", new string[] { "String literal contains newline" }),
                ("[.75, 0xabcdef, +9, -0xABCDEF, +0x0123456789]", "[0.75, 11259375, 9, -11259375, 4886718345]", new string[]
                {
                    "Numbers with a leading decimal point are only part of JSON5", "Hexadecimal numbers are only part of JSON5", "Leading + signs in numbers are not allowed except in JSON5", "Hexadecimal numbers are only part of JSON5", "Leading + signs in numbers are not allowed except in JSON5", "Hexadecimal numbers are only part of JSON5",
                }),
                ("{\"\u000c\t\": \"\u0008\t\u0009\"}", "{\"\\f\\t\": \"\\b\\t\\u0009\"}", new string[]{
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                }),
                ("{\"\u000a\": \"\u000a\"}", "{\"\\n\": \"\\n\"}", // newlines as unicode escapes
                    new string[]{
                        "Object key contains newline",
                        "String literal contains newline"}),
                ("Inf", "null",   new string[]{"Expected literal starting with 'I' to be Infinity"}),
                ("-Inf", "null",  new string[]{"Expected literal starting with 'I' to be Infinity"}),
                ("NaU", "null",   new string[]{"Expected literal starting with 'N' to be NaN or None"}),
                ("-Nae", "null",  new string[]{"Expected literal starting with 'N' to be NaN or None"}),
                ("trno", "null",  new string[]{"Expected literal starting with 't' to be true"}),
                ("Trno", "null",  new string[]{"Expected literal starting with 'T' to be True"}),
                ("froeu", "null", new string[]{"Expected literal starting with 'f' to be false"}),
                ("Froeu", "null", new string[]{"Expected literal starting with 'F' to be False"}),
                ("nurnoe", "null",new string[]{"Expected literal starting with 'n' to be null or nan"}),
                ("Hugre", "null", new string[]{"Badly located character"}),
                ("[undefined, underpants]", "[null, null]",
                new string[]{
                    "undefined is not part of any JSON specification",
                    "Expected literal starting with 'u' to be undefined"
                }),
                ("[nan, inf, -inf]", "[NaN, Infinity, -Infinity]",
                new string[]
                {
                    "nan is not a valid representation of Not a Number in JSON",
                    "inf is not the correct representation of Infinity in JSON",
                    "inf is not the correct representation of Infinity in JSON"
                }),
                ("\"\\i\"", "\"i\"", new string[]{"Escaped char 'i' is only valid in JSON5"}),
                ("", "null", new string[]{"No input"}),
                ("\t\r\n  // comments\r\n/* */ ", "null", new string[]{ "JavaScript comments are not part of the original JSON specification", "JavaScript comments are not part of the original JSON specification","Json string is only whitespace and maybe comments" }),
                ("[5/ ]", "[5]", new string[]{ "JavaScript comments are not part of the original JSON specification", "Expected JavaScript comment after '/'" }),
                ("\xa0\u2028\u2029\ufeff\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000\"\xa0\u2028\u2029\ufeff\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000\"", "\"\xa0\u2028\u2029\ufeff\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u202F\u205F\u3000\"", new string[]
                {
                    "Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
"Whitespace characters other than ' ', '\\t', '\\r', and '\\n' are only allowed in JSON5",
                }),
                ("{foo: 1, $baz: 2, 草: 2, _quЯ: 3, \\ud83d\\ude00_$\\u1ed3: 4, a\\uff6acf: 5, \\u0008\\u000a: 6}",
                 "{\"foo\": 1, \"$baz\": 2, \"草\": 2, \"_quЯ\": 3, \"😀_$ồ\": 4, \"aｪcf\": 5, \"\\b\\n\": 6}",
                 new string[]{
                    "Unquoted keys are only supported in JSON5",
                    "Unquoted keys are only supported in JSON5",
                    "Unquoted keys are only supported in JSON5",
                    "Unquoted keys are only supported in JSON5",
                    "Unquoted keys are only supported in JSON5",
                    "Unquoted keys are only supported in JSON5",
                    "Unquoted keys are only supported in JSON5",
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    "String literal contains newline", // the \u000a in \\b\\u000a is secretly a newline
                }),
                ("[1,\"b\\\nb\\\rb\\\r\nb\"]", "[1, \"bbbb\"]",
                new string[]
                {
                    "Escaped newline characters are only allowed in JSON5",
                    "Escaped newline characters are only allowed in JSON5",
                    "Escaped newline characters are only allowed in JSON5",
                }),
                ("{\"b\\\nb\\\rb\\\r\n  b\": 3}", "{\"bbb  b\": 3}",
                new string[]
                {
                    "Escaped newline characters are only allowed in JSON5",
                    "Escaped newline characters are only allowed in JSON5",
                    "Escaped newline characters are only allowed in JSON5",
                }),
                ("[\"a\\x00b\", 1]", "[\"a\"]", new string[]{"'\\x00' is the null character, which is illegal in JsonTools"}),
                ("[\"a\\u0000b\", 1]", "[\"a\"]", new string[]{"'\\x00' is the null character, which is illegal in JsonTools"}),
                ("{\"\\1\\A\": \"\\7\\B\"}", "{\"1A\": \"7B\"}",
                new string[]{
                    "Escaped char '1' is only valid in JSON5",
                    "Escaped char 'A' is only valid in JSON5",
                    "Escaped char '7' is only valid in JSON5",
                    "Escaped char 'B' is only valid in JSON5",
                }),
                ("{\"\\x51ED\\v\": \"\\x51ED\\v\"}", "{\"QED\\v\": \"QED\\v\"}",
                new string[]{
                    "\\x escapes are only allowed in JSON5",
                    "\\x escapes are only allowed in JSON5",
                }),
                ("{\"j\\u004\": 1}", "{}",
                new string[]
                {
                    "Could not find valid hexadecimal of length 4",
                }),
                ("[\"j\\x3\"]", "[\"j\"]",
                new string[]
                {
                    "Could not find valid hexadecimal of length 2",
                }),
                ("{\"j\\x\": 34}", "{}",
                new string[]
                {
                    "Could not find valid hexadecimal of length 2",
                }),
                ("{\"a\": 1, \"a\": 2}", "{\"a\": 2}", new string[]{"Object has multiple of key \"a\""}),
                ("[True, False, None]", "[true, false, null]",
                new string[]
                {
                    "True is not an accepted part of any JSON specification",
                    "False is not an accepted part of any JSON specification",
                    "None is not an accepted part of any JSON specification"
                }),
            };

            int tests_failed = 0;
            int ii = 0;
            foreach ((string inp, string desired_out, string[] expected_lint) in testcases)
            {
                ii++;
                JNode jdesired = TryParse(desired_out, simpleparser);
                if (jdesired == null)
                {
                    ii += 1;
                    tests_failed += 1;
                    continue;
                }
                JNode result = new JNode();
                string expected_lint_str = "[" + string.Join(", ", expected_lint) + "]";
                string base_message = $"Expected JsonParser(LoggerLevel.STRICT).Parse({inp})\nto return\n{desired_out} and have lint {expected_lint_str}\n";
                try
                {
                    result = parser.Parse(inp);
                    if (parser.lint.Count == 0 && expected_lint.Length != 0)
                    {
                        tests_failed++;
                        Npp.AddLine(base_message + "Parser had no lint");
                        continue;
                    }
                    StringBuilder lint_sb = new StringBuilder();
                    lint_sb.Append('[');
                    for (int jj = 0; jj < parser.lint.Count; jj++)
                    {
                        lint_sb.Append(parser.lint[jj].message);
                        if (jj < parser.lint.Count - 1) lint_sb.Append(", ");
                    }
                    lint_sb.Append("]");
                    string lint_str = lint_sb.ToString();
                    try
                    {
                        if (!jdesired.Equals(result) || lint_str != expected_lint_str)
                        {
                            tests_failed++;
                            Npp.AddLine($"{base_message}Instead returned\n{result.ToString()} and had lint {lint_str}");
                        }
                    }
                    catch
                    {
                        tests_failed++;
                        Npp.AddLine($"{base_message}Instead returned\n{result.ToString()} and had lint {lint_str}");
                    }
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine($"{base_message}Instead threw exception\n{ex}");
                }
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }

        public static bool TestJsonLines()
        {
            JsonParser parser = new JsonParser();
            var testcases = new string[][]
            {
                new string[]
                {
                    "[\"a\", \"b\"]\r\n" +
                    "{\"a\": false, \"b\": 2}\r\n" +
                    "null\r\n" +
                    "1.5", // normal style
                    "[[\"a\", \"b\"], {\"a\": false, \"b\": 2}, null, 1.5]"
                },
                new string[]
                {
                    "[1, 2]\n[3, 4]\n", // newline at end
                    "[[1, 2], [3, 4]]"
                },
                new string[]
                {
                    "{\"a\": [1, 2], \"b\": -7.8}", // single document
                    "[{\"a\": [1, 2], \"b\": -7.8}]"
                },
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (string[] test in testcases)
            {
                string input = test[0];
                JNode desired_output = TryParse(test[1], parser);
                JNode json = TryParse(input, parser, true);
                if (json == null)
                {
                    ii++;
                    tests_failed++;
                    continue;
                }
                if (!json.Equals(desired_output))
                {
                    tests_failed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
Expected
{1}
Got
{2} ",
                                     ii + 1, test[1], json.ToString()));
                }
            }

            string[] should_throw_testcases = new string[]
            {
                "[1,\n2]\n[3, 4]", // one doc covers multiple lines
                "[1, 2]\n\n3", // two lines between docs
                "[1, 2] [3, 4]", // two docs on same line
                "", // empty input
                "[1, 2]\n[3, 4", // final doc is invalid
                "[1, 2\n[3, 4]", // first doc is invalid
                "[1, 2]\nd", // bad char at EOF
                "[1, 2]\n\n", // multiple newlines after last doc
            };

            foreach (string test in should_throw_testcases)
            {
                ii++;
                try
                {
                    JNode json = parser.ParseJsonLines(test);
                    tests_failed++;
                    Npp.AddLine($"Expected JSON Lines parser to throw an error on input\n{test}\nbut instead returned {json.ToString()}");
                }
                catch { }
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }

        public static bool TestCultureIssues()
        {
            int ii = 0;
            int tests_failed = 0;
            // change current culture to German because they use a comma decimal sep (see https://github.com/molsonkiko/JsonToolsNppPlugin/issues/17)
            ii++;
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-de", true);
            JNode jsonFloat2 = new JsonParser().Parse("2.0");
            string jsonFloat2str = jsonFloat2.ToString();
            if (jsonFloat2str != "2.0")
            {
                tests_failed++;
                Npp.AddLine("Expected parsing of 2.0 to return 2.0 even when the current culture is German, " +
                    $"but instead got {jsonFloat2str} when the culture is German");
            }
            CultureInfo.CurrentCulture = currentCulture;
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
            return tests_failed > 0;
        }
    }
}
