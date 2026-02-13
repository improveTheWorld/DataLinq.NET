using BenchmarkDotNet.Attributes;
using CsvHelper;
using CsvHelper.Configuration;
using DataLinq.Data;

using System.Globalization;

namespace DataLinq.Data.Benchmarks;

[Config(typeof(DefaultBenchmarkConfig))]
[MemoryDiagnoser]
public class CompetitorBenchmarks
{
    private string _csvPath = default!;

    // Simple POCO for fair comparison
    public class Person
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public DateTime Registered { get; set; }
        public bool IsActive { get; set; }
    }

    [GlobalSetup]
    public void Setup()
    {
        var root = Path.Combine(Path.GetTempPath(), "DataLinqBench", "competitors");
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);

        // Generate ~13M rows (~1GB) for a significant throughput test
        var rowCount = 13_000_000;
        _csvPath = Path.Combine(root, "data.csv");

        using var writer = new StreamWriter(_csvPath);
        writer.WriteLine("Id,Name,Email,Registered,IsActive");

        var date = DateTime.Now;
        for (int i = 0; i < rowCount; i++)
        {
            writer.WriteLine($"{i},User {i},user{i}@example.com,{date:O},{i % 2 == 0}");
        }
    }

    [Benchmark(Baseline = true)]
    public async Task DataLinq_Async()
    {
        var opts = new CsvReadOptions
        {
            HasHeader = true,
            InferSchema = true
        };

        await foreach (var row in Read.Csv<Person>(_csvPath, opts))
        {
            // Consumer loop
        }
    }

    [Benchmark]
    public void DataLinq_Sync()
    {
        var opts = new CsvReadOptions
        {
            HasHeader = true,
            InferSchema = true
        };

        foreach (var row in Read.CsvSync<Person>(_csvPath, opts))
        {
            // Consumer loop
        }
    }

    [Benchmark]
    public async Task CsvHelper_ReadPOCO()
    {
        using var reader = new StreamReader(_csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        await foreach (var record in csv.GetRecordsAsync<Person>())
        {
            // Consumer loop
        }
    }


}
