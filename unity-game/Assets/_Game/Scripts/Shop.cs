using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Openfort;
using PlayFab;
using PlayFab.ClientModels;

public class Shop : MonoBehaviour
{
    public static Action<bool> isBuying;
    public static Action<float> onItemBought;
    public static Action<ShopItem, int> onItemEquipped;
    public static Action onItemUnequipped;
    
    [SerializeField] ShopItem[] items;
    
    List<ExchangeOffer> offers = new List<ExchangeOffer>();
    List<CollectionItem> ofPlayerItems = new List<CollectionItem>();

    [SerializeField] private GameObject panel;
    [SerializeField] private Animator animator;

    private ShopItem _currentItem;
    private Coroutine _poolCoroutine;

    [HideInInspector] public bool buyingProcessActive;
    
    private static readonly int CloseCondition = Animator.StringToHash("Close");

    private OpenfortClient _openfort;

    #region UNITY_LIFECYCLE

    private void Start()
    {
        _openfort = new OpenfortClient(OpenfortStaticData.publishableKey);
    }

    private void OnEnable()
    {
        ShopItem.onBuyClicked += OnBuyItemClicked;
        ShopItem.onEquipClicked += OnEquipItemClicked;
        ShopItem.onUnEquipClicked += OnUnEquipItemClicked;
        
        AzureFunctionCaller.onGetItemBalanceSuccess += OnGetItemBalanceSuccess;
        AzureFunctionCaller.onRequestFailure += OnGetItemBalanceFailure;
        
        AzureFunctionCaller.onBuyWeaponSuccess += OnBuyWeaponSuccess;
        AzureFunctionCaller.onBuyWeaponFailure += OnBuyWeaponFailure;

        AzureFunctionCaller.onPoolingSuccess += OnPoolingSuccess;
        AzureFunctionCaller.onPoolingFailure += OnPoolingFailure;
    }

    private void OnDisable()
    {
        ShopItem.onBuyClicked -= OnBuyItemClicked;
        ShopItem.onEquipClicked -= OnEquipItemClicked;
        ShopItem.onUnEquipClicked -= OnUnEquipItemClicked;
        
        AzureFunctionCaller.onGetItemBalanceSuccess -= OnGetItemBalanceSuccess;
        AzureFunctionCaller.onRequestFailure -= OnGetItemBalanceFailure;
        
        AzureFunctionCaller.onBuyWeaponSuccess -= OnBuyWeaponSuccess;
        AzureFunctionCaller.onBuyWeaponFailure -= OnBuyWeaponFailure;
        
        AzureFunctionCaller.onPoolingSuccess -= OnPoolingSuccess;
        AzureFunctionCaller.onPoolingFailure -= OnPoolingFailure;
    }
    #endregion

    #region PUBLIC_METHODS
    public void Open()
    {
        panel.SetActive(true);
        
        //TODO GetShopContract items
        if (buyingProcessActive) return;
        
        // We don't want to be able to interact with items while getting player's inventory
        SetItemsApproachability(false);
        // Getting Openfort Player's inventory
        AzureFunctionCaller.GetItemBalance();
    }
    
    public void Close()
    {
        animator.SetTrigger(CloseCondition);
    }
    #endregion

    #region AZURE_FUNCTION_CALLER_HANDLERS
    private void OnGetItemBalanceSuccess(ItemBalance itemBalance)
    {
        //{"object":"inventory","nftAssets":[{"assetType":4,"address":"0x898cf2a67e8887d3c69236147a201608565ff3b3","tokenId":0,"amount":1}],"nativeAsset":{"assetType":1,"amount":0},"tokenAssets":[{"assetType":2,"address":"0x658d55c80ab4d153774fc5f1d08aa396cc8243b7","amount":"4000000000000000000"}]}

        Debug.Log("Item balance retrieved successfully.");
        
        offers = new List<ExchangeOffer>
        {
            new ExchangeOffer {InputCurrencyAmount = 1m, Id=1m, OutputCollectionItemIds = new List<decimal> { 102m } },
            new ExchangeOffer {InputCurrencyAmount = 10m, Id=2m, OutputCollectionItemIds = new List<decimal> { 101m } },
            new ExchangeOffer {InputCurrencyAmount = 30m, Id=3m, OutputCollectionItemIds = new List<decimal> { 103m } },
            new ExchangeOffer {InputCurrencyAmount = 40m, Id=4m, OutputCollectionItemIds = new List<decimal> { 104m } },
            new ExchangeOffer {InputCurrencyAmount = 50m, Id=5m, OutputCollectionItemIds = new List<decimal> { 105m } },
        };
        
        // Initialize itemsList
        ofPlayerItems = new List<CollectionItem>
        {
            new CollectionItem { count = 0 },
            new CollectionItem { count = 0 },
            new CollectionItem { count = 0 },
            new CollectionItem { count = 0 },
            new CollectionItem { count = 0 },
        };

        // Adding Player's Inventory assets (NFTs) to PlayFab player's inventory. Web3 to Web2.
        foreach (var asset in itemBalance.nftAssets)
        {
            int tokenId = asset.tokenId;
            if (tokenId >= 0 && tokenId <= 4)
            {
                // If it exists, get the struct, modify it, and set it back
                CollectionItem item = ofPlayerItems[tokenId];
                item.count = 1;
                ofPlayerItems[tokenId] = item;
            }
        }
            
        // TODO check this later
        Refresh();
        // We can interact with items again.
        SetItemsApproachability(true);
    }
    
