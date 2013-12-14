namespace Box2Virt
{
    partial class CommandView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CommandView));
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.bBack = new System.Windows.Forms.Button();
            this.imgButton = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // webBrowser
            // 
            this.webBrowser.Location = new System.Drawing.Point(-2, 1);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(429, 546);
            this.webBrowser.TabIndex = 0;
            // 
            // bBack
            // 
            this.bBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this.bBack.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.bBack.FlatAppearance.BorderSize = 0;
            this.bBack.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.bBack.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.bBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bBack.ImageIndex = 0;
            this.bBack.ImageList = this.imgButton;
            this.bBack.Location = new System.Drawing.Point(343, 553);
            this.bBack.Name = "bBack";
            this.bBack.Size = new System.Drawing.Size(75, 51);
            this.bBack.TabIndex = 1;
            this.bBack.UseVisualStyleBackColor = true;
            this.bBack.Click += new System.EventHandler(this.bBack_Click);
            // 
            // imgButton
            // 
            this.imgButton.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgButton.ImageStream")));
            this.imgButton.TransparentColor = System.Drawing.Color.Transparent;
            this.imgButton.Images.SetKeyName(0, "Back.ico");
            // 
            // CommandView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(426, 612);
            this.ControlBox = false;
            this.Controls.Add(this.bBack);
            this.Controls.Add(this.webBrowser);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CommandView";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "2Virt Device Command";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser;
        private System.Windows.Forms.Button bBack;
        private System.Windows.Forms.ImageList imgButton;
    }
}