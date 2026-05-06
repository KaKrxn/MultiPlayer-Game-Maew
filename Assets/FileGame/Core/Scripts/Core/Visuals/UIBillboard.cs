using UnityEngine;

namespace Blocks.Gameplay.Core
{
    public enum BillboardType
    {
        LookAtCamera,
        CameraForward
    }

    /// <summary>
    /// A generic component suitable for any Local Space Player UI.
    /// Rotates the object to face the main camera, offering multi-mode billboarding.
    /// </summary>
    public class UIBillboard : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private BillboardType billboardType = BillboardType.CameraForward;
        
        [Header("Axis Constraints")]
        [SerializeField] private bool lockX = false;
        [SerializeField] private bool lockY = false;
        [SerializeField] private bool lockZ = false;

        private Camera m_CachedCamera;

        private void LateUpdate()
        {
            if (m_CachedCamera == null)
            {
                m_CachedCamera = Camera.main;
            }

            if (m_CachedCamera == null)
            {
                return;
            }

            Vector3 originalEulerAngles = transform.rotation.eulerAngles;

            switch (billboardType)
            {
                case BillboardType.LookAtCamera:
                    transform.LookAt(transform.position + m_CachedCamera.transform.rotation * Vector3.forward,
                                     m_CachedCamera.transform.rotation * Vector3.up);
                    break;
                case BillboardType.CameraForward:
                    // Makes the UI exactly parallel to the camera viewing plane
                    transform.forward = m_CachedCamera.transform.forward;
                    break;
            }

            // Apply axis constraints
            Vector3 finalEulerAngles = transform.rotation.eulerAngles;
            
            if (lockX) finalEulerAngles.x = originalEulerAngles.x;
            if (lockY) finalEulerAngles.y = originalEulerAngles.y;
            if (lockZ) finalEulerAngles.z = originalEulerAngles.z;

            if (lockX || lockY || lockZ)
            {
                transform.rotation = Quaternion.Euler(finalEulerAngles);
            }
        }
    }
}
