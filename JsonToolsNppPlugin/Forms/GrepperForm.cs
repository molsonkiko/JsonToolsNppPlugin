using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;

namespace JSON_Tools.Forms
{
    public partial class GrepperForm : Form
    {
        public TreeViewer tv;
        public JsonGrepper grepper;
        HashSet<string> files_found;
        private readonly FileInfo directories_visited_file;
        private readonly FileInfo urls_queried_file;
        private List<string> urls_queried;

        public GrepperForm()
        {
            InitializeComponent();
            grepper = new JsonGrepper(Main.jsonParser.Copy(),
                Main.settings.max_threads_parsing
            );
            tv = null;
            files_found = new HashSet<string>();
            var config_subdir = Path.Combine(Npp.notepad.GetConfigDirectory(), Main.PluginName);
            directories_visited_file = new FileInfo(Path.Combine(config_subdir, $"{Main.PluginName} directories visited.txt"));
            int max_dirname_chars = DirectoriesVisitedBox.Items[0].ToString().Length;
            if (directories_visited_file.Exists)
            {
                string[] dirs_visited = new string[] { "" };
                using (var fp = new StreamReader(directories_visited_file.OpenRead(), Encoding.UTF8, true))
                {
                    dirs_visited = fp.ReadToEnd().Split('\n');
                }
                foreach (string dir_visited in dirs_visited)
                {
                    if (dir_visited.Length > 0)
                        DirectoriesVisitedBox.Items.Add(dir_visited);
                    // increase the dropdown width as needed to accomodate the longest dirname
                    if (dir_visited.Length > max_dirname_chars)
                        max_dirname_chars = dir_visited.Length;
                }
            }
            DirectoriesVisitedBox.DropDownWidth = max_dirname_chars * 7;
            urls_queried_file = new FileInfo(Path.Combine(config_subdir, $"{Main.PluginName} urls queried.txt"));
            urls_queried = new List<string>();
            if (urls_queried_file.Exists)
            {
                using (var fp2 = new StreamReader(urls_queried_file.OpenRead(), Encoding.UTF8, true))
                {
                    var urls_text = CleanUrlsBoxText(fp2.ReadToEnd().Split('\n'));
                    UrlsBox.Text = urls_text;
                    urls_queried.AddRange(UrlsBox.Lines);
                }
            }
            DirectoriesVisitedBox.SelectedIndex = 0;
        }

        private async void SendRequestsButton_Click(object sender, EventArgs e)
        {
            string[] urls;
            try
            {
                // try to parse the UrlsBox content as JSON.
                var parser = new JsonParser(linting: true);
                var parsed = (JArray)parser.Parse(UrlsBox.Text);
                urls = parsed.children.Select((node) => (string)node.value).ToArray();
            }
            catch
            {
                // if JSON parsing fails, we fall back on treating urls box content as
                // just a simple list with one url per line
                urls = UrlsBox.Lines;
            }
            try
            {
                await grepper.GetJsonFromApis(urls);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), 
                    "Error while sending API requests",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            AddFilesToFilesFound();
        }

        /// <summary>
        /// remove all empty lines from inp
        /// </summary>
        /// <param name="inp">a series of lines</param>
        /// <returns></returns>
        private string CleanUrlsBoxText(IEnumerable<string> inp)
        {
            return string.Join("\r\n",
                inp
                .Select((x) => x.TrimEnd('\r'))
                .Where((x) => x.Length > 0));
        }

        private void DocsButton_Click(object sender, EventArgs e)
        {
            Main.docs();
        }

        private string LastVisitedDirectory()
        {
            return DirectoriesVisitedBox.Items[DirectoriesVisitedBox.Items.Count - 1].ToString();
        }

