using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    public float speed = 40f;
    public float lifeTime = 2f;
    public int damage = 10;
    public bool isCritical = false;

    public int pierceCount = 0;
    public int bounceCount = 0;
    public bool explosive = false;

    [Header("Evoluções Lendárias")]
    public bool isSupernova = false;
    public bool isNebulaFlak = false;
    public bool isAntimatterLance = false;
    private float antimatterDropTimer = 0f;
    private bool hasFragmented = false;

    private System.Collections.Generic.Dictionary<Enemy, float> hitCooldowns = new System.Collections.Generic.Dictionary<Enemy, float>();

    public Transform planet;
    private float timer;
    public ObjectPool<GameObject> pool;
    private Rigidbody rb;
    private bool hasHit;
    private bool released;

    void Start() { 
        if(planet != null) { 
            transform.SetParent(planet, true); 
        } 
    }

    void Awake()
    {
        transform.localScale = new Vector3(0.8f, 0.8f, 0.8f); 
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        GravityBody gb = GetComponent<GravityBody>();
        if (gb != null) Destroy(gb);
    }

    void OnEnable()
    {
        timer = lifeTime;
        hasHit = false;
        released = false;
        isCritical = false;
        isSupernova = false;
        isNebulaFlak = false;
        isAntimatterLance = false;
        hasFragmented = false;
        antimatterDropTimer = 0f;
        hitCooldowns.Clear();
    }

    void Update()
    {
        if (hasHit || released) return;

        if (planet != null)
        {
            Vector3 localForward = planet.InverseTransformDirection(transform.forward);
            float localSpeed = speed / planet.localScale.x;
            Vector3 nextLocalPos = transform.localPosition + localForward * localSpeed * Time.deltaTime;
            
            float localRadius = 0.5f + (0.3f / planet.localScale.x);
            transform.localPosition = nextLocalPos.normalized * localRadius;

            Vector3 localSurfaceNormal = transform.localPosition.normalized;
            Vector3 localForwardOnSphere = Vector3.ProjectOnPlane(localForward, localSurfaceNormal).normalized;
            
            if (localForwardOnSphere.sqrMagnitude > 0.01f)
            {
                transform.localRotation = Quaternion.LookRotation(localForwardOnSphere, localSurfaceNormal);
            }
        }
        else 
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        if (isAntimatterLance && planet != null)
        {
            antimatterDropTimer -= Time.deltaTime;
            if (antimatterDropTimer <= 0f)
            {
                antimatterDropTimer = 0.08f;
                SpawnAntimatterZone();
            }
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (isNebulaFlak && !hasFragmented)
            {
                hasFragmented = true;
                SpawnNebulaShards();
            }
            ReleaseProjectile();
        }
    }

    void OnTriggerEnter(Collider other) { HandleHit(other.gameObject); }
    void OnTriggerStay(Collider other) { HandleHit(other.gameObject); }
    void OnCollisionEnter(Collision col) { HandleHit(col.gameObject); }
    void OnCollisionStay(Collision col) { HandleHit(col.gameObject); }

    void HandleHit(GameObject other)
    {
        if (released || !isActiveAndEnabled) return;

        BossLeviathan boss = other.GetComponentInParent<BossLeviathan>();
        if (boss != null)
        {
            boss.TakeDamage(damage, isCritical, isSupernova ? DamageTextStyle.Area : DamageTextStyle.Normal);
            GameObject fxPrefab = Resources.Load<GameObject>("BulletImpactFX");
            if (fxPrefab)
            {
                GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
                if (planet != null) fx.transform.SetParent(planet, true);
                Destroy(fx, 1f);
            }

            if (isSupernova)
            {
                // Micro-burst supernova
                Collider[] hits = Physics.OverlapSphere(transform.position, 2.5f);
                foreach (Collider c in hits)
                {
                    Enemy e = c.GetComponentInParent<Enemy>();
                    if (e != null && (!hitCooldowns.ContainsKey(e) || Time.time - hitCooldowns[e] >= 0.15f))
                    {
                        hitCooldowns[e] = Time.time;
                        e.TakeDamage(Mathf.RoundToInt(damage * 0.65f), false, DamageTextStyle.Area);
                    }
                }
            }

            if (isNebulaFlak && !hasFragmented)
            {
                hasFragmented = true;
                SpawnNebulaShards();
            }

            if (pierceCount > 0)
            {
                pierceCount--;
            }
            else
            {
                hasHit = true;
                ReleaseProjectile();
            }
            return;
        }

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            if (hitCooldowns.ContainsKey(enemy) && Time.time - hitCooldowns[enemy] < 0.2f) return;
            hitCooldowns[enemy] = Time.time;

            enemy.TakeDamage(damage, isCritical, isSupernova ? DamageTextStyle.Area : DamageTextStyle.Normal);

            if (isSupernova)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 2.6f);
                foreach (Collider c in hits)
                {
                    Enemy e = c.GetComponentInParent<Enemy>();
                    if (e != null && e != enemy && (!hitCooldowns.ContainsKey(e) || Time.time - hitCooldowns[e] >= 0.15f))
                    {
                        hitCooldowns[e] = Time.time;
                        e.TakeDamage(Mathf.RoundToInt(damage * 0.65f), false, DamageTextStyle.Area);
                    }
                }
            }

            if (isNebulaFlak && !hasFragmented)
            {
                hasFragmented = true;
                SpawnNebulaShards();
            }

            if (explosive)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
                foreach(Collider c in hits)
                {
                    Enemy e = c.GetComponentInParent<Enemy>();
                    if (e != null && e != enemy && (!hitCooldowns.ContainsKey(e) || Time.time - hitCooldowns[e] >= 0.2f))
                    {
                        hitCooldowns[e] = Time.time;
                        e.TakeDamage(damage, false, DamageTextStyle.Area);
                    }
                }
            }

            GameObject fxPrefab = Resources.Load<GameObject>("BulletImpactFX");
            if (fxPrefab)
            {
                GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
                if (planet != null) fx.transform.SetParent(planet, true);
                Destroy(fx, 1f);
            }

            if (pierceCount > 0)
            {
                pierceCount--;
            }
            else if (bounceCount > 0)
            {
                bounceCount--;
                Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                Enemy nearest = null;
                float minDist = float.MaxValue;
                foreach(Enemy e in allEnemies)
                {
                    if (e != enemy && !e.isDead && !hitCooldowns.ContainsKey(e))
                    {
                        float d = Vector3.Distance(transform.position, e.transform.position);
                        if (d < minDist && d < 15f)
                        {
                            minDist = d;
                            nearest = e;
                        }
                    }
                }
                if (nearest != null)
                {
                    transform.forward = (nearest.transform.position - transform.position).normalized;
                }
                else
                {
                    hasHit = true;
                    ReleaseProjectile();
                }
            }
            else
            {
                hasHit = true;
                ReleaseProjectile();
            }
        }
    }

    void SpawnAntimatterZone()
    {
        if (planet == null) return;
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zone.name = "AntimatterZone";
        zone.transform.position = transform.position;
        zone.transform.up = transform.up;
        zone.transform.SetParent(planet, true);
        float pScale = planet.lossyScale.x > 0.001f ? planet.lossyScale.x : 1f;
        zone.transform.localScale = new Vector3(1.6f / pScale, 0.04f / pScale, 1.6f / pScale);
        Destroy(zone.GetComponent<Collider>());

        MeshRenderer mr = zone.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color zoneColor = new Color(0.65f, 0f, 1f, 0.6f); // Radiant Violet
        mat.SetColor("_BaseColor", zoneColor);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", zoneColor * 3f);
        mr.material = mat;

        AntimatterZoneLogic logic = zone.AddComponent<AntimatterZoneLogic>();
        logic.Setup(Mathf.Max(5, Mathf.RoundToInt(damage * 0.35f)));
    }

    void SpawnNebulaShards()
    {
        if (planet == null) return;
        float shardAngleStep = 90f;
        for (int i = 0; i < 4; i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shard.name = "NebulaShard";
            shard.transform.position = transform.position;
            shard.transform.SetParent(planet, true);
            float pScale = planet.lossyScale.x > 0.001f ? planet.lossyScale.x : 1f;
            shard.transform.localScale = Vector3.one * (0.45f / pScale);
            Destroy(shard.GetComponent<Collider>());

            Vector3 shootDir = Quaternion.AngleAxis(i * shardAngleStep + 45f, transform.up) * transform.forward;
            shard.transform.rotation = Quaternion.LookRotation(shootDir, transform.up);

            MeshRenderer mr = shard.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color shardColor = new Color(1f, 0.2f, 0.8f); // Neon Magenta/Pink
            mat.SetColor("_BaseColor", shardColor);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", shardColor * 4f);
            mr.material = mat;

            NebulaShardLogic logic = shard.AddComponent<NebulaShardLogic>();
            logic.Setup(Mathf.Max(4, Mathf.RoundToInt(damage * 0.55f)), planet, 28f);
        }
    }

    void ReleaseProjectile()
    {
        if (released) return;
        released = true;
        if (pool != null) pool.Release(gameObject);
        else Destroy(gameObject);
    }
}

