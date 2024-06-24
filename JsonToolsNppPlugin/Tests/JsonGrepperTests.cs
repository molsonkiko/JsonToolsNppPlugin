using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class JsonGrepperTester
    {
        public static JsonParser jparser = new JsonParser();
        public static RemesParser rparser = new RemesParser();

        public static bool TestFnames()
        {
            DirectoryInfo smalldir;
            try
            {
                smalldir = new DirectoryInfo(Path.Combine(Npp.pluginDllDirectory, "testfiles", "small"));
            }
            catch
            {
                Npp.AddLine("Could not find the testfiles directory in this plugin's folder\nThis directory contains the files required for this test.");
                return true;
            }
            JObject allJsons = new JObject();
            foreach (FileInfo f in smalldir.GetFiles())
            {
                string jsontxt = File.ReadAllText(f.FullName);
                try
                {
                    allJsons[f.FullName] = jparser.Parse(jsontxt);
                }
                catch { }
            }
            DirectoryInfo subdir = new DirectoryInfo($"{smalldir.FullName}\\subsmall");
            foreach (FileInfo f in subdir.GetFiles())
            {
                string jsontxt = File.ReadAllText(f.FullName);
                try
                {
                    allJsons[f.FullName] = jparser.Parse(jsontxt);
                }
                catch { }
            }
            var testcases = new (string[] patterns, bool recursive, JNode desiredFiles)[]
            {
                (new string[] { "*.json" }, false, rparser.Search("keys(@)[@ =~ `.json$` & not(@ =~ `subsmall`)]", allJsons)), // fnames w/o submall but ending in .json
				(new string[] { "*.ipynb" }, false, rparser.Search("keys(@)[@ =~ `ipynb$` & not(@ =~ `subsmall`)]", allJsons)), // fnames w/o subsmall but ending  .ip{ynb
				(new string[] { "*.json" }, true, rparser.Search("keys(@)[@ =~ `json$`]", allJsons)), // fnames ending in .json
				(new string[] { "*.txt" }, true, rparser.Search("keys(@)[@ =~ `txt$`]", allJsons) ), // fnames ending in .txt
				(new string[] { "*.txt", "*.json" }, true, rparser.Search("keys(@)[@ =~ `(?:txt|json)$`]", allJsons) ), // fnames ending in .txt or .json
            };
            // test string slicer
            int testsFailed = 0;
            int ii = 0;
            JsonGrepper grepper = new JsonGrepper(new JsonParser());
            foreach ((string[] patterns, bool recursive, JNode desiredFiles) in testcases)
            {
                grepper.Reset();
                grepper.Grep(smalldir.FullName, recursive, patterns);
                JNode foundFiles = rparser.Search("keys(@)", grepper.fnameJsons);
                ((JArray)foundFiles).children.Sort();
                ((JArray)desiredFiles).children.Sort();
                if (foundFiles.ToString() != desiredFiles.ToString())
                {
                    testsFailed++;
                    Npp.AddLine(String.Format("Test {0} (grepper.Grep({1}, {2}, {3})) failed:\n" +
                                                    "Expected to find files\n{4}\nGot files\n{5}",
                                                    ii + 1, subdir.FullName, recursive, string.Join(", ", patterns), desiredFiles.PrettyPrint(), foundFiles.PrettyPrint()));
                }
                ii++;
            }

            // test nonstandard JsonParser settings for the grepper
            grepper.jsonParser = new JsonParser(LoggerLevel.JSON5);
            string jsonSubdirName = subdir.FullName.Replace("\\", "\\\\");

            var specialTestcases = new Dictionary<string, JNode>
            {
                ["*comment*.txt"] = jparser.Parse($"[\"{jsonSubdirName}\\\\comment_json_as_txt.txt\", \"{jsonSubdirName}\\\\comments_and_singlequotes_json.txt\"]"),
                ["*singlequote*.txt"] = jparser.Parse($"[\"{jsonSubdirName}\\\\singlequote_json_as_txt.txt\", \"{jsonSubdirName}\\\\comments_and_singlequotes_json.txt\"]"),
            };
            foreach (KeyValuePair<string, JNode> kv in specialTestcases)
            {
                string searchPattern = kv.Key;
                JNode desiredFiles = kv.Value;
                grepper.Reset();
                grepper.Grep(subdir.FullName, false, searchPattern);
                JNode foundFiles = rparser.Search("keys(@)", grepper.fnameJsons);
                ((JArray)foundFiles).children.Sort();
                ((JArray)desiredFiles).children.Sort();
                if (foundFiles.ToString() != desiredFiles.ToString())
                {
                    testsFailed++;
                    Npp.AddLine(String.Format("Test {0} (grepper.Grep({1}, {2}, {3})) failed:\n" +
                                                    "Expected to find files\n{4}\nGot files\n{5}",
                                                    ii + 1, subdir.FullName, false, searchPattern, desiredFiles.PrettyPrint(), foundFiles.PrettyPrint()));
                }
                ii++;
            }

            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }

        public static async Task<int[]> TestApiRequesterHelper(string[] urls, int ii, int testsFailed, JsonGrepper grepper)
        {
            int n = urls.Length;
            ii += 1;
            Npp.AddLine($"Testing with {n} urls");
            try
            {
                grepper.fnameJsons.children.Clear();
                grepper.exceptions.children.Clear();
                await grepper.GetJsonFromApis(urls);
                int jsonDownloaded = grepper.fnameJsons.Length;
                int errors = grepper.exceptions.Length;
                int errPlusJson = jsonDownloaded + errors;
                ii += 1;
                if (errPlusJson != n)
                {
                    testsFailed += 1;
                    Npp.AddLine($"After making {n} requests, expected a total of {n} JSON downloads or errors," +
                        $" instead got {errPlusJson}.");
                }
                if (errors > 0)
                {
                    testsFailed += errors;
                    Npp.AddLine($"Expected no errors, instead got the following:\n{grepper.exceptions.PrettyPrint()}");
                }
            }
            catch (Exception ex)
            {
                testsFailed += 1;
                Npp.AddLine($"Expected API requester to not raise any exceptions, instead got exception\n{ex}");
            }
            return new int[] { ii, testsFailed };
        }

        public static async Task<bool> TestApiRequester()
        {
            JsonGrepper grepper = new JsonGrepper(new JsonParser());
            int ii = 0;
            int testsFailed = 0;

            string[] urls = new string[] {
                "https://api.weather.gov",
                "https://api.weather.gov/points/37.68333333333334,-121.92500000000001", // Alameda
                "https://api.weather.gov/points/37.75833333333334,-122.43333333333334", // San Francisco
            };
            // test when requesting from multiple urls
            int[] testResult = await TestApiRequesterHelper(urls, ii, testsFailed, grepper);
            ii = testResult[0];
            testsFailed = testResult[1];
            // test when requesting from only one url
            string[] firstUrl = new string[] { "https://api.weather.gov" };
            testResult = await TestApiRequesterHelper(firstUrl, ii, testsFailed, grepper);
            ii = testResult[0];
            testsFailed = testResult[1];
            Npp.AddLine($"Failed {testsFailed} tests.");
            Npp.AddLine($"Passed {ii - testsFailed} tests.");
            return testsFailed > 0;
        }
    }
}
