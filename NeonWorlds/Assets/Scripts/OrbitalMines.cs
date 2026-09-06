using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitalMines : MonoBehaviour
{
    public static OrbitalMines Instance { get; private set; }

    public int level = 0; // 0 = not unlocked, 1..5
    public float dropInterval = 3.5f;
    public int mineDamage = 35;
    public float blastRadius = 3.5f;

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

        GameObject mine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mine.name = "VoidMine";
        mine.transform.position = transform.position;
        mine.transform.up = transform.up;
        mine.transform.localScale = new Vector3(0.8f, 0.15f, 0.8f);
        mine.transform.SetParent(planet, true);

        // Visuals
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
        core.transform.localPosition = new Vector3(0, 0.5f, 0);
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
    private float blastRadius = 3.5f;
    private Transform planet;
    private float armTimer = 0.5f;
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

        // Pulse scale
        float pulse = 1f + Mathf.PingPong(Time.time * 4f, 0.2f);
        transform.localScale = new Vector3(0.8f * pulse, 0.15f, 0.8f * pulse);

        // Check distance to any enemy or boss
        Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius * 0.7f);
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

        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.name = "MineBlastFX";
        fx.transform.position = transform.position;
        if (planet != null) fx.transform.SetParent(planet, true);
        Destroy(fx.GetComponent<Collider>());
        MeshRenderer fxMr = fx.GetComponent<MeshRenderer>();
        Material fxMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color fxColor = new Color(1f, 0.1f, 0.7f, 0.8f);
        fxMat.SetColor("_BaseColor", fxColor);
        fxMat.EnableKeyword("_EMISSION");
        fxMat.SetColor("_EmissionColor", fxColor * 3f);
        fxMr.material = fxMat;

        fx.AddComponent<MineExplosionFX>().Setup(blastRadius);
        Destroy(gameObject);
    }
}

public class MineExplosionFX : MonoBehaviour
{
    private float maxRadius = 4f;
    private float elapsed = 0f;
    private float duration = 0.35f;
    private Material mat;

    public void Setup(float radius)
    {
        maxRadius = radius;
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mat = mr.material;
        transform.localScale = Vector3.one * 0.2f;
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

        transform.localScale = Vector3.one * (maxRadius * 2f * progress);
        if (mat != null)
        {
            Color c = mat.GetColor("_BaseColor");
            c.a = 1f - progress;
            mat.SetColor("_BaseColor", c);
        }
    }
}
