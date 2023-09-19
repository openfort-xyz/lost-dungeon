using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

[System.Serializable]
public class Wave
{
    public string waveName;
    public int noOfEnemies;
    public GameObject[] typeOfEnemies;
    public float spawnInterval;
}

public class WaveSpawnner : MonoBehaviour
{
    public static Action onGameCompleted;
    
    public Wave[] waves;
    public Transform[] spawnPoints;
    [SerializeField] Transform target;
    //public Animator animator;
    //public Text waveName;

    private Wave currentWave;
    private int currentWaveNumber;
    private float nextSpawnTime;

    private bool canSpawn = true;
    private bool canAnimate = false;
    

    private void Update()
    {
        currentWave = waves[currentWaveNumber];
        SpawnWave();
        GameObject[] totalEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (totalEnemies.Length == 0  )
        {
            if ( currentWaveNumber + 1 != waves.Length )
            {
                if ( canAnimate)
                {
                    //waveName.text = waves[currentWaveNumber + 1].waveName;
                    //animator.SetTrigger("WaveComplete");
                    SpawnNextWave();
                    canAnimate = false;
                }
            }
            else
            {
                Debug.Log("GameFinish");
                currentWaveNumber = 0;
                
                // Only trigger one time
                onGameCompleted?.Invoke();
                gameObject.SetActive(false);
            }
        }
    }

    private void SpawnNextWave()
    {
        currentWaveNumber++;
        canSpawn = true;
    }
    
    private void SpawnWave()
    {
        if (canSpawn && nextSpawnTime < Time.time)
        {
            GameObject randomEnemy = currentWave.typeOfEnemies[Random.Range(0, currentWave.typeOfEnemies.Length)];
            randomEnemy.GetComponent<EnemyBehaviour>().target = target;
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Instantiate(randomEnemy, randomPoint.position, Quaternion.identity);
            currentWave.noOfEnemies--;
            nextSpawnTime = Time.time + currentWave.spawnInterval;
            if (currentWave.noOfEnemies == 0)
            {
                canSpawn = false;
                canAnimate = true;
            }
        }
    }
}
