# DataLinq.Spark v1.2.1
**Release Date: April 2026**

This is a high-priority stability patch focusing exclusively on the underlying UDF execution engine and `ForEachCase` Delta Reflection mechanics. It hardens the closure behaviors introduced in v1.2.0 without introducing any breaking API changes.

## 🛡️ Critical Fixes & Hardening

* **UDF Closure Sanitization**: Completely eliminated compiler `DisplayClass` contamination during distributed `ForEach` execution, ensuring deterministic memory space isolation across Spark workers.
* **Singleton Delta Sync (BUG-001)**: Fixed an edge case where static `<>c` (compiler-generated singletons) were improperly classified as stateful closures during delta reflection, causing sync failures on static field updates.
* **ForEachCase State Propagation**: Fixed a regression in complex topology chains. `PostExecutionSync` delegates are now properly forwarded through `AllCases` and `UnCase` boundaries, ensuring 100% data integrity when chaining multiple categorization pipelines.
* **Integration Reliability**: Resolved 5 minor integration regressions related to context configurations in Spark Workers.

## 📦 Compatibility & Upgrade
This is a **drop-in, non-breaking upgrade** for all `1.2.x` users. No code changes are required.

## 📊 Test Suite Validation
As with all DataLinq releases, this patch passes the complete high-integrity audit suite with a **100% pass rate** across all automated tests (Unit, Integration, and Advanced Edge-Case Batches) covering UDF instances, IL translation constraints, and distributed state serialization.
