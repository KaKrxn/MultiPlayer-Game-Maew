using Unity.Netcode;
using UnityEngine;

namespace Game.Train
{
    /// <summary>
    /// Train shared state synced to all clients (server-authoritative).
    /// Only server should write to most variables.
    /// </summary>
    public class TrainState : NetworkBehaviour
    {
        // --- Runtime State (synced) ---
        public NetworkVariable<float> Speed = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<bool> IsMoving = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> Fuel = new(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public NetworkVariable<float> Hp = new(
            200f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // --- Config (not synced by default; same prefab values for all) ---
        [Header("Config")]
        [Min(0f)] public float MaxSpeed = 12f;
        [Min(0f)] public float Accel = 4f;
        [Min(0f)] public float BrakePower = 8f;

        [Header("Fuel Config")]
        [Min(0f)] public float FuelCapacity = 100f;
        [Min(0f)] public float FuelRate = 0.6f;

        [Header("Health Config")]
        [Min(0f)] public float MaxHp = 200f;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            // Server initializes authoritative values.
            Speed.Value = 0f;
            IsMoving.Value = false;

            // Clamp initial values to config
            Fuel.Value = Mathf.Clamp(Fuel.Value, 0f, FuelCapacity);
            Hp.Value = Mathf.Clamp(Hp.Value, 0f, MaxHp);
        }

        // Server-only helpers (optional but handy)
        [ContextMenu("Server: Refill Fuel")]
        private void EditorRefillFuel()
        {
            if (IsServer) Fuel.Value = FuelCapacity;
        }

        [ContextMenu("Server: Full Heal")]
        private void EditorFullHeal()
        {
            if (IsServer) Hp.Value = MaxHp;
        }
    }
}