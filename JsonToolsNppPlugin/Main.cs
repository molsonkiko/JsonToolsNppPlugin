// NPP plugin platform for .Net v0.91.57 by Kasper B. Graversen etc.
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
using Kbg.NppPluginNET.PluginInfrastructure;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using JSON_Tools.Forms;
using JSON_Tools.Tests;
using System.Linq;
using System.Runtime.Serialization;

namespace Kbg.NppPluginNET
{
    class Main
    {
        #region " Fields "
        internal const int UNDO_BUFFER_SIZE = 64;
        internal const string PluginName = "JsonTools";
        // general stuff things
        public static Settings settings = new Settings();
        public static JsonParser jsonParser = new JsonParser(settings.logger_level,
                                                             settings.allow_datetimes,
                                                             false,
                                                             false);
        public static RemesParser remesParser = new RemesParser();
        public static YamlDumper yamlDumper = new YamlDumper();
        public static string activeFname = null;
        public static Dictionary<string, JsonFileInfo> jsonFileInfos = new Dictionary<string, JsonFileInfo>();
        // tree view stuff
        public static TreeViewer openTreeViewer = null;
        private static Dictionary<IntPtr, string> jsonFilesRenamed = new Dictionary<IntPtr, string>();
        // grepper form stuff
        private static bool shouldRenameGrepperForm = false;
        public static GrepperForm grepperForm = null;
        public static bool grepperTreeViewJustOpened = false;
        public static bool pluginIsEditing = false;
        // stuff related to selection management
        // sometimes it is best to forget selections if the user performs an action
        // that makes the selections no longer map to the JSON elements that had been selected
        // warn the user the first time this happens
        public static bool hasWarnedSelectionsForgotten = false;
        // sort form stuff
        public static SortForm sortForm = null;
        // error form stuff
        public static ErrorForm errorForm = null;
        // schema auto-validation stuff
        private static string schemasToFnamePatternsFname = null;
        private static JObject schemasToFnamePatterns = new JObject();
        private static SchemaCache schemaCache = new SchemaCache(16);
        private static readonly Func<JNode, JsonSchemaValidator.ValidationProblem?> schemasToFnamePatterns_SCHEMA = JsonSchemaValidator.CompileValidationFunc(new JsonParser().Parse("{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\"," +
            "\"properties\":{},\"required\":[],\"type\":\"object\"," + // must be object
            "\"patternProperties\":{" +
                "\".+\":{\"items\":{\"type\":\"string\"},\"minItems\":1,\"type\":\"array\"}," + // nonzero-length keys must be mapped to non-empty string arrays
                "\"^$\":false" + // zero-length keys are not allowed
            "}}"));
        // stuff for periodically parsing and possibly validating a file
        public static DateTime lastEditedTime = DateTime.MaxValue;
        private static long millisecondsAfterLastEditToParse = 1000 * settings.inactivity_seconds_before_parse;
        private static System.Threading.Timer parseTimer = new System.Threading.Timer(DelayedParseAfterEditing, new System.Threading.AutoResetEvent(true), 1000, 1000);
        private static readonly string[] fileExtensionsToAutoParse = new string[] { "json", "jsonc", "jsonl" };
        private static bool currentFileTooBigToAutoParse = true;
        private static bool bufferFinishedOpening = false;
        // toolbar icons
        static Icon treeIcon = null;
        static Icon tbIcon = null;

