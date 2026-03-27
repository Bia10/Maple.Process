namespace Maple.Process;

/// <summary>
/// Immutable metadata for one process module.
/// </summary>
public readonly record struct ProcessModuleInfo(string ModuleName, string FileName, nuint BaseAddress, int ImageSize)
{
    /// <summary>
    /// Gets whether <paramref name="address"/> falls inside this module image.
    /// </summary>
    public bool Contains(nuint address) =>
        ImageSize > 0 && address >= BaseAddress && address - BaseAddress < (nuint)ImageSize;
}
