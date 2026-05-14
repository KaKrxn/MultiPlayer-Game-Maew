using Blocks.Gameplay.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class TrainRepairWorldPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrainRepairZone repairZone;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text promptText;

    [Header("Display")]
    [SerializeField] private string repairPrompt = "Hold E to Repair";
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private float fadeSpeed = 12f;

    private Camera _mainCamera;
    private Transform _targetTransform;
    private float _targetAlpha;

    private void Reset()
    {
        repairZone = GetComponentInParent<TrainRepairZone>();
        worldCanvas = GetComponentInChildren<Canvas>(true);
        canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        promptText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Awake()
    {
        if (repairZone == null)
            repairZone = GetComponentInParent<TrainRepairZone>();

        if (worldCanvas == null)
            worldCanvas = GetComponentInChildren<Canvas>(true);

        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        if (promptText == null)
            promptText = GetComponentInChildren<TMP_Text>(true);

        _targetTransform = repairZone != null ? repairZone.transform : transform.parent;

        if (worldCanvas != null)
            worldCanvas.renderMode = RenderMode.WorldSpace;

        if (promptText != null)
            promptText.text = repairPrompt;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void LateUpdate()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        bool shouldShow = CanLocalPlayerRepair();
        _targetAlpha = shouldShow ? 1f : 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (!shouldShow && (canvasGroup == null || canvasGroup.alpha <= 0f))
            return;

        if (_targetTransform != null)
            transform.position = _targetTransform.position + worldOffset;

        if (_mainCamera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);

        if (promptText != null && promptText.text != repairPrompt)
            promptText.text = repairPrompt;
    }

    private bool CanLocalPlayerRepair()
    {
        if (repairZone == null ||
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsConnectedClient ||
            NetworkManager.Singleton.LocalClient == null ||
            NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            return false;
        }

        return repairZone.CanInteract(NetworkManager.Singleton.LocalClient.PlayerObject.gameObject);
    }
}
