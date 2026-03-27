using System.Diagnostics;
using System.Runtime.Versioning;

namespace Maple.Process.Test;

[SupportedOSPlatform("windows")]
public sealed class ProcessHandleTests
{
    [Test]
    public async Task Open_CurrentProcess_AttachesWithExpectedState()
    {
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var processHandle = ProcessHandle.Open(currentProcess.Id);

        await Assert.That(processHandle.ProcessId).IsEqualTo(currentProcess.Id);
        await Assert.That(processHandle.ProcessName).IsEqualTo(currentProcess.ProcessName);
        await Assert.That(processHandle.Access).IsEqualTo(WindowsProcessAccess.QueryInformation);
        await Assert.That(processHandle.IsAttached).IsTrue();
        await Assert.That(processHandle.HasExited).IsFalse();
    }

    [Test]
    public async Task TryAttach_UnknownProcess_ReturnsFalse()
    {
        bool attached = ProcessHandle.TryAttach($"maple-missing-{Guid.NewGuid():N}", out ProcessHandle? processHandle);

        await Assert.That(attached).IsFalse();
        await Assert.That(processHandle).IsNull();
    }

    [Test]
    public async Task TryAttach_WhenAnAccessibleProcessExists_AttachesToOne()
    {
        using var processes = new DisposableProcessCollection(System.Diagnostics.Process.GetProcesses());

        ProcessHandle? processHandle = null;
        bool attached = false;

        foreach (System.Diagnostics.Process process in processes.Items.OrderBy(static process => process.Id))
        {
            if (!ProcessHandle.TryAttach(process.ProcessName, out processHandle))
                continue;

            attached = true;
            break;
        }

        await Assert.That(attached).IsTrue();
        await Assert.That(processHandle).IsNotNull();

        using (processHandle)
            await Assert.That(processHandle!.IsAttached).IsTrue();
    }

    [Test]
    public async Task Detach_IsIdempotentAndMarksHandleDetached()
    {
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var processHandle = ProcessHandle.Open(currentProcess.Id);

        processHandle.Detach();
        processHandle.Detach();

        await Assert.That(processHandle.IsAttached).IsFalse();
        await Assert.That(() => processHandle.SafeHandle).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task GetMainModule_CurrentProcess_ReturnsTypedMetadata()
    {
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var processHandle = ProcessHandle.Open(currentProcess.Id);
        ProcessModule currentMainModule = currentProcess.MainModule!;

        ProcessModuleInfo module = processHandle.GetMainModule();

        await Assert.That(module.ModuleName).IsEqualTo(currentMainModule.ModuleName);
        await Assert.That(module.FileName).IsEqualTo(currentMainModule.FileName);
        await Assert.That(module.BaseAddress).IsEqualTo((nuint)currentMainModule.BaseAddress);
        await Assert.That(module.ImageSize).IsEqualTo(currentMainModule.ModuleMemorySize);
        await Assert.That(module.Contains((nuint)currentMainModule.BaseAddress)).IsTrue();
    }

    [Test]
    public async Task WindowsProcessMemory_OpenFromProcessHandle_UsesIndependentLifetime()
    {
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var processHandle = ProcessHandle.Open(currentProcess.Id, WindowsProcessAccess.Default);
        using var processMemory = WindowsProcessMemory.Open(processHandle, WindowsProcessAccess.Default);

        Microsoft.Win32.SafeHandles.SafeProcessHandle attachedHandle = processHandle.SafeHandle;

        processHandle.Detach();

        await Assert.That(object.ReferenceEquals(processMemory.ProcessHandle, attachedHandle)).IsFalse();
        await Assert.That(attachedHandle.IsClosed).IsTrue();
        await Assert.That(processMemory.ProcessHandle.IsClosed).IsFalse();
    }

    private sealed class DisposableProcessCollection : IDisposable
    {
        public DisposableProcessCollection(System.Diagnostics.Process[] items) => Items = items;

        public IReadOnlyList<System.Diagnostics.Process> Items { get; }

        public void Dispose()
        {
            foreach (System.Diagnostics.Process process in Items)
                process.Dispose();
        }
    }
}
