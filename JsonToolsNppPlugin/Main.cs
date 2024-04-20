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
using PluginNetResources = JSON_Tools.Properties.Resources;

namespace Kbg.NppPluginNET
{
    class Main
    {
        #region " Fields "
        internal const int UNDO_BUFFER_SIZE = 64;
        internal const string PluginName = "JsonTools";
        // general stuff things
        public static Settings settings = new Settings();
        public static IniFileParser iniParser = new IniFileParser();
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
        /// <summary>
        /// we need this boolean to avoid "infinite" loops as follows:<br></br>
        /// 1. The user presses Enter in the error form to parse the document.<br></br>
        /// 2. the parse fails<br></br>
        /// 3. the user is shown a dialog box warning them that the parse failed.<br></br>
        /// 4. the user presses Enter to close the dialog box<br></br>
        /// 5. focus returns to the error form<br></br>
        /// 6. the user releases the Enter key, triggering the ErrorForm_KeyUp event again and returning to step 1.
        /// </summary>
        public static bool errorFormTriggeredParse = false;
        // regex search to json stuff
        public static RegexSearchForm regexSearchForm = null;
        // schema auto-validation stuff
        private static string schemasToFnamePatternsFname = null;
        private static JObject schemasToFnamePatterns = new JObject();
        private static SchemaCache schemaCache = new SchemaCache(16);
        private static readonly JsonSchemaValidator.ValidationFunc schemasToFnamePatterns_SCHEMA = JsonSchemaValidator.CompileValidationFunc(new JsonParser().Parse("{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\"," +
            "\"properties\":{},\"required\":[],\"type\":\"object\"," + // must be object
            "\"patternProperties\":{" +
                "\".+\":{\"items\":{\"type\":\"string\"},\"minItems\":1,\"type\":\"array\"}," + // nonzero-length keys must be mapped to non-empty string arrays
                "\"^$\":false" + // zero-length keys are not allowed
            "}}"), 0);
        // stuff for periodically parsing and possibly validating a file
        public static DateTime lastEditedTime = DateTime.MaxValue;
        private static long millisecondsAfterLastEditToParse = 1000 * settings.inactivity_seconds_before_parse;
        private static System.Threading.Timer parseTimer = new System.Threading.Timer(DelayedParseAfterEditing, new System.Threading.AutoResetEvent(true), 1000, 1000);
        private static readonly string[] fileExtensionsToAutoParse = new string[] { "json", "jsonc", "jsonl", "json5" };
        private static bool bufferFinishedOpening = false;
        ///// <summary>
        ///// this form is always created on the main thread, so mainThreadForm.Invoke(X) could be a way to call X on the main thread.<br></br>
        ///// I am not using this approach at present because it appears to have a noticeable (and annoying) impact on Notepad++ startup.
        ///// </summary>
        //public static Form mainThreadForm;
        // toolbar icons
        static Icon dockingFormIcon = null;
        // indicators (used for selection remembering)
        // we need two so that we can have two touching selections remembered separately
        // because Scintilla merges touching regions with the same indicator
        // the below values appear to be safe defaults, based on looking at some other plugins
        private static int selectionRememberingIndicator1 = 12;
        private static int selectionRememberingIndicator2 = 13;
        // when the indicators are used, need to notify the user if there could be a collision
        private static bool selectionRememberingIndicatorsMayCollide = true;
        private static bool hasWarnedSelectionRememberingIndicatorsMayCollide = false;
        // fields related to forms
        static internal int jsonTreeId = -1;
        static internal int grepperFormId = -1;
        static internal int AboutFormId = -1;
        static internal int sortFormId = -1;
        static internal int errorFormId = -1;
        static internal int regexSearchToJsonFormId = -1;
        static internal int compressId = -1;
        static internal int prettyPrintId = -1;
        static internal int pathToPositionId = -1;
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
            PluginBase.SetCommand(1, "&Pretty-print current JSON file", PrettyPrintJson, new ShortcutKey(true, true, true, Keys.P)); prettyPrintId = 1;
            PluginBase.SetCommand(2, "&Compress current JSON file", CompressJson, new ShortcutKey(true, true, true, Keys.C)); compressId = 2;
            PluginBase.SetCommand(3, "Path to current p&osition", CopyPathToCurrentPosition, new ShortcutKey(true, true, true, Keys.L)); pathToPositionId = 3;
            PluginBase.SetCommand(4, "Select every val&id JSON in selection", SelectEveryValidJson);
            PluginBase.SetCommand(5, "Chec&k JSON syntax now", CheckJsonSyntaxNow);
            // Here you insert a separator
            PluginBase.SetCommand(6, "---", null);
            PluginBase.SetCommand(7, "Open &JSON tree viewer", () => OpenJsonTree(), new ShortcutKey(true, true, true, Keys.J)); jsonTreeId = 7;
            PluginBase.SetCommand(8, "&Get JSON from files and APIs", OpenGrepperForm, new ShortcutKey(true, true, true, Keys.G)); grepperFormId = 8;
            PluginBase.SetCommand(9, "Sort arra&ys", OpenSortForm); sortFormId = 9;
            PluginBase.SetCommand(10, "&Settings", OpenSettings, new ShortcutKey(true, true, true, Keys.S));
            PluginBase.SetCommand(11, "---", null);
            PluginBase.SetCommand(12, "&Validate JSON against JSON schema", () => ValidateJson());
            PluginBase.SetCommand(13, "Choose schemas to automatically validate &filename patterns", MapSchemasToFnamePatterns);
            PluginBase.SetCommand(14, "Generate sc&hema from JSON", GenerateJsonSchema);
            PluginBase.SetCommand(15, "Generate &random JSON from schema", GenerateRandomJson);
            PluginBase.SetCommand(16, "---", null);
            PluginBase.SetCommand(17, "Run &tests", async () => await TestRunner.RunAll());
            PluginBase.SetCommand(18, "A&bout", ShowAboutForm); AboutFormId = 18;
            PluginBase.SetCommand(19, "See most recent syntax &errors in this file", () => OpenErrorForm(activeFname, false)); errorFormId = 19;
            PluginBase.SetCommand(20, "JSON to YAML", DumpYaml);
            PluginBase.SetCommand(21, "---", null);
            PluginBase.SetCommand(22, "Parse JSON Li&nes document", () => OpenJsonTree(DocumentType.JSONL));
            PluginBase.SetCommand(23, "&Array to JSON Lines", DumpJsonLines);
            PluginBase.SetCommand(24, "---", null);
            PluginBase.SetCommand(25, "D&ump selected text as JSON string(s)", DumpSelectedTextAsJsonString);
            PluginBase.SetCommand(26, "Dump JSON string(s) as ra&w text", DumpSelectedJsonStringsAsText);
            PluginBase.SetCommand(27, "---", null);
            PluginBase.SetCommand(28, "Open tree for &INI file", () => OpenJsonTree(DocumentType.INI));
            PluginBase.SetCommand(29, "---", null);
            PluginBase.SetCommand(30, "Rege&x search to JSON", RegexSearchToJson);

