using System;
using UnityEngine;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;

#if UNITY_WEBGL
public class Web3GL : MonoBehaviour
{
    // Singleton instance
    public static Web3GL Instance { get; private set; }
    
    // Import methods from our .jslib file
    [DllImport("__Internal")]
    private static extern void ConnectToWeb3();

    [DllImport("__Internal")]
    private static extern void PersonalSign(string message, string account);
    
    [DllImport("__Internal")]
    private static extern void GetConnectedAddress();
    
    [DllImport("__Internal")]
    private static extern void GetChainId();
    

    // Declare events using Action
    public static event Action<string> OnWeb3ConnectedEvent;
    public static event Action<string> OnWeb3ConnectErrorEvent;

    // UCS to handle async responses
    private UniTaskCompletionSource<string> personalSignUcs;
    private UniTaskCompletionSource<int> chainIdUcs;
    private UniTaskCompletionSource<string> addressUcs;

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
    public void Connect()
    {
        ConnectToWeb3();
    }
    
    public UniTask<string> Sign(string message, string account)
    {
        personalSignUcs = new UniTaskCompletionSource<string>();
        PersonalSign(message, account);
        return personalSignUcs.Task;
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
    void OnWeb3Connected(string account)
    {
        Debug.Log("Connected to Web3. Account: " + account);

        // Trigger the event
        OnWeb3ConnectedEvent?.Invoke(account);
    }

    void OnWeb3ConnectError(string error)
    {
        Debug.Log("Error connecting to Web3: " + error);

        // Trigger the event
        OnWeb3ConnectErrorEvent?.Invoke(error);
    }

    public void OnPersonalSign(string signature)
    {
        personalSignUcs.TrySetResult(signature);
    }

    public void OnPersonalSignError(string errorMessage)
    {
        personalSignUcs.TrySetException(new Exception(errorMessage));
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