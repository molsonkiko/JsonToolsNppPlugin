using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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
        public string fname;

        public GrepperForm()
        {
            InitializeComponent();
            grepper = new JsonGrepper(Main.jsonParser.Copy(),
                Main.settings.thread_count_parsing,
                Main.settings.max_api_request_threads
            );
            tv = null;
            files_found = new HashSet<string>();
            fname = null;
        }

        private void SendRequestsButton_Click(object sender, EventArgs e)
        {
            try
            {
                grepper.GetJsonFromApis(UrlsBox.Lines);
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

        private void DocsButton_Click(object sender, EventArgs e)
        {
            Main.docs();
        }

        private void ChooseDirectoriesButton_Click(object sender, EventArgs e)
        {
            string[] search_patterns = SearchPatternsBox.Lines;
            bool recursive = RecursiveSearchCheckBox.Checked;
            string root_dir;
            FolderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            FolderBrowserDialog1.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (FolderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                root_dir = FolderBrowserDialog1.SelectedPath;
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
            AddFilesToFilesFound();
        }

        private void ViewErrorsButton_Click(object sender, EventArgs e)
        {
            if (grepper.exceptions.Length == 0)
            {
                MessageBox.Show("No exceptions! \U0001f600");
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
            {
                selected_files.Add((string)file);
            }
            // have to create a separate list to avoid an error from mutating something
            // while enumerating it
            JObject query_results = null;
            bool tv_null = tv == null;
            if (!tv_null)
                query_results = (JObject)tv.query_result;
            foreach (string file in selected_files)
            {
                FilesFoundBox.Items.Remove(file);
                files_found.Remove(file);
                grepper.fname_jsons.children.Remove(file);
                if (!tv_null)
                    query_results.children.Remove(file);
            }
            if (!tv_null)
            {
                tv.json = grepper.fname_jsons;
                tv.JsonTreePopulate(grepper.fname_jsons);
                Npp.notepad.FileNew();
                fname = Npp.notepad.GetCurrentFilePath();
                tv.fname = fname;
                string result = grepper.fname_jsons.PrettyPrintAndChangeLineNumbers(
                    Main.settings.indent_pretty_print,
                    Main.settings.sort_keys,
                    Main.settings.pretty_print_style
                );
                Npp.AddLine(result);
                Npp.SetLangJson();
                if (Main.treeViewer != null && !Main.treeViewer.IsDisposed)
                    Npp.notepad.HideDockingForm(Main.treeViewer);
            }
        }

        private void ViewResultsButton_Click(object sender, EventArgs e)
        {
            Main.grepperTreeViewJustOpened = true;
            Npp.notepad.FileNew();
            fname = Npp.notepad.GetCurrentFilePath();
            string result = grepper.fname_jsons.PrettyPrintAndChangeLineNumbers(
                Main.settings.indent_pretty_print,
                Main.settings.sort_keys,
                Main.settings.pretty_print_style
            );
            Npp.AddLine(result);
            if (tv != null && !tv.IsDisposed)
            {
                Npp.notepad.HideDockingForm(tv);
                tv.Close();
            }
            tv = new TreeViewer(grepper.fname_jsons);
            Main.DisplayJsonTree(tv, tv.json, "JSON from files and APIs tree");
            if (Main.treeViewer != null && !Main.treeViewer.IsDisposed)
                Npp.notepad.HideDockingForm(Main.treeViewer);
        }

        /// <summary>
        /// when the form closes, clear all the JSON
        /// and hide and then destroy the associated tree view
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
            if (Main.treeViewer != null && !Main.treeViewer.IsDisposed)
            {
                Npp.notepad.ShowDockingForm(Main.treeViewer);
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
            foreach (string fname in grepper.fname_jsons.children.Keys)
            {
                if (!files_found.Contains(fname))
                {
                    FilesFoundBox.Items.Add(fname);
                    files_found.Add(fname);
                }
            }
        }
    }
}
