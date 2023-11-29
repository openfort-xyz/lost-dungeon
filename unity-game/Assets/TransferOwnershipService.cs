using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Openfort;
using Openfort.Model;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
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
        AcceptingOwnership,
        RegisteringSession,
        SigningSession,
        SessionSigned,
        Web3AuthSuccessful,
        Disconnecting,
        Disconnected,
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

    private AccountResponse _currentAccount;
    
    private string _currentWalletAddress;
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
        _currentWalletAddress = await Web3GL.Instance.GetConnectedAddressAsync();
        _currentChainId = await Web3GL.Instance.GetChainIdAsync();
        #else
        _currentWalletAddress = _wcController.GetConnectedAddress();
        _currentChainId = _wcController.GetChainId();
        #endif

        Debug.Log("Address: " + _currentWalletAddress);
        Debug.Log("IntegerChainId: " + _currentChainId);

        if (string.IsNullOrEmpty(_currentWalletAddress) || _currentChainId == null)
        {
            Debug.Log("Wallet Address or ChainId null or empty.");
            Disconnect();
            return;
        }

        AzureFunctionCaller.ChallengeRequest(_currentWalletAddress, _currentChainId);
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
    
    private async void AcceptOwnership(string contractAddress, string newOwnerAddress)
    {
        ChangeState(State.AcceptingOwnership);
        string txHash = null;

        try
        {
#if UNITY_WEBGL
            txHash = await Web3GL.Instance.AcceptAccountOwnership(contractAddress, newOwnerAddress);
#else
            txHash = await _wcController.AcceptAccountOwnership(contractAddress, newOwnerAddress);
#endif
            if (string.IsNullOrEmpty(txHash))
            {
                Debug.LogError("txHash is null or empty.");
                Disconnect();
                return;
            }
        
            Debug.Log("Ownership accepted.");
            RegisterSession();
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Disconnect();
            throw;
        }
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
    private void OnDeployAccountSuccess(ExecuteFunctionResult result)
    {
        try
        {
            _currentAccount = JsonConvert.DeserializeObject<AccountResponse>(result.FunctionResult.ToString());
            
            RequestMessage();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
    }
    
    private async void OnChallengeRequestSuccess(string requestResponse)
    {
        ChangeState(State.SigningMessage);
        Debug.Log("ChallengeRequest success.");
        var response = JsonUtility.FromJson<ChallengeRequestResponse>(requestResponse);

        string signature = null;

        try
        {
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
        
            RequestTransferOwnership(_currentAccount.Id, response.address);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Disconnect();
            throw;
        }
    }
    
    private void OnTransferOwnershipSuccess(string requestResponse)
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
        
        AcceptOwnership(response.contractAddress, response.newOwnerAddress);
    }
    
    private void OnTransferOwnershipFailure(PlayFabError error)
    {
        var errorReport = error.GenerateErrorReport();
        Debug.Log(errorReport);
        
        // If function timeout
        if (errorReport.ToLower().Contains("10000ms"))
        {
            //TODO delay
            // TODO POOLING! Now we assume requestTransferOwnership was successful.
            if (string.IsNullOrEmpty(_currentWalletAddress) || _currentAccount == null)
            {
                Debug.LogError("Some required field is null or empty.");
                Disconnect();
                return;
            }
            
            AcceptOwnership(_currentAccount.Address, _currentWalletAddress);
        }
        else
        {
            Debug.LogError("Requesting transfer ownership failed.");
            Disconnect();
        }
    }

    private async void OnRegisterSessionSuccess(string response)
    {
        Debug.Log("RegisterSessionSuccess");
        ChangeState(State.SigningSession);

        var session = JsonConvert.DeserializeObject<SessionResponse>(response);
        var userOpHash = session.NextAction.Payload.UserOpHash;
        Debug.Log("USEROPHASH: " + userOpHash);

        string signature = null;

        try
        {
#if UNITY_WEBGL
            var address = await Web3GL.Instance.GetConnectedAddressAsync();
            signature = await Web3GL.Instance.Sign(userOpHash, address);
#else
            var address = _wcController.GetConnectedAddress();
            signature = await _wcController.Sign(userOpHash, address);
#endif

            ChangeState(State.SessionSigned);

            if (string.IsNullOrEmpty(signature))
            {
                Debug.Log("Signature failed.");
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

        AzureFunctionCaller.CompleteWeb3Auth();
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