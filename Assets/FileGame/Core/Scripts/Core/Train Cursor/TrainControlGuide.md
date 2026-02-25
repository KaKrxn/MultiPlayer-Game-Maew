# 🎮 Train Control & Setup Guide

## 📋 สารบัญ
1. [วิธีควบคุม Train](#1-วิธีควบคุม-train)
2. [Setup Module System](#2-setup-module-system)
3. [Setup TrainUI](#3-setup-trainui)

---

## 1. วิธีควบคุม Train

### **วิธีที่ 1: ใช้ TrainInputHandler (แนะนำ)**

#### **Setup:**
1. **บน Player GameObject**:
   - หา Player GameObject ใน Scene (หรือ prefab)
   - เพิ่ม Component `TrainInputHandler`
   - Configure:
     - `Train Controller` = ลาก Train GameObject จาก Scene มาใส่ (หรือจะหา auto)
     - `Throttle Key` = W (เร่ง)
     - `Brake Key` = S (เบรก)
     - `Refuel Key` = R (เติมน้ำมัน)

2. **หรือสร้าง GameObject แยก**:
   ```
   สร้าง GameObject ใหม่ชื่อ "TrainInputController"
   → เพิ่ม TrainInputHandler
   → Assign Train Controller
   ```

#### **วิธีใช้งาน:**
- กด **W** = เร่งรถไฟ
- กด **S** = เบรก
- กด **R** = เติมน้ำมัน 50 หน่วย

---

### **วิธีที่ 2: เรียก ServerRpc โดยตรงจาก Script**

#### **ตัวอย่าง Script:**

```csharp
using UnityEngine;
using Unity.Netcode;

public class MyTrainController : MonoBehaviour
{
    private TrainNetworkController train;

    private void Start()
    {
        // หา Train ใน Scene
        train = FindFirstObjectByType<TrainNetworkController>();
    }

    private void Update()
    {
        if (train == null) return;

        // เร่งรถไฟ
        if (Input.GetKey(KeyCode.W))
        {
            train.SetThrottleServerRpc(1f); // throttle = 1 (เต็มที่)
        }

        // หยุด
        if (Input.GetKey(KeyCode.S))
        {
            train.SetBrakeServerRpc(true);
        }

        // เติมน้ำมัน
        if (Input.GetKeyDown(KeyCode.R))
        {
            train.RefuelServerRpc(50f);
        }
    }
}
```

---

### **วิธีที่ 3: ใช้ UI Buttons**

#### **สร้าง UI Buttons:**

1. **สร้าง Canvas** (ถ้ายังไม่มี):
   - คลิกขวา → `UI → Canvas`

2. **สร้าง Buttons**:
   - คลิกขวา → `UI → Button - TextMeshPro` → ตั้งชื่อ `ThrottleButton`
   - คลิกขวา → `UI → Button - TextMeshPro` → ตั้งชื่อ `BrakeButton`
   - คลิกขวา → `UI → Button - TextMeshPro` → ตั้งชื่อ `RefuelButton`

3. **สร้าง Script สำหรับ UI:**

```csharp
using UnityEngine;
using Unity.Netcode;

public class TrainButtonController : MonoBehaviour
{
    [SerializeField] private TrainNetworkController train;

    public void OnThrottlePressed()
    {
        if (train != null)
            train.SetThrottleServerRpc(1f);
    }

    public void OnThrottleReleased()
    {
        if (train != null)
            train.SetThrottleServerRpc(0f);
    }

    public void OnBrakePressed()
    {
        if (train != null)
            train.SetBrakeServerRpc(true);
    }

    public void OnBrakeReleased()
    {
        if (train != null)
            train.SetBrakeServerRpc(false);
    }

    public void OnRefuel()
    {
        if (train != null)
            train.RefuelServerRpc(50f);
    }
}
```

4. **Setup Buttons**:
   - เพิ่ม Component `TrainButtonController` บน GameObject ใดก็ได้
   - Assign Train Controller
   - ใน Button Inspector:
     - `ThrottleButton` → `OnClick()` → `TrainButtonController.OnThrottlePressed()`
     - `ThrottleButton` → `OnPointerUp()` → `TrainButtonController.OnThrottleReleased()` (ถ้าต้องการ)
     - `BrakeButton` → `OnClick()` → `TrainButtonController.OnBrakePressed()`
     - `RefuelButton` → `OnClick()` → `TrainButtonController.OnRefuel()`

---

### **API Reference:**

```csharp
// Movement
train.SetThrottleServerRpc(float throttle); // throttle: -1 ถึง 1
train.SetBrakeServerRpc(bool isBraking);

// Fuel
train.RefuelServerRpc(float amount);

// Damage
train.ApplyDamageServerRpc(float amount);

// อ่านค่า
float speed = train.CurrentSpeed.Value;
float fuel = train.Fuel.Value;
float health = train.Health.Value;
```

---

## 2. Setup Module System

### **ขั้นตอนที่ 1: สร้าง TrainModuleDefinition Assets**

1. **สร้าง Module Assets**:
   - คลิกขวาใน Project window → `Create → Train → Module`
   - ตั้งชื่อเป็น `Module_ArmorUpgrade1`
   - Configure:
     - `Id` = `"armor_upgrade_1"` (สำคัญมาก! ต้อง unique)
     - `Type` = `Armor`
     - `Armor Multiplier` = `1.5` (เพิ่มเกราะ 50%)
     - `Fuel Efficiency Multiplier` = `1.0`
     - `Extra Storage Capacity` = `0`
     - `Extra Max Speed Multiplier` = `1.0`

2. **สร้าง Module อื่น ๆ**:
   - `Module_FuelEfficiency1`:
     - `Id` = `"fuel_efficiency_1"`
     - `Type` = `FuelEfficiency`
     - `Fuel Efficiency Multiplier` = `0.7` (ใช้เชื้อเพลิงน้อยลง 30%)
   
   - `Module_SpeedBoost1`:
     - `Id` = `"speed_boost_1"`
     - `Type` = `Utility`
     - `Extra Max Speed Multiplier` = `1.3` (เร็วขึ้น 30%)

3. **เก็บ Module Assets ในโฟลเดอร์**:
   ```
   Assets/FileGame/Core/Data/TrainModules/
   ├── Module_ArmorUpgrade1.asset
   ├── Module_FuelEfficiency1.asset
   └── Module_SpeedBoost1.asset
   ```

---

### **ขั้นตอนที่ 2: Setup TrainModuleManager**

1. **บน Train GameObject**:
   - เปิด `TrainModuleManager` component
   - `Train` = ลาก `TrainNetworkController` จาก Hierarchy มาใส่ (หรือจะ auto)
   - `Module Database`:
     - กด `+` เพื่อเพิ่ม item
     - ลาก Module Assets ทั้งหมดมาใส่ (เช่น `Module_ArmorUpgrade1`, `Module_FuelEfficiency1`)

---

### **ขั้นตอนที่ 3: ติดตั้ง Module จาก Script**

#### **ตัวอย่าง Script:**

```csharp
using UnityEngine;
using Unity.Netcode;

public class TrainModuleInstaller : MonoBehaviour
{
    [SerializeField] private TrainModuleManager moduleManager;
    [SerializeField] private string moduleIdToInstall = "armor_upgrade_1";

    public void InstallModule()
    {
        if (moduleManager != null)
        {
            moduleManager.InstallModuleServerRpc(moduleIdToInstall);
        }
    }
}
```

#### **หรือเรียกโดยตรง:**

```csharp
TrainModuleManager moduleManager = FindFirstObjectByType<TrainModuleManager>();
moduleManager.InstallModuleServerRpc("armor_upgrade_1");
```

---

### **ขั้นตอนที่ 4: สร้าง UI สำหรับ Module (Optional)**

1. **สร้าง Module UI Script:**

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class TrainModuleUI : MonoBehaviour
{
    [SerializeField] private TrainModuleManager moduleManager;
    [SerializeField] private Transform moduleButtonParent;
    [SerializeField] private GameObject moduleButtonPrefab;

    private void Start()
    {
        if (moduleManager == null)
            moduleManager = FindFirstObjectByType<TrainModuleManager>();

        CreateModuleButtons();
    }

    private void CreateModuleButtons()
    {
        if (moduleManager == null || moduleButtonPrefab == null) return;

        // สร้าง Button สำหรับแต่ละ Module ใน Database
        foreach (var module in moduleManager.ModuleDatabase) // ถ้ามี public property
        {
            GameObject button = Instantiate(moduleButtonPrefab, moduleButtonParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = module.name;
            button.GetComponent<Button>().onClick.AddListener(() => 
            {
                moduleManager.InstallModuleServerRpc(module.Id);
            });
        }
    }
}
```

---

## 3. Setup TrainUI

### **ขั้นตอนที่ 1: สร้าง Canvas**

1. **สร้าง Canvas**:
   - คลิกขวา → `UI → Canvas`
   - ตั้งชื่อเป็น `TrainUICanvas`
   - Configure:
     - `Render Mode` = `Screen Space - Overlay` (หรือตามต้องการ)
     - `Canvas Scaler` → `UI Scale Mode` = `Scale With Screen Size`

---

### **ขั้นตอนที่ 2: สร้าง UI Elements**

#### **2.1 สร้าง Panel สำหรับ Train UI:**

1. คลิกขวาบน Canvas → `UI → Panel`
2. ตั้งชื่อเป็น `TrainUIPanel`
3. Configure:
   - `Anchor` = Top Left (หรือตามต้องการ)
   - `Position` = (10, -10, 0)
   - `Size` = (300, 200)
   - `Color` = สีดำโปร่งใส (Alpha ≈ 200)

#### **2.2 สร้าง Speed Text:**

1. คลิกขวาบน `TrainUIPanel` → `UI → Text - TextMeshPro`
2. ตั้งชื่อเป็น `SpeedText`
3. Configure:
   - `Text` = `Speed: 0.0 m/s`
   - `Font Size` = 24
   - `Alignment` = Left
   - `Position` = (10, -30, 0)

#### **2.3 สร้าง Fuel Text:**

1. คลิกขวาบน `TrainUIPanel` → `UI → Text - TextMeshPro`
2. ตั้งชื่อเป็น `FuelText`
3. Configure:
   - `Text` = `Fuel: 100 / 100`
   - `Font Size` = 24
   - `Position` = (10, -60, 0)

#### **2.4 สร้าง Fuel Slider:**

1. คลิกขวาบน `TrainUIPanel` → `UI → Slider`
2. ตั้งชื่อเป็น `FuelSlider`
3. Configure:
   - `Min Value` = 0
   - `Max Value` = 100
   - `Value` = 100
   - `Position` = (10, -90, 0)
   - `Width` = 280
   - `Fill Area → Fill` → `Color` = สีเขียว (หรือสีที่ต้องการ)

#### **2.5 สร้าง Health Text:**

1. คลิกขวาบน `TrainUIPanel` → `UI → Text - TextMeshPro`
2. ตั้งชื่อเป็น `HealthText`
3. Configure:
   - `Text` = `Health: 100 / 100`
   - `Font Size` = 24
   - `Position` = (10, -120, 0)

#### **2.6 สร้าง Health Slider:**

1. คลิกขวาบน `TrainUIPanel` → `UI → Slider`
2. ตั้งชื่อเป็น `HealthSlider`
3. Configure:
   - `Min Value` = 0
   - `Max Value` = 100
   - `Value` = 100
   - `Position` = (10, -150, 0)
   - `Width` = 280
   - `Fill Area → Fill` → `Color` = สีแดง (หรือสีที่ต้องการ)

---

### **ขั้นตอนที่ 3: Setup TrainUI Component**

1. **สร้าง GameObject สำหรับ Controller**:
   - คลิกขวาบน Canvas → `Create Empty`
   - ตั้งชื่อเป็น `TrainUIController`

2. **เพิ่ม TrainUI Component**:
   - เพิ่ม Component `TrainUI` บน `TrainUIController`

3. **Assign References**:
   - `Train Controller` = ลาก Train GameObject จาก Scene มาใส่ (หรือจะหา auto)
   - `Speed Text` = ลาก `SpeedText` มาใส่
   - `Fuel Text` = ลาก `FuelText` มาใส่
   - `Health Text` = ลาก `HealthText` มาใส่
   - `Fuel Slider` = ลาก `FuelSlider` มาใส่
   - `Health Slider` = ลาก `HealthSlider` มาใส่
   - `Update Interval` = 0.1 (อัปเดตทุก 0.1 วินาที)

---

### **ขั้นตอนที่ 4: Test**

1. **กด Play**
2. **ตรวจสอบว่า UI แสดงค่า Speed, Fuel, Health**
3. **ทดสอบว่า UI อัปเดตตามค่า Train จริง**

---

## 🎨 Tips สำหรับ UI Design

### **Layout แนะนำ:**

```
TrainUIPanel
├── SpeedText (Top)
├── FuelText
├── FuelSlider (Visual bar)
├── HealthText
└── HealthSlider (Visual bar)
```

### **สีที่แนะนำ:**
- **Fuel Slider**: เขียว (ปกติ), เหลือง (< 30%), แดง (< 10%)
- **Health Slider**: เขียว (ปกติ), เหลือง (< 50%), แดง (< 25%)

### **เพิ่ม Visual Effects (Optional):**
- เพิ่ม `Image` สำหรับ Background
- เพิ่ม `Outline` หรือ `Shadow` ให้ Text
- ใช้ `Animation` สำหรับ Slider เมื่อค่าเปลี่ยน

---

## ✅ Checklist

### **Train Control:**
- [ ] Setup TrainInputHandler หรือ Script อื่น
- [ ] ทดสอบ Movement (เร่ง/เบรก)
- [ ] ทดสอบ Refuel

### **Module System:**
- [ ] สร้าง TrainModuleDefinition assets
- [ ] Setup TrainModuleManager (Assign Database)
- [ ] ทดสอบติดตั้ง Module
- [ ] ตรวจสอบว่า Stats เปลี่ยนตาม Module

### **TrainUI:**
- [ ] สร้าง Canvas และ UI Elements
- [ ] Setup TrainUI Component
- [ ] ทดสอบว่า UI แสดงค่าถูกต้อง
- [ ] ทดสอบว่า UI อัปเดตตามค่า Train

---

## 🐛 Troubleshooting

### **Train ไม่เคลื่อนที่:**
- ✅ ตรวจสอบว่า `TrainInputHandler` มี `Train Controller` assigned
- ✅ ตรวจสอบว่า Train ถูก spawn แล้ว
- ✅ ตรวจสอบว่า Fuel > 0 และ Health > 0

### **Module ไม่ทำงาน:**
- ✅ ตรวจสอบว่า Module Id ถูกต้อง (case-sensitive)
- ✅ ตรวจสอบว่า Module อยู่ใน Database ของ TrainModuleManager
- ✅ ตรวจสอบว่า Module Stats ถูก configure

### **UI ไม่แสดงค่า:**
- ✅ ตรวจสอบว่า `TrainUI` มี `Train Controller` assigned
- ✅ ตรวจสอบว่า UI Elements ทั้งหมดถูก assign
- ✅ ตรวจสอบว่า Train ถูก spawn แล้ว

---

**เสร็จแล้ว!** 🎉 ตอนนี้คุณสามารถควบคุม Train, Setup Module และแสดง UI ได้แล้ว!
