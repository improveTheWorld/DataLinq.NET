using DataLinq.Data;
using DataLinq.Data.Tests.Generators;
using System.Globalization;
using System.Text;

namespace DataLinq.Data.Benchmarks;

/// <summary>
/// Memory profiler for streaming benchmarks.
/// Collects fine-grained data, then smooths saw-tooth pattern for visualization.
/// </summary>
public static class MemoryProfiler
{
    public record Snapshot(double DataProcessedMB, double LiveMemoryMB);

    /// <summary>
    /// Profiles CSV streaming with fine-grained sampling.
    /// </summary>
    /// <param name="targetSizeMB">Target CSV file size in MB</param>
    /// <param name="samplesPerCycle">Samples to collect per expected GC cycle (~every 5MB processed)</param>
    public static async Task<(List<Snapshot> raw, List<Snapshot> smoothed, double fileSizeMB)> ProfileCsvStreaming(
        int targetSizeMB = 500,
        int samplesPerCycle = 10)
    {
        // Estimate rows needed for target size (~36 bytes/row based on 5 columns of numbers)
        int rowCount = (int)(targetSizeMB * 1024 * 1024 / 36.0);

        var root = Path.Combine(Path.GetTempPath(), "DataLinqBench", "profile500");

        // Delete old files to regenerate
        if (Directory.Exists(root))
            Directory.Delete(root, true);
        Directory.CreateDirectory(root);

        var cfg = new DataGenConfig
        {
            InjectErrors = false,
            CsvRows = rowCount,
            CsvColumns = 5
        };

        Console.WriteLine($"Generating dataset targeting {targetSizeMB} MB ({rowCount:N0} rows)...");
        var files = DataSetGenerator.EnsureGenerated(root, cfg, Console.WriteLine);

        var fileInfo = new FileInfo(files.CsvPath);
        long fileSizeBytes = fileInfo.Length;
        double fileSizeMB = fileSizeBytes / 1024.0 / 1024.0;
        Console.WriteLine($"CSV file size: {fileSizeMB:F1} MB");

        var opts = new CsvReadOptions
        {
            HasHeader = true,
            InferSchema = true,
            SchemaInferenceMode = SchemaInferenceMode.ColumnNamesAndTypes,
            QuoteMode = CsvQuoteMode.Lenient,
            ErrorAction = ReaderErrorAction.Skip,
            ErrorSink = NullErrorSink.Instance
        };

        var rawSnapshots = new List<Snapshot>();

        // Sample every N rows to get ~100-200 total samples
        int sampleEveryNRows = Math.Max(1000, rowCount / 200);
        double bytesPerRow = (double)fileSizeBytes / rowCount;

        // Force full GC before starting
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long initialMemory = GC.GetTotalMemory(true);
        int rowIndex = 0;

        rawSnapshots.Add(new Snapshot(0, 0));

        Console.WriteLine($"Streaming {rowCount:N0} rows, sampling every {sampleEveryNRows:N0}...");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        await foreach (var row in Read.Csv<dynamic>(files.CsvPath, opts))
        {
            rowIndex++;

            if (rowIndex % sampleEveryNRows == 0)
            {
                double dataProcessedMB = (rowIndex * bytesPerRow) / 1024.0 / 1024.0;
                long liveMemory = GC.GetTotalMemory(false) - initialMemory;
                double liveMB = Math.Max(0, liveMemory) / 1024.0 / 1024.0;
                rawSnapshots.Add(new Snapshot(dataProcessedMB, liveMB));
            }
        }

        sw.Stop();
        Console.WriteLine($"Streaming completed in {sw.Elapsed.TotalSeconds:F1}s ({fileSizeMB / sw.Elapsed.TotalSeconds:F1} MB/s)");

        // Final snapshot after GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long finalMemory = GC.GetTotalMemory(true) - initialMemory;
        double finalMB = Math.Max(0, finalMemory) / 1024.0 / 1024.0;
        rawSnapshots.Add(new Snapshot(fileSizeMB, finalMB));

        // Smooth the data: find middle value of each saw-tooth cycle
        var smoothed = SmoothSawTooth(rawSnapshots);

        return (rawSnapshots, smoothed, fileSizeMB);
    }

    /// <summary>
    /// Smooths saw-tooth pattern by finding the median value of each GC cycle.
    /// A cycle is detected by a significant memory drop (>50% decrease).
    /// </summary>
    public static List<Snapshot> SmoothSawTooth(List<Snapshot> raw)
    {
        if (raw.Count < 3) return raw;

        var smoothed = new List<Snapshot>();
        var currentCycle = new List<Snapshot>();
        double prevMem = 0;

        foreach (var s in raw)
        {
            // Detect cycle boundary: memory dropped significantly
            if (currentCycle.Count > 0 && s.LiveMemoryMB < prevMem * 0.5 && prevMem > 1)
            {
                // End of cycle - take median
                if (currentCycle.Count > 0)
                {
                    var median = GetMedian(currentCycle);
                    smoothed.Add(median);
                }
                currentCycle.Clear();
            }

            currentCycle.Add(s);
            prevMem = s.LiveMemoryMB;
        }

        // Add remaining cycle
        if (currentCycle.Count > 0)
        {
            var median = GetMedian(currentCycle);
            smoothed.Add(median);
        }

        return smoothed;
    }

