using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using _2Virt;

namespace Box2Virt
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisibleAttribute(true)]
    public partial class CommandView : Form
    {
        protected static string HTMLstart()
        {
            string path = DeviceCommand.ResourcePath;

            return "<HTML><HEAD><TITLE>2Virt Device Command Output</TITLE>" +
                "<LINK REL=\"stylesheet\" TYPE=\"text/css\" HREF=\"" + path + "\\jstree\\webcommand.css\"></LINK>" +
                "<SCRIPT LANGUAGE=\"JavaScript\" SRC=\"" + path + "\\jstree\\externalClient.js\"></SCRIPT>" +
                "<LINK REL=\"stylesheet\" TYPE=\"text/css\" HREF=\"" + path + "\\jstree\\mktree.css\"></LINK>" +
                "<SCRIPT LANGUAGE=\"JavaScript\" SRC=\"" + path + "\\jstree\\mktree.js\"></SCRIPT>" +
                "<LINK REL=\"stylesheet\" TYPE=\"text/css\" HREF=\"" + path + "\\jstree\\prettify.css\"></LINK>" +
                "<SCRIPT LANGUAGE=\"JavaScript\" SRC=\"" + path + "\\jstree\\prettify.js\"></SCRIPT>" +
                "</HEAD><BODY onload=\"prettyPrint()\">";
        }

        protected static string HTMLend()
        {
            return "</BODY></HTML>";
        }

        private DeviceCommand Device;
        private DeviceCommand.ParamCommand Command;

        public CommandView()
        {
            InitializeComponent();
            webBrowser.AllowNavigation = true;
            webBrowser.ScriptErrorsSuppressed = false;
            webBrowser.Navigate("about: blank");
            Device = null;
            Command = null;
        }

        public const string ParamsFormName = "WebParams";

        // this should be called on the same thread as the GUI
        public void BrowserCallback(string data)
        {
            string[] values = data.Split('\n');
            // set the parameters
            DeviceCommand.CommandParam[] args = Command.Parameters;
            int i = 0;
            foreach (string value in values)
            {
                if (i >= args.Length)
                    break;
                args[i++].Value = value;
            }
            Command.Parameters = args;

            IoStatus status = Command.Execute(Device);
            DisplayCommand(status, Device.Logger.ToArray());
        }

        internal bool DisplayCommand(DeviceCommand.ParamCommand command, DeviceCommand device)
        {
            DeviceCommand.CommandParam[] args = command.Parameters;

            if ((args == null) || (args.Length == 0))
            {
                return false;
            }

            Device = device;
            Command = command;

            this.webBrowser.ObjectForScripting = this;

            string result = HTMLstart();
            result += "<div id=\"stylized\" class=\"myform\">";
            result += "<FORM action=\"\" method=\"GET\" name=\"" + ParamsFormName + "\"><br/>";
            result += "<h1>" + command.DisplayName + " form</h1>";
            result += "<p>Command <b>" + command.DisplayName + "</b> requires the following parameters:</p>";
            
            foreach (DeviceCommand.CommandParam arg in args)
            {
                result += "<label>" + arg.Name + "<span class=\"small\">Input Web Method parameter</span></label>";
                result += "<input type=\"text\" name=\"" + arg.Name + "\"><br/>";
            }
            result += "<button onClick=\"ClientCallback(this.form)\">Perform</button>";
            result += "<div class=\"spacer\"></div>";
            result += "</FORM></div>";
            result += HTMLend();

            webBrowser.DocumentText = result;

            return true;
        }

        internal void DisplayCommand(IoStatus status, Log[] logs)
        {
            string error  = null;
            string result = null;

            switch (status.error) {
                case USBError.SUCCESS :
                    error = "Command Succedded"; break;
                case USBError.DISCONNECTED :
                    error = "Device has been disconnected!"; break;
                case USBError.OVERFLOW :
                    error = "Overflow detected!"; break;
                case USBError.NOTFOUND :
                    error = "Command not found!"; break;
                case USBError.WIN_API_SPECIFIC :
                case USBError.FAIL :
                    error = "Catastrophic error encountered!"; break;
            }

            result = HTMLstart();
            result += "<b>Command Status: </b> " + error + "<br/><br/>";
            if (status.buffer != null) {
                result += Encoding.ASCII.GetString(status.buffer, 0, (int)status.size);
            } else {
                result += "No information returned!";
            }

            // try to display the xml information in Internet Explorer
            result = ExpandXml2Html(result);

            // display the logs
            if ((logs != null) && (logs.Length > 0))
            {
                string imgPath = DeviceCommand.ResourcePath + "\\";
                result += "<br/><br/><ul class=\"mktree\" id=\"USBLogs\">";
                result += "<li><img src=\"" + imgPath + "port.ico\"/> Command Logs<ul>";
                foreach (Log log in logs)
                {
                    result += "<li><img src=\"" + imgPath + "bang.ico\"/>";
                    switch (log.Type)
                    {
                        case LogType.HttpQuery:
                            result += " USB2Web Query";
                            result += "<ul><li>" + log.Message + "</li></ul></li>";
                            break;
                        case LogType.HttpResponse:
                            result += " Web2USB Response";
                            result += "<ul><li>" + ExpandXml2Html(log.Message) + "</li></ul></li>";
                            break;
                        case LogType.CSharpCode:
                            result += " C# Web Service Code";
                            result += "<ul><pre class=\"prettyprint lang-cs\">" + log.Message + "</pre></ul></li>";
                            break;
                        case LogType.DotNetAssembly:
                            result += " Loaded .Net Assembly";
                            result += "<ul><li>" + log.Message + "</li></ul></li>";
                            break;
                        case LogType.USBError:
                        case LogType.WEBError:
                            result += " Web Service Error";
                            result += "<ul><li>" + log.Message + "</li></ul></li>";
                            break;
                    }
                }
                result += "</ul></li></ul>";
            }
            result += HTMLend();

            webBrowser.DocumentText = result;
        }

        private string ExpandXml2Html(string xml)
        {
            // try to display the xml information in Internet Explorer
            int xmlStart = xml.IndexOf("<?xml");
            if (xmlStart >= 0)
            {
                try
                {
                    int xmlEnd = xml.Length;
                    if (xml.EndsWith("0\r\n\r\n"))
                        xmlEnd -= 5;
                    // Convert the XML data to HTML using IE7 .xls file
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.LoadXml(xml.Substring(xmlStart, xmlEnd - xmlStart));
                    StringWriter sw = new StringWriter();
                    XslCompiledTransform xslTrans = new XslCompiledTransform();
                    xslTrans.Load(DeviceCommand.ResourcePath + "\\jstree\\xml2html.xls");
                    xslTrans.Transform(xDoc.CreateNavigator(), new XsltArgumentList(), sw);

                    return xml.Substring(0, xmlStart) + sw.ToString();
                }
                catch (Exception) { }
            }
            return xml;
        }

        private void bBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
