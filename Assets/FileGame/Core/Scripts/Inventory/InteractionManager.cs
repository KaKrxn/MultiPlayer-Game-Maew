using PurrNet;
using PurrNet.Utils;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float interactDistance = 4f;

    private Camera _cam;
    // ตัวแปร _currentInteractable ไม่ได้ถูกใช้งานในโค้ดส่วนนี้ แต่เก็บไว้ได้หากมีการใช้ระบบ Hover ในอนาคต
    private AInteractable _currentInteractable; 

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        float rayLength = interactDistance * 3f;

        // เส้น debug
        Debug.DrawRay(_cam.transform.position, _cam.transform.forward * rayLength, Color.green);

        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (Physics.Raycast(_cam.transform.position, _cam.transform.forward,
            out RaycastHit hit, rayLength, interactableLayer))
        {
            Debug.DrawLine(_cam.transform.position, hit.point, Color.red);

            var interactables = hit.collider.GetComponents<AInteractable>();

            foreach (var interactable in interactables)
            {
                if (interactable.CanInteract())
                {
                    interactable.Interact();
                    break;
                }
            }
        }
    }

}



public abstract class AInteractable : NetworkBehaviour
{
    public abstract void Interact();

    public virtual void OnHover() {}
    public virtual void OnStopHover() {}
    public virtual bool CanInteract()
    {
        return true;
    }

}
