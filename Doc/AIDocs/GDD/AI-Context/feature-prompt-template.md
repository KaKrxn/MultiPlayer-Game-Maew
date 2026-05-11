# AI Feature Prompt Template

> **How to use:** Copy this template, fill in the blanks, paste into your AI coding assistant.
> This replaces freeform "hey can you make..." prompts with structured requests.

---

## 🔖 Project Context
_Attach or reference:_ `Docs/GDD/AI-Context/project-stack.md`

## 🎯 Feature Request

### What
_{One sentence: what should the player experience?}_

### GDD Reference
_{Which `@tag:` section of the GDD describes this? Quote the relevant lines.}_

### Why Now
_{Why is this the next priority? What does it unblock?}_

---

## 🏗️ Technical Scope

### Systems Affected
- [ ] Player Controller
- [ ] Inventory
- [ ] Combat / Threat
- [ ] Level / World
- [ ] UI / HUD
- [ ] Scene Management
- [ ] Economy / IAP
- [ ] Audio
- [ ] Networking
- [ ] Other: ___

### Existing Code to Read First
_{List the specific files the AI should read before writing anything:}_
- `Assets/...`
- `Assets/...`

### ADRs to Follow
- ADR-{NNN}: ...

---

## 🎮 Game Feel Requirements

### How Should It Feel?
_{Describe the intended player sensation in plain language:}_

### Juice Checklist
- [ ] Screen shake: intensity ___, duration ___
- [ ] Particles: ___
- [ ] Camera response: ___
- [ ] Audio cue: ___
- [ ] UI feedback: ___
- [ ] Timing/easing: ___
- [ ] Haptics (if applicable): ___

### Reference
_{Link to a video, GIF, or game that shows a similar feel:}_

---

## ✅ Acceptance Criteria
- [ ] ___
- [ ] ___
- [ ] Does not break existing: ___
- [ ] Works on {primary platform} input
- [ ] Follows naming convention (see `RULES_AND_POLICY.md` §2)

---

## 📝 After Implementation
- [ ] Write DevLog entry
- [ ] Write ADR if architecture changed
- [ ] Commit with conventional format: `feat(scope): ...`
