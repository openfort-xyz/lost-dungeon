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
      !req.body.FunctionArgument.offerId
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    const offerId = req.body.FunctionArgument.offerId;

    //TODO Set PlayFab player data with some of the verified data!
    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

    context.log("HTTP trigger function processed a request.");

    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

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
    const _receiver: string = resultData["OFplayer"].Value;

    const player = await openfort.players
      .get({ id: _receiver, expand: ["transactionIntents"] })
      .catch((error) => {
        context.log(error);
        context.res = {
          status: 500,
          body: JSON.stringify(error),
        };
        return;
      });
    const transactionIntents = player["transactionIntents"];
    if (!transactionIntents || transactionIntents.length === 0) return;

    const interactions = await transactionIntents[0].interactions;
    // check that there are two interactions (approve and claim)
    if (!interactions || interactions.length !== 2) return;

    // check that the functionName in interactions[1] includes 'claim'
    if (
      !interactions[1].functionName ||
      !interactions[1].functionName.includes("claim")
    )
      return;

    // Check that the tokenId in interactions[1] matches the offerId
    if (
      !interactions[1].functionArgs ||
      interactions[1].functionArgs[1] !== offerId
    )
      return;

    let userOpHash;
    if (transactionIntents[0].nextAction) {
      userOpHash =
        transactionIntents[0].nextAction.payload.userOpHash ?? undefined;
    } else {
      userOpHash = undefined;
    }

    // Check that
    context.res = {
      status: 200,
      body: JSON.stringify({
        id: transactionIntents[0].id,
        userOpHash: userOpHash,
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
