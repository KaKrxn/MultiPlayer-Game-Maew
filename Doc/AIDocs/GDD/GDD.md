# MultiPlayer-Game-Maew — Game Design Document

## Purpose

This document is the shared north star for AI coders, senior engineers, technical designers, and future contributors working on **MultiPlayer-Game-Maew**.

It has two equally important jobs:

1. Preserve the intended identity of the game.
2. Give the current prototype a clean technical direction without pretending that the clean architecture already exists.

Read this as a **design-led execution charter**:

- The **game fantasy, emotional tone, and player loop are authoritative**.
- The **current codebase is prototype truth**, not final architecture.
- Any refactor must protect the intended player experience while improving maintainability.

---

<!-- @tag:identity -->
## Project Identity

| Field | Direction |
| --- | --- |
| Working Title | **MultiPlayer-Game-Maew** |
| Genre | **Multiplayer Third Person Survival / Sandbox** |
| Core Fantasy | Survive and thrive in a modular multiplayer environment with combat and survival mechanics. |
| Player Promise | Experience dynamic and responsive multiplayer survival action with friends. |
| Platform Priority | **PC**, with **TBD** support |
| Camera / Play Plane | **Third Person** |
| Tone | **Action, Survival, Modular, Multiplayer** |
| Story Intent | Form alliances, gather resources, and survive in an open sandbox. |

### Experience Pillars

1. **Responsive Multiplayer**
   Smooth netcode and synchronization are critical for the player experience.

2. **Modular Survival**
   Extensible stats, damage, and item systems to allow for rich gameplay mechanics.

---

<!-- @tag:visual-audio -->
## Visual, Mood, and Audio Direction

### Mood Board Keywords

- Survival
- Action
- Modular

### Art Direction
<!-- TODO: Human fill -->

### Audio Direction
<!-- TODO: Human fill -->

---

<!-- @tag:platform-input -->
## Platform and Input Philosophy

### Primary Rule
**PC is the authoritative input model.** TBD support is a mapped port layer, not a separate game design.

### PC Controls
- Movement: WASD
- Look: Mouse
- Jump: Spacebar
- Sprint: Shift
<!-- TODO: Human fill additional inputs -->

### TBD Port Rules
- Mirror the same verbs and timing windows.
- Do not redesign encounters around TBD precision.

---

<!-- @tag:core-loop -->
## Core Game Loop

The fundamental loop is:

1. Connect to the multiplayer lobby
2. Spawn into the sandbox environment
3. Gather resources and manage survival stats (Health, Stamina)
4. Engage in combat and dynamic encounters
5. Extract or Respawn

### Progression Cadence
<!-- TODO: Human fill -->

### Failure / Recovery Pattern
Players enter an eliminated state upon health depletion, triggering a death sequence (e.g., graveyard teleport and spectating), followed by a respawn.

---

<!-- @tag:items -->
## Canonical Gameplay Content — Items

| Item | Role | Intended Effect |
| --- | --- | --- |
| Bandage | Healing | Restores health and reduces pain. |
<!-- TODO: Human fill additional items -->

---

<!-- @tag:enemies -->
## Canonical Gameplay Content — Enemies and Threats
<!-- TODO: Human fill -->

---

<!-- @tag:mechanics -->
## System Mechanics

### Movement and Stamina
- Players can sprint and jump, consuming stamina/health based on stats.
- Integrated via `CoreStatsHandler` and `CoreMovement`.

<!-- TODO: Human fill additional mechanics -->

---

<!-- @tag:inspirations -->
## Inspirations and Design Boundaries
<!-- TODO: Human fill -->

---

<!-- @tag:tech-stack -->
## Actual Project Stack and Repo Reality

> This section reflects the repository **as it exists today**.

### Technical Stack

| Area | Current Repo Truth |
| --- | --- |
| Engine | **Unity** |
| Render Pipeline | **URP (Universal Render Pipeline)** |
| Gameplay Dimension | **3D** |
| Input | **Unity Input System (Event-driven via ScriptableObjects)** |
| Camera | **CoreCameraController** |
| UI | **Unity UI (UGUI)** |
| Networking | **Netcode for GameObjects (NGO)** |
| Build Scenes | [BB] MainMenu Scene, Lobby/RoomSelect, Lobby/Lobby, [Ten] Test Player |

---

<!-- @tag:prototype-truth -->
## Current Prototype Truth

> This is the most important reality check for future contributors.

**The current implementation already contains usable gameplay ideas, but it is still prototype-grade.**

### Existing Runtime Shape
Moving from a monolithic prototype to a scalable, data-driven architecture using ScriptableObject events and modular components. `CorePlayerManager` orchestrates player state.

### What Exists Today

#### Player Manager
Current script: `Assets/Simple-Multiplayer/Core/Scripts/Runtime/Components/CorePlayerManager.cs`

