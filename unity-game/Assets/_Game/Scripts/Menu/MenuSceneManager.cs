using System;
using MetaMask.Unity;
using Openfort;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;

public class MenuSceneManager : MonoBehaviour
{
    public GameObject MenuPanel;
    public GameObject LeaderboardPanel;
    public GameObject ConfigurationPanel;

    public Text notifyText;
    public Text username;

    private string _currentPlayerAddress;
    
    private OpenfortClient _openfortClient;

    private void Start()
    {
        // Get Openfort client with publishable key.
        _openfortClient = new OpenfortClient(OpenfortStaticData.publishableKey);
        
        getTitleData();
        GetUserData();
    }

    private void OnEnable()
    {
        AzureFunctionCaller.onLogoutSuccess += OnLogoutSuccess;
        AzureFunctionCaller.onRequestFailure += OnLogoutFailure;
    }

    private void OnDisable()
    {
        AzureFunctionCaller.onLogoutSuccess -= OnLogoutSuccess;
        AzureFunctionCaller.onRequestFailure -= OnLogoutFailure;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCloseLeaderboard();
        }
        
        if (Application.platform != RuntimePlatform.WindowsPlayer &&
            Application.platform != RuntimePlatform.OSXPlayer &&
            Application.platform != RuntimePlatform.LinuxPlayer) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Application.Quit();
        }
    }
    
    void getTitleData()
    {
        var request = new GetTitleDataRequest();
        PlayFabClientAPI.GetTitleData(request, OnGetTitleDataSuccess, OnGetTitleDataFailure);
    }
    
    void OnGetTitleDataSuccess(GetTitleDataResult result)
    {
        if (result.Data == null || result.Data.ContainsKey("Message") == false)
        {
            Debug.Log("No message found");
            return;
        }
        
        notifyText.text = result.Data["Message"];
    }
    
    void OnGetTitleDataFailure(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    void GetUserData()
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserReadOnlyData(request, OnGetUserDataSuccess, OnGetUserDataFailure);
    }

    private void OnGetUserDataFailure(PlayFabError error)
    {
        username.text = error.ToString();
    }

    private void OnGetUserDataSuccess(GetUserDataResult result)
    {
        if (result.Data == null || !result.Data.ContainsKey("address"))
        {
            Debug.Log("PlayFab user has no wallet address linked");
            username.text = "No Web3 Wallet connected";
        }
        else
        {
            username.text = "Click here to checkout out your account: " + result.Data["address"].Value;
        }
    }

    public void OnPlayClicked()
    {
        // We go to Lobby scene
        SceneManager.LoadScene("Lobby");
    }

    public void OnLeaderboardClicked()
    {
        LeaderboardPanel.SetActive(true);
        MenuPanel.SetActive(false);
        ConfigurationPanel.SetActive(false);
    }
    public void OnCloseLeaderboard()
    {
        LeaderboardPanel.SetActive(false);
        ConfigurationPanel.SetActive(false);
        MenuPanel.SetActive(true);
    }

    public void OnCloseConfiguration()
    {
        ConfigurationPanel.SetActive(false);
        LeaderboardPanel.SetActive(false);
        MenuPanel.SetActive(true);
    }

    public void OnConfigurationClicked()
    {
        MenuPanel.SetActive(false);
        LeaderboardPanel.SetActive(false);
        ConfigurationPanel.SetActive(true);
    }

    public void OnLogoutClicked()
    {
        // This is removing the sessionId from user data in case the user is self-custody. If it's not, removing it won't do anything :)
        AzureFunctionCaller.Logout();
    }
    
    private void OnLogoutSuccess()
    {
        // Clear "RememberMe" stored PlayerPrefs (Ideally just the ones related to login, but here we clear all)
        PlayerPrefs.DeleteKey("RememberMe");
        PlayerPrefs.DeleteKey("CustomID");
        PlayerPrefs.DeleteKey("LastPlayer");

        // Clear all locally saved data related to the PlayFab session
        PlayFabClientAPI.ForgetAllCredentials();
        
        // Logout from Web3
        //TODO Disconnect if WEBGL?
#if !UNITY_WEBGL
        MetaMaskUnity.Instance.Disconnect(true);
#endif
        
        // Remove openfort session key
        var sessionKey = _openfortClient.LoadSessionKey();
        if (sessionKey == null)
        {
            _openfortClient.RemoveSessionKey();
        }

        // Navigate back to the login scene
        SceneManager.LoadScene("Login");
    }
    
    private void OnLogoutFailure()
    {
        Debug.Log("Logout call failed.");
    }
}
