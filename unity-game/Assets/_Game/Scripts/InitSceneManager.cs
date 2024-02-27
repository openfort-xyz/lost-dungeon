using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WalletConnectUnity.Modal;

public class InitSceneManager : MonoBehaviour
{
    private void OnEnable()
    {
        WalletConnectModal.Ready += (sender, args) => SceneManager.LoadScene("Login");
    }
}
