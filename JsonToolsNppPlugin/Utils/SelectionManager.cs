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
        public static List<(int start, int end)> GetSelectedRanges()
        {
            var selList = new List<(int start, int end)>();
            int selCount = Npp.editor.GetSelections();
            for (int ii = 0; ii < selCount; ii++)
                selList.Add((Npp.editor.GetSelectionNStart(ii), Npp.editor.GetSelectionNEnd(ii)));
            return selList;
        }

        public static bool NoTextSelected(IList<(int start, int end)> selections)
        {
            (int start, int end) = selections[0];
            return selections.Count < 2 && start == end;
        }

        public static bool NoTextSelected(IList<string> selections)
        {
            int[] startEnd = ParseStartEnd(selections[0]);
            return selections.Count < 2 && startEnd[0] == startEnd[1];
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

        public static int StartEndCompareByStart((int start, int end) se1, (int start, int end) se2)
        {
            return se1.start.CompareTo(se2.start);
        }

        /// <summary>
        /// Given selections (selstart1,selend1), (selstart2,selend2), ..., (selstartN,selendN)<br></br>
        /// returns JSON array string [[selstart1,selend1], [selstart2,selend2], ..., [selstartN,selendN]]
        /// </summary>
        public static string StartEndListToJsonString(IEnumerable<(int start, int end)> selections)
        {
            return "[" + string.Join(", ", selections.OrderBy(x => x.start).Select(x => $"[{x.start},{x.end}]")) + "]";
        }

        /// <summary>
        /// Given selection strings "selstart1,selend1", "selstart2,selend2", ..., "selstartN,selendN"<br></br>
        /// returns JSON array string [[selstart1,selend1], [selstart2,selend2], ..., [selstartN,selendN]]
        /// </summary>
        public static string StartEndListToJsonString(IEnumerable<string> selections)
        {
            return "[" + string.Join(", ", selections.OrderBy(x => StartFromStartEnd(x)).Select(x => $"[{x}]")) + "]";
        }
    }
}
