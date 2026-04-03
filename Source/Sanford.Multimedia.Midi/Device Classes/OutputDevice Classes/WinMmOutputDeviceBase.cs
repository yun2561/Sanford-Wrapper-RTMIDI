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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Sanford.Threading;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// WinMM-based MIDI output base class used by <see cref="OutputStream"/> (midiStream API).
    /// </summary>
    public abstract class WinMmOutputDeviceBase : MidiDevice
    {
        [DllImport("winmm.dll")]
        protected static extern int midiOutReset(IntPtr handle);

        [DllImport("winmm.dll")]
        protected static extern int midiOutShortMsg(IntPtr handle, int message);

        [DllImport("winmm.dll")]
        protected static extern int midiOutPrepareHeader(IntPtr handle,
            IntPtr headerPtr, int sizeOfMidiHeader);

        [DllImport("winmm.dll")]
        protected static extern int midiOutUnprepareHeader(IntPtr handle,
            IntPtr headerPtr, int sizeOfMidiHeader);

        [DllImport("winmm.dll")]
        protected static extern int midiOutLongMsg(IntPtr handle,
            IntPtr headerPtr, int sizeOfMidiHeader);

        [DllImport("winmm.dll")]
        protected static extern int midiOutGetDevCaps(IntPtr deviceID,
            ref MidiOutCaps caps, int sizeOfMidiOutCaps);

        [DllImport("winmm.dll")]
        protected static extern int midiOutGetNumDevs();

        protected const int MOM_OPEN = 0x3C7;
        protected const int MOM_CLOSE = 0x3C8;
        protected const int MOM_DONE = 0x3C9;

        protected delegate void GenericDelegate<T>(T args);

        protected delegate void MidiOutProc(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

        protected DelegateQueue delegateQueue = new DelegateQueue();
        
        protected readonly object lockObject = new object();

        protected int bufferCount = 0;

        private MidiHeaderBuilder headerBuilder = new MidiHeaderBuilder();

        protected IntPtr handle = IntPtr.Zero;        

        public WinMmOutputDeviceBase(int deviceID) : base(deviceID)
        {
        }

        ~WinMmOutputDeviceBase()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                delegateQueue.Dispose();
            }

            base.Dispose(disposing);
        }

        public virtual void Send(ChannelMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            Send(message.Message);
        }

        public virtual void SendShort(int message)
        {
            #region Require

            if (IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            Send(message);
        }

        public virtual void Send(SysExMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock(lockObject)
            {
                headerBuilder.InitializeBuffer(message);
                headerBuilder.Build();

                int result = midiOutPrepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);

                if(result == MidiDeviceException.MMSYSERR_NOERROR)
                {
                    bufferCount++;

                    result = midiOutLongMsg(Handle, headerBuilder.Result, SizeOfMidiHeader);

                    if(result != MidiDeviceException.MMSYSERR_NOERROR)
                    {
                        midiOutUnprepareHeader(Handle, headerBuilder.Result, SizeOfMidiHeader);
                        bufferCount--;
                        headerBuilder.Destroy();

                        throw new OutputDeviceException(result);
                    }
                }
                else
                {
                    headerBuilder.Destroy();

                    throw new OutputDeviceException(result);
                }
            }
        }

        public virtual void Send(SysCommonMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            Send(message.Message);
        }

        public virtual void Send(SysRealtimeMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            Send(message.Message);
        }

        public override void Reset()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock(lockObject)
            {
                int result = midiOutReset(Handle); 

                if(result == MidiDeviceException.MMSYSERR_NOERROR)
                {
                    while(bufferCount > 0)
                    {
                        Monitor.Wait(lockObject);
                    }
                }
                else
                {
                    throw new OutputDeviceException(result);
                }                
            }
        }        

        protected void Send(int message)
        {
            lock(lockObject)
            {
                int result = midiOutShortMsg(Handle, message);

                if(result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    throw new OutputDeviceException(result);
                }
            }
        }

        public static MidiOutCaps GetDeviceCapabilities(int deviceID)
        {
            MidiOutCaps caps = new MidiOutCaps();

            IntPtr devId = (IntPtr)deviceID;
            int result = midiOutGetDevCaps(devId, ref caps, Marshal.SizeOf(caps));

            if(result != MidiDeviceException.MMSYSERR_NOERROR)
            {
                throw new OutputDeviceException(result);
            }

            return caps;
        }

        protected virtual void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
        {
            if(msg == MOM_OPEN)
            {
            }
            else if(msg == MOM_CLOSE)
            {
            }
            else if(msg == MOM_DONE)
            {
                delegateQueue.Post(ReleaseBuffer, param1);
            }
        }

        private void ReleaseBuffer(object state)
        {
            lock(lockObject)
            {
                IntPtr headerPtr = (IntPtr)state;

                int result = midiOutUnprepareHeader(Handle, headerPtr, SizeOfMidiHeader);

                if(result != MidiDeviceException.MMSYSERR_NOERROR)
                {
                    Exception ex = new OutputDeviceException(result);

                    OnError(new ErrorEventArgs(ex));
                }

                headerBuilder.Destroy(headerPtr);

                bufferCount--;

                Monitor.Pulse(lockObject);

                Debug.Assert(bufferCount >= 0);                
            }
        }

        public override void Dispose()
        {
            #region Guard

            if(IsDisposed)
            {
                return;
            }

            #endregion

            lock(lockObject)
            {
                Close();          
            }
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
                return midiOutGetNumDevs();
            }
        }        
    }
}
