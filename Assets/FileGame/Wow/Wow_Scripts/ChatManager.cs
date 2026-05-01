using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.EventSystems;
using Blocks.Gameplay.Core;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Singleton;

    [Header("Chat UI")]
    [SerializeField] private GameObject chatRoot;
    [SerializeField] private Transform chatContent;
    [SerializeField] private TMP_InputField chatInput;
    [SerializeField] private ChatMessage chatMessagePrefab;

    [Header("Player Info")]
    [SerializeField] private string defaultPlayerName = "Player";

    [Header("Optional")]
    [SerializeField] private Behaviour[] disableWhileChatOpen;
    [SerializeField] private bool unlockCursorWhileChatOpen = true;

    private bool isChatOpen = false;
    private CursorLockMode cachedLockMode;
    private bool cachedCursorVisible;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        CloseChat(true);
    }

    private void Update()
    {
        bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (!enterPressed)
            return;

        if (!isChatOpen)
        {
            OpenChat();
            return;
        }

        SubmitOrCloseChat();
    }

    private string GetCurrentLocalPlayerName()
    {
        if (!string.IsNullOrWhiteSpace(PlayerRuntimeData.LocalPlayerName))
            return PlayerRuntimeData.LocalPlayerName;

        return PlayerPrefs.GetString("PlayerName", defaultPlayerName);
    }

    private bool CanSendMessage()
    {
        if (NetworkManager.Singleton == null) return false;
        if (!NetworkManager.Singleton.IsListening) return false;
        if (!IsSpawned) return false;
        return true;
    }

    private void OpenChat()
    {
        isChatOpen = true;

        if (chatRoot != null)
            chatRoot.SetActive(true);

        if (unlockCursorWhileChatOpen)
        {
            cachedLockMode = Cursor.lockState;
            cachedCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        SetGameplayScriptsEnabled(false);
        StartCoroutine(FocusInputNextFrame());
    }

    private void CloseChat(bool clearInput)
    {
        isChatOpen = false;

        if (clearInput && chatInput != null)
            chatInput.text = "";

        if (chatInput != null)
            chatInput.DeactivateInputField();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (chatRoot != null)
            chatRoot.SetActive(false);

        if (unlockCursorWhileChatOpen)
        {
            Cursor.lockState = cachedLockMode;
            Cursor.visible = cachedCursorVisible;
        }

        SetGameplayScriptsEnabled(true);
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

    private void SubmitOrCloseChat()
    {
        if (chatInput == null)
            return;

        string message = chatInput.text.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            CloseChat(true);
            return;
        }

        if (!CanSendMessage())
            return;

        string senderName = GetCurrentLocalPlayerName();
        string finalMessage = senderName + " > " + message;

        SendChatMessageServerRpc(finalMessage);

        chatInput.text = "";
        CloseChat(false);
    }

    private void SetGameplayScriptsEnabled(bool enabledState)
    {
        if (disableWhileChatOpen == null) return;

        for (int i = 0; i < disableWhileChatOpen.Length; i++)
        {
            if (disableWhileChatOpen[i] != null)
                disableWhileChatOpen[i].enabled = enabledState;
        }
    }

    private void AddMessage(string msg)
    {
        if (chatMessagePrefab == null || chatContent == null)
            return;

        ChatMessage cm = Instantiate(chatMessagePrefab, chatContent);

        RectTransform rect = cm.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        cm.transform.SetAsLastSibling();
        cm.SetText(msg);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(string message)
    {
        ReceiveChatMessageClientRpc(message);
    }

    [ClientRpc]
    private void ReceiveChatMessageClientRpc(string message)
    {
        if (Singleton != null)
            Singleton.AddMessage(message);
    }
}