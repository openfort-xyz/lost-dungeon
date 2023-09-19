using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class BulletPhysics : MonoBehaviour {

    public float moveSpeed = 150f;

    public void Setup(Vector3 shootDir) {
        Rigidbody2D rigidbody2D = GetComponent<Rigidbody2D>();
        rigidbody2D.AddForce(shootDir * moveSpeed*Time.deltaTime, ForceMode2D.Impulse);
        
        transform.eulerAngles = new Vector3(0, 0, UtilsClass.GetAngleFromVectorFloat(shootDir));
        Destroy(gameObject, 5f);
    }
    
    private void OnTriggerEnter2D(Collider2D collider) {
        // Physics Hit Detection
        Target target = collider.GetComponent<Target>();
        if (target != null) {
            // Hit a Target
            target.Damage();
            Destroy(gameObject);
        }
    }

}
