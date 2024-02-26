using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Web3;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using WalletConnectSharp.Common.Model.Errors;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Sign.Models.Engine.Events;
using WalletConnectUnity.Core;
using WalletConnectUnity.Modal;
using WalletConnectUnity.Modal.Sample;

public class WalletConnectController : MonoBehaviour
{
    public static bool isFirstTime = true;
    
    public class WCTransaction
    {
        [JsonProperty("from")] public string From { get; set; }

        [JsonProperty("to")] public string To { get; set; }

        [JsonProperty("gas", NullValueHandling = NullValueHandling.Ignore)]
        public string Gas { get; set; }

        [JsonProperty("gasPrice", NullValueHandling = NullValueHandling.Ignore)]
        public string GasPrice { get; set; }

        [JsonProperty("value")] public string Value { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public string Data { get; set; } = "0x";
    }
    
    [RpcMethod("eth_sendTransaction")]
    [RpcRequestOptions(Clock.ONE_MINUTE, 99997)]
    public class WCEthSendTransaction : List<WCTransaction>
    {
        public WCEthSendTransaction(params WCTransaction[] transactions) : base(transactions)
        {
        }
        
        [Preserve]
        public WCEthSendTransaction()
        {
        }
    }
    
    [RpcMethod("personal_sign")]
    [RpcRequestOptions(Clock.ONE_MINUTE, 99998)]
    public class PersonalSign : List<string>
    {
        public PersonalSign(string hexUtf8, string account) : base(new[] { hexUtf8, account })
        {
        }

        [Preserve]
        public PersonalSign()
        {
        }
    }
    
    [RpcMethod("wallet_switchEthereumChain")]
    [RpcRequestOptions(Clock.ONE_MINUTE, 99998)] // Adjust the clock and priority as needed
    public class WCSwitchEthereumChain : List<object>
    {
        public WCSwitchEthereumChain(object chainIdData) : base(new[] { chainIdData })
        {
        }
        
        [Preserve]
        public WCSwitchEthereumChain()
        {
        }
    }
    
    [RpcMethod("wallet_addEthereumChain")]
    [RpcRequestOptions(Clock.ONE_MINUTE, 99997)] // Adjust the clock and priority if necessary
    public class WCAddEthereumChain
    {
        // Required parameter:
        [JsonProperty("chainId")]
        public string ChainId { get; set; }

        // Optional parameters (consider making these nullable or have default values)
        [JsonProperty("chainName", NullValueHandling = NullValueHandling.Ignore)]
        public string ChainName { get; set; }

        [JsonProperty("nativeCurrency", NullValueHandling = NullValueHandling.Ignore)]
        public WCNativeCurrency NativeCurrency { get; set; }

        [JsonProperty("rpcUrls", NullValueHandling = NullValueHandling.Ignore)]
        public string[] RpcUrls { get; set; }

        [JsonProperty("blockExplorerUrls", NullValueHandling = NullValueHandling.Ignore)]
        public string[] BlockExplorerUrls { get; set; }

        // Constructor with required parameter
        public WCAddEthereumChain(string chainId)
        {
            ChainId = chainId;
        }

        // Parameterless constructor if needed 
        [Preserve]
        public WCAddEthereumChain()
        {
        }
    }

