using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;

public static partial class AzureFunctionCaller
{
    public static Action onMintCurrencySuccess;

    #region FUNCTIONS
    public static void MintCurrency(string coinAmount)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId, //Get this from when you logged in,
                Type = PlayFabSettings.staticPlayer.EntityType, //Get this from when you logged in
            },
            FunctionName = "MintCurrency",
            FunctionParameter = new Dictionary<string, object>() { { "coins", coinAmount } }, //This is the data that you would want to pass into your function.            FunctionParameter = new Dictionary<string, object>() { { "offerId", offer.Id.ToString() } },

            GeneratePlayStreamEvent = true,
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnMintCurrencySuccess, OnRequestFailure);
    }
    #endregion
    
    #region CALLBACK_HANDLERS
    private static void OnMintCurrencySuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;
        
        var functionResult = result.FunctionResult.ToString();
        if (string.IsNullOrEmpty(functionResult))
        {
            onRequestFailure?.Invoke();
            return;
        }

        onMintCurrencySuccess?.Invoke();
    }
    #endregion
}
