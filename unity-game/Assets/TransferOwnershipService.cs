using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Openfort;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.Serialization;
using WalletConnect;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;

public class TransferOwnershipService : MonoBehaviour
{
    [Serializable]
    public class TransferOwnershipResponse
    {
        public string contractAddress;
        public string newOwnerAddress;
    }
    
    private WalletConnectController _wcController;
    
    public enum State
    {
        None,
        WalletConnecting,
        WalletConnectionCancelled,
        WalletConnected,
        DeployingAccount,
        RequestingMessage,
        SigningMessage,
        RequestingOwnershipTransfer,
        RegisteringSession,
        SigningSession,
        SessionSigned,
        Web3AuthSuccessful,
        Disconnecting,
        Disconnected,
    }
    
    public State currentState = State.None;
    
    public class Transaction
    {
        public string id;
        public string userOpHash;
    }
    
    [Serializable]
    public class ChallengeRequestResponse
    {
        public string address;
        public string chainId;
        public string message;
    }
    
    [Header("Events")]
    public UnityEvent<State> onStateChanged = new UnityEvent<State>();

    private string _accountId;
    private string _currentAddress;
    private int? _currentChainId;
    
    private OpenfortClient _openfort;
    
    #region UNITY_LIFECYCLE

    private void Awake()
    {
        _wcController = FindObjectOfType<WalletConnectController>();
        if (_wcController == null)
        {
            Debug.LogError("WalletConnectController not found. Web3AuthService needs it to function.");
        }
    }

    private void OnEnable()
    {
        // WC Events
        _wcController.OnConnected += WcController_OnConnected_Handler;
        _wcController.OnDisconnected += WcController_OnDisconnected_Handler;

#if UNITY_WEBGL
        // Web3GL Events
        Web3GL.OnWeb3ConnectedEvent += OnWeb3GLConnected;
        Web3GL.OnWeb3ConnectErrorEvent += OnWeb3GLConnectionFailure;  
        Web3GL.OnWeb3DisconnectedEvent += OnWeb3GLDisconnected;
#endif

        // AzureFunctionCaller Events
        AzureFunctionCaller.onChallengeRequestSuccess += OnChallengeRequestSuccess;
        AzureFunctionCaller.onDeployAccountSuccess += OnDeployAccountSuccess;
        AzureFunctionCaller.onRequestTransferOwnershipSuccess += OnTransferOwnershipSuccess;
        AzureFunctionCaller.onRegisterSessionSuccess += OnRegisterSessionSuccess;
        AzureFunctionCaller.onCompleteWeb3AuthSuccess += OnCompleteWeb3AuthSuccess;
        
        AzureFunctionCaller.onRegisterSessionFailure += OnRegisterSessionFailure;
        AzureFunctionCaller.onRequestTransferOwnershipFailure += OnTransferOwnershipFailure;
        AzureFunctionCaller.onRequestFailure += OnAnyRequestFailure;
    }

    private void OnDisable()
    {
        // WC Events
        _wcController.OnConnected -= WcController_OnConnected_Handler;
        _wcController.OnDisconnected -= WcController_OnDisconnected_Handler;
        
#if UNITY_WEBGL
        // Web3GL Events
        Web3GL.OnWeb3ConnectedEvent -= OnWeb3GLConnected;
        Web3GL.OnWeb3ConnectErrorEvent -= OnWeb3GLConnectionFailure;
        Web3GL.OnWeb3DisconnectedEvent -= OnWeb3GLDisconnected;
#endif

        // AzureFunctionCaller Events
        AzureFunctionCaller.onChallengeRequestSuccess -= OnChallengeRequestSuccess;
        AzureFunctionCaller.onDeployAccountSuccess -= OnDeployAccountSuccess;
        AzureFunctionCaller.onRequestTransferOwnershipSuccess -= OnTransferOwnershipSuccess;
        AzureFunctionCaller.onRegisterSessionSuccess -= OnRegisterSessionSuccess;
        AzureFunctionCaller.onCompleteWeb3AuthSuccess -= OnCompleteWeb3AuthSuccess;
        
        AzureFunctionCaller.onRegisterSessionFailure -= OnRegisterSessionFailure;
        AzureFunctionCaller.onRequestTransferOwnershipFailure -= OnTransferOwnershipFailure;
        AzureFunctionCaller.onRequestFailure -= OnAnyRequestFailure;
    }

    private void Start()
    {
        _openfort = new OpenfortClient(OFStaticData.PublishableKey);
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    #endregion

    #region PUBLIC_METHODS
    public void Connect()
    {
        ChangeState(State.WalletConnecting);

        #if UNITY_WEBGL
        Web3GL.Instance.Connect();
        #else
        _wcController.Connect();
        #endif
    }
    #endregion

    #region WC_EVENT_HANDLERS
    private async void WcController_OnConnected_Handler(SessionStruct session)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET CONNECTED");
        DeployAccount();
    }
    
