// NPP plugin platform for .Net v0.91.57 by Kasper B. Graversen etc.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Kbg.Demo.Namespace.Properties;
using Kbg.NppPluginNET.PluginInfrastructure;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using JSON_Tools.Forms;
using JSON_Tools.Tests;

namespace Kbg.NppPluginNET
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "JsonTools";
        // json tools related things
        public static Settings settings = new Settings();
        public static JsonParser jsonParser = new JsonParser(settings.allow_datetimes,
                                                            settings.allow_singlequoted_str,
                                                            settings.allow_javascript_comments,
                                                            settings.linting,
                                                            false,
                                                            settings.allow_nan_inf);
        public static RemesParser remesParser = new RemesParser();
        public static JsonSchemaMaker schemaMaker = new JsonSchemaMaker();
        public static YamlDumper yamlDumper = new YamlDumper();
        public static string active_fname = null;
        public static TreeViewer openTreeViewer = null;
        public static Dictionary<string, TreeViewer> treeViewers = new Dictionary<string, TreeViewer>();
        public static GrepperForm grepperForm = null;
        //public static Form nodeSelectedForm = null;
        public static bool grepperTreeViewJustOpened = false;
        public static Dictionary<string, JsonLint[]> fname_lints = new Dictionary<string, JsonLint[]>();
        public static Dictionary<string, JNode> fname_jsons = new Dictionary<string, JNode>();

        // toolbar icons
        static Bitmap tbBmp_tbTab = Resources.star_bmp;
        static Icon tbIcon = null;

        // fields related to forms
        static internal int jsonTreeId = -1;
        static internal int grepperFormId = -1;
        static internal int AboutFormId = -1;
        //static internal int nodeSelectedLabelId = -1;
        #endregion

        #region " Startup/CleanUp "

        static internal void CommandMenuInit()
        {
            // Initialization of your plugin commands

            // with function :
            // SetCommand(int index,                            // zero based number to indicate the order of command
            //            string commandName,                   // the command name that you want to see in plugin menu
            //            NppFuncItemDelegate functionPointer,  // the symbol of function (function pointer) associated with this command. The body should be defined below. See Step 4.
            //            ShortcutKey *shortcut,                // optional. Define a shortcut to trigger this command
            //            bool check0nInit                      // optional. Make this menu item be checked visually
            //            );
            PluginBase.SetCommand(0, "Documentation", docs);
            // adding shortcut keys may cause crash issues if there's a collision, so try not adding shortcuts
            PluginBase.SetCommand(1, "&Pretty-print current JSON file", PrettyPrintJson, new ShortcutKey(true, true, true, Keys.P));
            PluginBase.SetCommand(2, "&Compress current JSON file", CompressJson, new ShortcutKey(true, true, true, Keys.C));
            PluginBase.SetCommand(3, "Path to current &line", CopyPathToCurrentLine, new ShortcutKey(true, true, true, Keys.L));
            // Here you insert a separator
            PluginBase.SetCommand(3, "---", null);
            PluginBase.SetCommand(4, "Open &JSON tree viewer", () => OpenJsonTree(), new ShortcutKey(true, true, true, Keys.J)); jsonTreeId = 4;
            PluginBase.SetCommand(5, "&Get JSON from files and APIs", OpenGrepperForm, new ShortcutKey(true, true, true, Keys.G)); grepperFormId = 5;
            PluginBase.SetCommand(6, "&Settings", OpenSettings, new ShortcutKey(true, true, true, Keys.S));
            PluginBase.SetCommand(7, "---", null);
            PluginBase.SetCommand(8, "Parse JSON Li&nes document", () => OpenJsonTree(true));
            PluginBase.SetCommand(9, "&Array to JSON Lines", DumpJsonLines);
            PluginBase.SetCommand(10, "---", null);
            PluginBase.SetCommand(11, "JSON to &YAML", DumpYaml);
            PluginBase.SetCommand(12, "&Run tests", async () => await TestRunner.RunAll());
            PluginBase.SetCommand(13, "A&bout", ShowAboutForm); AboutFormId = 13;

            //// set mouse dwell time in preparation for mouse dwell related-methods
            //Npp.editor.SetMouseDwellTime(400);
            //nodeSelectedLabelId = 14;
        }

        public static void OnNotification(ScNotification notification)
        {
            uint code = notification.Header.Code;
            // This method is invoked whenever something is happening in notepad++
            // use eg. as
            // if (code == (uint)NppMsg.NPPN_xxx)
            // { ... }
            // or
            //
            // if (code == (uint)SciMsg.SCNxxx)
            // { ... }
            //// changing tabs
            switch (code)
            {
                case (uint)NppMsg.NPPN_BUFFERACTIVATED:
                    string new_fname = Npp.notepad.GetFilePath(notification.Header.IdFrom);
                    if (active_fname != null)
                    {
                        // check to see if there was a treeview for the file
                        // we just navigated away from
                        if (openTreeViewer != null && openTreeViewer.IsDisposed)
                        {
                            treeViewers.Remove(openTreeViewer.fname);
                            openTreeViewer = null;
                        }
                        // now see if there's a treeview for the file just opened
                        if (treeViewers.TryGetValue(new_fname, out TreeViewer new_tv))
                        {
                            if (new_tv.IsDisposed)
                                treeViewers.Remove(new_fname);
                            else
                            {
                                // minimize the treeviewer that was visible and make the new one visible
                                if (openTreeViewer != null)
                                    Npp.notepad.HideDockingForm(openTreeViewer);
                                Npp.notepad.ShowDockingForm(new_tv);
                                openTreeViewer = new_tv;
                            }
                        }
                        // else { }
                        // don't hide the active TreeViewer just because the user opened a new tab,
                        // they might want to still have it open
                    }
                    // if grepper form and tree viewer are both open,
                    // ensure that only one is visible at a time
                    // don't do anything when the tree view first opens though
                    if (grepperForm != null
                        && grepperForm.tv != null
                        && !grepperForm.tv.IsDisposed
                        && !grepperTreeViewJustOpened)
                    {
                        if (Npp.notepad.GetCurrentFilePath() == grepperForm.fname)
                        {
                            if (openTreeViewer != null && !openTreeViewer.IsDisposed)
                                Npp.notepad.HideDockingForm(openTreeViewer);
                            Npp.notepad.ShowDockingForm(grepperForm.tv);
                        }
                        else
                        {
                            Npp.notepad.HideDockingForm(grepperForm.tv);
                            if (openTreeViewer != null && !openTreeViewer.IsDisposed)
                                Npp.notepad.ShowDockingForm(openTreeViewer);
                        }
                    }
                    grepperTreeViewJustOpened = false;
                    active_fname = new_fname;
                    return;
                // when closing a file
                case (uint)NppMsg.NPPN_FILEBEFORECLOSE:
                    IntPtr buffer_closed_id = notification.Header.IdFrom;
                    string buffer_closed = Npp.notepad.GetFilePath(buffer_closed_id);
                    // if you close the file belonging the GrepperForm, delete its tree viewer
                    if (grepperForm != null && grepperForm.tv != null && !grepperForm.tv.IsDisposed &&
                        buffer_closed == grepperForm.fname)
                    {
                        Npp.notepad.HideDockingForm(grepperForm.tv);
                        grepperForm.tv.Close();
                        grepperForm.tv = null;
                        // also unhide the normal tree viewer if it exists
                        if (treeViewers.TryGetValue(buffer_closed, out TreeViewer treeViewer1) && !treeViewer1.IsDisposed)
                            Npp.notepad.ShowDockingForm(treeViewer1);
                        return;
                    }
                    // clean up data associated with the buffer that was just closed
                    fname_jsons.Remove(buffer_closed);
                    if (!treeViewers.TryGetValue(buffer_closed, out TreeViewer closed_tv))
                        return;
                    if (!closed_tv.IsDisposed)
                    {
                        Npp.notepad.HideDockingForm(closed_tv);
                        closed_tv.Close();
                    }
                    treeViewers.Remove(buffer_closed);
                    return;
                // the editor color scheme changed, so update the tree view colors
                case (uint)NppMsg.NPPN_WORDSTYLESUPDATED:
                    foreach (TreeViewer treeViewer2 in treeViewers.Values)
                    {
                        if (treeViewer2 != null)
                            treeViewer2.ApplyStyle(settings.use_npp_styling);
                        if (grepperForm != null && grepperForm.tv != null)
                            grepperForm.tv.ApplyStyle(settings.use_npp_styling);
                    }
                    return;
                // the user left their mouse in one place for a while (default 0.4s)
                //case (uint)SciMsg.SCN_DWELLSTART:
                //    // create a little menu that shows the path to the current node.
                //    if (Npp.editor.GetLength() > 1e5)
                //    int dwell_line = Npp.editor.LineFromPosition(notification.Position.Value);
                //    string node_path = PathToLine(dwell_line);
                //    if (node_path.Length == 0)
                //        return;
                //    if (nodeSelectedForm != null)
                //    {
                //        nodeSelectedForm.Dispose();
                //        nodeSelectedForm = null;
                //    }
                //    nodeSelectedForm = new Form();
                //    nodeSelectedForm.Text = node_path;
                //    nodeSelectedForm.MouseUp += new MouseEventHandler(
                //        (o, a) => Clipboard.SetText(node_path)
                //    );
                //    nodeSelectedForm.Show();
                //    return;
                //// the user did something that ended the passive mouse dwell period
                //case (uint)SciMsg.SCN_DWELLEND:
                //    if (notification.Position.Value == -1)
                //        // ignore DWELLEND events that fire for no particular reason
                //        // this happens all the time and I don't know why
                //        return;
                //    if (nodeSelectedForm != null)
                //        nodeSelectedForm.Close();
                //    nodeSelectedForm = null;
                //    return;
                // after an undo (Ctrl + Z) or redo (Ctrl + Y) action
                //case (uint)SciMsg.SCI_UNDO:
                //case (uint)SciMsg.SCI_REDO:
                //    string fname = Npp.notepad.GetCurrentFilePath();
                //    if (!fname_jsons.ContainsKey(fname))
                //        return; // ignore files with no JSON yet
                //    // reparse the file
                //    string ext = Npp.FileExtension(fname);
                //    JNode json = TryParseJson(ext == "jsonl");
                //    treeViewer.json = json;
                //    // if the tree view is open, refresh it
                //    if (treeViewer != null)
                //        treeViewer.JsonTreePopulate(json);
                //    return;
                //if (code > int.MaxValue) // windows messages
                //{
                //    int wm = -(int)code;
                //    }
                //}
            }
        }

        static internal void PluginCleanUp()
        {
            //treeViewers.Clear();
            if (grepperForm != null && !grepperForm.IsDisposed)
                grepperForm.Close();
            foreach (string key in treeViewers.Keys)
            {
                treeViewers[key].Dispose();
                treeViewers[key] = null;
            }
        }
        #endregion

        #region " Menu functions "
        public static void docs()
        {
            string help_url = "https://github.com/molsonkiko/JsonToolsNppPlugin";
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

        /// <summary>
        /// Try to parse the current document as JSON (or JSON Lines if is_json_lines).<br></br>
        /// If parsing fails, throw up a message box telling the user what happened.<br></br>
        /// If linting is active and the linter catches anything, throw up a message box
        /// asking the user if they want to view the caught errors in a new buffer.<br></br>
        /// Finally, associate the parsed JSON with the current filename in fname_jsons
        /// and return the JSON.
        /// </summary>
        /// <param name="is_json_lines"></param>
        /// <returns></returns>
        public static JNode TryParseJson(bool is_json_lines = false)
        {
            int len = Npp.editor.GetLength();
            string fname = Npp.notepad.GetCurrentFilePath();
            string text = Npp.editor.GetText(len);
            JNode json = new JNode();
            try
            {
                if (is_json_lines)
                    json = jsonParser.ParseJsonLines(text);
                else
                    json = jsonParser.Parse(text);
            }
            catch (Exception e)
            {
                string expretty = RemesParser.PrettifyException(e);
                MessageBox.Show($"Could not parse the document because of error\n{expretty}",
                                "Error while trying to parse JSON",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return null;
            }
            if (jsonParser.lint != null && jsonParser.lint.Count > 0)
            {
                fname_lints[fname] = jsonParser.lint.ToArray();
                string msg = $"There were {jsonParser.lint.Count} syntax errors in the document. Would you like to see them?";
                if (MessageBox.Show(msg, "View syntax errors in document?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    == DialogResult.Yes)
                {
                    Npp.notepad.FileNew();
                    var sb = new StringBuilder();
                    sb.AppendLine($"Syntax errors for {fname} on {System.DateTime.Now}");
                    foreach (JsonLint lint in jsonParser.lint)
                    {
                        sb.AppendLine($"Syntax error on line {lint.line} (position {lint.pos}, char {lint.cur_char}): {lint.message}");
                    }
                    Npp.AddLine(sb.ToString());
                    Npp.notepad.OpenFile(fname);
                }
            }
            fname_jsons[fname] = json;
            return json;
        }

        static void PrettyPrintJson()
        {
            JNode json = TryParseJson();
            if (json == null) return;
            Npp.editor.SetText(json.PrettyPrintAndChangeLineNumbers(settings.indent_pretty_print, settings.sort_keys, settings.pretty_print_style));
            Npp.SetLangJson();
        }

        static void CompressJson()
        {
            JNode json = TryParseJson();
            if (json == null) return;
            if (settings.minimal_whitespace_compression)
                Npp.editor.SetText(json.ToStringAndChangeLineNumbers(settings.sort_keys, null, ":", ","));
            else
                Npp.editor.SetText(json.ToStringAndChangeLineNumbers(settings.sort_keys));
            Npp.SetLangJson();
        }

        static void DumpYaml()
        {
            string warning = "This feature has known bugs that may result in invalid YAML being emitted. Run the tests to see examples. Use it anyway?";
            if (MessageBox.Show(warning,
                            "JSON to YAML feature has some bugs",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Warning)
                == DialogResult.Cancel)
                return;
            JNode json = TryParseJson();
            if (json == null) return;
            Npp.notepad.FileNew();
            string yaml = "";
            try
            {
                yaml = yamlDumper.Dump(json);
            }
            catch (Exception ex)
            {
                string expretty = RemesParser.PrettifyException(ex);
                MessageBox.Show($"Could not convert the JSON to YAML because of error\n{expretty}",
                                "Error while trying to convert JSON to YAML",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }
            Npp.editor.SetText(yaml);
            Npp.notepad.SetCurrentLanguage(LangType.L_YAML);
        }

        /// <summary>
        /// If the current file is a JSON array, open a new buffer with a JSON Lines
        /// document containing all entries in the array.
        /// </summary>
        static void DumpJsonLines()
        {
            JNode json = TryParseJson();
            if (json == null) return;
            if (!(json is JArray arr))
            {
                MessageBox.Show("Only JSON arrays can be converted to JSON Lines format.",
                                "Only arrays can be converted to JSON Lines",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Npp.notepad.FileNew();
            string result;
            if (settings.minimal_whitespace_compression)
                result = arr.ToJsonLines(settings.sort_keys, ":", ",");
            else
                result = arr.ToJsonLines(settings.sort_keys);
            Npp.editor.AppendText(result.Length, result);
        }

        //form opening stuff

        static void OpenSettings()
        {
            settings.ShowDialog();
            jsonParser.allow_nan_inf = settings.allow_nan_inf;
            jsonParser.allow_datetimes = settings.allow_datetimes;
            jsonParser.allow_javascript_comments = settings.allow_javascript_comments;
            jsonParser.allow_singlequoted_str = settings.allow_singlequoted_str;
            jsonParser.lint = settings.linting ? new List<JsonLint>() : null;
            // make sure grepperForm gets these new settings as well
            if (grepperForm != null && !grepperForm.IsDisposed)
            {
                grepperForm.grepper.json_parser = jsonParser.Copy();
                grepperForm.grepper.max_threads_parsing = settings.max_threads_parsing;
                if (grepperForm.tv != null && !grepperForm.tv.IsDisposed)
                {
                    grepperForm.tv.use_tree = settings.use_tree;
                    grepperForm.tv.max_size_full_tree_MB = settings.max_size_full_tree_MB;
                    grepperForm.tv.ApplyStyle(settings.use_npp_styling);
                }
            }
            // when the user changes their mind about whether to use editor styling
            // for the tree viewer, reflect their decision immediately
            foreach (TreeViewer treeViewer in treeViewers.Values)
                treeViewer.ApplyStyle(settings.use_npp_styling);
        }

        private static void CopyPathToCurrentLine()
        {
            int line = Npp.editor.GetCurrentLineNumber();
            string result = PathToLine(line);
            if (result.Length == 0)
            {
                MessageBox.Show($"Did not find a node on line {line} of this file",
                    "Could not find a node on this line",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            Npp.TryCopyToClipboard(result);
        }

        private static string PathToLine(int line)
        {
            string fname = Npp.notepad.GetCurrentFilePath();
            JNode json;
            if (!fname_jsons.TryGetValue(fname, out json))
            {
                if (grepperForm != null && grepperForm.fname == fname)
                    json = grepperForm.grepper.fname_jsons;
            }
            if (json == null)
            {
                json = TryParseJson(Npp.FileExtension() == "jsonl");
                if (json == null)
                    return "";
            }
            return json.PathToFirstNodeOnLine(line, new List<string>(), Main.settings.key_style);
        }

        /// <summary>
        /// Try to parse a JSON document and then open up the tree view.<br></br>
        /// If is_json_lines or the file extension is ".jsonl", try to parse it as a JSON Lines document.<br></br>
        /// If the tree view is already open, close it instead.
        /// </summary>
        static void OpenJsonTree(bool is_json_lines = false)
        {
            if (openTreeViewer != null)
            {
                if (openTreeViewer.IsDisposed)
                    openTreeViewer = null;
                else
                    Npp.notepad.HideDockingForm(openTreeViewer);
            }
            treeViewers.TryGetValue(active_fname, out TreeViewer treeViewer);
            if (treeViewer != null && !treeViewer.IsDisposed)
            {
                // if the tree view is open, hide the tree view and then dispose of it
                // if the grepper form is open, this should just toggle it.
                // it's counterintuitive to the user that the plugin command would toggle
                // a tree view other than the one they're currently looking at
                bool was_visible = treeViewer.Visible;
                if (!was_visible
                    && grepperForm != null
                    && grepperForm.tv != null && !grepperForm.tv.IsDisposed
                    && Npp.notepad.GetCurrentFilePath() == grepperForm.fname)
                {
                    if (grepperForm.tv.Visible)
                        Npp.notepad.HideDockingForm(grepperForm.tv);
                    else Npp.notepad.ShowDockingForm(grepperForm.tv);
                    return;
                }
                Npp.notepad.HideDockingForm(treeViewer);
                treeViewer.Close();
                if (was_visible)
                    return;
                if (openTreeViewer != null && treeViewer == openTreeViewer)
                {
                    openTreeViewer = null;
                    treeViewers.Remove(active_fname);
                    return;
                }
            }
            if (Npp.FileExtension() == "jsonl") // jsonl is the canonical file path for JSON Lines docs
                is_json_lines = true;
            JNode json = TryParseJson(is_json_lines);
            if (json == null)
                return; // don't open the tree view for non-json files
            openTreeViewer = new TreeViewer(json);
            treeViewers[active_fname] = openTreeViewer;
            DisplayJsonTree(openTreeViewer, json, $"Json Tree View for {openTreeViewer.RelativeFilename()}");
        }

        static void OpenGrepperForm()
        {
            if (grepperForm != null && !grepperForm.IsDisposed)
                grepperForm.Focus();
            else
            {
                grepperForm = new GrepperForm();
                grepperForm.Show();
            }
        }

        public static void DisplayJsonTree(TreeViewer treeViewer, JNode json, string title)
        {
            using (Bitmap newBmp = new Bitmap(16, 16))
            {
                Graphics g = Graphics.FromImage(newBmp);
                ColorMap[] colorMap = new ColorMap[1];
                colorMap[0] = new ColorMap();
                colorMap[0].OldColor = Color.Fuchsia;
                colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                ImageAttributes attr = new ImageAttributes();
                attr.SetRemapTable(colorMap);
                g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                tbIcon = Icon.FromHandle(newBmp.GetHicon());
            }

            NppTbData _nppTbData = new NppTbData();
            _nppTbData.hClient = treeViewer.Handle;
            _nppTbData.pszName = title;
            // the dlgDlg should be the index of funcItem where the current function pointer is in
            // this case is 15.. so the initial value of funcItem[15]._cmdID - not the updated internal one !
            _nppTbData.dlgID = jsonTreeId;
            // define the default docking behaviour
            _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
            _nppTbData.hIconTab = (uint)tbIcon.Handle;
            _nppTbData.pszModuleName = PluginName;
            IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
            Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            // Following message will toogle both menu item state and toolbar button
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[jsonTreeId]._cmdID, 1);
            // now populate the tree and show it
            Npp.SetLangJson();
            treeViewer.tbData = _nppTbData;
            treeViewer.JsonTreePopulate(json);
            Npp.notepad.ShowDockingForm(treeViewer);
            treeViewer.QueryBox.Focus();
            // select QueryBox on startup
            // note that this is only possible because we changed the access modifier
            // of that control from private (the default) to internal.
        }

        static void ShowAboutForm()
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
            aboutForm.Focus();
        }
        #endregion
    }
}   
