using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using JSON_Tools.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;

namespace JSON_Tools.Tests
{
    public class UserInterfaceTester
    {
        const string ui_test_filename = "UI tests.json";

        private static RemesParser remesParser = new RemesParser();

        /// <summary>
        /// Run a command (see below switch statement for options) to manipulate the test file.
        /// Returns true if one of the "compare" commands (compare_text, compare_selections, compare_treeview) fails the test
        /// </summary>
        /// <param name="command"></param>
        /// <param name="messages"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool ExecuteFileManipulation(string command, List<string> messages, params object[] args)
        {
            bool hasJsonInfo = Main.TryGetInfoForFile(ui_test_filename, out JsonFileInfo info);
            bool hasTreeView = hasJsonInfo && info.tv != null;
            switch (command)
            {
            case "overwrite":
                var text = (string)args[0];
                messages.Add("overwrite file with\r\n{text}");
                Npp.editor.SetText(text);
                break;
            case "select":
                var startEndStrings = (string[])args[0];
                messages.Add("select" + string.Join(" ", startEndStrings));
                SelectionManager.SetSelectionsFromStartEnds(startEndStrings);
                break;
            case "insert":
                var start = (int)args[0];
                text = (string)args[1];
                messages.Add($"insert {text} at {start}");
                Npp.editor.InsertText(start, text);
                break;
            case "delete":
                start = (int)args[0];
                int length = (int)args[1];
                messages.Add($"delete {length} chars starting at {start}");
                Npp.editor.DeleteRange(start, length);
                break;
            case "pretty_print":
                Main.PrettyPrintJson();
                messages.Add("pretty-print");
                break;
            case "compress":
                Main.CompressJson();
                messages.Add("compress");
                break;
            case "compare_selections":
                var correctSelections = (string[])args[0]; // should be array of strings of the form "INTEGER1,INTEGER2"
                var gotSelections = SelectionManager.GetSelectedRanges();
                string correctSelStr = "[" + string.Join(", ", correctSelections.Select(x => $"\"{x}\"")) + "]";
                string gotSelStr = SelectionManager.StartEndListToJsonString(gotSelections);
                if (correctSelStr != gotSelStr)
                {
                    messages.Add($"FAIL: expected selections\r\n{correctSelStr}\r\nGOT\r\n{gotSelStr}");
                    return true;
                }
                break;
            case "compare_text":
                var correctText = (string)args[0];
                string gotText = Npp.editor.GetText(Npp.editor.GetLength());
                if (correctText != gotText)
                {
                    messages.Add($"FAIL: expected text\r\n{correctText}\r\nGOT\r\n{gotText}");
                    return true;
                }
                break;
            case "tree_open":
                bool isJsonLines = args.Length >= 1 ? (bool)args[0] : false;
                Main.OpenJsonTree(isJsonLines);
                break;
            case "click_treenode":
                // the text of each parent of the treenode to be clicked (except the root treenode)
                // followed by the text of the treenode to be clicked
                var treeNodeTexts = (List<string>)args[0];
                var treeNodeTextArr = new JArray(0, treeNodeTexts.Select(s => new JNode(s)).ToList());
                if (!hasTreeView)
                {
                    messages.Add("Wanted to click a treenode, but treeview wasn't open");
                    return true;
                }
                TreeNode root = info.tv.Tree.Nodes[0];
                for (int ii = 0; ii < treeNodeTexts.Count; ii++)
                {
                    string treeNodeText = treeNodeTexts[ii];
                    if (!root.IsExpanded)
                        root.Expand();
                    bool hasFoundNewRoot = false;
                    foreach (TreeNode child in root.Nodes)
                    {
                        if (child.Text == treeNodeText)
                        {
                            root = child;
                            hasFoundNewRoot = true;
                            break;
                        }
                    }
                    if (!hasFoundNewRoot)
                    {
                        string previousNodes = new JArray(0, treeNodeTextArr.children.LazySlice(0, ii).ToList()).ToString();
                        messages.Add($"Expected treeview to contain path {treeNodeTextArr.ToString()}, " +
                                     $"but did not contain {treeNodeText} after nodes {previousNodes}");
                        return true;
                    }
                }
                info.tv.Tree.SelectedNode = root;
                break;
            case "tree_query":
                if (!hasTreeView)
                {
                    messages.Add("Wanted to execute a RemesPath query, but the treeview wasn't open");
                    return true;
                }
                var query = (string)args[0];
                try
                {
                    JNode queryResult = remesParser.Search(query, info.json);
                }
                catch (Exception ex)
                {
                    messages.Add($"RemesPath query {query} was invalid, produced exception {ex}");
                    return true;
                }
                info.tv.QueryBox.Text = query;
                info.tv.SubmitQueryButton.PerformClick();
                break;
            }
            return false;
        }

