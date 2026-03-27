using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Maple.Process;

internal static partial class WindowsProcessNative
{
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial SafeProcessHandle OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint processId
    );

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial uint GetProcessId(SafeProcessHandle processHandle);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nuint VirtualQueryEx(
        SafeProcessHandle processHandle,
        nint address,
        out MemoryBasicInformation32 buffer,
        nuint length
    );

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial nuint VirtualQueryEx(
        SafeProcessHandle processHandle,
        nint address,
        out MemoryBasicInformation64 buffer,
        nuint length
    );

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial SafeWaitHandle CreateRemoteThread(
        SafeProcessHandle processHandle,
        nint threadAttributes,
        nuint stackSize,
        nint startAddress,
        nint parameter,
        uint creationFlags,
        out uint threadId
    );

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial uint WaitForSingleObject(SafeWaitHandle handle, uint milliseconds);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetExitCodeThread(SafeWaitHandle threadHandle, out uint exitCode);

    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryBasicInformation32
    {
        internal uint BaseAddress;
        internal uint AllocationBase;
        internal uint AllocationProtect;
        internal uint RegionSize;
        internal uint State;
        internal uint Protect;
        internal uint Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryBasicInformation64
    {
        internal ulong BaseAddress;
        internal ulong AllocationBase;
        internal uint AllocationProtect;
        internal uint Alignment1;
        internal ulong RegionSize;
        internal uint State;
        internal uint Protect;
        internal uint Type;
        internal uint Alignment2;
    }
}
