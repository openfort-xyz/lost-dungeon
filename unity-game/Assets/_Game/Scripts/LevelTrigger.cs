using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTrigger : MonoBehaviour
{
    [SerializeField] GameObject ShopInteraction;
    [SerializeField] GameObject EnterInteraction;
    [SerializeField] TextMeshPro EnterInteractionText;
    
    bool CanTrigger;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (LobbyManager.Instance.currentState != LobbyManager.LobbyState.WeaponEquipped)
        {
            ShopInteraction.SetActive(true);
            return;
        }
        if (collision.CompareTag("Player"))
        {
#if UNITY_ANDROID
            EnterInteractionText.text = "Enter";
#endif
            EnterInteraction.SetActive(true);
            CanTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (LobbyManager.Instance.currentState != LobbyManager.LobbyState.WeaponEquipped)
        {
            ShopInteraction.SetActive(false);
            return;
        }
        
        if (collision.CompareTag("Player"))
        {
            EnterInteraction.SetActive(false);
            CanTrigger = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            EnterLevelScene();
        }
    }

    public void EnterLevelScene()
    {
        if (CanTrigger)
        {
            SceneManager.LoadScene("Level");
        }
    }
}
