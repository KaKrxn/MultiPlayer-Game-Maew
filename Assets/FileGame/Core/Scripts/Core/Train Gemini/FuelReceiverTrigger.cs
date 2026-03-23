using UnityEngine;
using Unity.Netcode;

public class FuelReceiverTrigger : NetworkBehaviour
{
    [Tooltip("ลากคอมโพเนนต์ TrainFuelSystem บนรถไฟมาใส่ช่องนี้")]
    public TrainFuelSystem fuelSystem;

    [Tooltip("ชื่อไอเทมที่จะอนุญาตให้เป็นเชื้อเพลิง (ต้องตรงกับ Item Name ในสคริปต์ Item)")]
    public string targetFuelItemName = "Fuel";

    [Tooltip("ปริมาณน้ำมันที่จะเพิ่มให้รถไฟ ต่อ 1 ถัง")]
    public float fuelRefillAmount = 25f;

    private void OnTriggerEnter(Collider other)
    {
        // 🚨 ให้ Server เป็นคนจัดการ เพื่อให้ข้อมูลตรงกันทั้งห้อง
        if (!IsServer) return;

        // 1. พยายามดึงสคริปต์ Item จากวัตถุที่หล่นลงมาชน
        Item droppedItem = other.GetComponent<Item>();

        // 2. ถ้าวัตถุนั้นเป็นไอเทม (มีสคริปต์ Item) และชื่อตรงกับที่เราต้องการ
        if (droppedItem != null && droppedItem.ItemName == targetFuelItemName)
        {
            // 3. สั่งเติมน้ำมันเข้าระบบรถไฟ
            if (fuelSystem != null)
            {
                fuelSystem.AddFuel(fuelRefillAmount);
                Debug.Log($"[Server] ได้รับเชื้อเพลิง ({droppedItem.ItemName})! พลังงานรถไฟเพิ่ม {fuelRefillAmount}");
            }

            // 4. ทำลายไอเทมนั้นทิ้ง (ผ่าน Network)
            NetworkObject fuelNetObj = other.GetComponent<NetworkObject>();
            if (fuelNetObj != null && fuelNetObj.IsSpawned)
            {
                // สั่ง Despawn เพื่อลบออกจากระบบ Network (จะหายไปจากจอทุกคนพร้อมกัน)
                fuelNetObj.Despawn(true);
            }
            else
            {
                // (กันเหนียว) ถ้าของชิ้นนั้นไม่มี NetworkObject ก็ลบแบบปกติ
                Destroy(other.gameObject);
            }
        }
    }
}