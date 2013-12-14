#include "SoftUSBWrapper.h"
#include <atlconv.h>

/*++
Routine Description:
   Creates the USB device and initializes the device
   member variables and creates and initializes the
   device qualifier. The device qualifier is required
   for USB2.0 devices.
--*/
HRESULT CSoftUSBWrapper::ConfigDSFDevice(ISoftUSBDevice **pUSBDevice, USBIdentifier *pUsbId)
{
	HRESULT hr = S_OK;
	ISoftUSBDeviceQualifier *piDeviceQual = NULL;
	ISoftUSBDevice *pDevice = NULL;

	// Create the DSF Soft USB device interface coclass
	CHECK_FAIL( ::CoCreateInstance(CLSID_SoftUSBDevice, \
	                        NULL, \
	                        CLSCTX_INPROC_SERVER, \
	                        __uuidof(ISoftUSBDevice), \
	                        reinterpret_cast<void**>(&pDevice)) );
	*pUSBDevice = pDevice;

	// Create the device qualifer
	CHECK_FAIL( ::CoCreateInstance(CLSID_SoftUSBDeviceQualifier, \
	                        NULL, \
	                        CLSCTX_INPROC_SERVER, \
	                        __uuidof(ISoftUSBDeviceQualifier), \
	                        reinterpret_cast<void**>(&piDeviceQual)) );

	// Setup the device qualifier
	CHECK_FAIL( piDeviceQual->put_USB(0x0200) ); //binary coded decimal USB version 2.0
	CHECK_FAIL( piDeviceQual->put_DeviceClass(pUsbId->_class) );
	CHECK_FAIL( piDeviceQual->put_DeviceSubClass(pUsbId->subClass) );
	CHECK_FAIL( piDeviceQual->put_DeviceProtocol(pUsbId->protocol) );
	CHECK_FAIL( piDeviceQual->put_MaxPacketSize0(64) ); //max packet size endpoint 0
	CHECK_FAIL( piDeviceQual->put_NumConfigurations(pUsbId->configNo) );

	// Setup the device
	CHECK_FAIL( pDevice->put_USB(0x0200) );                           //binary coded decimal USB version 2.0
	CHECK_FAIL( pDevice->put_DeviceClass(pUsbId->_class) );
	CHECK_FAIL( pDevice->put_DeviceSubClass(pUsbId->subClass) );
	CHECK_FAIL( pDevice->put_DeviceProtocol(pUsbId->protocol) );
	CHECK_FAIL( pDevice->put_MaxPacketSize0(64) );                    //max packet size endpoint 0
	CHECK_FAIL( pDevice->put_Vendor(pUsbId->vendorId) );
	CHECK_FAIL( pDevice->put_Product(pUsbId->productId) );
	CHECK_FAIL( pDevice->put_Device(0x0100) );                        //Binary decimal coded version 1.0
	CHECK_FAIL( pDevice->put_RemoteWakeup(VARIANT_FALSE) );           //Device does not suppport remote wake up
	CHECK_FAIL( pDevice->put_HasExternalPower(VARIANT_TRUE) );        //Indicate that the device has power

	// Insert the manufacturer string
	CHECK_FAIL( CSoftUSBWrapper::AddDSFString(pDevice, pUsbId->sManufacturer, pUsbId->stringIndex) );
	CHECK_FAIL( pDevice->put_Manufacturer(pUsbId->stringIndex++)); //Index of the manufacturer string

	// Insert the product descripton string
	CHECK_FAIL( CSoftUSBWrapper::AddDSFString(pDevice, pUsbId->sProductDesc, pUsbId->stringIndex) );
	CHECK_FAIL( pDevice->put_ProductDesc(pUsbId->stringIndex++)); //Index of the product descripton string

	// Add the device qualifier
	CHECK_FAIL( pDevice->put_DeviceQualifier(piDeviceQual) );

	// Add the configurations for this device
	CHECK_FAIL( CSoftUSBWrapper::AddDSFConfigs(pDevice, pUsbId) );

Exit:
	RELEASE(piDeviceQual);
	return hr;
}

