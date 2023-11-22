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

[DefaultExecutionOrder(100)] //VERY IMPORTANT FOR ANDROID BUILD --> OnEnable() method was called very early in script execution order therefore we weren't subscribing to events.
public class Web3AuthService : MonoBehaviour
{
    [Header("Wallet Connectors")] [SerializeField]
    private WalletConnectController wcController;
    
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
    
    private string _currentAddress;
    private int? _currentChainId;
    
    private OpenfortClient _openfort;

    [HideInInspector] public bool authCompletedOnce;

    #region UNITY_LIFECYCLE
    private void OnEnable()
    {
        wcController.OnConnected += WcController_OnConnected_Handler;
        /*TODOMETAMASK
        //TODO we could handle this managing MetaMask initialization manually.
        // Get MetaMask UI Handler
        _metaMaskUIHandler = FindObjectOfType<MetaMaskUnityUIHandler>();
        _metaMaskUIHandler.onCancelClicked += OnWalletUnauthorized;
        
        MetaMaskUnity.Instance.Wallet.Events.WalletUnauthorized += OnWalletUnauthorized;
        MetaMaskUnity.Instance.Wallet.Events.WalletDisconnected += OnWalletDisconnected;
        MetaMaskUnity.Instance.Events.EthereumRequestFailed += EventsOnEthereumRequestFailed;
        
        */

#if UNITY_WEBGL
        // Web3GL Events
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
        wcController.OnConnected -= WcController_OnConnected_Handler;
        /*TODOMETAMASK
        _metaMaskUIHandler.onCancelClicked -= OnWalletUnauthorized;
        
        MetaMaskUnity.Instance.Wallet.Events.WalletUnauthorized -= OnWalletUnauthorized;
        MetaMaskUnity.Instance.Wallet.Events.WalletDisconnected -= OnWalletDisconnected;
        MetaMaskUnity.Instance.Events.EthereumRequestFailed -= EventsOnEthereumRequestFailed;
        */
        
#if UNITY_WEBGL
        // Web3GL Events
        Web3GL.OnWeb3ConnectedEvent -= OnWeb3GLConnected;
        Web3GL.OnWeb3ConnectErrorEvent -= OnWeb3GLConnectionFailure;  
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
        Web3GL.Instance.Connect();
        #else
        wcController.Connect();
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

    //TODOMETAMASK
    private void OnWalletUnauthorized(object sender, EventArgs e)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET UNAUTHORIZED");
        //TODOMETAMASK _metaMaskUIHandler.CloseQRCode();
        Disconnect();
    }

    private void OnWalletDisconnected(object sender, EventArgs e)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET DISCONNECTED");
        ChangeState(authCompletedOnce ? State.Disconnected_Web3AuthCompleted : State.Disconnected);
    }

    /*TODOMETAMASK
    private void EventsOnEthereumRequestFailed(object sender, MetaMaskEthereumRequestFailedEventArgs eventArgs)
    {
    switch (eventArgs.Request.Method)
    {
        case "eth_requestAccounts":
            //TODO We don't need it? OnWalletUnauthorized(sender, eventArgs);
            break;
        case "personal_sign":
            Disconnect();
            break;
    }
    }
    */
    #endregion

    #region WEB3GL_WALLET_EVENTS
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

        #if UNITY_WEBGL
        signature = await Web3GL.Instance.Sign(response.message, response.address);
        #else
        signature = await wcController.Sign(response.message, response.address);
        #endif

        if (string.IsNullOrEmpty(signature))
        {
            Disconnect();
            return;
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
        var address = wcController.GetConnectedAddress();
        signature = await wcController.Sign(tx.userOpHash, address);
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

    private void OnRegisterSessionFailure()
    {
        // Remove the session key if we have failed during registering a new session
        var sessionKey = _openfort.LoadSessionKey();
        if (sessionKey == null)
        {
            _openfort.RemoveSessionKey();
        }

        Disconnect();
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
        _currentAddress = wcController.GetConnectedAddress();
        _currentChainId = wcController.GetChainId();
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
        _currentAddress = wcController.GetConnectedAddress(); 
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

    private void Disconnect()
    {
        ChangeState(State.Disconnecting);

        #if !UNITY_WEBGL
        //TODOMETAMASK MetaMaskUnity.Instance.Wallet.EndSession(true);
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