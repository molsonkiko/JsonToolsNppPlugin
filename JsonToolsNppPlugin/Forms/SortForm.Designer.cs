namespace JSON_Tools.Forms
{
    partial class SortForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SortForm));
            this.SortFormTitle = new System.Windows.Forms.Label();
            this.PathTextBoxLabel = new System.Windows.Forms.Label();
            this.PathTextBox = new System.Windows.Forms.TextBox();
            this.SortButton = new System.Windows.Forms.Button();
            this.ReverseOrderCheckBox = new System.Windows.Forms.CheckBox();
            this.SortMethodBox = new System.Windows.Forms.ComboBox();
            this.SortMethodBoxLabel = new System.Windows.Forms.Label();
            this.QueryKeyIndexTextBox = new System.Windows.Forms.TextBox();
            this.QueryKeyIndexTextBoxLabel = new System.Windows.Forms.Label();
            this.IsMultipleArraysCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // SortFormTitle
            // 
            this.SortFormTitle.AutoSize = true;
            this.SortFormTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SortFormTitle.Location = new System.Drawing.Point(70, 9);
            this.SortFormTitle.Name = "SortFormTitle";
            this.SortFormTitle.Size = new System.Drawing.Size(181, 22);
            this.SortFormTitle.TabIndex = 6;
            this.SortFormTitle.Text = "Sort JSON array(s)";
            // 
            // PathTextBoxLabel
            // 
            this.PathTextBoxLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PathTextBoxLabel.AutoSize = true;
            this.PathTextBoxLabel.Location = new System.Drawing.Point(218, 57);
            this.PathTextBoxLabel.Name = "PathTextBoxLabel";
            this.PathTextBoxLabel.Size = new System.Drawing.Size(97, 16);
            this.PathTextBoxLabel.TabIndex = 7;
            this.PathTextBoxLabel.Text = "Path to array(s)";
            // 
            // PathTextBox
            // 
            this.PathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PathTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.PathTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.HistoryList;
            this.PathTextBox.Location = new System.Drawing.Point(12, 54);
            this.PathTextBox.Name = "PathTextBox";
            this.PathTextBox.Size = new System.Drawing.Size(196, 22);
            this.PathTextBox.TabIndex = 0;
            this.PathTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyDown);
            this.PathTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.PathTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyUp);
            // 
            // SortButton
            // 
            this.SortButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SortButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SortButton.Location = new System.Drawing.Point(111, 270);
            this.SortButton.Name = "SortButton";
            this.SortButton.Size = new System.Drawing.Size(111, 26);
            this.SortButton.TabIndex = 5;
            this.SortButton.Text = "Sort";
            this.SortButton.UseVisualStyleBackColor = true;
            this.SortButton.Click += new System.EventHandler(this.SortButton_Clicked);
            this.SortButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyUp);
            // 
            // ReverseOrderCheckBox
            // 
            this.ReverseOrderCheckBox.AutoSize = true;
            this.ReverseOrderCheckBox.Location = new System.Drawing.Point(12, 131);
            this.ReverseOrderCheckBox.Name = "ReverseOrderCheckBox";
            this.ReverseOrderCheckBox.Size = new System.Drawing.Size(149, 20);
            this.ReverseOrderCheckBox.TabIndex = 2;
            this.ReverseOrderCheckBox.Text = "&Biggest to smallest?";
            this.ReverseOrderCheckBox.UseVisualStyleBackColor = true;
            this.ReverseOrderCheckBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyDown);
            this.ReverseOrderCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyUp);
            // 
            // SortMethodBox
            // 
            this.SortMethodBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SortMethodBox.FormattingEnabled = true;
            this.SortMethodBox.Items.AddRange(new object[] {
            "Default",
            "As strings (ignoring case)",
            "By index/key of each child",
            "By query on each child",
            "Shuffle"});
            this.SortMethodBox.Location = new System.Drawing.Point(12, 171);
            this.SortMethodBox.Name = "SortMethodBox";
            this.SortMethodBox.Size = new System.Drawing.Size(196, 24);
            this.SortMethodBox.TabIndex = 3;
            this.SortMethodBox.SelectedValueChanged += new System.EventHandler(this.SortMethodBox_SelectionChanged);
            this.SortMethodBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyDown);
            this.SortMethodBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.SortMethodBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyUp);
            // 
            // SortMethodBoxLabel
            // 
            this.SortMethodBoxLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SortMethodBoxLabel.AutoSize = true;
            this.SortMethodBoxLabel.Location = new System.Drawing.Point(218, 174);
            this.SortMethodBoxLabel.Name = "SortMethodBoxLabel";
            this.SortMethodBoxLabel.Size = new System.Drawing.Size(79, 16);
            this.SortMethodBoxLabel.TabIndex = 8;
            this.SortMethodBoxLabel.Text = "Sort method";
            // 
            // QueryKeyIndexTextBox
            // 
            this.QueryKeyIndexTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.QueryKeyIndexTextBox.Enabled = false;
            this.QueryKeyIndexTextBox.Location = new System.Drawing.Point(12, 219);
            this.QueryKeyIndexTextBox.Name = "QueryKeyIndexTextBox";
            this.QueryKeyIndexTextBox.Size = new System.Drawing.Size(196, 22);
            this.QueryKeyIndexTextBox.TabIndex = 4;
            this.QueryKeyIndexTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyDown);
            this.QueryKeyIndexTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.QueryKeyIndexTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyUp);
            // 
            // QueryKeyIndexTextBoxLabel
            // 
            this.QueryKeyIndexTextBoxLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.QueryKeyIndexTextBoxLabel.AutoSize = true;
            this.QueryKeyIndexTextBoxLabel.Location = new System.Drawing.Point(218, 222);
            this.QueryKeyIndexTextBoxLabel.Name = "QueryKeyIndexTextBoxLabel";
            this.QueryKeyIndexTextBoxLabel.Size = new System.Drawing.Size(104, 16);
            this.QueryKeyIndexTextBoxLabel.TabIndex = 9;
            this.QueryKeyIndexTextBoxLabel.Text = "Key/index/query";
            // 
            // IsMultipleArraysCheckBox
            // 
            this.IsMultipleArraysCheckBox.AutoSize = true;
            this.IsMultipleArraysCheckBox.Location = new System.Drawing.Point(12, 94);
            this.IsMultipleArraysCheckBox.Name = "IsMultipleArraysCheckBox";
            this.IsMultipleArraysCheckBox.Size = new System.Drawing.Size(311, 20);
            this.IsMultipleArraysCheckBox.TabIndex = 1;
            this.IsMultipleArraysCheckBox.Text = "Path goes to &multiple arrays; sort each subarray";
            this.IsMultipleArraysCheckBox.UseVisualStyleBackColor = true;
            this.IsMultipleArraysCheckBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyDown);
            this.IsMultipleArraysCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyUp);
            // 
            // SortForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(329, 308);
            this.Controls.Add(this.PathTextBoxLabel);
            this.Controls.Add(this.PathTextBox);
            this.Controls.Add(this.IsMultipleArraysCheckBox);
            this.Controls.Add(this.ReverseOrderCheckBox);
            this.Controls.Add(this.SortMethodBoxLabel);
            this.Controls.Add(this.SortMethodBox);
            this.Controls.Add(this.QueryKeyIndexTextBoxLabel);
            this.Controls.Add(this.QueryKeyIndexTextBox);
            this.Controls.Add(this.SortButton);
            this.Controls.Add(this.SortFormTitle);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SortForm";
            this.Text = "Sort JSON arrays";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SortForm_KeyUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label SortFormTitle;
        private System.Windows.Forms.Label PathTextBoxLabel;
        private System.Windows.Forms.Label SortMethodBoxLabel;
        private System.Windows.Forms.Label QueryKeyIndexTextBoxLabel;
        internal System.Windows.Forms.TextBox PathTextBox;
        internal System.Windows.Forms.ComboBox SortMethodBox;
        internal System.Windows.Forms.TextBox QueryKeyIndexTextBox;
        internal System.Windows.Forms.Button SortButton;
        internal System.Windows.Forms.CheckBox ReverseOrderCheckBox;
        internal System.Windows.Forms.CheckBox IsMultipleArraysCheckBox;
    }
}