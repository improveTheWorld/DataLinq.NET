# DataLinq.Snowflake v1.2.2

**Release Date:** March 28, 2026
**Requires:** DataLinq.NET 1.0.0+

## 🐛 LEFT JOIN Conditional Projections

Ternary null-check expressions in JOIN result selectors are now fully supported. This is the common defensive pattern used in LEFT JOINs to guard against null references on the right side:

```csharp
var results = await context.Read.Table<Order>("ORDERS")
    .Join(context.Read.Table<Employee>("EMPLOYEES"),
        o => o.EmployeeId, e => e.Id,
        (o, e) => new { o.Id, Dept = e != null ? e.Department : null },
        joinType: "LEFT")
    .ToList();
```

The expression translator now detects this pattern and simplifies it to the underlying column reference — SQL already handles NULL propagation natively for unmatched rows.

## 🐛 Cases + SelectCase Write Consistency

`WriteTables` after a `SelectCase` transformation now auto-creates target tables, consistent with the non-transformed `Cases().WriteTables()` path. Previously, the transformed path required tables to pre-exist, causing a `Table does not exist` error on first use.

```csharp
await context.Read.Table<Order>("ORDERS")
    .Cases(o => o.Amount > 10000, o => o.IsActive)
    .SelectCase(
        o => new LiteOrder { Id = o.Id, Total = o.Amount },
        o => new LiteOrder { Id = o.Id, Total = o.Amount },
        o => new LiteOrder { Id = o.Id, Total = o.Amount }
    )
    .WriteTables(new[] { "PREMIUM", "RUSH", "STANDARD" });
```

## 🐛 GroupJoin Aggregates & Column Naming

- `GroupJoin` result selectors now correctly translate aggregate methods (`Count()`, `Sum()`, `Min()`, `Max()`, `Average()`).
- Fixed a naming inconsistency where write operations generated lowercased column names instead of Snowflake-compliant `SNAKE_CASE` for models with `ALL_CAPS` property names.

## 🐛 Local Collection `.Any()` in Where Filters

`localList.Any(s => s == o.Column)` now correctly translates to `column IN (val1, val2, ...)`. Previously, this pattern was incorrectly routed through the VARIANT array lambda translator, crashing with `PrimitiveParameterExpression`.

```csharp
var departments = new List<string> { "Engineering", "Sales" };
var result = await employees
    .Where(e => departments.Any(d => d == e.Department))
    .ToList();
// → WHERE department IN ('Engineering', 'Sales')
```

## 🐛 Ternary Expressions in GroupBy Keys

Dynamic bucketing via ternary expressions in `GroupBy` keys is now supported:

```csharp
var tiers = await employees
    .GroupBy(e => e.Salary > 70000 ? "High" : "Low")
    .Select(g => new { Tier = g.Key, Count = g.Count() })
    .ToList();
// → GROUP BY CASE WHEN salary > 70000 THEN 'High' ELSE 'Low' END
```

## 📖 Documentation

- **MergeTables snippet** — Updated to include the explicit `Expression<>` type annotations required by C# for tuple-based match key declarations.
- **Dispose pattern** — Corrected README instructions to use `await using var sf = Snowflake.Connect(...)`.

## 🔧 Maintenance

- Removed leftover `Console.WriteLine` debug statements that emitted raw query ASTs to standard output.
