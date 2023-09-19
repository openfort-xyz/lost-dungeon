using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniDemonEnemy : EnemyBehaviour
{
    [SerializeField] LayerMask player;
    [SerializeField] GameObject explosionvfx;

    GameObject DummyTarget;
    float detectionRange = 4;
    float Damagerange = 1;
    bool canFindTarget = true;

    private void Start()
    {
        target = null;
        detectionRange = 4;
        Damagerange = 1;
        
        //if (target == null)
        {
            DummyTarget = new GameObject("Dummy Target");
            DummyTarget.transform.position = transform.position;
            target = DummyTarget.transform;
            Destroy(DummyTarget, Random.Range(2, 5));
        }
    }

    public override void Update()
    {
        base.Update();

        if(canFindTarget)
            TryToFindTarget();

        SetDummyTarget();

    }

    void SetDummyTarget()
    {
        if (target == null)
        {
            DummyTarget = new GameObject("Dummy Target");
            DummyTarget.transform.position = Vector3.zero;

            Vector2 Direction = Vector2.right;
            Direction = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Direction;

            DummyTarget.transform.position = transform.position + (Vector3)Direction * 1;
            target = DummyTarget.transform;
            Destroy(DummyTarget, Random.Range(2, 5));
        }
    }

    void TryToFindTarget()
    {
        var targetCol = Physics2D.OverlapCircle(transform.position, detectionRange, player);
        if (targetCol)
        {
            target = targetCol.transform;
            canFindTarget = false;
            var col = GetComponentInChildren<Collider2D>();
            col.isTrigger = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(transform.position, detectionRange);

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            Destroy(gameObject);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        CinemachineShake.Instance.ShakeCamera(5f, .2f);

        var vfx = Instantiate(explosionvfx, transform.position, Quaternion.identity);
        GameObject.Destroy(vfx, 1f);
        var targetCol = Physics2D.OverlapCircleAll(transform.position, Damagerange);
        foreach (var target in targetCol)
        {
            var hp = target.GetComponent<Health>();
            if (hp) hp.DealDamage(5);
        }
        // deal area damage
    }

}
