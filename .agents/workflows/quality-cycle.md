---
description: Bug-driven quality cycle — from detection to release readiness
---

# /quality-cycle — Standard Quality Workflow

This workflow defines the mandatory process for bug detection, documentation, fixing, and release readiness.

> **âš ï¸ CRITICAL: This workflow must be followed for ALL bug fixes and audit responses. No exceptions.**

## Core Principle

> Tests that capture bugs or limitations must **FAIL** until the issue is resolved.
> When the fix lands, the test turns **GREEN** — that IS the proof of fix.
> **NEVER** write bug tests that pass while the bug exists (e.g., `ThrowsAny<Exception>`).

---

## Phase 1: Bug Detection

Bugs are discovered through one of two paths:

### Path A: During Development
1. A bug is found during coding, testing, or code review
2. Write a **failing unit test** that asserts the **correct expected behavior**
3. The test **FAILS** — this proves the bug exists
4. Add `// BUG: XX-NNN - short description` comment above the `[Fact]` attribute

### Path B: From an Audit Report
1. Receive or generate an audit report (e.g., package audit, edge-case stress test)
2. **Analyze the report** — extract each distinct bug finding
3. For each bug: write a **failing unit test** asserting the correct behavior
4. Add `// BUG: XX-NNN - short description` comment above the `[Fact]` attribute
5. **Check documentation suggestions** in the audit report
6. Apply any doc clarifications or corrections immediately

---

## Phase 2: Bug Registry

After adding bug tests:

// turbo
1. Run the bug registry script:
   ```
   python scripts/sync_bug_registry.py
   ```
2. The script scans all test files for `// BUG:` annotations
3. It auto-generates `README.md` in the bugs directory
4. **NEVER manually edit the bugs README** — it is auto-generated

---

## Phase 3: Bug Documentation

For each new bug ID (e.g., `SF-002`), create a detailed description file:

1. Create `docs/bugs/XX-NNN.md` with this template:

```markdown
# XX-NNN: [Short Title]

| Field | Value |
|-------|-------|
| **Product** | [DataLinq.Snowflake / DataLinq.Spark / DataLinq.NET] |
| **Severity** | [🔴 Critical / 🟡 Medium / 🟢 Low] |
| **Status** | 🔴 Open |
| **Found** | [Date] |
| **Fixed** | — |

## Description
[Detailed description of the bug]

## Steps to Reproduce
[How to trigger the bug]

## Expected Behavior
[What should happen]

## Actual Behavior
[What actually happens]

## Capturing Test
`[TestClassName.TestMethodName]`

## Fix
— (updated when fixed)
```

// turbo
2. Re-run `python scripts/sync_bug_registry.py` to pick up the new file
3. Commit: `git add -A && git commit -m "bug: add XX-NNN - [title]"`

---

## Phase 4: Bug Fixing

1. Fix bugs one at a time, starting with highest severity
2. After each fix:
   - The capturing test should now **PASS** (turn green)
   - Update the bug `.md` file: set **Status** to `✅ Fixed`, fill **Fixed** date and **Fix** section
   - Run `python scripts/sync_bug_registry.py` to update the registry
   - Run full test suite to confirm no regressions
3. Commit each fix separately: `git commit -m "fix: resolve XX-NNN - [title]"`

---

## Phase 5: Release Readiness

A version is **ready for release** when ALL of these are true:

| Check | Criteria |
|-------|----------|
| ✅ Bug registry | **Zero open bugs** — `🎉 No open bugs!` in README |
| ✅ Audit clean | Latest audit report has no unresolved findings |
| ✅ Doc accuracy | All doc clarifications from audit applied |
| ✅ Tests green | All non-limitation tests pass |
| ✅ Limitation tests | Failing as expected (documented limitations only) |

> **The registry being empty is the signal that the version is release-ready.**

---

## Quick Reference

| Action | Command |
|--------|---------|
| Find all bugs | `grep -rn "// BUG:" tests/` |
| Regenerate registry | `python scripts/sync_bug_registry.py` |
| Run all tests | `dotnet test` |
| Check readiness | Open bugs README — must show "No open bugs!" |

