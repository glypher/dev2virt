using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace _2Virt
{
    public class USBView
    {
        public class USBController
        {
            private string devicePath;
            private string systemClass;
            private string deviceDescr;
            private USBHub rootHub;

            protected static string GetHCDDriverKeyName(string devicePath)
            {
                char[] decoded = null;
                IOCTLcommand ioctl = new IOCTLcommand();
                ioctl.ioctlNo = UsbApi.CTL_CODE(0x00000022, (uint)UsbApi.UsbIoctlFunction.HCD_GET_DRIVERKEY_NAME, 0, 0x0);
                ioctl.inputBuffer = null;
                ioctl.outputMaxSize = 260;
                try
                {
                    IntPtr hDevice = UsbApi.OpenDevice(devicePath, true);
                    IoStatus status = UsbApi.DeviceIoControl(hDevice, ioctl);
                    UsbApi.CloseDevice(hDevice);
                    // struct {ULONG Length; WCHAR Name[256];} DriverKeyName;
                    decoded = Encoding.Unicode.GetChars(status.buffer, IntPtr.Size, (int)status.size - IntPtr.Size);
                }
                catch (Exception) { }

                return (decoded == null) ? null : new String(decoded);
            }

            protected unsafe static string GetClassDescription(string className)
            {
                bool walkDone = false;
                IntPtr devInst = new IntPtr();
                IntPtr devInstNext = new IntPtr();
                byte[] buffer = new byte[256];
                GCHandle hInput = new GCHandle();
                uint len;

                // Get registry root node
                if ( UsbApi.UsbNativeApi.CM_Locate_DevNodeA(ref devInst, (IntPtr)0,
                    (uint)UsbApi.UsbNativeType.DevNodeSearchFlags.CM_LOCATE_DEVNODE_NORMAL) > 0 )
                    return null;

                try
                {
                    // pin the buffers into place
                    hInput = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                    // Do a depth first search for the DevNode with a matching DriverName value
                    while (!walkDone)
                    {
                        // Get the registry node value and compare with the systemName
                        len = (uint)buffer.Length;
                        if ((UsbApi.UsbNativeApi.CM_Get_DevNode_Registry_PropertyA(devInst, 
                                (uint)UsbApi.UsbNativeType.DevNodeRegistryProperty.CM_DRP_DRIVER,
                                (IntPtr)0, hInput.AddrOfPinnedObject(), ref len, 0) == 0) &&
                            (className.CompareTo(new string(Encoding.ASCII.GetChars(buffer, 0, (int)len))) == 0) )
                        {
                            // Get the device description
                            len = (uint)buffer.Length;
                            if (UsbApi.UsbNativeApi.CM_Get_DevNode_Registry_PropertyA(devInst,
                                (uint)UsbApi.UsbNativeType.DevNodeRegistryProperty.CM_DRP_DEVICEDESC,
                                (IntPtr)0, hInput.AddrOfPinnedObject(), ref len, 0) == 0)
                            {
                                return new String(Encoding.ASCII.GetChars(buffer, 0, (int)len));
                            }
                            return null;
                        }
                        // This DevNode didn't match, go down a level to the first child
                        if (UsbApi.UsbNativeApi.CM_Get_Child(ref devInstNext, devInst, 0) == 0)
                        {
                            devInst = devInstNext;
                            continue;
                        }
                        // Can't go down any further, go across to the next sibling.  If
                        // there are no more siblings, go back up until there is a sibling.
                        // If we can't go up any further, we're back at the root and we're done.
                        for (;;)
                        {
                            if (UsbApi.UsbNativeApi.CM_Get_Sibling(ref devInstNext, devInst, 0) == 0)
                            {
                                devInst = devInstNext;
                                break;
                            }

                            if (UsbApi.UsbNativeApi.CM_Get_Parent(ref devInstNext, devInst, 0) == 0)
                                devInst = devInstNext;
                            else
                            {
                                walkDone = true;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    if (hInput.IsAllocated)
                        hInput.Free();
                }

                return null;
            }

            public USBController(string systemPath)
            {
                devicePath = systemPath;
                systemClass = GetHCDDriverKeyName(devicePath);
                rootHub = new USBHub(this);
            }

            public string ControllerName
            { get {
                if (deviceDescr == null)
                    deviceDescr = GetClassDescription(systemClass);
                return deviceDescr;
            } }

            public USBHub RootHub { get { return rootHub; } }

            public class USBHub
            {
                private USBController controller;
                private string devicePath;
                private string deviceDesc;
                private List<USBPort> ports;

                public USBHub(USBController hostC)
                {
                    controller = hostC;
                    deviceDesc = "RootHub";
                    devicePath = GetRootHubDevicePath(controller);
                }

                public string HubName { get { return deviceDesc; } }

                protected static List<USBPort> GetHubPorts(USBHub hub)
                {
                    List<USBPort> ports = new List<USBPort>();
                    // Collect the node information
                    IOCTLcommand ioctl = new IOCTLcommand();
                    ioctl.ioctlNo = UsbApi.CTL_CODE(0x00000022, (uint)UsbApi.UsbIoctlFunction.USB_GET_NODE_INFORMATION, 0, 0x0);
                    ioctl.inputBuffer = null;
                    ioctl.outputMaxSize = 128;
                    try {
                        // Open Root hub
                        IntPtr hHub = UsbApi.OpenDevice(hub.devicePath, true);
                        IoStatus status = UsbApi.DeviceIoControl(hHub, ioctl);
                        UsbApi.UsbNativeType.USB_NODE_INFORMATION hubInfo = (UsbApi.UsbNativeType.USB_NODE_INFORMATION)UsbApi.UsbNativeType.Deserialize(
                                        status.buffer, 0, typeof(UsbApi.UsbNativeType.USB_NODE_INFORMATION));
                        UsbApi.CloseDevice(hHub);
                        
                        // Now Iterate through all the ports
                        for (uint i = 1; i <= hubInfo.NodeInfo.hubDescriptor.bNumberOfPorts; i++)
                        {
                            ports.Add(new USBPort(hub, i));
                        }
                    } catch (Exception) {}
           

                    return ports;
                }

                public class USBPort
                {
                    private USBHub rootHub;
                    private uint portIndex;
                    private UsbApi.UsbNativeType.USB_NODE_CONNECTION_INFORMATION connInfo;

                    public struct Property
                    {
                        public string Key;
                        public object Value;
                        internal Property(string key, object value)
                        {
                            Key = key;
                            Value = value;
                        }
                    }

                    public USBPort(USBHub hub, uint index)
                    {
                        rootHub = hub;
                        portIndex = index;
                        GetPortInformation(this);
                    }

                    public bool HasAttachedDevice { get { return connInfo.ConnectionStatus != UsbApi.UsbNativeType.USB_CONNECTION_STATUS.NoDeviceConnected; } }

                    public string PortInformation
                    {
                        get
                        {
                            string info = "[Port " + portIndex + "]: ";
                            if (connInfo.ConnectionStatus == UsbApi.UsbNativeType.USB_CONNECTION_STATUS.NoDeviceConnected)
                            {
                                return info + "NoDeviceConnected";
                            }

                            try
                            {
                                IOCTLcommand ioctl = new IOCTLcommand();
                                ioctl.ioctlNo = UsbApi.CTL_CODE(0x00000022, (uint)UsbApi.UsbIoctlFunction.USB_GET_NODE_CONNECTION_DRIVERKEY_NAME, 0, 0x0);
                                UsbApi.UsbNativeType.USB_NODE_CONNECTION_DRIVERKEY_NAME keyName = new UsbApi.UsbNativeType.USB_NODE_CONNECTION_DRIVERKEY_NAME();
                                keyName.ConnectionIndex = portIndex;
                                ioctl.inputBuffer = UsbApi.UsbNativeType.Serialize(keyName);
                                ioctl.outputMaxSize = (uint)Marshal.SizeOf(keyName);
                                IntPtr hHub = UsbApi.OpenDevice(rootHub.devicePath, true);
                                IoStatus status = UsbApi.DeviceIoControl(hHub, ioctl);
                                keyName = (UsbApi.UsbNativeType.USB_NODE_CONNECTION_DRIVERKEY_NAME)UsbApi.UsbNativeType.Deserialize(
                                            status.buffer, 0, typeof(UsbApi.UsbNativeType.USB_NODE_CONNECTION_DRIVERKEY_NAME));
                                // now finally we can take the registry name
                                info += USBController.GetClassDescription(new String(
                                    Encoding.Unicode.GetChars(keyName.DriverKeyName, 0, (int)keyName.ActualLength)));
                                UsbApi.CloseDevice(hHub);
                            }
                            catch (Exception)
                            {
                                info += "Unknown device";
                            }
                            
                            return info;
                        }
                    }


                    public List<Property> Properties
                    {
                        get
                        {
                            List<Property> props = new List<Property>();
                            if (connInfo.ConnectionStatus != UsbApi.UsbNativeType.USB_CONNECTION_STATUS.NoDeviceConnected)
                            {
                                string str = "0x" + connInfo.DeviceDescriptor.idVendor.ToString("X");
                                if (connInfo.DeviceDescriptor.iManufacturer > 0)
                                    str += " (" + GetStringDescriptor(this, connInfo.DeviceDescriptor.iManufacturer) + ")";
                                props.Add(new Property("VendorId: ", str));
                                str = "0x" + connInfo.DeviceDescriptor.idProduct.ToString("X");
                                if (connInfo.DeviceDescriptor.iProduct > 0)
                                    str += " (" + GetStringDescriptor(this, connInfo.DeviceDescriptor.iProduct) + ")";
                                props.Add(new Property("ProductId: ", str));
                                if (connInfo.DeviceDescriptor.iSerialNumber > 0)
                                    props.Add(new Property("Serial Number: ", GetStringDescriptor(this, connInfo.DeviceDescriptor.iSerialNumber)));

                                switch (connInfo.ConnectionStatus)
                                {
                                    case UsbApi.UsbNativeType.USB_CONNECTION_STATUS.DeviceConnected:
                                        props.Add(new Property("Connection Status: ", "Device Connected")); break;
                                    case UsbApi.UsbNativeType.USB_CONNECTION_STATUS.DeviceCausedOvercurrent:
                                        props.Add(new Property("Connection Status: ", "Device Caused Overcurrent")); break;
                                    case UsbApi.UsbNativeType.USB_CONNECTION_STATUS.DeviceFailedEnumeration:
                                    case UsbApi.UsbNativeType.USB_CONNECTION_STATUS.DeviceGeneralFailure:
                                    case UsbApi.UsbNativeType.USB_CONNECTION_STATUS.DeviceHubNestedTooDeeply:
                                    case UsbApi.UsbNativeType.USB_CONNECTION_STATUS.DeviceInLegacyHub:
                                    case UsbApi.UsbNativeType.USB_CONNECTION_STATUS.DeviceNotEnoughBandwidth:
                                    case UsbApi.UsbNativeType.USB_CONNECTION_STATUS.DeviceNotEnoughPower:
                                    case UsbApi.UsbNativeType.USB_CONNECTION_STATUS.NoDeviceConnected:
                                        props.Add(new Property("Connection Status: ", "Device Connection failuare")); break;
                                }
                                props.Add(new Property("USB Type: ", "0x" + connInfo.DeviceDescriptor.bcdUSB.ToString("X")));
                                props.Add(new Property("Device Class: ", "0x" + connInfo.DeviceDescriptor.bDeviceClass.ToString("X")));
                                props.Add(new Property("Device SubClass: ", "0x" + connInfo.DeviceDescriptor.bDeviceSubClass.ToString("X")));
                                props.Add(new Property("Device Protocol: ", "0x" + connInfo.DeviceDescriptor.bDeviceProtocol.ToString("X")));
                                props.Add(new Property("Max Packet Size Enpoint0: ", "0x" + connInfo.DeviceDescriptor.bMaxPacketSize0.ToString("X")));
                                props.Add(new Property("Number of Device Configurations: ", connInfo.DeviceDescriptor.bNumConfigurations));

                                props.Add(new Property("Active Device Configuration: ", connInfo.CurrentConfigurationValue));

                                List<UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR> configList = GetConfigurationDescriptors(this, 0);
                                foreach (UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR common in configList)
                                {
                                    switch (common.DescriptorType)
                                    {
                                        case UsbApi.UsbNativeType.UsbDescriptorType.USB_CONFIGURATION_DESCRIPTOR_TYPE:
                                            UsbApi.UsbNativeType.USB_CONFIGURATION_DESCRIPTOR config = (UsbApi.UsbNativeType.USB_CONFIGURATION_DESCRIPTOR)common;
                                            props.Add(new Property( "Active Configuration: ",
                                                GetStringDescriptor(this, config.iConfiguration)));
                                            break;
                                        case UsbApi.UsbNativeType.UsbDescriptorType.USB_INTERFACE_DESCRIPTOR_TYPE:
                                            UsbApi.UsbNativeType.USB_INTERFACE_DESCRIPTOR interf = (UsbApi.UsbNativeType.USB_INTERFACE_DESCRIPTOR)common;
                                            props.Add(new Property("Interface " + interf.bInterfaceNumber + ": ",
                                                GetStringDescriptor(this, interf.iInterface)));
                                            props.Add(new Property("Interface number of endpoints:", interf.bNumEndpoints));
                                            props.Add(new Property("Interface Class:", "0x" + interf.bInterfaceClass.ToString("X")));
                                            props.Add(new Property("Interface SubClass:", "0x" + interf.bInterfaceSubClass.ToString("X")));
                                            props.Add(new Property("Interface Protocol:", "0x" + interf.bInterfaceProtocol.ToString("X")));
                                            break;
                                        case UsbApi.UsbNativeType.UsbDescriptorType.USB_ENDPOINT_DESCRIPTOR_TYPE:
                                            UsbApi.UsbNativeType.USB_ENDPOINT_DESCRIPTOR endpoint = (UsbApi.UsbNativeType.USB_ENDPOINT_DESCRIPTOR)common;
                                            props.Add(new Property("Endpoint Address:", "0x" + (endpoint.bEndpointAddress & 0xFF).ToString("X") +
                                                ((endpoint.bEndpointAddress >> 7) == 1? " IN" : " OUT")));
                                            string strType = "Unknown";
                                            switch ((UsbApi.UsbNativeType.UsbTranferType)(endpoint.bmAttributes & 0xF))
                                            {
                                                case UsbApi.UsbNativeType.UsbTranferType.USB_ENDPOINT_TYPE_CONTROL:
                                                    strType = "Control"; break;
                                                case UsbApi.UsbNativeType.UsbTranferType.USB_ENDPOINT_TYPE_ISOCHRONOUS:
                                                    strType = "Isonchronous"; break;
                                                case UsbApi.UsbNativeType.UsbTranferType.USB_ENDPOINT_TYPE_BULK:
                                                    strType = "Bulk"; break;
                                                case UsbApi.UsbNativeType.UsbTranferType.USB_ENDPOINT_TYPE_INTERRUPT:
                                                    strType = "Interrupt"; break;
                                            }

                                            props.Add(new Property("Endpoint Tranfer Type:", strType));
                                            props.Add(new Property("Maximum packet size:", endpoint.wMaxPacketSize));
                                            break;
                                    }
                                }
                            }
                            return props;
                        }
                    }

                    internal unsafe static List<UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR> GetConfigurationDescriptors(USBPort port, Byte configIndex)
                    {
                        UsbApi.UsbNativeType.USB_DESCRIPTOR_REQUEST descriptor = new UsbApi.UsbNativeType.USB_DESCRIPTOR_REQUEST();
                        descriptor.ConnectionIndex = port.portIndex;
                        // USBD will automatically initialize these fields:
                        //     bmRequest = 0x80
                        //     bRequest  = 0x06
                        //     wValue    = Descriptor Type (high) and Descriptor Index (low byte)
                        descriptor.wValue = (ushort)(((ushort)UsbApi.UsbNativeType.UsbDescriptorType.USB_CONFIGURATION_DESCRIPTOR_TYPE << 8) | (ushort)configIndex);
                        //     wIndex    = Zero (or Language ID for String Descriptors)
                        descriptor.wIndex = (ushort)0;
                        //     wLength   = Length of descriptor buffer
                        descriptor.wLength = (ushort)512;
                        // Collect the information string
                        IOCTLcommand ioctl = new IOCTLcommand();
                        ioctl.ioctlNo = UsbApi.CTL_CODE(0x00000022, (uint)UsbApi.UsbIoctlFunction.USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, 0, 0x0);
                        ioctl.inputBuffer = UsbApi.UsbNativeType.Serialize(descriptor);
                        ioctl.outputMaxSize = (uint)Marshal.SizeOf(descriptor);
                        List<UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR> descriptorList = new List<UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR>();
                        try
                        {
                            // Open Root hub
                            IntPtr hHub = UsbApi.OpenDevice(port.rootHub.devicePath, true);
                            IoStatus status = UsbApi.DeviceIoControl(hHub, ioctl);
                            UsbApi.CloseDevice(hHub);
                            descriptor = (UsbApi.UsbNativeType.USB_DESCRIPTOR_REQUEST)UsbApi.UsbNativeType.Deserialize(
                                            status.buffer, 0, typeof(UsbApi.UsbNativeType.USB_DESCRIPTOR_REQUEST));
                            UsbApi.UsbNativeType.USB_CONFIGURATION_DESCRIPTOR confDescr = (UsbApi.UsbNativeType.USB_CONFIGURATION_DESCRIPTOR)UsbApi.UsbNativeType.Deserialize(
                                            descriptor.Data, 0, typeof(UsbApi.UsbNativeType.USB_CONFIGURATION_DESCRIPTOR));
                            descriptorList.Add(confDescr);

                            // Now deserialize all returned descriptors
                            int descrStart = confDescr.bLength;
                            UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR common;
                            while (descrStart < confDescr.wTotalLength)
                            {
                                common = (UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR)UsbApi.UsbNativeType.Deserialize(
                                    descriptor.Data, descrStart, typeof(UsbApi.UsbNativeType.USB_COMMON_DESCRIPTOR));
                                switch (common.DescriptorType)
                                {
                                    case UsbApi.UsbNativeType.UsbDescriptorType.USB_CONFIGURATION_DESCRIPTOR_TYPE:
                                        common = (UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR)UsbApi.UsbNativeType.Deserialize(
                                            descriptor.Data, descrStart, typeof(UsbApi.UsbNativeType.USB_CONFIGURATION_DESCRIPTOR));
                                        break;
                                    case UsbApi.UsbNativeType.UsbDescriptorType.USB_INTERFACE_DESCRIPTOR_TYPE:
                                        common = (UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR)UsbApi.UsbNativeType.Deserialize(
                                            descriptor.Data, descrStart, typeof(UsbApi.UsbNativeType.USB_INTERFACE_DESCRIPTOR));
                                        break;
                                    case UsbApi.UsbNativeType.UsbDescriptorType.USB_ENDPOINT_DESCRIPTOR_TYPE:
                                        common = (UsbApi.UsbNativeType.IUSB_COMMON_DESCRIPTOR)UsbApi.UsbNativeType.Deserialize(
                                            descriptor.Data, descrStart, typeof(UsbApi.UsbNativeType.USB_ENDPOINT_DESCRIPTOR));
                                        break;
                                }
                                descrStart += common.Length;
                                descriptorList.Add(common);
                            }
                        }
                        catch (Exception) {}

                        return descriptorList;
                    }

                    protected unsafe static string GetStringDescriptor(USBPort port, Byte stringIndex)
                    {
                        UsbApi.UsbNativeType.USB_DESCRIPTOR_REQUEST descriptor = new UsbApi.UsbNativeType.USB_DESCRIPTOR_REQUEST();
                        descriptor.ConnectionIndex = port.portIndex;
                        // USBD will automatically initialize these fields:
                        //     bmRequest = 0x80
                        //     bRequest  = 0x06
                        //     wValue    = Descriptor Type (high) and Descriptor Index (low byte)
                        descriptor.wValue = (ushort)(((ushort)UsbApi.UsbNativeType.UsbDescriptorType.USB_STRING_DESCRIPTOR_TYPE << 8) | (ushort)stringIndex);
                        //     wIndex    = Zero (or Language ID for String Descriptors)
                        descriptor.wIndex = (ushort)0;
                        //     wLength   = Length of descriptor buffer
                        descriptor.wLength = (ushort)Marshal.SizeOf(typeof(UsbApi.UsbNativeType.USB_STRING_DESCRIPTOR));
                        // Collect the information string
                        IOCTLcommand ioctl = new IOCTLcommand();
                        ioctl.ioctlNo = UsbApi.CTL_CODE(0x00000022, (uint)UsbApi.UsbIoctlFunction.USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, 0, 0x0);
                        ioctl.inputBuffer = UsbApi.UsbNativeType.Serialize(descriptor);
                        ioctl.outputMaxSize = (uint)Marshal.SizeOf(descriptor);
                        string result = null;
                        try {
                            // Open Root hub
                            IntPtr hHub = UsbApi.OpenDevice(port.rootHub.devicePath, true);
                            IoStatus status = UsbApi.DeviceIoControl(hHub, ioctl);
                            UsbApi.CloseDevice(hHub);
                            descriptor = (UsbApi.UsbNativeType.USB_DESCRIPTOR_REQUEST)UsbApi.UsbNativeType.Deserialize(
                                            status.buffer, 0, typeof(UsbApi.UsbNativeType.USB_DESCRIPTOR_REQUEST));
                            UsbApi.UsbNativeType.USB_STRING_DESCRIPTOR strDescr = (UsbApi.UsbNativeType.USB_STRING_DESCRIPTOR)UsbApi.UsbNativeType.Deserialize(
                                            descriptor.Data, 0, typeof(UsbApi.UsbNativeType.USB_STRING_DESCRIPTOR));
                            result = new String(Encoding.Unicode.GetChars(strDescr.bString, 0, strDescr.bLength));
                        }
                        catch (Exception) { result = "Invalid string value";  }

                        return result;
                    }

                    protected unsafe static void GetPortInformation(USBPort port)
                    {
                        // Collect the node information
                        IOCTLcommand ioctl = new IOCTLcommand();
                        ioctl.ioctlNo = UsbApi.CTL_CODE(0x00000022, (uint)UsbApi.UsbIoctlFunction.USB_GET_NODE_CONNECTION_INFORMATION_EX, 0, 0x0);
                        port.connInfo = new UsbApi.UsbNativeType.USB_NODE_CONNECTION_INFORMATION();
                        port.connInfo.ConnectionIndex = port.portIndex;
                        ioctl.inputBuffer = UsbApi.UsbNativeType.Serialize(port.connInfo);
                        ioctl.outputMaxSize = (uint)Marshal.SizeOf(port.connInfo);
                        try {
                            // Open Root hub
                            IntPtr hHub = UsbApi.OpenDevice(port.rootHub.devicePath, true);
                            IoStatus status = UsbApi.DeviceIoControl(hHub, ioctl);
                            UsbApi.CloseDevice(hHub);
                            port.connInfo = (UsbApi.UsbNativeType.USB_NODE_CONNECTION_INFORMATION)UsbApi.UsbNativeType.Deserialize(
                                            status.buffer, 0, typeof(UsbApi.UsbNativeType.USB_NODE_CONNECTION_INFORMATION));
                        } catch (Exception) {}
                    }
                }

                public List<USBPort> Ports
                {
                    get {
                        if (ports == null)
                            ports = GetHubPorts(this);
                        return ports;
                } }

                protected static string GetRootHubDevicePath(USBController controller)
                {
                    // Now get the system name of it's root hub for interrogation
                    char[] decoded = null;
                    IOCTLcommand ioctl = new IOCTLcommand();
                    ioctl.ioctlNo = UsbApi.CTL_CODE(0x00000022, (uint)UsbApi.UsbIoctlFunction.HCD_GET_ROOT_HUB_NAME, 0, 0x0);
                    ioctl.inputBuffer = null;
                    ioctl.outputMaxSize = 260;
                    try
                    {
                        IntPtr hDevice = UsbApi.OpenDevice(controller.devicePath, true);
                        IoStatus status = UsbApi.DeviceIoControl(hDevice, ioctl);
                        UsbApi.CloseDevice(hDevice);
                        // struct {ULONG Length; WCHAR Name[256];} DriverKeyName;
                        decoded = Encoding.Unicode.GetChars(status.buffer, IntPtr.Size, (int)status.size - IntPtr.Size);
                    }
                    catch (Exception) { }

                    return (decoded == null) ? null : "\\\\.\\" + new String(decoded);
                }
            }
        }

        public static List<USBController> GetUSBControllers()
        {
            // Get all host controllers active on the machine
            List<string> devPaths = UsbApi.GetDevices(null, UsbApi.GUID_CLASS_USB_HOST_CONTROLLER);
            List<USBController> controllers = new List<USBController>();
            foreach (string path in devPaths)
            {
                controllers.Add(new USBController(path));
            }
            return controllers;
        }
    }
}
