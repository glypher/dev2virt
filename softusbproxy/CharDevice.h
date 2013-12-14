/*++BUILD Version 0000

Copyright (c) 2Virt.com
This product is made available subject to the terms of GNU Lesser General Public License Version 3

Module Name:

    ICharDevice

Abstract:
    Definies some common functionality to work with a char device node

--*/

#pragma once

#include "stdafx.h"

#include "Logger.h"

template <class T>
class ICharDevice
{
public:

static
	HRESULT Open(const char *sPath, const CLogger::UserLogCallback *pLogger, ICharDevice<T> **pCharDevice);

	HRESULT virtual Close(void) = 0;

	HRESULT virtual DeviceIoControl(ULONG request, \
			T* inputData, ULONG inputSize, \
			T*& returnedData, ULONG &returnedSize) = 0;

	HRESULT virtual Write(T* data, ULONG size)     = 0;

	typedef struct {
		HRESULT (*ProcessData) (void *pUser, T* data, ULONG size);
		void *pUser;
	} UserDeviceCallback;

	HRESULT virtual Read(UserDeviceCallback *pUserCallback) = 0;
};
