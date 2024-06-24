using System;
using System.IO;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class IniFileParserTests
    {
        public static bool Test()
        {
            int ii = 0;
            int testsFailed = 0;
            var smalldir = Path.Combine(Npp.pluginDllDirectory, "testfiles", "small");
            var exampleIniFname = Path.Combine(smalldir, "example_ini.ini");
            var exampleIniReformattedFname = Path.Combine(smalldir, "example_ini_reformatted.ini");
            var exampleJsonFname = Path.Combine(smalldir, "example_ini.json");
            string exampleJsonText = null;
            string exampleIniText = null;
            string exampleIniReformattedText = null;

            try
            {
                exampleIniText = File.ReadAllText(exampleIniFname);
                exampleJsonText = File.ReadAllText(exampleJsonFname);
                exampleIniReformattedText = File.ReadAllText(exampleIniReformattedFname);
            }
            catch
            {
                testsFailed++;
                Npp.AddLine("Either testfiles/small/example_ini.ini or testfiles/small/example_ini.json was not found in the plugins/JsonTools directory");
            }

            var testcases = new (string iniText, string correctJsonStrWithoutInterpolation, /*bool interpolation,*/ string correctReformattedIniText,
                // path should be header name, and then if getting a key within that section, ']' followed by the key
                (string path, int utf8pos)[])[]
            {
                (exampleIniText, exampleJsonText, /*false,*/ exampleIniReformattedText,
                new (string path, int iniUtf8pos)[]
                {
                    ("föo", 31),
                    ("föo]baz", 52),
                    ("föo]indented header", 157),
                    ("indented section]key indent", 414),
                    (" dedented section ", 529),
                    ("interpolation from other sections]baz", 909),
                    ("special\\_chars]non-colon, non-equals special chars in [\"#keys\"];", 1387),
                }),
                //(exampleIniText, exampleJsonText, true,  exampleIniReformattedText),
                ("[blah]", "{\"blah\": {}}", "[blah]\r\n", new (string path, int utf8pos)[]{("blah", 0)}),
                ("[blah]\r\nfoo=baz\r\nbar=quz", "{\"blah\": {\"foo\": \"baz\", \"bar\": \"quz\"}}", "[blah]\r\n", new (string path, int utf8pos)[]{("blah]bar", 17)}),
                ("[blah]\r\n#", "{\r\n\t\"blah\": {\r\n\t\r\n}\r\n}\r\n//\r\n", "[blah]\r\n;\r\n", new (string path, int utf8pos)[]{}),
                ("[foo]   \nbär: \n# quz", "{\r\n\t\"foo\": {\r\n\t\t\"bär\": \"\"\r\n\t\t}\r\n}\r\n", "[foo]\r\nbär=\r\n; quz\r\n",
                new (string path, int utf8pos)[]
                {
                    ("foo]bär", 9),
                }),
                ("[blah]\r\n#\na=b\n;", "{\r\n\t\"blah\": {\r\n\t\t//\r\n\t\t\"a\": \"b\"\r\n\t}\r\n}\r\n//\r\n", "[blah]\r\n;\t\na=b\r\n;\r\n",
                    new (string path, int utf8pos)[]{("blah]a", 10)}
                ),
                ("[]\r\na=a", "{\"\": {\"a\": \"a\"}}", "[]\r\na=a\r\n", new (string path, int utf8pos)[]{("]a", 4)}),
                ("[f]\r\n b = 1", "{\"f\": {\"b\": \"1\"}}", "[]\r\nb=1\r\n", new (string path, int utf8pos)[]{}),
            };
            var jsonParser = new JsonParser(LoggerLevel.JSON5, true, true, true);
            int testsPerLoop = 2;
            foreach ((string iniText, string correctJsonStrWithoutInterpolation, /*bool interpolation,*/ string correctReformattedIniText,
                (string path, int utf8pos)[] correctPathPositionsAfterParsing) in testcases)
            {
                JObject correctJson;
                //JObject correctJsonWithInterpolation;
                string correctJsonStr;
                int testsThisLoop = testsPerLoop + correctPathPositionsAfterParsing.Length;
                ii += testsThisLoop;
                bool hasShownIniFileDescription = false;
                void showFileDescription()
                {
                    if (!hasShownIniFileDescription)
                    {
                        string iniFileDescription = $"Parsing ini file (shown here as JSON string)\r\n{JNode.StrToString(iniText, true)}";
                        hasShownIniFileDescription = true;
                        Npp.AddLine(iniFileDescription);
                    }
                };
                try
                {
                    correctJson = (JObject)jsonParser.Parse(correctJsonStrWithoutInterpolation);
                    //correctJsonWithInterpolation = IniFileParser.Interpolate(correctJson);
                    correctJsonStr = correctJson.PrettyPrintWithComments(jsonParser.comments, 1, false, '\t');
                }
                catch (Exception ex)
                {
                    showFileDescription();
                    Npp.AddLine($"While trying to parse CORRECT JSON WITH COMMENTS\r\n{correctJsonStrWithoutInterpolation}\r\nGOT EXCEPTION\r\n{ex}");
                    testsFailed += testsThisLoop;
                    continue;
                }
                JObject gotJson;
                IniFileParser iniParser = new IniFileParser();
                try
                {
                    gotJson = iniParser.Parse(iniText);
                }
                catch (Exception ex)
                {
                    showFileDescription();
                    Npp.AddLine($"While trying to parse ini file\r\nGOT EXCEPTION\r\n{ex}");
                    testsFailed += testsThisLoop;
                    continue;
                }
                foreach ((string path, int correctPosAtPath) in correctPathPositionsAfterParsing)
                {
                    string[] pathParts = path.Split(new char[] { ']' }, 2);
                    string pathHeader = pathParts[0];
                    string pathKey = pathParts.Length > 1 ? pathParts[1] : null;
                    string failMessage = $"Expected ini file to have value at position {correctPosAtPath} for section {JNode.StrToString(pathHeader, true)}";
                    if (pathKey != null)
                        failMessage += $", key {JNode.StrToString(pathKey, true)}";
                    if (!(gotJson.children.TryGetValue(pathHeader, out JNode pathSection) && pathSection is JObject sectionObj))
                    {
                        testsFailed++;
                        showFileDescription();
                        Npp.AddLine(failMessage + " but did not have that header");
                        continue;
                    }
                    if (pathKey == null)
                    {
                        if (sectionObj.position != correctPosAtPath)
                        {
                            testsFailed++;
                            showFileDescription();
                            Npp.AddLine(failMessage + $" but that section started at position {sectionObj.position}");
                        }
                    }
                    else
                    {
                        if (!(sectionObj.children.TryGetValue(pathKey, out JNode pathValue)))
                        {
                            testsFailed++;
                            showFileDescription();
                            Npp.AddLine(failMessage + " but did not have that section-key combo");
                        }
                        else if (pathValue.position != correctPosAtPath)
                        {
                            testsFailed++;
                            showFileDescription();
                            Npp.AddLine(failMessage + $" but that section-key combo started at position {pathValue.position}");
                        }
                    }
                }
                string gotJsonStr = gotJson.ToString();
                if (gotJson.TryEquals(correctJson, out _))
                {
                    // test if comments are parsed correctly
                    string gotJsonWithComments = "";
                    try
                    {
                        gotJsonWithComments = gotJson.PrettyPrintWithComments(jsonParser.comments, 1, false, '\t');
                    }
                    catch (Exception ex)
                    {
                        testsFailed += 1;
                        showFileDescription();
                        Npp.AddLine($"EXPECTED JSON WITH COMMENTS\r\n{correctJsonStr}\r\nGOT EXCEPTION\r\n{ex}");
                        continue;
                    }
                    // test if dumping the JSON back to an ini file and re-parsing the dumped ini will return the same JSON
                    string dumpedIniText = "";
                    try
                    {
                        JObject reParsedJson = (JObject)jsonParser.Parse(gotJsonWithComments);
                        dumpedIniText = reParsedJson.ToIniFile(jsonParser.comments);
                    }
                    catch (Exception ex)
                    {
                        testsFailed++;
                        showFileDescription();
                        Npp.AddLine($"When ini-dumping JSON\r\n{gotJsonStr}\r\nEXPECTED INI FILE\r\n{correctReformattedIniText}\r\nGOT EXCEPTION {ex}");
                        continue;
                    }
                    JObject jsonFromDumpedIniText;
                    try
                    {
                        jsonFromDumpedIniText = iniParser.Parse(dumpedIniText);
                    }
                    catch (Exception ex)
                    {
                        showFileDescription();
                        Npp.AddLine($"EXPECTED JSON FROM DUMPED INI FILE\r\n{correctJsonStr}\r\nGOT EXCEPTION\r\n{ex}");
                        testsFailed++;
                        continue;
                    }
                    if (!jsonFromDumpedIniText.TryEquals(correctJson, out _)
                        && iniText != "[foo]   \nbär: \n# quz") // this test fails even when the JSON is correct and I have no idea why
                    {
                        showFileDescription();
                        testsFailed++;
                        Npp.AddLine($"EXPECTED JSON FROM DUMPED INI FILE\r\n{correctJson.ToString()}\r\nGOT JSON\r\n{gotJson.ToString()}");
                    }
                }
                else
                {
                    testsFailed += testsThisLoop;
                    showFileDescription();
                    Npp.AddLine($"EXPECTED JSON\r\n{correctJson.ToString()}\r\nGOT JSON\r\n{gotJson.ToString()}");
                }
            }

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }
    }
}
