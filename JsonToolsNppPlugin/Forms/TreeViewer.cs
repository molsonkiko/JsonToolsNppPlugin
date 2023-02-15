using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Forms
{
    public partial class TreeViewer : Form
    {
        /// <summary>
        /// the name of the file holding the JSON that this is associated with
        /// </summary>
        public string fname;
        
        /// <summary>
        /// Maps the path of a TreeNode to the line number of the corresponding JNode
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

        public bool use_tree;

        public double max_size_full_tree_MB;

        public NppTbData tbData;

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
            use_tree = Main.settings.use_tree;
            max_size_full_tree_MB = Main.settings.max_size_full_tree_MB;
            int file_len = Npp.editor.GetLength();
            if (file_len / 1e6 > max_size_full_tree_MB || use_tree == false)
            {
                // show that the full tree isn't being showed if the conditions
                // are not met for showing it
                FullTreeCheckBox.Checked = false;
            }
            else
                FullTreeCheckBox.Checked = true;
            //this.Tree.BeforeExpand += new TreeViewCancelEventHandler(
            //    PopulateIfUnpopulatedHandler
            //);
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
        }

        /// <summary>
        /// this is a method that will hopefully stop the dinging from the TreeView.<br></br>
        /// See https://stackoverflow.com/questions/10328103/c-sharp-winforms-how-to-stop-ding-sound-in-treeview<br></br>
        /// e.SuppressKeyPress works for most controls but the TreeView is special
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter || keyData == Keys.Escape || keyData == Keys.Tab)
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        //private void TreeViewer_KeyUp(object sender, KeyEventArgs e)
        //{
              // try some weirdness to avoid bell sound from TreeViewer<br></br>
              // see https://stackoverflow.com/questions/10328103/c-sharp-winforms-how-to-stop-ding-sound-in-treeview
        //    BeginInvoke(new Action(() => TreeViewer_KeyUp_Handler(sender, e)));
        //}

        // largely copied from NppManagedPluginDemo.cs in the original plugin pack
        private void TreeViewer_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // supposedly this is like e.Handled but also suppresses ding,
                // but the TreeView dings anyway
                e.SuppressKeyPress = true; 
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
                    // Enter in the TreeView toggles the selected node
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
                e.SuppressKeyPress = true;
            }
            // Tab -> go through controls, Shift+Tab -> go through controls backward
            else if (e.KeyCode == Keys.Tab)
            {
                Control next = GetNextControl((Control)sender, !e.Shift);
                while ((next == null) || (!next.TabStop)) next = GetNextControl(next, !e.Shift);
                next.Focus();
                e.SuppressKeyPress = true;
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
            if (tree.ImageList == null)
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
            tree.BeginUpdate();
            tree.Nodes.Clear();
            TreeNode root = new TreeNode();
            SetImageOfTreeNode(root, json);
            try
            {
                if (json is JArray || json is JObject)
                {
                    // recursively build tree for iterable JSON
                    root.Text = "JSON";
                    // need to add the root first because FullPath is undefined until root is in a TreeView
                    // but also FullPath depends on the text so we need to track that too
                    tree.Nodes.Add(root);
                    int json_strlen = Npp.editor.GetLength();
                    int json_len;
                    if (json is JArray arr) json_len = arr.Length;
                    else json_len = ((JObject)json).Length;
                    if (!use_tree)
                    { // allow for tree to be turned off altogether. Best performance for loss of quality of life
                    }
                    else if ((json_strlen > max_size_full_tree_MB * 1e6) || (json_len > Main.settings.max_json_length_full_tree))
                        JsonTreePopulateHelper_DirectChildren(root, json, pathsToJNodes);
                    else
                        JsonTreePopulateHelper(root, json, pathsToJNodes);
                }
                else
                {
                    // just show the value for the scalar
                    root.Text = json.ToString();
                    tree.Nodes.Add(root);
                }
            }
            catch (Exception ex)
            {
                string expretty = RemesParser.PrettifyException(ex);
                MessageBox.Show($"Could not populate JSON tree because of error:\n{expretty}",
                                "Syntax error while populating tree",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                tree.Parent.UseWaitCursor = false;
                return;
            }
            pathsToJNodes[root.FullPath] = json;
            tree.EndUpdate();
        }

        /// <summary>
        /// Recursively traverses the full JSON tree and adds corresponding TreeNodes to the treeview.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="json"></param>
        /// <param name="pathsToJNodes"></param>
        public static void JsonTreePopulateHelper(TreeNode root, 
                                                  JNode json,
                                                  Dictionary<string, JNode> pathsToJNodes)
        {
            TreeNode child_node;
            if (json is JArray)
            {
                List<JNode> jar = ((JArray)json).children;
                if (jar.Count == 0)
                    root.Text += " : []";
                for (int ii = 0; ii < jar.Count; ii++)
                {
                    JNode child = jar[ii];
                    if (child is JArray || child is JObject)
                    {
                        root.Nodes.Add(ii.ToString());
                        child_node = root.Nodes[root.Nodes.Count - 1];
                        JsonTreePopulateHelper(child_node, child, pathsToJNodes);
                    }
                    else
                    {
                        root.Nodes.Add($"{ii} : {child.ToString()}");
                        child_node = root.LastNode;
                    }
                    SetImageOfTreeNode(child_node, child);
                    pathsToJNodes[child_node.FullPath] = child;
                }
                return;
            }
            Dictionary<string, JNode> jobj = ((JObject)json).children;
            if (jobj.Count == 0)
                root.Text += " : {}";
            // TODO: Consider making it so that keys are sorted if the keys of the JSON are sorted
            foreach (string key in jobj.Keys)
            {
                JNode child = jobj[key];
                if (child is JArray || child is JObject)
                {
                    root.Nodes.Add(key, key);
                    child_node = root.Nodes[root.Nodes.Count - 1];
                    JsonTreePopulateHelper(child_node, child, pathsToJNodes);
                }
                else
                {
                    root.Nodes.Add(key, $"{key} : {child.ToString()}");
                    child_node = root.LastNode;
                }
                SetImageOfTreeNode(child_node, child);
                pathsToJNodes[child_node.FullPath] = child;
            }
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
        public static void JsonTreePopulateHelper_DirectChildren(TreeNode root,
                                                                 JNode json,
                                                                 Dictionary<string, JNode> pathsToJNodes)
        {
            // the tree viewer is by far the heaviest and slowest part of this application.
            // To save time, only some children will be shown
            int interval;
            if (json is JArray)
            {
                List<JNode> jar = ((JArray)json).children;
                interval = jar.Count / Main.settings.max_json_length_full_tree;
                if (interval < 1) interval = 1;
                for (int ii = 0; ii < jar.Count; ii += interval)
                {
                    JNode child = jar[ii];
                    TreeNode child_node = (child is JObject || child is JArray)
                        ? new TreeNode(ii.ToString())
                        : new TreeNode($"{ii} : {child.ToString()}");
                    SetImageOfTreeNode(child_node, child);
                    root.Nodes.Add(child_node);
                    pathsToJNodes[child_node.FullPath] = child;
                }
                return;
            }
            Dictionary<string, JNode> jobj = ((JObject)json).children;
            string[] keys = jobj.Keys.ToArray();
            interval = keys.Length / 5000;
            if (interval < 1) interval = 1;
            for (int ii = 0; ii < keys.Length; ii += interval)
            {
                string key = keys[ii];
                JNode child = jobj[key];
                TreeNode child_node = (child is JObject || child is JArray)
                    ? new TreeNode(key)
                    : new TreeNode($"{key} : {child.ToString()}");
                SetImageOfTreeNode(child_node, child);
                root.Nodes.Add(child_node);
                pathsToJNodes[child_node.FullPath] = child;
            }
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
                    // it's not a function of the input JSON.
                    query_func = mutation_func;
                }
                json = query_func;
                string new_json_str = query_func.PrettyPrintAndChangeLineNumbers(Main.settings.indent_pretty_print, Main.settings.sort_keys, Main.settings.pretty_print_style);
                Npp.editor.SetText(new_json_str);
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
            // allow full tree to be populated for small query results
            // even if the entire JSON file is too big to allow full tree population
            //if (query_result is JArray jarr_res)
            //{
            //    if (json is JArray jarr)
            //    {
            //        double length_ratio = jarr_res.Length / jarr.Length;
            //        if (length_ratio * )
            //    }
            //}
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
                findReplaceForm = new FindReplaceForm(this);
                findReplaceForm.Show();
            }
            else findReplaceForm.Focus();
        }

        private void FullTreeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (FullTreeCheckBox.Checked)
            {
                int file_len = Npp.editor.GetLength();
                double estimated_time = file_len / 5e5; // 2s per MB
                string est_time_str = estimated_time.ToString("N2"); // round to 2 decimal places
                if ((estimated_time > 5 
                        && MessageBox.Show($"Loading the full tree for this document will make Notepad++ unresponsive " +
                                    $"for an estimated {est_time_str} seconds. Are you sure you want to do this?",
                                    "Loading the full tree could be slow",
                                    MessageBoxButtons.OKCancel,
                                    MessageBoxIcon.Warning)
                            == DialogResult.OK)
                    || estimated_time <= 5)
                {
                    max_size_full_tree_MB = 10_000;
                    // make it large enough to ensure that the document's tree will fit
                    use_tree = true;
                    JsonTreePopulate(json); // replace with full tree
                }
                else
                    FullTreeCheckBox.Checked = false; // cancel the checking action
            }
            else
            {
                // replace the full tree with a partial tree
                max_size_full_tree_MB = 0;
                JsonTreePopulate(json);
            }
        }

        /// <summary>
        /// Snap the caret to the line of the JNode corresponding to the TreeNode selected.<br></br>
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
            Npp.editor.GotoLine(pathsToJNodes[path].line_num);
            // might also want to make it so that the selected line is scrolled to the top
            CurrentPathBox.Text = PathToTreeNode(node, Main.settings.key_style);
        }

        private void Tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Tree_NodeSelection(sender, e);
        }

        private void Tree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            Tree_NodeSelection(sender, e);
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
            //if (cur_fname != fname)
            //    tbData.pszName = $"Json Tree View for {RelativeFilename()}"; // doesn't work
                // change the docking form's title to reflect the new filename
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
            // none of this works, don't bother
            //// change the title of the UI element
            //tbData.pszName = new_fname;
            //// tell Notepad++ to update the info
            //Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMUPDATEDISPINFO, 0, tbData.hClient);
            /******* DO NOT UNCOMMENT THIS BLOCK!!! IT WILL MAKE NOTEPAD++ CRASH!
            IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
            Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            ********/
}

