using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace _2Virt
{

    public enum USBError : int
    {
        WIN_API_SPECIFIC = 0,
        SUCCESS,
        DISCONNECTED,
        OVERFLOW,
        NOTFOUND,
        FAIL
    }

    public class UsbApi
    {
        internal static class UsbNativeType
        {
            public static object Deserialize(byte[] rawData, int position, Type nativeType)
            {
                IntPtr buffer = IntPtr.Zero;
                object retobj = null;
                try {
                    int rawSize = Marshal.SizeOf(nativeType);
                    if(rawSize > (rawData.Length - position))
                        return null;
                    buffer = Marshal.AllocHGlobal( rawSize );
                    Marshal.Copy(rawData, position, buffer, rawSize);
                    retobj = Marshal.PtrToStructure(buffer, nativeType);
                } catch (Exception) {}
                finally {
                    if (buffer != null)
                        Marshal.FreeHGlobal( buffer );
                }
                return retobj;
            }

            public static byte[] Serialize(object nativeObj)
            {
                IntPtr buffer = IntPtr.Zero;
                byte[] rawData = null;
                try {
                    int rawSize = Marshal.SizeOf(nativeObj);
                    buffer = Marshal.AllocHGlobal(rawSize);
                    Marshal.StructureToPtr(nativeObj, buffer, false);
                    rawData = new byte[rawSize];
                    Marshal.Copy(buffer, rawData, 0, rawSize );
                } catch (Exception) {}
                finally {
                    if (buffer != null)
                        Marshal.FreeHGlobal(buffer);
                }

                return rawData;
            }


            // GUID structure
            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct GUID
            {
                public int Data1;
                public System.UInt16 Data2;
                public System.UInt16 Data3;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
                public byte[] data4;
            }


            // Device interface data
            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct SP_DEVICE_INTERFACE_DATA
            {
                public int cbSize;
                public GUID InterfaceClassGuid;
                public int Flags;
                public int Reserved;
            }


            // Device interface detail data
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public unsafe struct PSP_DEVICE_INTERFACE_DETAIL_DATA
            {
                public int cbSize;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
                public string DevicePath;
            }

            // HIDD_ATTRIBUTES
            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct HIDD_ATTRIBUTES
            {
                public int Size; // = sizeof (struct _HIDD_ATTRIBUTES) = 10

                //
                // Vendor ids of this hid device
                //
                public System.UInt16 VendorID;
                public System.UInt16 ProductID;
                public System.UInt16 VersionNumber;

                //
                // Additional fields will be added to the end of this structure.
                //
            }


            // 
            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct HIDP_CAPS
            {
                public System.UInt16 Usage;					// USHORT
                public System.UInt16 UsagePage;				// USHORT
                public System.UInt16 InputReportByteLength;
                public System.UInt16 OutputReportByteLength;
                public System.UInt16 FeatureReportByteLength;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
                public System.UInt16[] Reserved;				// USHORT  Reserved[17];			
                public System.UInt16 NumberLinkCollectionNodes;
                public System.UInt16 NumberInputButtonCaps;
                public System.UInt16 NumberInputValueCaps;
                public System.UInt16 NumberInputDataIndices;
                public System.UInt16 NumberOutputButtonCaps;
                public System.UInt16 NumberOutputValueCaps;
                public System.UInt16 NumberOutputDataIndices;
                public System.UInt16 NumberFeatureButtonCaps;
                public System.UInt16 NumberFeatureValueCaps;
                public System.UInt16 NumberFeatureDataIndices;
            }


            // 
            public enum HIDP_REPORT_TYPE
            {
                HidP_Input,		// 0 input
                HidP_Output,	// 1 output
                HidP_Feature	// 2 feature
            }

            // Structures in the union belonging to HIDP_VALUE_CAPS (see below)

            // Range
            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct Range
            {
                public System.UInt16 UsageMin;			// USAGE	UsageMin; // USAGE  Usage; 
                public System.UInt16 UsageMax; 			// USAGE	UsageMax; // USAGE	Reserved1;
                public System.UInt16 StringMin;			// USHORT  StringMin; // StringIndex; 
                public System.UInt16 StringMax;			// USHORT	StringMax;// Reserved2;
                public System.UInt16 DesignatorMin;		// USHORT  DesignatorMin; // DesignatorIndex; 
                public System.UInt16 DesignatorMax;		// USHORT	DesignatorMax; //Reserved3; 
                public System.UInt16 DataIndexMin;		// USHORT  DataIndexMin;  // DataIndex; 
                public System.UInt16 DataIndexMax;		// USHORT	DataIndexMax; // Reserved4;
            }

            // Range
            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct NotRange
            {
                public System.UInt16 Usage;
                public System.UInt16 Reserved1;
                public System.UInt16 StringIndex;
                public System.UInt16 Reserved2;
                public System.UInt16 DesignatorIndex;
                public System.UInt16 Reserved3;
                public System.UInt16 DataIndex;
                public System.UInt16 Reserved4;
            }


            // Very many thanks to Mathias Sjogren for his help in
            // the proper way of marshalling this structure into C#
            //
            //
            [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
            public unsafe struct HIDP_VALUE_CAPS
            {
                //
                [FieldOffset(0)]
                public System.UInt16 UsagePage;					// USHORT
                [FieldOffset(2)]
                public System.Byte ReportID;						// UCHAR  ReportID;
                [MarshalAs(UnmanagedType.I1)]
                [FieldOffset(3)]
                public System.Boolean IsAlias;						// BOOLEAN  IsAlias;
                [FieldOffset(4)]
                public System.UInt16 BitField;						// USHORT  BitField;
                [FieldOffset(6)]
                public System.UInt16 LinkCollection;				//USHORT  LinkCollection;
                [FieldOffset(8)]
                public System.UInt16 LinkUsage;					// USAGE  LinkUsage;
                [FieldOffset(10)]
                public System.UInt16 LinkUsagePage;				// USAGE  LinkUsagePage;
                [MarshalAs(UnmanagedType.I1)]
                [FieldOffset(12)]
                public System.Boolean IsRange;					// BOOLEAN  IsRange;
                [MarshalAs(UnmanagedType.I1)]
                [FieldOffset(13)]
                public System.Boolean IsStringRange;				// BOOLEAN  IsStringRange;
                [MarshalAs(UnmanagedType.I1)]
                [FieldOffset(14)]
                public System.Boolean IsDesignatorRange;			// BOOLEAN  IsDesignatorRange;
                [MarshalAs(UnmanagedType.I1)]
                [FieldOffset(15)]
                public System.Boolean IsAbsolute;					// BOOLEAN  IsAbsolute;
                [MarshalAs(UnmanagedType.I1)]
                [FieldOffset(16)]
                public System.Boolean HasNull;					// BOOLEAN  HasNull;
                [FieldOffset(17)]
                public System.Char Reserved;						// UCHAR  Reserved;
                [FieldOffset(18)]
                public System.UInt16 BitSize;						// USHORT  BitSize;
                [FieldOffset(20)]
                public System.UInt16 ReportCount;					// USHORT  ReportCount;
                [FieldOffset(22)]
                public System.UInt16 Reserved2a;					// USHORT  Reserved2[5];		
                [FieldOffset(24)]
                public System.UInt16 Reserved2b;					// USHORT  Reserved2[5];
                [FieldOffset(26)]
                public System.UInt16 Reserved2c;					// USHORT  Reserved2[5];
                [FieldOffset(28)]
                public System.UInt16 Reserved2d;					// USHORT  Reserved2[5];
                [FieldOffset(30)]
                public System.UInt16 Reserved2e;					// USHORT  Reserved2[5];
                [FieldOffset(32)]
                public System.UInt16 UnitsExp;					// ULONG  UnitsExp;
                [FieldOffset(34)]
                public System.UInt16 Units;						// ULONG  Units;
                [FieldOffset(36)]
                public System.Int16 LogicalMin;					// LONG  LogicalMin;   ;
                [FieldOffset(38)]
                public System.Int16 LogicalMax;					// LONG  LogicalMax
                [FieldOffset(40)]
                public System.Int16 PhysicalMin;					// LONG  PhysicalMin, 
                [FieldOffset(42)]
                public System.Int16 PhysicalMax;					// LONG  PhysicalMax;
                // The Structs in the Union			
                [FieldOffset(44)]
                public Range Range;
                [FieldOffset(44)]
                public Range NotRange;
            }


            //*  Comm Timeout Grasp at Straws
            [StructLayout(LayoutKind.Sequential)]
            public struct COMMTIMEOUTS
            {
                public int ReadIntervalTimeout;
                public int ReadTotalTimeoutMultiplier;
                public int ReadTotalTimeoutConstant;
                public int WriteTotalTimeoutMultiplier;
                public int WriteTotalTimeoutConstant;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OVERLAPPED
            {
                public int Internal;
                public int InternalHigh;
                public int Offset;
                public int OffsetHigh;
                public int hEvent;
            }

            public enum DevNodeSearchFlags : uint
            {
                CM_LOCATE_DEVNODE_NORMAL       = 0x00000000,
                CM_LOCATE_DEVNODE_PHANTOM      = 0x00000001,
                CM_LOCATE_DEVNODE_CANCELREMOVE = 0x00000002,
                CM_LOCATE_DEVNODE_NOVALIDATION = 0x00000004,
                CM_LOCATE_DEVNODE_BITS         = 0x00000007,
            }

            //
            // Registry properties (specified in call to CM_Get_DevInst_Registry_Property or CM_Get_Class_Registry_Property,
            // some are allowed in calls to CM_Set_DevInst_Registry_Property and CM_Set_Class_Registry_Property)
            // CM_DRP_xxxx values should be used for CM_Get_DevInst_Registry_Property / CM_Set_DevInst_Registry_Property
            // CM_CRP_xxxx values should be used for CM_Get_Class_Registry_Property / CM_Set_Class_Registry_Property
            // DRP/CRP values that overlap must have a 1:1 correspondence with each other
            public enum DevNodeRegistryProperty : uint
            {
                CM_DRP_DEVICEDESC                  = 0x00000001, // DeviceDesc REG_SZ property (RW)
                CM_DRP_HARDWAREID                  = 0x00000002, // HardwareID REG_MULTI_SZ property (RW)
                CM_DRP_COMPATIBLEIDS               = 0x00000003, // CompatibleIDs REG_MULTI_SZ property (RW)
                CM_DRP_UNUSED0                     = 0x00000004, // unused
                CM_DRP_SERVICE                     = 0x00000005, // Service REG_SZ property (RW)
                CM_DRP_UNUSED1                     = 0x00000006, // unused
                CM_DRP_UNUSED2                     = 0x00000007, // unused
                CM_DRP_CLASS                       = 0x00000008, // Class REG_SZ property (RW)
                CM_DRP_CLASSGUID                   = 0x00000009, // ClassGUID REG_SZ property (RW)
                CM_DRP_DRIVER                      = 0x0000000A, // Driver REG_SZ property (RW)
                CM_DRP_CONFIGFLAGS                 = 0x0000000B, // ConfigFlags REG_DWORD property (RW)
                CM_DRP_MFG                         = 0x0000000C, // Mfg REG_SZ property (RW)
                CM_DRP_FRIENDLYNAME                = 0x0000000D, // FriendlyName REG_SZ property (RW)
                CM_DRP_LOCATION_INFORMATION        = 0x0000000E, // LocationInformation REG_SZ property (RW)
                CM_DRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000F, // PhysicalDeviceObjectName REG_SZ property (R)
                CM_DRP_CAPABILITIES                = 0x00000010, // Capabilities REG_DWORD property (R)
                CM_DRP_UI_NUMBER                   = 0x00000011, // UiNumber REG_DWORD property (R)
                CM_DRP_UPPERFILTERS                = 0x00000012  // UpperFilters REG_MULTI_SZ property (RW)
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_HUB_INFORMATION
            {
                public USB_HUB_DESCRIPTOR hubDescriptor;                
                public Boolean HubIsBusPowered;
            };

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_NODE_INFORMATION
            {
                public UInt16 NodeType; // typedef enum _USB_HUB_NODE {UsbHub, UsbMIParent} USB_HUB_NODE;
                public USB_HUB_INFORMATION NodeInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_DEVICE_DESCRIPTOR
            {
                public Byte bLength;
                public Byte bDescriptorType;
                public UInt16 bcdUSB;
                public Byte bDeviceClass;
                public Byte bDeviceSubClass;
                public Byte bDeviceProtocol;
                public Byte bMaxPacketSize0;
                public UInt16 idVendor;
                public UInt16 idProduct;
                public UInt16 bcdDevice;
                public Byte iManufacturer;
                public Byte iProduct;
                public Byte iSerialNumber;
                public Byte bNumConfigurations;
            }

            public enum USB_CONNECTION_STATUS : byte
            {
                NoDeviceConnected = 0x0,
                DeviceConnected,
                DeviceFailedEnumeration,
                DeviceGeneralFailure,
                DeviceCausedOvercurrent,
                DeviceNotEnoughPower,
                DeviceNotEnoughBandwidth,
                DeviceHubNestedTooDeeply,
                DeviceInLegacyHub
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_PIPE_INFO
            {
                USB_ENDPOINT_DESCRIPTOR  EndpointDescriptor;
                UInt16 ScheduleOffset;
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_NODE_CONNECTION_INFORMATION
            {
                public UInt32 ConnectionIndex;
                public USB_DEVICE_DESCRIPTOR DeviceDescriptor;
                public Byte CurrentConfigurationValue;
                public Boolean LowSpeed;
                public Boolean DeviceIsHub;
                public UInt16 DeviceAddress;
                public UInt32 NumberOfOpenPipes;
                public USB_CONNECTION_STATUS  ConnectionStatus;
                // Endpoint numbers are 0-15.  Endpoint number 0 is the standard
                // control endpoint which is not explicitly listed in the Configuration
                // Descriptor.  There can be an IN endpoint and an OUT endpoint at
                // endpoint numbers 1-15 so there can be a maximum of 30 endpoints
                // per device configuration.
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
                public USB_PIPE_INFO[]  PipeList; // USB_PIPE_INFO  PipeList[0];
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_NODE_CONNECTION_DRIVERKEY_NAME
            {
                public UInt32 ConnectionIndex;
                public UInt32 ActualLength;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
                public Byte[] DriverKeyName; // WCHAR  DriverKeyName[1] - just preallocate 128 to be on the safe side
            }

            public enum UsbDescriptorType : byte
            {
                USB_DEVICE_DESCRIPTOR_TYPE = 0x01,
                USB_CONFIGURATION_DESCRIPTOR_TYPE = 0x02,
                USB_STRING_DESCRIPTOR_TYPE = 0x03,
                USB_INTERFACE_DESCRIPTOR_TYPE = 0x04,
                USB_ENDPOINT_DESCRIPTOR_TYPE = 0x05
            }

            public enum UsbTranferType : byte
            {
                USB_ENDPOINT_TYPE_CONTROL = 0x0,
                USB_ENDPOINT_TYPE_ISOCHRONOUS,
                USB_ENDPOINT_TYPE_BULK,
                USB_ENDPOINT_TYPE_INTERRUPT
            }

            public interface IUSB_COMMON_DESCRIPTOR
            {
                Byte Length { get; } // Specifies the length, in bytes, of the descriptor
                UsbDescriptorType DescriptorType { get; } // Specifies the descriptor type. Must always be USB_STRING_DESCRIPTOR_TYPE.
            }

            public unsafe struct USB_COMMON_DESCRIPTOR : IUSB_COMMON_DESCRIPTOR
            {
                public Byte bLength;
                public Byte bDescriptorType;

                public Byte Length { get { return bLength; } }
                public UsbDescriptorType DescriptorType { get { return (UsbDescriptorType)bDescriptorType; } }
            }


            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_HUB_DESCRIPTOR : IUSB_COMMON_DESCRIPTOR
            {
                public Byte bLength;
                public Byte bDescriptorType;
                public Byte bNumberOfPorts;
                public Byte wHubCharacteristics;
                public Byte bPowerOnToPowerGood;
                public Byte bHubControlCurrent;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
                public Byte[] bRemoveAndPowerMask; // UCHAR  bRemoveAndPowerMask[64]

                public Byte Length { get { return bLength; } }
                public UsbDescriptorType DescriptorType { get { return (UsbDescriptorType)bDescriptorType; } }
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_STRING_DESCRIPTOR : IUSB_COMMON_DESCRIPTOR
            {
                public Byte bLength; // Specifies the length, in bytes, of the descriptor
                public Byte bDescriptorType; // Specifies the descriptor type. Must always be USB_STRING_DESCRIPTOR_TYPE.
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
                public Byte[] bString; // Pointer to a client-allocated buffer that contains a Unicode string with the requested string descriptor.

                public Byte Length { get { return bLength; } }
                public UsbDescriptorType DescriptorType { get { return (UsbDescriptorType)bDescriptorType; } }
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_ENDPOINT_DESCRIPTOR : IUSB_COMMON_DESCRIPTOR
            {
                public Byte bLength; // Specifies the length, in bytes, of the descriptor
                public Byte bDescriptorType; // Specifies the descriptor type. Must always be USB_ENDPOINT_DESCRIPTOR_TYPE
                public Byte bEndpointAddress; // Specifies the USB-defined endpoint address. The four low-order bits specify the endpoint number. The high-order bit specifies the direction of data flow on this endpoint: 1 for in, 0 for out
                public Byte bmAttributes; // The two low-order bits specify the endpoint type, one of USB_ENDPOINT_TYPE_CONTROL, USB_ENDPOINT_TYPE_ISOCHRONOUS, USB_ENDPOINT_TYPE_BULK, or USB_ENDPOINT_TYPE_INTERRUPT.
                public UInt16 wMaxPacketSize; // Specifies the maximum packet size that can be sent from or to this endpoint
                public Byte bInterval; // For interrupt endpoints, bInterval contains the polling interval. For other types of endpoint, this value should be ignored

                public Byte Length { get { return bLength; } }
                public UsbDescriptorType DescriptorType { get { return (UsbDescriptorType)bDescriptorType; } }
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_CONFIGURATION_DESCRIPTOR : IUSB_COMMON_DESCRIPTOR
            {
                public Byte bLength; // Specifies the length, in bytes, of the descriptor
                public Byte bDescriptorType; // Specifies the descriptor type. Must always be USB_CONFIGURATION_DESCRIPTOR_TYPE
                public UInt16 wTotalLength; // Specifies the total length, in bytes, of all data for the configuration. The length includes all interface, endpoint, class, or vendor-specific descriptors
                public Byte bNumInterfaces; // Specifies the total number of interfaces supported by this configuration
                public Byte bConfigurationValue; // Contains the value that is used to select a configuration that can be passed to the USB SetConfiguration request 
                public Byte iConfiguration; // Specifies the device-defined index of the string descriptor for this configuration
                public Byte bmAttributes; // Specifies a bitmap to describe behavior of this configuration. The bits are described and set in little-endian order.
                public Byte MaxPower; // Specifies the power requirements of this device in two-milliampere units. This member is valid only if bit seven is set in bmAttributes.

                public Byte Length { get { return bLength; } }
                public UsbDescriptorType DescriptorType { get { return (UsbDescriptorType)bDescriptorType; } }
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_INTERFACE_DESCRIPTOR : IUSB_COMMON_DESCRIPTOR
            {
                public Byte bLength; // Specifies the length, in bytes, of the descriptor
                public Byte bDescriptorType; // Specifies the descriptor type. Must always be USB_INTERFACE_DESCRIPTOR_TYPE
                public Byte bInterfaceNumber; // The index number of the interface
                public Byte bAlternateSetting; // The index number of the alternate setting of the interface
                public Byte bNumEndpoints; // The number of endpoints that are used by the interface, excluding the default status endpoint
                public Byte bInterfaceClass; // The class code of the device that the USB specification group assigned
                public Byte bInterfaceSubClass; // The subclass code of the device that the USB specification group assigned
                public Byte bInterfaceProtocol; // The protocol code of the device that the USB specification group assigned
                public Byte iInterface; // The index of a string descriptor that describes the interface. iInterface must be set to 0x1

                public Byte Length { get { return bLength; } }
                public UsbDescriptorType DescriptorType { get { return (UsbDescriptorType)bDescriptorType; } }
            }

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct USB_DESCRIPTOR_REQUEST
            {
                // The port whose descriptors are retrieved
                public UInt32 ConnectionIndex;
                // The type of USB device request (standard, class, or vendor), the direction of the data transfer,
                // and the type of data recipient (device, interface, or endpoint).  On input to the
                // IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION I/O control request, the USB stack ignores the value 
                // of bmRequest and inserts a value of 0x80. This value indicates a standard USB device request and a
                // device-to-host data transfer.
                public Byte bmRequest;
                // The request number. On input to the IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION I/O control request,
                // the USB stack ignores the value of bRequest and inserts a value of 0x06. This value indicates a request of GET_DESCRIPTOR
                public Byte bRequest;
                // On input to the IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION I/O control request, the caller should specify
                // the type of descriptor to retrieve in the high byte of wValue and the descriptor index in the low byte.
                // The following table lists the possible descriptor types:
                // USB_DEVICE_DESCRIPTOR_TYPE Instructs the USB stack to return the device descriptor. 
                // USB_CONFIGURATION_DESCRIPTOR_TYPE Instructs the USB stack to return the configuration descriptor and all interface, endpoint, class-specific, and vendor-specific descriptors associated with the current configuration..  
                // USB_STRING_DESCRIPTOR_TYPE Instructs the USB stack to return the indicated string descriptor. 
                // USB_INTERFACE_DESCRIPTOR_TYPE Instructs the USB stack to return the indicated interface descriptor. 
                // USB_ENDPOINT_DESCRIPTOR_TYPE Instructs the USB stack to return the indicated endpoint descriptor. 
                public UInt16 wValue;
                // The device-specific index of the descriptor that is to be retrieved
                public UInt16 wIndex;
                // The length of the data that is transferred during the second phase of the control transfer
                public UInt16 wLength;
                // On output from the IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION I/O control request, this member contains the retrieved descriptors
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
                public Byte[] Data; // UCHAR  Data[0] - just preallocate a string descriptor
            }
        }

        internal static class UsbNativeApi
        {
            // Constants usefull on certain Native calls
            public const int DIGCF_PRESENT = 0x00000002;
            public const int DIGCF_DEVICEINTERFACE = 0x00000010;
            public const int DIGCF_INTERFACEDEVICE = 0x00000010;
            public const uint GENERIC_READ = 0x80000000;
            public const uint GENERIC_WRITE = 0x40000000;
            public const uint FILE_SHARE_READ = 0x00000001;
            public const uint FILE_SHARE_WRITE = 0x00000002;
            public const int FILE_ATTRIBUTE_NORMAL = 0x00000080;
            public const int FILE_FLAG_OVERLAPPED = 0x40000000;
            public const int OPEN_EXISTING = 3;
            public const int EV_RXFLAG = 0x0002; // received certain character

            // 1-- Get GUID for the HID Class
            [DllImport("hid.dll", SetLastError = true)]
            public static extern unsafe void HidD_GetHidGuid(
                ref UsbNativeType.GUID lpHidGuid);

            // 2- Get array of structures with the HID info
            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern unsafe int SetupDiGetClassDevs(
                ref UsbNativeType.GUID lpHidGuid,
                int* Enumerator,
                int* hwndParent,
                int Flags);


            // 3- Get context structure for a device interface element
            /*
              SetupDiEnumDeviceInterfaces returns a context structure for a device 
              interface element of a device information set. Each call returns information 
              about one device interface; the function can be called repeatedly to get information 
              about several interfaces exposed by one or more devices.
            */
            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern unsafe int SetupDiEnumDeviceInterfaces(
                int DeviceInfoSet,
                int DeviceInfoData,
                ref UsbNativeType.GUID lpHidGuid,
                int MemberIndex,
                ref UsbNativeType.SP_DEVICE_INTERFACE_DATA lpDeviceInterfaceData);


            //	4 a- Get device Path name
            //  Works for the first pass  --> to get the required size
            //
            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern unsafe int SetupDiGetDeviceInterfaceDetail(
                int DeviceInfoSet,
                ref UsbNativeType.SP_DEVICE_INTERFACE_DATA lpDeviceInterfaceData,
                int* aPtr,
                int detailSize,
                ref int requiredSize,
                int* bPtr);

            //	4 b- Get device Path name
            //  Works for second pass (overide), once size value is known
            //	
            // 
            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern unsafe int SetupDiGetDeviceInterfaceDetail(
                int DeviceInfoSet,
                ref UsbNativeType.SP_DEVICE_INTERFACE_DATA lpDeviceInterfaceData,
                ref UsbNativeType.PSP_DEVICE_INTERFACE_DETAIL_DATA myPSP_DEVICE_INTERFACE_DETAIL_DATA,
                int detailSize,
                ref int requiredSize,
                int* bPtr);

            // 5
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int CreateFile(
                string lpFileName,							// file name
                uint dwDesiredAccess,						// access mode
                uint dwShareMode,							// share mode
                uint lpSecurityAttributes,					// SD
                uint dwCreationDisposition,					// how to create
                uint dwFlagsAndAttributes,					// file attributes
                uint hTemplateFile							// handle to template file
                );

            // 6
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int CloseHandle(
                int hObject                                    // handle to object
                );

            // 7
            [DllImport("hid.dll", SetLastError = true)]
            public static extern int HidD_GetAttributes(
                int hObject,                                   // IN HANDLE  HidDeviceObject,
                ref UsbNativeType.HIDD_ATTRIBUTES Attributes); // OUT PHIDD_ATTRIBUTES  Attributes

            // 8
            [DllImport("hid.dll", SetLastError = true)]
            public unsafe static extern int HidD_GetPreparsedData(
                int hObject,                                // IN HANDLE  HidDeviceObject,
                ref int pPHIDP_PREPARSED_DATA);             // OUT PHIDP_PREPARSED_DATA  *PreparsedData

            // 9
            [DllImport("hid.dll", SetLastError = true)]
            public unsafe static extern int HidP_GetCaps(
                int pPHIDP_PREPARSED_DATA,                  // IN PHIDP_PREPARSED_DATA  PreparsedData,
                ref UsbNativeType.HIDP_CAPS myPHIDP_CAPS);  // OUT PHIDP_CAPS  Capabilities

            // 10
            [DllImport("hid.dll", SetLastError = true)]
            public unsafe static extern int HidP_GetValueCaps(
                UsbNativeType.HIDP_REPORT_TYPE ReportType,            // IN HIDP_REPORT_TYPE  ReportType,
                [In, Out] UsbNativeType.HIDP_VALUE_CAPS[] ValueCaps,  // OUT PHIDP_VALUE_CAPS  ValueCaps,
                ref int ValueCapsLength,                              // IN OUT PULONG  ValueCapsLength,
                int pPHIDP_PREPARSED_DATA);                           // IN PHIDP_PREPARSED_DATA  PreparsedData

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetCommTimeouts(
                int hFile,
                ref UsbNativeType.COMMTIMEOUTS lpCommTimeouts
                );

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetCommTimeouts(
                int hFile,
                ref UsbNativeType.COMMTIMEOUTS lpCommTimeouts
                );

            // 11
            [DllImport("kernel32.dll", SetLastError = true)]
            public unsafe static extern bool ReadFile(
                int hFile,                                // handle to file
                byte[] lpBuffer,                          // data buffer
                int nNumberOfBytesToRead,                 // number of bytes to read
                ref int lpNumberOfBytesRead,              // number of bytes read
                ref UsbNativeType.OVERLAPPED lpOverlapped // overlapped buffer
                );

            [DllImport("kernel32.dll")]
            public static extern bool GetOverlappedResult(
                IntPtr hFile,
                [In] ref UsbNativeType.OVERLAPPED lpOverlapped,
                out uint lpNumberOfBytesTransferred,
                bool bWait);

            [DllImport("kernel32.dll")]
            public static extern bool HasOverlappedIoCompleted(
                [In] ref UsbNativeType.OVERLAPPED lpOverlapped
                );


            // 12
            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern unsafe int SetupDiDestroyDeviceInfoList(
                int DeviceInfoSet				// IN HDEVINFO  DeviceInfoSet
                );

            // 13
            [DllImport("hid.dll", SetLastError = true)]
            public static extern unsafe int HidD_FreePreparsedData(
                int pPHIDP_PREPARSED_DATA			// IN PHIDP_PREPARSED_DATA  PreparsedData
                );

            // 14
            [DllImport("kernel32.dll", SetLastError = true)]
            public extern static int DeviceIoControl(
                IntPtr hDevice,            // device file handle
                uint IoControlCode,        // IOCTL command number
                IntPtr lpInBuffer,         // input IOCTL buffer
                uint InBufferSize,         // input IOCTL buffer size
                IntPtr lpOutBuffer,        // output command buffer
                uint nOutBufferSize,       // output command buffer maximum size
                ref uint lpBytesReturned,  // number of bytes returned in the output buffer
                IntPtr lpOverlapped);      // overlapped buffer

            // 15
            [DllImport("cfgmgr32.dll", SetLastError = true)]
            public extern static uint CM_Locate_DevNodeA(
                ref IntPtr pDevInst, // device handle
                IntPtr pDeviceId,        // optionally a null terminated string for the device id
                uint  ulFlags);  // search Flags defined above

            // 16
            [DllImport("cfgmgr32.dll", SetLastError = true)]
            public extern static uint CM_Get_DevNode_Registry_PropertyA(
                [In]      IntPtr pDevInst, // device handle
                [In]      uint ulProperty,
                [Out]     IntPtr pulRegDataType,
                [Out]     IntPtr Buffer,
                [In, Out] ref uint pulLength,
                [In]      uint ulFlags);

            // 17
            [DllImport("cfgmgr32.dll", SetLastError = true)]
            public extern static uint CM_Get_Parent(
                [In, Out] ref IntPtr pDevInst, // returned parent handle
                [In]      IntPtr pDeviceId,    // current device handle
                [In]      uint ulFlags);      // search Flags

            [DllImport("cfgmgr32.dll", SetLastError = true)]
            public extern static uint CM_Get_Child(
                [In, Out] ref IntPtr pDevInst, // returned child handle
                [In]      IntPtr pDeviceId,    // current device handle
                [In]      uint ulFlags);      // search Flags

            [DllImport("cfgmgr32.dll", SetLastError = true)]
            public extern static uint CM_Get_Sibling(
                [In, Out] ref IntPtr pDevInst, // returned siblings handle
                [In]      IntPtr pDeviceId,    // current device handle
                [In]      ulong  ulFlags);     // search Flags
        }



        // This are the NTSTATUS codes if we want to retrieve and handle our return codes&errors
        /*
         * 
         * #define HIDP_ERROR_CODES(SEV, CODE) \
        ((NTSTATUS) (((SEV) << 28) | (FACILITY_HID_ERROR_CODE << 16) | (CODE)))
         * #define HIDP_STATUS_SUCCESS                  (HIDP_ERROR_CODES(0x0,0))
        #define HIDP_STATUS_NULL                     (HIDP_ERROR_CODES(0x8,1))
        #define HIDP_STATUS_INVALID_PREPARSED_DATA   (HIDP_ERROR_CODES(0xC,1))
        #define HIDP_STATUS_INVALID_REPORT_TYPE      (HIDP_ERROR_CODES(0xC,2))
        #define HIDP_STATUS_INVALID_REPORT_LENGTH    (HIDP_ERROR_CODES(0xC,3))
        #define HIDP_STATUS_USAGE_NOT_FOUND          (HIDP_ERROR_CODES(0xC,4))
        #define HIDP_STATUS_VALUE_OUT_OF_RANGE       (HIDP_ERROR_CODES(0xC,5))
        #define HIDP_STATUS_BAD_LOG_PHY_VALUES       (HIDP_ERROR_CODES(0xC,6))
        #define HIDP_STATUS_BUFFER_TOO_SMALL         (HIDP_ERROR_CODES(0xC,7))
        #define HIDP_STATUS_INTERNAL_ERROR           (HIDP_ERROR_CODES(0xC,8))
        #define HIDP_STATUS_I8042_TRANS_UNKNOWN      (HIDP_ERROR_CODES(0xC,9))
        #define HIDP_STATUS_INCOMPATIBLE_REPORT_ID   (HIDP_ERROR_CODES(0xC,0xA))
        #define HIDP_STATUS_NOT_VALUE_ARRAY          (HIDP_ERROR_CODES(0xC,0xB))
        #define HIDP_STATUS_IS_VALUE_ARRAY           (HIDP_ERROR_CODES(0xC,0xC))
        #define HIDP_STATUS_DATA_INDEX_NOT_FOUND     (HIDP_ERROR_CODES(0xC,0xD))
        #define HIDP_STATUS_DATA_INDEX_OUT_OF_RANGE  (HIDP_ERROR_CODES(0xC,0xE))
        #define HIDP_STATUS_BUTTON_NOT_PRESSED       (HIDP_ERROR_CODES(0xC,0xF))
        #define HIDP_STATUS_REPORT_DOES_NOT_EXIST    (HIDP_ERROR_CODES(0xC,0x10))
        #define HIDP_STATUS_NOT_IMPLEMENTED          (HIDP_ERROR_CODES(0xC,0x20))
        */


        // specified in DCB
        public const int INVALID_HANDLE_VALUE = -1;
        public const int ERROR_INVALID_HANDLE = 6;

        public const string GUID_CLASS_USB_HOST_CONTROLLER = "3ABF6F2D-71C4-462a-8A92-1E6861E6AF27";
        public const string GUID_CLASS_USBHUB              = "F18A0E88-C30C-11D0-8815-00A0C906BED8";
        public const string GUID_CLASS_USB_DEVICE          = "A5DCBF10-6530-11D2-901F-00C04FB951ED";
        public const string GUID_USB_WMI_STD_DATA          = "4E623B20-CB14-11D1-B331-00A0C959BBD2";
        public const string GUID_USB_WMI_STD_NOTIFICATION  = "4E623B20-CB14-11D1-B331-00A0C959BBD2";

        // specific IOCTL codes for USB nodes defined in Usbioctl.h
        public enum UsbIoctlFunction : uint
        {
            HCD_GET_STATS_1 = 255,
            HCD_DIAGNOSTIC_MODE_ON = 256,
            HCD_DIAGNOSTIC_MODE_OFF = 257,
            HCD_GET_ROOT_HUB_NAME = 258,
            HCD_GET_DRIVERKEY_NAME = 265,
            HCD_GET_STATS_2 = 266,
            HCD_DISABLE_PORT = 268,
            HCD_ENABLE_PORT = 269,
            HCD_USER_REQUEST = 270,
            USB_GET_NODE_INFORMATION = 258,
            USB_GET_NODE_CONNECTION_INFORMATION = 259,
            USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION = 260,
            USB_GET_NODE_CONNECTION_NAME = 261,
            USB_DIAG_IGNORE_HUBS_ON = 262,
            USB_DIAG_IGNORE_HUBS_OFF = 263,
            USB_GET_NODE_CONNECTION_DRIVERKEY_NAME = 264,
            USB_GET_HUB_CAPABILITIES = 271,
            USB_GET_NODE_CONNECTION_ATTRIBUTES = 272,
            USB_HUB_CYCLE_PORT = 273,
            USB_GET_NODE_CONNECTION_INFORMATION_EX = 274
        }

        private unsafe static string GetDevicePath(UsbNativeType.GUID guid, int hardwareDevInfo, int device_number)
        {
            int result = 0;
            UsbNativeType.SP_DEVICE_INTERFACE_DATA interfaceData = new UsbNativeType.SP_DEVICE_INTERFACE_DATA();
            interfaceData.cbSize = Marshal.SizeOf(interfaceData);

            // get the device_number device interface
            result = UsbNativeApi.SetupDiEnumDeviceInterfaces(
                    hardwareDevInfo,
                    0,
                    ref guid,
                    device_number,
                    ref interfaceData);

            // get the required size for the device interface system path
            int devicePathSize = 0;
            result = UsbNativeApi.SetupDiGetDeviceInterfaceDetail(
                hardwareDevInfo,           // IN HDEVINFO  DeviceInfoSet,
                ref interfaceData,         // IN PSP_DEVICE_INTERFACE_DATA  DeviceInterfaceData,
                null,                      // DeviceInterfaceDetailData,  OPTIONAL
                0,                         // IN DWORD  DeviceInterfaceDetailDataSize,
                ref devicePathSize,        // OUT PDWORD  RequiredSize,  OPTIONAL
                null); // 

            // get the actual device interface details
            UsbNativeType.PSP_DEVICE_INTERFACE_DETAIL_DATA interfaceDetail = new UsbNativeType.PSP_DEVICE_INTERFACE_DETAIL_DATA();
            interfaceDetail.cbSize = 5;    // sizeof (SP_DEVICE_INTERFACE_DETAIL_DATA) == 5 in C
            result = UsbNativeApi.SetupDiGetDeviceInterfaceDetail(
                hardwareDevInfo,           // IN HDEVINFO  DeviceInfoSet,
                ref interfaceData,         // IN PSP_DEVICE_INTERFACE_DATA  DeviceInterfaceData,
                ref interfaceDetail,       // DeviceInterfaceDetailData,  OPTIONAL
                devicePathSize,            // IN DWORD  DeviceInterfaceDetailDataSize,
                ref devicePathSize,        // OUT PDWORD  RequiredSize,  OPTIONAL
                null); // 

            return interfaceDetail.DevicePath;
        }

        // initializes a GUID from a 873FDF-61A8-11D1-AA5E-00C04FB1728B
        private static UsbNativeType.GUID ParseClassGuid(string classGuid)
        {
            UsbNativeType.GUID guid = new UsbNativeType.GUID();

            if ((classGuid == null) && (classGuid == string.Empty))
                return guid;

            string[] data = classGuid.Split('-');
            if (data.Length != 5)
                return guid;

            guid.Data1 = Int32.Parse(data[0], System.Globalization.NumberStyles.AllowHexSpecifier);
            guid.Data2 = UInt16.Parse(data[1], System.Globalization.NumberStyles.AllowHexSpecifier);
            guid.Data3 = UInt16.Parse(data[2], System.Globalization.NumberStyles.AllowHexSpecifier);
            guid.data4 = new byte[8];
            byte[] dataAux = BitConverter.GetBytes(UInt16.Parse(data[3], System.Globalization.NumberStyles.AllowHexSpecifier));
            guid.data4[0] = dataAux[1]; guid.data4[1] = dataAux[0];
            dataAux = BitConverter.GetBytes(Int64.Parse(data[4], System.Globalization.NumberStyles.AllowHexSpecifier));
            guid.data4[2] = dataAux[5]; guid.data4[3] = dataAux[4];
            guid.data4[4] = dataAux[3]; guid.data4[5] = dataAux[2];
            guid.data4[6] = dataAux[1]; guid.data4[7] = dataAux[0];

            return guid;
        }

        public unsafe static List<string> GetDevices(string identification, string classGuid)
        {
            string devicePath = string.Empty;
            int deviceNumber = 0;
            int hardwareDevInfo = 0;
            UsbNativeType.GUID guid;
            List<string> devices = new List<string>();

            if (classGuid != null)
            {
                guid = ParseClassGuid(classGuid);
            }
            else
            {
                // falback to human interface devices GUID class
                guid = new UsbNativeType.GUID();
                UsbNativeApi.HidD_GetHidGuid(ref guid);
            }


            hardwareDevInfo = UsbNativeApi.SetupDiGetClassDevs(
                ref guid,
                null,
                null,
                UsbNativeApi.DIGCF_INTERFACEDEVICE | UsbNativeApi.DIGCF_PRESENT);

            // iterate through the available GUID interface devices
            while (true)
            {
                devicePath = GetDevicePath(guid, hardwareDevInfo, deviceNumber);
                if ((devicePath == null) || devicePath.Equals(String.Empty))
                {
                    UsbNativeApi.SetupDiDestroyDeviceInfoList(hardwareDevInfo);
                    return devices;
                }
                // get 2Virt devices interfaces only
                if ((identification == null) || (devicePath.ToUpper().IndexOf(identification) > 0))
                {
                    devices.Add(devicePath);
                }
                deviceNumber++;
            }
        }

        public unsafe static IntPtr OpenDevice(string devicePath, bool sync)
        {
            uint fileAttributes = UsbNativeApi.FILE_ATTRIBUTE_NORMAL;
            if (!sync)
                fileAttributes |= UsbNativeApi.FILE_FLAG_OVERLAPPED;

            return (IntPtr)UsbNativeApi.CreateFile(devicePath,
                        UsbNativeApi.GENERIC_WRITE | UsbNativeApi.GENERIC_READ,
                        UsbNativeApi.FILE_SHARE_WRITE | UsbNativeApi.FILE_SHARE_READ,
                        0, // default security
                        UsbNativeApi.OPEN_EXISTING,
                        fileAttributes,
                        0);
        }

        public unsafe static void CloseDevice(IntPtr handle)
        {
            UsbNativeApi.CloseHandle((int)handle);
        }

        public static uint CTL_CODE(uint deviceType, uint function, uint method, uint access)
        {
            return (deviceType << 16) | (access << 14) | (function << 2) | method;
        }

        public unsafe static IoStatus DeviceIoControl(IntPtr handle, IOCTLcommand command)
        {
            GCHandle hInput = new GCHandle();
            GCHandle hOutput = new GCHandle();
            IoStatus status = new IoStatus();

            status.buffer = new byte[command.outputMaxSize];
            status.size = 0;
            status.error = USBError.SUCCESS;

            try
            {
                // pin the buffers into place
                hInput = GCHandle.Alloc(command.inputBuffer, GCHandleType.Pinned);
                hOutput = GCHandle.Alloc(status.buffer, GCHandleType.Pinned);

                if (UsbNativeApi.DeviceIoControl(
                     handle,
                     command.ioctlNo,
                     hInput.AddrOfPinnedObject(), (command.inputBuffer == null) ? 0 : (uint)command.inputBuffer.Length,
                     hOutput.AddrOfPinnedObject(), (uint)command.outputMaxSize,
                     ref status.size,
                     (IntPtr)0) != 0)
                    status.error = USBError.WIN_API_SPECIFIC;
            }
            finally
            {
                if (hInput.IsAllocated)
                    hInput.Free();
                if (hOutput.IsAllocated)
                    hOutput.Free();
            }

            return status;
        }
           
    }
}
