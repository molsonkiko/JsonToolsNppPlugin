﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Forms
{
    public partial class JsonToCsvForm : Form
    {
        public JsonTabularizer tabularizer;
        public JNode json;
        public JsonToCsvForm(JNode json)
        {
            InitializeComponent();
            NppFormHelper.RegisterFormIfModeless(this, true);
            FormStyle.ApplyStyle(this, Main.settings.use_npp_styling);
            tabularizer = new JsonTabularizer();
            this.json = json;
            DelimBox.SelectedIndex = 0;
            StrategyBox.SelectedIndex = 0;
            eolComboBox.SelectedIndex = (int)Main.settings.csv_newline;
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

        private void JsonToCsvForm_KeyUp(object sender, KeyEventArgs e)
        {
            NppFormHelper.GenericKeyUpHandler(this, sender, e, true);
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
                string eol = Npp.GetEndOfLineString(eolComboBox.SelectedIndex);
                csv = tabularizer.TableToCsv((JArray)tab, delim, '"', eol, null, BoolsToIntsCheckBox.Checked);
            }
            catch (Exception ex)
            {
                Translator.ShowTranslatedMessageBox(
                    "While trying to create CSV from JSON, raised this exception:\r\n{0}",
                    "Exception while converting JSON to CSV",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    1, RemesParser.PrettifyException(ex));
                return;
            }
            Npp.notepad.FileNew();
            int outLen = Encoding.UTF8.GetByteCount(csv);
            Npp.editor.AppendText(outLen, csv);
            Npp.RemoveTrailingSOH();
            Close();
        }

        private void DocsButton_Click(object sender, EventArgs e)
        {
            Main.OpenUrlInWebBrowser("https://github.com/molsonkiko/JSONToolsNppPlugin/tree/big_integer_for_Dtype_INT/docs/json-to-csv.md");
        }
    }
}
