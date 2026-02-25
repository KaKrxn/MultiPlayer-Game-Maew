# 🚂 Train System Setup Guide

## 📋 ขั้นตอนการ Setup Train System ใน Unity Scene

> **หมายเหตุ**: เอกสารนี้จะสอนการ setup Train System แบบทีละขั้นตอน พร้อมตัวอย่างการใช้งาน

---

## 🎯 สารบัญ

1. [สร้าง Train Prefab](#ขั้นตอนที่-1-สร้าง-train-prefab)
2. [Setup ใน Scene](#ขั้นตอนที่-2-setup-ใน-scene)
3. [Setup Player Input](#ขั้นตอนที่-3-setup-player-input-optional)
4. [Register ใน NetworkManager](#ขั้นตอนที่-4-register-train-prefab-ใน-networkmanager)
5. [สร้าง UI](#ขั้นตอนที่-5-สร้าง-ui-optional)
6. [Test](#ขั้นตอนที่-6-test)

### **ขั้นตอนที่ 1: สร้าง Train Prefab**

1. **สร้าง GameObject ใหม่** ใน Hierarchy:
   - คลิกขวา → `Create Empty`
   - ตั้งชื่อเป็น `Train`

2. **เพิ่ม Components ต่อไปนี้**:
   - `NetworkObject` (จาก Unity Netcode)
   - `NetworkTransform` (สำหรับ sync ตำแหน่ง/rotation)
   - `TrainNetworkController` (แกนกลางของระบบ)
   - `TrainModuleManager` (จัดการ Module/Upgrade)
   - `TrainRepairSystem` (ระบบซ่อม)

3. **Configure NetworkObject**:
   - ✅ `Dont Destroy With Owner` = false
   - ✅ `Spawn With Scene` = false (เราจะ spawn แบบ manual)

4. **Configure NetworkTransform**:
   - ✅ `Sync Position` = true
   - ✅ `Sync Rotation` = true
   - ✅ `Sync Scale` = false (ถ้าไม่ต้องการ)

5. **Configure TrainNetworkController**:
   - `Base Acceleration` = 3
   - `Base Deceleration` = 4
   - `Base Max Speed` = 20
   - `Obstacle Mask` = เลือก Layer ที่เป็นสิ่งกีดขวาง (เช่น "Obstacle")
   - `Obstacle Check Distance` = 5
   - `Base Fuel Consumption Per Second` = 1
   - `Base Max Fuel` = 100
   - `Base Max Health` = 100

6. **Configure TrainModuleManager**:
   - `Train` = ลาก TrainNetworkController จาก Hierarchy มาใส่
   - `Module Database` = สร้าง List แล้วเพิ่ม TrainModuleDefinition assets (ถ้ามี)

7. **Configure TrainRepairSystem**:
   - `Train` = ลาก TrainNetworkController จาก Hierarchy มาใส่
   - `Base Repair Per Second` = 5

8. **เพิ่ม Visual Model** (ถ้าต้องการ):
   - ลาก Model 3D ของรถไฟมาเป็น Child ของ Train GameObject
   - หรือสร้าง Cube/Capsule เป็น placeholder

9. **Save เป็น Prefab**:
   - ลาก `Train` จาก Hierarchy ไปที่ Project window
   - ตั้งชื่อเป็น `TrainPrefab`
   - ลบ Train GameObject ออกจาก Hierarchy (เก็บแค่ prefab)

---

### **ขั้นตอนที่ 2: Setup ใน Scene**

1. **สร้าง Train Spawner**:
   - คลิกขวา → `Create Empty`
   - ตั้งชื่อเป็น `TrainSpawner`
   - เพิ่ม Component `TrainSpawner`
   - Configure:
     - `Train Prefab` = ลาก `TrainPrefab` มาใส่
     - `Spawn Position` = ตำแหน่งที่ต้องการให้รถไฟ spawn (เช่น 0, 0, 0)
     - `Spawn Rotation` = rotation ที่ต้องการ
     - `Spawn On Start` = true (ถ้าต้องการให้ spawn อัตโนมัติเมื่อเริ่มเกม)

2. **เพิ่ม NetworkObject ให้ TrainSpawner** (ถ้ายังไม่มี):
   - เพิ่ม Component `NetworkObject`
   - ✅ `Spawn With Scene` = true (เพื่อให้ TrainSpawner อยู่ใน Scene)

---

### **ขั้นตอนที่ 3: Setup Player Input (Optional)**

1. **บน Player GameObject**:
   - เพิ่ม Component `TrainInputHandler`
   - Configure:
     - `Train Controller` = จะ set อัตโนมัติเมื่อ Player อยู่ใกล้รถไฟ (หรือ set manual)
     - `Throttle Key` = W
     - `Brake Key` = S
     - `Refuel Key` = R

2. **หรือสร้าง UI Controller แยก**:
   - สร้าง GameObject ใหม่ชื่อ `TrainUIController`
   - เพิ่ม `TrainInputHandler`
   - ใช้ UI Buttons แทน Keyboard Input

---

### **ขั้นตอนที่ 4: Register Train Prefab ใน NetworkManager**

1. **เปิด NetworkManager**:
   - หา `NetworkManager` GameObject ใน Scene (หรือ prefab)
   - หรือหา `[BB] NetworkManager` prefab

2. **เพิ่ม Train Prefab ลงใน Network Prefabs List**:
   - เปิด `NetworkManager` component
   - หา `Network Prefabs List` หรือ `Spawnable Prefabs`
   - กด `+` เพื่อเพิ่ม item
   - ลาก `TrainPrefab` มาใส่

---

### **ขั้นตอนที่ 5: สร้าง UI (Optional)**

1. **สร้าง Canvas** (ถ้ายังไม่มี):
   - คลิกขวา → `UI → Canvas`
   - ตั้งชื่อเป็น `TrainUI`

2. **สร้าง UI Elements**:
   - **Speed Text**: `UI → Text - TextMeshPro` → ตั้งชื่อ `SpeedText`
   - **Fuel Text**: `UI → Text - TextMeshPro` → ตั้งชื่อ `FuelText`
   - **Health Text**: `UI → Text - TextMeshPro` → ตั้งชื่อ `HealthText`
   - **Fuel Slider**: `UI → Slider` → ตั้งชื่อ `FuelSlider`
   - **Health Slider**: `UI → Slider` → ตั้งชื่อ `HealthSlider`

3. **เพิ่ม TrainUI Component**:
   - สร้าง GameObject ใหม่ชื่อ `TrainUIController` (เป็น child ของ Canvas หรือแยก)
   - เพิ่ม Component `TrainUI`
   - Configure:
     - `Train Controller` = ลาก Train จาก Scene มาใส่ (หรือจะหา auto)
     - `Speed Text` = ลาก SpeedText มาใส่
     - `Fuel Text` = ลาก FuelText มาใส่
     - `Health Text` = ลาก HealthText มาใส่
     - `Fuel Slider` = ลาก FuelSlider มาใส่
     - `Health Slider` = ลาก HealthSlider มาใส่

4. **เพิ่ม TrainDebugControls** (สำหรับ Development):
   - สร้าง GameObject ใหม่ชื่อ `TrainDebugControls`
   - เพิ่ม Component `TrainDebugControls`
   - Configure:
     - `Train Controller` = ลาก Train มาใส่
     - `Enable Debug Controls` = true

---

### **ขั้นตอนที่ 6: Test**

1. **เปิด Scene ที่มี TrainSpawner**
2. **Start Game**:
   - กด Play
   - ถ้าเป็น Host → Train จะ spawn อัตโนมัติ
   - ถ้าเป็น Client → จะเห็น Train ที่ Host spawn

3. **ทดสอบ Movement**:
   - ใช้ `TrainInputHandler` หรือเรียก `SetThrottleServerRpc` จาก script อื่น
   - ตรวจสอบว่า Train เคลื่อนที่ได้

4. **ทดสอบ Fuel**:
   - ดูว่า Fuel ลดลงเมื่อรถไฟวิ่ง
   - ทดสอบ `RefuelServerRpc`

5. **ทดสอบ Repair**:
   - เรียก `StartRepairServerRpc` จาก `TrainRepairSystem`
   - ตรวจสอบว่า HP เพิ่มขึ้น

---

## 🔧 Troubleshooting

### **Train ไม่ spawn**
- ✅ ตรวจสอบว่า `TrainSpawner` มี `NetworkObject` และ `Spawn With Scene = true`
- ✅ ตรวจสอบว่า `Train Prefab` มี `NetworkObject` component
- ✅ ตรวจสอบว่า Train Prefab ถูก register ใน NetworkManager

### **Train ไม่เคลื่อนที่**
- ✅ ตรวจสอบว่า `IsServer` = true (TrainNetworkController ทำงานฝั่ง Server เท่านั้น)
- ✅ ตรวจสอบว่า `SetThrottleServerRpc` ถูกเรียก
- ✅ ตรวจสอบว่า Fuel > 0 และ Health > 0

### **Network Sync ไม่ทำงาน**
- ✅ ตรวจสอบว่า Train มี `NetworkTransform` component
- ✅ ตรวจสอบว่า `NetworkObject` ถูก spawn แล้ว (`IsSpawned = true`)

---

---

## 📝 Notes

- **Authority**: ทุกอย่างทำงานฝั่ง Server เท่านั้น (Movement, Fuel, Health, Damage)
- **Client**: แค่ส่ง Input ผ่าน `ServerRpc` และรับข้อมูลผ่าน `NetworkVariable`
- **Train Prefab**: ควรเป็น Scene Object หรือ Spawnable Prefab ขึ้นอยู่กับ design ของคุณ

---

## 🎮 ตัวอย่างการใช้งานจาก Script

### **ควบคุมรถไฟจาก Script อื่น:**

```csharp
// หา Train Controller
TrainNetworkController train = FindFirstObjectByType<TrainNetworkController>();

// เร่งรถไฟ
train.SetThrottleServerRpc(1f); // throttle = 1 (เต็มที่)

// เบรก
train.SetBrakeServerRpc(true);

// เติมน้ำมัน
train.RefuelServerRpc(50f);

// โดน damage
train.ApplyDamageServerRpc(10f);

// อ่านค่าต่าง ๆ
float speed = train.CurrentSpeed.Value;
float fuel = train.Fuel.Value;
float health = train.Health.Value;
```

### **ซ่อมรถไฟ:**

```csharp
TrainRepairSystem repairSystem = FindFirstObjectByType<TrainRepairSystem>();

// เริ่มซ่อม (จากผู้เล่นคนที่ 1)
repairSystem.StartRepairServerRpc();

// หยุดซ่อม
repairSystem.StopRepairServerRpc();
```

### **ติดตั้ง Module:**

```csharp
TrainModuleManager moduleManager = FindFirstObjectByType<TrainModuleManager>();

// ติดตั้ง Module (ใช้ ID จาก TrainModuleDefinition)
moduleManager.InstallModuleServerRpc("armor_upgrade_1");
```

---

## 📦 Structure ที่แนะนำ

```
Scene Hierarchy:
├── NetworkManager (มีอยู่แล้ว)
├── TrainSpawner
│   └── TrainSpawner (Component)
├── Train (Spawned at runtime)
│   ├── NetworkObject
│   ├── NetworkTransform
│   ├── TrainNetworkController
│   ├── TrainModuleManager
│   └── TrainRepairSystem
├── Canvas
│   ├── TrainUIController
│   │   └── TrainUI (Component)
│   └── ... (UI Elements)
└── TrainDebugControls
    └── TrainDebugControls (Component)
```

---

## ✅ Checklist

- [ ] สร้าง Train Prefab พร้อม Components ทั้งหมด
- [ ] Configure NetworkObject และ NetworkTransform
- [ ] Setup TrainNetworkController (ค่า Movement, Fuel, Health)
- [ ] Setup TrainModuleManager และ TrainRepairSystem
- [ ] สร้าง TrainSpawner ใน Scene
- [ ] Register Train Prefab ใน NetworkManager
- [ ] สร้าง UI (Optional)
- [ ] ทดสอบ Spawn Train
- [ ] ทดสอบ Movement และ Fuel System
- [ ] ทดสอบ Repair System
- [ ] ทดสอบ Multiplayer Sync

---

## 🚀 Quick Start (สรุปสั้น ๆ)

1. **สร้าง Train Prefab** → เพิ่ม NetworkObject, NetworkTransform, TrainNetworkController, TrainModuleManager, TrainRepairSystem
2. **สร้าง TrainSpawner** → Assign Train Prefab, Set Spawn Position
3. **Register Prefab** → เพิ่ม Train Prefab ใน NetworkManager
4. **Test** → กด Play และใช้ Debug Controls หรือ TrainInputHandler

**เสร็จแล้ว!** 🎉
