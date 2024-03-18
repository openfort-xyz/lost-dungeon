using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SocialLoginPanel : MonoBehaviour
{
    [Header("Buttons")]
    public Button googleButton;
    public Button appleButton;
    public Button googlePlayButton;

    [Header("Other")]
    public TextMeshProUGUI notAvailableText;
    
    private void OnEnable()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                notAvailableText.gameObject.SetActive(true);
                break;
            case RuntimePlatform.OSXPlayer:
                appleButton.gameObject.SetActive(true);
                break;
            case RuntimePlatform.WindowsPlayer:
                notAvailableText.gameObject.SetActive(true);
                break;
            case RuntimePlatform.WindowsEditor:
                notAvailableText.gameObject.SetActive(true);
                break;
            case RuntimePlatform.IPhonePlayer:
                appleButton.gameObject.SetActive(true);
                break;
            case RuntimePlatform.Android:
                googlePlayButton.gameObject.SetActive(true);
                break;
            case RuntimePlatform.WebGLPlayer:
                googleButton.gameObject.SetActive(true);
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
}
