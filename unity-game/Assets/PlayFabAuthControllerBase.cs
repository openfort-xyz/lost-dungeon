using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public abstract class PlayFabAuthControllerBase : MonoBehaviour
{
    [SerializeField]
    private GetPlayerCombinedInfoRequestParams playerCombinedInfoRequestParams;
    protected GetPlayerCombinedInfoRequestParams PlayerCombinedInfoRequestParams => playerCombinedInfoRequestParams;

    public event Action<LoginResult> OnLoginSuccess;
    public event Action<PlayFabError> OnLoginFailure;

    protected void RaiseLoginSuccess(LoginResult result)
    {
        OnLoginSuccess?.Invoke(result);
    }

    protected void RaiseLoginFailure(PlayFabError error)
    {
        OnLoginFailure?.Invoke(error);
    }
}
