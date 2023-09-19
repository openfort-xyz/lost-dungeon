using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    public static Action<ShopItem> onBuyClicked;
    public static Action<ShopItem, int> onEquipClicked;
    public static Action onUnEquipClicked;
    
    [Header("UI")]
    public Button BuyBtn, EquipBtn;
    [SerializeField] Text equipText, costText;
    [SerializeField] private Image coinImg;
    
    private bool _bought, _equipped;
    private ExchangeOffer _offer;

    private void OnDisable()
    {
        BuyBtn.onClick.RemoveAllListeners();
        EquipBtn.onClick.RemoveAllListeners();
    }
    
    #region PUBLIC_METHODS
    public void Setup(ExchangeOffer offer, bool bought, bool equipped)
    {
        _offer = offer;
        _bought = bought;
        _equipped = equipped; //TODO?????

        if (_bought)
        {
            BuyBtn.gameObject.SetActive(false);
            EquipBtn.gameObject.SetActive(true);
        }
        else
        {
            costText.text = ((int)_offer.InputCurrencyAmount).ToString();   
        }

        BuyBtn.onClick.AddListener(OnBuyButtonClicked);
        EquipBtn.onClick.AddListener(OnEquipButtonClicked);
    }

    public void Equip()
    {
        equipText.text = "Equipped";
        _equipped = true;
    }

    public void UnEquip()
    {
        equipText.text = "Equip";
        _equipped = false;
    }

    public void Approachable(bool status)
    {
        BuyBtn.interactable = status;
        EquipBtn.interactable = status;
    }

    public void Bought()
    {
        BuyBtn.gameObject.SetActive(false);
        EquipBtn.gameObject.SetActive(true);
    }

    public void ResetUI()
    {
        costText.text = costText.text = ((int)_offer.InputCurrencyAmount).ToString();
        coinImg.enabled = true;
    }
    
    public ExchangeOffer GetOffer()
    {
        return _offer;
    }
    #endregion

    #region PRIVATE_METHODS
    private void OnBuyButtonClicked()
    {
        if (float.TryParse(StaticPlayerData.currencyAmount, out float currencyAmount))
        {
            if (currencyAmount < (float)_offer.InputCurrencyAmount)
            {
                Debug.Log("Not enough money.");
                return;
            }
        }
        else
        {
            Debug.Log("CurrencyAmount is not a valid float.");
            return;
        }
        
        Debug.Log("Buying weapon: " + _offer.Id);
        costText.text = "Buying...";
        coinImg.enabled = false;
        
        onBuyClicked?.Invoke(this);
    }

    private void OnEquipButtonClicked()
    {
        if (_equipped)
        {
            UnEquip();
            onUnEquipClicked?.Invoke();
        }
        else
        {
            Equip();
            onEquipClicked?.Invoke(this, (int)_offer.OutputCollectionItemIds[0]);
        }
    }
    #endregion
}
