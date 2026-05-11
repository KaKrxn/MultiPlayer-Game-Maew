# Agent System Prompts

> **Ready-to-use system prompts for each agent role in the multi-agentic workflow.**

## Prerequisites

> [!IMPORTANT]
> **Run `/bootstrap-project` first.** These prompts reference `Docs/GDD/AI-Context/project-stack.md` which is empty until you fill in the project context brief and run the bootstrap workflow.

## How to Use

1. **Run `/bootstrap-project`** to fill in your GDD and project-stack (one-time setup)
2. **Load `project-stack.md`** (~400 tokens) as the universal context for every agent call
3. **Load the agent prompt** for the current role (not all agents at once — see `RULES_AND_POLICY.md` §8)
4. **Load the relevant GDD `@tag:` section** for the task (not the full GDD)

## Agent Roster

| Agent | File | Input | Output | Saves To |
|-------|------|-------|--------|----------|
| 🎮 Game Designer | [game-design-agent.md](game-design-agent.md) | Feature request or GDD section | Design spec with juice/feel parameters | `Docs/Specs/` |
| 🏗️ Architect | [architect-agent.md](architect-agent.md) | Design spec | Technical plan, class diagrams, ADR | `Docs/Specs/` |
| 💻 Implementer | [implementation-agent.md](implementation-agent.md) | Architecture spec + design spec | Production C# code | `Assets/` |
| 👀 Reviewer | [code-review-agent.md](code-review-agent.md) | Git diff or code file | Review with actionable feedback | PR comment |
| 🧪 QA | [qa-agent.md](qa-agent.md) | Feature spec + implementation | Test plan + edge cases | `Docs/TestPlans/` |
| 📋 PM Reporter | [pm-report-agent.md](pm-report-agent.md) | Git log + DevLogs | Formatted sprint report | `Docs/Reports/` |

## Pipeline Order

```
GDD @tag:{section}
  └──► 🎮 Game Design Agent → design spec (Docs/Specs/)
         └──► 🏗️ Architect Agent → technical plan + ADR (Docs/Specs/)
                └──► 💻 Implementation Agent → code on feature branch
                       └──► 👀 Code Review Agent → feedback
                              └──► 🧪 QA Agent → test plan (Docs/TestPlans/)
                                     └──► 📋 PM Agent → sprint report (Docs/Reports/)
```

## Context Loading (Token-Optimized)

```
Per agent call:
  1. Load project-stack.md            (~400 tokens — universal bootstrap)
  2. Load GDD @tag:{relevant-section} (~200-500 tokens — task-specific)
  3. Load the agent prompt            (~700 tokens — role-specific)
  ──────────────────────────────────
  TOTAL: ~1,300-1,600 tokens per call (vs ~5,600 before optimization)
```

For framework-based pipelines (CrewAI, AutoGen), load the `.md` file as the agent's `backstory` or `system_message`.
