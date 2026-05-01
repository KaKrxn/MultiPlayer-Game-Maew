using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkGameBootstrap : MonoBehaviour
{
    public static NetworkGameBootstrap Instance { get; private set; }

    [Header("Local Test Settings")]
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private ushort serverPort = 7777;

    private NetworkManager networkManager;
    private UnityTransport unityTransport;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        networkManager = GetComponent<NetworkManager>();
        unityTransport = GetComponent<UnityTransport>();

        if (networkManager == null)
            Debug.LogError("NetworkGameBootstrap: NetworkManager not found.");

        if (unityTransport == null)
            Debug.LogError("NetworkGameBootstrap: UnityTransport not found.");
    }

    public bool IsNetworkRunning()
    {
        return networkManager != null && networkManager.IsListening;
    }

    public bool IsHost()
    {
        return networkManager != null && networkManager.IsHost;
    }

    public bool IsServer()
    {
        return networkManager != null && networkManager.IsServer;
    }

    public bool StartHostLocal()
    {
        if (networkManager == null || unityTransport == null)
            return false;

        if (networkManager.IsListening)
            return true;

        unityTransport.SetConnectionData("0.0.0.0", serverPort, "0.0.0.0");

        bool success = networkManager.StartHost();
        Debug.Log(success
            ? $"Host started on port {serverPort}"
            : "Failed to start host.");

        return success;
    }

    public bool StartClientLocal()
    {
        if (networkManager == null || unityTransport == null)
            return false;

        if (networkManager.IsListening)
            return true;

        unityTransport.SetConnectionData(serverAddress, serverPort);

        bool success = networkManager.StartClient();
        Debug.Log(success
            ? $"Client started. Connecting to {serverAddress}:{serverPort}"
            : "Failed to start client.");

        return success;
    }

    public void Shutdown()
    {
        if (networkManager != null && networkManager.IsListening)
            networkManager.Shutdown();
    }
}