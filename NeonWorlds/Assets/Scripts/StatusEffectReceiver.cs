using UnityEngine;

// Reapplications refresh duration and retain the strongest tick; pooled lives never share status.
public sealed class StatusEffectReceiver : MonoBehaviour
{
    public bool IsBurning => remaining > 0f;
    float remaining, elapsed;
    int burnDamage;
    LineRenderer flame;
    Material flameMaterial;
    Enemy enemy;
    BossLeviathan boss;
    void Awake() { enemy = GetComponent<Enemy>(); boss = GetComponent<BossLeviathan>(); }
    void OnDisable() { Clear(); }
    public void Clear() { remaining = elapsed = 0f; burnDamage = 0; if (flame != null) flame.enabled = false; }
    public void ApplyBurn(int damagePerTick, float duration = 3f)
    {
        if (!isActiveAndEnabled || damagePerTick <= 0 || duration <= 0 || (enemy != null && enemy.isDead)) return;
        if (flame == null)
        {
            var visual = new GameObject("BurnFlame"); visual.transform.SetParent(transform, false);
            flame = visual.AddComponent<LineRenderer>(); flame.useWorldSpace = true;
            flameMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            flameMaterial.SetColor("_BaseColor", new Color(1f, .4f, 0f));
            flame.sharedMaterial = flameMaterial; flame.positionCount = 5;
            flame.startWidth = .055f; flame.endWidth = .012f;
        }
        flame.enabled = true;
        if (!IsBurning) elapsed = 0f;
        remaining = Mathf.Max(remaining, duration);
        burnDamage = Mathf.Max(burnDamage, damagePerTick);
    }
    void Update() { Advance(Time.deltaTime); }
    void LateUpdate()
    {
        if (!IsBurning || flame == null) return;
        Vector3 up = transform.up;
        Vector3 side = transform.right;
        for (int i = 0; i < 5; i++)
            flame.SetPosition(i, transform.position + up * (.2f + i * .13f)
                + side * (Mathf.Sin(Time.time * 18f + i * 2f) * .12f));
    }
    void OnDestroy() { if (flameMaterial != null) Destroy(flameMaterial); }
    public void Advance(float delta)
    {
        if (!IsBurning || delta <= 0f) return;
        elapsed += Mathf.Min(delta, remaining);
        remaining = Mathf.Max(0f, remaining - delta);
        while (elapsed >= .25f - .00001f)
        {
            elapsed -= .25f;
            if (enemy != null && !enemy.isDead && enemy.isActiveAndEnabled)
                enemy.TakeDamage(burnDamage, false, DamageTextStyle.Burn);
            else if (boss != null && boss.isActiveAndEnabled)
                boss.TakeDamage(burnDamage, false, DamageTextStyle.Burn);
            else { Clear(); return; }
            if (!isActiveAndEnabled || (enemy != null && enemy.isDead)) { Clear(); return; }
        }
        if (remaining <= 0f) Clear();
    }
}
