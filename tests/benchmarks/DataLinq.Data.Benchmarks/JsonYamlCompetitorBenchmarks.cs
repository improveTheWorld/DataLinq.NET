using BenchmarkDotNet.Attributes;
using DataLinq.Data;
using System.Text.Json;
using YamlDotNet.Core;

namespace DataLinq.Data.Benchmarks;

[Config(typeof(DefaultBenchmarkConfig))]
[MemoryDiagnoser]
public class JsonCompetitorBenchmarks
{
    private string _jsonPath = default!;

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
        var root = Path.Combine(Path.GetTempPath(), "DataLinqBench", "json_competitors");
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);

        var rowCount = 8_000_000; // ~1GB JSON file
        _jsonPath = Path.Combine(root, "data.json");

        using var writer = new StreamWriter(_jsonPath);
        writer.Write("[");
        var date = DateTime.Now;
        for (int i = 0; i < rowCount; i++)
        {
            if (i > 0) writer.Write(",");
            writer.Write($"{{\"Id\":{i},\"Name\":\"User {i}\",\"Email\":\"user{i}@example.com\",\"Registered\":\"{date:O}\",\"IsActive\":{(i % 2 == 0 ? "true" : "false")}}}");
        }
        writer.Write("]");
    }

    [Benchmark(Baseline = true)]
    public async Task DataLinq_JsonAsync()
    {
        var opts = new JsonReadOptions<Person>
        {
            RequireArrayRoot = true
        };

        await foreach (var item in Read.Json<Person>(_jsonPath, opts))
        {
            // Consumer loop
        }
    }

    [Benchmark]
    public void DataLinq_JsonSync()
    {
        var opts = new JsonReadOptions<Person> { RequireArrayRoot = true };
        foreach (var _ in Read.JsonSync<Person>(_jsonPath, opts))
        {
        }
    }

    [Benchmark]
    public async Task SystemTextJson_Deserialize()
    {
        await using var fs = File.OpenRead(_jsonPath);
        var items = await JsonSerializer.DeserializeAsync<Person[]>(fs);
        foreach (var item in items!)
        {
            // Consumer loop
        }
    }

    [Benchmark]
    public async Task SystemTextJson_DeserializeAsyncEnumerable()
    {
        await using var fs = File.OpenRead(_jsonPath);
        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<Person>(fs))
        {
            // Consumer loop
        }
    }
}

[Config(typeof(DefaultBenchmarkConfig))]
[MemoryDiagnoser]
public class YamlCompetitorBenchmarks
{
    private string _yamlPath = default!;

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
        var root = Path.Combine(Path.GetTempPath(), "DataLinqBench", "yaml_competitors");
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);

        var docCount = 8_000_000; // ~1GB YAML file
        _yamlPath = Path.Combine(root, "data.yaml");
        using (var writer = new StreamWriter(_yamlPath))
        {
            var date = DateTime.Now;
            for (int i = 0; i < docCount; i++)
            {
                writer.WriteLine($"---");
                writer.WriteLine($"id: {i}");
                writer.WriteLine($"name: User {i}");
                writer.WriteLine($"email: user{i}@example.com");
                writer.WriteLine($"registered: {date:O}");
                writer.WriteLine($"isActive: {(i % 2 == 0).ToString().ToLower()}");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public async Task DataLinq_YamlAsync()
    {
        var opts = new YamlReadOptions<Person> { RestrictTypes = true };
        await foreach (var _ in Read.Yaml<Person>(_yamlPath, opts))
        {
        }
    }

    [Benchmark]
    public void DataLinq_YamlSync()
    {
        var opts = new YamlReadOptions<Person> { RestrictTypes = true };
        foreach (var _ in Read.YamlSync<Person>(_yamlPath, opts))
        {
        }
    }

    [Benchmark]
    public void YamlDotNet_Direct()
    {
        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance) // Match DataLinq behavior
            .Build();

        // YamlDotNet doesn't have a simple async streaming API for multiple docs in one file
        // equivalent to Read.Yaml, so we read the text and parse.
        // For fairness with DataLinq (which streams), we'll read line by line? 
        // No, YamlDotNet usually requires a TextReader.

        using var reader = File.OpenText(_yamlPath);
        var parser = new YamlDotNet.Core.Parser(reader);

        parser.Consume<YamlDotNet.Core.Events.StreamStart>();
        while (parser.Accept<YamlDotNet.Core.Events.DocumentStart>(out _))
        {
            var p = deserializer.Deserialize<Person>(parser);
        }
    }
}
