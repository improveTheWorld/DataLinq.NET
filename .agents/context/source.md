# Source Code Map

## Solution Location
`src/DataLinq.NET.sln`

## Git Repository Root
> **âš ï¸ The `.git` folder is inside `src/`**, not at the workspace root.
> All git commands must target `c:\CodeSource\DataLinq` (or use `git -C src`).
> Example: `git -C c:\CodeSource\DataLinq status`

## Package Scope

> **DataLinq.NET package includes 9 projects.** Other projects are for future products or internal use.

### In Package (DataLinq.NET)

| Layer | Project | Purpose |
|-------|---------|---------|
| Data | `DataLinq.Data.Read` | CSV, JSON, YAML readers |
| Data | `DataLinq.Data.Write` | File writers |
| Framework | `DataLinq.Framework.ParallelAsyncQuery` | Parallel async execution (main: `ParallelAsyncQuery.cs` + `SelectManyParallelAsyncQuery.cs`) |
| Framework | `DataLinq.Framework.AsyncEnumerable` | Async stream utilities |
| Framework | `DataLinq.Framework.Guard` | Input validation |
| Extensions | `DataLinq.Extensions.EnumerableExtensions` | IEnumerable LINQ |
| Extensions | `DataLinq.Extensions.AsyncEnumerableExtensions` | IAsyncEnumerable LINQ |
| Extensions | `DataLinq.Extensions.ParallelQueryExtensions` | ParallelQuery LINQ |
| Extensions | `DataLinq.Extensions.ParallelAsyncQueryExtensions` | ParallelAsyncQuery LINQ |

### NOT in Package (Future / Internal)

| Project | Purpose | Future Product |
|---------|---------|----------------|
| `DataLinq.Logger` | Logging framework | **SpyCloud** |
| `DataLinq.Framework.Syntaxi` | Custom file parser | **Custom DSL** |
| `DataLinq.Extensions.FileSystemExtensions` | File utilities | Internal |
| `DataLinq.Extensions.StringExtensions` | String utilities | Internal |
| `DataLinq.Extensions.ArrayExtensions` | Array utilities | Internal |
| `DataLinq.Extensions.IntExtensions` | Int utilities | Internal |
| `DataLinq.Extensions.DictionaryExtensions` | Dictionary utilities | Internal |
| `DataLinq.Framework.RegexTokenizer` | Tokenizer | Internal |
| `DataLinq.Framework.UnixStyleArgs` | CLI parsing | Internal |
| `DataLinq.Framework.WatchedValue` | Observable wrapper | Internal |
| `DataLinq.Framework.EnumerablePlus` | Extended enums | Internal |
| `DataLinq.Framework.ObjectMaterializer` | Object creation | Indirect (via Data.Read) |
| `DataLinq.Extensions.RegexTokenizerExtensions` | Tokenizer ext | Internal |

## Examples & Test Utilities

| Component | Path | Purpose |
|-----------|------|---------| 
| **UsageExamples** | `tests/IntegrationTests/DataLinq.UsageExamples/` | Runnable usage examples |
| **Logger Examples** | `tests/IntegrationTests/DataLinq.Logger.UsageExamples/` | Logging examples |

## Special Directories

| Directory | Purpose |
|-----------|---------|
| `src/archive/` | **Recycle bin** — deprecated/deleted code kept for reference |
| `scripts/` | Automation scripts (`fix_encoding.py`, Spark PS1 scripts) |
| `docs/bugs/` | Bug registry — `NET-NNN.md` files + auto-generated `README.md` |

> **Note:** `sync_bug_registry.py` lives in **Enterprise repo** (`DataLinq.Enterprise/scripts/`).
> The `/quality-cycle` workflow references it — needs an OSS-adapted copy in `scripts/`.

---

## DataLinq.Enterprise Repository

> **Location:** `c:\CodeSource\DataLinq.Enterprise` (PRIVATE repo)
> **Solution:** `DataLinq.Enterprise.sln`
> **Structure:** `snowflake/`, `spark/`, `shared/`, `.archive/`
> **Full context:** See [Enterprise .agents/index.md](file:///c:/CodeSource/DataLinq.Enterprise/.agents/index.md) for up-to-date details.

### Source Projects (`src/`)

| Project | Location | Version |
|---------|----------|---------|
| `DataLinq.Framework.SnowflakeQuery` | `snowflake/src/` | v1.4.0 |
| `DataLinq.Framework.SparkQuery` | `spark/src/` | v1.3.0 |
| `DataLinq.Extensions.SparkQueryExtensions` | `spark/src/` | - |
| `DataLinq.Licensing` | `shared/src/` | - |
| `DataLinq.Spark.Analyzers` | `spark/src/` | - |

### Key Snowflake Source Files

| File | Size | Purpose |
|------|------|---------|
| `SnowflakeQuery.cs` | 73KB | Query builder, SQL translator, materialization |
| `Write.Snowflake.cs` | 56KB | PUT+COPY INTO streaming, MERGE |
| `SnowflakeQueryCasesExtensions.cs` | 17KB | Cases pattern (multi-output routing) |
| `Snowflake.cs` | 5KB | Entry point factory (`Snowflake.Connect`) |
| `SnowflakeReadBuilder.cs` | 5KB | Read API (`ctx.Read.Table<T>`) |

### Dependencies (Snowflake csproj)

- `Snowflake.Data` v5.2.1 · `DataLinq.NET` v1.2.0 · `DataLinq.Licensing` (bundled)

### Test Projects (`tests/`)

| Project | Type | Location |
|---------|------|----------|
| `DataLinq.SnowflakeQuery.Tests` | UT | `snowflake/tests/` |
| `DataLinq.SnowflakeQuery.IntegrationTests` | Integration | `snowflake/tests/` |
| `SnowflakePackageIntegrationTests` | Package Integ | `snowflake/tests/` |
| `DataLinq.SparkQuery.UnitTests` | UT | `spark/tests/` |
| `DataLinq.SparkQuery.IntegrationTests` | Integration | `spark/tests/` |
| `DataLinq.Licensing.Tests` / `.IntegrationTests` | UT+Integration | `shared/tests/` |

### Package Output

- `nupkgs/DataLinq.Spark.1.2.1.nupkg` · `nupkg/DataLinq.Snowflake.1.2.0.nupkg`

### Key Docs (`docs/`)

- `snowflake/docs/LINQ-to-Snowflake-Status-and-Roadmap.md.md` — Full evaluation (~90% LINQ, Grade A)
- `shared/docs/Licensing.md` / `LicenseGuide.md` — License model
- `spark/docs/Spark-Distributed-Execution.md` / `SparkQuery-Internal-Architecture.md`

> **Public-facing docs** for Snowflake/Spark live in the DataLinq (public) repo:
> `src/docs/LINQ-to-Snowflake.md`, `LINQ-to-Snowflake-Capabilities.md`, `LINQ-to-Spark.md`

### Tools

- `tools/LicenseGenerator/` — CLI for RSA-signed license generation




