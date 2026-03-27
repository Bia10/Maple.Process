namespace Maple.Process;

/// <summary>
/// Raw remote-process memory primitives for an attached Maple client process.
/// </summary>
public interface IRemoteProcessMemory : IDisposable
{
    /// <summary>
    /// Allocates writable memory in the remote process and returns its 32-bit address.
    /// </summary>
    uint Allocate(int size);

    /// <summary>
    /// Reads bytes from the remote process into <paramref name="destination"/>.
    /// </summary>
    bool Read(uint address, Span<byte> destination);

    /// <summary>
    /// Writes bytes into the remote process at <paramref name="address"/>.
    /// </summary>
    bool Write(uint address, ReadOnlySpan<byte> data);

    /// <summary>
    /// Releases a block previously allocated in the remote process.
    /// </summary>
    void Free(uint address);
}
