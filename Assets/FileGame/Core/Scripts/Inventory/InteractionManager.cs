using UnityEngine;
using Unity.Netcode;


public class InteractionManager : NetworkBehaviour
{
    [SerializeField] private Transform interactionSource;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float interactDistance = 3f;

    private AInteractable[] _currentHoveredInteractables; 


    private void Update()
    {
        if (!IsOwner) return;

        if (interactionSource == null)
        {
            // UnityEngine.Debug.LogError($"[InteractionManager] {gameObject.name} interactionSource is MISSING on Client!");
            return;
        }

        HandleHovers();

        Debug.DrawRay(interactionSource.position, interactionSource.forward * interactDistance, Color.green);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Physics.Raycast(interactionSource.position, interactionSource.forward,
                out RaycastHit hit, interactDistance, interactableLayer))
            {
                Debug.DrawLine(interactionSource.position, hit.point, Color.red, 1.0f);

                var interactables = hit.collider.GetComponents<AInteractable>();
                if (interactables == null || interactables.Length == 0)
                    interactables = hit.collider.GetComponentsInParent<AInteractable>();

                if (interactables != null)
                {
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
    }


    private void HandleHovers()
    {
        // ยิง Ray ออกไปเช็คตลอดเวลาเพื่อทำระบบ Highlighting (ขอบขาว/สีเปลี่ยน)
        if (!Physics.Raycast(interactionSource.position, interactionSource.forward, 
            out RaycastHit hit, interactDistance, interactableLayer))
        {
            ClearHover();
            return;
        }

        var interactables = hit.collider.GetComponents<AInteractable>();
        if (interactables == null || interactables.Length == 0)
        {
            interactables = hit.collider.GetComponentsInParent<AInteractable>();
        }

        if (interactables == null || interactables.Length == 0)
        {
            ClearHover();
            return;
        }

        // เช็คว่ายังเป็นวัตถุชิ้นเดิมไหม
        if (_currentHoveredInteractables != null && _currentHoveredInteractables.Length > 0 && 
            hit.collider.gameObject == _currentHoveredInteractables[0].gameObject)
        {
            return;
        }

        ClearHover();

        _currentHoveredInteractables = interactables;
        foreach (var interactable in interactables)
        {
            if (interactable.CanInteract())
            {
                interactable.OnHover();
            }
        }
    }

    private void ClearHover()
    {
        if (_currentHoveredInteractables == null) return;

        foreach (var interactable in _currentHoveredInteractables)
        {
            if (interactable != null) interactable.OnStopHover();
        }

        _currentHoveredInteractables = null;
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