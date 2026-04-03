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
namespace Sanford.Multimedia.Midi
{
    public abstract partial class OutputDeviceBase
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RtMidiWrapperNative
        {
            public IntPtr ptr;
            public IntPtr callback_proxy;
            public IntPtr error_callback_proxy;
            [MarshalAs(UnmanagedType.U1)]
            public bool ok;
            public IntPtr msg;
        }

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr rtmidi_out_create_default();

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rtmidi_out_free(IntPtr device);

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rtmidi_open_port(IntPtr device, uint portNumber,
            [MarshalAs(UnmanagedType.LPStr)] string portName);

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rtmidi_close_port(IntPtr device);

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint rtmidi_get_port_count(IntPtr device);

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rtmidi_get_port_name(IntPtr device, uint portNumber, IntPtr bufOut, IntPtr bufLen);

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rtmidi_out_send_message(IntPtr device, IntPtr message, int length);

        protected static void ThrowIfRtMidiFailed(IntPtr device)
        {
            if (device == IntPtr.Zero)
            {
                throw new OutputDeviceException("RtMidi device handle is null.");
            }

            RtMidiWrapperNative w = (RtMidiWrapperNative)Marshal.PtrToStructure(device, typeof(RtMidiWrapperNative));
            if (!w.ok)
            {
                string m = w.msg != IntPtr.Zero ? Marshal.PtrToStringAnsi(w.msg) : "RtMidi operation failed.";
                throw new OutputDeviceException(m);
            }
        }

        private static string GetRtMidiOutputPortName(IntPtr device, uint portNumber)
        {
            return RtMidiPortName.Read(device, portNumber);//, rtmidi_get_port_name, ThrowIfRtMidiFailed);
        }

        private static int GetRtMidiOutputDeviceCountInternal()
        {
            RtMidiLoader.EnsureLoaded();
            IntPtr temp = rtmidi_out_create_default();
            if (temp == IntPtr.Zero)
            {
                throw new OutputDeviceException("RtMidi failed to create output enumerator.");
            }

            try
            {
                ThrowIfRtMidiFailed(temp);
                uint count = rtmidi_get_port_count(temp);
                ThrowIfRtMidiFailed(temp);
                return (int)count;
            }
            finally
            {
                rtmidi_out_free(temp);
            }
        }

        /// <summary>
        /// Closes the RtMidi port and frees the wrapper. Used by <see cref="OutputDevice"/>.
        /// </summary>
        protected void ReleaseRtMidiHandleForDispose(bool throwOnError)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            rtmidi_close_port(handle);
            if (throwOnError)
            {
                ThrowIfRtMidiFailed(handle);
            }

            rtmidi_out_free(handle);
            handle = IntPtr.Zero;
        }
    }
}
