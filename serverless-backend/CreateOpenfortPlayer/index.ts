import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";
import { PlayFabServer } from "playfab-sdk";
import { ethers } from "ethers";

const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID;
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY;
const GasWalletPK = process.env.GAS_WALLET_SECRET_KEY;
const InfuraKey = process.env.INFURA_KEY;

const openfort = new Openfort(process.env.OPENFORT_API_KEY);

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    if (
      !req.body ||
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    context.log("HTTP trigger function processed a request.");

    const OFplayer = await openfort.players
      .create({
        name: req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId,
      })
      .catch((error) => {
        context.log(error);
        context.res = {
          status: 500,
          body: JSON.stringify(error),
        };
        return;
      });
    if (!OFplayer) return;

    const OFaccount = await openfort.accounts
      .create({
        player: OFplayer.id,
        chainId: 43113,
      })
      .catch((error) => {
        context.log(error);
        context.res = {
          status: 500,
          body: JSON.stringify(error),
        };
        return;
      });

    if (!OFaccount) return;
    // Transfer one token to the player
    const provider = new ethers.providers.JsonRpcProvider(
      "https://avalanche-fuji.infura.io/v3/" + InfuraKey
    );
    const tankSigner = new ethers.Wallet(GasWalletPK, provider);
    // transfer 1 token to the player
    const goldAddress = process.env.GOLD_CONTRACT_ADDRESS;
    const goldContract = new ethers.Contract(
      goldAddress,
      ["function transfer(address to, uint256 amount) public returns (bool)"],
      tankSigner
    );
    const tx = await goldContract
      .transfer(OFaccount.address, ethers.utils.parseEther("1"))
      .catch((error) => {
        context.log(error);
        context.res = {
          status: 500,
          body: JSON.stringify(error),
        };
        return;
      });
    setImmediate(() => {
      tx.wait();
    });

    //TODO Set PlayFab player data with some of the verified data!
    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

    // Preparing request
    var updateUserDataRequest = {
      PlayFabId: req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId,
      Data: {
        OFplayer: OFplayer.id,
        address: OFaccount.address,
        custodial: "true"
      },
    };

    const result = await new Promise((resolve, reject) => {
      PlayFabServer.UpdateUserReadOnlyData(
        updateUserDataRequest,
        (error, result) => {
          if (error) {
            reject(error);
          } else {
            resolve(result);
          }
        }
      );
    }).catch((error) => {
      context.log("Something went wrong with the API call.");
      context.res = {
        status: 500,
        body: JSON.stringify(error),
      };
    });

    if (!result) return;

    context.log("API call was successful.");
    context.res = {
      status: 200,
      body: JSON.stringify({
        address: OFaccount.address,
        short_address:
          OFaccount.address?.substr(0, 5) +
          "..." +
          OFaccount.address?.substr(-4),
      }),
    };
  } catch (error) {
    context.log(error);
    context.res = {
      status: 500,
      body: JSON.stringify(error),
    };
  }
};

export default httpTrigger;
