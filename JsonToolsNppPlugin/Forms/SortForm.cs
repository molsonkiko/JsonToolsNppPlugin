using System;
using System.Collections.Generic;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;

namespace JSON_Tools.Forms
{
    public partial class SortForm : Form
    {
        private RemesParser remesParser;

        public SortForm()
        {
            InitializeComponent();
            NppFormHelper.RegisterFormIfModeless(this, false);
            SortMethodBox.SelectedIndex = 0;
            remesParser = new RemesParser();
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
            //OutputFormatBox.SelectedIndex = 0;
        }

        private void SortButton_Clicked(object sender, EventArgs e)
        {
            (ParserState parserState, JNode json, bool usesSelections, _) = Main.TryParseJson(preferPreviousDocumentType:true);
            if (parserState == ParserState.FATAL || json == null)
                return;
            string pathQuery = "@" + PathTextBox.Text;
            JNode jsonAtPath;
            try
            {
                jsonAtPath = remesParser.Search(pathQuery, json);
            }
            catch (Exception ex)
            {
                Translator.ShowTranslatedMessageBox("Could not find json at the specified path ({0}).\r\nGot the following error:\r\n{1}",
                    "Could not find json at that path",
                    MessageBoxButtons.OK, MessageBoxIcon.Error,
                    2, pathQuery, RemesParser.PrettifyException(ex));
                return;
            }
            Func<JNode, JNode> query = null;
            if (IsUsingQuery())
            {
                string queryText = SortMethodBox.SelectedIndex == 2
                    ? $"@[{QueryKeyIndexTextBox.Text}]"
                    : QueryKeyIndexTextBox.Text;
                try
                {
                    query = ((CurJson)remesParser.Compile(queryText)).function;
                }
                catch (Exception ex)
                {
                    Translator.ShowTranslatedMessageBox(
                        "Based on selected sort method, attempted to compile query \"{0}\",\r\nbut got the following error:\r\n{1}",
                        "Failed to compile query for sorting",
                        MessageBoxButtons.OK, MessageBoxIcon.Error,
                        2, queryText, RemesParser.PrettifyException(ex));
                    return;
                }
            }
            if (IsMultipleArraysCheckBox.Checked)
            {
                if (jsonAtPath is JArray arr)
                {
                    foreach (JNode child in arr.children)
                    {
                        if (!SortSingleJson(child, query))
                            return;
                    }
                }
                else if (jsonAtPath is JObject obj)
                {
                    foreach (JNode child in obj.children.Values)
                    {
                        if (!SortSingleJson(child, query))
                            return;
                    }
                }
                else
                {
                    string gotType = JNode.FormatDtype(jsonAtPath.type);
                    Translator.ShowTranslatedMessageBox("JSON at the specified path must be object or array, got {0}",
                        "JSON at specified path must be object or array",
                        MessageBoxButtons.OK, MessageBoxIcon.Error,
                        1, gotType);
                    return;
                }
            }
            else
            {
                if (!SortSingleJson(jsonAtPath, query))
                    return;
            }
            Main.ReformatFileWithJson(json, Main.PrettyPrintFromSettings, usesSelections);
        }

        /// <summary>
        /// Sorts an array in-place using the settings specified in the form's fields.
        /// Returns true if the sorting was successful.
        /// </summary>
        /// <param name="query">query to be applied to each child of the array. Only applicable for some sort methods.</param>
        private bool SortSingleJson(JNode toSort, Func<JNode, JNode> query)
        {
            if (!(toSort is JArray arr))
            {
                string gotType = JNode.FormatDtype(toSort.type);
                Translator.ShowTranslatedMessageBox("Can only sort arrays, got JSON of type {0}",
                    "Can only sort arrays",
                    MessageBoxButtons.OK, MessageBoxIcon.Error,
                    1, gotType);
                return false;
            }
            try
            {
                switch (SortMethodBox.SelectedIndex)
                {
                    case 0: // default
                        arr.children.Sort();
                        break;
                    case 1: // by string value
                        arr.children.Sort((v1, v2) => string.Compare(v1.ToString(), v2.ToString(), StringComparison.CurrentCultureIgnoreCase));
                        break;
                    case 2: // key/index of children
                    case 3: // query on children
                        arr.children.Sort((v1, v2) => query(v1).CompareTo(query(v2)));
                        break;
                    case 4:
                        arr.children.Shuffle();
                        break;
                }
            }
            catch (Exception ex)
            {
                Translator.ShowTranslatedMessageBox("While sorting an array, got the following error:\r\n{0}",
                    "Error while sorting array",
                    MessageBoxButtons.OK, MessageBoxIcon.Error,
                    1, RemesParser.PrettifyException(ex));
                return false;
            }
            if (ReverseOrderCheckBox.Checked && ReverseOrderCheckBox.Enabled)
                arr.children.Reverse();
            return true;
        }

        private bool IsUsingQuery()
        {
            return SortMethodBox.SelectedIndex == 2 || SortMethodBox.SelectedIndex == 3;
        }

        /// <summary>
        /// the query/key/index box should only be enabled if index/key of child
        /// or query on child is the sort method
        /// </summary>
        private void SortMethodBox_SelectionChanged(object sender, EventArgs e)
        {
            QueryKeyIndexTextBox.Enabled = IsUsingQuery();
            ReverseOrderCheckBox.Enabled = SortMethodBox.SelectedIndex != 4;
        }

        /// <summary>
        /// suppress annoying ding when user hits escape or enter
        /// </summary>
        private void SortForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
                e.SuppressKeyPress = true;
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

        private void SortForm_KeyUp(object sender, KeyEventArgs e)
        {
            NppFormHelper.GenericKeyUpHandler(this, sender, e, false);
            //if (e.Alt)
            //{
            //    if (e.KeyCode == Keys.S)
            //    {
            //        e.Handled = true;
            //        SortButton.PerformClick();
            //    }
            //}
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
                e.Handled = true;
        }
    }
}