/*++
Routine Description:
   Returns all the endpoints for the interfaceNo interface of the configNo USB configuration
   for the ISoftUSBDevice object
--*/
HRESULT CSoftUSBWrapper::GetDSFEndpoints(ISoftUSBEndpoint**& ppiEndpoint, long& endpointNo, \
		 ISoftUSBDevice *pUSBDevice, long configNo, long interfaceNo)
{
	HRESULT hr = S_OK;
	ISoftUSBConfigurations *piConfigurations = NULL;
	ISoftUSBConfiguration  *piConfig         = NULL;
	ISoftUSBInterfaces     *piInterfaces     = NULL;
	ISoftUSBInterface      *piInterface      = NULL;
	ISoftUSBEndpoints      *piEndpoints      = NULL;
	ISoftUSBEndpoint       *piEndpoint       = NULL;
	VARIANT varIndex;      ::VariantInit(&varIndex);
	long noEndpoint = 0;

	CHECK_FAIL( pUSBDevice->get_Configurations(&piConfigurations) );
	// Set up the variant to recover the desired configuration
	varIndex.vt   = VT_I4;
	varIndex.lVal = configNo;
	CHECK_FAIL( piConfigurations->get_Item(varIndex, \
			reinterpret_cast<SoftUSBConfiguration**>(&piConfig)) );

	CHECK_FAIL( piConfig->get_Interfaces(&piInterfaces) );
	// Set up the variant to recover the desired interface
	varIndex.lVal = interfaceNo;
	CHECK_FAIL( piInterfaces->get_Item(varIndex, \
			reinterpret_cast<SoftUSBInterface**>(&piInterface)) );

	// Get the endpoints collection for the interface
	CHECK_FAIL( piInterface->get_Endpoints(&piEndpoints) );
	// Create the array of endpoints
	CHECK_FAIL( piEndpoints->get_Count(&noEndpoint) );
	endpointNo  = noEndpoint;
	ppiEndpoint = new ISoftUSBEndpoint*[endpointNo];
	memset(ppiEndpoint, 0, endpointNo * sizeof(ISoftUSBEndpoint*));
	for (long index = 0; index < endpointNo; index++) {
		varIndex.lVal = index + 1; // 1 based collection
		CHECK_FAIL( piEndpoints->get_Item(varIndex, \
				reinterpret_cast<SoftUSBEndpoint**>(&piEndpoint)) );
		ppiEndpoint[index] = piEndpoint;
	}

Exit:
	RELEASE(piEndpoints);
	RELEASE(piInterface);
	RELEASE(piInterfaces);
	RELEASE(piConfig);
	RELEASE(piConfigurations);
	return hr;
}


/*++
Routine Description:
   Creates all the strings used by the device. These strings are
   added to the strings collection which is maintained by the
   USB device.
--*/
HRESULT CSoftUSBWrapper::AddDSFString(ISoftUSBDevice *pUSBDevice, LPCWSTR sString, BYTE index)
{
	USES_CONVERSION;
	HRESULT             hr = S_OK;
	ISoftUSBStrings    *piStrings  = NULL;
	ISoftUSBString     *piString   = NULL;
	VARIANT             varIndex;    ::VariantInit(&varIndex);
	BSTR                bstrString = ::SysAllocString(sString);

	//Check that all BSTR allocations succeeded
	CHECK_FALSE( 0 != ::SysStringLen(bstrString), E_OUTOFMEMORY );

	//Get the string collection from the device
	CHECK_FAIL( pUSBDevice->get_Strings(&piStrings) );

	//Create and initialize the string descriptor with the assigned string index.
	//This index is used both to set the string's descriptors position in the pUSBDevice->Strings
	//and is the index value the GetDescriptors request from the host.
	//Note that we don't use string descriptor index zero because that is a reserved value for a
	//device's language ID descriptor.

	CHECK_FAIL( CoCreateInstance(CLSID_SoftUSBString,
	                      NULL,
	                      CLSCTX_INPROC_SERVER,
	                      __uuidof(ISoftUSBString),
	                      reinterpret_cast<void**>(&piString)) );

	CHECK_FAIL( piString->put_Value(bstrString) );

	//Set up the variant used as the index
	varIndex.vt = VT_I4; varIndex.lVal = index;
	CHECK_FAIL( piStrings->Add(reinterpret_cast<SoftUSBString*>(piString), varIndex) );

Exit:
	RELEASE(piString);
	RELEASE(piStrings);
	::SysFreeString(bstrString);

	return hr;
}


/*++
Routine Description:
    Initialize the USB configuration settings
--*/
HRESULT CSoftUSBWrapper::AddDSFConfigs(ISoftUSBDevice *pUSBDevice, USBIdentifier* pUsbId)
{
	HRESULT                 hr               = S_OK;
	ISoftUSBConfigurations *piConfigurations = NULL;
	ISoftUSBConfiguration  *piConfig         = NULL;

	// All configurations in the collection will start at 1 index
	VARIANT                 confIndex; ::VariantInit(&confIndex);
	confIndex.vt = VT_I4; confIndex.lVal = 1;

	// Get the USB Device configuration collection
	CHECK_FAIL( pUSBDevice->get_Configurations(&piConfigurations) );

	// Create the configuration
	CHECK_FAIL( CoCreateInstance(CLSID_SoftUSBConfiguration,
	                      NULL,
	                      CLSCTX_INPROC_SERVER,
	                      __uuidof(ISoftUSBConfiguration),
	                      reinterpret_cast<void**>(&piConfig)) );

	// Set the configuration data up
	for (BYTE index = 1; index <= pUsbId->configNo; index++, confIndex.lVal++) {
		USBIdentifier::USBConfiguration *pConfiguration = &pUsbId->pConfig[index - 1];

		CHECK_FAIL( piConfig->put_ConfigurationValue(index) );                     // The configuration identifier
		CHECK_FAIL( CSoftUSBWrapper::AddDSFString(pUSBDevice, pConfiguration->sConfiguration, pUsbId->stringIndex) );
		CHECK_FAIL( piConfig->put_Configuration(pUsbId->stringIndex++) );          // The configuration string
		CHECK_FAIL( piConfig->put_MaxPower(0) );                                   // Max bus power consumption in 2mA units
		CHECK_FAIL( piConfig->put_Attributes(pConfiguration->configAttr.Byte) );   // The configuration attribute data
		// Add the interfaces defined for the current configuration
		CHECK_FAIL( CSoftUSBWrapper::AddDSFInterfaces(pUSBDevice, piConfig, pUsbId, pConfiguration) );

		// Add the configuration to the collection
		CHECK_FAIL( piConfigurations->Add(reinterpret_cast<SoftUSBConfiguration*>(piConfig), confIndex) );

		RELEASE(piConfig);
	}

Exit:
	RELEASE(piConfig);
	RELEASE(piConfigurations);
	return hr;
}


