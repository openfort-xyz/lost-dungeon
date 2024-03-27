using Cysharp.Threading.Tasks;
using Openfort;
using Openfort.Model;
using Openfort.Recovery;
using UnityEngine;

public class OpenfortController: MonoBehaviour
{
    public static OpenfortController Instance { get; private set; }
    
    private OpenfortSDK _openfort;
    private string _oauthAccessToken;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public async UniTask AuthenticateWithOAuth(string idToken)
    {
        Debug.Log("PlayFab session ticket: " + idToken);
    
        _openfort = new OpenfortSDK(OFStaticData.PublishableKey); 
        _oauthAccessToken = await _openfort.AuthenticateWithOAuth(OAuthProvider.Playfab, idToken, TokenType.IdToken);
        Debug.Log("Access Token: " + _oauthAccessToken);
        
        try
        {
            _openfort.ConfigureEmbeddedSigner(4337);
        }
        catch (MissingRecoveryMethod)
        {
            await _openfort.ConfigureEmbeddedRecovery(new PasswordRecovery("secret"));
        }
    }
}