            // write the schema to fname patterns file if it doesn't exist, then parse it
            SetSchemasToFnamePatternsFname();
            var schemasToFnamePatternsFile = new FileInfo(schemasToFnamePatternsFname);
            if (!schemasToFnamePatternsFile.Exists || schemasToFnamePatternsFile.Length == 0)
                WriteSchemasToFnamePatternsFile(schemasToFnamePatterns);
            ParseSchemasToFnamePatternsFile();

            // JsonTools uses two indicators (see https://www.scintilla.org/ScintillaDoc.html#Indicators for more about the API)
            // to indicate where remembered selections are.
            if (Npp.nppVersionAtLeast8p5p6 && Npp.notepad.AllocateIndicators(2, out int[] indicators) && indicators.Length >= 2)
            {
                selectionRememberingIndicatorsMayCollide = false;
                selectionRememberingIndicator1 = indicators[0];
                selectionRememberingIndicator2 = indicators[1];
            }
            HideSelectionRememberingIndicators();
        }
        
        /// <summary>
        /// selection-remembering indicators should be hidden
        /// </summary>
        static internal void HideSelectionRememberingIndicators()
        {
            if (selectionRememberingIndicator1 >= 0)
                Npp.editor.IndicSetStyle(selectionRememberingIndicator1, IndicatorStyle.HIDDEN);
            if (selectionRememberingIndicator2 >= 0)
                Npp.editor.IndicSetStyle(selectionRememberingIndicator2, IndicatorStyle.HIDDEN);
        }

        static internal void SetToolBarIcons()
        {
            string iconsToUseChars = settings.toolbar_icons.ToLower();
            var iconInfo = new (Bitmap bmp, Icon icon, Icon iconDarkMode, int id, char representingChar)[]
            {
                (PluginNetResources.json_tree_toolbar_bmp, PluginNetResources.json_tree_toolbar, PluginNetResources.json_tree_toolbar_darkmode,jsonTreeId, 't'),
                (PluginNetResources.json_compress_toolbar_bmp, PluginNetResources.json_compress_toolbar, PluginNetResources.json_compress_toolbar_darkmode, compressId, 'c'),
                (PluginNetResources.json_pretty_print_toolbar_bmp, PluginNetResources.json_pretty_print_toolbar, PluginNetResources.json_pretty_print_toolbar_darkmode, prettyPrintId, 'p'),
                (PluginNetResources.json_path_to_position_toolbar_bmp, PluginNetResources.json_path_to_position_toolbar, PluginNetResources.json_path_to_position_toolbar_darkmode, pathToPositionId, 'o'),
            }
                .Where(x => iconsToUseChars.IndexOf(x.representingChar) >= 0)
                .OrderBy(x => iconsToUseChars.IndexOf(x.representingChar));
            // order the icons according to their order in settings.toolbar_icons, and exclude those without their representing char listed

            foreach ((Bitmap bmp, Icon icon, Icon iconDarkMode, int funcId, char representingChar) in iconInfo)
            {
                // create struct
                toolbarIcons tbIcons = new toolbarIcons();

                // add bmp icon
                tbIcons.hToolbarBmp = bmp.GetHbitmap();
                tbIcons.hToolbarIcon = icon.Handle;
                tbIcons.hToolbarIconDarkMode = iconDarkMode.Handle;

                // convert to c++ pointer
                IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
                Marshal.StructureToPtr(tbIcons, pTbIcons, false);

                // call Notepad++ api
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON_FORDARKMODE,
                    PluginBase._funcItems.Items[funcId]._cmdID, pTbIcons);

                // release pointer
                Marshal.FreeHGlobal(pTbIcons);
            }
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
                lastEditedTime = System.DateTime.UtcNow;
                if (openTreeViewer != null)
                    openTreeViewer.shouldRefresh = true;
                break;
            // a find/replace action was performed.
            // Tag the treeview of the modified file as needing to refresh, and indicate that the document was edited.
            case (uint)NppMsg.NPPN_GLOBALMODIFIED:
                IntPtr bufferModifiedId = notification.Header.hwndFrom;
                string bufferModified = Npp.notepad.GetFilePath(bufferModifiedId);
                if (bufferModified == activeFname)
                    lastEditedTime = System.DateTime.UtcNow;
                if (TryGetInfoForFile(bufferModified, out info) && !(info.tv is null) && !info.tv.IsDisposed)
                    info.tv.shouldRefresh = true;
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
                if (TryGetInfoForFile(bufferOldName, out JsonFileInfo oldInfo))
                {
                    jsonFilesRenamed.Remove(bufferRenamedId);
                    oldInfo.tv?.Rename(bufferNewName);
                    jsonFileInfos.Remove(bufferOldName);
                    jsonFileInfos[bufferNewName] = oldInfo;
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
            if (sortForm != null && !sortForm.IsDisposed)
            {
                sortForm.Close();
                sortForm.Dispose();
            }
            if (regexSearchForm != null && !regexSearchForm.IsDisposed)
            {
                regexSearchForm.Close();
                regexSearchForm.Dispose();
            }
            WriteSchemasToFnamePatternsFile(schemasToFnamePatterns);
            parseTimer.Dispose();
        }
        #endregion

