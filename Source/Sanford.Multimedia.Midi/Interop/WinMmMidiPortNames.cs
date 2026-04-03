using System;
using System.Runtime.InteropServices;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// WinMM <c>midiInGetDevCapsW</c> / <c>midiOutGetDevCapsW</c> — fallback when <c>rtmidi_get_port_name</c> yields an empty name
    /// in <c>GetDeviceCapabilities</c> (same device index as RtMidi ports on Windows).
    /// </summary>
    internal static class WinMmMidiPortNames
    {
        private const int MaxPNameLen = 32;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MidiInCapsW
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPNameLen)]
            public string szPname;
            public uint dwSupport;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MidiOutCapsW
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPNameLen)]
            public string szPname;
            public ushort wTechnology;
            public ushort wVoices;
            public ushort wNotes;
            public ushort wChannelMask;
            public uint dwSupport;
        }

        [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "midiInGetDevCapsW")]
        private static extern int midiInGetDevCapsW(UIntPtr uDeviceID, ref MidiInCapsW caps, uint cbMidiInCaps);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "midiOutGetDevCapsW")]
        private static extern int midiOutGetDevCapsW(UIntPtr uDeviceID, ref MidiOutCapsW caps, uint cbMidiOutCaps);

        private static bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }

        internal static string TryGetInputName(int deviceId)
        {
            if (!IsWindows())
            {
                return null;
            }

            var caps = new MidiInCapsW();
            UIntPtr id = (UIntPtr)(uint)deviceId;
            int sz = Marshal.SizeOf(typeof(MidiInCapsW));
            if (midiInGetDevCapsW(id, ref caps, (uint)sz) != 0)
            {
                return null;
            }

            return caps.szPname;
        }

        internal static string TryGetOutputName(int deviceId)
        {
            if (!IsWindows())
            {
                return null;
            }

            var caps = new MidiOutCapsW();
            UIntPtr id = (UIntPtr)(uint)deviceId;
            int sz = Marshal.SizeOf(typeof(MidiOutCapsW));
            if (midiOutGetDevCapsW(id, ref caps, (uint)sz) != 0)
            {
                return null;
            }

            return caps.szPname;
        }
    }
}
