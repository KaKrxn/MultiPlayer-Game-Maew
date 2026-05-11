# Test Plans

> **Output directory for the QA Agent.**

## Naming Convention

| Type | Pattern | Example |
|------|---------|---------|
| Test Plan | `{feature-name}-test-plan.md` | `parry-timing-test-plan.md` |
| Regression Report | `{feature-name}-regression.md` | `inventory-refactor-regression.md` |

## Lifecycle

1. **QA Agent** reads the design spec + implementation → writes a test plan → saves here
2. Developer (human) executes the test plan during playtesting
3. Failed tests become bug reports (`.github/ISSUE_TEMPLATE/bug_report.md`)
4. After feature ships, test plans remain as regression reference

## Context Loading Rule

> Agents should load **only** the test plan relevant to their current task, not the entire directory.
> See `RULES_AND_POLICY.md` §8 for token budget policy.
