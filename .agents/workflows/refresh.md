---
description: Read AI context files to refresh understanding for current task
---

# /refresh - Refresh AI Context for Current Task

This workflow instructs the AI to **read** the existing context files to refresh its understanding of the project, focusing on what's relevant to the current task or conversation.

> **Primary action**: Read context files. 
> **Secondary action**: If gaps are found, enrich the context files with missing information.

## When to Use

Invoke `/refresh` when:
- Starting work on a new area of the codebase
- The AI seems to have forgotten project context
- You want the AI to re-read architecture patterns before making changes
- The conversation has drifted and you want to re-anchor on project knowledge

## Execution Steps

### Step 1: Read the Index

Read `.agent/index.md` to get the project overview and navigation map.

### Step 2: Identify Relevant Context

Based on the current conversation topic, determine which context files are relevant:

| If working on... | Read... |
|------------------|---------|
| Core components, patterns, abstractions | `context/architecture.md` |
| Navigating or modifying source code | `context/source.md` |
| Documentation tasks | `context/docs.md` |
| Running or writing tests | `context/tests.md` |
| Building, packaging, releasing | `context/packaging.md` |

### Step 3: Read Relevant Context Files

Read the identified context file(s) into memory.

### Step 4: Check for Custom Guidance

If `.agent/guidance.md` exists, read it for project-specific AI instructions.

### Step 5: Detect Gaps

While reading, check if the context files are missing information needed for the current task:
- Are there components mentioned that aren't documented?
- Are there missing commands or paths?
- Is there outdated information?

### Step 6: Enrich if Needed

**If gaps are detected:**
1. Research the missing information (explore the codebase)
2. Add the new information to the appropriate context file
3. Notify the user: "I noticed [X] was missing from context, so I added it."

**Examples of enrichment:**
- Task needs test info, but a new test project exists → Add to `tests.md`
- Working on packaging, but version location changed → Update `packaging.md`
- New documentation file exists → Add to `docs.md`

### Step 7: Confirm Readiness

After reading (and optionally enriching), briefly confirm to the user:
- What context was loaded
- What was added (if anything)
- Key points relevant to the current task

---

## Example Usage

```
User: I need to add a new extension method to ParallelAsyncQuery
User: /refresh

AI: [Reads index.md, context/architecture.md, context/source.md]
AI: [Notices DataLinq.Extensions.ParallelAsyncQueryExtensions has new files not documented]
AI: [Adds missing info to source.md]
AI: "I've refreshed my context and added 2 new extension files I found.
     Key points for this task:
     - ParallelAsyncQuery is in DataLinq.Framework.ParallelAsyncQuery
     - Extensions go in DataLinq.Extensions.ParallelAsyncQueryExtensions
     - Follow the existing LINQ extension pattern (lazy, composable)"
```

---

## Context Maintenance

### Resolve Contradictions
If you find contradictory information between what you know and what's in the context files:
1. Investigate to determine which is correct
2. Update the context file to reflect accurate information
3. Clarify ambiguous statements to prevent future confusion

### Manage File Size (Hierarchy)
If a context file becomes too large (>200 lines or hard to navigate):
1. Keep a **general summary** in the original file
2. Create **sub-files** for detailed topics (e.g., `context/source/extensions.md`)
3. Add links from the parent file to the sub-files
4. Update `index.md` to reflect the new structure

**Example:**
```
context/source.md (too big)
  ↓ Split into:
context/source.md (general overview + links)
context/source/data-layer.md
context/source/extensions.md
context/source/framework.md
```

---

## Notes

- Primary purpose is **reading** — enrichment is secondary
- Only add info directly relevant to the current task (don't do a full rescan)
- For **comprehensive** context regeneration, use `/update` instead
- The AI should synthesize and summarize what it learned, not just list files read

