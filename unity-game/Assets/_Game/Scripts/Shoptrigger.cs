using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Shoptrigger : MonoBehaviour
{
    public static Action onShopTriggered;
    
    [SerializeField] GameObject ShopInteraction;
    [SerializeField] TextMeshPro ShopInteractionText;
    bool CanTrigger;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
#if UNITY_ANDROID || UNITY_IOS
            ShopInteractionText.text = "Open";
#endif
            ShopInteraction.SetActive(true);
            CanTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ShopInteraction.SetActive(false);
            CanTrigger= false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            EnterShop();
        }
    }

    public void EnterShop()
    {
        if (!CanTrigger) return;
        
        ShopInteraction.SetActive(false);
        onShopTriggered?.Invoke();
        Debug.Log("Pressed E to open Shop.");

        CanTrigger = false;
    }
}
