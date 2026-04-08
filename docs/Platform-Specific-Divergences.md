# Platform-Specific Divergences (Spark vs. Snowflake)

The foundational promise of DataLinq is **"Write Once, Run Anywhere."** 

For 95% of standard domain logic, this promise is absolute. You can write a `Where() -> Select() -> Cases() -> SelectCase()` pipeline and execute it against a local CSV, a local Entity Framework `DbContext`, a Snowflake Cloud Data Warehouse, or a Petabyte-scale Apache Spark cluster without changing a single character of code.

However, DataLinq explicitly prioritizes **Engine Empathy** over perfect API symmetry. We refuse to build "leaky abstractions" or "lowest common denominators" that degrade performance on the underlying hardware just to make the C# API look identical.

Below is the matrix of intentional API divergences between our `DataLinq.Spark` and `DataLinq.Snowflake` providers.

---

## 1. Unified API (Identical on Both)

These features work identically across both platforms with the exact same fluent C# signature:

| Feature Area | API | Description |
|--------------|-----|-------------|
| **Core LINQ** | `Where`, `Select`, `OrderBy`, `GroupBy`, `Join` | Standard projection, filtering, joining, grouping. |
| **Materialization** | `ToArray()`, `ToList()`, `Count()`, `First()` | Evaluates the deferred expression tree. |
| **Streaming** | `Pull()` | Yields `IAsyncEnumerable<T>` for low-memory, row-by-row streaming. |
| **Advanced Join** | `GroupJoin(left, right, ...)` | Translates to `LEFT JOIN + GROUP BY`. |
| **Analytics** | `WithWindow(spec, ...)` | Strongly-typed window functions (`Rank`, `Lag`, `Sum() OVER`). |
| **Routing** | `Cases(c1, c2)` → `SelectCase()` / `AllCases()` | Evaluates multiple conditions in a single pass. |
| **Delta Reflection**| `ForEach(o => state += o.Val).Do()` | Server-side iteration that merges additive primitive state back to C#. |
| **Custom Methods**| `Where(o => MyMethod(o.Val))` | Automatic translation of C# methods to UDFs / Server-Side functions. |

---

## 2. Platform-Specific Divergences

Due to the fundamental difference between distributed in-memory orchestrators (Spark) and cloud data warehouses (Snowflake), the API surfaces diverge intentionally in specific edge cases:

### A. Key Selectors & Determinism
| Scenario | DataLinq.Spark | DataLinq.Snowflake | The Engine Empathy Reason |
|----------|----------------|--------------------|---------------------------|
| **GroupBy Keys** | ✅ **Supported**. Translates to Spark SQL or Auto-UDFs. | ✅ **Supported**. Translates computed expressions to SQL or Java transpiled UDFs. | (Unification Achieved in v1.1.1) Both engines now fully support complex GroupBy key expressions (e.g., `GroupBy(x => CustomUdf(x))`). |
| **Join Keys** | ❌ **Rejected**. Must be direct property access. | ✅ **Supported**. | Spark requires pure properties for deterministic map-side shuffling. Use a preceding `Select` to project computed join keys. |
| **UUIDs** | ❌ **Rejected**. (`Guid.NewGuid()`). | ✅ **Supported**. Supported via `UUID_STRING()`. | Spark enforces total determinism so that failed partitions can be retried identically without corrupting data pipelines. |
| **Groups**| ❌ **Rejected** (No grouping by constants). | ✅ **Supported**. | Spark requires direct aggregates on the query object (`.Count()`) to prevent single-node bottlenecks over shuffling global constants. |

### B. Set Operations (Union)
| Set Operation | DataLinq.Spark | DataLinq.Snowflake | The Engine Empathy Reason |
|---------------|----------------|--------------------|---------------------------|
| **`Union()`** | Translates to Spark `union()` (**Keeps duplicates**, = `UNION ALL`). | Translates to SQL `UNION ALL`. | To maximize pure ingestion speed, Spark avoids global duplication shuffles. We map `Union()` directly to Spark's raw union. |
| **`UnionDistinct()`**| Requires explicit `.DropDuplicates()`. | Native support, translates to SQL `UNION`. | If you require strict unique rows in Spark, we force you to explicitly declare `.DropDuplicates()` so you are aware of the shuffle penalty you are invoking. |

### C. Nested & Semi-Structured Data
| Feature | DataLinq.Spark | DataLinq.Snowflake | The Engine Empathy Reason |
|---------|----------------|--------------------|---------------------------|
| **JSON Mapping** | Implicit structural mapping via Spark `Row` objects. | Native `[Variant]` attribute mapping with `:` SQL syntax. | Snowflake treats `VARIANT` as a first-class proprietary type for extreme JSON traversal speed. |
| **Arrays**| Implicit translation to `expr("exists(...)")`. | Translates to native Snowflake `FILTER` and `TRANSFORM`. | Snowflake SQL provides specific array functional processing which we tap into natively. |

### D. Writing & DDL
| Action | DataLinq.Spark | DataLinq.Snowflake | The Engine Empathy Reason |
|--------|----------------|--------------------|---------------------------|
| **Writing** | `WriteTable`, `WriteParquet`, `WriteCsv`, `WriteJson`. | `WriteTable`, `MergeTable`. | Spark is a multi-format lakehouse engine. Snowflake is a strict tabular warehouse. The write syntax is unified (`overwrite: true`, `createIfMissing: true`). |
| **Routing** | ✅ **Unified**. `WriteTables()`, `WriteParquets()`, `MergeTables()`. | ✅ **Unified**. `WriteTables()`, `MergeTables()`. | (Unification Achieved in v1.1.1) Both engines separate side-effect compute (`ForEachCase(Action<R>[])`) from high-speed bulk IO routing APIs. |
| **DDL** | Implicit schema tracking via DataFrame API. | `CreateDatabase()`, `CreateSchema()`, `DropTable()`. | Snowflake allows pure database management commands that Spark dataframes do not directly model. |

## The Verdict

We will never try to hide a massive distributed shuffle behind a "convenient" unified method call. We trust our developers to handle the truth of the underlying engine. 

When your code compiles in DataLinq, it represents the absolute most optimal execution plan for that specific platform. That is the true promise.
