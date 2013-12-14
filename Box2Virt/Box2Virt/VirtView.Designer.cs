namespace Box2Virt
{
    partial class VirtView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VirtView));
            this.lbMessages = new System.Windows.Forms.Label();
            this.cbDevice = new System.Windows.Forms.ComboBox();
            this.bDone = new System.Windows.Forms.Button();
            this.imgButtons = new System.Windows.Forms.ImageList(this.components);
            this.imgDeviceComm = new System.Windows.Forms.ImageList(this.components);
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.lDeviceCom = new System.Windows.Forms.ListView();
            this.lbCommands = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lbMessages
            // 
            this.lbMessages.AutoSize = true;
            this.lbMessages.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lbMessages.Location = new System.Drawing.Point(25, 319);
            this.lbMessages.Name = "lbMessages";
            this.lbMessages.Size = new System.Drawing.Size(190, 15);
            this.lbMessages.TabIndex = 1;
            this.lbMessages.Text = "Searching for 2Virt devices...";
            // 
            // cbDevice
            // 
            this.cbDevice.BackColor = System.Drawing.Color.Honeydew;
            this.cbDevice.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.cbDevice.ForeColor = System.Drawing.SystemColors.InfoText;
            this.cbDevice.FormattingEnabled = true;
            this.cbDevice.Location = new System.Drawing.Point(119, 4);
            this.cbDevice.Name = "cbDevice";
            this.cbDevice.Size = new System.Drawing.Size(251, 24);
            this.cbDevice.TabIndex = 4;
            this.cbDevice.SelectedIndexChanged += new System.EventHandler(this.cbDevice_SelectedIndexChanged);
            // 
            // bDone
            // 
            this.bDone.Cursor = System.Windows.Forms.Cursors.Hand;
            this.bDone.FlatAppearance.BorderColor = System.Drawing.SystemColors.ButtonFace;
            this.bDone.FlatAppearance.BorderSize = 0;
            this.bDone.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.bDone.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.bDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bDone.ImageIndex = 0;
            this.bDone.ImageList = this.imgButtons;
            this.bDone.Location = new System.Drawing.Point(299, 302);
            this.bDone.Name = "bDone";
            this.bDone.Size = new System.Drawing.Size(70, 51);
            this.bDone.TabIndex = 5;
            this.bDone.UseVisualStyleBackColor = true;
            this.bDone.Click += new System.EventHandler(this.bDone_Click);
            // 
            // imgButtons
            // 
            this.imgButtons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgButtons.ImageStream")));
            this.imgButtons.TransparentColor = System.Drawing.Color.Transparent;
            this.imgButtons.Images.SetKeyName(0, "Close.ico");
            // 
            // imgDeviceComm
            // 
            this.imgDeviceComm.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgDeviceComm.ImageStream")));
            this.imgDeviceComm.TransparentColor = System.Drawing.Color.Transparent;
            this.imgDeviceComm.Images.SetKeyName(0, "Device Capabilities.ico");
            this.imgDeviceComm.Images.SetKeyName(1, "Device Info.ico");
            this.imgDeviceComm.Images.SetKeyName(2, "Web Service.ico");
            this.imgDeviceComm.Images.SetKeyName(3, "WebMethod.ico");
            this.imgDeviceComm.Images.SetKeyName(4, "UpParent.ico");
            // 
            // trayIcon
            // 
            this.trayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("trayIcon.Icon")));
            this.trayIcon.Text = "2Virt";
            this.trayIcon.Visible = true;
            // 
            // lDeviceCom
            // 
            this.lDeviceCom.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lDeviceCom.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.lDeviceCom.Location = new System.Drawing.Point(12, 34);
            this.lDeviceCom.Name = "lDeviceCom";
            this.lDeviceCom.Size = new System.Drawing.Size(357, 266);
            this.lDeviceCom.TabIndex = 6;
            this.lDeviceCom.UseCompatibleStateImageBehavior = false;
            this.lDeviceCom.DoubleClick += new System.EventHandler(this.lDeviceCom_DoubleClick);
            // 
            // lbCommands
            // 
            this.lbCommands.AutoSize = true;
            this.lbCommands.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lbCommands.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lbCommands.Location = new System.Drawing.Point(10, 8);
            this.lbCommands.Name = "lbCommands";
            this.lbCommands.Size = new System.Drawing.Size(103, 16);
            this.lbCommands.TabIndex = 7;
            this.lbCommands.Text = "Online Devices";
            // 
            // VirtView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(380, 374);
            this.ControlBox = false;
            this.Controls.Add(this.lbCommands);
            this.Controls.Add(this.lDeviceCom);
            this.Controls.Add(this.bDone);
            this.Controls.Add(this.cbDevice);
            this.Controls.Add(this.lbMessages);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VirtView";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Box2Virt";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.Load += new System.EventHandler(this.VirtView_Load);
            this.Move += new System.EventHandler(this.VirtView_Move);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbMessages;
        private System.Windows.Forms.ComboBox cbDevice;
        private System.Windows.Forms.Button bDone;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ListView lDeviceCom;
        private System.Windows.Forms.Label lbCommands;
        private System.Windows.Forms.ImageList imgDeviceComm;
        private System.Windows.Forms.ImageList imgButtons;
    }
}