    private static Snapshot GetMedian(List<Snapshot> cycle)
    {
        var sorted = cycle.OrderBy(s => s.LiveMemoryMB).ToList();
        var median = sorted[sorted.Count / 2];
        // Use middle data position and median memory
        var midData = cycle[cycle.Count / 2].DataProcessedMB;
        return new Snapshot(midData, median.LiveMemoryMB);
    }

    public static string GenerateMermaidChart(List<Snapshot> snapshots, double fileSizeMB, string label)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;

        // Sample for chart (max ~15 points for readability)
        var sampled = snapshots.Count <= 15
            ? snapshots
            : snapshots.Where((s, i) => i % (snapshots.Count / 15 + 1) == 0 || i == snapshots.Count - 1).ToList();

        double maxMem = Math.Max(10, Math.Ceiling(snapshots.Max(s => s.LiveMemoryMB) / 5) * 5);

        sb.AppendLine("```mermaid");
        sb.AppendLine("%%{init: {'theme': 'base', 'themeVariables': {'xyChart': {'plotColorPalette': '#22c55e'}}}}%%");
        sb.AppendLine("xychart-beta");
        sb.AppendLine($"    title \"{label} ({fileSizeMB:F0} MB CSV)\"");

        var xLabels = sampled.Select(s => s.DataProcessedMB.ToString("F0", inv)).ToList();
        sb.AppendLine($"    x-axis \"Data Processed (MB)\" [{string.Join(", ", xLabels)}]");
        sb.AppendLine($"    y-axis \"Live Memory (MB)\" 0 --> {maxMem:F0}");

        var yData = sampled.Select(s => s.LiveMemoryMB.ToString("F1", inv)).ToList();
        sb.AppendLine($"    line [{string.Join(", ", yData)}]");

        sb.AppendLine("```");
        return sb.ToString();
    }

    public static string GenerateMarkdownTable(List<Snapshot> snapshots, string header1, string header2)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"| {header1} | {header2} |");
        sb.AppendLine("|--------------------:|----------------:|");

        foreach (var s in snapshots)
        {
            sb.AppendLine($"| {s.DataProcessedMB:F0} | {s.LiveMemoryMB:F2} |");
        }
        return sb.ToString();
    }

    public static string GenerateCsv(List<Snapshot> snapshots)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        sb.AppendLine("DataProcessedMB,LiveMemoryMB");
        foreach (var s in snapshots)
        {
            sb.AppendLine($"{s.DataProcessedMB.ToString("F2", inv)},{s.LiveMemoryMB.ToString("F2", inv)}");
        }
        return sb.ToString();
    }

    public static async Task RunAndPrint()
    {
        const int TARGET_SIZE_MB = 1000;

        Console.WriteLine("=== DataLinq.NET Memory Streaming Benchmark ===\n");
        Console.WriteLine($"Target: {TARGET_SIZE_MB} MB CSV file\n");

        var (raw, smoothed, fileSizeMB) = await ProfileCsvStreaming(TARGET_SIZE_MB);

        Console.WriteLine($"\nCaptured {raw.Count} raw samples, smoothed to {smoothed.Count} points\n");

        Console.WriteLine("### Smoothed Data (Median per GC cycle)\n");
        Console.WriteLine(GenerateMarkdownTable(smoothed, "Data Processed (MB)", "Live Memory (MB)"));

        Console.WriteLine("\n### Smoothed Chart (abstracts GC fluctuations)\n");
        Console.WriteLine(GenerateMermaidChart(smoothed, fileSizeMB, "Streaming Memory (Smoothed)"));

        Console.WriteLine("\n### Raw Chart (shows GC saw-tooth pattern)\n");
        Console.WriteLine(GenerateMermaidChart(raw, fileSizeMB, "Streaming Memory (Raw)"));

        // Save CSV
        var csvPath = Path.Combine(Path.GetTempPath(), "memory_profile_raw.csv");
        File.WriteAllText(csvPath, GenerateCsv(raw));
        var csvSmoothedPath = Path.Combine(Path.GetTempPath(), "memory_profile_smoothed.csv");
        File.WriteAllText(csvSmoothedPath, GenerateCsv(smoothed));
        Console.WriteLine($"\nCSV data saved to:");
        Console.WriteLine($"  Raw: {csvPath}");
        Console.WriteLine($"  Smoothed: {csvSmoothedPath}");

        Console.WriteLine("\n### How to Reproduce\n");
        Console.WriteLine("```bash");
        Console.WriteLine("cd benchmarks");
        Console.WriteLine("dotnet run -c Release --project DataLinq.Data.Benchmarks -- --profile");
        Console.WriteLine("```");
    }
}
