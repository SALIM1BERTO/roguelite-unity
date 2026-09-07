using UnityEngine;

public sealed class WeaponPlasmaFlamer : MonoBehaviour
{
    public const float Range = 8f;
    public const float HalfAngle = 22.5f;
    readonly LineRenderer[] streams = new LineRenderer[5];
    Material material;
    AudioSource sound;
    AudioClip clip;
    float timer;
    bool firing;
    void Awake()
    {
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", new Color(1f, .3f, .04f));
        for (int i = 0; i < streams.Length; i++)
        {
            var obj = new GameObject("PlasmaStream"); obj.transform.SetParent(transform, false);
            var line = obj.AddComponent<LineRenderer>(); streams[i] = line;
            line.sharedMaterial = material; line.useWorldSpace = true; line.positionCount = 17;
            line.startWidth = .055f; line.endWidth = .012f; line.enabled = false;
        }
        // A seamless, deterministic filtered-noise loop, generated once per weapon.
        const int count = 22050;
        var samples = new float[count]; uint seed = 7331; float filtered = 0;
        for (int i = 0; i < count; i++)
        {
            seed = seed * 1664525u + 1013904223u;
            filtered = Mathf.Lerp(filtered, ((seed >> 8) / 16777215f) * 2f - 1f, .18f);
            samples[i] = filtered * .25f * Mathf.Sin(Mathf.PI * i / (count - 1));
        }
        clip = AudioClip.Create("Plasma combustion", count, 1, 22050, false); clip.SetData(samples, 0);
        sound = gameObject.AddComponent<AudioSource>(); sound.clip = clip; sound.loop = true;
        sound.playOnAwake = false; sound.volume = .35f; sound.spatialBlend = 0; sound.dopplerLevel = 0;
    }
    public static bool Contains(Transform planet, Vector3 origin, Vector3 direction, Vector3 target)
    {
        if (planet == null) return false;
        Vector3 normal = (origin - planet.position).normalized;
        Vector3 targetNormal = (target - planet.position).normalized;
        float angle = Vector3.Angle(normal, targetNormal) * Mathf.Deg2Rad;
        float radius = Mathf.Abs(planet.lossyScale.x) * .5f;
        if (angle >= Mathf.PI - .001f || angle * radius > Range) return false;
        Vector3 tangent = Vector3.ProjectOnPlane(targetNormal, normal);
        return tangent.sqrMagnitude < .000001f || Vector3.Angle(Vector3.ProjectOnPlane(direction, normal), tangent) <= HalfAngle;
    }
    public void SetFiring(bool active, Vector3 direction, int damage, float rateMultiplier)
    {
        GravityBody body = GetComponent<GravityBody>();
        if (!active || Time.timeScale <= 0f || body == null || body.planet == null) { StopFiring(); return; }
        Transform planet = body.planet.transform;
        Vector3 normal = (transform.position - planet.position).normalized;
        direction = Vector3.ProjectOnPlane(direction, normal).normalized;
        if (direction.sqrMagnitude < .01f) direction = transform.forward;
        if (!firing) { timer = 0; sound.Play(); }
        firing = true;
        GameAudio mix = GameAudio.Instance;
        sound.volume = mix != null ? (mix.muted ? 0f : .35f * mix.masterVolume * mix.gameplayVolume) : .22f;
        for (int i = 0; i < streams.Length; i++)
        {
            var line = streams[i]; line.enabled = true;
            Vector3 ray = Quaternion.AngleAxis(Mathf.Lerp(-HalfAngle, HalfAngle, i / 4f), normal) * direction;
            float range = Mathf.Min(Range, Mathf.Abs(planet.lossyScale.x) * .5f * (Mathf.PI - .01f));
            for (int j = 0; j < 17; j++)
                line.SetPosition(j, BossWorldMotion.SurfacePoint(planet, transform.position, ray, range * j / 16f * (1f + (body.surfaceOffset + .1f) / (Mathf.Abs(planet.lossyScale.x) * .5f)), body.surfaceOffset + .1f));
        }
        timer -= Time.deltaTime;
        if (timer > 0) return;
        timer += 1f / (10f * Mathf.Clamp(rateMultiplier, .1f, 5f));
        // Iterate backwards: lethal damage can remove the current enemy from the registry.
        for (int i = Enemy.activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = Enemy.activeEnemies[i];
            if (enemy == null || enemy.isDead) continue;
            GravityBody targetBody = enemy.GetComponent<GravityBody>();
            if (targetBody == null || targetBody.planet != body.planet || !Contains(planet, transform.position, direction, enemy.transform.position)) continue;
            Burn(enemy.gameObject, damage); enemy.TakeDamage(damage, false, DamageTextStyle.Burn);
        }
        BossLeviathan boss = BossLeviathan.Instance;
        if (boss != null && boss.isActiveAndEnabled)
        {
            GravityBody targetBody = boss.GetComponent<GravityBody>();
            if (targetBody != null && targetBody.planet == body.planet && Contains(planet, transform.position, direction, boss.transform.position))
            { Burn(boss.gameObject, damage); boss.TakeDamage(damage, false, DamageTextStyle.Burn); }
        }
    }
    static void Burn(GameObject target, int damage)
    {
        var status = target.GetComponent<StatusEffectReceiver>();
        if (status == null) status = target.AddComponent<StatusEffectReceiver>();
        status.ApplyBurn(Mathf.Max(1, damage));
    }
    public void StopFiring()
    {
        firing = false; timer = 0;
        if (sound != null) sound.Stop();
        foreach (var line in streams) if (line != null) line.enabled = false;
    }
    void OnDisable() { StopFiring(); }
    void OnDestroy() { if (sound != null) Destroy(sound); if (clip != null) Destroy(clip); if (material != null) Destroy(material); }
}

