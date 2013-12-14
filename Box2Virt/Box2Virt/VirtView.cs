using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using System.Reflection;
using _2Virt;
using System.Threading;

namespace Box2Virt
{
    public partial class VirtView : Form
    {
        private DeviceCommand currentDevice;

        private CommandView currentCommandView;

        private Thread checkUsbThread;

        public VirtView()
        {
            InitializeComponent();
            currentDevice = null;
            currentCommandView = null;
        }

        private void CloseCommandView()
        {
            if (currentCommandView != null)
            {
                try {
                    currentCommandView.Close();
                } catch {}
            }
            currentCommandView = null;
        }

        private void bDone_Click(object sender, EventArgs e)
        {
            CloseCommandView();
            this.Hide();
        }

        private void VirtView_Load(object sender, EventArgs e)
        {
            trayIcon.DoubleClick += new EventHandler(trayIcon_DoubleClick);

            //context menu
            MenuItem exitItem = new MenuItem();
            exitItem.Text = "Exit";
            exitItem.Click += new EventHandler(exitTray_Click);

            MenuItem aboutItem = new MenuItem();
            aboutItem.Text = "About...";
            aboutItem.Click += new EventHandler(aboutTray_Click);

            MenuItem restoreItem = new MenuItem();
            restoreItem.Text = "Restore";
            restoreItem.DefaultItem = true;
            restoreItem.Click += new EventHandler(restoreTray_Click);

            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(restoreItem);
            contextMenu.MenuItems.Add(aboutItem);
            contextMenu.MenuItems.Add(exitItem);

            trayIcon.ContextMenu = contextMenu;

            // Register our Axis Server Parser
            WebServer.RegisterServer(new AxisWebServer("Axis"));

            // now check for 2Virt devices
            UsbManager.UsbNotify += new UsbManager.UsbDeviceEvent(UsbReceiver);
            checkUsbThread = new Thread(new ThreadStart(CheckSystemForDevices));
            checkUsbThread.Start();
            
            // add a device command change event
            DeviceCommand.DeviceEvent += new DeviceCommand.DeviceCommandEvent(DeviceCommandChanged);
        }

        private void CheckSystemForDevices()
        {
            while (true)
            {
                string oldText  = lbMessages.Text;
                ChangeMessage("Searching for 2Virt devices...");
                Thread.Sleep(2000);
                UsbManager.CheckForDevice(Properties.Resources.VendorID_2Virt, Properties.Resources.ProductID_2Virt, Properties.Resources.GUID_2Virt);
                Thread.Sleep(1000);
                ChangeMessage(oldText);
                Thread.Sleep(17000);
            }
        }

        internal delegate void ChangeMessageDelegate(string message);
        private void ChangeMessage(string message)
        {
            if (lbMessages.InvokeRequired)
                lbMessages.Invoke(new ChangeMessageDelegate(ChangeMessage), message);
            else
                lbMessages.Text = message;
        }

        internal delegate void AddListItemDelegate(object item, bool add);
        private void AddListItem(object item, bool add)
        {
            if (cbDevice.InvokeRequired)
            {
                cbDevice.Invoke(new AddListItemDelegate(AddListItem), new object[2] { item, add });
            }
            else
            {
                if (add)
                    cbDevice.Items.Add(item);
                else
                    cbDevice.Items.Remove(item);
            }
        }

        private void UsbReceiver(IUsbDevice device, bool add)
        {
            if (add)
            {
                currentDevice = new DeviceCommand(device);
                AddListItem(device, true);
                ChangeMessage("Found " + device.DeviceName + " on your system.");
            }
            else
            {
                DeviceCommand.RemoveDeviceCommand(device);
                AddListItem(device, false);
                ChangeMessage(device.DeviceName + " has been removed from your system.");
            }
        }


        private void DeviceCommandChanged(DeviceCommand newDevice, string message)
        {
            if (message != null)
            {
                ChangeMessage(message);
            }
            DeviceCommand.Command[] commands = newDevice.GetDeviceCommands();
            UpdateCommandList(commands);
            currentDevice = newDevice;
        }

        private void restoreTray_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.Show();
        }

        private void aboutTray_Click(object sender, EventArgs e)
        {
            AboutBox2Virt about = new AboutBox2Virt();
            about.ShowDialog();
        }

        private void exitTray_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            this.restoreTray_Click(sender, e);
        }

        private void cbDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            IUsbDevice dev = (IUsbDevice)cbDevice.SelectedItem;

            UpdateCommands(dev);
        }

        private void UpdateCommands(IUsbDevice dev)
        {
            DeviceCommand.Command[] commands = DeviceCommand.GetDeviceCommands(dev);
            UpdateCommandList(commands);
        }

        private void UpdateCommandList(DeviceCommand.Command[] commands)
        {
            // Suspending automatic refreshes as items are added/removed.
            lDeviceCom.BeginUpdate();

            lDeviceCom.Items.Clear();
            lDeviceCom.View = View.LargeIcon;
            lDeviceCom.LargeImageList = imgDeviceComm;

            try
            {
                foreach (DeviceCommand.Command comm in commands)
                {
                    ListViewItem listItem = new ListViewItem(comm.DisplayName);
                    listItem.ImageIndex = comm.ImageIndex;

                    lDeviceCom.Items.Add(listItem);
                }
            }
            catch (System.NullReferenceException)
            { }

            // Re-enable the display.
            lDeviceCom.EndUpdate();
        }


        private void lDeviceCom_DoubleClick(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection items = lDeviceCom.SelectedItems;
            foreach (ListViewItem item in items)
            {
                DeviceCommand commandDev = currentDevice;
                CloseCommandView();
                commandDev.Logger.Clear();

                DeviceCommand.Command command = commandDev.GetCommand(item.Text);

                currentCommandView = new CommandView();
                bool hasCommands = command is DeviceCommand.ParamCommand;
                if (hasCommands)
                {
                    hasCommands = currentCommandView.DisplayCommand((DeviceCommand.ParamCommand)command, commandDev);
                }
                if (!hasCommands)
                {
                    IoStatus status = command.Execute(commandDev);
                    if ((status.error == USBError.SUCCESS) && (status.size == 0))
                        continue;
                    currentCommandView = new CommandView();
                    currentCommandView.DisplayCommand(status, commandDev.Logger.ToArray());
                }

                currentCommandView.Location = new Point(this.Location.X + this.RestoreBounds.Width + 10,
                    this.Location.Y - (currentCommandView.Height - this.Height) / 2);
                currentCommandView.Show();
            }
            DeviceCommandChanged(currentDevice, null);
        }

        private void VirtView_Move(object sender, EventArgs e)
        {
            if (currentCommandView != null)
            {
                currentCommandView.Location = new Point(this.Location.X + this.RestoreBounds.Width + 10,
                    this.Location.Y - (currentCommandView.Height - this.Height) / 2);
                currentCommandView.Refresh();
            }
        }
    }

}
