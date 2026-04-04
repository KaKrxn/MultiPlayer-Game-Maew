using UnityEngine;
using UnityEngine.UIElements;

namespace Blocks.Gameplay.Core
{
    public static class PlayerRuntimeData
    {
        public static string LocalPlayerName = "Player";
    }

    [RequireComponent(typeof(UIDocument))]
    public class GameNetworkUI : MonoBehaviour
    {
        [Tooltip("The visual tree asset template for the network UI.")]
        [SerializeField] private VisualTreeAsset networkUITemplate;

        private UIDocument m_UIDocument;
        private VisualElement m_Root;
        private VisualElement m_ConnectionPanel;
        private Button m_HostButton;
        private Button m_ClientButton;
        private TextField m_PlayerNameField;

        private GameNetworkManager m_CachedManager;

        private GameNetworkManager Manager
        {
            get
            {
                if (m_CachedManager == null)
                    m_CachedManager = GameNetworkManager.Instance;

                return m_CachedManager;
            }
        }

        private void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
            if (m_UIDocument == null)
            {
                Debug.LogError("GameNetworkUI requires a UIDocument component.");
                return;
            }

            if (networkUITemplate == null)
            {
                Debug.LogError("Network UI Template not assigned in the Inspector.");
                return;
            }

            m_Root = m_UIDocument.rootVisualElement;
            if (m_Root == null)
            {
                Debug.LogError("UIDocument is missing a rootVisualElement.");
                return;
            }

            CreateUI();
        }

        private void Update()
        {
            if (m_Root != null && m_ConnectionPanel != null && Manager != null)
                UpdateUI();
        }

        private void CreateUI()
        {
            m_Root = m_UIDocument.rootVisualElement;
            m_ConnectionPanel = m_Root.Q<VisualElement>("connection-panel");
            m_HostButton = m_Root.Q<Button>("host-button");
            m_ClientButton = m_Root.Q<Button>("client-button");
            m_PlayerNameField = m_Root.Q<TextField>("player-name-field");

            LoadSavedPlayerName();
            SetupUICallbacks();
            UpdateUI();
        }

        private void LoadSavedPlayerName()
        {
            if (m_PlayerNameField == null)
                return;

            string savedName = PlayerPrefs.GetString("PlayerName", "Player");
            m_PlayerNameField.value = savedName;
            PlayerRuntimeData.LocalPlayerName = savedName;
        }

        private string GetPlayerNameFromUI()
        {
            if (m_PlayerNameField == null)
                return "Player";

            string playerName = m_PlayerNameField.value.Trim();

            if (string.IsNullOrWhiteSpace(playerName))
                playerName = "Player";

            return playerName;
        }

        private void SaveLocalPlayerName()
        {
            string playerName = GetPlayerNameFromUI();

            PlayerRuntimeData.LocalPlayerName = playerName;

            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
        }

        private void SetupUICallbacks()
        {
            if (m_HostButton != null)
            {
                m_HostButton.clicked += () =>
                {
                    if (Manager == null)
                        return;

                    if (Manager.NetworkState.ConnectionState == GameNetworkManager.ConnectionStates.Connected)
                    {
                        Manager.Disconnect();
                    }
                    else
                    {
                        SaveLocalPlayerName();
                        Manager.StartHostConnection();
                    }
                };
            }

            if (m_ClientButton != null)
            {
                m_ClientButton.clicked += () =>
                {
                    if (Manager == null)
                        return;

                    SaveLocalPlayerName();
                    Manager.StartClientConnection();
                };
            }
        }

        private void UpdateUI()
        {
            if (Manager?.NetworkState == null) return;
            var state = Manager.NetworkState;

            bool canChangeSettings =
                state.ConnectionState == GameNetworkManager.ConnectionStates.None ||
                state.ConnectionState == GameNetworkManager.ConnectionStates.Failed;

            if (m_HostButton != null)
            {
                switch (state.ConnectionState)
                {
                    case GameNetworkManager.ConnectionStates.None:
                    case GameNetworkManager.ConnectionStates.Failed:
                        m_HostButton.text = "Start Host";
                        m_HostButton.SetEnabled(true);
                        break;

                    case GameNetworkManager.ConnectionStates.Connecting:
                        m_HostButton.text = "Connecting...";
                        m_HostButton.SetEnabled(false);
                        break;

                    case GameNetworkManager.ConnectionStates.Connected:
                        m_HostButton.text = "Disconnect";
                        m_HostButton.SetEnabled(true);
                        break;
                }
            }

            if (m_ClientButton != null)
            {
                bool showClientButton = canChangeSettings;
                m_ClientButton.SetEnabled(showClientButton);
                m_ClientButton.style.display = showClientButton ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (m_PlayerNameField != null)
                m_PlayerNameField.SetEnabled(canChangeSettings);
        }
    }
}