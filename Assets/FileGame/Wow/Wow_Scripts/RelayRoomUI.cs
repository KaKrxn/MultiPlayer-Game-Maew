using UnityEngine;
using UnityEngine.UIElements;

namespace Blocks.Gameplay.Core
{
    [RequireComponent(typeof(UIDocument))]
    public class RelayRoomUI : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private TextField playerNameField;
        private TextField roomCodeField;

        private Button hostButton;
        private Button clientButton;
        private Button copyCodeButton;

        private Label generatedRoomCodeLabel;
        private Label statusLabel;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            root = uiDocument.rootVisualElement;

            playerNameField = root.Q<TextField>("player-name-field");
            roomCodeField = root.Q<TextField>("room-code-field");

            hostButton = root.Q<Button>("host-button");
            clientButton = root.Q<Button>("client-button");
            copyCodeButton = root.Q<Button>("copy-code-button");

            generatedRoomCodeLabel = root.Q<Label>("generated-room-code-label");
            statusLabel = root.Q<Label>("status-label");

            LoadSavedValues();
            BindUI();
        }

        private void Update()
        {
            if (RelayRoomService.Instance == null)
                return;

            if (generatedRoomCodeLabel != null)
                generatedRoomCodeLabel.text = string.IsNullOrWhiteSpace(RelayRoomService.Instance.CurrentJoinCode)
                    ? "----"
                    : RelayRoomService.Instance.CurrentJoinCode;

            if (statusLabel != null)
                statusLabel.text = RelayRoomService.Instance.StatusMessage;
        }

        private void LoadSavedValues()
        {
            if (playerNameField != null)
                playerNameField.value = PlayerPrefs.GetString("PlayerName", "Player");

            if (roomCodeField != null)
                roomCodeField.value = PlayerPrefs.GetString("LastJoinCode", "");
        }

        private void SaveLocalValues()
        {
            string playerName = GetPlayerName();
            string roomCode = GetRoomCode();

            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.SetString("LastJoinCode", roomCode);
            PlayerPrefs.Save();
        }

        private string GetPlayerName()
        {
            if (playerNameField == null)
                return "Player";

            string value = playerNameField.value.Trim();
            return string.IsNullOrWhiteSpace(value) ? "Player" : value;
        }

        private string GetRoomCode()
        {
            if (roomCodeField == null)
                return "";

            return roomCodeField.value.Trim().ToUpperInvariant();
        }

        private void BindUI()
        {
            if (hostButton != null)
            {
                hostButton.clicked += () =>
                {
                    SaveLocalValues();

                    if (RelayRoomService.Instance != null)
                        RelayRoomService.Instance.CreateRoom(GetPlayerName());
                };
            }

            if (clientButton != null)
            {
                clientButton.clicked += () =>
                {
                    SaveLocalValues();

                    if (RelayRoomService.Instance != null)
                        RelayRoomService.Instance.JoinRoom(GetRoomCode(), GetPlayerName());
                };
            }

            if (copyCodeButton != null)
            {
                copyCodeButton.clicked += () =>
                {
                    if (generatedRoomCodeLabel == null)
                        return;

                    string code = generatedRoomCodeLabel.text;
                    if (string.IsNullOrWhiteSpace(code) || code == "----")
                        return;

                    GUIUtility.systemCopyBuffer = code;
                };
            }
        }
    }
}