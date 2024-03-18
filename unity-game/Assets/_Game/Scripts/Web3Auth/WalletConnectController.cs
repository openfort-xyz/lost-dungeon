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
    
    [RpcMethod("eth_chainId")]
    [RpcRequestOptions(Clock.ONE_MINUTE, 99998)]
    public class EthChainId
    {
        [Preserve]
        public EthChainId()
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
    [RpcRequestOptions(Clock.ONE_MINUTE, 99998)] // Adjust the clock and priority as needed
    public class WCAddEthereumChain : List<object>
    {
        public WCAddEthereumChain(object chainData) : base(new[] { chainData })
        {
        }
        
        [Preserve]
        public WCAddEthereumChain()
        {
        }
    }
    
    public event UnityAction<SessionStruct?> OnConnected;
    public event UnityAction<string> OnConnectionError;
    public event UnityAction OnDisconnected;

    public SessionStruct? connectedSession;
    
    #region UNITY_LIFECYCLE

    private void Start()
    {
        SubscribeToEvents();
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
                //WalletConnect.Instance.SessionConnected += OnSessionConnected_Handler;
                // Invoked after wallet disconnected
                WalletConnect.Instance.SessionDisconnected += OnSessionDisconnected_Handler;
            
                // We don't do anything here but we want to have it for logs.
                WalletConnect.Instance.ActiveSessionChanged += (s, @struct) =>
                {
                    if (string.IsNullOrEmpty(@struct.Topic))
                        return;
                    
                    Debug.Log($"[WalletConnectModalSample] Session connected. Topic: {@struct.Topic}");
                    connectedSession = @struct;
                    OnSessionConnected_Handler(s, connectedSession);
                };
            }
        };
    }

    private void OnDisable()
    {
        // No need to unsubscribe as this class persists throughout whole game.
        /*
        WalletConnectModal.ConnectionError -= ConnectionError_Handler;
        WalletConnect.Instance.SessionConnected -= OnSessionConnected_Handler;
        WalletConnect.Instance.SessionDisconnected -= OnSessionDisconnected_Handler;
        */
    }
    #endregion
    
    public async void Connect()
    {
        try
        {
            if (WalletConnect.Instance.SignClient.PendingSessionRequests.Length == 0)
            {
                // Normal connection.
                Debug.Log("No pending session requests. Connecting...");
                var dappConnectOptions = new WalletConnectModalOptions
                {
                    ConnectOptions = BuildConnectOptions()
                };

                WalletConnectModal.Open(dappConnectOptions);
            }
            else
            {
                Debug.Log("SignClient has some pending requests");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Debug.LogWarning("BUG: Pending session requests error inside SignClient. Disposing requests...");

            OnConnectionError?.Invoke("Known bug error.");
        }
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
            
            var desiredChainId = GameConstants.GameChainId; // BEAM network chain ID
            
            // Get ActiveSession namespace
            //var currentNamespace = WalletConnect.Instance.ActiveSession.Namespaces.FirstOrDefault();
            var currentChainId = GetChainId(); // Implement this method to get the current chain ID
            var currentFullChainId = Chain.EvmNamespace + ":" + currentChainId;
            
            if (currentChainId != desiredChainId)
            {
                Debug.LogWarning($"Wrong network. Please switch your wallet to the correct network. Chain ID should be {desiredChainId}");
                var switched = await SwitchToBeamNetwork(currentFullChainId);

                if (!switched)
                {
                    Debug.LogError("Failed switching to BEAM network.");
                    var added = await AddBeamNetwork(currentFullChainId);

                    if (!added)
                    {
                        var message = "Failed to switch to BEAM network and add BEAM network";
                        Debug.Log(message);
                        return null;
                    }
                    // This means we added BEAM correctly, we can go ahead.
                }
                // This means we switched to BEAM correctly, we can go ahead.
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
            
            await UniTask.Delay(1500);
            
            /* TODO not using it
            // Let's update the session with Beam network
            var updated = await AddBeamNetworkToSession();
            if (!updated) return null;
            */
            
            // Send the transaction
            //var signClient = WalletConnect.Instance.SignClient;
            var txHash = await WalletConnect.Instance.RequestAsync<WCEthSendTransaction, string>(ethSendTransaction);
            
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
    private void OnSessionConnected_Handler(object sender, SessionStruct? session)
    {
        Debug.Log("WC SESSION CONNECTED");
        connectedSession = session;
        OnConnected?.Invoke(connectedSession);
    }
    
    private void OnSessionDisconnected_Handler(object sender, EventArgs eventArgs)
    {
        Debug.LogWarning("WC SESSION DISCONNECTED");
        connectedSession = null;
        OnDisconnected?.Invoke();
    }
    
    private void ConnectionError_Handler(object sender, EventArgs eventArgs)
    {
        Debug.Log("WC SESSION CONNECTION ERROR");
        // No need for real disconnection as we're not connected yet.
        connectedSession = null;
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
        var currentAddress = connectedSession.GetValueOrDefault().CurrentAddress(Chain.EvmNamespace);
        return currentAddress.Address;
    }
    
    private int? GetChainId()
    {
        var defaultChain = connectedSession.GetValueOrDefault().Namespaces.Keys.FirstOrDefault();
    
        if (string.IsNullOrWhiteSpace(defaultChain))
            return null;

        var defaultNamespace = connectedSession.GetValueOrDefault().Namespaces[defaultChain];
    
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
    
    private async UniTask<bool> AddBeamNetwork(string currentFullChainId)
    {
        try
        {
            var addChainParams = new
            {
                chainId = GameConstants.GameChainIdHex,
                chainName = "Beam",
                rpcUrls = new [] {"https://build.onbeam.com/rpc"},
                nativeCurrency = new
                {
                    symbol = "BEAM",
                    decimals = 18
                },
                blockExplorerUrls = new [] {"https://subnets.avax.network/beam"}
            };
            var addChainRequest = new WCAddEthereumChain(addChainParams);
            
            var signClient = WalletConnect.Instance.SignClient;
            // Request to switch the Ethereum chain
            //TODO big change
            var result = await WalletConnect.Instance.RequestAsync<WCAddEthereumChain, object>(addChainRequest);

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
    
    private async UniTask<bool> SwitchToBeamNetwork(string currentFullChainId)
    {
        try
        {
            var chainIdData = new { chainId = GameConstants.GameChainIdHex }; // Desired chain ID in hexadecimal
            var switchChainRequest = new WCSwitchEthereumChain(chainIdData);
            
            var signClient = WalletConnect.Instance.SignClient;
            // Request to switch the Ethereum chain
            //TODO big change!
            var result = await WalletConnect.Instance.RequestAsync<WCSwitchEthereumChain, object>(switchChainRequest);

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
            "wallet_switchEthereumChain",
            "wallet_addEthereumChain"
        };

        var events = new string[]
        {
            "chainChanged", "accountsChanged"
        };
        
        requiredNamespaces.Add(Chain.EvmNamespace, new ProposedNamespace()
        {
            Chains = new []{"eip155:4337"},
            Events = events,
            Methods = methods
        });

        return new ConnectOptions
        {
            RequiredNamespaces = requiredNamespaces
        };
    }
    
    private async UniTask<bool> AddBeamNetworkToSession()
    {
        var namespaces = new Namespaces();
        
        var methods = new string[]
        {
            "eth_sendTransaction",
            "eth_signTransaction",
            "eth_sign",
            "personal_sign",
            "eth_signTypedData",
            "wallet_switchEthereumChain",
            "wallet_addEthereumChain"
        };

        var events = new string[]
        {
            "chainChanged", "accountsChanged"
        };

        var connectedAddress = GetConnectedAddress();
        
        namespaces.Add(Chain.EvmNamespace, new Namespace()
        {
            Chains = new []{$"eip155:{GameConstants.GameChainId}"},
            Events = events,
            Methods = methods,
            Accounts = new []{$"eip155:1:{connectedAddress}"}
        });

        try
        {
            var result = await WalletConnect.Instance.SignClient.UpdateSession(WalletConnect.Instance.ActiveSession.Topic,
                new Namespaces(namespaces));

            // We updated the session and added Beam network to namespaces.
            Debug.Log("Updated");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
    #endregion
}
