# Documentation Map

## Location
All documentation is in `src/docs/`

## Core Concepts

| File | Topic |
|------|-------|
| [DataLinq-SUPRA-Pattern.md](file:///c:/CodeSource/DataLinq/docs/DataLinq-SUPRA-Pattern.md) | The SUPRA pattern (Sink-Unify-Process-Route-Apply) |
| [Cases-Pattern.md](file:///c:/CodeSource/DataLinq/docs/Cases-Pattern.md) | Declarative routing with Cases/SelectCase/ForEachCase |
| [ObjectMaterializer.md](file:///c:/CodeSource/DataLinq/docs/ObjectMaterializer.md) | Zero-allocation object instantiation engine |
| [ObjectMaterializer-Limitations.md](file:///c:/CodeSource/DataLinq/docs/ObjectMaterializer-Limitations.md) | Known limitations (v1.2.x) → v2.x refactoring targets |

## Data I/O

| File | Topic |
|------|-------|
| [DataLinq-Data-Reading-Infrastructure.md](file:///c:/CodeSource/DataLinq/docs/DataLinq-Data-Reading-Infrastructure.md) | CSV, JSON, YAML readers (comprehensive) |
| [DataLinq-Data-Writing-Infrastructure.md](file:///c:/CodeSource/DataLinq/docs/DataLinq-Data-Writing-Infrastructure.md) | File writers |
| [Materialization-Quick-Reference.md](file:///c:/CodeSource/DataLinq/docs/Materialization-Quick-Reference.md) | Quick guide for data class design |

## Parallel & Async

| File | Topic |
|------|-------|
| [Parallel-Processing.md](file:///c:/CodeSource/DataLinq/docs/Parallel-Processing.md) | Parallel processing overview |
| [ParallelAsyncQuery-API-Reference.md](file:///c:/CodeSource/DataLinq/docs/ParallelAsyncQuery-API-Reference.md) | ParallelAsyncQuery<T> API |
| [Polling-Buffering.md](file:///c:/CodeSource/DataLinq/docs/Polling-Buffering.md) | Data acquisition patterns |
| [Stream-Merging.md](file:///c:/CodeSource/DataLinq/docs/Stream-Merging.md) | UnifiedStream multi-source merging |

## Cloud/Premium

> **These are the official public-facing docs** for the commercial Snowflake/Spark products.
> The Enterprise repo is private, so these docs in the public DataLinq repo are what users see.
> **Must be updated on every Snowflake/Spark release.**

| File | Topic |
|------|-------|
| [LINQ-to-Spark.md](file:///c:/CodeSource/DataLinq/docs/LINQ-to-Spark.md) | LINQ-to-Spark translation |
| [LINQ-to-Snowflake.md](file:///c:/CodeSource/DataLinq/docs/LINQ-to-Snowflake.md) | LINQ-to-Snowflake provider |
| [LINQ-to-Snowflake-Capabilities.md](file:///c:/CodeSource/DataLinq/docs/LINQ-to-Snowflake-Capabilities.md) | Snowflake capabilities matrix |

## API Reference

| File | Topic |
|------|-------|
| [API-Reference.md](file:///c:/CodeSource/DataLinq/docs/API-Reference.md) | Complete API documentation |
| [Extension-Methods-API-Reference.md](file:///c:/CodeSource/DataLinq/docs/Extension-Methods-API-Reference.md) | LINQ extension methods matrix |
| [Architecture-APIs.md](file:///c:/CodeSource/DataLinq/docs/Architecture-APIs.md) | Architecture layer APIs |

## Project Info

| File | Topic |
|------|-------|
| [Roadmap.md](file:///c:/CodeSource/DataLinq/docs/Roadmap.md) | Future development plans |
| [COVERAGE.md](file:///c:/CodeSource/DataLinq/docs/COVERAGE.md) | Test coverage reports |
| [Benchmarks.md](file:///c:/CodeSource/DataLinq/docs/Benchmarks.md) | Performance benchmarks |

## Release Notes
Located in `src/docs/changelog/` and root docs folder:
- `DataLinq.NET_1.2.1.md` — YAML record fix, ObjectMaterializer case-sensitivity (NET-005), ParallelAsyncQuery timeout (NET-001) and cancellation (NET-002) fixes
- `RELEASE-NOTES-v1.0.2.md`
- `RELEASE-NOTES-v1.1.0.md`

## Bugs
Located in `src/docs/bugs/`:
- `NET-001.md` — WithTimeout not enforced in ParallelAsyncQuery (✅ Fixed v1.2.1)
- `NET-002.md` — Combined cancellation tokens not honored (✅ Fixed v1.2.1)
- `NET-003.md` — ObjectMaterializer constructor-only types (✅ Fixed v1.2.1)
- `NET-005.md` — Schema dictionary case-insensitive in ObjectMaterializer (✅ Fixed v1.2.1)
- `NET-006.md` — YAML record type deserialization fix (✅ Fixed v1.2.1)
- `NET-007.md` — UnifiedStream mutation during enumeration (✅ Fixed v1.2.1)
- `NET-008.md` — snake_case schema normalization silently failed (✅ Fixed v1.2.1)
- `NET-009.md` — JsonLinesFormat option had no effect on IEnumerable overloads (✅ Fixed v1.2.1)

## Audit Reports
Located in `tests/IntegrationTests/DataLinq.AuditTest/`:
- `AUDIT_REPORT.md` — v1.1.0 Spark package audit
- `AUDIT_REPORT2.md` — v1.2.0 final audit (5 valid doc discrepancies, 1 locale bug)
- `AUDIT_REPORT3.md` — v1.2.1 pre-fix audit (NET-008 snake_case, Sum ambiguity, doc issues)
- `AUDIT_REPORT4.md` — v1.2.1 final audit (134 tests, 133 pass, NET-009 JsonLinesFormat found)



