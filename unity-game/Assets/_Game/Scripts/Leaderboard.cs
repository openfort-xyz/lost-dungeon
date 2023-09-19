using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.ClientModels;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] List<LeaderboardItem> leaderboardItems = new List<LeaderboardItem>();

    List<LeaderBoardPlayerData> leaderboard = new List<LeaderBoardPlayerData>();

    private void OnEnable()
    {
        refresh();

        // make sure that leaderboard[i] exists
        // if (leaderboard.Count > 0)
        // {
        //     for (int i = 0; i < leaderboardItems.Count; i++)
        //     {
        //         leaderboardItems[i].SetUp(leaderboard[i].playerName, leaderboard[i].Score.ToString());
        //     }
        // }

    }

    void refresh()
    {
        var request = new GetLeaderboardRequest { StatisticName = "PlatformScore", StartPosition = 0, MaxResultsCount = 10 };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardGet, OnError);
    }

    void OnLeaderboardGet(GetLeaderboardResult result)
    {
        Dictionary<string, LeaderBoardPlayerData> leaderboardDict = new Dictionary<string, LeaderBoardPlayerData>();

        foreach (var item in result.Leaderboard)
        {
            Debug.Log(item.DisplayName + ": " + item.StatValue);
            if (!leaderboardDict.ContainsKey(item.DisplayName))
            {
                leaderboardDict[item.DisplayName] = new LeaderBoardPlayerData() { playerName = item.DisplayName ?? "unknown", Score = item.StatValue };
            }
        }

        List<LeaderBoardPlayerData> leaderboard = new List<LeaderBoardPlayerData>(leaderboardDict.Values);

        // Update UI after leaderboard has been populated
        for (int i = 0; i < leaderboard.Count; i++)
        {
            leaderboardItems[i].SetUp(leaderboard[i].playerName, leaderboard[i].Score.ToString());
        }
    }

    void OnError(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
    }

}
