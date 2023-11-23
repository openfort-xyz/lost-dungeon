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
      !req.body.FunctionArgument.playerId ||
      !req.body.FunctionArgument.newOwnerAddress
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    const playerId = req.body.FunctionArgument.playerId;

    const accounts = await openfort.accounts
      .get({
        player: playerId
      })
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
      // Retrieve the ID of the account
      const accountId = accounts.data[0].id;

      // TODO: Perform other tasks with the accountId
      // For example, request ownership transfer

      // Example response with account ID
      context.res = {
        status: 200,
        body: { accountId: accountId }
      };

    } else {
      context.res = {
        status: 400,
        body: "There should be exactly one account"
      };
      return;
    }

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
