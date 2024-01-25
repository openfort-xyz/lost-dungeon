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
    
    public static Action<string> onStartRecoverySuccess;
    public static Action<PlayFabError> onStartRecoveryFailure;
    
    public static Action<string> onCompleteRecoverySuccess;
    public static Action<PlayFabError> onCompleteRecoveryFailure;

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
    
    public static void StartRecovery(string playerId, string newOwnerAddress)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "StartRecovery",
            FunctionParameter = new Dictionary<string, object>()
            {
                {"playerId", playerId},
                {"newOwnerAddress", newOwnerAddress}
            },
            GeneratePlayStreamEvent = true,
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnStartRecoverySuccess, OnStartRecoveryFailure);
    }

    public static void CompleteRecovery(string playerId, string newOwnerAddress)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "CompleteRecovery",
            FunctionParameter = new Dictionary<string, object>()
            {
                {"playerId", playerId},
                {"newOwnerAddress", newOwnerAddress}
            },
            GeneratePlayStreamEvent = true,
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnCompleteRecoverySuccess, OnCompleteRecoveryFailure);
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
    
    private static void OnStartRecoverySuccess(ExecuteFunctionResult result)
    {
        onStartRecoverySuccess?.Invoke(result.FunctionResult.ToString());
    }
    
    private static void OnStartRecoveryFailure(PlayFabError error)
    {
        onStartRecoveryFailure?.Invoke(error);
    }
    
    private static void OnCompleteRecoverySuccess(ExecuteFunctionResult result)
    {
        onCompleteRecoverySuccess?.Invoke(result.FunctionResult.ToString());
    }

    private static void OnCompleteRecoveryFailure(PlayFabError error)
    {
        onCompleteRecoveryFailure?.Invoke(error);
    }
    #endregion
}
