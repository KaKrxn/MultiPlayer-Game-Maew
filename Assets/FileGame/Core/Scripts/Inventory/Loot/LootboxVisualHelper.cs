using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Handles client-side visualization helpers for the Lootbox (HCI implementation).
    /// </summary>
    public class LootboxVisualHelper : MonoBehaviour
    {
        [Header("Light Pillar Settings")]
        [Tooltip("The MeshRenderer for the Light Pillar object.")]
        [SerializeField] private Renderer lightPillarRenderer;
        
        [Tooltip("The distance at which the pillar becomes completely invisible.")]
        [SerializeField] private float maxVisibilityDistance = 40f;
        
        [Tooltip("The distance at which the pillar starts fading out.")]
        [SerializeField] private float fadeStartDistance = 20f;
        
        [Header("Pulse Animation")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float minPulseAlpha = 0.4f;
        [SerializeField] private float maxPulseAlpha = 1.0f;

        private Material _pillarMaterial;
        private Color _baseColor;
        private bool _isUsingBaseColorProp = false;

        private void Start()
        {
            if (lightPillarRenderer != null)
            {
                // Instantiate material to avoid changing the shared project asset
                _pillarMaterial = lightPillarRenderer.material;
                
                // Support both Built-In and URP/HDRP shaders
                if (_pillarMaterial.HasProperty("_BaseColor"))
                {
                    _baseColor = _pillarMaterial.GetColor("_BaseColor");
                    _isUsingBaseColorProp = true;
                }
                else if (_pillarMaterial.HasProperty("_Color"))
                {
                    _baseColor = _pillarMaterial.color;
                    _isUsingBaseColorProp = false;
                }
                else
                {
                    Debug.LogWarning("[LootboxVisualHelper] Light Pillar material doesn't have _Color or _BaseColor property.");
                }
            }
        }

        private void Update()
        {
            if (lightPillarRenderer == null || _pillarMaterial == null) return;

            // Use Camera.main to find the local player's view
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            float distance = Vector3.Distance(transform.position, mainCam.transform.position);

            // 1. Calculate Distance Alpha (fade out when far)
            float distanceAlpha = 1f;
            if (distance > maxVisibilityDistance)
            {
                distanceAlpha = 0f;
            }
            else if (distance > fadeStartDistance)
            {
                distanceAlpha = 1f - ((distance - fadeStartDistance) / (maxVisibilityDistance - fadeStartDistance));
            }

            // 2. Calculate Pulse Alpha (breathing effect)
            float pulseAlpha = 1f;
            if (enablePulse)
            {
                // Sine wave from 0 to 1
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; 
                pulseAlpha = Mathf.Lerp(minPulseAlpha, maxPulseAlpha, t);
            }

            // 3. Apply final color
            Color newColor = _baseColor;
            newColor.a = _baseColor.a * distanceAlpha * pulseAlpha;

            if (_isUsingBaseColorProp)
            {
                _pillarMaterial.SetColor("_BaseColor", newColor);
            }
            else
            {
                _pillarMaterial.color = newColor;
            }
            
            // Optimization: Disable renderer completely if invisible to save draw calls
            lightPillarRenderer.enabled = (newColor.a > 0.01f);
        }

        private void OnDestroy()
        {
            // Clean up instantiated material to prevent memory leaks
            if (_pillarMaterial != null)
            {
                Destroy(_pillarMaterial);
            }
        }
    }
}
