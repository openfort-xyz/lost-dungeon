using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public GameObject player;
    
    [SerializeField] private GameObject gameOverPanel, joystick;
    [SerializeField] UnityEngine.UI.Text title, status, coins, score;

    public GameObject currentCoinsLabel, currentScoreLabel;
    
    private bool _endingGame;

    private void Awake()
    {
#if UNITY_ANDROID
        joystick.SetActive(true);
#endif
    }

    private void OnEnable()
    {
        AzureFunctionCaller.onMintCurrencySuccess += OnMintCurrencySuccess;
        AzureFunctionCaller.onRequestFailure += OnMintCurrencyFailure;
        
        WaveSpawnner.onGameCompleted += OnGameCompleted;
        Health.onGameOver += OnGameOver;
    }

    private void OnDisable()
    {
        AzureFunctionCaller.onMintCurrencySuccess -= OnMintCurrencySuccess;
        AzureFunctionCaller.onRequestFailure -= OnMintCurrencyFailure;
        
        WaveSpawnner.onGameCompleted -= OnGameCompleted;
        Health.onGameOver -= OnGameOver;
    }

    private void Update()
    {
        if (_endingGame) return;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            player.SetActive(false); // We prevent from getting shot during ending game process.
            OnGameOver();
        }
        
        if (Application.platform != RuntimePlatform.WindowsPlayer &&
            Application.platform != RuntimePlatform.OSXPlayer &&
            Application.platform != RuntimePlatform.LinuxPlayer) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Application.Quit();
        }
    }

    public bool IsGameEnding()
    {
        return _endingGame;
    }

    #region EVENT_HANDLERS
    private void OnGameCompleted()
    {
        title.text = "You Win!";
        HandleGameEnding();
    }
    
    private void OnGameOver()
    {
        title.text = "Game Over";
        HandleGameEnding();
    }
    #endregion
    
    private void HandleGameEnding()
    {
        _endingGame = true;
        
        currentCoinsLabel.SetActive(false);
        currentScoreLabel.SetActive(false);
        
        gameOverPanel.SetActive(true);
        
        if (joystick.activeSelf) joystick.SetActive(false);
        
        coins.text = Coin.Collected.ToString();
        score.text = EnemyBehaviour.score.ToString();
        
        if (EnemyBehaviour.score > 0)
        {
            status.text = "Updating player statistics...";
            var request = new UpdatePlayerStatisticsRequest()
            {
                Statistics = new List<StatisticUpdate>() { new StatisticUpdate() { StatisticName = "PlatformScore", Value = EnemyBehaviour.score } }
            };
            PlayFabClientAPI.UpdatePlayerStatistics(request, null, null);
            
            EnemyBehaviour.ResetCount();
            EnemyBehaviour.ended = true;
        }
        
        if (Coin.Collected > 0)
        {
            status.text = "Minting acquired coins...";
            AzureFunctionCaller.MintCurrency(Coin.Collected.ToString());
            
            Coin.ResetCount();
            return;
        }
        
        status.text = "You didn't score this time.";
        Invoke(nameof(EndGame), 4f);
    }

    void OnMintCurrencySuccess()
    {
        //back.SetActive(true);
        Debug.Log("MintCurrency successful.");
        SceneManager.LoadScene("Menu");
    }

    void OnMintCurrencyFailure()
    {
        Debug.Log("MintCurrency failed.");
        SceneManager.LoadScene("Menu");
    }

    private void EndGame()
    {
        Debug.Log("Game ended.");
        SceneManager.LoadScene("Menu");
    }
}
