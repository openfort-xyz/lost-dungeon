using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Openfort.Model;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;

public static partial class AzureFunctionCaller
{
    public static Action<string, string> onRequestTransferOwnershipSuccess;
    public static Action onRequestTransferOwnershipFailure;

    #region FUNCTIONS
    public static void RequestTransferOwnership(string playerId, string newOwnerAddress)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "RequestTransferOwnership",
            FunctionParameter = new Dictionary<string, object>()
            {
                {"playerId", playerId},
                {"newOwnerAddress", newOwnerAddress}
            },

            GeneratePlayStreamEvent = true,
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnRequestTransferOwnershipSuccess, OnRequestTransferOwnershipFailure);
    }

    #endregion
    
    #region CALLBACK_HANDLERS
    private static void OnRequestTransferOwnershipSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;
        
        var functionResult = result.FunctionResult.ToString();
        if (string.IsNullOrEmpty(functionResult))
        {
            onRequestTransferOwnershipFailure?.Invoke();
            return;
        }
        
        //TODO GET things
        AccountResponse accounts = JsonConvert.DeserializeObject<AccountResponse>(functionResult);

        Debug.Log(accounts);
        onRequestTransferOwnershipSuccess?.Invoke(null, null);
    }
    
    private static void OnRequestTransferOwnershipFailure(PlayFabError error)
    {
        error.GenerateErrorReport();
        onRequestTransferOwnershipFailure?.Invoke();
    }
    #endregion
}