    private void OnGetItemBalanceFailure()
    {
        Debug.Log("Failed retrieving item balance.");
        // TODO check this later
        Refresh();
        // We can interact with items again.
        SetItemsApproachability(true);
    }
    
    private void OnBuyWeaponSuccess(Transaction tx, bool hasUserOpHash)
    {
        if (hasUserOpHash)
        {
            SendSignUserOpHash(tx.id, tx.userOpHash);
        }
        else
        {
            _poolCoroutine = StartCoroutine(PoolEveryTwoSeconds(tx.id));   
        }
    }

    private void OnBuyWeaponFailure(PlayFabError error)
    {
        Debug.Log("Buy weapon failed.");
        var errorReport = error.GenerateErrorReport();
        
        // If function timeout
        if (errorReport.ToLower().Contains("10000ms"))
        {
            // We try again.
            AzureFunctionCaller.FindTransactionIntent(_currentItem.GetOffer().Id);
        }
        else
        {
            SetItemsApproachability(true);
            _currentItem.ResetUI();
            // Weapon is not bought. Shop is not in buying process anymore.
            buyingProcessActive = false;
            isBuying?.Invoke(false);
        }
    }
    
    private void OnPoolingSuccess()
    {
        // Stop the pooling
        if (_poolCoroutine != null)
        {
            StopCoroutine(_poolCoroutine);
            _poolCoroutine = null;
        }
        
        // Weapon is bought successfully
        _currentItem.Bought();
        SetItemsApproachability(true);
        buyingProcessActive = false;
        isBuying?.Invoke(false);
        onItemBought?.Invoke((float)_currentItem.GetOffer().InputCurrencyAmount);
    }

    private void OnPoolingFailure()
    {
        // Stop the pooling
        if (_poolCoroutine != null)
        {
            StopCoroutine(_poolCoroutine);
            _poolCoroutine = null;
        }
        
        // Weapon not bought, and shop is not in buying process anymore.
        _currentItem.ResetUI();
        SetItemsApproachability(true);
        buyingProcessActive = false;
        isBuying?.Invoke(false);
    }
    #endregion

    #region PRIVATE_METHODS
    private async void SendSignUserOpHash(string transactionIntentId, string userOpHash)
    {
        // Prepare the ExecuteFunctionRequest
        Debug.Log("sendSignUserOpHash");
        
        // should sign using a session
        _openfort.LoadSessionKey();

        var signature = _openfort.SignMessage(userOpHash);
        await _openfort.SendSignatureTransactionIntentRequest(transactionIntentId, signature);

        _poolCoroutine = StartCoroutine(PoolEveryTwoSeconds(transactionIntentId));
    }
    
    private IEnumerator PoolEveryTwoSeconds(string id)
    {
        int attemptCount = 0;
        int maxAttempts = 30;

        while (attemptCount < maxAttempts)
        {
            AzureFunctionCaller.GetTransactionIntent(id);

            // Wait for two seconds before sending the next request
            yield return new WaitForSeconds(2);
            attemptCount++;
        }

        if (attemptCount >= maxAttempts)
        {
            Debug.Log("Maximum pooling attempts reached, stopping pooling.");
            if (_poolCoroutine != null)
            {
                StopCoroutine(_poolCoroutine);
                _poolCoroutine = null;
            }
            
            _currentItem.ResetUI();
            SetItemsApproachability(true);
            buyingProcessActive = false;
            isBuying?.Invoke(false);
        }
    }
    
    private void OnBuyItemClicked(ShopItem item)
    {
        _currentItem = item;
        SetItemsApproachability(false);
        buyingProcessActive = true;
        isBuying?.Invoke(true);
        
        AzureFunctionCaller.BuyWeapon(item.GetOffer().Id);
    }
    
    private void OnEquipItemClicked(ShopItem equippedItem, int id)
    {
        // Unequip all other items.
        foreach (var item in items)
        {
            if (item != equippedItem)
                item.UnEquip();
        }
        
        onItemEquipped?.Invoke(equippedItem, id);
    }
    
    private void OnUnEquipItemClicked()
    {
        onItemUnequipped?.Invoke();
    }
    
    private void Refresh()
    {
        // We make a comparison between shop offers with player's inventory to see what items the player has already bought.
        if (offers.Count == 0)
        {
            Debug.Log("No offers");
            return;
        }
        if (ofPlayerItems.Count == 0)
        {
            Debug.Log("No items");
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            items[i].Setup(offers[i], ofPlayerItems[i].count > 0, false);
        }
    }

    private void SetItemsApproachability(bool status)
    {
        foreach (var item in items)
        {
            item.Approachable(status);
        }
    }
    #endregion
}