What it currently does:
- Orchestrates player components (Movement, Stats, Input).
- Handles player lifecycle and death.

#### Movement
Current script: `Assets/Simple-Multiplayer/Core/Scripts/Runtime/Components/CoreMovement.cs`

What it currently does:
- Handles character movement and jumping.

#### Stats / Survival
Current script: `Assets/Simple-Multiplayer/Core/Scripts/Runtime/Components/CoreStatsHandler.cs`

What it currently does:
- Manages player health and stamina consumption.

### Current Prototype Debt
- Non-English comments to translate.
- Naming inconsistencies to fix.
- Quarantining duplicate scripts.
- Tightly coupled UI in Game Manager.

---

<!-- @tag:scene-roles -->
## Canonical Future Scene Roles

| Scene Role | Purpose |
| --- | --- |
| `[BB] MainMenu Scene` | Main menu entry point |
| `Lobby/RoomSelect` | Multiplayer room selection |
| `Lobby/Lobby` | Multiplayer lobby |
| `[Ten] Test Player` | Player testing scene |

---

<!-- @tag:system-ownership -->
## Canonical Future System Ownership

| System | Ownership |
| --- | --- |
| `Player Manager` | Orchestrator, data-driven architecture |
| `Movement` | Character physics and abilities |
| `Stats / Survival` | Health, Stamina |
| `Game Manager` | Decoupling UI from Logic |

### Hard Rule
No future refactor should dump new features back into a single global MonoBehaviour just because it is faster in the short term.

---

<!-- @tag:architecture -->
## Target Architecture Direction

### Architecture Principles

1. **Game design authority first**
   The mood, pace, pressure, and PC-first loop are more important than neat code alone.

2. **Data-driven content**
   Items, stats, and abilities should move toward authorable data assets rather than hardcoded scene logic.

3. **Thin scene wiring**
   Scenes should assemble references and presentation. They should not permanently own progression logic.

4. **State separated from presentation**
   UI, animation, sound, and VFX should react to gameplay state instead of defining it.

5. **Explicit module boundaries**
   Player Manager, Movement, Stats, and Game Manager should be individually testable or replaceable.

### Recommended Folder Direction
_(See `RULES_AND_POLICY.md` §3 for the standard folder structure.)_

### Data-Driven Content Targets
- `StatDefinition` — Defines stats and their bounds.
- `ItemDefinition` — Defines items and their use effects.

---

<!-- @tag:guardrails -->
## Contributor Guardrails

### Preserve These Non-Negotiables
- Do not tightly couple UI logic with core game logic (e.g., GameManager).
- Use ScriptableObject events for cross-system communication.

### You May Refactor Aggressively
- Existing Game Manager UI coupling.
- Duplicate scripts or misnamed components.

### Do Not Do These
- Introduce Singletons for manager classes if they can be ScriptableObjects or localized managers.
- Bypass the `CorePlayerManager` for direct stats/movement modification if not appropriate.

### Naming Policy
_(See `RULES_AND_POLICY.md` §2 for the standard naming conventions.)_

### AI Safety Rule
Every future implementation must clearly separate **current implementation** from **target architecture**. Do not hallucinate completed architecture.

---

<!-- @tag:roadmap -->
## Phased Refactor Roadmap

### Phase 1 — Stabilize the Prototype
**Goal:** Make the current game loop less fragile without changing the design identity.
- Decouple Game Manager and UI.
- Fix compilation issues (e.g., StatKeys conflicts).
- Translate non-English comments.

### Phase 2 — Split Core Runtime Systems
**Goal:** Break the prototype monolith into clear system owners.
- Refine Survival mechanics.
- Ensure Player Lifecycle (death/respawn) is fully robust.

### Phase 3 — Data-Drive Content
**Goal:** Make content authorable without rewriting runtime code.
- Extract item data and stat definitions to ScriptableObjects.

### Phase 4 — Vertical Slice
**Goal:** Turn the core loop into a production-worthy slice.
- Clean up test scenes.
- Polish multiplayer lobby flow.

### Phase 5 — Expand Content and Port Readiness
**Goal:** Scale content while preserving PC-first usability.
- Add additional content (enemies, items).

---

## Final Instruction To Contributors

When working on **MultiPlayer-Game-Maew**, do not optimize only for clean code and do not optimize only for vibes.

The correct target is:

- **emotionally faithful Multiplayer Third Person Survival / Sandbox design**
- **PC-first readable gameplay**
- **data-driven scalable architecture**
- **clear ownership instead of prototype sprawl**

If a choice improves architecture but weakens the core fantasy, it is the wrong choice.
If a choice preserves mood but makes the code impossible to scale, it is also the wrong choice.

Build toward both.
