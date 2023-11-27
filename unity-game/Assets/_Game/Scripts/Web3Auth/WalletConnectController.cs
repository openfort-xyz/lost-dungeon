using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    
    public event UnityAction<SessionStruct> OnConnected;
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
    
    public async Task<string> Sign(string message, string address)
    {
        var result = await PersonalSignAsync(message, address);
        return result;
    }
    
    public string GetConnectedAddress()
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
    
    public int? GetChainId()
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

    #region EVENT_HANDLERS
    private void WcSignClientOnSessionConnectionErrored(object sender, Exception e)
    {
        Debug.LogWarning("WC SESSION CONNECTION ERROR");
        // No need for real disconnection as we're not connected yet.
        OnDisconnected?.Invoke();
    }
    
    private void WcSignClientOnSessionDeleted(object sender, SessionEvent e) => MTQ.Enqueue(() =>
    {
        Debug.LogWarning("WC SESSION DELETED");
        OnDisconnected?.Invoke();
    });
    
    private void WcQrCodeHandlerOnOnCancelButtonClicked()
    {
        Debug.LogWarning("WC CANCEL BUTTON CLICKED");
        OnDisconnected?.Invoke();
    }
    #endregion

    #region PRIVATE_METHODS
    private async Task<string> PersonalSignAsync(string message, string address)
    {
        try
        {
            var fullChainId = Chain.EvmNamespace + ":" + GetChainId(); // Needs to be something like "eip155:80001"

            var hexUtf8 = "0x" + Encoding.UTF8.GetBytes(message).ToHex();
            var request = new PersonalSign(hexUtf8, address);                                
        
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

    public async Task<string> AcceptAccountOwnership(string contractAddress, string newOwnerAddress)
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
            
            var txParams = new WCTransaction()
            {
                From = newOwnerAddress,
                To = contractAddress,
                Data = encodedData,
                Value = "0x0",
                Gas = new HexBigInteger(65000).ToString(), // "0x249F0" for 150,000 gas limit
            };

            var ethSendTransaction = new WCEthSendTransaction(txParams);

            var fullChainId = Chain.EvmNamespace + ":" + GetChainId(); // Needs to be something like "eip155:80001"

            // Send the transaction
            var txHash =
                await _wcSignClient.Request<WCEthSendTransaction, string>(CurrentSession.Topic, ethSendTransaction, fullChainId);

            // Handle the transaction hash (e.g., display it, log it, etc.)
            Debug.Log("Transaction Hash: " + txHash);
            return txHash;
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred: {e.Message}");
            // Optionally, you can handle the exception more specifically or rethrow it
            return null; // Or handle the failure case appropriately
        }
    }
    #endregion
}
