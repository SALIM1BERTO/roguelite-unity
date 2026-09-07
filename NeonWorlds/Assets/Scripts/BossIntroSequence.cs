using UnityEngine;
using System.Collections;
public class BossIntroSequence : MonoBehaviour
{
    public static bool isIntroPlaying;
    GameObject banner;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { isIntroPlaying=false; }
    public static void StartSequence(Vector3 position,PlanetGravity planet,System.Action onSpawnBoss)
    {
        if(isIntroPlaying) return;
        isIntroPlaying=true;
        var director=new GameObject("BossIntroDirector").AddComponent<BossIntroSequence>();
        director.StartCoroutine(director.Sequence(planet,onSpawnBoss));
    }
    IEnumerator Sequence(PlanetGravity planet,System.Action onSpawnBoss)
    {
        GameObject canvas=GameObject.Find("CanvasHUD");
        if(canvas!=null)
        {
            var panel=NeonUI.Panel("BossArrival",canvas.transform,new Vector2(.5f,1),new Vector2(0,-112),new Vector2(480,62),NeonUI.Surface);
            banner=panel.gameObject;
            NeonUI.Label("Title",panel.transform,"LEVIATÃƒ ORBITAL",18,NeonUI.Danger,new Vector2(.5f,1),new Vector2(0,-5),new Vector2(460,30),TextAnchor.MiddleCenter);
            NeonUI.Label("Hint",panel.transform,"AmeaÃ§a detectada. Prepare-se para desviar.",12,NeonUI.Muted,new Vector2(.5f,1),new Vector2(0,-34),new Vector2(460,22),TextAnchor.MiddleCenter);
        }
        GameAudio.Play(AudioCue.Teleport);
        yield return new WaitForSeconds(1.2f);
        if(planet!=null && GameManager.Instance!=null && !GameManager.Instance.isGameOver) onSpawnBoss?.Invoke();
        GameAudio.Play(AudioCue.Explosion);
        CameraFollow follow=Camera.main!=null ? Camera.main.GetComponent<CameraFollow>() : null;
        if(follow!=null) follow.TriggerShake(.25f,.12f);
        yield return new WaitForSeconds(.35f);
        Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(banner!=null) Destroy(banner);
        isIntroPlaying=false;
        if(GameManager.Instance!=null) GameManager.Instance.CheckPendingLevelUp();
    }
}
