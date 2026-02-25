using UnityEngine;

public enum TrainModuleType
{
    Armor,
    FuelEfficiency,
    Storage,
    Turret,
    Utility
}

[CreateAssetMenu(menuName = "Train/Module")]
public class TrainModuleDefinition : ScriptableObject
{
    public string Id; // ใช้ sync ข้าม network เป็น string/int
    public TrainModuleType Type;

    [Header("Stats")]
    public float armorMultiplier = 1f;
    public float fuelEfficiencyMultiplier = 1f;
    public float extraStorageCapacity = 0f;
    public float extraMaxSpeedMultiplier = 1f;
}