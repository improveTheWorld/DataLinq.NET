# Test Organization

## Testing Standard (DataLinq.NET / Spark / Snowflake)

All packages follow a **3-tier test strategy**:

| Tier | Type | Location |
|------|------|----------|
| 1 | **Unit Tests** | `src/UnitTests/` |
| 2 | **Code Integration** | `src/IntegrationTests/` |
| 3 | **Package Integration** | `src/PackageTests/` |

## Known Bug Test Policy

> **âš ï¸ CRITICAL RULE: No `Skip` in the test suite â€” EVER. Bugs must FAIL visibly.**

- Bug-capturing tests **must be active and failing** — never hidden with `Skip`
- Each bug test must have a comment referencing the bug registry: `// BUG: NET-001 - CSV quoted fields RFC 4180`
- Bug registry lives in `src/docs/bugs/` with one file per bug
- When a bug is fixed: the test turns green, update bug status to `✅ Fixed`
- Discover all known bugs: `grep -rn "// BUG:" src/UnitTests/ src/IntegrationTests/`

> **âš ï¸ NEVER manually edit `src/docs/bugs/README.md`** â€” it is auto-generated.
> Always use: `python src/scripts/sync_bug_registry.py`
>
> **Workflow for new bugs:**
> 1. Add `// BUG: NET-XXX - description` annotation to the failing test's `[Fact]` line
> 2. Create `src/docs/bugs/NET-XXX.md` with the bug report (severity, status, description)
> 3. Run `python src/scripts/sync_bug_registry.py` to regenerate the README

### Fixed Bugs
| Bug | Component | Fix |
|-----|-----------|-----|
| NET-001 | ParallelAsyncQuery | `BuildCombinedCts` applies `OperationTimeout` via `CancelAfter()` in all query classes |
| NET-002 | ParallelAsyncQuery | `WithCancellation()` links tokens instead of replacing; explicit `ThrowIfCancellationRequested()` in source loops |
| NET-003 | ObjectMaterializer | Constructor-only types now handled via `CreateViaPrimaryConstructorWithSchema` |
| NET-005 | ObjectMaterializer | `computeSchemaDict()` auto-detects case-variant duplicates → switches to `Ordinal` comparer |
| NET-006 | Data.Read (YAML) | Dictionary→ObjectMaterializer bridge for record types |

| NET-007 | UnifiedStream | Mutation guard: eager `_frozen` in `GetAsyncEnumerator` wrapper vs lazy iterator body |
| NET-008 | ObjectMaterializer | `BuildSchemaAction` + `CreateViaPrimaryConstructorWithSchema` now route through `SchemaMemberResolver.ResolveSchemaToMembers<T>()` |
| NET-009 | Data.Write | `WriteJsonSync(path)`, `WriteJson(IEnumerable, path)`, `WriteJsonSync(stream)` now check `options.JsonLinesFormat` |

## Limitation Test Policy

> **âš ï¸ CRITICAL RULE: Limitation tests must FAIL until the limitation is fixed â€” same as bug tests.**

