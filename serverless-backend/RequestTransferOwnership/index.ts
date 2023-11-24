import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort, { AccountResponse } from "@openfort/openfort-node";
import { recoverPersonalSignature } from "@metamask/eth-sig-util";

const OF_TX_SPONSOR = process.env.OF_TX_SPONSOR;
const openfort = new Openfort(process.env.OPENFORT_API_KEY);

function delay(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    if (
      !req.body ||
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
      !req.body.FunctionArgument.message ||
      !req.body.FunctionArgument.signature ||
      !req.body.FunctionArgument.playerId ||
      !req.body.FunctionArgument.newOwnerAddress
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    const message = req.body.FunctionArgument.message;
    const signature = req.body.FunctionArgument.signature;
    const playerId = req.body.FunctionArgument.playerId;
    //const newOwnerAddress = req.body.FunctionArgument.newOwnerAddress;

    // Recover the address of the account used to create the given Ethereum signature.
    const newOwnerAddress = recoverPersonalSignature({
      data: message,
      signature: signature,
    });

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

      let deployedAccount: AccountResponse;

      // Deploy account if not deployed
      if (account.deployed === false)
      {
        deployedAccount = await openfort.accounts.deploy({ id: accountId, policy: OF_TX_SPONSOR });
        if (!deployedAccount) return;
      };

      await delay(500);

      context.log("Deployed Account ID: " + deployedAccount.id);
      context.log("New Owner Address: " + newOwnerAddress);

      /*
      const transferResponse = await openfort.players.requestTransferAccountOwnership(
        {
          playerId: playerId,
          policy: OF_TX_SPONSOR,
          chainId: 4337,
          newOwnerAddress: newOwnerAddress,
        }
      )
      */

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
