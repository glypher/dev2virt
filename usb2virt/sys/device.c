/*++

Copyright (c) 2Virt.com

	This product is made available subject to the terms of
	GNU Lesser General Public License Version 3

Module Name:

    device.c

Abstract:

    2Virt USB device driver for calling Web Services

Environment:

    Kernel mode only


--*/

#include "usb2virt.h"

#if defined(EVENT_TRACING)
#include "device.tmh"
#endif

#ifdef ALLOC_PRAGMA
#pragma alloc_text(PAGE, DeviceAdd)
#pragma alloc_text(PAGE, DevicePrepareHardware)
#pragma alloc_text(PAGE, SelectInterfaces)
#pragma alloc_text(PAGE, SetPowerPolicy)
#pragma alloc_text(PAGE, GetDeviceEventLoggingNames)
#endif


const char* kAxisDescription  = "Axis 1.4 on Tomcat 6.0";
const char* kAxisListServices = "GET /axis/servlet/AxisServlet HTTP/1.1\r\nHost: localhost\r\n\r\n";
WebServer   AxisServer;

NTSTATUS
DeviceAdd(
    WDFDRIVER Driver,
    PWDFDEVICE_INIT DeviceInit
    )
/*++
Routine Description:

    DeviceAdd is called by the framework in response to AddDevice
    call from the PnP manager. We create and initialize a device object to
    represent a new instance of the device. All the software resources
    should be allocated in this callback.

Arguments:

    Driver - Handle to a framework driver object created in DriverEntry

    DeviceInit - Pointer to a framework-allocated WDFDEVICE_INIT structure.

Return Value:

    NTSTATUS

--*/
{
    WDF_PNPPOWER_EVENT_CALLBACKS        pnpPowerCallbacks;
    WDF_OBJECT_ATTRIBUTES               attributes;
    NTSTATUS                            status;
    WDFDEVICE                           device;
    WDF_DEVICE_PNP_CAPABILITIES         pnpCaps;
    WDF_IO_QUEUE_CONFIG                 ioQueueConfig;
    PDEVICE_CONTEXT                     pDevContext;
    WDFQUEUE                            queue;
    GUID                                activity;

    UNREFERENCED_PARAMETER(Driver);

    PAGED_CODE();

    TraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,"--> DeviceAdd routine\n");

    //
    // Initialize the pnpPowerCallbacks structure.  Callback events for PNP
    // and Power are specified here.  If you don't supply any callbacks,
    // the Framework will take appropriate default actions based on whether
    // DeviceInit is initialized to be an FDO, a PDO or a filter device
    // object.
    //
    WDF_PNPPOWER_EVENT_CALLBACKS_INIT(&pnpPowerCallbacks);
    //
    // For usb devices, PrepareHardware callback is the to place select the
    // interface and configure the device.
    //
    pnpPowerCallbacks.EvtDevicePrepareHardware = DevicePrepareHardware;

    WdfDeviceInitSetPnpPowerEventCallbacks(DeviceInit, &pnpPowerCallbacks);

    WdfDeviceInitSetIoType(DeviceInit, WdfDeviceIoBuffered);

    //
    // Now specify the size of device extension where we track per device
    // context.DeviceInit is completely initialized. So call the framework
    // to create the device and attach it to the lower stack.
    //
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, DEVICE_CONTEXT);

    status = WdfDeviceCreate(&DeviceInit, &attributes, &device);
    if (!NT_SUCCESS(status)) {
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
            "WdfDeviceCreate failed with Status code %!STATUS!\n", status);
        return status;
    }

    //
    // Setup the activity ID so that we can log events using it.
    //
    activity = DeviceToActivityId(device);

    //
    // Get the DeviceObject context by using accessor function specified in
    // the WDF_DECLARE_CONTEXT_TYPE_WITH_NAME macro for DEVICE_CONTEXT.
    //
    pDevContext = GetDeviceContext(device);

    //
    // Get the device's friendly name and location so that we can use it in 
    // error logging.  If this fails then it will setup dummy strings.
    //
    GetDeviceEventLoggingNames(device);

    //
    // Tell the framework to set the SurpriseRemovalOK in the DeviceCaps so
    // that you don't get the popup in usermode (on Win2K) when you surprise
    // remove the device.
    //
    WDF_DEVICE_PNP_CAPABILITIES_INIT(&pnpCaps);
    pnpCaps.SurpriseRemovalOK = WdfTrue;

    WdfDeviceSetPnpCapabilities(device, &pnpCaps);

    //
    // Create a parallel default queue and register an event callback to
    // receive ioctl requests. We will create separate queues for
    // handling read and write requests. All other requests will be
    // completed with error status automatically by the framework.
    //
    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&ioQueueConfig,
                             WdfIoQueueDispatchParallel);

    ioQueueConfig.EvtIoDeviceControl    = IoDeviceControl;

    status = WdfIoQueueCreate(device,
                             &ioQueueConfig,
                             WDF_NO_OBJECT_ATTRIBUTES,
                             &queue);// pointer to default queue
    if (!NT_SUCCESS(status)) {
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                            "WdfIoQueueCreate failed  %!STATUS!\n", status);
        goto Error;
    }

    //
    // We will create a separate sequential queue and configure it
    // to receive read requests.  We also need to register a EvtIoStop
    // handler so that we can acknowledge requests that are pending
    // at the target driver.
    //
    WDF_IO_QUEUE_CONFIG_INIT(&ioQueueConfig, WdfIoQueueDispatchSequential);

    ioQueueConfig.EvtIoRead = BulkIoRead;
    ioQueueConfig.EvtIoStop = EvtIoStop;

    status = WdfIoQueueCreate(
                 device,
                 &ioQueueConfig,
                 WDF_NO_OBJECT_ATTRIBUTES,
                 &queue // queue handle
             );

    if (!NT_SUCCESS (status)) {
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
            "WdfIoQueueCreate failed 0x%x\n", status);
        goto Error;
    }

    status = WdfDeviceConfigureRequestDispatching(
                    device,
                    queue,
                    WdfRequestTypeRead);

    if(!NT_SUCCESS (status)){
        ASSERT(NT_SUCCESS(status));
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                    "WdfDeviceConfigureRequestDispatching failed 0x%x\n", status);
        goto Error;
    }


    //
    // We will create another sequential queue and configure it
    // to receive write requests.
    //
    WDF_IO_QUEUE_CONFIG_INIT(&ioQueueConfig, WdfIoQueueDispatchSequential);

    ioQueueConfig.EvtIoWrite = BulkIoWrite;
    ioQueueConfig.EvtIoStop  = EvtIoStop;

    status = WdfIoQueueCreate(
                 device,
                 &ioQueueConfig,
                 WDF_NO_OBJECT_ATTRIBUTES,
                 &queue // queue handle
             );

    if (!NT_SUCCESS (status)) {
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
            "WdfIoQueueCreate failed 0x%x\n", status);
        goto Error;
    }

     status = WdfDeviceConfigureRequestDispatching(
                    device,
                    queue,
                    WdfRequestTypeWrite);

    if(!NT_SUCCESS (status)){
        ASSERT(NT_SUCCESS(status));
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                    "WdfDeviceConfigureRequestDispatching failed 0x%x\n", status);
        goto Error;
    }

    //
    // Register a device interface so that app can find our device and talk to it.
    //
    status = WdfDeviceCreateDeviceInterface(device,
                        (LPGUID) &GUID_DEVINTERFACE_USB2VIRT,
                        NULL);// Reference String
    if (!NT_SUCCESS(status)) {
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                 "WdfDeviceCreateDeviceInterface failed  %!STATUS!\n", status);
        goto Error;
    }

    //
    // Add our Axis web server backend
    //
    pDevContext->UsbServer.noWebServers = 1;
    AxisServer.Description  = kAxisDescription;
    AxisServer.ListServices = kAxisListServices;
    pDevContext->UsbServer.webServer = &AxisServer;

    TraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "<-- DeviceAdd\n");

    return status;
    
