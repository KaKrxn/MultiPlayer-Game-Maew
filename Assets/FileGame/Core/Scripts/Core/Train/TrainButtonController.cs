using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Script สำหรับควบคุม Train ผ่าน UI Buttons
/// ใส่ไว้บน GameObject ใดก็ได้ใน Scene
/// </summary>
public class TrainButtonController : MonoBehaviour
{
    [Header("Train Reference")]
    [SerializeField] private TrainNetworkController train;

    [Header("Settings")]
    [SerializeField] private float refuelAmount = 50f;

    private void Start()
    {
        // หา Train ใน Scene ถ้ายังไม่ได้ assign
        if (train == null)
        {
            train = FindFirstObjectByType<TrainNetworkController>();
        }
    }

    #region Movement Controls

    /// <summary>
    /// เรียกเมื่อกดปุ่มเร่ง (Throttle)
    /// </summary>
    public void OnThrottlePressed()
    {
        if (train != null)
        {
            train.SetThrottleServerRpc(1f);
        }
    }

    /// <summary>
    /// เรียกเมื่อปล่อยปุ่มเร่ง
    /// </summary>
    public void OnThrottleReleased()
    {
        if (train != null)
        {
            train.SetThrottleServerRpc(0f);
        }
    }

    /// <summary>
    /// เรียกเมื่อกดปุ่มเบรก (Brake)
    /// </summary>
    public void OnBrakePressed()
    {
        if (train != null)
        {
            train.SetBrakeServerRpc(true);
        }
    }

    /// <summary>
    /// เรียกเมื่อปล่อยปุ่มเบรก
    /// </summary>
    public void OnBrakeReleased()
    {
        if (train != null)
        {
            train.SetBrakeServerRpc(false);
        }
    }

    #endregion

    #region Fuel Controls

    /// <summary>
    /// เติมน้ำมัน
    /// </summary>
    public void OnRefuel()
    {
        if (train != null)
        {
            train.RefuelServerRpc(refuelAmount);
        }
    }

    /// <summary>
    /// เติมน้ำมันแบบกำหนดจำนวน
    /// </summary>
    public void OnRefuel(float amount)
    {
        if (train != null)
        {
            train.RefuelServerRpc(amount);
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// Set Train Controller Reference
    /// </summary>
    public void SetTrainController(TrainNetworkController controller)
    {
        train = controller;
    }

    #endregion
}
