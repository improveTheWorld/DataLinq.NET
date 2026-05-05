# DataLinq.Snowflake v1.4.0

**Release Date:** April 13, 2026
**Requires:** DataLinq.NET 1.0.0+

## 🎯 OrderedSnowflakeQuery&lt;T&gt; — Compile-Time Sorting Enforcement

`OrderBy` and `OrderByDescending` now return `OrderedSnowflakeQuery<T>` instead of `SnowflakeQuery<T>`. This enables **compile-time enforcement** of sorting-dependent operations:

- **`ThenBy` / `ThenByDescending`** — only available after `OrderBy`, no more runtime `InvalidOperationException`
- **`Skip`** — only available after `OrderBy`, deterministic pagination guaranteed by the type system

This mirrors standard .NET `IOrderedEnumerable<T>` semantics. `OrderedSnowflakeQuery<T>` inherits from `SnowflakeQuery<T>`, so full polymorphic compatibility with `WriteTable`, `Cases`, `ForEach`, and set operations is preserved.

> ⚠️ **Breaking change:** Code that calls `.Skip()` or `.ThenBy()` without a preceding `OrderBy` will now fail at **compile time** instead of runtime.

## 🔗 Composite & Computed Join Keys

Joins now support multi-column and expression-based keys:

```csharp
// Composite key — AND-joined conditions
orders.Join(customers,
    o => new { o.Region, o.CustomerId },
    c => new { c.Region, CustomerId = c.Id },
    (o, c) => new { o.OrderId, c.Name });
// SQL: ON l.region = r.region AND l.customer_id = r.id

// Computed key — full expression engine
orders.Join(customers,
    o => o.Name.ToUpper(),
    c => c.Name.ToUpper(),
    (o, c) => new { o.OrderId, c.Email });
// SQL: ON UPPER(l.name) = UPPER(r.name)
```

Both `Join` and `GroupJoin` support composite and computed keys. An arity guard throws `ArgumentException` at runtime if key column counts don't match (though C# generics also enforce this at compile time for most cases).

## ⚙️ AliasMapping SQL Translation Engine

The internal SQL translation engine has been refactored to use an `AliasMapping` dictionary that maps lambda parameters to SQL table aliases (`l`, `r`). This replaces the brittle `TranslateJoinExpression` helper with a native engine that correctly handles:

- Member access (`l.amount`, `r.name`)
- Method calls (`UPPER(l.name)`)
- Arithmetic, ternary, coalesce expressions — all with correct alias resolution

## 🧪 Test Suite

- **59 unit tests** (47 existing + 12 new) — all passing with zero regressions
- New tests cover composite keys, computed keys, result selector expressions, `OrderedSnowflakeQuery` type enforcement, and polymorphic compatibility

## 📖 Upgrading

Always ensure that your primary OSS dependency ([DataLinq.NET](https://github.com/improveTheWorld/DataLinq.NET)) is running an equivalent up-to-date baseline version.
