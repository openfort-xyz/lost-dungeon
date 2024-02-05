import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";
import Openfort, { CreateTransactionIntentRequest, Interaction } from "@openfort/openfort-node";
import { recoverPersonalSignature } from "@metamask/eth-sig-util";

const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID;
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY;
const DeveloperAccount = "dac_a421f3ec-b640-4869-8a69-dc6ee0fad956"

const openfort = new Openfort(process.env.OPENFORT_API_KEY);

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    if (
      !req.body ||
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
      !req.body.FunctionArgument.signature ||
      !req.body.FunctionArgument.message
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    const message = req.body.FunctionArgument.message;
    const signature = req.body.FunctionArgument.signature;

    context.log("HTTP trigger function processed a request.");

    // Recover the address of the account used to create the given Ethereum signature.
    const address = recoverPersonalSignature({
      data: message,
      signature: signature,
    });

    const OFaccount = await openfort.accounts
      .create({
        externalOwnerAddress: address,
        chainId: 4337,
      })
      .catch((error) => {
        context.log(error);
        context.res = {
          status: 500,
          body: JSON.stringify(error),
        };
        return;
      });

    if (!OFaccount) return;

    // Transfer one token to the player
    const interaction: Interaction = {
      contract: process.env.OF_GOLD_CONTRACT,
      functionName: "transfer",
      functionArgs: [OFaccount.id, "1000000000000000000"],
    };
    const transactionIntentRequest: CreateTransactionIntentRequest = {
      account: DeveloperAccount,
      chainId: 4337,
      optimistic: true,
      interactions: [interaction],
      policy: process.env.OF_TX_SPONSOR,
    };
    
    await openfort.transactionIntents.create(
      transactionIntentRequest
    );

    //TODO Set PlayFab player data with some of the verified data!
    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

    // Preparing request
    var updateUserDataRequest = {
      PlayFabId: req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId,
      Data: {
        OFplayer: OFaccount.player.id,
        address: OFaccount.address,
        ownerAddress: OFaccount.ownerAddress,
      },
    };

    await new Promise((resolve, reject) => {
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

    context.log("API call was successful.");
    context.res = {
      status: 200,
      body: "tx_res",
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
