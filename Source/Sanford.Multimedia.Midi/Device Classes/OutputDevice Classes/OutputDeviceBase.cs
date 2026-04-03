#region License

/* Copyright (c) 2006 Leslie Sanford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

#endregion

#region Contact

/*
 * Leslie Sanford
 * Email: jabberdabber@hotmail.com
 */

#endregion

using System;
using System.Runtime.InteropServices;
using Sanford.Multimedia;

namespace Sanford.Multimedia.Midi
{
    public abstract partial class OutputDeviceBase : MidiDevice
    {
        protected readonly object lockObject = new object();

        protected IntPtr handle = IntPtr.Zero;

        public OutputDeviceBase(int deviceID) : base(deviceID)
        {
            try
            {
                RtMidiLoader.EnsureLoaded();
                int portCount = GetRtMidiOutputDeviceCountInternal();
                if (deviceID < 0 || deviceID >= portCount)
                {
                    throw new OutputDeviceException(DeviceException.MMSYSERR_BADDEVICEID);
                }

                handle = rtmidi_out_create_default();
                if (handle == IntPtr.Zero)
                {
                    throw new OutputDeviceException("RtMidi failed to allocate output.");
                }

                ThrowIfRtMidiFailed(handle);
                RtMidiWrapperNative w = (RtMidiWrapperNative)Marshal.PtrToStructure(handle, typeof(RtMidiWrapperNative));
                if (w.ptr == IntPtr.Zero)
                {
                    throw new OutputDeviceException("RtMidi failed to create output.");
                }

                rtmidi_open_port(handle, (uint)deviceID, "Sanford.Multimedia.Midi");
                ThrowIfRtMidiFailed(handle);
            }
            catch
            {
                if (handle != IntPtr.Zero)
                {
                    rtmidi_out_free(handle);
                    handle = IntPtr.Zero;
                }

                throw;
            }
        }

        ~OutputDeviceBase()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public virtual void Send(ChannelMessage message)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            Send(message.Message);
        }

        public virtual void SendShort(int message)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            Send(message);
        }

        public virtual void Send(SysExMessage message)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            byte[] data = message.GetBytes();
            if (data == null || data.Length == 0)
            {
                return;
            }

            lock (lockObject)
            {
                GCHandle pin = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    int r = rtmidi_out_send_message(handle, pin.AddrOfPinnedObject(), data.Length);
                    if (r != 0)
                    {
                        ThrowIfRtMidiFailed(handle);
                    }
                }
                finally
                {
                    pin.Free();
                }
            }
        }

        public virtual void Send(SysCommonMessage message)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            Send(message.Message);
        }

        public virtual void Send(SysRealtimeMessage message)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            Send(message.Message);
        }

        public override void Reset()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            lock (lockObject)
            {
                uint port = (uint)DeviceID;
                rtmidi_close_port(handle);
                ThrowIfRtMidiFailed(handle);
                rtmidi_open_port(handle, port, "Sanford.Multimedia.Midi");
                ThrowIfRtMidiFailed(handle);
            }
        }

        protected void Send(int message)
        {
            int n = GetShortMessageByteCount(message);
            byte[] buf = new byte[n];
            for (int i = 0; i < n; i++)
            {
                buf[i] = (byte)((message >> (8 * i)) & 0xFF);
            }

            lock (lockObject)
            {
                GCHandle pin = GCHandle.Alloc(buf, GCHandleType.Pinned);
                try
                {
                    int r = rtmidi_out_send_message(handle, pin.AddrOfPinnedObject(), n);
                    if (r != 0)
                    {
                        ThrowIfRtMidiFailed(handle);
                    }
                }
                finally
                {
                    pin.Free();
                }
            }
        }

        private static int GetShortMessageByteCount(int packed)
        {
            int status = packed & 0xFF;
            if (status >= 0xF8 && status <= 0xFF)
            {
                return 1;
            }

            if (status >= 0xF0 && status <= 0xF7)
            {
                if (status == 0xF1 || status == 0xF3)
                {
                    return 2;
                }

                if (status == 0xF2)
                {
                    return 3;
                }

                return 1;
            }

            int cmd = status & 0xF0;
            if (cmd == 0xC0 || cmd == 0xD0)
            {
                return 2;
            }

            return 3;
        }

        public static MidiOutCaps GetDeviceCapabilities(int deviceID)
        {
            RtMidiLoader.EnsureLoaded();
            IntPtr temp = rtmidi_out_create_default();
            if (temp == IntPtr.Zero)
            {
                throw new OutputDeviceException("RtMidi failed to create output.");
            }

            try
            {
                ThrowIfRtMidiFailed(temp);
                uint count = rtmidi_get_port_count(temp);
                ThrowIfRtMidiFailed(temp);

                if (deviceID < 0 || (uint)deviceID >= count)
                {
                    throw new OutputDeviceException(DeviceException.MMSYSERR_BADDEVICEID);
                }

                // Primary: rtmidi_get_port_name (via ReadUtf8). Fallback: WinMM if empty.
                string name = GetRtMidiOutputPortName(temp, (uint)deviceID);
                if (string.IsNullOrWhiteSpace(name))
                {
                    string fallback = WinMmMidiPortNames.TryGetOutputName(deviceID);
                    if (!string.IsNullOrEmpty(fallback))
                    {
                        name = fallback;
                    }
                }

                MidiOutCaps caps = new MidiOutCaps();
                caps.name = name;
                caps.mid = 0;
                caps.pid = 0;
                caps.driverVersion = 0;
                caps.support = 0;

                return caps;
            }
            finally
            {
                rtmidi_out_free(temp);
            }
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            lock (lockObject)
            {
                Close();
            }
        }

        /// <summary>
        /// Same backing value as <see cref="Device.DeviceID"/>, redeclared on the concrete type so .NET clients
        /// (e.g. LabVIEW property nodes) that only expose a setter for properties declared on the runtime class can write it.
        /// </summary>
        /// <remarks>
        /// Assigning does not reconnect the RtMidi port; create a new output device instance to use another physical output.
        /// </remarks>
        public new int DeviceID
        {
            get => base.DeviceID;
            set => base.DeviceID = value;
        }

        public override IntPtr Handle
        {
            get
            {
                return handle;
            }
        }

        public static int DeviceCount
        {
            get
            {
                return GetRtMidiOutputDeviceCountInternal();
            }
        }
    }
}
