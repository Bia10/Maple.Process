# Maple.Process

![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/Bia10/Maple.Process/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/Bia10/Maple.Process/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/Bia10/Maple.Process/branch/main/graph/badge.svg)](https://codecov.io/gh/Bia10/Maple.Process)
[![Nuget](https://img.shields.io/nuget/v/Maple.Process?color=purple)](https://www.nuget.org/packages/Maple.Process/)
[![License](https://img.shields.io/github/license/Bia10/Maple.Process)](https://github.com/Bia10/Maple.Process/blob/main/LICENSE)

Windows process attachment and raw remote memory primitives for Maple client tooling. **Windows-only** — trimmable and AOT/NativeAOT compatible.

⭐ Please star this project if you like it. ⭐

[Example](#example) | [Example Catalogue](#example-catalogue) | [Public API](docs/PublicApi.md)

## Example

```csharp
// Skip on non-Windows: ProcessHandle is a Windows-only API.
if (!OperatingSystem.IsWindows())
    return;

// Try to attach to a process by name; returns false if not running.
bool attached = ProcessHandle.TryAttach("Maplestory.exe", out ProcessHandle? handle);
handle?.Dispose();
_ = attached;
```

For more examples see [Example Catalogue](#example-catalogue).

## Benchmarks

Benchmarks.

### Detailed Benchmarks

#### Comparison Benchmarks

##### TestBench Benchmark Results

###### Results will be populated here after running `dotnet Build.cs comparison-bench` then `dotnet test`

## Example Catalogue

The following examples are available in [ReadMeTest.cs](src/Maple.Process.DocTest/ReadMeTest.cs).

### Example - Empty

```csharp
// Skip on non-Windows: ProcessHandle is a Windows-only API.
if (!OperatingSystem.IsWindows())
    return;

// Try to attach to a process by name; returns false if not running.
bool attached = ProcessHandle.TryAttach("Maplestory.exe", out ProcessHandle? handle);
handle?.Dispose();
_ = attached;
```

## Public API Reference

See [docs/PublicApi.md](docs/PublicApi.md) for the complete auto-generated public API reference.

> **Note**: `docs/PublicApi.md` is auto-updated by the `ReadMeTest_PublicApi` test on every `dotnet test` run. Do not edit it manually.
