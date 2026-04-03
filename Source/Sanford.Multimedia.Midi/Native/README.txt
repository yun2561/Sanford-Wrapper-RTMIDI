RtMidi native library (required at runtime)
==========================================

Sanford.Multimedia.Midi (x86 / 32-bit) uses P/Invoke to load "rtmidi32.dll". Place your 32-bit
RtMidi build here:

  Source\Sanford.Multimedia.Midi\Native\rtmidi32.dll

If this file is missing next to Sanford.Multimedia.Midi.dll (or in the Native subfolder), you will get:

  System.DllNotFoundException

If the file EXISTS but you still see:
  无法加载 DLL "rtmidi32.dll" / HRESULT 0x8007007E
  (or Win32 error 126)

this usually means a *dependency* of rtmidi32.dll failed to load — most often:
  - Microsoft Visual C++ Redistributable (MSVC runtime) is not installed
    Install the latest "VC++ Redistributable" **x86** from Microsoft (32-bit build).
  - Or you copied only rtmidi32.dll but not other DLLs from the same build output folder.
    Copy the whole bin folder from vcpkg / your CMake build, or use Dependencies.exe on rtmidi32.dll
    to see the missing module (https://github.com/lucasg/Dependencies).

MinGW builds also need libgcc_s_*.dll, libstdc++-6.dll, libwinpthread-1.dll next to rtmidi32.dll.

Steps (pick one):

1) vcpkg (Windows x86 example)
   vcpkg install rtmidi:x86-windows
   Copy from:
     <vcpkg>\installed\x86-windows\bin\
   Rename the produced rtmidi.dll to rtmidi32.dll (or build with output name rtmidi32.dll), and copy
   all dependent DLLs from the same folder.

2) CMake build from https://github.com/thestk/rtmidi
   Build shared library for **Win32**; copy the produced DLL as rtmidi32.dll and any dependent DLLs from the same output.

3) Place rtmidi32.dll here:
     Source\Sanford.Multimedia.Midi\Native\rtmidi32.dll
   The project copies it to the library output and demo apps when the file exists.

Architecture must match: this project targets **x86** (32-bit). Use a 32-bit RtMidi DLL only.
