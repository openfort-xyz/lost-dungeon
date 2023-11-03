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
    public void SignIn()
    {
        if (!_initialized)
        {
            Debug.LogError("Google SDK not initialized.");
            return;
        }
#if UNITY_WEBGL && !UNITY_EDITOR
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
        
        // Use the hashed token to log in with PlayFab
        LoginWithPlayFab(authData.customID, authData.email);
    }
    #endregion

    private void LoginWithPlayFab(string customId, string email)
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true, // set to true if you want to create an account if it doesn't exist
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetUserAccountInfo = true
            }
        };

        PlayFabClientAPI.LoginWithCustomID(request, RaiseLoginSuccess, RaiseLoginFailure);
    }
}