        #region " Menu functions "
        public static void docs()
        {
            string helpUrl = "https://github.com/molsonkiko/JsonToolsNppPlugin";
            try
            {
                var ps = new ProcessStartInfo(helpUrl)
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
        /// Try to parse the current document as JSON (or JSON Lines if isJsonLines or the file extension is ".jsonl").<br></br>
        /// If parsing fails, throw up a message box telling the user what happened.<br></br>
        /// If linting is active and the linter catches anything, throw up a message box
        /// asking the user if they want to view the caught errors in a new buffer.<br></br>
        /// Finally, associate the parsed JSON with the current filename in fnameJsons
        /// and return the JSON.
        /// </summary>
        /// <param name="documentType">what type of document to parse the document as (JSON, JSON Lines, or INI file)</param>
        /// <param name="wasAutotriggered">was triggered by a direct action of the user (e.g., reformatting, opening tree view)</param>
        /// <param name="preferPreviousDocumentType">attempt to re-parse the document in whatever way it was previously parsed (potentially ignoring documentType parameter)</param>
        /// <param name="isRecursion">IGNORE THIS PARAMETER, IT IS ONLY FOR RECURSIVE SELF-CALLS</param>
        /// <param name="ignoreSelections">If true, always parse the entire file even the selected text is valid JSON.</param>
        /// <returns></returns>
        public static (ParserState parserState, JNode node, bool usesSelections, DocumentType DocumentType) TryParseJson(DocumentType documentType = DocumentType.JSON, bool wasAutotriggered = false, bool preferPreviousDocumentType = false, bool isRecursion = false, bool ignoreSelections = false)
        {
            JsonParser jsonParser = JsonParserFromSettings();
            string fname = Npp.notepad.GetCurrentFilePath();
            List<(int start, int end)> selRanges = SelectionManager.GetSelectedRanges();
            var oldSelections = new List<string>();
            (int start, int end) firstSel = selRanges[0];
            int firstSelLen = firstSel.end - firstSel.start;
            bool oneSelRange = selRanges.Count == 1;
            bool noTextSelected = (oneSelRange && firstSelLen == 0) // one selection of length 0 (i.e., just a cursor)
                // the grepperform's buffer is special, and allowing multiple selections in that file would mess things up
                || (grepperForm != null && grepperForm.tv != null && activeFname == grepperForm.tv.fname)
                // for some reason we don't want to use selections no matter what (probably because it was an automatic TryParseJson triggered by the timer)
                || ignoreSelections;
            bool stopUsingSelections = false;
            int len = Npp.editor.GetLength();
            double sizeThreshold = settings.max_file_size_MB_slow_actions * 1e6;
            bool hasOldSelections = false;
            DocumentType previouslyChosenDocType = DocumentType.NONE;
            if (TryGetInfoForFile(fname, out JsonFileInfo info))
            {
                previouslyChosenDocType = info.documentType;
                if (info.usesSelections)
                {
                    oldSelections = SelectionManager.GetRegionsWithIndicator(selectionRememberingIndicator1, selectionRememberingIndicator2);
                    hasOldSelections = oldSelections.Count > 0;
                }
            }
            if (noTextSelected)
            {
                if (hasOldSelections)
                {
                    // if the user doesn't have any text selected, and they already had one or more selections saved,
                    // preserve the existing selections
                    selRanges = SelectionManager.SetSelectionsFromStartEnds(oldSelections);
                    noTextSelected = selRanges.Count == 1 && selRanges[0].end - selRanges[0].start == 0;
                }
            }
            else if (oneSelRange && firstSelLen == len)
            {
                // user can revert to no-selection mode by selecting entire doc
                noTextSelected = true;
            }
            JNode json = new JNode();
            List<JsonLint> lints;
            List<Comment> comments = null;
            var fatalErrors = new List<bool>();
            string errorMessage = null;
            if (noTextSelected)
            {
                if (wasAutotriggered && len > sizeThreshold)
                    return (ParserState.OK, null, false, DocumentType.NONE);
                stopUsingSelections = true;
                string text = Npp.editor.GetText(len + 1);
                if (documentType == DocumentType.NONE || preferPreviousDocumentType) // if user didn't specify how to parse the document
                {
                    // 1. check if the document has a file extension with an associated DocumentType
                    string fileExtension = Npp.FileExtension().ToLower();
                    DocumentType docTypeFromExtension = Npp.DocumentTypeFromFileExtension(fileExtension);
                    if (docTypeFromExtension != DocumentType.NONE)
                        documentType = docTypeFromExtension;
                    // 2. check if the user previously chose a document type, and use that if so.
                    //    this overrides the default for the file extension.
                    if (previouslyChosenDocType != DocumentType.NONE)
                        documentType = previouslyChosenDocType;
                }
                if (documentType == DocumentType.INI)
                {
                    lints = new List<JsonLint>();
                    try
                    {
                        json = iniParser.Parse(text);
                        documentType = DocumentType.INI;
                    }
                    catch (Exception ex)
                    {
                        errorMessage = ex.ToString();
                        if (ex is IniParserException iniEx)
                        {
                            lints.Add(iniEx.ToJsonLint());
                        }
                    }
                    fatalErrors.Add(errorMessage != null);
                    comments = iniParser.comments;
                }
                else if (documentType == DocumentType.REGEX)
                {
                    json = new JNode(text);
                    lints = null;
                }
                else
                {
                    try
                    {
                        if (documentType == DocumentType.JSONL)
                            json = jsonParser.ParseJsonLines(text);
                        else
                            json = jsonParser.Parse(text);
                        lints = jsonParser.lint.ToList();
                        fatalErrors.Add(jsonParser.fatal);
                        errorMessage = jsonParser.fatalError?.ToString();
                    }
                    catch (Exception ex)
                    {
                        lints = jsonParser.lint.ToList();
                        errorMessage = HandleJsonParserError(text, fatalErrors, lints, jsonParser, ex);
                    }
                    if (settings.remember_comments && jsonParser.comments != null)
                        comments = jsonParser.comments.ToList();
                }
            }
            else
            {
                int previouslySelectedIndicator = Npp.editor.GetIndicatorCurrent();
                // it's a selection-based document, so for regex documents, each selection is just a string JNode, and otherwise we parse each selection as JSON
                // we do not have support for selection-based INI file parsing
                int currentIndicator = selectionRememberingIndicator1;
                ClearPreviouslyRememberedSelections(len, selRanges.Count);
                json = new JObject();
                JObject obj = (JObject)json;
                lints = new List<JsonLint>();
                int combinedLength = 0;
                foreach ((int start, int end) in selRanges)
                    combinedLength += end - start;
                if (wasAutotriggered && combinedLength > sizeThreshold)
                    return (ParserState.OK, null, false, DocumentType.NONE);
                stopUsingSelections = false;
                foreach ((int start, int end) in selRanges)
                {
                    string selRange = Npp.GetSlice(start, end);
                    JNode subJson;
                    if (documentType == DocumentType.REGEX)
                    {
                        subJson = new JNode(selRange, 0);
                    }
                    else
                    {
                        try
                        {
                            subJson = jsonParser.Parse(selRange);
                            lints.AddRange(jsonParser.lint.Select(x => new JsonLint(x.message, x.pos + start, x.curChar, x.severity)));
                            fatalErrors.Add(jsonParser.fatal);
                            if (jsonParser.fatal)
                            {
                                if (errorMessage == null)
                                    errorMessage = jsonParser.fatalError?.ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            subJson = new JNode();
                            errorMessage = HandleJsonParserError(selRange, fatalErrors, lints, jsonParser, ex);
                        }
                    }
                    currentIndicator = ApplyAndSwapIndicator(currentIndicator, start, end - start);
                    string key = $"{start},{end}";
                    obj[key] = subJson;
                }
                Npp.editor.SetIndicatorCurrent(previouslySelectedIndicator);
            }
            if (!isRecursion && oneSelRange && SelectionManager.NoSelectionIsValidJson(json, !noTextSelected, fatalErrors))
            {
                // If there's a single selection that doesn't begin with a valid JSON document,
                // there's a good chance (from molsonkiko's experience) that the user was just selecting some text to copy/paste
                // If this happens, we just treat this as an empty selection and restore old selections
                SelectionManager.SetSelectionsFromStartEnds(oldSelections);
                return TryParseJson(documentType, wasAutotriggered, preferPreviousDocumentType, true);
            }
            int lintCount = lints is null ? 0 : lints.Count;
            info = AddJsonForFile(fname, json);
            bool fatal = errorMessage != null;
            info.documentType = fatal ? DocumentType.NONE : documentType;
            if (stopUsingSelections)
                info.usesSelections = false;
            else
                info.usesSelections |= !noTextSelected;
            info.lints = lints;
            ParserState parserStateToSet = fatal ? ParserState.FATAL : jsonParser.state;
            SetStatusBarSection(parserStateToSet, fname, info, documentType, lintCount);
            if (lintCount > 0 && settings.offer_to_show_lint && !wasAutotriggered)
            {
                string msg = $"There were {lintCount} syntax errors in the document. Would you like to see them?\r\n(You can turn off these prompts in the settings (offer_to_show_lint setting))";
                if (MessageBox.Show(msg, "View syntax errors in document?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    == DialogResult.Yes)
                {
                    if (errorForm != null)
                        Npp.notepad.HideDockingForm(errorForm);
                    OpenErrorForm(activeFname, true);
                    //mainThreadForm.Invoke(new Action(() => OpenErrorForm(activeFname, true)));
                }
            }
            if (fatal)
            {
                // jump to the position of the fatal error (if in whole document mode and not auto-triggered)
                if (lintCount > 0 && noTextSelected && !wasAutotriggered)
                    Npp.editor.GoToLegalPos(lints[lintCount - 1].pos);
                // unacceptable error, show message box
                if (!errorFormTriggeredParse)
                    MessageBox.Show($"Could not parse the document because of error\n{errorMessage}",
                                    $"Error while trying to parse {Npp.DocumentTypeSuperTypeName(documentType)}",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
            }
            if (info.tv != null)
            {
                info.tv.Invoke(new Action(() =>
                {
                    info.tv.json = json;
                    info.tv.documentTypeIndexChangeWasAutomatic = true;
                    info.tv.SetDocumentTypeComboBoxIndex(documentType);
                    info.tv.documentTypeIndexChangeWasAutomatic = false;
                }));
            }
            info.comments = comments;
            jsonFileInfos[activeFname] = info;
            return (parserStateToSet, json, info.usesSelections, documentType);
        }

        private static string HandleJsonParserError(string text, List<bool> fatalErrors, List<JsonLint> lints, JsonParser jsonParser, Exception ex)
        {
            fatalErrors.Add(true);
            char errorChar;
            int errorPos;
            if (jsonParser.ii >= text.Length)
            {
                errorChar = '\x00';
                errorPos = text.Length;
            }
            else
            {
                errorPos = jsonParser.ii;
                errorChar = text[errorPos];
            }
            string errorMessage = RemesParser.PrettifyException(ex);
            lints.Add(new JsonLint(errorMessage, errorPos, errorChar, ParserState.FATAL));
            return errorMessage;
        }

        private static int ApplyAndSwapIndicator(int currentIndicator, int start, int length)
        {
            Npp.editor.SetIndicatorCurrent(currentIndicator);
            Npp.editor.IndicatorFillRange(start, length);
            return currentIndicator == selectionRememberingIndicator1 ? selectionRememberingIndicator2 : selectionRememberingIndicator1;
        }

        private static void ClearPreviouslyRememberedSelections(int len, int numSelections)
        {
            if (selectionRememberingIndicatorsMayCollide && !hasWarnedSelectionRememberingIndicatorsMayCollide)
            {
                hasWarnedSelectionRememberingIndicatorsMayCollide = true;
                string warning = $"JsonTools is using the indicators {selectionRememberingIndicator1} and {selectionRememberingIndicator2} to remember selections, " +
                                "but one or both of those may collide with another plugin.\r\n" +
                                "If you see this message and then you notice Notepad++ or a plugin start to behave oddly, " +
                                "please consider creating an issue describing what happened in the JsonTools GitHub repository.";
                MessageBox.Show(warning, "Possible issue with remembering selections", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Npp.editor.SetIndicatorCurrent(selectionRememberingIndicator1);
            Npp.editor.IndicatorClearRange(0, len);
            if (numSelections > 1)
            {
                Npp.editor.SetIndicatorCurrent(selectionRememberingIndicator2);
                Npp.editor.IndicatorClearRange(0, len);
                Npp.editor.SetIndicatorCurrent(selectionRememberingIndicator1);
            }
        }

        public static void SetStatusBarSection(ParserState parserState, string fname, JsonFileInfo info, DocumentType documentType, int lintCount)
        {
            if (parserState == ParserState.FATAL)
            {
                string ext = Npp.FileExtension(fname);
                // if there is a fatal error, don't hijack the doctype status bar section
                // unless it's a normal JSON document type.
                // For instance, we expect a Python file to be un-parseable,
                // and it's not helpful to indicate that it's a JSON document with fatal errors.
                if (fileExtensionsToAutoParse.Contains(ext))
                    Npp.notepad.SetStatusBarSection($"JSON with fatal errors - {lintCount} errors (Alt-P-J-E to view)",
                        StatusBarSection.DocType);
            }
            else if (parserState < ParserState.FATAL && !(!info.usesSelections && documentType != DocumentType.JSON && documentType != DocumentType.JSONL))
                // only touch status bar if whole doc is parsed as JSON or JSON Lines, and there are no fatal errors
            {
                string doctypeDescription;
                switch (parserState)
                {
                case ParserState.STRICT: doctypeDescription = documentType == DocumentType.JSONL ? "JSON lines" : "JSON"; break;
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
            char indentChar = settings.tab_indent_pretty_print ? '\t' : ' ';
            int indent = settings.tab_indent_pretty_print
                ? 1
                : settings.indent_pretty_print;
            return json.PrettyPrintAndChangePositions(indent, settings.sort_keys, settings.pretty_print_style, int.MaxValue, indentChar);
        }

        /// <summary>
        /// create a new file and pretty-print this JSON in it, then set the lexer language to JSON.<br></br>
        /// at present this does not support remembering comments
        /// </summary>
        /// <param name="json"></param>
        public static void PrettyPrintJsonInNewFile(JNode json)
        {
            Npp.notepad.FileNew();
            ReformatFileWithJson(json, PrettyPrintFromSettings, false);
        }

        public static bool UseComments(JsonFileInfo info) =>
            settings.remember_comments && !info.usesSelections && info.comments != null && info.comments.Count > 0;

        /// <summary>
        /// overwrite the current file or saved selections with its JSON in pretty-printed format
        /// </summary>
        public static void PrettyPrintJson()
        {
            (ParserState parserState, JNode json, bool usesSelections, DocumentType documentType) = TryParseJson(preferPreviousDocumentType:true);
            if (parserState == ParserState.FATAL || json == null || !TryGetInfoForFile(activeFname, out JsonFileInfo info))
                return;
            bool isJsonLines = info.documentType == DocumentType.JSONL;
            if (isJsonLines && (settings.ask_before_pretty_printing_json_lines == AskUserWhetherToDoThing.DONT_DO_DONT_ASK
                || settings.ask_before_pretty_printing_json_lines == AskUserWhetherToDoThing.ASK_BEFORE_DOING
                    && MessageBox.Show(
                        "Pretty-printing a JSON Lines document will generally lead to it no longer being a valid JSON Lines document. Pretty-print anyway?",
                        "Pretty-print JSON Lines document?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    ) == DialogResult.No)
                )
                return;
            int indent = settings.tab_indent_pretty_print ? 1 : settings.indent_pretty_print; 
            Func<JNode, string> formatter;
            if (UseComments(info))
                formatter = x => x.PrettyPrintWithCommentsAndChangePositions(info.comments, indent, settings.sort_keys, settings.tab_indent_pretty_print ? '\t' : ' ', settings.pretty_print_style);
            else
                formatter = PrettyPrintFromSettings;
            if (isJsonLines)
                // pretty-printing a JSON Lines document makes it a JSON document (not JSON Lines anymore)
                info.documentType = DocumentType.JSON;
            ReformatFileWithJson(json, formatter, usesSelections);
        }

        /// <summary>
        /// overwrite the current file or saved selections with its JSON in compressed format
        /// </summary>
        public static void CompressJson()
        {
            (ParserState parserState, JNode json, bool usesSelections, DocumentType _) = TryParseJson(preferPreviousDocumentType:true);
            if (parserState == ParserState.FATAL || json == null || !TryGetInfoForFile(activeFname, out JsonFileInfo info))
                return;
            Func<JNode, string> formatter;
            if (UseComments(info))
            {
                formatter = x => x.ToStringWithCommentsAndChangePositions(info.comments, settings.sort_keys);
            }
            else
            {
                if (settings.minimal_whitespace_compression)
                    formatter = x => x.ToStringAndChangePositions(settings.sort_keys, ":", ",");
                else
                    formatter = x => x.ToStringAndChangePositions(settings.sort_keys);
            }
            ReformatFileWithJson(json, formatter, usesSelections);
        }

        /// <summary>
        /// If the file is using one or more selections, use the formatter to reformat each selection separately
        /// Otherwise, replace the text of the file with formatter(json)
        /// </summary>
        /// <param name="json"></param>
        /// <param name="formatter"></param>
        public static Dictionary<string, (string newKey, JNode child)> ReformatFileWithJson(JNode json, Func<JNode, string> formatter, bool usesSelections)
        {
            var keyChanges = new Dictionary<string, (string newKey, JNode child)>();
            JsonFileInfo info;
            Npp.editor.BeginUndoAction();
            if (usesSelections)
            {
                var obj = (JObject)json;
                int delta = 0;
                pluginIsEditing = true;
                int previouslySelectedIndicator = Npp.editor.GetIndicatorCurrent();
                var keyvalues = obj.children.ToArray();
                ClearPreviouslyRememberedSelections(Npp.editor.GetLength(), keyvalues.Length);
                int currentIndicator = selectionRememberingIndicator1;
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
                    currentIndicator = ApplyAndSwapIndicator(currentIndicator, newStart, newCount);
                    int newEnd = newStart + newCount;
                    delta = newEnd - end;
                    keyChanges[kv.Key] = ($"{newStart},{newEnd}", kv.Value);
                }
                pluginIsEditing = false;
                RenameAll(keyChanges, obj);
                SelectionManager.SetSelectionsFromStartEnds(obj.children.Keys);
                Npp.editor.SetIndicatorCurrent(previouslySelectedIndicator);
                json = obj;
            }
            else if (TryGetInfoForFile(activeFname, out info) && info.documentType == DocumentType.INI && json is JObject obj)
            {
                string iniText;
                try
                {
                    iniText = obj.ToIniFile(info.comments);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while reformatting INI file:\r\n{ex.ToString()}", "Error while reformatting INI file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return keyChanges;
                }
                Npp.editor.SetText(iniText);
            }
            else
            {
                string newText = formatter(json);
                Npp.editor.SetText(newText);
            }
            info = AddJsonForFile(activeFname, json);
            Npp.RemoveTrailingSOH();
            Npp.editor.EndUndoAction();
            lastEditedTime = DateTime.MaxValue; // avoid redundant parsing
            if (!usesSelections)
                Npp.SetLangBasedOnDocType(false, false, info.documentType);
            return keyChanges;
        }

        public static JNode RenameAll(Dictionary<string, (string, JNode)> keyChanges, JObject obj)
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

        public static void CheckJsonSyntaxNow()
        {
            string curFname = Npp.notepad.GetCurrentFilePath();
            (ParserState parserState, _, _, _) = TryParseJson(Npp.FileExtension(curFname) == "jsonl" ? DocumentType.JSONL : DocumentType.JSON);
            if (parserState == ParserState.FATAL)
                RefreshErrorFormInOwnThread(curFname);
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
                string text = Npp.editor.GetText();
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
            JsonParser jsonParser = JsonParserFromSettings();
            if (SelectionManager.NoTextSelected(selections))
            {
                string selStrValue = TryGetSelectedJsonStringValue(jsonParser);
                if (selStrValue == null)
                    return;
                sb.Append(selStrValue);
            }
            else
            {
                foreach ((int start, int end) in selections)
                {
                    string selStrValue = TryGetSelectedJsonStringValue(jsonParser, start, end);
                    if (selStrValue == null)
                        return;
                    sb.Append(selStrValue);
                    sb.Append("\r\n");
                }
            }
            Npp.notepad.FileNew();
            Npp.editor.SetText(sb.ToString());
        }

        public static string TryGetSelectedJsonStringValue(JsonParser jsonParser, int start = -1, int end = -1)
        {
            string text = start < 0 || end < 0
                ? Npp.editor.GetText()
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
            (ParserState parserState, JNode json, _, _) = TryParseJson(preferPreviousDocumentType:true);
            if (parserState == ParserState.FATAL || json == null) return;
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

        public static string ToJsonLinesFromSettings(JNode json)
        {
            if (settings.minimal_whitespace_compression)
                return ((JArray)json).ToJsonLines(settings.sort_keys, ":", ",");
            return ((JArray)json).ToJsonLines(settings.sort_keys);

        }

        /// <summary>
        /// If the current file is a JSON array, open a new buffer with a JSON Lines
        /// document containing all entries in the array.
        /// </summary>
        static void DumpJsonLines()
        {
            (ParserState parserState, JNode json, bool usesSelections, _) = TryParseJson(preferPreviousDocumentType:true);
            if (parserState == ParserState.FATAL || json == null) return;
            if (!(json is JArray))
            {
                MessageBox.Show("Only JSON arrays can be converted to JSON Lines format.",
                                "Only arrays can be converted to JSON Lines",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Npp.notepad.FileNew();
            ReformatFileWithJson(json, ToJsonLinesFromSettings, usesSelections);
        }

        //form opening stuff

        static void OpenSettings()
        {
            settings.ShowDialog();
            millisecondsAfterLastEditToParse = (settings.inactivity_seconds_before_parse < 1)
                    ? 1000
                    : 1000 * settings.inactivity_seconds_before_parse;
            // make sure grepperForm gets these new settings as well
            if (grepperForm != null && !grepperForm.IsDisposed)
            {
                grepperForm.grepper.jsonParser = JsonParserFromSettings();
                grepperForm.grepper.maxThreadsParsing = settings.max_threads_parsing;
            }
            RestyleEverything();
        }

        /// <summary>
        /// having a global shared JsonParser is an obvious risk factor for race conditions, so having this method is preferable
        /// </summary>
        public static JsonParser JsonParserFromSettings()
        {
            return new JsonParser(settings.logger_level,
                                  settings.allow_datetimes,
                                  false,
                                  false,
                                  settings.remember_comments);
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
            if (regexSearchForm != null && !regexSearchForm.IsDisposed)
                FormStyle.ApplyStyle(regexSearchForm, settings.use_npp_styling);
            string[] keys = jsonFileInfos.Keys.ToArray();
            List<string> keysToRemove = new List<string>();
            foreach (string fname in keys)
            {
                JsonFileInfo info = jsonFileInfos[fname];
                if (info == null || info.IsDisposed)
                    keysToRemove.Add(fname);
                else if (info.tv != null && !info.tv.IsDisposed)
                    FormStyle.ApplyStyle(info.tv, settings.use_npp_styling);
            }
            foreach (string fname in keysToRemove)
                jsonFileInfos.Remove(fname);
        }

        /// <summary>
        /// Open the error form if it didn't already exist.
        /// If it was open, close it.
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="wasAutoTriggered"></param>
        public static void OpenErrorForm(string fname, bool wasAutoTriggered)
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
            else if (errorForm == null || errorForm.IsDisposed)
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
                dockingFormIcon = Icon.FromHandle(newBmp.GetHicon());
            }

            NppTbData _nppTbData = new NppTbData();
            _nppTbData.hClient = form.Handle;
            _nppTbData.pszName = form.Text;
            // the dlgDlg should be the index of funcItem where the current function pointer is in
            // this case is 15.. so the initial value of funcItem[15]._cmdID - not the updated internal one !
            _nppTbData.dlgID = errorFormId;
            // dock on bottom
            _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_BOTTOM | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
            _nppTbData.hIconTab = (uint)dockingFormIcon.Handle;
            _nppTbData.pszModuleName = PluginName;
            IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
            Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            Npp.notepad.ShowDockingForm(form);
        }

        private static void RefreshErrorFormInOwnThread(string fname)
        {
            if (errorForm == null)
                return;
            errorForm.Invoke(new Action(() =>
            {
                if (errorForm.IsDisposed || !TryGetInfoForFile(fname, out JsonFileInfo info) || info.lints == null)
                    return;
                errorForm.Reload(fname, info.lints);
            }));
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
        /// get the path to the JNode at position pos
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
                    json = grepperForm.grepper.fnameJsons;
                }
                else
                    json = null;
            }
            if (json == null)
            {
                ParserState parserState;
                (parserState, json, usesSelections, _) = TryParseJson(preferPreviousDocumentType:true);
                if (parserState == ParserState.FATAL || json == null)
                    return "";
            }
            if (usesSelections)
            {
                // check if pos is inside a selection
                // re-parse this remembered selection
                // (we can't rely on the position-json mapping of the selection-remembering object being in sync with the document)
                (int start, int end) = SelectionManager.GetEnclosingRememberedSelection(pos, selectionRememberingIndicator1, selectionRememberingIndicator2);
                if (start < 0)
                    return "";
                string selText = Npp.GetSlice(start, end);
                var parser = JsonParserFromSettings();
                JNode selJson = parser.Parse(selText);
                if (parser.fatal)
                    return "";
                // this remembered selection still contains JSON, so find the path to this position in it
                string formattedKey = JNode.FormatKey($"{start},{end}", style);
                return formattedKey + selJson.PathToPosition(pos - start, style);
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
            Predicate<char> isTryParseStart = c => settings.try_parse_start_chars.IndexOf(c) >= 0;
            foreach ((int start, int end) in selections)
            {
                string text = Npp.GetSlice(start, end);
                int ii = 0;
                int len = text.Length;
                int utf8Pos = start;
                while (ii < len)
                {
                    char c = text[ii];
                    if (isTryParseStart(c))
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
                        utf8Pos += jsonChecker.utf8Pos - startII;
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
            TryParseJson(DocumentType.JSON, false);
        }

        /// <summary>
        /// Try to parse a JSON document and then open up the tree view.<br></br>
        /// If the tree view is already open, close it instead.
        /// </summary>
        /// <param name="documentType">see the documentType argument for TryParseJson above</param>
        public static void OpenJsonTree(DocumentType documentType = DocumentType.JSON)
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
                info = new JsonFileInfo(documentType:documentType);
            }
            if (info.tv != null && !info.tv.IsDisposed)
            {
                // if the tree view is open, hide the tree view and then dispose of it
                // if the grepper form is open, this should just toggle it.
                // it's counterintuitive to the user that the plugin command would toggle
                // a tree view other than the one they're currently looking at
                bool wasVisible = info.tv.Visible;
                if (!wasVisible
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
                if (wasVisible)
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
            JNode json;
            bool usesSelections;
            ParserState parserState;
            (parserState, json, usesSelections, documentType) = TryParseJson(documentType);
            if (json == null || json == new JNode() || !TryGetInfoForFile(activeFname, out info)) // open a tree view for partially parsed JSON
                return; // don't open the tree view for non-json files
            openTreeViewer = new TreeViewer(json);
            info.tv = openTreeViewer;
            jsonFileInfos[activeFname] = info;
            DisplayJsonTree(openTreeViewer, json, $"Json Tree View for {openTreeViewer.RelativeFilename()}", usesSelections, documentType, parserState == ParserState.FATAL);
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

        public static void DisplayJsonTree(TreeViewer treeViewer, JNode json, string title, bool usesSelections, DocumentType documentType, bool fatalParserError)
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
                dockingFormIcon = Icon.FromHandle(newBmp.GetHicon());
            }

            NppTbData _nppTbData = new NppTbData();
            _nppTbData.hClient = treeViewer.Handle;
            _nppTbData.pszName = title;
            // the dlgDlg should be the index of funcItem where the current function pointer is in
            // this case is 15.. so the initial value of funcItem[15]._cmdID - not the updated internal one !
            _nppTbData.dlgID = jsonTreeId;
            // define the default docking behaviour
            _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
            _nppTbData.hIconTab = (uint)dockingFormIcon.Handle;
            _nppTbData.pszModuleName = PluginName;
            IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
            Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

            Npp.SetLangBasedOnDocType(fatalParserError, usesSelections, documentType);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            // Following message will toogle both menu item state and toolbar button
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
        /// Prompt the user to choose a locally saved JSON schema file,
        /// parse the JSON schema,<br></br>
        /// and try to validate the currently open file against the schema.<br></br>
        /// Send the user a message telling the user if validation succeeded,
        /// or if it failed, where the first error was.<br></br>
        /// If ignoreSelections, always validate the entire file even the selected text is valid JSON.
        /// </summary>
        public static void ValidateJson(string schemaPath = null, bool messageOnSuccess = true, bool ignoreSelections = false, bool wasAutotriggered = false)
        {
            (ParserState parserState, JNode json, _, DocumentType documentType) = TryParseJson(wasAutotriggered: wasAutotriggered, preferPreviousDocumentType:true, ignoreSelections: ignoreSelections);
            string curFname = Npp.notepad.GetCurrentFilePath();
            if (parserState == ParserState.FATAL || json == null)
            {
                RefreshErrorFormInOwnThread(curFname);
                return;
            }
            if (schemaPath == null)
            {
                FileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 0;
                openFileDialog.Title = "Select JSON schema file to validate against";
                if (openFileDialog.ShowDialog() != DialogResult.OK || !openFileDialog.CheckFileExists)
                    return;
                schemaPath = openFileDialog.FileName;
            }
            if (!schemaCache.Get(schemaPath, out JsonSchemaValidator.ValidationFunc validator)
                && !schemaCache.TryAdd(schemaPath, out validator))
                return;
            List<JsonLint> problems;
            bool validates;
            try
            {
                validates = validator(json, out problems);
            }
            catch (Exception e)
            {
                MessageBox.Show($"While validating JSON against the schema at path {schemaPath}, the following error occurred:\r\n{e}",
                    "Error while validating JSON against schema", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!TryGetInfoForFile(curFname, out JsonFileInfo info))
                info = AddJsonForFile(curFname, json);
            info.filenameOfMostRecentValidatingSchema = schemaPath;
            info.lints = info.lints is null ? problems : info.lints.Concat(problems).ToList();
            int lintCount = info.lints.Count;
            RefreshErrorFormInOwnThread(curFname);
            SetStatusBarSection(parserState, curFname, info, documentType, lintCount);
            if (!validates && problems.Count > 0)
            {
                JsonLint firstProblem = problems[0];
                if (!wasAutotriggered)
                    Npp.editor.GoToLegalPos(firstProblem.pos);
                MessageBox.Show($"The JSON in file {curFname} DOES NOT validate against the schema at path {schemaPath}. Problem 1 of {problems.Count}:\n{firstProblem}",
                    "Validation failed...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (messageOnSuccess)
                MessageBox.Show($"The JSON in file {curFname} validates against the schema at path {schemaPath}.",
                    "Validation succeeded!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Analyze the current JSON file and generate a minimal JSON schema that describes it.
        /// </summary>
        static void GenerateJsonSchema()
        {
            (ParserState parserState, JNode json, _, _) = TryParseJson(preferPreviousDocumentType:true);
            if (parserState == ParserState.FATAL || json == null) return;
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
            (ParserState parserState, JNode json, _, _) = TryParseJson(preferPreviousDocumentType:true);
            if (parserState == ParserState.FATAL || json == null) return;
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
            bool validates = schemasToFnamePatterns_SCHEMA(schemasToFnamePatterns, out List<JsonLint> lints);
            if (!validates && lints.Count > 0)
            {
                MessageBox.Show("Validation of the schemas to fnames patterns JSON must be an object mapping filenames to non-empty arrays of valid regexes (strings).\r\nThere were the following validation problem(s):\r\n"
                        + JsonSchemaValidator.LintsAsJArrayString(lints),
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
                schemaCache.TryAdd(fname, out _);
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
        static bool ValidateIfFilenameMatches(string fname, bool wasAutotriggered = false)
        {
            if (wasAutotriggered && Npp.editor.GetLength() > Main.settings.max_file_size_MB_slow_actions * 1e6)
                return false;
            foreach (string schemaFname in schemasToFnamePatterns.children.Keys)
            {
                JArray fnamePatterns = (JArray)schemasToFnamePatterns[schemaFname];
                foreach (JNode pat in fnamePatterns.children)
                {
                    var regex = ((JRegex)pat).regex;
                    if (!regex.IsMatch(fname)) continue;
                    // the filename matches a pattern for this schema, so we'll try to validate it.
                    ValidateJson(schemaFname, false, true, wasAutotriggered);
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
        #region moreHelperFunctions
        /// <summary>
        /// Assumes that startUtf8Pos is the start of a JNode in the current document.<br></br>
        /// Returns the position in that document of the end of that JNode.
        /// </summary>
        /// <param name="startUtf8Pos"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static int EndOfJNodeAtPos(int startUtf8Pos, int end)
        {
            string slice = Npp.GetSlice(startUtf8Pos, end);
            var parser = new JsonParser(LoggerLevel.JSON5, false, true, true, false);
            try
            {
                parser.ParseSomething(slice, 0);
            }
            catch
            {
                return startUtf8Pos;
            }
            return startUtf8Pos + parser.utf8Pos;
        }

        /// <summary>
        /// Assumes that positions is a set of start positions for non-root JNodes in the current document.<br></br>
        /// If any of those start positions is 0, do nothing (unless isJsonLines) because it is not possible for any JNode
        /// other than the root to have a position of 0 in a parsed document (it is only possible in a RemesPath query result).<br></br>
        /// Otherwise, attempts to parse the document starting at each position, and if parsing is successful,
        /// adds a new selection of the parsed child starting at that position.
        /// </summary>
        public static void SelectAllChildren(IEnumerable<int> positions, bool isJsonLines)
        {
            int[] sortedPositions = positions.Distinct().ToArray();
            if (sortedPositions.Length == 0)
                return;
            Array.Sort(sortedPositions);
            int minPos = sortedPositions[0];
            if (!isJsonLines && minPos == 0)
            {
                // it's not possible for a child of a parsed JNode to have a position of 0,
                // because only the root can be at position 0.
                // this would only happen if this is being invoked from the right click context menu
                // on the treeview for a RemesPath query result
                MessageBox.Show("Cannot select all children because one or more of the children does not correspond to a JSON node in the document",
                    "Can't select all children", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            string slice = Npp.GetSlice(minPos, Npp.editor.GetLength());
            var parser = new JsonParser(LoggerLevel.JSON5, false, true, true);
            int utf8ExtraBytes = 0;
            int positionsIdx = 0;
            int nextStartPos = 0;
            var selections = new List<string>();
            int ii = 0;
            while (ii < slice.Length)
            {
                if (ii + utf8ExtraBytes == nextStartPos)
                {
                    parser.Reset();
                    parser.ii = ii;
                    try
                    {
                        parser.ParseSomething(slice, 0);
                        int selStart = minPos + nextStartPos;
                        selections.Add($"{selStart},{selStart + parser.utf8Pos - ii}");
                    }
                    catch { }
                    ii = parser.ii;
                    utf8ExtraBytes += parser.utf8Pos - ii;
                    positionsIdx++;
                    if (positionsIdx == sortedPositions.Length)
                        break;
                    nextStartPos = sortedPositions[positionsIdx] - minPos;
                }
                else
                {
                    utf8ExtraBytes += JsonParser.ExtraUTF8Bytes(slice[ii]);
                    ii++;
                }
            }
            SelectionManager.SetSelectionsFromStartEnds(selections);
        }

        public static void RegexSearchToJson()
        {
            if (regexSearchForm != null && regexSearchForm.Focused)
            {
                Npp.editor.GrabFocus();
            }
            else
            {
                if (regexSearchForm == null || regexSearchForm.IsDisposed)
                {
                    regexSearchForm = new RegexSearchForm();
                    regexSearchForm.Show();
                }
                if (!regexSearchForm.Focused)
                {
                    regexSearchForm.GrabFocus();
                    if (settings.auto_try_guess_csv_delim_newline)
                    {
                        bool csvBoxShouldBeChecked = RegexSearchForm.TrySniffCommonDelimsAndEols(out EndOfLine eol, out char delim, out int nColumns);
                        regexSearchForm.SetCsvSettingsFromEolNColumnsDelim(csvBoxShouldBeChecked, eol, delim, nColumns);
                    }
                }
            }
        }
        #endregion // moreHelperFunctions

        #region timerStuff
        /// <summary>
        /// This callback fires once every second.<br></br>
        /// It checks if the last edit was more than 2 seconds ago.<br></br>
        /// If it was, it checks if the filename is one of fileExtensionsToAutoParse (see above)
        /// and also if the filename is 
        /// </summary>
        /// <param name="state"></param>
        private static void DelayedParseAfterEditing(object s)
        {
            DateTime now = DateTime.UtcNow;
            if (!settings.auto_validate
                || !bufferFinishedOpening
                || Npp.editor.GetLength() > settings.max_file_size_MB_slow_actions * 1e6 // current file too big
                || lastEditedTime == DateTime.MaxValue // set when we don't want to edit it
                || lastEditedTime.AddMilliseconds(millisecondsAfterLastEditToParse) > now)
                return;
            lastEditedTime = DateTime.MaxValue;
            // an edit happened recently, so check if it's a json file
            // and also check if the file matches a schema validation pattern
            string fname = Npp.notepad.GetCurrentFilePath();
            string ext = Npp.FileExtension(fname);
            if ((TryGetInfoForFile(fname, out JsonFileInfo info)
                    && (info.documentType == DocumentType.INI || info.documentType == DocumentType.REGEX // file is already being parsed as regex or ini, so stop
                        || info.usesSelections)) // file uses selections, so stop (because that could change the user's selections unexpectedly)
                || ValidateIfFilenameMatches(fname, true) // if filename is associated with a schema, it will be parsed during the schema validation, so stop
                || !fileExtensionsToAutoParse.Contains(ext)) // extension is not marked for auto-parsing, so stop
                return;
            // filename matches but it's not associated with a schema or being parsed as non-JSON/JSONL, so just parse normally
            TryParseJson(ext == "jsonl" ? DocumentType.JSONL : DocumentType.JSON, true, ignoreSelections:true);
            RefreshErrorFormInOwnThread(fname);
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
        LruCache<string, JsonSchemaValidator.ValidationFunc> cache;
        Dictionary<string, DateTime> lastRetrieved;

        public SchemaCache(int capacity)
        {
            cache = new LruCache<string, JsonSchemaValidator.ValidationFunc>(capacity);
            lastRetrieved = new Dictionary<string, DateTime>();
        }

        public bool Get(string fname, out JsonSchemaValidator.ValidationFunc validator)
        {
            validator = null;
            if (!cache.cache.ContainsKey(fname)) return false;
            var fileInfo = new FileInfo(fname);
            var retrieved = lastRetrieved[fname];
            if (fileInfo.LastWriteTime > retrieved)
            {
                // the file has been edited since we cached it.
                // thus, we need to read the file, parse and compile the schema, and then re-cache it.
                return TryAdd(fname, out validator);
            }
            validator = cache[fname];
            return true;
        }

        /// <summary>
        /// read the schema file, parse it and compile it to a validator, cache the validator, then return true<br></br>
        /// if compiling or parsing of the schema fails, return false
        /// </summary>
        /// <param name="fname"></param>
        public bool TryAdd(string fname, out JsonSchemaValidator.ValidationFunc validator)
        {
            string schemaText = File.ReadAllText(fname);
            validator = null;
            JNode schema;
            try
            {
                var parser = new JsonParser(LoggerLevel.JSON5, false, true, true, false);
                schema = parser.Parse(schemaText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"While trying to parse the schema at path {fname}, the following error occurred:\r\n{RemesParser.PrettifyException(ex)}", "error while trying to parse schema", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (schema == null)
                return false;
            if (cache.isFull)
            {
                // the cache is about to have its oldest key purged
                // we find out what that is so we can also purge it from lastRetrieved
                string lastFnameAdded = cache.OldestKey();
                lastRetrieved.Remove(lastFnameAdded);
            }
            try
            {
                validator = JsonSchemaValidator.CompileValidationFunc(schema, Main.settings.max_schema_validation_problems);
                lastRetrieved[fname] = DateTime.Now;
                cache[fname] = validator;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"While compiling schema for file \"{fname}\", got exception {RemesParser.PrettifyException(ex)}",
                    "error while compiling JSON schema",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
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
                if (IsDisposed)
                    return;
                _json = value;
                if (tv != null && !tv.IsDisposed)
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
        public List<Comment> comments;
        public DocumentType documentType;
        /// <summary>
        /// the name of the last JSON schema file used to validate this JsonFileInfo's json
        /// </summary>
        public string filenameOfMostRecentValidatingSchema = null;

        public bool IsDisposed { get; private set; }

        public JsonFileInfo(JNode json = null, List<JsonLint> lints = null, string statusBarSection = null, TreeViewer tv = null, bool hasJsonSelections = false, List<Comment> comments = null, DocumentType documentType = DocumentType.JSON)
        {
            _json = json;
            this.lints = lints;
            this.statusBarSection = statusBarSection;
            this.tv = tv;
            this.usesSelections = hasJsonSelections;
            IsDisposed = false;
            this.comments = comments;
            this.documentType = documentType;
        }

        public void Dispose()
        {
            if (tv != null && !tv.IsDisposed)
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
            if (comments != null)
                comments.Clear();
        }
    }
}   
