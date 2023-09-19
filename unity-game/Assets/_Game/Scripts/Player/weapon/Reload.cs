using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reload : MonoBehaviour
{
    [SerializeField] GameObject fill;

    bool startReload = false;
    float time = 1;
    float TimeStartedLerp;

    public void StartReloading(float time)
    {
        gameObject.SetActive(true);
        fill.transform.localScale = new Vector3(0,1,1);
        startReload = true;
        this.time = time;
        TimeStartedLerp = Time.time;
        Invoke(nameof(Stop), time);
    }

    void Stop()
    {
        startReload = false;
        fill.transform.localScale = new Vector3(0, 1, 1);
    }

    private void Update()
    {
        if (startReload)
        {
            fill.transform.localScale = new Vector3(Lerp(0f,1f, TimeStartedLerp, time), 1, 1);

        }
    }


    float Lerp(float start,float end,float timeStartedLerp,float lerpTime = 1)
    {
        float timeScinceStarted = Time.time - timeStartedLerp;
        float percentageComplete = timeScinceStarted / lerpTime;

        var result = Mathf.Lerp( start,end, percentageComplete);
        return result;
    }

}
