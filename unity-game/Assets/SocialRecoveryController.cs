using System.Collections;
using System.Collections.Generic;
using PlayFab;
using UnityEngine;
using UnityEngine.UI;

public class SocialRecoveryController : MonoBehaviour
{
    public GameObject view;
    public GameObject content;
    public InputField addressInput;
    public Text statusText;
    
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
    }

    public void StartRecovery()
    {
        if (string.IsNullOrEmpty(addressInput.text))
        {
            Debug.LogWarning("Address is null or empty.");
            return;
        }
        
        //TODO should also check if it's a valid address

        if (string.IsNullOrEmpty(OFStaticData.OFplayerValue))
        {
            Debug.LogError("OFplayerValue is null or empty.");
        }
        
        AzureFunctionCaller.StartRecovery(OFStaticData.OFplayerValue, addressInput.text);
    }

    #region PRIVATE_METHODS
    private void CompleteRecovery()
    {
        if (string.IsNullOrEmpty(addressInput.text))
        {
            Debug.LogWarning("Address is null or empty.");
            return;
        }
        
        //TODO should also check if it's a valid address

        if (string.IsNullOrEmpty(OFStaticData.OFplayerValue))
        {
            Debug.LogError("OFplayerValue is null or empty.");
        }
        
        AzureFunctionCaller.CompleteRecovery(OFStaticData.OFplayerValue, addressInput.text);
    }
    #endregion

    #region AZURE_FUNCTION_CALLER_CALLBACK_HANDLERS
    private void OnStartRecoverySuccess(string obj)
    {
        throw new System.NotImplementedException();
    }
    
    private void OnStartRecoveryFailure(PlayFabError obj)
    {
        throw new System.NotImplementedException();
    }
    
    private void OnCompleteRecoverySuccess(string obj)
    {
        throw new System.NotImplementedException();
    }
    
    private void OnCompleteRecoveryFailure(PlayFabError obj)
    {
        throw new System.NotImplementedException();
    }
    #endregion
}
