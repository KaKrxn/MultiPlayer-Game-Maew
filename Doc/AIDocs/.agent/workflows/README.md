---
description: 
---

# Agent Workflows

> **IDE workflow automation files for AI-assisted game development.**
> Compatible with: Cursor rules, Claude Code `CLAUDE.md`, Gemini CLI, Windsurf, or any agent that reads `.md` workflows.

## Available Workflows

| Workflow | Trigger | Description |
|----------|---------|-------------|
| [implement-feature.md](implement-feature.md) | `/implement-feature` | Full pipeline: GDD → Design → Architecture → Code → Review |
| [fix-bug.md](fix-bug.md) | `/fix-bug` | Investigate → Fix → Regression check |
| [code-review.md](code-review.md) | `/code-review` | Review a file or diff against project conventions |

## How to Use

### In Cursor / Windsurf
Copy the workflow `.md` files into your project's `.cursorrules` or equivalent config directory.

### In Claude Code
Reference these workflows in your `CLAUDE.md` file:
```
See .agent/workflows/ for available workflow commands.
```

### In Any AI Chat
Paste the workflow steps directly into the conversation to guide the AI through a structured process.
