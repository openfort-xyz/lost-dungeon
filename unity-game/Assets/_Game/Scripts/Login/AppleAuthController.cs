using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class AppleAuthController : PlayFabAuthControllerBase
{
    public void LoginWithApple()
    {
        LoginWithAppleRequest request = new LoginWithAppleRequest
        {
            IdentityToken = "",
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithApple(request, OnLoginSuccess, OnLoginError);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Successful login with Apple!");
        RaiseLoginSuccess(result);
    }

    private void OnLoginError(PlayFabError error)
    {
        Debug.LogError("Error logging in with Apple: " + error.ErrorMessage);
        RaiseLoginFailure(error);
    }
}