Error:
	pDevContext->UsbServer.noWebServers = 0;

    //
    // Log fail to add device to the event log
    //
    EventWriteFailAddDevice(&activity, 
                            pDevContext->DeviceName, 
                            pDevContext->Location,
                            status);

    return status;
}


NTSTATUS
DevicePrepareHardware(
    WDFDEVICE Device,
    WDFCMRESLIST ResourceList,
    WDFCMRESLIST ResourceListTranslated
    )
/*++

Routine Description:

    In this callback, the driver does whatever is necessary to make the
    hardware ready to use.  In the case of a USB device, this involves
    reading and selecting descriptors.

Arguments:

    Device - handle to a device

Return Value:

    NT status value

--*/
{
    NTSTATUS                            status;
    PDEVICE_CONTEXT                     pDeviceContext;
    WDF_USB_DEVICE_INFORMATION          deviceInfo;
    ULONG                               waitWakeEnable;

    UNREFERENCED_PARAMETER(ResourceList);
    UNREFERENCED_PARAMETER(ResourceListTranslated);
    waitWakeEnable = FALSE;
    PAGED_CODE();

    TraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "--> DevicePrepareHardware\n");

    pDeviceContext = GetDeviceContext(Device);

    //
    // Create a USB device handle so that we can communicate with the
    // underlying USB stack. The WDFUSBDEVICE handle is used to query,
    // configure, and manage all aspects of the USB device.
    // These aspects include device properties, bus properties,
    // and I/O creation and synchronization. We only create device the first
    // the PrepareHardware is called. If the device is restarted by pnp manager
    // for resource rebalance, we will use the same device handle but then select
    // the interfaces again because the USB stack could reconfigure the device on
    // restart.
    //
    if (pDeviceContext->UsbDevice == NULL) {
        status = WdfUsbTargetDeviceCreate(Device,
                                    WDF_NO_OBJECT_ATTRIBUTES,
                                    &pDeviceContext->UsbDevice);
        if (!NT_SUCCESS(status)) {
            TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                 "WdfUsbTargetDeviceCreate failed with Status code %!STATUS!\n", status);
            return status;
        }
    }    

    //
    // Retrieve USBD version information, port driver capabilites and device
    // capabilites such as speed, power, etc.
    //
    WDF_USB_DEVICE_INFORMATION_INIT(&deviceInfo);

    status = WdfUsbTargetDeviceRetrieveInformation(
                                pDeviceContext->UsbDevice,
                                &deviceInfo);
    if (NT_SUCCESS(status)) {
        TraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "IsDeviceHighSpeed: %s\n",
            (deviceInfo.Traits & WDF_USB_DEVICE_TRAIT_AT_HIGH_SPEED) ? "TRUE" : "FALSE");
        TraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,
                    "IsDeviceSelfPowered: %s\n",
            (deviceInfo.Traits & WDF_USB_DEVICE_TRAIT_SELF_POWERED) ? "TRUE" : "FALSE");

        waitWakeEnable = deviceInfo.Traits &
                            WDF_USB_DEVICE_TRAIT_REMOTE_WAKE_CAPABLE;

        TraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,
                            "IsDeviceRemoteWakeable: %s\n",
                            waitWakeEnable ? "TRUE" : "FALSE");
        //
        // Save these for use later. 
        //
        pDeviceContext->UsbDeviceTraits = deviceInfo.Traits;
    }
    
    status = SelectInterfaces(Device);
    if (!NT_SUCCESS(status)) {
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                    "SelectInterfaces failed 0x%x\n", status);
        return status;
    }
    
    //
    // Enable wait-wake and idle timeout if the device supports it
    //
    if (waitWakeEnable) {
        status = SetPowerPolicy(Device);
        if (!NT_SUCCESS (status)) {
            TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                                "SetPowerPolicy failed  %!STATUS!\n", status);
            return status;
        }
    }

    TraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "<-- DevicePrepareHardware\n");

    return status;
}