public class NebulaShardLogic : MonoBehaviour
{
    private int damage = 10;
    private Transform planet;
    private float speed = 25f;
    private float lifetime = 0.8f;

    public void Setup(int dmg, Transform p, float spd)
    {
        damage = dmg;
        planet = p;
        speed = spd;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (planet != null)
        {
            Vector3 localForward = planet.InverseTransformDirection(transform.forward);
            float localSpeed = speed / planet.localScale.x;
            Vector3 nextLocalPos = transform.localPosition + localForward * localSpeed * Time.deltaTime;
            float localRadius = 0.5f + (0.3f / planet.localScale.x);
            transform.localPosition = nextLocalPos.normalized * localRadius;
            Vector3 localSurfaceNormal = transform.localPosition.normalized;
            Vector3 localForwardOnSphere = Vector3.ProjectOnPlane(localForward, localSurfaceNormal).normalized;
            if (localForwardOnSphere.sqrMagnitude > 0.01f)
            {
                transform.localRotation = Quaternion.LookRotation(localForwardOnSphere, localSurfaceNormal);
            }
        }
        else
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, 0.7f);
        foreach (Collider c in hits)
        {
            BossLeviathan boss = c.GetComponentInParent<BossLeviathan>();
            if (boss != null)
            {
                boss.TakeDamage(damage, false, DamageTextStyle.Area);
                Destroy(gameObject);
                break;
            }
            Enemy e = c.GetComponentInParent<Enemy>();
            if (e != null && !e.isDead)
            {
                e.TakeDamage(damage, false, DamageTextStyle.Area);
                Destroy(gameObject);
                break;
            }
        }
    }
}

public class AntimatterZoneLogic : MonoBehaviour
{
    private int damage = 15;
    private float lifetime = 2.0f;
    private float tickTimer = 0f;

    public void Setup(int dmg)
    {
        damage = dmg;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            tickTimer = 0.25f;
            Collider[] hits = Physics.OverlapSphere(transform.position, 1.8f);
            foreach (Collider c in hits)
            {
                BossLeviathan boss = c.GetComponentInParent<BossLeviathan>();
                if (boss != null)
                {
                    boss.TakeDamage(damage, false, DamageTextStyle.Area);
                }
                Enemy e = c.GetComponentInParent<Enemy>();
                if (e != null && !e.isDead)
                {
                    e.TakeDamage(damage, false, DamageTextStyle.Area);
                }
            }
        }
    }
}
