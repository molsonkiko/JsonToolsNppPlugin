namespace JSON_Tools.Forms
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
            this.Tree = new System.Windows.Forms.TreeView();
            this.TypeIconList = new System.Windows.Forms.ImageList(this.components);
            this.QueryBox = new System.Windows.Forms.TextBox();
            this.SubmitQueryButton = new System.Windows.Forms.Button();
            this.SaveQueryButton = new System.Windows.Forms.Button();
            this.QueryToCsvButton = new System.Windows.Forms.Button();
            this.SchemaButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Tree
            // 
            this.Tree.Location = new System.Drawing.Point(4, 75);
            this.Tree.Name = "Tree";
            this.Tree.Size = new System.Drawing.Size(410, 550);
            this.Tree.TabIndex = 0;
            this.Tree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.Tree_NodeMouseClick);
            // 
            // TypeIconList
            // 
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
            // QueryBox
            // 
            this.QueryBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.RecentlyUsedList;
            this.QueryBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.QueryBox.Location = new System.Drawing.Point(4, 4);
            this.QueryBox.Multiline = true;
            this.QueryBox.Name = "QueryBox";
            this.QueryBox.Size = new System.Drawing.Size(164, 62);
            this.QueryBox.TabIndex = 0;
            this.QueryBox.Text = "@";
            // 
            // SubmitQueryButton
            // 
            this.SubmitQueryButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubmitQueryButton.Location = new System.Drawing.Point(174, 4);
            this.SubmitQueryButton.Name = "SubmitQueryButton";
            this.SubmitQueryButton.Size = new System.Drawing.Size(119, 26);
            this.SubmitQueryButton.TabIndex = 1;
            this.SubmitQueryButton.Text = "Submit query";
            this.SubmitQueryButton.UseVisualStyleBackColor = true;
            this.SubmitQueryButton.Click += new System.EventHandler(this.SubmitQueryButton_Click);
            // 
            // SaveQueryButton
            // 
            this.SaveQueryButton.Location = new System.Drawing.Point(174, 36);
            this.SaveQueryButton.Name = "SaveQueryButton";
            this.SaveQueryButton.Size = new System.Drawing.Size(119, 30);
            this.SaveQueryButton.TabIndex = 2;
            this.SaveQueryButton.Text = "Save query result";
            this.SaveQueryButton.UseVisualStyleBackColor = true;
            this.SaveQueryButton.Click += new System.EventHandler(this.SaveQueryResultButton_Click);
            // 
            // QueryToCsvButton
            // 
            this.QueryToCsvButton.Location = new System.Drawing.Point(299, 4);
            this.QueryToCsvButton.Name = "QueryToCsvButton";
            this.QueryToCsvButton.Size = new System.Drawing.Size(115, 26);
            this.QueryToCsvButton.TabIndex = 3;
            this.QueryToCsvButton.Text = "Query to CSV";
            this.QueryToCsvButton.UseVisualStyleBackColor = true;
            this.QueryToCsvButton.Click += new System.EventHandler(this.QueryToCsvButton_Click);
            // 
            // SchemaButton
            // 
            this.SchemaButton.Location = new System.Drawing.Point(299, 36);
            this.SchemaButton.Name = "SchemaButton";
            this.SchemaButton.Size = new System.Drawing.Size(115, 30);
            this.SchemaButton.TabIndex = 4;
            this.SchemaButton.Text = "Make schema";
            this.SchemaButton.UseVisualStyleBackColor = true;
            this.SchemaButton.Click += new System.EventHandler(this.SchemaButton_Click);
            // 
            // TreeViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(418, 631);
            this.Controls.Add(this.SchemaButton);
            this.Controls.Add(this.QueryToCsvButton);
            this.Controls.Add(this.SaveQueryButton);
            this.Controls.Add(this.SubmitQueryButton);
            this.Controls.Add(this.QueryBox);
            this.Controls.Add(this.Tree);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TreeViewer";
            this.Text = "TreeViewer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView Tree;
        private System.Windows.Forms.ImageList TypeIconList;
        private System.Windows.Forms.TextBox QueryBox;
        private System.Windows.Forms.Button SubmitQueryButton;
        private System.Windows.Forms.Button SaveQueryButton;
        private System.Windows.Forms.Button QueryToCsvButton;
        private System.Windows.Forms.Button SchemaButton;
    }
}