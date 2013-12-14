using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _2Virt;

namespace Box2Virt
{
    public enum LogType : int
    {
        USBError = 0x0,
        WEBError,
        HttpQuery,
        HttpResponse,
        CSharpCode,
        DotNetAssembly
    }

    public struct Log
    {
        LogType type;
        string message;

        public LogType Type { get { return type; } }
        public string Message { get { return message; } }

        public Log(LogType type, string message)
        {
            this.type = type;
            this.message = message;
        }
    }

    partial class DeviceCommand
    {
        public abstract class Command
        {
            public string DisplayName { set; get; }

            public int ImageIndex { set; get; }

            public abstract IoStatus Execute(DeviceCommand device);
        }

        public struct CommandParam
        {
            private string name;
            private Object value;

            public string Name { get { return name; } }

            public Object Value { get { return value; } set { this.value = value; } }

            internal CommandParam(string name, Object value) { this.name = name; this.value = value; }
        }
   
        public abstract class ParamCommand : Command
        {
            public virtual CommandParam[] Parameters { set; get; }
        }

        private List<Log> logs;

        public List<Log> Logger { get { return logs; } }

        public delegate void DeviceCommandEvent(DeviceCommand currentDevice, string message);

        private List<Command> commands;

        private IUsbDevice usb;

        private DeviceCommand parentDevice;
            
        public IUsbDevice UsbDevice { get { return usb; } }

        public DeviceCommand ParentDevice { get { return parentDevice; } }

        internal static event DeviceCommandEvent DeviceEvent;

        internal DeviceCommand(IUsbDevice usb)
        {
            this.usb = usb;
            this.parentDevice = null;
            logs = new List<Log>();
            commands = new List<Command>();
            // create the default IOCTL commands
            IOCTLcommand[] IoctlS = usb.Functions;
            if (IoctlS != null)
            {
                foreach (IOCTLcommand ioctl in IoctlS)
                {
                    commands.Add(new FunctionCommand(ioctl));
                    lock (devices)
                    {
                        devices.Add(this);
                    }
                }
            }
            // create the device information command
            commands.Add(new InfoCommand());
        }

        internal DeviceCommand(DeviceCommand dev)
        {
            this.usb = dev.UsbDevice;
            this.parentDevice = dev;
            logs = new List<Log>();
            commands = new List<Command>();
        }

        public void Add(Command command)
        {
            commands.Add(command);
        }

        public Command GetCommand(string displayName)
        {
            foreach (Command c in commands)
            {
                if (c.DisplayName.Equals(displayName))
                    return c;
            }
            return null;
        }

        private static List<DeviceCommand> devices = new List<DeviceCommand>();

        public static Command[] GetDeviceCommands(IUsbDevice usb)
        {
            Command[] found = null;
            lock (devices)
            {
                foreach (DeviceCommand dev in devices)
                {
                    if (dev.usb == usb)
                    {
                        found = dev.commands.ToArray();
                        break;
                    }
                }
            }
            return found;
        }

        public Command[] GetDeviceCommands()
        {
            return this.commands.ToArray();
        }

        internal static void RemoveDeviceCommand(IUsbDevice usb)
        {
            lock (devices)
            {
                foreach (DeviceCommand dev in devices)
                {
                    if (dev.usb == usb)
                    {
                        devices.Remove(dev);
                        break;
                    }
                }
            }
        }

        public static void IssueChange(DeviceCommand dev, string message)
        {
            DeviceEvent(dev, message);
        }
    }
}
