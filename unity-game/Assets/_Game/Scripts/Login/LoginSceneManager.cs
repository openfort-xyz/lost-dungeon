using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Openfort;

public class LoginSceneManager : MonoBehaviour
{
    [Header("Web3Auth")]
    public Web3AuthService web3AuthService;
    
    [Header("PlayFab")]
    // Settings for what data to get from playfab on login.
    public GetPlayerCombinedInfoRequestParams infoRequestParams;

    [Header("Login")]
    public GameObject loginPanel;
    public InputField email;
    public InputField password;
    public Toggle rememberMeToggle;

    [Header("Register")]
    public GameObject registerPanel;
    public InputField confirmPassword;

    [Header("Connect Wallet")]
    public GameObject connectWalletPanel;

    [Header("General")]
    public Text statusTextLabel;
    
    // OPENFORT
    private OpenfortClient _openfortClient;

    private void OnEnable()
    {
        AzureFunctionCaller.onCreateOpenfortPlayerSuccess += OnCreateOpenfortPlayerSuccess;
        AzureFunctionCaller.onCreateOpenfortPlayerFailure += OnCreateOpenfortPlayerFailure;
    }

    private void OnDisable()
    {
        AzureFunctionCaller.onCreateOpenfortPlayerSuccess -= OnCreateOpenfortPlayerSuccess;
        AzureFunctionCaller.onCreateOpenfortPlayerFailure -= OnCreateOpenfortPlayerFailure;
    }

    private void Start()
    {
        // Get Openfort client with publishable key.
        _openfortClient = new OpenfortClient(OpenfortStaticData.publishableKey);
        
        if (Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer)
        {
            statusTextLabel.text = "Press Q to exit the game anytime.";
        }
        
        // Check if PlayerPrefs has a key for "RememberMe" and it's set to 1 (True)
        if (PlayerPrefs.GetInt("RememberMe", 0) == 1)
        {
            loginPanel.SetActive(false);

            // Retrieve CustomID from PlayerPrefs (Secure this using encryption in a real-world application)
            string storedCustomID = PlayerPrefs.GetString("CustomID", null);

            // If CustomID exists
            if (!string.IsNullOrEmpty(storedCustomID))
            {
                statusTextLabel.text = $"Logging in as {storedCustomID}...";
                
                // Attempt to login using CustomID
                LoginWithCustomIDRequest request = new LoginWithCustomIDRequest
                {
                    CustomId = storedCustomID,
                    CreateAccount = false, // Set to false because this should already be an existing account
                    InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                    {
                        GetUserReadOnlyData = true,
                        UserReadOnlyDataKeys = null
                    }
                };

                PlayFabClientAPI.LoginWithCustomID(request, DecideWhereToGoNext, error =>
                {
                    PlayerPrefs.DeleteAll();
                    loginPanel.SetActive(true);
                    statusTextLabel.text = "Automatic login failed.";
                    Debug.Log("We couldn't log in using custom id");
                });
            }
        }
    }

