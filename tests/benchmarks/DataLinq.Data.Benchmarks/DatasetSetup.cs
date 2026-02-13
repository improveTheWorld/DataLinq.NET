using DataLinq.Data.Tests.Generators; // Reuse generator from tests (or duplicate if isolation needed)

namespace DataLinq.Data.Benchmarks;

public static class DatasetSetup
{

    private static DataSetGenerator.GeneratedFiles? _clean;
    private static DataSetGenerator.GeneratedFiles? _noisy;

    public static DataSetGenerator.GeneratedFiles Clean => _clean ?? throw new InvalidOperationException("Not initialized");
    public static DataSetGenerator.GeneratedFiles Noisy => _noisy ?? throw new InvalidOperationException("Not initialized");

    public static void Ensure(string root, bool includeNoisy = false)
    {
        if (_clean != null && (!includeNoisy || _noisy != null)) return;

        if (includeNoisy)
        {
            DataSetGenerator.EnsureVariants(root, out _clean!, out _noisy!);
        }
        else
        {
            var cfg = new DataGenConfig { InjectErrors = false };
            _clean = DataSetGenerator.EnsureGenerated(Path.Combine(root, "clean"), cfg, Console.WriteLine);
        }
        Report(root);
        SelfCheck();
    }

    private static void SelfCheck()
    {
        var opts = new CsvReadOptions
        {
            HasHeader = true,
            ErrorAction = ReaderErrorAction.Throw
        };

        // Force enumeration of a single record to ensure file is parseable.
        using var enumerator = Read.CsvSync<dynamic>(Clean.CsvPath, opts).GetEnumerator();
        if (!enumerator.MoveNext())
            throw new InvalidOperationException("SelfCheck: CSV dataset appears empty.");
    }


    private static void Report(string root)
    {
        Console.WriteLine("==== Benchmark Dataset Report ====");
        void dump(string label, string path)
        {
            var fi = new FileInfo(path);
            Console.WriteLine($"{label}: {path} size={fi.Length} bytes");
        }

        dump("CSV (clean)", Clean.CsvPath);
        if (_noisy != null) dump("CSV (noisy)", _noisy.CsvPath);
        dump("JSON (clean)", Clean.JsonArrayPath);
        if (_noisy != null) dump("JSON (noisy)", _noisy.JsonArrayPath);
    }
}
