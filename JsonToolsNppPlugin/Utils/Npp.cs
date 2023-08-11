using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
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
        /// If the file is really big (default 4+ MB, configured by settings.max_size_full_tree_MB),
        /// this is a no-op, because lexing big JSON files is very slow.
        /// </summary>
        public static void SetLangJson()
        {
            if (editor.GetLength() < Main.settings.max_file_size_MB_slow_actions * 1e6)
                notepad.SetCurrentLanguage(LangType.L_JSON);
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
            return version;
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
    }
}
