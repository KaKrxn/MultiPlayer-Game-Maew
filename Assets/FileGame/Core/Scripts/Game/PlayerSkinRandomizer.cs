using UnityEngine;
using Unity.Netcode;

public class PlayerSkinRandomizer : NetworkBehaviour
{
    [Tooltip("The renderer that displays the player's skin (e.g., SkinnedMeshRenderer).")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("The material index on the renderer to replace. Usually 0.")]
    [SerializeField] private int materialIndex = 0;

    [Tooltip("List of possible skin materials.")]
    [SerializeField] private Material[] skinMaterials;

    // A synchronized variable for the chosen skin index. 
    // It is synced to all clients, and updates when the value changes.
    private NetworkVariable<int> currentSkinIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        // Subscribe to changes in the skin index so clients update visually when it changes.
        currentSkinIndex.OnValueChanged += OnSkinIndexChanged;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Only the server determines the random skin when the player spawns
            if (skinMaterials != null && skinMaterials.Length > 0)
            {
                int randomIndex = Random.Range(0, skinMaterials.Length);
                currentSkinIndex.Value = randomIndex;
            }
        }
        else
        {
            // For clients, if the value is already set when we spawn, apply it immediately.
            if (currentSkinIndex.Value != -1)
            {
                ApplySkin(currentSkinIndex.Value);
            }
        }
    }

    private void OnSkinIndexChanged(int previousValue, int newValue)
    {
        ApplySkin(newValue);
    }

    private void ApplySkin(int index)
    {
        if (targetRenderer == null || skinMaterials == null || skinMaterials.Length == 0)
        {
            return;
        }

        if (index >= 0 && index < skinMaterials.Length)
        {
            Material newMat = skinMaterials[index];

            // Assign the material safely to the specific material index (supports multi-material renderers)
            Material[] mats = targetRenderer.materials;
            if (materialIndex >= 0 && materialIndex < mats.Length)
            {
                mats[materialIndex] = newMat;
                targetRenderer.materials = mats;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        currentSkinIndex.OnValueChanged -= OnSkinIndexChanged;
        base.OnNetworkDespawn();
    }
}
