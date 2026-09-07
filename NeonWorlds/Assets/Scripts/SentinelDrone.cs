using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentinelDrone : MonoBehaviour
{
    public static SentinelDrone Instance { get; private set; }

    public int level = 0; // 0 = locked, 1..5
    public float orbitRadius = 2.4f;
    public float orbitSpeed = 120f; // degrees per sec
    public float fireInterval = 0.8f;
    public int laserDamage = 18;
    public bool isTeslaChain = false;

    private List<GameObject> drones = new List<GameObject>();
    private float currentOrbitAngle = 0f;
    private float fireTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    public void SetLevel(int newLevel)
    {
        level = newLevel;
        RebuildDrones();
    }

    void RebuildDrones()
    {
        foreach (var d in drones)
        {
            if (d != null) Destroy(d);
        }
        drones.Clear();

        if (level <= 0) return;

        int droneCount = isTeslaChain ? 3 : (level >= 3 ? 2 : 1);
        for (int i = 0; i < droneCount; i++)
        {
            GameObject drone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drone.name = "SentinelDrone_" + i;
            drone.transform.SetParent(transform, false);
            drone.transform.localScale = Vector3.one * (isTeslaChain ? 0.52f : 0.45f);
            Destroy(drone.GetComponent<Collider>());

            // Outer ring on drone
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "DroneRing";
            ring.transform.SetParent(drone.transform, false);
            ring.transform.localScale = new Vector3(1.4f, 0.1f, 1.4f);
            Destroy(ring.GetComponent<Collider>());

            MeshRenderer mr = drone.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color c = isTeslaChain ? new Color(1f, 0.85f, 0.1f) : new Color(0f, 0.8f, 1f); // Golden Lightning or Neon Cyan
            mat.SetColor("_BaseColor", c);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", c * 3.5f);
            mr.material = mat;
            ring.GetComponent<MeshRenderer>().material = mat;

            drones.Add(drone);
        }
    }

    void Update()
    {
        if (level <= 0 || drones.Count == 0 || Time.timeScale == 0) return;

        currentOrbitAngle += orbitSpeed * Time.deltaTime;
        float angleStep = 360f / drones.Count;

        for (int i = 0; i < drones.Count; i++)
        {
            if (drones[i] == null) continue;
            float angle = (currentOrbitAngle + i * angleStep) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * orbitRadius, 0.3f, Mathf.Sin(angle) * orbitRadius);
            drones[i].transform.localPosition = offset;
            drones[i].transform.Rotate(0, 180f * Time.deltaTime, 0);
        }

        fireTimer -= Time.deltaTime;
        float actualInterval = Mathf.Max(0.35f, fireInterval - (level * 0.08f));
        if (fireTimer <= 0f)
        {
            fireTimer = actualInterval;
            FireAtNearest();
        }
    }

    void FireAtNearest()
    {
        // Find nearest enemy or boss within 18 units
        Transform target = null;
        float minDist = 18f;

        if (BossLeviathan.Instance != null && BossLeviathan.Instance.gameObject.activeInHierarchy)
        {
            float d = Vector3.Distance(transform.position, BossLeviathan.Instance.transform.position);
            if (d < minDist)
            {
                minDist = d;
                target = BossLeviathan.Instance.transform;
            }
        }

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in enemies)
        {
            if (e != null && !e.isDead && e.isActiveAndEnabled)
            {
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    target = e.transform;
                }
            }
        }

        if (target != null)
        {
            for (int i = 0; i < drones.Count; i++)
            {
                if (drones[i] != null)
                {
                    StartCoroutine(LaserBurst(drones[i].transform.position, target));
                }
            }
        }
    }

    Transform GetPlanet()
    {
        GravityBody gb = GetComponentInParent<GravityBody>();
        if (gb != null && gb.planet != null) return gb.planet.transform;
        if (transform.parent != null) return transform.parent;
        PlanetGravity pg = FindAnyObjectByType<PlanetGravity>();
        return pg != null ? pg.transform : null;
    }

    IEnumerator LaserBurst(Vector3 fromPos, Transform target)
    {
        if (target == null) yield break;

        Transform planet = GetPlanet();
        Vector3 targetPos = target.position;

        GameObject beamObj = new GameObject("CurvedDroneLaser");
        LineRenderer lr = beamObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.startWidth = 0.15f;
        lr.endWidth = 0.08f;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color laserColor = new Color(0f, 1f, 0.85f); // Neon Cyan
        mat.SetColor("_BaseColor", laserColor);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", laserColor * 4.5f);
        lr.material = mat;

        if (planet != null)
        {
            Vector3 center = planet.position;
            Vector3 dirFrom = (fromPos - center).normalized;
            Vector3 dirTo = (targetPos - center).normalized;

            float rFrom = Vector3.Distance(fromPos, center);
            float rTo = Vector3.Distance(targetPos, center);

            float angle = Vector3.Angle(dirFrom, dirTo);
            int segments = Mathf.Clamp(Mathf.CeilToInt(angle / 4f), 6, 28);
            lr.positionCount = segments + 1;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 slerpedDir = Vector3.Slerp(dirFrom, dirTo, t);
                float radius = Mathf.Lerp(rFrom, rTo, t);
                Vector3 point = center + slerpedDir * radius;
                lr.SetPosition(i, point);
            }
        }
        else
        {
            lr.positionCount = 2;
            lr.SetPosition(0, fromPos);
            lr.SetPosition(1, targetPos);
        }

        if (isTeslaChain)
        {
            GameAudio.Play(AudioCue.TeslaArc);
        }
        else
        {
            GameAudio.Play(AudioCue.Shot);
        }

        // Apply damage
        int dmg = laserDamage + (level * 6);
        Enemy enemy = target.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(dmg, false, isTeslaChain ? DamageTextStyle.Electric : DamageTextStyle.Normal);
        }
        else
        {
            BossLeviathan boss = target.GetComponentInParent<BossLeviathan>();
            if (boss != null) boss.TakeDamage(dmg, false, isTeslaChain ? DamageTextStyle.Electric : DamageTextStyle.Normal);
        }

        // Hit spark at target
        GameObject fxPrefab = Resources.Load<GameObject>("BulletImpactFX");
        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, targetPos, Quaternion.identity);
            if (planet != null) fx.transform.SetParent(planet, true);
            Destroy(fx, 0.5f);
        }

        if (isTeslaChain)
        {
            StartCoroutine(TeslaChainArcs(targetPos, enemy, dmg, planet));
        }

        float duration = 0.12f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            lr.startWidth = Mathf.Lerp(0.15f, 0f, progress);
            lr.endWidth = Mathf.Lerp(0.08f, 0f, progress);
            yield return null;
        }

        if (mat != null) Destroy(mat);
        if (beamObj != null) Destroy(beamObj);
    }

    IEnumerator TeslaChainArcs(Vector3 originPos, Enemy initialEnemy, int baseDamage, Transform planet)
    {
        Vector3 currentPos = originPos;
        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
        if (initialEnemy != null) hitEnemies.Add(initialEnemy);

        int jumps = 3;
        int chainDmg = Mathf.RoundToInt(baseDamage * 0.7f);

        for (int j = 0; j < jumps; j++)
        {
            Enemy nextTarget = null;
            float minDist = 8f;
            Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy e in allEnemies)
            {
                if (e != null && !e.isDead && !hitEnemies.Contains(e))
                {
                    float dist = Vector3.Distance(currentPos, e.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nextTarget = e;
                    }
                }
            }

            if (nextTarget == null) break;
            hitEnemies.Add(nextTarget);

            Vector3 nextPos = nextTarget.transform.position;

            GameObject lightningObj = new GameObject("TeslaArc");
            LineRenderer arcLr = lightningObj.AddComponent<LineRenderer>();
            arcLr.useWorldSpace = true;
            arcLr.startWidth = 0.14f;
            arcLr.endWidth = 0.05f;

            Material arcMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color arcCol = new Color(1f, 0.9f, 0.2f); // Radiant Gold-Yellow
            arcMat.SetColor("_BaseColor", arcCol);
            arcMat.EnableKeyword("_EMISSION");
            arcMat.SetColor("_EmissionColor", arcCol * 4.5f);
            arcLr.material = arcMat;

            int segments = 6;
            arcLr.positionCount = segments + 1;
            arcLr.SetPosition(0, currentPos);
            arcLr.SetPosition(segments, nextPos);

            Vector3 step = (nextPos - currentPos) / segments;
            Vector3 planetPos = planet != null ? planet.position : Vector3.zero;
            Vector3 upDir = (currentPos - planetPos).normalized;
            Vector3 perp = Vector3.Cross((nextPos - currentPos).normalized, upDir).normalized;

            for (int s = 1; s < segments; s++)
            {
                float jitter = Random.Range(-0.35f, 0.35f);
                Vector3 p = currentPos + step * s + perp * jitter;
                arcLr.SetPosition(s, p);
            }

            nextTarget.TakeDamage(chainDmg, true, DamageTextStyle.Electric);

            GameObject fxPrefab = Resources.Load<GameObject>("BulletImpactFX");
            if (fxPrefab != null)
            {
                GameObject fx = Instantiate(fxPrefab, nextPos, Quaternion.identity);
                if (planet != null) fx.transform.SetParent(planet, true);
                Destroy(fx, 0.4f);
            }

            currentPos = nextPos;
            Destroy(arcMat, 0.12f);
            Destroy(lightningObj, 0.12f);

            yield return new WaitForSeconds(0.04f);
        }
    }
}
