using Unity.Netcode;
using UnityEngine;

namespace Game.Train
{
    public class TrainDriverInput : NetworkBehaviour
    {
        [Header("Auto Find Train")]
        [SerializeField] private TrainMovementServer trainMovement;
        [SerializeField] private string trainTag = "Train";
        [SerializeField] private float retryFindEverySeconds = 0.5f;
        private float nextFindTime;

        [Header("Key")]
        [SerializeField] private KeyCode runToggleKey = KeyCode.F;

        private bool runEnabled;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            TryAutoFindTrain(true);
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (!trainMovement)
            {
                TryAutoFindTrain(false);
                return;
            }

            if (Input.GetKeyDown(runToggleKey))
            {
                runEnabled = !runEnabled;
                trainMovement.SetRunEnabledServerRpc(runEnabled);
            }
        }

        private void TryAutoFindTrain(bool force)
        {
            if (!force && Time.time < nextFindTime) return;
            nextFindTime = Time.time + retryFindEverySeconds;

            if (!string.IsNullOrEmpty(trainTag))
            {
                var go = GameObject.FindGameObjectWithTag(trainTag);
                if (go)
                {
                    trainMovement = go.GetComponent<TrainMovementServer>();
                    if (trainMovement) return;

                    trainMovement = go.transform.root.GetComponent<TrainMovementServer>();
                    if (trainMovement) return;
                }
            }

#if UNITY_2023_1_OR_NEWER
            trainMovement = Object.FindFirstObjectByType<TrainMovementServer>();
#else
            trainMovement = Object.FindObjectOfType<TrainMovementServer>();
#endif
        }
    }
}