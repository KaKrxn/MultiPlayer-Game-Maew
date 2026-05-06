using UnityEngine;
using Unity.Netcode;

namespace FileGame.Core
{
    /// <summary>
    /// Shared server-side utility methods.
    /// Eliminates duplicated helper code across DestroyNetworkItemSync, PlayerDropItem, etc.
    /// </summary>
    public static class ServerUtility
    {
        /// <summary>
        /// Applies a weight change to a player's survival system.
        /// Positive kgDelta = gained weight, negative = lost weight.
        /// Must be called on server only.
        /// </summary>
        public static void ApplyWeightToPlayer(ulong clientId, float kgDelta)
        {
            if (NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var clientData)) return;

            NetworkObject playerObject = clientData.PlayerObject;
            if (playerObject == null) return;

            if (playerObject.TryGetComponent<PlayerSurvivalSystem>(out var survivalSystem))
            {
                survivalSystem.ApplyCarriedWeightDelta(kgDelta);
            }
        }
    }
}
