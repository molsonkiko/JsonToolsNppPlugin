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
        public const int LINT_ROW_COUNT = 5000;
        public string fname;
        public JsonLint[] lints;

        public ErrorForm(string fname, JsonLint[] lints)
        {
            InitializeComponent();
            Reload(fname, lints);
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
        }

        public void Reload(string fname, JsonLint[] lints)
        {
            this.fname = fname;
            this.lints = lints;
            Text = $"JSON errors in {fname}";
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
                for (int ii = selRowIndex + 1; ii < ErrorGrid.RowCount; ii++)
                {
                    var row = ErrorGrid.Rows[ii];
                    if (!(row.Cells[1].Value is string description))
                        return;
                    var descStartChar = description[0];
                    if (descStartChar == startChar || descStartChar == startChar + 'a' - 'A')
                    {
                        ChangeSelectedRow(selRowIndex, ii);
                        return;
                    }
                }
            }
        }
    }
}
