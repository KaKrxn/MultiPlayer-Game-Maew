using Unity.Netcode;
using UnityEngine;

namespace Game.Train
{
    [RequireComponent(typeof(CharacterController))]
    public class TrainPassengerCC : MonoBehaviour
    {
        [Header("Authority")]
        [SerializeField] private bool ownerOnly = true;

        [Header("Train Identify")]
        [SerializeField] private string trainLayerName = "Train";
        [Tooltip("Optional: leave empty to ignore tag check.")]
        [SerializeField] private string trainTag = "Train";

        [Header("Ground Raycast")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundRayStartUp = 0.2f;
        [SerializeField] private float groundRayLength = 1.2f;

        [Header("Smoothing (delta)")]
        [SerializeField] private float followSmoothing = 0.03f;

        [Header("Stick To Ground")]
        [SerializeField] private float groundStickSpeed = 6f;
        [SerializeField] private float maxStickDistance = 0.25f;

        private CharacterController cc;
        private NetworkObject netObj;

        private int trainLayer = -1;

        private Transform trainRoot;   // collider root we stand on (authoritative)
        private Transform followRef;   // visual ref (smoothed)

        private Vector3 lastPos;
        private Quaternion lastRot;

        private Vector3 smoothedDelta;
        private float lastGroundDistance = float.PositiveInfinity;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            netObj = GetComponent<NetworkObject>();

            trainLayer = LayerMask.NameToLayer(trainLayerName);
            if (trainLayer >= 0)
                groundMask |= (1 << trainLayer);
        }

        private bool ShouldRun()
        {
            if (!ownerOnly) return true;
            if (netObj == null || !netObj.IsSpawned) return true;
            return netObj.IsOwner;
        }

        private void Update()
        {
            if (!ShouldRun()) return;
            DetectTrainUnderfoot();
        }

        private void LateUpdate()
        {
            if (!ShouldRun()) return;
            ApplyTrainDelta();
        }

        private void DetectTrainUnderfoot()
        {
            Vector3 origin = transform.position + Vector3.up * groundRayStartUp;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
            {
                lastGroundDistance = float.PositiveInfinity;
                trainRoot = null;
                followRef = null;
                return;
            }

            lastGroundDistance = hit.distance;

            if (!string.IsNullOrEmpty(trainTag) && !hit.collider.CompareTag(trainTag))
            {
                trainRoot = null;
                followRef = null;
                return;
            }

            Transform root = hit.collider.transform.root;

            if (trainLayer >= 0)
            {
                bool isTrain = root.gameObject.layer == trainLayer || hit.collider.gameObject.layer == trainLayer;
                if (!isTrain)
                {
                    trainRoot = null;
                    followRef = null;
                    return;
                }
            }

            if (trainRoot != root)
            {
                trainRoot = root;

                // Prefer smoothed visual reference if present
                var smoother = trainRoot.GetComponent<TrainVisualSmoother>();
                followRef = smoother != null ? smoother.VisualRoot : trainRoot;

                lastPos = followRef.position;
                lastRot = followRef.rotation;
                smoothedDelta = Vector3.zero;
            }
        }

        private void ApplyTrainDelta()
        {
            if (followRef == null) return;

            Vector3 newPos = followRef.position;
            Quaternion newRot = followRef.rotation;

            Vector3 posDelta = newPos - lastPos;

            Quaternion rotDelta = newRot * Quaternion.Inverse(lastRot);
            Vector3 pivot = newPos;
            Vector3 rel = transform.position - pivot;
            Vector3 rotMove = (rotDelta * rel) - rel;

            Vector3 delta = posDelta + rotMove;

            if (followSmoothing > 0f)
                smoothedDelta = Vector3.Lerp(smoothedDelta, delta, 1f - Mathf.Exp(-Time.deltaTime / followSmoothing));
            else
                smoothedDelta = delta;

            cc.Move(smoothedDelta);

            if (lastGroundDistance <= maxStickDistance)
                cc.Move(Vector3.down * groundStickSpeed * Time.deltaTime);

            lastPos = newPos;
            lastRot = newRot;
        }
    }
}