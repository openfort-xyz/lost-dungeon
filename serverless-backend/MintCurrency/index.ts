import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";
import { ethers } from "ethers";

const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID;
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY;
const GasWalletPK = process.env.GAS_WALLET_SECRET_KEY;

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    if (
      !req.body ||
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
      !req.body.FunctionArgument.coins
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    //TODO Set PlayFab player data with some of the verified data!
    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

    const coins = req.body.FunctionArgument.coins;

    context.log("HTTP trigger function processed a request.");

    const getUserReadOnlyData = {
      PlayFabId: req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId,
    };

    const result: any = await new Promise((resolve, reject) => {
      PlayFabServer.GetUserReadOnlyData(
        getUserReadOnlyData,
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
    const resultData = result.data.Data;
    const _receiver = resultData["address"].Value;

    // Transfer one token to the player
    const provider = new ethers.providers.JsonRpcProvider(
      "https://subnets.avax.network/beam/mainnet/rpc"
    );
    const tankSigner = new ethers.Wallet(GasWalletPK, provider);
    // transfer 1 token to the player
    const goldContract = new ethers.Contract(
      process.env.GOLD_CONTRACT_ADDRESS,
      ["function transfer(address to, uint256 amount) public returns (bool)"],
      tankSigner
    );
    const tx = await goldContract
      .transfer(_receiver, ethers.utils.parseEther(coins))
      .catch((error) => {
        context.log(error);
        context.res = {
          status: 500,
          body: JSON.stringify(error),
        };
        return;
      });
    const tx_res = await tx.wait();

    context.log("API call was successful.");
    context.res = {
      status: 200,
      body: tx_res,
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
