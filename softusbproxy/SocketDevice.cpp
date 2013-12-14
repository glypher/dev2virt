#include "SocketDevice.h"

// Helper macros for checking HRESULT status
#define CHECK_FAIL(EXPR)      { hr = (EXPR); if(FAILED(hr)) goto Exit; }
#define CHECK_FALSE(EXPR, HR) { if(!(EXPR)) { hr = (HR);    goto Exit; } }

HRESULT ICharDevice<BYTE>::Open(const char *sPath, const CLogger::UserLogCallback *pLogger, ICharDevice<BYTE> **pCharDevice)
{
	WORD wVersionRequested = MAKEWORD(1, 1);
	WSADATA wsaData;
	HRESULT hr = S_OK;
	int i = 0;

	CHECK_FAIL( WSAStartup(wVersionRequested, &wsaData) );

	// Separate the Ip and Port
	while ( (sPath[i] != '\0') && (sPath[i] != ':') )
		i++;

	CHECK_FALSE( sPath[i] != '\0', E_FAIL );

	char *pHost = new char[i + 1];
	memcpy(pHost, sPath, i); pHost[i] = '\0';

	SockAddress* address = SockAddress::ResolveIP((const char*)pHost, (SHORT)atoi(sPath + i + 1));

	*pCharDevice = new CSocketClient<BYTE>(address, pLogger);

Exit:
	if (pHost)
		delete[] pHost;
	return S_OK;
}


/*
   =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
      SockAddress
   =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
*/
SockAddress::SockAddress(ULONG addr, USHORT port, SHORT AF)
{
	m_rawAddr.sin_family      = AF;
	m_rawAddr.sin_addr.s_addr = addr;
	m_rawAddr.sin_port        = htons(port);
	m_rawAddrSize             = sizeof(struct sockaddr_in);
}


SockAddress* SockAddress::ResolveIP(const char *host, USHORT port)
{
	ULONG remoteAddr = inet_addr(host);

	if (remoteAddr == INADDR_NONE) {
		// pcHost isn't a dotted IP, so resolve it through DNS
		hostent* pHE = gethostbyname(host);
		if (pHE != NULL) {
			remoteAddr = *((ULONG*)pHE->h_addr_list[0]);
			return new SockAddress(remoteAddr, port, AF_INET);
		}

		return NULL;
	}

	return new SockAddress(remoteAddr, port, AF_INET);
}

int SockAddress::GetAF(void)
{
   return ((struct sockaddr_in *)&m_rawAddr)->sin_family;
}

const void* const SockAddress::GetRaw(void)
{
   return &m_rawAddr;
}

int SockAddress::GetRawAddrSize(void)
{
   return m_rawAddrSize;
}


/*
   =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
      CSocketClient
   =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
*/
template <class T>
CSocketClient<T>::CSocketClient(SockAddress* peer, const CLogger::UserLogCallback *pLogger)
	:CWorkerThread<Win32ThreadTraits>()
{
	m_Peer = peer;
	// Initialize the Logger
	m_pLogger = new CSocketLogger(pLogger);

	if (m_Peer == NULL) {
		m_pLogger->Log((BYTE*)"Invalid address", 15);
		return;
	}

	if ( (m_Sockfd = socket(m_Peer->GetAF(), SOCK_STREAM, 0)) == INVALID_SOCKET ) {
		m_pLogger->LogLastError();
		return;
	}

	if ( connect(m_Sockfd, (struct sockaddr *)m_Peer->GetRaw(), m_Peer->GetRawAddrSize()) < 0 ) {
		m_pLogger->LogLastError();
		closesocket(m_Sockfd);
		m_Sockfd = INVALID_SOCKET;
		return;
	}

	// spawn CWorkerThread reader thread
	this->Initialize();
	m_hWorkerMutex  = CreateMutex (NULL, FALSE, NULL);
	m_pSocketWorker = new CSocketClient<T>::SocketWorker(this);
}


