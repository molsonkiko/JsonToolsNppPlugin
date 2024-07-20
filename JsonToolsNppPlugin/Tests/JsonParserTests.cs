using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class JsonParserTester
    {
        public const string NL = JNode.NL;

        public static JNode TryParse(string input, JsonParser parser, bool isJsonLines = false)
        {
            try
            {
                if (isJsonLines)
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
            int testsFailed = 0;
            JsonParser parser = new JsonParser(LoggerLevel.JSON5);
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
                    testsFailed++;
                    continue;
                }
                JNode cp = node.Copy();
                if (!node.TryEquals(cp, out _))
                {
                    testsFailed++;
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
                        testsFailed++;
                        Npp.AddLine($"Expected mutating {node.ToString()}\nto not mutate Copy({node.ToString()})\nbut it did.");
                        continue;
                    }
                    if (node.Equals(cp))
                    {
                        testsFailed++;
                        Npp.AddLine($"Expected mutating {node.ToString()}\nto not mutate Copy({node.ToString()})\nbut it did.");
                        continue;
                    }
                }
                catch { }
            }
            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
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
            string normExample = "{"
                + "\"a\u10ff\": [true, false, NaN, Infinity, -Infinity, {}, \"\u043ea\", []], "
                + "\"a\": [-1, true, {\"b\": 0.5, \"c\": \"\uae77\"}, null], "
                + "\"back'slas\\\"h\": [\"\\\"'\\f\\n\\b\\t/\", -0.5, 23, \"\"]}";
            string pprintExample = "{" +
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
                (example, normExample, pprintExample, "general parsing"),
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
                ("[3.1234e15, -2.178e15, 7.59e15, 5.71138315710726E+18]",
                    "[3.1234E+15, -2.178E+15, 7.59E+15, 5.71138315710726E+18]",
                    "["+NL+"3.1234E+15,"+NL+"-2.178E+15,"+NL+"7.59E+15,"+NL+"5.71138315710726E+18"+NL+"]",
                    "floating point numbers using 'E' notation that can exactly represent integers"
                ),
                (
                    // valid datetime,              invalid datetime,           invalid date,    valid datetime (no milliseconds),  valid date
                    "[\"2027-02-08 14:02:09.987\", \"1915-13-08 23:54:09.100\", \"1903-05-44\", \"2005-03-04 01:00:00\",            \"1843-06-11\"]",
                    "[\"2027-02-08 14:02:09.987\", \"1915-13-08 23:54:09.100\", \"1903-05-44\", \"2005-03-04 01:00:00\", \"1843-06-11\"]",
                    "["+ NL +
                    "\"2027-02-08 14:02:09.987\"," + NL + 
                    "\"1915-13-08 23:54:09.100\"," + NL + 
                    "\"1903-05-44\"," + NL +
                    "\"2005-03-04 01:00:00\"," + NL +
                    "\"1843-06-11\"" + NL +
                    "]",
                    "dates and datetimes (both valid and invalid)"
                ),
            };
            int testsFailed = 0;
            int ii = 0;
            foreach ((string input, string normInput, string pprintDesired, string msg_) in testcases)
            {
                string msg = msg_;
                JNode json = TryParse(input, parser);
                if (json == null)
                {
                    ii += 4;
                    testsFailed += 4;
                    continue;
                }
                string normStrOut = json.ToString();
                if (normStrOut != normInput)
                {
                    testsFailed++;
                    string fullMsg = string.Format(@"Test {0} ({1}) failed:
Expected
{2}
Got
{3}
", ii + 1, msg, normInput, normStrOut);
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(fullMsg), fullMsg);
                }
                ii++;
                JNode jsonFromNormStrOut = parser.Parse(normStrOut);
                if (!jsonFromNormStrOut.TryEquals(json, out _)
                    && input != "111111111111111111111111111111") // skip b/c floating-point imprecision
                {
                    testsFailed++;
                    msg = string.Format(@"Test {0} (parsing ToString result returns original) failed:
Expected Parse(Parse({1}).toString()) to return
{1}
Got
{2}
", ii + 1, normStrOut, jsonFromNormStrOut.ToString());
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(msg), msg);
                }
                ii++;
                string pprintOut = json.PrettyPrint(4, true, PrettyPrintStyle.Whitesmith);
                if (pprintOut != pprintDesired)
                {
                    testsFailed++;
                    msg = string.Format(@"Test {0} (pretty-print {1}) failed:
Expected
{2}
Got
{3}
", ii + 1, msg, pprintDesired, pprintOut);
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(msg), msg);
                }
                ii++;
                JNode jsonFromPprintOut = parser.Parse(pprintOut);
                if (!jsonFromPprintOut.TryEquals(json, out _)
                    && input != "111111111111111111111111111111") // skip b/c floating-point imprecision
                {
                    testsFailed++;
                    msg = string.Format(@"Test {0} (parsing PrettyPrint result returns original) failed:
Expected Parse(Parse({1}).PrettyPrint()) to return
{1}
Got
{2}
", ii + 1, normStrOut, jsonFromPprintOut.ToString());
                    Npp.editor.AppendText(Encoding.UTF8.GetByteCount(msg), msg);
                }
                ii++;
            }
            #endregion
            #region OtherPrintStyleTests
            var minimalWhitespaceTestcases = new string[][]
            {
                new string[]
                {
                    "[1, {\"a\": 2, \"b\": 3}, [4, 5]]",
                    "[1,{\"a\":2,\"b\":3},[4,5]]"
                },
            };
            foreach (string[] test in minimalWhitespaceTestcases)
            {
                string inp = test[0];
                string compactDesired = test[1];
                JNode json = TryParse(inp, parser);
                if (json == null)
                {
                    testsFailed++;
                    continue;
                }
                string compactTrue = json.ToString(true, ":", ",");
                if (compactTrue != compactDesired)
                {
                    testsFailed++;
                    Npp.AddLine(string.Format(@"Test {0} (minimal whitespace printing) failed:
Expected
{1}
Got
{2} ",
                                     ii + 1, compactDesired, compactTrue));
                }
                ii++;
            }
            var dontSortKeysTestcases = new string[][]
            {
                new string[]
                {
                    "{\"a\": 2,  \"c\": 4,   \"b\": [{\"e\":   1,\"d\":2}]}",
                    "{\"a\": 2, \"c\": 4, \"b\": [{\"e\": 1, \"d\": 2}]}"
                },
            };
            foreach (string[] test in dontSortKeysTestcases)
            {
                string inp = test[0];
                string unsortedDesired = test[1];
                JNode json = TryParse(inp, parser);
                ii++;
                if (json == null)
                {
                    testsFailed++;
                    continue;
                }
                string unsortedTrue = json.ToString(false);
                if (unsortedTrue != unsortedDesired)
                {
                    testsFailed++;
                    Npp.AddLine(string.Format(@"Test {0} (minimal whitespace printing) failed:
Expected
{1}
Got
{2} ",
                                     ii + 1, unsortedDesired, unsortedTrue));
                }
            }
            #endregion
            #region JNode Position Tests
            string objstr = "/*foo*/ //bar\r\n{\"a\":  [1, 2, 3],  \"b\": {}, 　\"Я草\": [], \"😀\": [[100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112], [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113], [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112,//😀\r\n [113, 114]]],/*cömment*/\"d\":[{\"o\":\"öyster\"},\"cät\",#python \r\n\"dog\"],\"e\":false,//cömment\r\n\"f\":null}//baz \r\n# more python\r\n/*quz\r\nzuq*/";
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
            bool oldThrowIfLogged = parser.throwIfLogged;
            parser.throwIfLogged = false;
            // also includes some weird space like '\xa0' only allowed in JSON5, and all kinds of comments (including Python-style)
            JNode onode = TryParse(objstr, parser);
            if (onode != null && onode is JObject obj)
            {
                Npp.AddLine($"obj =\r\n{objstr}\r\n");
                string correctPprintObjstr = "{\r\n    \"a\": [1, 2, 3],\r\n    \"b\": {},\r\n    \"Я草\": [],\r\n    \"😀\": [\r\n        [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112],\r\n        [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113],\r\n        [\r\n            100,\r\n            101,\r\n            102,\r\n            103,\r\n            104,\r\n            105,\r\n            106,\r\n            107,\r\n            108,\r\n            109,\r\n            110,\r\n            111,\r\n            112,\r\n            [113, 114]\r\n        ]\r\n    ],\r\n    \"d\": [{\"o\": \"öyster\"}, \"cät\", \"dog\"],\r\n    \"e\": false,\r\n    \"f\": null\r\n}";
                ii++;
                string actualPprintObjstr = obj.PrettyPrint(4, false, PrettyPrintStyle.PPrint);
                if (actualPprintObjstr != correctPprintObjstr)
                {
                    testsFailed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
    Expected PPrint-style PrettyPrintAndChangePositions(obj) to return
    {1}
    instead got
    {2}",
                        ii + 1, correctPprintObjstr, actualPprintObjstr));
                }
                ii++;
                // the formatting with '\t' indent is actually significantly different
                // because having only 1 char of indent makes it so all the sub-children fit on a single line 
                string correctPprintObjstrTabIndent = "{\r\n\t\"a\": [1, 2, 3],\r\n\t\"b\": {},\r\n\t\"Я草\": [],\r\n\t\"😀\": [\r\n\t\t[100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112],\r\n\t\t[100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113],\r\n\t\t[100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, [113, 114]]\r\n\t],\r\n\t\"d\": [{\"o\": \"öyster\"}, \"cät\", \"dog\"],\r\n\t\"e\": false,\r\n\t\"f\": null\r\n}";
                string actualPprintObjstrTabIndent = obj.PrettyPrint(1, false, PrettyPrintStyle.PPrint, int.MaxValue, '\t');
                if (actualPprintObjstrTabIndent != correctPprintObjstrTabIndent)
                {
                    testsFailed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
    Expected PPrint-style PrettyPrintAndChangePositions(obj) with tab indentation to return
    {1}
    instead got
    {2}",
                        ii + 1, correctPprintObjstrTabIndent, actualPprintObjstrTabIndent));
                }
                ii++;
                string correctTostringWithcommentsObjstr = "/*foo*/\r\n//bar\r\n//😀\r\n/*cömment*/\r\n//python \r\n//cömment\r\n//baz \r\n// more python\r\n/*quz\r\nzuq*/\r\n{\"a\": [1, 2, 3], \"b\": {}, \"Я草\": [], \"😀\": [[100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112], [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113], [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, [113, 114]]], \"d\": [{\"o\": \"öyster\"}, \"cät\", \"dog\"], \"e\": false, \"f\": null}";
                string actualTostringWithcommentsObjstr = obj.ToStringWithComments(parser.comments, false);
                if (correctTostringWithcommentsObjstr != actualTostringWithcommentsObjstr)
                {
                    testsFailed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
    Expected ToStringWithComments(obj) to return
    {1}
    instead got
    {2}",
                        ii + 1, correctTostringWithcommentsObjstr,
                        actualTostringWithcommentsObjstr));
                }
                ii++;
                string correctPrettyprintWithcommentsObjstr = "/*foo*/\r\n//bar\r\n{\r\n    \"a\": [\r\n        1,\r\n        2,\r\n        3\r\n    ],\r\n    \"b\": {\r\n    },\r\n    \"Я草\": [\r\n    ],\r\n    \"😀\": [\r\n        [\r\n            100,\r\n            101,\r\n            102,\r\n            103,\r\n            104,\r\n            105,\r\n            106,\r\n            107,\r\n            108,\r\n            109,\r\n            110,\r\n            111,\r\n            112\r\n        ],\r\n        [\r\n            100,\r\n            101,\r\n            102,\r\n            103,\r\n            104,\r\n            105,\r\n            106,\r\n            107,\r\n            108,\r\n            109,\r\n            110,\r\n            111,\r\n            112,\r\n            113\r\n        ],\r\n        [\r\n            100,\r\n            101,\r\n            102,\r\n            103,\r\n            104,\r\n            105,\r\n            106,\r\n            107,\r\n            108,\r\n            109,\r\n            110,\r\n            111,\r\n            112,\r\n            //😀\r\n            [\r\n                113,\r\n                114\r\n            ]\r\n        ]\r\n    ],\r\n    /*cömment*/\r\n    \"d\": [\r\n        {\r\n            \"o\": \"öyster\"\r\n        },\r\n        \"cät\",\r\n        //python \r\n        \"dog\"\r\n    ],\r\n    \"e\": false,\r\n    //cömment\r\n    \"f\": null\r\n}\r\n//baz \r\n// more python\r\n/*quz\r\nzuq*/\r\n";
                string actualPrettyprintWithcommentsObjstr = obj.PrettyPrintWithComments(parser.comments, 4, false, ' ');
                if (correctPrettyprintWithcommentsObjstr != actualPrettyprintWithcommentsObjstr)
                {
                    testsFailed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
    Expected PrettyPrintWithComments(obj) to return
    {1}
    instead got
    {2}",
                        ii + 1, correctPrettyprintWithcommentsObjstr,
                        actualPrettyprintWithcommentsObjstr));
                }
                var keylines = new (string key,
                    int originalPos, int whitesmithPos, int googlePos,
                    int tostringMiniwhitePos, int tostringPos, int pprintPos,
                    int tostringWithcommentsPos, int prettyprintWithCommentsPos)[]
                {
                    ("a", 24, 13, 12, 5, 6, 12, 105, 28),
                    ("b", 42, 57, 67, 17, 22, 33, 121, 83),
                    ("Я草", 58, 82, 91, 28, 35, 51, 134, 107),
                    ("😀", 70, 106, 114, 38, 47, 68, 146, 130),
                    ("d", 313, 818, 993, 220, 272, 525, 371, 1047),
                    ("e", 358, 905, 1096, 255, 312, 570, 411, 1169),
                    ("f", 380, 918, 1113, 265, 324, 587, 423, 1202),
                };
                foreach ((string key, int originalPosition, _, _, _, _, _, _, _) in keylines)
                {
                    ii++;
                    int gotPos = obj[key].position;
                    if (gotPos != originalPosition)
                    {
                        testsFailed++;
                        Npp.AddLine($"After parsing of obj, expected the position of child {key} to be {originalPosition}, got {gotPos}.");
                    }
                }
                foreach (PrettyPrintStyle style in new[] {PrettyPrintStyle.Whitesmith, PrettyPrintStyle.Google, PrettyPrintStyle.PPrint })
                {
                    string pp = obj.PrettyPrint(4, false, style);
                    string ppChLine = obj.PrettyPrintAndChangePositions(4, false, style);
                    if (pp != ppChLine)
                    {
                        testsFailed++;
                        Npp.AddLine(string.Format(@"Test {0} failed:
    Expected {1}-style PrettyPrintAndChangePositions(obj) to return
    {2}
    instead got
    {3}",
                                                        ii + 1, style, pp, ppChLine));
                    }
                    ii++;
                }

                foreach ((string key, _, int whitesmithPos, int googlePos, int tostringMiniwhitePos, int tostringPos, int pprintPos, int tostringWithcommentsPos, int prettyprintWithcommentsPos) in keylines)
                {
                    obj.PrettyPrintAndChangePositions(4, false, PrettyPrintStyle.Whitesmith);
                    int gotPos = obj[key].position;
                    if (gotPos != whitesmithPos)
                    {
                        testsFailed++;
                        Npp.AddLine($"After Whitesmith-style PrettyPrintAndChangePositions(obj), expected the position of child {key} to be {whitesmithPos}, got {gotPos}.");
                    }
                    ii++;
                    obj.PrettyPrintAndChangePositions(4, false, PrettyPrintStyle.Google);
                    gotPos = obj[key].position;
                    if (gotPos != googlePos)
                    {
                        testsFailed++;
                        Npp.AddLine($"After Google-style PrettyPrintAndChangePositions(obj), expected the position of child {key} to be {googlePos}, got {gotPos}.");
                    }
                    ii++;
                    obj.ToStringAndChangePositions(false);
                    gotPos = obj[key].position;
                    if (gotPos != tostringPos)
                    {
                        testsFailed++;
                        Npp.AddLine($"After ToStringAndChangePositions(obj), expected the position of child {key} to be {tostringPos}, got {gotPos}.");
                    }
                    ii++;
                    obj.ToStringAndChangePositions(false, ":", ",");
                    gotPos = obj[key].position;
                    if (gotPos != tostringMiniwhitePos)
                    {
                        testsFailed++;
                        Npp.AddLine($"After minimal-whitespace ToStringAndChangePositions(obj), expected the position of child {key} to be {tostringMiniwhitePos}, got {gotPos}.");
                    }
                    ii++;
                    obj.PrettyPrintAndChangePositions(4, false, PrettyPrintStyle.PPrint);
                    gotPos = obj[key].position;
                    if (gotPos != pprintPos)
                    {
                        testsFailed++;
                        Npp.AddLine($"After PPrint-style PrettyPrintAndChangePositions(obj), expected the position of child {key} to be {pprintPos}, got {gotPos}.");
                    }
                    ii++;
                    obj = (JObject)parser.Parse(objstr); // the WithComments methods only work if the JSON is parsed immediately before calling them
                    obj.ToStringWithCommentsAndChangePositions(parser.comments, false);
                    gotPos = obj[key].position;
                    if (gotPos != tostringWithcommentsPos)
                    {
                        testsFailed++;
                        Npp.AddLine($"After ToStringWithCommentsAndChangePositions(obj), expected the position of child {key} to be {tostringWithcommentsPos}, got {gotPos}.");
                    }
                    ii++;
                    obj = (JObject)parser.Parse(objstr);
                    obj.PrettyPrintWithCommentsAndChangePositions(parser.comments, 4, false, ' ');
                    gotPos = obj[key].position;
                    if (gotPos != prettyprintWithcommentsPos)
                    {
                        testsFailed++;
                        Npp.AddLine($"After PrettyPrintWithCommentsAndChangePositions(obj), expected the position of child {key} to be {prettyprintWithcommentsPos}, got {gotPos}.");
                    }
                    ii++;
                }
                string tostr = obj.ToString(false);
                string tostrChLine = obj.ToStringAndChangePositions(false);
                ii++;
                if (tostr != tostrChLine)
                {
                    testsFailed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
    Expected ToStringAndChangePositions({1}) to return
    {2}
    instead got
    {3}",
                                                    ii + 1, objstr, tostr, tostrChLine));
                }
            }
            // test PrettyPrintWithComments using the PPrint style
            string pprintWithCommentsInitial = "# comment at start\r\n[\r\n    /* multiline comment at start */\r\n    [\"short\", {\"iterables\": \"get\", \"printed\": \"on\", \"one\": \"line\"}],\r\n    {\r\n        \"but\": [\r\n            \"this\",\r\n            /* has a comment in it */\r\n            \"and gets more lines\"\r\n        ]\r\n    },\r\n    [\"array\", \"would be short enough\", /* but has */ 1, \"comment\", true],\r\n    [\r\n        \"and this array is too long\",\r\n        \"so it goes Google-style\",\r\n        \"even though it has\", [0.0, \"comments\"] # comment at end\r\n    ],\r\n    [\"the last thing\", \"in this array\", \"makes it too long\", \"to fit on 1 line\"],\r\n    {\"the last pair\": \"of this object\", \"makes it too long\": \"to fit on 1 line\"}\r\n    /* multiline comment at end */\r\n]";
            JNode parsedPPrintWithComments = TryParse(pprintWithCommentsInitial, parser);
            if (parsedPPrintWithComments == null || !(parsedPPrintWithComments is JArray) || parser.comments.Count != 6)
            {
                Npp.AddLine($"Expected to parse\r\n{parsedPPrintWithComments}\r\nas an array with 6 comments, but parsed {parser.comments.Count} comments and " +  parsedPPrintWithComments == null ? "failed to parse" : $"got type {parsedPPrintWithComments.type}");
                testsFailed += 5;
                ii += 5;
            }
            else
            {
                var pprintWithCommentsTestCases = new (bool sortKeys, bool tabIndent, int indent, string correctOut)[]
                {
                    (false, false, 4, "// comment at start\r\n[\r\n    /* multiline comment at start */\r\n    [\"short\", {\"iterables\": \"get\", \"printed\": \"on\", \"one\": \"line\"}],\r\n    {\r\n        \"but\": [\r\n            \"this\",\r\n            /* has a comment in it */\r\n            \"and gets more lines\"\r\n        ]\r\n    },\r\n    [\r\n        \"array\",\r\n        \"would be short enough\",\r\n        /* but has */\r\n        1,\r\n        \"comment\",\r\n        true\r\n    ],\r\n    [\r\n        \"and this array is too long\",\r\n        \"so it goes Google-style\",\r\n        \"even though it has\",\r\n        [0.0, \"comments\"]\r\n    ],\r\n    // comment at end\r\n    [\r\n        \"the last thing\",\r\n        \"in this array\",\r\n        \"makes it too long\",\r\n        \"to fit on 1 line\"\r\n    ],\r\n    {\r\n        \"the last pair\": \"of this object\",\r\n        \"makes it too long\": \"to fit on 1 line\"\r\n    }\r\n]\r\n/* multiline comment at end */\r\n"
                    ),
                    (true, false, 2, "// comment at start\r\n[\r\n  /* multiline comment at start */\r\n  [\"short\", {\"iterables\": \"get\", \"one\": \"line\", \"printed\": \"on\"}],\r\n  {\r\n    \"but\": [\r\n      \"this\",\r\n      /* has a comment in it */\r\n      \"and gets more lines\"\r\n    ]\r\n  },\r\n  [\r\n    \"array\",\r\n    \"would be short enough\",\r\n    /* but has */\r\n    1,\r\n    \"comment\",\r\n    true\r\n  ],\r\n  [\r\n    \"and this array is too long\",\r\n    \"so it goes Google-style\",\r\n    \"even though it has\",\r\n    [0.0, \"comments\"]\r\n  ],\r\n  // comment at end\r\n  [\"the last thing\", \"in this array\", \"makes it too long\", \"to fit on 1 line\"],\r\n  {\"makes it too long\": \"to fit on 1 line\", \"the last pair\": \"of this object\"}\r\n]\r\n/* multiline comment at end */\r\n"
                    ),
                    (true, true, 4, "// comment at start\r\n[\r\n\t/* multiline comment at start */\r\n\t[\"short\", {\"iterables\": \"get\", \"one\": \"line\", \"printed\": \"on\"}],\r\n\t{\r\n\t\t\"but\": [\r\n\t\t\t\"this\",\r\n\t\t\t/* has a comment in it */\r\n\t\t\t\"and gets more lines\"\r\n\t\t]\r\n\t},\r\n\t[\r\n\t\t\"array\",\r\n\t\t\"would be short enough\",\r\n\t\t/* but has */\r\n\t\t1,\r\n\t\t\"comment\",\r\n\t\ttrue\r\n\t],\r\n\t[\r\n\t\t\"and this array is too long\",\r\n\t\t\"so it goes Google-style\",\r\n\t\t\"even though it has\",\r\n\t\t[0.0, \"comments\"]\r\n\t],\r\n\t// comment at end\r\n\t[\"the last thing\", \"in this array\", \"makes it too long\", \"to fit on 1 line\"],\r\n\t{\"makes it too long\": \"to fit on 1 line\", \"the last pair\": \"of this object\"}\r\n]\r\n/* multiline comment at end */\r\n"
                    ),
                    (false, true, 4, "// comment at start\r\n[\r\n\t/* multiline comment at start */\r\n\t[\"short\", {\"iterables\": \"get\", \"printed\": \"on\", \"one\": \"line\"}],\r\n\t{\r\n\t\t\"but\": [\r\n\t\t\t\"this\",\r\n\t\t\t/* has a comment in it */\r\n\t\t\t\"and gets more lines\"\r\n\t\t]\r\n\t},\r\n\t[\r\n\t\t\"array\",\r\n\t\t\"would be short enough\",\r\n\t\t/* but has */\r\n\t\t1,\r\n\t\t\"comment\",\r\n\t\ttrue\r\n\t],\r\n\t[\r\n\t\t\"and this array is too long\",\r\n\t\t\"so it goes Google-style\",\r\n\t\t\"even though it has\",\r\n\t\t[0.0, \"comments\"]\r\n\t],\r\n\t// comment at end\r\n\t[\"the last thing\", \"in this array\", \"makes it too long\", \"to fit on 1 line\"],\r\n\t{\"the last pair\": \"of this object\", \"makes it too long\": \"to fit on 1 line\"}\r\n]\r\n/* multiline comment at end */\r\n"
                    ),
                    (false, false, 1, "// comment at start\r\n[\r\n /* multiline comment at start */\r\n [\"short\", {\"iterables\": \"get\", \"printed\": \"on\", \"one\": \"line\"}],\r\n {\r\n  \"but\": [\r\n   \"this\",\r\n   /* has a comment in it */\r\n   \"and gets more lines\"\r\n  ]\r\n },\r\n [\r\n  \"array\",\r\n  \"would be short enough\",\r\n  /* but has */\r\n  1,\r\n  \"comment\",\r\n  true\r\n ],\r\n [\r\n  \"and this array is too long\",\r\n  \"so it goes Google-style\",\r\n  \"even though it has\",\r\n  [0.0, \"comments\"]\r\n ],\r\n // comment at end\r\n [\"the last thing\", \"in this array\", \"makes it too long\", \"to fit on 1 line\"],\r\n {\"the last pair\": \"of this object\", \"makes it too long\": \"to fit on 1 line\"}\r\n]\r\n/* multiline comment at end */\r\n"
                    ),
                };
                bool hasShownPPrintInput = false;
                foreach ((bool sortKeys, bool tabIndent, int indent, string correctOut) in pprintWithCommentsTestCases)
                {
                    ii++;
                    try
                    {
                        string gotOut = parsedPPrintWithComments.PrettyPrintWithComments(parser.comments, tabIndent ? 1 : indent, sortKeys, tabIndent ? '\t' : ' ', PrettyPrintStyle.PPrint);
                        if (gotOut != correctOut)
                        {
                            testsFailed++;
                            if (!hasShownPPrintInput)
                            {
                                hasShownPPrintInput = true;
                                Npp.AddLine($"In PPrint-style pretty-printing with comments testcases, input was\r\n{pprintWithCommentsInitial}");
                            }
                            Npp.AddLine($"While trying to PPrint-style pretty print with sortKeys={sortKeys}, tabIndent={tabIndent}, indent={indent}, expected\r\n{correctOut}\r\ngot\r\n{gotOut}");
                        }
                    }
                    catch (Exception ex)
                    {
                        testsFailed++;
                        if (!hasShownPPrintInput)
                        {
                            hasShownPPrintInput = true;
                            Npp.AddLine($"In PPrint-style pretty-printing with comments testcases, input was\r\n{pprintWithCommentsInitial}");
                        }
                        Npp.AddLine($"While trying to PPrint-style pretty print with sortKeys={sortKeys}, tabIndent={tabIndent}, indent={indent}, expected\r\n{correctOut}\r\ngot exception\r\n{ex}");
                    }
                }
            }
            #endregion
            #region EqualityTests
            var equalityTestcases = new object[][]
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
            foreach (object[] test in equalityTestcases)
            {
                string astr = (string)test[0];
                string bstr = (string)test[1];
                bool aEqualsB = (bool)test[2];
                ii++;
                JNode a = parser.Parse(astr);
                JNode b = parser.Parse(bstr);
                bool result = a.TryEquals(b, out _);
                if (result != aEqualsB)
                {
                    testsFailed++;
                    Npp.AddLine($"Expected {a.ToString()} == {b.ToString()} to be {aEqualsB}, but it was called {result}");
                }
            }
            parser.throwIfLogged = oldThrowIfLogged;
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
                testsFailed++;
                Npp.AddLine($"While trying to get the string representation of JRegex array {correctJRegexArrayRepr}\r\ngot error\r\n{ex}");
            }
            if (gotRepr != correctJRegexArrayRepr)
            {
                testsFailed++;
                Npp.AddLine($"JRegex ToString() should return a string that would reproduce the original regex.\r\nExpected\r\n{correctJRegexArrayRepr}\r\nGot\r\n{gotRepr}");
            }
            #endregion

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }

        public static bool TestThrowsWhenAppropriate()
        {
            int ii = 0, testsFailed = 0;
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
                    testsFailed++;
                    Npp.AddLine($"Expected default settings JSON parser to throw an error on input\n{test}\nbut instead returned {json.ToString()}");
                }
                catch { }
            }

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }

        public static bool TestSpecialParserSettings()
        {
            JsonParser simpleparser = new JsonParser();
            JsonParser parser = new JsonParser(LoggerLevel.JSON5, false);
            //TimeSpan offsetFromUtc = DateTime.Now.Subtract(DateTime.UtcNow);
            var testcases = new (string inp, JNode desiredOut)[]
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
                //("\"2022-06-04\"", new JNode(new DateTime(2022, 6, 4), Dtype.DATE, 0)),
                //("\"1956-11-13 11:17:56.123\"", new JNode(new DateTime(1956, 11, 13, 11, 17, 56, 123), Dtype.DATETIME, 0)), // HH:mm:ss.fff datetime
                //("\"1956-11-13T11:17:56.6\"", new JNode(new DateTime(1956, 11, 13, 11, 17, 56, 600), Dtype.DATETIME, 0)), // HH:mm:ss.f datetime
                //("\"1956-11-13 11:17:56\"", new JNode(new DateTime(1956, 11, 13, 11, 17, 56, 0), Dtype.DATETIME, 0)), // HH:mm:ss datetime
                //// datetimes ending with "Z" are UTC by definition
                //("\"2027-05-08 14:02:09.987Z\"", new JNode(new DateTime(2027, 5, 8, 14, 2, 9, 987, DateTimeKind.Utc) + offsetFromUtc, Dtype.DATETIME, 0)), // HH:mm:ss.fffZ datetime
                //("\"2027-06-15 04:41:20.5Z\"", new JNode(new DateTime(2027, 6, 15, 4, 41, 20, 500, DateTimeKind.Utc) + offsetFromUtc, Dtype.DATETIME, 0)), // HH:mm:ss.fZ datetime
                //("\"2048-10-25 16:59:38Z\"", new JNode(new DateTime(2048, 10, 25, 16, 59, 38, 0, DateTimeKind.Utc) + offsetFromUtc, Dtype.DATETIME, 0)), // HH:mm:ssZ datetime
                ("\"1956-13-12\"", new JNode("1956-13-12", Dtype.STR, 0)), // bad date- month too high
                ("\"1956-11-13 25:56:17\"", new JNode("1956-11-13 25:56:17", Dtype.STR, 0)), // bad datetime- hour too high
                ("\"1956-11-13 \"", new JNode("1956-11-13 ", Dtype.STR, 0)), // bad date- has space at end
                ("['abc', 2, '1999-01-03']", // single-quoted strings 
                    new JArray(0, new List<JNode>(new JNode[]{new JNode("abc"), new JNode(2L), new JNode("1999-01-03")}))),
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
                 ("[1,2]#python comment with no trailing EOL", TryParse("[1,2]", simpleparser)),
                 ("[1,\n2]\n#another python comment with no trailing EOL", TryParse("[1,2]", simpleparser)),
                 ("[1,\r\n  2,\r\n  3\r\n]\r\n//javascript comment with no trailing EOL", TryParse("[1,2,3]", simpleparser)),
                 ("[1,\r\n  2,\r\n  3\r\n]\r\n//", TryParse("[1,2,3]", simpleparser)), // empty javascript comment with no trailing EOL
                 ("[1,\r\n  2,\r\n  3\r\n]\r\n#", TryParse("[1,2,3]", simpleparser)), // empty Python comment with no trailing EOL
            };
            int testsFailed = 0;
            int ii = 0;
            foreach ((string inp, JNode desiredOut) in testcases)
            {
                if (desiredOut == null)
                {
                    ii += 1;
                    testsFailed += 1;
                    continue;
                }
                ii++;
                JNode result = new JNode();
                string baseMessage = $"Expected JsonParser(ParserState.JSON5).Parse({inp})\nto return\n{desiredOut.ToString()}\n";
                try
                {
                    result = parser.Parse(inp);
                    if (!desiredOut.TryEquals(result, out _))
                    {
                        testsFailed++;
                        Npp.AddLine($"{baseMessage}Instead returned\n{result.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    testsFailed++;
                    Npp.AddLine($"{baseMessage}Instead threw exception\n{ex}");
                }
            }

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }

        public static bool TestLinter()
        {
            JsonParser simpleparser = new JsonParser();
            JsonParser parser = new JsonParser(LoggerLevel.STRICT, false, false);
            var testcases = new (string inp, string desiredOut, string[] desiredLint)[]
            {
                ("[1, 2]", "[1, 2]", new string[]{ } ), // syntactically valid JSON
                ("[1 2]", "[1, 2]", new string[]{"No comma between array members" }),
                ("[1, , 2]", "[1, 2]", new string[]{$"Two consecutive commas after element 0 of array"}),
                ("[1, 2,]", "[1, 2]", new string[]{"Comma after last element of array"}),
                ("[1 2,]", "[1, 2]", new string[]{"No comma between array members", "Comma after last element of array"}),
                ("{\"a\" 1}", "{\"a\": 1}", new string[]{"No ':' between key 0 and value 0 of object"}),
                ("{\"a\": 1 \"b\": 2}", "{\"a\": 1, \"b\": 2}", new string[]{ "No comma after key-value pair 0 in object" }),
                ("{\"a\": 1: \"b\": 2}", "{\"a\": 1, \"b\": 2}", new string[]{ "No comma after key-value pair 0 in object", "':' found instead of comma after key-value pair" }),
                ("{\"a\": 1, \"b\": \"q\" : //foo\n \"c\": 7}", "{\"a\": 1, \"b\": \"q\", \"c\": 7}", new string[] {
                    "No comma after key-value pair 1 in object",
                    "':' found instead of comma after key-value pair" ,
                    "JavaScript comments are not allowed in the original JSON specification" }),
                ("{\"a\": -1.5 :", "{\"a\": -1.5}", new string[]{
                    "No comma after key-value pair 0 in object",
                    "No valid unquoted key beginning at 11",
                    "At end of valid JSON document, got : instead of EOF" }),
                ("{\"a\":false: /* baz */", "{\"a\": false}", new string[]{
                    "No comma after key-value pair 0 in object",
                    "':' found instead of comma after key-value pair",
                    "JavaScript comments are not allowed in the original JSON specification",
                    "Unterminated object"
                }),
                ("{\"a\":true\n:\r\n\t ", "{\"a\": true}", new string[]{
                    "No comma after key-value pair 0 in object",
                    "':' found instead of comma after key-value pair",
                    "Unterminated object"
                }),
                ("[1  \"a\n\"]", "[1, \"a\\n\"]", new string[]{"No comma between array members", "String literal contains newline"}),
                ("[NaN, -Infinity, Infinity]", "[NaN, -Infinity, Infinity]",
                    new string[]{ "NaN is not allowed in the original JSON specification",
                                  "Infinity is not allowed in the original JSON specification",
                                  "Infinity is not allowed in the original JSON specification" }),
                ("{'a\n':[1,2,},]", "{\"a\\n\": [1,2]}", new string[]{"Singlequoted strings are only allowed in JSON5", "Object key contains newline", "Expected ']' at the end of an array, but found '}'", "Comma after last element of array", "Expected '}' at the end of an object, but found ']'", "Comma after last key-value pair of object" }),
                ("[1, 2", "[1, 2]", new string[]{ "Unterminated array" }),
                ("{\"a\": 1", "{\"a\": 1}", new string[]{ "Unterminated object" }),
                ("{\"a\": [1, {\"b\": 2", "{\"a\": [1, {\"b\": 2}]}", new string[] { "Unterminated object",
                                                                                                "Unterminated array",
                                                                                                "Unterminated object" }),
                ("{", "{}", new string[] { "Unterminated object" }),
                ("[", "[]", new string[] { "Unterminated array" }),
                ("[+1.5, +2e3, +Infinity, +7.5/-3]", "[1.5, 2000.0, Infinity, -2.5]", new string[]{ "Leading + signs in numbers are only allowed in JSON5", "Leading + signs in numbers are only allowed in JSON5", "Leading + signs in numbers are only allowed in JSON5", "Infinity is not allowed in the original JSON specification", "Leading + signs in numbers are only allowed in JSON5", "Fractions of the form 1/3 are not allowed in any JSON specification" }),
                ("[1] // comment", "[1]", new string[] { "JavaScript comments are not allowed in the original JSON specification" }),
                ("{\"a\": 1,,\"b\": 2}", "{\"a\": 1, \"b\": 2}", new string[] { "Two consecutive commas after key-value pair 0 of object" }),
                ("[1]\r\n# Python comment", "[1]", new string[] { "Python-style '#' comments are not allowed in any well-accepted JSON specification" }),
                ("[1] /* unterminated multiline", "[1]", new string[] { "JavaScript comments are not allowed in the original JSON specification", "Unterminated multi-line comment" }),
                ("\"\\u043\"", "\"\"", new string[] { "Could not find valid hexadecimal of length 4" }),
                ("'abc'", "\"abc\"", new string[] { "Singlequoted strings are only allowed in JSON5" }),
                ("  \"a\n\"", "\"a\\n\"", new string[] { "String literal contains newline" }),
                ("[.75, 0xabcdef, +9, -0xABCDEF, +0x0123456789]", "[0.75, 11259375, 9, -11259375, 4886718345]", new string[]
                {
                    "Numbers with a leading decimal point are only allowed in JSON5", "Hexadecimal numbers are only allowed in JSON5", "Leading + signs in numbers are only allowed in JSON5", "Hexadecimal numbers are only allowed in JSON5", "Leading + signs in numbers are only allowed in JSON5", "Hexadecimal numbers are only allowed in JSON5",
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
                ("Hugre", "null", new string[]{"Badly located character \"H\""}),
                ("[undefined, underpants]", "[null, null]",
                new string[]{
                    "undefined is not allowed in any JSON specification",
                    "Expected literal starting with 'u' to be undefined"
                }),
                ("[nan, inf, -inf]", "[NaN, Infinity, -Infinity]",
                new string[]
                {
                    "nan is not a valid representation of Not a Number in JSON",
                    "inf is not the correct representation of Infinity in JSON",
                    "inf is not the correct representation of Infinity in JSON"
                }),
                ("\"\\i\"", "\"i\"", new string[]{"Escaped char 'i' is only allowed in JSON5"}),
                ("", "null", new string[]{"No input"}),
                ("\t\r\n  // comments\r\n/* */ ", "null", new string[]{ "JavaScript comments are not allowed in the original JSON specification", "JavaScript comments are not allowed in the original JSON specification","Input is only whitespace and maybe comments" }),
                ("[5/ ]", "[5]", new string[]{ "JavaScript comments are not allowed in the original JSON specification", "Expected JavaScript comment after '/'" }),
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
                ("{foo: 1, $baz: 2, 草: 2, _quЯ: 3, \\ud83d\\ude00_$\\u1ed3: 4, a\\uff6acf: 5, \\u0008\\u000a: 6, f\\u0000o: 1}",
                 "{\"foo\": 1, \"$baz\": 2, \"草\": 2, \"_quЯ\": 3, \"😀_$ồ\": 4, \"aｪcf\": 5, \"\\b\\n\": 6}",
                 new string[]{
                    "Unquoted keys are only allowed in JSON5",
                    "Unquoted keys are only allowed in JSON5",
                    "Unquoted keys are only allowed in JSON5",
                    "Unquoted keys are only allowed in JSON5",
                    "Unquoted keys are only allowed in JSON5",
                    "Unquoted keys are only allowed in JSON5",
                    "Unquoted keys are only allowed in JSON5",
                    "Control characters (ASCII code less than 0x20) are disallowed inside strings under the strict JSON specification",
                    "String literal contains newline", // the \u000a in \\b\\u000a is secretly a newline
                    "Unquoted keys are only allowed in JSON5",
                    "'\\x00' is the null character, which is illegal in JsonTools"
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
                    "Escaped char '1' is only allowed in JSON5",
                    "Escaped char 'A' is only allowed in JSON5",
                    "Escaped char '7' is only allowed in JSON5",
                    "Escaped char 'B' is only allowed in JSON5",
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
                    "True is not allowed in any JSON specification",
                    "False is not allowed in any JSON specification",
                    "None is not allowed in any JSON specification"
                }),
                ("0xFFFFFFFFFFFFFFFFFFFFFFFFFFF", "null", new string[]{ "Hexadecimal numbers are only allowed in JSON5", "Hex number too large for a 64-bit signed integer type"}),
                ("-0xFFFFFFFFFFFFFFFFFFFFFFFFFFF", "null", new string[]{ "Hexadecimal numbers are only allowed in JSON5", "Hex number too large for a 64-bit signed integer type"}),
                ("{\"a\": [[1, 2], [3, 4, \"b\": [5 , \"c\": [, \"d\": 6}", "{\"a\": [[1, 2], [3, 4]], \"b\": [5], \"c\": [], \"d\": 6}",
                    new string[]{ "':' (key-value separator) where ',' between array members expected. Maybe you forgot to close the array?",
                                  "No comma between array members",
                                  "':' (key-value separator) where ',' between array members expected. Maybe you forgot to close the array?",
                                  "No comma after key-value pair 0 in object",
                                  "':' (key-value separator) where ',' between array members expected. Maybe you forgot to close the array?",
                                  "No comma after key-value pair 1 in object",
                                  "Comma before first value in array",
                                  "':' (key-value separator) where ',' between array members expected. Maybe you forgot to close the array?",
                                  "No comma after key-value pair 2 in object"}
                ),
                (   "[{\"a\": 0, null" + // seeing the 2 where a key was expected triggers falling out of the object
                    ", {2" + // we fall out of the empty object upon seeing this invalid key
                    ", {,3" + // we fall out of the empty object upon seeing the comma
                    ", {\"b\": [4, 5," +
                    "       {\"c\": 6, \"d\" ]}" + // seeing this ']' instead of a colon triggers falling out of the object, and reinterpreting the "d" as a member of the parent array
                    ", {\"e\": {" +
                    "       \"f\": [null, \"g\": {" + // seeing the colon after "g" triggers falling out of the array, and reinterpreting the "g" as a key of the parent object
                    "           \"h\": 7, \"i\"" + // seeing this comma instead of a colon triggers falling out of the object (and its parent object, and its grandparent object), and finally reinterpreting the "i" as a child of the great-grandparent array
                    ", \"j\"]",
                    "[{\"a\": 0}, null, {}, 2, {}, 3, {\"b\": [4, 5, {\"c\": 6}, \"d\"]}, {\"e\": {\"f\": [null], \"g\": {\"h\": 7}}}, \"i\", \"j\"]",
                    new string[]{
                            "Unquoted keys are only allowed in JSON5",
                            "Found ',' after key 1 when colon expected",
                            "No comma between array members",
                            "No valid unquoted key beginning at 17",
                            "No comma between array members",
                            "Comma before first value in object",
                            "No valid unquoted key beginning at 22",
                            "No comma between array members",
                            "Found ']' after key 1 when colon expected",
                            "No comma between array members",
                            "':' (key-value separator) where ',' between array members expected. Maybe you forgot to close the array?",
                            "No comma after key-value pair 0 in object",
                            "Found ',' after key 1 when colon expected",
                            "No comma after key-value pair 1 in object",
                            "Found ',' after key 2 when colon expected",
                            "No comma after key-value pair 0 in object",
                            "Found ',' after key 1 when colon expected",
                            "No comma between array members"
                    }
                ),
                (
                    "[ \r\n" +
                    "    {\"foo\": 1, \"bar\": [\"a\", \"b\"},\r\n" +  // missing close ']' of array
                    "    {\"foo\": 2, \"bar\": [\"c\", \"d\"]},\r\n" + // at the start of this object, the parser thinks it's parsing the last array
                                                                       // however, when it tries to parse the '{' opening this object as a key, it fails, and falls back into the parent array
                                                                       // once the parser has fallen through to the parent array, everything is fine.
                    "    {\"foo\": 3, \"bar\": [\"e\", \"f\"]}\r\n" +  // also fine
                    "]",
                    "[{\"foo\": 1, \"bar\": [\"a\", \"b\"]}, {\"foo\": 2, \"bar\": [\"c\", \"d\"]}, {\"foo\": 3, \"bar\": [\"e\", \"f\"]}]",
                    new string[]
                    {
                            "Expected ']' at the end of an array, but found '}'",
                            "No valid unquoted key beginning at 43",
                            "No comma between array members"
                    }
                ),
                ("-[]", "NaN", new string[]{"Number string \"-\" had bad format", "At end of valid JSON document, got [ instead of EOF"}),
                (" +\r\nfalse", "NaN", new string[]{"Leading + signs in numbers are only allowed in JSON5", "Number string \"+\" had bad format", "At end of valid JSON document, got f instead of EOF"}),
                (" +", "null", new string[]{"Leading + signs in numbers are only allowed in JSON5", "'+' sign at end of document"}),
                ("-", "null", new string[]{"'-' sign at end of document"}),
                ("[5e, -0.2e]", "[NaN, NaN]", new string[]{"Number string \"5e\" had bad format", "Number string \"-0.2e\" had bad format"}),
                ("[-5., 03, 2.e2, +05.0, -00]", "[-5.0, 3, 200.0, 5.0, 0]", new string[]
                {
                    "Numbers with a trailing decimal point are only allowed in JSON5",
                    "Numbers with an unnecessary leading 0 (like \"01\") are not allowed in any JSON specification",
                    "Numbers with a trailing decimal point are only allowed in JSON5",
                    "Leading + signs in numbers are only allowed in JSON5",
                    "Numbers with an unnecessary leading 0 (like \"01\") are not allowed in any JSON specification",
                    "Numbers with an unnecessary leading 0 (like \"01\") are not allowed in any JSON specification",
                }),
            };

            int testsFailed = 0;
            int ii = 0;
            foreach ((string inp, string desiredOut, string[] expectedLint) in testcases)
            {
                ii++;
                JNode jdesired = TryParse(desiredOut, simpleparser);
                if (jdesired == null)
                {
                    ii += 1;
                    testsFailed += 1;
                    continue;
                }
                JNode result = new JNode();
                string expectedLintStr = "[" + string.Join(", ", expectedLint.Select(x => JNode.StrToString(x, true))) + "]";
                string baseMessage = $"Expected JsonParser(LoggerLevel.STRICT).Parse({inp})\nto return\n{desiredOut} and have lint {expectedLintStr}\n";
                try
                {
                    result = parser.Parse(inp);
                    if (parser.lint.Count == 0 && expectedLint.Length != 0)
                    {
                        testsFailed++;
                        Npp.AddLine(baseMessage + "Parser had no lint");
                        continue;
                    }
                    StringBuilder lintSb = new StringBuilder();
                    lintSb.Append('[');
                    for (int jj = 0; jj < parser.lint.Count; jj++)
                    {
                        lintSb.Append(JNode.StrToString(parser.lint[jj].message, true));
                        if (jj < parser.lint.Count - 1) lintSb.Append(", ");
                    }
                    lintSb.Append("]");
                    string lintStr = lintSb.ToString();
                    if (!jdesired.TryEquals(result, out _) || lintStr != expectedLintStr)
                    {
                        testsFailed++;
                        Npp.AddLine($"{baseMessage}Instead returned\n{result.ToString()} and had lint {lintStr}");
                    }
                }
                catch (Exception ex)
                {
                    testsFailed++;
                    Npp.AddLine($"{baseMessage}Instead threw exception\n{ex}");
                }
            }

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
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
            int testsFailed = 0;
            int ii = 0;
            foreach (string[] test in testcases)
            {
                string input = test[0];
                JNode desiredOutput = TryParse(test[1], parser);
                JNode json = TryParse(input, parser, true);
                if (json == null)
                {
                    ii++;
                    testsFailed++;
                    continue;
                }
                if (!json.TryEquals(desiredOutput, out _))
                {
                    testsFailed++;
                    Npp.AddLine(string.Format(@"Test {0} failed:
Expected
{1}
Got
{2} ",
                                     ii + 1, test[1], json.ToString()));
                }
            }

            string[] shouldThrowTestcases = new string[]
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

            foreach (string test in shouldThrowTestcases)
            {
                ii++;
                try
                {
                    JNode json = parser.ParseJsonLines(test);
                    testsFailed++;
                    Npp.AddLine($"Expected JSON Lines parser to throw an error on input\n{test}\nbut instead returned {json.ToString()}");
                }
                catch { }
            }

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }

        public static bool TestCultureIssues()
        {
            int ii = 0;
            int testsFailed = 0;
            // change current culture to German because they use a comma decimal sep (see https://github.com/molsonkiko/JsonToolsNppPlugin/issues/17)
            ii++;
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-de", true);
            JNode jsonFloat2 = new JsonParser().Parse("2.0");
            string jsonFloat2str = jsonFloat2.ToString();
            if (jsonFloat2str != "2.0")
            {
                testsFailed++;
                Npp.AddLine("Expected parsing of 2.0 to return 2.0 even when the current culture is German, " +
                    $"but instead got {jsonFloat2str} when the culture is German");
            }
            CultureInfo.CurrentCulture = currentCulture;
            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }

        public static bool TestTryParseNumber()
        {
            var testcases = new (string inp, int start, int end, string correctJNodeToStr)[]
            {
                ("foo,12,bar", 4, 6, "12"),
                ("foo,12,bar", 4, 7, "\"12,\""),
                ("blah,-8.", 5, 10, "-8.0"),
                ("blah,+5.", 5, 8, "5.0"),
                ("blah,+48.", 5, 9, "48.0"),
                ("blah,-48.", 5, 9, "-48.0"),
                ("blah,-8.", 4, 8, "\",-8.\""),
                (".5,boo", 0, 2, "0.5"),
                ("df,.75,boo", 3, 6, "0.75"),
                ("df,-.75,boo", 3, 7, "-0.75"),
                ("df,+.25,boo", 3, 7, "0.25"),
                ("df,+a,boo", 3, 5, "\"+a\""),
                ("df,-foo,boo", 3, 7, "\"-foo\""),
                (".5,boo", 0, 3, "\".5,\""),
                ("1,15.5e3E7", 2, 8, "15500.0"),
                ("1,15.5e3E70", 2, 10, "\"15.5e3E7\""),
                ("1,2.8e-7,7", 2, 8, "2.8E-07"),
                (";17.4e+11,7", 1, 9, "1740000000000.0"),
                ("1,15.5e3e7", 2, 8, "15500.0"),
                ("1,15.5e3e70", 2, 10, "\"15.5e3e7\""),
                ("1,2.8E-7,7", 2, 8, "2.8E-07"),
                (";17.4E+11,7", 1, 9, "1740000000000.0"),
                ("1,15.5Eb,ekr", 2, 8, "\"15.5Eb\""),
                ("a,0x123456789abc,3", 2, 16, "20015998343868"),
                ("a,0xABCDEFabcdef123,3", 2, 19, "773738404492800291"),
                ("a,-0xABCDEFabcdef123,3", 2, 20, "-773738404492800291"),
                ("a,+0xABCDEFabcdef123,3", 2, 20, "773738404492800291"),
                ("a,+0xABCDEFabcdef123,3", 2, 21, "\"+0xABCDEFabcdef123,\""),
                ("a,-0xABCDEFabcdef123,3", 2, 21, "\"-0xABCDEFabcdef123,\""),
                ("a,NaN,b", 2, 5, "NaN"),
                ("a,-NaN,b", 2, 6, "NaN"),
                ("a,+NaN,b", 2, 6, "NaN"),
                ("a,NaNa,b", 2, 6, "\"NaNa\""),
                ("ab,Infinity,b", 3, 11, "Infinity"),
                ("ab,Infinity,b", 3, 12, "\"Infinity,\""),
                ("ab,+Infinity,b", 3, 12, "Infinity"),
                ("ab,-Infinity,b", 3, 12, "-Infinity"),
                ("ab,+Infinity,b", 3, 13, "\"+Infinity,\""),
                ("ab,-Infinity,b", 3, 13, "\"-Infinity,\""),
            };
            int testsFailed = 0;
            foreach ((string inp, int start, int end, string correctJNodeStr) in testcases)
            {
                JNode tryParseResult = JsonParser.TryParseNumber(inp, start, end, 0);
                string tryParseStr = tryParseResult.ToString();
                if (tryParseStr != correctJNodeStr)
                {
                    testsFailed++;
                    Npp.AddLine($"Expected TryParseNumber(\"{inp}\", {start}, {end}) to return {correctJNodeStr}, got {tryParseStr}");
                }
            }
            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {testcases.Length - testsFailed} tests.");
            return testsFailed > 0;
        }
    }
}
