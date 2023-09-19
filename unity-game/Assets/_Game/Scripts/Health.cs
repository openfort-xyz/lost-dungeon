using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public static Action onGameOver;
    
    [SerializeField] int maxHealth;
    [SerializeField] bool useHud = false;

    private int _currentHealth = 0;
    private string _myTag;

    private void OnEnable()
    {
        _myTag = gameObject.tag;
    }

    private void Start()
    {
        _currentHealth = maxHealth;
        if (useHud) HealthHUD.instance.SetHealth(_currentHealth);
    }

    public void DealDamage(int amount)
    {
        //if(currentHealth >= amount)
        {
            _currentHealth -= amount;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);

            if (useHud)
            {
                HealthHUD.instance.SetHealth(_currentHealth);
                CinemachineShake.Instance.ShakeCamera(2f, .1f);
            }
            
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;

                if (CompareTag("Player"))
                {
                    // Just trigger one time.
                    onGameOver?.Invoke();
                    gameObject.SetActive(false);
                }
                else
                {
                    // Enemy
                    Destroy(gameObject);
                }
            }
        }
    }
}
