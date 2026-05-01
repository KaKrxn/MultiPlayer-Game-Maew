using UnityEngine;
using TMPro;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using System.Collections;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Player-local UI script that hooks up to the existing InteractionText prefab.
    /// Listens to the InteractionAddon to show context dynamically.
    /// Attach this script to the root of the InteractionText prefab.
    /// </summary>
    public class InteractPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text keyPressText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.15f;

        private InteractionAddon m_LocalInteractionAddon;

        private void Start()
        {
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (keyPressText == null)
            {
                Transform keyPressTransform = transform.Find("KeyPress");
                if (keyPressTransform != null) keyPressText = keyPressTransform.GetComponent<TMP_Text>();
            }

            if (descText == null)
            {
                Transform descTransform = transform.Find("DescText");
                if (descTransform != null) descText = descTransform.GetComponent<TMP_Text>();
            }

            // Initial hide
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            
            StartCoroutine(FindLocalPlayerAddon());
        }

        private IEnumerator FindLocalPlayerAddon()
        {
            // Wait for NetworkManager to initialize and local player to spawn
            while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.LocalClient.PlayerObject == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            m_LocalInteractionAddon = playerObject.GetComponentInChildren<InteractionAddon>();

            if (m_LocalInteractionAddon != null)
            {
                m_LocalInteractionAddon.OnFocusChanged += HandleFocusChanged;
            }
            else
            {
                Debug.LogWarning("[InteractPromptUI] Failed to find local InteractionAddon on the player.");
            }
        }

        private void OnDestroy()
        {
            if (m_LocalInteractionAddon != null)
            {
                m_LocalInteractionAddon.OnFocusChanged -= HandleFocusChanged;
            }
        }

        private void HandleFocusChanged(IInteractable interactable)
        {
            StopAllCoroutines();

            if (interactable != null && !string.IsNullOrEmpty(interactable.InteractionPromptText))
            {
                if (keyPressText != null) keyPressText.text = "E"; // Set interact key string
                if (descText != null) descText.text = interactable.InteractionPromptText; // dynamic context like 'Start Train'
                
                StartCoroutine(FadeTo(1f));
            }
            else
            {
                StartCoroutine(FadeTo(0f));
            }
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }
    }
}
