using System.Collections;
using UnityEngine;

public class ShieldAegis : MonoBehaviour
{
    public static ShieldAegis Instance { get; private set; }

    public int level = 0; // 0 = locked, 1..5
    public float rechargeTime = 18f;
    public bool isShieldActive = false;

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
        Color c = new Color(0f, 0.6f, 1f, 0.35f);
        shieldMat.SetColor("_BaseColor", c);
        shieldMat.EnableKeyword("_EMISSION");
        shieldMat.SetColor("_EmissionColor", new Color(0f, 0.5f, 1f) * 1.5f);
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

        // Flash shield break
        if (shieldVisual != null)
        {
            StartCoroutine(ShieldBreakPulse());
        }

        return true;
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
        }
    }
}
