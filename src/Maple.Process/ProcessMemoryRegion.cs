namespace Maple.Process;

/// <summary>
/// Native Windows page-protection flags returned by <c>VirtualQueryEx</c>.
/// </summary>
[Flags]
public enum WindowsMemoryProtection : uint
{
    NoAccess = 0x01,
    ReadOnly = 0x02,
    ReadWrite = 0x04,
    WriteCopy = 0x08,
    Execute = 0x10,
    ExecuteRead = 0x20,
    ExecuteReadWrite = 0x40,
    ExecuteWriteCopy = 0x80,
    Guard = 0x100,
    NoCache = 0x200,
    WriteCombine = 0x400,
    TargetsInvalid = 0x40000000,
}

/// <summary>
/// Native Windows page-state values returned by <c>VirtualQueryEx</c>.
/// </summary>
public enum WindowsMemoryState : uint
{
    Commit = 0x1000,
    Reserve = 0x2000,
    Free = 0x10000,
}

/// <summary>
/// Native Windows region-type values returned by <c>VirtualQueryEx</c>.
/// </summary>
public enum WindowsMemoryType : uint
{
    Private = 0x20000,
    Mapped = 0x40000,
    Image = 0x1000000,
}

/// <summary>
/// Describes one region returned by <c>VirtualQueryEx</c>.
/// </summary>
public readonly record struct ProcessMemoryRegion(
    nuint BaseAddress,
    nuint AllocationBase,
    WindowsMemoryProtection AllocationProtect,
    nuint RegionSize,
    WindowsMemoryState State,
    WindowsMemoryProtection Protect,
    WindowsMemoryType Type
)
{
    /// <summary>
    /// Gets whether <paramref name="address"/> falls inside this region.
    /// </summary>
    public bool Contains(nuint address) => address >= BaseAddress && address - BaseAddress < RegionSize;
}
