using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;

namespace DataLinq.Data.Benchmarks;

public class DefaultBenchmarkConfig : ManualConfig
{
    public DefaultBenchmarkConfig()
    {
        AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance);
        AddLogger(ConsoleLogger.Default);
        if (!GetExporters().Any(e => e.Name == MarkdownExporter.GitHub.Name))
            AddExporter(MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
    }
}