- Limitation tests assert the **correct expected behavior** (e.g., "Union generates UNION ALL SQL")
- While the limitation exists, the test **FAILS** (because the feature doesn't work yet)
- When the limitation is fixed, the test **turns green** → remove from limitations file, update docs
- This is the **same philosophy as bug tests**: red until resolved, green = proof of fix
- **NEVER** write limitation tests that assert `ThrowsAny<Exception>` — that inverts the signal

## Flaky Test Policy

> **Flaky tests are NOT skipped — they are tagged and run separately.**
> **Always run flaky tests individually before reporting a failure.**

### Known Flaky Tests (batch-only failures, pass individually)

| Test | Suite | Root Cause |
|------|-------|------------|
| `Create_WithReflectionComparison_ShouldBeFaster_Session` | Core (Perf) | Timing-dependent |
| `CreateGeneralSession_WithReflectionComparison_ShouldBeFaster` | Core (Perf) | Timing-dependent |
| `Create_MemoryAllocation_ShouldBeMinimal` | Core (Perf) | GC nondeterminism |
| `Create_CacheEfficiency_ShouldBeEvident` | Core (Perf) | Timing-dependent |
| `Create_SchemaActionCaching_ShouldImprovePerformance` | Core (Perf) | Timing-dependent |
| `CreateFeedSession_MassiveRowCounts_ShouldBeEfficient` | Core (Perf) | Memory pressure |
| `Fix8_PerformanceRegression_ShouldRemainFast` | Core (Fix) | Timing-dependent |
| `SingleRoot_LargeObject_Validation_Buffering_Works` | Data.Tests | JSON buffering race |
| `Progress_TimeGated_FiresEvenWithLargeIntervalsOfRecords` | Data.Tests | Timer resolution |

### Exclusion Rules
- Performance tests tagged with `[Trait("Category", "Performance")]`
- Exclude from default CI: `dotnet test --filter "Category!=Performance"`
- Run perf tests in isolation: `dotnet test --filter "Category=Performance"`
- If a test fails in batch, **always re-run it individually** before investigating
- If it passes individually → flaky (batch contention). If it fails individually → real regression

## Performance Baseline Tracking

- Baselines stored in `src/docs/perf-baselines/{version}.json`
- Capture at each release: run perf tests 5× in isolation, record results
- Compare against previous release — flag regression >20%
- Current baseline: `v1.2.1.json` (6/6 pass, Feb 7, 2026)

## Test Projects Location
`src/UnitTests/`

## Test Projects

| Project | Path | Coverage |
|---------|------|----------|
| **Core Tests** | `src/UnitTests/DataLinq.Core.Tests/` | Extensions, Cases pattern |
| **Data Tests** | `src/UnitTests/DataLinq.Data.Tests/` | CSV, JSON, YAML readers |
| **Write Tests** | `src/UnitTests/DataLinq.Data.Write.Tests/` | File writers |
| **ParallelAsync Tests** | `src/UnitTests/DataLinq.ParallelAsyncQuery.Tests/` | Parallel async processing |
| **Cases Overload Tests** | `tests/UnitTests/DataLinq.CasesOverloadTests/` | Cases pattern variations |

## Running Tests

```bash
# Run ALL tests (Tier 1)
dotnet test src/DataLinq.NET.sln

# Run specific test project
dotnet test src/UnitTests/DataLinq.Core.Tests/DataLinq.Core.Tests.csproj

# Run with verbose output
dotnet test src/DataLinq.NET.sln --logger "console;verbosity=detailed"

# Run with coverage (requires coverlet)
dotnet test src/DataLinq.NET.sln --collect:"XPlat Code Coverage"
```

## Test Statistics
- **Total tests**: 868 (404 Core + 110 ParallelAsyncQuery + 298 Data + 49 Write + 5 Cases + 2 Skip)
- **Pass rate**: 99.5% (9 flaky tests fail in batch, pass individually — see Flaky Test Policy)
- **Code coverage**: 60% (weighted by line count)
- **Coverage report**: [COVERAGE.md](file:///c:/CodeSource/DataLinq/docs/COVERAGE.md)

## 🔜 Pending: Increase Unit Test Coverage

> **Goal:** Strategically improve coverage by targeting low-coverage package code.

**Low Coverage Packages (in DataLinq.NET):**

| Package | Coverage | Priority |
|---------|----------|----------|
| `DataLinq.Framework.Guard` | 0% | 🔴 High |
| `DataLinq.Extensions.ParallelQueryExtensions` | 0% | 🔴 High |
| `DataLinq.Extensions.ParallelAsyncQueryExtensions` | 12% | 🟠 Medium |
| `DataLinq.Extensions.EnumerableExtensions` | 50% | 🟡 Low |
| `DataLinq.Extensions.AsyncEnumerableExtensions` | 50% | 🟡 Low |

**Approach:**
1. Run `reportgenerator` to generate detailed HTML coverage report
2. Identify uncovered methods in each low-coverage package
3. Add tests for critical public APIs first
4. Target 70%+ coverage for all in-package projects

## Usage Examples (Integration Tests)

| Project | Path | Purpose |
|---------|------|---------|
| **DataLinq Examples** | `tests/IntegrationTests/DataLinq.UsageExamples/` | End-to-end usage demonstrations |
| **Logger Examples** | `tests/IntegrationTests/DataLinq.Logger.UsageExamples/` | Logging integration examples |
| **API Audit Tests** | `tests/IntegrationTests/DataLinq.AuditTest/` | YAML security, JSON guards, Write API, boundary conditions |

```bash
# Run usage examples (Tier 2)
dotnet run --project tests/IntegrationTests/DataLinq.UsageExamples
```

## Enterprise Test Infrastructure

### Snowflake Credentials
> **Real Snowflake account** configured in `settings.json` / `settings.local.json`.
> Always used for integration tests and package integration tests.

### Test Licenses
> ✅ **Pre-generated 2-month keys** stored as constants in `TestLicenseGenerator.cs`.
> Expires **2026-04-08**. Rotation: re-run CLI commands in code comments, update constants, commit.
> Fixtures use `TestLicenseGenerator.ActivateSnowflakeTestLicense()` / `ActivateSparkTestLicense()`.

### Enterprise Test Projects (after reorg)

| Project | Location | Type |
|---------|----------|------|
| **Snowflake UT** | `snowflake/tests/DataLinq.SnowflakeQuery.Tests/` | Unit |
| **Snowflake IT** | `snowflake/tests/DataLinq.SnowflakeQuery.IntegrationTests/` | Integration (real SF) |
| **Snowflake Pkg** | `snowflake/tests/SnowflakePackageIntegrationTests/` | Package |
| **Spark UT** | `spark/tests/DataLinq.SparkQuery.UnitTests/` | Unit |
| **Spark IT** | `spark/tests/DataLinq.SparkQuery.IntegrationTests/` | Integration (real Spark) |
| **Spark Pkg** | `spark/tests/SparkPackageAudit/` | Package |
| **Licensing** | `shared/tests/DataLinq.Licensing.Tests/` | Unit |
| **Licensing IT** | `shared/tests/DataLinq.Licensing.IntegrationTests/` | Integration |


