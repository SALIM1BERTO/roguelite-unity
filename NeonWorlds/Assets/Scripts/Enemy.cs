using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public float xpRewardMultiplier = 1f;
    public static readonly List<Enemy> activeEnemies = new List<Enemy>();

    // Per-frame throttle to prevent burst stutter when hitting many enemies at once
    private static int s_hitSoundFrame = -1;
    private static int s_hitSoundCount;
    private static int s_floatTextFrame = -1;
    private static int s_floatTextCount;
    private const int MaxHitSoundsPerFrame = 8;
    private const int MaxFloatTextsPerFrame = 12;

    public int hp = 30;
    private Transform hpFill;
    public int maxHp = 30; 
    public int baseHp = -1;
    public float speed = 3f;
    public float baseSpeed = -1f;
    public bool isDead = false;

    [Header("Attack Settings")]
    public int attackDamage = 8;
    public float attackCooldown = 0.8f;
    public float attackRange = 1.3f;
    private float lastAttackTime = 0f;

    [Header("Flocking / Anti-Overlap")]
    public float avoidanceRadius = 1.2f;
    private Vector3 cachedSeparation = Vector3.zero;
    private int staggerOffset;

    private static Material s_BgMat;
    private static Material s_FillMat;

    public ObjectPool<GameObject> pool;
    public GameObject gemPrefab;

    private Material originalMat;
    private Material flashMat;
    private MeshRenderer meshR;
    private float flashUntil;
    private bool healthDirty;
    private Coroutine deathRoutine;
    private GravityBody gravityBody;
    private StatusEffectReceiver statusReceiver;
    public Vector3 baseWorldScale = Vector3.one;

    void Awake()
    {
        if (gameObject.name.Contains("Tank")) baseWorldScale = new Vector3(1.5f, 1.5f, 1.5f);
        else if (gameObject.name.Contains("Swarmer")) baseWorldScale = new Vector3(0.65f, 0.65f, 0.65f);
        else if (transform.localScale.x >= 0.2f && transform.localScale.x <= 3.0f) baseWorldScale = transform.localScale;
        else baseWorldScale = Vector3.one;

        staggerOffset = UnityEngine.Random.Range(0, 3);
        gravityBody = GetComponent<GravityBody>();
        statusReceiver = GetComponent<StatusEffectReceiver>();
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
        ResetScale();
    }

    public void ResetScale()
    {
        float pScale = 1f;
        if (gravityBody != null && gravityBody.planet != null)
        {
            pScale = gravityBody.planet.transform.lossyScale.x;
        }
        else if (transform.parent != null)
        {
            pScale = transform.parent.lossyScale.x;
        }
        pScale = Mathf.Max(0.001f, pScale);
        transform.localScale = baseWorldScale / pScale;
    }

    void Start()
    {
        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgObj.name = "HealthBg";
        bgObj.transform.SetParent(transform, false);
        bgObj.transform.localPosition = new Vector3(0, 1.2f, 0);
        bgObj.transform.localScale = new Vector3(0.55f, 0.045f, 0.1f);
        if (s_BgMat == null)
        {
            s_BgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            s_BgMat.SetColor("_BaseColor", Color.black);
        }
        bgObj.GetComponent<MeshRenderer>().sharedMaterial = s_BgMat;

        GameObject fillObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fillObj.name = "HealthFill";
        fillObj.transform.SetParent(bgObj.transform, false);
        fillObj.transform.localPosition = new Vector3(0, 0, -0.01f);
        fillObj.transform.localScale = Vector3.one;
        if (s_FillMat == null)
        {
            s_FillMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            s_FillMat.SetColor("_BaseColor", NeonUI.Danger);
        }
        fillObj.GetComponent<MeshRenderer>().sharedMaterial = s_FillMat;
        
        hpFill = fillObj.transform;

        Destroy(bgObj.GetComponent<Collider>());
        Destroy(fillObj.GetComponent<Collider>());
        ResetScale();
        UpdateHealthBar();
    }

    void OnEnable()
    {
        cachedSeparation=Vector3.zero;
        hp = maxHp;
        isDead = false;
        lastAttackTime = 0f;
        ResetScale();
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
        flashUntil=0; healthDirty=false;
        if (deathRoutine != null) StopCoroutine(deathRoutine);
        deathRoutine = null;
        ResetScale();
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
        if (statusReceiver != null && statusReceiver.IsStunned) return;
        if (GameManager.Instance == null || GameManager.Instance.player == null) return;

        // Verify that the player and enemy are on the exact same planet
        GravityBody playerBody = GameManager.Instance.player.GetComponent<GravityBody>();
        if (playerBody == null || playerBody.planet == null) return;

        Transform enemyPlanet = gravityBody != null && gravityBody.planet != null 
            ? gravityBody.planet.transform 
            : transform.parent;

        if (enemyPlanet == null || enemyPlanet != playerBody.planet.transform) return;

        Transform playerTransform = GameManager.Instance.player;
        Vector3 playerPos = playerTransform.position;
        Vector3 surfaceNormal = (transform.position - enemyPlanet.position).normalized;

        Vector3 toPlayer = playerPos - transform.position;
        float realDist = toPlayer.magnitude; // True 3D Euclidean distance

        Vector3 dirToPlayer = Vector3.ProjectOnPlane(toPlayer, surfaceNormal);
        float distToPlayer = dirToPlayer.magnitude;

        // 1. Attack Player Check - MUST satisfy BOTH tangent planar distance and true 3D Euclidean distance
        if (distToPlayer <= attackRange && realDist <= attackRange * 1.5f)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                GameManager.Instance.TakeDamage(attackDamage);
            }
        }

        if (((staggerOffset + Time.frameCount) % 3) == 0)
            cachedSeparation = EnemyNeighborhood.Separation(this,transform.position,surfaceNormal,avoidanceRadius);

        Vector3 separation = cachedSeparation;

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
        if (statusReceiver != null && statusReceiver.IsStunned) return;
        if (obj.GetComponentInParent<PlayerMovement>() != null || obj.GetComponentInParent<PlayerShip>() != null)
        {
            GravityBody playerBody = GetPlayerBody();
            if (playerBody == null || playerBody.planet == null) return;

            Transform enemyPlanet = gravityBody != null && gravityBody.planet != null 
                ? gravityBody.planet.transform 
                : transform.parent;

            if (enemyPlanet == null || enemyPlanet != playerBody.planet.transform) return;

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
        TakeDamage(damage, false, DamageTextStyle.Normal);
    }

    public void TakeDamage(int damage, bool isCrit)
    {
        TakeDamage(damage, isCrit, DamageTextStyle.Normal);
    }

    public void TakeDamage(int damage, bool isCrit, DamageTextStyle style)
    {
        if (isDead || !isActiveAndEnabled || damage <= 0) return;
        
        if (statusReceiver != null) damage = statusReceiver.ModifyIncomingDamage(damage);
        hp -= damage;
        isDead = hp <= 0;

        // Throttle hit sounds per frame to prevent audio system overload
        if (s_hitSoundFrame != Time.frameCount) { s_hitSoundFrame = Time.frameCount; s_hitSoundCount = 0; }
        if (s_hitSoundCount < MaxHitSoundsPerFrame)
        {
            s_hitSoundCount++;
            if (isCrit)
                GameAudio.Play(AudioCue.CriticalHit);
            else
                GameAudio.PlayAt(AudioCue.Hit, transform.position,
                    gravityBody != null ? gravityBody.planet : null);
        }
        healthDirty=true;
        
        if (meshR != null && flashMat != null && gameObject.activeInHierarchy) {
            flashUntil=Time.time+.05f;
            if(meshR.sharedMaterial!=flashMat)meshR.sharedMaterial=flashMat;
        }

        // Throttle floating text per frame to prevent TextMesh allocation spikes
        if (s_floatTextFrame != Time.frameCount) { s_floatTextFrame = Time.frameCount; s_floatTextCount = 0; }
        if (s_floatTextCount < MaxFloatTextsPerFrame)
        {
            s_floatTextCount++;
            Transform planet = gravityBody != null && gravityBody.planet != null
                ? gravityBody.planet.transform : null;
            Vector3 surfaceNormal = planet != null
                ? (transform.position - planet.position).normalized : Vector3.up;
            Vector3 textPosition = transform.position + surfaceNormal;
            FloatingText.Spawn(textPosition, planet, damage, isCrit, style);
        }

        if (isDead)
        {
            if (GameManager.Instance != null && GameManager.Instance.lifeStealChance > 0f)
            {
                if (Random.value < GameManager.Instance.lifeStealChance)
                {
                    GameManager.Instance.Heal(2);
                }
            }
            Die();
        }
    }
    
    void UpdateHealthBar()
    {
        if (hpFill != null) {
            hpFill.parent.gameObject.SetActive(hp > 0 && hp < maxHp);
            float ratio = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            hpFill.localScale = new Vector3(ratio, 1f, 1f);
            hpFill.localPosition = new Vector3((ratio - 1f) / 2f, 0, -0.01f);
        }
    }

    void LateUpdate()
    {
        if(!isDead && flashUntil>0 && Time.time>=flashUntil){flashUntil=0;RestoreMaterial();}
        if(healthDirty){healthDirty=false;UpdateHealthBar();}
        if(hpFill!=null && hpFill.parent.gameObject.activeSelf && Camera.main!=null) hpFill.parent.rotation=Camera.main.transform.rotation;
    }

    void Die()
    {
        if (deathRoutine != null) StopCoroutine(deathRoutine);
        deathRoutine = StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        isDead = true;
        activeEnemies.Remove(this);
        if (hpFill != null && hpFill.parent != null)
        {
            hpFill.parent.gameObject.SetActive(false);
        }

        // Clean death animation: white neon flash + rapid squash into surface (no particles)
        if (meshR != null && flashMat != null)
        {
            meshR.sharedMaterial = flashMat;
        }

        Vector3 startLocalScale = transform.localScale;
        float duration = 0.14f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Squash & implode into ground: horizontal expands slightly, vertical flattens quickly
            float horiz = Mathf.Lerp(1.15f, 0f, t * t);
            float vert = Mathf.Lerp(1f, 0f, Mathf.Sqrt(t));
            transform.localScale = new Vector3(startLocalScale.x * horiz, startLocalScale.y * vert, startLocalScale.z * horiz);
            yield return null;
        }

        ResetScale();
        RestoreMaterial();

        // Spawn XP Gem
        if (GameManager.Instance != null && EnemySpawner.Instance != null && EnemySpawner.Instance.gemPool != null)
        {
            XpGem.Drop(gravityBody != null ? gravityBody.planet : null, transform.position,
                XpProgression.Reward(GameManager.Instance.matchTime, xpRewardMultiplier));
        }

        deathRoutine = null;

        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void DespawnQuietly()
    {
        isDead = true;
        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void DespawnEnemiesOnOtherPlanets(PlanetGravity currentPlanet)
    {
        if (currentPlanet == null) return;
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (i >= activeEnemies.Count) continue;
            Enemy e = activeEnemies[i];
            if (e != null)
            {
                Transform enemyPlanet = e.gravityBody != null && e.gravityBody.planet != null 
                    ? e.gravityBody.planet.transform 
                    : e.transform.parent;

                if (enemyPlanet == null || enemyPlanet != currentPlanet.transform)
                {
                    e.DespawnQuietly();
                }
            }
        }
    }
}
