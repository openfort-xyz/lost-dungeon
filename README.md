# Lost Dungeon

Lost Dungeon is a dungeon crawler game built on top of [Openfort](https://openfort.xyz/) to showcase what's possible. It is compatible with Desktop (Windows, Linux, Mac), Android and WebGL.

## Features
- **Gold Coins**: ERC-20 token deployed at [0x658d55C80AB4D153774Fc5F1D08aA396Cc8243B7](https://testnet.snowtrace.io/address/0x658d55C80AB4D153774Fc5F1D08aA396Cc8243B7). These can be obtained after killing a monster and minted to the player when they are killed. Every player is airdropped 1 Gold coin at the beginning of the game.
- **Weapons**: ERC-1155 tokens deployed at [0x898cf2A67E8887d3C69236147a201608565ff3B3](https://testnet.snowtrace.io/address/0x898cf2A67E8887d3C69236147a201608565ff3B3). Users can by them at the shop using Gold coins.
- **Score**: Kept in Metafab as a `uint256` value. Every time a player kills a monster, their score is increased. It is later on used to create a leaderboard.
- **Authentication**: Players can authenticate using email and password or can use a guest account.

This repository is divided into two parts:

- Serverless backend: Azure functions that handle the game logic and communicate with Openfort and Playfab.
- Unity game: The game itself, which is built using Unity.
  
## Serverless backend

### Prerequisites
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=windows%2Ccsharp%2Cbash)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator)

### Setup
1. Install the dependencies by running `npm install` in the `serverless-backend` folder.

For local development, create a file called `local.settings.json` in the `serverless-backed` folder with the following content:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "node",
    "OPENFORT_API_KEY": "sk_test_",
    "PLAYFAB_TITLE_ID": ,
    "PLAYFAB_DEV_SECRET_KEY": ,
    "GAS_WALLET_SECRET_KEY": ,
    "INFURA_KEY": ,
    "GOLD_CONTRACT_ADDRESS": "0x658d55C80AB4D153774Fc5F1D08aA396Cc8243B7",
    "OF_GOLD_CONTRACT": "con_fc4be711-6d1a-40a0-99f8-073e2c21ab43",
    "OF_WEAPON_CONTRACT": "con_7fe12b6a-93a7-4a0c-9e4f-18be57035632",
    "OF_TX_SPONSOR": "pol_b58c1b44-c9ed-4a92-8898-dd13910d5402"
  }
}
```
