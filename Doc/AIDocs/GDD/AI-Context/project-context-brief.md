# Project Context Brief

> **Instructions:** Fill in this file with your project's real details. This is the ONLY file you need to write manually.
> Then run the `/bootstrap-project` workflow — AI will use this to generate your filled `GDD.md` and `project-stack.md`.
>
> **Token optimization:** Keep answers concise. This brief should be ~500-800 tokens total. Don't write essays — write facts.

---

## 1. Identity

| Field | Your Answer |
|-------|-------------|
| Project Name | MultiPlayer-Game-Maew |
| Genre | Multiplayer Third Person Survival / Sandbox |
| Core Fantasy (1 sentence) | Survive and thrive in a modular multiplayer environment with combat and survival mechanics. |
| Tone keywords | Action, Survival, Modular, Multiplayer |
| Primary Platform | PC |
| Secondary Platform | TBD |
| Camera Style | Third Person |

## 2. Tech Stack

| Area | Value |
|------|-------|
| Engine + Version | Unity |
| Render Pipeline | URP (Universal Render Pipeline) |
| 2D or 3D | 3D |
| Input System | Unity Input System (Event-driven via ScriptableObjects) |
| Camera System | CoreCameraController |
| UI Framework | Unity UI (UGUI) |
| Networking | Netcode for GameObjects (NGO) |

## 3. Scenes in Build

List every scene in your Build Settings, one per line:
```
- [BB] MainMenu Scene → Main menu entry point
- Lobby/RoomSelect → Multiplayer room selection
- Lobby/Lobby → Multiplayer lobby
- [Ten] Test Player → Player testing scene
```

## 4. Core Systems — File Map

> **This is the most important section.** List every major system, its script path, and its current state.
> AI agents use this to know **which files to read** and **which files to ignore**.

| System | Script Path | State | Notes |
|--------|------------|-------|-------|
| Player Manager | `Assets/Simple-Multiplayer/Core/Scripts/Runtime/Components/CorePlayerManager.cs` | Works / Refactoring | Orchestrator, data-driven architecture |
| Movement | `Assets/Simple-Multiplayer/Core/Scripts/Runtime/Components/CoreMovement.cs` | Works | |
| Stats / Survival | `Assets/Simple-Multiplayer/Core/Scripts/Runtime/Components/CoreStatsHandler.cs` | Works | Health, Stamina |
| Game Manager | `Assets/Simple-Multiplayer/Core/Scripts/...` (Needs location) | Needs refactor | Decoupling UI from Logic |

## 5. Architecture Reality

### Current Pattern (be honest)
_How are things actually wired right now? Singletons? God-objects? ScriptableObjects? Events?_

```
Moving from a monolithic prototype to a scalable, data-driven architecture using ScriptableObject events and modular components. CorePlayerManager orchestrates player state.
```

### Known Tech Debt
- Non-English comments to translate.
- Naming inconsistencies to fix.
- Quarantining duplicate scripts.
- Tightly coupled UI in Game Manager.

## 6. Design Boundaries

### Experience Pillars (3-5 max)
1. **Responsive Multiplayer** — Smooth netcode and synchronization.
2. **Modular Survival** — Extensible stats, damage, and item systems.

### Hard Rules (things AI must never break)
- Do not tightly couple UI logic with core game logic (e.g., GameManager).
- Use ScriptableObject events for cross-system communication.

### Architecture Rules (from your design)
1. Follow the component-based `Core` architecture.
2. Respect the single responsibility principle for scenes.

## 7. Commit Scopes

List the scope names for your conventional commits (these become the vocabulary for all agents):
```
player · inventory · survival · ui · network · scene · build · docs
```
