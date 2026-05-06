using System.Collections;
using Blocks.Gameplay.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Singleton;

    [Header("Chat UI")]
    [SerializeField] private GameObject chatRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform chatContent;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private ChatMessageUI chatMessagePrefab;

    [Header("Player Info")]
    [SerializeField] private string defaultPlayerName = "Player";

    [Header("Optional")]
    [SerializeField] private Behaviour[] disableWhileChatOpen;
    [SerializeField] private bool unlockCursorWhileChatOpen = true;
    [SerializeField] private bool alwaysShowCursor = false;
    [SerializeField] private bool closeWhenSubmittingEmptyMessage = true;

    private bool m_IsChatOpen;
    private CursorLockMode m_CachedLockMode;
    private bool m_CachedCursorVisible;

    private void Awake()
    {
        Singleton = this;

        if (sendButton != null)
            sendButton.onClick.AddListener(SendCurrentInput);
    }

    private void Start()
    {
        CloseChat(true);

        if (alwaysShowCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient && PlayerNameRegistry.Instance != null)
            PlayerNameRegistry.Instance.SubmitLocalNameServerRpc(GetCurrentLocalPlayerName());
    }

    private void OnDestroy()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(SendCurrentInput);

        if (Singleton == this)
            Singleton = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && m_IsChatOpen)
        {
            CloseChat(false);
            return;
        }

        bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (!enterPressed)
            return;

        if (!m_IsChatOpen)
        {
            OpenChat();
            return;
        }

        SendCurrentInput();
    }

    public void OpenChat()
    {
        m_IsChatOpen = true;

        if (chatRoot != null)
            chatRoot.SetActive(true);

        if (unlockCursorWhileChatOpen)
        {
            m_CachedLockMode = Cursor.lockState;
            m_CachedCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        SetGameplayScriptsEnabled(false);
        StartCoroutine(FocusInputNextFrame());
    }

    public void CloseChat(bool clearInput)
    {
        m_IsChatOpen = false;

        if (clearInput && chatInput != null)
            chatInput.text = string.Empty;

        if (chatInput != null)
            chatInput.DeactivateInputField();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (chatRoot != null)
            chatRoot.SetActive(false);

        if (unlockCursorWhileChatOpen)
        {
            Cursor.lockState = m_CachedLockMode;
            Cursor.visible = m_CachedCursorVisible;
        }

        if (alwaysShowCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        SetGameplayScriptsEnabled(true);
    }

    public void SendCurrentInput()
    {
        if (chatInput == null)
            return;

        string message = chatInput.text.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            if (closeWhenSubmittingEmptyMessage)
                CloseChat(true);
            return;
        }

        if (!CanSendMessage())
            return;

        SendChatMessageServerRpc(message, GetCurrentLocalPlayerName());

        chatInput.text = string.Empty;
        if (!m_IsChatOpen)
            OpenChat();

        StartCoroutine(FocusInputNextFrame());
    }

    private string GetCurrentLocalPlayerName()
    {
        string registryName = PlayerNameRegistry.GetSavedLocalPlayerName();
        if (!string.IsNullOrWhiteSpace(registryName))
            return registryName;

        if (!string.IsNullOrWhiteSpace(PlayerRuntimeData.LocalPlayerName))
            return PlayerRuntimeData.LocalPlayerName;

        return PlayerPrefs.GetString(PlayerNameRegistry.PlayerPrefsKey, defaultPlayerName);
    }

    private bool CanSendMessage()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening &&
               IsSpawned;
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;

        if (chatInput == null)
            yield break;

        chatInput.ActivateInputField();
        chatInput.Select();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(chatInput.gameObject);

        chatInput.caretPosition = chatInput.text.Length;
    }

    private void SetGameplayScriptsEnabled(bool enabledState)
    {
        if (disableWhileChatOpen == null)
            return;

        for (int i = 0; i < disableWhileChatOpen.Length; i++)
        {
            if (disableWhileChatOpen[i] != null)
                disableWhileChatOpen[i].enabled = enabledState;
        }
    }

    private void AddMessage(ulong senderClientId, string senderName, string message)
    {
        if (chatMessagePrefab == null || chatContent == null)
            return;

        ChatMessageUI messageUi = Instantiate(chatMessagePrefab, chatContent);

        RectTransform rect = messageUi.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        bool isOwnMessage = NetworkManager.Singleton != null &&
                            senderClientId == NetworkManager.Singleton.LocalClientId;

        messageUi.transform.SetAsLastSibling();
        messageUi.Bind($"[{senderName}]: {message}", isOwnMessage);
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(string message, string submittedName, ServerRpcParams rpcParams = default)
    {
        string trimmedMessage = message.Trim();
        if (string.IsNullOrWhiteSpace(trimmedMessage))
            return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        string senderName = PlayerNameRegistry.SanitizeName(submittedName);

        if (PlayerNameRegistry.Instance != null)
        {
            PlayerNameRegistry.Instance.EnsureNameForClient(senderClientId, senderName);
            senderName = PlayerNameRegistry.Instance.GetName(senderClientId);
        }

        ReceiveChatMessageClientRpc(senderClientId, senderName, trimmedMessage);
    }

    [ClientRpc]
    private void ReceiveChatMessageClientRpc(ulong senderClientId, string senderName, string message)
    {
        if (Singleton != null)
            Singleton.AddMessage(senderClientId, senderName, message);
    }
}
