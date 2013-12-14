/*++BUILD Version 0000

Copyright (c) 2Virt.com
This product is made available subject to the terms of GNU Lesser General Public License Version 3

Module Name:

    SoftUSBWrapper

Abstract:
    Definies some common functionality to work with a Soft USB simulated DSF framework

--*/

#pragma once

#include "stdafx.h"
// DSF includes
#include <USBProtocolDefs.h>
#include <dsfif.h>
#include <softusbif.h>

// Helper macros for checking HRESULT status
#define CHECK_FAIL(EXPR)      { hr = (EXPR); if(FAILED(hr)) goto Exit; }
#define CHECK_FALSE(EXPR, HR) { if(!(EXPR)) { hr = (HR);    goto Exit; } }

#define RELEASE(p)\
{\
    if ((p) != NULL)\
    {\
        IUnknown *pAux = (p);\
        (p) = NULL;\
        pAux->Release();\
    }\
}


struct USBIdentifier
{
	static struct USBEndpoint
	{
		USBENDPOINTATTRIBS endpointAttr;
		BYTE               address;
		SHORT              maxPacketSize;
	};

	static struct USBInterface
	{
		LPCWSTR      sInterface;
		BYTE         _class;
		BYTE         subClass;
		BYTE         protocol;
		BYTE         endpointsNo;
		USBEndpoint *pEndpoints;
	};

	static struct USBConfiguration
	{

		LPCWSTR          sConfiguration;
		USBCONFIGATTRIBS configAttr;
		// Array of USB Device exposed interfaces
		BYTE             interfaceNo;
		USBInterface    *pInterface;
	};

	// USB Device specific identifiers
	SHORT   vendorId;
	SHORT   productId;
	LPCWSTR sManufacturer;
	LPCWSTR sProductDesc;
	BYTE    _class;
	BYTE    subClass;
	BYTE    protocol;

	// Array of USB Device exposed configuration
	BYTE              configNo;
	USBConfiguration *pConfig;

	// current number of string
	BYTE   stringIndex;
};


class CSoftUSBWrapper
{
public:
     static HRESULT ConfigDSFDevice(ISoftUSBDevice **pUSBDevice, USBIdentifier *pIdentifier);

     static HRESULT GetDSFEndpoints(ISoftUSBEndpoint**& ppiEndpoint, long& endpointNo, \
    		 ISoftUSBDevice *pUSBDevice, long configNo, long interfaceNo);

private:
     static HRESULT AddDSFString(ISoftUSBDevice *pUSBDevice, LPCWSTR sString, BYTE index);

     static HRESULT AddDSFConfigs(ISoftUSBDevice *pUSBDevice, USBIdentifier *pUsbId);

     static HRESULT AddDSFInterfaces(ISoftUSBDevice *pUSBDevice, ISoftUSBConfiguration *piConfig, \
    		 USBIdentifier *pUsbId, USBIdentifier::USBConfiguration *pUsbConf);
};
