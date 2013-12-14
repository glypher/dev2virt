dev2virt
========

*Full documentation can be found at [2Virt](http://www.2virt.com).*

2Virt Devices are meant to be a special type of USB devices that offer USB interfaces for accessing different services of the Virtualization platform.

Different 2Virt Interfaces can be used for adding the to a USB device, that a client program, running in a Hardware Virtual Machine can use to take control of platform services, even from priviledged domains.

USB Device simulation
-------------------------

softusbproxy directory contains the implementation of the COM objects which simulates a USB bulk device using Microsoft's Device Simulation Framework ([DSF](http://msdn.microsoft.com/en-us/library/windows/hardware/gg454516.aspx))

You will need to install [Windows Driver Kit](http://www.microsoft.com/en-us/download/details.aspx?id=11800) in order to install DSF.

Once compiled you will need to register the resulted COM object to the system. Run regsvr32 SoftUSBProxy.dll as an administrator.

You can simulate the plug-in of a such device on your machine using the RunUSBProxy.wsf VBscript: cscript.exe RunUSBProxy.wsf

The device is described by the following:
 VendorID=02A0
 ProductID=A123

Dev2Virt USB Device Driver
-------------------------

usb2virt directory contains the sources for a USB device driver implementing a Bulk interface.

If you add the device using DSF framework and COM device implementation you will notice you will be prompted for a device driver.
You will need to locate the compiled usb2virt.sys file which will load the device's functionality.

Box2Virt application
-------------------------

Box2Virt directory contains a C# app that will use the device files exposed by usb2virt driver to issue IOCTLs and read/write to comunicate with the device.


