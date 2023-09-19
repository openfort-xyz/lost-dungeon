using System;
using System.Collections.Generic;
using MetaMask;
using MetaMask.Models;
using MetaMask.Transports.Unity.UI;
using MetaMask.Unity;
using UnityEngine;
using UnityEngine.Events;
using Openfort;
using PlayFab;
using PlayFab.ClientModels;

[DefaultExecutionOrder(100)] //VERY IMPORTANT FOR ANDROID BUILD --> OnEnable() method was called very early in script execution order therefore we weren't subscribing to events.
public class Web3AuthService : MonoBehaviour
{
    public enum State
    {
        None,
        WalletConnecting,
        WalletConnectionCancelled,
        WalletConnected,
        RequestingMessage,
        SigningMessage,
        VerifyingSignature,
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
    
    private string _currentAddress;
    private int? _currentChainId;
    
    private OpenfortClient _openfort;
    private MetaMaskUnityUIHandler _metaMaskUIHandler;

    [HideInInspector] public bool authCompletedOnce;

    #region UNITY_LIFECYCLE
    private void OnEnable()
    {
        //TODO we could handle this managing MetaMask initialization manually.
        // Get MetaMask UI Handler
        _metaMaskUIHandler = FindObjectOfType<MetaMaskUnityUIHandler>();
        _metaMaskUIHandler.onCancelClicked += OnWalletUnauthorized;
        
        // MetaMaskUnity Events
        MetaMaskUnity.Instance.Wallet.Events.WalletConnected += OnWalletConnected;
        MetaMaskUnity.Instance.Wallet.Events.WalletAuthorized += OnWalletAuthorized;
        MetaMaskUnity.Instance.Wallet.Events.WalletReady += OnWalletReady;
        MetaMaskUnity.Instance.Wallet.Events.WalletUnauthorized += OnWalletUnauthorized;
        MetaMaskUnity.Instance.Wallet.Events.WalletDisconnected += OnWalletDisconnected;
        MetaMaskUnity.Instance.Events.EthereumRequestFailed += EventsOnEthereumRequestFailed;

#if UNITY_WEBGL
        // Web3GL Events
        Web3GL.OnWeb3ConnectedEvent += OnWeb3GLConnected;
        Web3GL.OnWeb3ConnectErrorEvent += OnWeb3GLConnectionFailure;  
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
        _metaMaskUIHandler.onCancelClicked -= OnWalletUnauthorized;
        
        // MetaMaskUnity Events
        MetaMaskUnity.Instance.Wallet.Events.WalletConnected -= OnWalletConnected;
        MetaMaskUnity.Instance.Wallet.Events.WalletAuthorized -= OnWalletAuthorized;
        MetaMaskUnity.Instance.Wallet.Events.WalletReady -= OnWalletReady;
        MetaMaskUnity.Instance.Wallet.Events.WalletUnauthorized -= OnWalletUnauthorized;
        MetaMaskUnity.Instance.Wallet.Events.WalletDisconnected -= OnWalletDisconnected;
        MetaMaskUnity.Instance.Events.EthereumRequestFailed -= EventsOnEthereumRequestFailed;
        
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
        _openfort = new OpenfortClient(OpenfortStaticData.publishableKey);
    }
    #endregion

    #region PUBLIC_METHODS
    public void Connect()
    {
        ChangeState(State.WalletConnecting);
        
#if UNITY_WEBGL
        Web3GL.Instance.Connect();
#else
        // Disconnect before connecting
        MetaMaskUnity.Instance.Disconnect(true);
        // Connect
        MetaMaskUnity.Instance.Connect();
#endif
    }
    #endregion

    #region WALLET_EVENT_HANDLERS
    private void OnWalletReady(object sender, EventArgs e)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET READY");
        RequestMessage();
    }

