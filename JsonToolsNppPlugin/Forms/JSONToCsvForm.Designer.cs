using System.Windows.Forms;

namespace JSON_Viewer.Forms
{
    partial class JSONToCsvForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JSONToCsvForm));
            this.StrategyBox = new System.Windows.Forms.ComboBox();
            this.KeySepBoxLabel = new System.Windows.Forms.Label();
            this.DelimBoxLabel = new System.Windows.Forms.Label();
            this.StrategyBoxLabel = new System.Windows.Forms.Label();
            this.GenerateCSVButton = new System.Windows.Forms.Button();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.DelimBox = new System.Windows.Forms.ComboBox();
            this.KeySepBox = new System.Windows.Forms.TextBox();
            this.DocsButton = new System.Windows.Forms.Button();
            this.BoolsAsIntsCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // StrategyBox
            // 
            this.StrategyBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.StrategyBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.StrategyBox.FormattingEnabled = true;
            this.StrategyBox.Items.AddRange(new object[] {
            "Default",
            "Full Recursive",
            "No recursion",
            "Stringify iterables"});
            this.StrategyBox.Location = new System.Drawing.Point(34, 188);
            this.StrategyBox.Name = "StrategyBox";
            this.StrategyBox.Size = new System.Drawing.Size(194, 28);
            this.StrategyBox.TabIndex = 2;
            // 
            // KeySepBoxLabel
            // 
            this.KeySepBoxLabel.AutoSize = true;
            this.KeySepBoxLabel.Location = new System.Drawing.Point(104, 62);
            this.KeySepBoxLabel.Name = "KeySepBoxLabel";
            this.KeySepBoxLabel.Size = new System.Drawing.Size(102, 20);
            this.KeySepBoxLabel.TabIndex = 4;
            this.KeySepBoxLabel.Text = "Key Separator";
            // 
            // DelimBoxLabel
            // 
            this.DelimBoxLabel.AutoSize = true;
            this.DelimBoxLabel.Location = new System.Drawing.Point(104, 101);
            this.DelimBoxLabel.Name = "DelimBoxLabel";
            this.DelimBoxLabel.Size = new System.Drawing.Size(160, 20);
            this.DelimBoxLabel.TabIndex = 5;
            this.DelimBoxLabel.Text = "Delimiter in output file";
            // 
            // StrategyBoxLabel
            // 
            this.StrategyBoxLabel.AutoSize = true;
            this.StrategyBoxLabel.Location = new System.Drawing.Point(245, 191);
            this.StrategyBoxLabel.Name = "StrategyBoxLabel";
            this.StrategyBoxLabel.Size = new System.Drawing.Size(64, 20);
            this.StrategyBoxLabel.TabIndex = 6;
            this.StrategyBoxLabel.Text = "Strategy";
            // 
            // GenerateCSVButton
            // 
            this.GenerateCSVButton.Location = new System.Drawing.Point(34, 245);
            this.GenerateCSVButton.Name = "GenerateCSVButton";
            this.GenerateCSVButton.Size = new System.Drawing.Size(109, 29);
            this.GenerateCSVButton.TabIndex = 4;
            this.GenerateCSVButton.Text = "Generate CSV";
            this.GenerateCSVButton.UseVisualStyleBackColor = true;
            this.GenerateCSVButton.Click += new System.EventHandler(this.GenerateCSVButton_Click);
            // 
            // TitleLabel
            // 
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.TitleLabel.Location = new System.Drawing.Point(74, 9);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(211, 28);
            this.TitleLabel.TabIndex = 9;
            this.TitleLabel.Text = "CSV Generation form";
            // 
            // DelimBox
            // 
            this.DelimBox.FormattingEnabled = true;
            this.DelimBox.Items.AddRange(new object[] {
            ",",
            "Tab (\\t)",
            "|"});
            this.DelimBox.Location = new System.Drawing.Point(34, 101);
            this.DelimBox.MaxLength = 1;
            this.DelimBox.Name = "DelimBox";
            this.DelimBox.Size = new System.Drawing.Size(53, 28);
            this.DelimBox.TabIndex = 1;
            // 
            // KeySepBox
            // 
            this.KeySepBox.Location = new System.Drawing.Point(34, 62);
            this.KeySepBox.MaxLength = 1;
            this.KeySepBox.Name = "KeySepBox";
            this.KeySepBox.Size = new System.Drawing.Size(53, 27);
            this.KeySepBox.TabIndex = 12;
            this.KeySepBox.Text = ".";
            // 
            // DocsButton
            // 
            this.DocsButton.Location = new System.Drawing.Point(245, 245);
            this.DocsButton.Name = "DocsButton";
            this.DocsButton.Size = new System.Drawing.Size(64, 29);
            this.DocsButton.TabIndex = 13;
            this.DocsButton.Text = "Docs";
            this.DocsButton.UseVisualStyleBackColor = true;
            this.DocsButton.Click += new System.EventHandler(this.DocsButton_Click);
            // 
            // BoolsAsIntsCheckBox
            // 
            this.BoolsAsIntsCheckBox.AutoSize = true;
            this.BoolsAsIntsCheckBox.Location = new System.Drawing.Point(34, 149);
            this.BoolsAsIntsCheckBox.Name = "BoolsAsIntsCheckBox";
            this.BoolsAsIntsCheckBox.Size = new System.Drawing.Size(229, 24);
            this.BoolsAsIntsCheckBox.TabIndex = 14;
            this.BoolsAsIntsCheckBox.Text = "Convert booleans to integers?";
            this.BoolsAsIntsCheckBox.UseVisualStyleBackColor = true;
            // 
            // JSONToCsvForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(345, 296);
            this.Controls.Add(this.BoolsAsIntsCheckBox);
            this.Controls.Add(this.DocsButton);
            this.Controls.Add(this.KeySepBox);
            this.Controls.Add(this.DelimBox);
            this.Controls.Add(this.TitleLabel);
            this.Controls.Add(this.GenerateCSVButton);
            this.Controls.Add(this.StrategyBoxLabel);
            this.Controls.Add(this.DelimBoxLabel);
            this.Controls.Add(this.KeySepBoxLabel);
            this.Controls.Add(this.StrategyBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "JSONToCsvForm";
            this.Text = "JSON to CSV";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private ComboBox StrategyBox;
        private Label KeySepBoxLabel;
        private Label DelimBoxLabel;
        private Label StrategyBoxLabel;
        private Button GenerateCSVButton;
        private Label TitleLabel;
        private ComboBox DelimBox;
        private TextBox KeySepBox;
        private Button DocsButton;
        private CheckBox BoolsAsIntsCheckBox;
    }
}