import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";
import Openfort from "@openfort/openfort-node";
import { ethers } from "ethers";

const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID;
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY;

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

    //TODO Set PlayFab player data with some of the verified data!
    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

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
    const _receiver = resultData["OFplayer"].Value;

    const balance = await openfort.inventories.getPlayerCryptoCurrencyInventory({
      playerId: _receiver,
      chainId: 43113,
    });

    const goldTokenAddress = process.env.GOLD_CONTRACT_ADDRESS.toLowerCase();

    let currencyBalance: any = balance.data.find(
      (tokenAsset) => tokenAsset.address.toLowerCase() === goldTokenAddress
    );

    if (currencyBalance) {
      let amountInSmallestUnit = ethers.BigNumber.from(currencyBalance.amount);
      let amountInUnit = ethers.utils.formatUnits(amountInSmallestUnit, 18);
      currencyBalance.amount = amountInUnit;
    }

    context.log("API call was successful.");
    context.res = {
      status: 200,
      body: currencyBalance,
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
