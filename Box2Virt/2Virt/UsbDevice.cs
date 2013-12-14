using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace _2Virt
{
    public struct IoStatus
    {
        public byte[] buffer;
        public uint size;
        public USBError error;
    }

    public struct IOCTLcommand
    {
        public string FunctionName;
        public uint ioctlNo;
        public byte[] inputBuffer;
        public uint outputMaxSize;
    }
    

    public delegate void IoCompletion(IoStatus status);


    public interface IUsbDevice
    {
        UsbManager.UsbIdentifier UsbId { set;get; }

        IOCTLcommand[] Functions { get; }

        string DevicePath { get; }

        string DeviceName { get; }

        IoStatus DeviceIoControl(IOCTLcommand command);
        USBError StartRead();
        USBError StopRead();
        USBError IssueWrite(byte[] buffer);

        event IoCompletion ReadDone;
        event IoCompletion WriteDone;
    }


    internal class UsbDevice : IUsbDevice, IDisposable
    {
        protected IOStream inPipe;
        private   IntPtr   inHandle;
        protected IOStream outPipe;
        private   IntPtr   outHandle;
        private UsbManager.UsbIdentifier usbId;
        protected string   devicePath;

        public UsbManager.UsbIdentifier UsbId { set { usbId = value; } get { return usbId; } }

        public virtual IOCTLcommand[] Functions { get { return null;  } }

        public string DevicePath { get { return devicePath; } }

        public virtual string DeviceName { get { return devicePath; } }

        internal UsbDevice(string systemPath)
        {
            devicePath = systemPath;
            inHandle   = UsbApi.OpenDevice(devicePath + "\\PIPE00", false);
            inPipe     = new IOStream(inHandle, true, new StreamCallback(PipeReadCallback));
            outHandle  = UsbApi.OpenDevice(devicePath + "\\PIPE01", false);
            outPipe    = new IOStream(outHandle, false, new StreamCallback(PipeWriteCallback));
        }

        public void PipeReadCallback(IOBuffer stream)
        {
            IoStatus status  = new IoStatus();
            status.buffer    = stream.Data;
            status.size      = (uint)status.buffer.Length;
            status.error     = USBError.SUCCESS;
            if (stream.lastException != null)
            {
                status.error = (stream.lastException is System.IO.IOException) ?
                     USBError.DISCONNECTED : USBError.FAIL;
            }
            // issue the partial read event
            if (ReadDone != null)
                ReadDone(status);
        }

        public void PipeWriteCallback(IOBuffer stream)
        {
            IoStatus status  = new IoStatus();
            status.buffer    = stream.Data;
            status.size      = (uint)status.buffer.Length;
            status.error     = USBError.SUCCESS;
            if (stream.lastException != null)
            {
                status.error = (stream.lastException is System.IO.IOException) ?
                    USBError.DISCONNECTED : USBError.FAIL;
            }
            // issue the partial write event
            if (WriteDone != null)
                WriteDone(status);
        }

        #region IUsbDevice Members

        public IoStatus DeviceIoControl(IOCTLcommand command)
        {
            return UsbApi.DeviceIoControl(outHandle, command);
        }

        public USBError StartRead()
        {
            return inPipe.Start();
        }

        public USBError StopRead()
        {
            return inPipe.Stop();
        }

        public USBError IssueWrite(byte[] buffer)
        {
            try
            {
                outPipe.Data = buffer;
            }
            catch (OverflowException)
            {
                return USBError.OVERFLOW;
            }
            return outPipe.Start();
        }

        public event IoCompletion ReadDone;

        public event IoCompletion WriteDone;

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (inHandle != null)  UsbApi.CloseDevice(inHandle);
            if (outHandle != null) UsbApi.CloseDevice(outHandle);
        }

        #endregion

        public override string ToString()
        {
            return DeviceName;
        }

    }
}
