using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Kbg.NppPluginNET;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Forms
{
    public partial class ErrorForm : Form
    {
        public const int SLOW_RELOAD_THRESHOLD = 300; // completely arbitrary
        public const int LINT_ROW_COUNT = 5000;
        public string fname;
        public JsonLint[] lints;

        public ErrorForm(string fname, JsonLint[] lints)
        {
            InitializeComponent();
            Reload(fname, lints, true);
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
        }

        public bool SlowReloadExpected(IList<JsonLint> lints) { return lints.Count >= SLOW_RELOAD_THRESHOLD; }

        public void Reload(string fname, JsonLint[] lints, bool onStartup = false)
        {
            bool wasBig = SlowReloadExpected(lints);
            if (wasBig && !onStartup)
            {
                if (MessageBox.Show("Reloading this error form could take an extremely long time!\r\n" +
                                    "You will get better results from closing it and reopening it from the plugin menu.\r\n" +
                                    "Do you still want to reload?",
                                    "Very slow error form reload expected",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning
                    ) == DialogResult.No)
                    return;
                Npp.notepad.HideDockingForm(this);
                Hide();
            }
            this.fname = fname;
            this.lints = lints;
            Text = "JSON errors in current file";
            ErrorGrid.Rows.Clear();
            int interval = 1;
            // add a row that warns not all rows are shown
            if (LINT_ROW_COUNT < lints.Length)
            {
                interval = lints.Length / LINT_ROW_COUNT;
                var warningNotAllRowsShown = new DataGridViewRow();
                warningNotAllRowsShown.CreateCells(ErrorGrid);
                warningNotAllRowsShown.Cells[1].Value = $"Showing approximately {LINT_ROW_COUNT} of {lints.Length} rows";
                ErrorGrid.Rows.Add(warningNotAllRowsShown);
            }
            int ii = 0;
            int cycler = 0;
            while (ii < lints.Length)
            {
                var lint = lints[ii];
                var row = new DataGridViewRow();
                row.CreateCells(ErrorGrid);
                row.Cells[0].Value = lint.severity;
                row.Cells[1].Value = lint.message;
                row.Cells[2].Value = lint.pos;
                ErrorGrid.Rows.Add(row);
                if (interval == 1)
                    ii++;
                else
                {
                    // Do not increase by exactly interval every time.
                    // Instead change the interval so that any regular pattern in the errors
                    //     that is not relatively prime with the interval will still be exposed.
                    ii += (cycler % (2 * interval)) + 1;
                    cycler++;
                }
            }
            if (wasBig)
                Npp.notepad.ShowDockingForm(this);
        }

        private void ErrorGrid_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            int rowIndex = e.RowIndex;
            if (!IsValidRowIndex(rowIndex))
                return;
            DataGridViewRow row = ErrorGrid.Rows[rowIndex];
            HandleCellOrRowClick(row);
        }

        /// <summary>
        /// If the row corresponds to a lint, go to that lint's position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleCellOrRowClick(DataGridViewRow row)
        {
            if (!(row.Cells[2].Value is int pos))
                return;
            Npp.editor.GotoPos(pos);
            this.Focus();
        }

        private void ErrorGrid_Resize(object sender, EventArgs e)
        {
            ErrorGrid.Columns[0].Width = 75;
            ErrorGrid.Columns[1].Width = ErrorGrid.Width - 218;
            ErrorGrid.Columns[2].Width = 75;
        }

        private bool IsValidRowIndex(int index)
        {
            return index >= 0 && index < ErrorGrid.RowCount;
        }

        private void ChangeSelectedRow(int oldIndex, int newIndex)
        {
            DataGridViewRow oldRow = ErrorGrid.Rows[oldIndex];
            DataGridViewRow newRow = ErrorGrid.Rows[newIndex];
            oldRow.Selected = false;
            newRow.Selected = true;
            int rowsVisible = ErrorGrid.GetCellCount(DataGridViewElementStates.Displayed) / 3;
            int firstRowToShow = newIndex - rowsVisible + 2;
            // scroll up if new row is above old; scroll down if new row is below old
            if ((firstRowToShow > ErrorGrid.FirstDisplayedScrollingRowIndex) == (newIndex > oldIndex)
                && IsValidRowIndex(firstRowToShow))
                ErrorGrid.FirstDisplayedScrollingRowIndex = firstRowToShow;
            else if (firstRowToShow < 0)
                ErrorGrid.FirstDisplayedScrollingRowIndex = 0;
            HandleCellOrRowClick(newRow); // move to location of error
        }

        private void ErrorForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                Npp.editor.GrabFocus();
                return;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                // refresh error form based on current contents of current file
                e.Handled = true;
                Main.TryParseJson();
                if (Main.TryGetInfoForFile(Main.activeFname, out JsonFileInfo info)
                    && info.lints != null)
                    Reload(Main.activeFname, info.lints);
                return;
            }
            var selRowIndex = ErrorGrid.SelectedCells[0].RowIndex;
            if (!IsValidRowIndex(selRowIndex))
                return;
            if (e.KeyCode == Keys.Up && selRowIndex > 0) // move up
                ChangeSelectedRow(selRowIndex, selRowIndex - 1);
            else if (e.KeyCode == Keys.Down && selRowIndex < ErrorGrid.RowCount - 1)
                ChangeSelectedRow(selRowIndex, selRowIndex + 1); // move down
            // for most keys, seek first row after current row
            // whose description start with that key's char
            else if (e.KeyValue >= ' ' && e.KeyValue <= 126)
            {
                if (ErrorGrid.SelectedCells.Count == 0)
                    return;
                char startChar = (char)e.KeyValue;
                if (!SearchForErrorStartingWithChar(startChar, selRowIndex + 1, ErrorGrid.RowCount, selRowIndex))
                {
                    // wrap around
                    SearchForErrorStartingWithChar(startChar, 0, selRowIndex, selRowIndex);
                }
            }
        }

        /// <summary>
        /// Iterates through rows between indices start (inclusive) and end (exclusive)
        /// until it finds an error whose description begins with startChar.<br></br>
        /// If a row is found, unselect initiallySelected and select the found row.<br></br>
        /// Returns true if such a row was found.
        /// </summary>
        /// <param name="startChar"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private bool SearchForErrorStartingWithChar(char startChar, int start, int end, int initiallySelected)
        {
            for (int ii = start; ii < end; ii++)
            {
                var row = ErrorGrid.Rows[ii];
                if (!(row.Cells[1].Value is string description))
                    break;
                var descStartChar = description[0];
                if (descStartChar == startChar ||
                    ((startChar >= 'A' && startChar <= 'Z') && descStartChar == startChar + 'a' - 'A'))
                {
                    ChangeSelectedRow(initiallySelected, ii);
                    return true;
                }
            }
            return false;
        }
    }
}