/*++
Routine Description:
    Initializes the devices USB interfaces, configuring the defined endpoints for each interface
--*/
HRESULT CSoftUSBWrapper::AddDSFInterfaces(ISoftUSBDevice *pUSBDevice, ISoftUSBConfiguration *piConfig, \
		USBIdentifier *pUsbId, USBIdentifier::USBConfiguration *pUsbConf)
{
	HRESULT hr = S_OK;
	ISoftUSBInterfaces     *piInterfaces     = NULL;
	ISoftUSBInterface      *piInterface      = NULL;
	ISoftUSBEndpoints      *piEndpoints      = NULL;
	ISoftUSBEndpoint       *piEndpoint       = NULL;

	// All interfaces the collection will start at 1 index
	VARIANT                 iIndex; ::VariantInit(&iIndex);
	iIndex.vt = VT_I4; iIndex.lVal = 1;

	// Get the USB Device Configuration interface collection
	CHECK_FAIL( piConfig->get_Interfaces(&piInterfaces) );

	for (BYTE index = 0; index < pUsbConf->interfaceNo; index++, iIndex.lVal++) {
		USBIdentifier::USBInterface *pInterface = &pUsbConf->pInterface[index];

		// Create the device config interface
		CHECK_FAIL( CoCreateInstance(CLSID_SoftUSBInterface,
		                          NULL,
		                          CLSCTX_INPROC_SERVER,
		                          __uuidof(ISoftUSBInterface),
		                          reinterpret_cast<void**>(&piInterface)) );
		CHECK_FAIL( piInterface->put_InterfaceNumber(index) );
		CHECK_FAIL( piInterface->put_AlternateSetting(0) );
		CHECK_FAIL( piInterface->put_InterfaceClass(pInterface->_class) );
		CHECK_FAIL( piInterface->put_InterfaceSubClass(pInterface->subClass) );
		CHECK_FAIL( piInterface->put_InterfaceProtocol(pInterface->protocol) );
		CHECK_FAIL( CSoftUSBWrapper::AddDSFString(pUSBDevice, pInterface->sInterface, pUsbId->stringIndex) );
		CHECK_FAIL( piInterface->put_Interface(pUsbId->stringIndex++) ); // The interface string

		// Get the endpoint collection for the current interface
		CHECK_FAIL(piInterface->get_Endpoints(&piEndpoints));

		// All endpoints in the collection will start at 1 index
		VARIANT endpIndex; ::VariantInit(&endpIndex);
		endpIndex.vt = VT_I4; endpIndex.lVal = 1;

		// Configure each endpoint of the interface
		for (long endpoint = 0; endpoint < pInterface->endpointsNo; endpoint++, endpIndex.lVal++) {
			USBIdentifier::USBEndpoint *pEndpoint  = &pInterface->pEndpoints[endpoint];
			piEndpoint = NULL;

			// Create the Endpoint
			CHECK_FAIL( CoCreateInstance(CLSID_SoftUSBEndpoint,
			                      NULL,
			                      CLSCTX_INPROC_SERVER,
			                      __uuidof(ISoftUSBEndpoint),
			                      reinterpret_cast<void**>(&piEndpoint)) );
			// Configure the Endpoint
			CHECK_FAIL( piEndpoint->put_EndpointAddress(pEndpoint->address) );
			CHECK_FAIL( piEndpoint->put_Attributes(pEndpoint->endpointAttr.Byte) );
			CHECK_FAIL( piEndpoint->put_MaxPacketSize(pEndpoint->maxPacketSize) );
			CHECK_FAIL( piEndpoint->put_Interval(0) );
			CHECK_FAIL( piEndpoint->put_Halted(FALSE) );

			// Add the Endpoint to the interface endpoint collection
			CHECK_FAIL( piEndpoints->Add(reinterpret_cast<SoftUSBEndpoint*>(piEndpoint), endpIndex) );

			RELEASE(piEndpoint);
		}

		// Add the interface to the configuration's interface collection
		CHECK_FAIL( piInterfaces->Add(reinterpret_cast<SoftUSBInterface*>(piInterface), iIndex) );

		RELEASE(piEndpoints);
		RELEASE(piInterface);
	}

Exit:
	RELEASE(piEndpoint);
	RELEASE(piEndpoints);
	RELEASE(piInterface);
	RELEASE(piInterfaces);
	return hr;
}
