using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Forms
{
    public partial class JsonToCsvForm : Form
    {
        public JsonTabularizer tabularizer;
        public JNode json;
        public JsonSchemaMaker schemaMaker;
        public JsonToCsvForm(JNode json)
        {
            InitializeComponent();
            tabularizer = new JsonTabularizer();
            this.json = json;
            schemaMaker = new JsonSchemaMaker();
            DelimBox.SelectedIndex = 0;
            StrategyBox.SelectedIndex = 0;
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
                    // I want to add the ability to let the user choose their own delimiter in addition to the options, but can't figure out how
                    //case 3: delim = DelimBox.SelectedText[0]; break;
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
                Dictionary<string, object> schema = schemaMaker.BuildSchema(json);
                JNode tab = tabularizer.BuildTable(json, schema, keysep);
                csv = tabularizer.TableToCsv((JArray)tab, delim, '"', null, BoolsToIntsCheckBox.Checked);
            }
            catch (Exception ex)
            {
                MessageBox.Show("While trying to create CSV from JSON, raised this exception:\n" + ex.Message,
                    "Exception while tabularizing JSON",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            Npp.notepad.FileNew();
            Npp.editor.AppendTextAndMoveCursor(csv);
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
                MessageBox.Show(ex.ToString(),
                    "Could not open documentation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