    public class AddEthereumChainParams
    {
        
    }
// Helper class for native currency
    public class WCNativeCurrency
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("decimals")]
        public int Decimals { get; set; }
    }
    
    public event UnityAction<SessionStruct> OnConnected;
    public event UnityAction<string> OnConnectionError;
    public event UnityAction OnDisconnected;
    
    #region UNITY_LIFECYCLE

    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private async void SubscribeToEvents()
    {
        WalletConnectModal.Ready += (sender, args) =>
        {
            if (args.SessionResumed)
            {
                // Session has been resumed, proceed to the game
                Debug.Log("Session resumed.");
            }
            else
            {
                // WalletConnectModal events. This happens before the wallet is connected.
                WalletConnectModal.ConnectionError += ConnectionError_Handler;

                // WalletConnect.Instance events. This happens when the wallet is connected.
                // Invoked after wallet connected
                WalletConnect.Instance.SessionConnected += OnSessionConnected_Handler;
                // Invoked after wallet disconnected
                WalletConnect.Instance.SessionDisconnected += OnSessionDisconnected_Handler;
            
                // We don't do anything here but we want to have it for logs.
                WalletConnect.Instance.ActiveSessionChanged += (_, @struct) =>
                {
                    if (string.IsNullOrEmpty(@struct.Topic))
                        return;
                    
                    Debug.Log($"[WalletConnectModalSample] Session connected. Topic: {@struct.Topic}");
                };
            }
        };
        
        //await WalletConnectModal.InitializeAsync();
    }
    
    // bug-wc
    private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Login")
        {
            if (isFirstTime)
            {
                isFirstTime = false;
                return;
            }
            
            Debug.Log("Login scene loaded. Checking SignClient...");
            // Check for sign client pending requests?
            if (WalletConnect.Instance.SignClient.PendingSessionRequests == null)
            {
                Debug.Log("Sign client is null. Reinitializing to create a new one...");
                await WalletConnect.Instance.InitializeAsync();
            }
        }
    }

    private void OnDisable()
    {
        /*
        WalletConnectModal.ConnectionError -= ConnectionError_Handler;
        WalletConnect.Instance.SessionConnected -= OnSessionConnected_Handler;
        WalletConnect.Instance.SessionDisconnected -= OnSessionDisconnected_Handler;
        */
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    #endregion
    
    public void Connect()
    {
        Debug.Log("Connecting...");
        var dappConnectOptions = new WalletConnectModalOptions
        {
            ConnectOptions = BuildConnectOptions()
        };

        WalletConnectModal.Open(dappConnectOptions);
    }
    
    public async void Disconnect()
    {
        await WalletConnect.Instance.DisconnectAsync();
    }
    
    public async UniTask<string> Sign(string message, string address)
    {
        var result = await PersonalSignAsync(message, address);
        return result;
    }
    
    public async UniTask<string> AcceptAccountOwnership(string contractAddress, string newOwnerAddress)
    {
        try
        {
            // Contract details
            string contractABI = "[{\"inputs\":[],\"name\":\"acceptOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";

            // Initialize Nethereum
            var web3 = new Web3();
            var contract = web3.Eth.GetContract(contractABI, contractAddress);

            // Get the function from the contract
            var acceptOwnershipFunction = contract.GetFunction("acceptOwnership");
            var encodedData = acceptOwnershipFunction.GetData();
            
            var currentChainId = GetChainId(); // Implement this method to get the current chain ID
            var currentFullChainId = Chain.EvmNamespace + ":" + currentChainId;
            var desiredChainId = 4337; // BEAM network chain ID

            if (currentChainId != desiredChainId)
            {
                Debug.LogWarning($"Wrong network. Please switch your wallet to the correct network. Chain ID should be {desiredChainId}");
                var success = await SwitchToBeamNetwork(currentFullChainId); // Implement this method for network switching

                if (!success)
                {
                    Debug.LogError("Failed switching to BEAM network.");
                    //TODO-check!!!!!!
                    var ueah = await AddBeamNetwork(currentFullChainId);
                    return null;
                }
            }

            // Prepare the transaction
            var txParams = new WCTransaction()
            {
                From = newOwnerAddress,
                To = contractAddress,
                Data = encodedData,
                Value = "0x0",
                Gas = "0xFDE8", // Hex value for 65,000 gas limit
            };

            var ethSendTransaction = new WCEthSendTransaction(txParams);
            // The fullChainId might need to be adjusted based on the network specifics
            var fullChainId = Chain.EvmNamespace + ":" + desiredChainId; // BEAM!
            
            await UniTask.Delay(2500);
            
            // Send the transaction
            var signClient = WalletConnect.Instance.SignClient;
            var txHash = await signClient.Request<WCEthSendTransaction, string>(ethSendTransaction, fullChainId);

            // Handle the transaction hash (e.g., display it, log it, etc.)
            Debug.Log("Transaction Hash: " + txHash);
            return txHash;   
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred: {e.Message}");
            return null; // Or handle the failure case appropriately
        }
    }
    
    public UniTask<string> GetConnectedAddressAsync()
    {
        return UniTask.Run(GetConnectedAddress);
    }
    
    
    public UniTask<int?> GetChainIdAsync()
    {
        return UniTask.Run(GetChainId);
    }

    #region EVENT_HANDLERS
    private void OnSessionConnected_Handler(object sender, SessionStruct session)
    {
        Debug.Log("WC SESSION CONNECTED");
        OnConnected?.Invoke(session);
    }
    
    private void OnSessionDisconnected_Handler(object sender, EventArgs eventArgs)
    {
        Debug.LogWarning("WC SESSION DISCONNECTED");
        OnDisconnected?.Invoke();
    }
    
    private void ConnectionError_Handler(object sender, EventArgs eventArgs)
    {
        Debug.Log("WC SESSION CONNECTION ERROR");
        // No need for real disconnection as we're not connected yet.
        OnConnectionError?.Invoke($"Connection error reason: {eventArgs}");
    }
    #endregion

    #region PRIVATE_METHODS
    private async UniTask<string> PersonalSignAsync(string message, string address)
    {
        var data = new PersonalSign(message, address);

        try
        {
            var result = await WalletConnect.Instance.RequestAsync<PersonalSign, string>(data);
            return result;
        }
        catch (WalletConnectException e)
        {
            Debug.Log(e.Message);
            return null;
        }
    }

    private string GetConnectedAddress()
    {
        var currentAddress = WalletConnect.Instance.ActiveSession.CurrentAddress(Chain.EvmNamespace);
        return currentAddress.Address;
    }
    
    private int? GetChainId()
    {
        var currentSession = WalletConnect.Instance.ActiveSession;
        
        var defaultChain = currentSession.Namespaces.Keys.FirstOrDefault();
    
        if (string.IsNullOrWhiteSpace(defaultChain))
            return null;

        var defaultNamespace = currentSession.Namespaces[defaultChain];
    
        if (defaultNamespace.Chains.Length == 0)
            return null;

        // Assuming we need the last chain if there are multiple chains
        var fullChain = defaultNamespace.Chains.LastOrDefault();

        if (string.IsNullOrWhiteSpace(fullChain))
            return null;

        var chainParts = fullChain.Split(':');

        // Check if the split operation gives at least 2 parts
        if (chainParts.Length < 2)
            return null;

        if (int.TryParse(chainParts[1], out int chainId))
        {
            return chainId;
        }

        return null;
    }
    
    private async UniTask<bool> AddBeamNetwork(string currentChain)
    {
        try
        {
            var chainIdData = new { chainId = "0x10F1" }; // Desired chain ID in hexadecimal
            var addChainRequest = new WCAddEthereumChain("0x10F1");
            
            
            var signClient = WalletConnect.Instance.SignClient;
            // Request to switch the Ethereum chain
            var result = await signClient.Request<WCAddEthereumChain, object>(addChainRequest, currentChain);

            // Interpret a null response as successful operation
            // https://docs.metamask.io/wallet/reference/wallet_addethereumchain/
            return result == null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error adding Ethereum chain: {e.Message}");
            return false;
        }
    }
    
    private async UniTask<bool> SwitchToBeamNetwork(string currentChain)
    {
        try
        {
            var chainIdData = new { chainId = "0x10F1" }; // Desired chain ID in hexadecimal
            var switchChainRequest = new WCSwitchEthereumChain(chainIdData);
            
            var signClient = WalletConnect.Instance.SignClient;
            // Request to switch the Ethereum chain
            var result = await signClient.Request<WCSwitchEthereumChain, object>(switchChainRequest, currentChain);

            // Interpret a null response as successful operation
            // https://docs.metamask.io/wallet/reference/wallet_switchethereumchain/
            return result == null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error switching Ethereum chain: {e.Message}");
            return false;
        }
    }
    #endregion

    #region PRIVATE_METHODS
    private ConnectOptions BuildConnectOptions()
    {
        var requiredNamespaces = new RequiredNamespaces();
        
        // TODO Make configurable
        var methods = new string[]
        {
            "eth_sendTransaction",
            "eth_signTransaction",
            "eth_sign",
            "personal_sign",
            "eth_signTypedData",
            "wallet_switchEthereumChain"
        };

        var events = new string[]
        {
            "chainChanged", "accountsChanged"
        };
        
        requiredNamespaces.Add(Chain.EvmNamespace, new ProposedNamespace()
        {
            Chains = new []{"eip155:1"}, //TODO!!
            Events = events,
            Methods = methods
        });

        return new ConnectOptions
        {
            RequiredNamespaces = requiredNamespaces
        };
    }
    #endregion
}
