using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWeaponHolder : MonoBehaviour
{
    [SerializeField] float sortYLimit;
    SpriteRenderer weaponVisual;
    EnemyBehaviour enemy;

    private void Awake()
    {
        weaponVisual = GetComponentInChildren<SpriteRenderer>();
        enemy = GetComponentInParent<EnemyBehaviour>();
    }

    void Update()
    {
        if (enemy)
        {
            if(enemy.target)
            faceMouse(enemy.target.position);
        }

    }

    void faceMouse(Vector3 target)
    {
        Vector3 targetPosition = target;

        if (weaponVisual != null)
        {
            // snap weapon
            if (targetPosition.x > transform.position.x) weaponVisual.flipY = false;
            else if (targetPosition.x < transform.position.x) weaponVisual.flipY = true;

            if (targetPosition.y > transform.position.y + sortYLimit) weaponVisual.sortingOrder = 0;
            else if (targetPosition.y < transform.position.y + sortYLimit) weaponVisual.sortingOrder = 2;
        }


        Vector2 direction = new Vector2(
            targetPosition.x - transform.position.x,
            targetPosition.y - transform.position.y
        );

        transform.right = direction;
    }

}
