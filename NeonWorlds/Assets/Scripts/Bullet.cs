using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    public float speed = 40f;
    public float lifeTime = 2f;
    public int damage = 10;

    public int pierceCount = 0;
    public int bounceCount = 0;
    public bool explosive = false;
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

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
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
            boss.TakeDamage(damage);
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

            enemy.TakeDamage(damage);

            if (explosive)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
                foreach(Collider c in hits)
                {
                    Enemy e = c.GetComponentInParent<Enemy>();
                    if (e != null && e != enemy && (!hitCooldowns.ContainsKey(e) || Time.time - hitCooldowns[e] >= 0.2f))
                    {
                        hitCooldowns[e] = Time.time;
                        e.TakeDamage(damage);
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

    void ReleaseProjectile()
    {
        if (released) return;
        released = true;
        if (pool != null) pool.Release(gameObject);
        else Destroy(gameObject);
    }
}
