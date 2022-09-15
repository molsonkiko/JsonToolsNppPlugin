// NPP plugin platform for .Net v0.91.57 by Kasper B. Graversen etc.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Kbg.Demo.Namespace.Properties;
using Kbg.NppPluginNET.PluginInfrastructure;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using JSON_Tools.Forms;
using JSON_Tools.Tests;
using static Kbg.NppPluginNET.PluginInfrastructure.Win32;

namespace Kbg.NppPluginNET
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "JsonTools";
        static string iniFilePath = null;
        static string sectionName = "Format JSON";
        static string keyName = "LoadJson";
        static bool shouldLoadJson = false;
        //static string sessionFilePath = @"C:\text.session";
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
        static internal int remesPathWindowId = -1;
        #endregion

        #region " Startup/CleanUp "

        static internal void CommandMenuInit()
        {
            // Initialization of your plugin commands
            // You should fill your plugins commands here
 
            //
            // Firstly we get the parameters from your plugin config file (if any)
            //

            // get path of plugin configuration
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();

            // if config path doesn't exist, we create it
            if (!Directory.Exists(iniFilePath))
            {
                Directory.CreateDirectory(iniFilePath);
            }

            // make your plugin config file full file path name
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");

            // get the parameter value from plugin config
            //shouldLoadJson = Win32.GetPrivateProfileInt(sectionName, keyName, 0, iniFilePath) != 0;

            // with function :
            // SetCommand(int index,                            // zero based number to indicate the order of command
            //            string commandName,                   // the command name that you want to see in plugin menu
            //            NppFuncItemDelegate functionPointer,  // the symbol of function (function pointer) associated with this command. The body should be defined below. See Step 4.
            //            ShortcutKey *shortcut,                // optional. Define a shortcut to trigger this command
            //            bool check0nInit                      // optional. Make this menu item be checked visually
            //            );
            PluginBase.SetCommand(0, "Documentation", docs);
            // adding shortcut keys may cause crash issues if there's a collision, so try not adding shortcuts
            PluginBase.SetCommand(1, "Pretty-print current JSON file", PrettyPrintJson, new ShortcutKey(true, true, true, Keys.P));
            PluginBase.SetCommand(2, "Compress current JSON file", CompressJson, new ShortcutKey(true, true, true, Keys.C));
            // Here you insert a separator
            PluginBase.SetCommand(3, "---", null);
            PluginBase.SetCommand(4, "Open JSON tree viewer", OpenJsonTree, new ShortcutKey(true, true, true, Keys.J)); jsonTreeId = 4;
            PluginBase.SetCommand(5, "---", null);
            PluginBase.SetCommand(6, "Settings", OpenSettings, new ShortcutKey(true, true, true, Keys.S));
            PluginBase.SetCommand(7, "JSON to YAML", DumpYaml);
            PluginBase.SetCommand(8, "Run tests", TestRunner.RunAll, new ShortcutKey(true, true, true, Keys.R));
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

            // changing tabs
            //if ((code == (uint)NppMsg.NPPN_BUFFERACTIVATED) ||
            //    (code == (uint)NppMsg.NPPN_LANGCHANGED))
            //{
            //    Main.LoadJson();
            //}

            //// when closing a file
            if (code == (uint)NppMsg.NPPN_FILEBEFORECLOSE)
            {
                fname_jsons.Remove(Npp.GetCurrentPath());
            }

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
            Win32.WritePrivateProfileString(sectionName, keyName, shouldLoadJson ? "1" : "0", iniFilePath);
        }
        #endregion

        #region " Menu functions "
        static void docs()
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

        static JNode TryParseJson()
        {
            int len = Npp.editor.GetLength();
            string fname = Npp.GetCurrentPath();
            string text = Npp.editor.GetText(len);
            JNode json = new JNode();
            try
            {
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
            Npp.editor.SetText(json.PrettyPrintAndChangeLineNumbers());
            Npp.SetLangJson();
        }

        static void CompressJson()
        {
            JNode json = TryParseJson();
            if (json == null) return;
            Npp.editor.SetText(json.ToStringAndChangeLineNumbers());
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
        }

        //form opening stuff

        static void OpenSettings()
        {
            settings.ShowDialog();
            jsonParser.allow_nan_inf = settings.allow_nan_inf;
            jsonParser.allow_datetimes = settings.allow_datetimes;
            jsonParser.allow_javascript_comments = settings.allow_javascript_comments;
            jsonParser.allow_singlequoted_str = settings.allow_singlequoted_str;
            if (settings.linting)
            {
                jsonParser.lint = new List<JsonLint>();
            }
        }

        static void OpenJsonTree()
        {
            // Dockable Dialog Demo
            // 
            // This demonstration shows you how to do a dockable dialog.
            // You can create your own non dockable dialog - in this case you don't need this demonstration.
            JNode json = TryParseJson();
            if (json == null) return;

            TreeViewer treeViewer = new TreeViewer(json);
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
            _nppTbData.pszName = "Json Tree View";
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

            Npp.SetLangJson();
            treeViewer.JsonTreePopulate(json);
            treeViewer.Focus();
        }
        #endregion
    }
}   
