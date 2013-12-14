using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using _2Virt;
using System.Web.Services.Protocols;
using System.Net;

namespace Box2Virt
{
    class AxisWebServer : WebServer
    {
        private WsdlImporter Importer;

        public AxisWebServer(object description, object serviceList)
            : base(description, serviceList) { }

        internal AxisWebServer(string description)
            : base(description, null) {}

        public override void ParseServices(string result, object device)
        {
            DeviceCommand dev = (DeviceCommand)device;
            try
            {
                DeviceCommand newDev = new DeviceCommand(dev);
                newDev.Add(new DeviceCommand.ParentCommand(dev.UsbDevice.DeviceName));

                string name, href;
                int startHref = -1;
                int endHref   = -1;
                int startLi   = 0;
                int endLi     = 0;
                do
                {
                    if ((startHref = result.IndexOf("href=", startLi)) < 0)
                        break;
                    startHref += 6;
                    if ((endHref = result.IndexOf("\"", startHref)) < 0)
                        break;
                    if ((startLi = result.LastIndexOf("<li", startHref, startHref - startLi)) < 0)
                        break;
                    startLi += 4;
                    if ((endLi = result.LastIndexOf("<a", startHref, startHref - startLi)) < 0)
                        break;

                    name = result.Substring(startLi, endLi - startLi);
                    href = result.Substring(startHref, endHref - startHref);

                    if (href.StartsWith("http://"))
                    {
                        int pos = -1;
                        if ((pos = href.IndexOf('/', 7)) > 0)
                            href = href.Substring(pos);
                    }

                    newDev.Add(new DeviceCommand.WebServiceCommand(this, name, href));

                    startLi = endHref + 1;
                } while (true);

                DeviceCommand.IssueChange(newDev, dev.UsbDevice.DeviceName + " Web Services...");
            }
            catch (Exception e)
            {
                dev.Logger.Add(new Log(LogType.WEBError, e.Message));
            }
        }

        public override void ParseWsdl(string wsdl, string service, object device)
        {
            DeviceCommand dev = (DeviceCommand)device;
            lock (this)
            {
                if (Importer == null)
                    Importer = new WsdlImporter(dev);
            }

            if (wsdl.EndsWith("0\r\n\r\n"))
                wsdl = wsdl.Substring(0, wsdl.Length - 5);

            DeviceCommand newDev = new DeviceCommand(dev);

            // Set the proxy server to our own application proxy in order to direct the http traffic to the USB device
            WebProxy2Usb usbProxy = WebProxy2Usb.GetProxy(newDev);
            IWebProxy webProxy = new WebProxy(usbProxy.ProxyAddress, true);

            string compiled = Importer.LoadWsdl(wsdl, dev);

            if (compiled == null)
                return;

            dev.Logger.Add(new Log(LogType.DotNetAssembly, compiled));

            AssemblyImporter.AssemblyCommand[] webMethods = null;

            try
            {
                webMethods = AssemblyImporter.LoadAssembly(compiled);
            }
            catch (Exception e)
            {
                dev.Logger.Add(new Log(LogType.WEBError, e.Message));
                return;
            }

            newDev.Add(new DeviceCommand.ParentCommand(service));

            foreach (AssemblyImporter.AssemblyCommand method in webMethods)
            {
                try
                {
                    SoapHttpClientProtocol soapMethod = (SoapHttpClientProtocol)method.Class;
                    string url = soapMethod.Url;
                    int pos = url.IndexOf("http://") + 7;
                    pos = url.IndexOf("/", pos);
                    if (pos > 0)
                        soapMethod.Url = usbProxy.ProxyAddress + url.Substring(pos);

                    newDev.Add(method);
                }
                catch (Exception e)
                {
                    dev.Logger.Add(new Log(LogType.WEBError, e.Message));
                }

                DeviceCommand.IssueChange(newDev, service + "'s Web Service Methods...");
            }
        }
    
    }

}
