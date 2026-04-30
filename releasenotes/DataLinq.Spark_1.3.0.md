# DataLinq.Spark v1.3.0 Release Notes

**Release Date**: April 2026

## Highlights

DataLinq.Spark v1.3.0 delivers **composite join key support**, **compile-time query safety**, **Where predicate delta synchronization**, **automatic memory management** for Cases pipelines, and a critical **pipeline stability fix** — plus a streamlined API surface with deprecated utilities removed.

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

Previously restricted to single-property keys, v1.3.0 lifts this limitation — any expression that works in `Where` or `Select` now works in `Join` key selectors, including anonymous types, ternary operators, and Auto-UDF methods.

### OrderedSparkQuery — Compile-Time Ordering Enforcement

`OrderBy` and `OrderByDescending` now return `OrderedSparkQuery<T>`, a distinct type that enforces correct API usage at compile time:

```csharp
// ✅ Compiles — Skip requires ordering
var page = query.OrderBy(o => o.Date).Skip(100).Take(50);

// ❌ Won't compile — Skip is not available on unordered SparkQuery<T>
var bad = query.Skip(100);  // CS1061: 'SparkQuery<T>' does not contain 'Skip'

// ✅ ThenBy chaining — only available after OrderBy
var sorted = query
    .OrderBy(o => o.Region)
    .ThenByDescending(o => o.Amount);
```

This prevents a category of runtime errors where Spark would silently return unpredictable results from unordered `Skip` operations.

### Where with Delta Reflection (Instance Field Sync in Predicates)

Instance methods used in `Where` predicates can now **write to fields and synchronize changes back** to the driver, using the same Delta Reflection Protocol as `ForEach`:

```csharp
var validator = new OrderValidator();
var valid = orders
    .Where(o => validator.IsValid(o.Amount))
    .ToList();

// validator.ProcessedCount and validator.TotalAmount are now synchronized!
```

Under the hood, a **composite UDF** returns both the boolean predicate result and field deltas in a single pass (`"1|field:+5"` or `"0|field:+3"`). A `postExecutionSync` callback collects deltas from all evaluated rows (both passed and failed) and applies them to the original instance.

### Automatic Memory Management for Cases Writes

All Cases terminal operations (`WriteTables`, `WriteParquets`, `WriteCsvs`, `MergeTables`, `ForEachCase`) now implement **Transparent Access Memory (TAM)** — the categorized DataFrame is automatically cached before multi-category writes and deterministically released afterward:

```csharp
// Before v1.3.0: each category re-scanned the full lineage (N+1 problem)
// After v1.3.0: single scan, cached, auto-released
await query.Cases(o => o.Amount > 1000)
    .SelectCase(vip => new { vip.Id }, std => new { std.Id })
    .WriteTables("VIP_ORDERS", "STD_ORDERS");
// ← Memory automatically freed here, even if an exception occurs
```

For pipelines with complex upstream transformations (joins, UDFs, multi-stage filters), this eliminates redundant re-computation and reduces executor memory pressure.

---

## Bug Fixes

### PostExecutionSync Forwarding (Critical Stability Fix)

Previously, 15 query chain methods silently **dropped** the `_postExecutionSync` callback when constructing new query instances. This meant that `ForEach` delta sync and `Where` delta sync would fail silently in chained pipelines:

```csharp
// Before v1.3.0: ForEach sync LOST after .Where() — fields never synced
query.ForEach(processor.Process).Where(o => o.Active).Do();

// After v1.3.0: sync correctly propagated through entire chain
query.ForEach(processor.Process).Where(o => o.Active).Do();  // ✅ Works
```

**Fixed in**: `Where`, `Select`, `Take`, `Distinct`, `DropDuplicates`, `Join`, `Union`, `Intersect`, `Except`, `Cache`, `Persist`, `Repartition`, `Coalesce`, `OrderBy`, `OrderByDescending`.

---

## API Changes

### Removed

| Method | Reason | Replacement |
|--------|--------|-------------|
| `Profile()` | Triggered query execution as a hidden side effect | Use `Explain()` for query plans, manual `Stopwatch` for timing |
| `Debug()` | Redundant with `Spy()` | Use `Spy("label")` for transparent pipeline inspection |
| `ShowComparison()` | Trivial utility with no discoverability | Call `Show()` on each query directly |

> These methods were undocumented internal utilities. If your code references them, the replacements above are direct substitutions.

---

## Quality

- **744 tests passing** (189 unit + 285 integration + 270 adversarial audit) — zero failures
- Composite join key tests cover: single-prop, multi-prop, computed, bool, DateTime, nested, mixed
- `OrderedSparkQuery<T>` contract verified at compile time (not just runtime)
- PostExecutionSync forwarding verified for all 15 chain methods
- TAM cache/unpersist lifecycle verified across all 5 Cases terminal methods

---

## Documentation

- **[LINQ-to-Spark Guide](https://github.com/improveTheWorld/DataLinq.NET/blob/main/docs/LINQ-to-Spark.md)** — Updated: Join examples with composite keys, OrderedSparkQuery section, Where Delta Reflection section, terminal reference table
- **[Cases Pattern](https://github.com/improveTheWorld/DataLinq.NET/blob/main/docs/Cases-Pattern.md)** — TAM memory management documented, chaining validity matrix updated

---

## Full Changelog

[`DataLinq.Spark_1.2.0...DataLinq.Spark_1.3.0`](https://github.com/improveTheWorld/DataLinq.NET/compare/DataLinq.Spark_1.2.0...DataLinq.Spark_1.3.0)
