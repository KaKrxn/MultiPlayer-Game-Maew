using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RepairHoldProgressUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image progressImage;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 12f;

    private InteractionAddon _interactionAddon;
    private float _targetAlpha;
    private float _progress;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (progressImage != null)
            progressImage.fillAmount = 0f;

        TrainRepairZone.OnLocalRepairHoldStateChanged += HandleFallbackRepairHoldStateChanged;
    }

    private void Update()
    {
        EnsureInteractionAddon();

        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, _targetAlpha, 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime));

        if (progressImage != null)
            progressImage.fillAmount = Mathf.Lerp(progressImage.fillAmount, _progress, 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime));
    }

    private void OnDestroy()
    {
        TrainRepairZone.OnLocalRepairHoldStateChanged -= HandleFallbackRepairHoldStateChanged;
        Unsubscribe();
    }

    private void EnsureInteractionAddon()
    {
        if (_interactionAddon != null)
            return;

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null)
            return;

        NetworkObject playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObject == null)
            return;

        _interactionAddon = playerObject.GetComponentInChildren<InteractionAddon>(true);
        if (_interactionAddon == null)
            return;

        _interactionAddon.OnHoldStateChanged += HandleHoldStateChanged;
        _interactionAddon.OnFocusChanged += HandleFocusChanged;
    }

    private void Unsubscribe()
    {
        if (_interactionAddon == null)
            return;

        _interactionAddon.OnHoldStateChanged -= HandleHoldStateChanged;
        _interactionAddon.OnFocusChanged -= HandleFocusChanged;
        _interactionAddon = null;
    }

    private void HandleFocusChanged(IInteractable interactable)
    {
        if (!(interactable is TrainRepairZone))
            Hide();
    }

    private void HandleHoldStateChanged(IInteractable interactable, float progress, bool isHolding)
    {
        if (interactable is TrainRepairZone && isHolding)
        {
            _targetAlpha = 1f;
            _progress = Mathf.Clamp01(progress);
            return;
        }

        Hide();
    }

    private void HandleFallbackRepairHoldStateChanged(float progress, bool isHolding)
    {
        if (isHolding)
        {
            _targetAlpha = 1f;
            _progress = Mathf.Clamp01(progress);
            return;
        }

        Hide();
    }

    private void Hide()
    {
        _targetAlpha = 0f;
        _progress = 0f;
    }
}