        public static List<(string command, object[] args)> testcases = new List<(string command, object[] args)>
        {
            ("overwrite", new object[]{"[1, 2, false]\r\n{\"a\": 3, \"b\": 1.5}\r\n[{\"c\": [null]}, -7]"}), // first line: 0 to 13, second line: 15 to 33, third line: 35 to 55
            ("select", new object[]{new string[]{ "0,13", "15,33", "35,55" } }),
            // when pretty-printed in PPrint style looks like this:
            //[
            //    1,
            //    2,
            //    false
            //]
            //{
            //    "a": 3,
            //    "b": 1.5
            //}
            //[
            //    {"c": [null]},
            //    -7
            //]
            // first JSON: 0 to 31, second JSON: 33 to 64, third JSON: 66 to 98
            ("pretty_print", new object[]{ }),
            ("compare_text", new object[]{ "[\r\n    1,\r\n    2,\r\n    false\r\n]\r\n{\r\n    \"a\": 3,\r\n    \"b\": 1.5\r\n}\r\n[\r\n    {\"c\": [null]},\r\n    -7\r\n]"}),
            ("compare_selections", new object[]{new string[]{"0,31", "33,64", "66,98"} }),
            // when compressed looks like this:
            //[1,2,false]
            //{"a":3,"b":1.5}
            //[{"c":[null]},-7]
            // first JSON: 0 to 11, second JSON: 13 to 28, third JSON: 30 to 47
            ("compress", new object[]{}),
            ("compare_text", new object[]{"[[1, 2, false]\r\n{\"a\": 3, \"b\": 1.5}\r\n[{\"c\": [null]}, -7]]"}),
        };

        public static void Test()
        {
            var messages = new List<string>();
            int failures = 0;
            string previouslyOpenFname = Npp.notepad.GetCurrentFilePath();
            Npp.notepad.FileNew();
            Npp.notepad.SetCurrentBufferInternalName(ui_test_filename);

            PrettyPrintStyle previousPrettyPrintStyle = Main.settings.pretty_print_style;
            bool previousTabIndentPrettyPrint = Main.settings.tab_indent_pretty_print;
            int previousIndentPrettyPrint = Main.settings.indent_pretty_print;
            bool previousMinimalWhiteSpaceCompression = Main.settings.minimal_whitespace_compression;
            // require these settings for the UI tests alone
            Main.settings.pretty_print_style = PrettyPrintStyle.PPrint;
            Main.settings.tab_indent_pretty_print = false;
            Main.settings.indent_pretty_print = 4;
            Main.settings.minimal_whitespace_compression = true;
            // run all the commands
            foreach ((string command, object[] args) in testcases)
            {
                try
                {
                    if (ExecuteFileManipulation(command, messages, args))
                        failures++;
                }
                catch (Exception ex)
                {
                    failures++;
                    messages.Add("While running command " + command + " with args [" + string.Join(", ", args) + "], got exception\r\n" + ex);
                }
            }
            Main.settings.pretty_print_style = previousPrettyPrintStyle;
            Main.settings.indent_pretty_print = previousIndentPrettyPrint;
            Main.settings.tab_indent_pretty_print = previousTabIndentPrettyPrint;
            Main.settings.minimal_whitespace_compression = previousMinimalWhiteSpaceCompression;
            // go back to the test file and show the results
            Npp.notepad.OpenFile(previouslyOpenFname);
            Npp.AddLine(string.Join("\r\n", messages));
            Npp.AddLine($"Failed {failures} tests");
            Npp.AddLine($"Passed {testcases.Count - failures} tests");
        }
    }
}
