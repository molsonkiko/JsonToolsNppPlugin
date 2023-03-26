﻿// NPP plugin platform for .Net v0.91.57 by Kasper B. Graversen etc.
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Kbg.Demo.Namespace.Properties;
using Kbg.NppPluginNET.PluginInfrastructure;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using JSON_Tools.Forms;
using JSON_Tools.Tests;
using System.Linq;
using System.ComponentModel;

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
                                                            settings.allow_comments,
                                                            settings.linting,
                                                            false,
                                                            settings.allow_nan_inf);
        public static RemesParser remesParser = new RemesParser();
        public static YamlDumper yamlDumper = new YamlDumper();
        public static string active_fname = null;
        public static TreeViewer openTreeViewer = null;
        public static Dictionary<string, TreeViewer> treeViewers = new Dictionary<string, TreeViewer>();
        private static Dictionary<IntPtr, string> treeviewer_buffers_renamed = new Dictionary<IntPtr, string>();
        private static bool should_rename_grepperForm = false;
        public static GrepperForm grepperForm = null;
        public static bool grepperTreeViewJustOpened = false;
        public static Dictionary<string, JsonLint[]> fname_lints = new Dictionary<string, JsonLint[]>();
        public static Dictionary<string, JNode> fname_jsons = new Dictionary<string, JNode>();
        private static string schemasToFnamePatternsFname = null;
        private static JObject schemasToFnamePatterns = new JObject();
        private static SchemaCache schemaCache = new SchemaCache(16);
        private static readonly Func<JNode, JsonSchemaValidator.ValidationProblem?> schemasToFnamePatterns_SCHEMA = JsonSchemaValidator.CompileValidationFunc((JObject)new JsonParser().Parse("{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"patternProperties\":{\".+\":{\"items\":{\"type\":\"string\"},\"minItems\":1,\"type\":\"array\"},\"^$\":false},\"properties\":{},\"required\":[],\"type\":\"object\"}"));
        // toolbar icons
        static Bitmap tbBmp_tbTab = Resources.star_bmp;
        static Icon tbIcon = null;

        // fields related to forms
        static internal int jsonTreeId = -1;
        static internal int grepperFormId = -1;
        static internal int AboutFormId = -1;
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
            PluginBase.SetCommand(0, "&Documentation", docs);
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
            PluginBase.SetCommand(11, "&Validate JSON against JSON schema", () => ValidateJson());
            PluginBase.SetCommand(12, "Choose schemas to automatically validate &filename patterns", MapSchemasToFnamePatterns);
            PluginBase.SetCommand(13, "Generate sc&hema from JSON", GenerateJsonSchema);
            PluginBase.SetCommand(14, "Generate &random JSON from schema", GenerateRandomJson);
            PluginBase.SetCommand(15, "---", null);
            PluginBase.SetCommand(16, "JSON to &YAML", DumpYaml);
            PluginBase.SetCommand(17, "Run &tests", async () => await TestRunner.RunAll());
            PluginBase.SetCommand(18, "A&bout", ShowAboutForm); AboutFormId = 18;

            // write the schema to fname patterns file if it doesn't exist, then parse it
            SetSchemasToFnamePatternsFname();
            var schemasToFnamePatternsFile = new FileInfo(schemasToFnamePatternsFname);
            if (!schemasToFnamePatternsFile.Exists || schemasToFnamePatternsFile.Length == 0)
                WriteSchemasToFnamePatternsFile(schemasToFnamePatterns);
            ParseSchemasToFnamePatternsFile();
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
                    // When a new buffer is activated, we need to reset the connector to the Scintilla editing component.
                    // This is usually unnecessary, but if there are multiple instances or multiple views,
                    // we need to track which of the currently visible buffers are actually being edited.
                    Npp.editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
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
                        if (Npp.notepad.GetCurrentFilePath() == grepperForm.tv.fname)
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
                    ValidateIfFilenameMatches(new_fname);
                    return;
                // when closing a file
                case (uint)NppMsg.NPPN_FILEBEFORECLOSE:
                    IntPtr buffer_closed_id = notification.Header.IdFrom;
                    string buffer_closed = Npp.notepad.GetFilePath(buffer_closed_id);
                    // if you close the file belonging the GrepperForm, delete its tree viewer
                    if (grepperForm != null && grepperForm.tv != null
                        && !grepperForm.tv.IsDisposed
                        && buffer_closed == grepperForm.tv.fname)
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
                    if (grepperForm != null && grepperForm.tv != null && !grepperForm.tv.IsDisposed)
                    {
                        FormStyle.ApplyStyle(grepperForm.tv, settings.use_npp_styling);
                    }
                    foreach (TreeViewer treeViewer2 in treeViewers.Values)
                    {
                        if (treeViewer2 != null)
                        {
                            FormStyle.ApplyStyle(treeViewer2, settings.use_npp_styling);
                        }
                    }
                    return;
                // Before a file is renamed or saved, add a note of the
                // buffer id of the associated treeviewer and what its old name was.
                // That way, the treeviewer can be renamed later.
                // If you do nothing, the renamed treeviewers will be unreachable and
                // the plugin will crash when Notepad++ closes.
                case (uint)NppMsg.NPPN_FILEBEFORESAVE:
                case (uint)NppMsg.NPPN_FILEBEFORERENAME:
                    IntPtr buffer_renamed_id = notification.Header.IdFrom;
                    string buffer_old_name = Npp.notepad.GetFilePath(buffer_renamed_id);
                    if (treeViewers.ContainsKey(buffer_old_name))
                    {
                        treeviewer_buffers_renamed[buffer_renamed_id] = buffer_old_name;
                    }
                    else if (grepperForm != null && grepperForm.tv != null 
                            && !grepperForm.tv.IsDisposed
                            && grepperForm.tv.fname == buffer_old_name)
                    {
                        should_rename_grepperForm = true;
                    }
                    return;
                // After the file is renamed or saved:
                // 1. change the fname attribute of any treeViewers that were renamed.
                // 2. Remap the new fname to that treeviewer and remove the old fname from treeViewers.
                // 3. when the schemas to fname patterns file is saved, parse it and validate to make sure it's ok
                // 4. if the file matches a pattern in schemasToFnamePatterns, validate with the appropriate schema
                case (uint)NppMsg.NPPN_FILESAVED:
                case (uint)NppMsg.NPPN_FILERENAMED:
                    buffer_renamed_id = notification.Header.IdFrom;
                    string buffer_new_name = Npp.notepad.GetFilePath(buffer_renamed_id);
                    ValidateIfFilenameMatches(buffer_new_name);
                    if (buffer_new_name == schemasToFnamePatternsFname)
                    {
                        ParseSchemasToFnamePatternsFile();
                    }
                    if (treeviewer_buffers_renamed.TryGetValue(buffer_renamed_id, out buffer_old_name))
                    {
                        treeviewer_buffers_renamed.Remove(buffer_renamed_id);
                        TreeViewer renamed = treeViewers[buffer_old_name];
                        renamed.Rename(buffer_new_name);
                        treeViewers.Remove(buffer_old_name);
                        treeViewers[buffer_new_name] = renamed;
                    }
                    else if (should_rename_grepperForm)
                    {
                        grepperForm.tv.fname = buffer_new_name;
                        should_rename_grepperForm = false;
                    }
                    return;
                // if a treeviewer was slated for renaming, just cancel that
                case (uint)NppMsg.NPPN_FILERENAMECANCEL:
                    buffer_renamed_id = notification.Header.IdFrom;
                    treeviewer_buffers_renamed.Remove(buffer_renamed_id);
                    should_rename_grepperForm = false;
                    return;
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
                TreeViewer tv = treeViewers[key];
                if (tv == null || !tv.IsDisposed)
                    continue;
                tv.Dispose();
                treeViewers[key] = null;
            }
            WriteSchemasToFnamePatternsFile(schemasToFnamePatterns);
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
            string text = Npp.editor.GetText(len + 1);
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
                if (e is JsonParserException je)
                    Npp.editor.GotoPos(je.Pos);
                    // unfortunately, this position is going to be wrong if there are non-ASCII characters in the document.
                    // that's because the position is the character number in the C# UTF-16 representation of the document,
                    // and the version displayed in the editor will probably be UTF-8.
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
                        sb.AppendLine($"Syntax error on line {lint.line} (position {lint.pos}, char {lint.curChar}): {lint.message}");
                    }
                    Npp.AddLine(sb.ToString());
                    Npp.notepad.OpenFile(fname);
                }
            }
            fname_jsons[fname] = json;
            return json;
        }

        /// <summary>
        /// create a new file and pretty-print this JSON in it, then set the lexer language to JSON.
        /// </summary>
        /// <param name="json"></param>
        public static void PrettyPrintJsonInNewFile(JNode json)
        {
            string printed = json.PrettyPrintAndChangeLineNumbers(settings.indent_pretty_print, settings.sort_keys, settings.pretty_print_style);
            Npp.notepad.FileNew();
            Npp.editor.SetText(printed);
            Npp.SetLangJson();
        }

        /// <summary>
        /// overwrite the current file with its JSON in pretty-printed format
        /// </summary>
        static void PrettyPrintJson()
        {
            JNode json = TryParseJson();
            if (json == null) return;
            Npp.editor.SetText(json.PrettyPrintAndChangeLineNumbers(settings.indent_pretty_print, settings.sort_keys, settings.pretty_print_style));
            Npp.SetLangJson();
        }

        /// <summary>
        /// overwrite the current file with its JSON in compressed format
        /// </summary>
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
            Npp.editor.SetText(result);
        }

        //form opening stuff

        static void OpenSettings()
        {
            settings.ShowDialog();
            jsonParser.allow_nan_inf = settings.allow_nan_inf;
            jsonParser.allow_datetimes = settings.allow_datetimes;
            jsonParser.allow_comments = settings.allow_comments;
            jsonParser.allow_singlequoted_str = settings.allow_singlequoted_str;
            jsonParser.lint = settings.linting ? new List<JsonLint>() : null;
            // make sure grepperForm gets these new settings as well
            if (grepperForm != null && !grepperForm.IsDisposed)
            {
                grepperForm.grepper.json_parser = jsonParser.Copy();
                grepperForm.grepper.max_threads_parsing = settings.max_threads_parsing;
                if (grepperForm.tv != null && !grepperForm.tv.IsDisposed)
                {
                    FormStyle.ApplyStyle(grepperForm.tv, settings.use_npp_styling);
                    grepperForm.tv.use_tree = settings.use_tree;
                    grepperForm.tv.max_size_full_tree_MB = settings.max_size_full_tree_MB;
                }
            }
            // when the user changes their mind about whether to use editor styling
            // for the tree viewer, reflect their decision immediately
            foreach (TreeViewer treeViewer in treeViewers.Values)
                FormStyle.ApplyStyle(treeViewer, settings.use_npp_styling);
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
                if (grepperForm != null 
                    && grepperForm.tv != null && !grepperForm.tv.IsDisposed
                    && grepperForm.tv.fname == fname)
                {
                    json = grepperForm.grepper.fname_jsons;
                }
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
                    && Npp.notepad.GetCurrentFilePath() == grepperForm.tv.fname)
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
            treeViewer.JsonTreePopulate(json);
            //treeViewer.JsonTreePopulateInBackground(json);
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

        /// <summary>
        /// parse a schema file, warning the user and returning null if parsing failed
        /// </summary>
        /// <param name="schema_path"></param>
        /// <returns></returns>
        public static JNode ParseSchemaFile(string schema_path)
        {
            string schema_text = File.ReadAllText(schema_path);
            JNode schema;
            try
            {
                schema = jsonParser.Parse(schema_text);
                return schema;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"While trying to parse the schema at path {schema_path}, the following error occurred:\r\n{ex}", "error while trying to parse schema", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Prompt the user to choose a locally saved JSON schema file,
        /// parse the JSON schema,<br></br>
        /// and try to validate the currently open file against the schema.<br></br>
        /// Send the user a message telling the user if validation succeeded,
        /// or if it failed, where the first error was.
        /// </summary>
        static void ValidateJson(string schema_path = null, bool message_on_success = true)
        {
            JNode json = TryParseJson();
            if (json == null) return;
            string cur_fname = Npp.notepad.GetCurrentFilePath();
            if (schema_path == null)
            {
                FileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 0;
                openFileDialog.Title = "Select JSON schema file to validate against";
                if (openFileDialog.ShowDialog() != DialogResult.OK || !openFileDialog.CheckFileExists)
                    return;
                schema_path = openFileDialog.FileName;
            }
            if (!schemaCache.Get(schema_path, out var validator))
            {
                schemaCache.Add(schema_path);
                validator = schemaCache[schema_path];
            }
            JsonSchemaValidator.ValidationProblem? problem;
            try
            {
                problem = validator(json);
            }
            catch (Exception e)
            {
                MessageBox.Show($"While validating JSON against the schema at path {schema_path}, the following error occurred:\r\n{e}",
                    "Error while validating JSON against schema", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (problem != null)
            {
                MessageBox.Show($"The JSON in file {cur_fname} DOES NOT validate against the schema at path {schema_path}. Problem description:\n{problem}",
                    "Validation failed...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Npp.editor.GotoLine((int)problem?.line_num);
                return;
            }
            if (!message_on_success) return;
            MessageBox.Show($"The JSON in file {cur_fname} validates against the schema at path {schema_path}.",
                "Validation succeeded!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Analyze the current JSON file and generate a minimal JSON schema that describes it.
        /// </summary>
        static void GenerateJsonSchema()
        {
            JNode json = TryParseJson();
            if (json == null) return;
            JNode schema;
            try
            {
                schema = JsonSchemaMaker.GetSchema(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not generate a JSON schema. Got the following error:\n{ex}",
                    "JSON schema generation error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }
            PrettyPrintJsonInNewFile(schema);
        }

        /// <summary>
        /// Generate random JSON that conforms to the schema of the current JSON file
        /// (as generated by JsonSchemaMaker from JsonSchema.cs)
        /// unless the current JSON file *is* a schema, in which case the random JSON
        /// will conform to the current file.
        /// </summary>
        static void GenerateRandomJson()
        {
            JNode json = TryParseJson();
            if (json == null) return;
            JNode randomJson = new JNode();
            try
            {
                // try to use the currently open file as a schema
                randomJson = RandomJsonFromSchema.RandomJson(json, settings.minArrayLength, settings.maxArrayLength, settings.extended_ascii_strings);
            }
            catch
            {
                try
                {
                    // the most likely reason for an exception above is that the JSON file wasn't a schema at all.
                    // we will instead build a JSON schema from the file, and use that as the basis for generating random JSON.
                    JNode schema = JsonSchemaMaker.GetSchema(json);
                    randomJson = RandomJsonFromSchema.RandomJson(schema, settings.minArrayLength, settings.maxArrayLength, settings.extended_ascii_strings);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"While trying to generate random JSON from this schema, got an error:\n{ex}");
                    return;
                }
            }
            PrettyPrintJsonInNewFile(randomJson);
        }

        /// <summary>
        /// save schemasToFnamePatterns to a JSON file so it can be restored next time
        /// </summary>
        /// <param name="schemasToPatterns">object where keys are names of JSON schema files and objects are lists of C# regex matching filenames</param>
        static void WriteSchemasToFnamePatternsFile(JObject schemasToPatterns)
        {
            Npp.CreateConfigSubDirectoryIfNotExists();
            using (var fp = new StreamWriter(schemasToFnamePatternsFname, false, Encoding.UTF8))
            {
                fp.WriteLine("// this file determines when automatic JSON validation should be performed");
                fp.WriteLine("// each key must be the filename of a JSON schema file");
                fp.WriteLine("// each value must be a non-empty list of valid C# regular expressions (e.g., [\"blah.*\\\\.txt\"]");
                fp.WriteLine("// thus, if this file contained {\"c:\\\\path\\\\to\\\\foo_schema.json\": [\"blah.*\\\\.txt\"]}");
                fp.WriteLine("// it would automatically perform validation using \"c:\\\\path\\\\to\\\\foo_schema.json\" whenever a \".txt\" file with the substring \"blah\" in its name was opened.");
                fp.Write(schemasToPatterns.PrettyPrint());
                fp.Flush();
            }
        }

        /// <summary>
        /// parse the schemas to fnames file, and perform validation to make sure does its job
        /// </summary>
        static void ParseSchemasToFnamePatternsFile()
        {
            var schemasToFnamePatternsFile = new FileInfo(schemasToFnamePatternsFname);
            if (!schemasToFnamePatternsFile.Exists || schemasToFnamePatternsFile.Length == 0)
            {
                WriteSchemasToFnamePatternsFile(schemasToFnamePatterns);
                schemasToFnamePatternsFile.Refresh();
            }
            using (var fp = new StreamReader(schemasToFnamePatternsFile.OpenRead(), Encoding.UTF8, true))
            {
                try
                {
                    JsonParser jParser = new JsonParser(allow_comments: true);
                    schemasToFnamePatterns = (JObject)jParser.Parse(fp.ReadToEnd());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to parse the schemas to fname patterns file. Got error\r\n{ex}");
                    return;
                }
            }
            // now validate schemasToFnamePatterns
            // (it must be an object, the keys must not be empty strings, and the children must be non-empty arrays of strings)
            var vp = schemasToFnamePatterns_SCHEMA(schemasToFnamePatterns);
            if (vp != null)
            {
                MessageBox.Show($"Validation of the schemas to fnames patterns JSON must be an object mapping filenames to non-empty arrays of valid regexes (strings).\r\nThere was the following validation problem:\r\n{vp?.ToString()}",
                    "schemas to fnames patterns JSON badly formatted",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                schemasToFnamePatterns = new JObject();
                return;
            }
            // now make sure that all the regexes compile
            var fnames = schemasToFnamePatterns.children.Keys.ToArray<string>();
            foreach (string fname in fnames)
            {
                string msgIfBadFname = $"No schema exists at path {fname}.";
                try
                {
                    if (!new FileInfo(fname).Exists)
                    {
                        MessageBox.Show(msgIfBadFname, msgIfBadFname, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        schemasToFnamePatterns.children.Remove(fname);
                        continue;
                    }

                }
                catch
                {
                    MessageBox.Show(msgIfBadFname, msgIfBadFname, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    schemasToFnamePatterns.children.Remove(fname);
                    continue;
                }
                schemaCache.Add(fname);
                JArray patterns = (JArray)schemasToFnamePatterns[fname];
                JArray regexes = new JArray();
                foreach (JNode patternNode in patterns.children)
                {
                    string pattern = (string)patternNode.value;
                    try
                    {
                        var regex = new Regex(pattern);
                        regexes.children.Add(new JRegex(regex));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"While testing all the regexes associated with file {fname},\r\nregular expression {pattern} failed to compile due to an error:\r\n{ex}",
                            "Regex did not compile",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                // now we replace all the patterns with all the correctly formatted regexes.
                // if no regexes were valid, we just won't use this schema
                if (regexes.Length == 0)
                    schemasToFnamePatterns.children.Remove(fname);
                else schemasToFnamePatterns[fname] = regexes;
            }
        }

        /// <summary>
        /// open the schemas to fname patterns file in Notepad++ so the user can edit it
        /// </summary>
        static void MapSchemasToFnamePatterns()
        {
            var schemasToFnamePatternsFile = new FileInfo(schemasToFnamePatternsFname);
            if (!schemasToFnamePatternsFile.Exists)
                WriteSchemasToFnamePatternsFile(schemasToFnamePatterns);
            Npp.notepad.OpenFile(schemasToFnamePatternsFname);
        }

        static void SetSchemasToFnamePatternsFname()
        {
            var config_dir = Npp.notepad.GetConfigDirectory();
            schemasToFnamePatternsFname = Path.Combine(config_dir, Main.PluginName, "schemasToFnamePatterns.json");
        }

        /// <summary>
        /// check if this filename matches any filename patterns associated with any schema<br></br>
        /// if it does, perform validation.<br></br>
        /// if validation fails, notify the user. Don't do anything if validation succeeds.
        /// </summary>
        /// <param name="fname"></param>
        static void ValidateIfFilenameMatches(string fname)
        {
            foreach (string schema_fname in schemasToFnamePatterns.children.Keys)
            {
                JArray fname_patterns = (JArray)schemasToFnamePatterns[schema_fname];
                foreach (JNode pat in fname_patterns.children)
                {
                    var regex = ((JRegex)pat).regex;
                    if (!regex.IsMatch(fname)) continue;
                    // the filename matches a pattern for this schema, so we'll try to validate it.
                    ValidateJson(schema_fname, false);
                    return;
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// a storage class for compiled JSON schemas that uses a least-recently-used cache for storage.<br></br>
    /// If a request is made for a schema, the cache checks if the file was edited since the last time
    /// it was compiled.<br></br>
    /// If so, the file is re-cached with an updated schema.
    /// </summary>
    internal class SchemaCache
    {
        LruCache<string, Func<JNode, JsonSchemaValidator.ValidationProblem?>> cache;
        Dictionary<string, DateTime> lastRetrieved;

        public SchemaCache(int capacity)
        {
            cache = new LruCache<string, Func<JNode, JsonSchemaValidator.ValidationProblem?>>(capacity);
            lastRetrieved = new Dictionary<string, DateTime>();
        }

        public bool Get(string fname, out Func<JNode, JsonSchemaValidator.ValidationProblem?> validator)
        {
            validator = null;
            if (!cache.cache.ContainsKey(fname)) return false;
            var fileInfo = new FileInfo(fname);
            var retrieved = lastRetrieved[fname];
            if (fileInfo.LastWriteTime > retrieved)
            {
                // the file has been edited since we cached it.
                // thus, we need to read the file, parse and compile the schema, and then re-cache it.
                Add(fname);
            }
            validator = cache[fname];
            return true;
        }

        /// <summary>
        /// read the schema file, parse it, and cache the compiled schema
        /// </summary>
        /// <param name="fname"></param>
        public void Add(string fname)
        {
            JNode schema = Main.ParseSchemaFile(fname);
            if (schema == null) return;
            if (cache.isFull)
            {
                // the cache is about to have its oldest key purged
                // we find out what that is so we can also purge it from lastRetrieved
                string lastFnameAdded = cache.OldestKey();
                lastRetrieved.Remove(lastFnameAdded);
            }
            lastRetrieved[fname] = DateTime.Now;
            cache[fname] = JsonSchemaValidator.CompileValidationFunc(schema);
        }

        public Func<JNode, JsonSchemaValidator.ValidationProblem?> this[string fname]
        {
            get { return cache[fname]; }
        }
    }
}   
