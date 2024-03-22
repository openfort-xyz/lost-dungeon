using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public abstract class PlayFabAuthControllerBase : MonoBehaviour
{
    [SerializeField]
    protected GetPlayerCombinedInfoRequestParams playerCombinedInfoRequestParams;
    protected GetPlayerCombinedInfoRequestParams PlayerCombinedInfoRequestParams => playerCombinedInfoRequestParams;

    public static event Action OnLoginStarted;
    public static event Action<LoginResult> OnLoginSuccess;
    public static event Action<PlayFabError> OnLoginFailure;
    public static event Action<RegisterPlayFabUserResult> OnRegisterSuccess;

    protected void RaiseLoginStarted()
    {
        OnLoginStarted?.Invoke();
    }
    
    protected void RaiseLoginSuccess(LoginResult result)
    {
        OnLoginSuccess?.Invoke(result);
    }

    protected void RaiseLoginFailure(PlayFabError error)
    {
        OnLoginFailure?.Invoke(error);
    }

    protected void RaiseRegisterSuccess(RegisterPlayFabUserResult result)
    {
        OnRegisterSuccess?.Invoke(result);
    }
}
