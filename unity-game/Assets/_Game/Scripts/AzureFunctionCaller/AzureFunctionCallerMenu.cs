using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.CloudScriptModels;

public static partial class AzureFunctionCaller
{
    public static Action<ExecuteFunctionResult> onDeployAccountSuccess;
    public static Action<string> onRequestTransferOwnershipSuccess;
    public static Action<PlayFabError> onRequestTransferOwnershipFailure;
    
    public static Action<string> onTransferUserDataSuccess;
    public static Action<PlayFabError> onTransferUserDataFailure;

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
    
    public static void TransferUserData(Dictionary<string,object> guestUserData)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "TransferUserData",
            FunctionParameter = guestUserData,
            GeneratePlayStreamEvent = true,
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnTransferUserDataSuccess, OnTransferUserDataFailure);
    }
    #endregion
    
    #region CALLBACK_HANDLERS
    private static void OnDeployAccountSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;

        onDeployAccountSuccess?.Invoke(result);
    }
    
    private static void OnRequestTransferOwnershipSuccess(ExecuteFunctionResult result)
    {
        onRequestTransferOwnershipSuccess?.Invoke(result.FunctionResult.ToString());
    }
    
    private static void OnRequestTransferOwnershipFailure(PlayFabError error)
    {
        onRequestTransferOwnershipFailure?.Invoke(error);
    }
    
    private static void OnTransferUserDataSuccess(ExecuteFunctionResult result)
    {
        onTransferUserDataSuccess?.Invoke(result.FunctionResult.ToString());
    }
    
    private static void OnTransferUserDataFailure(PlayFabError error)
    {
        onTransferUserDataFailure?.Invoke(error);
    }
    #endregion
}
