---
description: Code review workflow using project conventions and RULES_AND_POLICY
---

# Code Review Workflow

## Steps

### 1. Load Context
- Read `Docs/RULES_AND_POLICY.md` for all project conventions
- Read `Docs/GDD/AI-Context/project-stack.md` for project context
- Check `Docs/ADRs/README.md` for relevant architecture decisions

### 2. Identify Scope
- What files/diff are being reviewed?
- What feature or fix does this implement?
- What GDD @tag section does it relate to?

### 3. Run the Review Checklist
Load `AgentPrompts/code-review-agent.md` and run through the **Review Checklist** section.

Key areas (see agent prompt for the full checklist):
- Architecture — ADR compliance, god-object check, state/presentation separation
- Naming & Conventions — `RULES_AND_POLICY.md` §2 compliance
- Performance — `RULES_AND_POLICY.md` §5 compliance, no hot-path allocations
- Hierarchy — `RULES_AND_POLICY.md` §4 compliance (if scene objects changed)
- Edge Cases — null safety, rapid input, scene transitions

### 4. Write Review
Use the format from `AgentPrompts/code-review-agent.md`:
- Summary (approve / request changes / block)
- Issues with severity (🔴 🟡 🔵 ⚪)
- Positive feedback

### 9. Done
Review is ready for the developer to act on.
