using UnityEngine;

public class VaultInventory : AInteractable
{
    [SerializeField] private int slotCount = 20;

    private InventoryManager.InventoryItemData[] storedItems;

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

    public override void Interact()
    {
        if (!CompareTag("Vault")) return;

        if (InventoryManager.instance == null) return;

        InventoryManager.instance.OpenVault(this);
    }

    public override bool CanInteract()
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
