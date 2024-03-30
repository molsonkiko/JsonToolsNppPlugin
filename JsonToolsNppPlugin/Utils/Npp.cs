using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using Kbg.NppPluginNET;
using Kbg.NppPluginNET.PluginInfrastructure;

/// <summary>
/// miscellaneous useful things like a connector to Notepad++
/// </summary>
namespace JSON_Tools.Utils
{
    /// <summary>
    /// contains connectors to Scintilla (editor) and Notepad++ (notepad)
    /// </summary>
    public class Npp
    {
        /// <summary>
        /// connector to Scintilla
        /// </summary>
        public static IScintillaGateway editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
        /// <summary>
        /// connector to Notepad++
        /// </summary>
        public static INotepadPPGateway notepad = new NotepadPPGateway();

        public static readonly int[] nppVersion = notepad.GetNppVersion();

        public static readonly string nppVersionStr = NppVersionString(true);

        public static readonly bool nppVersionAtLeast8 = nppVersion[0] >= 8;

        /// <summary>this is when NPPM_ALLOCATEINDICATORS was introduced</summary>
        public static readonly bool nppVersionAtLeast8p5p6 = nppVersionStr.CompareTo("8.5.6") >= 0;

        /// <summary>
        /// the directory containing of the plugin DLL (i.e., the DLL that this code compiles into)<br></br>
        /// usually Path.Combine(notepad.GetNppPath(), "plugins", Main.PluginName) would work just as well,<br></br>
        /// but under some weird circumstances (see this GitHub issue comment: https://github.com/molsonkiko/NppCSharpPluginPack/issues/5#issuecomment-1982167513)<br></br>
        /// it can fail.
        /// </summary>
        public static readonly string pluginDllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// append text to current doc, then append newline and move cursor
        /// </summary>
        /// <param name="inp"></param>
        public static void AddLine(string inp)
        {
            editor.AppendText(Encoding.UTF8.GetByteCount(inp), inp);
            editor.AppendText(Environment.NewLine.Length, Environment.NewLine);
        }

        /// <summary>
        /// set the lexer language to JSON so the file looks nice<br></br>
        /// DOES NOTHING IF:<br></br>
        /// 1. the file is really big (default 4+ MB, configured by settings.max_size_full_tree_MB)<br></br>
        /// 2. fatalError is true (don't lex documents that couldn't be parsed as JSON without fatal errors)<br></br>
        /// 3. usesSelections is true (selection-based documents might contain JSON without being JSON documents)<br></br>
        /// 4. the documentType is not DocumentType.JSON or DocumentType.JSONL (any other DocumentType is not JSON-related)
        /// </summary>
        public static void SetLangJson(bool fatalError = false, bool usesSelections = false)
        {
            if (editor.GetLength() < Main.settings.max_file_size_MB_slow_actions * 1e6
                && !fatalError
                && !usesSelections)
            {
                notepad.SetCurrentLanguage(LangType.L_JSON);
            }
        }

        public static void SetLangIni(bool fatalError, bool usesSelections)
        {
            if (editor.GetLength() < Main.settings.max_file_size_MB_slow_actions * 1e6
                && !fatalError
                && !usesSelections)
            {
                notepad.SetCurrentLanguage(LangType.L_INI);
            }
        }

        public static DocumentType DocumentTypeFromFileExtension(string fileExtension)
        {
            switch (fileExtension)
            {
            case "json":
            case "json5":
            case "jsonc":
                return DocumentType.JSON;
            case "jsonl":
                return DocumentType.JSONL;
            case "ini":
                return DocumentType.INI;
            default:
                return DocumentType.NONE;
            }
        }

        public static void SetLangBasedOnDocType(bool fatalError, bool usesSelections, DocumentType documentType)
        {
            switch (documentType)
            {
            case DocumentType.JSON:
            case DocumentType.JSONL:
                SetLangJson(fatalError, usesSelections);
                break;
            case DocumentType.INI:
                SetLangIni(fatalError, usesSelections);
                break;
            default:
                break;
            }
        }

        /// <summary>
        /// input is one of 'p', 'd', 'f'<br></br>
        /// if 'p', get full path to current file (default)<br></br>
        /// if 'd', get directory of current file<br></br>
        /// if 'f', get filename of current file
        /// </summary>
        /// <param name="which"></param>
        /// <returns></returns>
        public static string GetCurrentPath(char which = 'p')
        {
            NppMsg msg = NppMsg.NPPM_GETFULLCURRENTPATH;
            switch (which)
            {
                case 'p': break;
                case 'd': msg = NppMsg.NPPM_GETCURRENTDIRECTORY; break;
                case 'f': msg = NppMsg.NPPM_GETFILENAME; break;
                default: throw new ArgumentException("GetCurrentPath argument must be one of 'p', 'd', 'f'");
            }

            StringBuilder path = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)msg, 0, path);

