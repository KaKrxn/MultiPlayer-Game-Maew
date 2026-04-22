using UnityEngine;
using Blocks.Gameplay.Core;

public class CurvedHorizonTile : MonoBehaviour
{
    [Header("Curved Horizon Settings")]
    public float curveStartDistance = 150f;
    public float curveDepth = -50f;

    [HideInInspector]
    public float flatDistance = 0f;

    private Transform trainTransform;
    private EndlessMapManager mapManager; // เพิ่มตัวแปรมารับ Manager

    private void Update()
    {
        // 1. หาระยะรถไฟทุกเฟรมจนกว่าจะเจอ
        if (trainTransform == null)
        {
            var train = FindFirstObjectByType<AutomatedNetworkTransform>();
            if (train != null) trainTransform = train.transform;
            else return; // ถ้ายังหาไม่เจอ ให้หยุดทำงานไปก่อน
        }

        // 2. --- [เพิ่มใหม่เพื่อแก้บั๊ก Client] ---
        // ให้ Client วิ่งไปอ่านค่า Safe Zone จาก Manager ในเครื่องตัวเอง!
        if (mapManager == null)
        {
            mapManager = FindFirstObjectByType<EndlessMapManager>();
            if (mapManager != null)
            {
                // คำนวณระยะแบนราบด้วยตัวเอง ไม่ต้องง้อ Server
                flatDistance = mapManager.safeFlatTilesCount * mapManager.standardTileLength;
            }
        }

        // 3. หาระยะห่างแกน Z
        float distanceZ = transform.position.z - trainTransform.position.z;

        // 4. ถ้าระยะห่างน้อยกว่า "ระยะพื้นราบ" ให้แบนราบ 100% (Y=0)
        if (distanceZ <= flatDistance)
        {
            SetYPosition(0f);
            return;
        }

        // 5. คำนวณความโค้ง
        float curveDistance = distanceZ - flatDistance;
        float distancePercentage = Mathf.Clamp01(curveDistance / curveStartDistance);
        float targetY = curveDepth * (distancePercentage * distancePercentage);

        SetYPosition(targetY);
    }

    private void SetYPosition(float yPos)
    {
        Vector3 pos = transform.position;
        pos.y = yPos;
        transform.position = pos;
    }
}