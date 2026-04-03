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
using System.Text;
using Sanford.Multimedia;

namespace Sanford.Multimedia.Midi
{
	/// <summary>
	/// Represents a device capable of sending MIDI messages.
	/// </summary>
	public sealed class OutputDevice : OutputDeviceBase
	{
        private bool runningStatusEnabled = false;

        private int runningStatus = 0;        

        #region Construction

        /// <summary>
        /// Initializes a new instance of the OutputDevice class for the given RtMidi output port index
        /// (valid range <c>0</c> through <c>DeviceCount - 1</c>) and opens that port.
        /// </summary>
        /// <param name="deviceID">Zero-based output port index.</param>
        public OutputDevice(int deviceID) : base(deviceID)
        {
        }

        #endregion     
   
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                lock(lockObject)
                {
                    runningStatus = 0;
                    ReleaseRtMidiHandleForDispose(true);
                }
            }
            else
            {
                ReleaseRtMidiHandleForDispose(false);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Closes the OutputDevice.
        /// </summary>
        /// <exception cref="OutputDeviceException">
        /// If an error occurred while closing the OutputDevice.
        /// </exception>
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
        /// Resets the OutputDevice.
        /// </summary>
        public override void Reset()
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            runningStatus = 0;

            base.Reset();
        }

        public override void Send(ChannelMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            lock(lockObject)
            {
                Send(message.Message);
            }

            if(runningStatusEnabled)
            {
                runningStatus = message.Status;
            }
        }

        public override void Send(SysExMessage message)
        {
            runningStatus = 0;

            base.Send(message);
        }

        public override void Send(SysCommonMessage message)
        {
            #region Require

            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            #endregion

            runningStatus = 0;

            base.Send(message);
        }

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the OutputDevice uses
        /// a running status.
        /// </summary>
        public bool RunningStatusEnabled
        {
            get
            {
                return runningStatusEnabled;
            }
            set
            {
                runningStatusEnabled = value;

                runningStatus = 0;
            }
        }
        
        #endregion
    }

    /// <summary>
    /// The exception that is thrown when a error occurs with the OutputDevice
    /// class.
    /// </summary>
    public class OutputDeviceException : MidiDeviceException
    {
        #region OutputDeviceException Members

        #region Fields

        private StringBuilder message = new StringBuilder(128);        

        private string customMessage;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the OutputDeviceException class with
        /// the specified error code.
        /// </summary>
        /// <param name="errCode">
        /// The error code.
        /// </param>
        public OutputDeviceException(int errCode) : base(errCode)
        {
            message.Append("MIDI output error (code ").Append(errCode).Append(").");
        }

        /// <summary>
        /// Initializes a new instance with a RtMidi or driver error description.
        /// </summary>
        public OutputDeviceException(string text) : base(DeviceException.MMSYSERR_ERROR)
        {
            customMessage = text;
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
                return customMessage != null ? customMessage : message.ToString();
            }
        }        

        #endregion

        #endregion
    }
}
