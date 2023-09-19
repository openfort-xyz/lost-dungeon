import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort, {
  CreateTransactionIntentRequest,
  Interaction,
} from "@openfort/openfort-node";
import { PlayFabServer } from "playfab-sdk";

const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID;
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY;

const openfort = new Openfort(process.env.OPENFORT_API_KEY);

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    if (
      !req.body ||
      !req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId ||
      !req.body.FunctionArgument.offerId
    ) {
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    const offerId = req.body.FunctionArgument.offerId;

    //TODO Set PlayFab player data with some of the verified data!
    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

    const getUserReadOnlyData = {
      PlayFabId: req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId,
    };

    const result: any = await new Promise((resolve, reject) => {
      PlayFabServer.GetUserReadOnlyData(
        getUserReadOnlyData,
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
    const resultData = result.data.Data;
    const currencyAddress = process.env.GOLD_CONTRACT_ADDRESS;
    const weaponAddress = process.env.OF_WEAPON_CONTRACT;

    const _proof = [
      "0x0000000000000000000000000000000000000000000000000000000000000000",
    ];
    const _quantityLimitPerWallet = 1;
    const _receiver = resultData["OFplayer"].Value;
    const _tokenId = Number(offerId);
    const _quantity = 1;

    const _pricePerItem = {
      "0": "1000000000000000000",
      "1": "10000000000000000000",
      "2": "20000000000000000000",
      "3": "30000000000000000000",
      "4": "40000000000000000000",
      "5": "50000000000000000000",
    };

    const _data = "0x";

    const _allowlistProof = [
      _proof,
      _quantityLimitPerWallet,
      _pricePerItem[offerId],
      currencyAddress,
    ];

    const interaction_1: Interaction = {
      contract: process.env.OF_GOLD_CONTRACT,
      functionName: "approve",
      functionArgs: [weaponAddress, _pricePerItem[offerId]],
    };
    const interaction_2: Interaction = {
      contract: process.env.OF_WEAPON_CONTRACT,
      functionName: "claim",
      value: "0",
      functionArgs: [
        _receiver,
        _tokenId,
        _quantity,
        currencyAddress,
        _pricePerItem[offerId],
        _allowlistProof,
        _data,
      ],
    };

    context.log("OFFER ID: " + offerId);
    context.log("MasterPlayerAccountId: " + req.body.CallerEntityProfile.Lineage.MasterPlayerAccountId);
    context.log("currencyAddress: " + currencyAddress);
    context.log("weaponAddress: " + weaponAddress);
    context.log("_receiver: " + _receiver);
    context.log("_tokenId: " + _tokenId);
    context.log("_pricePerItem[offerId]: " + _pricePerItem[offerId]);
    context.log("OF_GOLD_CONTRACT: " + process.env.OF_GOLD_CONTRACT);
    context.log("OF_WEAPON_CONTRACT: " + process.env.OF_WEAPON_CONTRACT);

    const transactionIntentRequest: CreateTransactionIntentRequest = {
      player: _receiver,
      chainId: 43113,
      optimistic: true,
      interactions: [interaction_1, interaction_2],
      policy: process.env.OF_TX_SPONSOR,
    };
    const transactionIntent = await openfort.transactionIntents.create(
      transactionIntentRequest
    );

    if (!transactionIntent) return;
    let userOpHash;
    if (transactionIntent.nextAction) {
      userOpHash = transactionIntent.nextAction.payload.userOpHash ?? undefined;
    } else {
      userOpHash = undefined;
    }

    context.log("API call was successful.");
    context.res = {
      status: 200,
      body: JSON.stringify({
        id: transactionIntent.id,
        userOpHash: userOpHash,
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
