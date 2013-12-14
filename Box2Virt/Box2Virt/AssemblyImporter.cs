using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using _2Virt;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Box2Virt
{
    internal class AssemblyImporter
    {
        internal class AssemblyCommand : DeviceCommand.ParamCommand
        {
            private MethodInfo classMethod;
            private Object     classObject;
            private DeviceCommand.CommandParam[] currentArgs;

            public override DeviceCommand.CommandParam[] Parameters {
                set
                {
                    int len = currentArgs.Length;
                    foreach (DeviceCommand.CommandParam param in value)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            if (currentArgs[i].Name.Equals(param.Name))
                            {
                                currentArgs[i].Value = param.Value;
                                break;
                            }
                        }
                    }
                }
                get { return currentArgs; }
            }

            public Object Class { get { return classObject; } }

            public override IoStatus Execute(DeviceCommand device)
            {
                IoStatus status = new IoStatus();
                ParameterInfo[] parameters = classMethod.GetParameters();

                try
                {
                    object[] args = null;
                    if (currentArgs != null)
                    {
                        args = new object[currentArgs.Length];
                        for (int i = 0; i < currentArgs.Length; i++)
                            args[i] = currentArgs[i].Value;
                    }
                    // Dynamically Invoke the method
                    Object Result = classMethod.Invoke(classObject, args);
                    status.error = USBError.SUCCESS;
                    string res = null;
                    if (Result.GetType() == typeof(string))
                        res = (string)(Result);
                    else
                        res = Result.ToString();
                    status.buffer = Encoding.ASCII.GetBytes(res.ToCharArray());
                    status.size   = (uint)status.buffer.Length;
                }
                catch (Exception e)
                {
                    status.error = USBError.FAIL;
                    device.Logger.Add(new Log(LogType.WEBError, e.Message));
                }

                return status;
            }

            public AssemblyCommand(MethodInfo method)
            {
                DisplayName = method.Name;
                ImageIndex  = 3;
                classMethod = method;
                classObject = Activator.CreateInstance(method.ReflectedType);
                // create the arguments
                currentArgs = null;
                ParameterInfo[] args = method.GetParameters();
                if (args != null)
                {
                    currentArgs = new DeviceCommand.CommandParam[args.Length];
                    int i = 0;
                    foreach (ParameterInfo arg in args)
                    {
                        currentArgs[i++] = new DeviceCommand.CommandParam(arg.Name, Activator.CreateInstance(arg.ParameterType));
                    }
                }
            }
        }


        public static AssemblyCommand[] LoadAssembly(string AssemblyName)
        {
            List<AssemblyCommand> aList = new List<AssemblyCommand>();
            try {
                Assembly assembly = Assembly.LoadFrom(AssemblyName);
                // Walk through each type in the assembly
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsClass == true)
                    {
                        MethodInfo[] methods = type.GetMethods();
                        foreach (MethodInfo method in methods)
                        {
                            // skip constructors, non public methods and base type methods
                            if (method.IsConstructor || !method.IsPublic || method.DeclaringType != type)
                                continue;

                            aList.Add(new AssemblyCommand(method));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return aList.ToArray();
        }

    }
}
