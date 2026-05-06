using UnityEngine;
using UnityEngine.UI;
using FileGame.Core;
using Unity.Netcode;

namespace FileGame.Core.UI
{
    /// <summary>
    /// Displays a donut-shaped progress bar when holding LMB to use an item.
    /// </summary>
    public class ItemUseProgressUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image progressImage; // Radial fill
        [SerializeField] private Image backgroundImage; // Static donut/circle
        
        [Header("Settings")]
        [SerializeField] private float fadeSpeed = 10f;
        
        private PlayerItemUseSystem m_LocalUseSystem;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            // Find local player use system if not assigned
            if (m_LocalUseSystem == null)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && NetworkManager.Singleton.LocalClient != null)
                {
                    var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
                    if (playerObj != null)
                    {
                        m_LocalUseSystem = playerObj.GetComponent<PlayerItemUseSystem>();
                    }
                }
                return;
            }

            float targetAlpha = (m_LocalUseSystem.IsHolding && m_LocalUseSystem.HoldProgress > 0) ? 1f : 0f;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            }

            if (progressImage != null)
            {
                progressImage.fillAmount = m_LocalUseSystem.HoldProgress;
            }
        }

        // --- Helper for creating the UI procedurally if needed ---
        [ContextMenu("Setup Default UI")]
        public void SetupDefaultUI()
        {
            // This is a helper to setup the components if they are missing
            if (GetComponent<Canvas>() == null)
            {
                Canvas c = gameObject.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 100;
            }
            
            if (GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Create Background
            if (backgroundImage == null)
            {
                GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(transform, false);
                backgroundImage = bg.GetComponent<Image>();
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Black/Grey
                RectTransform rt = bg.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100, 100);
            }

            // Create Progress
            if (progressImage == null)
            {
                GameObject fg = new GameObject("Progress", typeof(RectTransform), typeof(Image));
                fg.transform.SetParent(transform, false);
                progressImage = fg.GetComponent<Image>();
                progressImage.color = new Color(1f, 0.5f, 0f, 1f); // Orange
                progressImage.type = Image.Type.Filled;
                progressImage.fillMethod = Image.FillMethod.Radial360;
                progressImage.fillAmount = 0;
                RectTransform rt = fg.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100, 100);
            }
        }
    }
}
