using System.Runtime.Versioning;
using BenchmarkDotNet.Attributes;

namespace Maple.Process.Benchmarks;

[SupportedOSPlatform("windows")]
public class ProcessHandleBench
{
    private readonly int _currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

    [Benchmark]
    public void TryAttach_UnknownProcess()
    {
        ProcessHandle.TryAttach("maple-bench-not-running", out ProcessHandle? handle);
        handle?.Dispose();
    }
}