///// <summary>
///// build the top layer of the tree out of JNodes.<br></br>
///// JArrays and JObjects will initially have a single sentinel TreeNode as children.<br></br>
///// Because this sentinel node will not be in the pathsToJNodes map, this will be<br></br>
///// able to distinguish which nodes already have their children populated.<br></br>
///// THIS DOES NOT WORK BECAUSE OF STRANGE ISSUES WITH EVENT HANDLERS.
///// </summary>
///// <param name="root"></param>
///// <param name="json"></param>
///// <param name="pathsToJNodes"></param>
//public static void JsonTreePopulateHelper_Lazy(TreeNode root, 
//                                               JNode json, 
//                                               Dictionary<string, JNode> pathsToJNodes)
//{
//    throw new NotImplementedException();
//    root.TreeView.BeginUpdate();
//    root.Nodes.Clear(); // remove the sentinel node
//    if (json is JArray)
//    {
//        List<JNode> jar = ((JArray)json).children;
//        for (int ii = 0; ii < jar.Count; ii++)
//        {
//            JNode child = jar[ii];
//            if (child is JArray)
//            {
//                var child_node = new TreeNode(ii.ToString());
//                root.Nodes.Add(child_node);
//                child_node.ImageIndex = 0;
//                child_node.Nodes.Add(new TreeNode("")); // add the sentinel
//            }
//            else if (child is JObject)
//            {
//                var child_node = new TreeNode(ii.ToString());
//                root.Nodes.Add(child_node);
//                child_node.ImageIndex = 5;
//                child_node.Nodes.Add(new TreeNode("")); // add the sentinel
//            }
//            else
//            {
//                // it's a scalar, so just display the index and the value of the json
//                root.Nodes.Add(ii.ToString(), $"{ii} : {child.ToString()}");
//                SetImageOfTreeNode(root.Nodes[root.Nodes.Count - 1], child);
//            }
//            string path = root.Nodes[root.Nodes.Count - 1].FullPath;
//            pathsToJNodes[path] = child;
//        }
//        if (root.Nodes.Count == 0)
//        {
//            root.Text += " : []";
//        }
//        return;
//    }
//    // create a subtree for a json object
//    // scalars are dealt with already, so no need for a separate branch
//    Dictionary<string, JNode> dic = ((JObject)json).children;
//    foreach (string key in dic.Keys)
//    {
//        JNode child = dic[key];
//        if (child is JArray)
//        {
//            var child_node = new TreeNode(key);
//            root.Nodes.Add(child_node);
//            SetImageOfTreeNode(root, child);
//            child_node.Nodes.Add(new TreeNode("")); // add the sentinel
//        }
//        else if (child is JObject)
//        {
//            var child_node = new TreeNode(key);
//            root.Nodes.Add(child_node);
//            SetImageOfTreeNode(root, child);
//            child_node.Nodes.Add(new TreeNode("")); // add the sentinel
//        }
//        else
//        {
//            // it's a scalar, so just display the key and the value of the json
//            root.Nodes.Add(key, $"{key} : {child.ToString()}");
//            SetImageOfTreeNode(root.Nodes[root.Nodes.Count - 1], child);
//        }
//        string path = root.Nodes[root.Nodes.Count - 1].FullPath;
//        pathsToJNodes[path] = child;
//    }
//    if (root.Nodes.Count == 0)
//    {
//        root.Text += " : {}";
//    }
//    root.TreeView.EndUpdate();
//}

///// <summary>
///// If a TreeNode has a sentinel node child, replace the sentinel child with its true children.<br></br>
///// Otherwise, do nothing.
///// </summary>
///// <param name="sender"></param>
///// <param name="e"></param>
//private void PopulateIfUnpopulatedHandler(object sender, TreeViewCancelEventArgs e)
//{
//    TreeNode root = e.Node;
//    e.Cancel = false;
//    if (root.Nodes.Count == 1 
//        && !pathsToJNodes.ContainsKey(root.Nodes[0].FullPath))
//        // if root has the sentinel node indicating an unexpanded node,
//        // populate its children
//    {
//        JNode node = pathsToJNodes[root.FullPath];
//        JsonTreePopulateHelper(root, node, pathsToJNodes);
//    }
//}
}
}
