using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using WalletConnectUnity.Modal;

public class InitSceneManager : MonoBehaviour
{
    public async void Start()
    {
        var wcStoragePath = $"{Application.persistentDataPath}/WalletConnect/storage.json";

#if !UNITY_WEBGL
        
        WalletConnectModal.Ready += (sender, args) => SceneManager.LoadScene("Login");
        
        // check if the file exists
        if(File.Exists(wcStoragePath))
        {
            // if file is found, delete it
            File.Delete(wcStoragePath);
            Debug.Log("WC storage file deleted.");
        }
        // call your action irrespective of file deletion
        await WalletConnectModal.InitializeAsync();
#else
        // for WebGL we don't use WalletConnect
        //ExecuteAction();
#endif
    }
}
