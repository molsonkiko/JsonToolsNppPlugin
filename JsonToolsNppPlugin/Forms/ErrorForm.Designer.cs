namespace JSON_Tools.Forms
{
    partial class ErrorForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorForm));
            this.ErrorGrid = new System.Windows.Forms.DataGridView();
            this.Severity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Position = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.errorGridRightClickStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.refreshMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToJsonMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.ErrorGrid)).BeginInit();
            this.errorGridRightClickStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // ErrorGrid
            // 
            this.ErrorGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ErrorGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ErrorGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Severity,
            this.Description,
            this.Position});
            this.ErrorGrid.Location = new System.Drawing.Point(4, 4);
            this.ErrorGrid.Name = "ErrorGrid";
            this.ErrorGrid.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.ErrorGrid.RowTemplate.Height = 24;
            this.ErrorGrid.Size = new System.Drawing.Size(771, 277);
            this.ErrorGrid.TabIndex = 0;
            this.ErrorGrid.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.ErrorGrid_CellEnter);
            this.ErrorGrid.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.ErrorGrid_CellEnter);
            this.ErrorGrid.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ErrorForm_KeyUp);
            this.ErrorGrid.MouseClick += new System.Windows.Forms.MouseEventHandler(this.ErrorForm_RightClick);
            this.ErrorGrid.Resize += new System.EventHandler(this.ErrorGrid_Resize);
            // 
            // Severity
            // 
            this.Severity.HeaderText = "Severity";
            this.Severity.MinimumWidth = 6;
            this.Severity.Name = "Severity";
            this.Severity.ReadOnly = true;
            this.Severity.Width = 75;
            // 
            // Description
            // 
            this.Description.HeaderText = "Description";
            this.Description.MinimumWidth = 6;
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            this.Description.Width = 125;
            // 
            // Position
            // 
            this.Position.HeaderText = "Position";
            this.Position.MinimumWidth = 6;
            this.Position.Name = "Position";
            this.Position.ReadOnly = true;
            this.Position.Width = 75;
            // 
            // errorGridRightClickStrip
            // 
            this.errorGridRightClickStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.errorGridRightClickStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshMenuItem,
            this.exportToJsonMenuItem});
            this.errorGridRightClickStrip.Name = "errorGridRightClickStrip";
            this.errorGridRightClickStrip.Size = new System.Drawing.Size(252, 80);
            // 
            // refreshMenuItem
            // 
            this.refreshMenuItem.Name = "refreshMenuItem";
            this.refreshMenuItem.Size = new System.Drawing.Size(251, 24);
            this.refreshMenuItem.Text = "Refresh with current errors";
            this.refreshMenuItem.Click += new System.EventHandler(this.RefreshMenuItem_Click);
            // 
            // exportToJsonMenuItem
            // 
            this.exportToJsonMenuItem.Name = "exportToJsonMenuItem";
            this.exportToJsonMenuItem.Size = new System.Drawing.Size(190, 24);
            this.exportToJsonMenuItem.Text = "Export to JSON";
            this.exportToJsonMenuItem.Click += new System.EventHandler(this.ExportLintsToJsonMenuItem_Click);
            // 
            // ErrorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(780, 284);
            this.Controls.Add(this.ErrorGrid);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ErrorForm";
            this.Text = "Syntax errors in JSON";
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ErrorForm_KeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.ErrorGrid)).EndInit();
            this.errorGridRightClickStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.DataGridViewTextBoxColumn Severity;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.DataGridViewTextBoxColumn Position;
        private System.Windows.Forms.DataGridView ErrorGrid;
        private System.Windows.Forms.ContextMenuStrip errorGridRightClickStrip;
        private System.Windows.Forms.ToolStripMenuItem refreshMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToJsonMenuItem;
    }
}