using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class JsonParserTester
    {
        public static string NL = System.Environment.NewLine;

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

        public static void TestJNodeCopy()
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
        }

        public static void Test()
        {
            JsonParser parser = new JsonParser();
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
            var testcases = new string[][]
            {
                new string[]{ example, norm_example, pprint_example, "general parsing" },
                new string[] { "[[]]", "[[]]", "[" + NL + "    [" + NL + "    ]" + NL + "]", "empty lists" },
                new string[] { "\"abc\"", "\"abc\"", "\"abc\"", "scalar string" },
                new string[] { "1", "1", "1", "scalar int" },
                new string[] { "-1.0", "-1.0", "-1.0", "negative scalar float" },
                new string[] { "3.5", "3.5", "3.5", "scalar float" },
                new string[] { "-4", "-4", "-4", "negative scalar int" },
                new string[] { "[{\"FullName\":\"C:\\\\123467756\\\\Z\",\"LastWriteTimeUtc\":\"\\/Date(1600790697130)\\/\"}," +
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
                             "open issue in Kapilratnani's JSON-Viewer regarding forward slashes having '/' stripped" },
                new string[] { "111111111111111111111111111111", $"1.11111111111111E+29", $"1.11111111111111E+29",
                    "auto-conversion of int64 overflow to double" },
                new string[] {
                    "{\"basst\": 1, \"baßk\": 1, \"blue\": 1, \"bLue\": 1, \"blve\": 1, \"blüe\": 1, \"oyster\": 1, \"spb\": 1, \"Spb\": 1, \"spä\": 1, \"öyster\": 1}",
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
                    "culture-sensitive sorting of keys (e.g., 'baßk' should sort before 'basst')"}
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (string[] test in testcases)
            {
                //(string input, string norm_input, string pprint_desired, string msg)
                string input = test[0], norm_input = test[1], pprint_desired = test[2], msg = test[3];
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
                    string fullMsg = String.Format(@"Test {0} ({1}) failed:
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
                    msg = String.Format(@"Test {0} (parsing ToString result returns original) failed:
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
                    msg = String.Format(@"Test {0} (pretty-print {1}) failed:
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
                    msg = String.Format(@"Test {0} (parsing PrettyPrint result returns original) failed:
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
                    Npp.AddLine(String.Format(@"Test {0} (minimal whitespace printing) failed:
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
                    Npp.AddLine(String.Format(@"Test {0} (minimal whitespace printing) failed:
Expected
{1}
Got
{2} ",
                                     ii + 1, unsorted_desired, unsorted_true));
                }
            }
            #endregion
            #region PrettyPrintTests
            string objstr = "{\"a\": [1, 2, 3], \"b\": {}, \"c\": [], \"d\": 3}";
            JNode onode = TryParse(objstr, parser);
            if (onode != null)
            {
                JObject obj = (JObject)onode;
                string pp = obj.PrettyPrint(4, true, PrettyPrintStyle.Whitesmith);
                string pp_ch_line = obj.PrettyPrintAndChangePositions(4, true, PrettyPrintStyle.Whitesmith);
                if (pp != pp_ch_line)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format(@"Test {0} failed:
Expected PrettyPrintAndChangePositions({1}) to return
{2}
instead got
{3}",
                                                    ii + 1, objstr, pp, pp_ch_line));
                }
                ii++;

                var keylines = new (string key,
                    int whitesmith_pos, int google_pos,
                    int tostring_miniwhite_pos, int tostring_pos)[]
                {
                    ("a", 13, 12, 5, 6),
                    ("b", 57, 67, 17, 22),
                    ("c", 78, 87, 24, 31),
                    ("d", 94, 107, 31, 40)
                };
                foreach ((string key, int whitesmith_pos, int google_pos, int tostring_miniwhite_pos, int tostring_pos) in keylines)
                {
                    obj.PrettyPrintAndChangePositions(4, true, PrettyPrintStyle.Whitesmith);
                    int got_pos = obj[key].position;
                    if (got_pos != whitesmith_pos)
                    {
                        tests_failed++;
                        Npp.AddLine($"After Whitesmith-style PrettyPrintAndChangePositions({objstr}), expected the position of child {key} to be {whitesmith_pos}, got {got_pos}.");
                    }
                    ii++;
                    obj.PrettyPrintAndChangePositions(4, true, PrettyPrintStyle.Google);
                    got_pos = obj[key].position;
                    if (got_pos != google_pos)
                    {
                        tests_failed++;
                        Npp.AddLine($"After Google-style PrettyPrintAndChangePositions({objstr}), expected the position of child {key} to be {google_pos}, got {got_pos}.");
                    }
                    ii++;
                    obj.ToStringAndChangePositions(true);
                    got_pos = obj[key].position;
                    if (got_pos != tostring_pos)
                    {
                        tests_failed++;
                        Npp.AddLine($"After ToStringAndChangePositions({objstr}), expected the position of child {key} to be {tostring_pos}, got {got_pos}.");
                    }
                    ii++;
                    obj.ToStringAndChangePositions(true, ":", ",");
                    got_pos = obj[key].position;
                    if (got_pos != tostring_miniwhite_pos)
                    {
                        tests_failed++;
                        Npp.AddLine($"After minimal-whitespace ToStringAndChangePositions({objstr}), expected the position of child {key} to be {tostring_miniwhite_pos}, got {got_pos}.");
                    }
                    ii++;
                }
                string tostr = obj.ToString();
                string tostr_ch_line = obj.ToStringAndChangePositions();
                ii++;
                if (tostr != tostr_ch_line)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format(@"Test {0} failed:
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
                new object[] { "[]", "[1, 2]", false }
            };
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
        }

        public static void TestThrowsWhenAppropriate()
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
        }

        public static void TestSpecialParserSettings()
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
                ("[1,\"b\\\nb\"]", simpleparser.Parse("[1, \"b\\nb\"]")), // escaped newlines
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
                 ("{//\n'a'//\n://\n [//\n1 2 {\"a\" //\n1//\n} \n\"a\"//\n\n//\n5]//\n]//", // empty single-line comments everywhere
                    TryParse("{\"a\":[1,2,{\"a\":1},\"a\",5]}", simpleparser)),
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
        }

        public static void TestLinter()
        {
            JsonParser simpleparser = new JsonParser();
            JsonParser parser = new JsonParser(LoggerLevel.OK, true, false, false);
            var testcases = new (string inp, string desired_out, string[] desired_lint)[]
            {
                ("[1, 2]", "[1, 2]", new string[]{ } ), // syntactically valid JSON
                ("[1 2]", "[1, 2]", new string[]{"No comma between array members" }),
                ("[1, , 2]", "[1, 2]", new string[]{$"Two consecutive commas after element 0 of array"}),
                ("[1, 2,]", "[1, 2]", new string[]{"Comma after last element of array"}),
                ("[1 2,]", "[1, 2]", new string[]{"No comma between array members", "Comma after last element of array"}),
                ("{\"a\" 1}", "{\"a\": 1}", new string[]{"No ':' between key 0 and value 0 of object"}),
                ("{\"a\": 1 \"b\": 2}", "{\"a\": 1, \"b\": 2}", new string[]{ "No comma after key-value pair 0 in object" }),
                ("[1  \"a\n\"]", "[1, \"a\\n\"]", new string[]{"No comma between array members", "String literal starting at position 4 contains newline"}),
                ("[NaN, -Infinity, Infinity]", "[NaN, -Infinity, Infinity]",
                    new string[]{ "NaN is not part of the original JSON specification",
                                  "Infinity is not part of the original JSON specification",
                                  "Infinity is not part of the original JSON specification" }),
                ("{'a\n':[1,2,},]", "{\"a\\n\": [1,2]}", new string[]{"Strings must be quoted with \" rather than '",
                                                         "String literal starting at position 1 contains newline",
                                                         "Comma after last element of array",
                                                         "Tried to terminate an array with '}'",
                                                         "Comma after last key-value pair of object",
                                                         "Tried to terminate object with ']'" }),
                ("[1, 2", "[1, 2]", new string[]{ "Unterminated array" }),
                ("{\"a\": 1", "{\"a\": 1}", new string[]{ "Unterminated object" }),
                ("{\"a\": [1, {\"b\": 2", "{\"a\": [1, {\"b\": 2}]}", new string[] { "Unterminated object",
                                                                                                "Unterminated array", 
                                                                                                "Unterminated object" }),
                ("{", "{}", new string[] { "Unexpected end of JSON" }),
                ("[", "[]", new string[] { "Unexpected end of JSON" }),
                ("[+1.5, +2e3, +Infinity]", "[1.5, 2000, Infinity, 0.75]", new string[]{ "Infinity is not part of the original JSON specification" }),
                ("[1] // comment", "[]", new string[] { "Comments are not part of the original JSON specification" }),
                ("{\"a\": 1,,\"b\": 2}", "{\"a\": 1, \"b\": 2}", new string[] { "Two consecutive commas after key-value pair 0 of object" }),
                ("[1]\r\n# Python comment", "[1]", new string[] { "Python-style '#' comments are not part of any well-accepted JSON specification." }),
                ("[1] /* unterminated multiline", "[1]", new string[] { "Unterminated multi-line comment" }),
                ("\"\\u043\"", "null", new string[] { "Could not find valid hexadecimal of length 4" }),
                ("'abc'", "\"abc\"", new string[] { "Singlequoted strings are only allowed in JSON5" }),
                ("  \"a\n\"", "\"a\\n\"", new string[] { "String literal starting at position 2 contains newline" }),
                ("[.75, 0xff, +9]", "[0.75, 255, 9]", new string[]
                {
                    "leading decimal point",
                    "hex number",
                    "leading + sign"
                }),
                ("[1,\"b\\\nb\"]", "[1, \"b\\nb\"]", new string[]{"excaped newline"}),
                ("{\"\\u0009\t\": \"\\u0009\t\"}", "{\"\\u0009\\t\": \"\\t\\t\"}", new string[]{
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
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
                    if (parser.lint.Count == 0)
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
        }

        public static void TestJsonLines()
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
                    Npp.AddLine(String.Format(@"Test {0} failed:
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
        }
    }
}
