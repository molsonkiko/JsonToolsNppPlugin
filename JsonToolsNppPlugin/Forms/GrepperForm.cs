using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JSON_Viewer.JSONViewer;

namespace JSON_Viewer.Forms
{
    public partial class GrepperForm : Form
    {
        private JsonGrepper grepper;
        private TreeViewer treeViewer;
        private string root_dir;
        private HashSet<string> files_found;
        private JObject query_results;
        /// <summary>
        /// number of threads for remespath query evaluation
        /// </summary>
        public int thread_query_max_count;

        public GrepperForm(TreeViewer treeViewer, int thread_query_max_count = 4)
        {
            InitializeComponent();
            grepper = new JsonGrepper(treeViewer.jsonParser);
            this.treeViewer = treeViewer;
            root_dir = "";
            files_found = new HashSet<string>();
            query_results = new JObject(0, new Dictionary<string, JNode>());
            this.thread_query_max_count = thread_query_max_count;
        }

        private void GrepperForm_Load(object sender, EventArgs e) { }

        private static string API_REQUEST_WARNING = @"Make sure you've formatted your API request urls correctly!
If you don't understand the correct formatting of requests, review the API's documentation.
And of course, make sure the API is legitimate and you understand what you're requesting.";

        /// <summary>
        /// Asynchronously attempt to send HTTP GET requests to all the urls in
        /// the URL entry box (one URL per line).
        /// If there's an error in a request, put up a message box.
        /// If the request succeeds, map the URL to the returned JSON the json tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SendRequestsToUrlsButton_Click(object sender, EventArgs e)
        {
            HashSet<string> files_before_request = new HashSet<string>(files_found);
            DialogResult send_requests = MessageBox.Show(API_REQUEST_WARNING,
                                                         "Send API requests?",
                                                         MessageBoxButtons.OKCancel,
                                                         MessageBoxIcon.Exclamation);
            if (send_requests != DialogResult.OK) return;
            (HashSet<string> urls_requested, Dictionary<string, string> exceptions) = await grepper.GetJsonFromAllUrls(UrlBox.Lines);
            foreach ((string bad_url, string ex) in exceptions)
            {
                if (MessageBox.Show($"Exception raised on url {bad_url}:\n{ex}",
                                    "Exception on url request",
                                    MessageBoxButtons.OKCancel,
                                    MessageBoxIcon.Error)
                    == DialogResult.Cancel)
                {
                    // let the user stop looking at exceptions by hitting the cancel button
                    return;
                }
            }
            files_found.UnionWith(urls_requested);
            foreach (string url in urls_requested)
            {
                FilesFoundBox.Items.Add(url);
                if (grepper.fname_lints.TryGetValue(url, out JsonLint[] lints))
                {
                    treeViewer.fname_lints[url] = lints.LazySlice(":").ToArray();
                }
            }
            treeViewer.JsonTreePopulate(GrepTree, grepper.fname_jsons);
        }
        
