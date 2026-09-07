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
    private LineRenderer ring1;
    private LineRenderer ring2;
    private LineRenderer ring3;
    private Material ringMaterial;

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
        if (shieldVisual != null) Destroy(shieldVisual);

        shieldVisual = new GameObject("AegisShieldVisual");
        shieldVisual.transform.SetParent(transform, false);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localRotation = Quaternion.identity;

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

        ringMaterial = new Material(unlitShader);
        UpdateColors();

        // 1. Horizontal Orbit Ring (Equator around the ship)
        GameObject r1Obj = new GameObject("OrbitRing_Horizontal");
        r1Obj.transform.SetParent(shieldVisual.transform, false);
        ring1 = r1Obj.AddComponent<LineRenderer>();
        ring1.useWorldSpace = false;
        ring1.loop = true;
        ring1.positionCount = 36;
        ring1.widthMultiplier = 0.07f;
        ring1.sharedMaterial = ringMaterial;
        for (int i = 0; i < 36; i++)
        {
            float ang = i * Mathf.PI * 2f / 36f;
            ring1.SetPosition(i, new Vector3(Mathf.Sin(ang) * 1.55f, 0f, Mathf.Cos(ang) * 1.55f));
        }

        // 2. Tilted Orbit Ring (45 degrees)
        GameObject r2Obj = new GameObject("OrbitRing_Tilted");
        r2Obj.transform.SetParent(shieldVisual.transform, false);
        r2Obj.transform.localRotation = Quaternion.Euler(45f, 30f, 0f);
        ring2 = r2Obj.AddComponent<LineRenderer>();
        ring2.useWorldSpace = false;
        ring2.loop = true;
        ring2.positionCount = 36;
        ring2.widthMultiplier = 0.07f;
        ring2.sharedMaterial = ringMaterial;
        for (int i = 0; i < 36; i++)
        {
            float ang = i * Mathf.PI * 2f / 36f;
            ring2.SetPosition(i, new Vector3(Mathf.Sin(ang) * 1.5f, 0f, Mathf.Cos(ang) * 1.5f));
        }

        // 3. Counter-tilted Ring (-45 degrees)
        GameObject r3Obj = new GameObject("OrbitRing_CounterTilted");
        r3Obj.transform.SetParent(shieldVisual.transform, false);
        r3Obj.transform.localRotation = Quaternion.Euler(-45f, -30f, 0f);
        ring3 = r3Obj.AddComponent<LineRenderer>();
        ring3.useWorldSpace = false;
        ring3.loop = true;
        ring3.positionCount = 36;
        ring3.widthMultiplier = 0.05f;
        ring3.sharedMaterial = ringMaterial;
        for (int i = 0; i < 36; i++)
        {
            float ang = i * Mathf.PI * 2f / 36f;
            ring3.SetPosition(i, new Vector3(Mathf.Sin(ang) * 1.45f, 0f, Mathf.Cos(ang) * 1.45f));
        }

        shieldVisual.SetActive(isShieldActive);
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
        else if (shieldVisual != null && shieldVisual.activeSelf)
        {
            float pulse = 1f + Mathf.PingPong(Time.time * 2.5f, 0.05f);
            shieldVisual.transform.localScale = Vector3.one * pulse;

            if (ring1 != null) ring1.transform.Rotate(0, 45f * Time.deltaTime, 0);
            if (ring2 != null) ring2.transform.Rotate(35f * Time.deltaTime, 60f * Time.deltaTime, 0);
            if (ring3 != null) ring3.transform.Rotate(-30f * Time.deltaTime, -50f * Time.deltaTime, 20f * Time.deltaTime);
        }
    }

    public bool TryAbsorbDamage()
    {
        if (level <= 0 || !isShieldActive) return false;

        // Absorb hit!
        isShieldActive = false;
        rechargeTimer = Mathf.Max(6f, rechargeTime - (level - 1) * 2.5f);

        GameAudio.Play(AudioCue.Pickup);

        if (isHyperionBarrier)
        {
            TriggerHyperionPulse();
        }

        // Flash shield break animation
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
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float progress = t / 0.25f;
            if (shieldVisual != null)
            {
                shieldVisual.transform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.8f, progress);
                if (ringMaterial != null)
                {
                    Color baseCol = isHyperionBarrier ? new Color(1f, 0.85f, 0.1f) : new Color(0f, 1f, 0.95f);
                    ringMaterial.SetColor("_BaseColor", baseCol * (1f - progress));
                }
            }
            yield return null;
        }
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
            shieldVisual.transform.localScale = Vector3.one;
            UpdateColors();
        }
    }

    void UpdateColors()
    {
        if (ringMaterial == null) return;
        Color c = isHyperionBarrier ? new Color(1f, 0.85f, 0.15f, 1f) : new Color(0f, 0.95f, 1f, 1f);
        ringMaterial.SetColor("_BaseColor", c);
        ringMaterial.EnableKeyword("_EMISSION");
        ringMaterial.SetColor("_EmissionColor", c * 3.5f);
    }

    void UpdateVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(isShieldActive);
            UpdateColors();
        }
        else if (level > 0)
        {
            BuildVisual();
        }
    }

    void OnDestroy()
    {
        if (ringMaterial != null) Destroy(ringMaterial);
    }
}
