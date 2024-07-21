using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        private string bufferOpenBeforeProgressReport;
        private string progressReportBufferName;
        private static object progressReportLock = new object();

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
                try
                {
                    // TODO: this could take a long time; maybe add a line to show a MessageBox
                    // that asks if you want to stop the search after a little while?
                    UseWaitCursor = true;
                    grepper.Grep(rootDir, recursive, searchPatterns);
                    UseWaitCursor = false;
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
            }
            AddFilesToFilesFound();
        }

        /// <summary>
        /// Looks like this:
        /// <code>
        /// JSON from files and APIs - JSON parsing progress
        /// |========                 |
        /// 0.301 MB of 1.25 MB parsed
        /// </code>
        /// </summary>
        /// <param name="checkPointsFinished"></param>
        /// <returns></returns>
        private static string ProgressBarText(int checkPointsFinished, int lengthParsedSoFar, int totalLengthToParse)
        {
            return "JSON from files and APIs - JSON parsing progress\r\n|" +
                    ArgFunction.StrMulHelper("=", checkPointsFinished) + ArgFunction.StrMulHelper(" ", CHECKPOINT_COUNT - checkPointsFinished) + "|\r\n" +
                   $"{Math.Round(lengthParsedSoFar / 1e6, 3)} MB of {Math.Round(totalLengthToParse / 1e6, 3)} MB parsed" +
                   (lengthParsedSoFar == totalLengthToParse ? "\r\nPARSING COMPLETE!" : "");
        }

        private void CreateProgressReportBuffer(int totalLengthToParse)
        {
            bufferOpenBeforeProgressReport = Npp.notepad.GetCurrentFilePath();
            Npp.notepad.FileNew();
            Npp.notepad.SetCurrentBufferInternalName("JSON parsing progress report");
            progressReportBufferName = Npp.notepad.GetCurrentFilePath();
            Npp.editor.SetText(ProgressBarText(0, 0, totalLengthToParse));
            Npp.editor.GrabFocus();
            // wait a moment for the editor to finish acquiring focus
            Thread.Sleep(50);
        }

        private void ReportJsonParsingProgress(int lengthParsedSoFar, int totalLengthToParse)
        {
            lock (progressReportLock)
            {
                // we need to do a / (b / c) rather than a * c / b, because:
                //    a * (c / b) will be 0 because c is less than b
                //    (a * c) / b could easily cause overflow, because a * c might be larger than int32.Max
                int checkPointsFinished = lengthParsedSoFar / (totalLengthToParse / CHECKPOINT_COUNT);
                if (Npp.notepad.GetCurrentFilePath() == progressReportBufferName)
                {
                    Npp.editor.SetText(ProgressBarText(checkPointsFinished, lengthParsedSoFar, totalLengthToParse));
                }
            }
        }

        private void AfterProgressReport()
        {
            //// navigate to whatever buffer was open before the progress report started
            //if (progressReportBufferName != null)
            //{
            //    if (Npp.notepad.GetCurrentFilePath() == progressReportBufferName
            //        && bufferOpenBeforeProgressReport != null
            //        && Npp.notepad.GetOpenFileNames().Contains(bufferOpenBeforeProgressReport))
            //    {
            //        Npp.notepad.OpenFile(bufferOpenBeforeProgressReport);
            //    }
            //}
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
            Main.DisplayJsonTree(tv, tv.json, "JSON from files and APIs tree", false, DocumentType.JSON, false);
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
