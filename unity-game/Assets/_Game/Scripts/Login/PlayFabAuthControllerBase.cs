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
    public event Action<LoginResult> OnLoginSuccess;
    public event Action<PlayFabError> OnLoginFailure;

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
}
