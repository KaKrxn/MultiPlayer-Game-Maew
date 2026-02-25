using UnityEngine;
using Unity.Netcode;

public class TrainNetworkController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseAcceleration = 3f;
    [SerializeField] private float baseDeceleration = 4f;
    [SerializeField] private float baseMaxSpeed = 20f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float obstacleCheckDistance = 5f;

    [Header("Fuel")]
    [SerializeField] private float baseFuelConsumptionPerSecond = 1f;
    [SerializeField] private float baseMaxFuel = 100f;

    [Header("Health")]
    [SerializeField] private float baseMaxHealth = 100f;

    public float MaxHealth => baseMaxHealth;
    public float MaxFuel => baseMaxFuel;

    // Server-authoritative state
    public NetworkVariable<float> CurrentSpeed = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Fuel = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Health = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Flags
    private bool _isBraking;
    private bool _isStoppedByObstacle;
    private bool _isShutDown; // ไม่มีน้ำมัน / HP = 0

    // Public method สำหรับระบบอื่น ๆ เรียกใช้
    public void SetShutdown(bool shutdown)
    {
        if (IsServer)
        {
            _isShutDown = shutdown;
        }
    }

    // จาก Module Manager (ค่า modifier ต่าง ๆ)
    public float MaxSpeedModifier { get; set; } = 1f;
    public float FuelEfficiencyModifier { get; set; } = 1f;
    public float ArmorModifier { get; set; } = 1f;

    private void Start()
    {
        if (IsServer)
        {
            Fuel.Value = baseMaxFuel;
            Health.Value = baseMaxHealth;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        float dt = Time.deltaTime;
        HandleMovement(dt);
        HandleFuel(dt);
    }

    #region Movement

    // เรียกจาก Client (เช่น ผู้ขับรถไฟ) เพื่อขอเปลี่ยน throttle [-1, 1]
    [ServerRpc(RequireOwnership = false)]
    public void SetThrottleServerRpc(float throttle)
    {
        // Validation ฝั่ง Server (เช่น เช็คสิทธิ์ว่าใครเป็นคนขับ)
        throttle = Mathf.Clamp(throttle, -1f, 1f);

        // เก็บ throttle ไว้ใช้ใน HandleMovement
        _pendingThrottle = throttle;
    }

    private float _pendingThrottle = 0f;

    private void HandleMovement(float dt)
    {
        float targetMaxSpeed = baseMaxSpeed * MaxSpeedModifier;

        if (_isShutDown || Fuel.Value <= 0f || Health.Value <= 0f)
        {
            // ไม่มีแรงขับ → เบรกจนหยุด
            _pendingThrottle = 0f;
        }

        CheckObstacle();

        float accel = baseAcceleration;
        float decel = baseDeceleration;

        float speed = CurrentSpeed.Value;

        if (_isStoppedByObstacle)
        {
            // บังคับเบรกเมื่อมีสิ่งกีดขวาง
            speed = Mathf.MoveTowards(speed, 0f, decel * dt);
        }
        else
        {
            if (_pendingThrottle > 0f)
            {
                speed += accel * _pendingThrottle * dt;
            }
            else if (_pendingThrottle < 0f || _isBraking)
            {
                speed = Mathf.MoveTowards(speed, 0f, decel * dt);
            }
        }

        speed = Mathf.Clamp(speed, 0f, targetMaxSpeed);
        CurrentSpeed.Value = speed;

        // เคลื่อนที่ตาม forward หรือ path system ของคุณ
        // ใช้ Rigidbody.MovePosition หรือ transform.position ขึ้นอยู่กับว่าคุณใช้ Rigidbody หรือไม่
        if (TryGetComponent<Rigidbody>(out var rb) && !rb.isKinematic)
        {
            // ถ้าใช้ Rigidbody → ใช้ MovePosition
            rb.MovePosition(transform.position + transform.forward * speed * dt);
        }
        else
        {
            // ถ้าไม่ใช้ Rigidbody → ใช้ transform.position
            transform.position += transform.forward * speed * dt;
        }
    }

    private void CheckObstacle()
    {
        _isStoppedByObstacle = Physics.Raycast(
            transform.position + Vector3.up,
            transform.forward,
            obstacleCheckDistance,
            obstacleMask);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetBrakeServerRpc(bool isBraking)
    {
        _isBraking = isBraking;
    }

    #endregion

    #region Fuel

    private void HandleFuel(float dt)
    {
        if (CurrentSpeed.Value <= 0f || _isShutDown) return;

        float consumption = baseFuelConsumptionPerSecond / Mathf.Max(0.01f, FuelEfficiencyModifier);
        Fuel.Value = Mathf.Max(0f, Fuel.Value - consumption * dt);

        if (Fuel.Value <= 0f)
        {
            _isShutDown = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RefuelServerRpc(float amount)
    {
        if (amount <= 0f || (_isShutDown && Health.Value <= 0f)) return;

        float maxFuelWithModules = baseMaxFuel; // + bonus จาก Module ถ้ามี
        Fuel.Value = Mathf.Clamp(Fuel.Value + amount, 0f, maxFuelWithModules);

        if (Fuel.Value > 0f)
            _isShutDown = false;
    }

    #endregion

    #region Health & Damage

    [ServerRpc(RequireOwnership = false)]
    public void ApplyDamageServerRpc(float amount)
    {
        if (amount <= 0f || Health.Value <= 0f) return;

        float effectiveDamage = amount / Mathf.Max(0.1f, ArmorModifier);
        Health.Value = Mathf.Max(0f, Health.Value - effectiveDamage);

        if (Health.Value <= 0f)
        {
            _isShutDown = true;
            OnTrainDestroyed();
        }
    }

    private void OnTrainDestroyed()
    {
        // Game Over Event → Broadcast ให้ทุก Client
        NotifyTrainDestroyedClientRpc();
    }

    [ClientRpc]
    private void NotifyTrainDestroyedClientRpc()
    {
        // TODO: แสดง UI Game Over / Trigger Scene Logic
    }

    #endregion
}