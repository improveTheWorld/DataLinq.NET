# DataLinq.Snowflake v1.3.0

**Release Date:** April 11, 2026
**Requires:** DataLinq.NET 1.0.0+

## 🚀 Cases Pattern Terminal Auto-Materialization (TAM)

The `Cases` pattern now natively supports **Terminal Auto-Materialization (TAM)** on Snowflake! 

When running categorical flows (like `Cases().SelectCase()`), DataLinq automatically caches the evaluated base query into session-scoped `TEMPORARY TABLE`. This completely eliminates N+1 re-evaluation penalties without producing deferred pipeline memory leaks.

```csharp
await context.Read.Table<Order>("ORDERS")
    .Cases(o => o.Amount > 10000, o => o.IsActive)
    .SelectCase(
        o => new LiteOrder { Id = o.Id, Total = o.Amount, Priority = "High" },
        o => new LiteOrder { Id = o.Id, Total = o.Amount, Priority = "Active" },
        o => new LiteOrder { Id = o.Id, Total = o.Amount, Priority = "Standard" }
    )
    .WriteTables(new[] { "PREMIUM", "RUSH", "STANDARD" });
// Caches the base 'ORDERS' stream in O(1) natively, preventing duplicated table scans!
```

## ✨ GroupBy Auto-UDF & Delta Reflection Isolation

We brought complete Auto-UDF capabilities to `GroupBy` projections! You can now use your custom static or instance methods directly inside your grouping keys—DataLinq will convert them to Snowflake functions on the fly.

Additionally, Delta Reflection internal bounds inside `ForEachCase` have been stabilized, creating completely robust static scope isolation.

## 🧪 Massive Test Suite Hardening

We expanded the package's adversarial test automation to guarantee behavioral stability:
- **Batch 50 Integrated**: Added rigorous stress-testing for deep `WithWindow` analytical bounds (e.g. `Ntile` and `Lag`).
- **`.Pull()` Hardening**: Client-side cancellation tokens via the native `.Pull()` stream have been fortified.
- The total execution suite has escalated to **789 explicit API assertions**. 

## 📖 Upgrading 

Always ensure that your primary OSS dependency ([DataLinq.NET](https://github.com/improveTheWorld/DataLinq.NET)) is running an equivalent up-to-date baseline version.
