using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using JSON_Tools.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;

namespace JSON_Tools.Tests
{
    public class UserInterfaceTester
    {
        private static List<string> filenamesUsed;

        private static int lowestFilenameNumberNotUsed = 0;

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
                string filename = OpenUITestFile(fileIdx);
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
            case "select_whole_doc":
                var wholeSelStr = $"0,{Npp.editor.GetLength()}";
                messages.Add("select whole document");
                SelectionManager.SetSelectionsFromStartEnds(new string[] { wholeSelStr });
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
                    messages.Add("FAIL: Wanted to click a treenode, but treeview wasn't open");
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
                        messages.Add($"FAIL: Expected treeview to contain path {treeNodeTextArr.ToString()}, " +
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
                    messages.Add("FAIL: Wanted to execute a RemesPath query, but the treeview wasn't open");
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
                    messages.Add($"FAIL: RemesPath query {query} was invalid, produced exception {ex}");
                    return true;
                }
                Main.openTreeViewer.QueryBox.Text = query;
                Main.openTreeViewer.SubmitQueryButton.PerformClick();
                messages.Add($"Perform Remespath query {query}");
                break;
            case "select_treenode_json":
                if (!hasTreeView)
                {
                    messages.Add("FAIL: Wanted to select the JSON associated with a treenode, but the treeview wasn't open");
                    return true;
                }
                messages.Add("Select JSON associated with treenode");
                var selNode = openTv.Tree.SelectedNode;
                openTv.SelectTreeNodeJson(selNode);
                break;
            case "select_treenode_json_children":
                if (!hasTreeView)
                {
                    messages.Add("FAIL: Wanted to select the children of a treenode's associated JSON, but the treeview wasn't open");
                    return true;
                }
                messages.Add("Select children of JSON associated with treenode");
                selNode = openTv.Tree.SelectedNode;
                openTv.SelectTreeNodeJsonChildren(selNode);
                break;
            case "tree_compare_query_result":
                if (!hasTreeView)
                {
                    messages.Add("FAIL: Wanted to compare the tree's query result, but the treeview wasn't open");
                    return true;
                }
                string correctQueryResultStr = (string)args[0];
                JNode gotQueryResult = openTv.queryResult;
                string gotQueryResultStr = gotQueryResult.ToString();
                if (correctQueryResultStr != gotQueryResultStr)
                {
                    messages.Add($"FAIL: expected query result to be\r\n{correctQueryResultStr}\r\nbut it was\r\n{gotQueryResultStr}");
                    return true;
                }
                messages.Add("tree_compare_query_result passed");
                break;
            case "sort_form_open":
                Main.OpenSortForm();
                messages.Add("open sort form");
                break;
            case "sort_form_run":
                if (Main.sortForm == null)
                {
                    messages.Add("FAIL: Wanted to use sort form, but it wasn't open");
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
                    messages.Add($"FAIL: Expected path to position {position} to be {correctPathToCurrentPosition}, but got {gotPathtoCurrentPosition}");
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
            // TEST SELECTING NON-JSON RANGE DOES NOT FORGET SELECTIONS
            ("select", new object[]{new string[] {"8,15"} }),
            ("compress", new object[]{}),
            ("compare_text", new object[]{"[1,3,2]\r\n[44,5,6]\r\n[\"boo\",\"a\",\"c\"]"}),
            // TEST SELECTING JSON (AND JSON CHILDREN) FROM TREEVIEW IN SELECTION-BASED DOC
            ("delete_text", new object[]{20, 5}), // select treenode json and json children with array
            ("insert_text", new object[]{20, "[1, NaN,\"b\"]"}),
            ("compare_text", new object[]{"[1,3,2]\r\n[44,5,6]\r\n[[1, NaN,\"b\"],\"a\",\"c\"]"}),
            ("tree_query", new object[]{"@"}),
            ("treenode_click", new object[]{new string[] {"19,41 : [3]", "0 : [3]"} }),
            ("select_treenode_json", new object[]{}),
            ("compare_selections", new object[]{new string[] {"20,32"} }),
            ("select_treenode_json_children", new object[]{}),
            ("compare_selections", new object[]{new string[] {"21,22", "24,27", "28,31"} }),
            ("delete_text", new object[]{20, 12}), // now select treenode json and json children with object
            ("insert_text", new object[]{20, "{\"a\": [null], \"b\": {}}"}),
            ("tree_query", new object[]{"@"}),
            ("treenode_click", new object[]{new string[] {"19,51 : [3]", "0 : {2}"} }),
            ("select_treenode_json", new object[]{}),
            ("compare_selections", new object[]{new string[] {"20,42"} }),
            ("select_treenode_json_children", new object[]{}),
            ("compare_selections", new object[]{new string[] {"26,32", "39,41"} }),
            ("treenode_click", new object[]{new string[] {"19,51 : [3]", "0 : {2}", "a : [1]", "0 : null"} }), // try selecting treenode json with scalar
            ("select_treenode_json", new object[]{}),
            ("compare_selections", new object[]{new string[] {"27,31"} }),
            ("delete_text", new object[]{20, 22}), // revert to how it was before select-treenode-json tests
            ("insert_text", new object[]{20, "\"boo\""}),
            ("tree_query", new object[]{"@"}),
            ("treenode_click", new object[]{new string[] {"9,17 : [3]"} }), // try selecting json and json children on a different array
            ("select_treenode_json_children", new object[]{}),
            ("compare_selections", new object[]{new string[] {"10,12", "13,14", "15,16"} }),
            ("select_treenode_json", new object[]{}),
            ("compare_selections", new object[]{new string[] {"9,17"} }),
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
            ("tree_query", new object[]{"@[:][1] = @ + 3" }), // mutate document
            ("compare_text", new object[]{"[\r\n    [\"Я\", 4, \"a\"],\r\n    [\"◐\", 5, \"b\"],\r\n    [\"ồ\", 6, \"c\"],\r\n    [\"ｪ\", 7, \"d\"],\r\n    [\"草\", 8, \"e\"],\r\n    [\"😀\", 9, \"f\"]\r\n]"}),
            ("compare_path_to_position", new object[]{126, "[5][1]"}),
            // TEST SELECT NON-JSON RANGE AND MAKE SURE WHOLE DOCUMENT STILL PARSED (WHEN NOT IN SELECTION MODE)
            ("select", new object[]{new string[] {"1,7"} }),
            ("compress", new object[]{}),
            ("compare_text", new object[]{"[[\"Я\",4,\"a\"],[\"◐\",5,\"b\"],[\"ồ\",6,\"c\"],[\"ｪ\",7,\"d\"],[\"草\",8,\"e\"],[\"😀\",9,\"f\"]]"}),
            // TEST SELECTING JSON (AND JSON CHILDREN) FROM TREEVIEW IN SELECTION-BASED DOC
            ("tree_query", new object[]{"@"}),
            ("treenode_click", new object[]{new string[] {"2 : [3]"} }), // try selecting json and json children with array
            ("select_treenode_json_children", new object[]{}),
            ("compare_selections", new object[]{new string[] {"29,34", "35,36", "37,40"} }),
            ("select_treenode_json", new object[]{}),
            ("compare_selections", new object[]{new string[] {"28,41"} }),
            ("treenode_click", new object[]{new string[] {"3 : [3]", "0 : \"ｪ\"" } }), // try selecting treenode json with scalar
            ("select_treenode_json", new object[]{}),
            ("compare_selections", new object[]{new string[] {"43,48"} }),
            ("overwrite", new object[]{"[[\"Я\",4,\"a\"],[\"◐\",5,\"b\"],[\"ồ\",6,\"c\"],[\"ｪ\",7,\"d\"],{\"草\":[8],\"e\":-.5, gor:/**/ '😀'},[\"😀\",9,\"f\"]]"}), // try selecting treenode json and json children with object
            ("tree_query", new object[]{"@"}),
            ("treenode_click", new object[]{new string[] {"4 : {3}"} }),
            ("select_treenode_json", new object[]{}),
            ("compare_selections", new object[]{new string[] {"56,92"} }),
            ("select_treenode_json_children", new object[]{}),
            ("compare_selections", new object[]{new string[] {"63,66", "71,74", "85,91"} }),
            ("select", new object[]{new string[]{"0,0"} }),
            ("tree_query", new object[]{"@![4][@[1] >= 7]"}), // try selecting treenode json children when parent is RemesPath query node
            ("treenode_click", new object[]{new string[]{ } }),
            ("select_treenode_json_children", new object[]{}),
            ("pretty_print", new object[]{}),
            ("compare_text", new object[]{"[[\"Я\",4,\"a\"],[\"◐\",5,\"b\"],[\"ồ\",6,\"c\"],[\r\n    \"ｪ\",\r\n    7,\r\n    \"d\"\r\n],{\"草\":[8],\"e\":-.5, gor:/**/ '😀'},[\r\n    \"😀\",\r\n    9,\r\n    \"f\"\r\n]]"}),
            // TEST NON-MUTATING MULTI-STATEMENT QUERIES ON A DOCUMENT THAT DOES NOT USE SELECTIONS
            ("select_whole_doc", new object[]{}),
            ("compress", new object[]{}),
            ("compare_text", new object[]{"[[\"Я\",4,\"a\"],[\"◐\",5,\"b\"],[\"ồ\",6,\"c\"],[\"ｪ\",7,\"d\"],{\"e\":-0.5,\"gor\":\"😀\",\"草\":[8]},[\"😀\",9,\"f\"]]"}),
            ("tree_query", new object[]{"var a = @[0];\r\nvar b = @[1];\r\nvar c = @[-1];\r\nvar d = a + b * s_len(str(c));"}),
            ("compare_text", new object[]{"[[\"Я\",4,\"a\"],[\"◐\",5,\"b\"],[\"ồ\",6,\"c\"],[\"ｪ\",7,\"d\"],{\"e\":-0.5,\"gor\":\"😀\",\"草\":[8]},[\"😀\",9,\"f\"]]"}),
            ("treenode_click", new object[]{new string[] { "0 : \"Я◐◐\"" } }),
            ("treenode_click", new object[]{new string[] {"1 : 9"} }),
            ("treenode_click", new object[]{new string[] { "2 : \"ab\"" } }),
            ("tree_query", new object[]{"var mod_3s = (@[:][type(@) != object])[:]->append(@, @[1] % 3);\r\n" +
                                        "var gb_m3 = group_by(mod_3s, -1);\r\n" +
                                        "gb_m3.`1`"}),
            ("compare_text", new object[]{"[[\"Я\",4,\"a\"],[\"◐\",5,\"b\"],[\"ồ\",6,\"c\"],[\"ｪ\",7,\"d\"],{\"e\":-0.5,\"gor\":\"😀\",\"草\":[8]},[\"😀\",9,\"f\"]]"}),
            ("treenode_click", new object[]{new string[] { "0 : [4]", "2 : \"a\"" } }),
            ("treenode_click", new object[]{new string[] { "1 : [4]", "1 : 7" } }),
            // TEST MUTATING MULTI-STATEMENT QUERIES ON A DOCUMENT THAT DOES NOT USE SELECTIONS
            ("tree_query", new object[]{"var a = @[0];\r\n" +
                                        "var b = @[1];\r\n" +
                                        "var c = @[-1];\r\n" +
                                        "var d = a + b * s_len(str(c));\r\n" +
                                        "a[0] = @ + d[0];\r\nb[1] = @ + c[1];\r\nc[2] = @ + a[0]"}),
            ("compare_text", new object[]{"[\r\n    [\"ЯЯ◐◐\", 4, \"a\"],\r\n    [\"◐\", 14, \"b\"],\r\n    [\"ồ\", 6, \"c\"],\r\n    [\"ｪ\", 7, \"d\"],\r\n    {\"e\": -0.5, \"gor\": \"😀\", \"草\": [8]},\r\n    [\"😀\", 9, \"fЯЯ◐◐\"]\r\n]"}),
            ("treenode_click", new object[]{new string[]{"0 : [3]", "0 : \"ЯЯ◐◐\"" } }),
            ("compare_selections", new object[]{new string[]{ "8,8" } }),
            ("treenode_click", new object[]{new string[] {"1 : [3]", "1 : 14" } }),
            ("compare_selections", new object[]{new string[]{ "44,44" } }),
            ("treenode_click", new object[]{new string[]{"5 : [3]", "2 : \"fЯЯ◐◐\"" } }),
            ("compare_selections", new object[]{new string[]{ "160,160" } }),
            ("tree_query", new object[]{"var bumba = @[1][1]; bumba = @ / 7; bumba"}),
            ("compare_text", new object[]{"[\r\n    [\"ЯЯ◐◐\", 4, \"a\"],\r\n    [\"◐\", 2.0, \"b\"],\r\n    [\"ồ\", 6, \"c\"],\r\n    [\"ｪ\", 7, \"d\"],\r\n    {\"e\": -0.5, \"gor\": \"😀\", \"草\": [8]},\r\n    [\"😀\", 9, \"fЯЯ◐◐\"]\r\n]"}),
            ("treenode_click", new object[]{ new string[] { } }),
            ("compare_selections", new object[]{new string[] {"44,44"} }),
            ("tree_compare_query_result", new object[]{"2.0"}),
            // TEST ASSIGNMENT OPERATIONS ON MULTIPLE SELECTIONS (back to file with three arrays that were sorted by the sort form)
            ("file_open", new object[]{0}),
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
            // TEST QUERY THAT PRODUCES OBJECT WITH NON-"START,END" KEYS ON A FILE WITH SELECTIONS
            ("tree_query", new object[]{"j`{\"a\": \"foo\", \"b\": [1, 2]}`"}),
            ("treenode_click", new object[]{new string[] {"a : \"foo\""} }),
            ("treenode_click", new object[]{new string[] {"b : [2]", "1 : 2"} }),
            // TEST MULTI-STATEMENT QUERY THAT DOESN'T MUTATE ON A FILE WITH SELECTIONS
            ("tree_query", new object[]{"var s = str(@);\r\n" +
                                        "var sl = s_len(s);\r\n" +
                                        "(s + ` `) * sl"}),
            ("treenode_click", new object[]{new string[] {"27,42 : [3]", "0 : \"boo boo boo \""} }),
            ("compare_text", new object[]{"[0.25,0.75,0.5]blah[0,1.25][\"boo\",\"a\",\"c\"]"}),
            ("tree_query", new object[]{"var this_s = @[:]{@, str(@)};\r\n" +
                                        "var s_lens = s_len(this_s[:][1]);\r\n" +
                                        "var min_slen = min(s_lens);\r\n" +
                                        "var shortest_strs = this_s[s_lens == min_slen][0]"}),
            ("compare_text", new object[]{"[0.25,0.75,0.5]blah[0,1.25][\"boo\",\"a\",\"c\"]"}),
            ("treenode_click", new object[]{ new string[] { "0,15 : [1]", "0 : 0.5"} }),
            ("compare_selections", new object[]{new string[]{"11,11" } }),
            ("treenode_click", new object[]{ new string[] { "27,42 : [2]", "1 : \"c\""} }),
            ("compare_selections", new object[]{new string[]{"38,38" } }),
            // TEST MULTI-STATEMENT QUERY THAT *DOES* MUTATE ON A FILE WITH SELECTIONS
            ("tree_query", new object[]{"var this_s = @[:]{@, str(@)};\r\n" +
                                        "var s_lens = s_len(this_s[:][1]);\r\n" +
                                        "var max_slen = max(s_lens);\r\n" +
                                        "var longest_strs = this_s[s_lens == max_slen][0];\r\n" +
                                        "longest_strs = str(@) + ` is champion!`;\r\n" +
                                        "longest_strs"}),
            ("compare_text", new object[]{"[\r\n    \"0.25 is champion!\",\r\n    \"0.75 is champion!\",\r\n    0.5\r\n]blah[\r\n    0,\r\n    \"1.25 is champion!\"\r\n][\r\n    \"boo is champion!\",\r\n    \"a\",\r\n    \"c\"\r\n]"}),
            ("treenode_click", new object[]{ new string[] { "69,106 : [1]", "0 : \"1.25 is champion!\""} }),
            ("treenode_click", new object[]{ new string[] { "106,154 : [1]", "0 : \"boo is champion!\""} }),
            ("tree_compare_query_result", new object[]{"{\"0,65\": [\"0.25 is champion!\", \"0.75 is champion!\"], \"106,154\": [\"boo is champion!\"], \"69,106\": [\"1.25 is champion!\"]}"}),
        };

