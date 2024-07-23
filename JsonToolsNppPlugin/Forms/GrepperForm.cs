using System;
using System.Collections.Generic;
using System.Drawing;
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
        HashSet<string> filesFound;
        private readonly FileInfo directoriesVisitedFile;
        private readonly FileInfo urlsQueriedFile;
        private List<string> urlsQueried;
        // fields related to progress reporting
        private const int CHECKPOINT_COUNT = 25;
        private Form progressBarForm;
        private MyProgressBar progressBar;
        private bool isParsing;
        //private Button progressBarCancelButton;
        private Label progressLabel;
        private static object progressReportLock = new object();
        private static Dictionary<string, string> progressBarTranslatedStrings = null;

        public GrepperForm()
        {
            InitializeComponent();
            NppFormHelper.RegisterFormIfModeless(this, false);
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
            grepper = new JsonGrepper(Main.JsonParserFromSettings(), true, CHECKPOINT_COUNT, CreateProgressReportBuffer, ReportJsonParsingProgress, AfterProgressReport);
            tv = null;
            filesFound = new HashSet<string>();
            var configSubdir = Path.Combine(Npp.notepad.GetConfigDirectory(), Main.PluginName);
            directoriesVisitedFile = new FileInfo(Path.Combine(configSubdir, $"{Main.PluginName} directories visited.txt"));
            int maxDirnameChars = DirectoriesVisitedBox.Items[0].ToString().Length;
            if (directoriesVisitedFile.Exists)
            {
                string[] dirsVisited = new string[] { "" };
                using (var fp = new StreamReader(directoriesVisitedFile.OpenRead(), Encoding.UTF8, true))
                {
                    dirsVisited = fp.ReadToEnd().Split('\n');
                }
                foreach (string dirVisited in dirsVisited)
                {
                    if (Directory.Exists(dirVisited))
                        DirectoriesVisitedBox.Items.Add(dirVisited);
                    // increase the dropdown width as needed to accomodate the longest dirname
                    if (dirVisited.Length > maxDirnameChars)
                        maxDirnameChars = dirVisited.Length;
                }
            }
            DirectoriesVisitedBox.DropDownWidth = maxDirnameChars * 7;
            urlsQueriedFile = new FileInfo(Path.Combine(configSubdir, $"{Main.PluginName} urls queried.txt"));
            urlsQueried = new List<string>();
            if (urlsQueriedFile.Exists)
            {
                using (var fp2 = new StreamReader(urlsQueriedFile.OpenRead(), Encoding.UTF8, true))
                {
                    var urlsText = CleanUrlsBoxText(fp2.ReadToEnd().Split('\n'));
                    UrlsBox.Text = urlsText;
                    urlsQueried.AddRange(UrlsBox.Lines);
                }
            }
            DirectoriesVisitedBox.SelectedIndex = 0;
            // we need to select something other than the UrlsBox because otherwise all its text will be selected
            //    and the user could accidentally overwrite it.
            SearchDirectoriesButton.Select();
        }

        private async void SendRequestsButton_Click(object sender, EventArgs e)
        {
            string[] urls;
            try
            {
                // try to parse the UrlsBox content as JSON.
                var parser = new JsonParser(LoggerLevel.JSON5);
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
                Translator.ShowTranslatedMessageBox(ex.ToString(),
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
            Main.OpenUrlInWebBrowser("https://github.com/molsonkiko/JsonToolsNppPlugin/blob/main/docs/README.md#get-json-from-files-and-apis");
        }

        private string LastVisitedDirectory()
        {
            return DirectoriesVisitedBox.Items[DirectoriesVisitedBox.Items.Count - 1].ToString();
        }

        private void ChooseDirectoriesButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            // the user can select from previously opened directories, or they can use a folder browser dialog
            FolderBrowserDialog1.SelectedPath = DirectoriesVisitedBox.Items.Count == 0
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : LastVisitedDirectory();
            if (FolderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                DirectoriesVisitedBox.Text = FolderBrowserDialog1.SelectedPath;
                AddDirectoryToDirectoriesVisited(FolderBrowserDialog1.SelectedPath);
            }
        }

        private void SearchDirectoriesButton_Click(object sender, EventArgs e)
        {
            string[] searchPatterns = SearchPatternsBox.Lines;
            bool recursive = RecursiveSearchCheckBox.Checked;
            string rootDir = DirectoriesVisitedBox.Text;
            AddDirectoryToDirectoriesVisited(rootDir);
            if (Directory.Exists(rootDir))
            {
                UseWaitCursor = true;
                try
                {
                    grepper.Grep(rootDir, recursive, searchPatterns);
                }
                catch (Exception ex)
                {
                    Translator.ShowTranslatedMessageBox(
                        "While searching JSON files, got an exception:\r\n{0}",
                        "Json file search error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        1, RemesParser.PrettifyException(ex));
                }
                UseWaitCursor = false;
            }
            AddFilesToFilesFound();
        }

        private void CreateProgressReportBuffer(int totalLengthToParse, long totalLengthOnHardDrive)
        {
            string totLengthToParseMB = (totalLengthToParse / 1e6).ToString("F3", JNode.DOT_DECIMAL_SEP);
            string totLengthOnHardDriveMB = (totalLengthOnHardDrive / 1e6).ToString("F3", JNode.DOT_DECIMAL_SEP);
            isParsing = totalLengthOnHardDrive == -1;
            string titleIfParsing = "JSON parsing in progress";
            string titleIfReading = "File reading in progress";
            string captionIfParsing = "File reading complete.\r\nNow parsing {0} documents with combined length of about {1} MB";
            string captionIfReading = "Now reading {0} files with a combined length of about {1} MB";
            string progressLabelIfParsing = "0 MB of {0} MB parsed";
            string progressLabelIfReading = "0 of {0} files read";
            if (progressBarTranslatedStrings is null)
            {
                progressBarTranslatedStrings = new Dictionary<string, string>
                {
                    ["titleIfParsing"] = titleIfParsing,
                    ["titleIfReading"] = titleIfReading,
                    ["captionIfParsing"] = captionIfParsing,
                    ["captionIfReading"] = captionIfReading,
                    ["progressLabelIfParsing"] = progressLabelIfParsing,
                    ["progressLabelIfReading"] = progressLabelIfReading,
                };
                string[] keys = progressBarTranslatedStrings.Keys.ToArray();
                if (Translator.TryGetTranslationAtPath(new string[] {"forms", "GrepperFormProgressBar", "controls"}, out JNode progressBarTransNode) && progressBarTransNode is JObject progressBarTrans)
                {
                    foreach (string key in keys)
                    {
                        if (progressBarTrans.TryGetValue(key, out JNode val) && val.value is string s)
                            progressBarTranslatedStrings[key] = s;
                    }
                }
            }
            Label label = new Label
            {
                Name = "caption",
                Text = isParsing
                           ? Translator.TryTranslateWithFormatting(captionIfParsing, progressBarTranslatedStrings["captionIfParsing"], grepper.fnameStrings.Count, totLengthToParseMB)
                           : Translator.TryTranslateWithFormatting(captionIfReading, progressBarTranslatedStrings["captionIfReading"], totalLengthToParse, totLengthOnHardDriveMB),
                TextAlign = ContentAlignment.TopCenter,
                Top = 20,
                AutoSize = true,
            };
            progressLabel = new Label
            {
                Name = "progressLabel",
                Text = isParsing
                        ? Translator.TryTranslateWithFormatting(progressLabelIfParsing, progressBarTranslatedStrings["progressLabelIfParsing"], totLengthToParseMB)
                        : Translator.TryTranslateWithFormatting(progressLabelIfReading, progressBarTranslatedStrings["progressLabelIfReading"], totalLengthToParse),
                TextAlign = ContentAlignment.TopCenter,
                Top = 100,
                AutoSize = true,
            };
            progressBar = new MyProgressBar()
            {
                Name = "progress",
                Minimum = 0,
                Maximum = totalLengthToParse,
                Style = ProgressBarStyle.Blocks,
                Left = 20,
                Width = 450,
                Top = 200,
                Height = 50,
            };
            //progressBarCancelButton = new Button
            //{
            //    Name = "Cancel",
            //    Left = 200,
            //    Text = "Cancel parsing",
            //};
            //cancelProgressBar = false;
            //progressBarCancelButton.Click += new EventHandler((object sender, EventArgs e) =>
            //{

            //});

            progressBarForm = new Form
            {
                Name = "GrepperFormProgressBar",
                Text = isParsing ? progressBarTranslatedStrings["titleIfParsing"] : progressBarTranslatedStrings["titleIfReading"],
                Controls = { label, progressLabel, progressBar },
                Width = 500,
                Height = 300,
            };
            if (label.Right > progressBarForm.Width)
            {
                progressBarForm.SuspendLayout();
                progressBarForm.Width = label.Right + 20;
                progressBarForm.ResumeLayout(false);
            }
            progressBarForm.Show();
        }

        private class MyProgressBar : ProgressBar
        {
            public MyProgressBar() : base()
            {
                CheckForIllegalCrossThreadCalls = false;
            }
        }

        private void ReportJsonParsingProgress(int lengthParsedSoFar, int __)
        {
            if (isParsing)
            {
                lock (progressReportLock)
                {
                    progressLabel.Text = Regex.Replace(progressLabel.Text, @"^\d+(?:\.\d+)?", _ => (lengthParsedSoFar / 1e6).ToString("F3", JNode.DOT_DECIMAL_SEP));
                    progressBar.Value = lengthParsedSoFar;
                }
            }
            else
            {
                // don't need to use the lock when reading files, because that is single-threaded
                progressLabel.Text = Regex.Replace(progressLabel.Text, @"^\d+", _ => lengthParsedSoFar.ToString());
                progressBar.Value = lengthParsedSoFar;
                progressBarForm.Refresh();
            }
        }

        private void AfterProgressReport()
        {
            progressBarForm.Close();
            progressBarForm.Dispose();
            progressBarForm = null;
            progressBar = null;
            progressLabel = null;
            ViewResultsButton.Focus();
        }

        private void AddDirectoryToDirectoriesVisited(string rootDir)
        {
            if (!Directory.Exists(rootDir) || DirectoriesVisitedBox.Items.IndexOf(rootDir) >= 0)
                return; // only add existing directories that aren't already in the list
            DirectoriesVisitedBox.Items.Add(rootDir);
            // only track 10 most recently visited dirs
            // the count will be at most 11 because we always have the reminder as item at index 0.
            if (DirectoriesVisitedBox.Items.Count > 11)
                DirectoriesVisitedBox.Items.RemoveAt(1);
            // increase the dropdown width as needed to accomodate the longest dirname
            if (rootDir.Length * 7 > DirectoriesVisitedBox.DropDownWidth)
                DirectoriesVisitedBox.DropDownWidth = rootDir.Length * 7;
        }

        private void ViewErrorsButton_Click(object sender, EventArgs e)
        {
            if (grepper.exceptions.Length == 0)
            {
                Translator.ShowTranslatedMessageBox(
                    "No exceptions! {0}",
                    "No errors while searching documents",
                    MessageBoxButtons.OK, MessageBoxIcon.None,
                    1, "\U0001f600"); // smily face
                return;
            }
            Main.PrettyPrintJsonInNewFile(grepper.exceptions);
        }

        private void RemoveSelectedFilesButton_Click(object sender, EventArgs e)
        {
            Main.grepperTreeViewJustOpened = true;
            var selectedFiles = new List<string>();
            foreach (object file in FilesFoundBox.SelectedItems)
            // have to create a separate list to avoid an error from mutating something
            // while enumerating it
            {
                selectedFiles.Add((string)file);
            }
            foreach (string file in selectedFiles)
            {
                FilesFoundBox.Items.Remove(file);
                filesFound.Remove(file);
                grepper.fnameJsons.children.Remove(file);
            }
            if (tv != null)
            {
                // refresh the tree view with the current query executed
                // on the pruned JSON
                // also overwrite the grepper tree's buffer with the pruned JSON
                tv.json = grepper.fnameJsons;
                string fileOpen = Npp.notepad.GetCurrentFilePath();
                Npp.notepad.OpenFile(tv.fname);
                Npp.editor.SetText(Main.PrettyPrintFromSettings(tv.json));
                tv.SubmitQueryButton.PerformClick();
                Npp.notepad.OpenFile(fileOpen);
                if (Main.openTreeViewer != null && !Main.openTreeViewer.IsDisposed)
                    Npp.notepad.HideDockingForm(Main.openTreeViewer);
            }
        }

        private void ViewResultsButton_Click(object sender, EventArgs e)
        {
            Main.grepperTreeViewJustOpened = true;
            Main.PrettyPrintJsonInNewFile(grepper.fnameJsons);
            if (tv != null && !tv.IsDisposed)
            {
                RemoveOwnedForm(tv);
                Npp.notepad.HideDockingForm(tv);
                tv.Close();
            }
            tv = new TreeViewer(grepper.fnameJsons);
            AddOwnedForm(tv);
            string treeName = "JSON from files and APIs tree";
            if (Translator.TryGetTranslationAtPath(new string[] { "forms", "TreeViewer", "titleIfGrepperForm" }, out JNode node) && node.value is string s)
                treeName = s;
            Main.DisplayJsonTree(tv, tv.json, treeName, false, DocumentType.JSON, false);
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
                using (var fp = new StreamWriter(directoriesVisitedFile.OpenWrite(), Encoding.UTF8))
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
                using (var fp2 = new StreamWriter(urlsQueriedFile.OpenWrite(), Encoding.UTF8))
                {
                    fp2.Write(CleanUrlsBoxText(urlsQueried));
                    fp2.Write("\r\n");
                    fp2.Flush();
                }
            }
        }

        /// <summary>
        /// suppress the default response to the Tab key
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData.HasFlag(Keys.Tab)) // this covers Tab with or without modifiers
                return true;
            return base.ProcessDialogKey(keyData);
        }

        private void GrepperForm_KeyUp(object sender, KeyEventArgs e)
        {
            NppFormHelper.GenericKeyUpHandler(this, sender, e, false);
            // Enter in the DirectoriesVisitedBox selects the entered directory
            if (e.KeyCode == Keys.Enter &&
                sender is ComboBox cbx && cbx.Name == "DirectoriesVisitedBox"
                && Directory.Exists(cbx.Text))
            {
                SearchDirectoriesButton.PerformClick();
            }
            //if (e.Alt)
            //{
            //    switch (e.KeyCode)
            //    {
            //        case Keys.A: SendRequestsButton.PerformClick(); e.Handled = true; break;
            //        case Keys.B: ViewResultsButton.PerformClick(); e.Handled = true; break;
            //        case Keys.C: ChooseDirectoriesButton.PerformClick(); e.Handled = true; break;
            //        case Keys.D: DocsButton.PerformClick(); e.Handled = true; break;
            //        case Keys.E: ViewErrorsButton.PerformClick(); e.Handled = true; break;
            //        case Keys.R: RemoveSelectedFilesButton.PerformClick(); e.Handled = true; break;
            //    }
            //}
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
                e.Handled = true;
        }

        private void AddFilesToFilesFound()
        {
            var urlRegex = new Regex("^https?://");
            foreach (string fname in grepper.fnameJsons.children.Keys)
            {
                if (!filesFound.Contains(fname))
                {
                    FilesFoundBox.Items.Add(fname);
                    filesFound.Add(fname);
                }
                // if it's a URL, add it to the list
                if (urlRegex.IsMatch(fname) && !urlsQueried.Contains(fname))
                {
                    urlsQueried.Add(fname);
                    // keep list of remembered URLs to <= 10
                    if (urlsQueried.Count > 10)
                        urlsQueried.RemoveAt(0);
                }
            }
        }
    }
}
