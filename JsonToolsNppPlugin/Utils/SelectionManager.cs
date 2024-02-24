using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JSON_Tools.JSON_Tools;
using Kbg.NppPluginNET;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Utils
{
    public class SelectionManager
    {
        private static readonly Regex START_END_REGEX = new Regex(@"^\d+,\d+$", RegexOptions.Compiled);

        public static bool IsStartEnd(string x) => START_END_REGEX.IsMatch(x);

        public static (int start, int end) ParseStartEndAsTuple(string startEnd)
        {
            int[] startEndNums = ParseStartEnd(startEnd);
            return (startEndNums[0], startEndNums[1]);
        }

        public static List<string> GetRegionsWithIndicator(int indicator1, int indicator2)
        {
            var selList = new List<string>();
            int indEnd = -1;
            int indicator = indicator1;
            while (indEnd < Npp.editor.GetLength())
            {
                int indStart = Npp.editor.IndicatorStart(indicator, indEnd);
                indEnd = Npp.editor.IndicatorEnd(indicator, indStart);
                if (Npp.editor.IndicatorValueAt(indicator, indStart) == 1)
                {
                    selList.Add($"{indStart},{indEnd}");
                    indicator = indicator == indicator1 ? indicator2 : indicator1;
                }
                if (indEnd == 0 && indStart == 0)
                    break;
            }
            return selList;
        }

        public static (int start, int end) GetEnclosingRememberedSelection(int pos, int indicator1, int indicator2)
        {
            if (Npp.editor.IndicatorValueAt(indicator1, pos) == 0)
            {
                if (Npp.editor.IndicatorValueAt(indicator2, pos) == 0)
                    return (-1, -1);
                return (Npp.editor.IndicatorStart(indicator2, pos), Npp.editor.IndicatorEnd(indicator2, pos));
            }
            return (Npp.editor.IndicatorStart(indicator1, pos), Npp.editor.IndicatorEnd(indicator1, pos));
        }

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
                (int start, int end) = ParseStartEndAsTuple(startEnd);
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

        /// <summary>
        /// The user's selections (or the file in its entirety) are probably invalid if every selection's JSON is null and had a fatal error
        /// </summary>
        public static bool NoSelectionIsValidJson(JNode json, bool usesSelections, List<bool> fatalErrors)
        {
            return fatalErrors.All(x => x)
                && (usesSelections && json is JObject obj && obj.children.Values.All(x => x.type == Dtype.NULL)
                    || json.type == Dtype.NULL);
        }
    }
}
