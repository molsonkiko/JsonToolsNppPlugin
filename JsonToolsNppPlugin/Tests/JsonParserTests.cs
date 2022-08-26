﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Tests
{
    public class JsonParserTester
    {
        public static JNode TryParse(string input, JsonParser parser)
        {
            try
            {
                return parser.Parse(input);
            }
            catch (Exception e)
            {
                Npp.AddLine($"While parsing input {input}\nthrew error {e}");
            }
            return null;
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
            string NL = Environment.NewLine;
            #region ParserTests
            string example = "{\"a\":[-1, true, {\"b\" :  0.5, \"c\": \"\\uae77\"},null],\n"
                    + "\"a\\u10ff\":[true, false, NaN, Infinity,-Infinity, {},\t\"\\u043ea\", []], "
                    + "\"back'slas\\\"h\": [\"\\\"'\\f\\n\\b\\t/\", -0.5, 23, \"\"]} ";
            string norm_example = "{\"a\": [-1, true, {\"b\": 0.5, \"c\": \"\\uae77\"}, null], "
                    + "\"a\\u10ff\": [true, false, NaN, Infinity, -Infinity, {}, \"\\u043ea\", []], "
                    + "\"back'slas\\\"h\": [\"\\\"'\\f\\n\\b\\t/\", -0.5, 23, \"\"]}";
            string pprint_example = "{" +
                                    NL + "\"a\":" +
                                    NL + "    [" +
                                    NL + "    -1," +
                                    NL + "    true," +
                                    NL + "        {" +
                                    NL + "        \"b\": 0.5," +
                                    NL + "        \"c\": \"\\uae77\"" +
                                    NL + "        }," +
                                    NL + "    null" +
                                    NL + "    ]," +
                                    NL + "\"a\\u10ff\":" +
                                    NL + "    [" +
                                    NL + "    true," +
                                    NL + "    false," +
                                    NL + "    NaN," +
                                    NL + "    Infinity," +
                                    NL + "    -Infinity," +
                                    NL + "        {" +
                                    NL + "        }," +
                                    NL + "    \"\\u043ea\"," +
                                    NL + "        [" +
                                    NL + "        ]" +
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
                new string[] { "1/2", "0.5", "0.5", "fractions" },
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
                    ii += 2;
                    tests_failed += 2;
                    continue;
                }
                string norm_str_out = json.ToString();
                string pprint_out = json.PrettyPrint(4);
                if (norm_str_out != norm_input)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format(@"Test {0} ({1}) failed:
Expected
{2}
Got
{3} ",
                                     ii + 1, msg, norm_input, norm_str_out));
                }
                ii++;
                if (pprint_out != pprint_desired)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format(@"Test {0} (pretty-print {1}) failed:
Expected
{2}
Got
{3} ",
                                     ii + 1, msg, pprint_desired, pprint_out));
                }
                ii++;
            }
            #endregion
            #region PrettyPrintTests
            string objstr = "{\"a\": [1, 2, 3], \"b\": {}, \"c\": [], \"d\": 3}";
            JNode onode = TryParse(objstr, parser);
            if (onode != null)
            {
                JObject obj = (JObject)onode;
                string pp = obj.PrettyPrint();
                string pp_ch_line = obj.PrettyPrintAndChangeLineNumbers();
                if (pp != pp_ch_line)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format(@"Test {0} failed:
Expected PrettyPrintAndChangeLineNumbers({1}) to return
{2}
instead got
{3}",
                                                    ii + 1, objstr, pp, pp_ch_line));
                }
                ii++;

                var keylines = new object[][]
                {
                new object[]{"a", 2 },
                new object[]{ "b", 8 },
                new object[]{ "c", 11 },
                new object[]{"d", 13 }
                };
                foreach (object[] kl in keylines)
                {
                    string key = (string)kl[0];
                    int expected_line = (int)kl[1];
                    int true_line = obj[key].line_num;
                    if (true_line != expected_line)
                    {
                        tests_failed++;
                        Npp.AddLine($"After PrettyPrintAndChangeLineNumbers({objstr}), expected the line of child {key} to be {expected_line}, got {true_line}.");
                    }
                    ii++;
                }

                string tostr = obj.ToString();
                string tostr_ch_line = obj.ToStringAndChangeLineNumbers();
                ii++;
                if (tostr != tostr_ch_line)
                {
                    tests_failed++;
                    Npp.AddLine(String.Format(@"Test {0} failed:
Expected ToStringAndChangeLineNumbers({1}) to return
{2}
instead got
{3}",
                                                    ii + 1, objstr, tostr, tostr_ch_line));
                }
                foreach (object[] kl in keylines)
                {
                    string key = (string)kl[0];
                    ii++;
                    int true_line = obj[key].line_num;
                    if (true_line != 0)
                    {
                        tests_failed++;
                        Npp.AddLine($"After ToStringAndChangeLineNumbers({objstr}), expected the line of child {key} to be 0, got {true_line}.");
                    }
                }

                // test if the parser correctly counts line numbers in nested JSON
                JNode pp_node = TryParse(pp_ch_line, parser);
                if (pp_node != null)
                {
                    JObject pp_obj = (JObject)pp_node;
                    foreach (object[] kl in keylines)
                    {
                        string key = (string)kl[0];
                        int expected_line = (int)kl[1];
                        int true_line = pp_obj[key].line_num;
                        ii++;
                        if (true_line != expected_line)
                        {
                            tests_failed++;
                            Npp.AddLine($"After PrettyPrintAndChangeLineNumbers({pp}), expected the line of child {key} to be {expected_line}, got {true_line}.");
                        }
                    }
                }
            }
            else
            {
                tests_failed += 14;
                ii += 14;
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
                new object[] { "{\"a\": 1, \"b\": Infinity, \"c\": 0.5}", "{\"b\": Infinity, \"a\": 1, \"c\": 1/2}", true },
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

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }

        public static void TestSpecialParserSettings()
        {
            JsonParser simpleparser = new JsonParser();
            JsonParser parser = new JsonParser(true, true, true);
            var testcases = new object[][]
            {
                new object[]{ "{\"a\": 1, // this is a comment\n\"b\": 2}", simpleparser.Parse("{\"a\": 1, \"b\": 2}") },
                new object[]{ @"[1,
/* this is a
multiline comment
*/
2]",
                    simpleparser.Parse("[1, 2]")
                },
                new object[]{ "\"2022-06-04\"", new JNode(new DateTime(2022, 6, 4), Dtype.DATE, 0) },
                new object[]{ "\"1956-11-13 11:17:56.123\"", new JNode(new DateTime(1956, 11, 13, 11, 17, 56, 123), Dtype.DATETIME, 0) },
                new object[]{ "\"1956-13-12\"", new JNode("1956-13-12", Dtype.STR, 0) }, // bad date- month too high
                new object[]{ "\"1956-11-13 25:56:17\"", new JNode("1956-11-13 25:56:17", Dtype.STR, 0) }, // bad datetime- hour too high
                new object[]{ "\"1956-11-13 \"", new JNode("1956-11-13 ", Dtype.STR, 0) }, // bad date- has space at end
                new object[]{ "['abc', 2, '1999-01-03']", // single-quoted strings 
                    new JArray(0, new List<JNode>(new JNode[]{new JNode("abc", Dtype.STR, 0),
                                                          new JNode(Convert.ToInt64(2), Dtype.INT, 0),
                                                          new JNode(new DateTime(1999, 1, 3), Dtype.DATE, 0)}))},
                new object[]{ "{'a': \"1\", \"b\": 2}", // single quotes and double quotes in same thing
                    simpleparser.Parse("{\"a\": \"1\", \"b\": 2}") },
                new object[]{ @"{'a':
                  // one comment
                  // wow, another single-line comment?
                  // go figure
                  [2]}",
                simpleparser.Parse("{\"a\": [2]}")},
                new object[]{ "{'a': [ /* internal comment */ 2 ]}", TryParse("{\"a\": [2]}", simpleparser) },
                new object[]{ "[1, 2] // trailing comment", TryParse("[1, 2]", simpleparser) },
                new object[]{ "// the comments return!\n[2]", TryParse("[2]", simpleparser) },
                new object[]{ @"
                  /* multiline comment 
                   */
                  /* followed by another multiline comment */
                 // followed by a single line comment 
                 /* and then a multiline comment */ 
                 [1, 2]
                 /* and one last multiline comment */", TryParse("[1, 2]", simpleparser) }
            };
            int tests_failed = 0;
            int ii = 0;
            foreach (object[] test in testcases)
            {
                string inp = (string)test[0];
                if (test[1] is null)
                {
                    ii += 1;
                    tests_failed += 1;
                    continue;
                }
                JNode desired_out = (JNode)test[1];
                ii++;
                JNode result = new JNode(null, Dtype.NULL, 0);
                string base_message = $"Expected JsonParser(true, true, true, true).Parse({inp})\nto return\n{desired_out.ToString()}\n";
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
            JsonParser parser = new JsonParser(true, true, true, true);
            var testcases = new object[][]
            {
                new object[]{ "[1, 2]", "[1, 2]", new string[]{ } }, // syntactically valid JSON
                new object[]{ "[1 2]", "[1, 2]", new string[]{"No comma between array members" } },
                new object[]{ "[1, , 2]", "[1, 2]", new string[]{$"Two consecutive commas after element 0 of array"} },
                new object[]{ "[1, 2,]", "[1, 2]", new string[]{"Comma after last element of array"} },
                new object[]{ "[1 2,]", "[1, 2]", new string[]{"No comma between array members", "Comma after last element of array"} },
                new object[]{ "{\"a\" 1}", "{\"a\": 1}", new string[]{"No ':' between key 0 and value 0 of object"} },
                new object[]{ "{\"a\": 1 \"b\": 2}", "{\"a\": 1, \"b\": 2}", new string[]{ "No comma after key-value pair 0 in object" } },
                new object[]{ "[1  \"a\n\"]", "[1, \"a\\n\"]", new string[]{"No comma between array members", "String literal starting at position 4 contains newline"} },
                new object[]{ "[NaN, -Infinity, Infinity]", "[NaN, -Infinity, Infinity]",
                    new string[]{ "NaN is not part of the original JSON specification",
                                  "-Infinity is not part of the original JSON specification",
                                  "Infinity is not part of the original JSON specification" } },
                new object[]{ "{'a\n':[1,2,},]", "{\"a\\n\": [1,2]}", new string[]{"Strings must be quoted with \" rather than '",
                                                         "String literal starting at position 1 contains newline",
                                                         "Comma after last element of array",
                                                         "Tried to terminate an array with '}'",
                                                         "Comma after last key-value pair of object",
                                                         "Tried to terminate object with ']'"} },
            };

            int tests_failed = 0;
            int ii = 0;
            foreach (object[] test in testcases)
            {
                string inp = (string)test[0], desired_out = (string)test[1];
                string[] expected_lint = (string[])test[2];
                ii++;
                JNode jdesired = TryParse(desired_out, simpleparser);
                if (jdesired == null)
                {
                    ii += 1;
                    tests_failed += 1;
                    continue;
                }
                JNode result = new JNode(null, Dtype.NULL, 0);
                string expected_lint_str = "[" + string.Join(", ", expected_lint) + "]";
                string base_message = $"Expected JsonParser(true, true, true, true).Parse({inp})\nto return\n{desired_out} and have lint {expected_lint_str}\n";
                try
                {
                    result = parser.Parse(inp);
                    if (parser.lint == null)
                    {
                        tests_failed++;
                        Npp.AddLine(base_message + "Lint was null");
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
    }
}
