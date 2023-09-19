import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";

// Initialize PlayFab settings
const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID || '';
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY || '';
PlayFabServer.settings.titleId = PlayFabTitleId;
PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    // Check for PlayFab settings
    if (!PlayFabTitleId || !PlayFabDeveloperKey) {
      context.res = {
        status: 500,
        body: "PlayFab settings are not initialized",
      };
      return;
    }

    // Check for request body and its nested properties
    const masterPlayerAccountId = req?.body?.CallerEntityProfile?.Lineage?.MasterPlayerAccountId;

    if (typeof masterPlayerAccountId !== 'string') {
      context.log('Invalid request body');
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    // Delete OFsession key from PlayFab user data
    const updateUserDataRequest = {
      PlayFabId: masterPlayerAccountId,
      KeysToRemove: ["OFsession"] // Specify the keys to remove
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

    if (!result) {
      context.res = {
        status: 500,
        body: "API call did not return a result",
      };
      return;
    }

    context.res = {
      status: 200,
      body: "Keys Web3AuthCompletedOnce and OFsession have been deleted!",
    };
  } catch (error) {
    context.log(`Unexpected error: ${error}`);
    context.res = {
      status: 500,
      body: "An unexpected error occurred",
    };
  }
};

export default httpTrigger;
