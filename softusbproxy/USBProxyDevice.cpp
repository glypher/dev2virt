/*++ BUILD Version 0000

Copyright (c) 2Virt.com
This product is made available subject to the terms of GNU Lesser General Public License Version 3

Module Name:

    USBProxyDevice.cpp

Abstract:

    Implements the interface IUSBProxyDevice and
    configures the Proxy device to be a valid USB device.
    The device then processes input to its endpoint by receiving
    a callback to indicate that data needs to be processed,
    and then processes the data. In this way we more accurately simulate
    a usb-serial adapter.
--*/

#include "stdafx.h"
#include <stdio.h>
#include <conio.h>
// USBProxyDevice COM includes
#include "USBProxyDevice.h"
#include "SoftUSBProxy_i.c" // IDL auto generated file

const SHORT  kUsbVendorId  = 0x045E;
const USHORT kUsbProductId = 0x930A;

CUSBProxyDevice::CUSBProxyDevice()
{
	long endpointNo = 0;
	ISoftUSBEndpoint **ppiEndpoints = NULL;

	ResetMembers();

	m_pUsbIdentifier = CUSBProxyDevice::CreateUSBProxyIdentifier(kUsbVendorId, kUsbProductId);
	CSoftUSBWrapper::ConfigDSFDevice(&m_piSoftUSBDevice, m_pUsbIdentifier);

	// get the IN and OUT interface endpoint object for the first configuration and interface
	CSoftUSBWrapper::GetDSFEndpoints(ppiEndpoints, endpointNo, m_piSoftUSBDevice, 1, 1);

	if (endpointNo > 1)
	{
		m_piINEndpoint  = ppiEndpoints[0];
		m_piOUTEndpoint = ppiEndpoints[1];
	}
	if (ppiEndpoints != NULL)
		delete ppiEndpoints;

	// Set up the Char Device Read Callback
	m_UserCharCallback.ProcessData = CUSBProxyDevice::CharDeviceData;
	m_UserCharCallback.pUser       = this;
}

CUSBProxyDevice::~CUSBProxyDevice()
{
	if (NULL != m_pUsbIdentifier)
		delete m_pUsbIdentifier;

	// Release the conneciton point
	ReleaseConnectionPoint();

	if (NULL != m_CharDevice)
	{
		m_CharDevice->Close();
		delete m_CharDevice;
	}

	// Release any interface which the device is holding
	RELEASE(m_piConnectionPoint);
	RELEASE(m_piINEndpoint);
	RELEASE(m_piOUTEndpoint);

	if (NULL != m_piSoftUSBDevice)
	{
		(void)m_piSoftUSBDevice->Destroy();
		RELEASE(m_piSoftUSBDevice);
	}

	ResetMembers();
}

/*++
Routine Description:
   Initialize member variables for the class CUSBProxyDevice
--*/
void CUSBProxyDevice::ResetMembers()

{
	m_pUsbIdentifier     = NULL;
	m_piSoftUSBDevice    = NULL;
	m_piINEndpoint       = NULL;
	m_piOUTEndpoint      = NULL;
	m_piConnectionPoint  = NULL;
	m_dwConnectionCookie = 0;
	m_CharDevice         = NULL;
	m_UserCharCallback.ProcessData = NULL;
	m_UserCharCallback.pUser       = NULL;
}


USBIdentifier* CUSBProxyDevice::CreateUSBProxyIdentifier(SHORT vendorId, SHORT productId)
{
	USBIdentifier *pUsb = new USBIdentifier();

	pUsb->vendorId      = vendorId;
	pUsb->productId     = productId;
	pUsb->sManufacturer = L"2Virt.com USB-Char Device";
	pUsb->sProductDesc  = L"Simulated Generic USB-Char convertor device";
	pUsb->_class        = 0xFF; // 0xFF = Vendor specfic device class
	pUsb->subClass      = 0xFF; // 0xFF = Vendor specific device sub-class
	pUsb->protocol      = 0xFF; // 0xFF = Vendor specific device protocol

	pUsb->stringIndex = 1; // The first string collection ID is 1, 0 being reserved

	// Create the USB device configuration
	pUsb->configNo = 1;
	USBIdentifier::USBConfiguration *pConfig =  pUsb->pConfig = new USBIdentifier::USBConfiguration();
	pConfig->sConfiguration = L"Configuration with a single I/O interface";
	pConfig->configAttr.Bits.bRemoteWakeup = 0; // Device does not do remote wakeup
	pConfig->configAttr.Bits.bSelfPowered  = 1; // Device is self-powered

	// Create one USB interface for the above configuration
	pConfig->interfaceNo = 1;
	USBIdentifier::USBInterface *pInterface = pConfig->pInterface = new USBIdentifier::USBInterface();
	pInterface->sInterface = L"Interface with bulk IN endpoint and bulk OUT endpoint";
	pInterface->_class     = 0xFF; // 0xFF = Vendor specfic device class
	pInterface->subClass   = 0xFF; // 0xFF = Vendor specific device sub-class
	pInterface->protocol   = 0xFF; // 0xFF = Vendor specific device protocol

	// Create the 2 Enpoints for this interface
	pInterface->endpointsNo = 2;
	USBIdentifier::USBEndpoint *pEndpoint = pInterface->pEndpoints = new USBIdentifier::USBEndpoint[2];
	// Configure the IN Endpoint
	pEndpoint->address           = 0x81; // Enpoint IN Address
	pEndpoint->endpointAttr.Byte = 0x02; // Bulk data endpoint
	pEndpoint->maxPacketSize     = 1024;
	pEndpoint++;
	// Configure the OUT Endpoint
	pEndpoint->address           = 0x02; // Enpoint OUT Address
	pEndpoint->endpointAttr.Byte = 0x02; // Bulk data endpoint
	pEndpoint->maxPacketSize     = 1024;

	return pUsb;
}


