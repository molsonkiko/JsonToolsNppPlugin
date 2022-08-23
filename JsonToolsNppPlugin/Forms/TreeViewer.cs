/*
A form for creating a tree view of JSON and also making Remespath queries.
*/
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using JSON_Viewer.JSONViewer;
using JSON_Viewer.Infrastructure;

namespace JSON_Viewer.Forms
{
    public partial class TreeViewer : Form
    {
        public JsonParser jsonParser { get; set; }
        public RemesParser remesParser { get; set; }
        public JsonSchemaMaker schemaMaker { get; set; }
        public string json_fname { get; set; } = "";
        public JNode json { get; set; } = null;
        public JNode query_result { get; set; } = null;
        private Settings? settings = null;
        /// <summary>
        /// keeps track of the lint for each file/entry
        /// </summary>
        public Dictionary<string, JsonLint[]> fname_lints;
        /// <summary>
        /// tracks how many times JSON from text has been successfully used
        /// </summary>
        private int json_from_text_entries = 0;

        [STAThread] // this is needed to allow your form to open up a file browser dialog while in debug mode
        static void Main(string[] args)
        {
            // TestRunner.RunAll(args); // run all tests for the app
            Application.Run(new TreeViewer());
        }

        public TreeViewer()
        {
            InitializeComponent();
            settings = new Settings();
            jsonParser = new JsonParser(settings.allow_datetimes,
                                        settings.allow_singlequoted_str,
                                        settings.allow_javascript_comments,
                                        settings.linting,
                                        false,
                                        settings.allow_nan_inf);
            remesParser = new RemesParser();
            schemaMaker = new JsonSchemaMaker();
            fname_lints = new Dictionary<string, JsonLint[]>();
        }

