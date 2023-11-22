using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using WalletConnect;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Sign.Models.Engine.Events;
using WalletConnectUnity.Utils;

public class WalletConnectController : MonoBehaviour
{
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
    
    [SerializeField] private WCSignClient WC;
    
    [SerializeField] private bool autoDisconnect;
    
    [HideInInspector] public SessionStruct CurrentSession;

    #region UNITY_LIFECYCLE
    private void Start()
    {
        WC.OnSessionApproved += WCOnOnSessionApproved;
        WC.SessionDeleted += WCOnSessionDeleted;
    }

    private void OnDisable()
    {
        WC.OnSessionApproved -= WCOnOnSessionApproved;
        WC.SessionDeleted -= WCOnSessionDeleted;
    }
    #endregion

    private void WCOnOnSessionApproved(object sender, SessionStruct e) => MTQ.Enqueue(() =>
    {
        //TODO Estem connectats!!!
        Debug.LogWarning("WC SESSION APPROVED");
        //Session = e;
    });

    private void WCOnSessionDeleted(object sender, SessionEvent e) => MTQ.Enqueue(() =>
    {
        Debug.LogWarning("WC SESSION DELETED");
        OnDisconnected?.Invoke();
    });

    public void Disconnect()
    {
        if (autoDisconnect)
        {
            WC.Disconnect(CurrentSession.Topic);   
        }
    }
    
    public async void Connect()
    {
        if (WC.SignClient == null)
            await WC.InitSignClient();

        if (WC == null)
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

        var connectData = await WC.Connect(dappConnectOptions);
        
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

    #region PRIVATE_METHODS
    private async Task<string> PersonalSignAsync(string message, string address)                                          
    {                                                                                                                                                                                                         
        var fullChainId = Chain.EvmNamespace + ":" + GetChainId(); // Needs to be something like "eip155:80001"

        var hexUtf8 = "0x" + Encoding.UTF8.GetBytes(message).ToHex();                                    
        var request = new PersonalSign(hexUtf8, address);                                        
                                                                                                     
        var result = await WC.Request<PersonalSign, string>(CurrentSession.Topic, request, fullChainId);
                     
        Debug.Log("Got result from request: " + result);
        
        return result;                                                                                 
    }    
    #endregion
}
