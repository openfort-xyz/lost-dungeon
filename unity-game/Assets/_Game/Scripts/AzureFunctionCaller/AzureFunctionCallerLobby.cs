using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using UnityEngine;

[Serializable]
public class ItemBalance
{
    public string @object;
    public List<NftAsset> nftAssets;
    public NativeAsset nativeAsset;
    public List<TokenAsset> tokenAssets;
}

[Serializable]
public class NftAsset
{
    public int assetType;
    public string address;
    public int tokenId;
    public int amount;
}


[Serializable]
public class NativeAsset
{
    public int assetType;
    public int amount;
}

[Serializable]
public class TokenAsset
{
    public int assetType;
    public string address;
    public string amount;
}

[Serializable]
public class Transaction
{
    public string id;
    public string userOpHash;
}

public static partial class AzureFunctionCaller
{
    public static Action<TokenAsset> onGetCurrencyBalanceSuccess;
    public static Action<ItemBalance> onGetItemBalanceSuccess;
    public static Action<Transaction, bool> onBuyWeaponSuccess;
    public static Action<PlayFabError> onBuyWeaponFailure;
    public static Action onPoolingSuccess;
    public static Action onPoolingFailure;

    #region FUNCTIONS

    public static void GetCurrencyBalance()
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "CurrencyBalance",
            GeneratePlayStreamEvent = true,
        };
        
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnGetCurrencyBalanceSuccess, OnRequestFailure);
    }

    public static void GetItemBalance()
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "ItemBalance",
            GeneratePlayStreamEvent = true,
        };
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnGetItemBalanceSuccess, OnRequestFailure);
    }

    public static void BuyWeapon(decimal weaponId)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "BuyWeapon",
            FunctionParameter = new Dictionary<string, object>() { { "offerId", (weaponId - 1).ToString() } },

            GeneratePlayStreamEvent = true,
        };
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnBuyWeaponSuccess, OnBuyWeaponFailure);
    }

    public static void FindTransactionIntent(decimal weaponId)
    {
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "FindTransactionIntent",
            FunctionParameter = new Dictionary<string, object>() { { "offerId", (weaponId - 1).ToString() } },
            GeneratePlayStreamEvent = true,
        };

        // Send the request
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnBuyWeaponSuccess, OnRequestFailure);
    }

    public static void GetTransactionIntent(string id)
    {
        // Prepare the ExecuteFunctionRequest
        var request = new ExecuteFunctionRequest()
        {
            Entity = new PlayFab.CloudScriptModels.EntityKey()
            {
                Id = PlayFabSettings.staticPlayer.EntityId,
                Type = PlayFabSettings.staticPlayer.EntityType,
            },
            FunctionName = "GetTransactionIntent",
            FunctionParameter = new Dictionary<string, object>() { { "transactionIntentId", id } },
            GeneratePlayStreamEvent = true,
        };

        // Send the request
        PlayFabCloudScriptAPI.ExecuteFunction(request, OnPoolingSuccess, OnPoolingFailure);
    }
    #endregion

    #region CALLBACK_HANDLERS
    private static void OnGetCurrencyBalanceSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;

        var tokenAsset = JsonUtility.FromJson<TokenAsset>(result.FunctionResult.ToString());

        // Check if deserialization was successful
        if (tokenAsset == null)
        {
            Debug.Log("Failed to parse JSON");
            onRequestFailure?.Invoke();
            return;
        }
        
        onGetCurrencyBalanceSuccess?.Invoke(tokenAsset);
    }
    
    private static void OnGetItemBalanceSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return;
        
        var itemBalance = JsonUtility.FromJson<ItemBalance>(result.FunctionResult.ToString());

        // Check if deserialization was successful
        if (itemBalance == null)
        {
            Debug.Log("Failed to parse JSON.");
            onRequestFailure?.Invoke();
            return;
        }
        
        onGetItemBalanceSuccess?.Invoke(itemBalance);
    }
    
    private static void OnBuyWeaponSuccess(ExecuteFunctionResult result)
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
        
        if (tx.userOpHash != null)
        {
            onBuyWeaponSuccess?.Invoke(tx, true);
        }
        else
        {
            onBuyWeaponSuccess?.Invoke(tx, false);
        }
    }
    
    private static void OnBuyWeaponFailure(PlayFabError error)
    {
        Debug.Log($"Oops, something went wrong: {error.GenerateErrorReport()}");
        onBuyWeaponFailure?.Invoke(error);
    }
    
    private static void OnPoolingSuccess(ExecuteFunctionResult result)
    {
        if (!IsFunctionResultValid(result)) return; 

        // Check if the result contains "minted:true"
        if (result.FunctionResult.ToString().Contains("\"minted\":true"))
        {
            Debug.Log("Minted is true. Stop pooling...");
            onPoolingSuccess?.Invoke();
        }
        //TODO else onPoolingFailure???
    }

    private static void OnPoolingFailure(PlayFabError error)
    {
        Debug.Log($"Pooling failed: {error.GenerateErrorReport()}");
        onPoolingFailure?.Invoke();
    }
    #endregion
}
