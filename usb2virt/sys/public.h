/*++

Copyright (c) 2Virt.com

	This product is made available subject to the terms of
	GNU Lesser General Public License Version 3

Module Name:

    public.h

Abstract:

Environment:

    User & Kernel mode

--*/

#ifndef _PUBLIC_H
#define _PUBLIC_H

#include <initguid.h>

// {A9503447-6B3B-4581-99CF-663557F7E73A}
DEFINE_GUID(GUID_DEVINTERFACE_USB2VIRT,
            0xA9503447, 0x6B3B, 0x4581, 0x99, 0xCF, 0x66, 0x35, 0x57, 0xF7, 0xE7, 0x3A);

//
// Defines the service description list returned by the GET_WEBSERVICES IOCTL
//
typedef struct
{
	const char* Description;  // NULL terminated string
	const char* ListServices; // NULL terminated Query string to get the list of services
}WebServer, *PWebServer;

typedef struct
{
	ULONG      noWebServers;
	PWebServer webServer; // serialized web server list the device can access
}Usb2WebServer, *PUsb2WebServer;


//
// Define the structures that will be used by the IOCTL 
//  interface to the driver
//

#define IOCTL_INDEX            0x800
#define FILE_DEVICE_USB2VIRT   0x65500

#define IOCTL_USB2VIRT_GET_WEBSERVICES CTL_CODE(FILE_DEVICE_USB2VIRT, \
                                                     IOCTL_INDEX,     \
                                                     METHOD_BUFFERED, \
                                                     FILE_READ_ACCESS)
                                                   
#define IOCTL_USB2VIRT_GET_WSDL        CTL_CODE(FILE_DEVICE_USB2VIRT, \
                                                     IOCTL_INDEX + 1, \
                                                     METHOD_BUFFERED, \
                                                     FILE_READ_ACCESS)

#endif

