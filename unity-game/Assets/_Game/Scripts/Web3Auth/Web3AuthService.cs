using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Openfort;
using Openfort.Model;
using PlayFab;
using PlayFab.ClientModels;

[DefaultExecutionOrder(100)] //VERY IMPORTANT FOR ANDROID BUILD --> OnEnable() method was called very early in script execution order therefore we weren't subscribing to events.
public class Web3AuthService : MonoBehaviour
{
    private WalletConnectorKit _walletConnectorKit;
    
    public enum State
    {
        None,
        WalletConnecting,
        WalletConnecting_Web3AuthCompleted,
        WalletConnectionCancelled,
        WalletConnected,
        RequestingMessage,
        SigningMessage,
        VerifyingSignature,
        RegisteringSession,
        SigningSession,
        SessionSigned,
        Web3AuthSuccessful,
        WrongOwnerAddress,
        Disconnecting,
        Disconnected,
        Disconnected_Web3AuthCompleted
    }
    
    public State currentState = State.None;
    
    [Serializable]
    public class ChallengeRequestResponse
    {
        public string address;
        public string chainId;
        public string message;
    }
    
    [Header("Events")]
    public UnityEvent<State> onStateChanged = new UnityEvent<State>();
    
    private string _currentAddress;
    private int? _currentChainId;
    
    private OpenfortClient _openfort;

    [HideInInspector] public bool authCompletedOnce;

    private bool _web3GLInitialized;

    #region UNITY_LIFECYCLE
    private void Awake() {
        _walletConnectorKit = FindObjectOfType<WalletConnectorKit>();
        if (_walletConnectorKit == null) {
            Debug.LogError("WalletConnectorKit not found. Web3AuthService needs it to function.");
            return;
        }

        // Subscribe to events
        _walletConnectorKit.OnConnected += WalletConnectorKit_OnConnected_Handler;
        _walletConnectorKit.OnDisconnected += WalletConnectorKit_OnDisconnected_Handler;
        _walletConnectorKit.OnConnectionError += WalletConnectorKit_OnConnectionError_Handler;
    }

    private void OnEnable()
    {
        // AzureFunctionCaller Events
        AzureFunctionCaller.onChallengeRequestSuccess += OnChallengeRequestSuccess;
        AzureFunctionCaller.onChallengeVerifySuccess += OnChallengeVerifySuccess;
        AzureFunctionCaller.onRegisterSessionSuccess += OnRegisterSessionSuccess;
        AzureFunctionCaller.onCompleteWeb3AuthSuccess += OnCompleteWeb3AuthSuccess;
        AzureFunctionCaller.onRegisterSessionFailure += OnRegisterSessionFailure;
        AzureFunctionCaller.onRequestFailure += OnAnyRequestFailure;
    }

    private void OnDisable()
    {
        _walletConnectorKit.OnConnected -= WalletConnectorKit_OnConnected_Handler;
        _walletConnectorKit.OnDisconnected -= WalletConnectorKit_OnDisconnected_Handler;
        _walletConnectorKit.OnConnectionError -= WalletConnectorKit_OnConnectionError_Handler;
        
        // AzureFunctionCaller Events
        AzureFunctionCaller.onChallengeRequestSuccess -= OnChallengeRequestSuccess;
        AzureFunctionCaller.onChallengeVerifySuccess -= OnChallengeVerifySuccess;
        AzureFunctionCaller.onRegisterSessionSuccess -= OnRegisterSessionSuccess;
        AzureFunctionCaller.onCompleteWeb3AuthSuccess -= OnCompleteWeb3AuthSuccess;
        AzureFunctionCaller.onRegisterSessionFailure -= OnRegisterSessionFailure;
        AzureFunctionCaller.onRequestFailure -= OnAnyRequestFailure;
    }

    private void Start()
    {
        _openfort = new OpenfortClient(OFStaticData.PublishableKey);
    }
    #endregion

    #region PUBLIC_METHODS
    public void Connect()
    {
        if (authCompletedOnce)
        {
            ChangeState(State.WalletConnecting_Web3AuthCompleted);
        }
        
        _walletConnectorKit.Connect();
    }
    #endregion

