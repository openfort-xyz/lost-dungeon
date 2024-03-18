using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WalletConnectSharp.Sign.Models;
using Object = UnityEngine.Object;

public class StandardWalletConnector : IWalletConnector {
    private WalletConnectController _wcController;
    
    public event Action OnConnected;
    public event Action<string> OnConnectionError;
    public event Action<string> OnDisconnected;
    public event Action OnEthereumNotFound;

    public StandardWalletConnector() {
        _wcController = Object.FindObjectOfType<WalletConnectController>();
        if (_wcController == null) {
            Debug.LogError("WalletConnectController not found in the scene.");
            // Handle the error appropriately
            return;
        }
        
        // Subscribe to WalletConnectController events
        _wcController.OnConnected += HandleConnected;
        _wcController.OnDisconnected += HandleDisconnected;
        _wcController.OnConnectionError += HandleConnectionError;
    }

    public void Connect() {
        _wcController.Connect();
    }

    public void Disconnect() {
        _wcController.Disconnect();
    }

    public async UniTask<string> Sign(string message, string address) 
    {
        return await _wcController.Sign(message, address);
    }
    
    public async UniTask<string> AcceptOwnership(string contractAddress, string newOwnerAddress)
    {
        return await _wcController.AcceptAccountOwnership(contractAddress, newOwnerAddress);
    }

    public async UniTask<string> GetConnectedAddress() {
        return await _wcController.GetConnectedAddressAsync();
    }

    public async UniTask<int?> GetChainId() {
        return await _wcController.GetChainIdAsync();
    }

    // Event handler implementations
    private void HandleConnected(SessionStruct? session) {
        OnConnected?.Invoke();
    }
    
    private void HandleConnectionError(string error)
    {
        OnConnectionError?.Invoke(error);
    }

    private void HandleDisconnected() {
        OnDisconnected?.Invoke("Disconnected");
    }

    // Make sure to unsubscribe from events when this object is destroyed
    public void OnDestroy()
    {
        if (_wcController == null) return;
        _wcController.OnConnected -= HandleConnected;
        _wcController.OnDisconnected -= HandleDisconnected;
        _wcController.OnConnectionError -= HandleConnectionError;
    }
}
