# DataLinq.Spark v1.2.0 Release Notes

**Release Date**: April 2026

## Highlights

DataLinq.Spark v1.2.0 delivers **full API harmony** with DataLinq.Snowflake, eliminates stale simulation patterns, and introduces native distributed execution for advanced features.

---

## Breaking Changes

### `ForEachCase` Signature Change (Cases Pattern)
`ForEachCase` now takes **per-row `Action<R>[]`** (Delta Reflection Protocol) instead of the previous `Action<SparkQuery<R>>[]` (bulk-query handles).

**Before (v1.1.0):**
```csharp
.ForEachCase(
    vipQuery => vipQuery.Count(),     // ← receives SparkQuery<R>
    stdQuery => stdQuery.Count()
)
```

**After (v1.2.0):**
```csharp
.ForEachCase(
    vip => highCount++,               // ← receives R (per-row)
    std => lowCount++
)
```

**Migration:** Move bulk write operations to the new `WriteTables()`, `WriteParquets()`, or `WriteCsvs()` methods. Use `ForEachCase` exclusively for row-level side-effects (counting, logging) that benefit from Delta Reflection sync-back.

---

## New Features

### Categorized Bulk Write Methods
New dedicated methods for routing categories to separate output targets:

```csharp
// Write each case to its own table (with overwrite support)
await query.Cases(o => o.Amount > 1000)
    .SelectCase(vip => new { vip.Id }, std => new { std.Id })
    .WriteTables("VIP_ORDERS", "STD_ORDERS", overwrite: true);

// Also available:
.WriteParquets(overwrite: true, "/output/vip", "/output/std")
.WriteCsvs(overwrite: true, header: true, "/output/vip.csv", "/output/std.csv")
.MergeTables(o => o.OrderId, "VIP_ORDERS", "STD_ORDERS")
```

### GroupBy Auto-UDF Support
GroupBy key selectors now support **full computed expressions and Auto-UDFs**, matching client-side LINQ behavior:

```csharp
// Computed keys — just works!
orders.GroupBy(o => o.Amount > 1000 ? "High" : "Low")
      .Select(g => new { Band = g.Key, Count = g.Count() });

// Math expressions
orders.GroupBy(o => Math.Round(o.Amount / 1000.0))
      .Select(g => new { Bucket = g.Key, Total = g.Sum(o => o.Amount) });

// Auto-UDF in key selector
orders.GroupBy(o => MyHelpers.Categorize(o.Amount))
      .Select(g => new { Category = g.Key, Count = g.Count() });
```

Previously, GroupBy keys were restricted to direct property access. v1.2.0 lifts this restriction — any expression that works in `Where` or `Select` now works in `GroupBy` key selectors, including ternary operators, `Math` functions, and your own static methods.

### ForEachCase via Delta Reflection Protocol
`ForEachCase(Action<R>[])` now executes per-row side-effects on Spark workers with automatic primitive field sync-back to the driver — the same Delta Reflection Protocol used by `ForEach`.

---

## Bug Fixes

- **Static field sync in ForEachCase**: Completed the Delta Reflection Protocol for `ForEachCase` when the C# compiler generates a `<>c` singleton closure (lambdas with no local captures, only static field writes). Static counters like `CaseStats.VipCount++` now correctly sync back to the driver after distributed execution.
- **DisplayClass isolation**: Eliminated non-serializable objects (`DataFrame`, `SparkSession`) from leaking into the UDF closure's compiler-generated `DisplayClass`, preventing `MessagePackSerializationException` at runtime.
- **PostExecutionSync propagation**: `AllCases()` and `UnCase()` now correctly propagate the sync-back callback, ensuring `ForEachCase` deltas are applied when chaining through the full Cases pipeline.
- **Math.Max/Min**: Correct translation to Spark SQL `greatest()`/`least()` functions.
- **SingleColumnMapper**: Fixed materialization of single-column projections from `SelectCase`.
- **Spy diagnostic**: Restored `Spy()` chaining to work correctly in all pipeline positions.

---

## Documentation

- **[LINQ-to-Spark Guide](https://github.com/improveTheWorld/DataLinq.NET/blob/main/docs/LINQ-to-Spark.md)** — Updated Cases pattern examples, GroupBy best practices
- **[Platform-Specific Divergences](https://github.com/improveTheWorld/DataLinq.NET/blob/main/docs/Platform-Specific-Divergences.md)** — Updated with v1.2.0 unification status
