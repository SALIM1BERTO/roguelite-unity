using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BiomeType
{
    Sanctuary = 0,       // Planet_1: Neo-Verdia (Clean, training, stable)
    InfernoPyre = 1,     // Planet_2: Inferno Pyre (Magma geysers, thermal vents)
    VoidAbyss = 2,       // Planet_3: Void Abyss (Gravity rifts, cosmic vacuum)
    ToxicJungle = 3,     // Planet_4: Toxic Spore (Acid bulbs, +50% XP)
    TitanColossus = 4,   // Planet_5: Titan Colossus (Mega scale 200, flat plains)
    ElectroNexus = 5     // Planet_6: Electro Nexus (Ionic lightning storms)
}

public class PlanetaryBiome : MonoBehaviour
{
    public static PlanetaryBiome CurrentBiome { get; private set; }

    [Header("Biome Info")]
    public BiomeType biomeType;
    public string planetName = "Neo-Verdia";
    public string hazardDescription = "Santuário Estelar • Atmosfera Equilibrada";
    public Color themeColor = new Color(0f, 0.9f, 1f);
    public float xpMultiplier = 1.0f;

    private PlanetGravity planetGravity;
    private Coroutine hazardRoutine;
    private List<GameObject> activeHazards = new List<GameObject>();

    void Awake()
    {
        planetGravity = GetComponent<PlanetGravity>();
        ConfigureDefaultBiome();
    }

    public void ConfigureDefaultBiome()
    {
        string pName = gameObject.name;
        if (pName.Contains("1"))
        {
            biomeType = BiomeType.Sanctuary;
            planetName = "NEO-VERDIA";
            hazardDescription = "SANTUÁRIO ESTELAR • ATMOSFERA EQUILIBRADA";
            themeColor = new Color(0f, 0.9f, 1f); // Electric Cyan
            xpMultiplier = 1.0f;
        }
        else if (pName.Contains("2"))
        {
            biomeType = BiomeType.InfernoPyre;
            planetName = "INFERNO PYRE";
            hazardDescription = "PERIGO: GÊISERES DE MAGMA • CALOR EXTREMO";
            themeColor = new Color(1f, 0.25f, 0.05f); // Molten Orange
            xpMultiplier = 1.15f;
        }
        else if (pName.Contains("3"))
        {
            biomeType = BiomeType.VoidAbyss;
            planetName = "VOID ABYSS";
            hazardDescription = "PERIGO: FENDAS GRAVITACIONAIS DE VÁCUO";
            themeColor = new Color(0.75f, 0.15f, 1f); // Cosmic Purple
            xpMultiplier = 1.25f;
        }
        else if (pName.Contains("4"))
        {
            biomeType = BiomeType.ToxicJungle;
            planetName = "TOXIC SPORE";
            hazardDescription = "PERIGO: BOLSAS DE ESPOROS ÁCIDOS • XP +50%";
            themeColor = new Color(0.1f, 1f, 0.35f); // Toxic Emerald
            xpMultiplier = 1.5f;
        }
        else if (pName.Contains("5"))
        {
            biomeType = BiomeType.TitanColossus;
            planetName = "TITAN COLOSSUS";
            hazardDescription = "PLANETA GIGANTE (TAMANHO 200) • PLANÍCIES INFINITAS";
            themeColor = new Color(1f, 0.65f, 0.1f); // Solar Gold
            xpMultiplier = 1.35f;
        }
        else if (pName.Contains("6"))
        {
            biomeType = BiomeType.ElectroNexus;
            planetName = "ELECTRO NEXUS";
            hazardDescription = "PERIGO: TEMPESTADES DE RELÂMPAGOS IÔNICOS";
            themeColor = new Color(1f, 0.9f, 0.2f); // Radiant Yellow
            xpMultiplier = 1.4f;
        }
    }

    public static void OnPlayerArrived(PlanetGravity planet)
    {
        if (planet == null) return;
        PlanetaryBiome biome = planet.GetComponent<PlanetaryBiome>();
        if (biome == null)
        {
            biome = planet.gameObject.AddComponent<PlanetaryBiome>();
            biome.ConfigureDefaultBiome();
        }

        if (CurrentBiome != null && CurrentBiome != biome)
        {
            CurrentBiome.StopHazards();
        }

        CurrentBiome = biome;
        biome.StartHazards();

        RuntimeUIBuilder.ShowPlanetBanner(biome.planetName, biome.hazardDescription, biome.themeColor);
    }

