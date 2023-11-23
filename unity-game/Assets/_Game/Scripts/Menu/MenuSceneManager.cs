using System;
using Openfort;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;

public class MenuSceneManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject MenuPanel;
    public GameObject LeaderboardPanel;
    public GameObject ConfigurationPanel;

    public Text notifyText;
    public Text username;

    private string _currentPlayerAddress;

    private void Start()
    {
        getTitleData();
        GetUserData();
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
        //TODO check if custodial! --> enable button
        if (result.Data == null || !result.Data.ContainsKey(OFStaticData.OFaddressKey))
        {
            Debug.Log("PlayFab user has no wallet address linked");
            username.text = "No Web3 Wallet connected";
        }
        else
        {
            username.text = "Click here to checkout out your account: " + result.Data[OFStaticData.OFaddressKey].Value;
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
    
    public void OnQuitClicked()
    {
        Application.Quit();
    }
}
