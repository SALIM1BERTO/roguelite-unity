using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float speed = 22f;
    public float lifeTime = 5.0f;
    public int damage = 16;
    public Transform planet;

    private float timer;
    private bool hasHit = false;

    void Start()
    {
        timer = lifeTime;
        if (planet != null)
        {
            transform.SetParent(planet, true);
        }

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color neonColor = new Color(1f, 0.1f, 0.5f); // Neon Magenta
            mat.SetColor("_BaseColor", neonColor);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", neonColor * 4f);
            mr.material = mat;
        }

        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.8f;
    }

    void Update()
    {
        if (hasHit) return;

        if (planet != null)
        {
            Vector3 localForward = planet.InverseTransformDirection(transform.forward);
            float localSpeed = speed / planet.localScale.x;
            Vector3 nextLocalPos = transform.localPosition + localForward * localSpeed * Time.deltaTime;
            
            float localRadius = 0.5f + (0.5f / planet.localScale.x);
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
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    void OnCollisionEnter(Collision col)
    {
        HandleHit(col.gameObject);
    }

    void HandleHit(GameObject other)
    {
        if (hasHit) return;

        PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
        PlayerShip ps = other.GetComponentInParent<PlayerShip>();

        if (pm != null || ps != null)
        {
            hasHit = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakeDamage(damage);
            }

            GameObject fxPrefab = Resources.Load<GameObject>("BulletImpactFX");
            if (fxPrefab != null)
            {
                GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
                if (planet != null) fx.transform.SetParent(planet, true);
                Destroy(fx, 1f);
            }

            Destroy(gameObject);
        }
    }
}
