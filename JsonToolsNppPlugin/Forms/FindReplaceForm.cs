using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET.PluginInfrastructure;
using Kbg.NppPluginNET;

namespace JSON_Tools.Forms
{
    public partial class FindReplaceForm : Form
    {
        public TreeViewer treeViewer;
        private string findQuery;
        private string replaceQuery;
        private const int EXTENDED_HEIGHT = 402;
        private const int COLLAPSED_HEIGHT = EXTENDED_HEIGHT - ADVANCED_CONTROLS_EXTENDED_HEIGHT + ADVANCED_CONTROLS_COLLAPSED_HEIGHT;
        private const int ADVANCED_CONTROLS_EXTENDED_HEIGHT = 126;
        private const int ADVANCED_CONTROLS_COLLAPSED_HEIGHT = 11;
        int previousKeysValsBothBox_SelectedIndex = 2;

        public FindReplaceForm(TreeViewer treeViewer)
        {
            InitializeComponent();
            this.treeViewer = treeViewer;
            // if the user is querying a subset of the JSON, the find/replace is done on that subset
            if (treeViewer.Tree.SelectedNode == null || treeViewer.Tree.SelectedNode.FullPath == "JSON")
                RootTextBox.Text = "";
            else RootTextBox.Text = treeViewer.PathToTreeNode(treeViewer.Tree.SelectedNode, KeyStyle.RemesPath);
            KeysValsBothBox.SelectedIndex = 2;
            AdvancedGroupBox.Height = ADVANCED_CONTROLS_COLLAPSED_HEIGHT;
            Height = COLLAPSED_HEIGHT;
            findQuery = "";
            replaceQuery = "";
            //FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
        }

        public static readonly Regex BinopRegex = new Regex(@"[\+\-\|&^%\*/]|\*\*|//|[<>]=?|[=!]=");

        private void FindReplaceForm_KeyUp(object sender, KeyEventArgs e)
        {
            // enter presses button
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (sender is Button btn)
                {
                    // Enter has the same effect as clicking a selected button
                    btn.PerformClick();
                }
                if (sender is TextBox)
                {
                    FindButton.PerformClick();
                }
            }
            // Escape -> go to editor
            else if (e.KeyData == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                Npp.editor.GrabFocus();
            }
            // Tab -> go through controls, Shift+Tab -> go through controls backward
            else if (e.KeyCode == Keys.Tab)
            {
                Control next = GetNextControl((Control)sender, !e.Shift);
                while ((next == null) || (!next.TabStop)) next = GetNextControl(next, !e.Shift);
                next.Focus();
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
                e.Handled = true;
        }

        private void AdvancedGroupBoxLabel_Click(object sender, EventArgs e)
        {
            // show the advanced controls and expand the box
            if (!RegexBox.Visible)
            {
                if (Height < EXTENDED_HEIGHT)
                    Height = EXTENDED_HEIGHT;
                AdvancedGroupBoxLabel.Text = "Hide advanced options";
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
                AdvancedGroupBoxLabel.Text = "Show advanced options";
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
                    findQuery = $"((({root})..*)[is_num(@)])[@ {FindTextBox.Text}]";
                    return;
                }
                findQuery = $"(({root}.*)[is_num(@)])[@ {FindTextBox.Text}]";
                return;
            }
            string keys_find_text;
            string values_find_text;
            if (RegexBox.Checked)
            {
                if (IgnoreCaseCheckBox.Checked)
                    keys_find_text = "g`(?i)" + FindTextBox.Text.Replace("`", "\\`") + '`';
                else
                    keys_find_text = "g`" + FindTextBox.Text.Replace("`", "\\`") + '`';
                values_find_text = $"[str(@) =~ {keys_find_text}]";
            }
            else
            {
                if (IgnoreCaseCheckBox.Checked)
                {
                    // case-insensitive non-regex matching is equivalent to regex matching
                    // with the (?i) flag and anchors at both the end and beginning
                    keys_find_text = "g`(?i)^" + FindTextBox.Text.Replace("`", "\\`") + "$`";
                    values_find_text = $"[str(@) =~ {keys_find_text}]";
                }
                else
                {
                    keys_find_text = '`' + FindTextBox.Text.Replace("`", "\\`") + '`';
                    values_find_text = $"[str(@) == {keys_find_text}]";
                }
            }
            replaceQuery = $"s_sub(@, {keys_find_text}, `" + ReplaceTextBox.Text.Replace("`", "\\`") + "`)";
            if (RecursiveSearchBox.Checked)
            {
                switch (KeysValsBothBox.SelectedIndex)
                {
                    case 0: // keys
                        findQuery = $"({root})..{keys_find_text}";
                        break;
                    case 1: // values
                        findQuery = $"(({root})..*){values_find_text}";
                        break;
                    default: // both keys and values
                        findQuery = "concat(\r\n" +
                                    $"    ({root})..{keys_find_text},\r\n" +
                                    $"    (({root})..*){values_find_text}\r\n" +
                                    $")";
                        break;
                }
                return;
            }
            switch (KeysValsBothBox.SelectedIndex)
            {
                case 0: // keys
                    findQuery = $"{root}.{keys_find_text}";
                    break;
                case 1: // values
                    findQuery = $"({root}){values_find_text}";
                    break;
                default: // both keys and values
                    findQuery = "concat(\r\n" +
                                $"    {root}.{keys_find_text},\r\n" +
                                $"    ({root}){values_find_text}\r\n" +
                                $")";
                    break;
            }
        }

        private void FindButton_Click(object sender, EventArgs e)
        {
            AssignFindReplaceQueries();
            treeViewer.QueryBox.Text = findQuery;
            treeViewer.SubmitQueryButton.PerformClick();
            treeViewer.Tree.Nodes[0].Expand();
        }

        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            int keysvals_index = KeysValsBothBox.SelectedIndex;
            KeysValsBothBox.SelectedIndex = 1;
            AssignFindReplaceQueries();
            treeViewer.QueryBox.Text = findQuery + " = " + replaceQuery;
            treeViewer.SubmitQueryButton.PerformClick();
            KeysValsBothBox.SelectedIndex = keysvals_index;
        }

        private void SwapFindReplaceButton_Click(object sender, EventArgs e)
        {
            string temp = FindTextBox.Text;
            FindTextBox.Text = ReplaceTextBox.Text;
            ReplaceTextBox.Text = temp;
        }
    }
}
