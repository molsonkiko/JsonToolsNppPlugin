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
            var exampleIniFname = @"plugins\JsonTools\testfiles\small\example_ini.ini";
            var exampleIniReformattedFname = @"plugins\JsonTools\testfiles\small\example_ini_reformatted.ini";
            var exampleJsonFname = @"plugins\JsonTools\testfiles\small\example_ini.json";
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

            var testcases = new (string iniText, string correctJsonStrWithoutInterpolation, /*bool interpolation,*/ string correctReformattedIniText)[]
            {
                (exampleIniText, exampleJsonText, /*false,*/ exampleIniReformattedText),
                //(exampleIniText, exampleJsonText, true,  exampleIniReformattedText),
            };
            var jsonParser = new JsonParser(LoggerLevel.JSON5, false, true, true, true);
            int testsPerLoop = 3;
            foreach ((string iniText, string correctJsonStrWithoutInterpolation, /*bool interpolation,*/ string correctReformattedIniText) in testcases)
            {
                JObject correctJson;
                //JObject correctJsonWithInterpolation;
                string correctJsonStr;
                ii += testsPerLoop;
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
                    correctJsonStr = correctJson.PrettyPrintWithComments(jsonParser.comments, sort_keys:false);
                }
                catch (Exception ex)
                {
                    showFileDescription();
                    Npp.AddLine($"While trying to parse JSON\r\n{correctJsonStrWithoutInterpolation}\r\nGOT EXCEPTION\r\n{ex}");
                    testsFailed += testsPerLoop;
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
                    testsFailed += testsPerLoop;
                    continue;
                }
                string gotJsonStr = gotJson.ToString();
                if (gotJson.TryEquals(correctJson, out _))
                {
                    // test if comments are parsed correctly
                    string gotJsonWithComments = "";
                    try
                    {
                        gotJsonWithComments = gotJson.PrettyPrintWithComments(iniParser.comments, sort_keys:false);
                        if (gotJsonWithComments != correctJsonStr)
                        {
                            testsFailed++;
                            showFileDescription();
                            Npp.AddLine($"EXPECTED JSON WITH COMMENTS\r\n{correctJsonStr}\r\nGOT JSON WITH COMMENTS\r\n{gotJsonWithComments}");
                        }
                    }
                    catch (Exception ex)
                    {
                        testsFailed += 2;
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
                    if (!jsonFromDumpedIniText.TryEquals(correctJson, out _))
                    {
                        showFileDescription();
                        testsFailed++;
                        Npp.AddLine($"EXPECTED JSON FROM DUMPED INI FILE\r\n{correctJson.ToString()}\r\nGOT JSON\r\n{gotJson.ToString()}");
                    }
                }
                else
                {
                    testsFailed += testsPerLoop;
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
