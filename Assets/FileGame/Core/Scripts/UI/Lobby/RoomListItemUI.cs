using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text roomStateText;
    [SerializeField] private GameObject lockIconObject;
    [SerializeField] private Button joinButton;

    private string sessionId;
    private Action<string> onJoinClicked;

    public void Bind(RoomSelectManager.RoomListItemData data, Action<string> onJoin)
    {
        sessionId = data.SessionId;
        onJoinClicked = onJoin;

        if (roomNameText != null)
            roomNameText.text = data.RoomName;

        if (playerCountText != null)
            playerCountText.text = $"{data.CurrentPlayers}/{data.MaxPlayers}";

        if (roomStateText != null)
            roomStateText.text = data.RoomState;

        if (lockIconObject != null)
            lockIconObject.SetActive(data.IsLocked);

        if (joinButton != null)
        {
            joinButton.interactable = data.CanJoin;
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnClickJoin);
        }
    }

    private void OnClickJoin()
    {
        onJoinClicked?.Invoke(sessionId);
    }
}