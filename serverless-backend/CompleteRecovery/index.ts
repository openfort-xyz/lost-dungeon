import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort, { CompleteRecoveryRequest } from "@openfort/openfort-node";

const openfort = new Openfort(process.env.OPENFORT_API_KEY);

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
  
      context.log("API call was successful.");
      context.res = {
        status: 200,
        body: transactionIntent.response.transactionHash,
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

