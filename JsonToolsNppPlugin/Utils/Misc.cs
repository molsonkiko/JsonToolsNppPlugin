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
    }
}