    private void WcController_OnDisconnected_Handler()
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET DISCONNECTED");
        ChangeState(State.Disconnected);
    }
    #endregion

    #region WEB3GL_WALLET_EVENTS
    private async void OnWeb3GLConnected(string obj)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET CONNECTED");
        DeployAccount();
    }

    private void OnWeb3GLConnectionFailure(string obj)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET UNAUTHORIZED");
        Disconnect();
    }

    private void OnWeb3GLDisconnected(string obj)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET DISCONNECTED");
        ChangeState(State.Disconnected);
    }
    #endregion

    #region PRIVATE_METHODS
    private void DeployAccount()
    {
        // We need to make sure the account is deployed
        ChangeState(State.DeployingAccount);
        
        if (string.IsNullOrEmpty(OFStaticData.OFplayerValue))
        {
            Debug.LogError("OFplayerValue is null or empty. At this point, an Openfort Player should have been created.");
            Disconnect();
            return;
        }
        
        AzureFunctionCaller.DeployAccount(OFStaticData.OFplayerValue);
    }
    
    private async void RequestMessage()
    {
        ChangeState(State.WalletConnected);

        #if UNITY_WEBGL
        _currentAddress = await Web3GL.Instance.GetConnectedAddressAsync();
        _currentChainId = await Web3GL.Instance.GetChainIdAsync();
        #else
        _currentAddress = _wcController.GetConnectedAddress();
        _currentChainId = _wcController.GetChainId();
        #endif

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
    
    private void RequestTransferOwnership(string accountId, string newOwnerAddress)
    {
        ChangeState(State.RequestingOwnershipTransfer);
        
        if (string.IsNullOrEmpty(accountId))
        {
            Debug.LogError("Account ID is null or empty.");
            Disconnect();
            return;
        }
        
        AzureFunctionCaller.RequestTransferOwnership(accountId, newOwnerAddress);
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

    public void Disconnect()
    {
        ChangeState(State.Disconnecting);

        #if !UNITY_WEBGL
        _wcController.Disconnect();
        #else
        Web3GL.Instance.Disconnect();
        #endif
    }
    #endregion
    
    #region AZURE_FUNCTION_CALLER_EVENT_HANDLERS
    private void OnDeployAccountSuccess(string accountId)
    {
        _accountId = accountId;
        RequestMessage();
    }
    
    private async void OnChallengeRequestSuccess(string requestResponse)
    {
        ChangeState(State.SigningMessage);
        Debug.Log("ChallengeRequest success.");
        var response = JsonUtility.FromJson<ChallengeRequestResponse>(requestResponse);

        string signature = null;

        #if UNITY_WEBGL
        signature = await Web3GL.Instance.Sign(response.message, response.address);
        #else
        signature = await _wcController.Sign(response.message, response.address);
        #endif

        if (string.IsNullOrEmpty(signature))
        {
            Disconnect();
            return;
        }
        
        RequestTransferOwnership(_accountId, response.address);
    }
    
    private async void OnTransferOwnershipSuccess(string requestResponse)
    {
        Debug.Log("Received transfer ownership response.");
        
        var response = JsonUtility.FromJson<TransferOwnershipResponse>(requestResponse);

        // Check if deserialization was successful
        if (response == null)
        {
            Debug.Log("Failed to parse JSON");
            Disconnect();
            return;
        }
        
#if UNITY_WEBGL
        var address = await Web3GL.Instance.GetConnectedAddressAsync();
        signature = await Web3GL.Instance.Sign(tx.userOpHash, address);
#else
        //var address = _wcController.GetConnectedAddress();
        var txHash = await _wcController.AcceptAccountOwnership(response.contractAddress, response.newOwnerAddress);

        Debug.Log(txHash);

        var signature = await _wcController.Sign(txHash, response.newOwnerAddress);
        
        Debug.Log(signature);
#endif
    }
    
    private void OnTransferOwnershipFailure(PlayFabError error)
    {
        var errorReport = error.GenerateErrorReport();
        
        // If function timeout
        if (errorReport.ToLower().Contains("10000ms"))
        {
            //TODO 
        }
        else
        {
            Debug.LogError("Requesting transfer ownership failed.");
            Disconnect();
        }
    }

    private async void OnRegisterSessionSuccess(string txString)
    {
        Debug.Log("RegisterSessionSuccess");
        ChangeState(State.SigningSession);

        var tx = JsonUtility.FromJson<Transaction>(txString);

        Debug.Log("USEROPHASH: " + tx.userOpHash);

        string signature = null;

        #if UNITY_WEBGL
        var address = await Web3GL.Instance.GetConnectedAddressAsync();
        signature = await Web3GL.Instance.Sign(tx.userOpHash, address);
        #else
        var address = _wcController.GetConnectedAddress();
        signature = await _wcController.Sign(tx.userOpHash, address);
        #endif

        if (string.IsNullOrEmpty(signature))
        {
            Debug.Log("Signature failed.");
            Disconnect();
            return;
        }

        ChangeState(State.SessionSigned);

        try
        {
            await _openfort.SendSignatureSessionRequest(tx.id, signature);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Disconnect();
            throw;
        }

        AzureFunctionCaller.CompleteWeb3Auth(tx.id);
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

    #region STATE_MACHINE
    public void ChangeState(State newState)
    {
        currentState = newState;
        onStateChanged?.Invoke(currentState);
    }
    #endregion
}