# DataLinq.Snowflake v1.1.0

**Release Date:** February 18, 2026

## Breaking Changes

### Type-Safe UpdateOnly API
`UpdateOnly()` now requires **lambda expressions** instead of string column names:
```csharp
// v1.1.0 (new)
.UpdateOnly(o => o.Status, o => o.UpdatedAt)

// v1.0.2 (removed)
.UpdateOnly("Status", "UpdatedAt")
```

## What's New

### Undocumented API Surfacing
- **`SelectMany`** — LATERAL FLATTEN for VARIANT array processing
- **`GroupJoin`** — LEFT JOIN + GROUP BY in single operation
- **Terminal Aggregates** — `Sum()`, `Average()`, `Min()`, `Max()` as terminal operators
- **`Count(predicate)`** — Count with inline filter

## Bug Fixes

### SQL Translation
- String.Replace, TrimStart, TrimEnd translations
- Math.Log, Log10, Log2 support
- Broadened string.Length for complex expressions

### Query Operations
- Window functions in WHERE clause wrap as subquery
- Set operations snapshot operands (no clause leakage)
- OFFSET without LIMIT handled correctly
- Count with LIMIT/OFFSET wraps as subquery
- DateTime property in GroupBy key translates correctly
- Parameter index collision in set operations fixed

### Write Operations
- CreateIfMissing column order fixed with explicit column lists
- Obfuscation-safe JSON deserialization for licensing

## Validation

Validated across 750+ integration tests with zero regressions.
