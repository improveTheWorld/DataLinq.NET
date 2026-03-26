# Architecture Context

## Core Patterns

### SUPRA Pattern
The foundational data processing model: **S**ink → **U**nify → **P**rocess → **R**oute → **A**pply.
- Data is buffered/normalized at edges (Sink)
- Multiple sources merged into single stream (Unify)
- Items flow one-by-one for constant memory (Process)
- Declarative branching replaces if/else (Route)
- Side effects applied at the end (Apply)

**Key file**: [DataLinq-SUPRA-Pattern.md](file:///c:/CodeSource/DataLinq/docs/DataLinq-SUPRA-Pattern.md)

### Cases Pattern
Declarative routing engine using `Cases()`, `SelectCase()`, `ForEachCase()`.
- Single-pass dispatch tree
- Replaces nested if/else blocks
- Composable with all LINQ operators

**Key file**: [Cases-Pattern.md](file:///c:/CodeSource/DataLinq/docs/Cases-Pattern.md)

## Layer Organization

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚         DataLinq.NET (Facade)       â”‚  â† User-facing package
├─────────────────────────────────────┤
â”‚     DataLinq.Data.Read/Write        â”‚  â† I/O layer
├─────────────────────────────────────┤
â”‚     DataLinq.Extensions.*           â”‚  â† LINQ extensions
├─────────────────────────────────────┤
â”‚     DataLinq.Framework.*            â”‚  â† Core infrastructure
└─────────────────────────────────────┘
```

## Key Abstractions

| Class | Purpose |
|-------|---------|
| `Read` | Static entry point for file readers (CSV, JSON, YAML) |
| `UnifiedStream<T>` | Merges multiple IAsyncEnumerable sources |
| `ParallelAsyncQuery<T>` | Parallel async processing wrapper |
| `ObjectMaterializer` | Zero-allocation object instantiation engine |

## YAML Record Deserialization (NET-006 Fix)

When `T` lacks a parameterless constructor (e.g., C# records), `Read.Yaml` uses a **Dictionary bridge**:
1. YamlDotNet deserializes to `Dictionary<string, object>`
2. `ConvertYamlValues<T>()` converts string values to constructor parameter types
3. `ObjectMaterializer.Create<T>(schema, values)` constructs via primary constructor

This is transparent — classes with parameterless constructors still use native YamlDotNet deserialization.

**Key limitation**: Nested objects in YAML are not recursively materialized for records (see `docs/ObjectMaterializer-Limitations.md`).

## ObjectMaterializer Schema Resolution (NET-005 Fix)

`MemberMaterializationPlan.computeSchemaDict()` uses **auto-detection** for case sensitivity:
- If schema has case-variant duplicates (e.g., `Name`, `name`, `NAME`) → `StringComparer.Ordinal`
- Otherwise → `StringComparer.OrdinalIgnoreCase` (ergonomic default)

Detection via `HasCaseVariantDuplicates()` — compares `HashSet` counts using both comparers.

Full schema resolution uses a **5-pass pipeline** in `SchemaMemberResolver.Resolve()` (exact → case-insensitive → normalized → resemblance → Levenshtein).

## UnifiedStream Lazy Iterator Pattern (NET-007 Fix)

C# async iterators (`async IAsyncEnumerator<T>` methods with `yield return`) are **lazy** — the method body doesn't execute until the first `MoveNextAsync()` call. This means any state initialization inside the iterator body (like `_frozen = true`) is deferred.

**Fix pattern**: Split into eager synchronous wrapper + lazy async core:
```csharp
public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct)
{
    _frozen = true;  // Runs immediately
    return GetAsyncEnumeratorCore(ct);  // Lazy body
}
private async IAsyncEnumerator<T> GetAsyncEnumeratorCore(CancellationToken ct) { /* yield return */ }
```


## ParallelAsyncQuery Cancellation Architecture (NET-001 / NET-002 Fix)

All query classes (`Source`, `Select`, `Where`, `Take`, `SelectMany`) use a unified `BuildCombinedCts()` helper:

```csharp
private CancellationTokenSource BuildCombinedCts(CancellationToken cancellationToken)
{
    // 1. Collect active tokens: _settings.CancellationToken + cancellationToken
    // 2. Link them via CancellationTokenSource.CreateLinkedTokenSource
    // 3. Apply _settings.OperationTimeout via CancelAfter()
    // → Always returns non-null CTS; caller must dispose
}
```

**Key design decisions:**
- `WithCancellation(newToken)` on base class **links** with existing settings token (not replace) — prevents silent token loss
- `SourceParallelAsyncQuery` includes explicit `ThrowIfCancellationRequested()` in iteration loops — handles sources without `[EnumeratorCancellation]`
- `OperationTimeout` enforced at **overall operation level** (via `CancelAfter()`) AND at **per-item level** (in `ProcessAndPostToChannelAsync` / `ProcessItemAsync`)

> **âš ï¸ Pitfall:** `ParallelAsyncQuery.WithCancellation()` shadows `IAsyncEnumerable.WithCancellation()` extension. It stores the token in settings instead of wrapping in `ConfiguredCancelableAsyncEnumerable`. Callers using `await foreach (... in query.WithCancellation(token))` go through the class method, NOT the standard extension.

## Technology Stack
- **.NET 8.0+** (LTS)
- **C# 12** with expression trees
- **IAsyncEnumerable<T>** as core abstraction
- **Premium**: Spark (.NET for Apache Spark), Snowflake (Snowflake.Data)

## Scripting Rules

> **âš ï¸ CRITICAL RULE: All scripts must be written in Python â€” NEVER PowerShell/Bash.**
> Python is more readable, portable across Windows/Linux/macOS, and maintainable.
> This rule applies to: automation scripts, build helpers, code generators, and any tooling.

## Async Bug Debugging Methodology

When debugging async cancellation/timeout bugs in `ParallelAsyncQuery`:
1. **Write a standalone trace test** (outside xUnit lifecycle) with `Console.WriteLine` timestamps
2. **Isolate variables**: test settings-only token vs both tokens vs timeout separately
3. **Check token propagation**: `WithCancellation()` on `ParallelAsyncQuery` replaces vs links tokens
4. **Check source support**: raw `IAsyncEnumerable` sources may ignore `[EnumeratorCancellation]`
5. **Trace the chain**: `Select` → `Source` → raw source — each level creates its own CTS




