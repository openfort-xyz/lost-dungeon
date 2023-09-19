using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{

    public static WeaponHUD instance;

   

    [SerializeField] Image weaponImage;
    [SerializeField] TMPro.TMP_Text ammo;
    [SerializeField] GameObject weaponVisual;

    private void Awake()
    {
        instance = this;
    }

    public void SetImage(Sprite weapon)
    {
        weaponVisual.SetActive(true);
        weaponImage.sprite = weapon;
    }

    public void SetAmmo(int ammo,int maxAmmo)
    {
        this.ammo.text = ammo.ToString()+"/"+maxAmmo;
    }

    public void Hide()
    {
        weaponVisual.SetActive(false);
        weaponImage.sprite = null;
    }
}
