import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";

const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID;
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY;

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    if (
      !req.body ||
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
      !req.body.FunctionArgument
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    context.log("HTTP trigger function processed a request.");
    context.log("MasterPlayerAccountId: " + req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId);
    context.log("FunctionArgument: " + req.body.FunctionArgument)

    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

    // Preparing request
    var updateUserDataRequest = {
      PlayFabId: req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId,
      Data: req.body.FunctionArgument, // Assign the guestUserData directly
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
      body: "User data updated successfully",
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