        public static bool Test()
        {
            var messages = new List<string>();
            int failures = 0;
            string previouslyOpenFname = Npp.notepad.GetCurrentFilePath();
            filenamesUsed = new List<string>();
            string UITestFileName = OpenUITestFile(0);

            PrettyPrintStyle previousPrettyPrintStyle = Main.settings.pretty_print_style;
            string previousTryParseStartChars = Main.settings.try_parse_start_chars;
            bool previousTabIndentPrettyPrint = Main.settings.tab_indent_pretty_print;
            int previousIndentPrettyPrint = Main.settings.indent_pretty_print;
            bool previousMinimalWhiteSpaceCompression = Main.settings.minimal_whitespace_compression;
            int previousMaxTrackedJsonSelections = Main.settings.max_tracked_json_selections;
            bool previousRememberComments = Main.settings.remember_comments;
            bool previousHasWarnedSelectionsForgotten = Main.hasWarnedSelectionsForgotten;
            // require these settings for the UI tests alone
            Main.settings.pretty_print_style = PrettyPrintStyle.PPrint;
            Main.settings.try_parse_start_chars = "\"[{";
            Main.settings.tab_indent_pretty_print = false;
            Main.settings.indent_pretty_print = 4;
            Main.settings.minimal_whitespace_compression = true;
            Main.settings.max_tracked_json_selections = 1000;
            Main.settings.remember_comments = false;
            // if this is false, a message-box will pop up at some point.
            // this message box doesn't block the main thread, but it introduces some asynchronous behavior
            // that was probably responsible for crashing the UI tests
            Main.hasWarnedSelectionsForgotten = true;
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
            // go back to the test file and show the results
            Npp.notepad.OpenFile(previouslyOpenFname);
            if (failures > 0)
            {
                // show all the messages up to the last failure
                Npp.AddLine(string.Join("\r\n", messages.LazySlice(0, lastFailureIndex)));
            }
            Npp.AddLine($"Failed {failures} tests");
            Npp.AddLine($"Passed {testcases.Count - failures} tests");
            // restore old settings
            Main.settings.pretty_print_style = previousPrettyPrintStyle;
            Main.settings.try_parse_start_chars = previousTryParseStartChars;
            Main.settings.indent_pretty_print = previousIndentPrettyPrint;
            Main.settings.tab_indent_pretty_print = previousTabIndentPrettyPrint;
            Main.settings.minimal_whitespace_compression = previousMinimalWhiteSpaceCompression;
            Main.settings.max_tracked_json_selections = previousMaxTrackedJsonSelections;
            Main.settings.remember_comments = previousRememberComments;
            Main.hasWarnedSelectionsForgotten = previousHasWarnedSelectionsForgotten;
            return failures > 0;
        }

        private static string UITestFilename(int ii) { return $"UI test {ii}.json"; }

        private static string OpenUITestFile(int fileIdx)
        {
            while (fileIdx >= filenamesUsed.Count)
            {
                string newFilename = UITestFilename(lowestFilenameNumberNotUsed);
                if (Npp.notepad.GetOpenFileNames().Contains(newFilename))
                {
                    lowestFilenameNumberNotUsed++;
                    continue;
                }
                Npp.notepad.FileNew();
                Npp.notepad.SetCurrentBufferInternalName(newFilename);
                filenamesUsed.Add(newFilename);
            }
            string filename = filenamesUsed[fileIdx];
            Npp.notepad.OpenFile(filename);
            return filename;
        }
    }
}
