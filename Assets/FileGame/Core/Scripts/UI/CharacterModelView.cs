using UnityEngine;
using UnityEngine.UI;

namespace FileGame.Core.UI
{
    /// <summary>
    /// Renders a 3D character model to a UI RawImage.
    /// Spawns a clone of the player prefab in a hidden area and uses a secondary camera
    /// to render it to a RenderTexture.
    /// Attach this to the RawImage UI GameObject.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class CharacterModelView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(512, 512);
        [SerializeField] private Vector3 previewOffset = new Vector3(0, 1000f, 0); // Hide far away
        [SerializeField] private LayerMask previewLayer; // Create a "CharacterPreview" layer or use an unused one (e.g., 20)
        [SerializeField] private float rotationSpeed = 15f;
        
        [Header("Camera Settings")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 1f, -3f);
        [SerializeField] private float fieldOfView = 60f;
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0f);

        private RawImage _rawImage;
        private RenderTexture _renderTexture;
        private Camera _previewCamera;
        private GameObject _previewModel;
        
        private int _previewLayerId = 20; // Default to layer 20 if mask not set

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            
            // Try to find a valid layer from mask
            for (int i = 0; i < 32; i++)
            {
                if (previewLayer == (previewLayer | (1 << i)))
                {
                    _previewLayerId = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Called by InventoryManager when the inventory is toggled.
        /// </summary>
        public void Refresh(bool visible)
        {
            if (visible)
            {
                CleanupPreview(); // Safety cleanup
                SetupPreview();
            }
            else
            {
                CleanupPreview();
            }
        }

        private void OnDisable()
        {
            CleanupPreview();
        }

        private void SetupPreview()
        {
            if (PlayerLocation.localPlayerMovement == null)
            {
                Debug.LogWarning("[CharacterModelView] Local player movement reference is missing.");
                return;
            }

            // 1. Create Render Texture
            _renderTexture = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 24, RenderTextureFormat.ARGB32);
            _renderTexture.antiAliasing = 4;
            _renderTexture.Create();
            _rawImage.texture = _renderTexture;
            _rawImage.color = Color.white;

            // 2. Clone the player model
            _previewModel = Instantiate(PlayerLocation.localPlayerMovement.gameObject, previewOffset, Quaternion.identity);
            _previewModel.name = "Character_Preview_Clone";
            
            // Strip logic
            StripComponents(_previewModel);
            SetLayerRecursive(_previewModel, _previewLayerId);

            // 3. Create Camera
            GameObject camObj = new GameObject("CharacterPreviewCamera");
            camObj.transform.position = _previewModel.transform.position + cameraOffset;
            camObj.transform.LookAt(_previewModel.transform.position + Vector3.up * 1f); 
            
            _previewCamera = camObj.AddComponent<Camera>();
            _previewCamera.cullingMask = 1 << _previewLayerId; 
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = backgroundColor;
            _previewCamera.fieldOfView = fieldOfView;
            _previewCamera.targetTexture = _renderTexture;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 10f;
            _previewCamera.enabled = true;
        }

        private void Update()
        {
            if (_previewModel != null)
            {
                // Slowly rotate the character
                _previewModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void CleanupPreview()
        {
            if (_previewCamera != null)
            {
                _previewCamera.targetTexture = null;
                Destroy(_previewCamera.gameObject);
                _previewCamera = null;
            }

            if (_previewModel != null)
            {
                Destroy(_previewModel);
                _previewModel = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }

            if (_rawImage != null)
            {
                _rawImage.texture = null;
            }
        }

        private void StripComponents(GameObject root)
        {
            // Instead of destroying (which causes dependency errors), we just disable
            // irrelevant components to keep the preview clone clean and lightweight.
            
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var comp in components)
            {
                if (comp == null || comp is Transform) continue;

                // Keep only what's needed for visuals
                bool isVisual = comp is Renderer || comp is MeshFilter || comp is Animator || comp is Animation;
                
                if (isVisual)
                {
                    // Ensure visual components are active
                    if (comp is Renderer r) r.enabled = true;
                    if (comp is Behaviour b) b.enabled = true;
                    continue;
                }

                // Disable everything else (Colliders, Rigidbodies, Audio, Custom Scripts, UI)
                if (comp is Behaviour behaviour)
                {
                    behaviour.enabled = false;
                }
                else if (comp is Collider col)
                {
                    col.enabled = false;
                }
                // We don't destroy CanvasRenderer/Rigidbodies because of dependencies,
                // but since the behaviours are off, they won't do anything.
            }
        }

        private void SetLayerRecursive(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child == null) continue;
                SetLayerRecursive(child.gameObject, newLayer);
            }
        }
    }
}
