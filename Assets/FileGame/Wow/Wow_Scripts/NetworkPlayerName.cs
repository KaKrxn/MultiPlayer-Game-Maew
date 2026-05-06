using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

namespace Blocks.Gameplay.Core
{
    public class NetworkPlayerName : NetworkBehaviour
    {
        [Header("Name Tag")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private string fallbackName = "Player";

        private NetworkVariable<FixedString64Bytes> networkPlayerName =
            new NetworkVariable<FixedString64Bytes>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public override void OnNetworkSpawn()
        {
            networkPlayerName.OnValueChanged += OnPlayerNameChanged;

            UpdateNameVisual(networkPlayerName.Value.ToString());

            if (IsOwner)
            {
                string localName = GetLocalPlayerName();

                if (IsServer)
                {
                    ApplyPlayerName(localName);
                }
                else
                {
                    SubmitPlayerNameServerRpc(localName);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            networkPlayerName.OnValueChanged -= OnPlayerNameChanged;
        }

        private string GetLocalPlayerName()
        {
            if (!string.IsNullOrWhiteSpace(PlayerRuntimeData.LocalPlayerName))
                return PlayerRuntimeData.LocalPlayerName;

            return PlayerPrefs.GetString("PlayerName", fallbackName);
        }

        private void OnPlayerNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
        {
            UpdateNameVisual(newValue.ToString());
        }

        private void UpdateNameVisual(string newName)
        {
            if (nameText != null)
                nameText.text = newName;
        }

        private void ApplyPlayerName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                newName = fallbackName;

            if (newName.Length > 20)
                newName = newName.Substring(0, 20);

            networkPlayerName.Value = new FixedString64Bytes(newName);
            UpdateNameVisual(newName);
        }

        [ServerRpc]
        private void SubmitPlayerNameServerRpc(string newName)
        {
            ApplyPlayerName(newName);
        }
    }
}