# {Project Name} — Project Documentation Hub

All documentation lives here, **outside Unity Assets**, version-controlled alongside the codebase.

## Folder Structure

```
Docs/
├── GDD/                    # Game Design Document + AI context files
│   ├── _TEMPLATE.md        # Copy to GDD.md — fill in your game's details
│   └── AI-Context/         # Pre-built context snippets for AI agents
│       ├── project-context-brief.md  # ⭐ YOU FILL THIS FIRST
│       ├── project-stack.md          # Auto-derived from GDD
│       └── feature-prompt-template.md
│
├── ADRs/                   # Architecture Decision Records
│   ├── _TEMPLATE.md        # Copy this to create new ADRs
│   └── 001-*.md            # Numbered decisions
│
├── Specs/                  # Design specs and architecture plans (agent output)
│   └── README.md           # Naming conventions and lifecycle
│
├── TestPlans/              # QA test plans (agent output)
│   └── README.md           # Naming conventions and lifecycle
│
├── DevLog/                 # Git-ready development logs
│   ├── _TEMPLATE.md        # Copy this for each session
│   └── YYYY-MM-DD-*.md    # One per work session
│
├── Reports/                # Auto-generated PM reports
│   └── _TEMPLATE.md        # Sprint report format
│
├── AgentPrompts/           # System prompts for each AI agent role
│   ├── game-design-agent.md
│   ├── architect-agent.md
│   ├── implementation-agent.md
│   ├── code-review-agent.md
│   ├── qa-agent.md
│   └── pm-report-agent.md
│
├── .agent/workflows/       # IDE workflow automation
│   ├── bootstrap-project.md  # ⭐ Run FIRST on new projects
│   ├── implement-feature.md
│   ├── fix-bug.md
│   ├── code-review.md
│   └── refactor.md
│
├── .github/                # GitHub templates and CI/CD
│   ├── PULL_REQUEST_TEMPLATE.md
│   ├── COMMIT_CONVENTION.md    # Conventional commit guide
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md
│   │   └── feature_request.md
│   └── workflows/
│       └── sprint-report.yml
│
├── scripts/                # Automation scripts (CI/CD helpers)
│   └── format_sprint_report.py
├── CHANGELOG.md            # Keep a Changelog format
├── RULES_AND_POLICY.md     # AI rules, hierarchy, optimization, token budget policy
├── multi_agent_workflow_design.md  # Multi-agent maturity ladder (Agentic AI Project)<<< This File is Only for Agent Reference (Not to edit and use in project directly)
└── README.md               # This file
```

## How to Use

0. **⭐ Bootstrap (once per project)** → Fill `GDD/AI-Context/project-context-brief.md`, then run `/bootstrap-project`
1. **GDD is now filled** → AI generated `GDD/GDD.md` + `project-stack.md` from your brief
2. **Before coding** → Read the relevant GDD `@tag:` section + existing ADRs
3. **Before deciding architecture** → Check existing ADRs, write a new one from `ADRs/_TEMPLATE.md`
4. **After each session** → Write a DevLog entry from `DevLog/_TEMPLATE.md`
5. **Weekly** → DevLogs feed into PM reports using `Reports/_TEMPLATE.md`
6. **Set AI rules** → Copy `RULES_AND_POLICY.md` into your AI tool's config (`.cursorrules`, `CLAUDE.md`, etc.)
7. **Token budgets** → All agents load only `project-stack.md` + relevant `@tag:` section (~1,300 tokens vs ~5,600)

## Context Policy

> All `.md` files in this system get context **ONLY** from `GDD/GDD.md` and its `@tag:` markers.
> See `RULES_AND_POLICY.md` for the full context hierarchy and agent behavior rules.
