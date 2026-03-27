namespace Maple.Process;

/// <summary>
/// Optional region-inspection surface for raw remote-process memory backends.
/// </summary>
public interface IRemoteProcessMemoryInspector
{
    /// <summary>
    /// Attempts to query the virtual-memory region containing <paramref name="address"/>.
    /// </summary>
    bool TryQuery(nuint address, out ProcessMemoryRegion region);
}
