using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace Game.Train
{
    public class TrainMovementServer : NetworkBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Component ant; // AutomatedNetworkTransform (drag here)

        [Header("Driver Lock")]
        [SerializeField] private bool lockToSingleDriver = true;
        private ulong driverClientId = ulong.MaxValue;

        [Header("Anti-spam")]
        [SerializeField] private float minCommandInterval = 0.05f;
        private readonly System.Collections.Generic.Dictionary<ulong, float> lastCmdTime = new();

        // reflection cache
        private FieldInfo moveSpeedField;

        // stored speed so we can restore when resuming
        private float cachedMoveSpeed = 5f; // fallback if we can't read
        private bool hasCachedMoveSpeed = false;

        private void Awake()
        {
            if (!ant) ant = GetComponent("AutomatedNetworkTransform");
            CacheMoveSpeedField();
        }

        private void CacheMoveSpeedField()
        {
            moveSpeedField = null;
            if (!ant) return;

            var t = ant.GetType();
            const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            string[] candidates = { "moveSpeed", "m_MoveSpeed", "MoveSpeed", "m_WaypointMoveSpeed" };
            foreach (var name in candidates)
            {
                var f = t.GetField(name, F);
                if (f != null && f.FieldType == typeof(float))
                {
                    moveSpeedField = f;
                    return;
                }
            }

            // heuristic fallback
            foreach (var f in t.GetFields(F))
            {
                if (f.FieldType != typeof(float)) continue;
                var n = f.Name.ToLowerInvariant();
                if (n.Contains("move") && n.Contains("speed"))
                {
                    moveSpeedField = f;
                    return;
                }
            }
        }

        private bool TryGetANTMoveSpeed(out float speed)
        {
            speed = 0f;
            if (!ant) return false;
            if (moveSpeedField == null) CacheMoveSpeedField();
            if (moveSpeedField == null) return false;

            speed = (float)moveSpeedField.GetValue(ant);
            return true;
        }

        private void SetANTMoveSpeed(float speed)
        {
            if (!ant) return;
            if (moveSpeedField == null) CacheMoveSpeedField();
            if (moveSpeedField != null)
                moveSpeedField.SetValue(ant, speed);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            // Cache initial inspector speed once
            if (TryGetANTMoveSpeed(out var s))
            {
                cachedMoveSpeed = s;
                hasCachedMoveSpeed = true;
            }
        }

        /// <summary>
        /// Toggle run (true=resume, false=stop). No speed calculation here.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetRunEnabledServerRpc(bool runEnabled, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;

            if (lockToSingleDriver)
            {
                if (driverClientId == ulong.MaxValue) driverClientId = sender;
                if (sender != driverClientId) return;
            }

            float now = Time.time;
            if (lastCmdTime.TryGetValue(sender, out float last) && now - last < minCommandInterval)
                return;
            lastCmdTime[sender] = now;

            if (!ant) return;

            if (runEnabled)
            {
                // Restore last known speed (or current inspector value if we can read)
                if (!hasCachedMoveSpeed && TryGetANTMoveSpeed(out var current))
                {
                    cachedMoveSpeed = current;
                    hasCachedMoveSpeed = true;
                }

                SetANTMoveSpeed(cachedMoveSpeed);
            }
            else
            {
                // Cache current speed before stopping (so resume returns to it)
                if (TryGetANTMoveSpeed(out var current))
                {
                    cachedMoveSpeed = current;
                    hasCachedMoveSpeed = true;
                }

                SetANTMoveSpeed(0f);
            }
        }
    }
}