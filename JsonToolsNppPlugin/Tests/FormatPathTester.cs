using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Forms;
//using JSON_Tools.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    public class FormatPathTester
    {
        public static void Test()
        {
            JsonParser parser = new JsonParser();
            JNode json = parser.Parse("{\"a\":1,\"b \":\"f\",\"cu\":[false,true,[{\"\":{\"bufrear\":null}}]],\"d\":-1.5,\"e\\\"\":[NaN,Infinity,-Infinity]}");
            var testcases = new (int pos, KeyStyle style, string correct_path)[]
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
                (93, KeyStyle.JavaScript, "['e\"'][2]"),
                (93, KeyStyle.RemesPath, "[`e\\\"`][2]"),
                (93, KeyStyle.Python, "['e\"'][2]"),
            };
            int ii = 0;
            int tests_failed = 0;
            foreach ((int pos, KeyStyle style, string correct_path) in testcases)
            {
                ii++;
                string path;
                try
                {
                    path = json.PathToPosition(pos, style);
                }
                catch (Exception ex)
                {
                    tests_failed++;
                    Npp.AddLine($"While trying to get the path to position {pos}, threw exception\r\n{ex}");
                    continue;
                }
                if (path != correct_path)
                {
                    tests_failed++;
                    Npp.AddLine($"Got the path to position {pos} as {path}, but it should be {correct_path}");
                }
            }
            Npp.AddLine($"Failed {tests_failed} tests.");
            Npp.AddLine($"Passed {ii - tests_failed} tests.");
        }

        //public void TestPathToTreeNode()
        //{
        //    TreeNode root = new TreeNode();
        //    TreeView tree = new TreeView();
        //    var pathsToJNodes = new Dictionary<string, JNode>();
        //    TreeViewer.JsonTreePopulateHelper_DirectChildren(tree, root, json, pathsToJNodes);
        //    var testcases = new ()
        //}
    }
}
