using CsvHelper;
using DataLinq.Data;
using Sylvan.Data.Csv;
using System.Globalization;
using System.Text;

namespace DataLinq.Data.Benchmarks;

/// <summary>
/// Compares memory usage over time for DataLinq, CsvHelper, and Sylvan.
/// </summary>
public static class CompetitorMemoryProfiler
{
    public record Snapshot(double DataProcessedMB, double LiveMemoryMB);
    public record LibraryProfile(string Name, List<Snapshot> Snapshots, double TotalTimeSec);

    public static async Task<List<LibraryProfile>> ProfileAll(int rowCount = 1_000_000, int sampleEveryNRows = 50_000)
    {
        var root = Path.Combine(Path.GetTempPath(), "DataLinqBench", "memory_compare");
        if (Directory.Exists(root)) Directory.Delete(root, true);
        Directory.CreateDirectory(root);

        // Generate CSV
        var csvPath = Path.Combine(root, "data.csv");
        Console.WriteLine($"Generating {rowCount:N0} rows...");
        using (var writer = new StreamWriter(csvPath))
        {
            writer.WriteLine("Id,Name,Email,Registered,IsActive");
            var date = DateTime.Now;
            for (int i = 0; i < rowCount; i++)
                writer.WriteLine($"{i},User {i},user{i}@example.com,{date:O},{i % 2 == 0}");
        }

        var fileSize = new FileInfo(csvPath).Length;
        double fileSizeMB = fileSize / 1024.0 / 1024.0;
        double bytesPerRow = (double)fileSize / rowCount;
        Console.WriteLine($"CSV file: {fileSizeMB:F1} MB\n");

        var profiles = new List<LibraryProfile>();

        // Profile DataLinq
        profiles.Add(await ProfileDataLinq(csvPath, rowCount, sampleEveryNRows, bytesPerRow));

        // Profile CsvHelper
        profiles.Add(await ProfileCsvHelper(csvPath, rowCount, sampleEveryNRows, bytesPerRow));

        // Profile Sylvan
        profiles.Add(await ProfileSylvan(csvPath, rowCount, sampleEveryNRows, bytesPerRow));

        return profiles;
    }

    private static async Task<LibraryProfile> ProfileDataLinq(string csvPath, int rowCount, int sampleEvery, double bytesPerRow)
    {
        Console.WriteLine("Profiling DataLinq.NET...");
        var snapshots = new List<Snapshot>();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long baseline = GC.GetTotalMemory(true);
        int row = 0;
        snapshots.Add(new Snapshot(0, 0));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var opts = new CsvReadOptions { HasHeader = true, InferSchema = true };
        await foreach (var _ in Read.Csv<dynamic>(csvPath, opts))
        {
            row++;
            if (row % sampleEvery == 0)
            {
                double dataMB = (row * bytesPerRow) / 1024.0 / 1024.0;
                double liveMB = Math.Max(0, GC.GetTotalMemory(false) - baseline) / 1024.0 / 1024.0;
                snapshots.Add(new Snapshot(dataMB, liveMB));
            }
        }
        sw.Stop();

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        double finalMB = Math.Max(0, GC.GetTotalMemory(true) - baseline) / 1024.0 / 1024.0;
        double totalMB = (row * bytesPerRow) / 1024.0 / 1024.0;
        snapshots.Add(new Snapshot(totalMB, finalMB));

        Console.WriteLine($"  Done in {sw.Elapsed.TotalSeconds:F1}s\n");
        return new LibraryProfile("DataLinq.NET", snapshots, sw.Elapsed.TotalSeconds);
    }

