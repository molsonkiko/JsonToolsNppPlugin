using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Forms
{
    public partial class JsonToCsvForm : Form
    {
        public JsonTabularizer tabularizer;
        public JNode json;
        public JsonToCsvForm(JNode json)
        {
            InitializeComponent();
            tabularizer = new JsonTabularizer();
            this.json = json;
            DelimBox.SelectedIndex = 0;
            StrategyBox.SelectedIndex = 0;
        }

        private void JsonToCsvForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (sender is Button btn)
                    btn.PerformClick();
                e.Handled = true;
            }
        }

        private void GenerateCSVButton_Click(object sender, EventArgs e)
        {
            string keysep = new string(KeySepBox.Text[0], 1);
            char delim = ',';
            /*
            ,
            Tab (\t)
            |
            */
            switch (DelimBox.SelectedIndex)
            {
                case 0: delim = ','; break;
                case 1: delim = '\t'; break;
                case 2: delim = '|'; break;
            }
            /*
            Default
            Full Recursive
            No recursion
            Stringify iterables
            */
            switch (StrategyBox.SelectedIndex)
            {
                case 0: tabularizer.strategy = JsonTabularizerStrategy.DEFAULT; break;
                case 1: tabularizer.strategy = JsonTabularizerStrategy.FULL_RECURSIVE; break;
                case 2: tabularizer.strategy = JsonTabularizerStrategy.NO_RECURSION; break;
                case 3: tabularizer.strategy = JsonTabularizerStrategy.STRINGIFY_ITERABLES; break;
            }
            string csv = "";
            try
            {
                Dictionary<string, object> schema = JsonSchemaMaker.BuildSchema(json);
                JNode tab = tabularizer.BuildTable(json, schema, keysep);
                csv = tabularizer.TableToCsv((JArray)tab, delim, '"', null, BoolsToIntsCheckBox.Checked);
            }
            catch (Exception ex)
            {
                string expretty = RemesParser.PrettifyException(ex);
                MessageBox.Show("While trying to create CSV from JSON, raised this exception:\n" + expretty,
                    "Exception while tabularizing JSON",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            Npp.notepad.FileNew();
            int out_len = Encoding.UTF8.GetByteCount(csv);
            Npp.editor.AppendText(out_len, csv);
            Close();
        }

        private void DocsButton_Click(object sender, EventArgs e)
        {
            string help_url = "https://github.com/molsonkiko/JSONToolsNppPlugin/tree/main/docs/json-to-csv.md";
            try
            {
                var ps = new ProcessStartInfo(help_url)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                string expretty = RemesParser.PrettifyException(ex);
                MessageBox.Show(expretty,
                    "Could not open documentation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