/*++
Routine Description:
   Setup the connection point to the OUT Endpoint.
   It validates that the punkObject supports IConnectionPointContainer
   then finds the correct connection point and attaches this object
   as the event sink.

Arguments:
    punkObject - Object which implement IConnectionPointContainer

    iidConnectionPoint - IID of the connection point

Return value:
    S_OK if the connection point is correctly set up
    E_UNEXPECTED if the IUknown of this can not be obtained
    Otherwise from called functions.
--*/
HRESULT CUSBProxyDevice::SetupConnectionPoint(IUnknown *punkObject,
                                              REFIID    iidConnectionPoint)
{
	HRESULT hr = S_OK;
	IConnectionPointContainer *piConnectionPointContainer = NULL;
	IUnknown                  *punkSink = NULL;

	CHECK_FALSE( NULL != punkObject, E_UNEXPECTED );

	// If there is already connection point enabled, disable it
	if(NULL != m_piConnectionPoint)
	{
		CHECK_FAIL( ReleaseConnectionPoint() );
	}

	CHECK_FAIL( punkObject->QueryInterface(IID_IConnectionPointContainer,
	                                      reinterpret_cast<void **>(&piConnectionPointContainer)) );

	CHECK_FAIL( piConnectionPointContainer->FindConnectionPoint(iidConnectionPoint,
	                                                           &m_piConnectionPoint) );

	// Get the IUknown of this interface as this is the event sink
	punkSink = (this)->GetUnknown();

	CHECK_FALSE( NULL != punkSink, E_UNEXPECTED );

	CHECK_FAIL( m_piConnectionPoint->Advise(punkSink, &m_dwConnectionCookie) );

Exit:
	RELEASE(piConnectionPointContainer);

	return hr;
}


/*++
Routine Description:
   Release the connection point to the OUT Endpoint
   if one has been established.
--*/
HRESULT CUSBProxyDevice::ReleaseConnectionPoint()
{
	HRESULT hr = S_OK;

	if (NULL != m_piConnectionPoint)
	{
		m_piConnectionPoint->Unadvise(m_dwConnectionCookie);
		m_dwConnectionCookie = 0;
	}

	RELEASE(m_piConnectionPoint);

	return hr;
}


/*++
Routine Description:
   Callback from the Char Device reader thread to inform us that new data is available.
   Will be logged and placed in the IN USB endpoint
--*/
HRESULT CUSBProxyDevice::CharDeviceData(void *pUser, BYTE* data, ULONG size)
{
	HRESULT hr     = S_OK;
	BYTE bINStatus = USB_ACK;
	CUSBProxyDevice *pThis = reinterpret_cast<CUSBProxyDevice*>(pUser);

	// this came from the CharDevice, possible static allocation so copy the buffer
	BYTE *pCopy = new BYTE[size + 1];
	memcpy(pCopy, data, size * sizeof(BYTE));
	pCopy[size * sizeof(BYTE)] = 0; // NULL terminated for logging just in case

	CHECK_FAIL( pThis->Fire_LogDataProcessing(data, size) );

	// Send the data to the IN Endpoint
	CHECK_FAIL( pThis->m_piINEndpoint->QueueINData(pCopy, size, bINStatus, SOFTUSB_FOREVER) );

Exit:
	return hr;
}

