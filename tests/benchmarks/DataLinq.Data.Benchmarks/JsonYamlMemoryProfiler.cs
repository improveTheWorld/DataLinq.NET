using DataLinq.Data;
using System.Globalization;
using System.Text.Json;

namespace DataLinq.Data.Benchmarks;

/// <summary>
/// Memory profiler for JSON and YAML readers comparing DataLinq vs competitors.
/// </summary>
public static class JsonYamlMemoryProfiler
{
    public record Snapshot(double DataProcessedKB, double LiveMemoryMB);
    public record LibraryProfile(string Name, List<Snapshot> Snapshots, double TotalTimeSec);

    public static async Task<List<LibraryProfile>> ProfileJson(int objectCount = 100_000, int sampleEveryN = 10_000)
    {
        var root = Path.Combine(Path.GetTempPath(), "DataLinqBench", "json_memory");
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);

        var jsonPath = Path.Combine(root, "data.json");
        Console.WriteLine($"Generating {objectCount:N0} JSON objects...");

        using (var writer = new StreamWriter(jsonPath))
        {
            writer.Write("[");
            var date = DateTime.Now;
            for (int i = 0; i < objectCount; i++)
            {
                if (i > 0) writer.Write(",");
                writer.Write($"{{\"Id\":{i},\"Name\":\"User {i}\",\"Email\":\"user{i}@example.com\",\"Registered\":\"{date:O}\",\"IsActive\":{(i % 2 == 0 ? "true" : "false")}}}");
            }
            writer.Write("]");
        }

        var fileSize = new FileInfo(jsonPath).Length;
        double fileSizeKB = fileSize / 1024.0;
        Console.WriteLine($"JSON file: {fileSizeKB:F0} KB\n");

        var profiles = new List<LibraryProfile>();

        // Profile DataLinq JSON
        profiles.Add(await ProfileDataLinqJson(jsonPath, objectCount, sampleEveryN));

        // Profile System.Text.Json streaming
        profiles.Add(await ProfileSystemTextJsonStreaming(jsonPath, objectCount, sampleEveryN));

