using Kbg.NppPluginNET;
using System;
using System.Windows.Forms;
using JSON_Tools.Utils;
using JSON_Tools.JSON_Tools;
using System.Linq;
using System.Collections.Generic;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Forms
{
    public partial class RegexSearchForm : Form
    {
        private JsonParser jsonParser;
        private JsonSchemaValidator.ValidationFunc settingsValidator;
        public static bool csvCheckChangeIsAutoTriggered = false;

        public RegexSearchForm()
        {
            InitializeComponent();
            NppFormHelper.RegisterFormIfModeless(this, false);
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
            HeaderHandlingComboBox.SelectedIndex = 0;
            NewlineComboBox.SelectedIndex = 0;
            jsonParser = new JsonParser(LoggerLevel.JSON5, false, false, false);
            settingsValidator = JsonSchemaValidator.CompileValidationFunc(new JsonParser().Parse(
                "{\r\n" +
                 "\t\"$schema\": \"https://json-schema.org/draft/2020-12/schema\",\r\n" +
                 "\t\"anyOf\": [\r\n" +
                 "\t\t{\r\n" + // somewhat kludgy: if "csv" is true, follow this schema
                 "\t\t\t\"type\": \"object\",\r\n" +
                 "\t\t\t\"required\": [\"csv\", \"delim\", \"header\", \"nColumns\", \"newline\", \"quote\"],\r\n" +
                 "\t\t\t\"properties\": {\r\n" +
                 "\t\t\t\t\"csv\": {\"enum\": [true]},\r\n" + // sets the ParseCsvButton
                 "\t\t\t\t\"delim\": {\"type\": \"string\", \"minLength\": 1, \"maxLength\": 2},\r\n" + // sets the delimiter text box
                 "\t\t\t\t\"newline\": {\"enum\": [\"\\r\\n\", \"\\r\", \"\\n\", 0, 1, 2]},\r\n" + // sets the newline combo box (0 and "\r\n" map to "\r\n", 1 and "\n" map to "\n", 2 and "\r" map to "\r")
                 "\t\t\t\t\"header\": {\"enum\": [0, 1, 2, \"n\", \"d\", \"h\"]},\r\n" + // sets the header handling combo box (0 and "n" map to skipping header, 1 and "h" map to including header, 2 and "d" map to using header as keys)
                 "\t\t\t\t\"nColumns\": {\"type\": \"integer\", \"exclusiveMinimum\": 0},\r\n" + // nColumns text box
                 "\t\t\t\t\"quote\": {\"type\": \"string\", \"minLength\": 1, \"maxLength\": 1},\r\n" + // quote box
                 "\t\t\t\t\"numCols\": {\"type\": \"array\", \"items\": {\"type\": \"integer\"}, \"minItems\": 1}\r\n" + // column numbers to parse as number (omit instead of using 0-length array)
                 "\t\t\t}\r\n" +
                 "\t\t},\r\n" +
                 "\t\t{\r\n" + // and if "csv" is false, follow this schema
                 "\t\t\t\"type\": \"object\",\r\n" +
                 "\t\t\t\"required\": [\"csv\", \"regex\", \"fullMatch\", \"ignoreCase\"],\r\n" +
                 "\t\t\t\"properties\": {\r\n" +
                 "\t\t\t\t\"csv\": {\"enum\": [false]},\r\n" + // same as first branch
                 "\t\t\t\t\"regex\": {\"type\": \"string\", \"minLength\": 1},\r\n" + // regex text box
                 "\t\t\t\t\"ignoreCase\": {\"type\": \"boolean\"},\r\n" + // ignore case checkbox
                 "\t\t\t\t\"fullMatch\": {\"type\": \"boolean\"},\r\n" + // include full match as first item checkbox
                 "\t\t\t\t\"numCols\": {\"type\": \"array\", \"items\": {\"type\": \"integer\"}, \"minItems\": 1}\r\n" + // same as first branch
                 "\t\t\t}\r\n" +
                 "\t\t}\r\n" +
                 "\t]\r\n" +
                 "}"),
            0, false);
            GetTreeViewInRegexMode();
            // check if we have a CSV file
            if (Main.settings.auto_try_guess_csv_delim_newline
                && TrySniffCommonDelimsAndEols(out EndOfLine eol, out char delim, out int nColumns))
            {
                SetCsvSettingsFromEolNColumnsDelim(true, eol, delim, nColumns);
            }
        }

        public void GrabFocus()
        {
            Show();
            GetTreeViewInRegexMode();
            RegexTextBox.Focus();
        }

        /// <summary>
        /// if a treeview is open for the current document, change it to REGEX mode (if not already in that mode)<br></br>
        /// Otherwise, open a treeview for the current document in REGEX mode
        /// </summary>
        /// <returns></returns>
        private void GetTreeViewInRegexMode()
        {
            if (Main.openTreeViewer is null || Main.openTreeViewer.IsDisposed || Main.openTreeViewer.fname != Main.activeFname)
            {
                Main.OpenJsonTree(DocumentType.REGEX);
            }
            else
            {
                Main.openTreeViewer.SetDocumentTypeComboBoxIndex(DocumentType.REGEX);
            }
        }

        private static readonly string[] EOLS = new[] { "`\\r\\n`", "`\\n`", "`\\r`" };

        private static readonly char[] HEADER_HANDLING_ABBREVS = new char[] { 'n', 'h', 'd' };

        private static readonly Dictionary<string, int> HEADER_HANDLING_ABBREV_MAP = new Dictionary<string, int> { ["\"h\""] = 1, ["\"n\""] = 0, ["\"d\""] = 2, ["1"] = 1, ["2"] = 2, ["0"] = 0 };

        private static readonly Dictionary<string, int> NEWLINE_MAP = new Dictionary<string, int> { ["\"\\r\\n\""] = 0, ["\"\\n\""] = 1, ["\"\\r\""] = 2, ["0"] = 0, ["1"] = 1, ["2"] = 2 };

        public void SearchButton_Click(object sender, EventArgs e)
        {
            GetTreeViewInRegexMode();
            string columnsToParseAsNumber = "";
            if (ColumnsToParseAsNumberTextBox.Text.Length > 0)
            {
                JNode columnsToParseAsNumberArr = jsonParser.Parse(ColumnsToParseAsNumberTextBox.Text);
                if (jsonParser.fatal || !(columnsToParseAsNumberArr is JArray arr) || arr.Length == 0 || arr.children.Any(x => x.type != Dtype.INT))
                    Translator.ShowTranslatedMessageBox("Columns to parse as number must be a nonempty JSON array of integers", "Columns to parse as number must be array of integers", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    columnsToParseAsNumber = ", " + columnsToParseAsNumberArr.ToString(false).Slice("1:-1");
            }
            if (ParseAsCsvCheckBox.Checked)
            {
                string delim = DelimiterTextBox.Text == "\\t" ? "`\\t`" : RemesPathLexer.StringToBacktickString(DelimiterTextBox.Text);
                string quote = RemesPathLexer.StringToBacktickString(QuoteCharTextBox.Text);
                string newline = EOLS.FirstIfOutOfBounds(NewlineComboBox.SelectedIndex);
                char headerHandlingAbbrev = HEADER_HANDLING_ABBREVS.FirstIfOutOfBounds(HeaderHandlingComboBox.SelectedIndex);
                Main.openTreeViewer.QueryBox.Text = $"s_csv(@, {NColumnsTextBox.Text}, {delim}, {newline}, {quote}, {headerHandlingAbbrev}{columnsToParseAsNumber})";
            }
            else
            {
                string ignoreCaseFlag = IgnoreCaseCheckBox.Checked ? "(?i)" : "(?-i)";
                string regex = RemesPathLexer.StringToBacktickString(ignoreCaseFlag + RegexTextBox.Text);
                string includeFullMatchAsFirstItem = IncludeFullMatchAsFirstItemCheckBox.Checked ? "true" : "false";
                Main.openTreeViewer.QueryBox.Text = $"s_fa(@, {regex}, {includeFullMatchAsFirstItem}{columnsToParseAsNumber})";
            }
            if (!Main.openTreeViewer.Visible)
                Npp.notepad.ShowDockingForm(Main.openTreeViewer);
            Main.openTreeViewer.SubmitQueryButton.PerformClick();
        }

        /// <summary>
        /// suppress the default response to the Tab key
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData.HasFlag(Keys.Tab)) // this covers Tab with or without modifiers
                return true;
            return base.ProcessDialogKey(keyData);
        }

        /// <summary>
        /// suppress annoying dings from some control keys
        /// </summary>
        private void RegexSearchForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape || e.KeyCode == Keys.Tab)
                e.SuppressKeyPress = true;
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
                e.Handled = true;
        }

        private void RegexSearchForm_KeyUp(object sender, KeyEventArgs e)
        {
            NppFormHelper.GenericKeyUpHandler(this, sender, e, false);
        }

        /// <summary>
        /// <strong>Checking</strong>the ParseAsCsvCheckBox does the following:<br></br>
        /// - reveals all the CSV-related controls<br></br>
        /// - disables the regex-related controls<br></br>
        /// - sniffs the first 16 KB of the document (or 16 lines, whichever comes first)
        ///    using every combo of (',', '\t') delimiters and ('\r\n', '\n', '\r') newlines
        ///    and sets the CSV controls appropriately if a match is found<br></br>
        /// <strong>Unchecking</strong>the ParseAsCsvCheckBox does the following:<br></br>
        /// - hides the CSV related controls<br></br>
        /// - enables the regex-related controls
        /// </summary>
        public void ParseAsCsvCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            bool showCsvButtons = ParseAsCsvCheckBox.Checked;
            // thanks to the magical mysteries of registering this form with NPPM_MODELESSDIALOG,
            // the order in which I make controls visible defines their tab order.
            DelimiterTextBox.Visible = showCsvButtons;
            DelimiterTextBoxLabel.Visible = showCsvButtons;
            QuoteCharTextBox.Visible = showCsvButtons;
            QuoteCharTextBoxLabel.Visible = showCsvButtons;
            NewlineComboBox.Visible = showCsvButtons;
            NewlineComboBoxLabel.Visible = showCsvButtons;
            NColumnsTextBox.Visible = showCsvButtons;
            NColumnsTextBoxLabel.Visible = showCsvButtons;
            HeaderHandlingComboBox.Visible = showCsvButtons;
            HeaderHandlingComboBoxLabel.Visible = showCsvButtons;
            RegexTextBox.Enabled = !showCsvButtons;
            IgnoreCaseCheckBox.Enabled = !showCsvButtons;
            IncludeFullMatchAsFirstItemCheckBox.Enabled = !showCsvButtons;
            if (!csvCheckChangeIsAutoTriggered && showCsvButtons && Main.settings.auto_try_guess_csv_delim_newline
                && TrySniffCommonDelimsAndEols(out EndOfLine eol, out char delim, out int nColumns))
            {
                SetCsvSettingsFromEolNColumnsDelim(true, eol, delim, nColumns);
            }
        }

        public void SetCsvSettingsFromEolNColumnsDelim(bool csvBoxShouldBeChecked, EndOfLine eol, char delim, int nColumns)
        {
            if (ParseAsCsvCheckBox.Checked != csvBoxShouldBeChecked)
            {
                csvCheckChangeIsAutoTriggered = true;
                ParseAsCsvCheckBox.Checked = csvBoxShouldBeChecked;
                csvCheckChangeIsAutoTriggered = false;
            }
            if (csvBoxShouldBeChecked)
            {
                NColumnsTextBox.Text = nColumns.ToString();
                DelimiterTextBox.Text = ArgFunction.CsvCleanChar(delim);
                QuoteCharTextBox.Text = "\"";
                NewlineComboBox.SelectedIndex = eol == EndOfLine.CRLF ? 0 : eol == EndOfLine.LF ? 1 : 2;
            }
        }

        public static bool TrySniffCommonDelimsAndEols(out EndOfLine eol, out char delim, out int nColumns)
        {
            eol = EndOfLine.CRLF;
            delim = '\x00';
            nColumns = -1;
            string text = Npp.editor.GetText(CsvSniffer.DEFAULT_MAX_CHARS_TO_SNIFF * 12 / 10);
            int crlfCount = 0;
            int crCount = 0;
            int lfCount = 0;
            int ii = 0;
            while (ii < text.Length)
            {
                char c = text[ii++];
                if (c == '\r')
                {
                    if (ii < text.Length && text[ii] == '\n')
                    {
                        crlfCount++;
                        ii++;
                    }
                    else
                        crCount++;
                }
                else if (c == '\n')
                    lfCount++;
            }
            EndOfLine maybeEol = EndOfLine.CRLF;
            if (crCount > crlfCount && crCount > lfCount)
                maybeEol = EndOfLine.CR;
            else if (lfCount > crCount && lfCount > crlfCount)
                maybeEol = EndOfLine.LF;
            foreach (char maybeDelim in ",\t")
            {
                nColumns = CsvSniffer.Sniff(text, maybeEol, maybeDelim, '"');
                if (nColumns >= 2)
                {
                    delim = maybeDelim;
                    eol = maybeEol;
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// settings must be an object following the JSON schema shown in the constructor for this class<br></br>
        /// if settings does not follow that schema, return false and errorMessage = the validation problem<br></br>
        /// if settings follows the scheam, set settings according to the object and return true
        /// </summary>
        /// <param name="settingsNode">a JSON object representing the settings to use</param>
        /// <param name="wasCalledByUser">was this invoked by a user (if true, show a message box if there's an issue)</param>
        public bool SetFieldsFromJson(JNode settingsNode, bool wasCalledByUser, out string errorMessage)
        {
            bool validates = settingsValidator(settingsNode, out List<JsonLint> lints);
            errorMessage = null;
            if (!validates)
            {
                errorMessage = $"invalid settings {settingsNode.ToString()}, got validation problem(s) {JsonSchemaValidator.LintsAsJArrayString(lints)}";
                if (wasCalledByUser)
                    MessageBox.Show(errorMessage,
                        "invalid settings",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            JObject settings = (JObject)settingsNode;
            ParseAsCsvCheckBox.Checked = (bool)settings["csv"].value;
            ColumnsToParseAsNumberTextBox.Text = settings.children.TryGetValue("numCols", out JNode numColNode) && numColNode is JArray numColArr
                ? numColArr.ToString()
                : "";
            if (ParseAsCsvCheckBox.Checked)
            {
                DelimiterTextBox.Text = (string)settings["delim"].value;
                QuoteCharTextBox.Text = (string)settings["quote"].value;
                NColumnsTextBox.Text = settings["nColumns"].ToString();
                HeaderHandlingComboBox.SelectedIndex = HEADER_HANDLING_ABBREV_MAP[settings["header"].ToString()];
                NewlineComboBox.SelectedIndex = NEWLINE_MAP[settings["newline"].ToString()];
            }
            else
            {
                IgnoreCaseCheckBox.Checked = (bool)settings["ignoreCase"].value;
                IncludeFullMatchAsFirstItemCheckBox.Checked = (bool)settings["fullMatch"].value;
                RegexTextBox.Text = (string)settings["regex"].value;
            }
            return true;
        }
    }
}
