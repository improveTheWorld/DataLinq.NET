# Cases Pattern

> **This document covers the DataLinq.NET Cases pattern — its philosophy, lazy execution contract, operator chaining rules, and API reference.**

---

## Table of Contents

1. [The Philosophy — What the Cases Pattern Is For](#1-the-philosophy--what-the-cases-pattern-is-for)
2. [The Lazy Execution Contract](#2-the-lazy-execution-contract)
3. [Operator Chaining — All Valid Orderings](#3-operator-chaining--all-valid-orderings)
4. [The Exit Gate — `AllCases()` and `UnCase()`](#4-the-exit-gate--allcases-and-uncase)
5. [The Supra Category](#5-the-supra-category)
6. [Complete Examples](#6-complete-examples)
7. [Multi-Type Branching](#7-multi-type-branching)
8. [Write Once, Process Anywhere](#8-write-once-process-anywhere)
9. [API Reference](#9-api-reference)

---

## 1. The Philosophy — What the Cases Pattern Is For

The Cases pattern answers a single question:

> **"How do I route each item to a different processing branch, without loading everything into memory first?"**

The traditional answer is `if/else` or `switch` inside a `foreach`. The problem: you break the pipeline. The data is already in memory. The decisions are tangled with the processing. Testing any single branch requires running all of them.

DataLinq's answer is to treat **routing as a first-class lazy operation**:

```csharp
// ✅ The Cases philosophy: declare all branches upfront, execute nothing yet
source
    .Cases(
        item => item.Type == "A",   // Route 0
        item => item.Type == "B"    // Route 1
        // Everything else → supra (Route 2)
    )
    .SelectCase(
        a => Transform_A(a),        // Only runs for Route 0 items
        b => Transform_B(b)         // Only runs for Route 1 items
    )
    .ForEachCase(
        a => Accumulate_A(a),       // Side-effect: Route 0
        b => Accumulate_B(b)        // Side-effect: Route 1
    )
    .AllCases()                     // Exit the Cases context → back to general query
    .Do();                          // TERMINAL: trigger execution — nothing ran before this line
```

**Key principles:**
- Every step above `AllCases()`/`UnCase()` is lazy — it declares *intent*, not execution
- Items flow **one at a time**, never buffered
- `AllCases()`/`UnCase()` is the **exit gate** back to the general query type
- `.Do()` is the **default terminal** — "execute this, I need no return value"

---

## 2. The Lazy Execution Contract

Every operator in the Cases pipeline is a **lazy transformation**. Nothing executes until a **terminal action** is called.

```
Cases()         → LAZY  (returns categorized query)
SelectCase()    → LAZY  (returns transformed query)
ForEachCase()   → LAZY  (registers side-effects)
AllCases()      → LAZY  (exits Cases context, returns general query)
UnCase()        → LAZY  (exits Cases context, returns general query)
─────────────────────────────────────────────────────────
Do()            → ✅ TERMINAL — execute, discard result
Count()         → ✅ TERMINAL — execute, return count
ToList()        → ✅ TERMINAL — execute, collect to memory
WriteCsv()      → ✅ TERMINAL — execute, write to file
WriteTable()    → ✅ TERMINAL — execute, write to Snowflake/Spark
await foreach   → ✅ TERMINAL — execute, stream row by row
```

> **Rule**: Nothing crosses the wire, hits a database, or allocates significant memory until a terminal is called. The pipeline is just a description of what *will* happen.

### `.Do()` is the canonical terminal

When the pipeline's purpose is **side-effects only** (accumulators, logging, writing to external systems), and you don't need data back on the caller:

```csharp
// ✅ Correct — explicit, intention-revealing
source.Cases(...).ForEachCase(...).AllCases().Do();

// ⚠️ Misleading — implies you need the count
source.Cases(...).ForEachCase(...).AllCases().Count();

// ⚠️ Verbose — iterating only to trigger execution
await foreach (var _ in source.Cases(...).ForEachCase(...).AllCases()) { }
```

`.Do()` exists on every query type in every DataLinq provider. It is **always** the right choice when no return value is needed.

---

## 3. Operator Chaining — All Valid Orderings

`SelectCase` and `ForEachCase` are **independent operations**. Neither requires the other. They can appear in any order, or be omitted entirely.

### Minimal pipeline — CategoryOnly

```csharp
// Just categorize and exit
source
    .Cases(p => p.IsActive, p => p.IsPending)
    .UnCase()          // Exit Cases context, get back the original T items
    .Do();
```

### ForEachCase only (no SelectCase)

```csharp
// Accumulate per-category, no transformation needed
source
    .Cases(o => o.Amount > 1000, o => o.Amount <= 1000)
    .ForEachCase(
        high => Stats.HighCount++,
        low  => Stats.LowCount++
    )
    .UnCase()          // Exit Cases context → IAsyncEnumerable<T> / SnowflakeQuery<T>
    .Do();             // TERMINAL
```

### SelectCase only (no ForEachCase)

```csharp
// Transform per-category, no side-effects
var results = await source
    .Cases(o => o.IsVip, o => o.IsNew)
    .SelectCase(
        vip  => new Summary { Tag = "VIP",      Amount = vip.Amount * 1.1m },
        newC => new Summary { Tag = "NEW",       Amount = newC.Amount },
        supra=> new Summary { Tag = "STANDARD",  Amount = supra.Amount }
    )
    .AllCases()        // Exit Cases context → IAsyncEnumerable<Summary>
    .ToList();
```

### SelectCase → ForEachCase (transform first, then accumulate)

```csharp
// Transform → accumulate the transformed value
source
    .Cases(o => o.IsVip, o => o.IsNew)
    .SelectCase(
        vip  => new Summary { Tag = "VIP",  TotalAmount = vip.Amount * 1.1m },
        newC => new Summary { Tag = "NEW",  TotalAmount = newC.Amount }
    )
    .ForEachCase(
        vip  => Revenue.Vip  += vip.TotalAmount,   // accumulate on Summary.TotalAmount
        newC => Revenue.New  += newC.TotalAmount
    )
    .AllCases()        // Exit: returns general query of Summary
    .Do();
```

### ForEachCase → SelectCase (accumulate original, then transform)

```csharp
// Accumulate original item metrics, then transform for output
source
    .Cases(o => o.IsVip, o => o.IsNew)
    .ForEachCase(
        vip  => Stats.VipCount++,       // accumulate on original Order
        newC => Stats.NewCount++
    )
    .SelectCase(
        vip  => $"VIP-{vip.Id}",
        newC => $"NEW-{newC.Id}",
        supra=> $"STD-{supra.Id}"
    )
    .AllCases()        // Exit: returns general query of string
    .Do();
```

### Full pipeline — SelectCase + ForEachCase + data needed

```csharp
var summaries = await source
    .Cases(o => o.Amount > 10000, o => o.Status == "Rush")
    .SelectCase(
        premium => new OrderSummary { Id = premium.OrderId, Label = "PREMIUM" },
        rush    => new OrderSummary { Id = rush.OrderId,    Label = "RUSH"    },
        supra   => new OrderSummary { Id = supra.OrderId,   Label = "STANDARD"}
    )
    .ForEachCase(
        premium => Revenue.Premium += premium.Amount,
        rush    => Revenue.Rush    += rush.Amount
    )
    .AllCases()
    .ToList();         // TERMINAL — get data back
```

---

## 4. The Exit Gate — `AllCases()` and `UnCase()`

`AllCases()` and `UnCase()` are the **exit gate** out of the Cases context. After calling either, you are back in the general query pipeline with the full terminal vocabulary.

| Exit Method | Input | Output | Use When |
|-------------|-------|--------|----------|
| `AllCases()` | After `SelectCase` | General query of `R` | You want the **transformed** items |
| `UnCase()` | After `Cases` or `ForEachCase` | General query of `T` | You want the **original** items |

```
                    ┌──────────────────────────┐
    source          │     CASES CONTEXT         │     general query
    ──────► Cases() ─► [SelectCase()] ──────────►─► AllCases() ──► .Do()
                    │  [ForEachCase()] ──────►──────► UnCase()  ──► .Do()
                    │                           │                ──► .Count()
                    └──────────────────────────┘                ──► .ToList()
                                                                ──► .WriteCsv()
                                                                ──► await foreach
```

The exit methods are themselves **lazy** — they only repackage the query. The terminal (`.Do()`, `.Count()`, etc.) is where computation actually kicks off.

---

## 5. The Supra Category

When a `Cases()` call has N predicates, items that match **none** of them get category index N — the **supra category** (the "catch-all").

```csharp
source.Cases(
    p => p.Type == "A",   // Category 0
    p => p.Type == "B"    // Category 1
    // Type "C", "D", etc. → Category 2 (supra)
)
```

**Supra in SelectCase — partial coverage:**

If `SelectCase` receives fewer selectors than categories, supra items get `default(R)` (null for reference types). Use `filterNulls: true` on `AllCases()` (the default) to transparently exclude them:

```csharp
source
    .Cases(p => p.IsActive, p => p.IsPending)
    .SelectCase(
        active  => new Result { Status = "Active",  Value = active.Score * 2 },
        pending => new Result { Status = "Pending", Value = pending.Score }
        // Supra (neither active nor pending) → default(Result) → filtered by AllCases
    )
    .AllCases(filterNulls: true)   // true is the default — supra items silently dropped
    .ToList();
```

**Supra in ForEachCase — explicit null slot:**

```csharp
source
    .Cases(p => p.IsVip, p => p.IsNew)
    .ForEachCase(
        vip  => Stats.Vip++,
        newC => Stats.New++,
        null   // ← explicit null: "do nothing for supra"
    )
    .UnCase()
    .Do();
```

---

## 6. Complete Examples

### Pure accumulation, no data needed

```csharp
static long HighCount = 0;
static long LowCount = 0;
static void CountHigh(Order o) => HighCount++;
static void CountLow(Order o) => LowCount++;

// No data returned — .Do() is the natural terminal
await Read.Csv<Order>("orders.csv")
    .Cases(o => o.Amount > 1000, o => o.Amount <= 1000)
    .ForEachCase(CountHigh, CountLow)
    .UnCase()
    .Do();

Console.WriteLine($"High: {HighCount}, Low: {LowCount}");
```

### Transform + accumulate + stream output

```csharp
await foreach (var summary in Read.Csv<Order>("orders.csv")
    .Cases(o => o.Amount > 10000, o => o.Status == "Rush")
    .SelectCase(
        prem  => new Summary { Id = prem.OrderId,  Label = "PREMIUM" },
        rush  => new Summary { Id = rush.OrderId,   Label = "RUSH"   },
        supra => new Summary { Id = supra.OrderId,  Label = "STD"    }
    )
    .ForEachCase(
        prem  => Revenue.Premium += prem.Amount,
        rush  => Revenue.Rush    += rush.Amount
    )
    .AllCases())  // ← terminal via streaming (await foreach)
{
    await Sink.WriteAsync(summary);
}
```

### Configuration-driven transformation tree

Define all transformation branches declaratively, execute once:

```csharp
await dataSource
    .Cases(
        data => data.Type == "Customer",
        data => data.Type == "Order",
        data => data.Type == "Product"
    )
    .SelectCase(
        customer => EnrichCustomer(customer),
        order    => CalculateTotal(order),
        product  => NormalizeProduct(product),
        unknown  => LogUnknown(unknown)      // supra handler
    )
    .ForEachCase(
        customer => customerDB.Save(customer),
        order    => orderDB.Save(order),
        product  => productDB.Save(product),
        unknown  => errorLog.Log(unknown)
    )
    .AllCases()
    .WriteCsv("processed_output.csv");  // TERMINAL
```

---

## 7. Multi-Type Branching

When different branches require **different return types**, use `SelectCases` (note the plural **s**). This returns a flat nullable tuple — only the slot matching the executed branch contains a value.

```csharp
await logs
    .Cases(
        log => log.Level == "ERROR",
        log => log.Level == "WARNING",
        log => log.Level == "INFO"
    )
    .SelectCases<Log, ErrorReport, WarningLog, InfoMetric>(
        error   => new ErrorReport { Severity = 1, Message = error.Text },
        warning => new WarningLog  { Category = warning.Source },
        info    => new InfoMetric  { MetricName = info.Key, Value = info.Count }
    )
    .ForEachCases<Log, ErrorReport, WarningLog, InfoMetric>(
        error   => errorDb.Save(error),
        warning => logDb.Save(warning),
        info    => metricsDb.Save(info)
    )
    .UnCase()    // Returns original Log items
    .Do();
```

Multi-type branching supports **2 to 7 types** and is available on all paradigms:

| Paradigm | Available Types | Status |
|----------|----------------|--------|
| `IEnumerable<T>` | 2–7 | ✅ Free |
| `IAsyncEnumerable<T>` | 2–7 | ✅ Free |
| `ParallelQuery<T>` | 2–7 | ✅ Free |
| `ParallelAsyncQuery<T>` | 2–7 | ✅ Free |
| `SparkQuery<T>` | 2–4 | 🔒 Enterprise |
| `SnowflakeQuery<T>` | 2–4 | 🔒 Enterprise |

---

## 8. Write Once, Process Anywhere

The Cases pattern uses the same syntax across all DataLinq paradigms. Swap the source — the pipeline code stays identical.

```csharp
// Define processing logic ONCE — works on any paradigm
static async Task ProcessOrders<T>(T source) where T : IAsyncEnumerable<Order>
{
    await source
        .Cases(o => o.Amount > 10000, o => o.Status == "Rush")
        .SelectCase(
            prem  => new Summary { Label = "PREMIUM", Amount = prem.Amount * 1.1m },
            rush  => new Summary { Label = "RUSH",    Amount = rush.Amount },
            supra => new Summary { Label = "STD",     Amount = supra.Amount }
        )
        .AllCases()
        .WriteCsv("output.csv");
}

// DEVELOPMENT: in-memory test data
await ProcessOrders(new[] { new Order() }.Async());

// VALIDATION: CSV file
await ProcessOrders(Read.Csv<Order>("orders.csv"));

// PRODUCTION: live stream
await ProcessOrders(liveOrderStream);
```

For distributed providers (Spark, Snowflake), the source is a `SparkQuery<T>` or `SnowflakeQuery<T>` — the pipeline expression is identical, but execution happens on the cluster.

---

## 9. API Reference

### OSS — `IAsyncEnumerable<T>` (and `IEnumerable<T>`, `ParallelQuery`, `ParallelAsyncQuery`)

| Method | Signature | Returns | Lazy? |
|--------|-----------|---------|-------|
| `Cases` | `Cases(params Func<T,bool>[] filters)` | `IAsyncEnumerable<(int, T)>` | ✅ Lazy |
| `SelectCase` | `SelectCase(params Func<T,R>[] selectors)` | `IAsyncEnumerable<(int, T, R)>` | ✅ Lazy |
| `ForEachCase` | `ForEachCase(params Action<T>[] actions)` ¹ | `IAsyncEnumerable<(int, T)>` or `<(int, T, R)>` | ✅ Lazy |
| `AllCases` | `AllCases(bool filterNulls = true)` | `IAsyncEnumerable<R>` | ✅ Lazy |
| `UnCase` | `UnCase()` | `IAsyncEnumerable<T>` | ✅ Lazy |
| `Do` | `Do()` | `void` / `Task` | ⚡ **Terminal** |

¹ `ForEachCase` overloads exist for both `(int, T)` (after `Cases`) and `(int, T, R)` (after `SelectCase`), enabling chaining in both directions.

### Distributed providers (Spark & Snowflake)

Same operator names, but:
- Returns `SparkQuery<T>`/`SnowflakeQuery<T>` instead of `IAsyncEnumerable<T>`
- Execution is distributed (Spark cluster / Snowflake compute)
- `.Do()` returns `Task` (async) instead of `void`
- See [Cases-Pattern-Providers.md](Cases-Pattern-Providers.md) for provider-specific details

### Chaining Validity Matrix

| Chain | Valid? | Note |
|-------|--------|------|
| `Cases → UnCase → terminal` | ✅ | Minimal pipeline |
| `Cases → ForEachCase → UnCase → terminal` | ✅ | Accumulate original |
| `Cases → SelectCase → AllCases → terminal` | ✅ | Transform only |
| `Cases → SelectCase → ForEachCase → AllCases → terminal` | ✅ | Transform then accumulate |
| `Cases → ForEachCase → SelectCase → AllCases → terminal` | ✅ | Accumulate original then transform |
| `Cases → ForEachCase → AllCases → terminal` | ✅ | ForEachCase on categorized T, exit with T as R |

---

## See Also

- [Cases-Pattern-Providers.md](Cases-Pattern-Providers.md) — Spark and Snowflake extensions
- [DataLinq-SUPRA-Pattern.md](DataLinq-SUPRA-Pattern.md) — Stream pipeline philosophy
- [LINQ-to-Spark.md](LINQ-to-Spark.md) — Distributed processing
- [LINQ-to-Snowflake.md](LINQ-to-Snowflake.md) — Cloud warehouse integration
