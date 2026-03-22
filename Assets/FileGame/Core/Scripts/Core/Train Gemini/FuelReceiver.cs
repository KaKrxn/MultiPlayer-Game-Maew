using UnityEngine;
using Unity.Netcode;

public class FuelReceiver : AInteractable
{
    [Tooltip("ลากระบบน้ำมันของรถไฟ (TrainFuelSystem) มาใส่ช่องนี้")]
    public TrainFuelSystem fuelSystem;

    [Tooltip("ชื่อไอเทมน้ำมัน (ต้องตรงกับ ItemName ในสคริปต์ Item ของคุณ)")]
    public string fuelItemName = "Fuel";

    [Tooltip("ปริมาณน้ำมันที่จะเพิ่มให้รถไฟ ต่อการใช้ 1 ถัง")]
    public float fuelRefillAmount = 25f;

    // ระบบจะเรียกฟังก์ชันนี้ตอนที่คุณหันหน้าไปมองเตาเผาแล้วกดปุ่ม E
    public override void Interact()
    {
        // ทำงานฝั่ง Client ที่เป็นคนกดปุ่ม E
        if (InventoryManager.instance == null) return;

        // 1. ให้กระเป๋าลองค้นหาและหักไอเทมตามชื่อที่ตั้งไว้
        if (InventoryManager.instance.ConsumeItem(fuelItemName))
        {
            // 2. ถ้าหักไอเทมในกระเป๋าสำเร็จ ส่งคำสั่ง Rpc ไปบอก Server ให้เติมน้ำมัน
            AddFuelServerRpc(fuelRefillAmount);
            Debug.Log($"[Client] ใช้ไอเทม {fuelItemName} เติมน้ำมันสำเร็จ!");
        }
        else
        {
            Debug.LogWarning($"[Client] ไม่มีไอเทม {fuelItemName} ในกระเป๋า!");
        }
    }

    // ต้องให้ Server เป็นคนรันคำสั่งอัปเดตน้ำมัน เพราะระบบน้ำมันคุณอ้างอิงจาก Server
    // [RequireOwnership = false] เพราะผู้เล่นที่กด ไม่ใช่เจ้าของรถไฟ
    [ServerRpc(RequireOwnership = false)]
    private void AddFuelServerRpc(float amount)
    {
        if (fuelSystem != null)
        {
            fuelSystem.AddFuel(amount);
        }
    }

    // (ออปชันเสริม) เอาไว้แสดง UI ขอบสว่างตอนเอาเมาส์ชี้ได้ ถ้าคุณมีระบบรองรับ
    public override void OnHover()
    {
        base.OnHover();
        // Debug.Log("มองเตาเผาอยู่...");
    }
}