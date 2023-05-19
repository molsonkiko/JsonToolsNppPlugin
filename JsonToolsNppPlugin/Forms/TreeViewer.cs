using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;

namespace JSON_Tools.Forms
{
    public partial class TreeViewer : Form
    {
        /// <summary>
        /// the name of the file holding the JSON that this is associated with
        /// </summary>
        public string fname;
        
        /// <summary>
        /// Maps the path of a TreeNode to the corresponding JNode
        /// </summary>
        public Dictionary<string, JNode> pathsToJNodes;

        /// <summary>
        /// result of latest RemesPath query
        /// </summary>
        public JNode query_result;

        public JNode json;

        public RemesPathLexer lexer;

        public RemesParser remesParser;

        public JsonSchemaMaker schemaMaker;

        public FindReplaceForm findReplaceForm;

        // event handlers for the node mouseclick drop down menu
        private static MouseEventHandler valToClipboardHandler = null;
        private static MouseEventHandler pathToClipboardHandler_Remespath = null;
        private static MouseEventHandler pathToClipboardHandler_Python = null;
        private static MouseEventHandler pathToClipboardHandler_Javascript = null;
        private static MouseEventHandler keyToClipboardHandler_Remespath = null;
        private static MouseEventHandler keyToClipboardHandler_Python = null;
        private static MouseEventHandler keyToClipboardHandler_Javascript = null;
        private static MouseEventHandler ToggleSubtreesHandler = null;

        public TreeViewer(JNode json)
        {
            InitializeComponent();
            pathsToJNodes = new Dictionary<string, JNode>();
            fname = Npp.notepad.GetCurrentFilePath();
            this.json = json;
            query_result = json;
            remesParser = new RemesParser();
            lexer = new RemesPathLexer();
            findReplaceForm = null;
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
        }

