# DataLinq.Snowflake v1.0.2

**Release Date:** February 18, 2026

## What's New

### VARIANT Column Support
Store and retrieve complex nested objects as Snowflake VARIANT columns using the `[Variant]` attribute — automatic JSON serialization on write and deserialization on read.

### Enhanced Cases Pattern
`SelectCase()` now supports multi-property result types — each output property gets its own `CASE WHEN` expression, enabling complex routing patterns.

### Improved Join Operations
- Pre-join filters (`.Where()` before `.Join()`) are now correctly preserved in the generated SQL
- Join and GroupBy selectors support class initializer syntax (`new MyResult { Prop = val }`)

## Improvements

### Window Functions
Window operations (`WithWindow`, `WithRowNumber`, `WithRank`) now carry all source columns through the subquery, enabling full chaining with `.OrderBy()`, `.Where()`, and `.Take()`.

### Write Operations
- `MergeTable` upsert inserts all columns on `NOT MATCHED` (not just update columns)
- `CreateIfMissing` correctly initializes tables in all write paths
- VARIANT-aware property filtering across all write operations

### ForEach Server-Side Iteration
- Accumulator fields initialize from current C# values (composable across multiple calls)
- Fixed double-execution when using `ToList()` after ForEach
- Added `bool` type support

### SQL Translation
- Added `String.Replace`, `TrimStart`, `TrimEnd` → Snowflake equivalents
- Added `Math.Log`, `Log10`, `Log2`, `Floor`, `Ceiling`, `Round(digits)`, `Sqrt`, `Pow`, `Sign`, `Clamp`
- Broadened `string.Length` support for complex expressions

### Query Operations
- `Count()` correctly handles `Distinct` queries
- Set operations (`Union`, `Intersect`, `Except`) snapshot operands cleanly
- Pagination with `OFFSET` without `LIMIT` works correctly
- GroupBy with `DateTime` property access (`o.Date.Year`) translates correctly

## Validation

All known bugs resolved. Validated across 700+ integration tests with zero regressions.