template <class T>
CSocketClient<T>::~CSocketClient()
{
	this->Shutdown();
	if (m_Sockfd)
		closesocket(m_Sockfd);
	CloseHandle(m_hWorkerMutex);
	if (m_Peer)
		delete m_Peer;
	if (m_pSocketWorker)
		delete m_pSocketWorker;
	if (m_pLogger)
		delete m_pLogger;

	m_pSocketWorker = NULL;
	m_Sockfd        = INVALID_SOCKET;
	m_Peer          = NULL;
	m_hWorkerMutex  = NULL;
	m_pLogger       = NULL;
}


template <class T>
HRESULT CSocketClient<T>::Close()
{
	delete this;
	return S_OK;
}


template <class T>
HRESULT CSocketClient<T>::DeviceIoControl(ULONG request, \
		T* inputData, ULONG inputSize, T*& returnedData, ULONG &returnedSize)
{
    UNREFERENCED_PARAMETER(request);
    UNREFERENCED_PARAMETER(inputData);
    UNREFERENCED_PARAMETER(inputSize);
    UNREFERENCED_PARAMETER(returnedData);
    UNREFERENCED_PARAMETER(returnedSize);

    return E_NOTIMPL;
}


template <class T>
HRESULT CSocketClient<T>::Write(T* data, ULONG size)
{
	const char *buff = (const char*)data;
	ULONG sent = 0;
	size = size * sizeof(T);

	if (m_Sockfd == INVALID_SOCKET)
		return E_FAIL;

	do {
		if ((sent = send(m_Sockfd, buff, size, 0)) == SOCKET_ERROR) {
			m_pLogger->LogLastError();
			return E_FAIL;
		}
		buff += sent;
		size -= sent;
	} while (size > 0);

	return S_OK;
}


template <class T>
HRESULT CSocketClient<T>::Read(UserDeviceCallback *pUserCallback)
{
	return this->AddHandle(m_hWorkerMutex, m_pSocketWorker, (DWORD_PTR)pUserCallback);
}


template <class T>
HRESULT CSocketClient<T>::SocketWorker::Execute(DWORD_PTR dwParam, HANDLE hObject)
{
	static T buffer[4096];
	UserDeviceCallback *pUserCallback = reinterpret_cast<UserDeviceCallback*>(dwParam);
	ULONG read;

	UNREFERENCED_PARAMETER(hObject);

	if (m_SockClient->m_Sockfd == INVALID_SOCKET)
		return E_FAIL;

	m_SockClient->m_pLogger->Log((BYTE*)"eeeeeeee read thread", 19);

	if ( (read = recv(m_SockClient->m_Sockfd, (char*)buffer, 4096 * sizeof(T), 0)) == SOCKET_ERROR ) {
		m_SockClient->m_pLogger->LogLastError();
		return E_FAIL;
	}

	return (*pUserCallback->ProcessData)(pUserCallback->pUser, buffer, read);
}


/*
   =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
      CSocketLogger
   =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
*/
HRESULT CSocketLogger::LogLastError()
{
	CSocketLogger::ErrorEntry *pError = CSocketLogger::m_ErrorList;
	int error = WSAGetLastError();
	// do a liniar search of the errors
	while ( (pError->m_Id != 0xFFFFFFFF) && (pError->m_Id != error) )
		pError++;

	return Log((BYTE*)pError->m_Dsc, (ULONG)strlen(pError->m_Dsc));
};

