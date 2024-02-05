using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;

[Serializable]
public class OpenfortPlayerResponse
{
    public string address;
    public string short_address;
}

public static partial class AzureFunctionCaller
{
    public static Action<string> onChallengeRequestSuccess;
    public static Action onChallengeVerifySuccess;
    public static Action<string> onRegisterSessionSuccess;
    public static Action<string> onCompleteWeb3AuthSuccess;
    public static Action<OpenfortPlayerResponse> onCreateOpenfortPlayerSuccess;

    public static Action onCreateOpenfortPlayerFailure;
    public static Action<PlayFabError> onRegisterSessionFailure;
    // Any Request failure
    public static Action onRequestFailure;

    #region FUNCTIONS
    public static void ChallengeRequest(string address, int? chainId)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId, //Get this from when you logged in,
                Type = PlayFabSettings.staticPlayer.EntityType, //Get this from when you logged in
            },
            FunctionName = "ChallengeRequest", //This should be the name of your Azure Function that you created.
            FunctionParameter =
                new Dictionary<string, object>() //This is the data that you would want to pass into your function.
                {
                    {"address", address},
                    {"chainId", chainId}
                },
            GeneratePlayStreamEvent = true //Set this to true if you would like this call to show up in PlayStream
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnChallengeRequestSuccess, OnRequestFailure);
    }
    
    public static void ChallengeVerify(string msg, string signature)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId, //Get this from when you logged in,
                Type = PlayFabSettings.staticPlayer.EntityType, //Get this from when you logged in
            },
            FunctionName = "ChallengeVerify", //This should be the name of your Azure Function that you created.
            FunctionParameter =
                new Dictionary<string, object>() //This is the data that you would want to pass into your function.
                {
                    { "message", msg },
                    { "signature", signature }
                },
            GeneratePlayStreamEvent = true //Set this to true if you would like this call to show up in PlayStream
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnChallengeVerifySuccess, OnRequestFailure);
    }
    
    public static void RegisterSession(string sessionAddress, string playerId)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "RegisterSession",
            FunctionParameter =
                new Dictionary<string, object>()
                {
                    {"sessionAddress", sessionAddress},
                    {"playerId", playerId}
                },
            GeneratePlayStreamEvent = true
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnRegisterSessionSuccess, OnRegisterSessionFailure);
    }

    public static void CompleteWeb3Auth(string ownerAddress)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "CompleteWeb3Auth",
            FunctionParameter =
                new Dictionary<string, object>()
                {
                    {OFStaticData.OFownerAddressKey, ownerAddress}
                },
            GeneratePlayStreamEvent = true
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnCompleteWeb3AuthSuccess, OnRequestFailure);
    }

    public static void CreateOpenfortPlayer()
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId, //Get this from when you logged in,
                Type = PlayFabSettings.staticPlayer.EntityType, //Get this from when you logged in
            },
            FunctionName = "CreateOpenfortPlayer",
            GeneratePlayStreamEvent = true,
        };
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnCreateOpenfortPlayerSuccess, OnCreateOpenfortPlayerFailure);
    }
    #endregion
    
    #region CALLBACK_HANDLERS
    private static void OnChallengeRequestSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;
        
        // Check if we got a message
        //TODO deserialize and just get the message
        var msg = result.FunctionResult.ToString();

        if (string.IsNullOrEmpty(msg))
        {
            onRequestFailure?.Invoke();
            return;
        }

        Debug.Log("CHALLENGE REQUEST SUCCESS!!!!!!!!!!!!!!!!! " + msg);
        onChallengeRequestSuccess?.Invoke(msg);
    }
    
    private static void OnChallengeVerifySuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;

        // If the authentication succeeded the user profile is update and we get the UpdateUserDataAsync return values a response
        Debug.Log("Web3 Authentication successful!");
        onChallengeVerifySuccess?.Invoke();
    }

    private static void OnRegisterSessionSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;

        onRegisterSessionSuccess?.Invoke(result.FunctionResult.ToString());
    }
    
    private static void OnCompleteWeb3AuthSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;

        onCompleteWeb3AuthSuccess?.Invoke(result.FunctionResult.ToString());
    }

    private static void OnCreateOpenfortPlayerSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;

        OpenfortPlayerResponse ofPlayerResponse = JsonUtility.FromJson<OpenfortPlayerResponse>(result.FunctionResult.ToString());
        if (ofPlayerResponse == null)
        {
            Debug.Log("Openfort Player is null.");
            onRequestFailure?.Invoke();
            return;
        }
        
        StaticPlayerData.DisplayName = ofPlayerResponse.address;
        PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = ofPlayerResponse.short_address
        }, null, null);

        onCreateOpenfortPlayerSuccess?.Invoke(ofPlayerResponse);
        Debug.Log("Openfort player (custodial wallet) created!");
    }
    
    private static void OnCreateOpenfortPlayerFailure(PlayFabError error)
    {
        Debug.Log($"Oops, something went wrong: {error.GenerateErrorReport()}");
        onCreateOpenfortPlayerFailure?.Invoke();
    }

    private static void OnRegisterSessionFailure(PlayFabError error)
    {
        Debug.Log($"Oops, something went wrong: {error.GenerateErrorReport()}");
        onRegisterSessionFailure?.Invoke(error);
    }
    
    // Almost all AzureFunctionCaller partial classes use this.
    private static void OnRequestFailure(PlayFabError error)
    {
        Debug.Log($"Oops, something went wrong: {error.GenerateErrorReport()}");
        onRequestFailure?.Invoke();
    }
    #endregion

    #region PRIVATE_METHODS
    // All AzureFunctionCaller partial classes use this.
    private static bool IsFunctionResultValid(ExecuteFunctionResult result)
    {
        if (result == null)
        {
            Debug.LogError("Result is null.");
            onRequestFailure?.Invoke();
            return false;
        }
        
        if (result.FunctionResultTooLarge ?? false)
        {
            Debug.LogError("This can happen if you exceed the limit that can be returned from an Azure Function, See PlayFab Limits Page for details.");
            onRequestFailure?.Invoke();
            return false;
        }

        if (result.FunctionResult == null)
        {
            Debug.LogError("FunctionResult is null.");
            onRequestFailure?.Invoke();
            return false;
        }
        
        if (string.IsNullOrEmpty(result.FunctionResult.ToString()))
        {
            Debug.LogError("FunctionResult is empty.");
            onRequestFailure?.Invoke();
            return false;
        }
        
        Debug.Log($"The {result.FunctionName} function took {result.ExecutionTimeMilliseconds} to complete");
        return true;
    }
    #endregion
}
