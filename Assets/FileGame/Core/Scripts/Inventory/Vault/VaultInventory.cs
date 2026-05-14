using UnityEngine;
using Blocks.Gameplay.Core; // 1. เรียกใช้ Namespace ระบบ Interaction ของคุณ

// 2. เปลี่ยนจาก AInteractable เป็น MonoBehaviour และ IInteractable
[RequireComponent(typeof(NetworkVault))]
public class VaultInventory : MonoBehaviour, IInteractable
{
    [SerializeField] private int slotCount = 20;

    private InventoryManager.InventoryItemData[] storedItems;

    // 3. เพิ่ม Property ทั้งหมดที่ IInteractable บังคับให้ต้องมี
    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => 0;
    public string InteractionPromptText => "Open Vault"; 

    private void Awake()
    {
        EnsureStorageSize(slotCount);

        if (!CompareTag("Vault"))
        {
            Debug.LogWarning($"{name} has VaultInventory but its tag is not Vault.");
        }
    }

    public InventoryManager.InventoryItemData[] GetData(int requiredSlotCount)
    {
        EnsureStorageSize(requiredSlotCount);
        return storedItems;
    }

    public void SetData(InventoryManager.InventoryItemData[] data)
    {
        storedItems = data;
        EnsureStorageSize(slotCount);
    }

    // 4. ลบ override ออก และเติมรับพารามิเตอร์ (GameObject interactor)
    public void Interact(GameObject interactor)
    {
        if (!CompareTag("Vault")) return;

        if (InventoryManager.instance == null) return;

        VaultUI.Instance?.Open(GetComponent<NetworkVault>());
    }

    // 5. ลบ override ออก และเติมรับพารามิเตอร์ (GameObject interactor)
    public bool CanInteract(GameObject interactor)
    {
        return CompareTag("Vault");
    }

    private void EnsureStorageSize(int requiredSlotCount)
    {
        int targetSize = Mathf.Max(slotCount, requiredSlotCount);

        if (storedItems != null && storedItems.Length == targetSize) return;

        var resizedItems = new InventoryManager.InventoryItemData[targetSize];

        if (storedItems != null)
        {
            int copyCount = Mathf.Min(storedItems.Length, resizedItems.Length);
            System.Array.Copy(storedItems, resizedItems, copyCount);
        }

        storedItems = resizedItems;
    }
}