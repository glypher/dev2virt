/*++

Copyright (c) 2Virt.com

	This product is made available subject to the terms of
	GNU Lesser General Public License Version 3

Module Name:

    Ioctl.c

Abstract:

    Implementation for the IOCTL's for 2Virt device's web service discovery

Environment:

    Kernel mode only

--*/

#include <usb2virt.h>

#if defined(EVENT_TRACING)
#include "ioctl.tmh"
#endif

#pragma alloc_text(PAGE, IoDeviceControl)

const char* kUnImplemented = "Not yet implemented.";


VOID
IoDeviceControl(
    __in WDFQUEUE   Queue,
    __in WDFREQUEST Request,
    __in size_t     OutputBufferLength,
    __in size_t     InputBufferLength,
    __in ULONG      IoControlCode    
    )
/*++

Routine Description:

    This event is called when the framework receives IRP_MJ_DEVICE_CONTROL
    requests from the system.

Arguments:

    Queue - Handle to the framework queue object that is associated
            with the I/O request.
    Request - Handle to a framework request object.

    OutputBufferLength - length of the request's output buffer,
                        if an output buffer is available.
    InputBufferLength - length of the request's input buffer,
                        if an input buffer is available.

    IoControlCode - the driver-defined or system-defined I/O control code
                    (IOCTL) that is associated with the request.
Return Value:

    VOID

--*/
{
    WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
    size_t              bytesReturned = 0;
    PVOID               switchState   = NULL;
    WDFMEMORY*          OutputMemory  = NULL;
    NTSTATUS            status        = STATUS_INVALID_DEVICE_REQUEST;

    UNREFERENCED_PARAMETER(InputBufferLength);
    UNREFERENCED_PARAMETER(OutputBufferLength);

    PAGED_CODE();

    TraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "--> IoDeviceControl\n");
    //
    // initialize variables
    //
    device      = WdfIoQueueGetDevice(Queue);
    pDevContext = GetDeviceContext(device);

    switch(IoControlCode) {

    case IOCTL_USB2VIRT_GET_WEBSERVICES: {
    	PUsb2WebServer servers;
    	PCHAR          server;
    	ULONG          requiredSize = 0;
    	ULONG          iter = 0;
    	PWebServer     usbServer = NULL;
    	//
    	// First compute the required size
    	//
    	requiredSize = sizeof(pDevContext->UsbServer.noWebServers);
    	usbServer = pDevContext->UsbServer.webServer;
    	for (iter = 0; iter < pDevContext->UsbServer.noWebServers; iter++, usbServer++) {
    		requiredSize += 2;
    		if (usbServer->Description)
    			requiredSize += strlen(usbServer->Description);
    		if (usbServer->ListServices)
    			requiredSize += strlen(usbServer->ListServices);
    	}

    	//
    	// Get the buffer - make sure the buffer is big enough
    	//
    	status = WdfRequestRetrieveOutputBuffer(Request,
										(size_t)requiredSize,  // MinimumRequired
										&servers,
										NULL);
    	if (!NT_SUCCESS(status)) {
    	    TraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL,
    	    "WdfRequestRetrieveOutputBuffer failed 0x%x\n", status);
    	     break;
    	}

    	//
    	// Now copy the serialized data
    	//
    	servers->noWebServers = pDevContext->UsbServer.noWebServers;
    	server    = (PCHAR)&servers->webServer;
    	usbServer = pDevContext->UsbServer.webServer;
    	for (iter = 0; iter < servers->noWebServers; iter++, usbServer++) {
    		int size = 1;
    		if (usbServer->Description) {
    			size += strlen(usbServer->Description);
    			RtlCopyMemory(server, usbServer->Description, size);
    		} else
    			*server = '\0';
    		server += size;

    		size = 1;
    		if (usbServer->ListServices) {
    			size += strlen(usbServer->ListServices);
    			RtlCopyMemory(server, usbServer->ListServices, size);
    		} else
    			*server = '\0';
    		server += size;
    	}
    	bytesReturned = requiredSize;
    	status = STATUS_SUCCESS;
    	break;
    }
    case IOCTL_USB2VIRT_GET_WSDL : {
    	//
    	// Set up the warning message
    	//
        status = WdfRequestRetrieveOutputMemory(Request, &OutputMemory);
        if (!NT_SUCCESS(status)) {
            TraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL,
                "User's output memory cannot be optained for this IOCTL\n");
            bytesReturned = 0;
            break;
        }
        status = WdfMemoryCopyFromBuffer(OutputMemory, 0,
        		kUnImplemented, sizeof(kUnImplemented));
        if (!NT_SUCCESS(status)) {
			TraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL,
				"User's output buffer is too small for this IOCTL\n");
			bytesReturned = 0;
			break;
        }
        bytesReturned = sizeof(kUnImplemented);
        break;
    }
    default : {
        status = STATUS_INVALID_DEVICE_REQUEST;
        break;
    }
    }

    WdfRequestCompleteWithInformation(Request, status, bytesReturned);

    TraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "<-- IoDeviceControl\n");

    return;
}