        private void DocsButton_Click(object sender, EventArgs e)
        {
            string help_url = "https://github.com/molsonkiko/JSON-Tools/tree/main/docs";
            try
            {
                var ps = new ProcessStartInfo(help_url)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(),
                    "Could not open documentation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ParseAndSetJsonFromString(string json_str, string fname)
        {
            try
            {
                this.json = jsonParser.Parse(json_str);
            }
            catch (Exception ex)
            {
                string error_text = $"JSON parsing failed:\n{RemesParser.PrettifyException(ex)}";
                MessageBox.Show(error_text,
                    "JSON parsing error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            if (jsonParser.lint != null && jsonParser.lint.Count > 0)
            {
                // get a copy of the lint for this document
                fname_lints[fname] = jsonParser.lint.LazySlice(":").ToArray();
                //StringBuilder sb = new StringBuilder();
                //foreach (var lint in jsonParser.lint)
                //{
                //    sb.AppendLine(lint.ToString());
                //}
                //MessageBox.Show(sb.ToString());
            }
        }

        private void TreeCreationButton_Click(object sender, EventArgs e)
        {
            json_from_text_entries++;
            string text_docname = $"Text entry {json_from_text_entries}";
            ParseAndSetJsonFromString(JsonBox.Text, text_docname);
            JsonTreePopulate(JsonTree, this.json);
        }

        private void FileSelectionButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "JSON files|*.json|All files|*.*|Jupyter Notebooks|*.ipynb";
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.Title = "Browse JSON files";
            openFileDialog1.CheckFileExists = true;
            string json_str = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (!File.Exists(openFileDialog1.FileName))
                {
                    return;
                }
                json_fname = openFileDialog1.FileName;
                json_str = File.ReadAllText(json_fname);
            }
            else return; // don't do anything if the user hits Cancel
            ParseAndSetJsonFromString(json_str, json_fname);
            JsonTreePopulate(JsonTree, json);
        }

        private void ReloadFileButton_Click(object sender, EventArgs e)
        {
            if (!File.Exists(json_fname))
            {
                MessageBox.Show("No file has been loaded yet.",
                                "Can't reload before first file is chosen",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                return;
            }
            string json_str = File.ReadAllText(json_fname);
            ParseAndSetJsonFromString(json_str, json_fname);
            JsonTreePopulate(JsonTree, json);
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
                case Dtype.OBJ: root.ImageIndex= 5; root.SelectedImageIndex = 5; break;
                case Dtype.STR: root.ImageIndex = 6; root.SelectedImageIndex = 6; break;
                default: root.ImageIndex = 7; root.SelectedImageIndex = 7; break;
            }
        }

        public void JsonTreePopulate(TreeView tree, JNode json)
        {
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
                JsonTreePopulateHelper(root, json);
                root.Text = "JSON";
                root.Expand(); // could be slow for larger JSON
            }
            else
            {
                // just show the value for the scalar
                root.Text = json.ToString();
            }
            tree.Nodes.Add(root);
            tree.Parent.UseWaitCursor = false; // gotta turn it off or else it persists until the form closes
            tree.EndUpdate();
        }
        
        // recursively build the JSON tree out of TreeNodes
        public static void JsonTreePopulateHelper(TreeNode root, JNode json)
        {
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
                        SetImageOfTreeNode(child_node, child);
                        JsonTreePopulateHelper(child_node, child);
                        root.Nodes.Add(child_node);
                    }
                    else
                    {
                        // it's a scalar, so just display the index and the value of the json
                        root.Nodes.Add(ii.ToString(), $"{ii} : {child.ToString()}");
                        SetImageOfTreeNode(root.Nodes[root.Nodes.Count - 1], child);
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
            foreach ((string key, JNode child) in dic)
            {
                //// We need to make sure keys that are stringified integers are visually distinct from actual integers.
                //// otherwise the user might think that e.g. {"0": 1, "1": 2} is the same thing as [1, 2].
                //string keystr = double.TryParse(key, out _) ? '"' + key + '"' : key;
                if (child.type == Dtype.ARR || child.type == Dtype.OBJ)
                {
                    // it's an array or object, so add a subtree
                    var child_node = new TreeNode(key);
                    SetImageOfTreeNode(child_node, child);
                    JsonTreePopulateHelper(child_node, child);
                    root.Nodes.Add(child_node);
                }
                else
                {
                    // it's a scalar, so just display the key and the value of the json
                    root.Nodes.Add(key, $"{key} : {child.ToString()}");
                    SetImageOfTreeNode(root.Nodes[root.Nodes.Count - 1], child);
                }
            }
            if (root.Nodes.Count == 0)
            {
                root.Text += " : {}";
            }
        }

        private void QuerySubmissionButton_Click(object sender, EventArgs e)
        {
            if (json == null)
            {
                MessageBox.Show("Cannot submit a Remespath query because no JSON is stored.",
                    "Can't submit Remespath query",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            try
            {
                query_result = remesParser.Search(QueryBox.Text, json);
                JsonTreePopulate(QueryResultTree, query_result);
            }
            catch (Exception ex)
            {
                string error_text = $"Invalid Remespath query:\n{RemesParser.PrettifyException(ex)}";
                MessageBox.Show(error_text,
                    "Remespath parsing error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            settings.ShowDialog();
            jsonParser.allow_nan_inf = settings.allow_nan_inf;
            jsonParser.allow_datetimes = settings.allow_datetimes;
            jsonParser.allow_javascript_comments = settings.allow_javascript_comments;
            jsonParser.allow_singlequoted_str = settings.allow_singlequoted_str;
            if (settings.linting)
            {
                jsonParser.lint = new List<JsonLint>();
            }
        }

        public static void SaveJsonToFile(string json_name, JNode json)
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "All files|*.*|JSON files|*.json";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.Title = $"Save {json_name} to JSON file?";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    myStream.Write(Encoding.UTF8.GetBytes(json.PrettyPrint()));
                    myStream.Close();
                }
            }
        }

        private void SaveQueryToFileButton_Click(object sender, EventArgs e)
        {
            if (query_result == null)
            {
                MessageBox.Show("Cannot save a Remespath query result to file because no query has been made.",
                    "Can't store Remespath query result",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            SaveJsonToFile("query result", query_result);
        }

        private void GenerateSchemaButton_Click(object sender, EventArgs e)
        {
            if (query_result == null)
            {
                MessageBox.Show("Cannot create a JSON schema because no query has been made.",
                    "Can't create JSON schema",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            JNode schema = new JNode(null, Dtype.NULL, 0);
            try
            {
                schema = schemaMaker.GetSchema(query_result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("JSON schema creation failed:\n" + RemesParser.PrettifyException(ex),
                    "JSON schema creation error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            SaveJsonToFile("json schema", schema);
        }

        private void OpenJsonToCsvFormButton_Click(object sender, EventArgs e)
        {
            if (query_result == null)
            {
                MessageBox.Show("No query result to tabularize.",
                    "Can't tabularize query result",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            Form jsonToCsvForm = new JSONToCsvForm(query_result);
            jsonToCsvForm.Show();
        }

        private void OpenGrepperFormButton_Click(object sender, EventArgs e)
        {
            Form jsonGrepperForm = new GrepperForm(this);
            jsonGrepperForm.Show();
        }

        private void LintButton_Click(object sender, EventArgs e)
        {
            Form lintForm = new LintForm(this);
            lintForm.Show();
        }
    }
}
