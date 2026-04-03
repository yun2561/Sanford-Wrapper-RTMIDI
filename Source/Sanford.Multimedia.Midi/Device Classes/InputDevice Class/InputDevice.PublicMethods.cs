#region License

/* Copyright (c) 2005 Leslie Sanford
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
using Sanford.Multimedia;

namespace Sanford.Multimedia.Midi
{
    public partial class InputDevice : MidiDevice
    {
        public override void Close()
        {
            #region Guard

            if(IsDisposed)
            {
                return;
            }

            #endregion

            Dispose(true);
        }

        /// <summary>
        /// Ensures the input port is open and MIDI events are delivered to handlers.
        /// After construction the device is already receiving; use this after <see cref="StopRecording"/> or <see cref="Reset"/>.
        /// </summary>
        public void StartRecording()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("InputDevice");
            }

            #endregion

            #region Guard

            if(recording)
            {
                return;
            }

            #endregion

            lock(lockObject)
            {
                if (!portOpen)
                {
                    rtmidi_open_port(handle, (uint)DeviceID, "Sanford.Multimedia.Midi");
                    ThrowIfRtMidiFailed(handle);
                    portOpen = true;
                }

                recording = true;
            }
        }

        public void StopRecording()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("InputDevice");
            }

            #endregion

            #region Guard

            if(!recording)
            {
                return;
            }

            #endregion

            lock(lockObject)
            {
                rtmidi_close_port(handle);
                ThrowIfRtMidiFailed(handle);
                portOpen = false;
                recording = false;
            }
        }

        public override void Reset()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException("InputDevice");
            }

            #endregion

            lock(lockObject)
            {
                resetting = true;

                rtmidi_close_port(handle);
                ThrowIfRtMidiFailed(handle);

                portOpen = false;
                recording = false;
                resetting = false;
            }
        }
        
        public static MidiInCaps GetDeviceCapabilities(int deviceID)
        {
            RtMidiLoader.EnsureLoaded();
            IntPtr temp = rtmidi_in_create_default();
            if (temp == IntPtr.Zero)
            {
                throw new InputDeviceException("RtMidi failed to create input.");
            }

            try
            {
                ThrowIfRtMidiFailed(temp);
                uint count = rtmidi_get_port_count(temp);
                ThrowIfRtMidiFailed(temp);

                if (deviceID < 0 || (uint)deviceID >= count)
                {
                    throw new InputDeviceException(DeviceException.MMSYSERR_BADDEVICEID);
                }

                // Primary: rtmidi_get_port_name (via ReadUtf8). Fallback: WinMM if empty.
                string name = GetRtMidiPortName(temp, (uint)deviceID);
                if (string.IsNullOrWhiteSpace(name))
                {
                    string fallback = WinMmMidiPortNames.TryGetInputName(deviceID);
                    if (!string.IsNullOrEmpty(fallback))
                    {
                        name = fallback;
                    }
                }

                MidiInCaps caps = new MidiInCaps();
                caps.name = name;
                caps.mid = 0;
                caps.pid = 0;
                caps.driverVersion = 0;
                caps.support = 0;

                return caps;
            }
            finally
            {
                rtmidi_in_free(temp);
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

            Dispose(true);
        }
    }
}
