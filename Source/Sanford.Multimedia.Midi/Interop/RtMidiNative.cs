namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Windows RtMidi C API module. Use the 32-bit build named <c>rtmidi32.dll</c>
    /// (matches x86 / PlatformTarget x86 for Sanford.Multimedia.Midi).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Do not declare <c>rtmidi_get_port_name</c> as a two-argument function returning <c>string</c> / <c>LPStr</c>.
    /// The official C signature is <c>int rtmidi_get_port_name(RtMidiPtr device, unsigned int portNumber, char *bufOut, int *bufLen)</c>
    /// (see rtmidi_c.h / rtmidi_c.cpp): it returns a status / <c>snprintf</c> result, not a pointer to a static string.
    /// Marshaling it as <c>[return: MarshalAs(UnmanagedType.LPStr)] string</c> does not match the native ABI and will mis-bind or crash.
    /// </para>
    /// <para>
    /// Use the four-parameter <c>DllImport</c> on <see cref="InputDevice"/> / <see cref="OutputDeviceBase"/> partials and
    /// <see cref="RtMidiPortName"/> helpers to obtain the port name as a managed string.
    /// </para>
    /// </remarks>
    internal static class RtMidiNative
    {
        internal const string DllName = "rtmidi32.dll";
    }
}
