# Reports

PM reports are generated here from DevLog entries and git log.

## How to Generate

### Manual (Today)
```bash
git log --since="2 weeks ago" --pretty=format:"- %s (%h, %an, %ar)" --no-merges > sprint_report_raw.txt
```
Feed the output to an AI agent with the prompt: _"Format this git log into a PM sprint report using the template in `Reports/_TEMPLATE.md`."_

### Automated (Future)
See `multi_agent_workflow_design.md` §5 for GitHub Actions automation setup.

## Naming Convention
`{YYYY-MM-DD}-sprint-report.md`
