# DataLinq.Spark v1.1.0

**Release Date:** March 26, 2026
**Requires:** DataLinq.NET 1.0.0+

## ✨ Task-Returning Write API

All write operations (`WriteTable`, `MergeTable`, `WriteParquet`, `WriteCsv`, `WriteJson`) now return `Task` directly. The previous builder pattern silently swallowed unawaited calls; the new API triggers CS4014 compiler warnings if you forget `await`.

```csharp
await records.WriteTable(context, "ORDERS", overwrite: true);
```

## ✨ Compiler-Safe Merge Column Selection

`MergeTable` now accepts an expression-based `updateOnly` parameter instead of raw column name strings.

```csharp
await records.MergeTable(context, "SALES", r => r.Id,
    updateOnly: r => new { r.Amount, r.Status });
```

## ⚠️ Breaking: `SaveMode` Replaced by Named Parameters

The `SaveMode` enum has been removed. Use named `bool` parameters instead:

| Before (v1.0.0) | After (v1.1.0) |
|---|---|
| `WriteTable(ctx, "T", mode: SaveMode.Overwrite)` | `WriteTable(ctx, "T", overwrite: true)` |
| `WriteTable(ctx, "T", mode: SaveMode.ErrorIfExists)` | `WriteTable(ctx, "T")` *(default)* |
