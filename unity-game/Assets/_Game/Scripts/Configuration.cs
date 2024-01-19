using System;
using Openfort;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.Events;

public class Configuration : MonoBehaviour
{
    public event UnityAction OnLoggedOut;
    public UnityEvent closed;
    
    public TransferOwnershipService transferOwnershipService;
    
    [Header("UI")]
    public Button logoutButton;
    public Button registerButton;
    public Button selfCustodyButton;
    public Button backButton;
    
    public Text statusTextLabel;

    private PlayFabAuthService _AuthService = PlayFabAuthService.Instance;
    
    private OpenfortClient _openfortClient;
    private bool _loggingOut = false;

    [HideInInspector] public string guestCustomId;

    public void Start()
    {
        // Get Openfort client with publishable key.
        _openfortClient = new OpenfortClient(OFStaticData.PublishableKey);
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
                EnableButtons(false);
                statusTextLabel.text = "Connecting...";
                break;
            case TransferOwnershipService.State.WalletConnectionCancelled:
                EnableButtons(true);
                statusTextLabel.text = "Wallet connection cancelled.";
                break;
            case TransferOwnershipService.State.WalletConnected:
                statusTextLabel.text = "Wallet connection successful.";
                break;
            case TransferOwnershipService.State.DeployingAccount:
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
                EnableButtons(true);
                statusTextLabel.text = "Wallet disconnected. Please try again.";
                
                if (_loggingOut)
                {
                    _loggingOut = false;
                    OnLoggedOut?.Invoke();
                }
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
        PlayerPrefs.DeleteKey(PPStaticData.LastPlayerKey);

        // Clear all locally saved data related to the PlayFab session
        PlayFabClientAPI.ForgetAllCredentials();
        
        _AuthService.ClearRememberMe();
        _AuthService.Email = string.Empty;
        _AuthService.Password = string.Empty;
        _AuthService.AuthTicket = string.Empty;
        
        // Remove openfort session key
        var sessionKey = _openfortClient.LoadSessionKey();
        if (sessionKey == null)
        {
            _openfortClient.RemoveSessionKey();
        }
        
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

                if (string.IsNullOrEmpty(result.AccountInfo.PrivateInfo.Email))
                {
                    Debug.Log("Guest?");
                    var customId = result.AccountInfo.CustomIdInfo.CustomId;
                    
                    if (string.IsNullOrEmpty(customId))
                    {
                        Debug.Log("No CustomID found.");
                        return;
                    }

                    Debug.Log("Player is guest.");
                    guestCustomId = customId;
                    registerButton.gameObject.SetActive(true);
                }
                else
                {
                    Debug.Log("Player is registered.");
                    selfCustodyButton.gameObject.SetActive(true);
                }
            },
            error => 
            {
                Debug.LogError("Error getting account info: " + error.ErrorMessage);
            });
    }
}