        return profiles;
    }

    private static async Task<LibraryProfile> ProfileDataLinqJson(string jsonPath, int objectCount, int sampleEvery)
    {
        Console.WriteLine("Profiling DataLinq.NET JSON...");
        var snapshots = new List<Snapshot>();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long baseline = GC.GetTotalMemory(true);
        int count = 0;
        snapshots.Add(new Snapshot(0, 0));

        var fileSize = new FileInfo(jsonPath).Length;
        double bytesPerObject = (double)fileSize / objectCount;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var opts = new JsonReadOptions<Person> { RequireArrayRoot = true };

        await foreach (var _ in Read.Json<Person>(jsonPath, opts))
        {
            count++;
            if (count % sampleEvery == 0)
            {
                double dataKB = (count * bytesPerObject) / 1024.0;
                double liveMB = Math.Max(0, GC.GetTotalMemory(false) - baseline) / 1024.0 / 1024.0;
                snapshots.Add(new Snapshot(dataKB, liveMB));
            }
        }
        sw.Stop();

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        double finalMB = Math.Max(0, GC.GetTotalMemory(true) - baseline) / 1024.0 / 1024.0;
        double totalKB = (count * bytesPerObject) / 1024.0;
        snapshots.Add(new Snapshot(totalKB, finalMB));

        Console.WriteLine($"  Done in {sw.Elapsed.TotalSeconds:F1}s ({count:N0} objects)\n");
        return new LibraryProfile("DataLinq.NET", snapshots, sw.Elapsed.TotalSeconds);
    }

    private static async Task<LibraryProfile> ProfileSystemTextJsonStreaming(string jsonPath, int objectCount, int sampleEvery)
    {
        Console.WriteLine("Profiling System.Text.Json (AsyncEnumerable)...");
        var snapshots = new List<Snapshot>();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long baseline = GC.GetTotalMemory(true);
        int count = 0;
        snapshots.Add(new Snapshot(0, 0));

        var fileSize = new FileInfo(jsonPath).Length;
        double bytesPerObject = (double)fileSize / objectCount;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await using var fs = File.OpenRead(jsonPath);

        await foreach (var _ in JsonSerializer.DeserializeAsyncEnumerable<Person>(fs))
        {
            count++;
            if (count % sampleEvery == 0)
            {
                double dataKB = (count * bytesPerObject) / 1024.0;
                double liveMB = Math.Max(0, GC.GetTotalMemory(false) - baseline) / 1024.0 / 1024.0;
                snapshots.Add(new Snapshot(dataKB, liveMB));
            }
        }
        sw.Stop();

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        double finalMB = Math.Max(0, GC.GetTotalMemory(true) - baseline) / 1024.0 / 1024.0;
        double totalKB = (count * bytesPerObject) / 1024.0;
        snapshots.Add(new Snapshot(totalKB, finalMB));

        Console.WriteLine($"  Done in {sw.Elapsed.TotalSeconds:F1}s ({count:N0} objects)\n");
        return new LibraryProfile("System.Text.Json", snapshots, sw.Elapsed.TotalSeconds);
    }

    public static void PrintResults(List<LibraryProfile> profiles, string format)
    {
        var inv = CultureInfo.InvariantCulture;
        Console.WriteLine($"=== {format} Memory Comparison Results ===\n");

        // Summary table
        Console.WriteLine("| Library | Time (s) | Peak Memory (MB) | Final Memory (MB) |");
        Console.WriteLine("|---------|----------|-----------------|------------------|");
        foreach (var p in profiles)
        {
            double peak = p.Snapshots.Max(s => s.LiveMemoryMB);
            double final = p.Snapshots.Last().LiveMemoryMB;
            Console.WriteLine($"| {p.Name} | {p.TotalTimeSec:F1} | {peak:F1} | {final:F1} |");
        }

        // Mermaid chart
        Console.WriteLine($"\n### {format} Memory Over Time\n");
        Console.WriteLine("```mermaid");
        Console.WriteLine("%%{init: {'theme': 'base', 'themeVariables': {'xyChart': {'plotColorPalette': '#22c55e, #3b82f6'}}}}%%");
        Console.WriteLine("xychart-beta");
        Console.WriteLine($"    title \"Live Memory During {format} Streaming\"");

        var xLabels = profiles[0].Snapshots.Select(s => s.DataProcessedKB.ToString("F0", inv)).ToList();
        Console.WriteLine($"    x-axis \"Data Processed (KB)\" [{string.Join(", ", xLabels)}]");

        double maxMem = profiles.Max(p => p.Snapshots.Max(s => s.LiveMemoryMB));
        Console.WriteLine($"    y-axis \"Live Memory (MB)\" 0 --> {Math.Max(10, Math.Ceiling(maxMem / 5) * 5)}");

        foreach (var p in profiles)
        {
            var yData = p.Snapshots.Select(s => s.LiveMemoryMB.ToString("F1", inv)).ToList();
            Console.WriteLine($"    line [{string.Join(", ", yData)}]");
        }

        Console.WriteLine("```");
        Console.WriteLine("\n**Legend:** Green = DataLinq.NET, Blue = System.Text.Json");
    }

    public static async Task RunJsonAndPrint()
    {
        Console.WriteLine("=== JSON Memory Profiler ===\n");
        var profiles = await ProfileJson(objectCount: 8_000_000, sampleEveryN: 80_000);
        PrintResults(profiles, "JSON");
    }

    public static async Task RunYamlAndPrint()
    {
        Console.WriteLine("=== YAML Memory Profiler ===\n");
        var profiles = await ProfileYaml(docCount: 8_000_000, sampleEveryN: 80_000);
        PrintResults(profiles, "YAML");
    }

    public static async Task<List<LibraryProfile>> ProfileYaml(int docCount = 20_000, int sampleEveryN = 2_000)
    {
        var root = Path.Combine(Path.GetTempPath(), "DataLinqBench", "yaml_memory");
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);

        var yamlPath = Path.Combine(root, "data.yaml");
        Console.WriteLine($"Generating {docCount:N0} YAML docs...");

        using (var writer = new StreamWriter(yamlPath))
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

        var fileSize = new FileInfo(yamlPath).Length;
        double fileSizeKB = fileSize / 1024.0;
        Console.WriteLine($"YAML file: {fileSizeKB:F0} KB\n");

        var profiles = new List<LibraryProfile>();

        // Profile DataLinq YAML
        profiles.Add(await ProfileDataLinqYaml(yamlPath, docCount, sampleEveryN));

        return profiles;
    }

    private static async Task<LibraryProfile> ProfileDataLinqYaml(string yamlPath, int docCount, int sampleEvery)
    {
        Console.WriteLine("Profiling DataLinq.NET YAML...");
        var snapshots = new List<Snapshot>();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long baseline = GC.GetTotalMemory(true);
        int count = 0;
        snapshots.Add(new Snapshot(0, 0));

        var fileSize = new FileInfo(yamlPath).Length;
        double bytesPerDoc = (double)fileSize / docCount;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var opts = new YamlReadOptions<Person> { RestrictTypes = true };

        await foreach (var _ in Read.Yaml<Person>(yamlPath, opts))
        {
            count++;
            if (count % sampleEvery == 0)
            {
                double dataKB = (count * bytesPerDoc) / 1024.0;
                double liveMB = Math.Max(0, GC.GetTotalMemory(false) - baseline) / 1024.0 / 1024.0;
                snapshots.Add(new Snapshot(dataKB, liveMB));
            }
        }
        sw.Stop();

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        double finalMB = Math.Max(0, GC.GetTotalMemory(true) - baseline) / 1024.0 / 1024.0;
        double totalKB = (count * bytesPerDoc) / 1024.0;
        snapshots.Add(new Snapshot(totalKB, finalMB));

        Console.WriteLine($"  Done in {sw.Elapsed.TotalSeconds:F1}s ({count:N0} docs)\n");
        return new LibraryProfile("DataLinq.NET", snapshots, sw.Elapsed.TotalSeconds);
    }

    public static async Task RunLeakCheck()
    {
        Console.WriteLine("=== Leak Check (Run 10x) ===\n");
        await CheckLeak("JSON", async () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "DataLinqBench", "json_memory");
            var path = Path.Combine(root, "data.json");
            // Ensure file exists (reuse existing setup logic or check)
            if (!File.Exists(path)) await ProfileJson(1000, 1000); // quick gen

            var opts = new JsonReadOptions<Person> { RequireArrayRoot = true };
            await foreach (var item in Read.Json<Person>(path, opts)) { }
        });

        await CheckLeak("YAML", async () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "DataLinqBench", "yaml_memory");
            var path = Path.Combine(root, "data.yaml");
            if (!File.Exists(path)) await ProfileYaml(1000, 1000); // quick gen

            var opts = new YamlReadOptions<Person> { RestrictTypes = true };
            await foreach (var item in Read.Yaml<Person>(path, opts)) { }
        });
    }

    private static async Task CheckLeak(string name, Func<Task> action)
    {
        Console.WriteLine($"Testing {name}...");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long baseline = GC.GetTotalMemory(true);

        for (int i = 1; i <= 10; i++)
        {
            await action();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long mem = GC.GetTotalMemory(true);
            double diffMB = (mem - baseline) / 1024.0 / 1024.0;
            Console.WriteLine($"  Run {i}: {diffMB:F2} MB delta");
        }
        Console.WriteLine("  Done.\n");
    }

    // Simple Person class for deserialization
    public class Person
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public DateTime Registered { get; set; }
        public bool IsActive { get; set; }
    }
}
