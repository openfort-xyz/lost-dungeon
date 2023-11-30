using System;
using Cysharp.Threading.Tasks;

public class Web3GLWalletConnector : IWalletConnector {
    private readonly Web3GL _web3GL;

    public event Action OnInitialized;
    public event Action OnInitializationError;
    
    public event Action OnConnected;
    public event Action<string> OnConnectionError;
    public event Action<string> OnDisconnected;

    public Web3GLWalletConnector() {
        _web3GL = Web3GL.Instance; // Assuming Web3GL follows a singleton pattern

        // Subscribe to Web3GL events
        _web3GL.OnWeb3InitializedEvent += HandleWeb3GLInitialized;
        _web3GL.OnWeb3InitializeErrorEvent += HandleWeb3GLInitializeError;
        
        _web3GL.OnWeb3ConnectedEvent += HandleWeb3GLConnected;
        _web3GL.OnWeb3ConnectErrorEvent += HandleWeb3GLConnectionError;
        _web3GL.OnWeb3DisconnectedEvent += HandleWeb3GLDisconnected;
    }

    public void Initialize()
    {
        _web3GL.Initialize();
    }

    public void Connect() {
        _web3GL.Connect();
    }

    public void Disconnect() {
        _web3GL.Disconnect();
    }

    public async UniTask<string> Sign(string message, string address) {
        return await _web3GL.Sign(message, address);
    }

    public async UniTask<string> GetConnectedAddress() {
        return await _web3GL.GetConnectedAddressAsync(); // Or use the appropriate method in Web3GL
    }

    public async UniTask<int?> GetChainId() {
        return await _web3GL.GetChainIdAsync();
    }

    public async UniTask<string> AcceptOwnership(string contractAddress, string newOwnerAddress)
    {
        return await _web3GL.AcceptAccountOwnership(contractAddress, newOwnerAddress);
    }

    // Event handlers for Web3GL events
    private void HandleWeb3GLInitialized()
    {
        OnInitialized?.Invoke();
    }
    
    private void HandleWeb3GLInitializeError(string error)
    {
        OnInitializationError?.Invoke();
    }
    
    private void HandleWeb3GLConnected(string account) {
        OnConnected?.Invoke();
    }

    private void HandleWeb3GLDisconnected(string reason) {
        OnDisconnected?.Invoke(reason);
    }

    private void HandleWeb3GLConnectionError(string error) {
        OnConnectionError?.Invoke(error);
    }

    // Make sure to unsubscribe from events when this object is destroyed
    public void OnDestroy() {
        _web3GL.OnWeb3InitializedEvent -= HandleWeb3GLInitialized;
        _web3GL.OnWeb3InitializeErrorEvent -= HandleWeb3GLInitializeError;
        
        _web3GL.OnWeb3ConnectedEvent -= HandleWeb3GLConnected;
        _web3GL.OnWeb3ConnectErrorEvent -= HandleWeb3GLConnectionError;
        _web3GL.OnWeb3DisconnectedEvent -= HandleWeb3GLDisconnected;
    }
}