__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
SetPowerPolicy(
    __in WDFDEVICE Device
    )
{
    WDF_DEVICE_POWER_POLICY_IDLE_SETTINGS idleSettings;
    WDF_DEVICE_POWER_POLICY_WAKE_SETTINGS wakeSettings;
    NTSTATUS    status = STATUS_SUCCESS;

    PAGED_CODE();

    //
    // Init the idle policy structure.
    //
    WDF_DEVICE_POWER_POLICY_IDLE_SETTINGS_INIT(&idleSettings, IdleUsbSelectiveSuspend);
    idleSettings.IdleTimeout = 10000; // 10-sec

    status = WdfDeviceAssignS0IdleSettings(Device, &idleSettings);
    if ( !NT_SUCCESS(status)) {
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                "WdfDeviceSetPowerPolicyS0IdlePolicy failed %x\n", status);
        return status;
    }

    //
    // Init wait-wake policy structure.
    //
    WDF_DEVICE_POWER_POLICY_WAKE_SETTINGS_INIT(&wakeSettings);

    status = WdfDeviceAssignSxWakeSettings(Device, &wakeSettings);
    if (!NT_SUCCESS(status)) {
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
            "WdfDeviceAssignSxWakeSettings failed %x\n", status);
        return status;
    }

    return status;
}


