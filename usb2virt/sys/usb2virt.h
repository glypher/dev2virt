/*++

Copyright (c) 2Virt.com

	This product is made available subject to the terms of
	GNU Lesser General Public License Version 3

Module Name:

    usb2virt.h

Abstract:

    Contains structure definitions and function prototypes private to
    the driver.

Environment:

    Kernel mode

--*/

#pragma warning(disable:4200)  // nameless struct/union
#pragma warning(disable:4201)  // nameless struct/union
#pragma warning(disable:4214)  // bit field types other than int
#include <initguid.h>
#include <ntddk.h>
#include "usbdi.h"
#include "usbdlib.h"
#include "public.h"
#include "driverspecs.h"

#pragma warning(default:4200)
#pragma warning(default:4201)
#pragma warning(default:4214)

#include <wdf.h>
#include <wdfusb.h>
#define NTSTRSAFE_LIB
#include <ntstrsafe.h>

#include "trace.h"

//
// Include auto-generated ETW event functions (created by MC.EXE from 
// osrusbfx2.man)
//
#include "usb2virtEvents.h"

#ifndef _PRIVATE_H
#define _PRIVATE_H

#define POOL_TAG (ULONG) 'FRSO'
#define _DRIVER_NAME_ "USB2VIRT"

#define TEST_BOARD_TRANSFER_BUFFER_SIZE (64*1024)
#define DEVICE_DESC_LENGTH 256

extern const __declspec(selectany) LONGLONG DEFAULT_CONTROL_TRANSFER_TIMEOUT = 5 * -1 * WDF_TIMEOUT_TO_SEC;

//
// A structure representing the instance information associated with
// this particular device.
//

typedef struct _DEVICE_CONTEXT {

    WDFUSBDEVICE                    UsbDevice;

    WDFUSBINTERFACE                 UsbInterface;

    WDFUSBPIPE                      BulkReadPipe;

    WDFUSBPIPE                      BulkWritePipe;

    ULONG                           UsbDeviceTraits;

    //
    // The following fields are used during event logging to 
    // report the events relative to this specific instance 
    // of the device.
    //
    WDFMEMORY                       DeviceNameMemory;
    PCWSTR                          DeviceName;

    WDFMEMORY                       LocationMemory;
    PCWSTR                          Location;

    // The Web Server List that our USB 2Virt device can access
    Usb2WebServer                   UsbServer;
} DEVICE_CONTEXT, *PDEVICE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, GetDeviceContext)

extern ULONG DebugLevel;


DRIVER_INITIALIZE                  DriverEntry;

EVT_WDF_OBJECT_CONTEXT_CLEANUP     DriverContextCleanup;

EVT_WDF_DRIVER_DEVICE_ADD          DeviceAdd;

EVT_WDF_DEVICE_PREPARE_HARDWARE    DevicePrepareHardware;

EVT_WDF_IO_QUEUE_IO_READ           BulkIoRead;

EVT_WDF_IO_QUEUE_IO_WRITE          BulkIoWrite;

EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL IoDeviceControl;

EVT_WDF_IO_QUEUE_IO_STOP           EvtIoStop;

EVT_WDF_REQUEST_COMPLETION_ROUTINE EvtRequestReadCompletionRoutine;

EVT_WDF_REQUEST_COMPLETION_ROUTINE EvtRequestWriteCompletionRoutine;

__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
SelectInterfaces(
    __in WDFDEVICE Device
    );

__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
SetPowerPolicy(
        __in WDFDEVICE Device
    );

__drv_requiresIRQL(PASSIVE_LEVEL)
VOID
GetDeviceEventLoggingNames(
    __in WDFDEVICE Device
    );

__drv_requiresIRQL(PASSIVE_LEVEL)
PCHAR
DbgDevicePowerString(
    __in WDF_POWER_DEVICE_STATE Type
    );

FORCEINLINE
GUID
RequestToActivityId(
    __in WDFREQUEST Request
    )
{
    GUID activity = {0};
    RtlCopyMemory(&activity, &Request, sizeof(WDFREQUEST));
    return activity;
}

FORCEINLINE
GUID
DeviceToActivityId(
    __in WDFDEVICE Device
    )
{
    GUID activity = {0};
    RtlCopyMemory(&activity, &Device, sizeof(WDFDEVICE));
    return activity;
}


#endif


