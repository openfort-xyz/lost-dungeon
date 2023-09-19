using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CurrencyBalanceHelper : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinValue;
    [SerializeField] private Button reloadButton;

    private void OnEnable()
    {
        AzureFunctionCaller.onGetCurrencyBalanceSuccess += OnGetCurrencyBalanceSuccess;
        AzureFunctionCaller.onRequestFailure += OnGetCurrencyBalanceFailure;
        
        Shop.isBuying += ShopIsBuying;
    }
    
    private void OnDisable()
    {
        AzureFunctionCaller.onGetCurrencyBalanceSuccess -= OnGetCurrencyBalanceSuccess;
        AzureFunctionCaller.onRequestFailure -= OnGetCurrencyBalanceFailure;
        
        Shop.isBuying -= ShopIsBuying;
    }

    #region PUBLIC_METHODS
    public void GetCurrencyBalance()
    {
        coinValue.text = "Loading...";
        AzureFunctionCaller.GetCurrencyBalance();
    }

    public void UpdateCurrencyBalance(float substract_amount)
    {
        var currencyAmount = float.Parse(StaticPlayerData.currencyAmount, CultureInfo.InvariantCulture);
        var newBalance =  currencyAmount - substract_amount;
        coinValue.text = newBalance.ToString(CultureInfo.InvariantCulture); 
        StaticPlayerData.currencyAmount = coinValue.text;
    }

    public void OnReloadClicked()
    {
        reloadButton.gameObject.SetActive(false);
        GetCurrencyBalance();
    }
    #endregion

    #region AZURE_FUNCTION_CALLER_HANDLERS
    private void OnGetCurrencyBalanceSuccess(TokenAsset tokenAsset)
    {
        Debug.Log("Currency balance = " + tokenAsset.amount);
        coinValue.text = tokenAsset.amount;
        StaticPlayerData.currencyAmount = tokenAsset.amount;
    }

    private void OnGetCurrencyBalanceFailure()
    {
        Debug.Log("Couldn't get currency balance. Maybe it's 0.");
        StaticPlayerData.currencyAmount = "0.0";
        coinValue.text = "0";
        
        reloadButton.gameObject.SetActive(true);
    }
    #endregion

    #region OTHER_EVENT_HANDLERS
    private void ShopIsBuying(bool buying)
    {
        coinValue.text = buying ? "Buying..." : StaticPlayerData.currencyAmount;
    }
    #endregion
}
