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
        //public static Dictionary<string, TreeViewer> treeViewers = new Dictionary<string, TreeViewer>();
        public static TreeViewer treeViewer = null;
        public static GrepperForm grepperForm = null;
        public static bool grepperTreeViewJustOpened = false;
        //public static string active_fname;
        //public static bool IsWin32 = Npp.notepad.GetPluginConfig().Contains("x86");
        public static Dictionary<string, JsonLint[]> fname_lints = new Dictionary<string, JsonLint[]>();
        public static Dictionary<string, JNode> fname_jsons = new Dictionary<string, JNode>();

        // toolbar icons
        static Bitmap tbBmp = Resources.star;
        static Bitmap tbBmp_tbTab = Resources.star_bmp;
        static Icon tbIco = Resources.star_black_ico;
        static Icon tbIcoDM = Resources.star_white_ico;
        static Icon tbIcon = null;

        // fields related to forms
        static internal int jsonTreeId = -1;
        static internal int grepperFormId = -1;
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
            PluginBase.SetCommand(3, "Path to current &line", PathToCurrentLine, new ShortcutKey(true, true, true, Keys.L));
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
            PluginBase.SetCommand(12, "&Run tests", TestRunner.RunAll);
        }

        static internal void SetToolBarIcon()
        {
            // create struct
            toolbarIcons tbIcons = new toolbarIcons();
			
            // add bmp icon
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            tbIcons.hToolbarIcon = tbIco.Handle;            // icon with black lines
            tbIcons.hToolbarIconDarkMode = tbIcoDM.Handle;  // icon with light grey lines

            // convert to c++ pointer
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);

            // call Notepad++ api
            //Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON_FORDARKMODE, PluginBase._funcItems.Items[idFrmGotToLine]._cmdID, pTbIcons);

            // release pointer
            Marshal.FreeHGlobal(pTbIcons);
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
            // all the triggers below depend on IntPtrs representing buffer id's
            // being convertible into 32-bit integers. This only works for 32-bit Notepad++.
            // For 64-bit Notepad++, the IntPtrs are longs. 
            //if (!IsWin32)
            //    return;
            //// changing tabs
            if (code == (uint)NppMsg.NPPN_BUFFERACTIVATED)
            {
                // check to see if there was a treeview for the file
                // we just navigated away from
                //if (active_fname != null)
                //{
                //    if (treeViewers.TryGetValue(active_fname, out TreeViewer old_tv))
                //    {
                //        if (old_tv.IsDisposed)
                //            // it was closed by user earlier
                //            treeViewers.Remove(active_fname);
                //        else
                //            // minimize the treeviewer that was visible
                //            Npp.notepad.HideDockingForm(old_tv);
                //    }
                //}
                //// now open the treeview for the file just opened, assuming it exists
                //active_fname = Npp.notepad.GetCurrentFilePath();
                //if (treeViewers.TryGetValue(active_fname, out TreeViewer tv))
                //{
                //    if (tv.IsDisposed)
                //        // it was removed earlier, so clean it up
                //        treeViewers.Remove(active_fname);
                //    else
                //        // maximize the treeviewer belonging to this file
                //        Npp.notepad.ShowDockingForm(tv);
                //}
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
                        Npp.notepad.ShowDockingForm(grepperForm.tv);
                        if (treeViewer != null && !treeViewer.IsDisposed)
                            Npp.notepad.HideDockingForm(treeViewer);
                    }
                    else
                    {
                        Npp.notepad.HideDockingForm(grepperForm.tv);
                        if (treeViewer != null && !treeViewer.IsDisposed)
                            Npp.notepad.ShowDockingForm(treeViewer);
                    }
                }
                grepperTreeViewJustOpened = false;
                return;
            }
            // when closing a file
            if (code == (uint)NppMsg.NPPN_FILEBEFORECLOSE)
            {
                // if you close the file belonging the GrepperForm, delete its tree viewer
                if (grepperForm != null && grepperForm.tv != null && !grepperForm.tv.IsDisposed &&
                    Npp.notepad.GetCurrentFilePath() == grepperForm.fname)
                {
                    Npp.notepad.HideDockingForm(grepperForm.tv);
                    grepperForm.tv.Close();
                    grepperForm.tv = null;
                    // also unhide the normal tree viewer if it exists
                    if (treeViewer != null && !treeViewer.IsDisposed)
                        Npp.notepad.ShowDockingForm(treeViewer);
                    return;
                }
                //int buffer_closed_id = notification.Header.IdFrom.ToInt32();
                //string buffer_closed = Npp.notepad.GetFilePath(buffer_closed_id);
                //// clean up data associated with the buffer that was just closed
                //if (!treeViewers.TryGetValue(buffer_closed, out TreeViewer closed_tv))
                //    return;
                //if (!closed_tv.IsDisposed)
                //{
                //    Npp.notepad.HideDockingForm(closed_tv);
                //    closed_tv.Close();
                //}
                //treeViewers.Remove(buffer_closed);
                //fname_jsons.Remove(buffer_closed);
                //return;
            }
            // after an undo (Ctrl + Z) or redo (Ctrl + Y) action
            //if (code == (uint)SciMsg.SCI_UNDO
            //    || code == (uint)SciMsg.SCI_REDO)
            //{
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
            //}
            //if (code > int.MaxValue) // windows messages
            //{
            //    int wm = -(int)code;
            //    // leaving previous tab
            //    if (wm == 0x22A && sShouldResetCaretBack) // =554 WM_MDI_SETACTIVE
            //    {
            //        // set caret line to default on file change
            //        sShouldResetCaretBack = false;
            //        var editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
            //        editor.SetCaretLineBackAlpha(sAlpha);// Alpha.NOALPHA); // default
            //        editor.SetCaretLineBack(sCaretLineColor);
            //    }
            //}
        }

        static internal void PluginCleanUp()
        {
            //treeViewers.Clear();
            if (grepperForm != null && !grepperForm.IsDisposed)
                grepperForm.Close();
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
                grepperForm.grepper.max_api_request_threads = settings.max_api_request_threads;
                grepperForm.grepper.max_threads_parsing = settings.max_threads_parsing;
                if (grepperForm.tv != null && !grepperForm.tv.IsDisposed)
                {
                    grepperForm.tv.use_tree = settings.use_tree;
                    grepperForm.tv.max_size_full_tree_MB = settings.max_size_full_tree_MB;
                }
            }
        }

        private static void PathToCurrentLine()
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
                    return;
            }
            int line = Npp.editor.GetCurrentLineNumber();
            string result = json.PathToFirstNodeOnLine(line, new List<string>(), Main.settings.key_style);
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

        /// <summary>
        /// Try to parse a JSON document and then open up the tree view.<br></br>
        /// If is_json_lines or the file extension is ".jsonl", try to parse it as a JSON Lines document.<br></br>
        /// If the tree view is already open, close it instead.
        /// </summary>
        static void OpenJsonTree(bool is_json_lines = false)
        {
            //TreeViewer tv = null;
            //if (IsWin32)
            //    treeViewers.TryGetValue(cur_fname, out tv);
            //else
            //    tv = treeViewer; // 64-bit windows doesn't use the treeViewers dict
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
                    && Npp.GetCurrentPath() == grepperForm.fname)
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
                //if (IsWin32)
                //    treeViewers.Remove(cur_fname);
                //else treeViewer = null;
            }
            if (Npp.FileExtension() == "jsonl") // jsonl is the canonical file path for JSON Lines docs
                is_json_lines = true;
            JNode json = TryParseJson(is_json_lines);
            if (json == null)
                return; // don't open the tree view for non-json files
            treeViewer = new TreeViewer(json);
            DisplayJsonTree(treeViewer, json);
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

        public static void DisplayJsonTree(TreeViewer treeViewer, JNode json, string title = "Json Tree View")
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
            treeViewer.JsonTreePopulate(json);
            Npp.notepad.ShowDockingForm(treeViewer);
            //if (IsWin32)
            //    treeViewers[cur_fname] = tv;
            treeViewer.QueryBox.Focus();
            // select QueryBox on startup
            // note that this is only possible because we changed the access modifier
            // of that control from private (the default) to internal.
        }
        #endregion
    }
}   
