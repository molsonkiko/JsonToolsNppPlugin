namespace JSON_Tools.Forms
{
    partial class FindReplaceForm
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
            treeViewer = null;
            if (disposing && (components != null))
            {
                NppFormHelper.UnregisterFormIfModeless(this, false);
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindReplaceForm));
            this.KeysValsBothBox = new System.Windows.Forms.ComboBox();
            this.RecursiveSearchBox = new System.Windows.Forms.CheckBox();
            this.FindReplaceFormTitle = new System.Windows.Forms.Label();
            this.FindButton = new System.Windows.Forms.Button();
            this.ReplaceButton = new System.Windows.Forms.Button();
            this.FindTextBox = new System.Windows.Forms.TextBox();
            this.FindTextBoxLabel = new System.Windows.Forms.Label();
            this.RegexBox = new System.Windows.Forms.CheckBox();
            this.MathBox = new System.Windows.Forms.CheckBox();
            this.ReplaceTextBox = new System.Windows.Forms.TextBox();
            this.ReplaceTextBoxLabel = new System.Windows.Forms.Label();
            this.KeysValsBothBoxLabel = new System.Windows.Forms.Label();
            this.RootTextBox = new System.Windows.Forms.TextBox();
            this.RootTextBoxLabel = new System.Windows.Forms.Label();
            this.AdvancedGroupBox = new System.Windows.Forms.GroupBox();
            this.MatchExactlyBox = new System.Windows.Forms.CheckBox();
            this.IgnoreCaseCheckBox = new System.Windows.Forms.CheckBox();
            this.SwapFindReplaceButton = new System.Windows.Forms.Button();
            this.ShowAdvancedOptionsCheckBox = new System.Windows.Forms.CheckBox();
            this.AdvancedGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // KeysValsBothBox
            // 
            this.KeysValsBothBox.FormattingEnabled = true;
            this.KeysValsBothBox.Items.AddRange(new object[] {
            "Keys",
            "Values",
            "Keys and values"});
            this.KeysValsBothBox.Location = new System.Drawing.Point(8, 21);
            this.KeysValsBothBox.Name = "KeysValsBothBox";
            this.KeysValsBothBox.Size = new System.Drawing.Size(133, 24);
            this.KeysValsBothBox.TabIndex = 5;
            this.KeysValsBothBox.Visible = false;
            this.KeysValsBothBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyDown);
            this.KeysValsBothBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.KeysValsBothBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // RecursiveSearchBox
            // 
            this.RecursiveSearchBox.AutoSize = true;
            this.RecursiveSearchBox.Checked = true;
            this.RecursiveSearchBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.RecursiveSearchBox.Location = new System.Drawing.Point(8, 99);
            this.RecursiveSearchBox.Name = "RecursiveSearchBox";
            this.RecursiveSearchBox.Size = new System.Drawing.Size(141, 20);
            this.RecursiveSearchBox.TabIndex = 8;
            this.RecursiveSearchBox.Text = "Recursive &search?";
            this.RecursiveSearchBox.UseVisualStyleBackColor = true;
            this.RecursiveSearchBox.Visible = false;
            this.RecursiveSearchBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // FindReplaceFormTitle
            // 
            this.FindReplaceFormTitle.AutoSize = true;
            this.FindReplaceFormTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FindReplaceFormTitle.Location = new System.Drawing.Point(83, 9);
            this.FindReplaceFormTitle.Name = "FindReplaceFormTitle";
            this.FindReplaceFormTitle.Size = new System.Drawing.Size(201, 22);
            this.FindReplaceFormTitle.TabIndex = 13;
            this.FindReplaceFormTitle.Text = "Find/replace in JSON";
            // 
            // FindButton
            // 
            this.FindButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.FindButton.Location = new System.Drawing.Point(87, 359);
            this.FindButton.Name = "FindButton";
            this.FindButton.Size = new System.Drawing.Size(86, 23);
            this.FindButton.TabIndex = 11;
            this.FindButton.Text = "Find all";
            this.FindButton.UseVisualStyleBackColor = true;
            this.FindButton.Click += new System.EventHandler(this.FindButton_Click);
            this.FindButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // ReplaceButton
            // 
            this.ReplaceButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ReplaceButton.Location = new System.Drawing.Point(187, 359);
            this.ReplaceButton.Name = "ReplaceButton";
            this.ReplaceButton.Size = new System.Drawing.Size(97, 23);
            this.ReplaceButton.TabIndex = 12;
            this.ReplaceButton.Text = "Replace all";
            this.ReplaceButton.UseVisualStyleBackColor = true;
            this.ReplaceButton.Click += new System.EventHandler(this.ReplaceButton_Click);
            this.ReplaceButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // FindTextBox
            // 
            this.FindTextBox.Location = new System.Drawing.Point(31, 52);
            this.FindTextBox.Name = "FindTextBox";
            this.FindTextBox.Size = new System.Drawing.Size(196, 22);
            this.FindTextBox.TabIndex = 0;
            this.FindTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyDown);
            this.FindTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.FindTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // FindTextBoxLabel
            // 
            this.FindTextBoxLabel.AutoSize = true;
            this.FindTextBoxLabel.Location = new System.Drawing.Point(242, 55);
            this.FindTextBoxLabel.Name = "FindTextBoxLabel";
            this.FindTextBoxLabel.Size = new System.Drawing.Size(42, 16);
            this.FindTextBoxLabel.TabIndex = 14;
            this.FindTextBoxLabel.Text = "Find...";
            // 
            // RegexBox
            // 
            this.RegexBox.AutoSize = true;
            this.RegexBox.Checked = true;
            this.RegexBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.RegexBox.Location = new System.Drawing.Point(8, 59);
            this.RegexBox.Name = "RegexBox";
            this.RegexBox.Size = new System.Drawing.Size(182, 20);
            this.RegexBox.TabIndex = 6;
            this.RegexBox.Text = "Use re&gular expressions?";
            this.RegexBox.UseVisualStyleBackColor = true;
            this.RegexBox.Visible = false;
            this.RegexBox.CheckedChanged += new System.EventHandler(this.RegexBox_CheckedChanged);
            this.RegexBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // MathBox
            // 
            this.MathBox.AutoSize = true;
            this.MathBox.Location = new System.Drawing.Point(201, 59);
            this.MathBox.Name = "MathBox";
            this.MathBox.Size = new System.Drawing.Size(127, 20);
            this.MathBox.TabIndex = 7;
            this.MathBox.Text = "&Math expression";
            this.MathBox.UseVisualStyleBackColor = true;
            this.MathBox.Visible = false;
            this.MathBox.CheckedChanged += new System.EventHandler(this.MathBox_CheckedChanged);
            this.MathBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // ReplaceTextBox
            // 
            this.ReplaceTextBox.Location = new System.Drawing.Point(31, 91);
            this.ReplaceTextBox.Name = "ReplaceTextBox";
            this.ReplaceTextBox.Size = new System.Drawing.Size(196, 22);
            this.ReplaceTextBox.TabIndex = 2;
            this.ReplaceTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyDown);
            this.ReplaceTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.ReplaceTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // ReplaceTextBoxLabel
            // 
            this.ReplaceTextBoxLabel.AutoSize = true;
            this.ReplaceTextBoxLabel.Location = new System.Drawing.Point(242, 94);
            this.ReplaceTextBoxLabel.Name = "ReplaceTextBoxLabel";
            this.ReplaceTextBoxLabel.Size = new System.Drawing.Size(93, 16);
            this.ReplaceTextBoxLabel.TabIndex = 15;
            this.ReplaceTextBoxLabel.Text = "Replace with...";
            // 
            // KeysValsBothBoxLabel
            // 
            this.KeysValsBothBoxLabel.AutoSize = true;
            this.KeysValsBothBoxLabel.Location = new System.Drawing.Point(152, 24);
            this.KeysValsBothBoxLabel.Name = "KeysValsBothBoxLabel";
            this.KeysValsBothBoxLabel.Size = new System.Drawing.Size(160, 16);
            this.KeysValsBothBoxLabel.TabIndex = 18;
            this.KeysValsBothBoxLabel.Text = "Search in keys or values?";
            this.KeysValsBothBoxLabel.Visible = false;
            // 
            // RootTextBox
            // 
            this.RootTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.RootTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.HistoryList;
            this.RootTextBox.Location = new System.Drawing.Point(31, 130);
            this.RootTextBox.Name = "RootTextBox";
            this.RootTextBox.Size = new System.Drawing.Size(196, 22);
            this.RootTextBox.TabIndex = 3;
            this.RootTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyDown);
            this.RootTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.RootTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // RootTextBoxLabel
            // 
            this.RootTextBoxLabel.AutoSize = true;
            this.RootTextBoxLabel.Location = new System.Drawing.Point(242, 133);
            this.RootTextBoxLabel.Name = "RootTextBoxLabel";
            this.RootTextBoxLabel.Size = new System.Drawing.Size(36, 16);
            this.RootTextBoxLabel.TabIndex = 16;
            this.RootTextBoxLabel.Text = "Root";
            // 
            // AdvancedGroupBox
            // 
            this.AdvancedGroupBox.Controls.Add(this.KeysValsBothBox);
            this.AdvancedGroupBox.Controls.Add(this.KeysValsBothBoxLabel);
            this.AdvancedGroupBox.Controls.Add(this.RegexBox);
            this.AdvancedGroupBox.Controls.Add(this.MathBox);
            this.AdvancedGroupBox.Controls.Add(this.RecursiveSearchBox);
            this.AdvancedGroupBox.Controls.Add(this.IgnoreCaseCheckBox);
            this.AdvancedGroupBox.Controls.Add(this.MatchExactlyBox);
            this.AdvancedGroupBox.Location = new System.Drawing.Point(24, 180);
            this.AdvancedGroupBox.Name = "AdvancedGroupBox";
            this.AdvancedGroupBox.Size = new System.Drawing.Size(337, 163);
            this.AdvancedGroupBox.TabIndex = 4;
            this.AdvancedGroupBox.TabStop = false;
            // 
            // MatchExactlyBox
            // 
            this.MatchExactlyBox.AutoSize = true;
            this.MatchExactlyBox.Enabled = false;
            this.MatchExactlyBox.Location = new System.Drawing.Point(96, 130);
            this.MatchExactlyBox.Name = "MatchExactlyBox";
            this.MatchExactlyBox.Size = new System.Drawing.Size(117, 20);
            this.MatchExactlyBox.TabIndex = 10;
            this.MatchExactlyBox.Text = "Match &exactly?";
            this.MatchExactlyBox.UseVisualStyleBackColor = true;
            this.MatchExactlyBox.CheckedChanged += new System.EventHandler(this.MatchExactlyBox_CheckedChanged);
            this.MatchExactlyBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // IgnoreCaseCheckBox
            // 
            this.IgnoreCaseCheckBox.AutoSize = true;
            this.IgnoreCaseCheckBox.Location = new System.Drawing.Point(201, 99);
            this.IgnoreCaseCheckBox.Name = "IgnoreCaseCheckBox";
            this.IgnoreCaseCheckBox.Size = new System.Drawing.Size(107, 20);
            this.IgnoreCaseCheckBox.TabIndex = 9;
            this.IgnoreCaseCheckBox.Text = "Ignore &case?";
            this.IgnoreCaseCheckBox.UseVisualStyleBackColor = true;
            this.IgnoreCaseCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // SwapFindReplaceButton
            // 
            this.SwapFindReplaceButton.Location = new System.Drawing.Point(303, 60);
            this.SwapFindReplaceButton.Name = "SwapFindReplaceButton";
            this.SwapFindReplaceButton.Size = new System.Drawing.Size(49, 28);
            this.SwapFindReplaceButton.TabIndex = 1;
            this.SwapFindReplaceButton.Text = "Swap";
            this.SwapFindReplaceButton.UseVisualStyleBackColor = true;
            this.SwapFindReplaceButton.Click += new System.EventHandler(this.SwapFindReplaceButton_Click);
            this.SwapFindReplaceButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // ShowAdvancedOptionsCheckBox
            // 
            this.ShowAdvancedOptionsCheckBox.AutoSize = true;
            this.ShowAdvancedOptionsCheckBox.Location = new System.Drawing.Point(32, 170);
            this.ShowAdvancedOptionsCheckBox.Name = "ShowAdvancedOptionsCheckBox";
            this.ShowAdvancedOptionsCheckBox.Size = new System.Drawing.Size(173, 20);
            this.ShowAdvancedOptionsCheckBox.TabIndex = 4;
            this.ShowAdvancedOptionsCheckBox.Text = "Show advanced options";
            this.ShowAdvancedOptionsCheckBox.UseVisualStyleBackColor = true;
            this.ShowAdvancedOptionsCheckBox.CheckedChanged += new System.EventHandler(this.ShowAdvancedOptionsCheckBox_CheckedChanged);
            this.ShowAdvancedOptionsCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            // 
            // FindReplaceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(373, 394);
            this.Controls.Add(this.FindTextBoxLabel);
            this.Controls.Add(this.FindTextBox);
            this.Controls.Add(this.SwapFindReplaceButton);
            this.Controls.Add(this.ReplaceTextBox);
            this.Controls.Add(this.ReplaceTextBoxLabel);
            this.Controls.Add(this.RootTextBoxLabel);
            this.Controls.Add(this.RootTextBox);
            this.Controls.Add(this.ShowAdvancedOptionsCheckBox);
            this.Controls.Add(this.AdvancedGroupBox);
            this.Controls.Add(this.FindButton);
            this.Controls.Add(this.ReplaceButton);
            this.Controls.Add(this.FindReplaceFormTitle);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FindReplaceForm";
            this.Text = "Find/replace in JSON";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.FindReplaceForm_KeyUp);
            this.AdvancedGroupBox.ResumeLayout(false);
            this.AdvancedGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox KeysValsBothBox;
        private System.Windows.Forms.CheckBox RecursiveSearchBox;
        private System.Windows.Forms.Label FindReplaceFormTitle;
        private System.Windows.Forms.Button FindButton;
        private System.Windows.Forms.Button ReplaceButton;
        private System.Windows.Forms.TextBox FindTextBox;
        private System.Windows.Forms.Label FindTextBoxLabel;
        private System.Windows.Forms.CheckBox RegexBox;
        private System.Windows.Forms.CheckBox MathBox;
        private System.Windows.Forms.TextBox ReplaceTextBox;
        private System.Windows.Forms.Label ReplaceTextBoxLabel;
        private System.Windows.Forms.Label KeysValsBothBoxLabel;
        private System.Windows.Forms.TextBox RootTextBox;
        private System.Windows.Forms.Label RootTextBoxLabel;
        private System.Windows.Forms.GroupBox AdvancedGroupBox;
        private System.Windows.Forms.Button SwapFindReplaceButton;
        private System.Windows.Forms.CheckBox IgnoreCaseCheckBox;
        private System.Windows.Forms.CheckBox MatchExactlyBox;
        private System.Windows.Forms.CheckBox ShowAdvancedOptionsCheckBox;
    }
}