    private static async Task<LibraryProfile> ProfileCsvHelper(string csvPath, int rowCount, int sampleEvery, double bytesPerRow)
    {
        Console.WriteLine("Profiling CsvHelper...");
        var snapshots = new List<Snapshot>();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long baseline = GC.GetTotalMemory(true);
        int row = 0;
        snapshots.Add(new Snapshot(0, 0));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        await foreach (var record in csv.GetRecordsAsync<dynamic>())
        {
            row++;
            if (row % sampleEvery == 0)
            {
                double dataMB = (row * bytesPerRow) / 1024.0 / 1024.0;
                double liveMB = Math.Max(0, GC.GetTotalMemory(false) - baseline) / 1024.0 / 1024.0;
                snapshots.Add(new Snapshot(dataMB, liveMB));
            }
        }
        sw.Stop();

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        double finalMB = Math.Max(0, GC.GetTotalMemory(true) - baseline) / 1024.0 / 1024.0;
        double totalMB = (row * bytesPerRow) / 1024.0 / 1024.0;
        snapshots.Add(new Snapshot(totalMB, finalMB));

        Console.WriteLine($"  Done in {sw.Elapsed.TotalSeconds:F1}s\n");
        return new LibraryProfile("CsvHelper", snapshots, sw.Elapsed.TotalSeconds);
    }

    private static async Task<LibraryProfile> ProfileSylvan(string csvPath, int rowCount, int sampleEvery, double bytesPerRow)
    {
        Console.WriteLine("Profiling Sylvan.Data.Csv...");
        var snapshots = new List<Snapshot>();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long baseline = GC.GetTotalMemory(true);
        int row = 0;
        snapshots.Add(new Snapshot(0, 0));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var csvReader = Sylvan.Data.Csv.CsvDataReader.Create(csvPath);

        while (await csvReader.ReadAsync())
        {
            row++;
            if (row % sampleEvery == 0)
            {
                double dataMB = (row * bytesPerRow) / 1024.0 / 1024.0;
                double liveMB = Math.Max(0, GC.GetTotalMemory(false) - baseline) / 1024.0 / 1024.0;
                snapshots.Add(new Snapshot(dataMB, liveMB));
            }
        }
        sw.Stop();

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        double finalMB = Math.Max(0, GC.GetTotalMemory(true) - baseline) / 1024.0 / 1024.0;
        double totalMB = (row * bytesPerRow) / 1024.0 / 1024.0;
        snapshots.Add(new Snapshot(totalMB, finalMB));

        Console.WriteLine($"  Done in {sw.Elapsed.TotalSeconds:F1}s\n");
        return new LibraryProfile("Sylvan", snapshots, sw.Elapsed.TotalSeconds);
    }

    public static void PrintResults(List<LibraryProfile> profiles)
    {
        var inv = CultureInfo.InvariantCulture;
        Console.WriteLine("=== Memory Comparison Results ===\n");

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
        Console.WriteLine("\n### Memory Over Time (All Libraries)\n");
        Console.WriteLine("```mermaid");
        Console.WriteLine("%%{init: {'theme': 'base', 'themeVariables': {'xyChart': {'plotColorPalette': '#22c55e, #f97316, #3b82f6'}}}}%%");
        Console.WriteLine("xychart-beta");
        Console.WriteLine("    title \"Live Memory During CSV Streaming (1M rows)\"");

        // X-axis from first profile
        var xLabels = profiles[0].Snapshots.Select(s => s.DataProcessedMB.ToString("F0", inv)).ToList();
        Console.WriteLine($"    x-axis \"Data Processed (MB)\" [{string.Join(", ", xLabels)}]");

        double maxMem = profiles.Max(p => p.Snapshots.Max(s => s.LiveMemoryMB));
        Console.WriteLine($"    y-axis \"Live Memory (MB)\" 0 --> {Math.Ceiling(maxMem / 10) * 10}");

        foreach (var p in profiles)
        {
            var yData = p.Snapshots.Select(s => s.LiveMemoryMB.ToString("F1", inv)).ToList();
            Console.WriteLine($"    line [{string.Join(", ", yData)}]");
        }

        Console.WriteLine("```");
        Console.WriteLine("\n**Legend:** Green = DataLinq.NET, Orange = CsvHelper, Blue = Sylvan");
    }

    public static async Task RunAndPrint()
    {
        Console.WriteLine("=== Competitor Memory Profiler ===\n");
        var profiles = await ProfileAll(rowCount: 1_000_000, sampleEveryNRows: 100_000);
        PrintResults(profiles);
    }
}
