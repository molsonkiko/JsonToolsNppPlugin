using System;
using System.Collections.Generic;
using System.IO;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Tests
{
    public class JsonGrepperTester
    {
        public static DirectoryInfo smalldir = new DirectoryInfo(@"C:\Users\mjols\Documents\csharp\JSONToolsPlugin\testfiles\small");
        public static JsonParser jparser = new JsonParser();
        public static RemesParser rparser = new RemesParser();

        public static void TestFnames()
        {
            JObject all_jsons = new JObject(0, new Dictionary<string, JNode>());
            foreach (FileInfo f in smalldir.GetFiles())
            {
                string jsontxt = File.ReadAllText(f.FullName);
                try
                {
                    all_jsons[f.FullName] = jparser.Parse(jsontxt);
                }
                catch { }
            }
            DirectoryInfo subdir = new DirectoryInfo($"{smalldir.FullName}\\subsmall");
            foreach (FileInfo f in subdir.GetFiles())
            {
                string jsontxt = File.ReadAllText(f.FullName);
                try
                {
                    all_jsons[f.FullName] = jparser.Parse(jsontxt);
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
            grepper.json_parser.allow_javascript_comments = true;
            grepper.json_parser.allow_singlequoted_str = true;
            string json_subdir_name = subdir.FullName.Replace("\\", "\\\\");

            var special_testcases = new Dictionary<string, JNode>
            {
                ["*comment*.txt"] = jparser.Parse($"[\"{json_subdir_name}\\\\comment_json_as_txt.txt\"]"),
                ["*singlequote*.txt"] = jparser.Parse($"[\"{json_subdir_name}\\\\singlequote_json_as_txt.txt\"]"),
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
    }
}
