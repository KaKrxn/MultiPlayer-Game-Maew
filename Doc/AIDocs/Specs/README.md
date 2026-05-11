# Design Specs & Architecture Plans

> **Output directory for the Game Design Agent and Architect Agent.**

## Naming Convention

| Type | Pattern | Example |
|------|---------|---------|
| Design Spec | `{feature-name}-design-spec.md` | `parry-timing-design-spec.md` |
| Architecture Plan | `{feature-name}-arch-plan.md` | `parry-timing-arch-plan.md` |

## Lifecycle

1. **Game Design Agent** writes a design spec → saves here
2. **Architect Agent** reads the design spec → writes an architecture plan → saves here
3. **Implementation Agent** reads both → writes code
4. After merge, specs remain as historical reference (do not delete)

## Context Loading Rule

> Agents should load **only** the spec relevant to their current task, not the entire directory.
> See `RULES_AND_POLICY.md` §8 for token budget policy.