__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
SelectInterfaces(
    __in WDFDEVICE Device
    )
/*++

Routine Description:

    This helper routine selects the configuration, interface and
    creates a context for every pipe (end point) in that interface.

Arguments:

    Device - Handle to a framework device

Return Value:

    NT status value

--*/
{
    WDF_USB_DEVICE_SELECT_CONFIG_PARAMS configParams;
    NTSTATUS                            status;
    PDEVICE_CONTEXT                     pDeviceContext;
    WDFUSBPIPE                          pipe;
    WDF_USB_PIPE_INFORMATION            pipeInfo;
    UCHAR                               index;
    UCHAR                               numberConfiguredPipes;

    PAGED_CODE();

    pDeviceContext = GetDeviceContext(Device);

    WDF_USB_DEVICE_SELECT_CONFIG_PARAMS_INIT_SINGLE_INTERFACE( &configParams);

    status = WdfUsbTargetDeviceSelectConfig(pDeviceContext->UsbDevice,
                                        WDF_NO_OBJECT_ATTRIBUTES,
                                        &configParams);
    if(!NT_SUCCESS(status)) {

        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                        "WdfUsbTargetDeviceSelectConfig failed %!STATUS! \n",
                        status);

        //
        // Since the 2Virt device is capable of working at high speed, the only reason
        // the device would not be working at high speed is if the port doesn't 
        // support it. If the port doesn't support high speed it is a 1.1 port
        //
        if ((pDeviceContext->UsbDeviceTraits & WDF_USB_DEVICE_TRAIT_AT_HIGH_SPEED) == 0) {
            GUID activity = DeviceToActivityId(Device);

            EventWriteSelectConfigFailure(
                &activity,
                pDeviceContext->DeviceName,
                pDeviceContext->Location,
                status
                );
        }
        
        return status;
    }


    pDeviceContext->UsbInterface =
                configParams.Types.SingleInterface.ConfiguredUsbInterface;

    numberConfiguredPipes = configParams.Types.SingleInterface.NumberConfiguredPipes;

    //
    // Get pipe handles
    //
    for(index=0; index < numberConfiguredPipes; index++) {

        WDF_USB_PIPE_INFORMATION_INIT(&pipeInfo);

        pipe = WdfUsbInterfaceGetConfiguredPipe(
            pDeviceContext->UsbInterface,
            index, //PipeIndex,
            &pipeInfo
            );
        //
        // Tell the framework that it's okay to read less than
        // MaximumPacketSize
        //
        WdfUsbTargetPipeSetNoMaximumPacketSizeCheck(pipe);

        if(WdfUsbPipeTypeBulk == pipeInfo.PipeType &&
                WdfUsbTargetPipeIsInEndpoint(pipe)) {
            TraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,
                    "BulkInput Pipe is 0x%p\n", pipe);
            pDeviceContext->BulkReadPipe = pipe;
        }

        if(WdfUsbPipeTypeBulk == pipeInfo.PipeType &&
                WdfUsbTargetPipeIsOutEndpoint(pipe)) {
            TraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,
                    "BulkOutput Pipe is 0x%p\n", pipe);
            pDeviceContext->BulkWritePipe = pipe;
        }

    }

    //
    // If we didn't find all the 3 pipes, fail the start.
    //
    if(!(pDeviceContext->BulkWritePipe && pDeviceContext->BulkReadPipe)) {
        status = STATUS_INVALID_DEVICE_STATE;
        TraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                            "Device is not configured properly %!STATUS!\n",
                            status);
    }

    return status;
}

