namespace JSON_Tools.Forms
{
    partial class GrepperForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GrepperForm));
            this.GrepperFormTitle = new System.Windows.Forms.Label();
            this.UrlsBox = new System.Windows.Forms.TextBox();
            this.GetJsonFromApisTitle = new System.Windows.Forms.Label();
            this.UrlsBoxLabel = new System.Windows.Forms.Label();
            this.SendRequestsButton = new System.Windows.Forms.Button();
            this.GetJsonFromFilesTitle = new System.Windows.Forms.Label();
            this.RecursiveSearchCheckBox = new System.Windows.Forms.CheckBox();
            this.SearchPatternsBox = new System.Windows.Forms.TextBox();
            this.SearchPatternsBoxLabel = new System.Windows.Forms.Label();
            this.ChooseDirectoriesButton = new System.Windows.Forms.Button();
            this.ChooseFilesTitle = new System.Windows.Forms.Label();
            this.FilesFoundBox = new System.Windows.Forms.ListBox();
            this.RemoveSelectedFilesButton = new System.Windows.Forms.Button();
            this.ViewResultsButton = new System.Windows.Forms.Button();
            this.LeftCenterDivider = new System.Windows.Forms.Label();
            this.CenterRightDivider = new System.Windows.Forms.Label();
            this.TopBottomDivider = new System.Windows.Forms.Label();
            this.ViewErrorsButton = new System.Windows.Forms.Button();
            this.DocsButton = new System.Windows.Forms.Button();
            this.FolderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.DirectoriesVisitedBox = new System.Windows.Forms.ComboBox();
            this.SearchDirectoriesButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // GrepperFormTitle
            // 
            this.GrepperFormTitle.AutoSize = true;
            this.GrepperFormTitle.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GrepperFormTitle.Location = new System.Drawing.Point(339, 9);
            this.GrepperFormTitle.Name = "GrepperFormTitle";
            this.GrepperFormTitle.Size = new System.Drawing.Size(281, 31);
            this.GrepperFormTitle.TabIndex = 0;
            this.GrepperFormTitle.Text = "JSON from files and APIs";
            // 
            // UrlsBox
            // 
            this.UrlsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.UrlsBox.Location = new System.Drawing.Point(26, 133);
            this.UrlsBox.Multiline = true;
            this.UrlsBox.Name = "UrlsBox";
            this.UrlsBox.Size = new System.Drawing.Size(304, 293);
            this.UrlsBox.TabIndex = 3;
            this.UrlsBox.WordWrap = false;
            this.UrlsBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.UrlsBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // GetJsonFromApisTitle
            // 
            this.GetJsonFromApisTitle.AutoSize = true;
            this.GetJsonFromApisTitle.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GetJsonFromApisTitle.Location = new System.Drawing.Point(91, 51);
            this.GetJsonFromApisTitle.Name = "GetJsonFromApisTitle";
            this.GetJsonFromApisTitle.Size = new System.Drawing.Size(182, 25);
            this.GetJsonFromApisTitle.TabIndex = 1;
            this.GetJsonFromApisTitle.Text = "Get JSON from APIs";
            // 
            // UrlsBoxLabel
            // 
            this.UrlsBoxLabel.Location = new System.Drawing.Point(27, 83);
            this.UrlsBoxLabel.Name = "UrlsBoxLabel";
            this.UrlsBoxLabel.Size = new System.Drawing.Size(303, 35);
            this.UrlsBoxLabel.TabIndex = 2;
            this.UrlsBoxLabel.Text = "Enter URLs of APIs you want to request JSON from (one per line or as JSON array)";
            // 
            // SendRequestsButton
            // 
            this.SendRequestsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SendRequestsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SendRequestsButton.Location = new System.Drawing.Point(111, 441);
            this.SendRequestsButton.Name = "SendRequestsButton";
            this.SendRequestsButton.Size = new System.Drawing.Size(143, 33);
            this.SendRequestsButton.TabIndex = 4;
            this.SendRequestsButton.Text = "Send &API requests";
            this.SendRequestsButton.UseVisualStyleBackColor = true;
            this.SendRequestsButton.Click += new System.EventHandler(this.SendRequestsButton_Click);
            this.SendRequestsButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // GetJsonFromFilesTitle
            // 
            this.GetJsonFromFilesTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.GetJsonFromFilesTitle.AutoSize = true;
            this.GetJsonFromFilesTitle.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GetJsonFromFilesTitle.Location = new System.Drawing.Point(377, 59);
            this.GetJsonFromFilesTitle.Name = "GetJsonFromFilesTitle";
            this.GetJsonFromFilesTitle.Size = new System.Drawing.Size(224, 25);
            this.GetJsonFromFilesTitle.TabIndex = 5;
            this.GetJsonFromFilesTitle.Text = "Get JSON from local files";
            // 
            // RecursiveSearchCheckBox
            // 
            this.RecursiveSearchCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RecursiveSearchCheckBox.AutoSize = true;
            this.RecursiveSearchCheckBox.Location = new System.Drawing.Point(368, 95);
            this.RecursiveSearchCheckBox.Name = "RecursiveSearchCheckBox";
            this.RecursiveSearchCheckBox.Size = new System.Drawing.Size(180, 20);
            this.RecursiveSearchCheckBox.TabIndex = 6;
            this.RecursiveSearchCheckBox.Text = "&Search in subdirectories?";
            this.RecursiveSearchCheckBox.UseVisualStyleBackColor = true;
            this.RecursiveSearchCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // SearchPatternsBox
            // 
            this.SearchPatternsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchPatternsBox.Location = new System.Drawing.Point(363, 128);
            this.SearchPatternsBox.Multiline = true;
            this.SearchPatternsBox.Name = "SearchPatternsBox";
            this.SearchPatternsBox.Size = new System.Drawing.Size(87, 56);
            this.SearchPatternsBox.TabIndex = 7;
            this.SearchPatternsBox.Text = "*.json";
            this.SearchPatternsBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.SearchPatternsBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // SearchPatternsBoxLabel
            // 
            this.SearchPatternsBoxLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchPatternsBoxLabel.Location = new System.Drawing.Point(456, 128);
            this.SearchPatternsBoxLabel.Name = "SearchPatternsBoxLabel";
            this.SearchPatternsBoxLabel.Size = new System.Drawing.Size(158, 56);
            this.SearchPatternsBoxLabel.TabIndex = 20;
            this.SearchPatternsBoxLabel.Text = "Enter search pattern(s)\r\n(one per line)";
            // 
            // ChooseDirectoriesButton
            // 
            this.ChooseDirectoriesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ChooseDirectoriesButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChooseDirectoriesButton.Location = new System.Drawing.Point(406, 198);
            this.ChooseDirectoriesButton.Name = "ChooseDirectoriesButton";
            this.ChooseDirectoriesButton.Size = new System.Drawing.Size(156, 32);
            this.ChooseDirectoriesButton.TabIndex = 9;
            this.ChooseDirectoriesButton.Text = "&Choose directory...";
            this.ChooseDirectoriesButton.UseVisualStyleBackColor = true;
            this.ChooseDirectoriesButton.Click += new System.EventHandler(this.ChooseDirectoriesButton_Click);
            this.ChooseDirectoriesButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // ChooseFilesTitle
            // 
            this.ChooseFilesTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ChooseFilesTitle.AutoSize = true;
            this.ChooseFilesTitle.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChooseFilesTitle.Location = new System.Drawing.Point(704, 51);
            this.ChooseFilesTitle.Name = "ChooseFilesTitle";
            this.ChooseFilesTitle.Size = new System.Drawing.Size(198, 25);
            this.ChooseFilesTitle.TabIndex = 21;
            this.ChooseFilesTitle.Text = "Choose files and URLs";
            // 
            // FilesFoundBox
            // 
            this.FilesFoundBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.FilesFoundBox.FormattingEnabled = true;
            this.FilesFoundBox.HorizontalScrollbar = true;
            this.FilesFoundBox.ItemHeight = 16;
            this.FilesFoundBox.Location = new System.Drawing.Point(649, 86);
            this.FilesFoundBox.Name = "FilesFoundBox";
            this.FilesFoundBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.FilesFoundBox.Size = new System.Drawing.Size(313, 340);
            this.FilesFoundBox.TabIndex = 12;
            this.FilesFoundBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // RemoveSelectedFilesButton
            // 
            this.RemoveSelectedFilesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.RemoveSelectedFilesButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveSelectedFilesButton.Location = new System.Drawing.Point(709, 441);
            this.RemoveSelectedFilesButton.Name = "RemoveSelectedFilesButton";
            this.RemoveSelectedFilesButton.Size = new System.Drawing.Size(181, 33);
            this.RemoveSelectedFilesButton.TabIndex = 13;
            this.RemoveSelectedFilesButton.Text = "&Remove selected files";
            this.RemoveSelectedFilesButton.UseVisualStyleBackColor = true;
            this.RemoveSelectedFilesButton.Click += new System.EventHandler(this.RemoveSelectedFilesButton_Click);
            this.RemoveSelectedFilesButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // ViewResultsButton
            // 
            this.ViewResultsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ViewResultsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ViewResultsButton.Location = new System.Drawing.Point(363, 441);
            this.ViewResultsButton.Name = "ViewResultsButton";
            this.ViewResultsButton.Size = new System.Drawing.Size(238, 33);
            this.ViewResultsButton.TabIndex = 16;
            this.ViewResultsButton.Text = "View results in &buffer";
            this.ViewResultsButton.UseVisualStyleBackColor = true;
            this.ViewResultsButton.Click += new System.EventHandler(this.ViewResultsButton_Click);
            this.ViewResultsButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // LeftCenterDivider
            // 
            this.LeftCenterDivider.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LeftCenterDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.LeftCenterDivider.Location = new System.Drawing.Point(345, 51);
            this.LeftCenterDivider.Name = "LeftCenterDivider";
            this.LeftCenterDivider.Size = new System.Drawing.Size(3, 375);
            this.LeftCenterDivider.TabIndex = 16;
            // 
            // CenterRightDivider
            // 
            this.CenterRightDivider.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CenterRightDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.CenterRightDivider.Location = new System.Drawing.Point(631, 51);
            this.CenterRightDivider.Name = "CenterRightDivider";
            this.CenterRightDivider.Size = new System.Drawing.Size(3, 375);
            this.CenterRightDivider.TabIndex = 18;
            // 
            // TopBottomDivider
            // 
            this.TopBottomDivider.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.TopBottomDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.TopBottomDivider.Location = new System.Drawing.Point(345, 325);
            this.TopBottomDivider.Name = "TopBottomDivider";
            this.TopBottomDivider.Size = new System.Drawing.Size(287, 3);
            this.TopBottomDivider.TabIndex = 17;
            // 
            // ViewErrorsButton
            // 
            this.ViewErrorsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ViewErrorsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ViewErrorsButton.Location = new System.Drawing.Point(363, 393);
            this.ViewErrorsButton.Name = "ViewErrorsButton";
            this.ViewErrorsButton.Size = new System.Drawing.Size(238, 33);
            this.ViewErrorsButton.TabIndex = 15;
            this.ViewErrorsButton.Text = "View &errors";
            this.ViewErrorsButton.UseVisualStyleBackColor = true;
            this.ViewErrorsButton.Click += new System.EventHandler(this.ViewErrorsButton_Click);
            this.ViewErrorsButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // DocsButton
            // 
            this.DocsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DocsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DocsButton.Location = new System.Drawing.Point(363, 340);
            this.DocsButton.Name = "DocsButton";
            this.DocsButton.Size = new System.Drawing.Size(238, 33);
            this.DocsButton.TabIndex = 14;
            this.DocsButton.Text = "&Documentation";
            this.DocsButton.UseVisualStyleBackColor = true;
            this.DocsButton.Click += new System.EventHandler(this.DocsButton_Click);
            this.DocsButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // FolderBrowserDialog1
            // 
            this.FolderBrowserDialog1.Description = "Choose folder to find JSON files in";
            this.FolderBrowserDialog1.ShowNewFolderButton = false;
            // 
            // DirectoriesVisitedBox
            // 
            this.DirectoriesVisitedBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DirectoriesVisitedBox.FormattingEnabled = true;
            this.DirectoriesVisitedBox.Items.AddRange(new object[] {
            "Previously visited directories..."});
            this.DirectoriesVisitedBox.Location = new System.Drawing.Point(363, 245);
            this.DirectoriesVisitedBox.Name = "DirectoriesVisitedBox";
            this.DirectoriesVisitedBox.Size = new System.Drawing.Size(251, 24);
            this.DirectoriesVisitedBox.TabIndex = 10;
            this.DirectoriesVisitedBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            this.DirectoriesVisitedBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            // 
            // SearchDirectoriesButton
            // 
            this.SearchDirectoriesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchDirectoriesButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SearchDirectoriesButton.Location = new System.Drawing.Point(406, 281);
            this.SearchDirectoriesButton.Name = "SearchDirectoriesButton";
            this.SearchDirectoriesButton.Size = new System.Drawing.Size(156, 32);
            this.SearchDirectoriesButton.TabIndex = 11;
            this.SearchDirectoriesButton.Text = "Search directories";
            this.SearchDirectoriesButton.UseVisualStyleBackColor = true;
            this.SearchDirectoriesButton.Click += new System.EventHandler(this.SearchDirectoriesButton_Click);
            // 
            // GrepperForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(987, 486);
            this.Controls.Add(this.UrlsBoxLabel);
            this.Controls.Add(this.UrlsBox);
            this.Controls.Add(this.SendRequestsButton);
            this.Controls.Add(this.RecursiveSearchCheckBox);
            this.Controls.Add(this.SearchPatternsBoxLabel);
            this.Controls.Add(this.SearchPatternsBox);
            this.Controls.Add(this.ChooseDirectoriesButton);
            this.Controls.Add(this.DirectoriesVisitedBox);
            this.Controls.Add(this.SearchDirectoriesButton);
            this.Controls.Add(this.FilesFoundBox);
            this.Controls.Add(this.RemoveSelectedFilesButton);
            this.Controls.Add(this.DocsButton);
            this.Controls.Add(this.ViewErrorsButton);
            this.Controls.Add(this.ViewResultsButton);
            this.Controls.Add(this.TopBottomDivider);
            this.Controls.Add(this.CenterRightDivider);
            this.Controls.Add(this.LeftCenterDivider);
            this.Controls.Add(this.ChooseFilesTitle);
            this.Controls.Add(this.GetJsonFromFilesTitle);
            this.Controls.Add(this.GetJsonFromApisTitle);
            this.Controls.Add(this.GrepperFormTitle);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GrepperForm";
            this.Text = "JSON from files and APIs";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GrepperForm_FormClosing);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GrepperForm_KeyUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label GrepperFormTitle;
        private System.Windows.Forms.TextBox UrlsBox;
        private System.Windows.Forms.Label GetJsonFromApisTitle;
        private System.Windows.Forms.Label UrlsBoxLabel;
        private System.Windows.Forms.Button SendRequestsButton;
        private System.Windows.Forms.Label GetJsonFromFilesTitle;
        private System.Windows.Forms.CheckBox RecursiveSearchCheckBox;
        private System.Windows.Forms.TextBox SearchPatternsBox;
        private System.Windows.Forms.Label SearchPatternsBoxLabel;
        private System.Windows.Forms.Button ChooseDirectoriesButton;
        private System.Windows.Forms.Label ChooseFilesTitle;
        private System.Windows.Forms.ListBox FilesFoundBox;
        private System.Windows.Forms.Button RemoveSelectedFilesButton;
        private System.Windows.Forms.Button ViewResultsButton;
        private System.Windows.Forms.Label LeftCenterDivider;
        private System.Windows.Forms.Label CenterRightDivider;
        private System.Windows.Forms.Label TopBottomDivider;
        private System.Windows.Forms.Button ViewErrorsButton;
        private System.Windows.Forms.Button DocsButton;
        private System.Windows.Forms.FolderBrowserDialog FolderBrowserDialog1;
        private System.Windows.Forms.ComboBox DirectoriesVisitedBox;
        private System.Windows.Forms.Button SearchDirectoriesButton;
    }
}