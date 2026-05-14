using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// Spawns players in the Game scene.
///
/// Players that were already connected when the host started from the lobby
/// spawn at start spawn points. Late joiners spawn on the train.
/// </summary>
public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Player Settings")]
    [Tooltip("Player prefab to spawn for each connected client.")]
    public GameObject playerPrefab;

    [Header("Spawn Points")]
    [Tooltip("Spawn points used for players that started from the lobby.")]
    public Transform[] startSpawnPoints;

    [Tooltip("Spawn point used for late joiners. If empty, the manager searches for a train spawn fallback.")]
    public Transform trainSpawnPoint;

    [Header("Player Names")]
    [SerializeField] private PlayerNameRegistry playerNameRegistry;

    private readonly HashSet<ulong> m_Spawned = new HashSet<ulong>();
    private readonly HashSet<ulong> m_ReadyClients = new HashSet<ulong>();
    private readonly List<ulong> m_LobbySpawnOrder = new List<ulong>();
    private int m_SpawnIndex;
    private bool m_LobbySpawnGateCompleted;

    /// <summary>
    /// Called by LobbyManager before loading the Game scene.
    /// This is a server-side snapshot, not a client message, so scene-load timing
    /// and transport latency cannot change the spawn decision.
    /// </summary>
    public static void CaptureLobbyClients(NetworkManager networkManager)
    {
        if (networkManager == null)
        {
            Debug.LogWarning("[PSM] Cannot capture lobby clients because NetworkManager is null.");
            LobbyClientRegistry.Clear();
            return;
        }

        LobbyClientRegistry.CaptureLobbyClients(networkManager.ConnectedClientsIds);
        Debug.Log($"[PSM] Captured lobby clients: [{string.Join(", ", LobbyClientRegistry.LobbyClientIds)}]");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PSM] OnNetworkSpawn | IsServer={IsServer} IsHost={IsHost} IsClient={IsClient} | LocalClientId={NetworkManager.LocalClientId}");

        if (IsServer)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[PSM] playerPrefab is not assigned in the Inspector.");
                return;
            }

            if (playerNameRegistry == null)
                playerNameRegistry = FindObjectOfType<PlayerNameRegistry>();

            FindStartSpawnPoints();
            BuildLobbySpawnOrder();
            Debug.Log($"[PSM] Lobby client snapshot: [{string.Join(", ", LobbyClientRegistry.LobbyClientIds)}]");

            int pointCount = startSpawnPoints != null ? startSpawnPoints.Length : 0;
            Debug.Log($"[PSM] StartSpawnPoints={pointCount} | TrainSpawnPoint={(trainSpawnPoint != null ? trainSpawnPoint.name : "null")}");
            LogServerState("OnNetworkSpawn");
        }

        if (IsClient)
        {
            // LoadingScreenManager coordinates readiness and calls SpawnAllLobbyPlayers()
            // on the server when all conditions are met. Do NOT call NotifyReadyServerRpc here.
            Debug.Log($"[PSM] Client {NetworkManager.LocalClientId} loaded. Waiting for LoadingScreenManager to trigger spawn.");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        LobbyClientRegistry.Clear();
        m_Spawned.Clear();
        m_ReadyClients.Clear();
        m_LobbySpawnOrder.Clear();
        m_SpawnIndex = 0;
        m_LobbySpawnGateCompleted = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        Debug.Log($"[PSM-Server] NotifyReady | ClientId={clientId}");
        m_ReadyClients.Add(clientId);
        LogServerState($"NotifyReady({clientId})");

        if (m_Spawned.Contains(clientId))
        {
            Debug.LogWarning($"[PSM-Server] Client {clientId} sent NotifyReady more than once. Ignoring.");
            return;
        }

        bool fromLobby = IsLobbyClient(clientId);
        Debug.Log($"[PSM-Server] Client {clientId} fromLobby={fromLobby}");

        if (fromLobby)
        {
            TrySpawnLobbyClientsWhenReady();
        }
        else
        {
            Debug.Log($"[PSM-Server] Client {clientId} -> Train");
            m_Spawned.Add(clientId);
            SpawnPlayerAtTrain(clientId);
        }
    }

    private bool IsLobbyClient(ulong clientId)
    {
        return LobbyClientRegistry.Contains(clientId);
    }

    private void BuildLobbySpawnOrder()
    {
        m_LobbySpawnOrder.Clear();

        foreach (ulong clientId in LobbyClientRegistry.LobbyClientIds)
        {
            if (IsClientConnected(clientId))
            {
                m_LobbySpawnOrder.Add(clientId);
            }
            else
            {
                Debug.LogWarning($"[PSM-Server] Lobby snapshot contains disconnected client {clientId}; skipping start spawn.");
            }
        }

        m_LobbySpawnOrder.Sort();
        Debug.Log($"[PSM-Server] Lobby spawn order: [{string.Join(", ", m_LobbySpawnOrder)}]");
    }

    private void TrySpawnLobbyClientsWhenReady()
    {
        if (m_LobbySpawnGateCompleted)
        {
            Debug.Log("[PSM-Server] Lobby spawn gate already completed.");
            return;
        }

        if (m_LobbySpawnOrder.Count == 0)
        {
            Debug.LogWarning("[PSM-Server] No lobby spawn order was captured. Cannot spawn lobby clients at start points.");
            m_LobbySpawnGateCompleted = true;
            return;
        }

        List<ulong> waitingFor = new List<ulong>();
        foreach (ulong clientId in m_LobbySpawnOrder)
        {
            if (!m_ReadyClients.Contains(clientId))
                waitingFor.Add(clientId);
        }

        if (waitingFor.Count > 0)
        {
            Debug.Log($"[PSM-Server] Waiting before StartPoint spawn. Ready=[{string.Join(", ", m_ReadyClients)}] Waiting=[{string.Join(", ", waitingFor)}]");
            return;
        }

        Debug.Log($"[PSM-Server] All lobby clients are ready. Spawning StartPoint players in order: [{string.Join(", ", m_LobbySpawnOrder)}]");

        foreach (ulong clientId in m_LobbySpawnOrder)
        {
            if (m_Spawned.Contains(clientId))
            {
                Debug.LogWarning($"[PSM-Server] Lobby client {clientId} was already spawned before the lobby gate completed. Skipping duplicate.");
                continue;
            }

            int index = m_SpawnIndex++;
            Debug.Log($"[PSM-Server] Client {clientId} -> StartPoint index={index}");
            m_Spawned.Add(clientId);
            SpawnPlayerAtStartPoint(clientId, index);
        }

        m_LobbySpawnGateCompleted = true;
        LogServerState("After lobby StartPoint spawn");
    }

    private bool IsClientConnected(ulong clientId)
    {
        if (NetworkManager == null)
            return false;

        foreach (ulong connectedClientId in NetworkManager.ConnectedClientsIds)
        {
            if (connectedClientId == clientId)
                return true;
        }

        return false;
    }

    private void LogServerState(string context)
    {
        string connectedClients = NetworkManager != null
            ? string.Join(", ", NetworkManager.ConnectedClientsIds)
            : "NetworkManager=null";

        Debug.Log($"[PSM-Server] State@{context} | Connected=[{connectedClients}] Ready=[{string.Join(", ", m_ReadyClients)}] LobbyOrder=[{string.Join(", ", m_LobbySpawnOrder)}] Spawned=[{string.Join(", ", m_Spawned)}] SpawnIndex={m_SpawnIndex}");
    }

    private void SpawnPlayerAtStartPoint(ulong clientId, int index)
    {
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        if (startSpawnPoints != null && startSpawnPoints.Length > 0)
        {
            int pointIndex = index % startSpawnPoints.Length;
            Transform point = startSpawnPoints[pointIndex];

            if (point != null)
            {
                position = point.position;
                rotation = point.rotation;

                int round = index / startSpawnPoints.Length;
                if (round > 0 || startSpawnPoints.Length == 1)
                {
                    Vector3 offset = point.right * (pointIndex * 1.5f)
                                   + point.right * (round * (startSpawnPoints.Length * 1.5f));
                    position += offset;
                    Debug.Log($"[PSM-Server] Applied start-point offset={offset} (round={round}, pointIndex={pointIndex})");
                }

                Debug.Log($"[PSM-Server] Client {clientId} -> StartPoint[{pointIndex}] '{point.name}' | pos={position}");
            }
            else
            {
                Debug.LogWarning($"[PSM-Server] startSpawnPoints[{pointIndex}] is null. Spawning client {clientId} at 0,0,0.");
            }
        }
        else
        {
            Debug.LogWarning($"[PSM-Server] No startSpawnPoints found. Spawning client {clientId} at 0,0,0.");
        }

        InstantiateAndSpawn(clientId, position, rotation);
    }

    private void SpawnPlayerAtTrain(ulong clientId)
    {
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        if (trainSpawnPoint != null)
        {
            position = trainSpawnPoint.position;
            rotation = trainSpawnPoint.rotation;
            Debug.Log($"[PSM-Server] Client {clientId} -> TrainSpawnPoint '{trainSpawnPoint.name}' | pos={position}");
        }
        else
        {
            TrainFuelSystem train = FindObjectOfType<TrainFuelSystem>();
            if (train != null)
            {
                position = train.transform.position + Vector3.up * 2f;
                rotation = train.transform.rotation;
                Debug.Log($"[PSM-Server] Found TrainFuelSystem '{train.name}' | Client {clientId} -> pos={position}");
            }
            else
            {
                GameObject fallback = GameObject.Find("TrainSpawnPoint");
                if (fallback != null)
                {
                    position = fallback.transform.position;
                    rotation = fallback.transform.rotation;
                    Debug.Log($"[PSM-Server] Found fallback TrainSpawnPoint | pos={position}");
                }
                else
                {
                    Debug.LogWarning($"[PSM-Server] Could not find a train spawn point. Spawning client {clientId} at 0,0,0.");
                }
            }
        }

        InstantiateAndSpawn(clientId, position, rotation);
    }

    private void InstantiateAndSpawn(ulong clientId, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[PSM-Server] Instantiate | ClientId={clientId} | pos={position} rot={rotation.eulerAngles}");

        GameObject player = Instantiate(playerPrefab, position, rotation);
        NetworkObject networkObject = player.GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            if (playerNameRegistry == null)
                playerNameRegistry = FindObjectOfType<PlayerNameRegistry>();

            playerNameRegistry?.EnsureNameForClient(clientId);

            networkObject.SpawnAsPlayerObject(clientId, true);
            playerNameRegistry?.RegisterPlayerObject(clientId, networkObject);

            NameplateController[] nameplates = player.GetComponentsInChildren<NameplateController>(true);
            for (int i = 0; i < nameplates.Length; i++)
                nameplates[i].Initialize(clientId, networkObject);

            Debug.Log($"[PSM-Server] SpawnAsPlayerObject succeeded | ClientId={clientId} | NetworkObjectId={networkObject.NetworkObjectId} | Owner={networkObject.OwnerClientId} | IsSpawned={networkObject.IsSpawned} | finalPos={player.transform.position} | expectedObservers=[{string.Join(", ", NetworkManager.ConnectedClientsIds)}]");
        }
        else
        {
            Debug.LogError("[PSM-Server] Player prefab has no NetworkObject. Destroying spawned instance.");
            Destroy(player);
        }
    }

    /// <summary>
    /// Called by LoadingScreenManager on the server once all conditions are met.
    /// Marks every lobby client as ready and runs the existing spawn gate.
    /// </summary>
    public void SpawnAllLobbyPlayers()
    {
        if (!IsServer) return;
        if (m_LobbySpawnGateCompleted)
        {
            Debug.LogWarning("[PSM-Server] SpawnAllLobbyPlayers called but gate already completed.");
            return;
        }

        if (playerNameRegistry == null)
            playerNameRegistry = FindObjectOfType<PlayerNameRegistry>();

        SeedLobbyNamesFromSession();

        // LSM has already confirmed all clients are ready; mark them here so
        // TrySpawnLobbyClientsWhenReady sees a full ready set.
        foreach (ulong clientId in m_LobbySpawnOrder)
        {
            m_ReadyClients.Add(clientId);
            playerNameRegistry?.EnsureNameForClient(clientId);
        }

        TrySpawnLobbyClientsWhenReady();
    }

    private void SeedLobbyNamesFromSession()
    {
        if (!IsServer || playerNameRegistry == null)
            return;

        ISession session = SessionFlowContext.CurrentSession;
        if (session?.Players == null)
            return;

        int count = Mathf.Min(m_LobbySpawnOrder.Count, session.Players.Count);
        for (int i = 0; i < count; i++)
        {
            string playerName = PlayerNameRegistry.ReadSessionPlayerName(session.Players[i]);
            playerNameRegistry.EnsureNameForClient(m_LobbySpawnOrder[i], playerName);
        }
    }

    /// <summary>
    /// Called by LoadingScreenManager on the server to spawn a late-joining client
    /// (one who was not in LobbyClientRegistry) at the train spawn point.
    /// </summary>
    public void SpawnLateJoiner(ulong clientId)
    {
        if (!IsServer) return;
        if (m_Spawned.Contains(clientId))
        {
            Debug.LogWarning($"[PSM-Server] SpawnLateJoiner: client {clientId} already spawned. Skipping.");
            return;
        }
        Debug.Log($"[PSM-Server] SpawnLateJoiner {clientId} → Train");
        if (playerNameRegistry == null)
            playerNameRegistry = FindObjectOfType<PlayerNameRegistry>();

        playerNameRegistry?.EnsureNameForClient(clientId);
        m_Spawned.Add(clientId);
        SpawnPlayerAtTrain(clientId);
    }

    private void FindStartSpawnPoints()
    {
        if (startSpawnPoints != null && startSpawnPoints.Length > 0)
        {
            Debug.Log($"[PSM] Using {startSpawnPoints.Length} start spawn point(s) from the Inspector.");
            return;
        }

        GameObject[] found = GameObject.FindGameObjectsWithTag("Respawn");

        if (found.Length == 0)
        {
            GameObject singleSpawnPoint = GameObject.Find("StartSpawnPoint");
            if (singleSpawnPoint != null)
                found = new[] { singleSpawnPoint };
        }

        if (found.Length > 0)
        {
            startSpawnPoints = new Transform[found.Length];
            for (int i = 0; i < found.Length; i++)
                startSpawnPoints[i] = found[i].transform;

            Debug.Log($"[PSM] Found {found.Length} start spawn point(s) by tag/name.");
        }
        else
        {
            Debug.LogWarning("[PSM] No start spawn points found. Assign them in the Inspector or tag them as Respawn.");
        }
    }
}
