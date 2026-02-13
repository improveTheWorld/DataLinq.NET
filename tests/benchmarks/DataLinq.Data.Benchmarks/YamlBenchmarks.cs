using BenchmarkDotNet.Attributes;
using DataLinq.Data;

namespace DataLinq.Data.Benchmarks;

[Config(typeof(DefaultBenchmarkConfig))]
public class YamlBenchmarks
{
    record Node(int id, string name, bool ok);

    [GlobalSetup]
    public void Setup() => DatasetSetup.Ensure(Path.Combine(Path.GetTempPath(), "DataLinqBench"));

    [Benchmark]
    public async Task Yaml_Sequence_Read()
    {
        var opts = new YamlReadOptions<Node> { ErrorAction = ReaderErrorAction.Skip };
        int count = 0;
        await foreach (var _ in Read.Yaml<Node>(DatasetSetup.Clean.YamlSequencePath, opts))
            count++;
    }
}