    private void Update()
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer &&
            Application.platform != RuntimePlatform.OSXPlayer &&
            Application.platform != RuntimePlatform.LinuxPlayer) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Application.Quit();
        }
    }

    #region WEB3_AUTH_SERVICE_EVENT_HANDLERS
    public void OnWeb3AuthStateChanged(Web3AuthService.State state)
    {
        switch (state)
        {
            case Web3AuthService.State.None:
                connectWalletPanel.SetActive(false);
                loginPanel.SetActive(true);
                statusTextLabel.text = "Wallet disconnected. Please log in again.";
                break;
            case Web3AuthService.State.WalletConnecting:
                statusTextLabel.text = "Connecting...";
                connectWalletPanel.SetActive(false);
                break;
            case Web3AuthService.State.WalletConnectionCancelled:
                connectWalletPanel.SetActive(true);
                statusTextLabel.text = "Wallet connection cancelled.";
                break;
            case Web3AuthService.State.WalletConnected:
                statusTextLabel.text = "Wallet connection successful.";
                break;
            case Web3AuthService.State.RequestingMessage:
                statusTextLabel.text = "Requesting message...";
                break;
            case Web3AuthService.State.SigningMessage:
                statusTextLabel.text = "Please sign the message in your wallet.";
                break;
            case Web3AuthService.State.VerifyingSignature:
                statusTextLabel.text = "Sign successful. Verifying signature...";
                break;
            case Web3AuthService.State.RegisteringSession:
                statusTextLabel.text = "Registering openfort session...";
                break;
            case Web3AuthService.State.SigningSession:
                statusTextLabel.text = "Please sign the message in your wallet.";
                break;
            case Web3AuthService.State.SessionSigned:
                statusTextLabel.text = "Session signed successfully. completing process...";
                break;
            case Web3AuthService.State.Web3AuthSuccessful:
                if (string.IsNullOrEmpty(StaticPlayerData.OFplayer))
                {
                    Debug.LogError("No OFplayer in StaticPlayerData");
                    statusTextLabel.text = "Error: no OFplayer in StaticPlayerData";
                    return;
                }
                PlayerPrefs.SetString("LastPlayer", StaticPlayerData.OFplayer);
                statusTextLabel.text = "Self-custody ENABLED!";
                Invoke(nameof(LoadMenuScene), 1f);
                break;
            case Web3AuthService.State.Disconnecting:
                statusTextLabel.text = "Disconnecting...";
                break;
            case Web3AuthService.State.Disconnected:
                connectWalletPanel.SetActive(true);
                statusTextLabel.text = "Wallet disconnected. Please try again.";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
    #endregion
    
    #region PLAYFAB_EVENT_HANDLERS
    private void OnLoginSuccess(LoginResult result)
    {
        Debug.LogFormat("Logged In as: {0}", result.PlayFabId);
        statusTextLabel.text = "";
        
        // We get the CustomID we linked when we registered the user
        var request = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(request, accountInfoResult =>
        {
            Debug.Log("Got account info");

            // You can check if result.AccountInfo contains the CustomId field
            if (accountInfoResult.AccountInfo != null && accountInfoResult.AccountInfo.CustomIdInfo.CustomId != null)
            {
                string customId = accountInfoResult.AccountInfo.CustomIdInfo.CustomId;
                // If "Remember Me" is checked, save this custom ID locally (securely)
                if (rememberMeToggle.isOn)
                {
                    PlayerPrefs.SetString("CustomID", customId);  // TODO Secure this using encryption in a real-world application
                    PlayerPrefs.SetInt("RememberMe", 1);
                
                    Debug.Log("Added user CustomID to PlayerPrefs");
                }
                else
                {
                    PlayerPrefs.DeleteKey("CustomID");
                    PlayerPrefs.SetInt("RememberMe", 0);
                }
            }
            else
            {
                Debug.Log("Custom ID not found.");
            }
        }, error =>
        {
            Debug.LogError(error.GenerateErrorReport());
        });

        DecideWhereToGoNext(result);
    }
    
    private void OnRegistrationSuccess(RegisterPlayFabUserResult result)
    {
        Debug.Log("User registered and logged in successfully!");
        statusTextLabel.text = "";
        
        // Generate a secure custom ID for the user (e.g., a GUID)
        string customID = Guid.NewGuid().ToString();

        var linkCustomIDRequest = new LinkCustomIDRequest
        {
            CustomId = customID,  // The unique ID you've generated
            ForceLink = false // Set to true to overwrite any existing account with this Custom ID
        };
        PlayFabClientAPI.LinkCustomID(linkCustomIDRequest, requestResponse =>
        {
            Debug.Log("Successfully linked custom ID to the currently logged-in user");
            // We go to ConnectWallet panel
            connectWalletPanel.SetActive(true);
            
        }, error =>
        {
            Debug.LogError(error.GenerateErrorReport());
            Debug.Log("We couldn't link a custom ID to the user");
        });
    }
    
    private void OnLoginFailure(PlayFabError error)
    {
        switch (error.Error)
        {
            case PlayFabErrorCode.InvalidEmailAddress:
                statusTextLabel.text = "Invalid email address";
                loginPanel.SetActive(true);
                break;
            case PlayFabErrorCode.InvalidPassword:
                statusTextLabel.text = "Invalid password";
                loginPanel.SetActive(true);
                break;
            case PlayFabErrorCode.InvalidEmailOrPassword:
                statusTextLabel.text = "Invalid email or password";
                loginPanel.SetActive(true);
                break;
            case PlayFabErrorCode.AccountNotFound:
                // We go to registration panel
                registerPanel.SetActive(true);
                return;
            case PlayFabErrorCode.InvalidParams:
                statusTextLabel.text = "Invalid input parameters";
                loginPanel.SetActive(true);
                break;
            default:
                statusTextLabel.text = error.GenerateErrorReport();
                loginPanel.SetActive(true);
                break;
        }

        //Also report to debug console, this is optional.
        Debug.Log(error.Error);
        Debug.LogError(error.GenerateErrorReport());
    }
    
    private void OnRegistrationFailure(PlayFabError error)
    {
        Debug.LogError("User registration failed. Error: " + error.ErrorMessage);
        statusTextLabel.text = "Error occurred while registration. Please try again.";
        
        // We go back to Login panel
        loginPanel.SetActive(true);
    }
    
    private void OnGuestLoginSuccess(LoginResult result)
    {
        Debug.Log("Guest login successful!");
        CreateOpenfortPlayer();
    }

    private void OnGuestLoginFailure(PlayFabError error)
    {
        Debug.LogError("Guest login failed. Error: " + error.ErrorMessage);
        loginPanel.SetActive(true);
    }
    #endregion

    #region AZURE_FUNCTION_CALLBACK_HANDLERS
    private void OnCreateOpenfortPlayerSuccess(OpenfortPlayerResponse ofPlayerResponse)
    {
        statusTextLabel.text = "Openfort Player created successfully!";
        Invoke(nameof(LoadMenuScene), 1f);
    }
    private void OnCreateOpenfortPlayerFailure()
    {
        ClearStatusTextLabel();
        loginPanel.SetActive(true);
    }
    #endregion
    
    #region PUBLIC_BUTTON_METHODS
    public void OnLoginClicked()
    {
        if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(password.text))
        {
            statusTextLabel.text = "Please provide a correct username/password";
            return;
        }
        statusTextLabel.text = $"Logging in as {email.text}...";

        loginPanel.SetActive(false);
        
        // Try to login with email and password
        LoginWithEmailAddressRequest request = new LoginWithEmailAddressRequest
        {
            Email = email.text,
            Password = password.text,
            InfoRequestParameters = infoRequestParams
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }

    public void OnPlayAsGuestClicked()
    {
        statusTextLabel.text = "Logging in as a guest...";
        
        loginPanel.SetActive(false);
        
        // Try to login as guest
        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest
        {
            CustomId = Guid.NewGuid().ToString(), //TODO?
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnGuestLoginSuccess, OnGuestLoginFailure);
    }
    
    public void OnRegisterButtonClicked()
    {
        if (password.text != confirmPassword.text)
        {
            statusTextLabel.text = "Passwords do not match.";
            return;
        }

        registerPanel.SetActive(false);
        statusTextLabel.text = $"Registering User {email.text} ...";

        //Try to register new user
        RegisterPlayFabUserRequest request = new RegisterPlayFabUserRequest
        {
            Email = email.text,
            Password = password.text,
            RequireBothUsernameAndEmail = false // Set to true if you want to require a username in addition to email
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegistrationSuccess, OnRegistrationFailure);
    }
    
    public void OnCancelRegisterButtonClicked()
    {
        ResetFormsAndStatusLabel();

        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    public void OnConnectWalletClicked()
    {
        web3AuthService.Connect();
    }
    
    public void OnSkipButtonClicked()
    {
        connectWalletPanel.SetActive(false);
        CreateOpenfortPlayer();
    }

    public void OnBackToLoginClicked()
    {
        // We don't use it for the moment, can bring problems.
        ResetFormsAndStatusLabel();
        
        connectWalletPanel.SetActive(false);
        loginPanel.SetActive(true);

        //TODO logout from playfab
    }
    #endregion
    
    #region PRIVATE_METHODS
    private void CreateOpenfortPlayer()
    {
        AzureFunctionCaller.CreateOpenfortPlayer();
        statusTextLabel.text = "Creating an Openfort Player...";
    }

    private void DecideWhereToGoNext(LoginResult result)
    {
        var userReadOnlyData = result.InfoResultPayload.UserReadOnlyData;
        // We check if the PlayFab user has an Openfort Player assigned to its ReadOnlyData values.
        if (userReadOnlyData.ContainsKey("OFplayer") && userReadOnlyData.ContainsKey("address"))
        {
            // Save OFplayer to static data
            var currentOFplayer = userReadOnlyData["OFplayer"].Value;
            StaticPlayerData.OFplayer = currentOFplayer;

            if (userReadOnlyData.ContainsKey("custodial"))
            {
                // We can go to Menu as the OpenfortPlayer is existing and it's custodial
                LoadMenuScene();
            }
            else
            {
                // Check if the device has a session key
                var sessionKey = _openfortClient.LoadSessionKey();
                if (sessionKey == null)
                {
                    // Check if the user that has completed the Web3 Auth successfully at least once
                    if (userReadOnlyData.ContainsKey("Web3AuthCompletedOnce"))
                    {
                        // We need to register a new session for the player, without creating a new openfort player as we would lose all the progress for that PlayFab user.
                        web3AuthService.authCompletedOnce = true;
                        web3AuthService.Connect();
                    }
                    else
                    {
                        // There has been some error during web3 authentication and the user needs to repeat the process
                        connectWalletPanel.SetActive(true);
                    }
                }
                else
                {
                    // Check if the user that has completed the Web3 Auth successfully at least once
                    if (userReadOnlyData.ContainsKey("Web3AuthCompletedOnce"))
                    {
                        //check if it's the same user as the last that logged in.
                        //If it's not, we need to remove current session key and go to the process of creating and registering a new one
                        if (PlayerPrefs.HasKey("LastPlayer"))
                        {
                            var lastPlayer = PlayerPrefs.GetString("LastPlayer");
                            if (currentOFplayer == lastPlayer)
                            {
                                PlayerPrefs.SetString("LastPlayer", currentOFplayer);
                                // We're good we can go to Menu
                                LoadMenuScene();
                            }
                            else
                            {
                                // We need to create a new session key for the user that tries to log in
                                web3AuthService.authCompletedOnce = true;
                                web3AuthService.Connect();
                            }
                        }
                        else
                        {
                            // We probably have logged out and we need to register a new session key
                            web3AuthService.authCompletedOnce = true;
                            web3AuthService.Connect();
                        }
                    }
                    else
                    {
                        // There has been some error during web3 authentication and the user needs to repeat the process
                        connectWalletPanel.SetActive(true);
                    }
                }
            }
        }
        else
        {
            // It's a new user
            // We need to create an Openfort Player, either with self-custody account or not. The user will chose it in ConnectWallet panel. 
            connectWalletPanel.SetActive(true);
        }
    }

    private void LoadMenuScene()
    {
        SceneManager.LoadScene("Menu");
    }
    
    private void ResetFormsAndStatusLabel()
    {
        // Reset all forms
        email.text = string.Empty;
        password.text = string.Empty;
        confirmPassword.text = string.Empty;
        rememberMeToggle.isOn = false;
        
        ClearStatusTextLabel();
    }

    private void ClearStatusTextLabel()
    {
        statusTextLabel.text = string.Empty;
    }
    #endregion
}