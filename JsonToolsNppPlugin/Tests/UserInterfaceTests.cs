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

        public static string lastClipboardValue = null;

        private static RemesParser remesParser = new RemesParser();

        private static JsonParser jsonParser = new JsonParser();

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
                string ext = (args.Length > 1 && args[1] is string extension) ? extension : "json";
                string filename = OpenUITestFile(fileIdx, ext);
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
                bool tabIndent = args.Length >= 1 && args[0] is bool && (bool)args[0];
                bool rememberComments = args.Length >= 2 && args[1] is bool && (bool)args[1];
                bool previousTabIndent = Main.settings.tab_indent_pretty_print;
                bool previousRememberComments = Main.settings.remember_comments;
                PrettyPrintStyle previousPrettyPrintStyle = Main.settings.pretty_print_style;
                if (args.Length >= 3 && args[2] is PrettyPrintStyle pps)
                {
                    Main.settings.pretty_print_style = pps;
                }
                if (tabIndent)
                    Main.settings.tab_indent_pretty_print = tabIndent;
                if (rememberComments)
                    Main.settings.remember_comments = rememberComments;
                Main.PrettyPrintJson();
                messages.Add($"{Main.settings.pretty_print_style}-style" + (tabIndent
                    ?  rememberComments ? "pretty-print with tabs and comments" : "pretty-print with tabs"
                    : rememberComments ? "pretty-print with comments" : "pretty-print"));
                if (tabIndent)
                    Main.settings.tab_indent_pretty_print = previousTabIndent;
                if (rememberComments)
                    Main.settings.remember_comments = previousRememberComments;
                if (Main.settings.pretty_print_style != previousPrettyPrintStyle)
                    Main.settings.pretty_print_style = previousPrettyPrintStyle;
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
                string gotText = Npp.editor.GetText();
                if (correctText != gotText)
                {
                    messages.Add($"FAIL: expected text\r\n{correctText}\r\nGOT\r\n{gotText}");
                    return true;
                }
                messages.Add("compare_text passed");
                break;
            case "tree_open":
                DocumentType documentType = args.Length >= 1 ? (DocumentType)args[0] : DocumentType.JSON;
                Main.OpenJsonTree(documentType);
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
                        remesParser.Search(query, copyJson);
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
                lastClipboardValue = gotPathtoCurrentPosition;
                if (gotPathtoCurrentPosition != correctPathToCurrentPosition)
                    messages.Add($"FAIL: Expected path to position {position} to be {correctPathToCurrentPosition}, but got {gotPathtoCurrentPosition}");
                else
                    messages.Add($"Passed test of path to position {position}");
                break;
            case "regex_search":
                if (Main.regexSearchForm == null)
                    Main.RegexSearchToJson();
                string regexSearchFormSettingsAsJObjectStr = (string)args[0];
                JNode regexSearchFormSettings = jsonParser.Parse(regexSearchFormSettingsAsJObjectStr);
                if (Main.regexSearchForm.SetFieldsFromJson(regexSearchFormSettings, false, out string regexSettingsErrorMsg))
                    messages.Add($"Opened regex search form with settings {regexSearchFormSettingsAsJObjectStr}");
                else
                    messages.Add(regexSettingsErrorMsg);
                Main.regexSearchForm.SearchButton.PerformClick();
                break;
            case "set_document_type":
                string newDocumentTypeName = (string)args[0];
                if (!hasTreeView)
                {
                    messages.Add($"FAIL: Wanted to set the document type to {newDocumentTypeName} with the treeview's combo box, but the treeview wasn't open");
                    return true;
                }
                switch (newDocumentTypeName)
                {
                case "NONE":
                case "JSON":
                    Main.openTreeViewer.SetDocumentTypeComboBoxIndex(DocumentType.JSON);
                    break;
                case "JSONL": Main.openTreeViewer.SetDocumentTypeComboBoxIndex(DocumentType.JSONL); break;
                case "INI": Main.openTreeViewer.SetDocumentTypeComboBoxIndex(DocumentType.INI); break;
                case "REGEX": Main.openTreeViewer.SetDocumentTypeComboBoxIndex(DocumentType.REGEX); break;
                }
                messages.Add($"Set document type to {newDocumentTypeName} with the treeview's combo box");
                break;
            default:
                throw new ArgumentException($"Unrecognized command {command}");
            }
            return false;
        }

        public static bool Test()
        {
            List<(string command, object[] args)> testcases = new List<(string command, object[] args)>
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
                ("overwrite", new object[]{"[\r\n    [\"Я\", 1, \"a\"], // foo\r\n    [\"◐\", 2, \"b\"], // bar\r\n    [\"ồ\", 3, \"c\"], // baz\r\n    [\"ｪ\", 4, \"d\"],\r\n    [\"草\", 5, \"e\"],\r\n    [\"😀\", 6, \"f\"]\r\n]//a"}),
                // TEST PRETTY-PRINT WITH COMMENTS AND TABS
                ("pretty_print", new object[]{true, true, PrettyPrintStyle.Google}),
                ("compare_text", new object[]{"[\r\n\t[\r\n\t\t\"Я\",\r\n\t\t1,\r\n\t\t\"a\"\r\n\t],\r\n\t// foo\r\n\t[\r\n\t\t\"◐\",\r\n\t\t2,\r\n\t\t\"b\"\r\n\t],\r\n\t// bar\r\n\t[\r\n\t\t\"ồ\",\r\n\t\t3,\r\n\t\t\"c\"\r\n\t],\r\n\t// baz\r\n\t[\r\n\t\t\"ｪ\",\r\n\t\t4,\r\n\t\t\"d\"\r\n\t],\r\n\t[\r\n\t\t\"草\",\r\n\t\t5,\r\n\t\t\"e\"\r\n\t],\r\n\t[\r\n\t\t\"😀\",\r\n\t\t6,\r\n\t\t\"f\"\r\n\t]\r\n]\r\n//a\r\n"}),
                // TEST PRETTY-PRINT WITH COMMENTS ONLY
                ("pretty_print", new object[]{false, true, PrettyPrintStyle.Whitesmith}),
                ("compare_text", new object[]{"[\r\n    [\r\n        \"Я\",\r\n        1,\r\n        \"a\"\r\n    ],\r\n    // foo\r\n    [\r\n        \"◐\",\r\n        2,\r\n        \"b\"\r\n    ],\r\n    // bar\r\n    [\r\n        \"ồ\",\r\n        3,\r\n        \"c\"\r\n    ],\r\n    // baz\r\n    [\r\n        \"ｪ\",\r\n        4,\r\n        \"d\"\r\n    ],\r\n    [\r\n        \"草\",\r\n        5,\r\n        \"e\"\r\n    ],\r\n    [\r\n        \"😀\",\r\n        6,\r\n        \"f\"\r\n    ]\r\n]\r\n//a\r\n"
    }),
                // TEST PRETTY-PRINT WITH TABS ONLY
                ("pretty_print", new object[]{true}),
                ("compare_text", new object[]{"[\r\n\t[\"Я\", 1, \"a\"],\r\n\t[\"◐\", 2, \"b\"],\r\n\t[\"ồ\", 3, \"c\"],\r\n\t[\"ｪ\", 4, \"d\"],\r\n\t[\"草\", 5, \"e\"],\r\n\t[\"😀\", 6, \"f\"]\r\n]"}),
                // TEST TREE ON WHOLE DOCUMENT
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
                // TEST THAT JAVASCRIPT AND PYTHON COMMENTS AT EOF DON'T CAUSE PROBLEMS
                ("overwrite", new object[]{"\r\n[1,2,\r\n-]#"}),
                ("compress", new object[]{}),
                ("compare_text", new object[]{"[1,2,NaN]"}),
                ("overwrite", new object[]{"[1,2,+ ]#"}),
                ("compress", new object[]{}),
                ("compare_text", new object[]{"[1,2,NaN]"}),
                ("overwrite", new object[]{"[1\r\n  ,2,\r\n3]//"}),
                ("compress", new object[]{}),
                ("compare_text", new object[]{"[1,2,3]"}),
                ("overwrite", new object[]{"[1,2,\r\n3]#bar"}),
                ("compress", new object[]{}),
                ("compare_text", new object[]{"[1,2,3]"}),
                ("overwrite", new object[]{"[1,2,-9e15]\r\n//foo"}),
                ("compress", new object[]{}),
                ("compare_text", new object[]{"[1,2,-9E+15]"}),
                // TEST PARSE JSON LINES
                ("overwrite", new object[]{"[1,2,3]\r\n{\"a\": 1, \"b\": [-3,-4]}\r\n-7\r\nfalse"}),
                ("tree_open", new object[]{}), // to close the tree so it can be reopened
                ("tree_open", new object[]{DocumentType.JSONL}),
                ("treenode_click", new object[]{new string[] {} }),
                ("select_treenode_json_children", new object[]{}),
                ("compare_selections", new object[]{new string[] {"0,7", "9,31", "33,35", "37,42"} }),
                ("tree_query", new object[]{"@..* [abs(+@) < 3] = @ / 4"}), // divide values in open range (-3, 3) by 4; this should keep the file in JSON Lines format
                ("compare_text", new object[]{"[0.25,0.5,3]\n{\"a\":0.25,\"b\":[-3,-4]}\n-7\n0.0"}),
                ("tree_query", new object[]{"@"}), // re-parse document
                ("treenode_click", new object[]{new string[] {"3 : 0.0"} }),
                ("compare_selections", new object[]{new string[] {"39,39"} }),
                ("compare_path_to_position", new object[]{32, "[1].b[1]"}),
                // TEST PARSE INI FILE
                ("overwrite", new object[]{"[foồ]\r\n;a\r\nfoo=1\r\n[bar]\r\n  bar=2\r\n  [дaz]\r\n  baz=3\r\n  ;b\r\n  baz2 = 7 \r\n[quz]\r\nquz=4\r\n;c"}),
                ("set_document_type", new object[]{"INI"}),
                ("tree_query", new object[]{"@..g`z`"}),
                ("treenode_click", new object[]{new string[] {"0 : {2}", "baz2 : \"7 \""} }),
                ("compare_selections", new object[]{new string[]{"63,63" } }),
                ("compare_path_to_position", new object[]{81, ".quz.quz"}),
                ("tree_query", new object[]{"@.bar.bar = @ * int(@)"}), // edit one value
                ("compare_text", new object[]{"[foồ]\r\n;a\r\nfoo=1\r\n[bar]\r\nbar=22\r\n[дaz]\r\nbaz=3\r\n;b\r\nbaz2=7 \r\n[quz]\r\nquz=4\r\n;c\r\n"}),
                // TEST RUNNING SAME QUERY MULTIPLE TIMES ON SAME INPUT DOES NOT HAVE DIFFERENT RESULTS
                // test when mutating a compile-time constant array
                ("tree_query", new object[]{"var onetwo = j`[1,1]`; onetwo[1] = @ + 1; onetwo"}),
                ("tree_query", new object[]{"var onetwo = j`[1,1]`; onetwo[1] = @ + 1; onetwo"}),
                ("treenode_click", new object[]{new string[] {"1 : 2"} }),
                // test when mutating a compile-time constant loop variable
                ("tree_query", new object[]{"for onetwo = j`[1,1]`; onetwo = @ + 1; end for;"}),
                ("tree_query", new object[]{"for onetwo = j`[1,1]`; onetwo = @ + 1; end for;"}),
                ("treenode_click", new object[]{new string[] {"0 : 2"} }),
                ("treenode_click", new object[]{new string[] {"1 : 2"} }),
                // test when mutating projections (both object and array)
                ("tree_query", new object[]{"var onetwo = @{1, 1}; var mappy = @{a: 1}; onetwo[1] = @ + 1; mappy.a = @ + 1; @{onetwo, mappy}"}),
                ("tree_query", new object[]{"var onetwo = @{1, 1}; var mappy = @{a: 1}; onetwo[1] = @ + 1; mappy.a = @ + 1; @{onetwo, mappy}"}),
                ("treenode_click", new object[]{new string[] {"0 : [2]", "1 : 2"} }),
                ("treenode_click", new object[]{new string[] {"1 : {1}", "a : 2"} }),
                // test when mutating map projection
                ("tree_query", new object[]{"var x = @->j`[-1]`; x[0] = @ + 1; x"}),
                ("tree_query", new object[]{"var x = @->j`[-1]`; x[0] = @ + 1; x"}),
                ("treenode_click", new object[]{new string[] {"0 : 0"} }),
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
                ("treenode_click", new object[]{new string[] {} }),
                ("select_treenode_json_children", new object[]{}),
                ("compare_selections", new object[]{new string[] {"0,15", "15,27", "29,44"} }), // test selecting all remembered selections from tree root in JSON mode
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
                ("tree_query", new object[]{"j`{\"a\": \"foo\", \"b\\n\": [1, 2]}`"}),
                ("treenode_click", new object[]{new string[] {"a : \"foo\""} }),
                ("treenode_click", new object[]{new string[] {"b\\n : [2]", "1 : 2"} }),
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
                // TEST PRINTING SELECTION-BASED FILE WITH TABS
                ("pretty_print", new object[]{true}),
                ("compare_text", new object[]{"[\r\n\t\"0.25 is champion!\",\r\n\t\"0.75 is champion!\",\r\n\t0.5\r\n]blah[\r\n\t0,\r\n\t\"1.25 is champion!\"\r\n][\r\n\t\"boo is champion!\",\r\n\t\"a\",\r\n\t\"c\"\r\n]"}),
                // TEST QUERIES WITH LOOP VARIABLES ON SELECTION-BASED FILE
                ("tree_query", new object[]{// find all the strings in the array, then add the i^th string in the array to the i^th-from-last string in the array
                                            "var strs = @[:][is_str(@)];\r\n" +
                                            "var strs_cpy = str(strs);\r\n" +
                                            // need to iterate through (the i^th string, a *copy* of the i^th string, and a *copy* of the i^th-from-last string), 
                                            // because if we don't we will mutate some strings multiple times due to the in-place nature of mutation
                                            "for s = zip(strs, strs_cpy, strs_cpy[::-1]);\r\n" +
                                            "    s[0] = s[1] + s[2];\r\n" +
                                            "end for"
                                            }),
                ("compare_text", new object[]{"[\r\n    \"0.25 is champion!0.75 is champion!\",\r\n    \"0.75 is champion!0.25 is champion!\",\r\n    0.5\r\n]blah[\r\n    0,\r\n    \"1.25 is champion!1.25 is champion!\"\r\n][\r\n    \"boo is champion!c\",\r\n    \"aa\",\r\n    \"cboo is champion!\"\r\n]"}),
                // TEST REGEX SEARCH FORM (REGEX MODE)
                ("file_open", new object[]{2, "txt"}),
                ("overwrite", new object[]{"\u200a\u202f\n foo: 1ö\nBA\u042f: \u00a0-3\nbaz: +85"}),
                ("regex_search", new object[]{
                    "{\"csv\":false,\"regex\":\"^\\\\x20*([a-zЯ]+):\\\\s+(INT)ö?\",\"ignoreCase\":true,\"fullMatch\":false,\"numCols\":[1]}"
                }),
                ("treenode_click", new object[]{new string[] {"0 : [2]"} }),
                ("compare_selections", new object[]{new string[] {"7,7"} }),
                ("treenode_click", new object[]{new string[] {"1 : [2]", "1 : -3"} }),
                ("compare_selections", new object[]{new string[] {"25,25"} }),
                ("select_treenode_json", new object[]{}),
                ("compare_selections", new object[]{new string[] {"25,27"} }),
                ("treenode_click", new object[]{new string[] {"2 : [2]", "1 : 85"} }),
                ("compare_selections", new object[]{new string[] {"33,33"} }),
                ("select_treenode_json", new object[]{}),
                ("compare_selections", new object[]{new string[] {"33,36"} }),
                ("regex_search", new object[]{
                    "{\"csv\":false,\"regex\":\"^\\\\x20*([a-zЯ]+):\\\\s+(NUMBER)\",\"ignoreCase\":false,\"fullMatch\":true}"
                }),
                ("treenode_click", new object[]{new string[] {"1 : [3]", "0 : \"baz: +85\""} }),
                ("treenode_click", new object[]{new string[] {"0 : [3]", "1 : \"foo\""} }),
                ("treenode_click", new object[]{new string[] {"0 : [3]", "2 : \"1\""} }),
                ("tree_query", new object[]{
                    "@ = s_sub(@,\r\n" +
                     "    g`(?i)^\\x20*([a-zЯ]+):\\s+(INT)ö?`,\r\n" +
                     "    ifelse(float(@[2]) < 0,\r\n" +
                     "        str(loop()) + @[0],\r\n" +
                     "        f`{loop()}. {@[1]} ({int(@[2])})`\r\n" +
                     "    )\r\n" +
                     ")"}),
                ("compare_text", new object[]{"\u200a\u202f\n1. foo (1)\n2BA\u042f: \u00a0-3\n3. baz (85)"}),
                // TEST REGEX SEARCH FORM (CSV MODE)
                ("overwrite", new object[]{
                    "ab\tc\t😀\tö\r\n" +
                    "'fo\nö'\t7\tbar\t-8.5\r\n" +
                    "baz\t19\t''\t-4e3\r\n" +
                    "'zorq'\t\tkywq\t'.75'\r\n" +
                    "\t\t\t"
                }),
                ("regex_search", new object[]
                {
                    "{\"csv\":true,\"delim\":\"\\\\t\",\"quote\":\"'\",\"newline\":\"\\r\\n\",\"header\":\"d\",\"nColumns\":4,\"numCols\":[-1,1]}"
                }),
                ("treenode_click", new object[]{new string[]{"0 : {4}", "ab : \"fo\\nö\"" } }),
                ("treenode_click", new object[]{new string[]{"1 : {4}", "c : 19"} }),
                ("treenode_click", new object[]{new string[]{"1 : {4}", "😀 : \"\"" } }),
                ("select_treenode_json", new object[]{}),
                ("compare_selections", new object[]{new string[] {"41,43"} }),
                ("treenode_click", new object[]{new string[]{"2 : {4}", "ö : \".75\"" } }),
                ("compare_selections", new object[]{new string[] {"63,63"} }),
                ("regex_search", new object[]
                {
                    "{\"csv\": true, \"delim\": \"\\\\t\", \"quote\": \"'\", \"newline\": \"\\r\\n\", \"header\": 1, \"nColumns\": 4, \"numCols\": [1]}"
                }),
                ("treenode_click", new object[]{new string[] {"0 : [4]", "3 : \"ö\"" } }),
                ("treenode_click", new object[]{new string[] {"1 : [4]", "1 : 7"} }),
                ("treenode_click", new object[]{new string[] {"2 : [4]", "3 : \"-4e3\""} }),
                ("treenode_click", new object[]{new string[] {"3 : [4]", "0 : \"zorq\""} }),
                ("treenode_click", new object[]{new string[] {"4 : [4]", "2 : \"\""} }),
                ("select_treenode_json", new object[]{}),
                ("compare_selections", new object[]{new string[] {"72,72"} }),
                // TEST SELECT ALL CHILDREN IN CSV FILE
                ("tree_query", new object[]{"s_csv(@, 4, `\\t`, `\\r\\n`, `'`, h)[:][0]"}),
                ("treenode_click", new object[]{new string[]{ } }), // click root
                ("select_treenode_json_children", new object[]{}),
                ("compare_selections", new object[]{new string[] {"0,2", "14,21", "34,37", "50,56", "70,70"} }),
                ("treenode_click", new object[]{new string[]{ "1 : \"fo\\nö\"" } }),
                ("select_treenode_json", new object[]{}),
                ("compare_selections", new object[]{new string[] {"14,21"} }),
                // MUTATE CSV FILE WITH QUERY
                ("tree_query", new object[]{
                    "var c = s_csv(@, 4, `\\t`, `\\r\\n`, `'`, h, 1);\r\n" +
                    "c[:][1][is_num(@)] = @/4;\r\n" +
                    "@ = to_csv(c,`,`, `\\n`, `\"`)"}),
                ("compare_text", new object[]{"ab,c,😀,ö\n\"fo\nö\",1.75,bar,-8.5\nbaz,4.75,,-4e3\nzorq,,kywq,.75\n,,,\n"}),
                // TEST REGEX SEARCH FORM (SINGLE-CAPTURE-GROUP CSV AND REGEX MODES)
                ("overwrite", new object[]{" 草1\nｪ2\n◐3\n'4"}), // last row has CSV quote char to make sure CSV delimiter is forgotten now that a non-csv search was made
                ("regex_search", new object[]{
                    "{\"csv\":false,\"regex\":\"^\\\\S\\\\d\\\\r?$\",\"ignoreCase\":true,\"fullMatch\":false}"
                }),
                ("treenode_click", new object[]{new string[]{ } }), // click root
                ("select_treenode_json_children", new object[]{}),
                ("compare_selections", new object[]{new string[] {"6,10", "11,15", "16,18"} }),
                ("treenode_click", new object[]{new string[] { "1 : \"◐3\"" } }),
                ("compare_selections", new object[]{new string[] {"11,11"} }),
                ("select_treenode_json", new object[]{}),
                ("compare_selections", new object[]{new string[] {"11,15"} }),
                ("regex_search", new object[]{
                    "{\"csv\":false,\"regex\":\"^\\\\S(\\\\d)\\\\r?$\",\"ignoreCase\":true,\"fullMatch\":false}" // this time capture just the number after the nonspace
                }),
                ("treenode_click", new object[]{new string[] { "1 : \"3\"" } }),
                ("compare_selections", new object[]{new string[] {"14,14"} }),
                ("regex_search", new object[]
                {
                    "{\"csv\": true, \"delim\": \",\", \"quote\": \"'\", \"newline\": \"\\n\", \"header\": 0, \"nColumns\": 1}"
                }),
                ("treenode_click", new object[]{new string[] { "0 : \"ｪ2\"" } }),
                ("compare_selections", new object[]{new string[] {"6,6"} }),
                ("regex_search", new object[]
                {
                    "{\"csv\": true, \"delim\": \",\", \"quote\": \"'\", \"newline\": 1, \"header\": 2, \"nColumns\": 1}"
                }),
                ("treenode_click", new object[]{new string[] { "0 : {1}", " 草1 : \"ｪ2\"" } }),
                ("compare_selections", new object[]{new string[] {"6,6"} }),
                // TEST REGEX SEARCH ON FILE WITH MULTIPLE SELECTIONS
                ("overwrite", new object[]{"1 2\r[3]ö+4\r\"\u00a0\r+5.5 -0xa6F\r\"fo|\""}),
                ("tree_query", new object[]{"s_csv(@, 1, `|`, `\\r`, `\"`, h)"}),
                ("treenode_click", new object[]{new string[] {} }),
                ("select_treenode_json_children", new object[]{}),
                ("compare_selections", new object[]{new string[] {"0,3", "4,11", "16,27", "28,33"} }),
                ("tree_open", new object[]{}),
                ("tree_open", new object[]{}),
                ("set_document_type", new object[]{ "REGEX" }), // re-parse document in multi-selection mode
                ("treenode_click", new object[]{new string[]{ "16,27 : \"+5.5 -0xa6F\"" } }),
                ("compare_selections", new object[]{new string[] {"16,16"} }),
                ("tree_query", new object[]{"s_fa(@, g`(?:NUMBER)`,, 0)"}),
                ("treenode_click", new object[]{ new string[] { "4,11 : [2]", "1 : 4" } }),
                ("select_treenode_json", new object[]{}),
                ("compare_selections", new object[]{new string[]{"9,11" } }),
                ("treenode_click", new object[]{ new string[] { "16,27 : [2]" } }),
                ("select_treenode_json_children", new object[]{}),
                ("compare_selections", new object[]{new string[]{"16,20", "21,27" } }),
                ("treenode_click", new object[]{new string[] {} }),
                ("select_treenode_json_children", new object[]{}), // now test selecting all remembered selections from tree root in regex mode
                ("compare_selections", new object[]{new string[] {"0,3", "4,11", "16,27", "28,33"} }),
                // TEST REGEX EDITING ON FILE WITH MULTIPLE SELECTIONS
                ("select", new object[]{new string[] {"0,0"} }),
                ("tree_query", new object[]{ "@ = s_sub(@, g`(?:NUMBER)`, str(num(@[0])->ifelse(@ > 3, -@, @)))" }),
                ("compare_text", new object[]{"1.0 2.0\r[3.0]ö-4.0\r\"\u00a0\r-5.5 -2671.0\r\"fo|\"" }),
                ("compare_selections", new object[]{new string[] {"0,7", "8,19", "24,36", "37,42"} }),
                // TEST DOCUMENT TYPE CHANGE BUTTON IN TREEVIEW
                ("overwrite", new object[]{"[1, 2]\r\n{\"ö\": \"3 4\"}\r\n\"\"\r\n{\"5\": [6]}\r\n'foo'"}),
                ("set_document_type", new object[]{"JSON"}),
                ("treenode_click", new object[]{new string[] {"0 : 1"} }),
                ("treenode_click", new object[]{new string[] {"1 : 2"} }),
                ("set_document_type", new object[]{"JSONL"}),
                ("treenode_click", new object[]{new string[] {"3 : {1}", "5 : [1]", "0 : 6"} }),
                ("compare_selections", new object[]{new string[] {"34,34"} }),
                ("treenode_click", new object[]{new string[] {"4 : \"foo\""} }),
                ("set_document_type", new object[]{"REGEX"}),
                ("tree_query", new object[]{"s_csv(@, 1,`\\t`,`\\r\\n`,`'` ,h)"}),
                ("treenode_click", new object[]{new string[] { "1 : \"{\\\"ö\\\": \\\"3 4\\\"}\"" } }),
                ("treenode_click", new object[]{new string[] { "3 : \"{\\\"5\\\": [6]}\"" } }),
            };

            var messages = new List<string>();
            int failures = 0;
            string previouslyOpenFname = Npp.notepad.GetCurrentFilePath();
            filenamesUsed = new List<string>();
            string UITestFileName = OpenUITestFile(0, "json");

            PrettyPrintStyle previousPrettyPrintStyle = Main.settings.pretty_print_style;
            bool previousSortKeys = Main.settings.sort_keys;
            string previousTryParseStartChars = Main.settings.try_parse_start_chars;
            bool previousTabIndentPrettyPrint = Main.settings.tab_indent_pretty_print;
            int previousIndentPrettyPrint = Main.settings.indent_pretty_print;
            bool previousMinimalWhiteSpaceCompression = Main.settings.minimal_whitespace_compression;
            bool previousRememberComments = Main.settings.remember_comments;
            bool previousHasWarnedSelectionsForgotten = Main.hasWarnedSelectionsForgotten;
            bool previousOfferToShowLint = Main.settings.offer_to_show_lint;
            // remember what the user's clipboard was before tests start, because the tests hijack the clipboard and that's not nice
            string clipboardValueBeforeTests = Clipboard.GetText();
            // require these settings for the UI tests alone
            Main.settings.pretty_print_style = PrettyPrintStyle.PPrint;
            Main.settings.sort_keys = true;
            Main.settings.try_parse_start_chars = "\"[{";
            Main.settings.tab_indent_pretty_print = false;
            Main.settings.indent_pretty_print = 4;
            Main.settings.minimal_whitespace_compression = true;
            Main.settings.remember_comments = false;
            Main.settings.offer_to_show_lint = false;
            // if this is false, a message-box will pop up at some point.
            // this message box doesn't block the main thread, but it introduces some asynchronous behavior
            // that was probably responsible for crashing the UI tests
            Main.hasWarnedSelectionsForgotten = true;
            // add command to overwrite with a lot of arrays and select every valid json
            try
            {
                string oneArray2000xStr = (string)remesParser.Search("@ * 2000", new JNode("[1]\r\n")).value;
                JArray oneArray2000x = (JArray)jsonParser.ParseJsonLines(oneArray2000xStr);
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
                    messages.Add("FAIL: While running command " + command + " with args [" + string.Join(", ", args) + "], got exception\r\n" + ex);
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
            Main.settings.sort_keys = previousSortKeys;
            Main.settings.try_parse_start_chars = previousTryParseStartChars;
            Main.settings.indent_pretty_print = previousIndentPrettyPrint;
            Main.settings.tab_indent_pretty_print = previousTabIndentPrettyPrint;
            Main.settings.minimal_whitespace_compression = previousMinimalWhiteSpaceCompression;
            Main.settings.remember_comments = previousRememberComments;
            Main.hasWarnedSelectionsForgotten = previousHasWarnedSelectionsForgotten;
            Main.settings.offer_to_show_lint = previousOfferToShowLint;
            // if the user's clipboard is still set to whatever we most recently hijacked it with, reset it to whatever it was before the tests
            // this won't work if their clipboard contained non-text data beforehand, but it's better than nothing
            if (Clipboard.GetText() == lastClipboardValue && !(clipboardValueBeforeTests is null) && clipboardValueBeforeTests.Length > 0)
            {
                Npp.TryCopyToClipboard(clipboardValueBeforeTests);
            }
            return failures > 0;
        }

        private static string UITestFilename(int ii, string extension) { return $"UI test {ii}.{extension}"; }

        private static string OpenUITestFile(int fileIdx, string extension)
        {
            while (fileIdx >= filenamesUsed.Count)
            {
                string newFilename = UITestFilename(lowestFilenameNumberNotUsed, extension);
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