    #region WCK_EVENT_HANDLERS
    private async void WalletConnectorKit_OnConnected_Handler()
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET CONNECTED");
        if (authCompletedOnce)
        {
            bool correctAddress = await CheckIfCorrectAccount();
            if (correctAddress)
            {
                RequestMessage();
            }
            else
            {
                Disconnect();
            }
        }
        else
        {
            RequestMessage();
        }
    }
    
    private void WalletConnectorKit_OnDisconnected_Handler(string disconnectionReason)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET DISCONNECTED");
        Debug.Log(disconnectionReason);
        ChangeState(authCompletedOnce ? State.Disconnected_Web3AuthCompleted : State.Disconnected);
    }
    
    private void WalletConnectorKit_OnConnectionError_Handler(string error)
    {
        Debug.Log("WEB3AUTHSERVICE: CONNECTION ERROR");
        Debug.LogError(error);
        
        //TODO Disconnect????
        ChangeState(authCompletedOnce ? State.Disconnected_Web3AuthCompleted : State.Disconnected);
    }
    #endregion

    #region AZURE_FUNCTION_CALLER_EVENT_HANDLERS
    private async void OnChallengeRequestSuccess(string requestResponse)
    {
        ChangeState(State.SigningMessage);
        Debug.Log("ChallengeRequest success.");
        var response = JsonUtility.FromJson<ChallengeRequestResponse>(requestResponse);

        string signature;

        try
        {
            signature = await _walletConnectorKit.Sign(response.message, response.address);

            if (string.IsNullOrEmpty(signature))
            {
                Disconnect();
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Disconnect();
            throw;
        }
        
        if (authCompletedOnce)
        {
            RegisterSession();
        }
        else
        {
            AzureFunctionCaller.ChallengeVerify(response.message, signature);
            ChangeState(State.VerifyingSignature);
        }
    }

    private void OnChallengeVerifySuccess()
    {
        Debug.Log("ChallengeVerify success.");

        //////// Get OF Player ID
        // Create the request object
        GetUserDataRequest request = new GetUserDataRequest
        {
            Keys = new List<string> {OFStaticData.OFplayerKey, OFStaticData.OFownerAddressKey}
        };

        // Make the API call
        PlayFabClientAPI.GetUserReadOnlyData(request,
            result =>  // Inline success callback
            {
                if (result.Data == null || !result.Data.ContainsKey(OFStaticData.OFplayerKey) || !result.Data.ContainsKey(OFStaticData.OFownerAddressKey))
                {
                    Debug.LogError("OFplayer or address not found");
                    Disconnect();
                    return;
                }

                // Access the value of OFplayer
                string ofPlayer = result.Data[OFStaticData.OFplayerKey].Value;
                OFStaticData.OFplayerValue = ofPlayer;

                string ownerAddress = result.Data[OFStaticData.OFownerAddressKey].Value;
                OFStaticData.OFownerAddressValue = ownerAddress;

                RegisterSession();
            },
            error =>  // Inline failure callback
            {
                Debug.LogError("Failed to get OFplayer or address data: " + error.GenerateErrorReport());
                Disconnect();
            }
        );
    }

    private async void OnRegisterSessionSuccess(string response)
    {
        Debug.Log("RegisterSessionSuccess");
        ChangeState(State.SigningSession);

        var session = JsonConvert.DeserializeObject<SessionResponse>(response);
        var userOpHash = session.NextAction.Payload.UserOpHash;
        Debug.Log("USEROPHASH: " + userOpHash);

        string signature;

        try
        {
            _currentAddress = await _walletConnectorKit.GetConnectedAddress();
            signature = await _walletConnectorKit.Sign(userOpHash, _currentAddress);
        
            if (string.IsNullOrEmpty(signature))
            {
                Debug.Log("Signature failed.");
                Disconnect();
                return;
            }
            
            ChangeState(State.SessionSigned);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Disconnect();
            throw;
        }

        try
        {
            await _openfort.SendSignatureSessionRequest(session.Id, signature);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Disconnect();
            throw;
        }
        
        AzureFunctionCaller.CompleteWeb3Auth(_currentAddress);
    }

    private void OnRegisterSessionFailure(PlayFabError error)
    {
        var errorReport = error.GenerateErrorReport();
        
        // If function timeout
        if (errorReport.ToLower().Contains("10000ms"))
        {
            //TODO we should retry the registration of the sessionKey
            // Timeout means most probably succeeded.
            Debug.Log("RegisterSession timeout.");
            // Remove the session key if we have failed during registering a new session
            var sessionKey = _openfort.LoadSessionKey();
            if (sessionKey == null)
            {
                _openfort.RemoveSessionKey();
            }

            Disconnect();
        }
        else
        {
            Debug.Log("RegisterSession failed.");
            // Remove the session key if we have failed during registering a new session
            var sessionKey = _openfort.LoadSessionKey();
            if (sessionKey == null)
            {
                _openfort.RemoveSessionKey();
            }

            Disconnect();
        }
    }

    private void OnCompleteWeb3AuthSuccess(string result)
    {
        Debug.Log(result);
        ChangeState(State.Web3AuthSuccessful);
    }

    private void OnAnyRequestFailure()
    {
        // TODO Careful, almost all AzureFunctionCaller requests trigger this if failed.
        Debug.Log("Request failed.");
        Disconnect();
    }
    #endregion

    #region PRIVATE_METHODS
    private async void RequestMessage()
    {
        ChangeState(State.WalletConnected);
        
        _currentAddress = await _walletConnectorKit.GetConnectedAddress();
        _currentChainId = await _walletConnectorKit.GetChainId();

        Debug.Log("Address: " + _currentAddress);
        Debug.Log("IntegerChainId: " + _currentChainId);

        if (string.IsNullOrEmpty(_currentAddress) || _currentChainId == null)
        {
            Debug.Log("Wallet Address or ChainId null or empty.");
            return;
        }

        AzureFunctionCaller.ChallengeRequest(_currentAddress, _currentChainId);
        ChangeState(State.RequestingMessage);
    }

    private void RegisterSession()
    {
        ChangeState(State.RegisteringSession);

        // IMPORTANT Clear current session key if existent
        var loadedSessionKey = _openfort.LoadSessionKey();
        if (loadedSessionKey != null)
        {
            _openfort.RemoveSessionKey();
        }

        // To get public key use keyPair.PublicBase64 property
        var sessionKey = _openfort.CreateSessionKey();
        if (sessionKey == null)
        {
            Disconnect();
            return;
        }

        // In case of the previous step success save the key
        _openfort.SaveSessionKey();

        // Register session
        AzureFunctionCaller.RegisterSession(sessionKey.Address, OFStaticData.OFplayerValue); //OFplayer was saved during login
    }

    private async UniTask<bool> CheckIfCorrectAccount()
    {
        _currentAddress = await _walletConnectorKit.GetConnectedAddress();
        
        if (string.IsNullOrEmpty(_currentAddress))
        {
            Debug.LogError("current address is null or empty");
            return false;
        }

        if (string.IsNullOrEmpty(OFStaticData.OFownerAddressValue))
        {
            Debug.LogError("No OFplayerOwnerAddress in Static Data");
            return false;
        }

        if (_currentAddress.ToLower() != OFStaticData.OFownerAddressValue.ToLower())
        {
            Debug.LogError("You've connected with a wallet address that is not OFplayerOwnerAddress");
            return false;
        }

        return true;
    }

    public void Disconnect()
    {
        if (_walletConnectorKit.IsConnected())
        {
            ChangeState(State.Disconnecting);
            _walletConnectorKit.Disconnect();
        }
        else
        {
            ChangeState(State.Disconnected);
        }
    }
    #endregion

    #region STATE_MACHINE
    public void ChangeState(State newState)
    {
        currentState = newState;
        onStateChanged?.Invoke(currentState);
    }
    #endregion
}