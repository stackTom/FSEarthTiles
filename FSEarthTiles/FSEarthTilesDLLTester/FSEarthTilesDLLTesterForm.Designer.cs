namespace FSEarthTilesDLLTester
{
    partial class FSEarthTilesDLLTesterForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.StartFSEarthTiles = new System.Windows.Forms.Button();
            this.StartDownLoadButton = new System.Windows.Forms.Button();
            this.StopFSEarthTilesButton = new System.Windows.Forms.Button();
            this.AbortDownloadButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // StartFSEarthTiles
            // 
            this.StartFSEarthTiles.BackColor = System.Drawing.Color.LightBlue;
            this.StartFSEarthTiles.Location = new System.Drawing.Point(90, 31);
            this.StartFSEarthTiles.Name = "StartFSEarthTiles";
            this.StartFSEarthTiles.Size = new System.Drawing.Size(112, 46);
            this.StartFSEarthTiles.TabIndex = 0;
            this.StartFSEarthTiles.Text = "Start \r\nFS Earth Tiles\r\n";
            this.StartFSEarthTiles.UseVisualStyleBackColor = false;
            this.StartFSEarthTiles.Click += new System.EventHandler(this.StartFSEarthTiles_Click);
            // 
            // StartDownLoadButton
            // 
            this.StartDownLoadButton.BackColor = System.Drawing.Color.LightBlue;
            this.StartDownLoadButton.Location = new System.Drawing.Point(22, 92);
            this.StartDownLoadButton.Name = "StartDownLoadButton";
            this.StartDownLoadButton.Size = new System.Drawing.Size(113, 46);
            this.StartDownLoadButton.TabIndex = 1;
            this.StartDownLoadButton.Text = "  Start\r\nDownload";
            this.StartDownLoadButton.UseVisualStyleBackColor = false;
            this.StartDownLoadButton.Click += new System.EventHandler(this.StartDownLoadButton_Click);
            // 
            // StopFSEarthTilesButton
            // 
            this.StopFSEarthTilesButton.BackColor = System.Drawing.Color.LightBlue;
            this.StopFSEarthTilesButton.Location = new System.Drawing.Point(88, 160);
            this.StopFSEarthTilesButton.Name = "StopFSEarthTilesButton";
            this.StopFSEarthTilesButton.Size = new System.Drawing.Size(114, 45);
            this.StopFSEarthTilesButton.TabIndex = 2;
            this.StopFSEarthTilesButton.Text = "Stop\r\nFS Earth Tiles";
            this.StopFSEarthTilesButton.UseVisualStyleBackColor = false;
            this.StopFSEarthTilesButton.Click += new System.EventHandler(this.StopFSEarthTilesButton_Click);
            // 
            // AbortDownloadButton
            // 
            this.AbortDownloadButton.BackColor = System.Drawing.Color.LightBlue;
            this.AbortDownloadButton.Location = new System.Drawing.Point(150, 93);
            this.AbortDownloadButton.Name = "AbortDownloadButton";
            this.AbortDownloadButton.Size = new System.Drawing.Size(115, 45);
            this.AbortDownloadButton.TabIndex = 3;
            this.AbortDownloadButton.Text = "Abort\r\nDownload";
            this.AbortDownloadButton.UseVisualStyleBackColor = false;
            this.AbortDownloadButton.Click += new System.EventHandler(this.AbortDownloadButton_Click);
            // 
            // FSEarthTilesDLLTesterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.AbortDownloadButton);
            this.Controls.Add(this.StopFSEarthTilesButton);
            this.Controls.Add(this.StartDownLoadButton);
            this.Controls.Add(this.StartFSEarthTiles);
            this.Name = "FSEarthTilesDLLTesterForm";
            this.Text = "FSEarthTilesDLLTester";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FSEarthTilesDLLTesterForm_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button StartFSEarthTiles;
        private System.Windows.Forms.Button StartDownLoadButton;
        private System.Windows.Forms.Button StopFSEarthTilesButton;
        private System.Windows.Forms.Button AbortDownloadButton;
    }
}

