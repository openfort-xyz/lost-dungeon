using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopAnimEventHandler : MonoBehaviour
{
    public static Action onShopClosed;
    public void OnAnimationClose()
    {
        Debug.Log("Shop closed.");
        
        onShopClosed?.Invoke();
        gameObject.SetActive(false);
    }
}
