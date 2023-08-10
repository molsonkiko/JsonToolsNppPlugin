using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Utils
{
    public class SelectionManager
    {
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

        public static List<(int start, int end)> GetSelectedRanges()
        {
            var selList = new List<(int start, int end)>();
            int selCount = Npp.editor.GetSelections();
            for (int ii = 0; ii < selCount; ii++)
                selList.Add((Npp.editor.GetSelectionNStart(ii), Npp.editor.GetSelectionNEnd(ii)));
            return selList;
        }

        /// <summary>
        /// takes a list of one or more comma-separated integers
        /// and transforms it into an array of numbers.
        /// </summary>
        /// <param name="startEnd"></param>
        /// <returns></returns>
        public static int[] ParseStartEnd(string startEnd)
        {
            return startEnd.Split(',').Select(s => int.Parse(s)).ToArray();
        }

        public static List<(int start, int end)> SetSelectionsFromStartEnds(IEnumerable<string> startEnds)
        {
            int ii = 0;
            Npp.editor.ClearSelections();
            var result = new List<(int start, int end)>();
            foreach (string startEnd in startEnds)
            {
                int[] ints = ParseStartEnd(startEnd);
                int start = ints[0], end = ints[1];
                result.Add((start, end));
                if (ii++ == 0)
                {
                    Npp.editor.SetSelectionStart(start);
                    Npp.editor.SetSelectionEnd(end);
                }
                else
                {
                    Npp.editor.AddSelection(start, end);
                }
            }
            return result;
        }

        /// <summary>
        /// extract INTEGER1 from string of form INTEGER1,INTEGER2
        /// </summary>
        public static int StartFromStartEnd(string s)
        {
            int commaIdx = s.IndexOf(',');
            return int.Parse(s.Substring(0, commaIdx));
        }

        /// <summary>
        /// compare two strings, "INTEGER1,INTEGER2" and "INTEGER3,INTEGER4" 
        /// comparing INTEGER1 to INTEGER3
        /// </summary>
        public static int StartEndCompareByStart(string s1, string s2)
        {
            return StartFromStartEnd(s1).CompareTo(StartFromStartEnd(s2));
        }


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
