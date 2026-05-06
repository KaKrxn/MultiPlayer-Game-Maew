# Multiplayer Game — Project Rules

## Stack
- Unity 6 (6000.3.8f1)
- Netcode for GameObjects (NGO)
- Unity Multiplayer Services (Lobby)
- Base Prefab: Multiplayer Third Person Gameplay (Unity Template)

## Scene Flow
MainMenu → RoomSelect → Lobby → LoadingScreen → Game

## Key Rules
- NetworkManager lives in MainMenu (DontDestroyOnLoad)
- NetworkManager.PlayerPrefab = None (manual spawn only)
- PlayerSpawnManager owns all spawn logic
- CoreMovement: OnIsServerAuthoritative() => false
- Lobby clients → startSpawnPoints[], Late joiners → trainSpawnPoint
- GameManager must NOT call SetPosition() on player
- LoadingScreenManager controls spawn timing — PSM does not self-trigger

## Do NOT modify
- CoreMovement.cs
- CoreCameraController.cs
- CorePlayerManager.cs
- LoadingScreenManager.cs
- Anything under Assets/Simple-Multiplayer/

## Custom scripts live in
- Assets/FileGame/Core/Scripts/
- Assets/Wow/Wow_Scripts/