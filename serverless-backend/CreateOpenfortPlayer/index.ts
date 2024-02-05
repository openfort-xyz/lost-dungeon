import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort, { CreateTransactionIntentRequest, Interaction } from "@openfort/openfort-node";
import { PlayFabServer } from "playfab-sdk";

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
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    context.log("HTTP trigger function processed a request.");

    const OFaccount = await openfort.accounts
      .create({
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

    const amountTokenGold = "1000000000000000000"; // 1 Gold Token
  
    const interaction: Interaction = {
      contract: process.env.OF_GOLD_CONTRACT,
      functionName: "transfer",
      functionArgs: [OFaccount.id, amountTokenGold],
    };
    const transactionIntentRequest: CreateTransactionIntentRequest = {
      account: DeveloperAccount,
      chainId: 4337,
      optimistic: true,
      interactions: [interaction],
      policy: process.env.OF_TX_SPONSOR,
    };

    //TODO Set PlayFab player data with some of the verified data!
    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;



    await openfort.transactionIntents.create(transactionIntentRequest)

    var updateUserDataRequest = {
      PlayFabId: req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId,
      Data: {
        OFplayer: OFaccount.player.id,
        address: OFaccount.address,
        custodial: "true"
      },
    };
    await PlayFabServer.UpdateUserReadOnlyData(updateUserDataRequest, (error, result) => {
        if (error) {
          context.res = {
            status: 500,
            body: JSON.stringify(error),
          };
        }
    })

    context.log("API call was successful.");
    context.res = {
      status: 200,
      body: JSON.stringify({
        address: OFaccount.address,
        short_address:
          OFaccount.address?.substr(0, 5) +
          "..." +
          OFaccount.address?.substr(-4),
      }),
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
