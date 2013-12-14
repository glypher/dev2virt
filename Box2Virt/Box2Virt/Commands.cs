using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using _2Virt;
using System.Net;
using System.Threading;

namespace Box2Virt
{
    partial class DeviceCommand
    {
        public static string ResourcePath
        {
            get
            {
                string path = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            #if (DEBUG)
                path += "\\..\\..";
            #endif
                return path + "\\resources";
            }
        }

        public static string AssemblyPath
        {
            get
            {
                string path = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            #if (DEBUG)
                path += "\\..\\..";
            #endif
                return path + "\\assembly";
            }
        }

        internal class ParentCommand : Command
        {
            public ParentCommand(string name)
            {
                DisplayName = name;
                ImageIndex = 4;
            }

            public override IoStatus Execute(DeviceCommand device)
            {
                IoStatus status = new IoStatus();
                status.error = USBError.FAIL;
                if (device.parentDevice != null)
                {
                    status.error = USBError.SUCCESS;
                    DeviceCommand.IssueChange(device.parentDevice, DisplayName);
                }

                return status;
            }
        }

        /* IOCTL Function Exposed by a specific USB device */
        class FunctionCommand : Command
        {
            private IOCTLcommand usbIOCTL;
            public FunctionCommand(IOCTLcommand ioctl)
            {
                usbIOCTL = ioctl;
                DisplayName = ioctl.FunctionName;
                ImageIndex = 0;
            }

            public override IoStatus Execute(DeviceCommand device)
            {
                /* first get the service list query */
                IoStatus status = device.usb.DeviceIoControl(usbIOCTL);

                WebServer[] servers = WebServer.ParseWebServer(status);
                if (servers == null)
                    return status;

                string result = "";
                foreach (WebServer server in servers)
                {
                    result += "<b>" + server.Description + "</b><br/>";
                    HttpRestCommand services = new HttpRestCommand(server.ListServices);
                    status = services.Execute(device);
                    result += new string(Encoding.ASCII.GetChars(status.buffer, 0, (int)status.size)) + "<br/><br/>";
                    // Now add the new web service wsdl's if the are available
                    server.ParseServices(result, device);
                }

                status.buffer = Encoding.ASCII.GetBytes(result.ToCharArray());
                status.size = (uint)status.buffer.Length;
                status.error = USBError.SUCCESS;
                return status;
            }
        }

        /* Generic command for getting USB device properties */
        class InfoCommand : Command
        {
            public InfoCommand()
            {
                DisplayName = "Configuration";
                ImageIndex = 1;
            }

            public override IoStatus Execute(DeviceCommand device)
            {
                IoStatus status = new IoStatus();

                // Get all host controllers active on the machine
                List<USBView.USBController> hostList = USBView.GetUSBControllers();

                List<string> devPaths = UsbApi.GetDevices(null, UsbApi.GUID_CLASS_USB_HOST_CONTROLLER);

                string result = "<ul class=\"mktree\" id=\"USBView\">";
                string imgPath = ResourcePath + "\\";
                foreach (USBView.USBController hostC in hostList)
                {
                    result += "<li><img src=\"" + imgPath + "controller.ico\"/>"
                        + hostC.ControllerName + "<ul>";

                    USBView.USBController.USBHub rootHub = hostC.RootHub;
                    result += "<li><img src=\"" + imgPath + "hub.ico\"/>" + rootHub.HubName + "</li><ul>";
                    List<USBView.USBController.USBHub.USBPort> portList = rootHub.Ports;
                    foreach (USBView.USBController.USBHub.USBPort port in portList)
                    {
                        result += port.HasAttachedDevice?
                            "<li><img src=\"" + imgPath + "controller.ico\"/>" : "<li><img src=\"" + imgPath + "port.ico\"/>";
                        result += port.PortInformation;
                        List<USBView.USBController.USBHub.USBPort.Property> properties = port.Properties;
                        if (properties.Count > 0)
                        {
                            result += "<ul>";
                            foreach (USBView.USBController.USBHub.USBPort.Property prop in properties)
                            {
                                result += "<li><b>" + prop.Key + "</b>" + prop.Value + "</li>";
                            }
                            result += "</ul>";
                        }
                        result += "</li>";
                    }
                    result += "</ul></ul></li>";
                }
                result += "</ul>";
                status.buffer = Encoding.ASCII.GetBytes(result.ToCharArray());
                status.size   = (uint)status.buffer.Length;
                status.error  = USBError.SUCCESS;
                return status;
            }
        }

        /* a generic request/reply command */
        abstract internal class RestCommand : Command
        {
            private IoCompletion completionWrite;
            private IoCompletion completionRead;
            private IoStatus     completionStatus;
            private Object       completionLock;

