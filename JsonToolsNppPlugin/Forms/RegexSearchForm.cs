using Kbg.NppPluginNET;
using Kbg.NppPluginNET.PluginInfrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JSON_Tools.Utils;
using JSON_Tools.JSON_Tools;
using System.Globalization;

namespace JSON_Tools.Forms
{
    public partial class RegexSearchForm : Form
    {
        public string regex;
        private JsonParser jsonParser;

        public RegexSearchForm()
        {
            InitializeComponent();
            jsonParser = new JsonParser();
        }

        public void GrabFocus()
        {
            RegexTextBox.Focus();
        }

        public void SearchButton_Click(object sender, EventArgs e)
        {
            regex = RegexTextBox.Text;
            if (regex.IndexOf('(') < 0)
            {
                MessageBox.Show("Regex must have at least one capture group", "No capture groups", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            int regexLen = Encoding.UTF8.GetByteCount(regex);
            FindOption findOption = FindOption.REGEXP;
            if (!IgnoreCaseCheckBox.Checked)
                findOption |= FindOption.MATCHCASE;
            Npp.editor.SetSearchFlags(findOption);
            var startEnds = SelectionManager.GetSelectedRanges();
            (int searchStart, int searchEnd) = startEnds[0];
            if (startEnds.Count == 1 && searchEnd > searchStart)
            {
                // for simplicity, only set target to selection for nonzero length single selection
                Npp.editor.TargetFromSelection();
            }
            else
            {
                Npp.editor.TargetWholeDocument();
            }
            int[] groupsToParseAsNumber;
            if (NumGroupsTextBox.Text.Length == 0)
            {
                groupsToParseAsNumber = new int[0];
            }
            else
            {
                try
                {
                    groupsToParseAsNumber = jsonParser.ParseArray(NumGroupsTextBox.Text, 0).children
                        .Select(x => (int)x.value)
                        .ToArray();
                    if (!groupsToParseAsNumber.All(x => x > 0))
                        throw new ArgumentException("All group numbers to be parsed as number must be greater than 0");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"While trying to parse \"Groups to parse as number\" box as integer array, got error {RemesParser.PrettifyException(ex)}",
                        "Could not parse \"groups to parse as number\" box",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    groupsToParseAsNumber = new int[0];
                }
            }
            var allMatches = new List<JNode>();
            int numTags = int.MaxValue;
            int indexInGroupsToParseAsNumber = 0;
            while (true)
            {
                int nextMatchPos = Npp.editor.SearchInTarget(regexLen, regex);
                if (nextMatchPos < 0)
                    break;
                var tagJsons = new List<JNode>();
                for (int currentTag = 1; currentTag < numTags; currentTag++)
                {
                    string tagText = Npp.editor.GetTag(currentTag);
                    // TODO: How to deal with actual empty capture groups (as opposed to a dummy after the last capture group)?
                    // PythonScript somehow has direct access to the Boost regex match objects; how can we access those?
                    if (tagText.Length == 0 && numTags == int.MaxValue)
                        break;
                    JNode tagJson;
                    if (indexInGroupsToParseAsNumber < groupsToParseAsNumber.Length && groupsToParseAsNumber[indexInGroupsToParseAsNumber] == currentTag)
                    {
                        tagJson = ParseNumber(tagText, nextMatchPos);
                        indexInGroupsToParseAsNumber++;
                    }
                    else
                        tagJson = new JNode(tagText, nextMatchPos);
                    tagJsons.Add(tagJson);
                }
                if (tagJsons.Count > 0)
                    allMatches.Add(new JArray(nextMatchPos, tagJsons));
            }
            if (allMatches.Count > 0)
            {
                var newJson = new JArray(searchStart, allMatches);
                Main.AddJsonForFile(Npp.notepad.GetCurrentFilePath(), newJson);
            }
            else
            {
                MessageBox.Show("No matches, or no capture groups", "Search failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        /// <summary>
        /// try to parse inp as a number. If inp can't be parsed as a number, return a JNode with inp as value.<br></br>
        /// The following number formats (and corresponding types) can be parsed (leading + and - signs are both allowed for all)<br></br>
        /// integers (base 10 or hex) => integer<br></br>
        /// decimal numbers (leading or trailing '.' both allowed, as is scientific notation)<br></br>
        /// NaN -> NaN<br></br>
        /// Infinity -> Infinity
        /// </summary>
        /// <param name="inp"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public JNode ParseNumber(string inp, int pos)
        {
            // parsed tracks which portions of a number have been parsed.
            // So if the int part has been parsed, it will be 1.
            // If the int and decimal point parts have been parsed, it will be 3.
            // If the int, decimal point, and scientific notation parts have been parsed, it will be 7
            int parsed = 1;
            char c = inp[0];
            bool negative = false;
            int ii = 0;
            if (c < '0' || c > '9')
            {
                if (c == '-' || c == '+')
                {
                    if (c == '-')
                        negative = true;
                    ii++;
                }
                c = inp[ii];
                // Infinity
                if (c == 'I' && ii <= inp.Length - 8 && inp[ii + 1] == 'n' && inp.Substring(ii + 2, 6) == "finity")
                {
                    double infty = negative ? NanInf.neginf : NanInf.inf;
                    return new JNode(infty, pos);
                }
                // NaN
                else if (c == 'N' && ii <= inp.Length - 3 && inp[ii + 1] == 'a' && inp[ii + 2] == 'N')
                {
                    return new JNode(NanInf.nan, pos);
                }
                else if (c < '0' || c > '9')
                    return new JNode(inp, pos);
            }
            if (c == '0' && ii < inp.Length - 1 && inp[ii + 1] == 'x')
            {
                ii += 2;
                int start = ii;
                while (ii < inp.Length)
                {
                    c = inp[ii];
                    if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                        break;
                    ii++;
                }
                var hexnum = long.Parse(inp.Substring(start, ii - start), NumberStyles.HexNumber);
                return new JNode(negative ? -hexnum : hexnum, pos);
            }
            while (ii < inp.Length)
            {
                c = inp[ii];
                if (c >= '0' && c <= '9')
                {
                    ii++;
                }
                else if (c == '.')
                {
                    if (parsed != 1)
                    {
                        return new JNode(inp, pos); // too many decimal points
                    }
                    parsed = 3;
                    ii++;
                }
                else if (c == 'e' || c == 'E')
                {
                    if ((parsed & 4) != 0)
                    {
                        return new JNode(inp, pos); // already saw scientific notation 'e'
                    }
                    parsed += 4;
                    ii++;
                    if (ii < inp.Length)
                    {
                        c = inp[ii];
                        if (c == '+' || c == '-')
                        {
                            ii++;
                        }
                    }
                    else
                    {
                        // Scientific notation 'e' with no number following
                        return new JNode(inp, pos);
                    }
                }
                else // not a number character, so just treat whole string as not a number
                    return new JNode(inp, pos);
            }
            if (parsed == 1)
            {
                try
                {
                    return new JNode(long.Parse(inp), pos);
                }
                catch (OverflowException)
                {
                    // doubles can represent much larger numbers than 64-bit ints,
                    // albeit with loss of precision
                }
            }
            double num;
            try
            {
                num = double.Parse(inp, JNode.DOT_DECIMAL_SEP);
            }
            catch
            {
                num = NanInf.nan;
            }
            return new JNode(num, pos);
        }
    }
}
