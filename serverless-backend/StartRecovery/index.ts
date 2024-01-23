import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort, { StartRecoveryRequest } from "@openfort/openfort-node";

const openfort = new Openfort(process.env.OPENFORT_API_KEY);

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    if (
      !req.body ||
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
      !req.body.FunctionArgument.accountId ||
      !req.body.FunctionArgument.newOwnerAddress
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    context.log("HTTP trigger function processed a request.");

    const accountId = req.body.FunctionArgument.accountId;
    const newOwnerAddress = req.body.FunctionArgument.newOwnerAddress;
    
    const recoveryRequest: StartRecoveryRequest = {
      accountId: accountId,
      newOwnerAddress: newOwnerAddress,
      policy: process.env.OF_TX_SPONSOR
    };

    const transactionIntent = await openfort.accounts.startRecovery(recoveryRequest);

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