    public void StartHazards()
    {
        StopHazards();
        timeOnPlanet = 0f;
        isOverloaded = false;
        hazardRoutine = StartCoroutine(HazardLoop());
    }

    public void StopHazards()
    {
        if (hazardRoutine != null)
        {
            StopCoroutine(hazardRoutine);
            hazardRoutine = null;
        }

        for (int i = activeHazards.Count - 1; i >= 0; i--)
        {
            if (activeHazards[i] != null) Destroy(activeHazards[i]);
        }
        activeHazards.Clear();
    }

    private float timeOnPlanet = 0f;
    private bool isOverloaded = false;

    void Update()
    {
        if (CurrentBiome != this) return;
        
        timeOnPlanet += Time.deltaTime;

        if (timeOnPlanet >= 210f && !isOverloaded) // 3.5 minutes
        {
            TriggerOverload();
        }
    }

    void TriggerOverload()
    {
        isOverloaded = true;
        
        // Spawn dynamic teleporter near player
        PlanetGravity[] allPlanets = FindObjectsByType<PlanetGravity>(FindObjectsInactive.Exclude);
        PlanetGravity nextPlanet = null;
        List<PlanetGravity> validPlanets = new List<PlanetGravity>();
        foreach (var p in allPlanets) {
            if (p != planetGravity) validPlanets.Add(p);
        }
        if (validPlanets.Count > 0)
        {
            nextPlanet = validPlanets[Random.Range(0, validPlanets.Count)];
        }

        Vector3 spawnPos = GetSurfacePointNearPlayer(8f, 12f);
        Vector3 upNormal = (spawnPos - transform.position).normalized;

        GameObject portalObj = new GameObject("EscapeTeleporter");
        portalObj.transform.position = spawnPos;
        portalObj.transform.up = upNormal;
        portalObj.transform.SetParent(transform, true);

        Teleporter tp = portalObj.AddComponent<Teleporter>();
        tp.targetPlanet = nextPlanet;

        RuntimeUIBuilder.ShowPlanetBanner("SOBRECARGA CRÍTICA", "O NÚCLEO ESTÁ ENTRANDO EM COLAPSO! USE O PORTAL DE FUGA!", new Color(1f, 0.2f, 0.1f));
        GameAudio.Play(AudioCue.Explosion);
        if (CameraShake.Instance != null) CameraShake.Instance.TriggerShake(0.5f, 1f);
    }

