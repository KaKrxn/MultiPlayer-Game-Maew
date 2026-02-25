using Unity.Netcode;
using UnityEngine;

namespace Game.Train
{
    public class TrainDriverInputDeterministic : NetworkBehaviour
    {
        [Header("Find Train")]
        [SerializeField] private TrainDeterministicMover train;
        [SerializeField] private string trainTag = "Train";
        [SerializeField] private float retryFindEverySeconds = 0.5f;
        private float nextFindTime;

        [Header("Keys")]
        [SerializeField] private KeyCode runToggleKey = KeyCode.F;
        [SerializeField] private KeyCode forwardKey = KeyCode.T;
        [SerializeField] private KeyCode reverseKey = KeyCode.R;
        [SerializeField] private KeyCode brakeKey = KeyCode.B;

        [Header("Speeds")]
        [SerializeField] private float cruiseSpeed = 8f;
        [SerializeField] private float brakeSpeed = 0f; // เบรก = ตั้งสปีดเป็น 0 และหยุด

        private bool runEnabled = false;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            TryFindTrain(true);
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (!train)
            {
                TryFindTrain(false);
                return;
            }

            // Toggle run
            if (Input.GetKeyDown(runToggleKey))
            {
                runEnabled = !runEnabled;

                // ตั้งสปีด cruise ตอนเริ่มวิ่ง (ปรับเองได้)
                if (runEnabled)
                    train.SetSpeedServerRpc(cruiseSpeed);

                train.SetRunServerRpc(runEnabled);
            }

            // Direction toggles
            if (Input.GetKeyDown(forwardKey))
                train.SetDirectionServerRpc(+1);

            if (Input.GetKeyDown(reverseKey))
                train.SetDirectionServerRpc(-1); // ถ้าคุณเดินหน้าอย่างเดียว ก็ไม่ต้องกด R

            // Brake hold: stop immediately while holding
            if (Input.GetKeyDown(brakeKey))
            {
                train.SetSpeedServerRpc(brakeSpeed);
                train.SetRunServerRpc(false);
                runEnabled = false;
            }
        }

        private void TryFindTrain(bool force)
        {
            if (!force && Time.time < nextFindTime) return;
            nextFindTime = Time.time + retryFindEverySeconds;

            if (!string.IsNullOrEmpty(trainTag))
            {
                var go = GameObject.FindGameObjectWithTag(trainTag);
                if (go)
                {
                    train = go.GetComponent<TrainDeterministicMover>();
                    if (train) return;

                    train = go.transform.root.GetComponent<TrainDeterministicMover>();
                    if (train) return;
                }
            }

#if UNITY_2023_1_OR_NEWER
            train = Object.FindFirstObjectByType<TrainDeterministicMover>();
#else
            train = Object.FindObjectOfType<TrainDeterministicMover>();
#endif
        }
    }
}