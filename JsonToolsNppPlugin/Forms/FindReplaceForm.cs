using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;

namespace JSON_Tools.Forms
{
    public partial class FindReplaceForm : Form
    {
        public TreeViewer treeViewer;
        private string findQuery;
        private string replaceQuery;
        private const int EXTENDED_HEIGHT = 441;
        private const int COLLAPSED_HEIGHT = EXTENDED_HEIGHT - ADVANCED_CONTROLS_EXTENDED_HEIGHT + ADVANCED_CONTROLS_COLLAPSED_HEIGHT;
        private const int ADVANCED_CONTROLS_EXTENDED_HEIGHT = 163;
        private const int ADVANCED_CONTROLS_COLLAPSED_HEIGHT = 11;
        int previousKeysValsBothBox_SelectedIndex = 2;

        public FindReplaceForm(TreeViewer treeViewer)
        {
            InitializeComponent();
            NppFormHelper.RegisterFormIfModeless(this, false);
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
            this.treeViewer = treeViewer;
            // if the user is querying a subset of the JSON, the find/replace is done on that subset
            if (treeViewer.Tree.SelectedNode == null || treeViewer.Tree.SelectedNode.FullPath == "JSON")
                RootTextBox.Text = "";
            else RootTextBox.Text = treeViewer.PathToTreeNode(treeViewer.Tree.SelectedNode, KeyStyle.RemesPath);
            if (treeViewer.UsesSelections())
            {
                // when the tree uses selections, queries are performed on each selection separately
                // rather than on the selection tree, so get rid of the selection key
                var mtchSelEnd = new Regex("\\d+[\"'`]\\]?").Match(RootTextBox.Text);
                if (mtchSelEnd.Success)
                {
                    int selEnd = mtchSelEnd.Index + mtchSelEnd.Length;
                    RootTextBox.Text = RootTextBox.Text.Substring(selEnd);
                }
            }
            KeysValsBothBox.SelectedIndex = 2;
            AdvancedGroupBox.Height = ADVANCED_CONTROLS_COLLAPSED_HEIGHT;
            Height = COLLAPSED_HEIGHT;
            findQuery = "";
            replaceQuery = "";
        }

        public static readonly Regex BinopRegex = new Regex(@"[\+\-\|&^%\*/]|\*\*|//|[<>]=?|[=!]=");

        /// <summary>
        /// suppress annoying ding when user hits escape or enter
        /// </summary>
        private void FindReplaceForm_KeyDown(object sender, KeyEventArgs e)
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

        private void FindReplaceForm_KeyUp(object sender, KeyEventArgs e)
        {
            NppFormHelper.GenericKeyUpHandler(this, sender, e, false);
            //if (e.Alt)
            //{
            //    switch (e.KeyCode)
            //    {
            //        case Keys.F: FindButton.PerformClick(); e.Handled = true; break;
            //        case Keys.R: ReplaceButton.PerformClick(); e.Handled = true; break;
            //        case Keys.W: SwapFindReplaceButton.PerformClick(); e.Handled = true; break;
            //    }
            //}
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
                e.Handled = true;
        }

        private void ShowAdvancedOptionsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // show the advanced controls and expand the box
            if (!RegexBox.Visible)
            {
                if (Height < EXTENDED_HEIGHT)
                    Height = EXTENDED_HEIGHT;
                ShowAdvancedOptionsCheckBox.Text = "Hide advanced options";
                AdvancedGroupBox.Height = ADVANCED_CONTROLS_EXTENDED_HEIGHT;
                foreach (Control control in AdvancedGroupBox.Controls)
                {
                    control.Visible = true;
                }
            }
            // hide the advanced controls and collapse the box
            else
            {
                if (Height == EXTENDED_HEIGHT)
                    Height = COLLAPSED_HEIGHT;
                ShowAdvancedOptionsCheckBox.Text = "Show advanced options";
                AdvancedGroupBox.Height = ADVANCED_CONTROLS_COLLAPSED_HEIGHT;
                foreach (Control control in AdvancedGroupBox.Controls)
                {
                    control.Visible = false;
                }
            }
        }

        private void MathBox_CheckedChanged(object sender, EventArgs e)
        {
            // you can only filter on values when in math mode
            if (MathBox.Checked)
            {
                previousKeysValsBothBox_SelectedIndex = KeysValsBothBox.SelectedIndex;
                KeysValsBothBox.SelectedIndex = 1;
                KeysValsBothBox.Enabled = false;
                return;
            }
            KeysValsBothBox.SelectedIndex = previousKeysValsBothBox_SelectedIndex;
            KeysValsBothBox.Enabled = true;
        }

