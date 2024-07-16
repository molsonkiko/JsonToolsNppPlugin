namespace JSON_Tools.Forms
{
    partial class TreeViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used, and remove reference to associated JSON
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (pathsToJNodes != null)
                pathsToJNodes.Clear();
            json = null;
            queryResult = null;
            pathsToJNodes = null;
            if (findReplaceForm != null && !findReplaceForm.IsDisposed)
            {
                findReplaceForm.Close();
                findReplaceForm.Dispose();
                findReplaceForm = null;
            }
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TreeViewer));
            this.Tree = new System.Windows.Forms.TreeView();
            this.TypeIconList = new System.Windows.Forms.ImageList(this.components);
            this.QueryBox = new System.Windows.Forms.TextBox();
            this.SubmitQueryButton = new System.Windows.Forms.Button();
            this.SaveQueryButton = new System.Windows.Forms.Button();
            this.QueryToCsvButton = new System.Windows.Forms.Button();
            this.NodeRightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CopyValueMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CopyKeyItem = new System.Windows.Forms.ToolStripMenuItem();
            this.JavaScriptStyleItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PythonStyleItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemesPathStyleItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CopyPathItem = new System.Windows.Forms.ToolStripMenuItem();
            this.JavaScriptStylePathItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PythonStylePathItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemesPathStylePathItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToggleSubtreesItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SelectThisItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenSortFormItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SelectAllChildrenItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CurrentPathBox = new System.Windows.Forms.TextBox();
            this.RefreshButton = new System.Windows.Forms.Button();
            this.FindReplaceButton = new System.Windows.Forms.Button();
            this.DocumentTypeComboBox = new System.Windows.Forms.ComboBox();
            this.path_separatorStyleItem = new System.Windows.Forms.ToolStripMenuItem();
            this.path_separatorStylePathItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NodeRightClickMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // Tree
            // 
            this.Tree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Tree.Location = new System.Drawing.Point(4, 99);
            this.Tree.Name = "Tree";
            this.Tree.Size = new System.Drawing.Size(457, 331);
            this.Tree.TabIndex = 7;
            this.Tree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.Tree_BeforeExpand);
            this.Tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.Tree_AfterSelect);
            this.Tree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.Tree_NodeMouseClick);
            this.Tree.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Tree_KeyDown);
            this.Tree.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
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
            this.TypeIconList.Images.SetKeyName(8, "array type icon darkmode.PNG");
            this.TypeIconList.Images.SetKeyName(9, "bool type icon darkmode.PNG");
            this.TypeIconList.Images.SetKeyName(10, "date type icon darkmode.PNG");
            this.TypeIconList.Images.SetKeyName(11, "float type icon darkmode.PNG");
            this.TypeIconList.Images.SetKeyName(12, "int type icon darkmode.PNG");
            this.TypeIconList.Images.SetKeyName(13, "object type icon darkmode.PNG");
            this.TypeIconList.Images.SetKeyName(14, "string type icon darkmode.PNG");
            // 
            // QueryBox
            // 
            this.QueryBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.QueryBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.QueryBox.Location = new System.Drawing.Point(4, 4);
            this.QueryBox.Multiline = true;
            this.QueryBox.Name = "QueryBox";
            this.QueryBox.Size = new System.Drawing.Size(203, 89);
            this.QueryBox.TabIndex = 0;
            this.QueryBox.Text = "@";
            this.QueryBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.QueryBox_KeyPress);
            this.QueryBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // SubmitQueryButton
            // 
            this.SubmitQueryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SubmitQueryButton.AutoSize = true;
            this.SubmitQueryButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubmitQueryButton.Location = new System.Drawing.Point(220, 4);
            this.SubmitQueryButton.Name = "SubmitQueryButton";
            this.SubmitQueryButton.Size = new System.Drawing.Size(130, 26);
            this.SubmitQueryButton.TabIndex = 1;
            this.SubmitQueryButton.Text = "Submit query";
            this.SubmitQueryButton.UseVisualStyleBackColor = true;
            this.SubmitQueryButton.Click += new System.EventHandler(this.SubmitQueryButton_Click);
            this.SubmitQueryButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // SaveQueryButton
            // 
            this.SaveQueryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveQueryButton.AutoSize = true;
            this.SaveQueryButton.Location = new System.Drawing.Point(220, 36);
            this.SaveQueryButton.Name = "SaveQueryButton";
            this.SaveQueryButton.Size = new System.Drawing.Size(130, 27);
            this.SaveQueryButton.TabIndex = 3;
            this.SaveQueryButton.Text = "Save query result";
            this.SaveQueryButton.UseVisualStyleBackColor = true;
            this.SaveQueryButton.Click += new System.EventHandler(this.SaveQueryResultButton_Click);
            this.SaveQueryButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // QueryToCsvButton
            // 
            this.QueryToCsvButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.QueryToCsvButton.AutoSize = true;
            this.QueryToCsvButton.Location = new System.Drawing.Point(359, 4);
            this.QueryToCsvButton.Name = "QueryToCsvButton";
            this.QueryToCsvButton.Size = new System.Drawing.Size(103, 26);
            this.QueryToCsvButton.TabIndex = 2;
            this.QueryToCsvButton.Text = "Query to CSV";
            this.QueryToCsvButton.UseVisualStyleBackColor = true;
            this.QueryToCsvButton.Click += new System.EventHandler(this.QueryToCsvButton_Click);
            this.QueryToCsvButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // NodeRightClickMenu
            // 
            this.NodeRightClickMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.NodeRightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyValueMenuItem,
            this.CopyKeyItem,
            this.CopyPathItem,
            this.ToggleSubtreesItem,
            this.SelectThisItem,
            this.OpenSortFormItem,
            this.SelectAllChildrenItem});
            this.NodeRightClickMenu.Name = "NodeRightClickMenu";
            this.NodeRightClickMenu.Size = new System.Drawing.Size(268, 200);
            // 
            // CopyValueMenuItem
            // 
            this.CopyValueMenuItem.Name = "CopyValueMenuItem";
            this.CopyValueMenuItem.Size = new System.Drawing.Size(267, 24);
            this.CopyValueMenuItem.Text = "Value to clipboard";
            // 
            // CopyKeyItem
            // 
            this.CopyKeyItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.JavaScriptStyleItem,
            this.PythonStyleItem,
            this.RemesPathStyleItem,
            this.path_separatorStyleItem});
            this.CopyKeyItem.Name = "CopyKeyItem";
            this.CopyKeyItem.Size = new System.Drawing.Size(267, 24);
            this.CopyKeyItem.Text = "Key/index to clipboard";
            // 
            // JavaScriptStyleItem
            // 
            this.JavaScriptStyleItem.Name = "JavaScriptStyleItem";
            this.JavaScriptStyleItem.Size = new System.Drawing.Size(268, 26);
            this.JavaScriptStyleItem.Text = "JavaScript style";
            // 
            // PythonStyleItem
            // 
            this.PythonStyleItem.Name = "PythonStyleItem";
            this.PythonStyleItem.Size = new System.Drawing.Size(268, 26);
            this.PythonStyleItem.Text = "Python style";
            // 
            // RemesPathStyleItem
            // 
            this.RemesPathStyleItem.Name = "RemesPathStyleItem";
            this.RemesPathStyleItem.Size = new System.Drawing.Size(268, 26);
            this.RemesPathStyleItem.Text = "RemesPath style";
            // 
            // CopyPathItem
            // 
            this.CopyPathItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.JavaScriptStylePathItem,
            this.PythonStylePathItem,
            this.RemesPathStylePathItem,
            this.path_separatorStylePathItem});
            this.CopyPathItem.Name = "CopyPathItem";
            this.CopyPathItem.Size = new System.Drawing.Size(267, 24);
            this.CopyPathItem.Text = "Path to clipboard";
            // 
            // JavaScriptStylePathItem
            // 
            this.JavaScriptStylePathItem.Name = "JavaScriptStylePathItem";
            this.JavaScriptStylePathItem.Size = new System.Drawing.Size(268, 26);
            this.JavaScriptStylePathItem.Text = "JavaScript style";
            // 
            // PythonStylePathItem
            // 
            this.PythonStylePathItem.Name = "PythonStylePathItem";
            this.PythonStylePathItem.Size = new System.Drawing.Size(268, 26);
            this.PythonStylePathItem.Text = "Python style";
            // 
            // RemesPathStylePathItem
            // 
            this.RemesPathStylePathItem.Name = "RemesPathStylePathItem";
            this.RemesPathStylePathItem.Size = new System.Drawing.Size(268, 26);
            this.RemesPathStylePathItem.Text = "RemesPath style";
            // 
            // ToggleSubtreesItem
            // 
            this.ToggleSubtreesItem.Name = "ToggleSubtreesItem";
            this.ToggleSubtreesItem.Size = new System.Drawing.Size(267, 24);
            this.ToggleSubtreesItem.Text = "Expand/collapse all subtrees";
            // 
            // SelectThisItem
            // 
            this.SelectThisItem.Name = "SelectThisItem";
            this.SelectThisItem.Size = new System.Drawing.Size(267, 24);
            this.SelectThisItem.Text = "Select this";
            // 
            // OpenSortFormItem
            // 
            this.OpenSortFormItem.Name = "OpenSortFormItem";
            this.OpenSortFormItem.Size = new System.Drawing.Size(267, 24);
            this.OpenSortFormItem.Text = "Sort array...";
            this.OpenSortFormItem.Visible = false;
            // 
            // SelectAllChildrenItem
            // 
            this.SelectAllChildrenItem.Name = "SelectAllChildrenItem";
            this.SelectAllChildrenItem.Size = new System.Drawing.Size(267, 24);
            this.SelectAllChildrenItem.Text = "Select all children";
            this.SelectAllChildrenItem.Visible = false;
            // 
            // CurrentPathBox
            // 
            this.CurrentPathBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CurrentPathBox.Location = new System.Drawing.Point(124, 436);
            this.CurrentPathBox.Name = "CurrentPathBox";
            this.CurrentPathBox.ReadOnly = true;
            this.CurrentPathBox.Size = new System.Drawing.Size(337, 22);
            this.CurrentPathBox.TabIndex = 10;
            this.CurrentPathBox.TabStop = false;
            // 
            // RefreshButton
            // 
            this.RefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RefreshButton.AutoSize = true;
            this.RefreshButton.Location = new System.Drawing.Point(359, 36);
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(103, 27);
            this.RefreshButton.TabIndex = 4;
            this.RefreshButton.Text = "Refresh";
            this.RefreshButton.UseVisualStyleBackColor = true;
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            this.RefreshButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // FindReplaceButton
            // 
            this.FindReplaceButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.FindReplaceButton.Location = new System.Drawing.Point(4, 435);
            this.FindReplaceButton.Name = "FindReplaceButton";
            this.FindReplaceButton.Size = new System.Drawing.Size(114, 23);
            this.FindReplaceButton.TabIndex = 9;
            this.FindReplaceButton.Text = "Find/replace";
            this.FindReplaceButton.UseVisualStyleBackColor = true;
            this.FindReplaceButton.Click += new System.EventHandler(this.FindReplaceButton_Click);
            this.FindReplaceButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // DocumentTypeComboBox
            // 
            this.DocumentTypeComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DocumentTypeComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.DocumentTypeComboBox.FormattingEnabled = true;
            this.DocumentTypeComboBox.Items.AddRange(new object[] {
            "JSON mode",
            "JSONL mode",
            "INI mode",
            "REGEX mode"});
            this.DocumentTypeComboBox.Location = new System.Drawing.Point(220, 69);
            this.DocumentTypeComboBox.Name = "DocumentTypeComboBox";
            this.DocumentTypeComboBox.Size = new System.Drawing.Size(130, 24);
            this.DocumentTypeComboBox.TabIndex = 6;
            this.DocumentTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.DocumentTypeComboBox_SelectedIndexChanged);
            this.DocumentTypeComboBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.QueryBox_KeyPress);
            this.DocumentTypeComboBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // path_separatorStyleItem
            // 
            this.path_separatorStyleItem.Name = "path_separatorStyleItem";
            this.path_separatorStyleItem.Size = new System.Drawing.Size(268, 26);
            this.path_separatorStyleItem.Text = "Use path_separator setting";
            // 
            // path_separatorStylePathItem
            // 
            this.path_separatorStylePathItem.Name = "path_separatorStylePathItem";
            this.path_separatorStylePathItem.Size = new System.Drawing.Size(268, 26);
            this.path_separatorStylePathItem.Text = "Use path_separator setting";
            // 
            // TreeViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(471, 461);
            this.Controls.Add(this.QueryBox);
            this.Controls.Add(this.SubmitQueryButton);
            this.Controls.Add(this.QueryToCsvButton);
            this.Controls.Add(this.SaveQueryButton);
            this.Controls.Add(this.RefreshButton);
            this.Controls.Add(this.DocumentTypeComboBox);
            this.Controls.Add(this.Tree);
            this.Controls.Add(this.FindReplaceButton);
            this.Controls.Add(this.CurrentPathBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TreeViewer";
            this.Text = "TreeViewer";
            this.VisibleChanged += new System.EventHandler(this.TreeViewer_VisibleChanged);
            this.DoubleClick += new System.EventHandler(this.TreeViewer_DoubleClick);
            this.NodeRightClickMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ImageList TypeIconList;
        private System.Windows.Forms.Button SaveQueryButton;
        private System.Windows.Forms.Button QueryToCsvButton;
        private System.Windows.Forms.ContextMenuStrip NodeRightClickMenu;
        private System.Windows.Forms.ToolStripMenuItem CopyValueMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CopyKeyItem;
        private System.Windows.Forms.ToolStripMenuItem JavaScriptStyleItem;
        private System.Windows.Forms.ToolStripMenuItem PythonStyleItem;
        private System.Windows.Forms.ToolStripMenuItem RemesPathStyleItem;
        private System.Windows.Forms.ToolStripMenuItem CopyPathItem;
        private System.Windows.Forms.ToolStripMenuItem JavaScriptStylePathItem;
        private System.Windows.Forms.ToolStripMenuItem PythonStylePathItem;
        private System.Windows.Forms.ToolStripMenuItem RemesPathStylePathItem;
        private System.Windows.Forms.TextBox CurrentPathBox;
        private System.Windows.Forms.ToolStripMenuItem ToggleSubtreesItem;
        internal System.Windows.Forms.TextBox QueryBox;
        private System.Windows.Forms.Button FindReplaceButton;
        internal System.Windows.Forms.TreeView Tree;
        internal System.Windows.Forms.Button SubmitQueryButton;
        internal System.Windows.Forms.Button RefreshButton;
        private System.Windows.Forms.ToolStripMenuItem OpenSortFormItem;
        private System.Windows.Forms.ToolStripMenuItem SelectThisItem;
        private System.Windows.Forms.ToolStripMenuItem SelectAllChildrenItem;
        private System.Windows.Forms.ComboBox DocumentTypeComboBox;
        private System.Windows.Forms.ToolStripMenuItem path_separatorStyleItem;
        private System.Windows.Forms.ToolStripMenuItem path_separatorStylePathItem;
    }
}