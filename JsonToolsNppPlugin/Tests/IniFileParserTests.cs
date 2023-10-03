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
            var exampleJsonFname = @"plugins\JsonTools\testfiles\small\example_ini.json";
            string exampleJsonText = null;
            string exampleIniText = null;

            try
            {
                exampleIniText = File.ReadAllText(exampleIniFname);
                exampleJsonText = File.ReadAllText(exampleJsonFname);
            }
            catch
            {
                testsFailed++;
                Npp.AddLine("Either testfiles/small/example_ini.ini or testfiles/small/example_ini.json was not found in the plugins/JsonTools directory");
            }

            var testcases = new (string iniText, string correctJsonStrWithoutInterpolation, bool interpolation)[]
            {
                (exampleIniText, exampleJsonText, false),
                (exampleIniText, exampleJsonText, true),
            };
            var jsonParser = new JsonParser(LoggerLevel.JSON5, false, true, true, true);

            foreach ((string iniText, string correctJsonStrWithoutInterpolation, bool interpolation) in testcases)
            {
                JObject correctJson;
                JObject correctJsonWithInterpolation;
                string correctJsonStr;
                ii += 3;
                Npp.AddLine($"Parsing ini file (shown here as JSON string)\r\n{JNode.StrToString(iniText, true)}");
                try
                {
                    correctJson = (JObject)jsonParser.Parse(correctJsonStrWithoutInterpolation);
                    correctJsonWithInterpolation = IniFileParser.Interpolate(correctJson);
                    correctJsonStr = interpolation ? correctJsonStrWithoutInterpolation : correctJsonWithInterpolation.PrettyPrint();
                    if (interpolation)
                        correctJson = correctJsonWithInterpolation;
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"While trying to parse JSON\r\n{correctJsonStrWithoutInterpolation}\r\nGOT EXCEPTION\r\n{ex}");
                    testsFailed += 3;
                    continue;
                }
                JObject gotJson;
                IniFileParser iniParser = new IniFileParser(interpolation);
                try
                {
                    gotJson = iniParser.Parse(iniText);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"While trying to parse ini file\r\nGOT EXCEPTION\r\n{ex}");
                    testsFailed += 3;
                    continue;
                }
                string gotJsonStr = gotJson.ToString();
                if (gotJson.TryEquals(correctJson, out _))
                {
                    // test if comments are parsed correctly
                    try
                    {
                        string gotJsonWithComments = gotJson.ToStringWithComments(iniParser.comments);
                        if (gotJsonWithComments != correctJsonStr)
                        {
                            testsFailed++;
                            Npp.AddLine($"EXPECTED JSON WITH COMMENTS\r\n{correctJsonStr}\r\nGOT JSON WITH COMMENTS\r\n{gotJsonStr}");
                        }
                    }
                    catch (Exception ex)
                    {
                        testsFailed++;
                        Npp.AddLine($"EXPECTED JSON WITH COMMENTS\r\n{correctJsonStr}\r\nGOT EXCEPTION\r\n{ex}");
                    }
                    // test if dumping the JSON back to an ini file and re-parsing the dumped ini will return the same JSON
                    string dumpedIniText = "";
                    try
                    {
                        dumpedIniText = gotJson.ToIniFile(iniParser.comments);
                    }
                    catch (Exception ex)
                    {
                        testsFailed++;
                        Npp.AddLine($"When ini-dumping JSON\r\n{gotJsonStr}\r\nEXPECTED INI FILE\r\n{correctJsonStr}\r\nGOT EXCEPTION {ex}");
                        continue;
                    }
                    JObject jsonFromDumpedIniText;
                    try
                    {
                        jsonFromDumpedIniText = iniParser.Parse(dumpedIniText);
                    }
                    catch (Exception ex)
                    {
                        Npp.AddLine($"EXPECTED JSON FROM DUMPED INI FILE\r\n{correctJsonStr}\r\nGOT EXCEPTION\r\n{ex}");
                        testsFailed++;
                        continue;
                    }
                    if (!jsonFromDumpedIniText.TryEquals(correctJson, out _))
                    {
                        testsFailed++;
                        Npp.AddLine($"EXPECTED JSON FROM DUMPED INI FILE\r\n{correctJson.ToString()}\r\nGOT JSON\r\n{gotJson.ToString()}");
                    }
                }
                else
                {
                    testsFailed += 3;
                    Npp.AddLine($"EXPECTED JSON\r\n{correctJson.ToString()}\r\nGOT JSON\r\n{gotJson.ToString()}");
                }
            }

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }
    }
}