            return path.ToString();
        }

        /// <summary>
        /// Get the file type for a file path (no period)<br></br>
        /// Default path is the currently open file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string FileExtension(string path = null)
        {
            if (path == null)
                path = GetCurrentPath('f');
            StringBuilder sb = new StringBuilder();
            for (int ii = path.Length - 1; ii >= 0; ii--)
            {
                char c = path[ii];
                if (c == '.') break;
                sb.Append(c);
            }
            // the chars were added in the wrong direction, so reverse them
            return sb.ToString().Slice("::-1");
        }

        /// <summary>
        /// Trying to copy an empty string or null to the clipboard raises an error.<br></br>
        /// This shows a message box if the user tries to do that.
        /// </summary>
        /// <param name="text"></param>
        public static void TryCopyToClipboard(string text)
        {
            if (text == null || text.Length == 0)
            {
                MessageBox.Show("Couldn't find anything to copy to the clipboard",
                    "Nothing to copy to clipboard",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }
            Clipboard.SetText(text);
        }

        public static string AssemblyVersionString()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            while (version.EndsWith(".0"))
                version = version.Substring(0, version.Length - 2);
#if DEBUG
            return $"{version} Debug";
#else
            return version;
#endif // DEBUG
        }

        public static void CreateConfigSubDirectoryIfNotExists()
        {
            var jsonToolsConfigDir = Path.Combine(Npp.notepad.GetConfigDirectory(), Main.PluginName);
            var jsonToolsConfigDirInfo = new DirectoryInfo(jsonToolsConfigDir);
            if (!jsonToolsConfigDirInfo.Exists)
                jsonToolsConfigDirInfo.Create();
        }

        /// <summary>
        /// for some reason my methods occasionally add an SOH character ('\x01')
        /// to the end of the file. Trim this off.
        /// </summary>
        public static void RemoveTrailingSOH()
        {
            int lastPos = editor.GetLength() - 1;
            int lastChar = editor.GetCharAt(lastPos);
            if (lastChar == 0x01)
            {
                editor.DeleteRange(lastPos, 1);
            }
        }

        /// <summary>
        /// get all text starting at position start in the current document
        /// and ending at position end in the current document
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static string GetSlice(int start, int end)
        {
            int len = end - start;
            IntPtr rangePtr = editor.GetRangePointer(start, len);
            string ansi = Marshal.PtrToStringAnsi(rangePtr, len);
            // TODO: figure out a way to do this that involves less memcopy for non-ASCII
            if (ansi.Any(c => c >= 128))
                return Encoding.UTF8.GetString(Encoding.Default.GetBytes(ansi));
            return ansi;
        }

        private static readonly string[] newlines = new string[] { "\r\n", "\r", "\n" };

        /// <summary>0: CRLF, 1: CR, 2: LF<br></br>
        /// Anything less than 0 or greater than 2: LF</summary>
        public static string GetEndOfLineString(int eolType)
        {
            if (eolType < 0 || eolType >= 3)
                return "\n";
            return newlines[eolType];
        }

        public static string DocumentTypeSuperTypeName(DocumentType documentType)
        {
            switch (documentType)
            {
            case DocumentType.JSON: return "JSON";
            case DocumentType.JSONL: return "JSON";
            case DocumentType.INI: return "INI file";
            default: return documentType.ToString();
            }
        }

        public static readonly Dictionary<int, string> ModificationTypeFlagNames = new Dictionary<int, string>
        {
            [0x01] = "SC_MOD_INSERTTEXT",
            [0x02] = "SC_MOD_DELETETEXT",
            [0x04] = "SC_MOD_CHANGESTYLE",
            [0x08] = "SC_MOD_CHANGEFOLD",
            [0x10] = "SC_PERFORMED_USER",
            [0x20] = "SC_PERFORMED_UNDO",
            [0x40] = "SC_PERFORMED_REDO",
            [0x80] = "SC_MULTISTEPUNDOREDO",
            [0x100] = "SC_LASTSTEPINUNDOREDO",
            [0x200] = "SC_MOD_CHANGEMARKER",
            [0x400] = "SC_MOD_BEFOREINSERT",
            [0x800] = "SC_MOD_BEFOREDELETE",
            [0x4000] = "SC_MOD_CHANGEINDICATOR",
            [0x8000] = "SC_MOD_CHANGELINESTATE",
            [0x200000] = "SC_MOD_CHANGETABSTOPS",
            [0x80000] = "SC_MOD_LEXERSTATE",
            [0x10000] = "SC_MOD_CHANGEMARGIN",
            [0x20000] = "SC_MOD_CHANGEANNOTATION",
            [0x100000] = "SC_MOD_INSERTCHECK",
            [0x1000] = "SC_MULTILINEUNDOREDO",
            [0x2000] = "SC_STARTACTION",
            [0x40000] = "SC_MOD_CONTAINER",
        };

        public static string GetModificationTypeFlagsSet(int modType)
        {
            var sb = new StringBuilder();
            foreach (int modOption in ModificationTypeFlagNames.Keys)
            {
                if ((modType & modOption) != 0)
                {
                    sb.Append(ModificationTypeFlagNames[modOption]);
                    sb.Append(", ");
                }
            }
            sb.Remove(sb.Length - 2, 2);
            return sb.ToString();
        }

        private static string NppVersionString(bool include32bitVs64bit)
        {
            int[] nppVer = notepad.GetNppVersion();
            string nppVerStr = $"{nppVer[0]}.{nppVer[1]}.{nppVer[2]}";
            return include32bitVs64bit ? $"{nppVerStr} {IntPtr.Size * 8}bit" : nppVerStr;
        }
    }

    public enum AskUserWhetherToDoThing
    {
        DONT_DO_DONT_ASK,
        ASK_BEFORE_DOING,
        DO_WITHOUT_ASKING,
    }
}
