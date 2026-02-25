using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// Script สำหรับรับ Input จากผู้เล่นเพื่อควบคุมรถไฟ
/// ใส่ไว้บน Player GameObject หรือ UI Controller
/// รองรับ New Input System (Input System Package)
/// </summary>
public class TrainInputHandler : MonoBehaviour
{
    [Header("Train Reference")]
    [SerializeField] private TrainNetworkController trainController;

    [Header("Input Settings (New Input System)")]
    [SerializeField] private Key throttleKey = Key.W;
    [SerializeField] private Key brakeKey = Key.S;
    [SerializeField] private Key refuelKey = Key.R;

    [Header("Auto Find Settings")]
    [SerializeField] private bool autoFindTrain = true;
    [SerializeField] private float findTrainInterval = 1f; // หา Train ทุก 1 วินาทีถ้ายังหาไม่เจอ

    private bool isThrottlePressed;
    private bool isBrakePressed;
    private float lastFindTrainTime;

    private void Start()
    {
        // หา Train ทันทีเมื่อ Start
        if (trainController == null && autoFindTrain)
        {
            FindTrain();
        }
    }

    private void Update()
    {
        // หา Train ถ้ายังหาไม่เจอ (ทุก findTrainInterval วินาที)
        if (trainController == null && autoFindTrain)
        {
            if (Time.time - lastFindTrainTime >= findTrainInterval)
            {
                FindTrain();
                lastFindTrainTime = Time.time;
            }
            return; // ถ้ายังหาไม่เจอ → ไม่ต้องทำอะไร
        }

        if (trainController == null) return;

        // ตรวจสอบว่า Train ถูก spawn แล้วหรือยัง
        NetworkObject trainNetworkObject = trainController.GetComponent<NetworkObject>();
        if (trainNetworkObject == null || !trainNetworkObject.IsSpawned)
        {
            return; // รอให้ Train spawn ก่อน
        }

        // ตรวจสอบว่า NetworkManager พร้อมหรือยัง
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            return; // รอให้ NetworkManager พร้อมก่อน
        }

        // ใช้ New Input System
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // รับ Input
        isThrottlePressed = keyboard[throttleKey].isPressed;
        isBrakePressed = keyboard[brakeKey].isPressed;

        // ส่งไปยัง Server
        if (isThrottlePressed)
        {
            trainController.SetThrottleServerRpc(1f);
        }
        else if (isBrakePressed)
        {
            trainController.SetBrakeServerRpc(true);
        }
        else
        {
            trainController.SetThrottleServerRpc(0f);
            trainController.SetBrakeServerRpc(false);
        }

        // Refuel (ตัวอย่าง - อาจจะต้องมี UI หรือ interaction system)
        if (keyboard[refuelKey].wasPressedThisFrame)
        {
            trainController.RefuelServerRpc(50f); // เติม 50 หน่วย
        }
    }

    /// <summary>
    /// หา Train ใน Scene
    /// </summary>
    private void FindTrain()
    {
        trainController = FindFirstObjectByType<TrainNetworkController>();
        
        if (trainController != null)
        {
            Debug.Log($"[TrainInputHandler] Found Train: {trainController.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[TrainInputHandler] Train not found in scene. Make sure Train is spawned.");
        }
    }

    /// <summary>
    /// Set Train Controller Reference (เรียกจาก script อื่นหรือ Inspector)
    /// </summary>
    public void SetTrainController(TrainNetworkController controller)
    {
        trainController = controller;
    }
}
