# DataLinq Known Bugs Registry

> **Auto-generated** by `scripts/sync_bug_registry.py`.
> Source of truth: `// BUG:` comments in test files.
> Discover all: `grep -rn "// BUG:" tests/`

## Open Bugs

| ID | Product | Severity | Title | Test Reference |
|----|---------|----------|-------|----------------|
| [NET-007](NET-007.md) | DataLinq.NET | 🟡 Medium | UnifiedStream Allows Mutation During Active Enumeration | `MutatingAfterEnumerationStarts_Throws` |
| [NET-010](NET-010.md) | DataLinq.NET | 🔴 High | Nested Record/Anonymous Type Reconstruction from Flat Schema | `Create_WithNestedRecord_ShouldReconstructFromFlatSchema`, `CreateCtorSession_WithNestedRecord_ShouldReconstructFromFlatSchema` |
| [NET-011](NET-011.md) | DataLinq.NET | 🔴 High | Int64 to Enum Conversion Fails with InvalidCastException | `Create_WithInt64ForEnum_ShouldConvertCorrectly`, `CreateCtorSession_WithInt64ForEnum_ShouldConvertCorrectly`, `CreateGeneralSession_WithInt64ForEnum_ShouldConvertCorrectly` |
| [NET-008](NET-008.md) | DataLinq.NET | ❓ Unknown | snake_case Schema Normalization Silently Failed | ⚠️ No capturing test found |
| [NET-009](NET-009.md) | DataLinq.NET | ❓ Unknown | JsonLinesFormat Option Had No Effect | ⚠️ No capturing test found |

## Fixed Bugs

| ID | Product | Fixed In | Title |
|----|---------|----------|-------|
| [NET-001](NET-001.md) | DataLinq.NET | ✅ Fixed in v1.2.1 | WithTimeout Not Enforced During Parallel Execution |
| [NET-002](NET-002.md) | DataLinq.NET | ✅ Fixed in v1.2.1 | Combined Cancellation Tokens Not Honored in ParallelAsyncQuery |
| [NET-003](NET-003.md) | DataLinq.NET | ✅ Resolved (Not a Bug) | ObjectMaterializer Error Message Not Detailed for Missing Constructor |
| [NET-005](NET-005.md) | DataLinq.NET | 🔴 Fixed in v1.2.1 | Schema Dictionary Is Case-Insensitive — Cannot Map Case-Variant Properties |
| [NET-006](NET-006.md) | DataLinq.NET | ✅ Fixed | YAML List Deserialization Fails for Record Types |