/*++
Routine Description:
   Implement the get property DSFDevice for the interface
   ILoopbackDevice.

Arguments:
    ppDSFDevice - address of a pointer to receive the DSFDevice.

Return value:
    E_FAIL if the USB device does not exist
    E_POINTER if ppDSFDevice is not a valid pointer
    Otherwise from called function
--*/
STDMETHODIMP CUSBProxyDevice::get_DSFDevice(DSFDevice** ppDSFDevice)
{
	HRESULT hr = S_OK;

	CHECK_FALSE( NULL != ppDSFDevice, E_POINTER );
	*ppDSFDevice = NULL;

	// Validate the the USB device exists else this is an internal error
	CHECK_FALSE( NULL != m_piSoftUSBDevice, E_FAIL );

	CHECK_FAIL( m_piSoftUSBDevice->get_DSFDevice(ppDSFDevice) );

Exit:

	return hr;
}


/*++
Routine Description:
   Demonstrates how to setup event sinks so that the event mechanism can
   be used to control data flow to and from the USB controller. In this
   example an event sink is installed on the OUT USB endpoint, when the
   controller has data to send to the device the OnWriteTransfer event
   will fire, this will occur on an arbitrary thread. The device then
   simply copies this data and passes it the IN queue of the IN
   Endpoint. Control returns to the caller and event processing
   continues in an arbitrary thread. To terminate event processing call
   StopDataProcessing.

--*/
STDMETHODIMP CUSBProxyDevice::StartDataProcessing()
{
	HRESULT hr = S_OK;
	CLogger::UserLogCallback logger;
	logger.Logger = CUSBProxyDevice::CharDeviceData;
	logger.pUser  = this;

	// Set up event sink on the OUT endpoint
	CHECK_FAIL( SetupConnectionPoint(m_piOUTEndpoint, __uuidof(ISoftUSBEndpointEvents)) );

	// Start connecting to the Char Device
	CHECK_FAIL( ICharDevice<BYTE>::Open("127.0.0.1:8080", &logger, &m_CharDevice) );

	// Start the Reader Thread
	CHECK_FAIL( m_CharDevice->Read(&m_UserCharCallback) );

Exit:
	return hr;
}


/*++
Routine Description:
   This method terminates the event processing started by an earlier
   call to StartAsyncEventProcessing.
--*/
STDMETHODIMP CUSBProxyDevice::StopDataProcessing()
{
	HRESULT hr = S_OK;

	// Remove the event sink on the OUT endpoint
	CHECK_FAIL( ReleaseConnectionPoint() );

Exit:
	return hr;
}


HRESULT _stdcall CUSBProxyDevice::Fire_LogDataProcessing(BYTE *data, ULONG size)
{
	USES_CONVERSION;
	HRESULT hr = S_OK;

	int cConnections = m_vec.GetSize();

	for (int iConnection = 0; iConnection < cConnections; iConnection++)
	{
		this->Lock();
		CComPtr<IUnknown> punkConnection = m_vec.GetAt(iConnection);
		this->Unlock();

		IDispatch * pConnection = static_cast<IDispatch *>(punkConnection.p);

		if (pConnection)
		{
			VARIANTARG logParam[1];
			BSTR toBePassed;

			CHECK_FALSE( (toBePassed = SysAllocStringByteLen( (LPCSTR)(A2W((LPCSTR)data)), size * 2)) != NULL, \
			                     E_OUTOFMEMORY );
			logParam[0].vt = VT_BSTR; logParam[0].bstrVal = toBePassed;

			DISPPARAMS dispParams = { logParam, // paramters
									  NULL,     // Named Parameters = JScript doesn't understand this
									  1,        // number of parameters
									  0 };      // number of named parameters
			hr = pConnection->Invoke(1, // The method's id number in the SoftUSBProxy.idl IUSBProxyDeviceEvents dispinterface
					 IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD,
					 &dispParams, // the parameter list to be passed as Variants
					 NULL, NULL, NULL);

			SysFreeString(toBePassed);
		}
	}

Exit:
	return hr;
}


//ISoftUSBEndpointEvents

/*++
Routine Description:
   Implements the OnSetupTransfer for the interface ISoftUSBEndpointEvent

Arguments:
    DataToggle - DataToggle value
    pbDataBuffer - pointer to the buffer which contains the data
    cbDataBuffer - size of the data buffer
    pbStatus - pointer to transfer status value

Return value:
    E_NOTIMPL as this event is not handled by the device
--*/
STDMETHODIMP CUSBProxyDevice::OnSetupTransfer(BYTE DataToggle,BYTE *pbDataBuffer,
                               ULONG cbDataBuffer, BYTE *pbStatus)
{
	HRESULT hr = E_NOTIMPL;

	UNREFERENCED_PARAMETER(DataToggle);
	UNREFERENCED_PARAMETER(pbDataBuffer);
	UNREFERENCED_PARAMETER(cbDataBuffer);
	UNREFERENCED_PARAMETER(pbStatus);

	return hr;
}


