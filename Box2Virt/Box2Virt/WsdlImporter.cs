using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Web.Services;
using System.Web.Services.Description;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Net;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace Box2Virt
{
    public class WebMethod
    {}

    /// <summary>
    /// WSDL Parser class that is responsible for:   
    /// Creating a .cs code file
    /// Compiling the .cs code file into an Assembly
    /// Parsing the WSDL generated class into a class structure consumable by a COM client
    /// (since Reflection objects are not COM friendly)
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]    
    internal class WsdlImporter : MarshalByRefObject
    {
        private string LocalAssembly;
        private uint LocalCount;

        /// <summary>
        /// Generates an unique new assembly based on the device
        /// </summary>
        public WsdlImporter(DeviceCommand device)
        {
            LocalAssembly = "Usb2WebService";
            LocalCount = 0;
        }

        public string LoadWsdl(string wsdl, DeviceCommand device)
        {
            string compiled = null;
            lock (this)
            {
                try
                {
                    string code = GenerateWsdlProxyClass(wsdl);

                    device.Logger.Add(new Log(LogType.CSharpCode, code));

                    // Finally compile the code into a new .dll assembly
                    compiled = CompileSource(code);
                }
                catch (Exception e)
                {
                    device.Logger.Add(new Log(LogType.WEBError, e.Message));
                }
                // increment our current assembly generation
                LocalCount++;
            }

            return compiled;
        }

        /// <summary>
        /// This function basically reproduces the functionality that WSDL.exe provides and generates
        /// a CSharp class that is a proxy to the Web service specified at the provided WSDL URL.
        /// Returns the string for the generated C# code file
        /// </summary>
        private string GenerateWsdlProxyClass(string wsdl)
        {     
            // get the WSDL content into a service description
            StringReader sWsdl = new StringReader(wsdl);
            ServiceDescription sd = null;
            
            sd = ServiceDescription.Read(sWsdl);
            
            // create an importer and associate with the ServiceDescription
            ServiceDescriptionImporter importer = new ServiceDescriptionImporter();
            importer.ProtocolName = "SOAP";            
            importer.CodeGenerationOptions = CodeGenerationOptions.None;            
            importer.AddServiceDescription(sd, null, null);
            
            // Download and inject any imported schemas (ie. WCF generated WSDL)            
            foreach (XmlSchema wsdlSchema in sd.Types.Schemas)
            {
                // Loop through all detected imports in the main schema
                foreach (XmlSchemaObject externalSchema in wsdlSchema.Includes)
                {
                    WebClient http = new WebClient();
                    // Read each external schema into a schema object and add to importer
                    if (externalSchema is XmlSchemaImport)
                    {
                        Uri schemaUri = new Uri(((XmlSchemaExternal)externalSchema).SchemaLocation);
                        Stream schemaStream = http.OpenRead(schemaUri);
                        System.Xml.Schema.XmlSchema schema = XmlSchema.Read(schemaStream, null);
                        importer.Schemas.Add(schema);
                    }
                }
            }

            // set up for code generation by creating a namespace and adding to importer
            CodeNamespace ns = new CodeNamespace(LocalAssembly + LocalCount);
            CodeCompileUnit ccu = new CodeCompileUnit();
            ccu.Namespaces.Add(ns);
            importer.Import(ns, ccu);

            // final code generation in specified language
            CSharpCodeProvider provider = new CSharpCodeProvider();
            StringWriter sw = new StringWriter();
            provider.GenerateCodeFromCompileUnit(ccu, sw, new CodeGeneratorOptions());

            sw.Flush();
            string code = sw.ToString();
            sw.Close();

            return code;
        }
  
        /// <summary>
        /// Compiles the C# Web Service proxy code into a .Net assembly
        /// </summary>
        private string CompileSource(string code)
        {
            string targetAssembly = DeviceCommand.AssemblyPath + "\\" + LocalAssembly + LocalCount + ".dll";
            // delete existing assembly first 
            if (File.Exists(targetAssembly))
            {
                // this might fail if assembly is in use 
                File.Delete(targetAssembly);
            }

            // Embed COM visibility into code so Intellisense works on client
            int pos = code.IndexOf("namespace ");
            code = code.Substring(0, pos) + @"
                // Inserted to allow for COM registration
                using System.Runtime.InteropServices;
                [assembly: ComVisible(true)]
                [assembly: ClassInterface(ClassInterfaceType.AutoDual)]
                namespace " + code.Substring(pos + "namespace ".Length);

            // set up compiler and add required references
            CompilerParameters parameter = new CompilerParameters();
            parameter.OutputAssembly = targetAssembly;
            parameter.ReferencedAssemblies.Add("System.dll");
            parameter.ReferencedAssemblies.Add("System.Web.Services.dll");
            parameter.ReferencedAssemblies.Add("System.Xml.dll");

            // *** support DataSet/DataTable results
            parameter.ReferencedAssemblies.Add("System.Data.dll");
            
            // Do it: Final compilation to disk
            CompilerResults results = new CSharpCodeProvider().CompileAssemblyFromSource(parameter, code);

            if (!File.Exists(targetAssembly))
            {
                string error = null;
                // flatten the compiler error messages into a single string
                foreach (CompilerError err in results.Errors)
                {
                    error += err.ToString() + "\r\n";
                }
                if (error != null)
                    throw new Exception(error);
                targetAssembly = null;
            }

            return targetAssembly;
        }
    }
}
