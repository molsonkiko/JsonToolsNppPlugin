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
        public Dictionary<string, int> pathsToLines;
        /// <summary>
        /// result of latest RemesPath query
        /// </summary>
        public JNode query_result;

        public JNode json;

        public RemesParser remesParser;

        public JsonSchemaMaker schemaMaker;

        public TreeViewer(JNode json)
        {
            InitializeComponent();
            pathsToLines = new Dictionary<string, int>();
            fname = Npp.GetCurrentPath();
            this.json = json;
            query_result = json;
            remesParser = new RemesParser();
            schemaMaker = new JsonSchemaMaker();
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
            tree.Parent.UseWaitCursor = true; // get the spinny cursor that means the computer is processing
            tree.Nodes.Clear();
            TreeNode root = new TreeNode();
            SetImageOfTreeNode(root, json);
            if ((json.type & Dtype.ITERABLE) != 0)
            {
                // recursively build tree for iterable JSON
                root.Text = "JSON";
                // need to add the root first because FullPath is undefined until root is in a TreeView
                // but also FullPath depends on the text so we need to track that too
                tree.Nodes.Add(root);
                JsonTreePopulateHelper(root, json, pathsToLines);
                root.Expand(); // could be slow for larger JSON
            }
            else
            {
                // just show the value for the scalar
                root.Text = json.ToString();
                tree.Nodes.Add(root);
            }
            tree.Parent.UseWaitCursor = false; // gotta turn it off or else it persists until the form closes
            tree.EndUpdate();
        }

        // recursively build the JSON tree out of TreeNodes
        public static void JsonTreePopulateHelper(TreeNode root, JNode json, Dictionary<string, int> pathsToLines)
        {
            pathsToLines[root.FullPath] = json.line_num;
            if (json is JArray)
            {
                List<JNode> jar = ((JArray)json).children;
                for (int ii = 0; ii < jar.Count; ii++)
                {
                    JNode child = jar[ii];
                    if (child.type == Dtype.ARR || child.type == Dtype.OBJ)
                    {
                        // it's an array or object, so add a subtree
                        var child_node = new TreeNode(ii.ToString());
                        root.Nodes.Add(child_node);
                        SetImageOfTreeNode(child_node, child);
                        JsonTreePopulateHelper(child_node, child, pathsToLines);
                    }
                    else
                    {
                        // it's a scalar, so just display the index and the value of the json
                        root.Nodes.Add(ii.ToString(), $"{ii} : {child.ToString()}");
                        SetImageOfTreeNode(root.Nodes[root.Nodes.Count - 1], child);
                        string path = root.Nodes[root.Nodes.Count - 1].FullPath;
                        pathsToLines[path] = child.line_num;
                    }
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
                //// We need to make sure keys that are stringified integers are visually distinct from actual integers.
                //// otherwise the user might think that e.g. {"0": 1, "1": 2} is the same thing as [1, 2].
                //string keystr = double.TryParse(key, out _) ? '"' + key + '"' : key;
                JNode child = dic[key];
                if (child.type == Dtype.ARR || child.type == Dtype.OBJ)
                {
                    // it's an array or object, so add a subtree
                    var child_node = new TreeNode(key);
                    root.Nodes.Add(child_node);
                    SetImageOfTreeNode(child_node, child);
                    JsonTreePopulateHelper(child_node, child, pathsToLines);
                }
                else
                {
                    // it's a scalar, so just display the key and the value of the json
                    root.Nodes.Add(key, $"{key} : {child.ToString()}");
                    SetImageOfTreeNode(root.Nodes[root.Nodes.Count - 1], child);
                    string path = root.Nodes[root.Nodes.Count - 1].FullPath;
                    pathsToLines[path] = child.line_num;
                }
            }
            if (root.Nodes.Count == 0)
            {
                root.Text += " : {}";
            }
        }

        /// <summary>
        /// snap the caret to the line of the JNode corresponding to the TreeNode clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (Npp.GetCurrentPath() != fname) return;
            string path = e.Node.FullPath;
            if (!pathsToLines.ContainsKey(path)) return;
            Npp.editor.GotoLine(pathsToLines[path]);
            // might also want to make it so that the selected line is scrolled to the top
        }

        private void SubmitQueryButton_Click(object sender, EventArgs e)
        {
            if (json == null) return;
            string query = QueryBox.Text;
            JNode query_func = null;
            try
            {
                query_func = remesParser.Compile(query);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not compile query {query} because of error:\n{ex}",
                                "Error while compiling query",
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
                    MessageBox.Show($"While executing query {query}, encountered error:\n{ex}",
                                    "Query execution failed",
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

        private void SchemaButton_Click(object sender, EventArgs e)
        {
            if (query_result == null) return;
            JNode schema = null;
            try
            {
                schema = schemaMaker.GetSchema(query_result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"While creating JSON schema, encountered error:\n{ex}",
                                    "Exception while making JSON schema",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                return;
            }
            Npp.notepad.FileNew();
            Npp.editor.AppendTextAndMoveCursor(schema.PrettyPrint());
            Npp.SetLangJson();
        }
    }
}
