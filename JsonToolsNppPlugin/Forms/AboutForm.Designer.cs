namespace JSON_Tools.Forms
{
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.Title = new System.Windows.Forms.Label();
            this.GitHubLink = new System.Windows.Forms.LinkLabel();
            this.Thanks = new System.Windows.Forms.Label();
            this.Description = new System.Windows.Forms.Label();
            this.DebugInfoLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Title
            // 
            this.Title.AutoSize = true;
            this.Title.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Title.Location = new System.Drawing.Point(110, 9);
            this.Title.Name = "Title";
            this.Title.Size = new System.Drawing.Size(170, 20);
            this.Title.TabIndex = 0;
            this.Title.Text = "JsonTools vX.Y.Z.A";
            // 
            // GitHubLink
            // 
            this.GitHubLink.AutoSize = true;
            this.GitHubLink.LinkArea = new System.Windows.Forms.LinkArea(34, 48);
            this.GitHubLink.Location = new System.Drawing.Point(31, 143);
            this.GitHubLink.Name = "GitHubLink";
            this.GitHubLink.Size = new System.Drawing.Size(321, 35);
            this.GitHubLink.TabIndex = 1;
            this.GitHubLink.TabStop = true;
            this.GitHubLink.Text = "[{\"need\": \"help?\"}, \"ask me at\",\r\n\"https://github.com/molsonkiko/JsonToolsNppPlug" +
    "in\"]";
            this.GitHubLink.UseCompatibleTextRendering = true;
            this.GitHubLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.GitHubLink_LinkClicked);
            // 
            // Thanks
            // 
            this.Thanks.AutoSize = true;
            this.Thanks.Location = new System.Drawing.Point(28, 259);
            this.Thanks.Name = "Thanks";
            this.Thanks.Size = new System.Drawing.Size(328, 80);
            this.Thanks.TabIndex = 2;
            this.Thanks.Text = "Special thanks to:\r\n* Don Ho for making Notepad++\r\n* kbilsted for making the plug" +
    "in pack this is based on\r\n* And of course everyone who helped make this plugin\r\n" +
    "   better!";
            // 
            // Description
            // 
            this.Description.AutoSize = true;
            this.Description.Location = new System.Drawing.Point(28, 64);
            this.Description.Name = "Description";
            this.Description.Size = new System.Drawing.Size(344, 48);
            this.Description.TabIndex = 3;
            this.Description.Text = "Query/editing tool for JSON including linting, reformatting, \r\na tree viewer with" +
    " file navigation,\r\na JMESpath-like query language, and much more";
            // 
            // DebugInfoLabel
            // 
            this.DebugInfoLabel.AutoSize = true;
            this.DebugInfoLabel.Location = new System.Drawing.Point(28, 201);
            this.DebugInfoLabel.Name = "DebugInfoLabel";
            this.DebugInfoLabel.Size = new System.Drawing.Size(264, 32);
            this.DebugInfoLabel.TabIndex = 4;
            this.DebugInfoLabel.Text = "For info about your Notepad++ installation,\r\ngo to ? -> Debug Info on the main st" +
    "atus bar.";
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(399, 358);
            this.Controls.Add(this.DebugInfoLabel);
            this.Controls.Add(this.Description);
            this.Controls.Add(this.Thanks);
            this.Controls.Add(this.GitHubLink);
            this.Controls.Add(this.Title);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AboutForm";
            this.Text = "About JsonTools";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Title;
        private System.Windows.Forms.LinkLabel GitHubLink;
        private System.Windows.Forms.Label Thanks;
        private System.Windows.Forms.Label Description;
        private System.Windows.Forms.Label DebugInfoLabel;
    }
}