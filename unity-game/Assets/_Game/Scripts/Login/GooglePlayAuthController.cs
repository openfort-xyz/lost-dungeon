using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using UnityEngine;
using UnityEngine.Events;

public class GooglePlayAuthController : PlayFabAuthControllerBase
{
    public UnityEvent<string> OnGooglePlayAuthSuccess;
    public UnityEvent OnGooglePlayAuthError;
    
    public void Authenticate()
    {
        #if UNITY_ANDROID
        PlayGamesPlatform.Activate();
        
        PlayGamesPlatform.Instance.Authenticate(success =>
        {
            if (success == SignInStatus.Success)
            {
                Debug.Log("Login with Google Play successful.");
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, authCode =>
                {
                    Debug.Log($"Auth code is {authCode}");
                    LoginUserWithGooglePlay(authCode);
                });
            }
            else 
            {
                Debug.Log(success.ToString());
                Debug.LogError("Failed to retrieve Google Play auth code.");
                OnGooglePlayAuthError?.Invoke();
            }
        });
        #else
        Debug.Log("Google Play Games SDK only works on Android devices. Please build your app to an Android device.");
        #endif
    }
    
    private void LoginUserWithGooglePlay(string googleAuthCode)
    {
        var loginRequest = new LoginWithGooglePlayGamesServicesRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            ServerAuthCode = googleAuthCode,
            CreateAccount = true,
            InfoRequestParameters = playerCombinedInfoRequestParams
        };

        PlayFabClientAPI.LoginWithGooglePlayGamesServices(loginRequest, RaiseLoginSuccess, RaiseLoginFailure);
        RaiseLoginStarted();
    }
}
