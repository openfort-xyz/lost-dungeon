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
using PlayFab.CloudScriptModels;

public class TransferOwnershipService : MonoBehaviour
{
    private WalletConnectorKit _walletConnectorKit;
    
    [Serializable]
    public class TransferOwnershipResponse
    {
        public string contractAddress;
        public string newOwnerAddress;
    }
    
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
        _walletConnectorKit.OnConnected -= WalletConnectorKit_OnConnected_Handler;
        _walletConnectorKit.OnDisconnected -= WalletConnectorKit_OnDisconnected_Handler;
        _walletConnectorKit.OnConnectionError -= WalletConnectorKit_OnConnectionError_Handler;
        
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

        _walletConnectorKit.Connect();
    }
    #endregion

    #region WC_EVENT_HANDLERS
    private void WalletConnectorKit_OnConnected_Handler()
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET CONNECTED");
        DeployAccount();
    }
    
    private void WalletConnectorKit_OnDisconnected_Handler(string disconnectionReason)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET DISCONNECTED");
        Debug.LogWarning(disconnectionReason);
        ChangeState(State.Disconnected);
    }
    
    private void WalletConnectorKit_OnConnectionError_Handler(string error)
    {
        Debug.Log("WEB3AUTHSERVICE: CONNECTION ERROR");
        Debug.LogError(error);
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
        
        _currentWalletAddress = await _walletConnectorKit.GetConnectedAddress();
        _currentChainId = await _walletConnectorKit.GetChainId();

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
    
    private async UniTaskVoid AcceptOwnership(string contractAddress, string newOwnerAddress)
    {
        ChangeState(State.AcceptingOwnership);

        try
        {
            var txHash = await _walletConnectorKit.AcceptOwnership(contractAddress, newOwnerAddress);

            if (string.IsNullOrEmpty(txHash))
            {
                Debug.LogError("txHash is null or empty.");
                Disconnect();
                return;
            }
    
            Debug.Log("Ownership accepted.");

            // Wait for 5 seconds using UniTask
            await UniTask.Delay(4000);

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

        try
        {
            var signature = await _walletConnectorKit.Sign(response.message, response.address);

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

        string signature;

        try
        {
            _currentWalletAddress = await _walletConnectorKit.GetConnectedAddress();
            signature = await _walletConnectorKit.Sign(userOpHash, _currentWalletAddress);

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

        AzureFunctionCaller.CompleteWeb3Auth(_currentWalletAddress);
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