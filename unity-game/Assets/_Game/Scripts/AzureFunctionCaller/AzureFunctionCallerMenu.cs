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
    public static Action<Transaction> onRequestTransferOwnershipSuccess;

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
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnRequestTransferOwnershipSuccess, OnRequestFailure);
    }

    #endregion
    
    #region CALLBACK_HANDLERS
    private static void OnRequestTransferOwnershipSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;
        
        var tx = JsonUtility.FromJson<Transaction>(result.FunctionResult.ToString());

        // Check if deserialization was successful
        if (tx == null)
        {
            Debug.Log("Failed to parse JSON");
            onRequestFailure?.Invoke();
            return;
        }
        
        onRequestTransferOwnershipSuccess?.Invoke(tx);
    }
    #endregion
}
