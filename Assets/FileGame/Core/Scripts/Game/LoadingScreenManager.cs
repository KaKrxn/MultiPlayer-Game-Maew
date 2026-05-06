using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public enum LoadingPhase : byte
{
    LoadingScene     = 0,
    WaitingForPlayers = 1,
    StartingGame     = 2,
    Done             = 3
}

/// <summary>
/// Coordinates the loading screen shown between Lobby and Game.
///
/// Server side: waits for the train, the map, and every lobby client to report
/// ready before calling PlayerSpawnManager to spawn all players simultaneously,
/// then dismisses the loading screen on all clients.
///
/// Client side: shows the loading panel, updates status text as the phase
/// changes, and fades the panel out when the server says the game is ready.
///
/// Setup: place this NetworkBehaviour on a GameObject in the Game Scene that
/// has a child Canvas containing:
///   - loadingPanel  (the root panel – assign to the Canvas root or a child Panel)
///   - statusText    (TMP_Text for step messages)
///   - canvasGroup   (CanvasGroup on the same GameObject as loadingPanel for fade)
/// </summary>
public class LoadingScreenManager : NetworkBehaviour
{
    [Header("UI References")]
    [Tooltip("Root panel shown during loading. Assign the Canvas or a child Panel.")]
    [SerializeField] private GameObject loadingPanel;

    [Tooltip("TMP text element that shows the current loading step.")]
    [SerializeField] private TMP_Text statusText;

    [Tooltip("CanvasGroup on loadingPanel used for the fade-out. Optional.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("References")]
    [Tooltip("Auto-found if left empty.")]
    [SerializeField] private PlayerSpawnManager playerSpawnManager;

    // Synced phase so clients can update their status text.
    private readonly NetworkVariable<LoadingPhase> m_Phase = new(
        LoadingPhase.LoadingScene,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Server only: clients that sent NotifyClientReadyServerRpc.
    private readonly HashSet<ulong> m_ReadyClients = new();
    private bool m_SpawnTriggered;

    // ─────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        ShowLoadingUI();

        m_Phase.OnValueChanged += OnPhaseChanged;
        UpdateStatusText(m_Phase.Value);

        if (IsServer)
        {
            if (playerSpawnManager == null)
                playerSpawnManager = FindObjectOfType<PlayerSpawnManager>();

            // Host is both server and client; add it as ready immediately so
            // the server-side wait loop doesn't block on the host itself.
            m_ReadyClients.Add(NetworkManager.LocalClientId);

            StartCoroutine(WaitForAllConditions());
        }

        // Pure clients (not the host) notify the server they are loaded.
        if (IsClient && !IsServer)
        {
            NotifyClientReadyServerRpc();
        }
    }

    public override void OnNetworkDespawn()
    {
        m_Phase.OnValueChanged -= OnPhaseChanged;
        m_ReadyClients.Clear();
        m_SpawnTriggered = false;
    }

    // ─────────────────────────────────────────────────────────────
    // RPCs
    // ─────────────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void NotifyClientReadyServerRpc(ServerRpcParams p = default)
    {
        ulong clientId = p.Receive.SenderClientId;
        Debug.Log($"[LSM] Client {clientId} reported ready.");
        m_ReadyClients.Add(clientId);

        // If the lobby spawn gate already completed, this must be a late joiner.
        if (m_SpawnTriggered && !LobbyClientRegistry.Contains(clientId))
        {
            Debug.Log($"[LSM] Late joiner {clientId} reported after spawn gate → train spawn.");
            playerSpawnManager?.SpawnLateJoiner(clientId);
        }
    }

    [ClientRpc]
    private void DismissLoadingScreenClientRpc()
    {
        Debug.Log("[LSM-Client] Dismiss loading screen.");
        StopAllCoroutines();
        StartCoroutine(FadeOutLoading());
    }

    // ─────────────────────────────────────────────────────────────
    // Server coroutine
    // ─────────────────────────────────────────────────────────────

    private IEnumerator WaitForAllConditions()
    {
        // ── Step 1: wait for the train to be spawned ────────────
        Debug.Log("[LSM] Waiting for train to spawn...");
        TrainFuelSystem trainFuel = null;
        yield return new WaitUntil(() =>
        {
            trainFuel = FindObjectOfType<TrainFuelSystem>();
            return trainFuel != null;
        });
        Debug.Log("[LSM] Train detected.");

        // ── Step 2: wait for EndlessMapManager to find the train ─
        // (EMM sets trainTransform once it locates the train, after
        //  which it immediately begins spawning the initial tiles.)
        var mapManager = FindObjectOfType<EndlessMapManager>();
        if (mapManager != null)
        {
            Debug.Log("[LSM] Waiting for EndlessMapManager to initialise...");
            yield return new WaitUntil(() => mapManager.trainTransform != null);
            yield return null; // let the tile-spawn loop run one frame
            Debug.Log("[LSM] Map initialised.");
        }

        // ── Step 3: wait for all lobby clients to report ready ───
        m_Phase.Value = LoadingPhase.WaitingForPlayers;

        var lobbyClients = new List<ulong>(LobbyClientRegistry.LobbyClientIds);
        Debug.Log($"[LSM] Waiting for lobby clients: [{string.Join(", ", lobbyClients)}]");

        yield return new WaitUntil(() =>
        {
            foreach (ulong id in lobbyClients)
            {
                if (m_ReadyClients.Contains(id)) continue;
                // Skip clients that disconnected during load.
                if (!IsClientConnected(id)) continue;
                return false;
            }
            return true;
        });

        Debug.Log("[LSM] All lobby clients ready. Spawning players.");
        m_Phase.Value = LoadingPhase.StartingGame;

        // ── Step 4: spawn lobby players ──────────────────────────
        if (!m_SpawnTriggered && playerSpawnManager != null)
        {
            m_SpawnTriggered = true;
            playerSpawnManager.SpawnAllLobbyPlayers();
        }

        // ── Step 5: spawn any late joiners already in m_ReadyClients ─
        foreach (ulong id in m_ReadyClients)
        {
            if (!LobbyClientRegistry.Contains(id))
                playerSpawnManager?.SpawnLateJoiner(id);
        }

        yield return null; // frame for spawns to propagate

        // ── Step 6: dismiss loading screen on all clients ────────
        m_Phase.Value = LoadingPhase.Done;
        DismissLoadingScreenClientRpc();
    }

    // ─────────────────────────────────────────────────────────────
    // Client helpers
    // ─────────────────────────────────────────────────────────────

    private void OnPhaseChanged(LoadingPhase _, LoadingPhase next) => UpdateStatusText(next);

    private void UpdateStatusText(LoadingPhase phase)
    {
        if (statusText == null) return;
        statusText.text = phase switch
        {
            LoadingPhase.LoadingScene      => "Loading map...",
            LoadingPhase.WaitingForPlayers => "Waiting for other players...",
            LoadingPhase.StartingGame      => "Starting game...",
            _                              => string.Empty
        };
    }

    private void ShowLoadingUI()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (loadingPanel != null) loadingPanel.SetActive(true);
    }

    private IEnumerator FadeOutLoading()
    {
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    // Utilities
    // ─────────────────────────────────────────────────────────────

    private bool IsClientConnected(ulong clientId)
    {
        if (NetworkManager == null) return false;
        foreach (ulong id in NetworkManager.ConnectedClientsIds)
            if (id == clientId) return true;
        return false;
    }
}