        /// <summary>
        /// suppress annoying ding when user hits a key in the tree view
        /// </summary>
        private void Tree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape || e.KeyCode == Keys.Tab || e.KeyCode == Keys.Space)
                e.SuppressKeyPress = true;
        }

        // largely copied from NppManagedPluginDemo.cs in the original plugin pack
        private void TreeViewer_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || (e.KeyCode == Keys.Space && sender is TreeView))
            {
                // Ctrl+Enter in query box -> submit query
                if (e.Control && QueryBox.Focused)
                    SubmitQueryButton.PerformClick();
                else if (sender is Button btn)
                {
                    // Enter has the same effect as clicking a selected button
                    btn.PerformClick();
                }
                else if (sender is TreeView)
                {
                    // Enter or Space in the TreeView toggles children of the selected node
                    TreeNode selected = Tree.SelectedNode;
                    if (selected == null || selected.Nodes.Count == 0)
                        return;
                    if (selected.IsExpanded)
                        selected.Collapse(true); // don't collapse the children as well
                    else selected.Expand();
                }
            }
            // Escape -> go to editor
            else if (e.KeyData == Keys.Escape)
            {
                Npp.editor.GrabFocus();
            }
            // Tab -> go through controls, Shift+Tab -> go through controls backward
            else if (e.KeyCode == Keys.Tab)
            {
                Control next = GetNextControl((Control)sender, !e.Shift);
                while ((next == null) || (!next.TabStop)) next = GetNextControl(next, !e.Shift);
                next.Focus();
            }
        }

        private void QueryBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
                e.Handled = true;
        }

        public static void SetImageOfTreeNode(TreeNode root, JNode json)
        {
            switch (json.type)
            {
                case Dtype.ARR: root.ImageIndex = 0; root.SelectedImageIndex = 0; break;
                case Dtype.BOOL: root.ImageIndex = 1; root.SelectedImageIndex = 1; break;
                case Dtype.DATE:
                case Dtype.DATETIME: root.ImageIndex = 2; root.SelectedImageIndex = 2; break;
                case Dtype.FLOAT: root.ImageIndex = 3; root.SelectedImageIndex = 3; break;
                case Dtype.INT: root.ImageIndex = 4; root.SelectedImageIndex = 4; break;
                case Dtype.OBJ: root.ImageIndex = 5; root.SelectedImageIndex = 5; break;
                case Dtype.STR: root.ImageIndex = 6; root.SelectedImageIndex = 6; break;
                default: root.ImageIndex = 7; root.SelectedImageIndex = 7; break;
            }
        }

        public void JsonTreePopulate(JNode json, TreeView tree = null)
        {
            if (tree == null) tree = this.Tree;
            if (json == null)
            {
                MessageBox.Show("Cannot populate the JSON tree because no JSON is stored.",
                    "Can't populate JSON tree",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            if (tree.ImageList == null && Main.settings.tree_node_images)
            {
                tree.ImageList = TypeIconList;
                /* type indices are as follows:
                0: array
                1: bool
                2: date or datetime
                3: float
                4: int
                5: object
                6: string
                7: everything else
                */
            }
            tree.Nodes.Clear();
            TreeNode root = new TreeNode();
            if (Main.settings.tree_node_images)
                SetImageOfTreeNode(root, json);
            if (json is JArray arr)
            {
                root.Text = TextForTreeNode("JSON", json);
                if (arr.Length > 0)
                    root.Nodes.Add(""); // add the sentinel node
            }
            else if (json is JObject obj)
            {
                root.Text = TextForTreeNode("JSON", json);
                if (obj.Length > 0)
                    root.Nodes.Add("");
            }
            else
            {
                // just show the value for the scalar
                root.Text = json.ToString();
            }
            tree.Nodes.Add(root);
            pathsToJNodes[root.FullPath] = json;
        }

        /// <summary>
        /// Populate only the direct children of the root JNode into the TreeView.<br></br>
        /// Useful for very large JSON files where recursively populating all subtrees would<br></br>
        /// take an unacceptably long time and too much memory.<br></br>
        /// For JSON with more than some large number (default 10_000) children of the root node,
        /// only that many spaced children will be shown.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="json"></param>
        /// <param name="pathsToJNodes"></param>
        private static void JsonTreePopulateHelper_DirectChildren(TreeView tree,
                                                                  TreeNode root,
                                                                  JNode json,
                                                                  Dictionary<string, JNode> pathsToJNodes)
        {
            tree.BeginUpdate();
            try
            {
                int interval;
                if (json is JArray arr)
                {
                    List<JNode> jar = arr.children;
                    interval = jar.Count / Main.settings.max_json_length_full_tree;
                    if (interval < 1) interval = 1;
                    for (int ii = 0; ii < jar.Count; ii += interval)
                    {
                        JNode child = jar[ii];
                        TreeNode child_node = root.Nodes.Add(TextForTreeNode(ii.ToString(), child));
                        if ((child is JArray childarr && childarr.Length > 0) 
                            || (child is JObject childobj && childobj.Length > 0))
                        {
                            // add a sentinel node so that this node can later be expanded
                            child_node.Nodes.Add("");
                        }
                        if (Main.settings.tree_node_images)
                            SetImageOfTreeNode(child_node, child);
                        pathsToJNodes[child_node.FullPath] = child;
                    }
                }
                else if (json is JObject obj)
                {
                    Dictionary<string, JNode> jobj = obj.children;
                    interval = jobj.Count / Main.settings.max_json_length_full_tree;
                    if (interval < 1) interval = 1;
                    int count = 0;
                    foreach (string key in jobj.Keys)
                    {
                        // iterate through keys with a stepsize of interval (step over some)
                        if (count++ % interval != 0)
                            continue;
                        JNode child = jobj[key];
                        TreeNode child_node = root.Nodes.Add(key, TextForTreeNode(key, child));
                        if ((child is JArray childarr && childarr.Length > 0)
                            || (child is JObject childobj && childobj.Length > 0))
                        {
                            // add a sentinel node so that this node can later be expanded
                            child_node.Nodes.Add("");
                        }
                        if (Main.settings.tree_node_images)
                            SetImageOfTreeNode(child_node, child);
                        pathsToJNodes[child_node.FullPath] = child;
                    }
                }
            }
            catch (Exception ex)
            {
                string expretty = RemesParser.PrettifyException(ex);
                MessageBox.Show($"Could not populate JSON tree because of error:\n{expretty}",
                                "Error while populating tree",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
            tree.EndUpdate();
        }

        private static string TextForTreeNode(string key, JNode node)
        {
            if (node is JArray arr)
                return arr.children.Count == 0
                    ? $"{key} : []"
                    : $"{key} : [{arr.children.Count}]";
            if (node is JObject obj)
                return obj.children.Count == 0
                    ? $"{key} : {{}}"
                    : $"{key} : {{{obj.children.Count}}}";
            return $"{key} : {node.ToString()}";
        }

        private void SubmitQueryButton_Click(object sender, EventArgs e)
        {
            if (json == null) return;
            string query = QueryBox.Text;
            JNode query_func = null;
            List<object> toks = null;
            bool is_assignment_expr = false;
            try
            {
                toks = lexer.Tokenize(query, out is_assignment_expr);
            }
            catch (Exception ex)
            {
                string expretty = RemesParser.PrettifyException(ex);
                MessageBox.Show($"Could not compile query {query} because of syntax error:\n{expretty}",
                                "Syntax error in RemesPath query",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }
            foreach (object tok in toks)
            {
                if (tok is char c && c == '=')
                {
                    is_assignment_expr = true;
                    break;
                }
            }
            // if the query is an assignment expression, we need to overwrite the file with the
            // modified JSON after the query has been executed
            if (is_assignment_expr)
            {
                JNode mutation_func = null;
                try
                {
                    mutation_func = remesParser.CompileAssignmentExpr(toks);
                }
                catch (Exception ex)
                {
                    string expretty = RemesParser.PrettifyException(ex);
                    MessageBox.Show($"Could not compile query {query} because of syntax error:\n{expretty}",
                                "Syntax error in RemesPath query",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                    return;
                }
                if (mutation_func is CurJson cjmut)
                {
                    try
                    {
                        query_func = cjmut.function(json);
                    }
                    catch (Exception ex)
                    {
                        string expretty = RemesParser.PrettifyException(ex);
                        MessageBox.Show($"While executing query {query}, encountered error:\n{expretty}",
                                        "Runtime error while executing query",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    // it's a constant, not a function of the input JSON.
                    query_func = mutation_func;
                }
                json = query_func;
                Main.fname_jsons[fname] = query_func;
                query_result = query_func;
                string new_json_str = query_func.PrettyPrintAndChangePositions(Main.settings.indent_pretty_print, Main.settings.sort_keys, Main.settings.pretty_print_style);
                Npp.editor.SetText(new_json_str);
                Main.lastEditedTime = DateTime.UtcNow;
                JsonTreePopulate(query_func);
                return;
            }
            // not an assignment expression, so executing the query changes the contents of the tree
            // but leaves the text of the document unchanged
            try
            {
                query_func = remesParser.Compile(toks);
            }
            catch (Exception ex)
            {
                string expretty = RemesParser.PrettifyException(ex);
                MessageBox.Show($"Could not compile query {query} because of syntax error:\n{expretty}",
                                "Syntax error in RemesPath query",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }
            if (query_func is CurJson qf)
            {
                try
                {
                    query_result = qf.function(json);
                }
                catch (Exception ex)
                {
                    string expretty = RemesParser.PrettifyException(ex);
                    MessageBox.Show($"While executing query {query}, encountered error:\n{expretty}",
                                    "Runtime error while executing query",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                // query_func is a constant, so just set the query to that
                query_result = query_func;
            }
            JsonTreePopulate(query_result);
        }

        private void QueryToCsvButton_Click(object sender, EventArgs e)
        {
            if (query_result == null) return;
            using (var jsonToCsv = new JsonToCsvForm(query_result))
                jsonToCsv.ShowDialog();
        }

        private void SaveQueryResultButton_Click(object sender, EventArgs e)
        {
            if (query_result == null) return;
            Main.PrettyPrintJsonInNewFile(query_result);
        }

        private void FindReplaceButton_Click(object sender, EventArgs e)
        {
            if (findReplaceForm == null || findReplaceForm.IsDisposed)
            {
                RemoveOwnedForm(findReplaceForm);
                findReplaceForm = new FindReplaceForm(this);
                AddOwnedForm(findReplaceForm);
                findReplaceForm.Show();
            }
            else findReplaceForm.Focus();
        }

        /// <summary>
        /// Snap the caret to the position of the JNode corresponding to the TreeNode selected.<br></br>
        /// Also populate the current path box with the path to the selected node.<br></br>
        /// This happens when a node is clicked, expanded, or selected by arrow keys
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tree_NodeSelection(object sender, EventArgs e)
        {
            if (Npp.notepad.GetCurrentFilePath() != fname) return;
            TreeNode node;
            try
            {
                node = (TreeNode)e.GetType().GetProperty("Node").GetValue(e, null);
            }
            catch { return; }
            if (node == null) return;
            string path = node.FullPath;
            Npp.editor.GotoPos(pathsToJNodes[path].position);
            // might also want to make it so that the selected line is scrolled to the top
            CurrentPathBox.Text = PathToTreeNode(node, Main.settings.key_style);
        }

        private void Tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Tree_NodeSelection(sender, e);
        }

        private void Tree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            ReplaceSentinelWithChildren(Tree, e.Node);
            Tree_NodeSelection(sender, e);
        }

        private void ReplaceSentinelWithChildren(TreeView tree, TreeNode node)
        {
            var nodes = node.Nodes;
            if (nodes.Count == 1 && nodes[0].Text.Length == 0)
            // it's a sentinel node, so replace it with the actual children
            {
                nodes.RemoveAt(0);
                JNode jnode = pathsToJNodes[node.FullPath];
                JsonTreePopulateHelper_DirectChildren(tree, node, jnode, pathsToJNodes);
            }
        }

        /// <summary>
        /// On right click, throw up a context menu that lets you do the following:<br></br>
        /// - Copy the current node's value to the clipboard<br></br>
        /// - Copy the node's path (Python style) to the clipboard<br></br>
        /// - Copy the node's key/index (Python style) to the clipboard<br></br>
        /// - Copy the node's path (JavaScript style) to the clipboard<br></br>
        /// - Copy the node's path (RemesPath style) to the clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode node = e.Node;
            if (e.Button == MouseButtons.Right)
            {
                // open a context menu that lets the user copy things to clipboard
                // lets user copy string value of current node to clipboard
                var valToClipboard = NodeRightClickMenu.Items[0];
                if (valToClipboardHandler != null)
                {
                    try
                    {
                        valToClipboard.MouseUp -= valToClipboardHandler;
                    }
                    catch { }
                }
                valToClipboardHandler = new MouseEventHandler(
                    (object sender2, MouseEventArgs e2) =>
                    {
                        JNode jnode = pathsToJNodes[node.FullPath];
                        if (jnode is JObject || jnode is JArray)
                            return;
                        Npp.TryCopyToClipboard(jnode.ToString());
                    }
                );
                valToClipboard.MouseUp += valToClipboardHandler;
                // things that get the key of the current node to clipboard
                var keyToClipboard = (ToolStripMenuItem)NodeRightClickMenu.Items[1];
                var keyToClipboard_javascript = keyToClipboard.DropDownItems[0];
                if (keyToClipboardHandler_Javascript != null)
                {
                    try
                    {
                        keyToClipboard_javascript.MouseUp -= keyToClipboardHandler_Javascript;
                    }
                    catch { }
                }
                keyToClipboardHandler_Javascript = new MouseEventHandler(
                    (s2, e2) =>
                    {
                        Npp.TryCopyToClipboard(KeyOfTreeNode(node, KeyStyle.JavaScript));
                    }
                );
                keyToClipboard_javascript.MouseUp += keyToClipboardHandler_Javascript;
                var keyToClipboard_Python = keyToClipboard.DropDownItems[1];
                if (keyToClipboardHandler_Python != null)
                {
                    try
                    {
                        keyToClipboard_Python.MouseUp -= keyToClipboardHandler_Python;
                    }
                    catch { }
                }
                keyToClipboardHandler_Python = new MouseEventHandler(
                    (s2, e2) =>
                    {
                        Npp.TryCopyToClipboard(KeyOfTreeNode(node, KeyStyle.Python));
                    }
                );
                keyToClipboard_Python.MouseUp += keyToClipboardHandler_Python;
                var keyToClipboard_RemesPath = keyToClipboard.DropDownItems[2];
                if (keyToClipboardHandler_Remespath != null)
                {
                    try
                    {
                        keyToClipboard_RemesPath.MouseUp -= keyToClipboardHandler_Remespath;
                    }
                    catch { }
                }
                keyToClipboardHandler_Remespath = new MouseEventHandler(
                    (s2, e2) =>
                    {
                        Npp.TryCopyToClipboard(KeyOfTreeNode(node, KeyStyle.RemesPath));
                    }
                );
                keyToClipboard_RemesPath.MouseUp += keyToClipboardHandler_Remespath;
                // drop down menu for getting path to clipboard
                var pathToClipboard = (ToolStripMenuItem)NodeRightClickMenu.Items[2];
                var pathToClipboard_javascript = pathToClipboard.DropDownItems[0];
                if (pathToClipboardHandler_Javascript != null)
                {
                    try
                    {
                        pathToClipboard_javascript.MouseUp -= pathToClipboardHandler_Javascript;
                    }
                    catch { }
                }
                pathToClipboardHandler_Javascript = new MouseEventHandler(
                    (s2, e2) =>
                    {
                        Npp.TryCopyToClipboard(PathToTreeNode(node, KeyStyle.JavaScript));
                    }
                );
                pathToClipboard_javascript.MouseUp += pathToClipboardHandler_Javascript;
                var pathToClipboard_Python = pathToClipboard.DropDownItems[1];
                if (pathToClipboardHandler_Python != null)
                {
                    try
                    {
                        pathToClipboard_Python.MouseUp -= pathToClipboardHandler_Python;
                    }
                    catch { }
                }
                pathToClipboardHandler_Python = new MouseEventHandler(
                    (s2, e2) =>
                    {
                        Npp.TryCopyToClipboard(PathToTreeNode(node, KeyStyle.Python));
                    }
                );
                pathToClipboard_Python.MouseUp += pathToClipboardHandler_Python;
                var pathToClipboard_RemesPath = pathToClipboard.DropDownItems[2];
                if (pathToClipboardHandler_Remespath != null)
                {
                    try
                    {
                        pathToClipboard_RemesPath.MouseUp -= pathToClipboardHandler_Remespath;
                    }
                    catch { }
                }
                pathToClipboardHandler_Remespath = new MouseEventHandler(
                    (s2, e2) =>
                    {
                        Npp.TryCopyToClipboard(PathToTreeNode(node, KeyStyle.RemesPath));
                    }
                );
                pathToClipboard_RemesPath.MouseUp += pathToClipboardHandler_Remespath;
                switch (Main.settings.key_style)
                {
                    case (KeyStyle.RemesPath): pathToClipboard.MouseUp += pathToClipboardHandler_Remespath; break;
                    case (KeyStyle.Python): pathToClipboard.MouseUp += pathToClipboardHandler_Python; break;
                    case (KeyStyle.JavaScript): pathToClipboard.MouseUp += pathToClipboardHandler_Javascript; break;
                }
                NodeRightClickMenu.Items[3].MouseUp -= ToggleSubtreesHandler;
                ToggleSubtreesHandler = new MouseEventHandler(
                    (s2, e2) =>
                    {
                        if (node.Nodes.Count > 0)
                        {
                            if (node.IsExpanded)
                                node.Collapse();
                            else node.ExpandAll();
                        }
                    }
                );
                NodeRightClickMenu.Items[3].MouseUp += ToggleSubtreesHandler;
                NodeRightClickMenu.Show(MousePosition);
            }
            if (node.IsSelected)
                // normally clicking a node selects it, so we don't need to explicitly invoke this method
                // but if the node was already selected, we need to call it
                Tree_NodeSelection(sender, e);
        }

        /// <summary>
        /// Get the path to the current TreeNode.<br></br>
        /// Style: one of Python, JavaScript), or RemesPath<br></br>
        /// EXAMPLES (using the JSON {"a b": [1, {"c": 2}], "d": [4]}<br></br>
        /// Path to key "c":<br></br>
        /// - JavaScript style: ['a b'][1].c<br></br>
        /// - Python style: ['a b'][1]['c']<br></br>
        /// - RemesPath style: [`a b`][1].c
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public string PathToTreeNode(TreeNode node, KeyStyle style = KeyStyle.Python, List<string> keys = null)
        {
            if (keys == null)
                keys = new List<string>();
            if (node.Parent == null)
            {
                if (keys.Count == 0)
                    return "";
                keys.Reverse(); // cuz they were added from the node to the root
                return string.Join("", keys);
            }
            keys.Add(KeyOfTreeNode(node, style));
            return PathToTreeNode(node.Parent, style, keys);
        }

        /// <summary>
        /// See FormatKey, but uses the key of a TreeNode
        /// </summary>
        /// <param name="node"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public string KeyOfTreeNode(TreeNode node, KeyStyle style)
        {
            if (node.Name == "")
                return $"[{node.Index}]";
            return JNode.FormatKey(node.Name, style);
        }

        /// <summary>
        /// reset the JSON and query of this form to the JSON in whatever
        /// file is currently active
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            string cur_fname = Npp.notepad.GetCurrentFilePath();
            JNode new_json = Main.TryParseJson();
            if (new_json == null)
                return;
            fname = cur_fname;
            json = new_json;
            query_result = json;
            Main.fname_jsons[fname] = json;
            JsonTreePopulate(json);
        }

        // close the find/replace form when this becomes invisible
        private void TreeViewer_VisibleChanged(object sender, EventArgs e)
        {
            if (findReplaceForm != null && !findReplaceForm.IsDisposed)
                findReplaceForm.Close();
        }

        /// <summary>
        /// On double-click, activate the buffer with the tree viewer's fname
        /// </summary>
        private void TreeViewer_DoubleClick(object sender, EventArgs e)
        {
            Npp.notepad.OpenFile(fname);
        }

        /// <summary>
        /// Just the filename, no directory information.<br></br>
        /// If no fname supplied, gets the relative filename for this TreeViewer's fname.
        /// </summary>
        public string RelativeFilename(string fname = null)
        {
            if (fname == null) fname = this.fname;
            string[] fname_split = fname.Split('\\');
            return fname_split[fname_split.Length - 1];
        }

        /// <summary>
        /// Change the fname attribute of this.<br></br>
        /// We would like to be able to change the title of the UI element as well,
        /// but it seems pretty hard to do from C#.
        /// </summary>
        /// <param name="new_fname"></param>
        public void Rename(string new_fname)
        {
            fname = new_fname;
        }
    }
}
