using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// UI singleton สำหรับแสดงกระเป๋าที่สอง (Vault / Trunk)
/// ทำหน้าที่แค่ดึงข้อมูล NetworkVault มาใส่ในช่องของตัวเอง (การเปิด/ปิดหน้าจอให้ InventoryManager จัดการ)
/// </summary>
public class VaultUI : MonoBehaviour
{
    public static VaultUI Instance { get; private set; }

    [Header("Vault Slots Setup (ฝั่งขวาของตู้)")]
    [Tooltip("ช่อง slot ทั้งหมดของ Vault (เรียงตามลำดับ index จากซ้ายไปขวา บนลงล่าง)")]
    [SerializeField] private List<InventorySlot> vaultSlots = new List<InventorySlot>();

    [Header("Input Settings")]
    [Tooltip("ปุ่มปิดตู้เซฟ (โดยปกติเป็น Esc)")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private NetworkVault _currentVault;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnbindCurrent();
    }

    private void Update()
    {
        // ถ้าตู้เซฟเปิดอยู่ และกดปุ่มปิด (Esc) ให้สั่งปิดตู้เซฟ
        if (InventoryManager.instance != null && InventoryManager.instance.isVaultOpen)
        {
            if (Input.GetKeyDown(closeKey))
            {
                InventoryManager.instance.CloseVault();
                Close();
            }
        }
    }

    /// <summary>
    /// เปิดการเชื่อมต่อ UI เข้ากับ NetworkVault
    /// </summary>
    public void Open(NetworkVault vault)
    {
        if (vault == null) return;
        if (_currentVault != null && _currentVault != vault) UnbindCurrent();

        _currentVault = vault;
        BindSlotsToVault(vault);
        SubscribeToVault(vault);
        RefreshAllSlots();

        // บังคับโชว์ตัวเอง (ถ้าใช้ CanvasGroup)
        SetGroupVisible(true); 

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OpenVault(null);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    // เพิ่มฟังก์ชันนี้เข้าไปใน VaultUI.cs
    private void SetGroupVisible(bool visible)
    {
        var group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }

    /// <summary>
    /// ปิดการเชื่อมต่อ UI
    /// </summary>
    public void Close()
    {
        UnbindCurrent();
    }

    private void BindSlotsToVault(NetworkVault vault)
    {
        for (int i = 0; i < vaultSlots.Count; i++)
        {
            if (vaultSlots[i] != null)
            {
                vaultSlots[i].Bind(vault, i);
            }
        }
    }

    private void SubscribeToVault(NetworkVault vault)
    {
        if (vault.VaultData != null)
        {
            vault.VaultData.OnListChanged += OnVaultListChanged;
        }
    }

    private void UnbindCurrent()
    {
        if (_currentVault != null && _currentVault.VaultData != null)
        {
            _currentVault.VaultData.OnListChanged -= OnVaultListChanged;
        }

        // ล้าง UI items ที่อยู่ในช่อง vault ฝั่งขวาออกให้หมด (กันภาพค้างตอนเปลี่ยนตู้)
        for (int i = 0; i < vaultSlots.Count; i++)
        {
            var slot = vaultSlots[i];
            if (slot == null) continue;

            for (int c = slot.transform.childCount - 1; c >= 0; c--)
            {
                Destroy(slot.transform.GetChild(c).gameObject);
            }
            slot.ClearItem();
        }

        _currentVault = null;
    }

    private void OnVaultListChanged(NetworkListEvent<NetworkInventorySlotData> evt)
    {
        if (_currentVault == null) return;
        ApplyVaultSlotUpdate(evt.Index, evt.Value);
    }

    private void RefreshAllSlots()
    {
        if (_currentVault == null || _currentVault.VaultData == null) return;

        for (int i = 0; i < _currentVault.VaultData.Count && i < vaultSlots.Count; i++)
        {
            ApplyVaultSlotUpdate(i, _currentVault.VaultData[i]);
        }
    }

    private void ApplyVaultSlotUpdate(int index, NetworkInventorySlotData data)
    {
        if (index < 0 || index >= vaultSlots.Count) return;
        var slot = vaultSlots[index];
        if (slot == null) return;

        // 1. ล้างของเดิมในช่องนี้ก่อนเสมอ
        for (int c = slot.transform.childCount - 1; c >= 0; c--)
        {
            var child = slot.transform.GetChild(c).gameObject;
            Destroy(child);
        }
        slot.ClearItem();

        // 2. ถ้าข้อมูลจาก Server บอกว่าช่องว่าง ให้จบการทำงานตรงนี้
        if (data.isEmpty) return;

        if (InventoryManager.instance == null || InventoryManager.instance.itemPrefab == null)
        {
            Debug.LogWarning("[VaultUI] InventoryManager.instance.itemPrefab is missing.");
            return;
        }

        // 3. ตรวจสอบข้อมูลไอเทม
        var itemData = ResolveItemDataByName(data.itemName.ToString());
        if (itemData == null)
        {
            Debug.LogWarning($"[VaultUI] Cannot resolve ItemData '{data.itemName}'");
            return;
        }

        // 4. สร้างไอเทมใหม่
        var newItemObj = Instantiate(InventoryManager.instance.itemPrefab, slot.transform);
        var newItem = newItemObj.GetComponent<InventoryItem>();
        
        if (newItem == null)
        {
            Destroy(newItemObj);
            return;
        }

        // Reset เฉพาะ position/scale — ห้ามแตะ anchors/offsets เพราะ prefab ตั้งไว้แล้ว
        // ถ้าบังคับ anchorMin=0,anchorMax=1 ไอเทมจะยืดเต็มช่อง vault ทำให้ icon ใหญ่ผิด
        RectTransform rect = newItemObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        // เตรียมข้อมูล Instance
        var instance = new ItemInstanceData(itemData, data.durability, data.weight, data.stackCount);
        
        // 5. รันฟังก์ชัน Init เพื่อโหลดรูปภาพและข้อมูล
        newItem.Init(instance);
        slot.SetItem(newItem);
        
        // บังคับให้ InventoryItem อัปเดตการแสดงผลรูปภาพอีกครั้งในเฟรมนี้
        newItem.RefreshStatDisplay(); 
    }

    private static ItemData ResolveItemDataByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        var all = Resources.FindObjectsOfTypeAll<ItemData>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].itemName == itemName) return all[i];
        }
        return null;
    }
}