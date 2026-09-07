using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    private static HitStop instance;
    private Coroutine hitStopRoutine;

    public static void EnsureExists()
    {
        if (instance != null) return;
        GameObject go = new GameObject("HitStopManager");
        instance = go.AddComponent<HitStop>();
        DontDestroyOnLoad(go);
    }

    public static void Trigger(float duration = 0.04f, float slowTimeScale = 0.05f)
    {
        if (Time.timeScale <= 0f) return;
        EnsureExists();
        instance.DoHitStop(duration, slowTimeScale);
    }

    public void DoHitStop(float duration, float slowTimeScale)
    {
        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
        }
        hitStopRoutine = StartCoroutine(HitStopRoutine(duration, slowTimeScale));
    }

    private IEnumerator HitStopRoutine(float duration, float slowTimeScale)
    {
        float previousScale = Time.timeScale > 0.1f ? Time.timeScale : 1f;
        Time.timeScale = slowTimeScale;

        yield return new WaitForSecondsRealtime(duration);

        if (Time.timeScale == slowTimeScale)
        {
            Time.timeScale = previousScale;
        }
        hitStopRoutine = null;
    }
}
