using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthHUD : MonoBehaviour
{
    [SerializeField] Image heart1, heart2;
    [SerializeField] Sprite Full, Half, Empty;

    public static HealthHUD instance;

    [SerializeField] List<Image> hearts = new List<Image>();

    private void Awake()
    {
        instance = this;
    }

    public void SetHealth(float cHealth)
    {

        float count = cHealth / 10;

        int x = 0;

        foreach (var item in hearts)
        {
            item.sprite = Empty;
        }

        while (count > .5f)
        {
            hearts[x].sprite = Full;
            x++;
            count--;
        }


        if (count == .5f)
            hearts[x].sprite = Half;

    }

}
