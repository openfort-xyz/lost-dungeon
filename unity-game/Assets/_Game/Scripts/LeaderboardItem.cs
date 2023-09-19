using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardItem : MonoBehaviour
{
    [SerializeField] Text PlayerName, PlayerScore;

    public void SetUp(string PlayerName,string PlayerScore)
    {
        this.PlayerName.text = PlayerName;
        this.PlayerScore.text = PlayerScore; 
    }

    public void Clear()
    {
        this.PlayerName.text = "---------";
        this.PlayerScore.text = "0";
    }


}