// The winsock Errors sorted by number
CSocketLogger::ErrorEntry CSocketLogger::m_ErrorList[] = {
	CSocketLogger::ErrorEntry(0,                  "No error"),
	CSocketLogger::ErrorEntry(WSAEINTR,           "Interrupted system call"),
	CSocketLogger::ErrorEntry(WSAEBADF,           "Bad file number"),
	CSocketLogger::ErrorEntry(WSAEACCES,          "Permission denied"),
	CSocketLogger::ErrorEntry(WSAEFAULT,          "Bad address"),
	CSocketLogger::ErrorEntry(WSAEINVAL,          "Invalid argument"),
	CSocketLogger::ErrorEntry(WSAEMFILE,          "Too many open sockets"),
	CSocketLogger::ErrorEntry(WSAEWOULDBLOCK,     "Operation would block"),
	CSocketLogger::ErrorEntry(WSAEINPROGRESS,     "Operation now in progress"),
	CSocketLogger::ErrorEntry(WSAEALREADY,        "Operation already in progress"),
	CSocketLogger::ErrorEntry(WSAENOTSOCK,        "Socket operation on non-socket"),
	CSocketLogger::ErrorEntry(WSAEDESTADDRREQ,    "Destination address required"),
	CSocketLogger::ErrorEntry(WSAEMSGSIZE,        "Message too long"),
	CSocketLogger::ErrorEntry(WSAEPROTOTYPE,      "Protocol wrong type for socket"),
	CSocketLogger::ErrorEntry(WSAENOPROTOOPT,     "Bad protocol option"),
	CSocketLogger::ErrorEntry(WSAEPROTONOSUPPORT, "Protocol not supported"),
	CSocketLogger::ErrorEntry(WSAESOCKTNOSUPPORT, "Socket type not supported"),
	CSocketLogger::ErrorEntry(WSAEOPNOTSUPP,      "Operation not supported on socket"),
	CSocketLogger::ErrorEntry(WSAEPFNOSUPPORT,    "Protocol family not supported"),
	CSocketLogger::ErrorEntry(WSAEAFNOSUPPORT,    "Address family not supported"),
	CSocketLogger::ErrorEntry(WSAEADDRINUSE,      "Address already in use"),
	CSocketLogger::ErrorEntry(WSAEADDRNOTAVAIL,   "Can't assign requested address"),
	CSocketLogger::ErrorEntry(WSAENETDOWN,        "Network is down"),
	CSocketLogger::ErrorEntry(WSAENETUNREACH,     "Network is unreachable"),
	CSocketLogger::ErrorEntry(WSAENETRESET,       "Net connection reset"),
	CSocketLogger::ErrorEntry(WSAECONNABORTED,    "Software caused connection abort"),
	CSocketLogger::ErrorEntry(WSAECONNRESET,      "Connection reset by peer"),
	CSocketLogger::ErrorEntry(WSAENOBUFS,         "No buffer space available"),
	CSocketLogger::ErrorEntry(WSAEISCONN,         "Socket is already connected"),
	CSocketLogger::ErrorEntry(WSAENOTCONN,        "Socket is not connected"),
	CSocketLogger::ErrorEntry(WSAESHUTDOWN,       "Can't send after socket shutdown"),
	CSocketLogger::ErrorEntry(WSAETOOMANYREFS,    "Too many references, can't splice"),
	CSocketLogger::ErrorEntry(WSAETIMEDOUT,       "Connection timed out"),
	CSocketLogger::ErrorEntry(WSAECONNREFUSED,    "Connection refused"),
	CSocketLogger::ErrorEntry(WSAELOOP,           "Too many levels of symbolic links"),
	CSocketLogger::ErrorEntry(WSAENAMETOOLONG,    "File name too long"),
	CSocketLogger::ErrorEntry(WSAEHOSTDOWN,       "Host is down"),
	CSocketLogger::ErrorEntry(WSAEHOSTUNREACH,    "No route to host"),
	CSocketLogger::ErrorEntry(WSAENOTEMPTY,       "Directory not empty"),
	CSocketLogger::ErrorEntry(WSAEPROCLIM,        "Too many processes"),
	CSocketLogger::ErrorEntry(WSAEUSERS,          "Too many users"),
	CSocketLogger::ErrorEntry(WSAEDQUOT,          "Disc quota exceeded"),
	CSocketLogger::ErrorEntry(WSAESTALE,          "Stale NFS file handle"),
	CSocketLogger::ErrorEntry(WSAEREMOTE,         "Too many levels of remote in path"),
	CSocketLogger::ErrorEntry(WSASYSNOTREADY,     "Network system is unavailable"),
	CSocketLogger::ErrorEntry(WSAVERNOTSUPPORTED, "Winsock version out of range"),
	CSocketLogger::ErrorEntry(WSANOTINITIALISED,  "WSAStartup not yet called"),
	CSocketLogger::ErrorEntry(WSAEDISCON,         "Graceful shutdown in progress"),
	CSocketLogger::ErrorEntry(WSAHOST_NOT_FOUND,  "Host not found"),
	CSocketLogger::ErrorEntry(WSANO_DATA,         "No host data of that type was found"),
	CSocketLogger::ErrorEntry(0xFFFFFFFF,         "Winsock Unknown Error")
};

