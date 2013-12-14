#pragma once

#include "CharDevice.h"

#include <winsock.h>

// ATL CWorkerThread essentials
#include <atlwin.h>
#include <atlutil.h>

class SockAddress
{
public:
	SockAddress(ULONG addr, USHORT port, SHORT AF);
	virtual ~SockAddress(void) {};

	int                 GetAF(void);
	const void* const   GetRaw(void);
	int                 GetRawAddrSize(void);

	static SockAddress* ResolveIP(const char* host, USHORT port);

private:
	struct sockaddr_in m_rawAddr;
	int                m_rawAddrSize;
	short              m_AF;
};


class CSocketLogger : public CLogger
{
public:
	CSocketLogger(const UserLogCallback *pLog) :
		CLogger(pLog) {};

	struct ErrorEntry {
		int         m_Id;
		const char* m_Dsc;
		ErrorEntry(int id, const char* dsc) :
			m_Id(id), m_Dsc(dsc) {};
	};

	HRESULT LogLastError();

private:
	static ErrorEntry m_ErrorList[];
};


template <class T>
class CSocketClient:
	public ICharDevice<T>,
	public CWorkerThread<Win32ThreadTraits>
{
protected:
	CSocketClient<T>(SockAddress* peer, const CLogger::UserLogCallback *pLogger);

	class SocketWorker: public IWorkerThreadClient
	{
	public:
		SocketWorker(CSocketClient<T> *sockClient)
			: m_SockClient(sockClient) {}

	private:
		// IWorkerThreadClient
		HRESULT Execute(DWORD_PTR dwParam, HANDLE hObject);
		HRESULT CloseHandle(HANDLE hHandle)
		{
			::CloseHandle(hHandle);
			return S_OK;
		}

	protected:
		CSocketClient<T> *m_SockClient;
	};

	friend HRESULT ICharDevice<T>::Open(const char *sPath, const CLogger::UserLogCallback *pLogger, ICharDevice<T> **pCharDevice);

public:
	virtual ~CSocketClient<T>();

	//ICharDevice
	HRESULT virtual Close(void);

	HRESULT DeviceIoControl(ULONG request, \
			T* inputData, ULONG inputSize, \
			T*& returnedData, ULONG &returnedSize);

	HRESULT Write(T* data, ULONG size);

	HRESULT Read(UserDeviceCallback *pUserCallback);

private:
	int            m_Sockfd;
	SockAddress*   m_Peer;
	SocketWorker*  m_pSocketWorker;
	HANDLE         m_hWorkerMutex;
	CSocketLogger* m_pLogger;
};


