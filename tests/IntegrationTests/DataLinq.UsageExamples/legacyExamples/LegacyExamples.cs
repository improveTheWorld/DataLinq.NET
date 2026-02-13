using DataLinq;
using DataLinq.Extensions;
// ARCHIVED: using static DataLinq.Framework.RegexWrap; // RegexTokenizer moved to archive

namespace App.UsageExamples.legacyExamples;

/// <summary>
/// Legacy examples from the original Program.cs - preserved for backwards compatibility
/// </summary>
public static class LegacyExamples
{
    struct Person
    {
        public string FirstName;
        public string Name;
        public int Age;
    }

    public static void CsvSimpleExample()
    {
        Console.WriteLine("=== Legacy: CSV Simple Example ===\n");

        // Inline CSV data - no external file needed (portable sample)
        var csvData = """
            FirstName,Name,Age
            John,Smith,42
            Jane,Doe,35
            Bob,Johnson,28
            Alice,Williams,31
            Charlie,Brown,55
            """;

        // Read lines from inline CSV, extract data, and fill into an Enumerable of Person
        csvData.AsCsv<Person>(",")
            .Take(5)
            // Convert names to uppercase
            .Select(p =>
            {
                p.Name = p.Name.ToUpper();
                return p;
            })
            .ToList()
            .ForEach(p => Console.WriteLine($"  {p.FirstName} {p.Name}, Age {p.Age}"));

        Console.WriteLine("\n✅ CSV processed (first 5 records shown)");
    }

    public static void TextAdvancedExample()
    {
        Console.WriteLine("=== Legacy: Text Advanced Example ===\n");

        // Inline log data - no external file needed (portable sample)
        var logLines = new List<string>
        {
            "Application started successfully",
            "WARNING: Low memory condition detected",
            "Processing request from client 127.0.0.1",
            "ERROR: Connection timeout on database",
            "Retrying connection...",
            "WARNING: Disk space running low",
            "ERROR: Failed to write to log file",
            "Shutting down gracefully",
            "INFO: Cleanup complete",
            "STOP: Application terminated"
        };

        // Read lines and categorize by log level
        var results = logLines
            .Until(line => line.StartsWith("STOP:"))
            .Cases(
                line => line.ToUpper().Contains("ERROR"),
                line => line.ToUpper().Contains("WARNING")
            )
            // Apply suitable transformations for each category,
            // => Add Log level information at the beginning of each line
            .SelectCase(
                line => $"ERROR : {line}",   // for lines containing "error" 
                line => $"WARNING : {line}", // for lines containing "warning"
                line => $"INFO : {line}"     // for other lines, assume Info
            )
            // Collect to show results
            .AllCases()
            .Take(10)
            .ToList();

        Console.WriteLine("Categorized log entries (first 10):");
        foreach (var line in results)
        {
            Console.WriteLine($"  {line}");
        }

        Console.WriteLine("\n✅ Text categorization complete");
    }

    // ARCHIVED: RgxsUsageExample() — depends on RegexTokenizer (moved to archive)
    // Original code preserved in archive/LegacyExamples_RegexTokenizer.cs
}
public static class InMemory
{
    public static StreamReader PutInStream(this IEnumerable<string> lines)
    {
        MemoryStream stream = new MemoryStream();

        var writer = new StreamWriter(stream);

        lines.ForEach(line => writer.WriteLine(line)).Do();

        writer.Flush(); // Ensures all data is written to the stream

        stream.Position = 0; // Reset the position to the beginning of the stream

        return new StreamReader(stream);

    }

}