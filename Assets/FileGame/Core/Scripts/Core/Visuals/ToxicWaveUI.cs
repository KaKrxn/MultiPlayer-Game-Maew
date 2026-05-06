using UnityEngine;
using TMPro;
using System.Collections;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Handles the UI pop-up for the Toxic Wave activation.
    /// </summary>
    public class ToxicWaveUI : MonoBehaviour
    {
        public static ToxicWaveUI Instance;

        [Header("References")]
        [SerializeField] private DayNightCycleManager dayNightManager;

        [Header("UI Components")]
        [SerializeField] private TMP_Text warningText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private string message = "TOXIC WAVE IS COMING!";
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeOutDuration = 2f;
        [SerializeField] private Vector3 punchScale = new Vector3(1.2f, 1.2f, 1.2f);

        private void Awake()
        {
            Debug.Log($"[ToxicWaveUI] Awake called on GameObject: {gameObject.name}");
            if (Instance == null) 
            {
                Instance = this;
            }
            else 
            {
                Debug.LogWarning($"[ToxicWaveUI] Duplicate instance on {gameObject.name}, destroying.");
                Destroy(gameObject);
                return;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0;

            if (dayNightManager == null)
            {
                dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
            }

            if (dayNightManager != null)
            {
                dayNightManager.OnStateChanged += HandleDayNightChanged;
            }
        }

        private void OnDestroy()
        {
            if (dayNightManager != null)
            {
                dayNightManager.OnStateChanged -= HandleDayNightChanged;
            }
        }

        private void HandleDayNightChanged(DayNightState state)
        {
            if (state == DayNightState.Night)
            {
                ShowWarning("Beware Your back");
            }
        }

        public void ShowWarning(string customMessage = null)
        {
            StopAllCoroutines();
            StartCoroutine(WarningSequence(customMessage));
        }

        private IEnumerator WarningSequence(string customMessage)
        {
            if (warningText != null) warningText.text = !string.IsNullOrEmpty(customMessage) ? customMessage : message;
            if (canvasGroup == null) yield break;

            // Punch scale effect
            transform.localScale = Vector3.one;
            
            // Fade In
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                canvasGroup.alpha = t;
                transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, t);
                yield return null;
            }
            canvasGroup.alpha = 1;
            transform.localScale = Vector3.one;

            // Wait
            yield return new WaitForSeconds(displayDuration);

            // Fade Out
            elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                canvasGroup.alpha = 1 - t;
                yield return null;
            }
            canvasGroup.alpha = 0;
        }
    }
}
