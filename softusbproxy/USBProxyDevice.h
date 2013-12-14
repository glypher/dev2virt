/*++ BUILD Version 0000

Copyright (c) 2Virt.com
This product is made available subject to the terms of GNU Lesser General Public License Version 3

Module Name:

    USBProxyDevice.h

Abstract:
    Definition of the class CUSBProxyDevice

--*/

#pragma once
#include "resource.h"       // main symbols
#include "SoftUSBWrapper.h"
#include "SoftUSBProxy.h"
#include "CharDevice.h"

#pragma warning(disable: 4995) //Pragma deprecated

// CUSBProxyDevice

class ATL_NO_VTABLE CUSBProxyDevice :
    public CComObjectRootEx<CComSingleThreadModel>,
    public CComCoClass<CUSBProxyDevice, &CLSID_USBProxyDevice>,
    public IConnectionPointContainerImpl<CUSBProxyDevice>,
    public IConnectionPointImpl<CUSBProxyDevice, &__uuidof(IUSBProxyDeviceEvents)>,
    public ISoftUSBEndpointEvents, // DSF USB device interface
    public IDispatchImpl<IUSBProxyDevice, &IID_IUSBProxyDevice, &LIBID_SoftUSBProxyLib, /*wMajor =*/ 1, /*wMinor =*/ 0> // IUSBProxyDevice idl interface
{
public:
    CUSBProxyDevice();
    virtual ~CUSBProxyDevice();

DECLARE_REGISTRY_RESOURCEID(IDR_USBPROXYDEVICE)


BEGIN_COM_MAP(CUSBProxyDevice)
    COM_INTERFACE_ENTRY(IUSBProxyDevice)
    COM_INTERFACE_ENTRY(IDispatch)
    COM_INTERFACE_ENTRY(ISoftUSBEndpointEvents)
    COM_INTERFACE_ENTRY(IConnectionPointContainer)
END_COM_MAP()

BEGIN_CONNECTION_POINT_MAP(CUSBProxyDevice)
    CONNECTION_POINT_ENTRY(__uuidof(IUSBProxyDeviceEvents))
END_CONNECTION_POINT_MAP()

private:
     void                  ResetMembers();
     static USBIdentifier* CreateUSBProxyIdentifier(SHORT vendorId, SHORT productId);
     HRESULT               SetupConnectionPoint(IUnknown *punkObject, REFIID iidConnectionPoint);
     HRESULT               ReleaseConnectionPoint();
     static HRESULT        CharDeviceData(void *pUser, BYTE* data, ULONG size);

     USBIdentifier           *m_pUsbIdentifier;     //The User Defined DSF Soft Usb Device Configuration
     ISoftUSBDevice          *m_piSoftUSBDevice;    //Underlying SoftUSBDevice object
     ISoftUSBEndpoint        *m_piINEndpoint;       //IN Endpoint
     ISoftUSBEndpoint        *m_piOUTEndpoint;      //OUT Endpoint
     IConnectionPoint        *m_piConnectionPoint;  //Connection point interface
     DWORD                    m_dwConnectionCookie; //Connection point cookie

     ICharDevice<BYTE>                     *m_CharDevice;         //Char Device to forward
     ICharDevice<BYTE>::UserDeviceCallback  m_UserCharCallback;   //Callback structure to pass to the Char Device
     
public:

    //ILUSBProxyDevice
    STDMETHOD(get_DSFDevice)(DSFDevice** ppDSFDevice);
    STDMETHOD(StartDataProcessing)();
    STDMETHOD(StopDataProcessing)();
    STDMETHOD(Fire_LogDataProcessing)(BYTE *data, ULONG size);

    //ISoftUSBEndpointEvents
    STDMETHOD(OnSetupTransfer)(BYTE DataToggle,BYTE *pbDataBuffer,
                               ULONG cbDataBuffer, BYTE *pbStatus);

    STDMETHOD(OnWriteTransfer)(BYTE DataToggle, BYTE *pbDataBuffer,
                               ULONG cbDataBuffer, BYTE *pbStatus);

    STDMETHOD(OnReadTransfer)(BYTE DataToggle, BYTE  *pbDataBuffer,
                              ULONG   cbDataBuffer,ULONG *cbDataWritten,
                              BYTE *pbStatus);       

    STDMETHOD(OnDeviceRequest)(USBSETUPREQUEST *pSetupRequest,
                               ULONG *RequestHandle, 
                               BYTE  *pbHostData, ULONG cbHostData,
                               BYTE **ppbResponseData,
                               ULONG *pcbResponseData,BYTE  *pbSetupStatus);

    STDMETHOD(OnDeviceRequestComplete)(ULONG RequestHandle,
                                       BYTE *pbFinalRequestStatus);
};

OBJECT_ENTRY_AUTO(__uuidof(USBProxyDevice), CUSBProxyDevice)
