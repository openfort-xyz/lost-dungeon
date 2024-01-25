using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

public class SocialRecoveryController : MonoBehaviour
{
    public GameObject view;
    public GameObject startContent;
    public GameObject completeContent;
    public InputField recoveryAddressInput;
    public Text statusText;

    private string _recoveryAddress;
    
    private void OnEnable()
    {
        AzureFunctionCaller.onStartRecoverySuccess += OnStartRecoverySuccess;
        AzureFunctionCaller.onStartRecoveryFailure += OnStartRecoveryFailure;
        
        AzureFunctionCaller.onCompleteRecoverySuccess += OnCompleteRecoverySuccess;
        AzureFunctionCaller.onCompleteRecoveryFailure += OnCompleteRecoveryFailure;
    }
    
    private void OnDisable()
    {
        AzureFunctionCaller.onStartRecoverySuccess -= OnStartRecoverySuccess;
        AzureFunctionCaller.onStartRecoveryFailure -= OnStartRecoveryFailure;
        
        AzureFunctionCaller.onCompleteRecoverySuccess -= OnCompleteRecoverySuccess;
        AzureFunctionCaller.onCompleteRecoveryFailure -= OnCompleteRecoveryFailure;
    }

    public void Activate(bool status)
    {
        view.SetActive(status);

        if (status)
        {
            // Check if player has already started the recovery process.
            PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest()
                {
                    Keys = new List<string>() { "recoveryAddress" }
                },
                userDataResult => 
                {
                    if (userDataResult.Data.TryGetValue("recoveryAddress", out var recoveryAddress))
                    {
                        // GET RECOVERY address
                        _recoveryAddress = recoveryAddress.Value;
                        // Enable CompleteRecovery content
                        completeContent.SetActive(true);
                        Log("Recovery process is already started. Try to complete it.");
                    }
                    else
                    {
                        // Enable StartRecovery content
                        startContent.SetActive(true);
                        Log("You can start the recovery process by entering a trusted address.");
                    }
                },
                error => 
                {
                    Log("Got error getting user data:");
                    Log(error.GenerateErrorReport());
                });
        }
        else
        {
            //TODO disable content?
            recoveryAddressInput.text = string.Empty;
        }
    }

    #region PUBLIC_METHODS
    public void StartRecovery()
    {
        Log("Starting recovery process...");
        
        if (string.IsNullOrEmpty(recoveryAddressInput.text))
        {
            Log("Address is null or empty.");
            return;
        }
        
        //TODO should also check if it's a valid address

        if (string.IsNullOrEmpty(OFStaticData.OFplayerValue))
        {
            Log("OFplayerValue is null or empty.");
        }
        
        startContent.SetActive(false);
        AzureFunctionCaller.StartRecovery(OFStaticData.OFplayerValue, recoveryAddressInput.text);
    }
    
    public void CompleteRecovery()
    {
        Log("Completing recovery process...");
        
        if (string.IsNullOrEmpty(_recoveryAddress))
        {
            Log("RecoveryAddress is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(OFStaticData.OFplayerValue))
        {
            Log("OFplayerValue is null or empty.");
        }
        
        completeContent.SetActive(false);
        AzureFunctionCaller.CompleteRecovery(OFStaticData.OFplayerValue, _recoveryAddress);
    }
    #endregion

    #region AZURE_FUNCTION_CALLER_CALLBACK_HANDLERS
    private void OnStartRecoverySuccess(string result)
    {
        PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest()
        {
            Keys = new List<string>() { "recoveryAddress" }
        },
        userDataResult => 
        {
            Log("Get user data successful");
            if (userDataResult.Data.TryGetValue("recoveryAddress", out var recoveryAddress))
            {
                // GET RECOVERY address
                _recoveryAddress = recoveryAddress.Value;
                // Enable CompleteRecovery content
                completeContent.SetActive(true);
                
                Log(result);
            }
            else
            {
                Log("Failed getting recovery address.");
                // Enable StartRecovery content
                startContent.SetActive(true);
            }
        },
        error => 
        {
            Log("Got error getting user data:");
            Log(error.ErrorMessage);
        });
    }
    
    private void OnStartRecoveryFailure(PlayFabError error)
    {
        Log(error.ErrorMessage);
        startContent.SetActive(true);
    }
    
    private void OnCompleteRecoverySuccess(string result)
    {
        Log(result);
        startContent.SetActive(true);
        
        //TODO add delay?
        Activate(false);
    }
    
    private void OnCompleteRecoveryFailure(PlayFabError error)
    {
        Log(error.ErrorMessage);
        completeContent.SetActive(true);
    }
    #endregion

    #region PRIVATE_METHODS
    private void SwitchContent()
    {
        startContent.SetActive(!startContent.activeSelf);
        completeContent.SetActive(!completeContent.activeSelf);
    }

    private void Log(string message)
    {
        Debug.Log(message);
        statusText.text = message;
    }
    #endregion
}