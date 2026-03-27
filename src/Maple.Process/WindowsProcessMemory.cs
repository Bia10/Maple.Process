using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace Maple.Process;

/// <summary>
/// Windows access rights needed for remote Maple process memory operations.
/// </summary>
[Flags]
public enum WindowsProcessAccess : uint
{
    /// <summary>Permission to create a thread in the target process.</summary>
    CreateThread = 0x0002,

    /// <summary>Permission to create or free pages in the target process.</summary>
    VmOperation = 0x0008,

    /// <summary>Permission to read pages from the target process.</summary>
    VmRead = 0x0010,

    /// <summary>Permission to write pages into the target process.</summary>
    VmWrite = 0x0020,

    /// <summary>Permission to query process information needed by tooling.</summary>
    QueryInformation = 0x0400,

    /// <summary>Default access mask for native object allocation and patching.</summary>
    Default = VmOperation | VmRead | VmWrite | QueryInformation,

    /// <summary>Access mask required for remote thread-proc execution helpers.</summary>
    RemoteExecution = CreateThread | VmOperation | VmRead | VmWrite | QueryInformation,
}

/// <summary>
/// Windows-only remote-process memory backend implemented with <c>kernel32.dll</c>.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class WindowsProcessMemory : IRemoteProcessMemory, IRemoteProcessMemoryInspector, IDisposable
{
    private const uint MemCommit = 0x1000;
    private const uint MemReserve = 0x2000;
    private const uint MemRelease = 0x8000;
    private const uint PageReadWrite = 0x04;
    private readonly SafeProcessHandle _processHandle;
    private readonly bool _ownsHandle;
    private bool _disposed;

    /// <summary>
    /// Opens a remote-process memory session for <paramref name="processId"/>.
    /// </summary>
    public static WindowsProcessMemory Open(int processId, WindowsProcessAccess access = WindowsProcessAccess.Default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        SafeProcessHandle handle = WindowsProcessNative.OpenProcess((uint)access, false, (uint)processId);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            throw CreateWin32Exception($"Failed to open process {processId}.");
        }

        return new WindowsProcessMemory(handle, processId, ownsHandle: true);
    }

    /// <summary>
    /// Opens a remote-process memory session for an attached <see cref="ProcessHandle"/>.
    /// </summary>
    public static WindowsProcessMemory Open(
        ProcessHandle processHandle,
        WindowsProcessAccess access = WindowsProcessAccess.Default
    )
    {
        ArgumentNullException.ThrowIfNull(processHandle);
        return Open(processHandle.ProcessId, access);
    }

    /// <summary>
    /// Creates a remote-process memory backend over an existing <see cref="SafeProcessHandle"/>.
    /// </summary>
    public WindowsProcessMemory(SafeProcessHandle processHandle, int processId, bool ownsHandle = false)
    {
        ArgumentNullException.ThrowIfNull(processHandle);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (processHandle.IsInvalid)
            throw new ArgumentException("Process handle must not be invalid.", nameof(processHandle));

        ValidateProcessHandle(processHandle, processId, nameof(processHandle));

        _processHandle = processHandle;
        ProcessId = processId;
        _ownsHandle = ownsHandle;
    }

    /// <summary>
    /// Creates a remote-process memory backend over an existing raw process handle.
    /// </summary>
    public WindowsProcessMemory(nint processHandle, int processId, bool ownsHandle = false)
        : this(new SafeProcessHandle(processHandle, ownsHandle), processId, ownsHandle)
    {
        if (processHandle == 0)
            throw new ArgumentOutOfRangeException(nameof(processHandle));
    }

    /// <summary>
    /// Gets the target process identifier.
    /// </summary>
    public int ProcessId { get; }

    /// <summary>
    /// Gets the Windows safe process handle.
    /// </summary>
    public SafeProcessHandle ProcessHandle => _processHandle;

    /// <summary>
    /// Queries the virtual-memory region containing <paramref name="address"/>.
    /// </summary>
    public ProcessMemoryRegion Query(nuint address)
    {
        if (TryQuery(address, out ProcessMemoryRegion region))
            return region;

        throw CreateWin32Exception($"Failed to query the region containing 0x{address:X} in process {ProcessId}.");
    }

    /// <summary>
    /// Attempts to query the virtual-memory region containing <paramref name="address"/>.
    /// </summary>
    public bool TryQuery(nuint address, out ProcessMemoryRegion region)
    {
        ThrowIfDisposed();

        if (Environment.Is64BitProcess)
        {
            nuint result = WindowsProcessNative.VirtualQueryEx(
                _processHandle,
                (nint)address,
                out WindowsProcessNative.MemoryBasicInformation64 buffer,
                (nuint)Marshal.SizeOf<WindowsProcessNative.MemoryBasicInformation64>()
            );

            if (result == 0)
            {
                region = default;
                return false;
            }

            region = new ProcessMemoryRegion(
                (nuint)buffer.BaseAddress,
                (nuint)buffer.AllocationBase,
                (WindowsMemoryProtection)buffer.AllocationProtect,
                (nuint)buffer.RegionSize,
                (WindowsMemoryState)buffer.State,
                (WindowsMemoryProtection)buffer.Protect,
                (WindowsMemoryType)buffer.Type
            );
            return true;
        }

        nuint queryResult = WindowsProcessNative.VirtualQueryEx(
            _processHandle,
            (nint)address,
            out WindowsProcessNative.MemoryBasicInformation32 buffer32,
            (nuint)Marshal.SizeOf<WindowsProcessNative.MemoryBasicInformation32>()
        );

        if (queryResult == 0)
        {
            region = default;
            return false;
        }

        region = new ProcessMemoryRegion(
            buffer32.BaseAddress,
            buffer32.AllocationBase,
            (WindowsMemoryProtection)buffer32.AllocationProtect,
            buffer32.RegionSize,
            (WindowsMemoryState)buffer32.State,
            (WindowsMemoryProtection)buffer32.Protect,
            (WindowsMemoryType)buffer32.Type
        );
        return true;
    }

    /// <summary>
    /// Changes the protection on the pages covering <paramref name="address"/> and returns the previous protection.
    /// </summary>
    public WindowsMemoryProtection Protect(uint address, int size, WindowsMemoryProtection protection)
    {
        ThrowIfDisposed();

        if (address == 0)
            throw new ArgumentOutOfRangeException(nameof(address));

        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Protection size must be positive.");

        if (protection == 0)
            throw new ArgumentOutOfRangeException(nameof(protection));

        if (!VirtualProtectEx(_processHandle, (nint)address, (nuint)size, (uint)protection, out uint previousProtect))
        {
            throw CreateWin32Exception(
                $"Failed to change protection at 0x{address:X8} for {size} byte(s) in process {ProcessId}."
            );
        }

        return (WindowsMemoryProtection)previousProtect;
    }

    /// <summary>
    /// Creates one thread in the target process.
    /// </summary>
    public WindowsRemoteThread CreateRemoteThread(
        nint startAddress,
        nint parameter = 0,
        nuint stackSize = 0,
        uint creationFlags = 0
    )
    {
        ThrowIfDisposed();

        if (startAddress == 0)
            throw new ArgumentOutOfRangeException(nameof(startAddress));

        SafeWaitHandle threadHandle = WindowsProcessNative.CreateRemoteThread(
            _processHandle,
            0,
            stackSize,
            startAddress,
            parameter,
            creationFlags,
            out uint threadId
        );
        if (threadHandle.IsInvalid)
        {
            threadHandle.Dispose();
            throw CreateWin32Exception(
                $"Failed to create a remote thread in process {ProcessId} at 0x{startAddress.ToInt64():X}."
            );
        }

        return new WindowsRemoteThread(threadHandle, threadId, ownsHandle: true);
    }

    /// <inheritdoc/>
    public uint Allocate(int size)
    {
        ThrowIfDisposed();

        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Allocation size must be positive.");

        nint address = VirtualAllocEx(_processHandle, 0, (nuint)size, MemCommit | MemReserve, PageReadWrite);
        if (address == 0)
            throw CreateWin32Exception($"Failed to allocate {size} bytes in process {ProcessId}.");

        long rawAddress = address;
        if ((ulong)rawAddress > uint.MaxValue)
        {
            VirtualFreeEx(_processHandle, address, 0, MemRelease);
            throw new InvalidOperationException(
                $"Remote allocation returned 0x{rawAddress:X}, which does not fit in the x86 Maple address space."
            );
        }

        return (uint)rawAddress;
    }

    /// <inheritdoc/>
    public unsafe bool Read(uint address, Span<byte> destination)
    {
        ThrowIfDisposed();

        if (destination.IsEmpty)
            return true;

        fixed (byte* destinationPtr = destination)
        {
            return ReadProcessMemory(
                    _processHandle,
                    (nint)address,
                    destinationPtr,
                    (nuint)destination.Length,
                    out nuint bytesRead
                )
                && bytesRead == (nuint)destination.Length;
        }
    }

    /// <inheritdoc/>
    public unsafe bool Write(uint address, ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        if (data.IsEmpty)
            return true;

        fixed (byte* dataPtr = data)
        {
            return WriteProcessMemory(
                    _processHandle,
                    (nint)address,
                    dataPtr,
                    (nuint)data.Length,
                    out nuint bytesWritten
                )
                && bytesWritten == (nuint)data.Length;
        }
    }

    /// <inheritdoc/>
    public void Free(uint address)
    {
        ThrowIfDisposed();

        if (address == 0)
            throw new ArgumentOutOfRangeException(nameof(address));

        if (!VirtualFreeEx(_processHandle, (nint)address, 0, MemRelease))
            throw CreateWin32Exception($"Failed to free remote address 0x{address:X8} in process {ProcessId}.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_ownsHandle)
            _processHandle.Dispose();

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed || _processHandle.IsClosed)
            throw new ObjectDisposedException(GetType().FullName);
    }

    private static void ValidateProcessHandle(SafeProcessHandle processHandle, int processId, string paramName)
    {
        uint actualProcessId = WindowsProcessNative.GetProcessId(processHandle);
        if (actualProcessId == 0)
            throw CreateWin32Exception("Failed to determine the process id for the supplied handle.");

        if (actualProcessId != (uint)processId)
        {
            throw new ArgumentException(
                $"Process handle targets PID {actualProcessId}, which does not match the supplied PID {processId}.",
                paramName
            );
        }
    }

    private static Win32Exception CreateWin32Exception(string message)
    {
        int error = Marshal.GetLastPInvokeError();
        return new Win32Exception(error, message);
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint VirtualAllocEx(
        SafeProcessHandle processHandle,
        nint address,
        nuint size,
        uint allocationType,
        uint protect
    );

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualFreeEx(SafeProcessHandle processHandle, nint address, nuint size, uint freeType);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualProtectEx(
        SafeProcessHandle processHandle,
        nint address,
        nuint size,
        uint newProtect,
        out uint oldProtect
    );

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool ReadProcessMemory(
        SafeProcessHandle processHandle,
        nint baseAddress,
        byte* buffer,
        nuint size,
        out nuint bytesRead
    );

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool WriteProcessMemory(
        SafeProcessHandle processHandle,
        nint baseAddress,
        byte* buffer,
        nuint size,
        out nuint bytesWritten
    );
}
