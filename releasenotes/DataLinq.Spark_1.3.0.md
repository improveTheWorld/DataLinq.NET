# DataLinq.Spark v1.3.0 Release Notes

**Release Date**: May 2026

## Highlights

DataLinq.Spark v1.3.0 brings **richer LINQ joins**, **compile-time query safety**, **field synchronization in Where predicates**, **decimal support for ForEach sync**, **automatic memory management** for Cases pipelines, and improved **error visibility** — plus cleaner console output and a streamlined API surface.

---

## New Features

### Composite and Computed Join Keys

Joins now support **multi-property composite keys** and **computed expressions** in key selectors, matching full LINQ expressiveness:

```csharp
// Composite key — join on multiple columns
var result = orders
    .Join(customers,
        o => new { o.CustomerId, o.Region },
        c => new { c.Id, c.Region },
        (o, c) => new { o.OrderId, c.Name, c.Region })
    .ToList();

// Computed key — expressions in join selectors
var result = orders
    .Join(lookup,
        o => o.Amount > 1000 ? "VIP" : "Standard",
        l => l.Tier,
        (o, l) => new { o.OrderId, l.Discount })
    .ToList();
```

Any expression that works in `Where` or `Select` — anonymous types, ternary operators, Auto-UDF methods — now works in `Join` key selectors too.

### OrderedSparkQuery — Compile-Time Ordering Enforcement

`OrderBy` and `OrderByDescending` now return `OrderedSparkQuery<T>`, a distinct type that catches ordering mistakes at compile time:

```csharp
// ✅ Compiles — Skip requires ordering
var page = query.OrderBy(o => o.Date).Skip(100).Take(50);

// ❌ Won't compile — Skip is not available on unordered queries
var bad = query.Skip(100);  // CS1061: 'SparkQuery<T>' does not contain 'Skip'

// ✅ ThenBy chaining — only available after OrderBy
var sorted = query
    .OrderBy(o => o.Region)
    .ThenByDescending(o => o.Amount);
```

This prevents unpredictable results from unordered `Skip` operations.

### Where with Field Synchronization

Instance methods used in `Where` predicates can now **write to fields and synchronize changes back** to the driver — the same capability already available in `ForEach`:

```csharp
var validator = new OrderValidator();
var valid = orders
    .Where(o => validator.IsValid(o.Amount))
    .ToList();

// validator.ProcessedCount and validator.TotalAmount are now synchronized!
```

Field updates are collected from **all evaluated rows** (both matched and filtered), so your counters reflect the full dataset, not just the filtered results.

### Automatic Memory Management for Cases Writes

All Cases terminal operations (`WriteTables`, `WriteParquets`, `WriteCsvs`, `MergeTables`, `ForEachCase`) now **automatically cache and release** the categorized data:

```csharp
await query.Cases(o => o.Amount > 1000)
    .SelectCase(vip => new { vip.Id }, std => new { std.Id })
    .WriteTables("VIP_ORDERS", "STD_ORDERS");
// Memory automatically freed — even if an exception occurs
```

Previously, each category re-scanned the full upstream pipeline. v1.3.0 scans once and caches, eliminating redundant re-computation in complex pipelines.

### Decimal Field Synchronization

`ForEach`, `ForEachCase`, and `Where` now synchronize `decimal` fields. Previously, `decimal` mutations were silently lost:

```csharp
decimal totalRevenue = 0m;
query.ForEach(o => totalRevenue += o.Amount).Do();
Console.WriteLine(totalRevenue);  // ✅ Correct — was silently 0 in v1.2.0
```

**Supported sync types**: `int`, `long`, `double`, `float`, `decimal`, `string`

> **Tip**: For boolean-like tracking, use `int` with 0/1 values.

---

## Improvements

### Pipeline Stability

`ForEach` and `Where` field synchronization now works correctly through **all query chain methods**. Previously, chaining operations like `.Where()`, `.Select()`, or `.Join()` after `.ForEach()` could silently drop the sync — fields would stay at their initial values.

```csharp
// Now works correctly in all chain positions
query.ForEach(processor.Process).Where(o => o.Active).Do();
```

### Better Error Visibility

- **UDF errors** from Spark workers are now surfaced as exceptions instead of being silently swallowed
- **Sync errors** in Where predicates are now thrown as `InvalidOperationException` so your code can detect and handle them
- **Clean console output** — no more diagnostic trace messages in production

---

## API Changes

### Removed

| Method | Replacement |
|--------|-------------|
| `Profile()` | `Explain()` for query plans, `Stopwatch` for timing |
| `Debug()` | `Spy("label")` |
| `ShowComparison()` | Call `Show()` on each query directly |

> These were undocumented utilities. The replacements above are direct substitutions.

---

## Quality

- **791 tests** (208 unit + 313 integration + 270 adversarial audit) — zero failures
- All features verified across the full 15-stage release pipeline

---

## Documentation

- **[LINQ-to-Spark Guide](https://github.com/improveTheWorld/DataLinq.NET/blob/main/docs/LINQ-to-Spark.md)** — Updated with composite join examples, OrderedSparkQuery, Where field sync, decimal sync type list
- **[Cases Pattern](https://github.com/improveTheWorld/DataLinq.NET/blob/main/docs/Cases-Pattern.md)** — Updated with automatic memory management
