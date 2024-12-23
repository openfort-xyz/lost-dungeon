using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using PlayFab;
using PlayFab.ClientModels;

public class GoogleAuthController : PlayFabAuthControllerBase
{
    [Serializable]
    public class GoogleAuthData
    {
        public string customID;
        public string email;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    // Call this method from Unity to load the Google SDK
    [DllImport("__Internal")]
    private static extern void LoadGoogleSDK(string clientId);

    // Call this method from Unity to initiate Google Sign-In
    [DllImport("__Internal")]
    private static extern void StartGoogleSignIn();
#endif

    [SerializeField]
    private string googleClientId; // Set this in the Inspector or load from some config

    private bool _initialized;

    private bool _canLogIn = false;

    private void Start()
    {
        InitializeGoogleSDK();
    }

    #region PRIVATE_METHODS
    private void InitializeGoogleSDK()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LoadGoogleSDK(googleClientId);
#endif
    }
    #endregion

    #region BUTTON_METHODS
    public void SignIn(bool canLogIn)
    {
        if (!_initialized)
        {
            Debug.LogError("Google SDK not initialized.");
            return;
        }

        _canLogIn = canLogIn;
        
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("Starting Google Sign In...");
        StartGoogleSignIn();
#endif
    }
    #endregion
    
    #region JSLIB_MESSAGES
    // These will be called from JavaScript when the Google SDK finishes loading or when auth data is received
    public void GoogleSDKLoaded()
    {
        Debug.Log("Google SDK Loaded and Initialized!");

        _initialized = true;
    }

    public void OnReceiveAuthData(string jsonData)
    {
        var authData = JsonUtility.FromJson<GoogleAuthData>(jsonData);
        Debug.Log($"Received hashed token: {authData.customID} and email: {authData.email}");
        
        // Check if customId or email are null
        if (string.IsNullOrEmpty(authData.customID) || string.IsNullOrEmpty(authData.email))
        {
            Debug.LogError("Received invalid auth data");
            RaiseLoginFailure(new PlayFabError());
            return;
        }
        
        // Use the hashed token to log in with PlayFab
        LoginWithPlayFab(authData.customID, authData.email);
        RaiseLoginStarted();
    }
    #endregion

    private void LoginWithPlayFab(string customId, string email)
    {
        // TODO We can't use LoginWithGoogleAccount as it's expecting authToken from deprecated sign in method:
        // https://developers.google.com/identity/sign-in/web/sign-in
        // TODO In consequence, we're using google custom id as a password, which is not ideal security-wise.
        // TODO This needs to be updated when PlayFab updates LoginWithGoogleAccount to new Google sign in method.

        if (!_canLogIn)
        {
            RegisterPlayFabUser(email, customId);
            return;
        }

        var request = new LoginWithEmailAddressRequest()
        {
            Password = customId,
            Email = email,
            InfoRequestParameters = PlayerCombinedInfoRequestParams
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, RaiseLoginSuccess, error =>
        {
            if (error.Error == PlayFabErrorCode.AccountNotFound)
            {
                // Here, you can call your RegisterPlayFabUser function, with the username and password that was just rejected
                RegisterPlayFabUser(email, customId);
            }
            else
            {
                RaiseLoginFailure(error);
            }
        });
    }
    
    private void RegisterPlayFabUser(string email, string customId)
    {
        var request = new RegisterPlayFabUserRequest()
        {
            Email = email,
            Password = customId,
            InfoRequestParameters = playerCombinedInfoRequestParams,
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(
            request,
            result =>
            {
                Debug.Log("New PlayFab account registered successfully.");
                RaiseRegisterSuccess(result);
            },
            error =>
            {
                Debug.LogError("Failed to register a new PlayFab account.");
                RaiseLoginFailure(error);
            });
    }
}