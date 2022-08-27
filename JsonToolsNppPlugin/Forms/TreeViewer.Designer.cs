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
            this.SuspendLayout();
            // 
            // Tree
            // 
            this.Tree.Location = new System.Drawing.Point(4, 4);
            this.Tree.Name = "Tree";
            this.Tree.Size = new System.Drawing.Size(355, 581);
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
            // TreeViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(364, 590);
            this.Controls.Add(this.Tree);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TreeViewer";
            this.Text = "TreeViewer";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView Tree;
        private System.Windows.Forms.ImageList TypeIconList;
    }
}