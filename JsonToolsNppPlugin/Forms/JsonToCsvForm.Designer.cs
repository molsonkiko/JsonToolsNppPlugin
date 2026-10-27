namespace JSON_Tools.Forms
{
    partial class JsonToCsvForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                NppFormHelper.UnregisterFormIfModeless(this, true);
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JsonToCsvForm));
            this.KeySepBox = new System.Windows.Forms.TextBox();
            this.JsonToCsvFormTitle = new System.Windows.Forms.Label();
            this.DelimBox = new System.Windows.Forms.ComboBox();
            this.BoolsToIntsCheckBox = new System.Windows.Forms.CheckBox();
            this.StrategyBox = new System.Windows.Forms.ComboBox();
            this.StrategyBoxLabel = new System.Windows.Forms.Label();
            this.KeySepBoxLabel = new System.Windows.Forms.Label();
            this.DelimBoxLabel = new System.Windows.Forms.Label();
            this.GenerateCSVButton = new System.Windows.Forms.Button();
            this.DocsButton = new System.Windows.Forms.Button();
            this.AcceptCancelButton = new System.Windows.Forms.Button();
            this.eolComboBoxLabel = new System.Windows.Forms.Label();
            this.eolComboBox = new System.Windows.Forms.ComboBox();
            this.SortColNamesCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // KeySepBox
            // 
            this.KeySepBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.KeySepBox.Location = new System.Drawing.Point(34, 70);
            this.KeySepBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.KeySepBox.MaxLength = 1;
            this.KeySepBox.Name = "KeySepBox";
            this.KeySepBox.Size = new System.Drawing.Size(58, 28);
            this.KeySepBox.TabIndex = 0;
            this.KeySepBox.Text = ".";
            this.KeySepBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.JsonToCsvForm_KeyUp);
            // 
            // JsonToCsvFormTitle
            // 
            this.JsonToCsvFormTitle.AutoSize = true;
            this.JsonToCsvFormTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.JsonToCsvFormTitle.Location = new System.Drawing.Point(60, 11);
            this.JsonToCsvFormTitle.Name = "JsonToCsvFormTitle";
            this.JsonToCsvFormTitle.Size = new System.Drawing.Size(284, 29);
            this.JsonToCsvFormTitle.TabIndex = 8;
            this.JsonToCsvFormTitle.Text = "Create CSV from JSON";
            // 
            // DelimBox
            // 
            this.DelimBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DelimBox.FormattingEnabled = true;
            this.DelimBox.Items.AddRange(new object[] {
            ",",
            "\\t (Tab)",
            "|"});
            this.DelimBox.Location = new System.Drawing.Point(34, 128);
            this.DelimBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.DelimBox.Name = "DelimBox";
            this.DelimBox.Size = new System.Drawing.Size(84, 30);
            this.DelimBox.TabIndex = 1;
            this.DelimBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.JsonToCsvForm_KeyUp);
            // 
            // BoolsToIntsCheckBox
            // 
            this.BoolsToIntsCheckBox.AutoSize = true;
            this.BoolsToIntsCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BoolsToIntsCheckBox.Location = new System.Drawing.Point(34, 239);
            this.BoolsToIntsCheckBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.BoolsToIntsCheckBox.Name = "BoolsToIntsCheckBox";
            this.BoolsToIntsCheckBox.Size = new System.Drawing.Size(238, 26);
            this.BoolsToIntsCheckBox.TabIndex = 3;
            this.BoolsToIntsCheckBox.Text = "Convert true/false to 1/0?";
            this.BoolsToIntsCheckBox.UseVisualStyleBackColor = true;
            this.BoolsToIntsCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.JsonToCsvForm_KeyUp);
            // 
            // StrategyBox
            // 
            this.StrategyBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StrategyBox.FormattingEnabled = true;
            this.StrategyBox.Items.AddRange(new object[] {
            "Default",
            "Full recursive",
            "No recursion",
            "Stringify iterables"});
            this.StrategyBox.Location = new System.Drawing.Point(34, 344);
            this.StrategyBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.StrategyBox.Name = "StrategyBox";
            this.StrategyBox.Size = new System.Drawing.Size(219, 30);
            this.StrategyBox.TabIndex = 5;
            this.StrategyBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.JsonToCsvForm_KeyUp);
            // 
            // StrategyBoxLabel
            // 
            this.StrategyBoxLabel.AutoSize = true;
            this.StrategyBoxLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StrategyBoxLabel.Location = new System.Drawing.Point(276, 347);
            this.StrategyBoxLabel.Name = "StrategyBoxLabel";
            this.StrategyBoxLabel.Size = new System.Drawing.Size(77, 22);
            this.StrategyBoxLabel.TabIndex = 13;
            this.StrategyBoxLabel.Text = "Strategy";
            // 
            // KeySepBoxLabel
            // 
            this.KeySepBoxLabel.AutoSize = true;
            this.KeySepBoxLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.KeySepBoxLabel.Location = new System.Drawing.Point(138, 74);
            this.KeySepBoxLabel.Name = "KeySepBoxLabel";
            this.KeySepBoxLabel.Size = new System.Drawing.Size(125, 22);
            this.KeySepBoxLabel.TabIndex = 9;
            this.KeySepBoxLabel.Text = "Key Separator";
            // 
            // DelimBoxLabel
            // 
            this.DelimBoxLabel.AutoSize = true;
            this.DelimBoxLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DelimBoxLabel.Location = new System.Drawing.Point(138, 131);
            this.DelimBoxLabel.Name = "DelimBoxLabel";
            this.DelimBoxLabel.Size = new System.Drawing.Size(182, 22);
            this.DelimBoxLabel.TabIndex = 10;
            this.DelimBoxLabel.Text = "Delimiter in output file";
            // 
            // GenerateCSVButton
            // 
            this.GenerateCSVButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GenerateCSVButton.Location = new System.Drawing.Point(34, 407);
            this.GenerateCSVButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.GenerateCSVButton.Name = "GenerateCSVButton";
            this.GenerateCSVButton.Size = new System.Drawing.Size(143, 34);
            this.GenerateCSVButton.TabIndex = 6;
            this.GenerateCSVButton.Text = "Generate CSV";
            this.GenerateCSVButton.UseVisualStyleBackColor = true;
            this.GenerateCSVButton.Click += new System.EventHandler(this.GenerateCSVButton_Click);
            this.GenerateCSVButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.JsonToCsvForm_KeyUp);
            // 
            // DocsButton
            // 
            this.DocsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DocsButton.Location = new System.Drawing.Point(249, 407);
            this.DocsButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.DocsButton.Name = "DocsButton";
            this.DocsButton.Size = new System.Drawing.Size(83, 34);
            this.DocsButton.TabIndex = 7;
            this.DocsButton.Text = "Docs";
            this.DocsButton.UseVisualStyleBackColor = true;
            this.DocsButton.Click += new System.EventHandler(this.DocsButton_Click);
            this.DocsButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.JsonToCsvForm_KeyUp);
            // 
            // AcceptCancelButton
            // 
            this.AcceptCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.AcceptCancelButton.Location = new System.Drawing.Point(260, 241);
            this.AcceptCancelButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.AcceptCancelButton.Name = "AcceptCancelButton";
            this.AcceptCancelButton.Size = new System.Drawing.Size(126, 29);
            this.AcceptCancelButton.TabIndex = 12;
            this.AcceptCancelButton.TabStop = false;
            this.AcceptCancelButton.Text = "AcceptCancelButton";
            this.AcceptCancelButton.UseVisualStyleBackColor = true;
            this.AcceptCancelButton.Visible = false;
            // 
            // eolComboBoxLabel
            // 
            this.eolComboBoxLabel.AutoSize = true;
            this.eolComboBoxLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.eolComboBoxLabel.Location = new System.Drawing.Point(138, 186);
            this.eolComboBoxLabel.Name = "eolComboBoxLabel";
            this.eolComboBoxLabel.Size = new System.Drawing.Size(231, 22);
            this.eolComboBoxLabel.TabIndex = 11;
            this.eolComboBoxLabel.Text = "Line terminator in output file";
            // 
            // eolComboBox
            // 
            this.eolComboBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.eolComboBox.FormattingEnabled = true;
            this.eolComboBox.Items.AddRange(new object[] {
            "CRLF",
            "CR",
            "LF"});
            this.eolComboBox.Location = new System.Drawing.Point(34, 182);
            this.eolComboBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.eolComboBox.Name = "eolComboBox";
            this.eolComboBox.Size = new System.Drawing.Size(84, 30);
            this.eolComboBox.TabIndex = 2;
            this.eolComboBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.JsonToCsvForm_KeyUp);
            // 
            // SortColNamesCheckBox
            // 
            this.SortColNamesCheckBox.AutoSize = true;
            this.SortColNamesCheckBox.Checked = true;
            this.SortColNamesCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.SortColNamesCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SortColNamesCheckBox.Location = new System.Drawing.Point(34, 292);
            this.SortColNamesCheckBox.Name = "SortColNamesCheckBox";
            this.SortColNamesCheckBox.Size = new System.Drawing.Size(303, 26);
            this.SortColNamesCheckBox.TabIndex = 4;
            this.SortColNamesCheckBox.Text = "Sort column names alphabetically";
            this.SortColNamesCheckBox.UseVisualStyleBackColor = true;
            // 
            // JsonToCsvForm
            // 
            this.AcceptButton = this.AcceptCancelButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.AcceptCancelButton;
            this.ClientSize = new System.Drawing.Size(399, 468);
            this.Controls.Add(this.KeySepBox);
            this.Controls.Add(this.DelimBox);
            this.Controls.Add(this.eolComboBox);
            this.Controls.Add(this.BoolsToIntsCheckBox);
            this.Controls.Add(this.SortColNamesCheckBox);
            this.Controls.Add(this.StrategyBox);
            this.Controls.Add(this.GenerateCSVButton);
            this.Controls.Add(this.DocsButton);
            this.Controls.Add(this.JsonToCsvFormTitle);
            this.Controls.Add(this.KeySepBoxLabel);
            this.Controls.Add(this.DelimBoxLabel);
            this.Controls.Add(this.eolComboBoxLabel);
            this.Controls.Add(this.AcceptCancelButton);
            this.Controls.Add(this.StrategyBoxLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "JsonToCsvForm";
            this.Text = "JSON to CSV";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox KeySepBox;
        private System.Windows.Forms.Label JsonToCsvFormTitle;
        private System.Windows.Forms.ComboBox DelimBox;
        private System.Windows.Forms.CheckBox BoolsToIntsCheckBox;
        private System.Windows.Forms.ComboBox StrategyBox;
        private System.Windows.Forms.Label StrategyBoxLabel;
        private System.Windows.Forms.Label KeySepBoxLabel;
        private System.Windows.Forms.Label DelimBoxLabel;
        private System.Windows.Forms.Button GenerateCSVButton;
        private System.Windows.Forms.Button DocsButton;
        private System.Windows.Forms.Button AcceptCancelButton;
        private System.Windows.Forms.Label eolComboBoxLabel;
        private System.Windows.Forms.ComboBox eolComboBox;
        private System.Windows.Forms.CheckBox SortColNamesCheckBox;
    }
}