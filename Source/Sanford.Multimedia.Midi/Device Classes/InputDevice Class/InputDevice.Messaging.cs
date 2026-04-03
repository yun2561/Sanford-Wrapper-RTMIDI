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
using System.Runtime.InteropServices;
using Sanford.Multimedia;

namespace Sanford.Multimedia.Midi
{
    public partial class InputDevice
    {
        /// <summary>
        /// Gets or sets a value indicating whether the midi input driver callback should be posted on a delegate queue with its own thread.
        /// Default is <c>true</c>. If set to <c>false</c> the driver callback directly calls the events for lowest possible latency.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the midi input driver callback should be posted on a delegate queue with its own thread; otherwise, <c>false</c>.
        /// </value>
        public bool PostDriverCallbackToDelegateQueue
        {
            get;
            set;
        }

        internal void DispatchRtMidiCallback(double timeStamp, IntPtr message, UIntPtr messageSizeU)
        {
            int size = (int)messageSizeU.ToUInt32();
            if (size <= 0)
            {
                return;
            }

            int timestampMs = (int)(timeStamp * 1000.0);
            if (timestampMs < 0)
            {
                timestampMs = 0;
            }

            byte[] bytes = new byte[size];
            Marshal.Copy(message, bytes, 0, size);

            if (!recording || resetting)
            {
                return;
            }

            var state = new RtMidiMessageState { Bytes = bytes, TimestampMs = timestampMs };
            if (PostDriverCallbackToDelegateQueue)
            {
                delegateQueue.Post(ProcessRtMidiBytesPosted, state);
            }
            else
            {
                ProcessRtMidiBytesPosted(state);
            }
        }

        private void ProcessRtMidiBytesPosted(object state)
        {
            var s = (RtMidiMessageState)state;
            ProcessRtMidiBytesCore(s.Bytes, s.TimestampMs);
        }

        private static int PackShortMessage(byte[] bytes, int length)
        {
            if (length == 1)
            {
                return bytes[0];
            }

            if (length == 2)
            {
                return bytes[0] | (bytes[1] << 8);
            }

            return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
        }

        private void ProcessRtMidiBytesCore(byte[] bytes, int timestamp)
        {
            if (bytes == null || bytes.Length == 0 || resetting)
            {
                return;
            }

            int len = bytes.Length;

            if (bytes[0] == (byte)SysExType.Start)
            {
                if (len >= 2 && bytes[len - 1] == 0xF7)
                {
                    SysExMessage message = new SysExMessage(bytes);
                    message.Timestamp = timestamp;
                    OnMessageReceived(message);
                    OnSysExMessageReceived(new SysExMessageEventArgs(message));
                }

                return;
            }

            if (bytes[0] == (byte)SysExType.Continuation)
            {
                SysExMessage message = new SysExMessage(bytes);
                message.Timestamp = timestamp;
                OnMessageReceived(message);
                OnSysExMessageReceived(new SysExMessageEventArgs(message));
                return;
            }

            int packed = PackShortMessage(bytes, len);
            OnShortMessage(new ShortMessageEventArgs(packed, timestamp));

            int status = ShortMessage.UnpackStatus(packed);

            if (status >= (int)ChannelCommand.NoteOff &&
                   status <= (int)ChannelCommand.PitchWheel +
                   ChannelMessage.MidiChannelMaxValue)
            {
                cmBuilder.Message = packed;
                cmBuilder.Build();

                cmBuilder.Result.Timestamp = timestamp;
                OnMessageReceived(cmBuilder.Result);
                OnChannelMessageReceived(new ChannelMessageEventArgs(cmBuilder.Result));
            }
            else if (status == (int)SysCommonType.MidiTimeCode ||
                   status == (int)SysCommonType.SongPositionPointer ||
                   status == (int)SysCommonType.SongSelect ||
                   status == (int)SysCommonType.TuneRequest)
            {
                scBuilder.Message = packed;
                scBuilder.Build();

                scBuilder.Result.Timestamp = timestamp;
                OnMessageReceived(scBuilder.Result);
                OnSysCommonMessageReceived(new SysCommonMessageEventArgs(scBuilder.Result));
            }
            else
            {
                SysRealtimeMessageEventArgs e = null;

                switch ((SysRealtimeType)status)
                {
                    case SysRealtimeType.ActiveSense:
                        e = SysRealtimeMessageEventArgs.ActiveSense;
                        break;

                    case SysRealtimeType.Clock:
                        e = SysRealtimeMessageEventArgs.Clock;
                        break;

                    case SysRealtimeType.Continue:
                        e = SysRealtimeMessageEventArgs.Continue;
                        break;

                    case SysRealtimeType.Reset:
                        e = SysRealtimeMessageEventArgs.Reset;
                        break;

                    case SysRealtimeType.Start:
                        e = SysRealtimeMessageEventArgs.Start;
                        break;

                    case SysRealtimeType.Stop:
                        e = SysRealtimeMessageEventArgs.Stop;
                        break;

                    case SysRealtimeType.Tick:
                        e = SysRealtimeMessageEventArgs.Tick;
                        break;
                }

                if (e != null)
                {
                    e.Message.Timestamp = timestamp;
                    OnMessageReceived(e.Message);
                    OnSysRealtimeMessageReceived(e);
                }
            }
        }

        /// <summary>
        /// RtMidi delivers complete SysEx messages; this method is retained for API compatibility and always succeeds.
        /// </summary>
        public int AddSysExBuffer()
        {
            return DeviceException.MMSYSERR_NOERROR;
        }
    }
}
