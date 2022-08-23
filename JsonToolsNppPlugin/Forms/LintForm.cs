using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JSON_Viewer.JSONViewer;

namespace JSON_Viewer.Forms
{
    /// <summary>
    /// A form for viewing JSON syntax errors for files read so far.
    /// </summary>
    public partial class LintForm : Form
    {
        public TreeViewer treeViewer;

        public LintForm(TreeViewer treeViewer)
        {
            InitializeComponent();
            this.treeViewer = treeViewer;
            DocListBox.BeginUpdate();
            DocListBox.Items.Clear();
            foreach (string fname in treeViewer.fname_lints.Keys)
            {
                DocListBox.Items.Add(fname);
            }
            DocListBox.EndUpdate();
        }

        private void DocListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DocListBox.SelectedItems.Count > 0)
            {
                string fname = (string)DocListBox.SelectedItem;
                StringBuilder sb = new StringBuilder();
                //MessageBox.Show(treeViewer.fname_lints[fname].Length.ToString());
                foreach (JsonLint lint in treeViewer.fname_lints[fname])
                {
                    sb.AppendLine($"Syntax error on line {lint.line} (position {lint.pos}, char {lint.cur_char}): {lint.message}");
                }
                LintBox.Text = sb.ToString();
            }
        }

        /// <summary>
        /// Save the lint for the currently selected file to a text doc
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveLintButton_Click(object sender, EventArgs e)
        {
            if (DocListBox.SelectedItems.Count > 0)
            {
                string fname = (string)DocListBox.SelectedItem;
                Stream myStream;
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "All files|*.*|text files|*.txt";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;
                saveFileDialog1.Title = $"Save parser errors for {fname} to text file?";

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if ((myStream = saveFileDialog1.OpenFile()) != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"Syntax errors for {fname} on {System.DateTime.Now}"); // metadata
                        sb.Append(LintBox.Text);
                        myStream.Write(Encoding.UTF8.GetBytes(sb.ToString()));
                        myStream.Close();
                    }
                }
            }
        }
    }
}
