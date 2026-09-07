using System.Collections;
using UnityEngine;

public class ShieldAegis : MonoBehaviour
{
    public static ShieldAegis Instance { get; private set; }

    public int level = 0; // 0 = locked, 1..5
    public float rechargeTime = 18f;
    public bool isShieldActive = false;
    public bool isHyperionBarrier = false;

    private float rechargeTimer = 0f;
    private GameObject shieldVisual;
    private Material shieldMat;

    void Awake()
    {
        Instance = this;
    }

    public void SetLevel(int newLevel)
    {
        level = newLevel;
        if (level > 0 && shieldVisual == null)
        {
            BuildVisual();
            isShieldActive = true;
            UpdateVisual();
        }
        else if (shieldVisual != null)
        {
            UpdateVisual();
        }
    }

    void BuildVisual()
    {
        shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldVisual.name = "AegisShieldVisual";
        shieldVisual.transform.SetParent(transform, false);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localScale = Vector3.one * 2.2f;
        Destroy(shieldVisual.GetComponent<Collider>());

        MeshRenderer mr = shieldVisual.GetComponent<MeshRenderer>();
        shieldMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color c = isHyperionBarrier ? new Color(1f, 0.82f, 0.15f, 0.45f) : new Color(0f, 0.6f, 1f, 0.35f);
        shieldMat.SetColor("_BaseColor", c);
        shieldMat.EnableKeyword("_EMISSION");
        shieldMat.SetColor("_EmissionColor", (isHyperionBarrier ? new Color(1f, 0.75f, 0.1f) : new Color(0f, 0.5f, 1f)) * 2.5f);
        mr.material = shieldMat;
    }

    void Update()
    {
        if (level <= 0) return;

        if (!isShieldActive)
        {
            rechargeTimer -= Time.deltaTime;
            if (rechargeTimer <= 0f)
            {
                isShieldActive = true;
                GameAudio.Play(AudioCue.Upgrade);
                UpdateVisual();
                if (isHyperionBarrier)
                {
                    TriggerHyperionPulse();
                }
            }
        }
        else if (shieldVisual != null)
        {
            float pulse = 1f + Mathf.PingPong(Time.time * 2f, 0.08f);
            shieldVisual.transform.localScale = Vector3.one * (2.2f * pulse);
            shieldVisual.transform.Rotate(0, 30f * Time.deltaTime, 0);
        }
    }

    public bool TryAbsorbDamage()
    {
        if (level <= 0 || !isShieldActive) return false;

        // Absorb hit!
        isShieldActive = false;
        rechargeTimer = Mathf.Max(8f, rechargeTime - (level * 2.5f));
        UpdateVisual();

        GameAudio.Play(AudioCue.Explosion);

        if (isHyperionBarrier)
        {
            TriggerHyperionPulse();
        }

        // Flash shield break
        if (shieldVisual != null)
        {
            StartCoroutine(ShieldBreakPulse());
        }

        return true;
    }

    void TriggerHyperionPulse()
    {
        GameAudio.Play(AudioCue.HyperionPulse);

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.TriggerShake(0.2f, 0.45f);
        }

        Transform planet = null;
        GravityBody gb = GetComponentInParent<GravityBody>();
        if (gb != null && gb.planet != null) planet = gb.planet.transform;
        else if (transform.parent != null) planet = transform.parent;

        float pulseRadius = 7.5f;
        int pulseDamage = 60;

        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRadius);
        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.isDead)
            {
                Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
                enemy.transform.position += knockDir * 2.5f;
                enemy.TakeDamage(pulseDamage, true, DamageTextStyle.Area);
            }

            BossLeviathan boss = col.GetComponentInParent<BossLeviathan>();
            if (boss != null)
            {
                boss.TakeDamage(pulseDamage, true, DamageTextStyle.Area);
            }
        }

        // Radiant Golden Expanding Cylinder Ring
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "HyperionShockwaveRing";
        ring.transform.position = transform.position + transform.up * 0.05f;
        ring.transform.up = transform.up;
        Destroy(ring.GetComponent<Collider>());

        if (planet != null) ring.transform.SetParent(planet, true);

        MeshRenderer mr = ring.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color gold = new Color(1f, 0.85f, 0.1f);
        mat.SetColor("_BaseColor", gold);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", gold * 4f);
        mr.material = mat;

        float pScale = planet != null ? planet.lossyScale.x : 1f;
        ring.AddComponent<MineShockwaveRing>().Setup(pulseRadius, pScale, mat);
    }

    IEnumerator ShieldBreakPulse()
    {
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            if (shieldVisual != null) shieldVisual.transform.localScale = Vector3.one * (2.2f + t * 4f);
            yield return null;
        }
        if (shieldVisual != null) shieldVisual.SetActive(false);
    }

    void UpdateVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(isShieldActive);
            if (shieldMat != null)
            {
                Color c = isHyperionBarrier ? new Color(1f, 0.82f, 0.15f, 0.45f) : new Color(0f, 0.6f, 1f, 0.35f);
                shieldMat.SetColor("_BaseColor", c);
                shieldMat.SetColor("_EmissionColor", (isHyperionBarrier ? new Color(1f, 0.75f, 0.1f) : new Color(0f, 0.5f, 1f)) * 2.5f);
            }
        }
    }
}
