using System;
using UnityEngine;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;

#if UNITY_EDITOR || UNITY_WEBGL
public class Web3GL : MonoBehaviour
{
    // Singleton instance
    public static Web3GL Instance { get; private set; }
    
    
    [DllImport("__Internal")]
    private static extern void InitializeWeb3();
    
    // Import methods from our .jslib file
    [DllImport("__Internal")]
    private static extern void ConnectToWeb3();
    
    [DllImport("__Internal")]
    private static extern void DisconnectFromWeb3();

    [DllImport("__Internal")]
    private static extern void PersonalSign(string message, string account);
    
    [DllImport("__Internal")]
    private static extern void AcceptOwnership(string contractAddress, string newOwnerAddress);
    
    [DllImport("__Internal")]
    private static extern void GetConnectedAddress();
    
    [DllImport("__Internal")]
    private static extern void GetChainId();
    

    // Declare events using Action
    public static event Action OnWeb3InitializedEvent;
    public static event Action<string> OnWeb3InitializeErrorEvent;
    public static event Action<string> OnWeb3ConnectedEvent;
    public static event Action<string> OnWeb3ConnectErrorEvent;
    public static event Action<string> OnWeb3DisconnectedEvent;
    public static event Action<string> OnWeb3DisconnectErrorEvent;

    // UCS to handle async responses
    private UniTaskCompletionSource<string> personalSignUcs;
    private UniTaskCompletionSource<string> acceptOwnershipUcs;
    private UniTaskCompletionSource<int> chainIdUcs;
    private UniTaskCompletionSource<string> addressUcs;
    
    public static string connectedAccount; // To store the connected account address

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    #region PUBLIC_METHODS
    public void Initialize()
    {
        InitializeWeb3();
    }
    
    public void Connect()
    {
        ConnectToWeb3();
    }
    
    public void Disconnect()
    {
        DisconnectFromWeb3();
    }
    
    public UniTask<string> Sign(string message, string account)
    {
        personalSignUcs = new UniTaskCompletionSource<string>();
        PersonalSign(message, account);
        return personalSignUcs.Task;
    }
    
    public UniTask<string> AcceptAccountOwnership(string contractAddress, string newOwnerAddress)
    {
        acceptOwnershipUcs = new UniTaskCompletionSource<string>();
        AcceptOwnership(contractAddress, newOwnerAddress);
        return acceptOwnershipUcs.Task;
    }

    public UniTask<int> GetChainIdAsync()
    {
        chainIdUcs = new UniTaskCompletionSource<int>();
        GetChainId();
        return chainIdUcs.Task;
    }

    public UniTask<string> GetConnectedAddressAsync()
    {
        addressUcs = new UniTaskCompletionSource<string>();
        GetConnectedAddress();
        return addressUcs.Task;
    }
    #endregion

    #region CALLED_FROM_JAVASCRIPT
    // Method called when initialization is successful
    void OnWeb3Initialized()
    {
        Debug.Log("Initialization successful.");
        OnWeb3InitializedEvent?.Invoke();
    }

    // Method called when there's an error during initialization
    void OnWeb3InitializeError(string error)
    {
        Debug.LogError("Initialization error: " + error);
        OnWeb3InitializeErrorEvent?.Invoke(error);
    }
    
    void OnWeb3Connected(string account)
    {
        Debug.Log("Connected to Web3. Account: " + account);
        
        // Store the connected account address
        connectedAccount = account;

        // Trigger the event
        OnWeb3ConnectedEvent?.Invoke(account);
    }

    void OnWeb3ConnectError(string error)
    {
        Debug.Log("Error connecting to Web3: " + error);

        // Clear the connected account on Disconnect
        connectedAccount = null;
        
        // Trigger the event
        OnWeb3ConnectErrorEvent?.Invoke(error);
    }
    
    void OnWeb3Disconnected(string message)
    {
        Debug.Log("Disconnected from Web3: " + message);
        
        // Clear the connected account on Disconnect
        connectedAccount = null;

        // Trigger the event
        OnWeb3DisconnectedEvent?.Invoke(message);
    }

    void OnWeb3DisconnectError(string error)
    {
        Debug.Log("Error disconnecting from Web3: " + error);

        // Trigger the event
        OnWeb3DisconnectErrorEvent?.Invoke(error);
    }

    public void OnPersonalSign(string signature)
    {
        personalSignUcs.TrySetResult(signature);
    }

    public void OnPersonalSignError(string errorMessage)
    {
        personalSignUcs.TrySetException(new Exception(errorMessage));
    }
    
    public void OnAcceptOwnershipSuccess(string transactionHash)
    {
        Debug.Log("Ownership transfer transaction successful: " + transactionHash);
        acceptOwnershipUcs.TrySetResult(transactionHash);
    }

    public void OnAcceptOwnershipError(string errorMessage)
    {
        Debug.LogError("Error in ownership transfer transaction: " + errorMessage);
        acceptOwnershipUcs.TrySetException(new Exception(errorMessage));
    }

    private void OnAddressRetrieved(string address)
    {
        addressUcs.TrySetResult(address);
    }

    private void OnAddressError(string errorMessage)
    {
        addressUcs.TrySetException(new Exception($"Error retrieving address: {errorMessage}"));
    }
    
    private void OnChainIdRetrieved(string chainIdString)
    {
        if (int.TryParse(chainIdString, out int parsedChainId))
        {
            chainIdUcs.TrySetResult(parsedChainId);
        }
        else
        {
            chainIdUcs.TrySetException(new Exception("Failed to parse chain ID."));
        }
    }

    private void OnChainIdError(string errorMessage)
    {
        chainIdUcs.TrySetException(new Exception($"Error retrieving chain ID: {errorMessage}"));
    }
    #endregion
}
#endif