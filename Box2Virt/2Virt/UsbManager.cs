using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _2Virt
{
    public class UsbManager
    {
        public delegate void UsbDeviceEvent(IUsbDevice device, bool add);

        public static event UsbDeviceEvent UsbNotify;

        public class UsbIdentifier
        {
            private string vendorId;
            private string productId;
            private string classGuid;

            public string Identifier { get { return "VID_" + vendorId + "&PID_" + productId; } }

            public string Guid { get { return classGuid; } }

            internal UsbIdentifier(string vid, string pid, string guid)
            {
                vendorId  = vid;
                productId = pid;
                classGuid = guid;
                lock(devicesId)
                {
                    devicesId.Add(this);
                }

            }

            public static void GetUsbIdentifier(string vid, string pid, string guid)
            {
                bool found = false;
                lock(devicesId)
                {
                    // check the list for the device identifier
                    foreach (UsbIdentifier usbId in devicesId)
                    {
                        if ((usbId.vendorId == vid) && (usbId.productId == pid))
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    new UsbIdentifier(vid, pid, guid);

                // Now check your system for the devices based on the added indentifiers
                CheckSystem(devicesId);
            }

            private static List<UsbIdentifier> devicesId = new List<UsbIdentifier>(); // vid_{vendor id)&pid_{product id}
        }

        public static void CheckForDevice(string vid, string pid, string classGuid)
        {
            UsbIdentifier.GetUsbIdentifier(vid, pid, classGuid);
        }

        private static List<IUsbDevice> devices = new List<IUsbDevice>();

        // checks the system for the devices listed in the list
        private static void CheckSystem(List<UsbIdentifier> devicesId)
        {
            lock (devicesId)
            {
                foreach (UsbIdentifier usbId in devicesId)
                {
                    List<String> devPaths = UsbApi.GetDevices(usbId.Identifier, usbId.Guid);
                    foreach (String devicePath in devPaths)
                    {
                        bool found = false;
                        // check for duplicate devices
                        foreach (IUsbDevice device in devices)
                        {
                            if (device.DevicePath == devicePath)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            continue;

                        IUsbDevice dev = new WebServiceUsbDevice(devicePath);
                        dev.UsbId = usbId;
                        devices.Add(dev);
                        // call all the registered clients for USB events
                        UsbNotify(dev, true);
                    }
                }
            }
        }
    }
}
