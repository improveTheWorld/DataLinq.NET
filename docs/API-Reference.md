# DataLinq.NET — API Reference

> **Version:** 1.1  
> **Last Updated:** March 2026  
> **Namespace:** `DataLinq`

This document covers the **public API surface** of DataLinq.NET. For the full extension method matrix across all four paradigms (sync, async, parallel-sync, parallel-async), see the [Extension Methods API Reference →](Extension-Methods-API-Reference.md).

---

## Naming Convention

DataLinq uses an **inverted suffix convention** to encourage async-first development:

| Convention | Suffix | Returns | Example |
|-----------|--------|---------|---------|
| **Async (default)** | *(none)* | `IAsyncEnumerable<T>` | `Read.Csv<T>(path)` |
| **Synchronous** | `Sync` | `IEnumerable<T>` | `Read.CsvSync<T>(path)` |

> [!TIP]
> This is the opposite of the BCL convention (`Method` → sync, `MethodAsync` → async). DataLinq makes async the path of least resistance.

---

## 1. Read Class — Data Ingestion

Static class providing lazy, streaming data readers. All methods accept both file paths and `Stream` inputs.

### CSV

| Method | Returns | Description |
|--------|---------|-------------|
| `Read.Csv<T>(path, separator?, onError?, schema?)` | `IAsyncEnumerable<T>` | Simple async CSV reader |
| `Read.Csv<T>(path, CsvReadOptions, ct?)` | `IAsyncEnumerable<T>` | Options-based async CSV reader |
| `Read.Csv<T>(stream, ...)` | `IAsyncEnumerable<T>` | Stream overloads (same signatures) |
| `Read.CsvSync<T>(...)` | `IEnumerable<T>` | Synchronous equivalents |
| `Read.AsCsvSync<T>(string, ...)` | `IEnumerable<T>` | Parse CSV from an **in-memory string** |

```csharp
// Simple — async (default)
await foreach (var order in Read.Csv<Order>("orders.csv"))
    Console.WriteLine(order.Id);

// Simple — sync
foreach (var order in Read.CsvSync<Order>("orders.csv"))
    Console.WriteLine(order.Id);

// Options-based (full control)
var options = new CsvReadOptions
{
    Separator = ";",
    HasHeader = true,
    AllowMissingTrailingFields = true,
    ErrorAction = ReaderErrorAction.Skip
};
await foreach (var order in Read.Csv<Order>("orders.csv", options))
    Console.WriteLine(order.Id);
```

→ Deep dive: [DataLinq-Data-Reading-Infrastructure.md](DataLinq-Data-Reading-Infrastructure.md)

---

### JSON

| Method | Returns | Description |
|--------|---------|-------------|
| `Read.Json<T>(path, JsonSerializerOptions?, onError?, ct?)` | `IAsyncEnumerable<T>` | Simple async JSON reader |
| `Read.Json<T>(path, JsonReadOptions<T>, ct?)` | `IAsyncEnumerable<T>` | Options-based async JSON reader |
| `Read.Json<T>(stream, ...)` | `IAsyncEnumerable<T>` | Stream overloads |
| `Read.JsonSync<T>(...)` | `IEnumerable<T>` | Synchronous equivalents |

```csharp
// Streams JSON arrays lazily — O(1) memory
await foreach (var order in Read.Json<Order>("orders.json"))
    Console.WriteLine(order.Amount);

// With validation and guard rails
var opts = new JsonReadOptions<Order>
{
    MaxDepth = 32,
    MaxElements = 10_000,
    ValidateElements = true,
    ElementValidator = el => el.GetProperty("amount").GetDecimal() > 0
};
await foreach (var order in Read.Json<Order>("orders.json", opts))
    Process(order);
```

---

### YAML

| Method | Returns | Description |
|--------|---------|-------------|
| `Read.Yaml<T>(path, onError?, ct?)` | `IAsyncEnumerable<T>` | Simple async YAML reader |
| `Read.Yaml<T>(path, YamlReadOptions<T>, ct?)` | `IAsyncEnumerable<T>` | Options-based async YAML reader |
| `Read.Yaml<T>(stream, ...)` | `IAsyncEnumerable<T>` | Stream overloads |
| `Read.YamlSync<T>(...)` | `IEnumerable<T>` | Synchronous equivalents |

```csharp
// Multi-document YAML (--- separated)
await foreach (var config in Read.Yaml<AppConfig>("configs.yaml"))
    Apply(config);
```

---

### Text (Line-by-Line)

| Method | Returns | Description |
|--------|---------|-------------|
| `Read.Text(path, ct?)` | `IAsyncEnumerable<string>` | Async line reader |
| `Read.Text(path, TextReadOptions, ct?)` | `IAsyncEnumerable<string>` | Options-based (encoding, buffer size) |
| `Read.Text(stream, ...)` | `IAsyncEnumerable<string>` | Stream overloads |
| `Read.TextSync(...)` | `IEnumerable<string>` | Synchronous equivalents |

```csharp
await foreach (var line in Read.Text("server.log"))
    if (line.Contains("ERROR")) Console.WriteLine(line);
```

---

### Excel

| Method | Returns | Description |
|--------|---------|-------------|
| `Read.ExcelSheet<T>(path, sheetName, csvOpts?, excelOpts?, ct?)` | `IAsyncEnumerable<T>` | Read sheet by name |
| `Read.ExcelSheet<T>(path, sheetIndex, csvOpts?, excelOpts?, ct?)` | `IAsyncEnumerable<T>` | Read sheet by index |
| `Read.ExcelSheetSync<T>(...)` | `IEnumerable<T>` | Synchronous equivalents |
| `Read.ExcelToTempCsv(path, options?)` | `string[]` | Convert all sheets to temp CSV files |

