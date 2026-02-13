using BenchmarkDotNet.Attributes;
using DataLinq.Data;

namespace DataLinq.Data.Benchmarks;

[Config(typeof(DefaultBenchmarkConfig))]
public class TextBenchmarks
{
    [GlobalSetup]
    public void Setup() => DatasetSetup.Ensure(Path.Combine(Path.GetTempPath(), "DataLinqBench"));

    [Benchmark]
    public async Task Text_Read()
    {
        int count = 0;
        await foreach (var _ in Read.Text(DatasetSetup.Clean.TextPath))
            count++;
    }
}