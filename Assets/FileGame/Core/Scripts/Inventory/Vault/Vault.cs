using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// คอมโพเนนต์ที่แปะบน GameObject ของ Vault ใน scene (Tag: Vault)
/// เมื่อผู้เล่นกด E -> เปิด VaultUI โดยผูกกับ NetworkVault ของตัวเอง
/// </summary>
[RequireComponent(typeof(NetworkVault))]
public class Vault : MonoBehaviour, IInteractable 
{
    private NetworkVault _networkVault;

    public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    public int Priority => 0;
    public string InteractionPromptText => "Open Vault"; 

    private void Awake()
    {
        _networkVault = GetComponent<NetworkVault>();
    }

    public bool CanInteract(GameObject interactor)
    {
        return _networkVault != null && VaultUI.Instance != null;
    }

    public void Interact(GameObject interactor)
    {
        if (_networkVault == null)
        {
            Debug.LogWarning("[Vault] No NetworkVault on " + name);
            return;
        }

        if (VaultUI.Instance == null)
        {
            Debug.LogWarning("[Vault] No VaultUI in scene. Please add VaultUI to the player's UI canvas.");
            return;
        }

        VaultUI.Instance.Open(_networkVault);
    }
}