```csharp
// Read specific sheet
await foreach (var row in Read.ExcelSheet<SalesData>("report.xlsx", "Q1 Sales"))
    Process(row);
```

---

## 2. Write Extensions — Data Output

Extension methods for writing `IEnumerable<T>` to various formats.

| Method | Description |
|--------|-------------|
| `WriteText(path, autoFlush?)` | Write string lines to file |
| `WriteText(StreamWriter)` | Write string lines to stream |
| `WriteCsv<T>(path, header?, separator?)` | Write objects to CSV file |
| `WriteCsv<T>(StreamWriter, header?, separator?)` | Write objects to CSV stream |

```csharp
// Write results to CSV
orders.Where(o => o.Amount > 1000)
    .Select(o => new { o.Id, o.Region, o.Amount })
    .WriteCsv("high_value_orders.csv");

// Pipeline: read → transform → write
Read.CsvSync<Order>("input.csv")
    .Where(o => o.Status == "Active")
    .ForEach(o => Console.WriteLine($"Processing {o.Id}"))
    .WriteCsv("active_orders.csv");
```

→ Deep dive: [DataLinq-Data-Writing-Infrastructure.md](DataLinq-Data-Writing-Infrastructure.md)

---

## 3. IEnumerable\<T\> Extensions — Quick Reference

All methods are lazy (deferred) unless marked as terminal.

| Method | Description | Returns |
|--------|-------------|---------|
| **Control Flow** | | |
| `Until(condition)` | Process until condition (**inclusive**) | `IEnumerable<T>` |
| `ForEach(action)` | Execute action per item (lazy) | `IEnumerable<T>` |
| `Do()` | Force enumeration (**terminal**) | `void` |
| `Do(action)` | Force enumeration with action (**terminal**) | `void` |
| **Cases Pattern** | | |
| `Cases(predicates[])` | Categorize items | `IEnumerable<(int, T)>` |
| `SelectCase(selectors[])` | Transform per category | `IEnumerable<(int, T, R)>` |
| `ForEachCase(actions[])` | Side-effect per category | `IEnumerable<(int, T, R)>` |
| `AllCases()` | Extract transformed items | `IEnumerable<R>` |
| **Aggregation** | | |
| `Cumul(function)` | Cumulative reduce | `T` |
| `Sum()` | Generic sum (dynamic) | `dynamic` |
| **Utilities** | | |
| `MergeOrdered(other, comparer)` | Merge two sorted sequences | `IEnumerable<T>` |
| `Take(start, count)` | Slice by range | `IEnumerable<T>` |
| `IsNullOrEmpty()` | Null/empty check | `bool` |
| `Flatten<T>()` | Flatten nested enumerables | `IEnumerable<T>` |
| **Debugging** | | |
| `Spy(tag)` | Log items to console (lazy) | `IEnumerable<T>` |
| `Spy<T>(tag, formatter)` | Log with custom `Func<T, string>` | `IEnumerable<T>` |
| `Display(tag)` | Output to console | `IEnumerable<T>` |
| **String Building** | | |
| `BuildString(separator, before, after)` | Build formatted string | `string` |

→ Full 4-paradigm matrix: [Extension-Methods-API-Reference.md](Extension-Methods-API-Reference.md)

---

## 4. String Extensions

Extension methods on `string` in the `DataLinq` namespace.

| Method | Description | Returns |
|--------|-------------|---------|
| `IsNullOrEmpty()` | Check if null or empty | `bool` |
| `IsNullOrWhiteSpace()` | Check if null/whitespace | `bool` |
| `IsBetween(start, end)` | Check start/end delimiters | `bool` |
| `StartsWith(prefixes)` | Check multiple prefixes | `bool` |
| `ContainsAny(tokens)` | Check for any token | `bool` |
| `ReplaceAt(index, length, text)` | Replace at position | `string` |
| `LastIdx()` | Get last valid index | `int` |

---

## 5. Async & Parallel Extensions

DataLinq provides the same extension API surface across four execution paradigms:

| Paradigm | Target Type | Namespace | Use Case |
|----------|-------------|-----------|----------|
| Sync Sequential | `IEnumerable<T>` | `DataLinq` | Default for in-memory |
| Async Sequential | `IAsyncEnumerable<T>` | `DataLinq` | I/O-bound streaming |
| Sync Parallel | `ParallelQuery<T>` (PLINQ) | `DataLinq.Parallel` | CPU-bound parallelism |
| Async Parallel | `ParallelAsyncQuery<T>` | `DataLinq.Parallel` | I/O-bound parallelism |

→ Full matrix: [Extension-Methods-API-Reference.md](Extension-Methods-API-Reference.md)  
→ ParallelAsyncQuery deep dive: [ParallelAsyncQuery-API-Reference.md](ParallelAsyncQuery-API-Reference.md)

---

## See Also

- [DataLinq-Data-Reading-Infrastructure.md](DataLinq-Data-Reading-Infrastructure.md) — CSV/JSON/YAML deep dive (1375 lines)
- [DataLinq-Data-Writing-Infrastructure.md](DataLinq-Data-Writing-Infrastructure.md) — Write operations
- [Extension-Methods-API-Reference.md](Extension-Methods-API-Reference.md) — Full extension method matrix
- [Cases-Pattern.md](Cases-Pattern.md) — Conditional routing pattern
- [DataLinq-SUPRA-Pattern.md](DataLinq-SUPRA-Pattern.md) — Streaming pipeline pattern
- [ObjectMaterializer.md](ObjectMaterializer.md) — Object materialization engine
- [LINQ-to-Spark.md](LINQ-to-Spark.md) — Apache Spark integration
- [LINQ-to-Snowflake.md](LINQ-to-Snowflake.md) — Snowflake Data Cloud integration
