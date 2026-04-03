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
    public partial class InputDevice
    {
        /// <summary>
        /// Matches <c>struct RtMidiWrapper</c> from rtmidi_c.h (RtMidi C API).
        /// </summary>
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void RtMidiCCallback(double timeStamp, IntPtr message, UIntPtr messageSize, IntPtr userData);

        #region RtMidi C API (rtmidi32.dll)

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr rtmidi_in_create_default();

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rtmidi_in_free(IntPtr device);

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
        private static extern void rtmidi_in_set_callback(IntPtr device, RtMidiCCallback callback, IntPtr userData);

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rtmidi_in_cancel_callback(IntPtr device);

        [DllImport(RtMidiNative.DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void rtmidi_in_ignore_types(IntPtr device, [MarshalAs(UnmanagedType.U1)] bool midiSysex,
            [MarshalAs(UnmanagedType.U1)] bool midiTime, [MarshalAs(UnmanagedType.U1)] bool midiSense);

        #endregion

        private static void ThrowIfRtMidiFailed(IntPtr device)
        {
            if (device == IntPtr.Zero)
            {
                throw new InputDeviceException("RtMidi device handle is null.");
            }

            RtMidiWrapperNative w = (RtMidiWrapperNative)Marshal.PtrToStructure(device, typeof(RtMidiWrapperNative));
            if (!w.ok)
            {
                string m = w.msg != IntPtr.Zero ? Marshal.PtrToStringAnsi(w.msg) : "RtMidi operation failed.";
                throw new InputDeviceException(m);
            }
        }

        private static string GetRtMidiPortName(IntPtr device, uint portNumber)
        {
            return RtMidiPortName.Read(device, portNumber);//, rtmidi_get_port_name, ThrowIfRtMidiFailed);
        }

        private static void RtMidiStaticCallback(double timeStamp, IntPtr message, UIntPtr messageSize, IntPtr userData)
        {
            if (userData == IntPtr.Zero)
            {
                return;
            }

            InputDevice dev = GCHandle.FromIntPtr(userData).Target as InputDevice;
            if (dev == null)
            {
                return;
            }

            dev.DispatchRtMidiCallback(timeStamp, message, messageSize);
        }

        private static int GetRtMidiInputDeviceCountInternal()
        {
            RtMidiLoader.EnsureLoaded();
            IntPtr temp = rtmidi_in_create_default();
            if (temp == IntPtr.Zero)
            {
                throw new InputDeviceException("RtMidi failed to create input enumerator.");
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
                rtmidi_in_free(temp);
            }
        }
    }
}
