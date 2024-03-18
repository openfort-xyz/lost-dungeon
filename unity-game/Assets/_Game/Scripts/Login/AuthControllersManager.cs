using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuthControllersManager : MonoBehaviour
{
    [Header("Controllers")]
    public LoginSceneManager loginSceneManager; //TODO we need to change it to DefaultAuthController at some point
    public AppleAuthController appleController;
    public GooglePlayAuth googlePlayController;

    // Not using it as we now use SocialLoginPanel.cs
    /*
    private void Awake()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                loginSceneManager.StartLogin();
                break;
            case RuntimePlatform.OSXPlayer:
                appleController.Initialize();
                break;
            case RuntimePlatform.WindowsPlayer:
                loginSceneManager.StartLogin();
                break;
            case RuntimePlatform.WindowsEditor:
                loginSceneManager.StartLogin();
                break;
            case RuntimePlatform.IPhonePlayer:
                appleController.Initialize();
                break;
            case RuntimePlatform.Android:
                googlePlayController.Authenticate();
                break;
            case RuntimePlatform.WebGLPlayer:
                //TODO double-check this
                loginSceneManager.StartLogin();
                break;
            case RuntimePlatform.LinuxPlayer:
                //TODO
                break;
            case RuntimePlatform.LinuxEditor:
                //TODO
                break;
            case RuntimePlatform.PS4:
                //TODO
                break;
            case RuntimePlatform.XboxOne:
                //TODO
                break;
            case RuntimePlatform.tvOS:
                //TODO
                break;
            case RuntimePlatform.Switch:
                //TODO
                break;
            case RuntimePlatform.Lumin:
                //TODO
                break;
            case RuntimePlatform.Stadia:
                //TODO
                break;
            case RuntimePlatform.CloudRendering:
                //TODO
                break;
            case RuntimePlatform.GameCoreXboxSeries:
                //TODO
                break;
            case RuntimePlatform.GameCoreXboxOne:
                //TODO
                break;
            case RuntimePlatform.PS5:
                //TODO
                break;
            case RuntimePlatform.EmbeddedLinuxArm64:
                //TODO
                break;
            case RuntimePlatform.EmbeddedLinuxArm32:
                //TODO
                break;
            case RuntimePlatform.EmbeddedLinuxX64:
                //TODO
                break;
            case RuntimePlatform.EmbeddedLinuxX86:
                //TODO
                break;
            case RuntimePlatform.LinuxServer:
                //TODO
                break;
            case RuntimePlatform.WindowsServer:
                //TODO
                break;
            case RuntimePlatform.OSXServer:
                //TODO
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    */
}
