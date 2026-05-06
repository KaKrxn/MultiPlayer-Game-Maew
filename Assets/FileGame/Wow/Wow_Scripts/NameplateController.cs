using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NameplateController : MonoBehaviour
{
    [SerializeField] private GameObject nameplateRoot;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    private NetworkObject m_NetworkObject;
    private Camera m_Camera;
    private ulong m_ClientId;
    private bool m_Initialized;

    private void Awake()
    {
        if (nameplateRoot == null)
            nameplateRoot = gameObject;

        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (PlayerNameRegistry.Instance != null)
            PlayerNameRegistry.Instance.NameChanged -= HandleNameChanged;
    }

    public void Initialize(ulong clientId, NetworkObject networkObject)
    {
        m_ClientId = clientId;
        m_NetworkObject = networkObject;
        m_Initialized = true;
        TrySubscribe();
        Refresh();
    }

    private void LateUpdate()
    {
        if (!m_Initialized)
        {
            m_NetworkObject = GetComponentInParent<NetworkObject>();
            if (m_NetworkObject != null)
                Initialize(m_NetworkObject.OwnerClientId, m_NetworkObject);
        }

        if (m_Camera == null)
            m_Camera = Camera.main;

        if (m_Camera != null)
            transform.forward = m_Camera.transform.forward;

        if (m_NetworkObject != null)
            transform.position = m_NetworkObject.transform.position + worldOffset;
    }

    private void TrySubscribe()
    {
        if (PlayerNameRegistry.Instance == null)
            return;

        PlayerNameRegistry.Instance.NameChanged -= HandleNameChanged;
        PlayerNameRegistry.Instance.NameChanged += HandleNameChanged;
        Refresh();
    }

    private void HandleNameChanged(ulong clientId, string playerName)
    {
        if (clientId == m_ClientId)
            Refresh();
    }

    private void Refresh()
    {
        if (nameplateRoot != null)
        {
            bool isLocalPlayer = NetworkManager.Singleton != null &&
                                 m_ClientId == NetworkManager.Singleton.LocalClientId;
            nameplateRoot.SetActive(!isLocalPlayer);
        }

        if (nameText == null || PlayerNameRegistry.Instance == null)
            return;

        nameText.text = PlayerNameRegistry.Instance.GetName(m_ClientId);
    }
}
