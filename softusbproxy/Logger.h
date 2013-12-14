#pragma once

#include "stdafx.h"
#include <windows.h>

class CLogger
{
public:
	struct UserLogCallback
	{
		HRESULT (*Logger) (void* pUser, BYTE* data, ULONG size);
		void* pUser;
	};

	CLogger(const UserLogCallback *pLog)
	{
		m_LogCallback.Logger = pLog->Logger;
		m_LogCallback.pUser  = pLog->pUser;
		m_hLogMutex          = CreateMutex (NULL, FALSE, NULL);
	}

	virtual ~CLogger()
	{
		CloseHandle(m_hLogMutex);
	}

	virtual HRESULT LogLastError() = 0;

	virtual HRESULT Log(BYTE* data, ULONG size)
	{
		DWORD dwWaitResult;
		HRESULT hr = S_OK;

		dwWaitResult = WaitForSingleObject(m_hLogMutex, INFINITE);  // no time-out interval
		switch (dwWaitResult)
		{
			case WAIT_OBJECT_0:
				hr = (*m_LogCallback.Logger)(m_LogCallback.pUser, data, size);
				ReleaseMutex(m_hLogMutex);
				break;
			case WAIT_ABANDONED:
			default:
				hr = E_UNEXPECTED;
	    }

		return hr;
	}

private:
	HANDLE          m_hLogMutex;
	UserLogCallback m_LogCallback;
};
