using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GuestToRegisteredController : MonoBehaviour
{
    public UnityEvent onGuestRegistered;
    
    public Configuration configurationPanel;
    
    public GameObject view;
    public GameObject content;
    public InputField emailInput;
    public InputField passwordInput;
    public Text statusText;

    private Dictionary<string, object> _guestUserData = new Dictionary<string, object>();

    private void OnEnable()
    {
        AzureFunctionCaller.onTransferUserDataSuccess += OnTransferUserDataSuccess;
        AzureFunctionCaller.onTransferUserDataFailure += OnTransferUserDataFailure;
    }

    private void OnDisable()
    {
        AzureFunctionCaller.onTransferUserDataSuccess -= OnTransferUserDataSuccess;
        AzureFunctionCaller.onTransferUserDataFailure -= OnTransferUserDataFailure;
    }

    public void Activate(bool status)
    {
        view.SetActive(status);

        if (status)
        {
            // Create the request object
            GetUserDataRequest getUserDataRequest = new GetUserDataRequest();

            // Make the API call
            PlayFabClientAPI.GetUserReadOnlyData(getUserDataRequest,
                result =>  // Inline success callback
                {
                    if (result.Data == null)
                    {
                        string message = "No guest user data.";
                        Debug.LogError(message);
                        statusText.text = message;
                        return;
                    }
                
                    Debug.Log("Guest user data retrieved.");
                    foreach (var data in result.Data)
                    {
                        _guestUserData.Add(data.Key, data.Value.Value);
                    }
                },
                error =>  // Inline failure callback
                {
                    string errorMessage = error.GenerateErrorReport();
                    Debug.LogError(errorMessage);
                    statusText.text = errorMessage;
                    // Show the content
                    content.SetActive(true);
                }
            );
        }
    }
    
    
    public void ConvertToRegisteredUser()
    {
        if (string.IsNullOrEmpty(emailInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            string message = "Please fill in Email and Password.";
            Debug.Log(message);
            statusText.text = message;
            return;
        }
        
        // Hide the content
        content.SetActive(false);
        
        statusText.text = "Registering user...";

        // Assuming you have the Custom ID for the guest user
        string guestCustomId = GetGuestCustomId();

        if (string.IsNullOrEmpty(guestCustomId))
        {
            string message = "Guest custom id is null.";
            Debug.LogError(message);
            statusText.text = message;
            //TODO?
            return;
        }

        var request = new RegisterPlayFabUserRequest
        {
            Email = emailInput.text,
            Password = passwordInput.text,
            RequireBothUsernameAndEmail = false, 
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, result =>
        {
            // Account registered successfully
            LinkGuestAccountToRegistered(guestCustomId);
            statusText.text = "User registered. Linking guest account...";
        },
        error =>
        {
            string errorMessage = error.GenerateErrorReport();
            Debug.LogError(errorMessage);
            statusText.text = errorMessage;
            // Show the content
            content.SetActive(true);
        });
    }

    void LinkGuestAccountToRegistered(string guestCustomId)
    {
        var request = new LinkCustomIDRequest
        {
            CustomId = guestCustomId,
            ForceLink = true
        };

        PlayFabClientAPI.LinkCustomID(request, result =>
        {
            // Guest account linked to the registered account
            string message = "Guest account linked to registered account.";
            Debug.Log(message);
            statusText.text = message;

            // Log in the user
            var loginRequest = new LoginWithEmailAddressRequest()
            {
                Email= emailInput.text,
                Password = passwordInput.text
            };

            PlayFabClientAPI.LoginWithEmailAddress(loginRequest, loginResult =>
                {
                    message = "User logged in successfully.";
                    Debug.Log(message);
                    statusText.text = message;

                    if (_guestUserData == null)
                    {
                        Debug.LogError("GuestUserData is null.");
                        return;
                    }

                    statusText.text = "Transferring guest user data...";
                    AzureFunctionCaller.TransferUserData(_guestUserData);
                },
                error =>
                {
                    message = error.GenerateErrorReport();
                    Debug.LogError(message);
                    statusText.text = message;
                    // Show the content
                    content.SetActive(true);
                });
        },
        error =>
        {
            var message = error.GenerateErrorReport();
            Debug.LogError(message);
            statusText.text = message;
            // Show the content
            content.SetActive(true);
        });
    }
    
    private void OnTransferUserDataSuccess(string result)
    {
        Debug.Log(result);
        content.SetActive(true);
        onGuestRegistered?.Invoke();
        Activate(false);
    }
    
    private void OnTransferUserDataFailure(PlayFabError error)
    {
        var message = error.GenerateErrorReport();
        Debug.LogError(message);
        statusText.text = message;
        // Show the content
        content.SetActive(true);
    }

    private string GetGuestCustomId()
    {
        if (string.IsNullOrEmpty(configurationPanel.guestCustomId))
        {
            Debug.LogError("Guest custom id is null.");
            return null;
        }
        
        return configurationPanel.guestCustomId;
    }
}