using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WalletConnect;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Sign.Models.Engine.Events;
using WalletConnectUnity.Demo.SimpleSign;
using WalletConnectUnity.Utils;

public class WalletConnectController : MonoBehaviour
{
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
            });
        }
        catch (Exception e)
        {
            Debug.LogError(("Connection failed: " + e.Message));
            Debug.LogError(e);
        }
    }
}
