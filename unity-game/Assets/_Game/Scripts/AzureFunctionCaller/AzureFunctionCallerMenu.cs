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
    public static Action<string> onDeployAccountSuccess;
    public static Action<string> onRequestTransferOwnershipSuccess;
    public static Action<PlayFabError> onRequestTransferOwnershipFailure;

    #region FUNCTIONS
    public static void DeployAccount(string playerId)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "DeployAccount",
            FunctionParameter = new Dictionary<string, object>()
            {
                {"playerId", playerId}
            },

            GeneratePlayStreamEvent = true,
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnDeployAccountSuccess, OnRequestFailure);
    }

    public static void RequestTransferOwnership(string accountId, string newOwnerAddress)
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
                {"accountId", accountId},
                {"newOwnerAddress", newOwnerAddress}
            },

            GeneratePlayStreamEvent = true,
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnRequestTransferOwnershipSuccess, OnRequestTransferOwnershipFailure);
    }
    #endregion
    
    #region CALLBACK_HANDLERS
    private static void OnDeployAccountSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;

        onDeployAccountSuccess?.Invoke(result.FunctionResult.ToString());
    }
    
    private static void OnRequestTransferOwnershipSuccess(ExecuteFunctionResult result)
    {
        onRequestTransferOwnershipSuccess?.Invoke(result.FunctionResult.ToString());
    }
    
    private static void OnRequestTransferOwnershipFailure(PlayFabError error)
    {
        onRequestTransferOwnershipFailure?.Invoke(error);
    }
    #endregion
}
