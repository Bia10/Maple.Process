using Maple.Process;

Console.WriteLine($"Maple.Process version: {typeof(ProcessHandle).Assembly.GetName().Version}");

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("Skipping process attach test: not Windows.");
}
else
{
    bool attached = ProcessHandle.TryAttach("maple-not-running", out ProcessHandle? handle);
    handle?.Dispose();
    Console.WriteLine(attached ? "Attached to maple-not-running" : "Process not found (expected)");
}

Console.WriteLine("OK");
