# 🚂 Train Platform Setup Guide

## 📋 ระบบให้ Player ยืนบน Train และเคลื่อนที่ตาม Train

ระบบนี้จะทำให้ Player สามารถ:
- ✅ ยืนอยู่บน Train ได้
- ✅ เคลื่อนที่ตาม Train อัตโนมัติ
- ✅ เดินได้ปกติ (ไม่ไหล/ลื่น)
- ✅ ลงจาก Train ได้เมื่อกระโดดหรือเดินออก

---

## 🔧 Setup

### **ขั้นตอนที่ 1: เพิ่ม TrainPlatform บน Train**

1. **เปิด Train GameObject** (หรือ Train Prefab)
2. **เพิ่ม Component `TrainPlatform`**
3. **Configure Settings**:
   - `Platform Check Radius` = 5 (รัศมีที่ตรวจหา Player)
   - `Player Layer Mask` = เลือก Layer ของ Player (เช่น "Player")
   - `Platform Height` = 2 (ความสูงจาก Train ที่ Player ยืนได้)

---

### **ขั้นตอนที่ 2: ตรวจสอบ Player Layer**

1. **เปิด Player GameObject** (หรือ Player Prefab)
2. **ตรวจสอบว่า Player อยู่ใน Layer ที่ถูกต้อง**:
   - ไปที่ `Edit → Project Settings → Tags and Layers`
   - สร้าง Layer ชื่อ "Player" (ถ้ายังไม่มี)
   - ตั้งค่า Player GameObject ให้อยู่ใน Layer "Player"

---

### **ขั้นตอนที่ 3: Test**

1. **กด Play**
2. **Spawn Train**
3. **ให้ Player เดินไปยืนบน Train**
4. **ทดสอบว่า**:
   - Player ยืนบน Train ได้
   - Player เคลื่อนที่ตาม Train อัตโนมัติ
   - Player เดินได้ปกติ (ไม่ไหล)
   - Player ลงจาก Train ได้เมื่อกระโดดหรือเดินออก

---

## 🎮 วิธีทำงาน

### **TrainPlatform (บน Train)**:
- ตรวจจับ Player ที่อยู่ใกล้ Train (ใช้ OverlapSphere)
- ตรวจสอบว่า Player อยู่บน Train หรือไม่ (ใช้ Raycast)
- คำนวณ Train velocity
- เพิ่ม `TrainRider` component ให้ Player อัตโนมัติ

### **TrainRider (บน Player)**:
- ถูกเพิ่มอัตโนมัติเมื่อ Player ยืนบน Train
- ใช้ `FinalMoveCalculationOverride` ของ `CoreMovement` เพื่อเพิ่ม Train velocity
- ลบตัวเองเมื่อ Player ลงจาก Train

---

## ⚙️ Settings อธิบาย

### **Platform Check Radius**:
- รัศมีที่ระบบจะตรวจหา Player
- ค่ามาก = หา Player ได้ไกลขึ้น แต่ใช้ performance มากขึ้น
- แนะนำ: 5-10

### **Player Layer Mask**:
- Layer ที่เป็น Player
- ตั้งค่าให้ตรงกับ Layer ของ Player GameObject
- แนะนำ: สร้าง Layer "Player" แยก

### **Platform Height**:
- ความสูงจาก Train ที่ Player ยืนได้
- ค่านี้ใช้สำหรับ Raycast ตรวจสอบว่า Player อยู่บน Train หรือไม่
- แนะนำ: 2-3 (ขึ้นอยู่กับขนาด Train)

---

## 🐛 Troubleshooting

### **Player ไม่เคลื่อนที่ตาม Train**

**ตรวจสอบ:**
- ✅ TrainPlatform component ถูกเพิ่มบน Train แล้วหรือยัง?
- ✅ Player Layer Mask ถูกตั้งค่าถูกต้องหรือไม่?
- ✅ Train กำลังเคลื่อนที่หรือไม่? (Speed > 0)
- ✅ Player อยู่บน Train จริง ๆ หรือไม่? (ดู Gizmo ใน Scene view)

**วิธีแก้:**
1. เปิด Scene view → เลือก Train → ดู Gizmo (วงกลมสีเขียว)
2. ตรวจสอบว่า Player อยู่ใน Gizmo หรือไม่
3. ตรวจสอบ Console → ดู Error/Warning

---

### **Player ไหล/ลื่นบน Train**

**สาเหตุ:**
- Train velocity ถูกเพิ่มเข้าไปใน Y axis ด้วย

**วิธีแก้:**
- ✅ ตรวจสอบว่า `TrainRider.cs` มี `trainMove.y = 0f;` หรือไม่
- ✅ ตรวจสอบว่า Player มี CharacterController และใช้ CoreMovement

---

### **Player ไม่สามารถลงจาก Train ได้**

**ตรวจสอบ:**
- ✅ Player กระโดดหรือเดินออกจาก Train หรือไม่?
- ✅ TrainPlatform ตรวจจับว่า Player ไม่อยู่บน Train แล้วหรือไม่?

**วิธีแก้:**
- เพิ่ม `Platform Check Radius` ให้ใหญ่ขึ้น
- หรือปรับ `Platform Height` ให้เหมาะสม

---

## 📝 Notes

- **Network**: ระบบนี้ทำงานฝั่ง Server เท่านั้น (TrainPlatform ใช้ `IsServer`)
- **Performance**: ใช้ OverlapSphere ทุก FixedUpdate → ถ้ามี Player เยอะอาจจะช้า
- **CharacterController**: ระบบนี้ใช้กับ CharacterController เท่านั้น (ไม่รองรับ Rigidbody)

---

## ✅ Checklist

- [ ] เพิ่ม TrainPlatform component บน Train
- [ ] ตั้งค่า Platform Check Radius, Player Layer Mask, Platform Height
- [ ] ตรวจสอบว่า Player อยู่ใน Layer ที่ถูกต้อง
- [ ] ทดสอบว่า Player ยืนบน Train ได้
- [ ] ทดสอบว่า Player เคลื่อนที่ตาม Train ได้
- [ ] ทดสอบว่า Player เดินได้ปกติ (ไม่ไหล)
- [ ] ทดสอบว่า Player ลงจาก Train ได้

---

**เสร็จแล้ว!** 🎉 ตอนนี้ Player สามารถยืนบน Train และเคลื่อนที่ตาม Train ได้แล้ว!
