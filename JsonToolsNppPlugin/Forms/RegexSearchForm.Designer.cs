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
            this.NumGroupsTextBox = new System.Windows.Forms.TextBox();
            this.NumGroupsTextBoxLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // RegexTextBox
            // 
            this.RegexTextBox.Location = new System.Drawing.Point(12, 52);
            this.RegexTextBox.Name = "RegexTextBox";
            this.RegexTextBox.Size = new System.Drawing.Size(394, 22);
            this.RegexTextBox.TabIndex = 0;
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
            // 
            // SearchButton
            // 
            this.SearchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SearchButton.Location = new System.Drawing.Point(226, 162);
            this.SearchButton.Name = "SearchButton";
            this.SearchButton.Size = new System.Drawing.Size(63, 23);
            this.SearchButton.TabIndex = 2;
            this.SearchButton.Text = "Search";
            this.SearchButton.UseVisualStyleBackColor = true;
            this.SearchButton.Click += new System.EventHandler(this.SearchButton_Click);
            // 
            // RegexTextBoxLabel
            // 
            this.RegexTextBoxLabel.AutoSize = true;
            this.RegexTextBoxLabel.Location = new System.Drawing.Point(412, 55);
            this.RegexTextBoxLabel.Name = "RegexTextBoxLabel";
            this.RegexTextBoxLabel.Size = new System.Drawing.Size(86, 16);
            this.RegexTextBoxLabel.TabIndex = 3;
            this.RegexTextBoxLabel.Text = "Enter a regex";
            // 
            // Title
            // 
            this.Title.AutoSize = true;
            this.Title.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Title.Location = new System.Drawing.Point(139, 9);
            this.Title.Name = "Title";
            this.Title.Size = new System.Drawing.Size(217, 22);
            this.Title.TabIndex = 4;
            this.Title.Text = "Regex Search to JSON";
            // 
            // NumGroupsTextBox
            // 
            this.NumGroupsTextBox.Location = new System.Drawing.Point(12, 127);
            this.NumGroupsTextBox.Name = "NumGroupsTextBox";
            this.NumGroupsTextBox.Size = new System.Drawing.Size(257, 22);
            this.NumGroupsTextBox.TabIndex = 5;
            // 
            // NumGroupsTextBoxLabel
            // 
            this.NumGroupsTextBoxLabel.AutoSize = true;
            this.NumGroupsTextBoxLabel.Location = new System.Drawing.Point(271, 130);
            this.NumGroupsTextBoxLabel.Name = "NumGroupsTextBoxLabel";
            this.NumGroupsTextBoxLabel.Size = new System.Drawing.Size(227, 16);
            this.NumGroupsTextBoxLabel.TabIndex = 6;
            this.NumGroupsTextBoxLabel.Text = "Groups to parse as number (int array)";
            // 
            // RegexSearchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(510, 197);
            this.Controls.Add(this.NumGroupsTextBoxLabel);
            this.Controls.Add(this.NumGroupsTextBox);
            this.Controls.Add(this.Title);
            this.Controls.Add(this.RegexTextBoxLabel);
            this.Controls.Add(this.SearchButton);
            this.Controls.Add(this.IgnoreCaseCheckBox);
            this.Controls.Add(this.RegexTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "RegexSearchForm";
            this.Text = "Regex search to JSON";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox RegexTextBox;
        private System.Windows.Forms.CheckBox IgnoreCaseCheckBox;
        private System.Windows.Forms.Button SearchButton;
        private System.Windows.Forms.Label RegexTextBoxLabel;
        private System.Windows.Forms.Label Title;
        private System.Windows.Forms.TextBox NumGroupsTextBox;
        private System.Windows.Forms.Label NumGroupsTextBoxLabel;
    }
}