using System.Collections;
using System.Collections.Generic;
using System.Text;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.Events;

public class AppleAuthController : PlayFabAuthControllerBase
{
    public UnityEvent appleAuthError;
    
    private IAppleAuthManager _appleAuthManager;

    private string _appleIdToken;

    private void Update()
    {
        if (!AppleAuthManager.IsCurrentPlatformSupported) return;
        
        // Updates the AppleAuthManager instance to execute
        // pending callbacks inside Unity's execution loop
        if (this._appleAuthManager != null)
        {
            this._appleAuthManager.Update();
        }
    }

    public void Initialize()
    {
        // If the current platform is supported
        if (AppleAuthManager.IsCurrentPlatformSupported)
        {
            // Creates a default JSON deserializer, to transform JSON Native responses to C# instances
            var deserializer = new PayloadDeserializer();
            // Creates an Apple Authentication manager with the deserializer
            this._appleAuthManager = new AppleAuthManager(deserializer);    
        }
        
        // Check if the current platform supports Sign In With Apple
        if (this._appleAuthManager == null)
        {
            appleAuthError?.Invoke();
            return;
        }
        
        // If at any point we receive a credentials revoked notification, we delete the stored User ID, and go back to login
        this._appleAuthManager.SetCredentialsRevokedCallback(result =>
        {
            Debug.Log("Received revoked callback " + result);
            //TODO attempt quick login or to playfab default auth?
        });
        
        this.AttemptQuickLogin();
    }

    /* Not using it
    private void CheckCredentialStatusForUserId(string appleUserId)
    {
        // If there is an apple ID available, we should check the credential state
        this._appleAuthManager.GetCredentialState(
            appleUserId,
            state =>
            {
                switch (state)
                {
                    // If it's authorized, login with that user id
                    case CredentialState.Authorized:
                        Debug.Log("Authorized!");
                        LoginToPlayFabWithApple(appleUserId);
                        return;
                    
                    // If it was revoked, or not found, we need a new sign in with apple attempt
                    // Discard previous apple user id
                    case CredentialState.Revoked:
                        appleAuthError?.Invoke();
                        PlayerPrefs.DeleteKey(GameConstants.AppleIdTokenKey);
                        return;
                    
                    case CredentialState.NotFound:
                        appleAuthError?.Invoke();
                        PlayerPrefs.DeleteKey(GameConstants.AppleIdTokenKey);
                        return;
                }
            },
            error =>
            {
                var authorizationErrorCode = error.GetAuthorizationErrorCode();
                Debug.LogWarning("Error while trying to get credential state " + authorizationErrorCode.ToString() + " " + error.ToString());
                appleAuthError?.Invoke();
            });
    }
    */
    
    private void AttemptQuickLogin()
    {
        var quickLoginArgs = new AppleAuthQuickLoginArgs();
        
        // Quick login should succeed if the credential was authorized before and not revoked
        this._appleAuthManager.QuickLogin(
            quickLoginArgs,
            credential =>
            {
                // If it's an Apple credential, save the user ID, for later logins
                var appleIdCredential = credential as IAppleIDCredential;
                if (appleIdCredential == null) return;
                
                Debug.Log("Quick login success!");
                
                var appleIdToken = SetupIdentityToken(credential);
                LoginToPlayFabWithApple(appleIdToken);
            },
            error =>
            {
                // If Quick Login fails, we should show the normal sign in with apple menu, to allow for a normal Sign In with apple
                var authorizationErrorCode = error.GetAuthorizationErrorCode();
                Debug.LogWarning("Quick Login Failed " + authorizationErrorCode.ToString() + " " + error.ToString());
                SignInWithApple();
            });
    }
    
    private void SignInWithApple()
    {
        var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);
        
        this._appleAuthManager.LoginWithAppleId(
            loginArgs,
            credential =>
            {
                // If a sign in with apple succeeds, we should have obtained the credential with the user id, name, and email, save it
                Debug.Log("Sign in with Apple success!");

                var appleIdToken = SetupIdentityToken(credential);
                LoginToPlayFabWithApple(appleIdToken);
            },
            error =>
            {
                var authorizationErrorCode = error.GetAuthorizationErrorCode();
                Debug.LogWarning("Sign in with Apple failed " + authorizationErrorCode.ToString() + " " + error.ToString());
                appleAuthError?.Invoke();
            });
    }

    private string SetupIdentityToken(ICredential receivedCredential)
    {
        var appleIdCredential = receivedCredential as IAppleIDCredential;

        if (appleIdCredential.IdentityToken != null)
        {
            var identityToken = Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0,
                appleIdCredential.IdentityToken.Length);

            _appleIdToken = identityToken;
            return _appleIdToken;
        }
        
        Debug.LogError("Identity token is null.");
        return null;
    }

    private void LoginToPlayFabWithApple(string appleIdToken)
    {
        var request = new LoginWithAppleRequest()
        {
            IdentityToken = appleIdToken,
            CreateAccount = true,
            InfoRequestParameters = playerCombinedInfoRequestParams
        };
        PlayFabClientAPI.LoginWithApple(request, RaiseLoginSuccess, RaiseLoginFailure);
    }
}
