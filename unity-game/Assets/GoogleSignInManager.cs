using UnityEngine;
using System.Runtime.InteropServices;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class GoogleSignInManager : MonoBehaviour
{
    [System.Serializable]
    public class GoogleAuthData
    {
        public string customID;
        public string email;
    }

    [SerializeField]
    private string googleClientId; // Set this in the Inspector or load from some config

    public TextMeshProUGUI statusText;

    // Call this method from Unity to load the Google SDK
    [DllImport("__Internal")]
    private static extern void LoadGoogleSDK(string clientId);

    // Call this method from Unity to initiate Google Sign-In
    [DllImport("__Internal")]
    private static extern void StartGoogleSignIn();

    public void BeginGoogleLogin()
    {
        statusText.text = "Initializing Google SDK...";
        LoadGoogleSDK(googleClientId);
    }

    // This will be called from JavaScript when the Google SDK finishes loading
    public void GoogleSDKLoaded()
    {
        Debug.Log("Google SDK Loaded and Initialized!");
        statusText.text = "Google SDK loaded. Starting Google Sign-In...";
        StartGoogleSignIn();
    }

    // This will be called from JavaScript when you receive a server auth code
    public void OnReceiveAuthData(string jsonData)
    {
        var authData = JsonUtility.FromJson<GoogleAuthData>(jsonData);
        Debug.Log($"Received hashed token: {authData.customID} and email: {authData.email}");
        statusText.text = "Received Google authentication. Logging into PlayFab...";
        
        // Use the hashed token to log in with PlayFab
        LoginWithPlayFab(authData.customID, authData.email);
    }

    // Handle popup blocked by the browser
    public void OnPopupBlocked()
    {
        Debug.Log("Popup was blocked. Please allow popups for this site.");
        statusText.text = "Popup was blocked. Please enable popups and try again.";
    }

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

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Successfully logged in to PlayFab using Google account!");
        statusText.text = "Successfully logged in to PlayFab! Player ID: " + result.PlayFabId;
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("Failed to log in to PlayFab using Google account: " + error.GenerateErrorReport());
        statusText.text = "Failed to log in to PlayFab using Google account: " + error.ErrorMessage;
    }
}