using System.Windows.Forms;

namespace JSON_Viewer.Forms
{
    partial class LintForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LintForm));
            this.LintFormTitle = new System.Windows.Forms.Label();
            this.DocListBox = new System.Windows.Forms.ListBox();
            this.DocListBoxLabel = new System.Windows.Forms.Label();
            this.LintBox = new System.Windows.Forms.TextBox();
            this.LintBoxLabel = new System.Windows.Forms.Label();
            this.SaveLintButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // LintFormTitle
            // 
            this.LintFormTitle.AutoSize = true;
            this.LintFormTitle.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.LintFormTitle.Location = new System.Drawing.Point(201, 9);
            this.LintFormTitle.Name = "LintFormTitle";
            this.LintFormTitle.Size = new System.Drawing.Size(371, 31);
            this.LintFormTitle.TabIndex = 0;
            this.LintFormTitle.Text = "Syntax errors in JSON documents";
            // 
            // DocListBox
            // 
            this.DocListBox.FormattingEnabled = true;
            this.DocListBox.HorizontalScrollbar = true;
            this.DocListBox.ItemHeight = 20;
            this.DocListBox.Location = new System.Drawing.Point(12, 94);
            this.DocListBox.Name = "DocListBox";
            this.DocListBox.Size = new System.Drawing.Size(276, 284);
            this.DocListBox.TabIndex = 1;
            this.DocListBox.SelectedIndexChanged += new System.EventHandler(this.DocListBox_SelectedIndexChanged);
            // 
            // DocListBoxLabel
            // 
            this.DocListBoxLabel.AutoSize = true;
            this.DocListBoxLabel.Location = new System.Drawing.Point(92, 61);
            this.DocListBoxLabel.Name = "DocListBoxLabel";
            this.DocListBoxLabel.Size = new System.Drawing.Size(112, 20);
            this.DocListBoxLabel.TabIndex = 2;
            this.DocListBoxLabel.Text = "Files with errors";
            // 
            // LintBox
            // 
            this.LintBox.Location = new System.Drawing.Point(334, 94);
            this.LintBox.Multiline = true;
            this.LintBox.Name = "LintBox";
            this.LintBox.ReadOnly = true;
            this.LintBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.LintBox.Size = new System.Drawing.Size(430, 324);
            this.LintBox.TabIndex = 3;
            this.LintBox.WordWrap = false;
            // 
            // LintBoxLabel
            // 
            this.LintBoxLabel.AutoSize = true;
            this.LintBoxLabel.Location = new System.Drawing.Point(498, 61);
            this.LintBoxLabel.Name = "LintBoxLabel";
            this.LintBoxLabel.Size = new System.Drawing.Size(112, 20);
            this.LintBoxLabel.TabIndex = 4;
            this.LintBoxLabel.Text = "Files with errors";
            // 
            // SaveLintButton
            // 
            this.SaveLintButton.Location = new System.Drawing.Point(82, 384);
            this.SaveLintButton.Name = "SaveLintButton";
            this.SaveLintButton.Size = new System.Drawing.Size(136, 29);
            this.SaveLintButton.TabIndex = 5;
            this.SaveLintButton.Text = "Save errors to file";
            this.SaveLintButton.UseVisualStyleBackColor = true;
            this.SaveLintButton.Click += new System.EventHandler(this.SaveLintButton_Click);
            // 
            // LintForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 430);
            this.Controls.Add(this.SaveLintButton);
            this.Controls.Add(this.LintBoxLabel);
            this.Controls.Add(this.LintBox);
            this.Controls.Add(this.DocListBoxLabel);
            this.Controls.Add(this.DocListBox);
            this.Controls.Add(this.LintFormTitle);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "LintForm";
            this.Text = "Syntax errors in JSON documents";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label LintFormTitle;
        private ListBox DocListBox;
        private Label DocListBoxLabel;
        private TextBox LintBox;
        private Label LintBoxLabel;
        private Button SaveLintButton;
    }
}