    private void OnWalletDisconnected(object sender, EventArgs e)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET DISCONNECTED");
        if (authCompletedOnce)
        {
            // We don't let the user create connect another EOA or even create a custodial player, this would lose their progress as they would log in with another OFplayer
            ChangeState(State.None);
        }
        else
        {
            ChangeState(State.Disconnected); 
        }
    }

    private void OnWalletUnauthorized(object sender, EventArgs e)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET UNAUTHORIZED");
        _metaMaskUIHandler.CloseQRCode();

        if (authCompletedOnce)
        {
            // We don't let the user create connect another EOA or even create a custodial player, this would lose their progress as they would log in with another OFplayer
            ChangeState(State.None);
        }
        else
        {
            //We're not officially disconnecting as the wallet is still not connected, but setting the state to Disconnected is ok for UI reasons (managed in LoginSceneManager)
            ChangeState(State.Disconnected);   
        }
    }

    private void EventsOnEthereumRequestFailed(object sender, MetaMaskEthereumRequestFailedEventArgs eventArgs)
    {
        switch (eventArgs.Request.Method)
        {
            case "eth_requestAccounts":
                _metaMaskUIHandler.CloseQRCode();
                if (authCompletedOnce)
                {
                    // We don't let the user create connect another EOA or even create a custodial player, this would lose their progress as they would log in with another OFplayer
                    ChangeState(State.None);
                }
                else
                {
                    //We're not officially disconnecting as the wallet is still not connected, but setting the state to Disconnected is ok for UI reasons (managed in LoginSceneManager)
                    ChangeState(State.Disconnected);   
                }
                break;
            case "personal_sign":
                Disconnect();
                break;
        }
    }

    // Not used as OnWalletReady works best
    private void OnWalletAuthorized(object sender, EventArgs e)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET AUTHORIZED");
    }
    
    // Not used as OnWalletReady works best
    private void OnWalletConnected(object sender, EventArgs e)
    {
        Debug.Log("WEB3AUTHSERVICE: WALLET CONNECTED");
    }
    
    private void OnWeb3GLConnected(string obj)
    {
        RequestMessage();
    }
    
    private void OnWeb3GLConnectionFailure(string obj)
    {
        Disconnect();
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
        string fromAddress = MetaMaskUnity.Instance.Wallet.SelectedAddress;
        var paramsArray = new string[] { fromAddress, response.message };

        var ethereumRequest = new MetaMaskEthereumRequest
        {
            Method = "personal_sign",
            Parameters = paramsArray
        };
        
        var ethRequestResult = await MetaMaskUnity.Instance.Wallet.Request(ethereumRequest);
        signature = ethRequestResult.ToString();
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
        Debug.Log("Registering session...");
        var loadedSessionKey = _openfort.LoadSessionKey();
        
        if (loadedSessionKey == null)
        {
            //////// Get OF Player ID
            // Create the request object
            GetUserDataRequest request = new GetUserDataRequest
            {
                Keys = new List<string> {"OFplayer"}
            };

            // Make the API call
            PlayFabClientAPI.GetUserReadOnlyData(request,
                result =>  // Inline success callback
                {
                    if (result.Data == null || !result.Data.ContainsKey("OFplayer"))
                    {
                        Debug.LogError("OFplayer not found");
                        Disconnect();
                        return;
                    }

                    // Access the value of OFplayer
                    string ofPlayer = result.Data["OFplayer"].Value;
                    StaticPlayerData.OFplayer = ofPlayer;
                    
                    RegisterSession();
                },
                error =>  // Inline failure callback
                {
                    Debug.LogError("Failed to get OFplayer data: " + error.GenerateErrorReport());
                    Disconnect();
                }
            );
        }
        else
        {
            Debug.Log("Session already registered!");
            ChangeState(State.Web3AuthSuccessful);
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
        string fromAddress = MetaMaskUnity.Instance.Wallet.SelectedAddress;
        var paramsArray = new string[] { fromAddress, tx.userOpHash };

        var ethereumRequest = new MetaMaskEthereumRequest
        {
            Method = "personal_sign",
            Parameters = paramsArray
        };
        
        var ethRequestResult = await MetaMaskUnity.Instance.Wallet.Request(ethereumRequest);
        signature = ethRequestResult.ToString();
#endif
        
        if (string.IsNullOrEmpty(signature))
        {
            Debug.Log("Signature failed.");
            Disconnect();
            return;
        }
        
        ChangeState(State.SessionSigned);
        
        var sessionResponse = await _openfort.SendSignatureSessionRequest(tx.id, signature);

        if (sessionResponse == null)
        {
            Debug.Log("Session response null.");
            Disconnect();
            return;
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
        _currentAddress = MetaMaskUnity.Instance.Wallet.ConnectedAddress;
        _currentChainId = (int)MetaMaskUnity.Instance.Wallet.ChainId;
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
        AzureFunctionCaller.RegisterSession(sessionKey.Address, StaticPlayerData.OFplayer); //OFplayer was saved during login
    }
    
    private void Disconnect()
    {
        ChangeState(State.Disconnecting);

#if !UNITY_WEBGL
        MetaMaskUnity.Instance.Disconnect(true);
#else
        //TODO Real disconnect from Web3GL?
        ChangeState(State.Disconnected);
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