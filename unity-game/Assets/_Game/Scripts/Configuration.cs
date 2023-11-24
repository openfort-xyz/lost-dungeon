using System;
using Cysharp.Threading.Tasks.Triggers;
using Openfort;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Internal;
using UnityEngine.SceneManagement;

public class Configuration : MonoBehaviour
{
    public TransferOwnershipService transferOwnershipService;
    
    [Header("UI")]
    public Button LogoutButton;
    public Button ConnectWalletButton;
    public Text statusTextLabel;

    private PlayFabAuthService _AuthService = PlayFabAuthService.Instance;
    
    private OpenfortClient _openfortClient;

    public void Start()
    {
        // Get Openfort client with publishable key.
        _openfortClient = new OpenfortClient(OFStaticData.PublishableKey);
    }

    private void OnEnable()
    {
        //TODO Check if custodial or not --> appear connect button
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
            case TransferOwnershipService.State.RequestingMessage:
                statusTextLabel.text = "Requesting message...";
                break;
            case TransferOwnershipService.State.SigningMessage:
                statusTextLabel.text = "Please sign the message in your wallet.";
                break;
            case TransferOwnershipService.State.RequestingOwnershipTransfer:
                statusTextLabel.text = "Requesting ownership transfer...";
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
                Invoke(nameof(CloseOnSelfCustodyEnabled), 1f);
                break;
            case TransferOwnershipService.State.Disconnecting:
                statusTextLabel.text = "Disconnecting...";
                break;
            case TransferOwnershipService.State.Disconnected:
                EnableButtons(true);
                statusTextLabel.text = "Wallet disconnected. Please try again.";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }
    }
    
    public void OnLogoutClicked()
    {
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

        // Logout from Web3
        transferOwnershipService.Disconnect();
        
        // Remove openfort session key
        var sessionKey = _openfortClient.LoadSessionKey();
        if (sessionKey == null)
        {
            _openfortClient.RemoveSessionKey();
        }

        // Navigate back to the login scene
        SceneManager.LoadScene("Login");
    }

    private void CloseOnSelfCustodyEnabled()
    {
        //TODO maybe show eoa address
        EnableButtons(true);
        gameObject.SetActive(false);
    }
    
    private void EnableButtons(bool status)
    {
        ConnectWalletButton.gameObject.SetActive(status);
        LogoutButton.gameObject.SetActive(status);
    }
}