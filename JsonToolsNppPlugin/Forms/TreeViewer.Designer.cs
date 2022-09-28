﻿namespace JSON_Tools.Forms
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
            this.FullTreeCheckBox = new System.Windows.Forms.CheckBox();
            this.NodeRightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CopyValueMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CopyKeyItem = new System.Windows.Forms.ToolStripMenuItem();
            this.JavaScriptStyleItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PythonStyleItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemesPathStyleItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CopyPathItem = new System.Windows.Forms.ToolStripMenuItem();
            this.JavaScriptStyleKeyItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PythonStylePathItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RemesPathStylePathItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToggleSubtreesItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CurrentPathBox = new System.Windows.Forms.TextBox();
            this.CurrentPathButton = new System.Windows.Forms.Button();
            this.RefreshButton = new System.Windows.Forms.Button();
            this.TheAcceptButton = new System.Windows.Forms.Button();
            this.TheCancelButton = new System.Windows.Forms.Button();
            this.NodeRightClickMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // Tree
            // 
            this.Tree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Tree.Location = new System.Drawing.Point(4, 91);
            this.Tree.BackColor = System.Drawing.SystemColors.WindowFrame;
            this.Tree.Name = "Tree";
            this.Tree.Size = new System.Drawing.Size(398, 339);
            this.Tree.TabIndex = 6;
            this.Tree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.Tree_BeforeExpand);
            this.Tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.Tree_AfterSelect);
            this.Tree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.Tree_NodeMouseClick);
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
            // 
            // QueryBox
            // 
            this.QueryBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.QueryBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.QueryBox.Location = new System.Drawing.Point(4, 4);
            this.QueryBox.Multiline = true;
            this.QueryBox.Name = "QueryBox";
            this.QueryBox.Size = new System.Drawing.Size(164, 74);
            this.QueryBox.TabIndex = 0;
            this.QueryBox.Text = "@";
            this.QueryBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.QueryBox_KeyPress);
            this.QueryBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // SubmitQueryButton
            // 
            this.SubmitQueryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SubmitQueryButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubmitQueryButton.Location = new System.Drawing.Point(174, 4);
            this.SubmitQueryButton.Name = "SubmitQueryButton";
            this.SubmitQueryButton.Size = new System.Drawing.Size(119, 26);
            this.SubmitQueryButton.TabIndex = 1;
            this.SubmitQueryButton.Text = "Submit query";
            this.SubmitQueryButton.UseVisualStyleBackColor = true;
            this.SubmitQueryButton.Click += new System.EventHandler(this.SubmitQueryButton_Click);
            this.SubmitQueryButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // SaveQueryButton
            // 
            this.SaveQueryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveQueryButton.Location = new System.Drawing.Point(174, 36);
            this.SaveQueryButton.Name = "SaveQueryButton";
            this.SaveQueryButton.Size = new System.Drawing.Size(119, 27);
            this.SaveQueryButton.TabIndex = 3;
            this.SaveQueryButton.Text = "Save query result";
            this.SaveQueryButton.UseVisualStyleBackColor = true;
            this.SaveQueryButton.Click += new System.EventHandler(this.SaveQueryResultButton_Click);
            this.SaveQueryButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // QueryToCsvButton
            // 
            this.QueryToCsvButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.QueryToCsvButton.Location = new System.Drawing.Point(299, 4);
            this.QueryToCsvButton.Name = "QueryToCsvButton";
            this.QueryToCsvButton.Size = new System.Drawing.Size(103, 26);
            this.QueryToCsvButton.TabIndex = 2;
            this.QueryToCsvButton.Text = "Query to CSV";
            this.QueryToCsvButton.UseVisualStyleBackColor = true;
            this.QueryToCsvButton.Click += new System.EventHandler(this.QueryToCsvButton_Click);
            this.QueryToCsvButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // FullTreeCheckBox
            // 
            this.FullTreeCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.FullTreeCheckBox.AutoSize = true;
            this.FullTreeCheckBox.Location = new System.Drawing.Point(174, 69);
            this.FullTreeCheckBox.Name = "FullTreeCheckBox";
            this.FullTreeCheckBox.Size = new System.Drawing.Size(130, 20);
            this.FullTreeCheckBox.TabIndex = 5;
            this.FullTreeCheckBox.Text = "View all subtrees";
            this.FullTreeCheckBox.UseVisualStyleBackColor = true;
            this.FullTreeCheckBox.CheckedChanged += new System.EventHandler(this.FullTreeCheckBox_CheckedChanged);
            this.FullTreeCheckBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // NodeRightClickMenu
            // 
            this.NodeRightClickMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.NodeRightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyValueMenuItem,
            this.CopyKeyItem,
            this.CopyPathItem,
            this.ToggleSubtreesItem});
            this.NodeRightClickMenu.Name = "NodeRightClickMenu";
            this.NodeRightClickMenu.Size = new System.Drawing.Size(268, 100);
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
            this.RemesPathStyleItem});
            this.CopyKeyItem.Name = "CopyKeyItem";
            this.CopyKeyItem.Size = new System.Drawing.Size(267, 24);
            this.CopyKeyItem.Text = "Key/index to clipboard";
            // 
            // JavaScriptStyleItem
            // 
            this.JavaScriptStyleItem.Name = "JavaScriptStyleItem";
            this.JavaScriptStyleItem.Size = new System.Drawing.Size(198, 26);
            this.JavaScriptStyleItem.Text = "JavaScript style";
            // 
            // PythonStyleItem
            // 
            this.PythonStyleItem.Name = "PythonStyleItem";
            this.PythonStyleItem.Size = new System.Drawing.Size(198, 26);
            this.PythonStyleItem.Text = "Python style";
            // 
            // RemesPathStyleItem
            // 
            this.RemesPathStyleItem.Name = "RemesPathStyleItem";
            this.RemesPathStyleItem.Size = new System.Drawing.Size(198, 26);
            this.RemesPathStyleItem.Text = "RemesPath style";
            // 
            // CopyPathItem
            // 
            this.CopyPathItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.JavaScriptStyleKeyItem,
            this.PythonStylePathItem,
            this.RemesPathStylePathItem});
            this.CopyPathItem.Name = "CopyPathItem";
            this.CopyPathItem.Size = new System.Drawing.Size(267, 24);
            this.CopyPathItem.Text = "Path to clipboard";
            // 
            // JavaScriptStyleKeyItem
            // 
            this.JavaScriptStyleKeyItem.Name = "JavaScriptStyleKeyItem";
            this.JavaScriptStyleKeyItem.Size = new System.Drawing.Size(198, 26);
            this.JavaScriptStyleKeyItem.Text = "JavaScript style";
            // 
            // PythonStylePathItem
            // 
            this.PythonStylePathItem.Name = "PythonStylePathItem";
            this.PythonStylePathItem.Size = new System.Drawing.Size(198, 26);
            this.PythonStylePathItem.Text = "Python style";
            // 
            // RemesPathStylePathItem
            // 
            this.RemesPathStylePathItem.Name = "RemesPathStylePathItem";
            this.RemesPathStylePathItem.Size = new System.Drawing.Size(198, 26);
            this.RemesPathStylePathItem.Text = "RemesPath style";
            // 
            // ToggleSubtreesItem
            // 
            this.ToggleSubtreesItem.Name = "ToggleSubtreesItem";
            this.ToggleSubtreesItem.Size = new System.Drawing.Size(267, 24);
            this.ToggleSubtreesItem.Text = "Expand/collapse all subtrees";
            // 
            // CurrentPathBox
            // 
            this.CurrentPathBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CurrentPathBox.Location = new System.Drawing.Point(103, 436);
            this.CurrentPathBox.Name = "CurrentPathBox";
            this.CurrentPathBox.ReadOnly = true;
            this.CurrentPathBox.Size = new System.Drawing.Size(299, 22);
            this.CurrentPathBox.TabIndex = 8;
            this.CurrentPathBox.TabStop = false;
            // 
            // CurrentPathButton
            // 
            this.CurrentPathButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CurrentPathButton.Location = new System.Drawing.Point(4, 435);
            this.CurrentPathButton.Name = "CurrentPathButton";
            this.CurrentPathButton.Size = new System.Drawing.Size(93, 23);
            this.CurrentPathButton.TabIndex = 7;
            this.CurrentPathButton.Text = "Current path";
            this.CurrentPathButton.UseVisualStyleBackColor = true;
            this.CurrentPathButton.Click += new System.EventHandler(this.CurrentPathButton_Click);
            this.CurrentPathButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // RefreshButton
            // 
            this.RefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RefreshButton.Location = new System.Drawing.Point(299, 36);
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(103, 27);
            this.RefreshButton.TabIndex = 4;
            this.RefreshButton.Text = "Refresh";
            this.RefreshButton.UseVisualStyleBackColor = true;
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            this.RefreshButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeViewer_KeyUp);
            // 
            // TheAcceptButton
            // 
            this.TheAcceptButton.Location = new System.Drawing.Point(325, 435);
            this.TheAcceptButton.Name = "TheAcceptButton";
            this.TheAcceptButton.Size = new System.Drawing.Size(75, 23);
            this.TheAcceptButton.TabIndex = 9;
            this.TheAcceptButton.TabStop = false;
            this.TheAcceptButton.Text = "Accept";
            this.TheAcceptButton.UseVisualStyleBackColor = true;
            this.TheAcceptButton.Visible = false;
            // 
            // TheCancelButton
            // 
            this.TheCancelButton.Location = new System.Drawing.Point(244, 435);
            this.TheCancelButton.Name = "TheCancelButton";
            this.TheCancelButton.Size = new System.Drawing.Size(75, 23);
            this.TheCancelButton.TabIndex = 10;
            this.TheCancelButton.TabStop = false;
            this.TheCancelButton.Text = "Cancel";
            this.TheCancelButton.UseVisualStyleBackColor = true;
            this.TheCancelButton.Visible = false;
            // 
            // TreeViewer
            // 
            this.AcceptButton = this.TheAcceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.TheCancelButton;
            this.ClientSize = new System.Drawing.Size(412, 461);
            this.Controls.Add(this.TheCancelButton);
            this.Controls.Add(this.TheAcceptButton);
            this.Controls.Add(this.RefreshButton);
            this.Controls.Add(this.CurrentPathButton);
            this.Controls.Add(this.CurrentPathBox);
            this.Controls.Add(this.FullTreeCheckBox);
            this.Controls.Add(this.QueryToCsvButton);
            this.Controls.Add(this.SaveQueryButton);
            this.Controls.Add(this.SubmitQueryButton);
            this.Controls.Add(this.QueryBox);
            this.Controls.Add(this.Tree);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TreeViewer";
            this.Text = "TreeViewer";
            this.NodeRightClickMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView Tree;
        private System.Windows.Forms.ImageList TypeIconList;
        private System.Windows.Forms.Button SubmitQueryButton;
        private System.Windows.Forms.Button SaveQueryButton;
        private System.Windows.Forms.Button QueryToCsvButton;
        private System.Windows.Forms.CheckBox FullTreeCheckBox;
        private System.Windows.Forms.ContextMenuStrip NodeRightClickMenu;
        private System.Windows.Forms.ToolStripMenuItem CopyValueMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CopyKeyItem;
        private System.Windows.Forms.ToolStripMenuItem JavaScriptStyleItem;
        private System.Windows.Forms.ToolStripMenuItem PythonStyleItem;
        private System.Windows.Forms.ToolStripMenuItem RemesPathStyleItem;
        private System.Windows.Forms.ToolStripMenuItem CopyPathItem;
        private System.Windows.Forms.ToolStripMenuItem JavaScriptStyleKeyItem;
        private System.Windows.Forms.ToolStripMenuItem PythonStylePathItem;
        private System.Windows.Forms.ToolStripMenuItem RemesPathStylePathItem;
        private System.Windows.Forms.TextBox CurrentPathBox;
        private System.Windows.Forms.Button CurrentPathButton;
        private System.Windows.Forms.ToolStripMenuItem ToggleSubtreesItem;
        private System.Windows.Forms.Button RefreshButton;
        internal System.Windows.Forms.TextBox QueryBox;
        private System.Windows.Forms.Button TheAcceptButton;
        private System.Windows.Forms.Button TheCancelButton;
    }
}