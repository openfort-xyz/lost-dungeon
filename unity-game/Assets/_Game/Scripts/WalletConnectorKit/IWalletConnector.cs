using System;
using Cysharp.Threading.Tasks;

public interface IWalletConnector
{
    void Initialize();
    void Connect();
    void Disconnect();
    UniTask<string> Sign(string message, string address);
    UniTask<string> AcceptOwnership(string contractAddress, string newOwnerAddress);
    UniTask<string> GetConnectedAddress();
    UniTask<int?> GetChainId();

    // Define events that the connector can raise
    event Action OnInitialized;
    event Action OnInitializationError;
    
    event Action OnConnected;
    event Action<string> OnConnectionError;
    event Action<string> OnDisconnected;
}

