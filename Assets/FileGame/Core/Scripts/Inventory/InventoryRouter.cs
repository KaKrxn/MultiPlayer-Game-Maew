using UnityEngine;

/// <summary>
/// Dispatch การย้าย-วางของระหว่าง InventorySlot ทั้งภายใน container เดียวกัน
/// และข้าม container (Inventory <-> Vault) ผ่าน Server RPC ของแต่ละระบบ
/// </summary>
public static class InventoryRouter
{
    public static void HandleItemMove(InventorySlot src, InventorySlot dst, InventoryItem draggedItem)
    {
        if (src == null || dst == null || draggedItem == null) return;
        if (src == dst) return;
        if (src.Container == null || dst.Container == null)
        {
            Debug.LogWarning("[InventoryRouter] Slot has no container bound. Did you call slot.Bind()?");
            return;
        }

        var srcKind = src.Container.Kind;
        var dstKind = dst.Container.Kind;

        // กรณี: ทั้งสองช่องเป็นกระเป๋าหลัก -> ใช้ flow เดิมของ InventoryManager
        if (srcKind == ContainerKind.MainInventory && dstKind == ContainerKind.MainInventory)
        {
            if (InventoryManager.instance != null)
            {
                InventoryManager.instance.ItemMoved(draggedItem, dst);
            }
            return;
        }

        // กรณี: เกี่ยวข้องกับ Vault -> ส่งให้ NetworkVault จัดการ
        var srcVault = src.Container as NetworkVault;
        var dstVault = dst.Container as NetworkVault;

        // ภายใน vault เดียวกัน
        if (srcKind == ContainerKind.Vault && dstKind == ContainerKind.Vault && srcVault != null && srcVault == dstVault)
        {
            srcVault.RequestMoveWithinVaultServerRpc(src.Index, dst.Index);
            return;
        }

        // จาก Inventory -> Vault
        if (srcKind == ContainerKind.MainInventory && dstKind == ContainerKind.Vault && dstVault != null)
        {
            dstVault.RequestTransferToVaultServerRpc(src.Index, dst.Index);
            return;
        }

        // จาก Vault -> Inventory
        if (srcKind == ContainerKind.Vault && dstKind == ContainerKind.MainInventory && srcVault != null)
        {
            srcVault.RequestTransferFromVaultServerRpc(src.Index, dst.Index);
            return;
        }

        Debug.LogWarning($"[InventoryRouter] Unsupported container move: {srcKind} -> {dstKind}");
    }
}
