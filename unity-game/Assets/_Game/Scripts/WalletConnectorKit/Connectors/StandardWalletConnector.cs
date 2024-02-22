using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
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
        //TODO-WC _wcController.OnConnected += HandleConnected;
        _wcController.OnConnectionError += HandleConnectionError;
        //TODO-WC _wcController.OnDisconnected += HandleDisconnected;
    }

    public void Connect() {
        _wcController.Connect();
    }

    public void Disconnect() {
        _wcController.Disconnect();
    }

    public async UniTask<string> Sign(string message, string address) 
    {
        //TODO-WC return await _wcController.Sign(message, address);
        return "";
    }
    
    public async UniTask<string> AcceptOwnership(string contractAddress, string newOwnerAddress)
    {
        //TODO-WCreturn await _wcController.AcceptAccountOwnership(contractAddress, newOwnerAddress);
        return "";
    }

    public async UniTask<string> GetConnectedAddress() {
        //TODO-WC return await _wcController.GetConnectedAddressAsync();
        return "";
    }

    public async UniTask<int?> GetChainId() {
        //TODO-WC return await _wcController.GetChainIdAsync();
        return 0;
    }

    // Event handler implementations
    /* //TODO-WC
    private void HandleConnected(SessionStruct session) {
        OnConnected?.Invoke();
    }
    */
    
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
        //TODO-WC _wcController.OnConnected -= HandleConnected;
        _wcController.OnConnectionError -= HandleConnectionError;
        //TODO-WC _wcController.OnDisconnected -= HandleDisconnected;
    }
}
