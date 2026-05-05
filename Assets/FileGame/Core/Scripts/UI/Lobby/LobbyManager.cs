using System;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbyManager : MonoBehaviour
{
    [Serializable]
    public class PlayerSlotUI
    {
        public TMP_Text playerNameText;
        public GameObject hostTagObject;
        public GameObject youTagObject;
    }

    [Header("Scene Names")]
    [SerializeField] private string roomSelectSceneName = "RoomSelect";
    [SerializeField] private string gameSceneName = "Game";

    [Header("Room Info")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text roomStateText;
    [SerializeField] private TMP_Text lockStatusText;
    [SerializeField] private TMP_Text statusText;

    [Header("Player Slots")]
    [SerializeField] private PlayerSlotUI[] playerSlots = new PlayerSlotUI[4];

    [Header("Controls")]
    [SerializeField] private Toggle lockRoomToggle;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;

    private ISession session;
    private bool isBusy;
    private bool isSceneChanging;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnClickStartGame);
        if (leaveButton != null) leaveButton.onClick.AddListener(OnClickLeaveRoom);
        if (lockRoomToggle != null) lockRoomToggle.onValueChanged.AddListener(OnLockToggleChanged);
    }

    private void Start()
    {
        session = SessionFlowContext.CurrentSession;
        SessionFlowContext.SetCurrentSession(session, SessionRoleUtility.IsLocalPlayerHost(session));

        if (session == null)
        {
            SetStatus("No active session found.");
            SceneManager.LoadScene(roomSelectSceneName);
            return;
        }

        SubscribeEvents();

        if (RoomRuntimeState.CurrentState == RoomFlowState.None)
            RoomRuntimeState.EnterLobby(session, session.IsLocked);

        RefreshUI();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        if (startButton != null) startButton.onClick.RemoveListener(OnClickStartGame);
        if (leaveButton != null) leaveButton.onClick.RemoveListener(OnClickLeaveRoom);
        if (lockRoomToggle != null) lockRoomToggle.onValueChanged.RemoveListener(OnLockToggleChanged);
    }

    private void SubscribeEvents()
    {
        if (session == null) return;
        session.Changed += OnSessionChanged;
        session.PlayerJoined += OnPlayerJoined;
        session.PlayerHasLeft += OnPlayerHasLeft;
        session.RemovedFromSession += OnRemovedFromSession;
        session.Deleted += OnSessionDeleted;
        session.SessionHostChanged += OnSessionHostChanged;
        session.PlayerPropertiesChanged += OnPlayerPropertiesChanged;
    }

    private void UnsubscribeEvents()
    {
        if (session == null) return;
        session.Changed -= OnSessionChanged;
        session.PlayerJoined -= OnPlayerJoined;
        session.PlayerHasLeft -= OnPlayerHasLeft;
        session.RemovedFromSession -= OnRemovedFromSession;
        session.Deleted -= OnSessionDeleted;
        session.SessionHostChanged -= OnSessionHostChanged;
        session.PlayerPropertiesChanged -= OnPlayerPropertiesChanged;
    }

    private void OnSessionChanged()
    {
        SessionFlowContext.SetCurrentSession(session, SessionRoleUtility.IsLocalPlayerHost(session));
        RefreshUI();
    }

    private void OnPlayerJoined(string playerId) { RefreshUI(); SetStatus($"Player joined: {ShortId(playerId)}"); }
    private void OnPlayerHasLeft(string playerId) { RefreshUI(); SetStatus($"Player left: {ShortId(playerId)}"); }
    private void OnPlayerPropertiesChanged() { RefreshUI(); }

    private void OnRemovedFromSession()
    {
        SessionFlowContext.Clear();
        SetStatus("You were removed from the room.");
        SceneManager.LoadScene(roomSelectSceneName);
    }

    private void OnSessionDeleted()
    {
        SessionFlowContext.Clear();
        SetStatus("The room was deleted.");
        SceneManager.LoadScene(roomSelectSceneName);
    }

    private void OnSessionHostChanged(string newHostPlayerId)
    {
        bool isLocalHost = SessionRoleUtility.IsLocalPlayerHost(session);
        SessionFlowContext.SetCurrentSession(session, isLocalHost);
        RefreshUI();
        SetStatus(isLocalHost ? "You are now the host." : "Host changed.");
    }

    private void OnClickStartGame() => _ = StartGameForAllAsync();

    private async Task StartGameForAllAsync()
    {
        if (!SessionFlowContext.IsHost || isBusy || isSceneChanging) return;

        if (NetworkGameBootstrap.Instance == null) { SetStatus("NetworkGameBootstrap not found."); return; }
        if (!NetworkGameBootstrap.Instance.IsNetworkRunning()) { SetStatus("NetworkManager is not running."); return; }
        if (!NetworkGameBootstrap.Instance.IsHost() && !NetworkGameBootstrap.Instance.IsServer())
        {
            SetStatus("Only the host can start the game.");
            return;
        }

        if (NetworkManager.Singleton?.SceneManager == null)
        {
            SetStatus("Network scene management is not available.");
            return;
        }

        try
        {
            SetBusy(true, "Starting game for all players...");

            bool isLocked = lockRoomToggle != null && lockRoomToggle.isOn;
            RoomRuntimeState.StartGame(isLocked);

            // อัปเดต Session Property
            if (SessionFlowContext.IsHost && session != null)
            {
                var hostSession = session.AsHost();
                hostSession.SetProperty("GameState", new SessionProperty("InGame"));
                await hostSession.SavePropertiesAsync();
                Debug.Log("[LobbyManager] ✅ Session GameState → InGame");
            }

            // Capture the exact clients that are in the lobby now. The server will
            // use this snapshot after the Game scene loads, so we do not depend on
            // CustomMessaging timing.
            Debug.Log("[LobbyManager] Capturing lobby clients before LoadScene...");
            PlayerSpawnManager.CaptureLobbyClients(NetworkManager.Singleton);

            Debug.Log($"[LobbyManager] 🚀 LoadScene → '{gameSceneName}'");
            NetworkManager.Singleton.SceneManager.LoadScene(
                gameSceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single);

            isSceneChanging = true;
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to start game: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnClickLeaveRoom() => _ = LeaveRoomAsync();

    private void OnLockToggleChanged(bool isLocked)
    {
        if (!SessionFlowContext.IsHost || isBusy)
        {
            if (lockRoomToggle != null) lockRoomToggle.SetIsOnWithoutNotify(RoomRuntimeState.IsLocked);
            return;
        }
        RoomRuntimeState.SetLocked(isLocked);
        RefreshUI();
    }

    private async Task LeaveRoomAsync()
    {
        if (session == null || isBusy) return;
        try
        {
            SetBusy(true, "Leaving room...");
            bool handled = await TryInvokeTaskMethodAsync(session, "LeaveAsync");
            if (!handled && SessionFlowContext.IsHost)
                handled = await TryInvokeTaskMethodAsync(SafeAsHost(session), "DeleteAsync");
            if (!handled) Debug.LogWarning("LeaveAsync/DeleteAsync not found.");
        }
        catch (Exception ex) { SetStatus($"Failed to leave room cleanly: {ex.Message}"); Debug.LogException(ex); }
        finally
        {
            NetworkGameBootstrap.Instance?.Shutdown();
            SessionFlowContext.Clear();
            SceneManager.LoadScene(roomSelectSceneName);
        }
    }

    private void RefreshUI()
    {
        if (session == null) return;

        string roomName = !string.IsNullOrWhiteSpace(RoomRuntimeState.RoomName) ? RoomRuntimeState.RoomName
            : (string.IsNullOrWhiteSpace(session.Name) ? "Unnamed Room" : session.Name);

        string roomCode = !string.IsNullOrWhiteSpace(RoomRuntimeState.JoinCode) ? RoomRuntimeState.JoinCode
            : ReadJoinCode(session);

        if (roomNameText != null) roomNameText.text = roomName;
        if (roomCodeText != null) roomCodeText.text = roomCode;
        if (roomStateText != null) roomStateText.text = RoomRuntimeState.CurrentState == RoomFlowState.InGame ? "InGame" : "Lobby";
        if (lockStatusText != null) lockStatusText.text = RoomRuntimeState.IsLocked ? "Locked" : "Open";

        if (lockRoomToggle != null)
        {
            lockRoomToggle.SetIsOnWithoutNotify(RoomRuntimeState.IsLocked);
            lockRoomToggle.interactable = SessionFlowContext.IsHost && !isBusy;
        }

        RefreshPlayerSlots();
        UpdateButtons();
    }

    private void RefreshPlayerSlots()
    {
        if (playerSlots == null || playerSlots.Length == 0) return;

        string hostPlayerId = ReadHostPlayerId(session);
        string localPlayerId = ReadLocalPlayerId(session);
        int playerCount = session?.Players != null ? session.Players.Count : 0;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            var slot = playerSlots[i];
            if (slot == null) continue;

            if (i < playerCount)
            {
                object player = session.Players[i];
                string playerId = ReadPlayerId(player);
                string playerName = ReadPlayerName(player);

                if (slot.playerNameText != null) slot.playerNameText.text = playerName;
                if (slot.hostTagObject != null) slot.hostTagObject.SetActive(!string.IsNullOrEmpty(hostPlayerId) && playerId == hostPlayerId);
                if (slot.youTagObject != null) slot.youTagObject.SetActive(!string.IsNullOrEmpty(localPlayerId) && playerId == localPlayerId);
            }
            else
            {
                if (slot.playerNameText != null) slot.playerNameText.text = "Waiting...";
                if (slot.hostTagObject != null) slot.hostTagObject.SetActive(false);
                if (slot.youTagObject != null) slot.youTagObject.SetActive(false);
            }
        }
    }

    private void UpdateButtons()
    {
        if (startButton != null) startButton.interactable = SessionFlowContext.IsHost && !isBusy;
        if (leaveButton != null) leaveButton.interactable = !isBusy;
    }

    // ─── Helpers ──────────────────────────────────────────────────

    private static string ReadLocalPlayerId(ISession s) => s?.CurrentPlayer?.Id ?? string.Empty;

    private static string ReadPlayerId(object p)
    {
        if (p == null) return string.Empty;
        return p.GetType().GetProperty("Id")?.GetValue(p)?.ToString() ?? string.Empty;
    }

    private static string ReadPlayerName(object p)
    {
        if (p == null) return "Unknown Player";

        if (p is IReadOnlyPlayer readOnlyPlayer)
        {
            string sessionName = PlayerNameRegistry.ReadSessionPlayerName(readOnlyPlayer);
            if (!string.IsNullOrWhiteSpace(sessionName))
                return sessionName;
        }

        foreach (string n in new[] { "Name", "DisplayName" })
        {
            string v = p.GetType().GetProperty(n)?.GetValue(p)?.ToString();
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }
        string id = ReadPlayerId(p);
        return string.IsNullOrWhiteSpace(id) ? "Unknown Player" : ShortId(id);
    }

    private static string ReadJoinCode(ISession s)
    {
        if (s == null) return "-";
        foreach (string n in new[] { "Code", "SessionCode", "JoinCode" })
        {
            string v = s.GetType().GetProperty(n)?.GetValue(s)?.ToString();
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }
        return "-";
    }

    private static string ReadHostPlayerId(ISession s)
    {
        if (s == null) return string.Empty;
        string v = s.GetType().GetProperty("Host")?.GetValue(s)?.ToString();
        if (!string.IsNullOrEmpty(v)) return v;
        return SafeAsHost(s)?.GetType().GetProperty("Host")?.GetValue(SafeAsHost(s))?.ToString() ?? string.Empty;
    }

    private static IHostSession SafeAsHost(ISession s)
    {
        try { return s?.AsHost(); } catch { return null; }
    }

    private static async Task<bool> TryInvokeTaskMethodAsync(object target, string methodName)
    {
        if (target == null) return false;
        var m = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        if (m == null) return false;
        if (m.Invoke(target, null) is Task t) { await t; return true; }
        return false;
    }

    private void SetBusy(bool value, string message = null)
    {
        isBusy = value;
        if (!string.IsNullOrEmpty(message)) SetStatus(message);
        UpdateButtons();
        if (lockRoomToggle != null) lockRoomToggle.interactable = SessionFlowContext.IsHost && !isBusy;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log(message);
    }

    private static string ShortId(string v)
    {
        if (string.IsNullOrEmpty(v)) return "-";
        return v.Length <= 8 ? v : v.Substring(0, 8);
    }
}
