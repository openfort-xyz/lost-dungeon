using System;
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

    [Header("PlayFab Auth Controllers")]
    public GoogleAuthController googleAuthController;
    
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

    private void Start()
    {
        // Get Openfort client with publishable key.
        _openfortClient = new OpenfortClient(OFStaticData.PublishableKey);
    }

    private void OnEnable()
    {
        PlayFabAuthControllerBase.OnLoginStarted += () => loginPanel.SetActive(false);
        googleAuthController.OnLoginSuccess += OnLoginSuccess;
        googleAuthController.OnLoginFailure += OnLoginFailure;
        
        AzureFunctionCaller.onCreateOpenfortPlayerSuccess += OnCreateOpenfortPlayerSuccess;
        AzureFunctionCaller.onCreateOpenfortPlayerFailure += OnCreateOpenfortPlayerFailure;
    }

    private void OnDisable()
    {
        googleAuthController.OnLoginSuccess -= OnLoginSuccess;
        googleAuthController.OnLoginFailure -= OnLoginFailure;
        
        AzureFunctionCaller.onCreateOpenfortPlayerSuccess -= OnCreateOpenfortPlayerSuccess;
        AzureFunctionCaller.onCreateOpenfortPlayerFailure -= OnCreateOpenfortPlayerFailure;
    }

    public void StartLogin()
    {
        loginPanel.SetActive(true);

        if (Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer)
        {
            statusTextLabel.text = "Press Q to exit the game anytime.";
        }
        
        // Check if PlayerPrefs has a key for "RememberMe" and it's set to 1 (True)
        if (PlayerPrefs.GetInt(PPStaticData.RememberMeKey, 0) == 1)
        {
            loginPanel.SetActive(false);

            // Retrieve CustomID from PlayerPrefs (Secure this using encryption in a real-world application)
            string storedCustomID = PlayerPrefs.GetString(PPStaticData.CustomIdKey, null);

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
    
    public void LoginUserWithGooglePlay(string googleAuthCode)
    {
        statusTextLabel.text = "Logging in with Google Play...";
        
        var loginRequest = new LoginWithGooglePlayGamesServicesRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            ServerAuthCode = googleAuthCode,
            CreateAccount = true,
            InfoRequestParameters = infoRequestParams
        };

        PlayFabClientAPI.LoginWithGooglePlayGamesServices(loginRequest, OnLoginWithGooglePlaySuccess, OnLoginFailure);
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
                break;
            case Web3AuthService.State.WalletConnecting:
                statusTextLabel.text = "Connecting...";
                connectWalletPanel.SetActive(false);
                break;
            case Web3AuthService.State.WalletConnecting_Web3AuthCompleted:
                statusTextLabel.text = "Please connect with " + TrimWalletAddress(OFStaticData.OFownerAddressValue);
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
                if (string.IsNullOrEmpty(OFStaticData.OFplayerValue))
                {
                    Debug.LogError("No OFplayer in StaticData");
                    statusTextLabel.text = "Error: no OFplayer in StaticData";
                    return;
                }
                PlayerPrefs.SetString(PPStaticData.LastPlayerKey, OFStaticData.OFplayerValue);
                statusTextLabel.text = "Self-custody ENABLED!";
                Invoke(nameof(LoadMenuScene), 1f);
                break;
            case Web3AuthService.State.WrongOwnerAddress:
                //TODO?
                break;
            case Web3AuthService.State.Disconnecting:
                statusTextLabel.text = "Disconnecting...";
                break;
            case Web3AuthService.State.Disconnected:
                connectWalletPanel.SetActive(true);
                statusTextLabel.text = "Wallet disconnected. Please try again.";
                break;
            case Web3AuthService.State.Disconnected_Web3AuthCompleted:
                connectWalletPanel.SetActive(false);
                loginPanel.SetActive(true);
                statusTextLabel.text = "Wallet disconnected. Please log in again.";
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
        statusTextLabel.text = "Logged in as: " + result.PlayFabId;
    
        // We get the CustomID we linked when we registered the user
        var request = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(request, accountInfoResult =>
        {
            Debug.Log("Got account info");

            // You can check if result.AccountInfo contains the CustomId field
            if (accountInfoResult.AccountInfo != null && accountInfoResult.AccountInfo.CustomIdInfo.CustomId != null)
            {
                string customId = accountInfoResult.AccountInfo.CustomIdInfo.CustomId;
                Debug.Log("Custom ID found: " + customId);

                // If "Remember Me" is checked, save this custom ID locally (securely)
                if (rememberMeToggle.isOn)
                {
                    PlayerPrefs.SetString(PPStaticData.CustomIdKey, customId);  // TODO Secure this using encryption in a real-world application
                    PlayerPrefs.SetInt(PPStaticData.RememberMeKey, 1);
            
                    Debug.Log("Added user CustomID to PlayerPrefs");
                }
                else
                {
                    PlayerPrefs.DeleteKey(PPStaticData.CustomIdKey);
                    PlayerPrefs.SetInt(PPStaticData.RememberMeKey, 0);
                    Debug.Log("Custom ID not saved");
                }
            }
            else
            {
                Debug.Log("Custom ID not found.");
            }
        }, error =>
        {
            Debug.LogError(error.GenerateErrorReport());
            statusTextLabel.text = "Error: " + error.GenerateErrorReport();
        });

        DecideWhereToGoNext(result);
    }
    
    private void OnLoginWithGooglePlaySuccess(LoginResult result)
    {
        Debug.LogFormat("Logged In as: {0}", result.PlayFabId);

        statusTextLabel.text = "Logged in as: " + result.PlayFabId;
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
            
            // IMPORTANT! We reset this in order for next user not be biased by it.
            web3AuthService.authCompletedOnce = false;
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

    public void OnSelfCustodyCloseClicked()
    {
        //TODO NEW debug it
        // Back to log in!
        ResetFormsAndStatusLabel();
        
        connectWalletPanel.SetActive(false);
        loginPanel.SetActive(true);
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
        if (userReadOnlyData.ContainsKey(OFStaticData.OFplayerKey))
        {
            // Save OFplayer to static data
            var currentOFplayer = userReadOnlyData[OFStaticData.OFplayerKey].Value;
            OFStaticData.OFplayerValue = currentOFplayer;

            if (userReadOnlyData.ContainsKey("custodial"))
            {
                // We can go to Menu as the OpenfortPlayer is existing and it's custodial
                LoadMenuScene();
            }
            else
            {
                // We assume it cointains OFownerAddressKey. We save it to static data
                var currentOwnerAddress = userReadOnlyData[OFStaticData.OFownerAddressKey].Value;
                OFStaticData.OFownerAddressValue = currentOwnerAddress;
                
                // Check if the device has a session key
                var sessionKey = _openfortClient.LoadSessionKey();
                if (sessionKey == null)
                {
                    // Check if the user that has completed the Web3 Auth successfully at least once
                    if (userReadOnlyData.ContainsKey(OFStaticData.Web3AuthCompletedOnceKey))
                    {
                        // We need to register a new session for the player, without creating a new openfort player as we would lose all the progress for that PlayFab user.
                        web3AuthService.authCompletedOnce = true;
                        web3AuthService.Connect();
                    }
                    else
                    {
                        web3AuthService.authCompletedOnce = false;
                        // There has been some error during web3 authentication and the user needs to repeat the process
                        connectWalletPanel.SetActive(true);
                    }
                }
                else
                {
                    // Check if the user that has completed the Web3 Auth successfully at least once
                    if (userReadOnlyData.ContainsKey(OFStaticData.Web3AuthCompletedOnceKey))
                    {
                        web3AuthService.authCompletedOnce = true;
                        //check if it's the same user as the last that logged in.
                        //If it's not, we need to remove current session key and go to the process of creating and registering a new one
                        if (PlayerPrefs.HasKey(PPStaticData.LastPlayerKey))
                        {
                            var lastPlayer = PlayerPrefs.GetString(PPStaticData.LastPlayerKey);
                            if (currentOFplayer == lastPlayer)
                            {
                                PlayerPrefs.SetString(PPStaticData.LastPlayerKey, currentOFplayer);
                                // We're good we can go to Menu
                                LoadMenuScene();
                            }
                            else
                            {
                                // We need to create a new session key for the user that tries to log in
                                web3AuthService.Connect();
                            }
                        }
                        else
                        {
                            // We probably have logged out and we need to register a new session key
                            web3AuthService.Connect();
                        }
                    }
                    else
                    {
                        web3AuthService.authCompletedOnce = false;
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
            web3AuthService.authCompletedOnce = false;
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
    
    private string TrimWalletAddress(string address)
    {
        if (address.Length < 11) // Minimum length to trim: 6 from the front, 1 in the middle, 4 from the end
            return address; // Return the original address if it's too short to trim

        string front = address.Substring(0, 6); // Get the first 6 characters
        string end = address.Substring(address.Length - 4); // Get the last 4 characters

        return $"{front}...{end}"; // Concatenate the parts with "..."
    }
    #endregion
}