namespace JSON_Tools.Forms
{
    partial class RegexSearchForm
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
                NppFormHelper.RegisterFormIfModeless(this, false);
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegexSearchForm));
            this.RegexTextBox = new System.Windows.Forms.TextBox();
            this.IgnoreCaseCheckBox = new System.Windows.Forms.CheckBox();
            this.SearchButton = new System.Windows.Forms.Button();
            this.RegexTextBoxLabel = new System.Windows.Forms.Label();
            this.Title = new System.Windows.Forms.Label();
            this.ColumnsToParseAsNumberTextBox = new System.Windows.Forms.TextBox();
            this.ColumnsToParseAsNumberTextBoxLabel = new System.Windows.Forms.Label();
            this.ParseAsCsvCheckBox = new System.Windows.Forms.CheckBox();
            this.DelimiterTextBox = new System.Windows.Forms.TextBox();
            this.DelimiterTextBoxLabel = new System.Windows.Forms.Label();
            this.QuoteCharTextBox = new System.Windows.Forms.TextBox();
            this.QuoteCharTextBoxLabel = new System.Windows.Forms.Label();
            this.NewlineComboBox = new System.Windows.Forms.ComboBox();
            this.NewlineComboBoxLabel = new System.Windows.Forms.Label();
            this.HeaderHandlingComboBox = new System.Windows.Forms.ComboBox();
            this.HeaderHandlingComboBoxLabel = new System.Windows.Forms.Label();
            this.IncludeFullMatchAsFirstItemCheckBox = new System.Windows.Forms.CheckBox();
            this.NColumnsTextBoxLabel = new System.Windows.Forms.Label();
            this.NColumnsTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // RegexTextBox
            // 
            this.RegexTextBox.Location = new System.Drawing.Point(12, 52);
            this.RegexTextBox.Name = "RegexTextBox";
            this.RegexTextBox.Size = new System.Drawing.Size(655, 22);
            this.RegexTextBox.TabIndex = 0;
            this.RegexTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyDown);
            this.RegexTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.RegexTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // IgnoreCaseCheckBox
            // 
            this.IgnoreCaseCheckBox.AutoSize = true;
            this.IgnoreCaseCheckBox.Checked = true;
            this.IgnoreCaseCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.IgnoreCaseCheckBox.Location = new System.Drawing.Point(12, 90);
            this.IgnoreCaseCheckBox.Name = "IgnoreCaseCheckBox";
            this.IgnoreCaseCheckBox.Size = new System.Drawing.Size(107, 20);
            this.IgnoreCaseCheckBox.TabIndex = 1;
            this.IgnoreCaseCheckBox.Text = "Ignore case?";
            this.IgnoreCaseCheckBox.UseVisualStyleBackColor = true;
            this.IgnoreCaseCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // SearchButton
            // 
            this.SearchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SearchButton.Location = new System.Drawing.Point(354, 245);
            this.SearchButton.Name = "SearchButton";
            this.SearchButton.Size = new System.Drawing.Size(63, 23);
            this.SearchButton.TabIndex = 16;
            this.SearchButton.Text = "Search";
            this.SearchButton.UseVisualStyleBackColor = true;
            this.SearchButton.Click += new System.EventHandler(this.SearchButton_Click);
            this.SearchButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // RegexTextBoxLabel
            // 
            this.RegexTextBoxLabel.AutoSize = true;
            this.RegexTextBoxLabel.Location = new System.Drawing.Point(673, 55);
            this.RegexTextBoxLabel.Name = "RegexTextBoxLabel";
            this.RegexTextBoxLabel.Size = new System.Drawing.Size(86, 16);
            this.RegexTextBoxLabel.TabIndex = 17;
            this.RegexTextBoxLabel.Text = "Enter a regex";
            // 
            // Title
            // 
            this.Title.AutoSize = true;
            this.Title.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Title.Location = new System.Drawing.Point(264, 9);
            this.Title.Name = "Title";
            this.Title.Size = new System.Drawing.Size(217, 22);
            this.Title.TabIndex = 18;
            this.Title.Text = "Regex Search to JSON";
            // 
            // ColumnsToParseAsNumberTextBox
            // 
            this.ColumnsToParseAsNumberTextBox.Location = new System.Drawing.Point(12, 210);
            this.ColumnsToParseAsNumberTextBox.Name = "ColumnsToParseAsNumberTextBox";
            this.ColumnsToParseAsNumberTextBox.Size = new System.Drawing.Size(257, 22);
            this.ColumnsToParseAsNumberTextBox.TabIndex = 14;
            this.ColumnsToParseAsNumberTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyDown);
            this.ColumnsToParseAsNumberTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.ColumnsToParseAsNumberTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // ColumnsToParseAsNumberTextBoxLabel
            // 
            this.ColumnsToParseAsNumberTextBoxLabel.AutoSize = true;
            this.ColumnsToParseAsNumberTextBoxLabel.Location = new System.Drawing.Point(271, 213);
            this.ColumnsToParseAsNumberTextBoxLabel.Name = "ColumnsToParseAsNumberTextBoxLabel";
            this.ColumnsToParseAsNumberTextBoxLabel.Size = new System.Drawing.Size(227, 16);
            this.ColumnsToParseAsNumberTextBoxLabel.TabIndex = 15;
            this.ColumnsToParseAsNumberTextBoxLabel.Text = "Groups to parse as number (int array)";
            // 
            // ParseAsCsvCheckBox
            // 
            this.ParseAsCsvCheckBox.AutoSize = true;
            this.ParseAsCsvCheckBox.Location = new System.Drawing.Point(12, 127);
            this.ParseAsCsvCheckBox.Name = "ParseAsCsvCheckBox";
            this.ParseAsCsvCheckBox.Size = new System.Drawing.Size(120, 20);
            this.ParseAsCsvCheckBox.TabIndex = 3;
            this.ParseAsCsvCheckBox.Text = "Parse as CSV?";
            this.ParseAsCsvCheckBox.UseVisualStyleBackColor = true;
            this.ParseAsCsvCheckBox.CheckedChanged += new System.EventHandler(this.ParseAsCsvCheckBox_CheckedChanged);
            this.ParseAsCsvCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // DelimiterTextBox
            // 
            this.DelimiterTextBox.Location = new System.Drawing.Point(205, 127);
            this.DelimiterTextBox.Name = "DelimiterTextBox";
            this.DelimiterTextBox.Size = new System.Drawing.Size(22, 22);
            this.DelimiterTextBox.TabIndex = 4;
            this.DelimiterTextBox.Text = ",";
            this.DelimiterTextBox.Visible = false;
            this.DelimiterTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyDown);
            this.DelimiterTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.DelimiterTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // DelimiterTextBoxLabel
            // 
            this.DelimiterTextBoxLabel.AutoSize = true;
            this.DelimiterTextBoxLabel.Location = new System.Drawing.Point(233, 131);
            this.DelimiterTextBoxLabel.Name = "DelimiterTextBoxLabel";
            this.DelimiterTextBoxLabel.Size = new System.Drawing.Size(60, 16);
            this.DelimiterTextBoxLabel.TabIndex = 5;
            this.DelimiterTextBoxLabel.Text = "Delimiter";
            this.DelimiterTextBoxLabel.Visible = false;
            // 
            // QuoteCharTextBox
            // 
            this.QuoteCharTextBox.Location = new System.Drawing.Point(304, 127);
            this.QuoteCharTextBox.Name = "QuoteCharTextBox";
            this.QuoteCharTextBox.Size = new System.Drawing.Size(22, 22);
            this.QuoteCharTextBox.TabIndex = 6;
            this.QuoteCharTextBox.Text = "\"";
            this.QuoteCharTextBox.Visible = false;
            this.QuoteCharTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyDown);
            this.QuoteCharTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.QuoteCharTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // QuoteCharTextBoxLabel
            // 
            this.QuoteCharTextBoxLabel.AutoSize = true;
            this.QuoteCharTextBoxLabel.Location = new System.Drawing.Point(332, 131);
            this.QuoteCharTextBoxLabel.Name = "QuoteCharTextBoxLabel";
            this.QuoteCharTextBoxLabel.Size = new System.Drawing.Size(102, 16);
            this.QuoteCharTextBoxLabel.TabIndex = 7;
            this.QuoteCharTextBoxLabel.Text = "Quote character";
            this.QuoteCharTextBoxLabel.Visible = false;
            // 
            // NewlineComboBox
            // 
            this.NewlineComboBox.FormattingEnabled = true;
            this.NewlineComboBox.Items.AddRange(new object[] {
            "CR LF",
            "LF",
            "CR"});
            this.NewlineComboBox.Location = new System.Drawing.Point(444, 127);
            this.NewlineComboBox.Name = "NewlineComboBox";
            this.NewlineComboBox.Size = new System.Drawing.Size(68, 24);
            this.NewlineComboBox.TabIndex = 8;
            this.NewlineComboBox.Visible = false;
            this.NewlineComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyDown);
            this.NewlineComboBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.NewlineComboBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // NewlineComboBoxLabel
            // 
            this.NewlineComboBoxLabel.AutoSize = true;
            this.NewlineComboBoxLabel.Location = new System.Drawing.Point(518, 131);
            this.NewlineComboBoxLabel.Name = "NewlineComboBoxLabel";
            this.NewlineComboBoxLabel.Size = new System.Drawing.Size(55, 16);
            this.NewlineComboBoxLabel.TabIndex = 9;
            this.NewlineComboBoxLabel.Text = "Newline";
            this.NewlineComboBoxLabel.Visible = false;
            // 
            // HeaderHandlingComboBox
            // 
            this.HeaderHandlingComboBox.FormattingEnabled = true;
            this.HeaderHandlingComboBox.Items.AddRange(new object[] {
            "Skip header",
            "Include header",
            "Use header as keys"});
            this.HeaderHandlingComboBox.Location = new System.Drawing.Point(304, 169);
            this.HeaderHandlingComboBox.Name = "HeaderHandlingComboBox";
            this.HeaderHandlingComboBox.Size = new System.Drawing.Size(155, 24);
            this.HeaderHandlingComboBox.TabIndex = 12;
            this.HeaderHandlingComboBox.Visible = false;
            this.HeaderHandlingComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyDown);
            this.HeaderHandlingComboBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.HeaderHandlingComboBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // HeaderHandlingComboBoxLabel
            // 
            this.HeaderHandlingComboBoxLabel.AutoSize = true;
            this.HeaderHandlingComboBoxLabel.Location = new System.Drawing.Point(465, 172);
            this.HeaderHandlingComboBoxLabel.Name = "HeaderHandlingComboBoxLabel";
            this.HeaderHandlingComboBoxLabel.Size = new System.Drawing.Size(107, 16);
            this.HeaderHandlingComboBoxLabel.TabIndex = 13;
            this.HeaderHandlingComboBoxLabel.Text = "Header handling";
            this.HeaderHandlingComboBoxLabel.Visible = false;
            // 
            // IncludeFullMatchAsFirstItemCheckBox
            // 
            this.IncludeFullMatchAsFirstItemCheckBox.AutoSize = true;
            this.IncludeFullMatchAsFirstItemCheckBox.Location = new System.Drawing.Point(138, 90);
            this.IncludeFullMatchAsFirstItemCheckBox.Name = "IncludeFullMatchAsFirstItemCheckBox";
            this.IncludeFullMatchAsFirstItemCheckBox.Size = new System.Drawing.Size(229, 20);
            this.IncludeFullMatchAsFirstItemCheckBox.TabIndex = 2;
            this.IncludeFullMatchAsFirstItemCheckBox.Text = "Include full match text as first item?";
            this.IncludeFullMatchAsFirstItemCheckBox.UseVisualStyleBackColor = true;
            this.IncludeFullMatchAsFirstItemCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // NColumnsTextBoxLabel
            // 
            this.NColumnsTextBoxLabel.AutoSize = true;
            this.NColumnsTextBoxLabel.Location = new System.Drawing.Point(171, 172);
            this.NColumnsTextBoxLabel.Name = "NColumnsTextBoxLabel";
            this.NColumnsTextBoxLabel.Size = new System.Drawing.Size(122, 16);
            this.NColumnsTextBoxLabel.TabIndex = 11;
            this.NColumnsTextBoxLabel.Text = "Number of columns";
            this.NColumnsTextBoxLabel.Visible = false;
            // 
            // NColumnsTextBox
            // 
            this.NColumnsTextBox.Location = new System.Drawing.Point(108, 169);
            this.NColumnsTextBox.Name = "NColumnsTextBox";
            this.NColumnsTextBox.Size = new System.Drawing.Size(57, 22);
            this.NColumnsTextBox.TabIndex = 10;
            this.NColumnsTextBox.Visible = false;
            this.NColumnsTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyDown);
            this.NColumnsTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.NColumnsTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.RegexSearchForm_KeyUp);
            // 
            // RegexSearchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(771, 280);
            this.Controls.Add(this.RegexTextBox);
            this.Controls.Add(this.IgnoreCaseCheckBox);
            this.Controls.Add(this.IncludeFullMatchAsFirstItemCheckBox);
            this.Controls.Add(this.ParseAsCsvCheckBox);
            this.Controls.Add(this.DelimiterTextBoxLabel);
            this.Controls.Add(this.DelimiterTextBox);
            this.Controls.Add(this.QuoteCharTextBoxLabel);
            this.Controls.Add(this.QuoteCharTextBox);
            this.Controls.Add(this.NewlineComboBoxLabel);
            this.Controls.Add(this.NewlineComboBox);
            this.Controls.Add(this.NColumnsTextBoxLabel);
            this.Controls.Add(this.NColumnsTextBox);
            this.Controls.Add(this.HeaderHandlingComboBoxLabel);
            this.Controls.Add(this.HeaderHandlingComboBox);
            this.Controls.Add(this.ColumnsToParseAsNumberTextBoxLabel);
            this.Controls.Add(this.ColumnsToParseAsNumberTextBox);
            this.Controls.Add(this.SearchButton);
            this.Controls.Add(this.Title);
            this.Controls.Add(this.RegexTextBoxLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "RegexSearchForm";
            this.Text = "Regex search to JSON";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox RegexTextBox;
        private System.Windows.Forms.CheckBox IgnoreCaseCheckBox;
        private System.Windows.Forms.Label RegexTextBoxLabel;
        private System.Windows.Forms.Label Title;
        private System.Windows.Forms.TextBox ColumnsToParseAsNumberTextBox;
        private System.Windows.Forms.Label ColumnsToParseAsNumberTextBoxLabel;
        private System.Windows.Forms.CheckBox ParseAsCsvCheckBox;
        private System.Windows.Forms.TextBox DelimiterTextBox;
        private System.Windows.Forms.Label DelimiterTextBoxLabel;
        private System.Windows.Forms.TextBox QuoteCharTextBox;
        private System.Windows.Forms.Label QuoteCharTextBoxLabel;
        private System.Windows.Forms.ComboBox NewlineComboBox;
        private System.Windows.Forms.Label NewlineComboBoxLabel;
        private System.Windows.Forms.ComboBox HeaderHandlingComboBox;
        private System.Windows.Forms.Label HeaderHandlingComboBoxLabel;
        private System.Windows.Forms.CheckBox IncludeFullMatchAsFirstItemCheckBox;
        private System.Windows.Forms.Label NColumnsTextBoxLabel;
        private System.Windows.Forms.TextBox NColumnsTextBox;
        internal System.Windows.Forms.Button SearchButton;
    }
}