    IEnumerator HazardLoop()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.player != null)
            {
                // Speed up hazards if overloaded
                float rateMultiplier = isOverloaded ? 0.35f : 1f;

                switch (biomeType)
                {
                    case BiomeType.InfernoPyre:
                        yield return new WaitForSeconds(Random.Range(3.5f, 5.5f) * rateMultiplier);
                        SpawnMagmaGeyser();
                        break;

                    case BiomeType.VoidAbyss:
                        yield return new WaitForSeconds(Random.Range(5f, 7.5f) * rateMultiplier);
                        SpawnGravityFissure();
                        break;

                    case BiomeType.ToxicJungle:
                        yield return new WaitForSeconds(Random.Range(4f, 6f) * rateMultiplier);
                        SpawnToxicSpores();
                        break;

                    case BiomeType.ElectroNexus:
                        yield return new WaitForSeconds(Random.Range(3f, 4.5f) * rateMultiplier);
                        SpawnIonicLightningStrike();
                        break;

                    default:
                        yield return new WaitForSeconds(5f * rateMultiplier);
                        break;
                }
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }

    Vector3 GetSurfacePointNearPlayer(float minDist = 6f, float maxDist = 16f)
    {
        if (GameManager.Instance == null || GameManager.Instance.player == null)
            return transform.position + Vector3.up * (transform.localScale.x * 0.5f);

        Transform player = GameManager.Instance.player;
        Vector3 surfaceNormal = (player.position - transform.position).normalized;
        Vector3 randomTangent = Vector3.ProjectOnPlane(Random.onUnitSphere, surfaceNormal).normalized;
        if (randomTangent.sqrMagnitude < 0.01f) randomTangent = Vector3.Cross(surfaceNormal, Vector3.up).normalized;

        float distance = Random.Range(minDist, maxDist);
        Vector3 targetPos = player.position + randomTangent * distance;
        Vector3 finalNormal = (targetPos - transform.position).normalized;
        float radius = transform.localScale.x * 0.5f;

        return transform.position + finalNormal * radius;
    }

    void SpawnMagmaGeyser()
    {
        Vector3 pos = GetSurfacePointNearPlayer(5f, 15f);
        Vector3 upNormal = (pos - transform.position).normalized;

        GameObject geyserObj = new GameObject("MagmaGeyser");
        geyserObj.transform.position = pos;
        geyserObj.transform.up = upNormal;
        geyserObj.transform.SetParent(transform, true);
        activeHazards.Add(geyserObj);

        MagmaGeyserLogic logic = geyserObj.AddComponent<MagmaGeyserLogic>();
        logic.Setup(transform, upNormal, transform.lossyScale.x);
    }

    void SpawnGravityFissure()
    {
        Vector3 pos = GetSurfacePointNearPlayer(7f, 18f);
        Vector3 upNormal = (pos - transform.position).normalized;

        GameObject fissureObj = new GameObject("GravityFissure");
        fissureObj.transform.position = pos;
        fissureObj.transform.up = upNormal;
        fissureObj.transform.SetParent(transform, true);
        activeHazards.Add(fissureObj);

        GravityFissureLogic logic = fissureObj.AddComponent<GravityFissureLogic>();
        logic.Setup(transform, upNormal, transform.lossyScale.x);
    }

    void SpawnToxicSpores()
    {
        Vector3 pos = GetSurfacePointNearPlayer(6f, 16f);
        Vector3 upNormal = (pos - transform.position).normalized;

        GameObject sporeObj = new GameObject("ToxicSporePod");
        sporeObj.transform.position = pos;
        sporeObj.transform.up = upNormal;
        sporeObj.transform.SetParent(transform, true);
        activeHazards.Add(sporeObj);

        ToxicSporeLogic logic = sporeObj.AddComponent<ToxicSporeLogic>();
        logic.Setup(transform, upNormal, transform.lossyScale.x);
    }

    void SpawnIonicLightningStrike()
    {
        Vector3 pos = GetSurfacePointNearPlayer(8f, 18f);
        Vector3 upNormal = (pos - transform.position).normalized;

        GameObject strikeObj = new GameObject("IonicLightningStrike");
        strikeObj.transform.position = pos;
        strikeObj.transform.up = upNormal;
        strikeObj.transform.SetParent(transform, true);
        activeHazards.Add(strikeObj);

        IonicLightningLogic logic = strikeObj.AddComponent<IonicLightningLogic>();
        logic.Setup(transform, upNormal, transform.lossyScale.x);
    }
}

// ----------------------------------------------------
// 1. Magma Geyser Hazard Logic (Planet 2)
// ----------------------------------------------------
public class MagmaGeyserLogic : MonoBehaviour
{
    private Transform planet;
    private Vector3 normal;
    private float pScale = 1f;

    public void Setup(Transform p, Vector3 norm, float scale)
    {
        planet = p;
        normal = norm;
        pScale = Mathf.Max(0.01f, scale);
        StartCoroutine(GeyserRoutine());
    }

    private static Material s_WarnMat;
    private static Material s_EruptMat;

