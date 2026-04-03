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
	/// <summary>
	/// The base class for all MIDI devices.
	/// </summary>
	public abstract class MidiDevice : Device
	{
        #region MidiDevice Members

        // Size of the MidiHeader structure.
        protected static readonly int SizeOfMidiHeader;

        static MidiDevice()
        {
            SizeOfMidiHeader = Marshal.SizeOf(typeof(MidiHeader));
        }

        public MidiDevice(int deviceID) : base(deviceID)
        {            
        }
        
        /// <summary>
        /// Previously connected WinMM MIDI devices using midiConnect (HMIDIIN/HMIDIOUT).
        /// RtMidi does not expose driver-level port linking; this API is not supported.
        /// </summary>
        /// <param name="handleA">Unused.</param>
        /// <param name="handleB">Unused.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public static void Connect(IntPtr handleA, IntPtr handleB)
        {
            throw new NotSupportedException(
                "MidiDevice.Connect is not supported: RtMidi has no equivalent to WinMM midiConnect. " +
                "Input and output use RtMidi wrapper handles, not HMIDIIN/HMIDIOUT. " +
                "Route MIDI in application code or use OS/synth virtual MIDI cabling.");
        }

        /// <summary>
        /// Previously disconnected WinMM MIDI devices using midiDisconnect.
        /// RtMidi does not expose driver-level port linking; this API is not supported.
        /// </summary>
        /// <param name="handleA">Unused.</param>
        /// <param name="handleB">Unused.</param>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public static void Disconnect(IntPtr handleA, IntPtr handleB)
        {
            throw new NotSupportedException(
                "MidiDevice.Disconnect is not supported: RtMidi has no equivalent to WinMM midiDisconnect. " +
                "See MidiDevice.Connect.");
        }        

        #endregion        
    }
}
