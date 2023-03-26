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

        public static void TestFnames()
        {
            DirectoryInfo smalldir;
            try
            {
                smalldir = new DirectoryInfo(@"plugins\JsonTools\testfiles\small");
            }
            catch
            {
                Npp.AddLine("Could not find the testfiles directory in this plugin's folder\nThis directory contains the files required for this test.");
                return;
            }
            JObject all_jsons = new JObject();
            foreach (FileInfo f in smalldir.GetFiles())
            {
                string jsontxt = File.ReadAllText(f.FullName);
                try
                {
                    all_jsons[JObject.FormatAsKey(f.FullName)] = jparser.Parse(jsontxt);
                }
                catch { }
            }
            DirectoryInfo subdir = new DirectoryInfo($"{smalldir.FullName}\\subsmall");
            foreach (FileInfo f in subdir.GetFiles())
            {
                string jsontxt = File.ReadAllText(f.FullName);
                try
                {
                    all_jsons[JObject.FormatAsKey(f.FullName)] = jparser.Parse(jsontxt);
                }
                catch { }
            }
            var testcases = new object[][]
            {
                new object[]{"*.json", false, rparser.Search("keys(@)[@ =~ `.json$` & not(@ =~ `subsmall`)]", all_jsons, out bool _)}, // fnames w/o submall but ending in .json
				new object[]{"*.ipynb", false, rparser.Search("keys(@)[@ =~ `ipynb$` & not(@ =~ `subsmall`)]", all_jsons, out bool _) }, // fnames w/o subsmall but ending in .ipynb
				new object[]{"*.json", true, rparser.Search("keys(@)[@ =~ `json$`]", all_jsons, out bool _)}, // fnames ending in .json
				new object[]{"*.txt", true, rparser.Search("keys(@)[@ =~ `txt$`]", all_jsons, out bool _) }, // fnames ending in .txt
            };
            // test string slicer
            int tests_failed = 0;
            int ii = 0;
            JsonGrepper grepper = new JsonGrepper(new JsonParser());
            foreach (object[] test in testcases)
            {
                string search_pattern = (string)test[0];
                bool recursive = (bool)test[1];
                JNode desired_files = (JNode)test[2];
                grepper.Reset();
                grepper.Grep(smalldir.FullName, recursive, search_pattern);
                JNode found_files = rparser.Search("keys(@)", grepper.fname_jsons, out bool _);
                ((JArray)found_files).children.Sort();
                ((JArray)desired_files).children.Sort();
                if (found_files.ToString() != desired_files.ToString())
                {
                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} (grepper.Grep({1}, {2}, {3})) failed:\n" +
                                                    "Expected to find files\n{4}\nGot files\n{5}",
                                                    ii + 1, subdir.FullName, recursive, search_pattern, desired_files.PrettyPrint(), found_files.PrettyPrint()));
                }
                ii++;
            }

            // test nonstandard JsonParser settings for the grepper
            grepper.json_parser.allow_comments = true;
            grepper.json_parser.allow_singlequoted_str = true;
            string json_subdir_name = subdir.FullName.Replace("\\", "\\\\\\\\");

            var special_testcases = new Dictionary<string, JNode>
            {
                ["*comment*.txt"] = jparser.Parse($"[\"{json_subdir_name}\\\\\\\\comment_json_as_txt.txt\", \"{json_subdir_name}\\\\\\\\comments_and_singlequotes_json.txt\"]"),
                ["*singlequote*.txt"] = jparser.Parse($"[\"{json_subdir_name}\\\\\\\\singlequote_json_as_txt.txt\", \"{json_subdir_name}\\\\\\\\comments_and_singlequotes_json.txt\"]"),
            };
            foreach (KeyValuePair<string, JNode> kv in special_testcases)
            {
                string search_pattern = kv.Key;
                JNode desired_files = kv.Value;
                grepper.Reset();
                grepper.Grep(subdir.FullName, false, search_pattern);
                JNode found_files = rparser.Search("keys(@)", grepper.fname_jsons, out bool _);
                ((JArray)found_files).children.Sort();
                ((JArray)desired_files).children.Sort();
                if (found_files.ToString() != desired_files.ToString())
                {
                    tests_failed++;
                    Npp.AddLine(String.Format("Test {0} (grepper.Grep({1}, {2}, {3})) failed:\n" +
                                                    "Expected to find files\n{4}\nGot files\n{5}",
                                                    ii + 1, subdir.FullName, false, search_pattern, desired_files.PrettyPrint(), found_files.PrettyPrint()));
                }
                ii++;
            }

            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }

        public static async Task<int[]> TestApiRequesterHelper(string[] urls, int ii, int tests_failed, JsonGrepper grepper)
        {
            int n = urls.Length;
            ii += 1;
            Npp.AddLine($"Testing with {n} urls");
            try
            {
                grepper.fname_jsons.children.Clear();
                grepper.exceptions.children.Clear();
                await grepper.GetJsonFromApis(urls);
                int json_downloaded = grepper.fname_jsons.Length;
                int errors = grepper.exceptions.Length;
                int err_plus_json = json_downloaded + errors;
                ii += 1;
                if (err_plus_json != n)
                {
                    tests_failed += 1;
                    Npp.AddLine($"After making {n} requests, expected a total of {n} JSON downloads or errors," +
                        $" instead got {err_plus_json}.");
                }
                if (errors > 0)
                {
                    tests_failed += errors;
                    Npp.AddLine($"Expected no errors, instead got the following:\n{grepper.exceptions.PrettyPrint()}");
                }
            }
            catch (Exception ex)
            {
                tests_failed += 1;
                Npp.AddLine($"Expected API requester to not raise any exceptions, instead got exception\n{ex}");
            }
            return new int[] { ii, tests_failed };
        }

        public static async Task TestApiRequester()
        {
            JsonGrepper grepper = new JsonGrepper(new JsonParser());
            int ii = 0;
            int tests_failed = 0;

            string[] urls = new string[] {
                "https://api.weather.gov",
                "https://api.weather.gov/points/37.68333333333334,-121.92500000000001", // Alameda
                "https://api.weather.gov/points/37.75833333333334,-122.43333333333334", // San Francisco
            };
            // test when requesting from multiple urls
            int[] test_result = await TestApiRequesterHelper(urls, ii, tests_failed, grepper);
            ii = test_result[0];
            tests_failed = test_result[1];
            // test when requesting from only one url
            string[] first_url = new string[] { "https://api.weather.gov" };
            test_result = await TestApiRequesterHelper(first_url, ii, tests_failed, grepper);
            ii = test_result[0];
            tests_failed = test_result[1];
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }
    }
}
