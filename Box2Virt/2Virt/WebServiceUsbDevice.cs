using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _2Virt
{
    class WebServiceUsbDevice : UsbDevice
    {
        private IOCTLcommand[] webServiceWsdl;

        public override IOCTLcommand[] Functions { get { return webServiceWsdl; } }

        public override string DeviceName { get { return "2Virt SOAP Agent"; } }

        internal WebServiceUsbDevice(string devicePath)
            : base(devicePath)
        {
            // intialize the function to get the wsdl file
            webServiceWsdl = new IOCTLcommand[1];
            webServiceWsdl[0].FunctionName  = "UDDI descovery";
            webServiceWsdl[0].inputBuffer   = null;
            webServiceWsdl[0].outputMaxSize = 4096;
            // FILE_DEVICE_USB2VIRT, IOCTL_INDEX, METHOD_BUFFERED, FILE_READ_ACCESS
            webServiceWsdl[0].ioctlNo = UsbApi.CTL_CODE(0x65500, 0x800, 0x0, 0x1);
        }
    }

    public abstract class WebServer
    {
        public string Description;
        public string ListServices;

        private static List<WebServer> servers = new List<WebServer>();

        protected WebServer(object description, object serviceList)
        {
            Description  = (string)description;
            ListServices = (string)serviceList;
        }

        private static WebServer GetServer(string description, string serviceList)
        {
            foreach (WebServer serv in servers)
            {
                if (serv.Description == null)
                    continue;
                if (description.StartsWith(serv.Description))
                {
                    WebServer newServer = (WebServer)Activator.CreateInstance(serv.GetType(),
                        new object[2]{description, serviceList});
                    return newServer;
                }
            }
            return null;
        }

        public static void RegisterServer(WebServer server)
        {
            servers.Add(server);
        }

        public static WebServer[] ParseWebServer(IoStatus status)
        {
            //if (status.error != USBError.SUCCESS)
                //return null;

            WebServer[] servers = null;
            int iter = 0;

            try
            {
                uint noServers = BitConverter.ToUInt32(status.buffer, 0);
                servers = new WebServer[noServers];

                int pos = 4;
                for (iter = 0; iter < noServers; iter++)
                {
                    int end = pos;
                    while ((status.buffer[end] != '\0') && (end < status.size))
                        end++;
                    string description = new string(Encoding.ASCII.GetChars(status.buffer, pos, end - pos + 1));
                    pos = ++end;
                    while ((status.buffer[end] != '\0') && (end < status.size))
                        end++;
                    string serviceList = new string(Encoding.ASCII.GetChars(status.buffer, pos, end - pos + 1));
                    servers[iter] = WebServer.GetServer(description, serviceList);
                }
            }
            catch (Exception)
            {
            }

            return servers;
        }

        public abstract void ParseServices(string result, object device);

        public abstract void ParseWsdl(string wsdl, string service, object device);
    }
}
