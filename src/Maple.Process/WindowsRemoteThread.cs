using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Maple.Process;

/// <summary>
/// Windows thread handle returned by <c>CreateRemoteThread</c>.
/// </summary>
public sealed class WindowsRemoteThread : IDisposable
{
    private const uint WaitObject0 = 0x00000000;
    private const uint WaitTimeout = 0x00000102;
    private const uint WaitFailed = 0xFFFFFFFF;
    private const uint Infinite = 0xFFFFFFFF;

    private readonly SafeWaitHandle _threadHandle;
    private readonly bool _ownsHandle;
    private bool _disposed;

    internal WindowsRemoteThread(SafeWaitHandle threadHandle, uint threadId, bool ownsHandle)
    {
        _threadHandle = threadHandle ?? throw new ArgumentNullException(nameof(threadHandle));
        if (threadHandle.IsInvalid)
            throw new ArgumentException("Thread handle must not be invalid.", nameof(threadHandle));

        ThreadId = threadId;
        _ownsHandle = ownsHandle;
    }

    /// <summary>
    /// Gets the created thread identifier.
    /// </summary>
    public uint ThreadId { get; }

    /// <summary>
    /// Gets the Windows safe wait handle for the created thread.
    /// </summary>
    public SafeWaitHandle SafeHandle
    {
        get
        {
            ThrowIfDisposed();
            return _threadHandle;
        }
    }

    /// <summary>
    /// Waits for the thread to finish execution.
    /// </summary>
    public bool Wait(int millisecondsTimeout = Timeout.Infinite)
    {
        ThrowIfDisposed();

        if (millisecondsTimeout < Timeout.Infinite)
            throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout));

        uint waitResult = WindowsProcessNative.WaitForSingleObject(
            _threadHandle,
            millisecondsTimeout == Timeout.Infinite ? Infinite : (uint)millisecondsTimeout
        );

        return waitResult switch
        {
            WaitObject0 => true,
            WaitTimeout => false,
            WaitFailed => throw CreateWin32Exception("Failed while waiting for the remote thread."),
            _ => throw new InvalidOperationException(
                $"WaitForSingleObject returned unexpected result 0x{waitResult:X8}."
            ),
        };
    }

    /// <summary>
    /// Gets the thread exit code.
    /// </summary>
    public uint GetExitCode()
    {
        ThrowIfDisposed();

        if (!WindowsProcessNative.GetExitCodeThread(_threadHandle, out uint exitCode))
            throw CreateWin32Exception("Failed to get the remote thread exit code.");

        return exitCode;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_ownsHandle)
            _threadHandle.Dispose();

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed || _threadHandle.IsClosed)
            throw new ObjectDisposedException(GetType().FullName);
    }

    private static Win32Exception CreateWin32Exception(string message)
    {
        int error = Marshal.GetLastPInvokeError();
        return new Win32Exception(error, message);
    }
}
