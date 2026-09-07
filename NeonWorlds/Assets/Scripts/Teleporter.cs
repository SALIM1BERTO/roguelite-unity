using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public PlanetGravity targetPlanet;
    public float cooldown = 0f;
    public bool isLocked = false;

    public const float COUNTDOWN_DURATION = 3.0f;
    private float currentCountdown = COUNTDOWN_DURATION;
    private bool isPlayerInside = false;
    private int lastBeepSecond = -1;

    private GameObject visualsRoot;
    private GameObject lockBarrier;
    private Material barrierMaterial;
    private TextMesh labelMesh;
    private Transform ringsTransform;
    private Material beaconMaterial;

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
        UpdateLabelText();
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

    public string GetDestinationName()
    {
        if (targetPlanet != null)
        {
            PlanetaryBiome pb = targetPlanet.GetComponent<PlanetaryBiome>();
            if (pb != null && !string.IsNullOrEmpty(pb.planetName)) return pb.planetName;
            return targetPlanet.gameObject.name;
        }
        return "HIPERESPAÇO";
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

        // 1. Ground Platform Base (Glowing Disc)
        GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisc.name = "PlatformBase";
        baseDisc.transform.SetParent(visualsRoot.transform, false);
        baseDisc.transform.localPosition = new Vector3(0, 0.05f, 0);
        baseDisc.transform.localScale = new Vector3(5.5f, 0.1f, 5.5f);
        Destroy(baseDisc.GetComponent<Collider>());

        MeshRenderer mr = baseDisc.GetComponent<MeshRenderer>();
        Material baseMat = new Material(unlitShader);
        Color baseCol = new Color(0.05f, 0.25f, 0.35f, 0.9f);
        baseMat.SetColor("_BaseColor", baseCol);
        baseMat.EnableKeyword("_EMISSION");
        baseMat.SetColor("_EmissionColor", new Color(0f, 0.8f, 0.9f) * 1.5f);
        mr.material = baseMat;

        // Ground Glowing Ring
        GameObject groundRingObj = new GameObject("GroundRing");
        groundRingObj.transform.SetParent(visualsRoot.transform, false);
        groundRingObj.transform.localPosition = new Vector3(0, 0.12f, 0);
        LineRenderer gRing = groundRingObj.AddComponent<LineRenderer>();
        gRing.useWorldSpace = false;
        gRing.loop = true;
        gRing.positionCount = 36;
        gRing.widthMultiplier = 0.16f;
        Material ringMat = new Material(unlitShader);
        ringMat.SetColor("_BaseColor", new Color(0f, 1f, 0.9f));
        gRing.sharedMaterial = ringMat;
        for (int i = 0; i < 36; i++)
        {
            float ang = i * Mathf.PI * 2f / 36f;
            gRing.SetPosition(i, new Vector3(Mathf.Cos(ang) * 2.7f, 0, Mathf.Sin(ang) * 2.7f));
        }

        // 2. Tall Sky Beacon Light Pillar (Visible across the entire planet!)
        GameObject beaconObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beaconObj.name = "SkyBeacon";
        beaconObj.transform.SetParent(visualsRoot.transform, false);
        beaconObj.transform.localPosition = new Vector3(0, 20f, 0);
        beaconObj.transform.localScale = new Vector3(2.0f, 20f, 2.0f);
        Destroy(beaconObj.GetComponent<Collider>());

        MeshRenderer bmr = beaconObj.GetComponent<MeshRenderer>();
        beaconMaterial = new Material(unlitShader);
        Color beaconCol = new Color(0f, 1f, 0.95f, 0.45f);
        beaconMaterial.SetColor("_BaseColor", beaconCol);
        beaconMaterial.EnableKeyword("_EMISSION");
        beaconMaterial.SetColor("_EmissionColor", new Color(0f, 1f, 0.95f) * 4f);
        bmr.material = beaconMaterial;

        // 3. Floating Rotating Stargate Rings
        GameObject ringsRoot = new GameObject("SpinningRings");
        ringsRoot.transform.SetParent(visualsRoot.transform, false);
        ringsRoot.transform.localPosition = new Vector3(0, 1.8f, 0);
        ringsTransform = ringsRoot.transform;

        // Ring 1 (Horizontal tilt)
        GameObject r1 = new GameObject("Ring1");
        r1.transform.SetParent(ringsRoot.transform, false);
        LineRenderer lr1 = r1.AddComponent<LineRenderer>();
        lr1.useWorldSpace = false;
        lr1.loop = true;
        lr1.positionCount = 32;
        lr1.widthMultiplier = 0.12f;
        lr1.sharedMaterial = ringMat;
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI * 2f / 32f;
            lr1.SetPosition(i, new Vector3(Mathf.Cos(a) * 2.5f, Mathf.Sin(a) * 0.4f, Mathf.Sin(a) * 2.5f));
        }

        // Ring 2 (Vertical tilt)
        GameObject r2 = new GameObject("Ring2");
        r2.transform.SetParent(ringsRoot.transform, false);
        r2.transform.localRotation = Quaternion.Euler(60f, 45f, 0);
        LineRenderer lr2 = r2.AddComponent<LineRenderer>();
        lr2.useWorldSpace = false;
        lr2.loop = true;
        lr2.positionCount = 32;
        lr2.widthMultiplier = 0.12f;
        lr2.sharedMaterial = ringMat;
        for (int i = 0; i < 32; i++)
        {
            float a = i * Mathf.PI * 2f / 32f;
            lr2.SetPosition(i, new Vector3(Mathf.Cos(a) * 2.4f, Mathf.Sin(a) * 2.4f, 0));
        }

        // 4. 3D Floating Holographic Label
        GameObject labelObj = new GameObject("HoloLabel");
        labelObj.transform.SetParent(visualsRoot.transform, false);
        labelObj.transform.localPosition = new Vector3(0, 4.5f, 0);
        labelMesh = labelObj.AddComponent<TextMesh>();
        labelMesh.fontSize = 28;
        labelMesh.characterSize = 0.14f;
        labelMesh.alignment = TextAlignment.Center;
        labelMesh.anchor = TextAnchor.MiddleCenter;
        labelMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelMesh.color = Color.cyan;

        // Ensure collider is a wide trigger
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 4.0f;
    }

    void Update()
    {
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            if (cooldown <= 0f) ResetCountdown();
        }

        if (visualsRoot != null)
        {
            BossWorldMotion.SetWorldScale(visualsRoot.transform, Vector3.one);
        }

        // Rotate stargate rings
        float rotSpeed = isPlayerInside ? 280f : (isLocked ? 180f : 60f);
        if (ringsTransform != null)
        {
            ringsTransform.Rotate(0, rotSpeed * Time.deltaTime, rotSpeed * 0.4f * Time.deltaTime);
        }

        // Pulse sky beacon
        if (beaconMaterial != null)
        {
            float pulse = Mathf.PingPong(Time.time * (isPlayerInside ? 8f : 2f), 1f);
            Color bCol = isLocked ? Color.red : (isPlayerInside ? Color.yellow : new Color(0f, 1f, 0.95f));
            beaconMaterial.SetColor("_EmissionColor", bCol * (2.5f + pulse * 4f));
        }

        // Face 3D label to camera
        if (labelMesh != null && Camera.main != null)
        {
            labelMesh.transform.rotation = Quaternion.LookRotation(labelMesh.transform.position - Camera.main.transform.position);
        }

        // Locked Leviathan barrier visual
        if (isLocked && lockBarrier != null)
        {
            BossWorldMotion.SetWorldScale(lockBarrier.transform, Vector3.one);
            float pulse = Mathf.PingPong(Time.time * 4f, 1f);
            MeshRenderer bmr = lockBarrier.GetComponent<MeshRenderer>();
            if (bmr != null && bmr.material != null)
            {
                bmr.material.SetColor("_EmissionColor", Color.red * (3f + pulse * 4f));
            }
        }

        // Check distance to player for 3-second countdown
        CheckPlayerProximity();
    }

    void CheckPlayerProximity()
    {
        if (isLocked)
        {
            if (labelMesh != null)
                labelMesh.text = "<color=#ff2244><b>⛔ PORTAL BLOQUEADO ⛔</b></color>\n<size=18>DERROTE O LEVIATÃ PRIMEIRO</size>";
            return;
        }

        if (cooldown > 0f)
        {
            if (labelMesh != null)
                labelMesh.text = $"<color=#888888>RECARREGANDO... ({cooldown:0.0}s)</color>";
            return;
        }

        Transform player = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= 4.0f)
        {
            if (!isPlayerInside)
            {
                isPlayerInside = true;
                currentCountdown = COUNTDOWN_DURATION;
                lastBeepSecond = -1;
                GameAudio.Play(AudioCue.Upgrade);
            }

            currentCountdown -= Time.deltaTime;
            if (currentCountdown < 0f) currentCountdown = 0f;

            int secRemaining = Mathf.CeilToInt(currentCountdown);
            if (secRemaining != lastBeepSecond && secRemaining > 0)
            {
                lastBeepSecond = secRemaining;
                GameAudio.Play(AudioCue.Shot);
                SpawnWarningText($"SALTO EM {secRemaining}s...");
            }

            if (CameraShake.Instance != null && currentCountdown <= 1.0f)
            {
                CameraShake.Instance.TriggerShake(0.04f, 0.08f);
            }

            if (labelMesh != null)
            {
                string colorHex = currentCountdown <= 1.0f ? "#ff3300" : (currentCountdown <= 2.0f ? "#ffcc00" : "#00ffff");
                labelMesh.text = $"<color={colorHex}><b>⚡ INICIANDO SALTO HIPERESPACIAL ⚡</b></color>\nDESTINO: <b>{GetDestinationName()}</b>\n<size=38><color={colorHex}><b>{currentCountdown:0.0}s</b></color></size>";
            }

            if (currentCountdown <= 0f)
            {
                ExecuteTeleport(player);
            }
        }
        else
        {
            if (isPlayerInside)
            {
                ResetCountdown();
            }
            UpdateLabelText();
        }
    }

    void UpdateLabelText()
    {
        if (labelMesh == null) return;
        labelMesh.text = $"<color=#00ffcc><b>🌀 PORTAL DE HIPERESPAÇO 🌀</b></color>\nDESTINO: <color=#ffffff><b>{GetDestinationName()}</b></color>\n<size=18><color=#aaaaaa>Fique 3 segundos na base para saltar</color></size>";
    }

    public void ResetCountdown()
    {
        isPlayerInside = false;
        currentCountdown = COUNTDOWN_DURATION;
        lastBeepSecond = -1;
        UpdateLabelText();
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

        ResetCountdown();
        cooldown = 4.0f;

        // Position player on destination planet surface cleanly
        float targetRadius = Mathf.Abs(targetPlanet.transform.lossyScale.x) * 0.5f;
        Vector3 targetUp = targetPlanet.transform.up;
        player.position = targetPlanet.transform.position + targetUp * (targetRadius + 1.2f);
        body.planet = targetPlanet;
        body.SnapToSurface();

        GameAudio.Play(AudioCue.Teleport);
        if (CameraShake.Instance != null) CameraShake.Instance.TriggerShake(0.35f, 0.6f);

        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null) spawner.currentPlanet = targetPlanet;

        Teleporter[] allTps = FindObjectsByType<Teleporter>(FindObjectsInactive.Exclude);
        foreach (var tp in allTps)
        {
            tp.cooldown = 4.0f;
            tp.ResetCountdown();
        }

        PlanetaryBiome.OnPlayerArrived(targetPlanet);

        Debug.Log("Teleportado com sucesso para " + targetPlanet.name);
    }

    public void Lock()
    {
        isLocked = true;
        ResetCountdown();
        if (lockBarrier == null)
        {
            lockBarrier = new GameObject("LockBarrier");
            lockBarrier.transform.SetParent(transform, false);
            BossWorldMotion.SetWorldScale(lockBarrier.transform, Vector3.one);
            LineRenderer ring = lockBarrier.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 32;
            ring.widthMultiplier = 0.08f;
            barrierMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            barrierMaterial.SetColor("_BaseColor", new Color(1f, 0.2f, 0.3f));
            ring.sharedMaterial = barrierMaterial;
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI / 16f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), 1.5f, Mathf.Sin(angle)) * 2.8f);
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
        ResetCountdown();
        if (lockBarrier != null)
        {
            Destroy(lockBarrier);
            lockBarrier = null;
            if (barrierMaterial != null) Destroy(barrierMaterial);
            barrierMaterial = null;
        }

        GameObject fxPrefab = Resources.Load<GameObject>("TeleportFX");
        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }
    }

    void OnDestroy()
    {
        if (barrierMaterial != null) Destroy(barrierMaterial);
        if (beaconMaterial != null) Destroy(beaconMaterial);
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

    void SpawnWarningText(string msg)
    {
        Vector3 textPos = transform.position + transform.up * 2.5f;
        GameObject txtObj = null;
        if (GameManager.Instance != null && GameManager.Instance.floatingTextPrefab != null)
        {
            txtObj = Instantiate(GameManager.Instance.floatingTextPrefab, textPos, Quaternion.identity);
        }
        else
        {
            txtObj = new GameObject("FloatingText");
            txtObj.transform.position = textPos;
        }

        FloatingText ft = txtObj.GetComponent<FloatingText>();
        if (ft == null) ft = txtObj.AddComponent<FloatingText>();
        ft.Setup(msg);
    }
}
