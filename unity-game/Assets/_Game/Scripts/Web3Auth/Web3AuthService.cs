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
using UnityEngine.Serialization;
using WalletConnect;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;

[DefaultExecutionOrder(100)] //VERY IMPORTANT FOR ANDROID BUILD --> OnEnable() method was called very early in script execution order therefore we weren't subscribing to events.
public class Web3AuthService : MonoBehaviour
{
    private WalletConnectController _wcController;
    
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
        // Web3GL 
        Web3GL.OnWeb3InitializedEvent += OnWeb3GLInitialized;
        Web3GL.OnWeb3InitializeErrorEvent += OnWeb3GLInitializationFailure;
        Web3GL.OnWeb3ConnectedEvent += OnWeb3GLConnected;
        Web3GL.OnWeb3ConnectErrorEvent += OnWeb3GLConnectionFailure;  
        Web3GL.OnWeb3DisconnectedEvent += OnWeb3GLDisconnected;
#endif

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
        // WC Events
        _wcController.OnConnected -= WcController_OnConnected_Handler;
        _wcController.OnDisconnected -= WcController_OnDisconnected_Handler;
        
#if UNITY_WEBGL
        // Web3GL Events
        Web3GL.OnWeb3InitializedEvent -= OnWeb3GLInitialized;
        Web3GL.OnWeb3InitializeErrorEvent -= OnWeb3GLInitializationFailure;
        Web3GL.OnWeb3ConnectedEvent -= OnWeb3GLConnected;
        Web3GL.OnWeb3ConnectErrorEvent -= OnWeb3GLConnectionFailure;
        Web3GL.OnWeb3DisconnectedEvent -= OnWeb3GLDisconnected;
#endif

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
        
        #if UNITY_WEBGL
        Web3GL.Instance.Initialize();
        #endif
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    #endregion

    #region PUBLIC_METHODS
    public void Connect()
    {
        ChangeState(authCompletedOnce ? State.WalletConnecting_Web3AuthCompleted : State.WalletConnecting);

        #if UNITY_WEBGL
        if (_web3GLInitialized)
        {
            Web3GL.Instance.Connect();   
        }
        else
        {
            _wcController.Connect();
        }
        #else
        _wcController.Connect();
        #endif
    }
    #endregion

    #region WC_EVENT_HANDLERS
    private async void WcController_OnConnected_Handler(SessionStruct session)
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
    
    private void WcController_OnDisconnected_Handler()
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET DISCONNECTED");
        ChangeState(authCompletedOnce ? State.Disconnected_Web3AuthCompleted : State.Disconnected);
    }
    #endregion

    #region WEB3GL_WALLET_EVENTS
    private void OnWeb3GLInitialized()
    {
        // This means MetaMask or other injected ethereum wallets are installed on the browser.
        Debug.Log("Web3GL initialized.");
        _web3GLInitialized = true;
    }
    
    private void OnWeb3GLInitializationFailure(string error)
    {
        Debug.LogError(error);
        _web3GLInitialized = false;
        //TODO switch to WC connection!
    }
    
    private async void OnWeb3GLConnected(string obj)
    {
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

    private void OnWeb3GLConnectionFailure(string obj)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET UNAUTHORIZED");
        Disconnect();
    }

    private void OnWeb3GLDisconnected(string obj)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET DISCONNECTED");
        ChangeState(authCompletedOnce ? State.Disconnected_Web3AuthCompleted : State.Disconnected);
    }
    #endregion

    #region AZURE_FUNCTION_CALLER_EVENT_HANDLERS
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

    #region PRIVATE_METHODS
    //TODO UniTask void?
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
        //TODO??
        #if UNITY_WEBGL
        _currentAddress = await Web3GL.Instance.GetConnectedAddressAsync();
        #else
        _currentAddress = _wcController.GetConnectedAddress(); 
        #endif

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
        ChangeState(State.Disconnecting);

        #if !UNITY_WEBGL
        _wcController.Disconnect();
        #else
        Web3GL.Instance.Disconnect();
        #endif
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