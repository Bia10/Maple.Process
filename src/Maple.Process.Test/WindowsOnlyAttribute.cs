using TUnit.Core;

namespace Maple.Process.Test;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class WindowsOnlyAttribute : SkipAttribute
{
    public WindowsOnlyAttribute()
        : base("Only supported on Windows.") { }

    public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
        Task.FromResult(!OperatingSystem.IsWindows());
}