    IEnumerator GeyserRoutine()
    {
        // Telegraph Warning: Expanding pulsing red cylinder on ground
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "WarningRing";
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = new Vector3(2.5f / pScale, 0.02f / pScale, 2.5f / pScale);
        Destroy(ring.GetComponent<Collider>());

        MeshRenderer mr = ring.GetComponent<MeshRenderer>();
        if (s_WarnMat == null)
        {
            s_WarnMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color warnCol = new Color(1f, 0.2f, 0f, 0.6f);
            s_WarnMat.SetColor("_BaseColor", warnCol);
            s_WarnMat.EnableKeyword("_EMISSION");
            s_WarnMat.SetColor("_EmissionColor", warnCol * 2f);
        }
        mr.sharedMaterial = s_WarnMat;

        float telegraph = 1.8f;
        float t = 0f;
        while (t < telegraph)
        {
            t += Time.deltaTime;
            float pulse = 1f + Mathf.PingPong(t * 8f, 0.35f);
            ring.transform.localScale = new Vector3((2.5f * pulse) / pScale, 0.02f / pScale, (2.5f * pulse) / pScale);
            yield return null;
        }

        // Eruption! Tall glowing magma pillar
        GameAudio.Play(AudioCue.Explosion);
        if (CameraShake.Instance != null) CameraShake.Instance.TriggerShake(0.15f, 0.35f);

        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "MagmaPillar";
        pillar.transform.SetParent(transform, false);
        pillar.transform.localPosition = new Vector3(0, 2f / pScale, 0);
        pillar.transform.localScale = new Vector3(2.2f / pScale, 4f / pScale, 2.2f / pScale);
        Destroy(pillar.GetComponent<Collider>());

        MeshRenderer pmr = pillar.GetComponent<MeshRenderer>();
        if (s_EruptMat == null)
        {
            s_EruptMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color eruptCol = new Color(1f, 0.45f, 0.05f);
            s_EruptMat.SetColor("_BaseColor", eruptCol);
            s_EruptMat.EnableKeyword("_EMISSION");
            s_EruptMat.SetColor("_EmissionColor", eruptCol * 6f);
        }
        pmr.sharedMaterial = s_EruptMat;

        // Damage Tick without allocating arrays
        float radiusSqr = 3.0f * 3.0f;
        for (int i = Enemy.activeEnemies.Count - 1; i >= 0; i--)
        {
            if (i < Enemy.activeEnemies.Count)
            {
                Enemy e = Enemy.activeEnemies[i];
                if (e != null && !e.isDead && (e.transform.position - transform.position).sqrMagnitude <= radiusSqr)
                {
                    e.TakeDamage(45, true, DamageTextStyle.Area);
                }
            }
        }
        if (BossLeviathan.Instance != null && (BossLeviathan.Instance.transform.position - transform.position).sqrMagnitude <= radiusSqr)
        {
            BossLeviathan.Instance.TakeDamage(45, true, DamageTextStyle.Area);
        }

        yield return new WaitForSeconds(0.6f);

        // Fade out
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float scaleY = Mathf.Lerp(4f, 0f, t / 0.4f);
            pillar.transform.localScale = new Vector3(2.2f / pScale, scaleY / pScale, 2.2f / pScale);
            yield return null;
        }

        Destroy(gameObject);
    }
}

// ----------------------------------------------------
// 2. Void Fissure Logic (Planet 3)
// ----------------------------------------------------
public class GravityFissureLogic : MonoBehaviour
{
    private Transform planet;
    private float duration = 5f;
    private float pullRadius = 8f;
    private float pullForce = 6f;
    private static Material s_VoidMat;

    public void Setup(Transform p, Vector3 norm, float scale)
    {
        planet = p;
        float pScale = Mathf.Max(0.01f, scale);

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.transform.SetParent(transform, false);
        core.transform.localPosition = new Vector3(0, 0.3f / pScale, 0);
        core.transform.localScale = Vector3.one * (1.1f / pScale);
        Destroy(core.GetComponent<Collider>());

        MeshRenderer mr = core.GetComponent<MeshRenderer>();
        if (s_VoidMat == null)
        {
            s_VoidMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color voidCol = new Color(0.65f, 0.05f, 1f);
            s_VoidMat.SetColor("_BaseColor", Color.black);
            s_VoidMat.EnableKeyword("_EMISSION");
            s_VoidMat.SetColor("_EmissionColor", voidCol * 3.5f);
        }
        mr.sharedMaterial = s_VoidMat;

        GameAudio.Play(AudioCue.VoidVortexDrone);
        Destroy(gameObject, duration);
    }

    void Update()
    {
        // Pull enemies caught in fissure without GC allocations
        float pullRadiusSqr = pullRadius * pullRadius;
        Vector3 center = transform.position;

        for (int i = 0; i < Enemy.activeEnemies.Count; i++)
        {
            Enemy e = Enemy.activeEnemies[i];
            if (e != null && !e.isDead)
            {
                Vector3 toCenter = center - e.transform.position;
                if (toCenter.sqrMagnitude <= pullRadiusSqr && toCenter.sqrMagnitude > 0.001f)
                {
                    e.transform.position += toCenter.normalized * (pullForce * Time.deltaTime);
                }
            }
        }
    }
}

// ----------------------------------------------------
// 3. Toxic Spore Pod Logic (Planet 4)
// ----------------------------------------------------
public class ToxicSporeLogic : MonoBehaviour
{
    private bool detonated = false;
    private static Material s_SporeMat;

