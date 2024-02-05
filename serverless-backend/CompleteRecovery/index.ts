import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";
import Openfort, { CompleteRecoveryRequest } from "@openfort/openfort-node";

// Initialize Openfort
const openfort = new Openfort(process.env.OPENFORT_API_KEY);

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
    if (
      !req.body ||
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
      !req.body.FunctionArgument.playerId ||
      !req.body.FunctionArgument.newOwnerAddress
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    context.log("HTTP trigger function processed a request.");

    const playerId = req.body.FunctionArgument.playerId;
    const newOwnerAddress = req.body.FunctionArgument.newOwnerAddress;

    // Get player account
    const accounts = await openfort.accounts.list({ player: playerId })
    .catch((error) => {
      context.log(error);
      context.res = {
        status: 500,
        body: JSON.stringify(error),
      };
      return;
    });

    if (!accounts) return;

    // Check if there is only one account
    if (accounts.data.length === 1) {

      const account = accounts.data[0];

      // Retrieve the ID of the account
      const accountId = account.id;
      context.log("Account ID: " + accountId);

      const recoveryRequest: CompleteRecoveryRequest = {
        accountId: accountId,
        newOwnerAddress: newOwnerAddress,
        policy: process.env.OF_TX_SPONSOR
        //TODO signatures: ?
      };
  
      const transactionIntent = await openfort.accounts.completeRecovery(recoveryRequest);

      if (!transactionIntent) return;

      // Check the status of the transactionIntent
      if (transactionIntent.response.status !== 1) {
        context.log("Lockup period still live. Try again later.");
        context.res = {
          status: 500,
          body: "Lockup period still live. Try again later.",
        };
        return;
      }

      var updateUserDataRequest = {
        PlayFabId: req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId,
        Data: {
          recoveryAddress: null,
          //TODO?? ownerAddress: newOwnerAddress
        }
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
      
      context.log("Recovery process completed successfully.");
      context.res = {
        status: 200,
        body: "Recovery process completed successfully.",
      };

    } else {
      context.res = {
        status: 400,
        body: "There should be exactly one account"
      };
      return;
    }
  } catch (error) {
    context.log(error);
    context.res = {
      status: 500,
      body: JSON.stringify(error),
    };
  }
};

export default httpTrigger;

