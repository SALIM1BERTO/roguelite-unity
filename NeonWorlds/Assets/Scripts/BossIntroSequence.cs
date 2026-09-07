using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossIntroSequence : MonoBehaviour
{
    public static bool isIntroPlaying = false;

    public static void StartSequence(Vector3 bossSpawnPos, PlanetGravity planet, System.Action onSpawnBoss)
    {
        GameObject introObj = new GameObject("BossIntroDirector");
        BossIntroSequence seq = introObj.AddComponent<BossIntroSequence>();
        seq.StartCoroutine(seq.ExecuteSequence(bossSpawnPos, planet, onSpawnBoss));
    }

    void OnDestroy()
    {
        isIntroPlaying = false;
    }

    private IEnumerator ExecuteSequence(Vector3 bossSpawnPos, PlanetGravity planet, System.Action onSpawnBoss)
    {
        isIntroPlaying = true;
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        CameraFollow camFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

        // 1. Setup Distortion & Glitch Overlay
        GameObject glitchPanel = null;
        Image flashImg = null;
        GameObject alertBanner = null;

        if (canvasObj != null)
        {
            glitchPanel = new GameObject("BossIntroGlitchPanel");
            glitchPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform gRt = glitchPanel.AddComponent<RectTransform>();
            gRt.anchorMin = Vector2.zero; gRt.anchorMax = Vector2.one;
            gRt.sizeDelta = Vector2.zero; gRt.anchoredPosition = Vector2.zero;

            // Subtle scanlines overlay (raycastTarget = false so it never blocks UI clicks)
            Image gBg = glitchPanel.AddComponent<Image>();
            gBg.color = new Color(0.8f, 0f, 0.2f, 0.12f);
            gBg.raycastTarget = false;

            // Warning Banner
            alertBanner = new GameObject("AlertBanner");
            alertBanner.transform.SetParent(glitchPanel.transform, false);
            RectTransform bRt = alertBanner.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.1f, 0.42f);
            bRt.anchorMax = new Vector2(0.9f, 0.58f);
            bRt.sizeDelta = Vector2.zero;
            bRt.anchoredPosition = Vector2.zero;

            Image bBg = alertBanner.AddComponent<Image>();
            bBg.color = new Color(0.04f, 0.04f, 0.08f, 0.92f);
            bBg.raycastTarget = false;
            Outline bOutline = alertBanner.AddComponent<Outline>();
            bOutline.effectColor = new Color(1f, 0.1f, 0.2f, 0.9f);
            bOutline.effectDistance = new Vector2(3, -3);

            GameObject txtObj = new GameObject("AlertText");
            txtObj.transform.SetParent(alertBanner.transform, false);
            RectTransform tRt = txtObj.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero; tRt.anchoredPosition = Vector2.zero;

            Text alertTxt = txtObj.AddComponent<Text>();
            alertTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            alertTxt.text = ">>> ANOMALIA GRAVITACIONAL CRÍTICA <<<\nRUPTURA HIPERESPACIAL IMINENTE";
            alertTxt.fontSize = 28;
            alertTxt.fontStyle = FontStyle.Bold;
            alertTxt.alignment = TextAnchor.MiddleCenter;
            alertTxt.color = new Color(1f, 0.2f, 0.3f);
            alertTxt.raycastTarget = false;

            // Fullscreen Flash image (hidden initially)
            GameObject flashObj = new GameObject("BangFlash");
            flashObj.transform.SetParent(canvasObj.transform, false);
            RectTransform fRt = flashObj.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.sizeDelta = Vector2.zero; fRt.anchoredPosition = Vector2.zero;
            flashImg = flashObj.AddComponent<Image>();
            flashImg.color = new Color(1f, 1f, 1f, 0f);
            flashImg.raycastTarget = false;
        }

        // 2. Phase 1: Pre-Spawn Tension (1.6 seconds of growing glitch and screen tremors)
        float elapsed = 0f;
        float duration = 1.6f;
        GameAudio.Play(AudioCue.Teleport);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            // Increasing camera rumble
            if (camFollow != null)
            {
                camFollow.TriggerShake(0.1f, Mathf.Lerp(0.15f, 0.6f, progress));
            }

            // Glitch banner jitter & pulse
            if (alertBanner != null)
            {
                RectTransform brt = alertBanner.GetComponent<RectTransform>();
                brt.anchoredPosition = new Vector2(Random.Range(-4f, 4f) * progress, Random.Range(-4f, 4f) * progress);
                Image bImg = alertBanner.GetComponent<Image>();
                float pulse = Mathf.PingPong(Time.unscaledTime * 8f, 1f);
                bImg.color = Color.Lerp(new Color(0.04f, 0.04f, 0.08f, 0.92f), new Color(0.4f, 0f, 0.1f, 0.95f), pulse);
            }

            yield return null;
        }

        // 3. Phase 2: THE BANG! (Instant of Rupture)
        if (glitchPanel != null) Destroy(glitchPanel);

        // Huge Screen Flash & Violent Camera Punch
        if (camFollow != null)
        {
            camFollow.TriggerShake(1.2f, 2.0f);
        }
        GameAudio.Play(AudioCue.Explosion);

        // Expanding Planetary Shockwave Ring
        if (planet != null)
        {
            GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shockwave.name = "PlanetaryShockwave";
            shockwave.transform.position = bossSpawnPos;
            shockwave.transform.SetParent(planet.transform, true);
            Destroy(shockwave.GetComponent<Collider>());

            MeshRenderer mr = shockwave.GetComponent<MeshRenderer>();
            Material swMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color neonCyan = new Color(0f, 1f, 1f, 0.8f);
            swMat.SetColor("_BaseColor", neonCyan);
            swMat.EnableKeyword("_EMISSION");
            swMat.SetColor("_EmissionColor", neonCyan * 6f);
            mr.material = swMat;

            StartCoroutine(ExpandShockwave(shockwave, mr, swMat));
        }

        // Spawn the Boss Entity
        onSpawnBoss?.Invoke();

        // Fade out the flash
        if (flashImg != null)
        {
            float fTime = 0f;
            flashImg.color = new Color(1f, 1f, 1f, 0.95f);
            while (fTime < 0.35f)
            {
                fTime += Time.unscaledDeltaTime;
                flashImg.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.95f, 0f, fTime / 0.35f));
                yield return null;
            }
            Destroy(flashImg.gameObject);
        }

        isIntroPlaying = false;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckPendingLevelUp();
        }

        Destroy(gameObject);
    }

    private IEnumerator ExpandShockwave(GameObject obj, MeshRenderer mr, Material mat)
    {
        float t = 0f;
        float expandDuration = 0.7f;
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 endScale = Vector3.one * 28f;

        while (t < expandDuration)
        {
            if (obj == null) yield break;
            t += Time.unscaledDeltaTime;
            float p = t / expandDuration;
            obj.transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.Sqrt(p));
            mat.SetColor("_BaseColor", new Color(0f, 1f, 1f, 1f - p));
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }
}
