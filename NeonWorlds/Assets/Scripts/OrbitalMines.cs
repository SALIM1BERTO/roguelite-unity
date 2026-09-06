using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitalMines : MonoBehaviour
{
    public static OrbitalMines Instance { get; private set; }

    public int level = 0; // 0 = not unlocked, 1..5
    public float dropInterval = 3.5f;
    public int mineDamage = 35;
    public float blastRadius = 2.6f;

    private float timer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (level <= 0 || Time.timeScale == 0) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = Mathf.Max(1.0f, dropInterval - (level * 0.4f));
            DropMine();
            if (level >= 3)
            {
                StartCoroutine(DelayedDrop(0.25f));
            }
        }
    }

    IEnumerator DelayedDrop(float delay)
    {
        yield return new WaitForSeconds(delay);
        DropMine();
    }

    void DropMine()
    {
        GravityBody gb = GetComponent<GravityBody>();
        Transform planet = (gb != null && gb.planet != null) ? gb.planet.transform : transform.parent;
        if (planet == null) return;

        float pScale = planet.lossyScale.x > 0.001f ? planet.lossyScale.x : 1f;

        GameObject mine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mine.name = "VoidMine";
        mine.transform.position = transform.position;
        mine.transform.up = transform.up;
        mine.transform.SetParent(planet, true);
        mine.transform.localScale = new Vector3(0.7f / pScale, 0.1f / pScale, 0.7f / pScale);

        // Outer Material
        MeshRenderer mr = mine.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color mineColor = new Color(1f, 0f, 0.6f); // Neon Magenta
        mat.SetColor("_BaseColor", mineColor);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", mineColor * 3f);
        mr.material = mat;

        // Inner glowing core
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "MineCore";
        core.transform.SetParent(mine.transform, false);
        core.transform.localPosition = new Vector3(0, 0.4f, 0);
        core.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
        Destroy(core.GetComponent<Collider>());
        MeshRenderer coreMr = core.GetComponent<MeshRenderer>();
        Material coreMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        coreMat.SetColor("_BaseColor", Color.white);
        coreMat.EnableKeyword("_EMISSION");
        coreMat.SetColor("_EmissionColor", Color.white * 4f);
        coreMr.material = coreMat;

        VoidMineLogic logic = mine.AddComponent<VoidMineLogic>();
        logic.Setup(mineDamage + (level * 10), blastRadius, planet);
    }
}

public class VoidMineLogic : MonoBehaviour
{
    private int damage = 35;
    private float blastRadius = 2.6f;
    private Transform planet;
    private float armTimer = 0.4f;
    private bool isArmed = false;
    private bool hasExploded = false;
    private float lifetime = 25f;

    public void Setup(int dmg, float radius, Transform p)
    {
        damage = dmg;
        blastRadius = radius;
        planet = p;
    }

    void Update()
    {
        if (hasExploded) return;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (!isArmed)
        {
            armTimer -= Time.deltaTime;
            if (armTimer <= 0f) isArmed = true;
            return;
        }

        // Check distance to any enemy or boss
        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius * 0.75f);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].GetComponentInParent<Enemy>() != null || colliders[i].GetComponentInParent<BossLeviathan>() != null)
            {
                Explode();
                return;
            }
        }
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        GameAudio.Play(AudioCue.Explosion);

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.TriggerShake(0.18f, 0.4f);
        }

        // Spawn particle shards if available
        GameObject fxPrefab = Resources.Load<GameObject>("EnemyDeathFX");
        if (fxPrefab != null)
        {
            GameObject pfx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            if (planet != null) pfx.transform.SetParent(planet, true);
            Destroy(pfx, 1f);
        }

        // Damage targets in radius
        Collider[] hits = Physics.OverlapSphere(transform.position, blastRadius);
        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
        for (int i = 0; i < hits.Length; i++)
        {
            Enemy e = hits[i].GetComponentInParent<Enemy>();
            if (e != null && !hitEnemies.Contains(e))
            {
                hitEnemies.Add(e);
                e.TakeDamage(damage, true);
            }

            BossLeviathan boss = hits[i].GetComponentInParent<BossLeviathan>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
            }
        }

        // Create sleek expanding flat shockwave ring on surface
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "MineShockwaveRing";
        ring.transform.position = transform.position + transform.up * 0.04f;
        ring.transform.up = transform.up;
        Destroy(ring.GetComponent<Collider>());

        if (planet != null) ring.transform.SetParent(planet, true);

        MeshRenderer mr = ring.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color ringColor = new Color(1f, 0.05f, 0.65f); // Neon Magenta
        mat.SetColor("_BaseColor", ringColor);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", ringColor * 3.5f);
        mr.material = mat;

        float pScale = planet != null ? planet.lossyScale.x : 1f;
        ring.AddComponent<MineShockwaveRing>().Setup(blastRadius, pScale, mat);

        Destroy(gameObject);
    }
}

public class MineShockwaveRing : MonoBehaviour
{
    private float maxWorldRadius = 2.6f;
    private float planetScale = 1f;
    private float duration = 0.22f;
    private float elapsed = 0f;
    private Material mat;

    public void Setup(float worldRadius, float pScale, Material m)
    {
        maxWorldRadius = Mathf.Min(worldRadius, 2.8f);
        planetScale = Mathf.Max(0.001f, pScale);
        mat = m;
        float initialLocal = 0.2f / planetScale;
        transform.localScale = new Vector3(initialLocal, 0.015f / planetScale, initialLocal);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = elapsed / duration;
        if (progress >= 1f)
        {
            if (mat != null) Destroy(mat);
            Destroy(gameObject);
            return;
        }

        // Fast ease-out expansion
        float ease = Mathf.Sin(progress * Mathf.PI * 0.5f);
        float currentWorldDiameter = maxWorldRadius * 2f * ease;
        float localDiameter = currentWorldDiameter / planetScale;
        float localThickness = Mathf.Lerp(0.03f, 0.005f, progress) / planetScale;

        transform.localScale = new Vector3(localDiameter, localThickness, localDiameter);

        if (mat != null)
        {
            Color c = new Color(1f, 0.05f, 0.65f);
            mat.SetColor("_EmissionColor", c * Mathf.Lerp(3.5f, 0f, progress));
        }
    }
}
