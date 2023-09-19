using CodeMonkey.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHandGun : MonoBehaviour
{
    [SerializeField] GameObject flash;
    [SerializeField] float fireRate = .5f;
    [SerializeField] Transform projectile;
    [SerializeField] Transform startPoint;

    float lastShot = .0f;
    Animator anim;
    [SerializeField] bool canFire,enableTrail;
    [SerializeField] float bulletSpeed;

    EnemyBehaviour enemy;

    private void Awake()
    {
        anim = GetComponentInParent<Animator>();
        enemy = transform.parent.parent.GetComponentInParent<EnemyBehaviour>();
    }

    void Update()
    {
        if (canFire && enemy?.target)
        {
            Vector3 TargetPosition = enemy.target.position;

            Shoot(TargetPosition, startPoint.position);

        }
    }

    public void Shoot(Vector3 gunEndPointPosition, Vector3 shootPosition)
    {
        if (Time.time > fireRate + lastShot)
        {
            anim.SetTrigger("singleshot");
            flash.SetActive(true);
            ShootProjectile(shootPosition, gunEndPointPosition);
            CinemachineShake.Instance.ShakeCamera(2f, .1f);
            Invoke(nameof(disableFlash), .1f);
            lastShot = Time.time;
        }

    }

    void disableFlash()
    {
        flash.SetActive(false);
    }

    private void ShootProjectile(Vector3 gunEndPointPosition, Vector3 shootPosition)
    {
        Transform bulletTransform = Instantiate(projectile, gunEndPointPosition, Quaternion.identity);

        Vector3 shootDir = (shootPosition - gunEndPointPosition).normalized;
        bulletTransform.GetComponent<Bullet>().Setup(shootDir, 5f, bulletSpeed, enableTrail);
    }


}
