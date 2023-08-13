using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
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

        private static List<string> filenamesUsed;

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
            string activeFname = Main.activeFname;
            TreeViewer openTv = Main.openTreeViewer;
            bool hasTreeView = openTv != null && !openTv.IsDisposed && openTv.Visible && openTv.fname == activeFname;
            switch (command)
            {
            case "file_open":
                int fileIdx = (int)args[0];
                while (fileIdx >= filenamesUsed.Count)
                {
                    Npp.notepad.FileNew();
                    Npp.notepad.SetCurrentBufferInternalName($"UI tests {filenamesUsed.Count + 1}.json");
                    filenamesUsed.Add(Npp.notepad.GetCurrentFilePath());
                }
                string filename = filenamesUsed[fileIdx];
                Npp.notepad.OpenFile(filename);
                messages.Add($"Opened file {filename}");
                break;
            case "overwrite":
                var text = (string)args[0];
                messages.Add($"overwrite file with\r\n{text}");
                Npp.editor.SetText(text);
                break;
            case "select":
                var startEndStrings = (string[])args[0];
                messages.Add($"select {SelectionManager.StartEndListToJsonString(startEndStrings)}");
                SelectionManager.SetSelectionsFromStartEnds(startEndStrings);
                break;
            case "select_every_valid":
                Main.SelectEveryValidJson();
                messages.Add("select every valid JSON");
                break;
            case "insert_text":
                var start = (int)args[0];
                text = (string)args[1];
                messages.Add($"insert {JNode.StrToString(text, false)} at {start}");
                Npp.editor.InsertText(start, text);
                break;
            case "delete_text":
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
                Npp.editor.GrabFocus();
                var gotSelections = SelectionManager.GetSelectedRanges();
                string correctSelStr = SelectionManager.StartEndListToJsonString(correctSelections);
                string gotSelStr = SelectionManager.StartEndListToJsonString(gotSelections);
                if (correctSelStr != gotSelStr)
                {
                    messages.Add($"FAIL: expected selections\r\n{correctSelStr}\r\nGOT\r\n{gotSelStr}");
                    return true;
                }
                messages.Add("compare_selections passed");
                break;
            case "compare_text":
                var correctText = (string)args[0];
                string gotText = Npp.editor.GetText(Npp.editor.GetLength());
                if (correctText != gotText)
                {
                    messages.Add($"FAIL: expected text\r\n{correctText}\r\nGOT\r\n{gotText}");
                    return true;
                }
                messages.Add("compare_text passed");
                break;
            case "tree_open":
                bool isJsonLines = args.Length >= 1 ? (bool)args[0] : false;
                Main.OpenJsonTree(isJsonLines);
                messages.Add("tree_open");
                break;
            case "treenode_click":
                if (!hasTreeView)
                {
                    messages.Add("Wanted to click a treenode, but treeview wasn't open");
                    return true;
                }
                // the text of each parent of the treenode to be clicked (except the root treenode)
                // followed by the text of the treenode to be clicked
                var treeNodeTexts = (IList<string>)args[0];
                var treeNodeTextArr = new JArray(0, treeNodeTexts.Select(s => new JNode(s)).ToList());
                TreeNode root = Main.openTreeViewer.Tree.Nodes[0];
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
                messages.Add($"click treenodes {treeNodeTextArr.ToString()}");
                Main.openTreeViewer.Tree.SelectedNode = root;
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
                    // test query before running it in the treeview to avoid a messagebox
                    JNode copyJson = Main.openTreeViewer.json.Copy();
                    if (Main.openTreeViewer.UsesSelections())
                    {
                        // run on each selection (just like the treeview does)
                        JObject copyObj = (JObject)copyJson;
                        foreach (var kv in copyObj.children)
                        {
                            remesParser.Search(query, kv.Value);
                        }
                    }
                    else // not using selections
                        remesParser.Search(query, Main.openTreeViewer.json.Copy());
                }
                catch (Exception ex)
                {
                    messages.Add($"RemesPath query {query} was invalid, produced exception {ex}");
                    return true;
                }
                Main.openTreeViewer.QueryBox.Text = query;
                Main.openTreeViewer.SubmitQueryButton.PerformClick();
                messages.Add($"Perform Remespath query {query}");
                break;
            case "sort_form_open":
                Main.OpenSortForm();
                messages.Add("open sort form");
                break;
            case "sort_form_run":
                if (Main.sortForm == null)
                {
                    messages.Add("Wanted to use sort form, but it wasn't open");
                    return true;
                }
                var path = (string)args[0];
                var multiArray = (bool)args[1];
                var bigToSmall = (bool)args[2];
                // 0 for Default, 1 for as strings, 2 for by index/key, 3 for by query on each child, 4 for shuffle
                var sortMethodIndex = (int)args[3];
                var keyQueryIndex = (string)args[4];
                messages.Add($"Use sort form with path = {path}, multiArray = {multiArray}, bigToSmall = {bigToSmall}, sortMethodIndex={sortMethodIndex}, keyQueryIndex = {keyQueryIndex}");
                Main.sortForm.PathTextBox.Text = path;
                Main.sortForm.IsMultipleArraysCheckBox.Checked = multiArray;
                Main.sortForm.ReverseOrderCheckBox.Checked = bigToSmall;
                Main.sortForm.SortMethodBox.SelectedIndex = sortMethodIndex;
                if (Main.sortForm.QueryKeyIndexTextBox.Enabled)
                    Main.sortForm.QueryKeyIndexTextBox.Text = keyQueryIndex;
                Main.sortForm.SortButton.PerformClick();
                break;
            case "compare_path_to_position":
                int position = (int)args[0];
                var correctPathToCurrentPosition = (string)args[1];
                SelectionManager.SetSelectionsFromStartEnds(new string[] { $"{position},{position}" });
                Main.CopyPathToCurrentPosition();
                string gotPathtoCurrentPosition = Clipboard.GetText();
                if (gotPathtoCurrentPosition != correctPathToCurrentPosition)
                    messages.Add($"Expected path to position {position} to be {correctPathToCurrentPosition}, but got {gotPathtoCurrentPosition}");
                else
                    messages.Add($"Passed test of path to position {position}");
                break;
            default:
                throw new ArgumentException($"Unrecognized command {command}");
            }
            return false;
        }

        public static List<(string command, object[] args)> testcases = new List<(string command, object[] args)>
        {
            ("overwrite", new object[]{"[1, 2, false]\r\n{\"a\": 3, \"b\": 1.5}\r\n[{\"c\": [null]}, -7]"}), // first line: 0 to 13, second line: 15 to 33, third line: 35 to 54
            ("select", new object[]{new string[]{ "0,13", "15,33", "35,54" } }),
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
            ("compare_text", new object[]{"[1,2,false]\r\n{\"a\":3,\"b\":1.5}\r\n[{\"c\":[null]},-7]"}),
            ("compare_selections", new object[]{new string[] {"0,11", "13,28", "30,47"} }),
            // TEST THAT TREENODE CLICKS GO TO RIGHT LOCATION
            ("tree_open", new object[]{}),
            ("treenode_click", new object[]{ new string[] { "0,11 : [3]", "2 : false"} }),
            ("compare_selections", new object[]{new string[] {"5,5"} }), // in front of @.`0,11`[2]
            ("treenode_click", new object[]{ new string[] {"30,47 : [2]", "0 : {1}", "c : [1]", "0 : null"} }),
            ("compare_selections", new object[]{new string[] {"37,37"} }), // in front of @.`30,47`[0].c[0]
            ("insert_text", new object[]{11, "\r\n"}),
            ("compare_text", new object[]{"[1,2,false]\r\n\r\n{\"a\":3,\"b\":1.5}\r\n[{\"c\":[null]},-7]"}),
            // TEST QUERY ONLY SOME SELECTIONS
            ("tree_query", new object[]{"@..g`[bc]`"}),
            ("treenode_click", new object[]{new string[]{"15,30 : [1]", "0 : 1.5"} }),
            ("compare_selections", new object[]{new string[]{"26,26" } }),
            ("treenode_click", new object[]{new string[] {"32,49 : [1]", "0 : [1]", "0 : null"} }),
            ("compare_selections", new object[]{new string[] {"39,39"} }),
            // TEST DELETIONS/INSERTIONS THAT START AND END WITHIN A SINGLE SELECTION
            ("delete_text", new object[]{2, 2}),
            ("compare_text", new object[]{"[1,false]\r\n\r\n{\"a\":3,\"b\":1.5}\r\n[{\"c\":[null]},-7]"}),
            ("tree_query", new object[]{"@..*[is_num(@)][@ >= 2]"}),
            ("treenode_click", new object[]{new string[] {"0,9 : []"} }),
            ("treenode_click", new object[]{new string[] {"13,28 : [1]", "0 : 3"} }),
            ("compare_selections", new object[]{new string[] {"18,18"} }),
            ("insert_text", new object[]{27, ", \"c\": 0"}), // add new key to second JSON
            ("compare_text", new object[]{"[1,false]\r\n\r\n{\"a\":3,\"b\":1.5, \"c\": 0}\r\n[{\"c\":[null]},-7]"}),
            ("tree_query", new object[]{"@..c"}),
            ("treenode_click", new object[]{new string[] {"13,36 : [1]", "0 : 0"} }),
            ("treenode_click", new object[]{new string[] {"38,55 : [1]"} }),
            // TEST SELECT_EVERY_VALID
            ("overwrite", new object[]{ "Errör 1 [foo]: [1,2,NaN]\r\nWarning 2: {\"ä\":3}\r\nInfo 3 {bar}: [[6,{\"b\":false}],-7]\r\nError 4: \"baz\" \"quz\"\r\nError 5:   \"string with no close quote\r\nError 6:  not {\"even\" [json" }),
            ("select", new object[]{new string[]{"0,0"} }),
            ("select_every_valid", new object[]{}),
            ("compare_selections", new object[]{new string[] { "16,25", "38,46", "62,82", "93,98", "99,104" } }),
            ("compare_path_to_position", new object[]{22, "[`16,25`][2]"}),
            ("compare_path_to_position", new object[]{101, "[`99,104`]"}),
            ("compare_path_to_position", new object[]{74, "[`62,82`][0][1].b"}),
            // TEST SELECT_EVERY_VALID ON A SUBSET OF THE FILE
            ("select", new object[]{new string[] {"1,46", "93,100", "161,167"} }),
            ("select_every_valid", new object[]{}),
            ("compare_selections", new object[]{new string[] {"16,25", "38,46", "93,98", "161,167"} }),
            // TEST SORT FORM
            ("overwrite", new object[]{"[1,3,2]\r\n[6,5,44]\r\n[\"a\",\"c\",\"boo\"]"}),
            ("select", new object[]{new string[]{"0,0"} }),
            ("select_every_valid", new object[]{}),
            ("compare_selections", new object[]{ new string[] { "0,7", "9,17", "19,34" } }),
            ("sort_form_open", new object[]{}),
            ("sort_form_run", new object[]{".g`^(?!0,)`", true, false, 1, ""}), // sort selections not starting at 0, as strings, ascending
            ("compare_text", new object[]{"[\r\n    1,\r\n    3,\r\n    2\r\n]\r\n[\r\n    44,\r\n    5,\r\n    6\r\n]\r\n[\r\n    \"a\",\r\n    \"boo\",\r\n    \"c\"\r\n]"}),
            ("sort_form_run", new object[]{"", true, true, 3, "s_len(str(@))"}), // sort all arrays biggest to smallest using the length of a node's string representation as key
            ("compare_text", new object[]{"[\r\n    1,\r\n    3,\r\n    2\r\n]\r\n[\r\n    44,\r\n    5,\r\n    6\r\n]\r\n[\r\n    \"boo\",\r\n    \"a\",\r\n    \"c\"\r\n]"}),
            // TEST FILE WITH ONLY ONE JSON DOCUMENT
            ("file_open", new object[]{1}),
            ("overwrite", new object[]{"[\r\n    [\"Я\", 1, \"a\"], // foo\r\n    [\"◐\", 2, \"b\"], // bar\r\n    [\"ồ\", 3, \"c\"], // baz\r\n    [\"ｪ\", 4, \"d\"],\r\n    [\"草\", 5, \"e\"],\r\n    [\"😀\", 6, \"f\"]\r\n]"}),
            ("tree_open", new object[]{}),
            ("treenode_click", new object[]{new string[] {"1 : [3]", "0 : \"◐\"" } }),
            ("compare_selections", new object[]{new string[] {"36,36"} }),
            ("compare_path_to_position", new object[]{36, "[1][0]"}),
            ("treenode_click", new object[]{ new string[] { "5 : [3]", "1 : 6" } }),
            ("compare_selections", new object[]{new string[] {"146,146"} }),
            ("compare_path_to_position", new object[]{147, "[5][1]"}),
            ("tree_query", new object[]{"@[:][1] = @ + 3" }),
            ("compare_text", new object[]{"[\r\n    [\"Я\", 4, \"a\"],\r\n    [\"◐\", 5, \"b\"],\r\n    [\"ồ\", 6, \"c\"],\r\n    [\"ｪ\", 7, \"d\"],\r\n    [\"草\", 8, \"e\"],\r\n    [\"😀\", 9, \"f\"]\r\n]"}),
            ("compare_path_to_position", new object[]{126, "[5][1]"}),
            ("file_open", new object[]{0}),
            // TEST ASSIGNMENT OPERATIONS ON MULTIPLE SELECTIONS (back to file with three arrays that were sorted by the sort form)
            ("tree_query", new object[]{"@[:][is_num(@)] = @ / 4"}),
            ("compare_text", new object[]{"[\r\n    0.25,\r\n    0.75,\r\n    0.5\r\n]\r\n[\r\n    11.0,\r\n    1.25,\r\n    1.5\r\n]\r\n[\r\n    \"boo\",\r\n    \"a\",\r\n    \"c\"\r\n]"}),
            ("treenode_click", new object[]{new string[] {"37,72 : [3]", "1 : 1.25"} }),
            ("compare_selections", new object[]{ new string[] { "55,55" } }),
            // TEST DELETION THAT STARTS BEFORE A SELECTION AND ENDS INSIDE IT
            ("insert_text", new object[]{47, "["}),
            ("delete_text", new object[]{ 35, 12 }),
            ("compare_text", new object[]{"[\r\n    0.25,\r\n    0.75,\r\n    0.5\r\n][0,\r\n    1.25,\r\n    1.5\r\n]\r\n[\r\n    \"boo\",\r\n    \"a\",\r\n    \"c\"\r\n]"}),
            ("compress", new object[]{}),
            ("compare_text", new object[]{"[0.25,0.75,0.5][0,1.25,1.5]\r\n[\"boo\",\"a\",\"c\"]"}),
            // TEST INSERTION THAT BEGINS ON THE FIRST CHAR OF A SELECTION
            ("insert_text", new object[]{15, "blah"}),
            ("compare_text", new object[]{"[0.25,0.75,0.5]blah[0,1.25,1.5]\r\n[\"boo\",\"a\",\"c\"]"}),
            ("compress", new object[]{}),
            ("compare_text", new object[]{"[0.25,0.75,0.5]blah[0,1.25,1.5]\r\n[\"boo\",\"a\",\"c\"]"}),
            // TEST DELETION THAT STARTS INSIDE A SELECTION AND ENDS AFTER IT
            ("insert_text", new object[]{26, "]"}),
            ("delete_text", new object[]{27, 7}),
            ("compare_text", new object[]{"[0.25,0.75,0.5]blah[0,1.25][\"boo\",\"a\",\"c\"]"}),
            ("compress", new object[]{}),
            ("compare_text", new object[]{"[0.25,0.75,0.5]blah[0,1.25][\"boo\",\"a\",\"c\"]"}),
        };

        public static bool Test()
        {
            var messages = new List<string>();
            int failures = 0;
            string previouslyOpenFname = Npp.notepad.GetCurrentFilePath();
            Npp.notepad.FileNew();
            Npp.notepad.SetCurrentBufferInternalName(ui_test_filename);
            filenamesUsed = new List<string> { ui_test_filename };

            PrettyPrintStyle previousPrettyPrintStyle = Main.settings.pretty_print_style;
            bool previousTabIndentPrettyPrint = Main.settings.tab_indent_pretty_print;
            int previousIndentPrettyPrint = Main.settings.indent_pretty_print;
            bool previousMinimalWhiteSpaceCompression = Main.settings.minimal_whitespace_compression;
            int previousMaxTrackedJsonSelections = Main.settings.max_tracked_json_selections;
            // require these settings for the UI tests alone
            Main.settings.pretty_print_style = PrettyPrintStyle.PPrint;
            Main.settings.tab_indent_pretty_print = false;
            Main.settings.indent_pretty_print = 4;
            Main.settings.minimal_whitespace_compression = true;
            Main.settings.max_tracked_json_selections = 1000;
            // add command to overwrite with a lot of arrays and select every valid json
            try
            {
                string oneArray2000xStr = (string)remesParser.Search("@ * 2000", new JNode("[1]\r\n")).value;
                JArray oneArray2000x = (JArray)new JsonParser().ParseJsonLines(oneArray2000xStr);
                string[] oneArray2000xSelections = ((JArray)remesParser.Search("(range(2000) * 5)[:]{@, @ + 3}{s_join(`,`, str(@))}[0]", new JNode())).children
                    .Select(x => (string)x.value)
                    .ToArray();
                string[] oneArray2000xPPrintSelections = ((JArray)remesParser.Search("(range(2000) * 13)[:]{@, @ + 11}{s_join(`,`, str(@))}[0]", new JNode())).children
                    .Select(x => (string)x.value)
                    .ToArray();
                testcases.Add(("overwrite", new object[] { oneArray2000xStr }));
                testcases.Add(("select_every_valid", new object[] { }));
                testcases.Add(("compare_selections", new object[] { oneArray2000xSelections }));
                testcases.Add(("pretty_print", new object[] { }));
                testcases.Add(("compare_selections", new object[] { oneArray2000xPPrintSelections }));
            }
            catch (Exception ex)
            {
                messages.Add($"Failed to add testcases due to exception {ex}");
            }
            // run all the commands
            int lastFailureIndex = messages.Count;
            foreach ((string command, object[] args) in testcases)
            {
                try
                {
                    if (ExecuteFileManipulation(command, messages, args))
                    {
                        failures++;
                        lastFailureIndex = messages.Count;
                    }
                }
                catch (Exception ex)
                {
                    failures++;
                    messages.Add("While running command " + command + " with args [" + string.Join(", ", args) + "], got exception\r\n" + ex);
                    lastFailureIndex = messages.Count;
                }
            }
            Main.settings.pretty_print_style = previousPrettyPrintStyle;
            Main.settings.indent_pretty_print = previousIndentPrettyPrint;
            Main.settings.tab_indent_pretty_print = previousTabIndentPrettyPrint;
            Main.settings.minimal_whitespace_compression = previousMinimalWhiteSpaceCompression;
            Main.settings.max_tracked_json_selections = previousMaxTrackedJsonSelections;
            // go back to the test file and show the results
            Npp.notepad.OpenFile(previouslyOpenFname);
            if (failures > 0)
            {
                // show all the messages up to the last failure
                Npp.AddLine(string.Join("\r\n", messages.LazySlice(0, lastFailureIndex)));
            }
            Npp.AddLine($"Failed {failures} tests");
            Npp.AddLine($"Passed {testcases.Count - failures} tests");
            return failures > 0;
        }
    }
}
