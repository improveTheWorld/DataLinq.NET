# Cases Pattern: Distributed Providers (Spark & Snowflake)

> **This document extends the core [Cases Pattern](Cases-Pattern.md) to explain how DataLinq.NET's routing and lazy execution principles are implemented on distributed compute clusters.**

---

## Table of Contents

1. [Distributed Execution Model](#1-distributed-execution-model)
2. [DataLinq.Spark Implementation](#2-datalinqspark-implementation)
3. [DataLinq.Snowflake Implementation](#3-datalinqsnowflake-implementation)
4. [API Harmony & Divergences](#4-api-harmony--divergences)
5. [Bulk Routing (Write / Merge)](#5-bulk-routing-write--merge)

---

## 1. Distributed Execution Model

When you write a Cases pipeline against `SparkQuery<T>` or `SnowflakeQuery<T>`, the framework guarantees:

- **Zero Client-Side Buffering**: The predicates and transformations are translated into native cluster operations (Spark DataFrames or Snowflake SQL).
- **Single-Pass Execution**: The cluster evaluates the categorization logic exactly once per row.
- **Lazy Evaluation**: Nothing is sent to the cluster until you call a terminal action (`.Do()`, `.Count()`, `.WriteTables()`, etc.).
- **Synchronized Side-Effects**: `ForEachCase` side-effects (like incrementing static variables) run on the worker nodes, but the mutated deltas are reliably synced back to your driver application after execution.

---

## 2. DataLinq.Spark Implementation

The Spark provider translates the Cases pipeline into heavily optimized Spark DataFrame column expressions.

### Technical Mapping

| C# Operator | Spark Equivalent |
|-------------|------------------|
| `Cases()` | `Functions.When(...).Otherwise(...)` projecting a new `_category` column |
| `SelectCase()` | A wide DataFrame with `_c0`, `_c1` struct columns mapped to categories |
| `ForEachCase()` | Native Spark **UDFs** wrapped in the **Delta Reflection Protocol** |
| `AllCases()` | `Filter(_category < N)` followed by a schema unwrap |

### The Delta Reflection Protocol

When you use `ForEachCase` on Spark, your C# delegates execute on distributed worker nodes (JVM). The **Delta Reflection Protocol** makes this transparent:

1. **Serialize Base State**: The driver captures the initial state of your static/instance variables.
2. **Execute Distributed**: Worker nodes run the UDFs in isolation.
3. **Capture Deltas**: At the end of each partition, workers compute the *delta* of the variables.
4. **Merge on Driver**: Spark collects the deltas and applies them via addition (`+=`) to the original driver context.

### Terminal Auto-Materialization (TAM)

When executing multi-branch pipelines (e.g. `WriteTables` or `ForEachCase` with multiple routes), the Spark Catalyst optimizer normally treats each terminal action as a completely separate query, forcing an $O(N)$ execution penalty that rescans the root data table $N$ times.

DataLinq.Spark natively intercepts this via **Terminal Auto-Materialization (TAM)**. The framework automatically calls `DataFrame.Cache()` on the root `Cases()` lineage right before dispatching the terminal loops. The first branch action materializes the lineage into cluster memory, and all subsequent branches execute instantly in $O(1)$ time. A deterministically protected C# `finally` block calls `DataFrame.Unpersist()`, guaranteeing your cluster memory remains pristine even on task failure.

### Spark API Signatures

- `SparkQuery<T>` supports up to **4 branches** for multi-type branching (`SelectCases<T, R1, R2, R3, R4>`).
- `.Do()` is **synchronous** (`void`) because native Spark execution (`.count()`) blocks the driver JVM thread.
- `AllCases()` unwraps the intermediate struct storage and returns a pristine horizontally-scaled `SparkQuery<R>`.

---

## 3. DataLinq.Snowflake Implementation

The Snowflake provider relies on SQL translation and dynamically deployed Stored Procedures, operating completely over the wire.

### Technical Mapping

| C# Operator | Snowflake Equivalent |
|-------------|----------------------|
| `Cases()` | `SELECT CASE WHEN ... THEN 0 WHEN ... THEN 1 ELSE n END AS _category` |
| `SelectCase()` | Projection of transformed columns (`_r_AliasName`) based on `_category` |
| `ForEachCase()` | C# IL compiled to Java, deployed as **Stored Procedures**, executed via `PostExecutionSync` |
| `AllCases()` | Subquery wrapping: `SELECT <R_cols> FROM (...) WHERE _category < N` |

### Server-Side Stored Procedures

Because Snowflake compute is isolated, `ForEachCase` takes a different route than Spark:

1. **IL to Java Compilation**: Your C# delegates are compiled into Java using the DataLinq.Snowflake compiler.
2. **Terminal Auto-Materialization (TAM)**: To prevent N+1 base query execution penalties, DataLinq intercepts terminal actions (`.Do()`). It unconditionally injects a `CREATE TEMPORARY TABLE _DL_CAT_TMP... AS SELECT ...` before creating the stored procedures, so all SPs read from a flat, single-pass materialized cache.
3. **Deployment**: A temporary Stored Procedure is registered in your Snowflake schema for each category branch.
4. **Execution**: The lazy thunk issues synchronous `CALL procedure_name()` statements in sequence against the materialized temp table.
5. **Result & Cache Sync**: The returned scalars (variable mutations) are projected back into your static C# context, and the temporal footprint is erased via an injected `DROP TABLE IF EXISTS` in a resilient `finally` block.

### Snowflake API Signatures

- `.Do()` is **asynchronous** (`Task`) because Snowflake I/O relies on network calls.
- `AllCases()` acts purely as a SQL subquery wrapper, guaranteeing zero rows are transmitted to the client unless you explicitly pull them.

---

## 4. API Harmony & Divergences

DataLinq guarantees structural API harmony, meaning your code reads the same regardless of the compute engine. However, platform constraints introduce intentional variations:

| Feature | DataLinq.NET (OSS) | DataLinq.Spark | DataLinq.Snowflake |
|---------|------------------|----------------|--------------------|
| Engine | Local CPU | Distributed JVM | Cloud Warehouse |
| Branch Limit | 7 Types | 4 Types | 4 Types |
| `.Do()` Terminal | `void`/`Task` | `void` (Sync) | `Task` (Async) |
| Side-Effect Model | Shared Memory | Delta Reflection | Stored Procedure Sync |
| Network Egress | N/A | High bandwidth | High cost (avoid implicit pulls) |

> **Harmony Contract:** You can write a pipeline locally using `IEnumerable<T>`, validate it, and confidently swap the source to `SparkQuery<T>` or `SnowflakeQuery<T>`. The only required changes will be `await` keywords if shifting to an async I/O provider.

---

## 5. Bulk Routing (Write / Merge)

While `ForEachCase` is designed for **memory side-effects** (counters, metrics, small logging), it is the wrong tool for bulk database routing.

When you need to send different categories of data to different physical tables, use the **Terminal Write Operations**. These replace `.Do()`:

| Operator | Action |
|----------|--------|
| `WriteTables(tables...)` | Writes each category to a distinct table. |
| `MergeTables(configs...)` | Upserts each category into a target table using primary keys. |

> **Performance Note**: Just like `ForEachCase`, the `WriteTables` and `MergeTables` operations automatically leverage **Terminal Auto-Materialization (TAM)**. For Snowflake, `WriteTables`/`MergeTables` conditionally inject a `CREATE TEMPORARY TABLE` when routing to multiple targets (single-target writes skip the overhead). For Spark, the system unconditionally leverages native RDD DAG caching (`DataFrame.Cache()` and `DataFrame.Unpersist()`). Both systems eliminate the N+1 execution penalty inherent to dynamic column branching with zero developer overhead.

```csharp
// ☁️ Distributed Bulk Routing (No Data Moves to Driver)
await query
    .Cases(
        tx => tx.Amount > 10000,
        tx => tx.IsFraudulent
    )
    // Terminal — directly drives Spark/Snowflake partition writing
    .WriteTables("HIGH_VALUE_TX", "FRAUD_TX", "STANDARD_TX");
```
