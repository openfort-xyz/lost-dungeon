using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class Crate : MonoBehaviour {

    private HealthSystem healthSystem;

    private void Awake() {
        healthSystem = new HealthSystem(300);
    }

    private void Start() {
        World_Bar healthBar = new World_Bar(transform, new Vector3(0, 9), new Vector3(7, 1.2f), Color.grey, Color.red, 1f, 1000, new World_Bar.Outline { color = Color.black, size = .5f });
        healthSystem.OnHealthChanged += (object sender, EventArgs e) => {
            healthBar.SetSize(healthSystem.GetHealthNormalized());
        };
    }

    public void Damage() {
        int damageAmount = UnityEngine.Random.Range(28, 44);
        DamagePopup.Create(transform.position, damageAmount, false);

        healthSystem.Damage(damageAmount);
    }

}
