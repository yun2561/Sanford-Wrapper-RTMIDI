using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    internal static class RtMidiPortName
    {
        [DllImport(RtMidiNative.DllName, EntryPoint = "rtmidi_get_port_name", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetPortNamePtr(IntPtr device, uint portNumber);

        internal static string Read(IntPtr device, uint portNumber)
        {
            IntPtr ptr = GetPortNamePtr(device, portNumber);
            if (ptr == IntPtr.Zero)
                return string.Empty;

            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;

            if (len == 0)
                return string.Empty;

            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes, 0, len-2);
        }
    }
}