using System;
using System.Collections.Generic;
using Openfort;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Configuration : MonoBehaviour
{
    public event UnityAction OnLoggedOut;
    public UnityEvent closed;
    
    public TransferOwnershipService transferOwnershipService;
    
    [Header("UI")]
    public Button logoutButton;
    public Button registerButton;
    public Button selfCustodyButton;
    public Button recoveryButton;
    public Button backButton;
    
    public Text statusTextLabel;

    private PlayFabAuthService _AuthService = PlayFabAuthService.Instance;
    
    private OpenfortSDK _openfort;
    private bool _loggingOut = false;

    [HideInInspector] public string guestCustomId;

    public void Start()
    {
        // Get Openfort client with publishable key.
        _openfort = new OpenfortSDK(OFStaticData.PublishableKey);
    }

    private void OnEnable()
    {
        GetPlayFabAccountInfo();
    }

    public void TransferOwnershipService_OnStateChanged_Handler(TransferOwnershipService.State currentState)
    {
        switch (currentState)
        {
            case TransferOwnershipService.State.None:
                break;
            case TransferOwnershipService.State.WalletConnecting:
                //EnableButtons(false);
                statusTextLabel.text = "Connecting...";
                break;
            case TransferOwnershipService.State.WalletConnectionCancelled:
                EnableButtons(true);
                statusTextLabel.text = "Wallet connection cancelled.";
                break;
            case TransferOwnershipService.State.WalletConnected:
                EnableButtons(false);
                statusTextLabel.text = "Wallet connection successful.";
                break;
            case TransferOwnershipService.State.DeployingAccount:
                EnableButtons(false);
                statusTextLabel.text = "Deploying account...";
                break;
            case TransferOwnershipService.State.RequestingMessage:
                statusTextLabel.text = "Requesting message...";
                break;
            case TransferOwnershipService.State.SigningMessage:
                statusTextLabel.text = "Please sign the message in your wallet.";
                break;
            case TransferOwnershipService.State.RequestingOwnershipTransfer:
                statusTextLabel.text = "Requesting ownership transfer...";
                break;
            case TransferOwnershipService.State.AcceptingOwnership:
                statusTextLabel.text = "Accepting ownership transfer...";
                break;
            case TransferOwnershipService.State.RegisteringSession:
                statusTextLabel.text = "Registering openfort session...";
                break;
            case TransferOwnershipService.State.SigningSession:
                statusTextLabel.text = "Please sign the message in your wallet.";
                break;
            case TransferOwnershipService.State.SessionSigned:
                statusTextLabel.text = "Session signed successfully. completing process...";
                break;
            case TransferOwnershipService.State.Web3AuthSuccessful:
                if (string.IsNullOrEmpty(OFStaticData.OFplayerValue))
                {
                    Debug.LogError("No OFplayer in StaticData");
                    statusTextLabel.text = "Error: no OFplayer in StaticData";
                    return;
                }
                PlayerPrefs.SetString(PPStaticData.LastPlayerKey, OFStaticData.OFplayerValue);
                statusTextLabel.text = "Self-custody ENABLED!";
                Invoke(nameof(ClosePanel), 1f);
                break;
            case TransferOwnershipService.State.Disconnecting:
                EnableButtons(false);
                statusTextLabel.text = "Disconnecting...";
                break;
            case TransferOwnershipService.State.Disconnected:
                if (_loggingOut)
                {
                    _loggingOut = false;
                    OnLoggedOut?.Invoke();
                    return;
                }
                
                EnableButtons(true);
                statusTextLabel.text = "Wallet disconnected. Please try again.";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }
    }
    
    public void OnLogoutClicked()
    {
        _loggingOut = true;
        
        // Clear "RememberMe" stored PlayerPrefs (Ideally just the ones related to login, but here we clear all)
        PlayerPrefs.DeleteKey(PPStaticData.RememberMeKey);
        PlayerPrefs.DeleteKey(PPStaticData.CustomIdKey);
        PlayerPrefs.DeleteKey(PPStaticData.AppleSubjectIdKey);
        PlayerPrefs.DeleteKey(PPStaticData.GooglePlayGamesPlayerIdKey);
        PlayerPrefs.DeleteKey(PPStaticData.LastPlayerKey);

        // Clear all locally saved data related to the PlayFab session
        PlayFabClientAPI.ForgetAllCredentials();
        
        _AuthService.ClearRememberMe();
        _AuthService.Email = string.Empty;
        _AuthService.Password = string.Empty;
        _AuthService.AuthTicket = string.Empty;
        
        //TODO-EMB
        // We removed session key here. Should we do anything related to session keys?
        
        // Logout from Web3
        transferOwnershipService.Disconnect();
    }

    private void ClosePanel()
    {
        //TODO maybe show eoa address
        EnableButtons(true);
        closed?.Invoke();
    }
    
    private void EnableButtons(bool status)
    {
        selfCustodyButton.gameObject.SetActive(status);
        logoutButton.gameObject.SetActive(status);
        backButton.gameObject.SetActive(status);
    }
    
    public void GetPlayFabAccountInfo()
    {
        registerButton.gameObject.SetActive(false);
        selfCustodyButton.gameObject.SetActive(false);
        
        var request = new GetAccountInfoRequest();
        
        PlayFabClientAPI.GetAccountInfo(request, result =>
            {
                if (result.AccountInfo == null)
                {
                    Debug.Log("AccountInfo not found.");   
                    return;
                }

                if (result.AccountInfo.PrivateInfo.Email != null ||
                    result.AccountInfo.AppleAccountInfo != null ||
                    result.AccountInfo.GooglePlayGamesInfo != null)
                {
                    Debug.Log("Player is registered.");
                    
                    // IMPORTANT --> Check if player has a custodial account linked or it's self custodial.
                    PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest()
                        {
                            Keys = new List<string>() { "custodial" }
                        },
                        userDataResult => 
                        {
                            Debug.Log("Get user data successful");
                            if (userDataResult.Data.ContainsKey("custodial"))
                            {
                                Debug.Log("Player is custodial.");
                                selfCustodyButton.gameObject.SetActive(true);
                            }
                            else
                            {
                                Debug.Log("Player is self-custodial.");
                                recoveryButton.gameObject.SetActive(true);
                            }
                        },
                        error => 
                        {
                            Debug.Log("Got error getting user data:");
                            Debug.Log(error.GenerateErrorReport());
                        });
                }
                else
                {
                    Debug.Log("Guest?");

                    try
                    {
                        var customId = result.AccountInfo.CustomIdInfo.CustomId;
                        
                        //TODO Atm Google also has a custom ID! CHANGE
                        Debug.Log("Player is guest.");
                        guestCustomId = customId;
                        registerButton.gameObject.SetActive(true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Debug.Log("No CustomID found.");
                        throw;
                    }
                }
            },
            error => 
            {
                Debug.LogError("Error getting account info: " + error.ErrorMessage);
            });
    }
}