        private void AssignFindReplaceQueries()
        {
            string root = $"@{RootTextBox.Text}";
            if (MathBox.Checked) // it's a math expression like "> 2" or "% 2 == 1"
            {
                replaceQuery = BinopRegex.IsMatch(ReplaceTextBox.Text) // e.g. "* 3", "% 4"
                        ? $"@ {ReplaceTextBox.Text}" // convert to "@ * 3", "@ % 4"
                        : ReplaceTextBox.Text; // it's a constant like "3000"
                // can only filter on values when in math mode, so don't consider KeysValsBothBox
                if (RecursiveSearchBox.Checked)
                {
                    findQuery = $"({root})..*[is_num(@)][@ {FindTextBox.Text}]";
                    return;
                }
                findQuery = $"({root}).*[is_num(@)][@ {FindTextBox.Text}]";
                return;
            }
            string keysFindText;
            string valuesFindText;
            if (RegexBox.Checked)
            {
                if (IgnoreCaseCheckBox.Checked)
                    keysFindText = "g`(?i)" + FindTextBox.Text.Replace("`", "\\`") + '`';
                else
                    keysFindText = "g`" + FindTextBox.Text.Replace("`", "\\`") + '`';
                valuesFindText = $"[str(@) =~ {keysFindText}]";
            }
            else if (MatchExactlyBox.Checked)
            {
                if (IgnoreCaseCheckBox.Checked)
                {
                    // exact matching is equivalent to regex matching
                    // with all special metacharacters escaped and anchors at the beginning and end
                    // add the (?i) flag for case-insensitive matching
                    keysFindText = "g`(?i)\\A" + Regex.Escape(FindTextBox.Text).Replace("\\", "\\\\").Replace("`", "\\`") + "\\z`";
                    valuesFindText = $"[str(@) =~ {keysFindText}]";
                }
                else
                {
                    // simplest case; do normal hash map key search, or check if string equals find text exactly
                    keysFindText = "`" + FindTextBox.Text.Replace("`", "\\`") + "`";
                    valuesFindText = $"[str(@) == {keysFindText}]";
                }
            }
            else
            {
                // non-regex matching is equivalent to regex matching
                // with all special metacharacters escaped
                if (IgnoreCaseCheckBox.Checked)
                {
                    // add the (?i) flag for case-insensitive matching
                    keysFindText = "g`(?i)" + Regex.Escape(FindTextBox.Text).Replace("\\", "\\\\").Replace("`", "\\`") + "`";
                    valuesFindText = $"[str(@) =~ {keysFindText}]";
                }
                else
                {
                    keysFindText = "g`" + Regex.Escape(FindTextBox.Text).Replace("\\", "\\\\").Replace("`", "\\`") + "`";
                    valuesFindText = $"[str(@) =~ {keysFindText}]";
                }
            }
            replaceQuery = $"s_sub(@, {keysFindText}, `" + ReplaceTextBox.Text.Replace("`", "\\`") + "`)";
            if (RecursiveSearchBox.Checked)
            {
                switch (KeysValsBothBox.SelectedIndex)
                {
                    case 0: // keys
                        findQuery = $"({root})..{keysFindText}";
                        break;
                    case 1: // values
                        findQuery = $"({root})..*{valuesFindText}";
                        break;
                    default: // both keys and values
                        findQuery = "concat(\r\n" +
                                    $"    ({root})..{keysFindText},\r\n" +
                                    $"    ({root})..*{valuesFindText}\r\n" +
                                    $")";
                        break;
                }
                return;
            }
            switch (KeysValsBothBox.SelectedIndex)
            {
                case 0: // keys
                    findQuery = $"({root}).{keysFindText}";
                    break;
                case 1: // values
                    findQuery = $"{root}{valuesFindText}";
                    break;
                default: // both keys and values
                    findQuery = "concat(\r\n" +
                                $"    ({root}).{keysFindText},\r\n" +
                                $"    {root}{valuesFindText}\r\n" +
                                $")";
                    break;
            }
        }

        private void FindButton_Click(object sender, EventArgs e)
        {
            treeViewer.RefreshButton.PerformClick();
            AssignFindReplaceQueries();
            treeViewer.QueryBox.Text = findQuery;
            treeViewer.SubmitQueryButton.PerformClick();
            treeViewer.Tree.Nodes[0].Expand();
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            int keysvalsIndex = KeysValsBothBox.SelectedIndex;
            KeysValsBothBox.SelectedIndex = 1;
            AssignFindReplaceQueries();
            string root = (RecursiveSearchBox.Checked) ? $"(@{RootTextBox.Text})..*" : $"@{RootTextBox.Text}";
            // for math find/replace we want to filter before performing the operation
            // for regex find/replace the s_sub function naturally filters (because it's only replacing the target substring)
            // thus, we only want to use the findQuery to filter when doing math find/replace
            string findPart = MathBox.Checked
                ? $"var x = {findQuery}"
                : $"var x = {root}[is_str(@)]";
            treeViewer.QueryBox.Text = $"{findPart};\r\nx = {replaceQuery};\r\nx";
            // use variable assignment, then mutate the variable, then show the variable.
            // this is nice because (1) it introduces variable assignment,
            // and (2) it displays exactly the values that were mutated rather than forcing the user to find them
            treeViewer.SubmitQueryButton.PerformClick();
            treeViewer.Tree.Nodes[0].Expand();
            KeysValsBothBox.SelectedIndex = keysvalsIndex;
        }

        private void SwapFindReplaceButton_Click(object sender, EventArgs e)
        {
            string temp = FindTextBox.Text;
            FindTextBox.Text = ReplaceTextBox.Text;
            ReplaceTextBox.Text = temp;
        }

        private void MatchExactlyBox_CheckedChanged(object sender, EventArgs e)
        {
            // regex matching and whole word matching are mutually exclusive
            if (MatchExactlyBox.Checked) RegexBox.Checked = false;
            RegexBox.Enabled = !MatchExactlyBox.Checked;
        }

        private void RegexBox_CheckedChanged(object sender, EventArgs e)
        {
            // regex matching and whole word matching are mutually exclusive
            if (RegexBox.Checked) MatchExactlyBox.Checked = false;
            MatchExactlyBox.Enabled = !RegexBox.Checked;
        }
    }
}
