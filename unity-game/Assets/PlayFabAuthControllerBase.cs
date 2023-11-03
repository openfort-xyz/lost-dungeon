using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public abstract class PlayFabAuthControllerBase : MonoBehaviour
{
    [SerializeField]
    private GetPlayerCombinedInfoRequestParams infoRequestParameters;
    protected GetPlayerCombinedInfoRequestParams InfoRequestParameters => infoRequestParameters;

    public static event Action<LoginResult> OnLoginSuccess;
    public static event Action<PlayFabError> OnLoginFailure;

    protected void RaiseLoginSuccess(LoginResult result)
    {
        OnLoginSuccess?.Invoke(result);
    }

    protected void RaiseLoginFailure(PlayFabError error)
    {
        OnLoginFailure?.Invoke(error);
    }
}
