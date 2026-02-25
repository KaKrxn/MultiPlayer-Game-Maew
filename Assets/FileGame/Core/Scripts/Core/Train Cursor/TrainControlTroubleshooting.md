# 🔧 Train Control Troubleshooting Guide

## ❌ ปัญหาที่พบบ่อย

### **1. Train ไม่ขยับ / TrainInputHandler ไม่เชื่อมกับ Train**

#### **สาเหตุ:**
- Train ยังไม่ได้ถูก spawn
- TrainInputHandler หา Train ไม่เจอ
- NetworkManager ยังไม่พร้อม

#### **วิธีแก้:**

**วิธีที่ 1: ตรวจสอบว่า Train ถูก Spawn แล้วหรือยัง**
```
1. กด Play
2. ดูใน Hierarchy → หา Train GameObject
3. ตรวจสอบว่า Train มี NetworkObject component
4. ตรวจสอบว่า NetworkObject.IsSpawned = true
```

**วิธีที่ 2: ใช้ TrainControlTester (แนะนำ)**
```
1. สร้าง GameObject ใหม่ชื่อ "TrainControlTester"
2. เพิ่ม Component: TrainControlTester
3. กด Play
4. ดู GUI ด้านล่างซ้าย → จะบอกว่าเจอ Train หรือไม่
5. กด T = เร่ง, B = เบรก, F = เติมน้ำมัน
```

**วิธีที่ 3: Manual Assign ใน Inspector**
```
1. หา Train GameObject ใน Scene (หลัง spawn แล้ว)
2. เปิด TrainInputHandler component
3. ลาก Train GameObject มาใส่ในช่อง "Train Controller"
4. ปิด Auto Find Train = false (ถ้าต้องการ)
```

---

### **2. TrainInputHandler ไม่ทำงานเลย**

#### **ตรวจสอบ:**
- ✅ TrainInputHandler มีอยู่บน GameObject ใน Scene หรือไม่?
- ✅ GameObject ที่มี TrainInputHandler ยังอยู่ใน Scene หรือไม่?
- ✅ NetworkManager ถูก setup แล้วหรือยัง?

#### **วิธีแก้:**
```
1. ตรวจสอบ Console → ดูว่ามี Error หรือ Warning อะไรไหม
2. ใช้ TrainControlTester แทน (ง่ายกว่า)
3. หรือใช้ TrainDebugControls (GUI Controls)
```

---

### **3. Train ขยับได้แต่ช้ามาก / ไม่เร็ว**

#### **สาเหตุ:**
- Base Max Speed ต่ำเกินไป
- Fuel หมด
- Health = 0

#### **วิธีแก้:**
```
1. เปิด TrainNetworkController
2. ตรวจสอบ Base Max Speed (ควรเป็น 20 หรือมากกว่า)
3. ตรวจสอบ Fuel.Value (ควร > 0)
4. ตรวจสอบ Health.Value (ควร > 0)
```

---

### **4. Train ขยับได้แต่ Client คนอื่นไม่เห็น**

#### **สาเหตุ:**
- NetworkTransform ไม่ sync
- NetworkObject ไม่ถูก spawn

#### **วิธีแก้:**
```
1. ตรวจสอบว่า Train มี NetworkTransform component
2. ตรวจสอบว่า NetworkTransform.Sync Position = true
3. ตรวจสอบว่า NetworkObject.IsSpawned = true
```

---

## ✅ Checklist สำหรับ Debug

### **ก่อน Play:**
- [ ] Train Prefab มี NetworkObject component
- [ ] Train Prefab มี NetworkTransform component
- [ ] Train Prefab มี TrainNetworkController component
- [ ] Train Prefab ถูก register ใน NetworkManager
- [ ] TrainSpawner ถูก setup ใน Scene

### **หลัง Play (Host):**
- [ ] Train ถูก spawn แล้ว (ดูใน Hierarchy)
- [ ] NetworkObject.IsSpawned = true
- [ ] TrainNetworkController.Fuel.Value > 0
- [ ] TrainNetworkController.Health.Value > 0

### **หลัง Play (Client):**
- [ ] Train ปรากฏใน Scene (เห็นจาก Host)
- [ ] NetworkObject.IsSpawned = true
- [ ] TrainInputHandler หรือ TrainControlTester หา Train เจอ

---

## 🎮 วิธีทดสอบที่แนะนำ

### **วิธีที่ 1: ใช้ TrainControlTester (ง่ายที่สุด)**
```
1. สร้าง GameObject → เพิ่ม TrainControlTester
2. กด Play
3. ดู GUI ด้านล่างซ้าย → จะบอกสถานะ
4. กด T/B/F เพื่อทดสอบ
```

### **วิธีที่ 2: ใช้ TrainDebugControls**
```
1. สร้าง GameObject → เพิ่ม TrainDebugControls
2. กด Play
3. ดู GUI ด้านซ้ายบน → มีปุ่มให้กด
```

### **วิธีที่ 3: ใช้ Console Commands**
```
1. กด Play
2. เปิด Console (Ctrl+Shift+C)
3. ดู Debug.Log messages
```

---

## 📝 Debug Scripts ที่มีให้

1. **TrainControlTester** - ทดสอบง่าย ๆ พร้อม GUI
2. **TrainDebugControls** - GUI Controls สำหรับทดสอบ
3. **TrainInputHandler** - Input Handler ปกติ (แก้ไขแล้ว)

---

## 🐛 ถ้ายังแก้ไม่ได้

1. **ตรวจสอบ Console** → ดู Error/Warning
2. **ตรวจสอบ NetworkManager** → ดูว่า setup ถูกต้องหรือไม่
3. **ตรวจสอบ Train Spawn** → ดูว่า Train ถูก spawn แล้วหรือยัง
4. **ใช้ TrainControlTester** → จะบอกสถานะชัดเจน

---

**หมายเหตุ:** TrainInputHandler ตอนนี้หา Train อัตโนมัติแล้ว และไม่ต้องเป็น NetworkBehaviour อีกต่อไป!