        // fields related to forms
        static internal int jsonTreeId = -1;
        static internal int grepperFormId = -1;
        static internal int AboutFormId = -1;
        static internal int sortFormId = -1;
        static internal int errorFormId = -1;
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
            PluginBase.SetCommand(3, "Path to current p&osition", CopyPathToCurrentPosition, new ShortcutKey(true, true, true, Keys.L));
            PluginBase.SetCommand(4, "Select every val&id JSON in selection", SelectEveryValidJson);
            // Here you insert a separator
            PluginBase.SetCommand(5, "---", null);
            PluginBase.SetCommand(6, "Open &JSON tree viewer", () => OpenJsonTree(), new ShortcutKey(true, true, true, Keys.J)); jsonTreeId = 6;
            PluginBase.SetCommand(7, "&Get JSON from files and APIs", OpenGrepperForm, new ShortcutKey(true, true, true, Keys.G)); grepperFormId = 7;
            PluginBase.SetCommand(8, "Sort arra&ys", OpenSortForm); sortFormId = 6; 
            PluginBase.SetCommand(9, "&Settings", OpenSettings, new ShortcutKey(true, true, true, Keys.S));
            PluginBase.SetCommand(10, "---", null);
            PluginBase.SetCommand(11, "&Validate JSON against JSON schema", () => ValidateJson());
            PluginBase.SetCommand(12, "Choose schemas to automatically validate &filename patterns", MapSchemasToFnamePatterns);
            PluginBase.SetCommand(13, "Generate sc&hema from JSON", GenerateJsonSchema);
            PluginBase.SetCommand(14, "Generate &random JSON from schema", GenerateRandomJson);
            PluginBase.SetCommand(15, "---", null);
            PluginBase.SetCommand(16, "Run &tests", async () => await TestRunner.RunAll());
            PluginBase.SetCommand(17, "A&bout", ShowAboutForm); AboutFormId = 17;
            PluginBase.SetCommand(18, "See most recent syntax &errors in this file", () => OpenErrorForm(activeFname, false)); errorFormId = 18;
            PluginBase.SetCommand(19, "JSON to YAML", DumpYaml);
            PluginBase.SetCommand(20, "---", null);
            PluginBase.SetCommand(21, "Parse JSON Li&nes document", () => OpenJsonTree(true));
            PluginBase.SetCommand(22, "&Array to JSON Lines", DumpJsonLines);
            PluginBase.SetCommand(23, "---", null);
            PluginBase.SetCommand(24, "D&ump selected text as JSON string(s)", DumpSelectedTextAsJsonString);
            PluginBase.SetCommand(25, "Dump JSON string(s) as ra&w text", DumpSelectedJsonStringsAsText);

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
            case (uint)NppMsg.NPPN_FILEBEFOREOPEN:
                bufferFinishedOpening = false;
                break;
            case (uint)NppMsg.NPPN_BUFFERACTIVATED:
                bufferFinishedOpening = true;
                IsCurrentFileBig();
                // When a new buffer is activated, we need to reset the connector to the Scintilla editing component.
                // This is usually unnecessary, but if there are multiple instances or multiple views,
                // we need to track which of the currently visible buffers are actually being edited.
                Npp.editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
                string newFname = Npp.notepad.GetFilePath(notification.Header.IdFrom);
                JsonFileInfo info = null;
                JsonFileInfo newInfo = null;
                bool lastFileHasInfo = activeFname != null && TryGetInfoForFile(activeFname, out info);
                bool newFileHasInfo = TryGetInfoForFile(newFname, out newInfo);
                if (lastFileHasInfo)
                {
                    // check to see if there was a treeview for the file
                    // we just navigated away from
                    if (openTreeViewer != null && openTreeViewer.IsDisposed)
                    {
                        info.tv = null;
                        jsonFileInfos[activeFname] = info;
                        openTreeViewer = null;
                    }
                }
                if (newFileHasInfo)
                {
                    if (newInfo.usesSelections && newInfo.json != null && newInfo.json is JObject newObj && newObj.Length > 0)
                    {
                        // Notepad++ doesn't remember all selections if there are multiple; it only remembers one.
                        // This poses problems for our selection remembering
                        // because if the user ever does anything with a selection other than the JSON selections,
                        // the JSON selections are forgotten.
                        // To get around this, we will always set the selection to a zero-length cursor
                        // at the beginning of the first JSON selection
                        int firstSelStart = newObj.children.Keys.Min(SelectionManager.StartFromStartEnd);
                        SelectionManager.SetSelectionsFromStartEnds(new string[] { $"{firstSelStart},{firstSelStart}" });
                    }
                    if (newInfo.tv != null)
                    {
                        if (newInfo.tv.IsDisposed)
                        {
                            newInfo.tv = null;
                            jsonFileInfos[newFname] = newInfo;
                        }
                        else
                        {
                            // minimize the treeviewer that was visible (if any) and make the new one visible
                            if (openTreeViewer != null)
                                Npp.notepad.HideDockingForm(openTreeViewer);
                            Npp.notepad.ShowDockingForm(newInfo.tv);
                            openTreeViewer = newInfo.tv;
                        }
                    }
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
                activeFname = newFname;
                if (!settings.auto_validate) // if auto_validate is turned on, it'll be validated anyway
                    ValidateIfFilenameMatches(newFname);
                if (newFileHasInfo && newInfo.statusBarSection != null)
                    Npp.notepad.SetStatusBarSection(newInfo.statusBarSection, StatusBarSection.DocType);
                return;
            // when closing a file
            case (uint)NppMsg.NPPN_FILEBEFORECLOSE:
                IntPtr bufferClosedId = notification.Header.IdFrom;
                string bufferClosed = Npp.notepad.GetFilePath(bufferClosedId);
                if (TryGetInfoForFile(bufferClosed, out JsonFileInfo closedInfo))
                {
                    if (openTreeViewer != null && openTreeViewer == closedInfo.tv)
                        openTreeViewer = null;
                    closedInfo.Dispose();
                    jsonFileInfos.Remove(bufferClosed);
                }
                // if you close the file belonging the GrepperForm, delete its tree viewer
                if (grepperForm != null && grepperForm.tv != null
                    && !grepperForm.tv.IsDisposed
                    && bufferClosed == grepperForm.tv.fname)
                {
                    Npp.notepad.HideDockingForm(grepperForm.tv);
                    grepperForm.tv.Close();
                    grepperForm.tv = null;
                    return;
                }
                return;
            // the editor color scheme changed, so update form colors
            case (uint)NppMsg.NPPN_WORDSTYLESUPDATED:
                RestyleEverything();
                return;
            // Before a file is renamed or saved, add a note of the
            // buffer id of the associated treeviewer and what its old name was.
            // That way, the treeviewer can be renamed later.
            // If you do nothing, the renamed treeviewers will be unreachable and
            // the plugin will crash when Notepad++ closes.
            case (uint)NppMsg.NPPN_FILEBEFORESAVE:
            case (uint)NppMsg.NPPN_FILEBEFORERENAME:
                FileBeforeRename(notification.Header.IdFrom);
                return;
            // After the file is renamed or saved:
            // 1. change the fname attribute of any treeViewers that were renamed.
            // 2. Remap the new fname to that treeviewer and remove the old fname from treeViewers.
            // 3. when the schemas to fname patterns file is saved, parse it and validate to make sure it's ok
            // 4. if the file matches a pattern in schemasToFnamePatterns, validate with the appropriate schema
            case (uint)NppMsg.NPPN_FILESAVED:
            case (uint)NppMsg.NPPN_FILERENAMED:
                FileRenamed(notification.Header.IdFrom);
                return;
            // if a treeviewer was slated for renaming, just cancel that
            case (uint)NppMsg.NPPN_FILERENAMECANCEL:
                FileRenameCancel(notification.Header.IdFrom);
                return;
            // if the user did nothing for a while (default 1 second) after editing,
            // re-parse the file and also perform validation if that's enabled.
            case (uint)SciMsg.SCN_MODIFIED:
                // only turn on the flag if the user performed the modification
                lastEditedTime = System.DateTime.UtcNow;
                ChangeJsonSelectionsBasedOnEdit(notification);
                if (openTreeViewer != null)
                    openTreeViewer.shouldRefresh = true;
                break;
                //if (code > int.MaxValue) // windows messages
                //{
                //    int wm = -(int)code;
                //    }
                //}
            }
        }

