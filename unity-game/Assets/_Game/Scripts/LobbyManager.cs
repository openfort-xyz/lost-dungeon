using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : StateMachineSingleton<LobbyManager>
{
    public enum LobbyState
    {
        None,
        Hint,
        Unarmed,
        Shop,
        WeaponEquipped
    }

    public LobbyState currentState;

    [Header("Main Elements")]
    public Movement playerMovement;
    public Shop shop;
    public WeaponHolder weaponHolder;
    public CurrencyBalanceHelper currencyBalanceHelper;
    public GameObject hintPanel;
    public GameObject joystick;
    
    // Control vars
    private bool _weaponEquipped;
    
    #region UNITY_LIFECYCLE
    protected override void Awake()
    {
        playerMovement.EnableMovement(false);
    }

    private void OnEnable()
    {
        Shop.onItemBought += WeaponBought;

        Shoptrigger.onShopTriggered += ShopTriggered;
        ShopAnimEventHandler.onShopClosed += ShopClosed;
    }

    private void OnDisable()
    {
        Shop.onItemBought -= WeaponBought;
        
        Shoptrigger.onShopTriggered -= ShopTriggered;
        ShopAnimEventHandler.onShopClosed -= ShopClosed;
    }

    private void Start()
    {
        // Check if the player has bought any weapon at some point.
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("WeaponBought"))
            {
                // If so, we don't need to show the hint.
                currencyBalanceHelper.GetCurrencyBalance();
                currentState = LobbyState.Unarmed;
            }
            else
            {
                hintPanel.SetActive(true);
                currentState = LobbyState.Hint; 
            }
        }, error =>
        {
            Debug.Log("We couldn't find WeaponBought key.");
            hintPanel.SetActive(true);
            currentState = LobbyState.Hint; 
        });
    }
    
    private void Update()
    {
        if (Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Application.Quit();   
            }
        };
        
        //TODO implement event driven state machine
        // Call the appropriate state's update function based on the current state.
        switch (currentState)
        {
            case LobbyState.Hint:
                HintStateUpdate();
                break;
            case LobbyState.Unarmed:
                UnarmedStateUpdate();
                break;
            case LobbyState.Shop:
                ShopStateUpdate();
                break;
            case LobbyState.WeaponEquipped:
                WeaponEquippedStateUpdate();
                break;
            default:
                break;
        }
    }
    #endregion

    #region STATES_UPDATE
    // Define functions to handle the logic for each state.
    private void HintStateUpdate()
    {
        hintPanel.SetActive(true);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnHintPanelCloseClicked();
        }
    }

    private void UnarmedStateUpdate()
    {
        playerMovement.EnableMovement(true);
        
#if UNITY_ANDROID || UNITY_IOS
        joystick.SetActive(true);
#endif
    }

    private void ShopStateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            shop.Close();
            // Shop has an animation event that will trigger when closed and it will call ChangeState() there.
        }
    }

    private void WeaponEquippedStateUpdate()
    {
        playerMovement.EnableMovement(true);
        
#if UNITY_ANDROID || UNITY_IOS
        joystick.SetActive(true);
#endif
    }

    // Function to change the state when needed.
    public void ChangeState(LobbyState newState)
    {
        currentState = newState;
    }
    #endregion

    #region EVENT_HANDLERS
    private void WeaponBought(float weaponPrice)
    {
        // Let's use PlayFab SDK to save if the player has bought a weapon or not.
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                // Key is the name of the data, value is the data you want to set
                { "WeaponBought", "true" }
            }
        };

        // Make the API call to update player data
        PlayFabClientAPI.UpdateUserData(request, null, null);
        
        currencyBalanceHelper.UpdateCurrencyBalance(weaponPrice);
    }
    
    private void ShopTriggered()
    {
        playerMovement.EnableMovement(false);
        
#if UNITY_ANDROID || UNITY_IOS
        joystick.SetActive(false);
#endif
        
        shop.Open();
        
        ChangeState(LobbyState.Shop);
    }
    
    private void ShopClosed()
    {
        // We check if the player has some weapon equipped or not.
        ChangeState(weaponHolder.IsEquipped() ? LobbyState.WeaponEquipped : LobbyState.Unarmed);
    }
    #endregion

    #region BUTTON_METHODS
    public void OnHintPanelCloseClicked()
    {
        hintPanel.SetActive(false);
        currencyBalanceHelper.GetCurrencyBalance();
        ChangeState(LobbyState.Unarmed);
    }

    public void OnBackToMenuClicked()
    {
        //TODO something else?
        SceneManager.LoadScene("Menu");
    }
    #endregion
}
