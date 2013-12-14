using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace _2Virt
{
    public class IOBuffer
    {
        public byte[] Data
        {
            get
            {
                int size = _end - _start;
                byte[] buf = new byte[size];
                Buffer.BlockCopy(_data, _start, buf, 0, size);
                _start = 0;
                _end   = 0;
                return buf;
            }
            set
            {
                int size = _data.Length - _end - 1;
                if (value.Length > size)
                    throw new OverflowException();
                Buffer.BlockCopy(value, 0, _data, _start, value.Length);
                _end += value.Length;
            }
        }

        protected IOBuffer(int size)
        {
            _data  = new byte[size];
            _start = 0;
            _end   = 0;
        }

        public Exception lastException { set; get; }

        protected byte[] _data;
        protected int    _start;
        protected int    _end;
    }

    public delegate void StreamCallback(IOBuffer stream);

    public class IOStream : IOBuffer
    {
        public const int cBufferSize = 4096;

        private FileStream stream;

        private bool toStop;

        private bool toRead;

        private StreamCallback callback;

        public IOStream(IntPtr handle, bool forReading, StreamCallback function) : base(cBufferSize)
        {
            toStop   = false;
            toRead   = forReading;
            callback = function;
            // initializes an Async IO Stream for the handle
            stream = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(handle, false),
                (toRead == true)? FileAccess.Read : FileAccess.Write, 128, true);
        }

        public USBError Start()
        {
            toStop = false;
            try
            {
                if (toRead)
                {
                    // the buffer size must be greater than the packet size of the endpoint
                    stream.BeginRead(base._data, base._start, 2048, new AsyncCallback(Performed), null);
                }
                else
                {
                    if (base._start < base._end)
                        stream.BeginWrite(base._data, base._start, base._end - base._start, new AsyncCallback(Performed), null);
                    else
                        toStop = true;
                }
            }
            catch (Exception ioexc)
            {
                this.Stop();
                this.lastException = new Exception("Stream cannot start:", ioexc);
                return USBError.FAIL;
            }
            return USBError.SUCCESS;
        }

        public USBError Stop()
        {
            toStop = true;
            return USBError.SUCCESS;
        }

        protected void Performed(IAsyncResult ar)
        {
            try
            {
                if (toRead)
                    base._end += stream.EndRead(ar);
                else
                {
                    stream.EndWrite(ar);
                    stream.Flush();
                }
            }
            catch (IOException ioexc)
            {
                this.Stop();
                this.lastException = new Exception("Stream aborted:", ioexc);
            }

            if (ar.IsCompleted)
                callback(this);
            if (!toStop)
                Start();
        }
    }
}
