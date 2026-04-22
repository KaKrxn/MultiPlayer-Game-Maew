using System;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        if (startButton != null)
            startButton.onClick.AddListener(OnClickStartGame);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnClickLeaveRoom);

        if (lockRoomToggle != null)
            lockRoomToggle.onValueChanged.AddListener(OnLockToggleChanged);
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

        if (startButton != null)
            startButton.onClick.RemoveListener(OnClickStartGame);

        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(OnClickLeaveRoom);

        if (lockRoomToggle != null)
            lockRoomToggle.onValueChanged.RemoveListener(OnLockToggleChanged);
    }

    private void SubscribeEvents()
    {
        if (session == null)
            return;

        session.Changed += OnSessionChanged;
        session.PlayerJoined += OnPlayerJoined;
        session.PlayerHasLeft += OnPlayerHasLeft;
        session.RemovedFromSession += OnRemovedFromSession;
        session.Deleted += OnSessionDeleted;
        session.SessionHostChanged += OnSessionHostChanged;
    }

    private void UnsubscribeEvents()
    {
        if (session == null)
            return;

        session.Changed -= OnSessionChanged;
        session.PlayerJoined -= OnPlayerJoined;
        session.PlayerHasLeft -= OnPlayerHasLeft;
        session.RemovedFromSession -= OnRemovedFromSession;
        session.Deleted -= OnSessionDeleted;
        session.SessionHostChanged -= OnSessionHostChanged;
    }

    private void OnSessionChanged()
    {
        SessionFlowContext.SetCurrentSession(session, SessionRoleUtility.IsLocalPlayerHost(session));
        RefreshUI();
    }

    private void OnPlayerJoined(string playerId)
    {
        RefreshUI();
        SetStatus($"Player joined: {ShortId(playerId)}");
    }

    private void OnPlayerHasLeft(string playerId)
    {
        RefreshUI();
        SetStatus($"Player left: {ShortId(playerId)}");
    }

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

    private void OnClickStartGame()
    {
        if (!SessionFlowContext.IsHost || isBusy || isSceneChanging)
            return;

        bool isLocked = lockRoomToggle != null && lockRoomToggle.isOn;

        RoomRuntimeState.StartGame(isLocked);

        isSceneChanging = true;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnClickLeaveRoom()
    {
        _ = LeaveRoomAsync();
    }

    private void OnLockToggleChanged(bool isLocked)
    {
        if (!SessionFlowContext.IsHost || isBusy)
        {
            if (lockRoomToggle != null)
                lockRoomToggle.SetIsOnWithoutNotify(RoomRuntimeState.IsLocked);
            return;
        }

        RoomRuntimeState.SetLocked(isLocked);
        RefreshUI();
    }

    private async Task LeaveRoomAsync()
    {
        if (session == null || isBusy)
            return;

        try
        {
            SetBusy(true, "Leaving room...");

            bool handled = await TryInvokeTaskMethodAsync(session, "LeaveAsync");

            if (!handled && SessionFlowContext.IsHost)
            {
                IHostSession hostSession = SafeAsHost(session);
                handled = await TryInvokeTaskMethodAsync(hostSession, "DeleteAsync");
            }

            if (!handled)
                Debug.LogWarning("LeaveAsync/DeleteAsync not found. Returning locally.");
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to leave room cleanly: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            SessionFlowContext.Clear();
            SceneManager.LoadScene(roomSelectSceneName);
        }
    }

    private void RefreshUI()
    {
        if (session == null)
            return;

        string roomName = !string.IsNullOrWhiteSpace(RoomRuntimeState.RoomName)
            ? RoomRuntimeState.RoomName
            : (string.IsNullOrWhiteSpace(session.Name) ? "Unnamed Room" : session.Name);

        string roomCode = !string.IsNullOrWhiteSpace(RoomRuntimeState.JoinCode)
            ? RoomRuntimeState.JoinCode
            : ReadJoinCode(session);

        if (roomNameText != null)
            roomNameText.text = roomName;

        if (roomCodeText != null)
            roomCodeText.text = roomCode;

        if (roomStateText != null)
            roomStateText.text = RoomRuntimeState.CurrentState == RoomFlowState.InGame ? "InGame" : "Lobby";

        if (lockStatusText != null)
            lockStatusText.text = RoomRuntimeState.IsLocked ? "Locked" : "Open";

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
        if (playerSlots == null || playerSlots.Length == 0)
            return;

        string hostPlayerId = ReadHostPlayerId(session);
        string localPlayerId = ReadLocalPlayerId(session);

        int playerCount = session != null && session.Players != null ? session.Players.Count : 0;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            PlayerSlotUI slot = playerSlots[i];
            if (slot == null)
                continue;

            if (i < playerCount)
            {
                object player = session.Players[i];
                string playerId = ReadPlayerId(player);
                string playerName = ReadPlayerName(player);

                if (slot.playerNameText != null)
                    slot.playerNameText.text = playerName;

                if (slot.hostTagObject != null)
                    slot.hostTagObject.SetActive(!string.IsNullOrEmpty(hostPlayerId) && playerId == hostPlayerId);

                if (slot.youTagObject != null)
                    slot.youTagObject.SetActive(!string.IsNullOrEmpty(localPlayerId) && playerId == localPlayerId);
            }
            else
            {
                if (slot.playerNameText != null)
                    slot.playerNameText.text = "Waiting...";

                if (slot.hostTagObject != null)
                    slot.hostTagObject.SetActive(false);

                if (slot.youTagObject != null)
                    slot.youTagObject.SetActive(false);
            }
        }
    }

    private void UpdateButtons()
    {
        if (startButton != null)
            startButton.interactable = SessionFlowContext.IsHost && !isBusy;

        if (leaveButton != null)
            leaveButton.interactable = !isBusy;
    }

    private bool IsLocalPlayerId(string playerId)
    {
        string localId = ReadLocalPlayerId(session);
        return !string.IsNullOrEmpty(localId) && localId == playerId;
    }

    private static string ReadLocalPlayerId(ISession currentSession)
    {
        if (currentSession == null || currentSession.CurrentPlayer == null)
            return string.Empty;

        return currentSession.CurrentPlayer.Id;
    }

    private static string ReadPlayerId(object player)
    {
        if (player == null)
            return string.Empty;

        PropertyInfo idProp = player.GetType().GetProperty("Id");
        if (idProp == null)
            return string.Empty;

        object value = idProp.GetValue(player);
        return value?.ToString() ?? string.Empty;
    }

    private static string ReadPlayerName(object player)
    {
        if (player == null)
            return "Unknown Player";

        PropertyInfo nameProp = player.GetType().GetProperty("Name");
        if (nameProp != null)
        {
            object value = nameProp.GetValue(player);
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                return value.ToString();
        }

        PropertyInfo displayNameProp = player.GetType().GetProperty("DisplayName");
        if (displayNameProp != null)
        {
            object value = displayNameProp.GetValue(player);
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                return value.ToString();
        }

        string id = ReadPlayerId(player);
        return string.IsNullOrWhiteSpace(id) ? "Unknown Player" : ShortId(id);
    }

    private static string ReadJoinCode(ISession currentSession)
    {
        if (currentSession == null)
            return "-";

        string[] names = { "Code", "SessionCode", "JoinCode" };

        foreach (string name in names)
        {
            PropertyInfo prop = currentSession.GetType().GetProperty(name);
            if (prop == null)
                continue;

            object value = prop.GetValue(currentSession);
            if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                return value.ToString();
        }

        return "-";
    }

    private static string ReadHostPlayerId(ISession currentSession)
    {
        if (currentSession == null)
            return string.Empty;

        PropertyInfo hostProp = currentSession.GetType().GetProperty("Host");
        if (hostProp != null)
        {
            object value = hostProp.GetValue(currentSession);
            if (value != null)
                return value.ToString();
        }

        IHostSession hostSession = SafeAsHost(currentSession);
        if (hostSession == null)
            return string.Empty;

        PropertyInfo hostSessionHostProp = hostSession.GetType().GetProperty("Host");
        if (hostSessionHostProp == null)
            return string.Empty;

        object hostValue = hostSessionHostProp.GetValue(hostSession);
        return hostValue?.ToString() ?? string.Empty;
    }

    private static IHostSession SafeAsHost(ISession currentSession)
    {
        try
        {
            return currentSession?.AsHost();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> TryInvokeTaskMethodAsync(object target, string methodName)
    {
        if (target == null)
            return false;

        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
            return false;

        object result = method.Invoke(target, null);

        if (result is Task task)
        {
            await task;
            return true;
        }

        return false;
    }

    private void SetBusy(bool value, string message = null)
    {
        isBusy = value;

        if (!string.IsNullOrEmpty(message))
            SetStatus(message);

        UpdateButtons();

        if (lockRoomToggle != null)
            lockRoomToggle.interactable = SessionFlowContext.IsHost && !isBusy;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log(message);
    }

    private static string ShortId(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "-";

        return value.Length <= 8 ? value : value.Substring(0, 8);
    }
}