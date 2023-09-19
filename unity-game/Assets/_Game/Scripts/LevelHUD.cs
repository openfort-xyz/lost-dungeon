using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelHUD : MonoBehaviour
{
    public static LevelHUD instance;
    [SerializeField] TMPro.TMP_Text coins, score;

    private void Awake()
    {
        instance = this;
    }

    public void UpdateCoins(int coins)
    {
        this.coins.text = coins.ToString(); 
    }

    public void UpdateScore(int score)
    {
        this.score.text = score.ToString();
    }

}