        /// <summary>
        /// Opern a selected directory, and using the entered search pattern
        /// and the choice of recursion, grep all the json files found.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GrepJsonButton_Click(object sender, EventArgs e)
        {
            string search_pattern = SearchPatternBox.Text;
            bool recursive = RecursiveGrepBox.Checked;
            var dir_dialog = new FolderBrowserDialog();
            if (dir_dialog.ShowDialog() == DialogResult.OK)
            {
                root_dir = dir_dialog.SelectedPath;
                if (Directory.Exists(root_dir))
                {
                    try
                    {
                        // TODO: this could take a long time; maybe add a line to show a MessageBox
                        // that asks if you want to stop the search after a little while?
                        UseWaitCursor = true;
                        grepper.ReadJsonFiles(root_dir, recursive, search_pattern);
                        UseWaitCursor = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("JSON file search failed:\n" + RemesParser.PrettifyException(ex),
                            "Json file search error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            if (grepper.fname_jsons.Length > 0)
            {
                foreach (string fname in grepper.fname_jsons.children.Keys)
                {
                    if (!files_found.Contains(fname))
                    {
                        FilesFoundBox.Items.Add(fname);
                        files_found.Add(fname);
                        if (grepper.fname_lints.TryGetValue(fname, out JsonLint[] lints))
                        {
                            treeViewer.fname_lints[fname] = lints.LazySlice(":").ToArray();
                        }
                    }
                }
                treeViewer.JsonTreePopulate(GrepTree, grepper.fname_jsons);
            }
        }

        /// <summary>
        /// call a compiled remespath query on a subset of the JSON documents.
        /// Each thread calls this function for a different subset of documents.
        /// </summary>
        /// <param name="compiled_query"></param>
        /// <param name="start_idx"></param>
        /// <param name="end_idx"></param>
        private void CallQueryOnJsons(JNode compiled_query, int start_idx, int end_idx)
        {
            for (int ii = start_idx; ii < end_idx; ii++)
            {
                string fname = (string)FilesFoundBox.Items[ii];
                JNode json = grepper.fname_jsons.children[fname];
                try
                {
                    JNode result = (compiled_query is CurJson) ? ((CurJson)compiled_query).function(json) : compiled_query;
                    query_results.children[fname] = result;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("RemesPath query execution failed:\n" + RemesParser.PrettifyException(ex),
                            "RemesPath query execution failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                }
            }
        }


        /// <summary>
        /// iterate through the JSONs in the grepper,
        /// execute the entered RemesPath query on the JSON, and map
        /// the filename to the query result in the Query Result tree.
        /// This is a multithreaded function; it deals with files on separate threads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExecuteQueryButton_Click(object sender, EventArgs e)
        {
            JNode compiled_query = new JNode(null, Dtype.NULL, 0);
            try
            {
                // we'll compile it beforehand because we're repeatedly executing it
                // this will also separate compile errors from runtime errors
                compiled_query = treeViewer.remesParser.Compile(QueryBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid Remespath query:\n" + RemesParser.PrettifyException(ex),
                            "Invalid RemesPath query",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                return;
            }
            query_results = new JObject(0, new Dictionary<string, JNode>());
            int num_files = files_found.Count;
            int files_per_thread = num_files / thread_query_max_count;
            Thread[] threads = new Thread[thread_query_max_count];
            UseWaitCursor = true;
            for (int tnum = 0; tnum < thread_query_max_count; tnum++)
            {
                int start_idx = tnum * files_per_thread;
                // each thread operates on files_per_thread files
                // except the last thread, which operates on num_files - (files_per_thread * thread_query_max_count - 1) files
                int end_idx = tnum == thread_query_max_count - 1 ? num_files : start_idx + files_per_thread;
                // threads have to be called with a static void method as an argument
                // you can't feed them an instance method
                // as a hack, we will just feed it a 0-arg method (a *thunk*) that calls the instance method
                // with the appropriate args when called 
                var th = new Thread(() => CallQueryOnJsons(compiled_query, start_idx, end_idx));
                threads[tnum] = th;
                th.Start();
            }
            // the threads do their thing
            foreach (var thread in threads) thread.Join();
            UseWaitCursor = false;
            treeViewer.JsonTreePopulate(QueryTree, query_results);
        }

        /// <summary>
        /// remove some selected files from the result set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveSelectedFilesButton_Click(object sender, EventArgs e)
        {
            var selected_items = new List<string>();
            // have to make a separate list of selected filenames
            // otherwise you get an error for changing the contents of an enumerator in mid-enumeration
            foreach (string fname in FilesFoundBox.SelectedItems)
            {
                selected_items.Add(fname);
            }
            foreach (string fname in selected_items)
            {
                grepper.fname_jsons.children.Remove(fname);
                FilesFoundBox.Items.Remove(fname);
                files_found.Remove(fname);
                query_results.children.Remove(fname);
            }
            treeViewer.JsonTreePopulate(GrepTree, grepper.fname_jsons);
            treeViewer.JsonTreePopulate(QueryTree, query_results);
        }

        /// <summary>
        /// Remove all JSON found so far.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearFilesButton_Click(object sender, EventArgs e)
        {
            DialogResult remove_all = MessageBox.Show("Remove all searched JSON?\nThis cannot be undone!",
                                                      "Remove all selected JSON?",
                                                      MessageBoxButtons.OKCancel,
                                                      MessageBoxIcon.Exclamation);
            if (remove_all == DialogResult.OK)
            {
                grepper.Reset();
                query_results.children.Clear();
                FilesFoundBox.Items.Clear();
                files_found.Clear();
                treeViewer.JsonTreePopulate(QueryTree, query_results);
                treeViewer.JsonTreePopulate(GrepTree, grepper.fname_jsons);
            }
        }

        private void SaveQueryResultsButton_Click(object sender, EventArgs e)
        {
            foreach ((string fname, JNode json) in query_results.children)
            {
                TreeViewer.SaveJsonToFile(fname, json);
            }
        }
    }
}
