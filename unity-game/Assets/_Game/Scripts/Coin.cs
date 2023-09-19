using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public static int Collected;

    public static void ResetCount()
    {
        Collected = 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Collected++;
            Destroy(gameObject);
            LevelHUD.instance.UpdateCoins(Collected);
        }
    }
}
