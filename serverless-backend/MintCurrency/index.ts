import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";
import Openfort, { CreateTransactionIntentRequest, Interaction } from "@openfort/openfort-node";
import { ethers } from "ethers";

const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID;
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY;
const DeveloperAccount = "dac_a421f3ec-b640-4869-8a69-dc6ee0fad956"

const openfort = new Openfort(process.env.OPENFORT_API_KEY);


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
    const coinsWEI = ethers.utils.parseUnits(coins, 'ether')

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

    const interaction: Interaction = {
      contract: process.env.OF_GOLD_CONTRACT,
      functionName: "transfer",
      functionArgs: [_receiver, coinsWEI],
    };
    const transactionIntentRequest: CreateTransactionIntentRequest = {
      account: DeveloperAccount,
      chainId: 4337,
      optimistic: false,
      interactions: [interaction],
      policy: process.env.OF_TX_SPONSOR,
    };
    
    const transactionIntent = await openfort.transactionIntents.create(
      transactionIntentRequest
    );


    context.log("API call was successful.");
    context.res = {
      status: 200,
      body: transactionIntent.response.transactionHash,
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
