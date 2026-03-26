---
trigger: always_on
glob:
description: Context management rules for AI - auto-refresh on startup, auto-update on corrections
---

# Context Management Rules

## Startup Behavior

**On every new conversation:**
1. Check if `.agent/context/` folder exists
2. If YES → Execute `/refresh` (read indexed context silently)
3. If NO → Execute `/scan` (generate the context index first)

## Learning from Corrections

**When the user corrects you about:**
- How a component works
- Where something is located
- What a pattern or abstraction does
- Documentation that contradicts your understanding

→ Execute `/update` silently to persist that correction.

## Context Awareness

**Always remember:**
- You have an indexed context in `.agent/` that helps you find information
- Check `.agent/index.md` first when exploring the codebase
- Context files: `architecture.md`, `source.md`, `docs.md`, `tests.md`, `packaging.md`
- **Read `.agent/workflows/quality-cycle.md`** before any bug fix, audit response, or release work

## Silent Operations

When you auto-trigger `/refresh` or `/update` (not explicitly requested):
- Do it **silently** — don't announce or explain
- Focus on the user's actual task

## Available Commands

| Command | Action |
|---------|--------|
| `/refresh` | Read context files for current task |
| `/update` | Add new information to context locally |
| `/scan` | Regenerate all context files from scratch |
| `/quality-cycle` | **Read the quality workflow before any bug/audit/release work** |
