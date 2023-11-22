using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityBinder;
using UnityEngine;
using UnityEngine.UI;
using WalletConnect;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign.Models;

public class DemoDapp : BindableMonoBehavior
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

    [RpcMethod("eth_getBalance"), RpcRequestOptions(Clock.ONE_MINUTE, 99998)]
    public class EthGetBalance : List<string>
    {
        public EthGetBalance(string address, BigInteger? blockNumber = null) : base(new[]
        {
            address,
            blockNumber != null ? blockNumber.ToString() : "latest"
        })
        {
        }
    }
    
    [Inject]
    private WCSignClient _wc;

    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI addressText;
    [SerializeField] private Button _signButton;
    [SerializeField] private Button _signOutButton;
    
    public const string EthereumChainId = "eip155";

    protected override void Awake()
    {
        base.Awake();
        
        _signButton.onClick.AddListener(Sign);
        _signOutButton.onClick.AddListener(SignOut);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CheckBalanceTask());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public (SessionStruct, string, string) GetAccountInfo()
    {
        var currentSession = _wc.Session.Get(_wc.Session.Keys[0]);

        var defaultChain = currentSession.Namespaces.Keys.FirstOrDefault();
            
        if (string.IsNullOrWhiteSpace(defaultChain))
            return (default, null, null);

        var defaultNamespace = currentSession.Namespaces[defaultChain];

        if (defaultNamespace.Accounts.Length == 0)
            return (default, null, null);
            
        var fullAddress = defaultNamespace.Accounts[0];
        var addressParts = fullAddress.Split(":");
            
        var address = addressParts[2];
        var chainId = string.Join(':', addressParts.Take(2));

        return (currentSession, address, chainId);
    }
    
    public async void Sign()
    {
        string message = $"Holaaaaa";
        var result = await PersonalSignAsync(message);
    }
    
    public async Task<string> PersonalSignAsync(string message)                                          
    {                                                                                                                                                                                                         
        var (session, address, chainId) = GetAccountInfo();

        var hexUtf8 = "0x" + Encoding.UTF8.GetBytes(message).ToHex();                                    
        var request = new PersonalSign(hexUtf8, address);                                        
                                                                                                     
        var result = await _wc.Request<PersonalSign, string>(session.Topic, request, chainId);
                     
        Debug.Log("Got result from request: " + result);
        
        return result;                                                                                 
    }         

    public async void SignOut()
    {
        _signButton.interactable = false;
        _signOutButton.interactable = false;
        
        await _wc.Disconnect();
        
        _signButton.interactable = true;
        _signOutButton.interactable = true;
    }

    private IEnumerator CheckBalanceTask()
    {
        while (this != null)
        {
            yield return new WaitForSeconds(5);
            
            // First check to see if we're connected
            if (!_wc.Core.Relayer.Connected)
                continue;

            if (_wc.Session.Length == 0)
                continue;

            var (_, address, _) = GetAccountInfo();
            if (string.IsNullOrWhiteSpace(address))
                continue;

            addressText.text = $"{string.Concat(address.Take(6))}...{string.Concat(address.TakeLast(4))}";
            
            // Now build the eth_getBalance request
            /*var request = new EthGetBalance(address);

            var resultTask = _wc.Request<EthGetBalance, BigInteger>(currentSession.Topic, request, chainId);

            yield return new WaitForTaskResult<BigInteger>(resultTask);

            var result = resultTask.Result;
            
            // Convert to correct units
            var finalResult = result / BigInteger.Pow(10, 18);

            balanceText.text = $"Balance: {finalResult} ETH";*/
        }
    }
}
