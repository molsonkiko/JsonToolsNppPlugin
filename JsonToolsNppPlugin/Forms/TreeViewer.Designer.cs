using System.Windows.Forms;

namespace JSON_Viewer.Forms
{
    partial class TreeViewer
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TreeViewer));
            this.DocsButton = new System.Windows.Forms.Button();
            this.JsonBox = new System.Windows.Forms.TextBox();
            this.JsonTree = new System.Windows.Forms.TreeView();
            this.TreeCreationButton = new System.Windows.Forms.Button();
            this.FileSelectionButton = new System.Windows.Forms.Button();
            this.QueryBox = new System.Windows.Forms.TextBox();
            this.SettingsButton = new System.Windows.Forms.Button();
            this.QueryResultTree = new System.Windows.Forms.TreeView();
            this.QuerySubmissionButton = new System.Windows.Forms.Button();
            this.SaveQueryToFileButton = new System.Windows.Forms.Button();
            this.TypeIconList = new System.Windows.Forms.ImageList(this.components);
            this.ReloadFileButton = new System.Windows.Forms.Button();
            this.GenerateSchemaButton = new System.Windows.Forms.Button();
            this.OpenJsonToCsvFormButton = new System.Windows.Forms.Button();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.OpenGrepperFormButton = new System.Windows.Forms.Button();
            this.CenterRightDivider = new System.Windows.Forms.Label();
            this.LeftCenterDivider = new System.Windows.Forms.Label();
            this.MiddleHorizDivider = new System.Windows.Forms.Label();
            this.LintButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // DocsButton
            // 
            this.DocsButton.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.DocsButton.Location = new System.Drawing.Point(486, 356);
            this.DocsButton.Name = "DocsButton";
            this.DocsButton.Size = new System.Drawing.Size(96, 39);
            this.DocsButton.TabIndex = 0;
            this.DocsButton.Text = "View Docs";
            this.DocsButton.UseVisualStyleBackColor = true;
            this.DocsButton.Click += new System.EventHandler(this.DocsButton_Click);
            // 
            // JsonBox
            // 
            this.JsonBox.Location = new System.Drawing.Point(316, 47);
            this.JsonBox.Multiline = true;
            this.JsonBox.Name = "JsonBox";
            this.JsonBox.PlaceholderText = "Enter JSON here";
            this.JsonBox.Size = new System.Drawing.Size(250, 125);
            this.JsonBox.TabIndex = 1;
            // 
            // JsonTree
            // 
            this.JsonTree.Location = new System.Drawing.Point(54, 47);
            this.JsonTree.Name = "JsonTree";
            this.JsonTree.Size = new System.Drawing.Size(232, 348);
            this.JsonTree.TabIndex = 2;
            // 
            // TreeCreationButton
            // 
            this.TreeCreationButton.Location = new System.Drawing.Point(362, 178);
            this.TreeCreationButton.Name = "TreeCreationButton";
            this.TreeCreationButton.Size = new System.Drawing.Size(157, 29);
            this.TreeCreationButton.TabIndex = 3;
            this.TreeCreationButton.Text = "Create tree from text";
            this.TreeCreationButton.UseVisualStyleBackColor = true;
            this.TreeCreationButton.Click += new System.EventHandler(this.TreeCreationButton_Click);
            // 
            // FileSelectionButton
            // 
            this.FileSelectionButton.Location = new System.Drawing.Point(54, 409);
            this.FileSelectionButton.Name = "FileSelectionButton";
            this.FileSelectionButton.Size = new System.Drawing.Size(121, 29);
            this.FileSelectionButton.TabIndex = 4;
            this.FileSelectionButton.Text = "Open JSON file";
            this.FileSelectionButton.UseVisualStyleBackColor = true;
            this.FileSelectionButton.Click += new System.EventHandler(this.FileSelectionButton_Click);
            // 
            // QueryBox
            // 
            this.QueryBox.Location = new System.Drawing.Point(599, 47);
            this.QueryBox.Multiline = true;
            this.QueryBox.Name = "QueryBox";
            this.QueryBox.PlaceholderText = "Enter RemesPath query";
            this.QueryBox.Size = new System.Drawing.Size(248, 67);
            this.QueryBox.TabIndex = 5;
            this.QueryBox.Text = "@";
            // 
            // SettingsButton
            // 
            this.SettingsButton.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.SettingsButton.Location = new System.Drawing.Point(308, 356);
            this.SettingsButton.Name = "SettingsButton";
            this.SettingsButton.Size = new System.Drawing.Size(94, 39);
            this.SettingsButton.TabIndex = 6;
            this.SettingsButton.Text = "Settings";
            this.SettingsButton.UseVisualStyleBackColor = true;
            this.SettingsButton.Click += new System.EventHandler(this.SettingsButton_Click);
            // 
            // QueryResultTree
            // 
            this.QueryResultTree.Location = new System.Drawing.Point(599, 159);
            this.QueryResultTree.Name = "QueryResultTree";
            this.QueryResultTree.Size = new System.Drawing.Size(248, 236);
            this.QueryResultTree.TabIndex = 8;
            // 
            // QuerySubmissionButton
            // 
            this.QuerySubmissionButton.Location = new System.Drawing.Point(599, 124);
            this.QuerySubmissionButton.Name = "QuerySubmissionButton";
            this.QuerySubmissionButton.Size = new System.Drawing.Size(186, 29);
            this.QuerySubmissionButton.TabIndex = 10;
            this.QuerySubmissionButton.Text = "Submit RemesPath query";
            this.QuerySubmissionButton.UseVisualStyleBackColor = true;
            this.QuerySubmissionButton.Click += new System.EventHandler(this.QuerySubmissionButton_Click);
            // 
            // SaveQueryToFileButton
            // 
            this.SaveQueryToFileButton.Location = new System.Drawing.Point(627, 409);
            this.SaveQueryToFileButton.Name = "SaveQueryToFileButton";
            this.SaveQueryToFileButton.Size = new System.Drawing.Size(192, 29);
            this.SaveQueryToFileButton.TabIndex = 11;
            this.SaveQueryToFileButton.Text = "Save query result to file";
            this.SaveQueryToFileButton.UseVisualStyleBackColor = true;
            this.SaveQueryToFileButton.Click += new System.EventHandler(this.SaveQueryToFileButton_Click);
            // 
            // TypeIconList
            // 
            this.TypeIconList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.TypeIconList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("TypeIconList.ImageStream")));
            this.TypeIconList.TransparentColor = System.Drawing.Color.Transparent;
            this.TypeIconList.Images.SetKeyName(0, "array type icon.PNG");
            this.TypeIconList.Images.SetKeyName(1, "bool type icon.PNG");
            this.TypeIconList.Images.SetKeyName(2, "date type icon.PNG");
            this.TypeIconList.Images.SetKeyName(3, "float type icon.PNG");
            this.TypeIconList.Images.SetKeyName(4, "int type icon.PNG");
            this.TypeIconList.Images.SetKeyName(5, "object type icon.PNG");
            this.TypeIconList.Images.SetKeyName(6, "string type icon.PNG");
            this.TypeIconList.Images.SetKeyName(7, "null type icon.PNG");
            // 
            // ReloadFileButton
            // 
            this.ReloadFileButton.Location = new System.Drawing.Point(192, 409);
            this.ReloadFileButton.Name = "ReloadFileButton";
            this.ReloadFileButton.Size = new System.Drawing.Size(94, 29);
            this.ReloadFileButton.TabIndex = 12;
            this.ReloadFileButton.Text = "Reload file";
            this.ReloadFileButton.UseVisualStyleBackColor = true;
            this.ReloadFileButton.Click += new System.EventHandler(this.ReloadFileButton_Click);
            // 
            // GenerateSchemaButton
            // 
            this.GenerateSchemaButton.Location = new System.Drawing.Point(308, 213);
            this.GenerateSchemaButton.Name = "GenerateSchemaButton";
            this.GenerateSchemaButton.Size = new System.Drawing.Size(277, 29);
            this.GenerateSchemaButton.TabIndex = 13;
            this.GenerateSchemaButton.Text = "Generate JSON Schema for query result";
            this.GenerateSchemaButton.UseVisualStyleBackColor = true;
            this.GenerateSchemaButton.Click += new System.EventHandler(this.GenerateSchemaButton_Click);
            // 
            // OpenJsonToCsvFormButton
            // 
            this.OpenJsonToCsvFormButton.Location = new System.Drawing.Point(362, 263);
            this.OpenJsonToCsvFormButton.Name = "OpenJsonToCsvFormButton";
            this.OpenJsonToCsvFormButton.Size = new System.Drawing.Size(157, 29);
            this.OpenJsonToCsvFormButton.TabIndex = 14;
            this.OpenJsonToCsvFormButton.Text = "Query result to CSV";
            this.OpenJsonToCsvFormButton.UseVisualStyleBackColor = true;
            this.OpenJsonToCsvFormButton.Click += new System.EventHandler(this.OpenJsonToCsvFormButton_Click);
            // 
            // TitleLabel
            // 
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.TitleLabel.Location = new System.Drawing.Point(383, 9);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(118, 28);
            this.TitleLabel.TabIndex = 15;
            this.TitleLabel.Text = "JSON Tools";
            // 
            // OpenGrepperFormButton
            // 
            this.OpenGrepperFormButton.Location = new System.Drawing.Point(345, 409);
            this.OpenGrepperFormButton.Name = "OpenGrepperFormButton";
            this.OpenGrepperFormButton.Size = new System.Drawing.Size(192, 29);
            this.OpenGrepperFormButton.TabIndex = 16;
            this.OpenGrepperFormButton.Text = "Open multiple files/APIs";
            this.OpenGrepperFormButton.UseVisualStyleBackColor = true;
            this.OpenGrepperFormButton.Click += new System.EventHandler(this.OpenGrepperFormButton_Click);
            // 
            // CenterRightDivider
            // 
            this.CenterRightDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.CenterRightDivider.Location = new System.Drawing.Point(588, 47);
            this.CenterRightDivider.Name = "CenterRightDivider";
            this.CenterRightDivider.Size = new System.Drawing.Size(2, 391);
            this.CenterRightDivider.TabIndex = 17;
            // 
            // LeftCenterDivider
            // 
            this.LeftCenterDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.LeftCenterDivider.Location = new System.Drawing.Point(300, 49);
            this.LeftCenterDivider.Name = "LeftCenterDivider";
            this.LeftCenterDivider.Size = new System.Drawing.Size(2, 391);
            this.LeftCenterDivider.TabIndex = 18;
            // 
            // MiddleHorizDivider
            // 
            this.MiddleHorizDivider.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MiddleHorizDivider.Location = new System.Drawing.Point(300, 343);
            this.MiddleHorizDivider.Name = "MiddleHorizDivider";
            this.MiddleHorizDivider.Size = new System.Drawing.Size(289, 2);
            this.MiddleHorizDivider.TabIndex = 19;
            // 
            // LintButton
            // 
            this.LintButton.Location = new System.Drawing.Point(395, 311);
            this.LintButton.Name = "LintButton";
            this.LintButton.Size = new System.Drawing.Size(94, 29);
            this.LintButton.TabIndex = 20;
            this.LintButton.Text = "View errors";
            this.LintButton.UseVisualStyleBackColor = true;
            this.LintButton.Click += new System.EventHandler(this.LintButton_Click);
            // 
            // TreeViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(891, 450);
            this.Controls.Add(this.LintButton);
            this.Controls.Add(this.MiddleHorizDivider);
            this.Controls.Add(this.LeftCenterDivider);
            this.Controls.Add(this.CenterRightDivider);
            this.Controls.Add(this.OpenGrepperFormButton);
            this.Controls.Add(this.TitleLabel);
            this.Controls.Add(this.OpenJsonToCsvFormButton);
            this.Controls.Add(this.GenerateSchemaButton);
            this.Controls.Add(this.ReloadFileButton);
            this.Controls.Add(this.SaveQueryToFileButton);
            this.Controls.Add(this.QuerySubmissionButton);
            this.Controls.Add(this.QueryResultTree);
            this.Controls.Add(this.SettingsButton);
            this.Controls.Add(this.QueryBox);
            this.Controls.Add(this.FileSelectionButton);
            this.Controls.Add(this.TreeCreationButton);
            this.Controls.Add(this.JsonTree);
            this.Controls.Add(this.JsonBox);
            this.Controls.Add(this.DocsButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TreeViewer";
            this.Text = "JSON Tree Viewer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button DocsButton;
        private TextBox JsonBox;
        private TreeView JsonTree;
        private Button TreeCreationButton;
        private Button FileSelectionButton;
        private TextBox QueryBox;
        private Button SettingsButton;
        private TreeView QueryResultTree;
        private Button QuerySubmissionButton;
        private Button SaveQueryToFileButton;
        private Button ReloadFileButton;
        private Button GenerateSchemaButton;
        private Button OpenJsonToCsvFormButton;
        private Label TitleLabel;
        private Button OpenGrepperFormButton;
        private Label CenterRightDivider;
        private Label LeftCenterDivider;
        private Label MiddleHorizDivider;
        public ImageList TypeIconList;
        private Button LintButton;
    }
}