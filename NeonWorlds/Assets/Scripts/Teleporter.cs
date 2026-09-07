using UnityEngine;
using System.Collections;

public class Teleporter : MonoBehaviour
{
    public PlanetGravity targetPlanet;
    public float cooldown = 0f;
    public bool isLocked = false;

    // Progression ring (0 to 1 over 3 seconds)
    public const float FILL_DURATION = 3.0f;
    public const float REGRESS_DURATION = 2.0f;
    [Range(0f, 1f)] public float fillProgress = 0f;
    private bool isPlayerInside = false;

    private GameObject visualsRoot;
    private GameObject lockBarrier;
    private Material barrierMaterial;
    private Transform ringsTransform;
    private LineRenderer progressRing;
    private LineRenderer trackRing;
    private Material progressMaterial;

    void Awake()
    {
        EnsureVisuals();
    }

    void Start()
    {
        EnsureVisuals();
        if (targetPlanet == null)
        {
            AutoResolveTargetPlanet();
        }
    }

    void AutoResolveTargetPlanet()
    {
        PlanetGravity[] all = FindObjectsByType<PlanetGravity>(FindObjectsInactive.Exclude);
        foreach (var p in all)
        {
            if (p.transform != transform.parent)
            {
                targetPlanet = p;
                break;
            }
        }
    }

    void EnsureVisuals()
    {
        if (visualsRoot != null) return;

        visualsRoot = new GameObject("TeleporterVisuals");
        visualsRoot.transform.SetParent(transform, false);
        visualsRoot.transform.localPosition = Vector3.zero;
        visualsRoot.transform.localRotation = Quaternion.identity;

        BossWorldMotion.SetWorldScale(visualsRoot.transform, Vector3.one);

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

        // 1. Ground Platform Base Disc
        GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisc.name = "PlatformBase";
        baseDisc.transform.SetParent(visualsRoot.transform, false);
        baseDisc.transform.localPosition = new Vector3(0, 0.04f, 0);
        baseDisc.transform.localScale = new Vector3(5.6f, 0.08f, 5.6f);
        Destroy(baseDisc.GetComponent<Collider>());

        MeshRenderer mr = baseDisc.GetComponent<MeshRenderer>();
        Material baseMat = new Material(unlitShader);
        Color baseCol = new Color(0.04f, 0.08f, 0.14f, 0.95f);
        baseMat.SetColor("_BaseColor", baseCol);
        baseMat.EnableKeyword("_EMISSION");
        baseMat.SetColor("_EmissionColor", new Color(0f, 0.4f, 0.6f) * 1.2f);
        mr.material = baseMat;

        // 2. Static Background Track Ring (faint guide ring)
        GameObject trackObj = new GameObject("TrackRing");
        trackObj.transform.SetParent(visualsRoot.transform, false);
        trackObj.transform.localPosition = new Vector3(0, 0.1f, 0);
        trackRing = trackObj.AddComponent<LineRenderer>();
        trackRing.useWorldSpace = false;
        trackRing.loop = true;
        trackRing.positionCount = 48;
        trackRing.widthMultiplier = 0.14f;
        Material trackMat = new Material(unlitShader);
        trackMat.SetColor("_BaseColor", new Color(0f, 0.35f, 0.45f, 0.4f));
        trackMat.EnableKeyword("_EMISSION");
        trackMat.SetColor("_EmissionColor", new Color(0f, 0.25f, 0.35f) * 0.8f);
        trackRing.sharedMaterial = trackMat;
        for (int i = 0; i < 48; i++)
        {
            float ang = i * Mathf.PI * 2f / 48f;
            trackRing.SetPosition(i, new Vector3(Mathf.Sin(ang) * 2.5f, 0, Mathf.Cos(ang) * 2.5f));
        }

        // 3. Dynamic Progress Ring (fills up / regresses around the base)
        GameObject progressObj = new GameObject("ProgressRing");
        progressObj.transform.SetParent(visualsRoot.transform, false);
        progressObj.transform.localPosition = new Vector3(0, 0.14f, 0);
        progressRing = progressObj.AddComponent<LineRenderer>();
        progressRing.useWorldSpace = false;
        progressRing.loop = false;
        progressRing.widthMultiplier = 0.24f;
        progressMaterial = new Material(unlitShader);
        progressMaterial.SetColor("_BaseColor", new Color(0f, 1f, 0.95f, 1f));
        progressMaterial.EnableKeyword("_EMISSION");
        progressMaterial.SetColor("_EmissionColor", new Color(0f, 1.8f, 1.6f) * 2.5f);
        progressRing.sharedMaterial = progressMaterial;
        progressRing.positionCount = 0;

        // 4. Low-profile Spinning Stargate Rings (close to base, no tall pillar)
        GameObject ringsRoot = new GameObject("SpinningRings");
        ringsRoot.transform.SetParent(visualsRoot.transform, false);
        ringsRoot.transform.localPosition = new Vector3(0, 0.6f, 0);
        ringsTransform = ringsRoot.transform;

        Material ringMat = new Material(unlitShader);
        ringMat.SetColor("_BaseColor", new Color(0f, 1f, 0.9f));
        ringMat.EnableKeyword("_EMISSION");
        ringMat.SetColor("_EmissionColor", new Color(0f, 1.2f, 1.1f) * 2f);

        GameObject r1 = new GameObject("Ring1");
        r1.transform.SetParent(ringsRoot.transform, false);
        LineRenderer lr1 = r1.AddComponent<LineRenderer>();
        lr1.useWorldSpace = false;
        lr1.loop = true;
        lr1.positionCount = 32;
        lr1.widthMultiplier = 0.10f;
        lr1.sharedMaterial = ringMat;
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI * 2f / 32f;
            lr1.SetPosition(i, new Vector3(Mathf.Cos(a) * 2.4f, Mathf.Sin(a) * 0.25f, Mathf.Sin(a) * 2.4f));
        }

