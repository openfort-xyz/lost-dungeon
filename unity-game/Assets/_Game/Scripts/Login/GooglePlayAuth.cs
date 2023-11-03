using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using UnityEngine;
using UnityEngine.Events;

public class GooglePlayAuth : MonoBehaviour
{
    public UnityEvent<string> OnGooglePlayAuthSuccess;
    public UnityEvent OnGooglePlayAuthError;

    private void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            Authenticate();
        }
        else
        {
            Debug.Log("Application.Platform is not Android. We go ahead with PlayFab standard login.");
            OnGooglePlayAuthError?.Invoke();
        }
    }

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
                    OnGooglePlayAuthSuccess?.Invoke(authCode);
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

    //TODO in another script
    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Close the game
            QuitGame();
        }
    }

    void QuitGame()
    {
        // If we are running in a standalone build of the game
#if UNITY_STANDALONE
        // Quit the application
        Application.Quit();
#endif

        // If we are running in the editor
#if UNITY_EDITOR
        // Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        // If we are running on mobile
#if UNITY_ANDROID || UNITY_IOS
        // Use native quit function
        Application.Quit();
#endif
    }

}
