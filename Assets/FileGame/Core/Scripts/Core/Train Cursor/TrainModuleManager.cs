using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class TrainModuleManager : NetworkBehaviour
{
    [SerializeField] private TrainNetworkController train;
    [SerializeField] private List<TrainModuleDefinition> moduleDatabase;

    /// <summary>
    /// Public property สำหรับเข้าถึง Module Database (Read-only)
    /// </summary>
    public List<TrainModuleDefinition> ModuleDatabase => moduleDatabase;

    // เก็บ Id ของ module slot แต่ละช่อง (Server → Sync ทุก Client)
    private NetworkList<FixedString64Bytes> _installedModuleIds = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Initialize empty list if needed (NetworkList จะจัดการเอง)

        _installedModuleIds.OnListChanged += OnModulesChanged;
        
        // Recalculate stats เมื่อ spawn
        RecalculateStats();
    }

    public override void OnNetworkDespawn()
    {
        if (_installedModuleIds != null)
        {
            _installedModuleIds.OnListChanged -= OnModulesChanged;
        }
        base.OnNetworkDespawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void InstallModuleServerRpc(string moduleId)
    {
        // TODO: ตรวจของ, ราคา, เงื่อนไข ปลดล็อก ฯลฯ
        var id = new FixedString64Bytes(moduleId);

        if (_installedModuleIds.Count == 0)
            _installedModuleIds.Add(id);
        else
            _installedModuleIds[0] = id; // ตัวอย่าง: มี 1 slot

        RecalculateStats();
    }

    private void OnModulesChanged(NetworkListEvent<FixedString64Bytes> change)
    {
        RecalculateStats();
    }

    private void RecalculateStats()
    {
        if (train == null || moduleDatabase == null) return;

        // reset เป็นค่า base
        train.MaxSpeedModifier = 1f;
        train.FuelEfficiencyModifier = 1f;
        train.ArmorModifier = 1f;

        foreach (var id in _installedModuleIds)
        {
            if (id.IsEmpty) continue;
            var module = moduleDatabase.Find(m => m.Id == id.ToString());
            if (module == null) continue;

            train.MaxSpeedModifier *= module.extraMaxSpeedMultiplier;
            train.FuelEfficiencyModifier *= module.fuelEfficiencyMultiplier;
            train.ArmorModifier *= module.armorMultiplier;
            // Storage/Defense system ไปจัดการที่ระบบอื่น เช่น Inventory / Turret
        }
    }
}