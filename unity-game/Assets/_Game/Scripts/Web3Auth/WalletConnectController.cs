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
using UnityBinder;
using UnityEngine;
using UnityEngine.Events;
using WalletConnect;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Sign.Models.Engine.Events;
using WalletConnectUnity.Utils;

public class WalletConnectController : BindableMonoBehavior
{
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
        public WCEthSendTransaction()
        {
        }

        public WCEthSendTransaction(params WCTransaction[] transactions) : base(transactions)
        {
        }
    }
    
    [RpcMethod("personal_sign")]
    [RpcRequestOptions(Clock.ONE_MINUTE, 99998)]
    private class PersonalSign : List<string>
    {
        public PersonalSign(string hexUtf8, string account) : base(new[] { hexUtf8, account })
        {
        }
        
        public PersonalSign()                                              
        {                                                                  
        }     
    }
    
    [RpcMethod("wallet_switchEthereumChain")]
    [RpcRequestOptions(Clock.ONE_MINUTE, 99999)] // Adjust the clock and priority as needed
    public class WCSwitchEthereumChain : List<string>
    {
        public WCSwitchEthereumChain(string chainId) : base(new[] { chainId })
        {
        }
        
        public WCSwitchEthereumChain()
        {
        }
    }

    
    public event UnityAction<SessionStruct> OnConnected;
    public event UnityAction<string> OnConnectionError;
    public event UnityAction OnDisconnected;
    
    [Inject]
    private WCSignClient _wcSignClient;
    [SerializeField] private WCQRCodeHandler wcQrCodeHandler;
    
    [HideInInspector] public SessionStruct CurrentSession;

    #region UNITY_LIFECYCLE
    private void Start()
    {
        _wcSignClient.SessionConnectionErrored += WcSignClientOnSessionConnectionErrored;
        _wcSignClient.SessionDeleted += WcSignClientOnSessionDeleted;
        wcQrCodeHandler.OnCancelButtonClicked += WcQrCodeHandlerOnOnCancelButtonClicked;
    }

    private void OnDisable()
    {
        _wcSignClient.SessionConnectionErrored -= WcSignClientOnSessionConnectionErrored;
        _wcSignClient.SessionDeleted -= WcSignClientOnSessionDeleted;
        wcQrCodeHandler.OnCancelButtonClicked -= WcQrCodeHandlerOnOnCancelButtonClicked;
    }
    #endregion

    public async void Initialize()
    {
        
    }
    
    public async void Connect()
    {
        if (_wcSignClient.SignClient == null)
            await _wcSignClient.InitSignClient();

        if (_wcSignClient == null)
        {
            Debug.LogError("No WCSignClient scripts found in scene!");
            return;
        }

        // Connect Sign Client
        Debug.Log("Connecting sign client..");

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
        
        var dappConnectOptions = new ConnectOptions()
        {
            RequiredNamespaces = requiredNamespaces
        };

        var connectData = await _wcSignClient.Connect(dappConnectOptions);
        
        Debug.Log($"Connection successful, URI: {connectData.Uri}");

        try
        {
            await connectData.Approval;
            
            // We need to move this to the main unity thread
            // TODO Perhaps ensure we are using Unity's Sync context inside WalletConnectSharp
            MTQ.Enqueue(() =>
            {
                Debug.Log($"Connection approved, URI: {connectData.Uri}");
                CurrentSession = connectData.Approval.Result;
                OnConnected?.Invoke(CurrentSession);
            });
        }
        catch (Exception e)
        {
            Debug.LogError(("Connection failed: " + e.Message));
            Debug.LogError(e);
        }
    }
    
    public void Disconnect()
    {
        _wcSignClient.Disconnect(CurrentSession.Topic); 
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
            var desiredChainId = 4337; // BEAM network chain ID

            if (currentChainId != desiredChainId)
            {
                Debug.LogWarning($"Wrong network. Please switch your wallet to the correct network. Chain ID should be {desiredChainId}");
                var success = await SwitchToBeamNetwork(); // Implement this method for network switching

                if (!success)
                {
                    Debug.LogError("Failed switching to BEAM network.");
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
            var fullChainId = Chain.EvmNamespace + ":" + GetChainId(); // Needs to be something like "eip155:80001"

            // Send the transaction
            var txHash = await _wcSignClient.Request<WCEthSendTransaction, string>(CurrentSession.Topic, ethSendTransaction, fullChainId);

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
    private void WcSignClientOnSessionConnectionErrored(object sender, Exception e)
    {
        Debug.LogWarning("WC SESSION CONNECTION ERROR");
        // No need for real disconnection as we're not connected yet.
        OnConnectionError?.Invoke(e.Message);
    }
    
    private void WcSignClientOnSessionDeleted(object sender, SessionEvent e) => MTQ.Enqueue(() =>
    {
        Debug.LogWarning("WC SESSION DELETED");
        OnDisconnected?.Invoke();
    });
    
    private void WcQrCodeHandlerOnOnCancelButtonClicked()
    {
        Debug.LogWarning("WC CANCEL BUTTON CLICKED");
        OnConnectionError?.Invoke("Connection error reason: cancel button pressed.");
    }
    #endregion

    #region PRIVATE_METHODS
    private async UniTask<string> PersonalSignAsync(string message, string address)
    {
        try
        {
            var fullChainId = Chain.EvmNamespace + ":" + GetChainId(); // Needs to be something like "eip155:80001"

            //var hexUtf8 = "0x" + Encoding.UTF8.GetBytes(message).ToHex();
            var request = new PersonalSign(message, address);                                
        
            var result = await _wcSignClient.Request<PersonalSign, string>(CurrentSession.Topic, request, fullChainId);
                 
            Debug.Log("Got result from request: " + result);
        
            return result; 
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred: {ex.Message}");
            // Optionally, you can handle the exception more specifically or rethrow it
            return null; // Or handle the failure case appropriately
        }                                                                                 
    }
    
    private string GetConnectedAddress()
    {
        var defaultChain = CurrentSession.Namespaces.Keys.FirstOrDefault();
            
        if (string.IsNullOrWhiteSpace(defaultChain))
            return null;

        var defaultNamespace = CurrentSession.Namespaces[defaultChain];
        
        if (defaultNamespace.Accounts.Length == 0)
            return null;
            
        var fullAddress = defaultNamespace.Accounts[0];
        var addressParts = fullAddress.Split(":");
            
        var address = addressParts[2];

        return address;
    }
    
    private int? GetChainId()
    {
        var defaultChain = CurrentSession.Namespaces.Keys.FirstOrDefault();
    
        if (string.IsNullOrWhiteSpace(defaultChain))
            return null;

        var defaultNamespace = CurrentSession.Namespaces[defaultChain];
    
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
    
    private async UniTask<bool> SwitchToBeamNetwork()
    {
        try
        {
            const string beamChainId = "0x10F1"; // Beam mainnet chain id in hex

            var switchChainRequest = new WCSwitchEthereumChain(beamChainId);
            var result = await _wcSignClient.Request<WCSwitchEthereumChain, string>(CurrentSession.Topic, switchChainRequest);

            // Handle result or success confirmation
            return result != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error switching Ethereum chain: {e.Message}");
            return false;
        }
    }
    #endregion
}
