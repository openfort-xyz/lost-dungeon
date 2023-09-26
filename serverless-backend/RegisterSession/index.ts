import { AzureFunction, Context, HttpRequest } from "@azure/functions";
import Openfort from "@openfort/openfort-node";
import { PlayFabServer } from "playfab-sdk";

// Moved outside the function
const PlayFabTitleId = process.env.PLAYFAB_TITLE_ID || '';
const PlayFabDeveloperKey = process.env.PLAYFAB_DEV_SECRET_KEY || '';
const openfortApiKey = process.env.OPENFORT_API_KEY || '';

if (!PlayFabTitleId || !PlayFabDeveloperKey || !openfortApiKey) {
  throw new Error("Essential environment variables are not set");
}

PlayFabServer.settings.titleId = PlayFabTitleId;
PlayFabServer.settings.developerSecretKey = PlayFabDeveloperKey;

const openfort = new Openfort(openfortApiKey);

const httpTrigger: AzureFunction = async function (
  context: Context,
  req: HttpRequest
): Promise<void> {
  try {
    const masterPlayerAccountId = req?.body?.CallerEntityProfile?.Lineage?.MasterPlayerAccountId;
    const sessionAddress = req?.body?.FunctionArgument?.sessionAddress;
    const playerId = req?.body?.FunctionArgument?.playerId;

    if (typeof masterPlayerAccountId !== 'string' || typeof sessionAddress !== 'string' || typeof playerId !== 'string') {
      context.log('Invalid request body');
      context.res = {
        status: 400,
        body: "Please pass a valid request body",
      };
      return;
    }

    context.log(masterPlayerAccountId);
    context.log(sessionAddress);
    context.log(playerId);

    const OFsession = await openfort.sessions
      .create({
        player: playerId,
        address: sessionAddress,
        chainId: 4337,
        policy: process.env.OF_TX_SPONSOR || '',
        validAfter: 0,
        validUntil: Math.floor(Date.now() / 1000) + 60 * 60 * 24 * 7,
      })
      .catch((error) => {
        context.log(`Openfort session creation failed: ${error}`);
        context.res = {
          status: 500,
          body: "Failed to create Openfort session",
        };
        return null;
      });

    if (!OFsession) {
      context.res = {
        status: 500,
        body: "Openfort session creation did not return a result",
      };
      return;
    }

    let userOpHash = OFsession.nextAction?.payload?.userOpHash;

    context.res = {
      status: 200,
      body: JSON.stringify({ id: OFsession.id, userOpHash }),
    };
  } catch (error) {
    context.log(`Unexpected error: ${error}`);
    context.res = {
      status: 500,
      body: "An unexpected error occurred",
    };
  }
};

export default httpTrigger;