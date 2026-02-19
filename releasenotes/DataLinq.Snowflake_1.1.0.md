# DataLinq.Snowflake v1.1.0

**Release Date:** February 20, 2026
**Requires:** DataLinq.NET 1.0.0+

> **"Write C#, Run Snowflake"** — Custom C# methods now auto-translate to Snowflake UDFs at runtime. No SQL, no escape hatches — just write C# and it runs server-side.

## ✨ Auto-UDF — Zero-Friction Server-Side C#

Static, instance, lambda, closure, and entity-parameter methods in `Where()` / `Select()` / `OrderBy()` / `GroupBy()` auto-deploy as Snowflake Java UDFs at connect time via IL-to-Java translation.

| Pattern | Example |
|---------|---------|
| **Static method** | `Where(o => IsHighValue(o.Amount))` |
| **Instance method** | `var v = new Validator(1000); Where(o => v.IsValid(o.Amount))` |
| **Entity parameter** | `Where(o => IsVipOrder(o))` — decomposes `o` into columns |
| **Multi-param** | `Where(o => IsInRange(o.Amount, 1000, 5000))` |
| **Mixed expression** | `Where(o => o.IsActive && IsHighValue(o.Amount))` |
| **Nested calls** | `Where(o => IsHighValueCorp(o.Amount, o.Name))` |

- **Mixed expression decomposition**: SQL parts translate natively, UDF parts generate function calls — zero developer friction
- **All LINQ positions**: Where, Select, OrderBy, OrderByDescending, ThenBy, ThenByDescending, GroupBy
- **Pipeline-safe**: UDFs work with Join, Union, Intersect, Except, WriteTable, Skip, Take, Distinct

## ✨ ForEach — Server-Side Iteration

- **`query.ForEach(action)`** — deferred server-side iteration via Snowflake stored procedures
- **Static field sync-back**: Accumulator fields in the delegate are written back after procedure execution
- **Entity-parameter support**: Methods that take the full entity auto-decompose into column arguments
- **Instance method + closure support**: Captured fields passed as procedure arguments

## ✨ Build-Time Source Generator

New `DataLinq.Snowflake.Analyzers` Roslyn Source Generator translates C# method bodies to Java at build time (AST analysis, not runtime IL). Emits `[GeneratedUdf]` classes with pre-translated Java source.

## Breaking Changes

### Type-Safe UpdateOnly API
`UpdateOnly()` now requires **lambda expressions** instead of string column names:
```csharp
// v1.1.0 (new)
.UpdateOnly(o => o.Status, o => o.UpdatedAt)

// v1.0.x (removed)
.UpdateOnly("Status", "UpdatedAt")
```

## What's New

### Undocumented API Surfacing
- **`SelectMany`** — LATERAL FLATTEN for VARIANT array processing
- **`GroupJoin`** — LEFT JOIN + GROUP BY in single operation
- **Terminal Aggregates** — `Sum()`, `Average()`, `Min()`, `Max()` as terminal operators
- **`Count(predicate)`** — Count with inline filter

### Intelligent Naming Mapping
- Automatic PascalCase ↔ snake_case resolution across the entire read/write/query pipeline

### Package Isolation
- Snowflake.Data driver internalized — types no longer leak into consumer IntelliSense

### Enhanced Obfuscation
- All 3 DLLs (SnowflakeQuery, Licensing, Analyzers) obfuscated with Obfuscar — string hiding + rename

## Bug Fixes

### SQL Translation
- String.Replace, TrimStart, TrimEnd translations
- Math.Log, Log10, Log2 support
- Java string `==` uses `.equals()` instead of reference identity (SF-058)
- Broadened string.Length for complex expressions

### Query Operations
- Window functions in WHERE clause wrap as subquery
- WithWindow SELECT * passthrough fix
- Set operations snapshot operands (no clause leakage)
- OFFSET without LIMIT handled correctly
- Count with LIMIT/OFFSET wraps as subquery
- DateTime property in GroupBy key translates correctly
- Parameter index collision in set operations fixed
- Bind variable lost after Select() projection (SF-013)
- List<T>.Contains() → IN support (SF-014)
- DateTime arithmetic → DATEADD (SF-009)

### Write Operations
- CreateIfMissing column order fixed with explicit column lists
- Obfuscation-safe JSON deserialization for licensing
- Licensing DLL bundling fix (SF-010)

## Validation

**540 integration tests passed with zero failures** across 44 batch suites, covering:
- Core LINQ operations, aggregations, joins, set operations
- Auto-UDF (static, instance, lambda, entity, nested, stress)
- ForEach (deferred, entity-param, accumulator, instance method)
- VARIANT (basic, deep, HoFs, cross-feature)
- Window functions, Cases pattern, write operations
- Cross-feature stress tests, boundary limits, known limitation tests.