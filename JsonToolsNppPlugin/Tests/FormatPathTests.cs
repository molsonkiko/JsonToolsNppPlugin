using System;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class FormatPathTester
    {
        public static bool Test()
        {
            JsonParser parser = new JsonParser();
            JNode json = parser.Parse("{\"a\":1,\"b \":\"f\",\"cu\":[false,true,[{\"\":{\"bufrear\":null}}]],\"d'\":-1.5,\"b\\\\e\\\"\\r\\n\\t`\":[NaN,Infinity,-Infinity], \"e\\\"'\": \"bar\"}");
            var testcases = new (int pos, KeyStyle style, string correctPath)[]
            {
                (6, KeyStyle.JavaScript, ".a"),
                (6, KeyStyle.RemesPath, ".a"),
                (6, KeyStyle.Python, "['a']"),
                (14, KeyStyle.JavaScript, "['b ']"),
                (14, KeyStyle.RemesPath, "[`b `]"),
                (14, KeyStyle.Python, "['b ']"),
                (21, KeyStyle.JavaScript, ".cu"),
                (21, KeyStyle.RemesPath, ".cu"),
                (21, KeyStyle.Python, "['cu']"),
                (31, KeyStyle.JavaScript, ".cu[1]"),
                (31, KeyStyle.RemesPath, ".cu[1]"),
                (31, KeyStyle.Python, "['cu'][1]"),
                (34, KeyStyle.JavaScript, ".cu[2][0]"),
                (34, KeyStyle.RemesPath, ".cu[2][0]"),
                (34, KeyStyle.Python, "['cu'][2][0]"),
                (52, KeyStyle.JavaScript, ".cu[2][0][''].bufrear"),
                (52, KeyStyle.RemesPath, ".cu[2][0][``].bufrear"),
                (52, KeyStyle.Python, "['cu'][2][0]['']['bufrear']"),
                (66, KeyStyle.JavaScript, "[\"d'\"]"),
                (66, KeyStyle.RemesPath, "[`d'`]"),
                (66, KeyStyle.Python, "[\"d'\"]"),
                (93, KeyStyle.JavaScript, "['b\\\\e\"\\r\\n\\t`'][1]"),
                (93, KeyStyle.RemesPath, "[`b\\\\e\"\\r\\n\\t\\``][1]"),
                (93, KeyStyle.Python, "['b\\\\e\"\\r\\n\\t`'][1]"),
                (121, KeyStyle.JavaScript, "[\"e\\\"'\"]"),
                (121, KeyStyle.RemesPath,  "[`e\"'`]"),
                (121, KeyStyle.Python,     "[\"e\\\"'\"]"),
            };
            int ii = 0;
            int testsFailed = 0;
            foreach ((int pos, KeyStyle style, string correctPath) in testcases)
            {
                ii++;
                string path;
                try
                {
                    path = json.PathToPosition(pos, style);
                }
                catch (Exception ex)
                {
                    testsFailed++;
                    Npp.AddLine($"While trying to get the path to position {pos} ({style} style), threw exception\r\n{ex}");
                    continue;
                }
                if (path != correctPath)
                {
                    testsFailed++;
                    Npp.AddLine($"Got the path to position {pos} ({style} style) as {path}, but it should be {correctPath}");
                }
            }

            string separatorTestJsonStr = "{\"foo\": [12, {\"ba/r\": \"a\", \"baz\\\"\": null, \"12\": true}], \"baz\": {\"01\": false, \"a b\": null}, \"_f\": [-10,0.25]}";
            JNode separatorTestJson = parser.Parse(separatorTestJsonStr);

            var separatorTestcases = new (int pos, char separator, string correctPath)[]
            {
                (8, '/', "/foo"),
                (8, ' ', " foo"),
                (10, '/', "/foo/0"),
                (23, '\\', "\\foo\\1\\\"ba/r\""),
                (24, '\\', "\\foo\\1\\\"ba/r\""),
                (39, '$', "$foo$1$\"baz\\\"\""),
                (49, 'f', "f\"foo\"f1f\"12\""),
                (75, 'z', "z\"baz\"z\"01\""),
                (75, '/', "/baz/\"01\""),
                (84, '/', "/baz/\"a b\""),
                (88, ' ', " baz \"a b\""),
                (100, '/', "/_f/0"),
                (104, '_', "_\"_f\"_1"),
            };
            
            foreach ((int pos, char separator, string correctPath) in separatorTestcases)
            {
                ii++;
                string path;
                try
                {
                    path = separatorTestJson.PathToPosition(pos, KeyStyle.JavaScript, separator);
                }
                catch (Exception ex)
                {
                    testsFailed++;
                    Npp.AddLine($"While trying to get the path to position {pos} (Separator style, separator \"{separator}\"), threw exception\r\n{ex}");
                    continue;
                }
                if (path != correctPath)
                {
                    testsFailed++;
                    Npp.AddLine($"Got the path to position {pos} (Separator style, separator \"{separator}\") as {path}, but it should be {correctPath}");
                }
            }

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }
    }
}
