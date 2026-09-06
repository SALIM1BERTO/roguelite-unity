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

        int droneCount = level >= 3 ? 2 : 1;
        for (int i = 0; i < droneCount; i++)
        {
            GameObject drone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drone.name = "SentinelDrone_" + i;
            drone.transform.SetParent(transform, false);
            drone.transform.localScale = Vector3.one * 0.45f;
            Destroy(drone.GetComponent<Collider>());

            // Outer ring on drone
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "DroneRing";
            ring.transform.SetParent(drone.transform, false);
            ring.transform.localScale = new Vector3(1.4f, 0.1f, 1.4f);
            Destroy(ring.GetComponent<Collider>());

            MeshRenderer mr = drone.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color c = new Color(0f, 0.8f, 1f); // Neon Cyan
            mat.SetColor("_BaseColor", c);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", c * 3f);
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

    IEnumerator LaserBurst(Vector3 fromPos, Transform target)
    {
        if (target == null) yield break;

        Vector3 toTarget = (target.position - fromPos);
        float dist = toTarget.magnitude;
        Vector3 dir = toTarget.normalized;

        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "DroneLaser";
        beam.transform.position = fromPos + dir * (dist * 0.5f);
        beam.transform.up = dir;
        beam.transform.localScale = new Vector3(0.12f, dist * 0.5f, 0.12f);
        Destroy(beam.GetComponent<Collider>());

        MeshRenderer mr = beam.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color c = new Color(0f, 1f, 0.8f);
        mat.SetColor("_BaseColor", c);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", c * 4f);
        mr.material = mat;

        GameAudio.Play(AudioCue.Shot);

        // Apply damage
        int dmg = laserDamage + (level * 6);
        Enemy enemy = target.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(dmg, false);
        }
        else
        {
            BossLeviathan boss = target.GetComponentInParent<BossLeviathan>();
            if (boss != null) boss.TakeDamage(dmg);
        }

        float duration = 0.08f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mat != null) Destroy(mat);
        if (beam != null) Destroy(beam);
    }
}
