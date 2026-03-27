using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Maple.Process.Test;

[SupportedOSPlatform("windows")]
public sealed partial class WindowsProcessMemoryTests
{
    [Test]
    public async Task TryQuery_CurrentProcessMainModule_ReturnsContainingRegion()
    {
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var processMemory = WindowsProcessMemory.Open(currentProcess.Id, WindowsProcessAccess.QueryInformation);

        nuint mainModuleAddress = (nuint)currentProcess.MainModule!.BaseAddress;

        bool queried = processMemory.TryQuery(mainModuleAddress, out ProcessMemoryRegion region);

        await Assert.That(queried).IsTrue();
        await Assert.That(region.Contains(mainModuleAddress)).IsTrue();
        await Assert.That(region.BaseAddress).IsNotEqualTo((nuint)0);
        await Assert.That(region.AllocationBase).IsNotEqualTo((nuint)0);
        await Assert.That(region.RegionSize).IsGreaterThan((nuint)0);
        await Assert.That(region.State).IsEqualTo(WindowsMemoryState.Commit);
        await Assert.That(region.Type).IsEqualTo(WindowsMemoryType.Image);
    }

    [Test]
    public async Task CreateRemoteThread_CurrentProcessExitThread_ReturnsExpectedExitCode()
    {
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var processMemory = WindowsProcessMemory.Open(currentProcess.Id, WindowsProcessAccess.RemoteExecution);

        nint kernel32 = GetModuleHandle("kernel32.dll");
        await Assert.That(kernel32).IsNotEqualTo((nint)0);

        nint exitThreadAddress = GetProcAddress(kernel32, "ExitThread");
        await Assert.That(exitThreadAddress).IsNotEqualTo((nint)0);

        using WindowsRemoteThread thread = processMemory.CreateRemoteThread(exitThreadAddress, (nint)123);

        bool completed = thread.Wait(millisecondsTimeout: 5000);

        await Assert.That(completed).IsTrue();
        await Assert.That(thread.GetExitCode()).IsEqualTo(123u);
    }

    [Test]
    public async Task Protect_AllocatedRegion_UpdatesPageProtection()
    {
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var processMemory = WindowsProcessMemory.Open(currentProcess.Id, WindowsProcessAccess.Default);

        nint allocation = AllocateLowPage();

        try
        {
            WindowsMemoryProtection previousProtection = processMemory.Protect(
                checked((uint)allocation),
                sizeof(uint),
                WindowsMemoryProtection.ExecuteRead
            );

            ProcessMemoryRegion region = processMemory.Query((nuint)allocation);

            await Assert.That(previousProtection).IsEqualTo(WindowsMemoryProtection.ReadWrite);
            await Assert.That(region.Protect).IsEqualTo(WindowsMemoryProtection.ExecuteRead);
        }
        finally
        {
            FreeLowPage(allocation);
        }
    }

    [Test]
    public async Task Constructor_WhenHandlePidMismatch_Throws()
    {
        using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var processHandle = ProcessHandle.Open(currentProcess.Id, WindowsProcessAccess.QueryInformation);
        int mismatchedProcessId = currentProcess.Id == int.MaxValue ? currentProcess.Id - 1 : currentProcess.Id + 1;

        await Assert
            .That(() => new WindowsProcessMemory(processHandle.SafeHandle, mismatchedProcessId))
            .Throws<ArgumentException>();
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport(
        "kernel32.dll",
        EntryPoint = "GetModuleHandleW",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16
    )]
    private static partial nint GetModuleHandle(string moduleName);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint GetProcAddress(nint moduleHandle, string procName);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint VirtualAlloc(nint address, nuint size, uint allocationType, uint protect);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualFree(nint address, nuint size, uint freeType);

    private static nint AllocateLowPage()
    {
        const uint memCommit = 0x1000;
        const uint memReserve = 0x2000;
        const uint pageReadWrite = 0x04;

        foreach (nint hint in new[] { (nint)0x10000000, (nint)0x20000000, (nint)0x30000000 })
        {
            nint allocation = VirtualAlloc(
                hint,
                (nuint)Environment.SystemPageSize,
                memCommit | memReserve,
                pageReadWrite
            );
            if (allocation != 0 && allocation.ToInt64() <= uint.MaxValue)
                return allocation;

            if (allocation != 0)
                FreeLowPage(allocation);
        }

        throw new InvalidOperationException("Failed to allocate a low x86-compatible page for the protection test.");
    }

    private static void FreeLowPage(nint address)
    {
        const uint memRelease = 0x8000;

        if (address != 0)
            _ = VirtualFree(address, 0, memRelease);
    }
}
