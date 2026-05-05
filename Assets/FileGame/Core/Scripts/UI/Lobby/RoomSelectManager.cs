using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomSelectManager : MonoBehaviour
{
    [Serializable]
    public class RoomListItemData
    {
        public string SessionId;
        public string RoomName;
        public bool IsLocked;
        public int CurrentPlayers;
        public int MaxPlayers;
        public string RoomState;
        public bool CanJoin;
    }

    [Header("Scenes")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Top Row")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private Button quickJoinButton;

    [Header("Bottom Row")]
    [SerializeField] private TMP_InputField roomNameInputField;
    [SerializeField] private Toggle lockRoomToggle;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button backButton;

    [Header("Room List")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private RoomListItemUI roomListItemPrefab;
    [SerializeField] private int maxRoomsToShow = 30;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text statusText;

    [Header("Settings")]
    [SerializeField] private int maxPlayersPerRoom = 4;
    [SerializeField] private bool quickJoinCreateRoomIfNotFound = true;
    [SerializeField] private float quickJoinTimeoutSeconds = 5f;
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float autoRefreshInterval = 3f;

    private bool isBusy;
    private bool isRefreshingList;
    private Coroutine autoRefreshCoroutine;

    private void Awake()
    {
        if (joinByCodeButton != null)
            joinByCodeButton.onClick.AddListener(OnClickJoinByCode);

        if (quickJoinButton != null)
            quickJoinButton.onClick.AddListener(OnClickQuickJoin);

        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnClickCreateRoom);

        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);

        if (codeInputField != null)
            codeInputField.onValueChanged.AddListener(OnCodeInputChanged);

        if (roomNameInputField != null)
            roomNameInputField.onValueChanged.AddListener(OnRoomNameInputChanged);

        UnityServicesBootstrap.OnInitializationCompleted += HandleInitializationCompleted;
    }

    private void Start()
    {
        UpdateButtons();

        if (!UnityServicesBootstrap.IsInitialized)
            SetStatus("Initializing Multiplayer Services...");
        else
            SetStatus("Ready.");

        if (autoRefresh)
            autoRefreshCoroutine = StartCoroutine(AutoRefreshRoutine());

        _ = RefreshRoomListAsync();
    }

    private void OnDestroy()
    {
        if (joinByCodeButton != null)
            joinByCodeButton.onClick.RemoveListener(OnClickJoinByCode);

        if (quickJoinButton != null)
            quickJoinButton.onClick.RemoveListener(OnClickQuickJoin);

        if (createRoomButton != null)
            createRoomButton.onClick.RemoveListener(OnClickCreateRoom);

        if (backButton != null)
            backButton.onClick.RemoveListener(OnClickBack);

        if (codeInputField != null)
            codeInputField.onValueChanged.RemoveListener(OnCodeInputChanged);

        if (roomNameInputField != null)
            roomNameInputField.onValueChanged.RemoveListener(OnRoomNameInputChanged);

        if (autoRefreshCoroutine != null)
            StopCoroutine(autoRefreshCoroutine);

        UnityServicesBootstrap.OnInitializationCompleted -= HandleInitializationCompleted;
    }

    private void OnCodeInputChanged(string _)
    {
        UpdateButtons();
    }

    private void OnRoomNameInputChanged(string _)
    {
        UpdateButtons();
    }

    private void HandleInitializationCompleted(bool success)
    {
        if (success)
        {
            SetStatus("Multiplayer Services ready.");
            UpdateButtons();
            _ = RefreshRoomListAsync();
        }
        else
        {
            SetStatus("Failed to initialize Multiplayer Services.");
            UpdateButtons();
        }
    }

    private IEnumerator AutoRefreshRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoRefreshInterval);

            if (!isBusy && !isRefreshingList && AreMultiplayerServicesInitialized())
                _ = RefreshRoomListAsync();
        }
    }

    private void UpdateButtons()
    {
        bool servicesReady = AreMultiplayerServicesInitialized();
        string code = codeInputField != null ? codeInputField.text : string.Empty;
        string roomName = roomNameInputField != null ? roomNameInputField.text : string.Empty;

        if (joinByCodeButton != null)
            joinByCodeButton.interactable = servicesReady && !isBusy && IsCodeValid(code);

        if (quickJoinButton != null)
            quickJoinButton.interactable = servicesReady && !isBusy;

        if (createRoomButton != null)
            createRoomButton.interactable = servicesReady && !isBusy && !string.IsNullOrWhiteSpace(roomName);
    }

    public void OnClickBack()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnClickJoinByCode()
    {
        _ = JoinByCodeAsync();
    }

    public void OnClickQuickJoin()
    {
        _ = QuickJoinAsync();
    }

    public void OnClickCreateRoom()
    {
        _ = CreateRoomAsync();
    }

    public async Task RefreshRoomListAsync()
    {
        if (isRefreshingList || isBusy)
            return;

        if (!AreMultiplayerServicesInitialized())
        {
            SetStatus("Multiplayer Services are not ready.");
            return;
        }

        try
        {
            isRefreshingList = true;
            SetStatus("Loading room list...");

            var queryResult = await MultiplayerService.Instance.QuerySessionsAsync(
                new QuerySessionsOptions
                {
                    SortOptions = new List<SortOption>
                    {
                        new SortOption(SortOrder.Descending, SortField.Name)
                    }
                });

            ClearRoomList();

            int count = Mathf.Min(queryResult.Sessions.Count, maxRoomsToShow);

            for (int i = 0; i < count; i++)
            {
                var info = queryResult.Sessions[i];
                int currentPlayers = Mathf.Max(0, info.MaxPlayers - info.AvailableSlots);

                RoomListItemData data = new RoomListItemData
                {
                    SessionId = info.Id,
                    RoomName = string.IsNullOrWhiteSpace(info.Name) ? "Unnamed Room" : info.Name,
                    IsLocked = info.IsLocked,
                    CurrentPlayers = currentPlayers,
                    MaxPlayers = info.MaxPlayers,
                    RoomState = info.IsLocked ? "Locked" : "Lobby",
                    CanJoin = !info.IsLocked
                };

                CreateRoomListItem(data);
            }

            SetStatus($"Found {count} room(s).");
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to load room list: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            isRefreshingList = false;
        }
    }

    private async Task CreateRoomAsync()
    {
        if (isBusy)
            return;

        if (!AreMultiplayerServicesInitialized())
        {
            SetStatus("Multiplayer Services are not ready.");
            return;
        }

        string roomName = roomNameInputField != null ? roomNameInputField.text.Trim() : string.Empty;
        bool isLocked = lockRoomToggle != null && lockRoomToggle.isOn;

        if (string.IsNullOrWhiteSpace(roomName))
        {
            SetStatus("Please enter a room name.");
            return;
        }

        try
        {
            SetBusy(true, "Creating room...");

            var sessionOptions = new SessionOptions
            {
                Name = roomName,
                MaxPlayers = maxPlayersPerRoom,
                IsLocked = isLocked,
                PlayerProperties = CreateLocalPlayerProperties(),
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    { "GameState", new SessionProperty("Lobby") },
                    { "HostIP", new SessionProperty(NetworkGameBootstrap.GetLocalIPAddress()) }
                }
            };

            ISession session = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
            await SaveLocalPlayerNameAsync(session);
            
            bool isHost = SessionRoleUtility.IsLocalPlayerHost(session);
            SessionFlowContext.SetCurrentSession(session, isHost);
            RoomRuntimeState.EnterLobby(session, isLocked);

            if (NetworkGameBootstrap.Instance == null)
            {
                SetStatus("NetworkGameBootstrap not found in scene.");
                return;
            }

            bool started = NetworkGameBootstrap.Instance.StartHostLocal();
            if (!started)
            {
                SetStatus("Failed to start host network.");
                return;
            }

            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.SceneManager != null)
            {
                Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                SceneManager.LoadScene(lobbySceneName);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to create room: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task JoinByCodeAsync()
    {
        if (isBusy)
            return;

        if (!AreMultiplayerServicesInitialized())
        {
            SetStatus("Multiplayer Services are not ready.");
            return;
        }

        string code = codeInputField != null ? codeInputField.text.Trim() : string.Empty;

        if (!IsCodeValid(code))
        {
            SetStatus("Invalid room code.");
            return;
        }

        try
        {
            SetBusy(true, "Joining room by code...");

            ISession session = await MultiplayerService.Instance.JoinSessionByCodeAsync(
                code,
                new JoinSessionOptions
                {
                    PlayerProperties = CreateLocalPlayerProperties()
                });

            await SaveLocalPlayerNameAsync(session);
            RouteAfterJoin(session);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to join room: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task JoinByIdAsync(string sessionId)
    {
        if (isBusy || string.IsNullOrWhiteSpace(sessionId))
            return;

        if (!AreMultiplayerServicesInitialized())
        {
            SetStatus("Multiplayer Services are not ready.");
            return;
        }

        try
        {
            SetBusy(true, "Joining selected room...");

            ISession session = await MultiplayerService.Instance.JoinSessionByIdAsync(
                sessionId,
                new JoinSessionOptions
                {
                    PlayerProperties = CreateLocalPlayerProperties()
                });

            await SaveLocalPlayerNameAsync(session);
            RouteAfterJoin(session);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to join room: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task QuickJoinAsync()
    {
        if (isBusy)
            return;

        if (!AreMultiplayerServicesInitialized())
        {
            SetStatus("Multiplayer Services are not ready.");
            return;
        }

        try
        {
            SetBusy(true, "Quick joining...");

            var quickJoinOptions = new QuickJoinOptions
            {
                Timeout = TimeSpan.FromSeconds(quickJoinTimeoutSeconds),
                CreateSession = quickJoinCreateRoomIfNotFound
            };

            var createOptions = new SessionOptions
            {
                Name = $"Room_{UnityEngine.Random.Range(1000, 9999)}",
                MaxPlayers = maxPlayersPerRoom,
                IsLocked = false,
                PlayerProperties = CreateLocalPlayerProperties()
            };

            ISession session = await MultiplayerService.Instance.MatchmakeSessionAsync(
                quickJoinOptions,
                createOptions);

            await SaveLocalPlayerNameAsync(session);
            RouteAfterJoin(session);
        }
        catch (Exception ex)
        {
            SetStatus($"Quick Join failed: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RouteAfterJoin(ISession session)
    {
        if (session == null)
        {
            SetStatus("Session not found.");
            return;
        }

        bool isHost = SessionRoleUtility.IsLocalPlayerHost(session);

        SessionFlowContext.SetCurrentSession(session, isHost);
        RoomRuntimeState.EnterLobby(session, session.IsLocked);

        if (NetworkGameBootstrap.Instance == null)
        {
            SetStatus("NetworkGameBootstrap not found in scene.");
            return;
        }

        string gameState = "Lobby";
        if (session.Properties != null && session.Properties.TryGetValue("GameState", out var gsProp))
            gameState = gsProp.Value;

        string hostIP = "127.0.0.1";
        if (session.Properties != null && session.Properties.TryGetValue("HostIP", out var ipProp))
            hostIP = ipProp.Value;

        bool started = isHost
            ? NetworkGameBootstrap.Instance.StartHostLocal()
            : NetworkGameBootstrap.Instance.StartClientLocal(hostIP);

        if (!started)
        {
            SetStatus(isHost ? "Failed to start host network." : "Failed to start client network.");
            return;
        }

        if (isHost)
        {
            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.SceneManager != null)
            {
                Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                SceneManager.LoadScene(lobbySceneName);
            }
        }
        // If it's a client, NetworkManager will automatically sync the scene to match the Host (Lobby or Game).
        // So we don't need to call SceneManager.LoadScene manually here.
    }

    private void CreateRoomListItem(RoomListItemData data)
    {
        if (roomListItemPrefab == null || roomListContent == null)
            return;

        RoomListItemUI item = Instantiate(roomListItemPrefab, roomListContent);
        item.Bind(data, sessionId => _ = JoinByIdAsync(sessionId));
    }

    private void ClearRoomList()
    {
        if (roomListContent == null)
            return;

        for (int i = roomListContent.childCount - 1; i >= 0; i--)
            Destroy(roomListContent.GetChild(i).gameObject);
    }

    private void SetBusy(bool value, string message = null)
    {
        isBusy = value;

        if (!string.IsNullOrEmpty(message))
            SetStatus(message);

        UpdateButtons();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log(message);
    }

    private static Dictionary<string, PlayerProperty> CreateLocalPlayerProperties()
    {
        string playerName = PlayerNameRegistry.GetSavedLocalPlayerName();
        return new Dictionary<string, PlayerProperty>
        {
            {
                PlayerNameRegistry.PlayerNamePropertyKey,
                new PlayerProperty(playerName, VisibilityPropertyOptions.Member)
            }
        };
    }

    private static async Task SaveLocalPlayerNameAsync(ISession session)
    {
        if (session?.CurrentPlayer == null)
            return;

        session.CurrentPlayer.SetProperty(
            PlayerNameRegistry.PlayerNamePropertyKey,
            new PlayerProperty(PlayerNameRegistry.GetSavedLocalPlayerName(), VisibilityPropertyOptions.Member));

        await session.SaveCurrentPlayerDataAsync();
    }

    private bool AreMultiplayerServicesInitialized()
    {
        return UnityServicesBootstrap.IsInitialized && MultiplayerService.Instance != null;
    }

    private static bool IsCodeValid(string code)
    {
        const string validChars = "6789BCDFGHJKLMNPQRTWbcdfghjklmnpqrtw";

        if (string.IsNullOrWhiteSpace(code) || code.Length < 6 || code.Length > 8)
            return false;

        foreach (char c in code)
        {
            if (!validChars.Contains(c.ToString()))
                return false;
        }

        return true;
    }
}
