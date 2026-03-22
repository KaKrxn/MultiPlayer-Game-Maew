using UnityEngine;


public class Item : AInteractable 
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemPicture;

    public string ItemName => itemName;
    public Sprite ItemPicture => itemPicture;

    private DestroyNetworkItemSync _networkSync;

    private void Awake()
    {
        _networkSync = GetComponent<DestroyNetworkItemSync>();
    }

    [ContextMenu("Pickup Item")]
    public void Pickup()
    {
        if (_networkSync != null)
        {
            _networkSync.RequestPickup();
        }
        else
        {
            Debug.LogWarning("ไม่มีสคริปต์ DestroyNetworkItemSync แปะอยู่บนไอเทมชิ้นนี้!");
        }
    }

    public override void Interact()
    {
        Pickup();
    }

    public override void OnHover()
    {
        base.OnHover();
    }

    public override void OnStopHover()
    {
        base.OnStopHover();
    }
}