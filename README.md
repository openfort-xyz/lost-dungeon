# Lost Dungeon

Lost Dungeon is a dungeon crawler game built on top of [Openfort](https://openfort.xyz/) to showcase what's possible. 
It's built on top of an Avalanche Subnet [Beam](https://www.onbeam.com/).

## How to play

It is compatible with Desktop (Windows, Linux, Mac), Android and WebGL.

- WebGL: [https://lostdungeon.openfort.xyz/](https://lostdungeon.openfort.xyz/)
- Mac Downloader: [Download now](https://lostdungeon.openfort.xyz/assets/downloads/lost-dungeon-mac.zip)
- Linux Downloader: [Download now](https://lostdungeon.openfort.xyz/assets/downloads/lost-dungeon-linux.zip)
- Windows Downloader: [Download now](https://lostdungeon.openfort.xyz/assets/downloads/lost-dungeon-windows.zip)
- Android Play Store: [Download now](https://play.google.com/store/apps/details?id=com.Openfort.LostDungeon&pcampaignid=web_share)

## Functional diagram

![Functional diagram](https://blog-cms.openfort.xyz/uploads/schema_lost_dungeon_3639e1aa09.svg)

## Features
- **Gold Coins**: ERC-20 token deployed at [0x2a8cbec28b8e0e6e6c2ece080c269f5cdf2549ac](https://subnets.avax.network/beam/address/0x2a8cbec28b8e0e6e6c2ece080c269f5cdf2549ac). These can be obtained after killing a monster and minted to the player when they are killed. Every player is airdropped 1 Gold coin at the beginning of the game.
- **Weapons**: ERC-1155 tokens deployed at [0x2b57688397a21c226cbc00dfe0414c4550d7bcc3](https://subnets.avax.network/beam/address/0x2b57688397a21c226cbc00dfe0414c4550d7bcc3). Users can by them at the shop using Gold coins.
- **Score**: Kept in [Playfab](https://playfab.com/) as a `uint256` value. Every time a player kills a monster, their score is increased. It is later on used to create a leaderboard.
- **Authentication**: Players can authenticate using email and password or can use a guest account. Powered by Playfab.

This repository is divided into two parts:

- Serverless backend & Playfab: Azure functions that handle the game logic and communicate with Openfort and Playfab.
- Unity game: The game itself, which is built using Unity.
  
## Serverless functions & Playfab

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
    "OPENFORT_API_KEY": "YOUR_OPENFORT_API_KEY",
    "PLAYFAB_TITLE_ID": "YOUR_PLAYFAB_TITLE_ID",
    "PLAYFAB_DEV_SECRET_KEY": "YOUR_PLAYFAB_DEV_SECRET_KEY",
    "GAS_WALLET_SECRET_KEY": "YOUR_GAS_WALLET_SECRET_KEY",
    "GOLD_CONTRACT_ADDRESS": "0x2a8cbEc28b8e0E6e6C2EcE080C269F5cDF2549Ac",
    "WEAPON_ADDRESS_CONTRACT": "0x2b57688397a21c226cbc00dfe0414c4550d7bcc3",
    "OF_GOLD_CONTRACT": "con_a400a645-06a2-4731-bd95-d3af3a945dd1",
    "OF_WEAPON_CONTRACT": "con_f7e0d688-9ea2-4235-ba75-ce6f0c2e4527",
    "OF_TX_SPONSOR": "pol_e8591dc3-2143-42d7-b6e2-ce171b20bb75"
  }
}
```
