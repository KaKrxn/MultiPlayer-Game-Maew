using UnityEngine;
using Unity.Netcode;

public class TrainSurfer : NetworkBehaviour
{
    private CharacterController cc;
    private Transform currentTrain;
    private Vector3 lastTrainPos;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    // ฟังก์ชันนี้รับคำสั่งตอนเหยียบโดนรถไฟ
    public void SetTrain(Transform train)
    {
        // 🚨 สำคัญมาก: ให้เฉพาะ "เจ้าของตัวละคร (เครื่องตัวเอง)" จัดการตัวเองเท่านั้น Server ไม่ต้องยุ่ง!
        if (!IsOwner) return;

        if (train != null)
        {
            currentTrain = train;
            lastTrainPos = train.position;
        }
        else
        {
            currentTrain = null;
        }
    }

    // ทำงานใน LateUpdate เพื่อรอให้รถไฟขยับเสร็จก่อน แล้วเราค่อยขยับตาม
    private void LateUpdate()
    {
        if (!IsOwner || currentTrain == null || cc == null || !cc.enabled) return;

        // คำนวณว่าเฟรมนี้รถไฟขยับไปกี่เมตร
        Vector3 delta = currentTrain.position - lastTrainPos;

        if (delta != Vector3.zero)
        {
            // บังคับดึงตัวละคร CharacterController ให้สไลด์ตามรถไฟไปเป๊ะๆ
            cc.Move(delta);
        }

        // จำตำแหน่งรถไฟปัจจุบันไว้ใช้คำนวณในเฟรมหน้า
        lastTrainPos = currentTrain.position;
    }
}