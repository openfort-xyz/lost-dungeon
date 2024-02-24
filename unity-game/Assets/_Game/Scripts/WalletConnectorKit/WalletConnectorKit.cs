using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WalletConnectorKit : MonoBehaviour
{
    // Wallet events
    public event Action OnConnected;
    public event Action<string> OnDisconnected;
    public event Action<string> OnConnectionError;
    
    private IWalletConnector _currentConnector;

    private bool _isConnected;
    
    public static WalletConnectorKit Instance { get; private set; }
    
    // Flag to enable persistent behaviour
    public bool persistAcrossScenes = true;

    public GameObject panel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
        {
            // This makes the game object persist across scenes
            DontDestroyOnLoad(gameObject);
        }
    }
    
    void Start() {
        Initialize();
    }

    private void Initialize() {
#if UNITY_EDITOR
        _currentConnector = new StandardWalletConnector();
#elif UNITY_WEBGL
    _currentConnector = new Web3GLWalletConnector();
#else
    _currentConnector = new StandardWalletConnector();
#endif
    
        _currentConnector.OnConnected += WalletConnector_OnConnected_Handler;
        _currentConnector.OnDisconnected += WalletConnector_OnDisconnected_Handler;
        _currentConnector.OnConnectionError += WalletConnector_ConnectionError_Handler;
        _currentConnector.OnEthereumNotFound += WalletConnector_OnEthereumNotFound_Handler;
    }
    
    public void Connect() {
        _currentConnector.Connect();
    }

    public void Disconnect() {
        _currentConnector.Disconnect();
    }
    
    public bool IsConnected()
    {
        return _isConnected;
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
    
    // Event handlers
    private void WalletConnector_OnConnected_Handler() {
        OnConnected?.Invoke();
        _isConnected = true;
        panel.SetActive(true);
    }

    private void WalletConnector_OnDisconnected_Handler(string reason) {
        OnDisconnected?.Invoke(reason);
        _isConnected = false;
        panel.SetActive(false);
    }

    private void WalletConnector_ConnectionError_Handler(string errorMessage) {
        OnConnectionError?.Invoke(errorMessage);
        _isConnected = false;
        panel.SetActive(false);
    }
    
    // Only in WebGL
    private void WalletConnector_OnEthereumNotFound_Handler()
    {
        // We haven't found any injected wallet in the browser.
        Debug.Log("No injected wallet installed in browser.");
        
        // Let's switch to StandardWalletConnector
        _currentConnector = new StandardWalletConnector();
        
        OnConnectionError?.Invoke("No injected wallet installed in browser.");
        /*
        _currentConnector.OnConnected += WalletConnector_OnConnected_Handler;
        _currentConnector.OnDisconnected += WalletConnector_OnDisconnected_Handler;
        _currentConnector.OnConnectionError += WalletConnector_ConnectionError_Handler;
        _currentConnector.OnEthereumNotFound += WalletConnector_OnEthereumNotFound_Handler;
        
        // Try to connect again with StandardWalletConnector
        _currentConnector.Connect();
        */
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_currentConnector == null) return;
        _currentConnector.OnConnected -= WalletConnector_OnConnected_Handler;
        _currentConnector.OnDisconnected -= WalletConnector_OnDisconnected_Handler;
        _currentConnector.OnConnectionError -= WalletConnector_ConnectionError_Handler;
    }
}
