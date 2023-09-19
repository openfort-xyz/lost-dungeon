using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    private LevelManager _levelManager;
    
    public static int score = 0;
    public static bool ended = false;

    public static void ResetCount()
    {
        score = 0;
    }


    public float speed;
    public Transform target;
    public float minimumDistance;
    [SerializeField] GameObject Coin;
    [SerializeField] int RewardScore;

    protected Animator anim;
    SpriteRenderer enemyVisual;
    private Rigidbody2D rb;


    public virtual void Awake()
    {
        _levelManager = FindObjectOfType<LevelManager>();
        anim = GetComponentInChildren<Animator>();
        enemyVisual = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void Update()
    {
        if (target)
        {
            if (transform.position.x > target.position.x) enemyVisual.flipX = true;
            else if (transform.position.x < target.position.x) enemyVisual.flipX = false;
        }
        /*if (Vector2.Distance(transform.position, target.position) > minimumDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            anim.SetBool("move",true);
        }
        else
        {
            anim.SetBool("move", false);
        }*/

    }


    private void FixedUpdate()
    {
        if (target)
        {
            if (Vector2.Distance(transform.position, target.position) > minimumDistance)
            {
                var dir = (target.position - transform.position).normalized;
                rb.MovePosition(rb.position + (Vector2)dir * speed * Time.fixedDeltaTime);
                anim.SetBool("move", true);
            }
            else if (Vector2.Distance(transform.position, target.position) < minimumDistance - (minimumDistance / 2))
            {
                var dir = (target.position - transform.position).normalized;
                rb.MovePosition(rb.position + -(Vector2)dir * speed * Time.fixedDeltaTime);
                anim.SetBool("move", true);
            }
            else
            {
                rb.velocity = Vector2.zero;
                anim.SetBool("move", false);
            }
        }
    }

    public virtual void OnDestroy()
    {
        if (_levelManager.IsGameEnding()) return;
        
        Instantiate(Coin, transform.position, Quaternion.identity);
        
        if (!ended)
        {
            score += RewardScore;

            LevelHUD.instance.UpdateScore(score);

        }

        Debug.Log("dead "+ gameObject.name);
    }
}
