import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";
import Openfort from "@openfort/openfort-node";

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
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
      !req.body.FunctionArgument.transactionIntentId
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

    const transactionIntentId = req.body.FunctionArgument.transactionIntentId;

    context.log("HTTP trigger function processed a request.");

    const transactionIntent = await openfort.transactionIntents
      .get({
        id: transactionIntentId,
      })
      .catch((error) => {
        context.log(error);
        context.res = {
          status: 500,
          body: JSON.stringify(error),
        };
        return;
      });
    if (!transactionIntent) return;

    context.res = {
      status: 200,
      body: JSON.stringify({
        minted: transactionIntent.response?.status ? true : false,
      }),
    };

    context.log("API call was successful.");
  } catch (error) {
    context.log(error);
    context.res = {
      status: 500,
      body: JSON.stringify(error),
    };
  }
};

export default httpTrigger;
