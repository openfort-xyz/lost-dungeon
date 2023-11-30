mergeInto(LibraryManager.library, {
  
  InitializeWeb3: function () {
    if (typeof window.ethereum !== 'undefined') {
      // MetaMask is installed
      SendMessage('WalletConnectorKit', 'OnWeb3Initialized');
    } else {
      // MetaMask is not installed
      SendMessage('WalletConnectorKit', 'OnWeb3InitializeError', 'Injected wallet not installed');
    }
  },
  
  ConnectToWeb3: function () {
    if (window.ethereum) {
      window.ethereum.request({ method: 'eth_requestAccounts' })
      .then(function(accounts) {
        SendMessage('WalletConnectorKit', 'OnWeb3Connected', accounts[0]);
      })
      .catch(function(error) {
        SendMessage('WalletConnectorKit', 'OnWeb3ConnectError', error.message);
      });
    } else {
      SendMessage('WalletConnectorKit', 'OnWeb3ConnectError', 'Ethereum not found');
    }
  },

  PersonalSign: function (messagePtr, accountPtr) {
    var message = UTF8ToString(messagePtr);
    var account = UTF8ToString(accountPtr);
    if (window.ethereum) {
      window.ethereum.request({
        method: 'personal_sign',
        params: [message, account]
      })
      .then(function(signature) {
        SendMessage('WalletConnectorKit', 'OnPersonalSign', signature);
      })
      .catch(function(error) {
        SendMessage('WalletConnectorKit', 'OnPersonalSignError', error.message);
      });
    } else {
      SendMessage('WalletConnectorKit', 'OnPersonalSignError', 'Ethereum not found');
    }
  },

  AcceptOwnership: function(contractAddressPtr, newOwnerAddressPtr) {
    var contractAddress = UTF8ToString(contractAddressPtr);
    var newOwnerAddress = UTF8ToString(newOwnerAddressPtr);

    if (window.ethereum) {
        // Define the desired chain ID --> BEAM!
        var desiredChainId = '0x10F1'; // Hexadecimal representation of 4337

        // Chain data for chain 4337
        var chainData = {
            chainId: desiredChainId,
            chainName: 'Beam Mainnet',
            nativeCurrency: {
                name: 'Beam',
                symbol: 'BEAM', // Typically 2-4 characters
                decimals: 18
            },
            rpcUrls: ['https://subnets.avax.network/beam/mainnet/rpc'],
            blockExplorerUrls: ['https://subnets.avax.network/beam']
        };

        // Function to add the desired chain
        function addDesiredChain() {
            return window.ethereum.request({
                method: 'wallet_addEthereumChain',
                params: [chainData]
            });
        }

        // Function to switch to the desired chain
        function switchToDesiredChain() {
            return window.ethereum.request({
                method: 'wallet_switchEthereumChain',
                params: [{ chainId: desiredChainId }],
            });
        }

        // Function to send the transaction
        function sendTransaction() {
            // Method ID for 'acceptOwnership()'
            var methodId = '0x79ba5097';

            // Constructing the transaction data
            var txData = {
                from: newOwnerAddress,
                to: contractAddress,
                data: methodId,
                gas: '0xFDE8', // Hex value for 65,000 gas limit
            };

            window.ethereum.request({
                method: 'eth_sendTransaction',
                params: [txData]
            })
            .then(function(txHash) {
                SendMessage('WalletConnectorKit', 'OnAcceptOwnershipSuccess', txHash);
            })
            .catch(function(error) {
                SendMessage('WalletConnectorKit', 'OnAcceptOwnershipError', error.message);
            });
        }

        // Check the current chain and switch or add if necessary
        window.ethereum.request({ method: 'eth_chainId' })
            .then(currentChainId => {
                if (currentChainId !== desiredChainId) {
                    switchToDesiredChain()
                        .then(() => sendTransaction())
                        .catch(() => {
                            // Attempt to add the chain if switch fails
                            addDesiredChain()
                                .then(() => switchToDesiredChain())
                                .then(() => sendTransaction())
                                .catch(error => SendMessage('WalletConnectorKit', 'OnAcceptOwnershipError', error.message));
                        });
                } else {
                    sendTransaction();
                }
            })
            .catch(error => SendMessage('WalletConnectorKit', 'OnAcceptOwnershipError', error.message));
    } else {
        SendMessage('WalletConnectorKit', 'OnAcceptOwnershipError', 'Ethereum not found');
    }
  },

  GetConnectedAddress: function () {
    if (window.ethereum) {
      window.ethereum.request({ method: 'eth_accounts' })
      .then(function(accounts) {
        if (accounts.length > 0) {
          const address = accounts[0];
          SendMessage('WalletConnectorKit', 'OnAddressRetrieved', address);
        } else {
          SendMessage('WalletConnectorKit', 'OnAddressError', 'No accounts connected');
        }
      })
      .catch(function(error) {
        SendMessage('WalletConnectorKit', 'OnAddressError', error.message);
      });
    } else {
      SendMessage('WalletConnectorKit', 'OnAddressError', 'Ethereum not found');
    }
  },

  GetChainId: function () {
    if (window.ethereum) {
      window.ethereum.request({ method: 'eth_chainId' })
      .then(function(chainId) {
        // Convert the chain ID to decimal format if it's in hexadecimal
        if (chainId.startsWith('0x')) {
          chainId = parseInt(chainId, 16).toString();
        }
        SendMessage('WalletConnectorKit', 'OnChainIdRetrieved', chainId);
      })
      .catch(function(error) {
        SendMessage('WalletConnectorKit', 'OnChainIdError', error.message);
      });
    } else {
      SendMessage('WalletConnectorKit', 'OnChainIdError', 'Ethereum not found');
    }
  },

  DisconnectFromWeb3: function () {
    // If there is an active Ethereum provider
    if (window.ethereum) {
      // Simply notify Unity that the disconnect is successful,
      // since there is no direct method to log out or disconnect.
      SendMessage('WalletConnectorKit', 'OnWeb3Disconnected', 'Successfully disconnected');
    } else {
      SendMessage('WalletConnectorKit', 'OnWeb3DisconnectError', 'Ethereum not found');
    }
  }
});
