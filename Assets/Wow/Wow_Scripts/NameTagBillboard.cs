using UnityEngine;

public class NameTagBillboard : MonoBehaviour
{
    private Camera cachedCamera;

    private void LateUpdate()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera == null)
            return;

        transform.forward = cachedCamera.transform.forward;
    }
}