        private void ChooseDirectoriesButton_Click(object sender, EventArgs e)
        {
            string[] search_patterns = SearchPatternsBox.Lines;
            bool recursive = RecursiveSearchCheckBox.Checked;
            FolderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            // the user can select from previously opened directories, or they can use a folder browser dialog
            FolderBrowserDialog1.SelectedPath = DirectoriesVisitedBox.Items.Count == 0
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : LastVisitedDirectory();
            if (DirectoriesVisitedBox.SelectedIndex != 0 || FolderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string root_dir;
                // if the user used the dialog, add the folder found to the list of recently visited dirs
                if (DirectoriesVisitedBox.SelectedIndex == 0)
                {
                    root_dir = FolderBrowserDialog1.SelectedPath;
                    DirectoriesVisitedBox.Items.Add(root_dir);
                    // only track 10 most recently visited dirs
                    // the count will be at most 11 because we always have the reminder as item at index 0.
                    if (DirectoriesVisitedBox.Items.Count > 11)
                        DirectoriesVisitedBox.Items.RemoveAt(1);
                    // increase the dropdown width as needed to accomodate the longest dirname
                    if (root_dir.Length * 7 > DirectoriesVisitedBox.DropDownWidth)
                        DirectoriesVisitedBox.DropDownWidth = root_dir.Length * 7;
                }
                else root_dir = DirectoriesVisitedBox.Items[DirectoriesVisitedBox.SelectedIndex].ToString();
                if (Directory.Exists(root_dir))
                {
                    foreach (string search_pattern in search_patterns)
                    {
                        try
                        {
                            // TODO: this could take a long time; maybe add a line to show a MessageBox
                            // that asks if you want to stop the search after a little while?
                            UseWaitCursor = true;
                            grepper.Grep(root_dir, recursive, search_pattern);
                            UseWaitCursor = false;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("JSON file search failed:\n" + RemesParser.PrettifyException(ex),
                                "Json file search error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
            DirectoriesVisitedBox.SelectedIndex = 0;
            AddFilesToFilesFound();
        }

        private void ViewErrorsButton_Click(object sender, EventArgs e)
        {
            if (grepper.exceptions.Length == 0)
            {
                MessageBox.Show("No exceptions! \U0001f600"); // smily face
                return;
            }
            Npp.notepad.FileNew();
            string exc_str = grepper.exceptions.PrettyPrintAndChangeLineNumbers(
                Main.settings.indent_pretty_print,
                Main.settings.sort_keys,
                Main.settings.pretty_print_style
            );
            Npp.AddLine(exc_str);
            Npp.SetLangJson();
        }

        private void RemoveSelectedFilesButton_Click(object sender, EventArgs e)
        {
            Main.grepperTreeViewJustOpened = true;
            var selected_files = new List<string>();
            foreach (object file in FilesFoundBox.SelectedItems)
            // have to create a separate list to avoid an error from mutating something
            // while enumerating it
            {
                selected_files.Add((string)file);
            }
            foreach (string file in selected_files)
            {
                FilesFoundBox.Items.Remove(file);
                files_found.Remove(file);
                grepper.fname_jsons.children.Remove(file);
            }
            if (tv != null)
            {
                // refresh the tree view with the current query executed
                // on the pruned JSON
                // also overwrite the grepper tree's buffer with the pruned JSON
                tv.json = grepper.fname_jsons;
                string file_open = Npp.notepad.GetCurrentFilePath();
                Npp.notepad.OpenFile(tv.fname);
                Npp.editor.SetText(tv.json.PrettyPrintAndChangeLineNumbers());
                tv.SubmitQueryButton.PerformClick();
                Npp.notepad.OpenFile(file_open);
                if (Main.openTreeViewer != null && !Main.openTreeViewer.IsDisposed)
                    Npp.notepad.HideDockingForm(Main.openTreeViewer);
            }
        }

        private void ViewResultsButton_Click(object sender, EventArgs e)
        {
            Main.grepperTreeViewJustOpened = true;
            Main.PrettyPrintJsonInNewFile(grepper.fname_jsons);
            if (tv != null && !tv.IsDisposed)
            {
                Npp.notepad.HideDockingForm(tv);
                tv.Close();
            }
            tv = new TreeViewer(grepper.fname_jsons);
            Main.DisplayJsonTree(tv, tv.json, "JSON from files and APIs tree");
            if (Main.openTreeViewer != null && !Main.openTreeViewer.IsDisposed)
                Npp.notepad.HideDockingForm(Main.openTreeViewer);
        }

        /// <summary>
        /// when the form closes, clear all the JSON
        /// and hide and then destroy the associated tree view.<br></br>
        /// Also write all directories visited to the config file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GrepperForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            grepper.Reset();
            if (tv != null)
            {
                Npp.notepad.HideDockingForm(tv);
                tv.Close();
            }
            if (Main.openTreeViewer != null && !Main.openTreeViewer.IsDisposed)
            {
                Npp.notepad.ShowDockingForm(Main.openTreeViewer);
            }
            // write directories visited to config file
            if (DirectoriesVisitedBox.Items.Count > 1)
            {
                Npp.CreateConfigSubDirectoryIfNotExists();
                using (var fp = new StreamWriter(directories_visited_file.OpenWrite(), Encoding.UTF8))
                {
                    for (int ii = 1; ii < DirectoriesVisitedBox.Items.Count; ii++)
                    {
                        fp.Write($"{DirectoriesVisitedBox.Items[ii]}\n");
                    }
                    fp.Flush();
                }
            }
            if (UrlsBox.Text.Length > 0)
            {
                Npp.CreateConfigSubDirectoryIfNotExists();
                using (var fp2 = new StreamWriter(urls_queried_file.OpenWrite(), Encoding.UTF8))
                {
                    fp2.Write(CleanUrlsBoxText(urls_queried));
                    fp2.Write("\r\n");
                    fp2.Flush();
                }
            }
        }

        private void GrepperForm_KeyUp(object sender, KeyEventArgs e)
        {
            // enter presses button
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                if (sender is Button btn)
                {
                    // Enter has the same effect as clicking a selected button
                    btn.PerformClick();
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
                e.Handled = true;
            }
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
                e.Handled = true;
        }

        private void AddFilesToFilesFound()
        {
            var url_regex = new Regex("^https?://");
            foreach (string fname in grepper.fname_jsons.children.Keys)
            {
                if (!files_found.Contains(fname))
                {
                    FilesFoundBox.Items.Add(fname);
                    files_found.Add(fname);
                }
                // if it's a URL, add it to the list
                if (url_regex.IsMatch(fname) && !urls_queried.Contains(fname))
                {
                    urls_queried.Add(fname);
                    // keep list of remembered URLs to <= 10
                    if (urls_queried.Count > 10)
                        urls_queried.RemoveAt(0);
                }
            }
        }
    }
}
