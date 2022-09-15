using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Forms
{
    public partial class TreeViewer : Form
    {
        /// <summary>
        /// the name of the file holding the JSON that this is associated with
        /// </summary>
        string fname;
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

        public TreeViewCancelEventHandler expandler;

        public TreeViewer(JNode json)
        {
            InitializeComponent();
            pathsToJNodes = new Dictionary<string, JNode>();
            fname = Npp.GetCurrentPath();
            this.json = json;
            query_result = json;
            remesParser = new RemesParser();
            lexer = new RemesPathLexer();
            schemaMaker = new JsonSchemaMaker();
            this.Tree.BeforeExpand += new TreeViewCancelEventHandler(
                PopulateIfUnpopulatedHandler
            );
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
                    if (json is JArray arr)
                    {
                        if (arr.Length > 0)
                        {
                            root.Nodes.Add(new TreeNode("")); // sentinel node
                        }
                    }
                    else if (json is JObject obj)
                    {
                        if (obj.Length > 0)
                        {
                            root.Nodes.Add(new TreeNode("")); // sentinel node
                        }
                    }
                    
                    //root.Expand(); // could be slow for larger JSON
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
        /// build the top layer of the tree out of JNodes.<br></br>
        /// JArrays and JObjects will initially have a single sentinel TreeNode as children.<br></br>
        /// Because this sentinel node will not be in the pathsToJNodes map, this will be<br></br>
        /// able to distinguish which nodes already have their children populated.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="json"></param>
        /// <param name="pathsToJNodes"></param>
        public static void JsonTreePopulateHelper(TreeNode root, 
                                                  JNode json, 
                                                  Dictionary<string, JNode> pathsToJNodes)
        {
            root.TreeView.BeginUpdate();
            root.Nodes.Clear(); // remove the sentinel node
            if (json is JArray)
            {
                List<JNode> jar = ((JArray)json).children;
                for (int ii = 0; ii < jar.Count; ii++)
                {
                    JNode child = jar[ii];
                    if (child is JArray)
                    {
                        var child_node = new TreeNode(ii.ToString());
                        root.Nodes.Add(child_node);
                        child_node.ImageIndex = 0;
                        child_node.Nodes.Add(new TreeNode("")); // add the sentinel
                    }
                    else if (child is JObject)
                    {
                        var child_node = new TreeNode(ii.ToString());
                        root.Nodes.Add(child_node);
                        child_node.ImageIndex = 5;
                        child_node.Nodes.Add(new TreeNode("")); // add the sentinel
                    }
                    else
                    {
                        // it's a scalar, so just display the index and the value of the json
                        root.Nodes.Add(ii.ToString(), $"{ii} : {child.ToString()}");
                        SetImageOfTreeNode(root.Nodes[root.Nodes.Count - 1], child);
                    }
                    string path = root.Nodes[root.Nodes.Count - 1].FullPath;
                    pathsToJNodes[path] = child;
                }
                if (root.Nodes.Count == 0)
                {
                    root.Text += " : []";
                }
                return;
            }
            // create a subtree for a json object
            // scalars are dealt with already, so no need for a separate branch
            Dictionary<string, JNode> dic = ((JObject)json).children;
            foreach (string key in dic.Keys)
            {
                JNode child = dic[key];
                if (child is JArray)
                {
                    var child_node = new TreeNode(key);
                    root.Nodes.Add(child_node);
                    SetImageOfTreeNode(root, child);
                    child_node.Nodes.Add(new TreeNode("")); // add the sentinel
                }
                else if (child is JObject)
                {
                    var child_node = new TreeNode(key);
                    root.Nodes.Add(child_node);
                    SetImageOfTreeNode(root, child);
                    child_node.Nodes.Add(new TreeNode("")); // add the sentinel
                }
                else
                {
                    // it's a scalar, so just display the key and the value of the json
                    root.Nodes.Add(key, $"{key} : {child.ToString()}");
                    SetImageOfTreeNode(root.Nodes[root.Nodes.Count - 1], child);
                }
                string path = root.Nodes[root.Nodes.Count - 1].FullPath;
                pathsToJNodes[path] = child;
            }
            if (root.Nodes.Count == 0)
            {
                root.Text += " : {}";
            }
            root.TreeView.EndUpdate();
        }

        /// <summary>
        /// snap the caret to the line of the JNode corresponding to the TreeNode clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //StringBuilder sb = new StringBuilder();
            //foreach (string k in pathsToJNodes.Keys)
            //{
            //    JNode v = pathsToJNodes[k];
            //    sb.Append($"{k}: {v.ToString().Slice(0, 10, 1)}...");
            //    sb.Append("\r\n");
            //}
            //MessageBox.Show(sb.ToString());
            if (Npp.GetCurrentPath() != fname) return;
            string path = e.Node.FullPath;
            if (!pathsToJNodes.ContainsKey(path)) return;
            Npp.editor.GotoLine(pathsToJNodes[path].line_num);
            // might also want to make it so that the selected line is scrolled to the top
        }

        /// <summary>
        /// if a TreeNode is checked, replace the sentinel child with its true children.<br></br>
        /// Otherwise, do nothing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PopulateIfUnpopulatedHandler(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode root = e.Node;
            e.Cancel = false;
            if (root.Nodes.Count == 1 
                && !pathsToJNodes.ContainsKey(root.Nodes[0].FullPath))
                // if root has the sentinel node indicating an unexpanded node,
                // populate its children
            {
                JNode node = pathsToJNodes[root.FullPath];
                JsonTreePopulateHelper(root, node, pathsToJNodes);
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
                    query_func = mutation_func;
                }
                json = query_func;
                string new_json_str = query_func.PrettyPrintAndChangeLineNumbers();
                Npp.editor.SetText(new_json_str);
                JsonTreePopulate(query_func);
                return;
            }
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
            Npp.notepad.FileNew();
            Npp.editor.AppendTextAndMoveCursor(query_result.PrettyPrint());
            Npp.SetLangJson();
        }

        // not creating schemas at present because schemas produced may be invalid
        //private void SchemaButton_Click(object sender, EventArgs e)
        //{
        //    if (query_result == null) return;
        //    JNode schema = null;
        //    try
        //    {
        //        schema = schemaMaker.GetSchema(query_result);
        //    }
        //    catch (Exception ex)
        //    {
        //        string expretty = RemesParser.PrettifyException(ex);
        //        MessageBox.Show($"While creating JSON schema, encountered error:\n{expretty}",
        //                            "Exception while making JSON schema",
        //                            MessageBoxButtons.OK,
        //                            MessageBoxIcon.Error);
        //        return;
        //    }
        //    Npp.notepad.FileNew();
        //    Npp.editor.AppendTextAndMoveCursor(schema.PrettyPrint());
        //    Npp.SetLangJson();
        //}
    }
}
