using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class Hyperlinks : MonoBehaviour
{
    public void OpenURLExplorer()
    {
        PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest()
        {
            PlayFabId = PlayFabSettings.staticPlayer.PlayFabId,
            Keys = null // Null fetches all keys for this user
        }, result =>
        {
            Debug.Log("Got user data");
            if (result.Data == null || !result.Data.ContainsKey("address"))
            {
                Debug.LogError("No address found");
            }
            else
            {
                var address = result.Data["address"].Value;
                Application.OpenURL("https://testnet.snowtrace.io/address/" + address);
            }
        }, error => Debug.LogError(error.GenerateErrorReport()));
    }
}
