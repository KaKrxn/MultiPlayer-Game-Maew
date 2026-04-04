using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Blocks.Gameplay.Core
{
    [RequireComponent(typeof(GameNetworkManager))]
    [RequireComponent(typeof(UnityTransport))]
    public class RelayRoomService : MonoBehaviour
    {
        public static RelayRoomService Instance { get; private set; }

        [Header("Relay Settings")]
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private string connectionType = "dtls";

        public string CurrentJoinCode { get; private set; } = "----";
        public string StatusMessage { get; private set; } = "Ready";

        private GameNetworkManager manager;
        private UnityTransport transport;
        private bool servicesReady;
        private bool isBusy;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            manager = GetComponent<GameNetworkManager>();
            transport = GetComponent<UnityTransport>();

            if (manager == null)
                Debug.LogError("RelayRoomService: GameNetworkManager not found.");

            if (transport == null)
                Debug.LogError("RelayRoomService: UnityTransport not found.");
        }

        public async void CreateRoom(string playerName)
        {
            await CreateRoomAsync(playerName);
        }

        public async Task<bool> CreateRoomAsync(string playerName)
        {
            if (isBusy || manager == null || transport == null)
                return false;

            isBusy = true;
            StatusMessage = "Creating room...";

            try
            {
                if (!await EnsureServicesReadyAsync())
                {
                    StatusMessage = "Unity Services init failed";
                    return false;
                }

                manager.PlayerName = SanitizeName(playerName);

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, maxPlayers - 1));
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

                CurrentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                manager.StartHostConnection();
                StatusMessage = "Room created";
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"RelayRoomService CreateRoom failed: {ex}");
                CurrentJoinCode = "----";
                StatusMessage = "Create room failed";
                return false;
            }
            finally
            {
                isBusy = false;
            }
        }

        public async void JoinRoom(string joinCode, string playerName)
        {
            await JoinRoomAsync(joinCode, playerName);
        }

        public async Task<bool> JoinRoomAsync(string joinCode, string playerName)
        {
            if (isBusy || manager == null || transport == null)
                return false;

            isBusy = true;
            StatusMessage = "Joining room...";

            try
            {
                if (!await EnsureServicesReadyAsync())
                {
                    StatusMessage = "Unity Services init failed";
                    return false;
                }

                joinCode = (joinCode ?? "").Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(joinCode))
                {
                    StatusMessage = "Please enter a join code";
                    return false;
                }

                manager.PlayerName = SanitizeName(playerName);

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, connectionType));

                CurrentJoinCode = joinCode;

                manager.StartClientConnection();
                StatusMessage = "Joined room";
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"RelayRoomService JoinRoom failed: {ex}");
                StatusMessage = "Join failed";
                return false;
            }
            finally
            {
                isBusy = false;
            }
        }

        public void DisconnectRoom()
        {
            CurrentJoinCode = "----";
            StatusMessage = "Disconnected";

            if (manager != null)
                manager.Disconnect();
        }

        private async Task<bool> EnsureServicesReadyAsync()
        {
            try
            {
                if (!servicesReady)
                {
                    if (UnityServices.State != ServicesInitializationState.Initialized)
                        await UnityServices.InitializeAsync();

                    if (!AuthenticationService.Instance.IsSignedIn)
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();

                    servicesReady = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"RelayRoomService EnsureServicesReadyAsync failed: {ex}");
                return false;
            }
        }

        private string SanitizeName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return "Player";

            playerName = playerName.Trim();

            if (playerName.Length > 20)
                playerName = playerName.Substring(0, 20);

            return playerName;
        }
    }
}