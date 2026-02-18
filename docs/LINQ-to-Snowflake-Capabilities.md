# LINQ-to-Snowflake Capabilities & Limitations

This document outlines the current capabilities of the `SnowflakeQuery<T>` provider in DataLinq.NET. It is designed to handle the "80% happy path" of analytics queries natively on Snowflake, while acknowledging specific limitations where client-side processing or alternative approaches are required.

## Table of Contents

1. [Supported Features](#supported-features-native-execution)
   - [Core Query Operations](#1-core-query-operations)
   - [Grouping & Aggregation](#2-grouping--aggregation)
   - [Joins](#3-joins)
   - [Expression Translation](#4-expression-translation)
   - [VARIANT Support](#5-variant-support-semi-structured-data)
   - [Window Functions](#6-window-functions)
   - [Set Operations](#7-set-operations)
   - [Debug & Diagnostics](#8-debug--diagnostics)
   - [Execution](#9-execution)
   - [Cases Pattern](#10-cases-pattern-multi-destination-routing)
   - [Server-Side Functions](#11-server-side-functions-custom-c-method-translation)
   - [Build-Time Diagnostics](#12-build-time-diagnostics)
2. [Design Philosophy](#design-philosophy--flat-pipeline-first)
3. [See Also](#see-also)

---

## ✅ Supported Features (Native Execution)

The following operations are translated directly to Snowflake SQL and executed server-side.

### 1. Core Query Operations
| LINQ Method | SQL Translation | Example |
|-------------|-----------------|---------|
| `Where(predicate)` | `WHERE ...` | `.Where(o => o.Amount > 100)` |
| `Select(selector)` | `SELECT ...` | `.Select(o => new { o.Id, o.Name })` |
| `OrderBy(key)` | `ORDER BY ...` | `.OrderBy(o => o.Date)` |
| `OrderByDescending` | `ORDER BY ... DESC` | `.OrderByDescending(o => o.Amount)` |
| `ThenBy` / `Descending` | `, ...` (chained sort) | `.OrderBy(...).ThenBy(o => o.Id)` |
| `Take(n)` | `LIMIT n` | `.Take(50)` |
| `Skip(n)` | `OFFSET n` | `.Skip(10)` — requires `OrderBy` |
| `Distinct()` | `SELECT DISTINCT` | `.Select(o => o.Category).Distinct()` — supports `Distinct().Count()` |
| `SelectMany(selector)` | `LATERAL FLATTEN(...)` | `.SelectMany(o => o.Items)` — flattens VARIANT arrays into rows |

### 2. Grouping & Aggregation
| LINQ Method | SQL Translation | Notes |
|-------------|-----------------|-------|
| `GroupBy(key)` | `GROUP BY key` | Single and composite keys (`new { x.A, x.B }`) supported |
| `.Select(g => g.Count())` | `COUNT(*)` | |
| `.Select(g => g.Sum(x))` | `SUM(x)` | |
| `.Select(g => g.Max(x))` | `MAX(x)` | |
| `.Select(g => g.Min(x))` | `MIN(x)` | |
| `.Select(g => g.Average(x))` | `AVG(x)` | Returns `double` |

**Terminal Aggregates (no GroupBy needed):**
| LINQ Method | SQL Translation | Notes |
|-------------|-----------------|-------|
| `.Sum(o => o.Amount)` | `SELECT SUM(amount)` | Overloads: `decimal`, `double`, `long`, `int` |
| `.Average(o => o.Amount)` | `SELECT AVG(amount)` | Always returns `double` |
| `.Min(o => o.Amount)` | `SELECT MIN(amount)` | Generic — works with any comparable type |
| `.Max(o => o.Amount)` | `SELECT MAX(amount)` | Generic — works with any comparable type |

### 3. Joins
| LINQ Method | SQL Translation | Notes |
|-------------|-----------------|-------|
| `Join(...)` | `INNER JOIN ... ON ...` | Supports multi-table joins via chaining |
| `Join(..., joinType: "LEFT")` | `LEFT OUTER JOIN` | Also supports `"RIGHT"`, `"FULL"` |
| `GroupJoin(...)` | `LEFT JOIN + GROUP BY` | Groups elements from another query by a matching key |

### 4. Expression Translation
| C# Expression | SQL Translation |
|---------------|-----------------|
| `==`, `!=` | `=`, `<>` |
| `>`, `>=`, `<`, `<=` | `>`, `>=`, `<`, `<=` |
| `&&`, `||` | `AND`, `OR` |
| `!boolProp` | `NOT (column = TRUE)` |
| `string.Contains(s)` | `LIKE '%s%'` |
| `string.StartsWith(s)` | `LIKE 's%'` |
| `string.EndsWith(s)` | `LIKE '%s'` |
| `string.IndexOf(s)` | `POSITION(s, col) - 1` |
| `string.Length` | `LENGTH(col)` |
| `collection.Contains(x)` | `column IN (1, 2, 3)` |
| `Math.Abs(x)` | `ABS(x)` |
| `Math.Round(x)` | `ROUND(x)` |
| `Math.Ceiling(x)` | `CEIL(x)` |
| `Math.Floor(x)` | `FLOOR(x)` |
| `Math.Sqrt(x)` | `SQRT(x)` |
| `Math.Pow(x, y)` | `POW(x, y)` |
| `date.Year` | `YEAR(date)` |
| `date.Month` | `MONTH(date)` |
| `date.Day` | `DAY(date)` |
| `date.Hour` | `HOUR(date)` |
| `date.Minute` | `MINUTE(date)` |
| `date.Second` | `SECOND(date)` |
| `date.DayOfWeek` | `DAYOFWEEK(date)` |
| `date.DayOfYear` | `DAYOFYEAR(date)` |
| `obj.Prop.Nested` | `obj:prop:nested` | (VARIANT path access) |

### 5. VARIANT Support (Semi-Structured Data)

Mark complex properties with `[Variant]` to map them to Snowflake `VARIANT` columns. Properties are auto-serialized to JSON on write and support Snowflake's native colon syntax on read.

**Array Operations (Higher-Order Functions):**
| C# Expression | SQL Translation |
|----------------|-----------------|
| `o.Items.Any(i => i.Price > 100)` | `ARRAY_SIZE(FILTER(items, i -> i:price > 100)) > 0` |
| `o.Items.All(i => i.Active)` | `ARRAY_SIZE(FILTER(items, i -> NOT i:active)) = 0` |
| `o.Items.Where(i => i.Active)` | `FILTER(items, i -> i:active)` |
| `o.Items.Select(i => i.Price * 2)` | `TRANSFORM(items, i -> i:price * 2)` |

### 6. Window Functions
| Method | SQL Translation |
|--------|-----------------|
| `WithRowNumber(partitionBy, orderBy)` | `ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ...)` |
| `WithRank(partitionBy, orderBy)` | `RANK() OVER (PARTITION BY ... ORDER BY ...)` |
| `w.Lag("col", 1)` | `LAG(col, 1) OVER (...)` |
| `w.Lead("col", 1)` | `LEAD(col, 1) OVER (...)` |
| `w.Sum("col")` | `SUM(col) OVER (...)` |

### 7. Set Operations
| Method | SQL Translation |
|--------|-----------------|
| `query1.Union(query2)` | `UNION ALL` |
| `query1.UnionDistinct(query2)` | `UNION` |
| `query1.Intersect(query2)` | `INTERSECT` |
| `query1.Except(query2)` | `EXCEPT` |

### 8. Debug & Diagnostics
| Method | Description |
|--------|-------------|
| `Show(n)` | Display first N rows to console |
| `Explain()` | Print SQL query plan (synchronous — do not `await`) |
| `PrintSchema()` | Print result type schema |
| `Spy(label)` | Display and continue chaining |
| `ToSql()` | Get generated SQL string |

### 9. Execution
- **Async Streaming**: `IAsyncEnumerable<T>` support via `GetAsyncEnumerator` (efficient memory usage).
- **Materialization**: `ToList()`, `ToArray()`, `First()`, `FirstOrDefault()`, `Count()`, `Any()`.
- **Single Element**: `Single()`, `SingleOrDefault()` (verify exactly 1 result).
- **Predicate overloads**: `Count(pred)`, `Any(pred)`, `First(pred)` — all accept a predicate directly (equivalent to `.Where(pred).Count()` etc.).
- **Server-Side Functions**: Custom C# methods in `Where()`/`Select()` are automatically deployed and executed on Snowflake — static, instance, lambda, and entity-parameter patterns all supported. No `Pull()` needed.
- **ForEach (Deferred)**: `ForEach(action)` deploys server-side logic to Snowflake. Execution deferred until `Count()`/`ToList()`/`ToArray()`. Static fields auto-synced back after execution. Supported accumulator types: `int`, `long`, `double`, `float`, `decimal`, `bool`, `string`.
- **Pull() (Escape Hatch)**: `Pull()` switches to client-side streaming for edge cases where server-side execution is not desired (e.g., accessing LINQ operators not available on `SnowflakeQuery<T>`).

### 10. Cases Pattern (Multi-Destination Routing)
| Method | Description |
|--------|-------------|
| `Cases(predicates...)` | Categorize rows by conditions (SQL CASE WHEN) |
| `SelectCase(selectors...)` | Transform each category (server-side projection) |
| `WriteTables(tables...)` | Write each category to different table |
| `MergeTables((table, key)...)` | Merge each category with different match key |

### 11. Server-Side Functions (Custom C# Method Translation)

When you use a custom C# method inside `.Where()` or `.Select()`, DataLinq automatically deploys it as a **server-side function** on Snowflake. Your method runs directly in the warehouse — no data leaves Snowflake:

```csharp
// Static method → deployed as server-side function
.Where(o => IsHighValue(o.Amount))
// → WHERE auto_helpers_ishighvalue(amount)

// Instance method
.Where(o => validator.IsValid(o.Amount))
// → WHERE auto_ordervalidator_isvalid(amount)

// Lambda / Func<>
Func<decimal, bool> f = x => x > 1000;
.Where(o => f(o.Amount))
// → WHERE auto_lambda_f(amount)

// Entity parameter — auto-decomposed into columns
.Where(o => CustomValidator(o))
// → WHERE auto_class_customvalidator(amount, status)
```

> ⚠️ **Performance & billing impact:** Server-side functions bypass Snowflake's query optimizer (no predicate pushdown). Prefer native operators (`.Where(o => o.Amount > 1000)`) when possible. The Roslyn analyzer (`DFSN004`) warns at build time.

### 12. Build-Time Diagnostics
| Rule | Severity | Description |
|------|----------|---------|
| **DFSN004** | ⚠️ Warning | Custom method in `Where()` — prevents predicate pushdown |
| **DFSN005** | ⚠️ Warning | Instance method — supported, but carries closure overhead |
| **DFSN006** | ⚠️ Warning | Multiple server-side functions in same `Where` — compounding performance impact |
| **DFSN007** | ℹ️ Info | Method will execute server-side on Snowflake — not locally in your .NET process |
| **DFSN008** | ❌ Error | Method uses a construct with no Snowflake equivalent — cannot be deployed |

---

## Design Philosophy — Flat Pipeline First

DataLinq deliberately avoids patterns that hide complexity. Every query reads top-to-bottom as a clear pipeline — no nesting, no magic, no hidden SQL. This keeps your analytics code **readable, debuggable, and transparent**.

### Why No Navigation Properties?

EF Core's `Include(c => c.Orders)` silently generates JOINs behind the scenes. In analytics workloads billed by compute time, hidden JOINs are hidden costs. DataLinq makes every join explicit:

```csharp
// Every relationship is visible — you see exactly what SQL will run
var result = await customers
    .Join(orders, c => c.Id, o => o.CustomerId,
        (c, o) => new { c.Name, o.Product, o.Amount })
    .ToList();
```

### Why No Graph Materialization?

Snowflake returns flat rows. Rather than hiding that behind ORM magic, DataLinq gives you the flat data directly. If you need a grouped view, you write it explicitly — the intent is always visible:

```csharp
// Flat join → client-side grouping when you actually need a tree
var flat = await customers.Join(orders, ...).ToList();
var grouped = flat.GroupBy(r => r.Name)
    .Select(g => new { Customer = g.Key, Orders = g.ToList() });
```

### Why No Nested Subqueries?

Correlated subqueries (`.Where(o => otherQuery.Any(...))`) nest logic inside logic. This rapidly becomes impossible to debug and hard to understand. DataLinq forces you to write flat, composable pipelines instead:

```csharp
// Each step is a named variable you can inspect with .ToSql()
var usCustomers = context.Read.Table<Customer>("CUSTOMERS")
    .Where(c => c.Region == "US");

var usOrders = await orders
    .Join(usCustomers, o => o.CustomerId, c => c.Id,
        (o, c) => new { o.Id, o.Amount, c.Name })
    .ToList();
```

> **Result**: Your C# reads like the SQL it generates. When something breaks, you know exactly where to look.

---

## See Also

- [LINQ-to-Snowflake Guide](LINQ-to-Snowflake.md) — Complete usage documentation
- [LINQ-to-Spark](LINQ-to-Spark.md) — SparkQuery provider documentation
- [Cases Pattern](Cases-Pattern.md) — Cases/SelectCase pattern
- [Licensing](../../DataLinq.Enterprise/docs/Licensing.md) — Product-specific licensing details
