using System;
using Cysharp.Threading.Tasks;

public interface IWalletConnector
{
    void Connect();
    void Disconnect();
    UniTask<string> Sign(string message, string address);
    UniTask<string> AcceptOwnership(string contractAddress, string newOwnerAddress);
    UniTask<string> GetConnectedAddress();
    UniTask<int?> GetChainId();
    
    event Action OnConnected;
    event Action<string> OnConnectionError;
    event Action<string> OnDisconnected;
    event Action OnEthereumNotFound; //Only used in WebGL
}

