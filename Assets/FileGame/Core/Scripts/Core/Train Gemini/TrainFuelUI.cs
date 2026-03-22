using UnityEngine;
using UnityEngine.UI;

public class TrainFuelUI : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("ลาก Image ตัวที่เป็น FuelBar_Fill มาใส่ช่องนี้")]
    public Image fuelFillImage;

    private TrainFuelSystem fuelSystem;

    private void Update()
    {
        // 1. ค้นหาระบบน้ำมันของรถไฟในฉาก (เผื่อรถไฟเพิ่งโหลดเข้ามาผ่านระบบ Multiplayer)
        if (fuelSystem == null)
        {
            fuelSystem = FindFirstObjectByType<TrainFuelSystem>();

            // ถ้ายังหาไม่เจอ ก็รอไปก่อน (return ออกไปรอบหน้าค่อยหาใหม่)
            if (fuelSystem == null) return;
        }

        // 2. คำนวณเปอร์เซ็นต์น้ำมัน (เอาค่าปัจจุบัน หาร ค่าสูงสุด จะได้เลข 0.0 ถึง 1.0)
        float fuelPercentage = fuelSystem.currentFuel.Value / fuelSystem.maxFuel;

        // 3. สั่งให้ภาพ UI หดหรือขยายตามเปอร์เซ็นต์น้ำมัน
        if (fuelFillImage != null)
        {
            fuelFillImage.fillAmount = fuelPercentage;
        }
    }
}