# DataLinq.Spark v1.3.1 Release Notes

**Release Date**: May 2026

## Highlights

DataLinq.Spark v1.3.1 delivers the **F5 Experience** — press Run in your IDE and everything just works. The JVM backend now launches automatically in local mode. This release also fixes a critical `Sum()` casting bug and adds a missing `ForEachCase` overload.

---

## New Features

### F5 Experience — Zero-Setup Local Development

`Spark.Connect(SparkMaster.Local(), "MyApp")` now **automatically launches the JVM debug backend** if no backend is listening. No more opening a separate terminal, no more `spark-submit` commands:

```csharp
// Before v1.3.1: requires manual spark-submit in a separate terminal
// After v1.3.1: just press F5
using var context = Spark.Connect(SparkMaster.Local(), "MyApp");

var count = context.Read.Parquet<Order>("/data/orders").Count();
// JVM auto-stops on Dispose()
```

How it works:
- Detects if the JVM debug backend is running on port 5567
- If not, discovers `JAVA_HOME`, `SPARK_HOME`, and the Microsoft.Spark JAR from the NuGet cache
- Launches `spark-submit` with DotnetRunner in debug mode and polls until ready (30s timeout)
- Stops the JVM on `SparkContext.Dispose()` — **only** if DataLinq launched it (externally managed JVMs are never touched)

**Requirements**: `JAVA_HOME` and `SPARK_HOME` environment variables must be set. The Microsoft.Spark JAR is discovered automatically.

**Disable**: Set `AutoStartBackend = false` in connect options:

```csharp
using var context = Spark.Connect(SparkMaster.Local(), "MyApp", o => {
    o.AutoStartBackend = false;
});
```

> Auto-launch is skipped for remote clusters (Standalone, YARN, Kubernetes).

### ForEachCase 2-Tuple Overload

The documented pattern `.Cases(...).ForEachCase(...).UnCase()` now compiles **without** requiring an intermediate `SelectCase()` identity projection:

```csharp
// Now works directly — no need for .SelectCase(x => x, x => x) in between
query.Cases(o => o.Amount > 1000)
     .ForEachCase(vip => ProcessVip(vip), std => ProcessStd(std))
     .UnCase()
     .Do();
```

---

## Bug Fixes

### Sum() InvalidCastException (BUG-001)

`Sum()` was throwing `InvalidCastException` on every call. The method was hardcoded to return `decimal?` and deserialize via `GetAs<decimal>`, but Spark's JVM always returns `double` for SUM aggregations:

```csharp
// v1.3.0: ❌ InvalidCastException — double cannot be cast to decimal
// v1.3.1: ✅ Works correctly
var total = orders.Sum(o => o.Amount);  // Returns double?
```

Fix: return type changed to `double?`, matching `Average()` which was already correct.

---

## Quality

- **808+ tests** — zero failures across unit, integration, adversarial audit, and package audit suites
- All features verified across the full 15-stage release pipeline
- 17 new integration tests covering terminal aggregates (`Sum`, `Average`, `Max`, `Min`) and ForEachCase 2-tuple flows

---

## Documentation

- **[LINQ-to-Spark Guide](https://github.com/improveTheWorld/DataLinq.NET/blob/main/docs/LINQ-to-Spark.md)** — Updated with F5 Experience section, `AutoStartBackend` option, `SparkMaster` helpers
- **[Package README](https://www.nuget.org/packages/DataLinq.Spark)** — Updated Quick Start with auto-launch, environment variable checklist

---

## Compatibility

This is a **drop-in, non-breaking upgrade** for all `1.3.x` users. The only behavioral change is that `Spark.Connect()` in local mode now auto-launches the JVM — set `AutoStartBackend = false` to restore previous behavior.
