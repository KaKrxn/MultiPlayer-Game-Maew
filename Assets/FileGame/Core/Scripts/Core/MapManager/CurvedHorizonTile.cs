using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// Applies a curved horizon visual effect to tiles based on distance from the train.
/// Tiles within the flat safe zone remain at Y=0, beyond that they curve downward.
/// </summary>
public class CurvedHorizonTile : MonoBehaviour
{
    [Header("Curved Horizon Settings")]
    public float curveStartDistance = 150f;
    public float curveDepth = -50f;

    [HideInInspector]
    public float flatDistance = 0f;

    private Transform trainTransform;
    private EndlessMapManager mapManager;

    private void Update()
    {
        // 1. Find the train reference (cached after first find)
        if (trainTransform == null)
        {
            var train = FindFirstObjectByType<AutomatedNetworkTransform>();
            if (train != null) trainTransform = train.transform;
            else return; // Train not spawned yet — skip this frame
        }

        // 2. Client-side fix: read safe zone from the map manager
        if (mapManager == null)
        {
            mapManager = FindFirstObjectByType<EndlessMapManager>();
            if (mapManager != null)
            {
                // Calculate flat distance locally without depending on server
                flatDistance = mapManager.safeFlatTilesCount * mapManager.standardTileLength;
            }
        }

        // 3. Calculate Z-axis distance from train
        float distanceZ = transform.position.z - trainTransform.position.z;

        // 4. Within flat zone — keep at ground level
        if (distanceZ <= flatDistance)
        {
            SetYPosition(0f);
            return;
        }

        // 5. Calculate curve effect (quadratic falloff)
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