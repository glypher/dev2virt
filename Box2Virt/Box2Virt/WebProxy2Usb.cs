using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using _2Virt;

namespace Box2Virt
{
    class WebProxy2Usb
    {
        private static int LocalPort = 2323;

        public string ProxyAddress { get { return "http://localhost:" + LocalPort; } }

        private static Hashtable usbProxies = new Hashtable();

        private DeviceCommand Device;

        private TcpListener tcpListener;
        private Thread listenThread;

        public static WebProxy2Usb GetProxy(DeviceCommand usb)
        {
            WebProxy2Usb proxy = null;
            lock (usbProxies)
            {
                proxy = (WebProxy2Usb)usbProxies[usb];
                if (proxy == null)
                {
                    LocalPort++;
                    proxy = new WebProxy2Usb(usb);
                    usbProxies[usb] = proxy;
                }
            }
            return proxy;
        }

        private WebProxy2Usb(DeviceCommand device)
        {
            Device = device;
            tcpListener = new TcpListener(IPAddress.Loopback, LocalPort);
            listenThread = new Thread(new ThreadStart(ListenForClients));
            tcpListener.Start();
            listenThread.Start();
        }

        private void ListenForClients()
        {
            while (true)
            {
                // blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                // create a thread to handle communication with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);
            }
        }

        private class SoapMethod : DeviceCommand.RestCommand
        {
            private byte[] toWrite;

            private HttpParser.HTTPResponse response;

            public HttpParser.HTTPResponse HTTPStatus { get { return response; } }

            internal SoapMethod(byte[] data, int size)
            {
                toWrite = new byte[size];
                Buffer.BlockCopy(data, 0, toWrite, 0, size);
            }

            protected override bool HasFinished(IoStatus status)
            {
                if (status.size >= response.expectedSize)
                    return HttpParser.ParseHttp(status, ref response);
                return false;
            }

            public override IoStatus Execute(DeviceCommand device)
            {
                response = HttpParser.HTTPResponse.InitResponse();
                return RestExecute(device, toWrite);
            }
        }

        private void HandleClient(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead = 0;

            try
            {
                bool done = true;
                do
                {
                    // blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                    if (bytesRead == 0)
                    {
                        // the client has disconnected from the server
                        tcpClient.Close();
                        return;
                    }

                    // log the http query
                    Device.Logger.Add(new Log(LogType.HttpQuery, new string(Encoding.ASCII.GetChars(message, 0, bytesRead))));

                    //message has successfully been received
                    SoapMethod usbMethod = new SoapMethod(message, bytesRead);

                    IoStatus status = usbMethod.Execute(Device);

                    if (status.error == USBError.SUCCESS)
                    {
                        done = !HttpParser.IsPartialResponse(usbMethod.HTTPStatus);
                        clientStream.Write(status.buffer, 0, (int)status.size);
                    }
                    else
                        done = true;

                    // log the http response
                    Device.Logger.Add(new Log(LogType.HttpResponse, new string(Encoding.ASCII.GetChars(status.buffer, 0, (int)status.size))));

                } while (!done);
            }
            catch (Exception)
            {
                tcpClient.Close();
            }
        }
    }
}
