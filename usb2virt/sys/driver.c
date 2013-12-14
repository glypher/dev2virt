/*++

Copyright (c) 2Virt.com

	This product is made available subject to the terms of
	GNU Lesser General Public License Version 3

Module Name:

    driver.c

Abstract:

    Main module.

    This driver is for 2Virt Web Service USB Device in order to query
    certain web services through the means of an USB device. The user
    will IOCTL to get the available web services, wsdl for each web service
    and will also be able to call an web service method. In this way the
    location of the underlying web service is hidden.

    The emulated 2Virt device firmware supports a single configuration,
    with one interface and 2 endpoints:

    Endpoint 1 is a bulk endpoint for sending streams of data to the
    device (method calls for example)
    Endpoint 2 is a bulk out endpoint to receive data from the device
    (the returned method result for example)

    Vendor ID of the device is 0xE2C0 and Product ID is 0xA123.

Environment:

    Kernel mode only

--*/

#include "usb2virt.h"

#if defined(EVENT_TRACING)
//
// The trace message header (.tmh) file must be included in a source file
// before any WPP macro calls and after defining a WPP_CONTROL_GUIDS
// macro (defined in toaster.h). During the compilation, WPP scans the source
// files for DoTraceMessage() calls and builds a .tmh file which stores a unique
// data GUID for each message, the text resource string for each message,
// and the data types of the variables passed in for each message.  This file
// is automatically generated and used during post-processing.
//
#include "driver.tmh"
#else
ULONG DebugLevel = TRACE_LEVEL_INFORMATION;
ULONG DebugFlag = 0xff;
#endif

#ifdef ALLOC_PRAGMA
#pragma alloc_text(INIT, DriverEntry)
#pragma alloc_text(PAGE, DriverContextCleanup)
#endif

__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
DriverEntry(
    __in PDRIVER_OBJECT  DriverObject,
    __in PUNICODE_STRING RegistryPath
    )
/*++

Routine Description:
    DriverEntry initializes the driver and is the first routine called by the
    system after the driver is loaded.

Parameters Description:

    DriverObject - represents the instance of the function driver that is loaded
    into memory. DriverEntry must initialize members of DriverObject before it
    returns to the caller. DriverObject is allocated by the system before the
    driver is loaded, and it is released by the system after the system unloads
    the function driver from memory.

    RegistryPath - represents the driver specific path in the Registry.
    The function driver can use the path to store driver related data between
    reboots. The path does not store hardware instance specific data.

Return Value:

    STATUS_SUCCESS if successful,
    STATUS_UNSUCCESSFUL otherwise.

--*/
{
    WDF_DRIVER_CONFIG       config;
    NTSTATUS                status;
    WDF_OBJECT_ATTRIBUTES   attributes;

    //
    // Initialize WPP Tracing
    //
    WPP_INIT_TRACING( DriverObject, RegistryPath );

    TraceEvents(TRACE_LEVEL_INFORMATION, DBG_INIT,
                       "2Virt Web Service Driver - Driver Simulation Framework Edition.\n");

    TraceEvents(TRACE_LEVEL_INFORMATION, DBG_INIT,
                "Built %s %s\n", __DATE__, __TIME__);

    //
    // Register with ETW (unified tracing)
    // 
    EventRegisterUSB2VIRT();

    //
    // Initialize driver config to control the attributes that
    // are global to the driver. Note that framework by default
    // provides a driver unload routine. If you create any resources
    // in the DriverEntry and want to be cleaned in driver unload,
    // you can override that by manually setting the EvtDriverUnload in the
    // config structure. In general xxx_CONFIG_INIT macros are provided to
    // initialize most commonly used members.
    //

    WDF_DRIVER_CONFIG_INIT(
        &config,
        DeviceAdd
        );

    //
    // Register a cleanup callback so that we can call WPP_CLEANUP when
    // the framework driver object is deleted during driver unload.
    //
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.EvtCleanupCallback = DriverContextCleanup;

    //
    // Create a framework driver object to represent our driver.
    //
    status = WdfDriverCreate(
        DriverObject,
        RegistryPath,
        &attributes,  // Driver Object Attributes
        &config,      // Driver Config Info
        WDF_NO_HANDLE // hDriver
        );

    if (!NT_SUCCESS(status)) {

        TraceEvents(TRACE_LEVEL_ERROR, DBG_INIT,
                "WdfDriverCreate failed with status 0x%x\n", status);
        //
        // Cleanup tracing here because DriverContextCleanup will not be called
        // as we have failed to create WDFDRIVER object itself.
        // Please note that if your return failure from DriverEntry after the
        // WDFDRIVER object is created successfully, you don't have to
        // call WPP cleanup because in those cases DriverContextCleanup
        // will be executed when the framework deletes the DriverObject.
        //
        WPP_CLEANUP(DriverObject);
        EventUnregisterUSB2VIRT();
    }

    return status;
}

VOID
DriverContextCleanup(
    WDFDRIVER Driver
    )
/*++
Routine Description:

    Free resources allocated in DriverEntry that are automatically
    cleaned up framework.

Arguments:

    Driver - handle to a WDF Driver object.

Return Value:

    VOID.

--*/
{
    PAGED_CODE ();

    UNREFERENCED_PARAMETER(Driver);

    TraceEvents(TRACE_LEVEL_INFORMATION, DBG_INIT,
                    "--> Usb2VirtDriverContextCleanup\n");

    WPP_CLEANUP( WdfDriverWdmGetDriverObject( Driver ));
    EventUnregisterUSB2VIRT();

}

#if !defined(EVENT_TRACING)

VOID
TraceEvents (
    __in ULONG DebugPrintLevel,
    __in ULONG DebugPrintFlag,
    __drv_formatString(printf)
    __in PCSTR DebugMessage,
    ...
    )

/*++

Routine Description:

    Debug print for the sample driver.

Arguments:

    TraceEventsLevel - print level between 0 and 3, with 3 the most verbose

Return Value:

    None.

 --*/
 {
#if DBG
#define     TEMP_BUFFER_SIZE        1024
    va_list    list;
    CHAR       debugMessageBuffer[TEMP_BUFFER_SIZE];
    NTSTATUS   status;

    va_start(list, DebugMessage);

    if (DebugMessage) {

        //
        // Using new safe string functions instead of _vsnprintf.
        // This function takes care of NULL terminating if the message
        // is longer than the buffer.
        //
        status = RtlStringCbVPrintfA( debugMessageBuffer,
                                      sizeof(debugMessageBuffer),
                                      DebugMessage,
                                      list );
        if(!NT_SUCCESS(status)) {

            DbgPrint (_DRIVER_NAME_": RtlStringCbVPrintfA failed 0x%x\n", status);
            return;
        }
        if (DebugPrintLevel <= TRACE_LEVEL_ERROR ||
            (DebugPrintLevel <= DebugLevel &&
             ((DebugPrintFlag & DebugFlag) == DebugPrintFlag))) {
            DbgPrint("%s %s", _DRIVER_NAME_, debugMessageBuffer);
        }
    }
    va_end(list);

    return;
#else
    UNREFERENCED_PARAMETER(TraceEventsLevel);
    UNREFERENCED_PARAMETER(TraceEventsFlag);
    UNREFERENCED_PARAMETER(DebugMessage);
#endif
}

#endif




