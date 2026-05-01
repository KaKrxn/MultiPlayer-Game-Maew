using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UnityServicesBootstrap : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }
    public static event Action<bool> OnInitializationCompleted;

    private async void Awake()
    {
        await InitializeAsync();
    }

    public static async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            OnInitializationCompleted?.Invoke(true);
            return;
        }

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            IsInitialized = true;
            Debug.Log("Unity Services initialized successfully.");
            OnInitializationCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            IsInitialized = false;
            Debug.LogException(ex);
            OnInitializationCompleted?.Invoke(false);
        }
    }
}