# DataLinq.Snowflake v1.2.1

**Release Date:** March 24, 2026
**Requires:** DataLinq.NET 1.0.0+

## ✨ Compiler-Safe Merge Column Selection

`MergeTable` and `MergeTables` now accept an expression-based `updateOnly` parameter instead of raw column name strings. The compiler catches typos and rename regressions at build time — no more silent runtime mismatches.

**Before** *(string-based, no compile-time safety)*:
```csharp
await records.MergeTable(context, "SALES", r => r.Id,
    updateOnly: new[] { "amount", "status" });
```

**After** *(expression-based, compiler-enforced)*:
```csharp
await records.MergeTable(context, "SALES", r => r.Id,
    updateOnly: r => new { r.Amount, r.Status });

// Single-column shorthand:
await records.MergeTable(context, "SALES", r => r.Id,
    updateOnly: r => r.Status);
```

## ✨ Task-Returning Write API

All write operations (`WriteTable`, `MergeTable`, `WriteTables`, `MergeTables`) now return `Task<WriteResult>` or `Task<MergeResult>` directly. The previous builder pattern silently swallowed unawaited calls; the new API triggers CS4014 compiler warnings if you forget `await`.

```csharp
var result = await records.WriteTable(context, "ORDERS", overwrite: true);
Console.WriteLine($"Rows affected: {result.RowsAffected}");
```

## ⚠️ Breaking: `SaveMode` Replaced by Named Parameters

The `SaveMode` enum has been removed. Use named `bool` parameters instead:

| Before (v1.2.0) | After (v1.2.1) |
|---|---|
| `WriteTable(ctx, "T", SaveMode.Overwrite)` | `WriteTable(ctx, "T", overwrite: true)` |
| `WriteTable(ctx, "T", SaveMode.CreateIfMissing)` | `WriteTable(ctx, "T", createIfMissing: true)` |

Both flags can be combined: `WriteTable(ctx, "T", createIfMissing: true, overwrite: true)`.

## 🔧 Cleaner Dependency Surface

`Snowflake.Data` is now fully privatized. It no longer leaks as a transitive dependency to consumers — your project gains no unwanted Snowflake SDK types or DLLs.
