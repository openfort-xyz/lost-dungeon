import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import { PlayFabServer } from "playfab-sdk";
import axios from 'axios';

const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID;
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY;

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

    //TODO Set PlayFab player data with some of the verified data!
    PlayFabServer.settings.titleId = PlayFabTitleId;
    PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

    context.log("HTTP trigger function processed a request.");

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
    const _receiver = resultData["address"].Value;

    const balance = await axios.get(`https://glacier-api.avax.network/v1/chains/4337/addresses/${_receiver}/balances:listErc1155`);
    
    if(balance.data.error) {
      context.res = {
        status: 500,
        body: JSON.stringify(balance.data.error),
      };
      return;
    }
    const weaponTokenAddress = process.env.WEAPON_ADDRESS_CONTRACT.toLowerCase();

    const weaponBalance = balance.data.erc1155TokenBalances.filter((tokenBalance: any) => tokenBalance.address.toLowerCase() === weaponTokenAddress);


    context.log("BALANCE: " + weaponBalance);

    context.log("API call was successful.");
    context.res = {
      status: 200,
      body: JSON.stringify({ data: weaponBalance })
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
