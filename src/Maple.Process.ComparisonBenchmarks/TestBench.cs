using System.Runtime.Versioning;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Maple.Process.ComparisonBenchmarks;

[SupportedOSPlatform("windows")]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
[BenchmarkCategory("0")]
public class TestBench
{
    [Params(25_000)]
    public int Count { get; set; }

    [Benchmark(Baseline = true)]
    public void MapleProcess______()
    {
        // Baseline: try attach to a non-existent process — measures P/Invoke + enum overhead
        ProcessHandle.TryAttach("maple-bench-not-running", out ProcessHandle? handle);
        handle?.Dispose();
    }
}
