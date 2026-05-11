---
description: End-to-end feature implementation following the multi-agent pipeline
---

# Implement Feature Workflow

## Prerequisites
- `Docs/GDD/GDD.md` is filled in with your project's design
- `Docs/GDD/AI-Context/project-stack.md` is filled in with your tech stack
- `Docs/RULES_AND_POLICY.md` is in place

## Steps

### 1. Identify the GDD Section
Read the relevant `@tag:` section from `Docs/GDD/GDD.md` that motivates this feature.
- Quote the specific lines that define the intended behavior
- Note any experience pillars this feature must support

### 2. Check Existing ADRs
Read `Docs/ADRs/README.md` and check if any existing ADR governs this area.
- If yes, follow the ADR's decisions
- If the feature requires a new architecture decision, draft an ADR later in Step 5

### 3. Write the Design Spec (🎮 Game Design Agent Role)
Using `AgentPrompts/game-design-agent.md` as your guide, produce:
- Player experience description
- Interaction flow diagram
- Juice specification (screen shake, particles, audio, timing)
- Edge cases from player perspective
- GDD alignment check

**Save to:** `Docs/Specs/{feature-name}-design-spec.md`

### 4. Write the Architecture Plan (🏗️ Architect Agent Role)
Using `AgentPrompts/architect-agent.md` as your guide, produce:
- System diagram (mermaid)
- Interface definitions
- Class responsibility table
- Data flow
- Which existing scripts need modification

**Save to:** `Docs/Specs/{feature-name}-arch-plan.md`

### 5. Write ADR (if architecture changed)
Copy `Docs/ADRs/_TEMPLATE.md` → `Docs/ADRs/{NNN}-{kebab-title}.md`
- Document the decision, alternatives, and consequences
- Update `Docs/ADRs/README.md` registry

### 6. Create Feature Branch
```bash
git checkout -b feat/{feature-name}
```

### 7. Implement (💻 Implementation Agent Role)
Using `AgentPrompts/implementation-agent.md` as your guide:
- Follow the architecture spec exactly
- Follow `RULES_AND_POLICY.md` naming and optimization rules
- Follow hierarchy convention for any new scene objects
- Commit with conventional format: `feat(scope): description`

### 8. Self-Review (👀 Code Review Agent Role)
Using `AgentPrompts/code-review-agent.md`, review your own code for:
- Architecture violations
- Naming convention issues
- Performance concerns (no Update allocations, cached refs)
- Hierarchy convention compliance
- Edge cases

### 9. Write Test Plan (🧪 QA Agent Role)
Using `AgentPrompts/qa-agent.md`, create:
- Functional test cases
- Edge case tests
- Platform-specific tests
- Regression checklist

**Save to:** `Docs/TestPlans/{feature-name}-test-plan.md`

### 10. Write DevLog Entry
Copy `Docs/DevLog/_TEMPLATE.md` → `Docs/DevLog/{YYYY-MM-DD}-{feature}.md`
- Document what was done, key decisions, bugs found, game feel notes

### 11. Final Commit and PR
```bash
git add -A
git commit -m "feat(scope): description"
git push origin feat/{feature-name}
```
Create PR using `.github/PULL_REQUEST_TEMPLATE.md`

### 12. Done
Feature is ready for human review and merge.
