using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Script สำหรับติดตั้ง Module ให้กับ Train
/// ใช้กับ UI หรือเรียกจาก Script อื่น
/// </summary>
public class TrainModuleInstaller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrainModuleManager moduleManager;

    [Header("Module Settings")]
    [SerializeField] private string moduleIdToInstall = "";

    private void Start()
    {
        // หา ModuleManager ใน Scene ถ้ายังไม่ได้ assign
        if (moduleManager == null)
        {
            moduleManager = FindFirstObjectByType<TrainModuleManager>();
        }
    }

    /// <summary>
    /// ติดตั้ง Module โดยใช้ Module ID ที่กำหนดไว้
    /// </summary>
    public void InstallModule()
    {
        if (string.IsNullOrEmpty(moduleIdToInstall))
        {
            Debug.LogWarning("[TrainModuleInstaller] Module ID is not set!");
            return;
        }

        InstallModule(moduleIdToInstall);
    }

    /// <summary>
    /// ติดตั้ง Module โดยระบุ Module ID
    /// </summary>
    public void InstallModule(string moduleId)
    {
        if (moduleManager == null)
        {
            Debug.LogWarning("[TrainModuleInstaller] Module Manager is not assigned!");
            return;
        }

        if (string.IsNullOrEmpty(moduleId))
        {
            Debug.LogWarning("[TrainModuleInstaller] Module ID is empty!");
            return;
        }

        moduleManager.InstallModuleServerRpc(moduleId);
        Debug.Log($"[TrainModuleInstaller] Installing module: {moduleId}");
    }

    /// <summary>
    /// ติดตั้ง Module จาก TrainModuleDefinition asset
    /// </summary>
    public void InstallModule(TrainModuleDefinition moduleDefinition)
    {
        if (moduleDefinition == null)
        {
            Debug.LogWarning("[TrainModuleInstaller] Module Definition is null!");
            return;
        }

        InstallModule(moduleDefinition.Id);
    }

    /// <summary>
    /// Set Module Manager Reference
    /// </summary>
    public void SetModuleManager(TrainModuleManager manager)
    {
        moduleManager = manager;
    }

    /// <summary>
    /// Set Module ID to install
    /// </summary>
    public void SetModuleId(string moduleId)
    {
        moduleIdToInstall = moduleId;
    }
}