            protected abstract bool HasFinished(IoStatus status);

            protected RestCommand()
            {
                completionWrite = new IoCompletion(DataWrite);
                completionRead  = new IoCompletion(DataRead);
                completionLock  = new Object();
            }

            private void DataWrite(IoStatus status)
            {
                lock(completionLock)
                {
                    if (status.error == USBError.SUCCESS)
                    {
                        completionStatus.size -= status.size;
                        if (completionStatus.size > 0)
                            return;
                    }
                    completionStatus = status;
                    Monitor.PulseAll(completionLock);
                }
            }

            private void DataRead(IoStatus status)
            {
                lock (completionLock)
                {
                    if (completionStatus.size == 0)
                        completionStatus = status;
                    else
                    {
                        // concat the 2 IoStatus
                        byte[] bufAux = completionStatus.buffer;
                        completionStatus.buffer = new byte[completionStatus.size + status.size];
                        Buffer.BlockCopy(bufAux, 0, completionStatus.buffer, 0, (int)completionStatus.size);
                        Buffer.BlockCopy(status.buffer, 0, completionStatus.buffer, (int)completionStatus.size, (int)status.size);
                        completionStatus.size += status.size;
                        completionStatus.error = status.error;
                    }

                    if (HasFinished(completionStatus))
                    {
                        Monitor.PulseAll(completionLock);
                    }
                }
            }

            public IoStatus RestExecute(DeviceCommand device, byte[] request)
            {

                completionStatus = new IoStatus();
                completionStatus.error = USBError.FAIL;

                lock (device.usb)
                {
                    lock (completionLock)
                    {
                        device.usb.WriteDone += completionWrite;
                        completionStatus.size = (uint)request.Length;
                        device.usb.IssueWrite(request);
                        Monitor.Wait(completionLock, 5000);
                        device.usb.WriteDone -= completionWrite;
                    }

                    if (completionStatus.error != USBError.SUCCESS)
                        return completionStatus;

                    // Reset the IoStatus to begin reading
                    completionStatus.size = 0;

                    lock (completionLock)
                    {
                        device.usb.ReadDone += completionRead;
                        device.usb.StartRead();
                        Monitor.Wait(completionLock, 5000);
                        device.usb.StopRead();
                        device.usb.ReadDone -= completionRead;
                    }
                }

                return completionStatus;
            }
        }

        /* A generic HTTP service method call */
        internal class HttpRestCommand : RestCommand
        {
            private string rawRequest;

            private HttpParser.HTTPResponse response;
            
            public HttpRestCommand(string request)
                :base()
            {

                if (request.StartsWith("GET"))
                    rawRequest = request;
                else
                    rawRequest = "GET " + request + " HTTP/1.1\r\nHost: localhost\r\n\r\n";

                DisplayName = request;
                
                ImageIndex = 2;
            }

            public HttpRestCommand(string name, string request)
                : this(request)
            {
                DisplayName = name;
            }

            protected override bool HasFinished(IoStatus status)
            {
                if (status.error != USBError.SUCCESS)
                    return true;

                if (status.size >= response.expectedSize)
                    return HttpParser.ParseHttp(status, ref response);

                return false;
            }

            public override IoStatus Execute(DeviceCommand device)
            {
                response = HttpParser.HTTPResponse.InitResponse();
                // Log the request
                device.Logger.Add(new Log(LogType.HttpQuery, rawRequest));
                // Issue the request
                IoStatus status = RestExecute(device, Encoding.ASCII.GetBytes(rawRequest.ToCharArray()));
                // Log the reply
                try
                {
                    device.Logger.Add(new Log(LogType.HttpResponse, new string(Encoding.ASCII.GetChars(status.buffer, 0, (int)status.size))));
                }
                catch (Exception) { }

                return status;
            }            
        }

        /* A generic HTTP service method call */
        internal class WebServiceCommand : HttpRestCommand
        {
            private WebServer Server;

            public WebServiceCommand(WebServer server, string name, string request)
                : base(name, request)
            {
                Server = server;
            }

            public override IoStatus Execute(DeviceCommand device)
            {
                IoStatus status = base.Execute(device);
                try
                {
                    string wsdl = new string(Encoding.ASCII.GetChars(status.buffer, 0, (int)status.size));
                    // Import and add new methods for this
                    Server.ParseWsdl(wsdl.Substring(wsdl.IndexOf("<?xml")), DisplayName, device);
                }
                catch (Exception e)
                {
                    device.Logger.Add(new Log(LogType.WEBError, e.Message));
                }
                return status;
            }    

        }
    }
}
