using Unity.Netcode;
using UnityEngine;

namespace Game.Train
{
    public class TrainDeterministicMover : NetworkBehaviour
    {
        [Header("Rail")]
        [SerializeField] private RailPath rail;
        [SerializeField] private bool loopRail = false; // ถ้าเป็นรางวน

        [Header("State (synced only on changes)")]
        public NetworkVariable<bool> IsRunning = new(false);
        public NetworkVariable<int> Direction = new(1); // 1 forward, -1 reverse (ถ้าจะเดินหน้าอย่างเดียวให้คง 1)
        public NetworkVariable<float> Speed = new(8f);  // units/sec (คงที่)
        public NetworkVariable<double> StartServerTime = new(0); // server time at last state change
        public NetworkVariable<float> StartDistance = new(0f);   // distance at StartServerTime

        [Header("Optional: rotate along rail")]
        [SerializeField] private bool rotateAlongRail = true;

        private void Awake()
        {
            if (!rail) rail = FindFirstObjectByType<RailPath>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // initialize start time so all clients align
                StartServerTime.Value = NetworkManager.Singleton.ServerTime.Time;
                StartDistance.Value = 0f;
                Direction.Value = 1;
                IsRunning.Value = false;
            }
        }

        private void Update()
        {
            if (!rail || rail.TotalLength <= 0.0001f) return;
            if (!NetworkManager.Singleton) return;

            double now = NetworkManager.Singleton.ServerTime.Time; // IMPORTANT: shared clock
            double dt = now - StartServerTime.Value;
            if (dt < 0) dt = 0;

            float distance = StartDistance.Value;

            if (IsRunning.Value)
            {
                distance += (float)dt * Speed.Value * Mathf.Sign(Direction.Value);
            }

            if (loopRail)
            {
                float L = rail.TotalLength;
                distance = Mathf.Repeat(distance, L);
            }
            else
            {
                distance = Mathf.Clamp(distance, 0f, rail.TotalLength);
            }

            rail.Sample(distance, out var pos, out var fwd);

            transform.position = pos;
            if (rotateAlongRail)
            {
                // ถ้าถอยหลังจริงให้หันกลับ
                Vector3 facing = (Direction.Value < 0) ? -fwd : fwd;
                transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            }
        }

        // ---- Server-side event APIs ----
        [ServerRpc(RequireOwnership = false)]
        public void SetRunServerRpc(bool run)
        {
            CommitDistanceNow();
            IsRunning.Value = run;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetDirectionServerRpc(int dir)
        {
            dir = dir >= 0 ? 1 : -1;
            CommitDistanceNow();
            Direction.Value = dir;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetSpeedServerRpc(float speed)
        {
            speed = Mathf.Max(0f, speed);
            CommitDistanceNow();
            Speed.Value = speed;
        }

        private void CommitDistanceNow()
        {
            // When changing any state, convert "time-based" position into a new start snapshot
            double now = NetworkManager.Singleton.ServerTime.Time;
            double dt = now - StartServerTime.Value;
            if (dt < 0) dt = 0;

            float distance = StartDistance.Value;

            if (IsRunning.Value)
                distance += (float)dt * Speed.Value * Mathf.Sign(Direction.Value);

            if (rail && rail.TotalLength > 0.0001f)
            {
                if (loopRail) distance = Mathf.Repeat(distance, rail.TotalLength);
                else distance = Mathf.Clamp(distance, 0f, rail.TotalLength);
            }

            StartDistance.Value = distance;
            StartServerTime.Value = now;
        }
    }
}