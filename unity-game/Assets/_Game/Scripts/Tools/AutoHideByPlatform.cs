using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoHideByPlatform : MonoBehaviour
{
    public RuntimePlatform visiblePlatform;
    
    private void OnEnable()
    {
        if (Application.platform != visiblePlatform)
        {
            gameObject.SetActive(false);   
        }
    }
}