/*++
Routine Description:
   Implements the OnWriteTransfer for the interface ISoftUSBEndpointEvent

Arguments:
    DataToggle - DataToggle value
    pbDataBuffer - pointer to the buffer which contains the data
    cbDataBuffer - size of the data buffer
    pbStatus - pointer to transfer status value

Return value:
    E_UNEXPECTED if the IN endpoint is not valit
    Otherwise form call function
--*/
STDMETHODIMP CUSBProxyDevice::OnWriteTransfer(BYTE DataToggle, BYTE *pbDataBuffer,
                               ULONG cbDataBuffer, BYTE *pbStatus)
{

	HRESULT hr = S_OK;
	UNREFERENCED_PARAMETER(DataToggle);

	// Check that the IN endpoint is valid
	CHECK_FALSE( NULL != m_piINEndpoint, E_UNEXPECTED);

	CHECK_FAIL( Fire_LogDataProcessing(pbDataBuffer, cbDataBuffer) )

	CHECK_FAIL( m_CharDevice->Write(pbDataBuffer, cbDataBuffer) );

	// ACK the status as the data was successfully sent to the IN endpoint
	*pbStatus = USB_ACK;

Exit:
	if (FAILED(hr))
	{
		*pbStatus = USB_STALL;
	}
	return hr;
}


/*++
Routine Description:
   Implements the OnReadTransfer for the interface ISoftUSBEndpointEvent

Arguments:
    DataToggle - DataToggle value
    pbDataBuffer - pointer to the data beffer
    cbDataBuffer - size of the data buffer
    cbDataWritten - amount of data actually written to the buffer
    pbStatus - pointer to transfer status value

Return value:
    E_NOTIMPL as this event is not handled by the device
--*/
STDMETHODIMP CUSBProxyDevice::OnReadTransfer(BYTE DataToggle, BYTE  *pbDataBuffer,
                              ULONG   cbDataBuffer,ULONG *cbDataWritten,
                              BYTE *pbStatus)
{
	HRESULT hr = E_NOTIMPL;

	UNREFERENCED_PARAMETER(DataToggle);
	UNREFERENCED_PARAMETER(pbDataBuffer);
	UNREFERENCED_PARAMETER(cbDataBuffer);
	UNREFERENCED_PARAMETER(cbDataWritten);
	UNREFERENCED_PARAMETER(pbStatus);

	return hr;
}


/*++
Routine Description:
   Implements the OnDeviceRequest for the interface ISoftUSBEndpointEvent

Arguments:
    pSetupRequest - pointer to the setup request begin handled
    RequestHandle - Handle fro the request
    pbHostData - pointer to a buffer which contain data sebt from the
                 host to the device for an OUT setup transfer
    cbHostData - amount of data in the HostData buffer
    ppbReponseData - pointer to data buffer to hold the response data
    pcbResponseData - pointer to the size of the data buffer
    pbSetpStatus - pointer to the setup status

Return value:
    E_NOTIMPL as this event is not handled by the device
--*/
STDMETHODIMP CUSBProxyDevice::OnDeviceRequest(USBSETUPREQUEST *pSetupRequest,
                               ULONG *RequestHandle, BYTE *pbHostData,
                               ULONG  cbHostData, BYTE **ppbResponseData,
                               ULONG *pcbResponseData,BYTE  *pbSetupStatus)
{
	HRESULT hr = E_NOTIMPL;

	UNREFERENCED_PARAMETER(pSetupRequest);
	UNREFERENCED_PARAMETER(RequestHandle);
	UNREFERENCED_PARAMETER(pbHostData);
	UNREFERENCED_PARAMETER(cbHostData);
	UNREFERENCED_PARAMETER(ppbResponseData);
	UNREFERENCED_PARAMETER(pcbResponseData);
	UNREFERENCED_PARAMETER(pbSetupStatus);

	return hr;
}


/*++
Routine Description:
   Implements the OnDeviceRequestComplete for the interface ISoftUSBEndpointEvent

Arguments:
    RequestHandle - Handle to the request which has just completed
    pbFinalStatus - Pointer to the final status that is to be returned.
    
Return value:
    E_NOTIMPL as this event is not handled by the device
--*/
STDMETHODIMP CUSBProxyDevice::OnDeviceRequestComplete(ULONG RequestHandle,
                                       BYTE *pbFinalRequestStatus)
{
	HRESULT hr = E_NOTIMPL;

	UNREFERENCED_PARAMETER(RequestHandle);
	UNREFERENCED_PARAMETER(pbFinalRequestStatus);

	return hr;
}
