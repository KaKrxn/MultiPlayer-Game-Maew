# MultiPlayer-Game-Maew — AI Context Bootstrap

> **AI INSTRUCTION:** This is your primary context file. The project uses a tag-based GDD to save tokens. When you need details about a specific area, read the corresponding `@tag:` section in `Docs/GDD/GDD.md`. Do NOT guess or hallucinate context.

## Project Identity
- **Name:** MultiPlayer-Game-Maew
- **Genre:** Multiplayer Third Person Survival / Sandbox
- **Tone:** Action, Survival, Modular, Multiplayer
- **Primary Platform:** PC (Keyboard/Mouse authoritative)
- **Engine:** Unity (URP, 3D, NGO for Networking, Unity Input System)
- **More detail:** Read `@tag:identity`, `@tag:visual-audio`, `@tag:platform-input`, `@tag:tech-stack` in GDD.md.

## Core Rules & Architecture
- Moving toward a **scalable, data-driven architecture** using ScriptableObject events and modular components.
- Do not tightly couple UI logic with core game logic.
- Avoid Singletons for manager classes if they can be localized.
- **More detail:** Read `@tag:architecture`, `@tag:guardrails` in GDD.md.

## Key Systems (Prototype Truth)
- `Player Manager` (Orchestrator): `Assets/Simple-Multiplayer/Core/Scripts/Runtime/Components/CorePlayerManager.cs`
- `Movement`: `Assets/Simple-Multiplayer/Core/Scripts/Runtime/Components/CoreMovement.cs`
- `Stats / Survival`: `Assets/Simple-Multiplayer/Core/Scripts/Runtime/Components/CoreStatsHandler.cs`
- **More detail:** Read `@tag:prototype-truth`, `@tag:system-ownership` in GDD.md.

## Current Focus
- Phase 1: Stabilize the Prototype (Decouple Game Manager and UI, Fix compilation issues, Translate comments).
- **More detail:** Read `@tag:roadmap` in GDD.md.

## Valid Commit Scopes
Use these scopes for your commits:
`player · inventory · survival · ui · network · scene · build · docs`
