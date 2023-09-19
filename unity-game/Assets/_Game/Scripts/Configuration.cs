using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Internal;
using UnityEngine.SceneManagement;

public class Configuration : MonoBehaviour
{

    public Button LogoutButton;
    public Button ConnectWalletButton;
    public Text StatusText;

    private PlayFabAuthService _AuthService = PlayFabAuthService.Instance;

    public void Start()
    {
        LogoutButton.onClick.AddListener(OnLogoutButtonClicked);
    }

    private void OnLogoutButtonClicked()
    {
        _AuthService.ClearRememberMe();
        StatusText.text = "Signin info cleared";

        PlayFabClientAPI.ForgetAllCredentials();
        _AuthService.Email = string.Empty;
        _AuthService.Password = string.Empty;
        _AuthService.AuthTicket = string.Empty;
        _AuthService.Authenticate();

    }
}