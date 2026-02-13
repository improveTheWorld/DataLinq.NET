using BenchmarkDotNet.Attributes;
using DataLinq.Data;

namespace DataLinq.Data.Benchmarks;

[Config(typeof(DefaultBenchmarkConfig))]
public class CsvBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        DatasetSetup.Ensure(Path.Combine(Path.GetTempPath(), "DataLinqBench"));
    }

    [Benchmark]
    public async Task Csv_RfcStrict_Inference()
    {
        var opts = new CsvReadOptions
        {
            HasHeader = true,
            InferSchema = true,
            SchemaInferenceMode = SchemaInferenceMode.ColumnNamesAndTypes,
            SchemaInferenceSampleRows = 500,
            // Changed: avoid throwing during perf run if generator produces stray quotes
            QuoteMode = CsvQuoteMode.Lenient,
            ErrorAction = ReaderErrorAction.Skip,
            ErrorSink = NullErrorSink.Instance
        };
        long count = 0;
        await foreach (var _ in Read.Csv<dynamic>(DatasetSetup.Clean.CsvPath, opts))
            count++;
    }

    [Benchmark]
    public async Task Csv_RfcStrict_NoInference()
    {
        var opts = new CsvReadOptions
        {
            HasHeader = true,
            QuoteMode = CsvQuoteMode.Lenient,
            ErrorAction = ReaderErrorAction.Skip,
            ErrorSink = NullErrorSink.Instance
        };
        long count = 0;
        await foreach (var _ in Read.Csv<dynamic>(DatasetSetup.Clean.CsvPath, opts))
            count++;
    }
}

[Config(typeof(DefaultBenchmarkConfig))]
public class CsvErrorBenchmarks
{
    [GlobalSetup]
    public void Setup() => DatasetSetup.Ensure(Path.Combine(Path.GetTempPath(), "DataLinqBench"), includeNoisy: true);

    [Benchmark]
    public async Task Csv_ErrorHeavy_Skip()
    {
        var opts = new CsvReadOptions
        {
            HasHeader = true,
            ErrorAction = ReaderErrorAction.Skip,
            QuoteMode = CsvQuoteMode.RfcStrict,
            ErrorSink = NullErrorSink.Instance
        };
        long count = 0;
        await foreach (var _ in Read.Csv<dynamic>(DatasetSetup.Noisy.CsvPath, opts))
            count++;
    }
}

internal sealed class NullErrorSink : IReaderErrorSink
{
    public static readonly NullErrorSink Instance = new();
    public void Report(ReaderError error) { }
    public void Dispose() { }
}
[Config(typeof(DefaultBenchmarkConfig))]
public class JsonErrorBenchmarks
{
    record JObj(int id, double? amount, bool ok);

    [GlobalSetup]
    public void Setup() => DatasetSetup.Ensure(Path.Combine(Path.GetTempPath(), "DataLinqBench"), includeNoisy: true);

    [Benchmark]
    public async Task Json_Array_Validator_Skip()
    {
        var opts = new JsonReadOptions<JObj>
        {
            RequireArrayRoot = true,
            ValidateElements = true,
            ElementValidator = e => e.TryGetProperty("amount", out _),
            ErrorAction = ReaderErrorAction.Skip,
            ErrorSink = NullErrorSink.Instance
        };
        int count = 0;
        await foreach (var _ in Read.Json<JObj>(DatasetSetup.Noisy.JsonArrayPath, opts))
            count++;
    }
}
