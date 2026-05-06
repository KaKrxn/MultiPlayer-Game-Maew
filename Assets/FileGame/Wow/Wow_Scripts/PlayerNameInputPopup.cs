using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameInputPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text validationText;

    [Header("Behaviour")]
    [SerializeField] private bool showAutomaticallyOnMissingName = true;

    public event Action<string> NameSubmitted;

    private bool m_CanCancel;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        SetVisible(false);
    }

    private void OnEnable()
    {
        UnityServicesBootstrap.OnInitializationCompleted += HandleUnityServicesInitialized;
    }

    private void Start()
    {
        if (!showAutomaticallyOnMissingName)
            return;

        if (UnityServicesBootstrap.IsInitialized)
            ShowIfNameMissing();
    }

    private void OnDisable()
    {
        UnityServicesBootstrap.OnInitializationCompleted -= HandleUnityServicesInitialized;
    }

    private void OnDestroy()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(Submit);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Cancel);
    }

    public bool HasSavedName()
    {
        return !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(PlayerNameRegistry.PlayerPrefsKey, string.Empty));
    }

    public void ShowIfNameMissing()
    {
        if (!HasSavedName())
            Show(false);
    }

    public void ShowForRename()
    {
        Show(true);
    }

    public void Show(bool canCancel)
    {
        m_CanCancel = canCancel;

        if (nameInput != null)
        {
            string savedName = PlayerPrefs.GetString(PlayerNameRegistry.PlayerPrefsKey, string.Empty);
            nameInput.text = savedName;
        }

        if (validationText != null)
            validationText.text = string.Empty;

        if (cancelButton != null)
            cancelButton.gameObject.SetActive(canCancel);

        SetVisible(true);
        StartCoroutine(FocusInputNextFrame());
    }

    private void Submit()
    {
        string submittedName = nameInput != null ? nameInput.text : string.Empty;
        string sanitizedName = PlayerNameRegistry.SanitizeName(submittedName);

        if (string.IsNullOrWhiteSpace(submittedName.Trim()))
        {
            if (validationText != null)
                validationText.text = "Please enter a name.";
            return;
        }

        PlayerNameRegistry.SaveLocalPlayerName(sanitizedName);
        NameSubmitted?.Invoke(sanitizedName);
        SetVisible(false);
    }

    private void Cancel()
    {
        if (!m_CanCancel)
            return;

        SetVisible(false);
    }

    private void HandleUnityServicesInitialized(bool success)
    {
        if (success && showAutomaticallyOnMissingName)
            ShowIfNameMissing();
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;

        if (nameInput == null)
            yield break;

        nameInput.Select();
        nameInput.ActivateInputField();
    }

    private void SetVisible(bool isVisible)
    {
        if (popupRoot != null)
            popupRoot.SetActive(isVisible);
    }
}
