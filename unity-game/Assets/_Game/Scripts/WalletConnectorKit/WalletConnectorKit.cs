using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WalletConnectorKit : MonoBehaviour
{

    public event Action OnConnected;
    public event Action<string> OnDisconnected;
    public event Action<string> OnConnectionError;
    
    private IWalletConnector _currentConnector;

    void Awake() {
        InitializeWeb3GLConnector();
    }

    private void InitializeWeb3GLConnector() {
#if UNITY_WEBGL
        var web3GLConnector = new Web3GLWalletConnector();
        web3GLConnector.OnInitialized += HandleWeb3GLInitializationSuccess;
        web3GLConnector.OnInitializationError += HandleWeb3GLInitializationFailure;

        web3GLConnector.Initialize(); // Assuming Web3GLWalletConnector has an Initialize method
#else
        InitializeWalletConnectConnector();
#endif
    }
    
    private void SubscribeToConnectorEvents() {
        
        _currentConnector.OnInitialized += WalletConnector_OnInitialized_Handler;
        _currentConnector.OnInitializationError += WalletConnector_OnInitializationError_Handler;
        _currentConnector.OnConnected += WalletConnector_OnConnected_Handler;
        _currentConnector.OnDisconnected += WalletConnector_OnDisconnected_Handler;
        _currentConnector.OnConnectionError += WalletConnector_ConnectionError_Handler;
    }

    public void Initialize()
    {
        //_currentConnector.Initialize();
    }

    public void Connect() {
        _currentConnector.Connect();
    }

    public void Disconnect() {
        _currentConnector.Disconnect();
    }

    public async UniTask<string> Sign(string message, string address) {
        return await _currentConnector.Sign(message, address);
    }

    public async UniTask<string> AcceptOwnership(string contractAddress, string newOwnerAddress) {
        return await _currentConnector.AcceptOwnership(contractAddress, newOwnerAddress);
    }

    public async UniTask<string> GetConnectedAddress() {
        return await _currentConnector.GetConnectedAddress();
    }

    public async UniTask<int?> GetChainId() {
        return await _currentConnector.GetChainId();
    }

    private void HandleWeb3GLInitializationSuccess() {
        _currentConnector = new Web3GLWalletConnector();
        // Subscribe to connector events
        SubscribeToConnectorEvents();
    }
    
    private void InitializeWalletConnectConnector() {
        _currentConnector = new WalletConnectWalletConnector();
        // Subscribe to connector events
        SubscribeToConnectorEvents();
    }

    private void HandleWeb3GLInitializationFailure() {
        InitializeWalletConnectConnector();
    }
    
    // Event handlers
    private void WalletConnector_OnInitialized_Handler()
    {
        throw new System.NotImplementedException();
    }
    
    private void WalletConnector_OnInitializationError_Handler()
    {
        throw new System.NotImplementedException();
    }
    
    private void WalletConnector_OnConnected_Handler() {
        OnConnected?.Invoke();
    }

    private void WalletConnector_OnDisconnected_Handler(string reason) {
        OnDisconnected?.Invoke(reason);
    }

    private void WalletConnector_ConnectionError_Handler(string errorMessage) {
        OnConnectionError?.Invoke(errorMessage);
    }

    private void OnDestroy()
    {
        _currentConnector.OnInitialized -= WalletConnector_OnInitialized_Handler;
        _currentConnector.OnInitializationError -= WalletConnector_OnInitializationError_Handler;
        _currentConnector.OnConnected -= WalletConnector_OnConnected_Handler;
        _currentConnector.OnDisconnected -= WalletConnector_OnDisconnected_Handler;
        _currentConnector.OnConnectionError -= WalletConnector_ConnectionError_Handler;
    }
}
