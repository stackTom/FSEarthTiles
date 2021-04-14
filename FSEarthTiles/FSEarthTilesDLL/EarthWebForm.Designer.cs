namespace FSEarthTilesDLL
{
    partial class EarthWebForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EarthWebForm));
            this.WebBrowser = new System.Windows.Forms.WebBrowser();
            this.WebAddressBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // WebBrowser
            // 
            this.WebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WebBrowser.Location = new System.Drawing.Point(0, 0);
            this.WebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.WebBrowser.Name = "WebBrowser";
            this.WebBrowser.Size = new System.Drawing.Size(302, 294);
            this.WebBrowser.TabIndex = 0;
            this.WebBrowser.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(this.EarthWebBrowserBox_Navigated);
            this.WebBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.EarthWebBrowserBox_Navigating);
            this.WebBrowser.FileDownload += new System.EventHandler(this.EarthWebBrowserBox_FileDownload);
            this.WebBrowser.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.EarthWebBrowserBox_DocumentCompleted);
            // 
            // WebAddressBox
            // 
            this.WebAddressBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.WebAddressBox.Location = new System.Drawing.Point(0, 0);
            this.WebAddressBox.Name = "WebAddressBox";
            this.WebAddressBox.Size = new System.Drawing.Size(302, 22);
            this.WebAddressBox.TabIndex = 2;
            this.WebAddressBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.WebAddressBox_KeyUp);
            this.WebAddressBox.TextChanged += new System.EventHandler(this.WebAddressBox_TextChanged);
            // 
            // EarthWebForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(302, 294);
            this.Controls.Add(this.WebAddressBox);
            this.Controls.Add(this.WebBrowser);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(310, 328);
            this.Name = "EarthWebForm";
            this.Text = "World Wide Web   (FSEarthTiles)";
            this.Shown += new System.EventHandler(this.EarthWebForm_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EarthWebForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.WebBrowser WebBrowser;
        private System.Windows.Forms.TextBox WebAddressBox;
    }
}