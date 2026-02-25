using UnityEngine;

namespace Game.Train
{
    /// <summary>
    /// Smooths a Visual child transform to follow the authoritative Train root.
    /// Root stays authoritative for physics/network; Visual is purely client-side smoothing.
    /// Works for Host too (fixes step-like motion on authority).
    /// </summary>
    public class TrainVisualSmoother : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Assign a child transform that contains meshes/FX (visual only).")]
        [SerializeField] private Transform visualRoot;

        [Header("Smoothing")]
        [Tooltip("Position smoothing time (smaller = tighter, larger = smoother).")]
        [SerializeField] private float positionSmoothTime = 0.06f;

        [Tooltip("Rotation smoothing time (smaller = tighter, larger = smoother).")]
        [SerializeField] private float rotationSmoothTime = 0.06f;

        private Vector3 posVel;

        public Transform VisualRoot => visualRoot != null ? visualRoot : transform;

        private void Awake()
        {
            // Auto-create if none assigned (optional)
            if (visualRoot == null)
            {
                var go = new GameObject("TrainVisual");
                go.transform.SetParent(transform, true);
                go.transform.position = transform.position;
                go.transform.rotation = transform.rotation;
                visualRoot = go.transform;
            }
        }

        private void LateUpdate()
        {
            if (visualRoot == null) return;

            // Smooth position
            visualRoot.position = Vector3.SmoothDamp(
                visualRoot.position,
                transform.position,
                ref posVel,
                Mathf.Max(0.0001f, positionSmoothTime)
            );

            // Smooth rotation
            float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, rotationSmoothTime));
            visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, transform.rotation, t);
        }
    }
}