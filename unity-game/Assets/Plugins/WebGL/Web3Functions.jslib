mergeInto(LibraryManager.library, {
  ConnectToWeb3: function () {
    if (window.ethereum) {
      window.ethereum.request({ method: 'eth_requestAccounts' })
      .then(function(accounts) {
        SendMessage('Web3AuthService', 'OnWeb3Connected', accounts[0]);
      })
      .catch(function(error) {
        SendMessage('Web3AuthService', 'OnWeb3ConnectError', error.message);
      });
    } else {
      SendMessage('Web3AuthService', 'OnWeb3ConnectError', 'Ethereum not found');
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
        SendMessage('Web3AuthService', 'OnPersonalSign', signature);
      })
      .catch(function(error) {
        SendMessage('Web3AuthService', 'OnPersonalSignError', error.message);
      });
    } else {
      SendMessage('Web3AuthService', 'OnPersonalSignError', 'Ethereum not found');
    }
  },

  GetConnectedAddress: function () {
    if (window.ethereum) {
      window.ethereum.request({ method: 'eth_accounts' })
      .then(function(accounts) {
        if (accounts.length > 0) {
          const address = accounts[0];
          SendMessage('Web3AuthService', 'OnAddressRetrieved', address);
        } else {
          SendMessage('Web3AuthService', 'OnAddressError', 'No accounts connected');
        }
      })
      .catch(function(error) {
        SendMessage('Web3AuthService', 'OnAddressError', error.message);
      });
    } else {
      SendMessage('Web3AuthService', 'OnAddressError', 'Ethereum not found');
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
        SendMessage('Web3AuthService', 'OnChainIdRetrieved', chainId);
      })
      .catch(function(error) {
        SendMessage('Web3AuthService', 'OnChainIdError', error.message);
      });
    } else {
      SendMessage('Web3AuthService', 'OnChainIdError', 'Ethereum not found');
    }
  },

  DisconnectFromWeb3: function () {
    // If there is an active Ethereum provider
    if (window.ethereum) {
      // Simply notify Unity that the disconnect is successful,
      // since there is no direct method to log out or disconnect.
      SendMessage('Web3AuthService', 'OnWeb3Disconnected', 'Successfully disconnected');
    } else {
      SendMessage('Web3AuthService', 'OnWeb3DisconnectError', 'Ethereum not found');
    }
  }
});