    public void Setup(Transform p, Vector3 norm, float scale)
    {
        float pScale = Mathf.Max(0.01f, scale);
        GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = "SporeBulb";
        bulb.transform.SetParent(transform, false);
        bulb.transform.localPosition = new Vector3(0, 0.4f / pScale, 0);
        bulb.transform.localScale = new Vector3(0.9f / pScale, 1.2f / pScale, 0.9f / pScale);

        SphereCollider sc = bulb.GetComponent<SphereCollider>();
        sc.isTrigger = true;

        MeshRenderer mr = bulb.GetComponent<MeshRenderer>();
        if (s_SporeMat == null)
        {
            s_SporeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color toxicCol = new Color(0.1f, 1f, 0.3f);
            s_SporeMat.SetColor("_BaseColor", toxicCol);
            s_SporeMat.EnableKeyword("_EMISSION");
            s_SporeMat.SetColor("_EmissionColor", toxicCol * 2.5f);
        }
        mr.sharedMaterial = s_SporeMat;

        Destroy(gameObject, 15f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (detonated) return;
        if (other.GetComponentInParent<Bullet>() != null ||
            other.GetComponentInParent<Enemy>() != null)
        {
            Detonate();
        }
    }

    void Detonate()
    {
        if (detonated) return;
        detonated = true;
        GameAudio.Play(AudioCue.Explosion);

        // Toxic Acid Cloud AoE without GC arrays
        float radiusSqr = 6f * 6f;
        Vector3 pos = transform.position;
        for (int i = Enemy.activeEnemies.Count - 1; i >= 0; i--)
        {
            if (i < Enemy.activeEnemies.Count)
            {
                Enemy e = Enemy.activeEnemies[i];
                if (e != null && !e.isDead && (e.transform.position - pos).sqrMagnitude <= radiusSqr)
                {
                    e.TakeDamage(55, false, DamageTextStyle.Area);
                }
            }
        }
        if (BossLeviathan.Instance != null && (BossLeviathan.Instance.transform.position - pos).sqrMagnitude <= radiusSqr)
        {
            BossLeviathan.Instance.TakeDamage(55, false, DamageTextStyle.Area);
        }

        Destroy(gameObject);
    }
}

// ----------------------------------------------------
// 4. Ionic Lightning Strike Logic (Planet 6)
// ----------------------------------------------------
public class IonicLightningLogic : MonoBehaviour
{
    private static Material s_BoltMat;

    public void Setup(Transform p, Vector3 norm, float scale)
    {
        StartCoroutine(StrikeRoutine(norm, Mathf.Max(0.01f, scale)));
    }

    IEnumerator StrikeRoutine(Vector3 upNorm, float pScale)
    {
        // Telegraph spark
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.transform.SetParent(transform, false);
        spark.transform.localPosition = Vector3.zero;
        spark.transform.localScale = Vector3.one * (0.5f / pScale);
        Destroy(spark.GetComponent<Collider>());

        yield return new WaitForSeconds(0.8f);

        // Thunder impact!
        GameAudio.Play(AudioCue.TeslaArc);

        GameObject boltObj = new GameObject("LightningBolt");
        boltObj.transform.position = transform.position;
        LineRenderer lr = boltObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = 0.25f;
        lr.endWidth = 0.08f;
        lr.positionCount = 6;

        Vector3 strikeOrigin = transform.position + upNorm * 18f;
        for (int i = 0; i < 6; i++)
        {
            float prog = (float)i / 5f;
            Vector3 pt = Vector3.Lerp(strikeOrigin, transform.position, prog);
            if (i > 0 && i < 5) pt += Random.insideUnitSphere * 0.6f;
            lr.SetPosition(i, pt);
        }

        if (s_BoltMat == null)
        {
            s_BoltMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color boltCol = new Color(1f, 0.95f, 0.2f);
            s_BoltMat.SetColor("_BaseColor", boltCol);
            s_BoltMat.EnableKeyword("_EMISSION");
            s_BoltMat.SetColor("_EmissionColor", boltCol * 5f);
        }
        lr.sharedMaterial = s_BoltMat;

        // Damage & Shock without GC arrays
        float radiusSqr = 3.5f * 3.5f;
        Vector3 pos = transform.position;
        for (int i = Enemy.activeEnemies.Count - 1; i >= 0; i--)
        {
            if (i < Enemy.activeEnemies.Count)
            {
                Enemy e = Enemy.activeEnemies[i];
                if (e != null && !e.isDead && (e.transform.position - pos).sqrMagnitude <= radiusSqr)
                {
                    e.TakeDamage(60, true, DamageTextStyle.Electric);
                }
            }
        }
        if (BossLeviathan.Instance != null && (BossLeviathan.Instance.transform.position - pos).sqrMagnitude <= radiusSqr)
        {
            BossLeviathan.Instance.TakeDamage(60, true, DamageTextStyle.Electric);
        }

        yield return new WaitForSeconds(0.15f);
        Destroy(boltObj);
        Destroy(gameObject);
    }
}
