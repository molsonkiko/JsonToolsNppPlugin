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
                NppFormHelper.UnregisterFormIfModeless(this, true);
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
            this.Description = new System.Windows.Forms.Label();
            this.DebugInfoLabel = new System.Windows.Forms.Label();
            this.ThanksWowLinkLabel = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // Title
            // 
            this.Title.AutoSize = true;
            this.Title.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Title.Location = new System.Drawing.Point(124, 11);
            this.Title.Name = "Title";
            this.Title.Size = new System.Drawing.Size(220, 25);
            this.Title.TabIndex = 0;
            this.Title.Text = "JsonTools vX.Y.Z.A";
            // 
            // GitHubLink
            // 
            this.GitHubLink.AutoSize = true;
            this.GitHubLink.LinkArea = new System.Windows.Forms.LinkArea(34, 50);
            this.GitHubLink.Location = new System.Drawing.Point(35, 179);
            this.GitHubLink.Name = "GitHubLink";
            this.GitHubLink.Size = new System.Drawing.Size(395, 42);
            this.GitHubLink.TabIndex = 1;
            this.GitHubLink.TabStop = true;
            this.GitHubLink.Text = "[{\"need\": \"help?\"}, \"ask me at\",\r\n\"https://github.com/molsonkiko/JsonToolsNppPlug" +
    "in\"]";
            this.GitHubLink.UseCompatibleTextRendering = true;
            this.GitHubLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.GitHubLink_LinkClicked);
            // 
            // Description
            // 
            this.Description.AutoSize = true;
            this.Description.Location = new System.Drawing.Point(32, 80);
            this.Description.Name = "Description";
            this.Description.Size = new System.Drawing.Size(414, 60);
            this.Description.TabIndex = 3;
            this.Description.Text = "Query/editing tool for JSON including linting, reformatting, \r\na tree viewer with" +
    " file navigation,\r\na JMESpath-like query language, and much more";
            // 
            // DebugInfoLabel
            // 
            this.DebugInfoLabel.AutoSize = true;
            this.DebugInfoLabel.Location = new System.Drawing.Point(32, 251);
            this.DebugInfoLabel.Name = "DebugInfoLabel";
            this.DebugInfoLabel.Size = new System.Drawing.Size(409, 40);
            this.DebugInfoLabel.TabIndex = 4;
            this.DebugInfoLabel.Text = "Notepad++ version: X.Y.Z. For more info about your\r\ninstallation, go to ? -> Debu" +
    "g Info on the main status bar.";
            // 
            // ThanksWowLinkLabel
            // 
            this.ThanksWowLinkLabel.AutoSize = true;
            this.ThanksWowLinkLabel.LinkArea = new System.Windows.Forms.LinkArea(228, 4);
            this.ThanksWowLinkLabel.LinkColor = System.Drawing.Color.Black;
            this.ThanksWowLinkLabel.Location = new System.Drawing.Point(32, 319);
            this.ThanksWowLinkLabel.Name = "ThanksWowLinkLabel";
            this.ThanksWowLinkLabel.Size = new System.Drawing.Size(414, 132);
            this.ThanksWowLinkLabel.TabIndex = 5;
            this.ThanksWowLinkLabel.TabStop = true;
            this.ThanksWowLinkLabel.Text = resources.GetString("ThanksWowLinkLabel.Text");
            this.ThanksWowLinkLabel.UseCompatibleTextRendering = true;
            this.ThanksWowLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ThanksWowLink_LinkClicked);
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(471, 486);
            this.Controls.Add(this.ThanksWowLinkLabel);
            this.Controls.Add(this.DebugInfoLabel);
            this.Controls.Add(this.Description);
            this.Controls.Add(this.GitHubLink);
            this.Controls.Add(this.Title);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "AboutForm";
            this.Text = "About JsonTools";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Title;
        private System.Windows.Forms.LinkLabel GitHubLink;
        private System.Windows.Forms.Label Description;
        private System.Windows.Forms.Label DebugInfoLabel;
        private System.Windows.Forms.LinkLabel ThanksWowLinkLabel;
    }
}