import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";
import { ethers } from "ethers";

const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID;
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY;
const GasWalletPK = process.env.GAS_WALLET_SECRET_KEY;
const InfuraKey = process.env.INFURA_KEY;

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    if (
      !req.body ||
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
      !req.body.FunctionArgument.address ||
      !req.body.FunctionArgument.chainId
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

    const chainId = req.body.FunctionArgument.chainId;
    const address = req.body.FunctionArgument.address;

    context.log("HTTP trigger function processed a request.");

    context.log("API call was successful.");
    context.res = {
      status: 200,
      body: JSON.stringify({
        address: address,
        chainId: chainId,
        message: "Hello from the backend!",
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
