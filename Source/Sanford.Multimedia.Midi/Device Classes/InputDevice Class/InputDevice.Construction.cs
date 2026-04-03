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
using Sanford.Threading;

namespace Sanford.Multimedia.Midi
{
    public partial class InputDevice : MidiDevice
    {
        #region Construction

        /// <summary>
        /// Initializes a new instance of the InputDevice class with the 
        /// specified device ID (RtMidi port index, valid range <c>0</c> through <c>DeviceCount - 1</c>).
        /// Opens the MIDI input port and begins delivering events to subscribers (same effect as an open port plus <see cref="StartRecording"/>).
        /// After <see cref="StopRecording"/> or <see cref="Reset"/>, call <see cref="StartRecording"/> to reopen and receive again.
        /// </summary>
        /// <param name="deviceID">Zero-based input port index.</param>
        //public InputDevice(int deviceID, bool postEventsOnCreationContext = true, bool postDriverCallbackToDelegateQueue = true)
        //   : base(deviceID)
        public InputDevice(int deviceID)
            : base(deviceID)    
        {
            delegateQueue = new DelegateQueue();

            RtMidiLoader.EnsureLoaded();
            int portCount = GetRtMidiInputDeviceCountInternal();
            if (deviceID < 0 || deviceID >= portCount)
            {
                throw new InputDeviceException(DeviceException.MMSYSERR_BADDEVICEID);
            }

            handle = rtmidi_in_create_default();
            if (handle == IntPtr.Zero)
            {
                throw new InputDeviceException("RtMidi failed to allocate input.");
            }

            try
            {
                ThrowIfRtMidiFailed(handle);
                RtMidiWrapperNative w = (RtMidiWrapperNative)Marshal.PtrToStructure(handle, typeof(RtMidiWrapperNative));
                if (w.ptr == IntPtr.Zero)
                {
                    throw new InputDeviceException("RtMidi failed to create input.");
                }

                rtMidiSelfHandle = GCHandle.Alloc(this);
                rtmidi_in_set_callback(handle, RtMidiStaticCallback, GCHandle.ToIntPtr(rtMidiSelfHandle));
                ThrowIfRtMidiFailed(handle);
                rtmidi_in_ignore_types(handle, false, false, false);
                ThrowIfRtMidiFailed(handle);

                rtmidi_open_port(handle, (uint)deviceID, "Sanford.Multimedia.Midi");
                ThrowIfRtMidiFailed(handle);
                portOpen = true;
                recording = true;
            }
            catch
            {
                if (rtMidiSelfHandle.IsAllocated)
                {
                    rtMidiSelfHandle.Free();
                }

                if (handle != IntPtr.Zero)
                {
                    rtmidi_in_free(handle);
                    handle = IntPtr.Zero;
                }

                throw;
            }

            //PostEventsOnCreationContext = postEventsOnCreationContext;
            //PostDriverCallbackToDelegateQueue = postDriverCallbackToDelegateQueue;
            PostEventsOnCreationContext = true;
            PostDriverCallbackToDelegateQueue = true;
        }

        ~InputDevice()
        {
            if (!IsDisposed)
            {
                IntPtr h = handle;
                if (h != IntPtr.Zero)
                {
                    rtmidi_in_cancel_callback(h);
                    rtmidi_close_port(h);
                    rtmidi_in_free(h);
                }

                if (rtMidiSelfHandle.IsAllocated)
                {
                    rtMidiSelfHandle.Free();
                }
            }
        }

        #endregion
    }
}
