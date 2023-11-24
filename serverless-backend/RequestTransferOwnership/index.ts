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
    const newOwnerAddress = req.body.FunctionArgument.newOwnerAddress;

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

      const transferResponse = await openfort.players.transferAccountOwnership(
        {
          playerId: playerId,
          policy: OF_TX_SPONSOR,
          chainId: 4337,
          newOwnerAddress: newOwnerAddress,
        }
      )

      // Extracting 'id' and 'userOperationHash' from transferResponse
      const transferId = transferResponse.id;
      const userOperationHash = transferResponse.userOperationHash;

      context.res = {
        status: 200,
        body: JSON.stringify({
          id: transferId,
          userOpHash: userOperationHash,
        })
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