__drv_requiresIRQL(PASSIVE_LEVEL)
VOID
GetDeviceEventLoggingNames(
    __in WDFDEVICE Device
    )
/*++

Routine Description:

    Retrieve the friendly name and the location string into WDFMEMORY objects
    and store them in the device context.

Arguments:

Return Value:

    NT status

--*/
{
    PDEVICE_CONTEXT pDevContext = GetDeviceContext(Device);

    WDF_OBJECT_ATTRIBUTES objectAttributes;

    WDFMEMORY deviceNameMemory = NULL;
    WDFMEMORY locationMemory = NULL;

    NTSTATUS status;

    PAGED_CODE();

    //
    // We want both memory objects to be children of the device so they will 
    // be deleted automatically when the device is removed.
    //
    WDF_OBJECT_ATTRIBUTES_INIT(&objectAttributes);
    objectAttributes.ParentObject = Device;

    //
    // First get the length of the string. If the FriendlyName
    // is not there then get the lenght of device description.
    //
    status = WdfDeviceAllocAndQueryProperty(Device,
                                            DevicePropertyFriendlyName,
                                            NonPagedPool,
                                            &objectAttributes,
                                            &deviceNameMemory);

    if (!NT_SUCCESS(status))
    {
        status = WdfDeviceAllocAndQueryProperty(Device,
                                                DevicePropertyDeviceDescription,
                                                NonPagedPool,
                                                &objectAttributes,
                                                &deviceNameMemory);
    }

    if (NT_SUCCESS(status))
    {
        pDevContext->DeviceNameMemory = deviceNameMemory;
        pDevContext->DeviceName = WdfMemoryGetBuffer(deviceNameMemory, NULL);
    }
    else
    {
        pDevContext->DeviceNameMemory = NULL;
        pDevContext->DeviceName = L"(error retrieving name)";
    }

    //
    // Retrieve the device location string.
    //
    status = WdfDeviceAllocAndQueryProperty(Device, 
                                            DevicePropertyLocationInformation,
                                            NonPagedPool,
                                            WDF_NO_OBJECT_ATTRIBUTES,
                                            &locationMemory);

    if (NT_SUCCESS(status))
    {
        pDevContext->LocationMemory = locationMemory;
        pDevContext->Location = WdfMemoryGetBuffer(locationMemory, NULL);
    }
    else
    {
        pDevContext->LocationMemory = NULL;
        pDevContext->Location = L"(error retrieving location)";
    }

    return;
}

__drv_requiresIRQL(PASSIVE_LEVEL)
PCHAR
DbgDevicePowerString(
    __in WDF_POWER_DEVICE_STATE Type
    )
/*++

Updated Routine Description:
    DbgDevicePowerString does not change in this stage of the function driver.

--*/
{
    switch (Type)
    {
    case WdfPowerDeviceInvalid:
        return "WdfPowerDeviceInvalid";
    case WdfPowerDeviceD0:
        return "WdfPowerDeviceD0";
    case PowerDeviceD1:
        return "WdfPowerDeviceD1";
    case WdfPowerDeviceD2:
        return "WdfPowerDeviceD2";
    case WdfPowerDeviceD3:
        return "WdfPowerDeviceD3";
    case WdfPowerDeviceD3Final:
        return "WdfPowerDeviceD3Final";
    case WdfPowerDevicePrepareForHibernation:
        return "WdfPowerDevicePrepareForHibernation";
    case WdfPowerDeviceMaximum:
        return "PowerDeviceMaximum";
    default:
        return "UnKnown Device Power State";
    }
}



