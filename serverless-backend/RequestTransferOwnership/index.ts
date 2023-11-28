import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";

const OF_TX_SPONSOR = process.env.OF_TX_SPONSOR;
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

    const accountId = req.body.FunctionArgument.accountId;
    const newOwnerAddress = req.body.FunctionArgument.newOwnerAddress;

    context.log("Account ID: " + accountId);
    context.log("New Owner Address: " + newOwnerAddress);

    const account = await openfort.accounts.get({id: accountId});

    if (!account) return;

    const transferResponse = await openfort.accounts.requestTransferOwnership(
      {
        accountId: accountId,
        policy: OF_TX_SPONSOR,
        newOwnerAddress: newOwnerAddress,
      }
    )

    if (!transferResponse) return;

    context.res = {
      status: 200,
      body: JSON.stringify({
        contractAddress: account.address,
        newOwnerAddress: newOwnerAddress,
      })
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
