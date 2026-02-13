using BenchmarkDotNet.Running;

namespace DataLinq.Data.Benchmarks;

public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--profile")
        {
            await MemoryProfiler.RunAndPrint();
            return;
        }
        if (args.Length > 0 && args[0] == "--compare")
        {
            await CompetitorMemoryProfiler.RunAndPrint();
            return;
        }
        if (args.Length > 0 && args[0] == "--json-memory")
        {
            await JsonYamlMemoryProfiler.RunJsonAndPrint();
            return;
        }
        if (args.Length > 0 && args[0] == "--yaml-memory")
        {
            await JsonYamlMemoryProfiler.RunYamlAndPrint();
            return;
        }
        if (args.Length > 0 && args[0] == "--leak-check")
        {
            await JsonYamlMemoryProfiler.RunLeakCheck();
            return;
        }
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}