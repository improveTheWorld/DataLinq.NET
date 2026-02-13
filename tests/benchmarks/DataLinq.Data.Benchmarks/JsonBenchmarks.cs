using BenchmarkDotNet.Attributes;
using DataLinq.Data;

namespace DataLinq.Data.Benchmarks;

[Config(typeof(DefaultBenchmarkConfig))]
public class JsonBenchmarks
{
    record JObj(int id, double amount, bool ok);

    [GlobalSetup]
    public void Setup() => DatasetSetup.Ensure(Path.Combine(Path.GetTempPath(), "DataLinqBench"));

    [Benchmark]
    public async Task Json_Array_FastPath()
    {
        var opts = new JsonReadOptions<JObj> { RequireArrayRoot = true };
        int count = 0;
        await foreach (var _ in Read.Json<JObj>(DatasetSetup.Clean.JsonArrayPath, opts))
            count++;
    }

    [Benchmark]
    public async Task Json_Array_Validation()
    {
        var opts = new JsonReadOptions<JObj>
        {
            RequireArrayRoot = true,
            ValidateElements = true,
            ElementValidator = e => e.TryGetProperty("amount", out _)
        };
        int count = 0;
        await foreach (var _ in Read.Json<JObj>(DatasetSetup.Clean.JsonArrayPath, opts))
            count++;
    }
}