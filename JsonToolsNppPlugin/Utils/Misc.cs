using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            editor.AppendTextAndMoveCursor(inp);
            editor.AppendTextAndMoveCursor(System.Environment.NewLine);
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

        //public static DateTime LastSavedTime(string fname)
        //{
        //    DateTime now = DateTime.Now;
        //    //NppMsg msg = NppMsg.
        //    return now;
        //}
    }
}
