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
using System.Text;
using Sanford.Multimedia;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Represents a MIDI device capable of receiving MIDI events.
    /// </summary>
    public partial class InputDevice : MidiDevice
    {
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                lock(lockObject)
                {
                    Reset();

                    rtmidi_in_cancel_callback(handle);
                    rtmidi_in_free(handle);
                    handle = IntPtr.Zero;

                    if (rtMidiSelfHandle.IsAllocated)
                    {
                        rtMidiSelfHandle.Free();
                    }

                    delegateQueue.Dispose();
                }
            }
            else
            {
                rtmidi_in_cancel_callback(handle);
                rtmidi_in_free(handle);
                if (rtMidiSelfHandle.IsAllocated)
                {
                    rtMidiSelfHandle.Free();
                }
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// The exception that is thrown when a error occurs with the InputDevice
    /// class.
    /// </summary>
    public class InputDeviceException : MidiDeviceException
    {
        #region InputDeviceException Members

        #region Fields

        private StringBuilder errMsg = new StringBuilder(128);

        private string customMessage;

        #endregion 

        #region Construction

        /// <summary>
        /// Initializes a new instance of the InputDeviceException class with
        /// the specified error code.
        /// </summary>
        /// <param name="errCode">
        /// The error code.
        /// </param>
        public InputDeviceException(int errCode) : base(errCode)
        {
            errMsg.Append("MIDI input error (code ").Append(errCode).Append(").");
        }

        /// <summary>
        /// Initializes a new instance with a RtMidi or driver error description.
        /// </summary>
        public InputDeviceException(string message) : base(DeviceException.MMSYSERR_ERROR)
        {
            customMessage = message;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message
        {
            get
            {
                return customMessage != null ? customMessage : errMsg.ToString();
            }
        }

        #endregion

        #endregion
    }
}
