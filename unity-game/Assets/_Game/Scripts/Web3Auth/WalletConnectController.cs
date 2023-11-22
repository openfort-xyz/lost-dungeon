using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using WalletConnect;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Sign.Models.Engine.Events;
using WalletConnectUnity.Demo.SimpleSign;
using WalletConnectUnity.Utils;

public class WalletConnectController : MonoBehaviour
{
    public event UnityAction<SessionStruct> OnConnected;
    
    [SerializeField] private WCSignClient WC;
    
    [SerializeField] private bool autoDisconnect;
    
    [HideInInspector] public SessionStruct CurrentSession;
    
    private void Start()
    {
        WC.OnSessionApproved += WCOnOnSessionApproved;
        WC.SessionDeleted += WCOnSessionDeleted;
    }

    private void OnApplicationQuit()
    {
        if (autoDisconnect)
        {
            WC.Disconnect(CurrentSession.Topic);   
        }
    }

    private void OnDisable()
    {
        WC.OnSessionApproved -= WCOnOnSessionApproved;
        WC.SessionDeleted -= WCOnSessionDeleted;
    }

    private void WCOnOnSessionApproved(object sender, SessionStruct e) => MTQ.Enqueue(() =>
    {
        //TODO Estem connectats!!!
        Debug.LogWarning("WC SESSION APPROVED");
        //Session = e;
    });

    private void WCOnSessionDeleted(object sender, SessionEvent e) => MTQ.Enqueue(() =>
    {
        //TODO
        Debug.LogWarning("WC SESSION DELETED");
    });
    
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
}
