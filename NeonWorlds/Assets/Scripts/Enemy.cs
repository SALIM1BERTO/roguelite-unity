using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> activeEnemies = new List<Enemy>();

    public int hp = 30;
    private Transform hpFill;
    public int maxHp = 30; 
    public int baseHp = -1;
    public float speed = 3f;
    public bool isDead = false;

    [Header("Attack Settings")]
    public int attackDamage = 8;
    public float attackCooldown = 0.8f;
    public float attackRange = 1.3f;
    private float lastAttackTime = 0f;

    [Header("Flocking / Anti-Overlap")]
    public float avoidanceRadius = 1.2f;

    public ObjectPool<GameObject> pool;
    public GameObject gemPrefab;

    private Material originalMat;
    private Material flashMat;
    private MeshRenderer meshR;
    private Coroutine flashRoutine;
    private GravityBody gravityBody;

    void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        if (gravityBody != null)
        {
            gravityBody.surfaceOffset = 0.5f;
        }

        Transform visual = transform.Find("EnemyVisual");
        if (visual != null)
        {
            visual.localPosition = Vector3.zero;
        }

        meshR = GetComponentInChildren<MeshRenderer>();
        if (meshR != null) originalMat = meshR.sharedMaterial;
        if (originalMat != null) {
            flashMat = new Material(originalMat);
            flashMat.SetColor("_BaseColor", Color.white);
            flashMat.EnableKeyword("_EMISSION");
            flashMat.SetColor("_EmissionColor", Color.white * 2f);
        }
    }

    void Start()
    {
        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgObj.name = "HealthBg";
        bgObj.transform.SetParent(transform, false);
        bgObj.transform.localPosition = new Vector3(0, 1.2f, 0);
        bgObj.transform.localScale = new Vector3(0.5f, 0.1f, 0.1f);
        Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        bgMat.SetColor("_BaseColor", Color.black);
        bgObj.GetComponent<MeshRenderer>().sharedMaterial = bgMat;

        GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fillObj.name = "HealthFill";
        fillObj.transform.SetParent(bgObj.transform, false);
        fillObj.transform.localPosition = new Vector3(0, 0, -0.01f);
        fillObj.transform.localScale = Vector3.one;
        Material fillMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        fillMat.SetColor("_BaseColor", Color.red);
        fillObj.GetComponent<MeshRenderer>().sharedMaterial = fillMat;
        
        hpFill = fillObj.transform;

        Destroy(bgObj.GetComponent<Collider>());
        Destroy(fillObj.GetComponent<Collider>());
        UpdateHealthBar();
    }

    void OnEnable()
    {
        hp = maxHp;
        isDead = false;
        lastAttackTime = 0f;
        RestoreMaterial();
        UpdateHealthBar();
        if (!activeEnemies.Contains(this))
        {
            activeEnemies.Add(this);
        }
    }

    void OnDisable()
    {
        activeEnemies.Remove(this);
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = null;
        RestoreMaterial();
    }

    void OnDestroy()
    {
        activeEnemies.Remove(this);
        if (flashMat != null) Destroy(flashMat);
    }

    void RestoreMaterial()
    {
        if (meshR != null) meshR.sharedMaterial = originalMat;
    }

    void Update()
    {
        if (isDead) return;
        if (GameManager.Instance == null || GameManager.Instance.player == null) return;

        Transform playerTransform = GameManager.Instance.player;
        Vector3 playerPos = playerTransform.position;
        Vector3 surfaceNormal = transform.parent != null
            ? (transform.position - transform.parent.position).normalized
            : transform.up;

        Vector3 toPlayer = playerPos - transform.position;
        Vector3 dirToPlayer = Vector3.ProjectOnPlane(toPlayer, surfaceNormal);
        float distToPlayer = dirToPlayer.magnitude;

        // 1. Attack Player Check
        if (distToPlayer <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                GameManager.Instance.TakeDamage(attackDamage);
            }
        }

        // 2. Separation from other enemies (Anti-Overlap)
        Vector3 separation = Vector3.zero;
        int neighborCount = 0;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy other = activeEnemies[i];
            if (other == this || other == null || other.isDead) continue;

            Vector3 diff = transform.position - other.transform.position;
            Vector3 planarDiff = Vector3.ProjectOnPlane(diff, surfaceNormal);
            float sqrDist = planarDiff.sqrMagnitude;

            if (sqrDist < avoidanceRadius * avoidanceRadius && sqrDist > 0.0001f)
            {
                float d = Mathf.Sqrt(sqrDist);
                float strength = (avoidanceRadius - d) / avoidanceRadius;
                separation += (planarDiff / d) * strength;
                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            separation /= neighborCount;
        }

        // 3. Movement direction
        Vector3 seekDir = dirToPlayer.normalized;
        if (distToPlayer < 0.8f)
        {
            seekDir = Vector3.zero; // Stop crushing into player center
        }

        Vector3 finalDir = seekDir;
        if (separation.sqrMagnitude > 0.001f)
        {
            finalDir = (seekDir + separation * 2.5f).normalized;
        }

        float planetScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        float localSpeed = speed / planetScale;
        Vector3 localMoveDir = transform.parent != null ? transform.parent.InverseTransformDirection(finalDir) : finalDir;

        transform.localPosition += localMoveDir * localSpeed * Time.deltaTime;

        // Direct soft separation displacement to prevent physical stacking
        if (separation.sqrMagnitude > 0.01f)
        {
            Vector3 localNudge = transform.parent != null ? transform.parent.InverseTransformDirection(separation) : separation;
            transform.localPosition += localNudge * (localSpeed * 0.8f) * Time.deltaTime;
        }
    }

    void OnCollisionStay(Collision col)
    {
        TryDamagePlayer(col.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        TryDamagePlayer(other.gameObject);
    }

    void TryDamagePlayer(GameObject obj)
    {
        if (isDead) return;
        if (obj.GetComponentInParent<PlayerMovement>() != null || obj.GetComponentInParent<PlayerShip>() != null)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TakeDamage(attackDamage);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || !isActiveAndEnabled || damage <= 0) return;
        
        hp -= damage;
        isDead = hp <= 0;
        GameAudio.PlayAt(isDead ? AudioCue.Explosion : AudioCue.Hit, transform.position,
            gravityBody != null ? gravityBody.planet : null);
        UpdateHealthBar();
        
        if (meshR != null && flashMat != null && gameObject.activeInHierarchy) {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        Transform planet = gravityBody != null && gravityBody.planet != null
            ? gravityBody.planet.transform : null;
        Vector3 surfaceNormal = planet != null
            ? (transform.position - planet.position).normalized : Vector3.up;
        Vector3 textPosition = transform.position + surfaceNormal;
        GameObject txtObj = null;
        if (GameManager.Instance != null && GameManager.Instance.floatingTextPrefab != null)
        {
            txtObj = Instantiate(GameManager.Instance.floatingTextPrefab, textPosition, Quaternion.identity);
        }
        else
        {
            txtObj = new GameObject("FloatingText");
            txtObj.transform.position = textPosition;
        }

        if (planet != null) txtObj.transform.SetParent(planet, true);
        FloatingText ft = txtObj.GetComponent<FloatingText>();
        if (ft == null) ft = txtObj.AddComponent<FloatingText>();
        ft.Setup(damage.ToString());

        if (isDead)
        {
            Die();
        }
    }
    
    IEnumerator FlashRoutine()
    {
        meshR.sharedMaterial = flashMat;
        yield return new WaitForSeconds(0.05f);
        RestoreMaterial();
        flashRoutine = null;
    }

    void UpdateHealthBar()
    {
        if (hpFill != null) {
            float ratio = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            hpFill.localScale = new Vector3(ratio, 1f, 1f);
            hpFill.localPosition = new Vector3((ratio - 1f) / 2f, 0, -0.01f);
        }
    }

    void Die()
    {
        if (meshR != null && originalMat != null) {
            for (int i = 0; i < 5; i++) {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = transform.position + Random.insideUnitSphere * 0.5f;
                cube.transform.localScale = Vector3.one * Random.Range(0.2f, 0.5f);
                cube.GetComponent<MeshRenderer>().sharedMaterial = originalMat;
                
                Rigidbody crb = cube.AddComponent<Rigidbody>();
                crb.useGravity = false;
                crb.AddExplosionForce(500f, transform.position, 2f);
                
                GravityBody gbCube = cube.AddComponent<GravityBody>();
                if (gravityBody != null) gbCube.planet = gravityBody.planet;
                
                Destroy(cube, 2f);
            }
        }

        if (GameManager.Instance != null && EnemySpawner.Instance != null && EnemySpawner.Instance.gemPool != null)
        {
            GameObject gem = EnemySpawner.Instance.gemPool.Get();
            GravityBody gemGb = gem.GetComponent<GravityBody>();
            if (gemGb != null) gemGb.planet = gravityBody != null ? gravityBody.planet : null;
            gem.transform.position = transform.position;
        }

        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
