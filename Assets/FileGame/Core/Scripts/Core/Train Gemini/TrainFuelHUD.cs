using UnityEngine;
using UnityEngine.UI;

public class TrainFuelHUD : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("ลากตัว Slider ของหลอดน้ำมันมาใส่ช่องนี้")]
    public Slider fuelSlider;

    [Header("Train Reference (Auto-Found)")]
    [Tooltip("ไม่ต้องลากใส่แล้ว โค้ดจะค้นหารถไฟในฉากให้เองตอนที่มันถูก Spawn")]
    public TrainFuelSystem fuelSystem;

    [Tooltip("ความเร็วในการเลื่อนหลอดน้ำมันให้สมูท")]
    public float smoothSpeed = 5f;

    private void Start()
    {
        if (fuelSlider != null)
        {
            // บังคับให้ Slider มีค่าระหว่าง 0 ถึง 1 (คิดเป็นเปอร์เซ็นต์)
            fuelSlider.minValue = 0f;
            fuelSlider.maxValue = 1f;
        }
    }

    private void Update()
    {
        // 1. ระบบค้นหาอัตโนมัติ: ถ้ายังไม่มีข้อมูลรถไฟ ให้พยายามหาในฉาก
        if (fuelSystem == null)
        {
            // ค้นหา Object ที่มีสคริปต์ TrainFuelSystem แปะอยู่
            fuelSystem = FindObjectOfType<TrainFuelSystem>();

            // ถ้าหาไม่เจอ (รถไฟอาจจะยังไม่ Spawn) ก็ให้ออกจากฟังก์ชันไปก่อน รอหาใหม่เฟรมหน้า
            if (fuelSystem == null) return;

            Debug.Log("[TrainFuelHUD] ค้นพบระบบรถไฟแล้ว! ทำการเชื่อมต่อหลอดน้ำมันอัตโนมัติ");
        }

        // 2. ถ้าหา Slider UI ไม่เจอ ให้หยุดทำงาน
        if (fuelSlider == null) return;

        // 3. ดึงค่าปัจจุบันจากระบบ (ใช้ .Value เพราะเป็น NetworkVariable)
        float currentFuel = fuelSystem.currentFuel.Value;
        float maxFuel = fuelSystem.maxFuel;

        // คำนวณหาเปอร์เซ็นต์ (เช่น 50 / 100 = 0.5)
        float targetPercentage = currentFuel / maxFuel;

        // ทำให้หลอดขยับแบบสมูท (Lerp) ไม่กระตุกเวลาน้ำมันลดหรือเพิ่ม
        fuelSlider.value = Mathf.Lerp(fuelSlider.value, targetPercentage, Time.deltaTime * smoothSpeed);
    }
}