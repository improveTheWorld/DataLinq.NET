# DataLinq.NET — Architecture & APIs

## Table of Contents

1. [Project Structure](#1-project-structure)
2. [Architecture](#2-architecture)
3. [Core APIs](#3-core-apis)
4. [Advanced Topics](#4-advanced-topics)
5. [Best Practices](#5-best-practices)
6. [Going Deeper](#6-going-deeper)

---

## 1. Project Structure

The solution is organized into **solution folders** that group related projects. Each project is a minimal, single-responsibility unit to keep dependencies lean.

### Solution Folder Organization

```
DataLinq.NET.sln
│
├── 📁 DataLinq.App/                    ← Demo & Test Applications
│   ├── DataLinq.App.UsageExamples      ★ Start here - documentation examples
│   ├── DataLinq.App.Tools
│   ├── DataLinq.App.DataLinqTest
│   ├── DataLinq.App.EnumerableExtensionsTest
│   └── ...
│
├── 📁 DataLinq.Data/                   ← Data I/O Layer
│   ├── DataLinq.Data.Read              CSV, JSON, YAML, Text readers
│   └── DataLinq.Data.Write             Writers for all formats
│
├── 📁 DataLinq.Extensions/             ← Extension Methods
│   ├── DataLinq.Extensions.EnumerableExtensions     Cases, Until, ForEach
│   ├── DataLinq.Extensions.AsyncEnumerableExtensions Async variants
│   ├── DataLinq.Extensions.StringExtensions
│   ├── DataLinq.Extensions.ArrayExtensions
│   ├── DataLinq.Extensions.DictionaryExtensions
│   ├── DataLinq.Extensions.RegexTokenizerExtensions
│   ├── DataLinq.Extensions.SparkQueryExtensions
│   └── ...
│
├── 📁 DataLinq.Framework/              ← Core Infrastructure
│   ├── DataLinq.Framework.DataLinq     Pub/sub, channels
│   ├── DataLinq.Framework.Guard        Defensive programming
│   ├── DataLinq.Framework.RegexTokenizer
│   ├── DataLinq.Framework.SparkQuery   LINQ-to-Spark translation
│   ├── DataLinq.Framework.Syntaxi      Grammar parsing
│   ├── DataLinq.Framework.UnixStyleArgs
│   └── ...
│
├── 📁 DataLinq.Logger/                 ← Logging Infrastructure
│   ├── DataLinq.Logger
│   └── DataLinq.Logger.Test.UsageExamples
│
└── 📁 UnitTests/                       ← Automated Tests
    ├── DataLinq.Core.Tests             Core extensions, Cases, Materialization
    ├── DataLinq.Data.Tests             Read layer tests
    ├── DataLinq.Data.Write.Tests       Write layer tests
    └── DataLinq.SparkQuery.Tests       Spark integration tests
```

### Design Rationale

- **Single Responsibility**: Each project contains one focused capability
- **Minimal Dependencies**: Reference only what you need
- **Easy Compilation**: Smaller dependency graphs = faster builds
- **Future NuGet Packaging**: Each project can become an independent package

---

## 2. Architecture

The DataLinq.NET framework follows a **four-layer architecture**:

### 2.1 DataLinq (Core Namespace)

**Unified Data Access & Core Extensions**

- File I/O operations (Read, Write) with async support
- Data format handling (CSV, Text, JSON, YAML)
- **Cases/SelectCase/ForEachCase pattern** for both sync and async
- Dual `IEnumerable`/`IAsyncEnumerable` extensions

### 2.2 DataLinq.Extensions

**Internal Utilities**

- String processing and file system utilities
- Helper extensions not meant for the global scope

### 2.3 DataLinq.Framework

**Stream Processing Infrastructure Layer**

- **UnifiedStream<T>** for multi-source stream union/merging
- Channel-based async communication
- Regular expression utilities

### 2.4 DataLinq.SparkQuery

**Distributed Processing Layer** (Optional - requires Apache Spark)

- LINQ-to-Spark translation for petabyte-scale processing
- Expression tree translation to Spark Column operations
- Window functions, aggregations, and distributed joins

---

## 3. Core APIs

### 3.1 Read Class — Unified Data Reading

*Default method names are **ASYNCHRONOUS**. Synchronous variants use the `Sync` suffix.*

```csharp
// Text file reading
IAsyncEnumerable<string> lines = Read.Text("file.txt");
IEnumerable<string> linesSync = Read.TextSync("file.txt");

// CSV reading with type mapping
IAsyncEnumerable<T> records = Read.Csv<T>("data.csv");
IEnumerable<T> recordsSync = Read.CsvSync<T>("data.csv", separator: ";");

// JSON/YAML reading
IAsyncEnumerable<T> items = Read.Json<T>("data.json");
IAsyncEnumerable<T> docs = Read.Yaml<T>("config.yaml");
```

> [!NOTE]
> For advanced configuration, error handling, and format-specific options, see [Data Reading Infrastructure](DataLinq-Data-Reading-Infrastructure.md).

### 3.2 Writers — Unified Data Writing

```csharp
// All writers support both IEnumerable and IAsyncEnumerable
await records.WriteCsv("output.csv");
await records.WriteJson("output.json");
await records.WriteYaml("output.yaml");
await lines.WriteText("output.txt");

// Synchronous versions
records.WriteCsvSync("output.csv");
```

### 3.3 Cases/SelectCase Pattern

The core processing pattern for categorized data transformation:

```csharp
data
    .Cases(predicate1, predicate2, ...)      // Categorize items
    .SelectCase(transform1, transform2, ...) // Transform per category
    .ForEachCase(action1, action2, ...)      // Side-effects per category
    .AllCases()                              // Extract results
```

> [!NOTE]
> For full pattern documentation, see [Cases Pattern](Cases-Pattern.md).

### 3.4 UnifiedStream

Merge multiple async sources into a single stream:

```csharp
var merger = new UnifiedStream<T>(new UnifyOptions
{
    ErrorMode = UnifyErrorMode.ContinueOnError,
    Fairness = UnifyFairness.RoundRobin
})
.Unify(source1, "name1")
.Unify(source2, "name2", predicate: item => item.IsValid);
```

> [!NOTE]
> For full streaming documentation, see [Stream Merging](Stream-Merging.md).

---

## 4. Advanced Topics

### 4.1 Lazy Evaluation

All pipelines use lazy evaluation—nothing executes until enumeration:

```csharp
// This doesn't execute yet
var pipeline = data.Cases(...).SelectCase(...).AllCases();

// Execution happens here
var results = await pipeline.ToList();

// Or streaming execution
await foreach (var item in pipeline) { }
```

### 4.2 Buffering Extensions

```csharp
// Convert sync to async with yielding
var asyncData = syncData.Async(yieldThresholdMs: 15);

// Add bounded buffer for backpressure
var buffered = source.WithBoundedBuffer(capacity: 500);
```

### 4.3 Utility Extensions

```csharp
// Conditional processing
data.Until(item => item.IsLast)   // Stop when condition met

// Debugging
data.Spy(item => Console.WriteLine(item))  // Peek at items

// Aggregation
data.Cumul((a, b) => a + b)  // Running aggregation
```

---

## 5. Best Practices

### 5.1 Reader Configuration

Configure error handling at the boundary:

```csharp
var options = new CsvReadOptions {
    ErrorAction = ReaderErrorAction.Skip,
    ErrorSink = new JsonLinesFileErrorSink("errors.ndjson"),
    Progress = new Progress<ReaderProgress>(p => Console.WriteLine($"{p.RecordsRead} rows"))
};

await Read.Csv<Order>("orders.csv", options)
    .Select(EnrichOrder)
    .Cases(...)
    .AllCases();
```

### 5.2 Testing Pipelines

```csharp
[Test]
public async Task TestProcessingLogic()
{
    var testData = new[] {
        new Order { Id = 1, Amount = 1500 },
        new Order { Id = 2, Amount = 500 }
    }.ToAsyncEnumerable();

    var results = await testData
        .Cases(o => o.Amount > 1000)
        .SelectCase(
            high => new { o.Id, Category = "High" },
            standard => new { o.Id, Category = "Standard" }
        )
        .AllCases()
        .ToList();

    Assert.AreEqual("High", results.First(r => r.Id == 1).Category);
}
```

---

## 6. Going Deeper

| Document | Description |
|----------|-------------|
| [Cases Pattern](Cases-Pattern.md) | Cases/SelectCase pattern, Supra Category |
| [Stream Merging](Stream-Merging.md) | UnifiedStream, multi-source streams |
| [Data Reading Infrastructure](DataLinq-Data-Reading-Infrastructure.md) | CSV, JSON, YAML readers with full options |
| [LINQ-to-Spark](LINQ-to-Spark.md) | Distributed processing with SparkQuery |
| [Roadmap](Roadmap.md) | Future enhancements and versions |

*For API references, see: [DataLinq.Data](DataLinq-Data-Layer.md) · [DataLinq.Extensions](DataLinq-Extensions-Layer.md) · [DataLinq.Framework](DataLinq-Framework-Layer.md)*

---

*DataLinq.NET — Unified Data Processing for Batch and Streaming*
