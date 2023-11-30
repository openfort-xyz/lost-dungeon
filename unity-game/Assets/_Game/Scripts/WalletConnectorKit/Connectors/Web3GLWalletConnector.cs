using System;
using Cysharp.Threading.Tasks;

public class Web3GLWalletConnector : IWalletConnector {
    private Web3GL _web3GL;
    
    public event Action OnConnected;
    public event Action<string> OnConnectionError;
    public event Action<string> OnDisconnected;
    public event Action OnEthereumNotFound;

    public Web3GLWalletConnector() {
        _web3GL = Web3GL.Instance; // Assuming Web3GL follows a singleton pattern
        
        _web3GL.OnWeb3ConnectedEvent += HandleWeb3GLConnected;
        _web3GL.OnWeb3ConnectErrorEvent += HandleWeb3GLConnectionError;
        _web3GL.OnWeb3DisconnectedEvent += HandleWeb3GLDisconnected;
        _web3GL.OnEthereumNotFoundEvent += HandleEthereumNotFound;
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
    
    private void HandleWeb3GLConnected(string account) {
        OnConnected?.Invoke();
    }

    private void HandleWeb3GLDisconnected(string reason) {
        OnDisconnected?.Invoke(reason);
    }

    private void HandleWeb3GLConnectionError(string error) {
        OnConnectionError?.Invoke(error);
    }
    
    private void HandleEthereumNotFound() {
        OnEthereumNotFound?.Invoke();
    }

    // Make sure to unsubscribe from events when this object is destroyed
    public void OnDestroy() {
        _web3GL.OnWeb3ConnectedEvent -= HandleWeb3GLConnected;
        _web3GL.OnWeb3ConnectErrorEvent -= HandleWeb3GLConnectionError;
        _web3GL.OnWeb3DisconnectedEvent -= HandleWeb3GLDisconnected;
        _web3GL.OnEthereumNotFoundEvent -= HandleEthereumNotFound;
    }
}

