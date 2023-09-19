using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class Bullet : MonoBehaviour
{
    //TODO refactor
    private LevelManager _levelManager;
    
    private Vector3 shootDir;
    public float moveSpeed = 150f;
    int damage;
    bool canDestroy;
    [SerializeField] GameObject impact;
    [SerializeField] GameObject trail;
    [SerializeField] Transform impactPos;

    private void OnEnable()
    {
        //TODO refactor
        _levelManager = FindObjectOfType<LevelManager>();
    }

    public void Setup(Vector3 shootDir,float duration,float moveSpeed = 10,bool enableTrail = false,int damage=5,bool canDestroy= true) {
        this.shootDir = shootDir;
        this.moveSpeed = moveSpeed;
        this.damage = damage;
        this.canDestroy = canDestroy;
        trail.SetActive(enableTrail);
        transform.eulerAngles = new Vector3(0, 0, UtilsClass.GetAngleFromVectorFloat(shootDir));
        Destroy(gameObject, duration);
    }

    private void Update() {
        transform.position += shootDir * moveSpeed * Time.deltaTime;

/*        // Distance based Hit Detection
        float hitDetectionSize = 3f;
        Target target = Target.GetClosest(transform.position, hitDetectionSize);
        if (target != null) {
            target.Damage();
            Destroy(gameObject);
        }*/
    }


    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Joystick")) return; // We don't want to collide with the joystick :)
        
        // Physics Hit Detection
        Health target = collider.GetComponent<Health>();
        if (target != null)
        {
            // Hit a Target
            target.DealDamage(damage);
            
        }

        if (canDestroy)
        {
            Destroy(gameObject);
        }
        else
        {
            var impact_ = Instantiate(impact, impactPos.position, transform.rotation);
            GameObject.Destroy(impact_, 1);
        }
    }

    private void OnDestroy()
    {
        if (_levelManager.IsGameEnding()) return;
        
        var impact_ = Instantiate(impact, impactPos.position, transform.rotation);
        GameObject.Destroy(impact_, 1);
    }
}
