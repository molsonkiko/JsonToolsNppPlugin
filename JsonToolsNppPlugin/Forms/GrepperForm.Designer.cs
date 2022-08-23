using System.Windows.Forms;

namespace JSON_Viewer.Forms
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
            this.GrepTree = new System.Windows.Forms.TreeView();
            this.FilesFoundBox = new System.Windows.Forms.ListBox();
            this.FilesFoundBoxLabel = new System.Windows.Forms.Label();
            this.UrlBox = new System.Windows.Forms.TextBox();
            this.UrlBoxLabel = new System.Windows.Forms.Label();
            this.GrepTreeLabel = new System.Windows.Forms.Label();
            this.SendRequestsToUrlsButton = new System.Windows.Forms.Button();
            this.RecursiveGrepBox = new System.Windows.Forms.CheckBox();
            this.GrepJsonSectionTitle = new System.Windows.Forms.Label();
            this.SearchPatternBox = new System.Windows.Forms.TextBox();
            this.SearchPatternBoxLabel = new System.Windows.Forms.Label();
            this.GrepJsonButton = new System.Windows.Forms.Button();
            this.SearchResultsSectionTitle = new System.Windows.Forms.Label();
            this.ApiRequestSectionLabel = new System.Windows.Forms.Label();
            this.QueryBox = new System.Windows.Forms.TextBox();
            this.QueryBoxLabel = new System.Windows.Forms.Label();
            this.ExecuteQueryButton = new System.Windows.Forms.Button();
            this.GrepperFormTitle = new System.Windows.Forms.Label();
            this.SaveQueryResultsButton = new System.Windows.Forms.Button();
            this.CenterRightDivider = new System.Windows.Forms.Label();
            this.LeftCenterDivider = new System.Windows.Forms.Label();
            this.MiddleHorizDivider = new System.Windows.Forms.Label();
            this.RemoveSelectedFilesButton = new System.Windows.Forms.Button();
            this.QueryTree = new System.Windows.Forms.TreeView();
            this.QueryTreeLabel = new System.Windows.Forms.Label();
            this.RightDivider = new System.Windows.Forms.Label();
            this.QueryJsonTitle = new System.Windows.Forms.Label();
            this.ClearFilesButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // GrepTree
            // 
            this.GrepTree.Location = new System.Drawing.Point(22, 105);
            this.GrepTree.Name = "GrepTree";
            this.GrepTree.Size = new System.Drawing.Size(296, 327);
            this.GrepTree.TabIndex = 1;
            // 
            // FilesFoundBox
            // 
            this.FilesFoundBox.FormattingEnabled = true;
            this.FilesFoundBox.HorizontalScrollbar = true;
            this.FilesFoundBox.ItemHeight = 20;
            this.FilesFoundBox.Location = new System.Drawing.Point(622, 157);
            this.FilesFoundBox.Name = "FilesFoundBox";
            this.FilesFoundBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.FilesFoundBox.Size = new System.Drawing.Size(253, 264);
            this.FilesFoundBox.Sorted = true;
            this.FilesFoundBox.TabIndex = 2;
            // 
            // FilesFoundBoxLabel
            // 
            this.FilesFoundBoxLabel.AutoSize = true;
            this.FilesFoundBoxLabel.Location = new System.Drawing.Point(666, 105);
            this.FilesFoundBoxLabel.Name = "FilesFoundBoxLabel";
            this.FilesFoundBoxLabel.Size = new System.Drawing.Size(163, 40);
            this.FilesFoundBoxLabel.TabIndex = 3;
            this.FilesFoundBoxLabel.Text = "Below are the files/urls\r\nwhere JSON was found.";
            // 
            // UrlBox
            // 
            this.UrlBox.Location = new System.Drawing.Point(340, 148);
            this.UrlBox.Multiline = true;
            this.UrlBox.Name = "UrlBox";
            this.UrlBox.PlaceholderText = "Enter URLs (one per line) of APIs";
            this.UrlBox.Size = new System.Drawing.Size(253, 104);
            this.UrlBox.TabIndex = 4;
            this.UrlBox.WordWrap = false;
            // 
            // UrlBoxLabel
            // 
            this.UrlBoxLabel.AutoSize = true;
            this.UrlBoxLabel.Location = new System.Drawing.Point(361, 91);
            this.UrlBoxLabel.Name = "UrlBoxLabel";
            this.UrlBoxLabel.Size = new System.Drawing.Size(197, 40);
            this.UrlBoxLabel.TabIndex = 5;
            this.UrlBoxLabel.Text = "Enter URLs of APIs you want \r\nto request JSON from.";
            // 
            // GrepTreeLabel
            // 
            this.GrepTreeLabel.AutoSize = true;
            this.GrepTreeLabel.Location = new System.Drawing.Point(66, 77);
            this.GrepTreeLabel.Name = "GrepTreeLabel";
            this.GrepTreeLabel.Size = new System.Drawing.Size(209, 20);
            this.GrepTreeLabel.TabIndex = 6;
            this.GrepTreeLabel.Text = "Files/urls and associated JSON";
            // 
            // SendRequestsToUrlsButton
            // 
            this.SendRequestsToUrlsButton.Location = new System.Drawing.Point(340, 262);
            this.SendRequestsToUrlsButton.Name = "SendRequestsToUrlsButton";
            this.SendRequestsToUrlsButton.Size = new System.Drawing.Size(253, 29);
            this.SendRequestsToUrlsButton.TabIndex = 7;
            this.SendRequestsToUrlsButton.Text = "Send API requests to URLs";
            this.SendRequestsToUrlsButton.UseVisualStyleBackColor = true;
            this.SendRequestsToUrlsButton.Click += new System.EventHandler(this.SendRequestsToUrlsButton_Click);
            // 
            // RecursiveGrepBox
            // 
            this.RecursiveGrepBox.AutoSize = true;
            this.RecursiveGrepBox.Location = new System.Drawing.Point(340, 358);
            this.RecursiveGrepBox.Name = "RecursiveGrepBox";
            this.RecursiveGrepBox.Size = new System.Drawing.Size(195, 24);
            this.RecursiveGrepBox.TabIndex = 8;
            this.RecursiveGrepBox.Text = "Search in subdirectories?";
            this.RecursiveGrepBox.UseVisualStyleBackColor = true;
            // 
            // GrepJsonSectionTitle
            // 
            this.GrepJsonSectionTitle.AutoSize = true;
            this.GrepJsonSectionTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.GrepJsonSectionTitle.Location = new System.Drawing.Point(340, 313);
            this.GrepJsonSectionTitle.Name = "GrepJsonSectionTitle";
            this.GrepJsonSectionTitle.Size = new System.Drawing.Size(251, 28);
            this.GrepJsonSectionTitle.TabIndex = 9;
            this.GrepJsonSectionTitle.Text = "Get JSON from local files";
            // 
            // SearchPatternBox
            // 
            this.SearchPatternBox.Location = new System.Drawing.Point(340, 398);
            this.SearchPatternBox.Name = "SearchPatternBox";
            this.SearchPatternBox.Size = new System.Drawing.Size(125, 27);
            this.SearchPatternBox.TabIndex = 10;
            this.SearchPatternBox.Text = "*.json";
            // 
            // SearchPatternBoxLabel
            // 
            this.SearchPatternBoxLabel.AutoSize = true;
            this.SearchPatternBoxLabel.Location = new System.Drawing.Point(471, 401);
            this.SearchPatternBoxLabel.Name = "SearchPatternBoxLabel";
            this.SearchPatternBoxLabel.Size = new System.Drawing.Size(105, 20);
            this.SearchPatternBoxLabel.TabIndex = 11;
            this.SearchPatternBoxLabel.Text = "Search pattern";
            // 
            // GrepJsonButton
            // 
            this.GrepJsonButton.Location = new System.Drawing.Point(340, 442);
            this.GrepJsonButton.Name = "GrepJsonButton";
            this.GrepJsonButton.Size = new System.Drawing.Size(251, 29);
            this.GrepJsonButton.TabIndex = 12;
            this.GrepJsonButton.Text = "Choose directory for JSON files";
            this.GrepJsonButton.UseVisualStyleBackColor = true;
            this.GrepJsonButton.Click += new System.EventHandler(this.GrepJsonButton_Click);
            // 
            // SearchResultsSectionTitle
            // 
            this.SearchResultsSectionTitle.AutoSize = true;
            this.SearchResultsSectionTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.SearchResultsSectionTitle.Location = new System.Drawing.Point(641, 57);
            this.SearchResultsSectionTitle.Name = "SearchResultsSectionTitle";
            this.SearchResultsSectionTitle.Size = new System.Drawing.Size(219, 28);
            this.SearchResultsSectionTitle.TabIndex = 13;
            this.SearchResultsSectionTitle.Text = "Choose files and URLs";
            // 
            // ApiRequestSectionLabel
            // 
            this.ApiRequestSectionLabel.AutoSize = true;
            this.ApiRequestSectionLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.ApiRequestSectionLabel.Location = new System.Drawing.Point(361, 57);
            this.ApiRequestSectionLabel.Name = "ApiRequestSectionLabel";
            this.ApiRequestSectionLabel.Size = new System.Drawing.Size(201, 28);
            this.ApiRequestSectionLabel.TabIndex = 14;
            this.ApiRequestSectionLabel.Text = "Get JSON from APIs";
            // 
            // QueryBox
            // 
            this.QueryBox.Location = new System.Drawing.Point(891, 136);
            this.QueryBox.Multiline = true;
            this.QueryBox.Name = "QueryBox";
            this.QueryBox.Size = new System.Drawing.Size(222, 53);
            this.QueryBox.TabIndex = 15;
            this.QueryBox.Text = "@";
            // 
            // QueryBoxLabel
            // 
            this.QueryBoxLabel.AutoSize = true;
            this.QueryBoxLabel.Location = new System.Drawing.Point(927, 104);
            this.QueryBoxLabel.Name = "QueryBoxLabel";
            this.QueryBoxLabel.Size = new System.Drawing.Size(160, 20);
            this.QueryBoxLabel.TabIndex = 16;
            this.QueryBoxLabel.Text = "Enter RemesPath query";
            // 
            // ExecuteQueryButton
            // 
            this.ExecuteQueryButton.Location = new System.Drawing.Point(952, 198);
            this.ExecuteQueryButton.Name = "ExecuteQueryButton";
            this.ExecuteQueryButton.Size = new System.Drawing.Size(113, 29);
            this.ExecuteQueryButton.TabIndex = 17;
            this.ExecuteQueryButton.Text = "Execute query";
            this.ExecuteQueryButton.UseVisualStyleBackColor = true;
            this.ExecuteQueryButton.Click += new System.EventHandler(this.ExecuteQueryButton_Click);
            // 
            // GrepperFormTitle
            // 
            this.GrepperFormTitle.AutoSize = true;
            this.GrepperFormTitle.Font = new System.Drawing.Font("Segoe UI", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.GrepperFormTitle.Location = new System.Drawing.Point(434, 9);
            this.GrepperFormTitle.Name = "GrepperFormTitle";
            this.GrepperFormTitle.Size = new System.Drawing.Size(324, 31);
            this.GrepperFormTitle.TabIndex = 18;
            this.GrepperFormTitle.Text = "Get JSON from files and APIs";
            // 
            // SaveQueryResultsButton
            // 
            this.SaveQueryResultsButton.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.SaveQueryResultsButton.Location = new System.Drawing.Point(891, 442);
            this.SaveQueryResultsButton.Name = "SaveQueryResultsButton";
            this.SaveQueryResultsButton.Size = new System.Drawing.Size(222, 29);
            this.SaveQueryResultsButton.TabIndex = 19;
            this.SaveQueryResultsButton.Text = "Save query results to files";
            this.SaveQueryResultsButton.UseVisualStyleBackColor = true;
            this.SaveQueryResultsButton.Click += new System.EventHandler(this.SaveQueryResultsButton_Click);
            // 
            // CenterRightDivider
            // 
            this.CenterRightDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.CenterRightDivider.Location = new System.Drawing.Point(599, 63);
            this.CenterRightDivider.Name = "CenterRightDivider";
            this.CenterRightDivider.Size = new System.Drawing.Size(2, 418);
            this.CenterRightDivider.TabIndex = 20;
            // 
            // LeftCenterDivider
            // 
            this.LeftCenterDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.LeftCenterDivider.Location = new System.Drawing.Point(332, 63);
            this.LeftCenterDivider.Name = "LeftCenterDivider";
            this.LeftCenterDivider.Size = new System.Drawing.Size(2, 418);
            this.LeftCenterDivider.TabIndex = 21;
            // 
            // MiddleHorizDivider
            // 
            this.MiddleHorizDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MiddleHorizDivider.Location = new System.Drawing.Point(332, 299);
            this.MiddleHorizDivider.Name = "MiddleHorizDivider";
            this.MiddleHorizDivider.Size = new System.Drawing.Size(268, 2);
            this.MiddleHorizDivider.TabIndex = 22;
            // 
            // RemoveSelectedFilesButton
            // 
            this.RemoveSelectedFilesButton.Location = new System.Drawing.Point(665, 442);
            this.RemoveSelectedFilesButton.Name = "RemoveSelectedFilesButton";
            this.RemoveSelectedFilesButton.Size = new System.Drawing.Size(164, 29);
            this.RemoveSelectedFilesButton.TabIndex = 23;
            this.RemoveSelectedFilesButton.Text = "Remove selected files";
            this.RemoveSelectedFilesButton.UseVisualStyleBackColor = true;
            this.RemoveSelectedFilesButton.Click += new System.EventHandler(this.RemoveSelectedFilesButton_Click);
            // 
            // QueryTree
            // 
            this.QueryTree.Location = new System.Drawing.Point(891, 262);
            this.QueryTree.Name = "QueryTree";
            this.QueryTree.Size = new System.Drawing.Size(222, 170);
            this.QueryTree.TabIndex = 24;
            // 
            // QueryTreeLabel
            // 
            this.QueryTreeLabel.AutoSize = true;
            this.QueryTreeLabel.Location = new System.Drawing.Point(961, 232);
            this.QueryTreeLabel.Name = "QueryTreeLabel";
            this.QueryTreeLabel.Size = new System.Drawing.Size(94, 20);
            this.QueryTreeLabel.TabIndex = 25;
            this.QueryTreeLabel.Text = "Query results";
            // 
            // RightDivider
            // 
            this.RightDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.RightDivider.Location = new System.Drawing.Point(883, 57);
            this.RightDivider.Name = "RightDivider";
            this.RightDivider.Size = new System.Drawing.Size(2, 418);
            this.RightDivider.TabIndex = 26;
            // 
            // QueryJsonTitle
            // 
            this.QueryJsonTitle.AutoSize = true;
            this.QueryJsonTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.QueryJsonTitle.Location = new System.Drawing.Point(938, 57);
            this.QueryJsonTitle.Name = "QueryJsonTitle";
            this.QueryJsonTitle.Size = new System.Drawing.Size(127, 28);
            this.QueryJsonTitle.TabIndex = 27;
            this.QueryJsonTitle.Text = "Query JSON";
            // 
            // ClearFilesButton
            // 
            this.ClearFilesButton.Location = new System.Drawing.Point(125, 442);
            this.ClearFilesButton.Name = "ClearFilesButton";
            this.ClearFilesButton.Size = new System.Drawing.Size(106, 29);
            this.ClearFilesButton.TabIndex = 28;
            this.ClearFilesButton.Text = "Clear all files";
            this.ClearFilesButton.UseVisualStyleBackColor = true;
            this.ClearFilesButton.Click += new System.EventHandler(this.ClearFilesButton_Click);
            // 
            // GrepperForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1134, 494);
            this.Controls.Add(this.ClearFilesButton);
            this.Controls.Add(this.QueryJsonTitle);
            this.Controls.Add(this.RightDivider);
            this.Controls.Add(this.QueryTreeLabel);
            this.Controls.Add(this.QueryTree);
            this.Controls.Add(this.RemoveSelectedFilesButton);
            this.Controls.Add(this.MiddleHorizDivider);
            this.Controls.Add(this.LeftCenterDivider);
            this.Controls.Add(this.CenterRightDivider);
            this.Controls.Add(this.SaveQueryResultsButton);
            this.Controls.Add(this.GrepperFormTitle);
            this.Controls.Add(this.ExecuteQueryButton);
            this.Controls.Add(this.QueryBoxLabel);
            this.Controls.Add(this.QueryBox);
            this.Controls.Add(this.ApiRequestSectionLabel);
            this.Controls.Add(this.SearchResultsSectionTitle);
            this.Controls.Add(this.GrepJsonButton);
            this.Controls.Add(this.SearchPatternBoxLabel);
            this.Controls.Add(this.SearchPatternBox);
            this.Controls.Add(this.GrepJsonSectionTitle);
            this.Controls.Add(this.RecursiveGrepBox);
            this.Controls.Add(this.SendRequestsToUrlsButton);
            this.Controls.Add(this.GrepTreeLabel);
            this.Controls.Add(this.UrlBoxLabel);
            this.Controls.Add(this.UrlBox);
            this.Controls.Add(this.FilesFoundBoxLabel);
            this.Controls.Add(this.FilesFoundBox);
            this.Controls.Add(this.GrepTree);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GrepperForm";
            this.Text = "JSON from files and APIs";
            this.Load += new System.EventHandler(this.GrepperForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TreeView GrepTree;
        private ListBox FilesFoundBox;
        private Label FilesFoundBoxLabel;
        private TextBox UrlBox;
        private Label UrlBoxLabel;
        private Label GrepTreeLabel;
        private Button SendRequestsToUrlsButton;
        private CheckBox RecursiveGrepBox;
        private Label GrepJsonSectionTitle;
        private TextBox SearchPatternBox;
        private Label SearchPatternBoxLabel;
        private Button GrepJsonButton;
        private Label SearchResultsSectionTitle;
        private Label ApiRequestSectionLabel;
        private TextBox QueryBox;
        private Label QueryBoxLabel;
        private Button ExecuteQueryButton;
        private Label GrepperFormTitle;
        private Button SaveQueryResultsButton;
        private Label CenterRightDivider;
        private Label LeftCenterDivider;
        private Label MiddleHorizDivider;
        private Button RemoveSelectedFilesButton;
        private TreeView QueryTree;
        private Label QueryTreeLabel;
        private Label RightDivider;
        private Label QueryJsonTitle;
        private Button ClearFilesButton;
    }
}