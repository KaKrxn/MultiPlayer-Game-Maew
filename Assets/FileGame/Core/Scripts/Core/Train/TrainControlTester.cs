using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// Script สำหรับทดสอบการควบคุม Train แบบง่าย ๆ
/// ใส่ไว้บน GameObject ใดก็ได้ใน Scene
/// รองรับ New Input System (Input System Package)
/// </summary>
public class TrainControlTester : MonoBehaviour
{
    [Header("Train Reference")]
    [SerializeField] private TrainNetworkController train;

    [Header("Test Settings")]
    [SerializeField] private bool autoFindTrain = true;
    [SerializeField] private Key testThrottleKey = Key.T;
    [SerializeField] private Key testBrakeKey = Key.B;
    [SerializeField] private Key testRefuelKey = Key.F;

    private void Start()
    {
        if (train == null && autoFindTrain)
        {
            FindTrain();
        }
    }

    private void Update()
    {
        // หา Train ถ้ายังหาไม่เจอ
        if (train == null && autoFindTrain)
        {
            FindTrain();
            return;
        }

        if (train == null) return;

        // ตรวจสอบว่า NetworkManager พร้อมหรือยัง
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Test Controls
        if (keyboard[testThrottleKey].wasPressedThisFrame)
        {
            Debug.Log("[TrainControlTester] Throttle Pressed");
            train.SetThrottleServerRpc(1f);
        }

        if (keyboard[testThrottleKey].wasReleasedThisFrame)
        {
            Debug.Log("[TrainControlTester] Throttle Released");
            train.SetThrottleServerRpc(0f);
        }

        if (keyboard[testBrakeKey].wasPressedThisFrame)
        {
            Debug.Log("[TrainControlTester] Brake Pressed");
            train.SetBrakeServerRpc(true);
        }

        if (keyboard[testBrakeKey].wasReleasedThisFrame)
        {
            Debug.Log("[TrainControlTester] Brake Released");
            train.SetBrakeServerRpc(false);
        }

        if (keyboard[testRefuelKey].wasPressedThisFrame)
        {
            Debug.Log("[TrainControlTester] Refuel Pressed");
            train.RefuelServerRpc(50f);
        }
    }

    private void FindTrain()
    {
        train = FindFirstObjectByType<TrainNetworkController>();
        
        if (train != null)
        {
            Debug.Log($"[TrainControlTester] Found Train: {train.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[TrainControlTester] Train not found. Make sure Train is spawned.");
        }
    }

    private void OnGUI()
    {
        if (train == null)
        {
            GUILayout.Box("Train not found!");
            return;
        }

        GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 140));
        GUILayout.Box("Train Control Tester");
        GUILayout.Label($"Train: {train.gameObject.name}");
        GUILayout.Label($"Speed: {train.CurrentSpeed.Value:F2}");
        GUILayout.Label($"Fuel: {train.Fuel.Value:F2}");
        GUILayout.Label($"Health: {train.Health.Value:F2}");
        GUILayout.Label($"Press T = Throttle, B = Brake, F = Refuel");
        GUILayout.EndArea();
    }
}
