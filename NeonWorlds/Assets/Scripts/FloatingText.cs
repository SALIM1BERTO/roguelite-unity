using UnityEngine;

public enum DamageTextStyle
{
    Normal,
    Critical,
    Area,
    Electric
}

public class FloatingText : MonoBehaviour
{
    private TextMesh textMesh;
    private float lifetime = 0.85f;
    private float timer = 0f;
    private float baseScale = 1f;
    private Vector3 lateralVelocity = Vector3.zero;
    private float verticalSpeed = 2.4f;

    public void Setup(string text)
    {
        Setup(text, new Color(0.92f, 0.98f, 1f), 1f);
    }

    public void SetupDamage(int damage, bool isCrit, DamageTextStyle style = DamageTextStyle.Normal)
    {
        string display;
        Color color;
        float scale;

        if (isCrit)
        {
            display = $"★ {damage}!";
            color = new Color(1f, 0.88f, 0.15f); // Radiant Gold
            scale = 1.55f;
        }
        else if (style == DamageTextStyle.Area)
        {
            display = damage.ToString();
            color = new Color(0.92f, 0.25f, 1f); // Neon Violet
            scale = 1.25f;
        }
        else if (style == DamageTextStyle.Electric)
        {
            display = damage.ToString();
            color = new Color(0f, 0.95f, 1f); // Electric Cyan
            scale = 1.15f;
        }
        else
        {
            display = damage.ToString();
            color = new Color(0.92f, 0.98f, 1f); // Crisp Cyan-White
            scale = 1.0f;
        }

        Setup(display, color, scale);
    }

    public void Setup(string text, Color color, float sizeMultiplier = 1f)
    {
        timer = 0f;
        baseScale = sizeMultiplier;
        textMesh = gameObject.GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
        }

        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f * sizeMultiplier;
        textMesh.fontSize = Mathf.RoundToInt(26 * sizeMultiplier);
        textMesh.color = color;
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        if (mr != null && textMesh.font != null)
        {
            mr.sharedMaterial = textMesh.font.material;
        }

        // Add soft lateral blossom velocity so damage numbers don't stack directly on top of each other
        Transform planet = transform.parent;
        Vector3 surfaceNormal = planet != null
            ? (transform.position - planet.position).normalized : Vector3.up;
        Vector3 randomTang = Vector3.Cross(surfaceNormal, Random.onUnitSphere).normalized;
        lateralVelocity = randomTang * Random.Range(-0.85f, 0.85f);

        // Initial punch scale
        transform.localScale = Vector3.one * (baseScale * 1.5f);

        FaceCamera();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Punch-scale settle animation (pops up and settles down in 0.15s)
        float popDuration = 0.15f;
        if (timer < popDuration)
        {
            float p = timer / popDuration;
            float currentScale = Mathf.Lerp(baseScale * 1.5f, baseScale, Mathf.Sin(p * Mathf.PI * 0.5f));
            transform.localScale = Vector3.one * currentScale;
        }
        else
        {
            transform.localScale = Vector3.one * baseScale;
        }

        // Upward and lateral drift
        Transform planet = transform.parent;
        Vector3 surfaceNormal = planet != null
            ? (transform.position - planet.position).normalized : Vector3.up;
        transform.position += (surfaceNormal * verticalSpeed + lateralVelocity) * Time.deltaTime;
        lateralVelocity = Vector3.Lerp(lateralVelocity, Vector3.zero, Time.deltaTime * 3f);

        // Smooth fade out in the last 40% of lifetime
        if (textMesh != null)
        {
            float fadeStart = 0.55f;
            float alpha = timer < fadeStart ? 1f : Mathf.Clamp01(1f - ((timer - fadeStart) / (lifetime - fadeStart)));
            Color c = textMesh.color;
            c.a = alpha;
            textMesh.color = c;
        }
    }

    void LateUpdate()
    {
        FaceCamera();
    }

    void FaceCamera()
    {
        Camera camera = Camera.main;
        if (camera != null) transform.rotation = camera.transform.rotation;
    }
}
