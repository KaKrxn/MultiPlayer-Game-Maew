using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Script สำหรับทดสอบ Train System ใน Editor/Development
/// ใส่ไว้บน GameObject ใดก็ได้ใน Scene
/// </summary>
public class TrainDebugControls : MonoBehaviour
{
    [Header("Train Reference")]
    [SerializeField] private TrainNetworkController trainController;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugControls = true;

    private void Start()
    {
        // หา Train ใน Scene ถ้ายังไม่ได้ assign
        if (trainController == null)
        {
            trainController = FindFirstObjectByType<TrainNetworkController>();
        }
    }

    private void OnGUI()
    {
        if (!enableDebugControls || trainController == null) return;

        // ตรวจสอบว่าเป็น Server หรือไม่
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Box("Train Debug Controls");

        // แสดงสถานะ
        GUILayout.Label($"Is Server: {isServer}");
        GUILayout.Label($"Speed: {trainController.CurrentSpeed.Value:F2}");
        GUILayout.Label($"Fuel: {trainController.Fuel.Value:F2}");
        GUILayout.Label($"Health: {trainController.Health.Value:F2}");

        GUILayout.Space(10);

        // Controls (ทำงานได้ทั้งฝั่ง Server และ Client เพราะใช้ ServerRpc)
        if (GUILayout.Button("Throttle Forward"))
        {
            trainController.SetThrottleServerRpc(1f);
        }

        if (GUILayout.Button("Throttle Stop"))
        {
            trainController.SetThrottleServerRpc(0f);
        }

        if (GUILayout.Button("Brake"))
        {
            trainController.SetBrakeServerRpc(true);
        }

        if (GUILayout.Button("Release Brake"))
        {
            trainController.SetBrakeServerRpc(false);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Refuel (+50)"))
        {
            trainController.RefuelServerRpc(50f);
        }

        if (GUILayout.Button("Damage (-10)"))
        {
            trainController.ApplyDamageServerRpc(10f);
        }

        if (GUILayout.Button("Full Repair"))
        {
            trainController.Health.Value = trainController.MaxHealth;
        }

        GUILayout.EndArea();
    }
}
