using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Sanford.Multimedia.Midi
{
    /// <summary>
    /// Pre-loads rtmidi32.dll before P/Invoke so dependencies resolve. Searches the folder that
    /// contains Sanford.Multimedia.Midi.dll, then the app base directory (exe folder), then other
    /// common locations — so native MIDI still works when the managed assembly is not beside the
    /// host exe or when the debugger loads a copy from a different path.
    /// </summary>
    internal static class RtMidiLoader
    {
        private const int ErrorModNotFound = 126;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr reserved, uint flags);

        /// <summary>
        /// LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR (0x100) — search the directory that contains the native DLL for its dependencies first.
        /// Helps when MSVC runtime DLLs are copied next to rtmidi32.dll.
        /// </summary>
        private const uint LoadLibrarySearchDllLoadDir = 0x00000100;

        private static bool loaded;
        private static readonly object sync = new object();

        /// <summary>
        /// Distinct folders to probe for rtmidi32.dll (and Native\rtmidi32.dll), most specific first.
        /// </summary>
        private static string[] GetNativeDllSearchBases()
        {
            var list = new List<string>();

            void TryAdd(string dir)
            {
                if (string.IsNullOrEmpty(dir))
                {
                    return;
                }

                try
                {
                    dir = Path.GetFullPath(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                }
                catch
                {
                    return;
                }

                foreach (string existing in list)
                {
                    if (string.Equals(existing, dir, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                list.Add(dir);
            }

            string midiLoc = typeof(RtMidiLoader).Assembly.Location;
            if (!string.IsNullOrEmpty(midiLoc))
            {
                TryAdd(Path.GetDirectoryName(midiLoc));
            }

            try
            {
                TryAdd(AppDomain.CurrentDomain.BaseDirectory);
            }
            catch
            {
            }

            try
            {
                Assembly entry = Assembly.GetEntryAssembly();
                if (entry != null && !string.IsNullOrEmpty(entry.Location))
                {
                    TryAdd(Path.GetDirectoryName(entry.Location));
                }
            }
            catch
            {
            }

            TryAdd(Environment.CurrentDirectory);

            return list.ToArray();
        }

        internal static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            lock (sync)
            {
                if (loaded)
                {
                    return;
                }

                foreach (string baseDir in GetNativeDllSearchBases())
                {
                    string[] relativePaths =
                    {
                        RtMidiNative.DllName,
                        Path.Combine("Native", RtMidiNative.DllName)
                    };

                    foreach (string rel in relativePaths)
                    {
                        string p = Path.Combine(baseDir, rel);
                        if (!File.Exists(p))
                        {
                            continue;
                        }

                        IntPtr h = LoadLibraryEx(p, IntPtr.Zero, LoadLibrarySearchDllLoadDir);
                        if (h == IntPtr.Zero)
                        {
                            h = LoadLibrary(p);
                        }

                        if (h == IntPtr.Zero)
                        {
                            int err = Marshal.GetLastWin32Error();
                            throw BuildLoadFailure(p, err);
                        }

                        loaded = true;
                        return;
                    }
                }

                // No file found on disk in known locations; let the CLR resolve rtmidi32.dll on first P/Invoke (default search path).
                loaded = true;
            }
        }

        private static Exception BuildLoadFailure(string path, int win32Error)
        {
            var sb = new StringBuilder(512);
            sb.Append("Failed to load ").Append(RtMidiNative.DllName).Append(" from \"").Append(path).Append("\". ");
            sb.Append("Win32 error ").Append(win32Error).Append(" (0x").Append(win32Error.ToString("X")).Append("). ");

            if (win32Error == ErrorModNotFound)
            {
                sb.Append("The file is present but a dependency DLL could not be loaded (HRESULT 0x8007007E is common here). ");
                sb.Append("Install the latest \"Microsoft Visual C++ Redistributable\" for x86 (match 32-bit apps). ");
                sb.Append("If you use MinGW-built ").Append(RtMidiNative.DllName).Append(", also copy libgcc_s_*.dll, libstdc++-6.dll, libwinpthread-1.dll next to it. ");
                sb.Append("Use the Dependencies tool (github.com/lucasg/Dependencies) on ").Append(RtMidiNative.DllName).Append(" to see which DLL is missing.");
            }
            else
            {
                try
                {
                    sb.Append(new Win32Exception(win32Error).Message);
                }
                catch
                {
                }
            }

            return new DllNotFoundException(sb.ToString());
        }
    }
}
