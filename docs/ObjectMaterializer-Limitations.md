# ObjectMaterializer – Known Limitations

> **Version:** 1.2.x  
> **Planned Refactoring:** v2.0

This document catalogs the known limitations of `DataLinq.Framework.ObjectMaterializer` in its current form. These are targeted for resolution in the v2.x refactoring effort.

---

## 1. ~~No Automatic Type Conversion in Constructor Path~~ ⚠️ Partially Resolved

**Impact:** ~~High~~ → Medium (remaining cases only)  
**Affects:** `CreateViaPrimaryConstructorWithSchema<T>`, `GeneralMaterializationSession<T>`

The compiled constructor delegates use direct `Expression.Convert` casts. When source values arrive as strings (e.g., from YAML, CSV without pre-conversion), the cast fails with `InvalidCastException`.

> **v1.0.1 Fix (NET-011):** Pre-conversion logic added before compiled delegates. Now handles:
> - **Int64 → Enum** via `Enum.ToObject()` (common from Snowflake/Postgres)
> - **Cross-numeric** (e.g., Int64 → Int32) via `Convert.ChangeType()`
> - Applied in all three paths: `CtorMaterializationSession.Create`, `CreateViaPrimaryConstructorWithSchema`, and `ConvertObject` (GeneralSession)

**Remaining gap:** String-to-primitive conversion still requires caller-side pre-conversion (e.g., YAML's `ConvertYamlValues<T>()`).

**v2.x Goal:** Full string-to-primitive parsing in the compiled expression tree.

---

## 2. Constructor Resolution Relies on Parameter Count Heuristic 

**Impact:** Medium  
**Affects:** `ConstructorHelper<T>.PrimaryCtor`

The "primary constructor" is identified by selecting the constructor with the most parameters. This works for standard C# records but can fail for:
- Types with multiple constructors of equal parameter count
- Types where the copy constructor has more parameters than the intended data constructor
- Types with explicit secondary constructors

**v2.x Goal:** Use `[ConstructorAttribute]` or convention-based resolution (e.g., prefer constructors matching schema names).

---

## 3. ~~No Support for Nested Object Materialization~~ ⚠️ Partially Resolved

**Impact:** ~~Medium~~ → Low (remaining cases only)  
**Affects:** Record path in `Read.Yaml.cs`, flat-schema scenarios

> **v1.0.1 Fix (NET-010):** Recursive nested type construction from flat schema columns. When a constructor parameter doesn't match any schema column but its type has a constructor whose parameters DO match schema columns, the materializer now recursively constructs the nested type.
>
> ```csharp
> // This now works with flat schema:
> public record GroupKey(bool IsActive, string Region);
> public record GroupResult(GroupKey Key, int Count);
>
> var result = ObjectMaterializer.Create<GroupResult>(
>     schema: new[] { "IsActive", "Region", "Count" },
>     values: new object[] { true, "US-West", 42 }
> );
> // Key is auto-constructed from flat columns ✅
> ```
>
> Applied in both hot path (`CtorMaterializationSession.BuildNestedFactory`) and cold path (`TryConstructNested`).

**Remaining gap:** YAML nested mapping deserialization (`Dictionary<object, object>` → nested type) still requires mutable classes.

**v2.x Goal:** Dictionary-to-nested-type recursive materialization for YAML/JSON dictionary inputs.

---

## 4. ~~Schema Matching is Case-Insensitive but Not Fuzzy~~ ✅ Resolved

**Status:** **No longer a limitation** as of v1.2.x.

`SchemaMemberResolver` implements a **5-pass resolution pipeline**:

| Pass | Strategy | Example |
|------|----------|---------|
| 1 | Exact (case-sensitive) | `Name` → `Name` |
| 2 | Case-insensitive | `name` → `Name` |
| 3 | Normalized (snake_case, camelCase, lowercase) | `first_name` → `FirstName` |
| 4 | Resemblance (prefix/suffix/contains) | `CustomerName` → `Name` |
| 5 | Levenshtein (≤2 edits) | `Nmae` → `Name` |

> **v1.2.1 Fix (NET-005):** `computeSchemaDict()` now auto-detects case-variant properties. If the schema has entries differing only by case (e.g., `Name`, `name`, `NAME`), it switches from `OrdinalIgnoreCase` to `Ordinal` to preserve distinct mappings.

---

## 5. No Nullable<T> Aware Construction

**Impact:** Low  
**Affects:** Constructor parameter binding

When a constructor parameter is `int?` and the source value is an empty string or null, the materializer may attempt `Convert.ChangeType` which throws instead of returning `null`.

**v2.x Goal:** Special-case `Nullable<T>` in the expression tree compilation to handle null/empty inputs gracefully.

---

## 6. No Async Materialization Path

**Impact:** Low (performance edge case)  
**Affects:** Streaming scenarios

`ObjectMaterializer.Create<T>()` is synchronous. In high-throughput async streaming pipelines, the synchronous constructor compilation and reflection can cause thread pool starvation under extreme load.

**v2.x Goal:** Evaluate whether async factory delegates are beneficial, or if the current approach is sufficient with the existing caching.

---

## Summary Table

| # | Limitation | Impact | Status |
|---|-----------|--------|--------|
| 1 | ~~No auto type conversion in ctor path~~ | ⚠️ Partial | v1.0.1: Int64→Enum + cross-numeric fixed (NET-011) |
| 2 | Constructor heuristic by param count | Medium | ✅ Works for standard records |
| 3 | ~~No nested object materialization~~ | ⚠️ Partial | v1.0.1: Flat-schema→nested-record fixed (NET-010) |
| 4 | ~~No fuzzy schema matching~~ | ✅ Resolved | 5-pass fuzzy matching implemented |
| 5 | Nullable\<T\> handling gaps | Low | ⚠️ Partial |
| 6 | No async path | Low | ✅ Not needed for most cases |