        static internal void FileBeforeRename(IntPtr bufferRenamedId)
        {
            string bufferOldName = Npp.notepad.GetFilePath(bufferRenamedId);
            jsonFilesRenamed[bufferRenamedId] = bufferOldName;
            if (grepperForm != null && grepperForm.tv != null
                    && !grepperForm.tv.IsDisposed
                    && grepperForm.tv.fname == bufferOldName)
            {
                shouldRenameGrepperForm = true;
            }
        }

        static internal void FileRenamed(IntPtr bufferRenamedId)
        {
            string bufferNewName = Npp.notepad.GetFilePath(bufferRenamedId);
            ValidateIfFilenameMatches(bufferNewName);
            if (bufferNewName == schemasToFnamePatternsFname)
            {
                ParseSchemasToFnamePatternsFile();
            }
            if (jsonFilesRenamed.TryGetValue(bufferRenamedId, out string bufferOldName))
            {
                if (activeFname == bufferOldName)
                    activeFname = bufferNewName;
                if (TryGetInfoForFile(bufferOldName, out JsonFileInfo old_info))
                {
                    jsonFilesRenamed.Remove(bufferRenamedId);
                    old_info.tv?.Rename(bufferNewName);
                    jsonFileInfos.Remove(bufferOldName);
                    jsonFileInfos[bufferNewName] = old_info;
                }
            }
            if (shouldRenameGrepperForm)
            {
                grepperForm.tv.fname = bufferNewName;
                shouldRenameGrepperForm = false;
            }
        }

        static internal void FileRenameCancel(IntPtr bufferRenamedId)
        {
            jsonFilesRenamed.Remove(bufferRenamedId);
            shouldRenameGrepperForm = false;
        }

