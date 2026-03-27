using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace Maple.Process;

/// <summary>
/// Windows-only process attachment session for Maple tooling.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ProcessHandle : IDisposable
{
    private readonly System.Diagnostics.Process _process;
    private readonly SafeProcessHandle _processHandle;
    private readonly bool _ownsHandle;
    private bool _disposed;

    private ProcessHandle(
        System.Diagnostics.Process process,
        SafeProcessHandle processHandle,
        WindowsProcessAccess access,
        bool ownsHandle
    )
    {
        _process = process;
        _processHandle = processHandle;
        Access = access;
        _ownsHandle = ownsHandle;
        ProcessId = process.Id;
    }

    /// <summary>
    /// Opens an attached process session for the specified <paramref name="processId"/>.
    /// </summary>
    public static ProcessHandle Open(int processId, WindowsProcessAccess access = WindowsProcessAccess.QueryInformation)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(processId);

        try
        {
            SafeProcessHandle handle = WindowsProcessNative.OpenProcess((uint)access, false, (uint)processId);
            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw CreateWin32Exception($"Failed to open process {processId}.");
            }

            return new ProcessHandle(process, handle, access, ownsHandle: true);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Tries to attach to the first running process whose name matches <paramref name="processName"/>.
    /// </summary>
    public static bool TryAttach(
        string processName,
        out ProcessHandle? processHandle,
        WindowsProcessAccess access = WindowsProcessAccess.QueryInformation
    )
    {
        string normalizedName = NormalizeProcessName(processName);
        System.Diagnostics.Process[] candidates = System.Diagnostics.Process.GetProcessesByName(normalizedName);

        Array.Sort(candidates, static (left, right) => left.Id.CompareTo(right.Id));

        foreach (System.Diagnostics.Process candidate in candidates)
        {
            if (candidate.Id <= 0)
                continue;

            try
            {
                processHandle = Open(candidate.Id, access);
                return true;
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }
            finally
            {
                candidate.Dispose();
            }
        }

        processHandle = null;
        return false;
    }

    /// <summary>
    /// Attaches to the first running process whose name matches <paramref name="processName"/>.
    /// </summary>
    public static ProcessHandle Attach(
        string processName,
        WindowsProcessAccess access = WindowsProcessAccess.QueryInformation
    )
    {
        if (TryAttach(processName, out ProcessHandle? processHandle, access))
            return processHandle!;

        throw new InvalidOperationException(
            $"No running process named '{NormalizeProcessName(processName)}' could be attached."
        );
    }

    /// <summary>
    /// Gets the attached process identifier.
    /// </summary>
    public int ProcessId { get; }

    /// <summary>
    /// Gets the access mask used when opening the process handle.
    /// </summary>
    public WindowsProcessAccess Access { get; }

    /// <summary>
    /// Gets the attached process name.
    /// </summary>
    public string ProcessName
    {
        get
        {
            ThrowIfDisposed();
            _process.Refresh();
            return _process.ProcessName;
        }
    }

    /// <summary>
    /// Gets typed metadata for the attached process' main module.
    /// </summary>
    public ProcessModuleInfo GetMainModule()
    {
        if (TryGetMainModule(out ProcessModuleInfo module))
            return module;

        throw new InvalidOperationException($"Failed to query the main module for process {ProcessId}.");
    }

    /// <summary>
    /// Attempts to get typed metadata for the attached process' main module.
    /// </summary>
    public bool TryGetMainModule(out ProcessModuleInfo module)
    {
        ThrowIfDisposed();

        try
        {
            _process.Refresh();
            ProcessModule? mainModule = _process.MainModule;
            if (mainModule is null || mainModule.ModuleMemorySize <= 0)
            {
                module = default;
                return false;
            }

            module = new ProcessModuleInfo(
                mainModule.ModuleName,
                mainModule.FileName,
                (nuint)mainModule.BaseAddress,
                mainModule.ModuleMemorySize
            );
            return true;
        }
        catch (InvalidOperationException)
        {
            module = default;
            return false;
        }
        catch (Win32Exception)
        {
            module = default;
            return false;
        }
        catch (NotSupportedException)
        {
            module = default;
            return false;
        }
    }

    /// <summary>
    /// Gets whether the process session is currently attached.
    /// </summary>
    public bool IsAttached => !_disposed && !_processHandle.IsClosed && !_processHandle.IsInvalid;

    /// <summary>
    /// Gets whether the attached process has exited.
    /// </summary>
    public bool HasExited
    {
        get
        {
            ThrowIfDisposed();
            _process.Refresh();
            return _process.HasExited;
        }
    }

    /// <summary>
    /// Gets the Windows safe process handle for the attached session.
    /// </summary>
    public SafeProcessHandle SafeHandle
    {
        get
        {
            ThrowIfDisposed();
            return _processHandle;
        }
    }

    /// <summary>
    /// Detaches from the process and releases owned resources.
    /// </summary>
    public void Detach() => Dispose();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_ownsHandle)
            _processHandle.Dispose();

        _process.Dispose();
        _disposed = true;
    }

    private static Win32Exception CreateWin32Exception(string message)
    {
        int error = Marshal.GetLastPInvokeError();
        return new Win32Exception(error, message);
    }

    private static string NormalizeProcessName(string processName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);
        return Path.GetFileNameWithoutExtension(processName.Trim());
    }

    private void ThrowIfDisposed()
    {
        if (_disposed || _processHandle.IsClosed)
            throw new ObjectDisposedException(GetType().FullName);
    }
}
