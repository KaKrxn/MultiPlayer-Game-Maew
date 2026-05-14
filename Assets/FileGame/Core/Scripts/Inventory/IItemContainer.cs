// ประเภทของ container ที่ slot สังกัด
public enum ContainerKind
{
    MainInventory,
    Vault
}

// Marker interface สำหรับ container ของ Slot
// ใช้ตอน dispatch การลาก-วางข้าม container (Inventory <-> Vault)
public interface IItemContainer
{
    ContainerKind Kind { get; }
}
