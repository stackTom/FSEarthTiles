namespace FSEarthMasksDLL
{
    partial class FSEarthMasksForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FSEarthMasksForm));
            this.StatusBox = new System.Windows.Forms.RichTextBox();
            this.BitmapBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.BitmapBox)).BeginInit();
            this.SuspendLayout();
            // 
            // StatusBox
            // 
            this.StatusBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.StatusBox.Location = new System.Drawing.Point(0, 237);
            this.StatusBox.Name = "StatusBox";
            this.StatusBox.ReadOnly = true;
            this.StatusBox.Size = new System.Drawing.Size(292, 29);
            this.StatusBox.TabIndex = 0;
            this.StatusBox.Text = "";
            // 
            // BitmapBox
            // 
            this.BitmapBox.Image = ((System.Drawing.Image)(resources.GetObject("BitmapBox.Image")));
            this.BitmapBox.Location = new System.Drawing.Point(17, -11);
            this.BitmapBox.Name = "BitmapBox";
            this.BitmapBox.Size = new System.Drawing.Size(256, 256);
            this.BitmapBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.BitmapBox.TabIndex = 1;
            this.BitmapBox.TabStop = false;
            // 
            // FSEarthMasksForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.StatusBox);
            this.Controls.Add(this.BitmapBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(300, 300);
            this.Name = "FSEarthMasksForm";
            this.Text = "FS Earth Masks";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FSEarthMasksForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.BitmapBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox StatusBox;
        private System.Windows.Forms.PictureBox BitmapBox;
    }
}

