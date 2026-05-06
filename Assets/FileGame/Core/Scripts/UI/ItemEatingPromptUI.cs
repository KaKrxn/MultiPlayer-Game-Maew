using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FileGame.Core;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Specialized UI component for showing item consumption (eating) progress.
    /// Extracted from InteractPromptUI to decouple player actions from world interactions.
    /// </summary>
    public class ItemEatingPromptUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private float uiSmoothTime = 0.1f;
        
        [Header("Donut Visuals")]
        [SerializeField] private Image backgroundDonut;
        [SerializeField] private Image progressDonut;
        [SerializeField] private float donutSize = 100f;
        [SerializeField] private float donutThickness = 10f;
        
        private Camera m_MainCamera;
        private bool m_IsEatingItem;
        private float m_CurrentHoldProgress;
        private bool m_IsShowing;
        private Vector3 m_CurrentVelocity = Vector3.zero;

        private void Start()
        {
            m_MainCamera = Camera.main;
            
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            SetupProceduralDonut();
            PlayerItemUseSystem.OnItemEatingProgress += HandleItemEatingProgress;
            
            // Start hidden
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
        }

        private void SetupProceduralDonut()
        {
            // If background is missing, create it
            if (backgroundDonut == null)
            {
                GameObject bgObj = new GameObject("DonutBG", typeof(RectTransform), typeof(Image));
                bgObj.transform.SetParent(transform, false);
                backgroundDonut = bgObj.GetComponent<Image>();
                backgroundDonut.color = new Color(0, 0, 0, 0.75f);
                
                RectTransform rt = bgObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(donutSize, donutSize);
            }

            // If progress is missing, create it
            if (progressDonut == null)
            {
                GameObject fgObj = new GameObject("DonutProgress", typeof(RectTransform), typeof(Image));
                fgObj.transform.SetParent(transform, false);
                progressDonut = fgObj.GetComponent<Image>();
                progressDonut.color = new Color(1f, 0.5f, 0f, 1f); // Orange
                progressDonut.type = Image.Type.Filled;
                progressDonut.fillMethod = Image.FillMethod.Radial360;
                progressDonut.fillOrigin = (int)Image.Origin360.Top;
                progressDonut.fillClockwise = true;
                
                RectTransform rt = fgObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(donutSize, donutSize);
            }

            // Add an inner hole to make it look like a donut
            GameObject holeObj = new GameObject("DonutHole", typeof(RectTransform), typeof(Image));
            holeObj.transform.SetParent(transform, false);
            Image holeImg = holeObj.GetComponent<Image>();
            // If the UI is floating in world space, we can't easily "mask" to transparency 
            // without a specialized shader or Mask component. 
            // For now, we'll make the hole dark to match the background.
            holeImg.color = new Color(0, 0, 0, 1f); 
            
            RectTransform holeRT = holeObj.GetComponent<RectTransform>();
            float holeSize = donutSize - (donutThickness * 2);
            holeRT.sizeDelta = new Vector2(holeSize, holeSize);
        }

        private void OnDestroy()
        {
            PlayerItemUseSystem.OnItemEatingProgress -= HandleItemEatingProgress;
        }

        private void Update()
        {
            if (m_MainCamera == null) m_MainCamera = Camera.main;

            if (!m_IsShowing || !m_IsEatingItem) return;

            if (m_MainCamera == null) return;
            
            Vector3 targetPosition = m_MainCamera.transform.position + m_MainCamera.transform.forward * 1.5f;
            
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref m_CurrentVelocity, uiSmoothTime);
            transform.rotation = Quaternion.LookRotation(transform.position - m_MainCamera.transform.position);
            
            // Update progress
            if (progressDonut != null)
            {
                progressDonut.fillAmount = m_CurrentHoldProgress;
            }
        }

        private void HandleItemEatingProgress(float progress, bool isEating)
        {
            m_IsEatingItem = isEating;
            m_CurrentHoldProgress = progress;
            
            if (isEating && !m_IsShowing)
            {
                m_IsShowing = true;
                StopAllCoroutines();
                StartCoroutine(ShowAnimation());
            }
            else if (!isEating && m_IsShowing)
            {
                m_IsShowing = false;
                StopAllCoroutines();
                StartCoroutine(HideAnimation());
            }
        }

        private IEnumerator ShowAnimation()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
                yield return null;
            }
            
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        private IEnumerator HideAnimation()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            Vector3 startScale = transform.localScale;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
        }
    }
}