        static internal void PluginCleanUp()
        {
            if (grepperForm != null && !grepperForm.IsDisposed)
            {
                grepperForm.Close();
                grepperForm.Dispose();
            }
            string[] keys = jsonFileInfos.Keys.ToArray();
            foreach (string key in keys)
            {
                JsonFileInfo info = jsonFileInfos[key];
                if (info == null || !info.IsDisposed)
                    continue;
                info.Dispose();
                jsonFileInfos.Remove(key);
            }
            WriteSchemasToFnamePatternsFile(schemasToFnamePatterns);
            parseTimer.Dispose();
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
        /// Try to parse the current document as JSON (or JSON Lines if is_json_lines or the file extension is ".jsonl").<br></br>
        /// If parsing fails, throw up a message box telling the user what happened.<br></br>
        /// If linting is active and the linter catches anything, throw up a message box
        /// asking the user if they want to view the caught errors in a new buffer.<br></br>
        /// Finally, associate the parsed JSON with the current filename in fname_jsons
        /// and return the JSON.
        /// </summary>
        /// <param name="is_json_lines"></param>
        /// <returns></returns>
        public static (bool fatal, JNode node, bool usesSelections) TryParseJson(bool is_json_lines = false, bool was_autotriggered = false)
        {
            string fname = Npp.notepad.GetCurrentFilePath();
            List<(int start, int end)> selRanges = SelectionManager.GetSelectedRanges();
            (int start, int end) firstSel = selRanges[0];
            int firstSelLen = firstSel.end - firstSel.start;
            bool noTextSelected = (selRanges.Count < 2 && firstSelLen == 0) // one selection of length 0 (i.e., just a cursor)
                // the grepperform's buffer is special, and allowing multiple selections in that file would mess things up
                || (grepperForm != null && grepperForm.tv != null && activeFname == grepperForm.tv.fname);
            bool stopUsingSelections = false;
            int len = Npp.editor.GetLength();
            double sizeThreshold = settings.max_file_size_MB_slow_actions * 1e6;
            JsonFileInfo info;
            if (noTextSelected)
            {
                if (TryGetInfoForFile(fname, out info) && info.usesSelections)
                {
                    // if the user doesn't have any text selected, and they already had one or more selections saved,
                    // preserve the existing selections
                    selRanges = SelectionManager.SetSelectionsFromStartEnds(((JObject)info.json).children.Keys);
                    if (selRanges.Count > 0)
                        noTextSelected = false;
                    else
                    {
                        noTextSelected = true;
                        stopUsingSelections = true;
                    }
                }
            }
            else if (selRanges.Count == 1 && firstSelLen == len)
            {
                // user can revert to no-selection mode by selecting entire doc
                noTextSelected = true;
                stopUsingSelections = true;
            }
            JNode json = new JNode();
            List<JsonLint> lints;
            if (noTextSelected)
            {
                if (was_autotriggered && len > sizeThreshold)
                    return (false, null, false);
                string text = Npp.editor.GetText(len + 1);
                // always parse ".jsonl" documents as Json Lines 
                is_json_lines |= Npp.FileExtension() == "jsonl";
                if (is_json_lines)
                    json = jsonParser.ParseJsonLines(text);
                else
                    json = jsonParser.Parse(text);
                lints = jsonParser.lint.ToList();
            }
            else
            {
                json = new JObject();
                JObject obj = (JObject)json;
                lints = new List<JsonLint>();
                int combinedLength = 0;
                foreach ((int start, int end) in selRanges)
                    combinedLength += end - start;
                if (was_autotriggered && combinedLength > sizeThreshold)
                    return (false, null, false);
                foreach ((int start, int end) in selRanges)
                {
                    string selRange = Npp.GetSlice(start, end);
                    JNode subJson = jsonParser.Parse(selRange);
                    string key = $"{start},{end}";
                    obj[key] = subJson;
                    lints.AddRange(jsonParser.lint);
                }
            }
            int lintCount = lints.Count;
            info = AddJsonForFile(fname, json);
            if (stopUsingSelections)
                info.usesSelections = false;
            else
                info.usesSelections |= !noTextSelected;
            if (lintCount > 0 && settings.offer_to_show_lint)
            {
                string msg = $"There were {lintCount} syntax errors in the document. Would you like to see them?";
                if (MessageBox.Show(msg, "View syntax errors in document?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    == DialogResult.Yes)
                {
                    OpenErrorForm(activeFname, true);
                }
            }
            info.lints = lints;
            if (jsonParser.fatal)
            {
                // unacceptable error, show message box
                string errorMessage = jsonParser.fatalError?.ToString();
                MessageBox.Show($"Could not parse the document because of error\n{errorMessage}",
                                "Error while trying to parse JSON",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                string ext = Npp.FileExtension(fname);
                // if there is a fatal error, don't hijack the doctype status bar section
                // unless it's a normal JSON document type.
                // For instance, we expect a Python file to be un-parseable,
                // and it's not helpful to indicate that it's a JSON document with fatal errors.
                if (fileExtensionsToAutoParse.Contains(ext))
                    Npp.notepad.SetStatusBarSection($"JSON with fatal errors - {lintCount} errors (Alt-P-J-E to view)",
                        StatusBarSection.DocType);
            }
            else
            {
                string doctypeDescription;
                switch (jsonParser.state)
                {
                case ParserState.STRICT: doctypeDescription = "JSON"; break;
                case ParserState.OK: doctypeDescription = "JSON (w/ control chars in strings)"; break;
                case ParserState.NAN_INF: doctypeDescription = "JSON (w/ NaN and/or Infinity)"; break;
                case ParserState.JSONC: doctypeDescription = "JSON with comments"; break;
                case ParserState.JSON5: doctypeDescription = "JSON5"; break;
                case ParserState.BAD: doctypeDescription = "Non-compliant JSON"; break;
                // ParserState.FATAL covered earlier
                default: throw new ArgumentOutOfRangeException("Unreachable");
                }
                string doctypeStatusBarEntry = lintCount == 0
                    ? doctypeDescription
                    : $"{doctypeDescription} - {lintCount} errors (Alt-P-J-E to view)";
                info.statusBarSection = doctypeStatusBarEntry;
                Npp.notepad.SetStatusBarSection(doctypeStatusBarEntry, StatusBarSection.DocType);
            }
            if (info.tv != null)
                info.tv.json = json;
            jsonFileInfos[activeFname] = info;
            return (jsonParser.fatal, json, info.usesSelections);
        }

        public static JsonFileInfo AddJsonForFile(string fname, JNode json)
        {
            if (TryGetInfoForFile(fname, out JsonFileInfo info))
                info.json = json;
            else
                info = new JsonFileInfo(json);
            jsonFileInfos[fname] = info;
            return info;
        }

        /// <summary>
        /// If no JsonFileInfo was associated with fname, return false and info = null<br></br>
        /// returns the JsonFileInfo associated with fname<br></br>
        /// If the info was null or disposed, clear that entry from jsonFileInfos, info = null, and return false
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool TryGetInfoForFile(string fname, out JsonFileInfo info)
        {
            if (!jsonFileInfos.TryGetValue(fname, out info))
                return false;
            if (info == null)
            {
                jsonFileInfos.Remove(fname);
                return false;
            }
            if (info.IsDisposed)
            {
                info = null;
                jsonFileInfos.Remove(fname);
                return false;
            }
            return true;
        }

        public static string PrettyPrintFromSettings(JNode json)
        {
            char indent_char = settings.tab_indent_pretty_print ? '\t' : ' ';
            int indent = settings.tab_indent_pretty_print
                ? 1
                : settings.indent_pretty_print;
            return json.PrettyPrintAndChangePositions(indent, settings.sort_keys, settings.pretty_print_style, int.MaxValue, indent_char);
        }

        /// <summary>
        /// create a new file and pretty-print this JSON in it, then set the lexer language to JSON.
        /// </summary>
        /// <param name="json"></param>
        public static void PrettyPrintJsonInNewFile(JNode json)
        {
            Npp.notepad.FileNew();
            ReformatFileWithJson(json, PrettyPrintFromSettings, false);
        }

        /// <summary>
        /// overwrite the current file or saved selections with its JSON in pretty-printed format
        /// </summary>
        public static void PrettyPrintJson()
        {
            (bool fatal, JNode json, bool usesSelections) = TryParseJson();
            if (fatal || json == null) return;
            ReformatFileWithJson(json, PrettyPrintFromSettings, usesSelections);
        }

        /// <summary>
        /// overwrite the current file or saved selections with its JSON in compressed format
        /// </summary>
        public static void CompressJson()
        {
            (bool fatal, JNode json, bool usesSelections) = TryParseJson();
            if (fatal || json == null) return;
            Func<JNode, string> formatter;
            if (settings.minimal_whitespace_compression)
                formatter = x => x.ToStringAndChangePositions(settings.sort_keys, ":", ",");
            else
                formatter = x => x.ToStringAndChangePositions(settings.sort_keys);
            ReformatFileWithJson(json, formatter, usesSelections);
        }

        /// <summary>
        /// If the file is using one or more selections, use the formatter to reformat each selection separately
        /// Otherwise, replace the text of the file with formatter(json)
        /// </summary>
        /// <param name="json"></param>
        /// <param name="formatter"></param>
        public static void ReformatFileWithJson(JNode json, Func<JNode, string> formatter, bool usesSelections)
        {
            if (usesSelections)
            {
                var obj = (JObject)json;
                int delta = 0;
                var keyChanges = new Dictionary<string, (string newKey, JNode child)>();
                pluginIsEditing = true;
                Npp.editor.BeginUndoAction();
                var keyvalues = obj.children.ToArray();
                Array.Sort(keyvalues, (kv1, kv2) => SelectionManager.StartEndCompareByStart(kv1.Key, kv2.Key));
                foreach (KeyValuePair<string, JNode> kv in keyvalues)
                {
                    int[] startEnd = SelectionManager.ParseStartEnd(kv.Key);
                    int start = startEnd[0], end = startEnd[1];
                    int oldLen = end - start;
                    string printed = formatter(kv.Value);
                    int newStart = start + delta;
                    int newCount = Encoding.UTF8.GetByteCount(printed);
                    Npp.editor.DeleteRange(newStart, oldLen);
                    Npp.editor.InsertText(newStart, printed);
                    int newEnd = newStart + newCount;
                    delta = newEnd - end;
                    keyChanges[kv.Key] = ($"{newStart},{newEnd}", kv.Value);
                }
                pluginIsEditing = false;
                RenameAll(keyChanges, obj);
                SelectionManager.SetSelectionsFromStartEnds(obj.children.Keys);
                Npp.editor.EndUndoAction();
                json = obj;
            }
            else
            {
                string newText = formatter(json);
                Npp.editor.SetText(newText);
                Npp.SetLangJson();
            }
            AddJsonForFile(activeFname, json);
            Npp.RemoveTrailingSOH();
            lastEditedTime = DateTime.MaxValue; // avoid redundant parsing
            IsCurrentFileBig();
        }

        static JNode RenameAll(Dictionary<string, (string, JNode)> keyChanges, JObject obj)
        {
            foreach (string oldKey in keyChanges.Keys)
            {
                obj.children.Remove(oldKey);
            }
            foreach (string oldKey in keyChanges.Keys)
            {
                (string newKey, JNode child) = keyChanges[oldKey];
                if (newKey != null)
                    obj[newKey] = child;
            }
            return obj;
        }

        static void ChangeJsonSelectionsBasedOnEdit(ScNotification notif)
        {
            int modType = notif.ModificationType;
            if (pluginIsEditing
                || notif.Length == IntPtr.Zero
                || ((modType & 3) == 0) // is not a text change modification
                || !TryGetInfoForFile(activeFname, out JsonFileInfo info)
                || !info.usesSelections
                || !(info.json is JObject obj))
                return;
#if DEBUG
            var modTypeFlagsSet = Npp.GetModificationTypeFlagsSet(modType);
#endif // DEBUG
            // just normal deletion/insertion of text (number of chars = notif.Length)
            // for each saved selection, if its start is after notif.Position, increment start by notif.Length
            // do the save for the end of each selection
            int length = notif.Length.ToInt32();
            int start = (int)notif.Position.Value;
            int end = start + length;
            if ((modType & 2) != 0) // text is being deleted, treat length as negative
                length = -length;
            bool isDeletion = length < 0;
            if ((isDeletion && Npp.editor.GetLength() == 0)
                // entire document is being erased, which invalidates all remembered selections
                // we'll just clear them all and revert to whole-document mode
                || obj.Length > settings.max_tracked_json_selections) // too many selections to track changes
            {
                if (!hasWarnedSelectionsForgotten)
                {
                    hasWarnedSelectionsForgotten = true;
                    // use a separate thread to show the messagebox, to avoid weird side effects from
                    // interrupting the main thread with a messagebox in the middle of overwriting the file
                    new System.Threading.Thread(() => MessageBox.Show("JsonTools has forgotten all remembered selections.\r\n" +
                                    "This happens when:\r\n" +
                                    "* the entire document is erased or overwritten by something other than JsonTools OR\r\n" +
                                    $"* you have more than {settings.max_tracked_json_selections} active selections (max_tracked_json_selections in settings)\r\n" +
                                    "You will only receive this reminder the first time this happens.",
                                    "JsonTools forgot all remembered selections.",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    ).Start();
                }
                obj.children.Clear();
                info.usesSelections = false;
                jsonFileInfos[activeFname] = info;
                return;
            }
            var keyChanges = new Dictionary<string, (string, JNode)>();
            foreach (KeyValuePair<string, JNode> kv in obj.children)
            {
                int[] startEnd = SelectionManager.ParseStartEnd(kv.Key);
                int selStart = startEnd[0], selEnd = startEnd[1];
                bool endAfterSelEnd = end >= selEnd;
                if (isDeletion && endAfterSelEnd && start <= selStart)
                {
                    // the entire selection has been deleted, so just remove it
                    keyChanges[kv.Key] = (null, null);
                }
                else if (start < selEnd)
                {
                    if (isDeletion)
                    {
                        if (start < selStart)
                        {
                            if (end > selStart)
                                // text was deleted from a point within the selection to a point before the selection
                                // so the selection now starts where the deletion started
                                selStart = start;
                            else
                                selStart += length; // the deletion was entirely before the selection
                        }
                    }
                    else
                    {
                        if (start <= selStart)
                            selStart += length; // text was inserted before the selection
                        // TODO: this behavior can have some potentially undesirable consequences that I might want to fix
                        // For example, if you have an array selected, and you delete the opening square brace
                        // and then re-insert it in the same place, the re-inserted square brace is treated as text BEFORE
                        // the selection rather than part of it.
                        // However, I'd rather optimize for the (presumably) more common scenario
                        // where you are inserting some text right before a JSON element and that text should be separate.
                    }
                    selEnd = end > selEnd && isDeletion
                        ? start // we deleted a range from after the end of the JSON to some point inside it,
                                // so the selection is truncated to the start of the deletion
                        : selEnd + length; // the inserted/deleted text is wholly within the selection
                    keyChanges[kv.Key] = ($"{selStart},{selEnd}", kv.Value);
                }
            }
            info.json = RenameAll(keyChanges, obj);
            jsonFileInfos[activeFname] = info;
        }

        /// <summary>
        /// if there are one or more selections, dump each selection as a JSON string on a separate line.<br></br>
        /// Otherwise just dump the whole document on the same line.
        /// </summary>
        public static void DumpSelectedTextAsJsonString()
        {
            var selections = SelectionManager.GetSelectedRanges();
            if (SelectionManager.NoTextSelected(selections))
            {
                string text = Npp.editor.GetText(Npp.editor.GetLength());
                JNode textNode = new JNode(text);
                PrettyPrintJsonInNewFile(textNode);
            }
            else
            {
                var sb = new StringBuilder();
                foreach ((int start, int end) in selections)
                {
                    string sel = Npp.GetSlice(start, end);
                    JNode selNode = new JNode(sel);
                    sb.Append(selNode.ToString());
                    sb.Append("\r\n");
                }
                Npp.notepad.FileNew();
                Npp.editor.SetText(sb.ToString());
                Npp.SetLangJson();
                Npp.RemoveTrailingSOH();
                IsCurrentFileBig();
            }
        }

        /// <summary>
        /// dumps the values of any number of selected json strings as raw text.<br></br>
        /// If multiple selections, the raw text of each selection starts on a separate line
        /// </summary>
        public static void DumpSelectedJsonStringsAsText()
        {
            var selections = SelectionManager.GetSelectedRanges();
            selections.Sort(SelectionManager.StartEndCompareByStart);
            var sb = new StringBuilder();
            if (SelectionManager.NoTextSelected(selections))
            {
                string selStrValue = TryGetSelectedJsonStringValue();
                if (selStrValue == null)
                    return;
                sb.Append(selStrValue);
            }
            else
            {
                foreach ((int start, int end) in selections)
                {
                    string selStrValue = TryGetSelectedJsonStringValue(start, end);
                    if (selStrValue == null)
                        return;
                    sb.Append(selStrValue);
                    sb.Append("\r\n");
                }
            }
            Npp.notepad.FileNew();
            Npp.editor.SetText(sb.ToString());
        }

        public static string TryGetSelectedJsonStringValue(int start = -1, int end = -1)
        {
            string text = start < 0 || end < 0
                ? Npp.editor.GetText(Npp.editor.GetLength())
                : Npp.GetSlice(start, end);
            try
            {
                JNode textNode = jsonParser.Parse(text);
                if (textNode.value is string s)
                    return s;
            }
            catch { }
            MessageBox.Show("Selected text is not a JSON string", "Failed to parse selected text as JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
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
            (bool fatal, JNode json, _) = TryParseJson();
            if (fatal || json == null) return;
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
            Npp.RemoveTrailingSOH();
            Npp.notepad.SetCurrentLanguage(LangType.L_YAML);
        }

        /// <summary>
        /// If the current file is a JSON array, open a new buffer with a JSON Lines
        /// document containing all entries in the array.
        /// </summary>
        static void DumpJsonLines()
        {
            (bool fatal, JNode json, bool usesSelections) = TryParseJson();
            if (fatal || json == null) return;
            if (!(json is JArray))
            {
                MessageBox.Show("Only JSON arrays can be converted to JSON Lines format.",
                                "Only arrays can be converted to JSON Lines",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Npp.notepad.FileNew();
            Func<JNode, string> formatter;
            if (settings.minimal_whitespace_compression)
                formatter = x => ((JArray)x).ToJsonLines(settings.sort_keys, ":", ",");
            else
                formatter = x => ((JArray)x).ToJsonLines(settings.sort_keys);
            ReformatFileWithJson(json, formatter, usesSelections);
        }

        //form opening stuff

        static void OpenSettings()
        {
            settings.ShowDialog();
            jsonParser = new JsonParser(settings.logger_level, settings.allow_datetimes, false, false);
            millisecondsAfterLastEditToParse = (settings.inactivity_seconds_before_parse < 1)
                    ? 1000
                    : 1000 * settings.inactivity_seconds_before_parse;
            // make sure grepperForm gets these new settings as well
            if (grepperForm != null && !grepperForm.IsDisposed)
            {
                grepperForm.grepper.json_parser = jsonParser.Copy();
                grepperForm.grepper.max_threads_parsing = settings.max_threads_parsing;
            }
            RestyleEverything();
        }

        /// <summary>
        /// Apply the appropriate styling
        /// (either generic control styling or Notepad++ styling as the case may be)
        /// to all forms.
        /// </summary>
        private static void RestyleEverything()
        {
            if (errorForm != null && !errorForm.IsDisposed)
                FormStyle.ApplyStyle(errorForm, settings.use_npp_styling);
            if (sortForm != null && !sortForm.IsDisposed)
                FormStyle.ApplyStyle(sortForm, settings.use_npp_styling);
            if (grepperForm != null && !grepperForm.IsDisposed)
                FormStyle.ApplyStyle(grepperForm, settings.use_npp_styling);
            foreach (string fname in jsonFileInfos.Keys)
            {
                JsonFileInfo info = jsonFileInfos[fname];
                if (info == null || info.IsDisposed)
                    jsonFileInfos.Remove(fname);
                else if (info.tv != null && !info.tv.IsDisposed)
                    FormStyle.ApplyStyle(info.tv, settings.use_npp_styling);
            }
        }

        /// <summary>
        /// Open the error form if it didn't already exist.
        /// If it was open, close it.
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="wasAutoTriggered"></param>
        private static void OpenErrorForm(string fname, bool wasAutoTriggered)
        {
            bool wasVisible = errorForm != null && errorForm.Visible;
            if ((!TryGetInfoForFile(fname, out JsonFileInfo info)
                || info.lints == null
                || info.lints.Count == 0)
                && !wasAutoTriggered)
            {

                MessageBox.Show($"No JSON syntax errors (at or below {settings.logger_level} level) for {fname}",
                    "No JSON syntax errors for this file",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (wasVisible)
                Npp.notepad.HideDockingForm(errorForm);
            else if (errorForm == null)
            {
                errorForm = new ErrorForm(activeFname, info.lints);
                DisplayErrorForm(errorForm);
            }
            else if (errorForm.SlowReloadExpected(info.lints))
            {
                // for some reason destroying the form and reopening it
                // appears to be cheaper when there are many rows
                Npp.notepad.HideDockingForm(errorForm);
                errorForm.Dispose();
                errorForm.Close();
                errorForm = new ErrorForm(activeFname, info.lints);
                DisplayErrorForm(errorForm);
            }
            else
            {
                errorForm.Reload(activeFname, info.lints);
                Npp.notepad.ShowDockingForm(errorForm);
            }
        }

        private static void DisplayErrorForm(ErrorForm form)
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
                //g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                tbIcon = Icon.FromHandle(newBmp.GetHicon());
            }

            NppTbData _nppTbData = new NppTbData();
            _nppTbData.hClient = form.Handle;
            _nppTbData.pszName = form.Text;
            // the dlgDlg should be the index of funcItem where the current function pointer is in
            // this case is 15.. so the initial value of funcItem[15]._cmdID - not the updated internal one !
            _nppTbData.dlgID = errorFormId;
            // dock on bottom
            _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_BOTTOM | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
            _nppTbData.hIconTab = (uint)tbIcon.Handle;
            _nppTbData.pszModuleName = PluginName;
            IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
            Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            Npp.notepad.ShowDockingForm(form);
        }

        public static void CopyPathToCurrentPosition()
        {
            int pos = Npp.editor.GetCurrentPos();
            string result = PathToPosition(settings.key_style, pos);
            if (result.Length == 0)
            {
                MessageBox.Show($"Did not find a node at position {pos} of this file",
                    "Could not find a node on this line",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            Npp.TryCopyToClipboard(result);
        }

        /// <summary>
        /// get the path to the JNode at postition pos
        /// (or the current caret position if pos is unspecified)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static string PathToPosition(KeyStyle style, int pos = -1)
        {
            if (pos == -1)
                pos = Npp.editor.GetCurrentPos();
            string fname = Npp.notepad.GetCurrentFilePath();
            JNode json;
            bool fatal;
            bool usesSelections = false;
            if (TryGetInfoForFile(fname, out JsonFileInfo info))
            {
                json = info.json;
                usesSelections = info.usesSelections;
            }
            else
            {
                if (grepperForm != null
                    && grepperForm.tv != null && !grepperForm.tv.IsDisposed
                    && grepperForm.tv.fname == fname)
                {
                    json = grepperForm.grepper.fname_jsons;
                }
                else
                    json = null;
            }
            if (json == null)
            {
                (fatal, json, usesSelections) = TryParseJson();
                if (fatal || json == null)
                    return "";
            }
            if (usesSelections)
            {
                var obj = (JObject)json;
                foreach (KeyValuePair<string, JNode> kv in obj.children)
                {
                    int[] startEnd = SelectionManager.ParseStartEnd(kv.Key);
                    int start = startEnd[0], end = startEnd[1];
                    if (pos >= start && pos <= end)
                    {
                        // each sub-json treats the start of its selection as position 0
                        // this means we need to translate the position in the document into a position within that sub-json
                        string formattedKey = JNode.FormatKey(kv.Key, style);    
                        return formattedKey + kv.Value.PathToPosition(pos - start, style);
                    }
                }
                return "";
            }
            else
                return json.PathToPosition(pos, style);
        }

        /// <summary>
        /// Select every valid JSON (according to LoggerLevel.NAN_INF) in the user's selection (or entire document if no text selected)<br></br>
        /// starting with any character in settings.try_parse_start_chars.
        /// </summary>
        public static void SelectEveryValidJson()
        {
            int utf8Len = Npp.editor.GetLength();
            var selections = SelectionManager.GetSelectedRanges();
            if (SelectionManager.NoTextSelected(selections))
            {
                selections.Clear();
                selections.Add((0, utf8Len));
            }
            selections.Sort(SelectionManager.StartEndCompareByStart);
            JsonParser jsonChecker = new JsonParser(LoggerLevel.NAN_INF, false, true);
            var startEnds = new List<string>();
            int lastEnd = 0;
            foreach ((int start, int end) in selections)
            {
                string text = Npp.GetSlice(start, end);
                int ii = 0;
                int len = text.Length;
                int utf8Pos = start;
                while (ii < len)
                {
                    char c = text[ii];
                    if (settings.try_parse_start_chars.Contains(c))
                    {
                        int startUtf8Pos = utf8Pos;
                        int startII = ii;
                        jsonChecker.Reset();
                        jsonChecker.ii = startII;
                        try
                        {
                            JNode result = jsonChecker.ParseSomething(text, 0);
                        }
                        catch { }
                        ii = jsonChecker.ii;
                        utf8Pos += jsonChecker.utf8_pos - startII;
                        if (!jsonChecker.exitedEarly)
                        {
                            int endUtf8Pos = utf8Pos > utf8Len ? utf8Len : utf8Pos;
                            startEnds.Add($"{startUtf8Pos},{endUtf8Pos}");
                        }
                    }
                    else
                    {
                        utf8Pos += 1 + JsonParser.ExtraUTF8Bytes(c);
                        ii++;
                    }
                }
                lastEnd = end;
            }
            if (startEnds.Count == 0)
            {
                MessageBox.Show($"No valid JSON elements starting with chars {settings.try_parse_start_chars} were found in the document",
                    "No valid JSON elements found",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
                SelectionManager.SetSelectionsFromStartEnds(startEnds);
            TryParseJson(false, false);
        }

        /// <summary>
        /// Try to parse a JSON document and then open up the tree view.<br></br>
        /// If is_json_lines or the file extension is ".jsonl", try to parse it as a JSON Lines document.<br></br>
        /// If the tree view is already open, close it instead.
        /// </summary>
        public static void OpenJsonTree(bool is_json_lines = false)
        {
            if (openTreeViewer != null)
            {
                if (openTreeViewer.IsDisposed)
                    openTreeViewer = null;
                else
                    Npp.notepad.HideDockingForm(openTreeViewer);
            }
            if (!TryGetInfoForFile(activeFname, out JsonFileInfo info))
            {
                info = new JsonFileInfo();
            }
            if (info.tv != null && !info.tv.IsDisposed)
            {
                // if the tree view is open, hide the tree view and then dispose of it
                // if the grepper form is open, this should just toggle it.
                // it's counterintuitive to the user that the plugin command would toggle
                // a tree view other than the one they're currently looking at
                bool was_visible = info.tv.Visible;
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
                Npp.notepad.HideDockingForm(info.tv);
                info.tv.Close();
                if (was_visible)
                    return;
                if (openTreeViewer != null && info.tv == openTreeViewer)
                {
                    openTreeViewer.Dispose();
                    openTreeViewer = null;
                    info.tv = null;
                    jsonFileInfos[activeFname] = info;
                    return;
                }
            }
            (_, JNode json, bool usesSelections) = TryParseJson(is_json_lines);
            if (json == null || json == new JNode() || !TryGetInfoForFile(activeFname, out info)) // open a tree view for partially parsed JSON
                return; // don't open the tree view for non-json files
            openTreeViewer = new TreeViewer(json);
            info.tv = openTreeViewer;
            jsonFileInfos[activeFname] = info;
            DisplayJsonTree(openTreeViewer, json, $"Json Tree View for {openTreeViewer.RelativeFilename()}", usesSelections);
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

        public static void DisplayJsonTree(TreeViewer treeViewer, JNode json, string title, bool usesSelections)
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
                //g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
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
            if (!jsonParser.fatal && !usesSelections)
                Npp.SetLangJson();
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETMENUITEMCHECK, PluginBase._funcItems.Items[jsonTreeId]._cmdID, 1);
            // now populate the tree and show it
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
            (bool fatal, JNode json, _) = TryParseJson();
            if (fatal || json == null) return;
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
                Npp.editor.GotoPos((int)problem?.position);
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
            (bool fatal, JNode json, _) = TryParseJson();
            if (fatal || json == null) return;
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
            (bool fatal, JNode json, _) = TryParseJson();
            if (fatal || json == null) return;
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
                    JsonParser jParser = new JsonParser(LoggerLevel.JSON5);
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
            // we're mutating the keys, so make an array first
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
        /// if the filename matches, perform validation and return true.<br></br>
        /// if validation fails, notify the user. Don't do anything if validation succeeds.<br></br>
        /// if the filename doesn't match, return false.
        /// </summary>
        /// <param name="fname"></param>
        static bool ValidateIfFilenameMatches(string fname, bool was_autotriggered = false)
        {
            if (was_autotriggered && Npp.editor.GetLength() > Main.settings.max_file_size_MB_slow_actions * 1e6)
                return false;
            foreach (string schema_fname in schemasToFnamePatterns.children.Keys)
            {
                JArray fname_patterns = (JArray)schemasToFnamePatterns[schema_fname];
                foreach (JNode pat in fname_patterns.children)
                {
                    var regex = ((JRegex)pat).regex;
                    if (!regex.IsMatch(fname)) continue;
                    // the filename matches a pattern for this schema, so we'll try to validate it.
                    ValidateJson(schema_fname, false);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// open the SortForm with path set to the path to the current position
        /// </summary>
        public static void OpenSortForm()
        {
            if (sortForm == null || sortForm.IsDisposed)
                sortForm = new SortForm();
            sortForm.PathTextBox.Text = PathToPosition(KeyStyle.RemesPath);
            sortForm.Show();
            sortForm.Focus();
        }
        #endregion
        #region timer_stuff
        /// <summary>
        /// This callback fires once every second.<br></br>
        /// It checks if the last edit was more than 2 seconds ago.<br></br>
        /// If it was, it checks if the filename is one of ["json", "jsonc", "jsonl"]
        /// and also if the filename is 
        /// </summary>
        /// <param name="state"></param>
        private static void DelayedParseAfterEditing(object s)
        {
            DateTime now = DateTime.UtcNow;
            if (!settings.auto_validate
                || !bufferFinishedOpening
                || currentFileTooBigToAutoParse
                || lastEditedTime == DateTime.MaxValue // set when we don't want to edit it
                || lastEditedTime.AddMilliseconds(millisecondsAfterLastEditToParse) > now)
                return;
            lastEditedTime = DateTime.MaxValue;
            // an edit happened recently, so check if it's a json file
            // and also check if the file matches a schema validation pattern
            string fname = Npp.notepad.GetCurrentFilePath();
            string ext = Npp.FileExtension(fname);
            if (ValidateIfFilenameMatches(fname)
                || !fileExtensionsToAutoParse.Contains(ext))
                return;
            // filename matches but it's not associated with a schema, so just parse normally
            TryParseJson(false, true);
        }

        /// <summary>
        /// sets currentFileTooBigToAutoParse to (Npp.editor.GetLength() > settings.max_file_size_MB_slow_actions * 1e6)
        /// </summary>
        public static void IsCurrentFileBig()
        {
            currentFileTooBigToAutoParse = Npp.editor.GetLength() > settings.max_file_size_MB_slow_actions * 1e6;
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

    public class JsonFileInfo : IDisposable
    {
        private JNode _json;
        public JNode json
        {
            get { return _json; }
            set
            {
                _json = value;
                if (tv != null)
                    tv.json = value;
            }
        }
        public List<JsonLint> lints;
        public string statusBarSection;
        public TreeViewer tv;
        /// <summary>
        /// if true, rather than the json attribute representing the entire file,
        /// it is a map from positions to the JSON starting at those positions.
        /// </summary>
        public bool usesSelections;
        ///// <summary>
        ///// if not usesSelections, this is null
        ///// if usesSelections, indicates if the user has performed an edit that included text inside of a JSON selection
        ///// </summary>
        //public Dictionary<string, bool> selectionsDirty;
        public bool IsDisposed { get; private set; }

        public JsonFileInfo(JNode json = null, List<JsonLint> lints = null, string statusBarSection = null, TreeViewer tv = null, bool hasJsonSelections = false)
        {
            _json = json;
            this.lints = lints;
            this.statusBarSection = statusBarSection;
            this.tv = tv;
            this.usesSelections = hasJsonSelections;
            IsDisposed = false;
            //selectionsDirty = new Dictionary<string, bool>();
        }

        public void Dispose()
        {
            if (tv != null)
            {
                Npp.notepad.HideDockingForm(tv);
                tv.Close();
                tv.Dispose();
                tv = null;
            }
            if (json is JArray arr)
                arr.children.Clear();
            else if (json is JObject obj)
                obj.children.Clear();
            lints = null;
            _json = null;
            statusBarSection = null;
            IsDisposed = true;
            usesSelections = false;
            //selectionsDirty.Clear();
        }
    }
}   
