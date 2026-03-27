# Public API Reference

## Public API Reference

```csharp
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/Maple.Process/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Process.Benchmarks")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Process.ComparisonBenchmarks")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Process.DocTest")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Process.Test")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace Maple.Process
{
    public interface IRemoteProcessMemory : System.IDisposable
    {
        uint Allocate(int size);
        void Free(uint address);
        bool Read(uint address, System.Span<byte> destination);
        bool Write(uint address, System.ReadOnlySpan<byte> data);
    }
    public interface IRemoteProcessMemoryInspector
    {
        bool TryQuery(nuint address, out Maple.Process.ProcessMemoryRegion region);
    }
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class ProcessHandle : System.IDisposable
    {
        public Maple.Process.WindowsProcessAccess Access { get; }
        public bool HasExited { get; }
        public bool IsAttached { get; }
        public int ProcessId { get; }
        public string ProcessName { get; }
        public Microsoft.Win32.SafeHandles.SafeProcessHandle SafeHandle { get; }
        public void Detach() { }
        public void Dispose() { }
        public Maple.Process.ProcessModuleInfo GetMainModule() { }
        public bool TryGetMainModule(out Maple.Process.ProcessModuleInfo module) { }
        public static Maple.Process.ProcessHandle Attach(string processName, Maple.Process.WindowsProcessAccess access = 1024) { }
        public static Maple.Process.ProcessHandle Open(int processId, Maple.Process.WindowsProcessAccess access = 1024) { }
        public static bool TryAttach(string processName, out Maple.Process.ProcessHandle? processHandle, Maple.Process.WindowsProcessAccess access = 1024) { }
    }
    public readonly struct ProcessMemoryRegion : System.IEquatable<Maple.Process.ProcessMemoryRegion>
    {
        public ProcessMemoryRegion(nuint BaseAddress, nuint AllocationBase, Maple.Process.WindowsMemoryProtection AllocationProtect, nuint RegionSize, Maple.Process.WindowsMemoryState State, Maple.Process.WindowsMemoryProtection Protect, Maple.Process.WindowsMemoryType Type) { }
        public System.UIntPtr AllocationBase { get; init; }
        public Maple.Process.WindowsMemoryProtection AllocationProtect { get; init; }
        public System.UIntPtr BaseAddress { get; init; }
        public Maple.Process.WindowsMemoryProtection Protect { get; init; }
        public System.UIntPtr RegionSize { get; init; }
        public Maple.Process.WindowsMemoryState State { get; init; }
        public Maple.Process.WindowsMemoryType Type { get; init; }
        public bool Contains(nuint address) { }
    }
    public readonly struct ProcessModuleInfo : System.IEquatable<Maple.Process.ProcessModuleInfo>
    {
        public ProcessModuleInfo(string ModuleName, string FileName, nuint BaseAddress, int ImageSize) { }
        public System.UIntPtr BaseAddress { get; init; }
        public string FileName { get; init; }
        public int ImageSize { get; init; }
        public string ModuleName { get; init; }
        public bool Contains(nuint address) { }
    }
    [System.Flags]
    public enum WindowsMemoryProtection : uint
    {
        NoAccess = 1u,
        ReadOnly = 2u,
        ReadWrite = 4u,
        WriteCopy = 8u,
        Execute = 16u,
        ExecuteRead = 32u,
        ExecuteReadWrite = 64u,
        ExecuteWriteCopy = 128u,
        Guard = 256u,
        NoCache = 512u,
        WriteCombine = 1024u,
        TargetsInvalid = 1073741824u,
    }
    public enum WindowsMemoryState : uint
    {
        Commit = 4096u,
        Reserve = 8192u,
        Free = 65536u,
    }
    public enum WindowsMemoryType : uint
    {
        Private = 131072u,
        Mapped = 262144u,
        Image = 16777216u,
    }
    [System.Flags]
    public enum WindowsProcessAccess : uint
    {
        CreateThread = 2u,
        VmOperation = 8u,
        VmRead = 16u,
        VmWrite = 32u,
        QueryInformation = 1024u,
        Default = 1080u,
        RemoteExecution = 1082u,
    }
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class WindowsProcessMemory : Maple.Process.IRemoteProcessMemory, Maple.Process.IRemoteProcessMemoryInspector, System.IDisposable
    {
        public WindowsProcessMemory(Microsoft.Win32.SafeHandles.SafeProcessHandle processHandle, int processId, bool ownsHandle = false) { }
        public WindowsProcessMemory(nint processHandle, int processId, bool ownsHandle = false) { }
        public Microsoft.Win32.SafeHandles.SafeProcessHandle ProcessHandle { get; }
        public int ProcessId { get; }
        public uint Allocate(int size) { }
        public Maple.Process.WindowsRemoteThread CreateRemoteThread(nint startAddress, nint parameter = 0, nuint stackSize = 0, uint creationFlags = 0) { }
        public void Dispose() { }
        public void Free(uint address) { }
        public Maple.Process.WindowsMemoryProtection Protect(uint address, int size, Maple.Process.WindowsMemoryProtection protection) { }
        public Maple.Process.ProcessMemoryRegion Query(nuint address) { }
        public bool Read(uint address, System.Span<byte> destination) { }
        public bool TryQuery(nuint address, out Maple.Process.ProcessMemoryRegion region) { }
        public bool Write(uint address, System.ReadOnlySpan<byte> data) { }
        public static Maple.Process.WindowsProcessMemory Open(Maple.Process.ProcessHandle processHandle, Maple.Process.WindowsProcessAccess access = 1080) { }
        public static Maple.Process.WindowsProcessMemory Open(int processId, Maple.Process.WindowsProcessAccess access = 1080) { }
    }
    public sealed class WindowsRemoteThread : System.IDisposable
    {
        public Microsoft.Win32.SafeHandles.SafeWaitHandle SafeHandle { get; }
        public uint ThreadId { get; }
        public void Dispose() { }
        public uint GetExitCode() { }
        public bool Wait(int millisecondsTimeout = -1) { }
    }
}
```
