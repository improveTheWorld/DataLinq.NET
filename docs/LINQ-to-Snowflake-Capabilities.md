# Snowflake Expression Translation Reference

A compact reference for how C# expressions map to Snowflake SQL. For the full usage guide, see [LINQ-to-Snowflake](LINQ-to-Snowflake.md).

## Table of Contents

1. [Expression Translation Matrix](#expression-translation-matrix)
2. [Window Function Translation](#window-function-translation)
3. [VARIANT / Higher-Order Function Translation](#variant--higher-order-function-translation)
4. [Design Philosophy — Flat Pipeline First](#design-philosophy--flat-pipeline-first)
5. [See Also](#see-also)

---

## Expression Translation Matrix

Every C# expression used in `.Where()`, `.Select()`, `.OrderBy()`, and join key selectors is translated to the corresponding Snowflake SQL at runtime:

### Comparison & Logical Operators
| C# Expression | SQL Translation |
|---------------|-----------------|
| `==`, `!=` | `=`, `<>` |
| `>`, `>=`, `<`, `<=` | `>`, `>=`, `<`, `<=` |
| `&&`, `\|\|` | `AND`, `OR` |
| `!boolProp` | `NOT (column = TRUE)` |
| `x == null` | `column IS NULL` |
| `x != null` | `column IS NOT NULL` |
| `condition ? a : b` | `CASE WHEN condition THEN a ELSE b END` |

### String Operations
| C# Expression | SQL Translation |
|---------------|-----------------|
| `s.Contains("abc")` | `column LIKE '%abc%'` |
| `s.StartsWith("abc")` | `column LIKE 'abc%'` |
| `s.EndsWith("abc")` | `column LIKE '%abc'` |
| `s.Length` | `LENGTH(column)` |
| `s.IndexOf("x")` | `POSITION('x', column) - 1` |
| `s.ToUpper()` | `UPPER(column)` |
| `s.ToLower()` | `LOWER(column)` |
| `s.Trim()` | `TRIM(column)` |
| `s.Substring(i, n)` | `SUBSTR(column, i+1, n)` |
| `s.Replace("a", "b")` | `REPLACE(column, 'a', 'b')` |

### Math Functions
| C# Expression | SQL Translation |
|---------------|-----------------|
| `Math.Abs(x)` | `ABS(x)` |
| `Math.Round(x)` | `ROUND(x)` |
| `Math.Round(x, n)` | `ROUND(x, n)` |
| `Math.Ceiling(x)` | `CEIL(x)` |
| `Math.Floor(x)` | `FLOOR(x)` |
| `Math.Sqrt(x)` | `SQRT(x)` |
| `Math.Pow(x, y)` | `POW(x, y)` |

### DateTime Extraction
| C# Expression | SQL Translation |
|---------------|-----------------|
| `d.Year` | `YEAR(column)` |
| `d.Month` | `MONTH(column)` |
| `d.Day` | `DAY(column)` |
| `d.Hour` | `HOUR(column)` |
| `d.Minute` | `MINUTE(column)` |
| `d.Second` | `SECOND(column)` |
| `d.DayOfWeek` | `DAYOFWEEK(column)` |
| `d.DayOfYear` | `DAYOFYEAR(column)` |

### Collection Operations
| C# Expression | SQL Translation |
|---------------|-----------------|
| `list.Contains(x.Id)` | `column IN (1, 2, 3)` |
| `list.Any(s => s == x.Name)` | `column IN ('a', 'b', 'c')` |

---

## Window Function Translation

| Method | SQL Translation |
|--------|-----------------|
| `WithWindow(spec, projection)` | Expression-based window with full type safety |
| `WithRowNumber(partitionBy, orderBy)` | `ROW_NUMBER() OVER (PARTITION BY ... ORDER BY ...)` |
| `WithRank(partitionBy, orderBy)` | `RANK() OVER (PARTITION BY ... ORDER BY ...)` |
| `w.DenseRank()` | `DENSE_RANK() OVER (...)` |
| `w.PercentRank()` | `PERCENT_RANK() OVER (...)` |
| `w.Ntile(n)` | `NTILE(n) OVER (...)` |
| `w.CumeDist()` | `CUME_DIST() OVER (...)` |
| `w.Lag("col", offset)` | `LAG(col, offset) OVER (...)` |
| `w.Lead("col", offset)` | `LEAD(col, offset) OVER (...)` |
| `w.FirstValue("col")` | `FIRST_VALUE(col) OVER (...)` |
| `w.LastValue("col")` | `LAST_VALUE(col) OVER (...)` |
| `w.Sum("col")` | `SUM(col) OVER (...)` |
| `w.Avg("col")` | `AVG(col) OVER (...)` |
| `w.Max("col")` | `MAX(col) OVER (...)` |
| `w.Min("col")` | `MIN(col) OVER (...)` |
| `w.Count("col")` | `COUNT(col) OVER (...)` |

---

## VARIANT / Higher-Order Function Translation

Nested property access on `[Variant]`-annotated properties uses Snowflake's colon path syntax:

| C# Expression | SQL Translation |
|---------------|-----------------|
| `o.Metadata.Region` | `metadata:region` |
| `o.Metadata.Address.City` | `metadata:address:city` |
| `o.Items.Any(i => i.Price > 100)` | `ARRAY_SIZE(FILTER(items, i -> i:price > 100)) > 0` |
| `o.Items.All(i => i.Active)` | `ARRAY_SIZE(FILTER(items, i -> NOT i:active)) = 0` |
| `o.Items.Where(i => i.Active)` | `FILTER(items, i -> i:active)` |
| `o.Items.Select(i => i.Price * 2)` | `TRANSFORM(items, i -> i:price * 2)` |
| `o.Items.Count()` | `ARRAY_SIZE(items)` |

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