        GameObject r2 = new GameObject("Ring2");
        r2.transform.SetParent(ringsRoot.transform, false);
        r2.transform.localRotation = Quaternion.Euler(30f, 60f, 0);
        LineRenderer lr2 = r2.AddComponent<LineRenderer>();
        lr2.useWorldSpace = false;
        lr2.loop = true;
        lr2.positionCount = 32;
        lr2.widthMultiplier = 0.10f;
        lr2.sharedMaterial = ringMat;
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI * 2f / 32f;
            lr2.SetPosition(i, new Vector3(Mathf.Cos(a) * 2.3f, Mathf.Sin(a) * 0.25f, Mathf.Sin(a) * 2.3f));
        }

        // Wide trigger collider
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 3.5f;
    }

    void Update()
    {
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
        }

        if (visualsRoot != null)
        {
            BossWorldMotion.SetWorldScale(visualsRoot.transform, Vector3.one);
        }

        // Smooth rotation of decorative rings
        float rotSpeed = isPlayerInside ? (60f + fillProgress * 240f) : (isLocked ? 120f : 40f);
        if (ringsTransform != null)
        {
            ringsTransform.Rotate(0, rotSpeed * Time.deltaTime, rotSpeed * 0.3f * Time.deltaTime);
        }

        // Check if player is inside the portal area
        CheckPlayerInside();

        // Update filling vs regressing
        UpdateFillProgress();

        // Update the circular progress ring line
        UpdateProgressRingMesh();
    }

    void CheckPlayerInside()
    {
        if (isLocked || cooldown > 0f)
        {
            isPlayerInside = false;
            return;
        }

        Transform player = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (player == null)
        {
            isPlayerInside = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        isPlayerInside = (dist <= 3.2f);
    }

    void UpdateFillProgress()
    {
        if (isPlayerInside)
        {
            // Fills up over FILL_DURATION (3 seconds)
            fillProgress = Mathf.MoveTowards(fillProgress, 1f, Time.deltaTime / FILL_DURATION);

            // Color shifts from bright cyan to warm electric gold as it nears completion
            if (progressMaterial != null)
            {
                Color activeColor = Color.Lerp(new Color(0f, 1f, 0.95f), new Color(1f, 0.9f, 0.2f), fillProgress);
                progressMaterial.SetColor("_BaseColor", activeColor);
                progressMaterial.SetColor("_EmissionColor", activeColor * (2f + fillProgress * 3f));
            }

            // Once fully filled, trigger the jump!
            if (fillProgress >= 1f)
            {
                Transform player = GameManager.Instance != null ? GameManager.Instance.player : null;
                if (player != null)
                {
                    ExecuteTeleport(player);
                }
            }
        }
        else
        {
            // Regresses smoothly down when player exits
            if (fillProgress > 0f)
            {
                fillProgress = Mathf.MoveTowards(fillProgress, 0f, Time.deltaTime / REGRESS_DURATION);

                if (progressMaterial != null)
                {
                    Color activeColor = Color.Lerp(new Color(0f, 1f, 0.95f), new Color(1f, 0.9f, 0.2f), fillProgress);
                    progressMaterial.SetColor("_BaseColor", activeColor);
                    progressMaterial.SetColor("_EmissionColor", activeColor * 2f);
                }
            }
        }
    }

    void UpdateProgressRingMesh()
    {
        if (progressRing == null) return;

        if (fillProgress <= 0.005f)
        {
            progressRing.positionCount = 0;
            return;
        }

        int maxSegments = 48;
        int activeSegments = Mathf.Clamp(Mathf.CeilToInt(fillProgress * maxSegments), 2, maxSegments);
        progressRing.positionCount = activeSegments + 1;

        float radius = 2.5f;
        for (int i = 0; i <= activeSegments; i++)
        {
            float t = (float)i / maxSegments;
            if (t > fillProgress) t = fillProgress;
            float angle = t * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * radius, 0.14f, Mathf.Cos(angle) * radius);
            progressRing.SetPosition(i, pos);
        }
    }

    void ExecuteTeleport(Transform player)
    {
        if (targetPlanet == null)
        {
            AutoResolveTargetPlanet();
            if (targetPlanet == null) return;
        }

        GravityBody body = player.GetComponent<GravityBody>();
        if (body == null) return;

        fillProgress = 0f;
        isPlayerInside = false;
        cooldown = 4.0f;
        if (progressRing != null) progressRing.positionCount = 0;

        // Position player on destination planet surface cleanly
        float targetRadius = Mathf.Abs(targetPlanet.transform.lossyScale.x) * 0.5f;
        Vector3 targetUp = targetPlanet.transform.up;
        player.position = targetPlanet.transform.position + targetUp * (targetRadius + 1.2f);
        body.planet = targetPlanet;
        body.SnapToSurface();

        GameAudio.Play(AudioCue.Teleport);
        if (CameraShake.Instance != null) CameraShake.Instance.TriggerShake(0.3f, 0.5f);

        // Grant 3.0 seconds of invincibility grace period so player takes NO damage when arriving
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isInvincible = true;
            GameManager.Instance.StartCoroutine(ArrivalInvincibility(3.0f));
        }

        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null) spawner.currentPlanet = targetPlanet;

        // Cleanly despawn all lingering enemies from previous planets
        Enemy.DespawnEnemiesOnOtherPlanets(targetPlanet);

        Teleporter[] allTps = FindObjectsByType<Teleporter>(FindObjectsInactive.Exclude);
        foreach (var tp in allTps)
        {
            tp.cooldown = 4.0f;
            tp.fillProgress = 0f;
            tp.isPlayerInside = false;
            if (tp.progressRing != null) tp.progressRing.positionCount = 0;
        }

        PlanetaryBiome.OnPlayerArrived(targetPlanet);

        Debug.Log("Teleportado com sucesso para " + targetPlanet.name);
    }

    IEnumerator ArrivalInvincibility(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isInvincible = false;
        }
    }

    public void Lock()
    {
        isLocked = true;
        fillProgress = 0f;
        isPlayerInside = false;
        if (progressRing != null) progressRing.positionCount = 0;

        if (lockBarrier == null)
        {
            lockBarrier = new GameObject("LockBarrier");
            lockBarrier.transform.SetParent(transform, false);
            BossWorldMotion.SetWorldScale(lockBarrier.transform, Vector3.one);
            LineRenderer ring = lockBarrier.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 32;
            ring.widthMultiplier = 0.1f;
            barrierMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            barrierMaterial.SetColor("_BaseColor", new Color(1f, 0.2f, 0.3f));
            ring.sharedMaterial = barrierMaterial;
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI / 16f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), 0.3f, Mathf.Sin(angle)) * 2.8f);
            }
        }
        else
        {
            lockBarrier.SetActive(true);
        }
    }

    public void Unlock()
    {
        isLocked = false;
        if (lockBarrier != null)
        {
            Destroy(lockBarrier);
            lockBarrier = null;
            if (barrierMaterial != null) Destroy(barrierMaterial);
            barrierMaterial = null;
        }
    }

    void OnDestroy()
    {
        if (barrierMaterial != null) Destroy(barrierMaterial);
        if (progressMaterial != null) Destroy(progressMaterial);
    }

    public static void LockAllTeleporters()
    {
        Teleporter[] allTps = FindObjectsByType<Teleporter>(FindObjectsInactive.Exclude);
        foreach (var tp in allTps) tp.Lock();
    }

    public static void UnlockAllTeleporters()
    {
        Teleporter[] allTps = FindObjectsByType<Teleporter>(FindObjectsInactive.Exclude);
        foreach (var tp in allTps) tp.Unlock();
    }
}
