/*++BUILD Version 0000

Copyright (c) 2Virt.com
This product is made available subject to the terms of GNU Lesser General Public License Version 3

Module Name:

    SoftUSBProxy.cpp

Abstract:
    Definies entry point for the DLL. ATL .DLL server wrappers for our USBProxyDevice coclass

--*/

#include "stdafx.h"
#include "resource.h"
#include <dsfif.h>
#include <USBProtocolDefs.h>
#include <softusbif.h>
#include "USBProxyDevice.h"
#include "SoftUSBProxy.h"

class CSoftUSBProxyModule : public CAtlDllModuleT<CSoftUSBProxyModule>
{
    public: 
        DECLARE_LIBID(LIBID_SoftUSBProxyLib)
        // Use the registry file SoftUSBProxy.rgs defined as IDR_SOFTUSBPROXY key in SoftUSBProxy.rc resource file
        DECLARE_REGISTRY_APPID_RESOURCEID(IDR_SOFTUSBPROXY, "{FB33445C-43D1-413E-B0E0-52B9DD84655A}")
        // the above SoftUSBProxy application's unique UUID was generated with GuidGen.exe
};

CSoftUSBProxyModule _AtlModule;

/////////////////////////////////////////////////////////////////////////////
// DLL Entry Point

extern "C"
BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    hInstance;
    return _AtlModule.DllMain(dwReason, lpReserved);
}

/////////////////////////////////////////////////////////////////////////////
// Used to determine whether the DLL can be unloaded by OLE

STDAPI DllCanUnloadNow(void)
{
    return (_AtlModule.DllCanUnloadNow());
}

/////////////////////////////////////////////////////////////////////////////
// Returns a class factory to create an object of the requested type

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv)
{
    return _AtlModule.DllGetClassObject(rclsid, riid, ppv);
}

/////////////////////////////////////////////////////////////////////////////
// DllRegisterServer - Adds entries to the system registry

STDAPI DllRegisterServer(void)
{
    // registers object, typelib and all interfaces in typelib
    HRESULT hr =  _AtlModule.DllRegisterServer();
    return hr;
}

/////////////////////////////////////////////////////////////////////////////
// DllUnregisterServer - Removes entries from the system registry

STDAPI DllUnregisterServer(void)
{
    HRESULT hr = _AtlModule.DllUnregisterServer();
    return hr;
}





