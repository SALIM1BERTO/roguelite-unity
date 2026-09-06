using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossLeviathan : MonoBehaviour
{
    public static BossLeviathan Instance;

    [Header("Stats")]
    public int maxHp = 800;
    public int hp;
    public float baseSpeed = 2.4f;
    public int contactDamage = 15;
    public float contactCooldown = 0.6f;
    private float lastContactTime = 0f;

    [Header("Phases")]
    public int currentPhase = 1; // 1: Cyan (Nova), 2: Amber (Dash + Swarm), 3: Crimson (Rage)

    [Header("Visuals")]
    private Transform coreTransform;
    private Material coreMat;
    private Color currentColor;
    private List<Transform> orbitalCannons = new List<Transform>();
    private Transform ringPivot;

    private GravityBody gravityBody;
    private bool isDead = false;
    private bool isDashing = false;
    private Vector3 dashDirection;
    private float dashTimer = 0f;

    // Timers
    private float novaTimer = 2f;
    private float dashCooldownTimer = 8f;
    private float spawnMinionsTimer = 6f;
    private float antipodalTimer = 0f;
    private bool isIntercepting = false;

    void Awake()
    {
        Instance = this;
        hp = maxHp;

        gravityBody = GetComponent<GravityBody>();
        if (gravityBody == null) gravityBody = gameObject.AddComponent<GravityBody>();
        gravityBody.surfaceOffset = 0.8f;

        BuildVisuals();

        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = false;
        sc.radius = 1.3f;
    }

    void Start()
    {
        SetPhase(1);
        Teleporter.LockAllTeleporters();

        // Wipe surrounding mobs so player and boss duel cleanly
        if (Enemy.activeEnemies != null)
        {
            List<Enemy> mobs = new List<Enemy>(Enemy.activeEnemies);
            foreach (var m in mobs)
            {
                if (m != null && !m.isDead) m.TakeDamage(9999);
            }
        }

        if (RuntimeUIBuilder.Instance != null)
        {
            RuntimeUIBuilder.BuildBossHealthBar(this);
        }
        else
        {
            RuntimeUIBuilder.BuildBossHealthBarDirect(this);
        }
    }

    void BuildVisuals()
    {
        // Central Core
        GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObj.name = "BossCore";
        coreObj.transform.SetParent(transform, false);
        coreObj.transform.localPosition = Vector3.zero;
        coreObj.transform.localScale = Vector3.one * 2.4f;
        Destroy(coreObj.GetComponent<Collider>());

        MeshRenderer mr = coreObj.GetComponent<MeshRenderer>();
        coreMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        coreMat.EnableKeyword("_EMISSION");
        mr.material = coreMat;
        coreTransform = coreObj.transform;

        // Orbiting Cannons Pivot
        GameObject pivotObj = new GameObject("RingPivot");
        pivotObj.transform.SetParent(transform, false);
        pivotObj.transform.localPosition = Vector3.zero;
        ringPivot = pivotObj.transform;

        int cannonCount = 4;
        for (int i = 0; i < cannonCount; i++)
        {
            float angle = i * (360f / cannonCount);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 pos = rot * Vector3.forward * 2.5f;

            GameObject cannon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cannon.name = "Cannon_" + i;
            cannon.transform.SetParent(ringPivot, false);
            cannon.transform.localPosition = pos;
            cannon.transform.localRotation = rot;
            cannon.transform.localScale = new Vector3(0.5f, 0.5f, 1.2f);
            Destroy(cannon.GetComponent<Collider>());

            MeshRenderer cmr = cannon.GetComponent<MeshRenderer>();
            cmr.material = coreMat;
            orbitalCannons.Add(cannon.transform);
        }
    }

    void Update()
    {
        if (isDead) return;

        // Rotate orbital cannons
        float spinSpeed = currentPhase == 1 ? 60f : (currentPhase == 2 ? 140f : 240f);
        if (ringPivot != null)
        {
            ringPivot.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.Self);
        }

        // Pulse emission
        float pulse = Mathf.PingPong(Time.time * (currentPhase == 3 ? 6f : 2f), 1f);
        coreMat.SetColor("_EmissionColor", currentColor * (2f + pulse * 2f));

        if (GameManager.Instance == null || GameManager.Instance.player == null) return;
        Transform player = GameManager.Instance.player;

        Vector3 surfaceNormal = transform.parent != null
            ? (transform.position - transform.parent.position).normalized
            : transform.up;

        Vector3 toPlayer = player.position - transform.position;
        Vector3 planarDir = Vector3.ProjectOnPlane(toPlayer, surfaceNormal);
        float distToPlayer = planarDir.magnitude;
        planarDir = planarDir.normalized;

        // Contact attack check
        if (distToPlayer <= 1.8f && Time.time >= lastContactTime + contactCooldown)
        {
            lastContactTime = Time.time;
            GameManager.Instance.TakeDamage(contactDamage);
        }

        // State & Attack Execution
        if (isDashing)
        {
            ExecuteDash();
        }
        else if (isIntercepting)
        {
            // Warping
        }
        else
        {
            MoveTowardsPlayer(planarDir);
            HandleAttackTimers(planarDir, surfaceNormal);
            CheckAntiKiting(player);
        }
    }

    void CheckAntiKiting(Transform player)
    {
        if (transform.parent == null) return;
        Vector3 toPlayerNorm = (player.position - transform.parent.position).normalized;
        Vector3 toSelfNorm = (transform.position - transform.parent.position).normalized;
        float angularDist = Vector3.Angle(toSelfNorm, toPlayerNorm);

        if (angularDist > 120f)
        {
            antipodalTimer += Time.deltaTime;
            if (antipodalTimer >= 5f)
            {
                antipodalTimer = 0f;
                StartCoroutine(HyperspaceIntercept(player));
            }
        }
        else
        {
            antipodalTimer = Mathf.Max(0f, antipodalTimer - Time.deltaTime);
        }
    }

    IEnumerator HyperspaceIntercept(Transform player)
    {
        isIntercepting = true;
        GameAudio.Play(AudioCue.Teleport);

        Vector3 origScale = coreTransform.localScale;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            coreTransform.localScale = Vector3.Lerp(origScale, Vector3.zero, t / 0.4f);
            yield return null;
        }

        Vector3 playerForward = player.forward;
        Vector3 surfaceNormal = (player.position - transform.parent.position).normalized;
        Vector3 aheadDir = Vector3.ProjectOnPlane(playerForward, surfaceNormal).normalized;
        if (aheadDir.sqrMagnitude < 0.01f) aheadDir = Vector3.Cross(surfaceNormal, Vector3.up).normalized;

        float planetRadius = transform.parent.localScale.x * 0.5f;
        Vector3 newPos = player.position + aheadDir * 8f;
        Vector3 newNormal = (newPos - transform.parent.position).normalized;
        transform.position = transform.parent.position + newNormal * (planetRadius + 0.8f);

        t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            coreTransform.localScale = Vector3.Lerp(Vector3.zero, origScale, t / 0.3f);
            yield return null;
        }
        coreTransform.localScale = origScale;

        FireRadialNova(newNormal);

        isIntercepting = false;
    }

    void MoveTowardsPlayer(Vector3 planarDir)
    {
        float speed = baseSpeed * (currentPhase == 1 ? 1f : (currentPhase == 2 ? 1.35f : 1.7f));
        float planetScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        float localSpeed = speed / planetScale;

        Vector3 localMove = transform.parent != null ? transform.parent.InverseTransformDirection(planarDir) : planarDir;
        transform.localPosition += localMove * localSpeed * Time.deltaTime;
    }

    void HandleAttackTimers(Vector3 planarDir, Vector3 surfaceNormal)
    {
        // 1. Nova Attack
        novaTimer -= Time.deltaTime;
        float novaCooldown = currentPhase == 1 ? 4.2f : (currentPhase == 2 ? 3.2f : 2.0f);
        if (novaTimer <= 0f)
        {
            novaTimer = novaCooldown;
            FireRadialNova(surfaceNormal);
        }

        // 2. Dash Attack (Phase 2 & 3)
        if (currentPhase >= 2)
        {
            dashCooldownTimer -= Time.deltaTime;
            float dashCooldown = currentPhase == 2 ? 8f : 5.5f;
            if (dashCooldownTimer <= 0f)
            {
                dashCooldownTimer = dashCooldown;
                StartCoroutine(TelegraphAndDash(planarDir));
            }
        }

        // 3. Minion Spawner (Phase 2 & 3)
        if (currentPhase >= 2)
        {
            spawnMinionsTimer -= Time.deltaTime;
            if (spawnMinionsTimer <= 0f)
            {
                spawnMinionsTimer = currentPhase == 2 ? 9f : 7f;
                SpawnMinions();
            }
        }
    }

    void FireRadialNova(Vector3 surfaceNormal)
    {
        GameAudio.Play(AudioCue.Shoot);
        int projectileCount = currentPhase == 3 ? 16 : 12;
        float offsetAngle = (currentPhase == 3) ? Random.Range(0f, 30f) : 0f;

        Transform planetTransform = transform.parent;
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = offsetAngle + i * (360f / projectileCount);
            Quaternion rot = Quaternion.AngleAxis(angle, surfaceNormal);
            Vector3 forwardOnSphere = rot * Vector3.Cross(surfaceNormal, Vector3.up);
            if (forwardOnSphere.sqrMagnitude < 0.01f)
            {
                forwardOnSphere = rot * Vector3.Cross(surfaceNormal, Vector3.right);
            }
            forwardOnSphere = forwardOnSphere.normalized;

            GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = "BossNovaBullet";
            proj.transform.position = transform.position + forwardOnSphere * 2.2f;
            proj.transform.localScale = Vector3.one * 0.7f;
            proj.transform.forward = forwardOnSphere;

            BossProjectile bp = proj.AddComponent<BossProjectile>();
            bp.planet = planetTransform;
            bp.speed = currentPhase == 3 ? 22f : 17f;
            bp.damage = 10;
        }
    }

    IEnumerator TelegraphAndDash(Vector3 targetDir)
    {
        // Telegraph phase: Freeze and glow bright white/yellow
        Color oldColor = currentColor;
        coreMat.SetColor("_BaseColor", Color.yellow);
        coreMat.SetColor("_EmissionColor", Color.yellow * 6f);
        GameAudio.Play(AudioCue.Hit);

        yield return new WaitForSeconds(1.0f);

        // Initiate Dash
        coreMat.SetColor("_BaseColor", oldColor);
        isDashing = true;
        dashDirection = targetDir;
        dashTimer = 1.0f;
    }

    void ExecuteDash()
    {
        dashTimer -= Time.deltaTime;
        float dashSpeed = 16f;
        float planetScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        float localSpeed = dashSpeed / planetScale;

        Vector3 localMove = transform.parent != null ? transform.parent.InverseTransformDirection(dashDirection) : dashDirection;
        transform.localPosition += localMove * localSpeed * Time.deltaTime;

        if (dashTimer <= 0f)
        {
            isDashing = false;
        }
    }

    void SpawnMinions()
    {
        if (EnemySpawner.Instance == null || EnemySpawner.Instance.swarmerPrefab == null) return;
        Transform planetTransform = transform.parent;

        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = transform.position + Random.onUnitSphere * 3f;
            GameObject minion = Instantiate(EnemySpawner.Instance.swarmerPrefab, spawnPos, Quaternion.identity);
            minion.transform.SetParent(planetTransform, true);
            GravityBody gb = minion.GetComponent<GravityBody>();
            if (gb != null) gb.planet = EnemySpawner.Instance.currentPlanet;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        hp -= damage;
        GameAudio.PlayAt(AudioCue.Hit, transform.position, transform.parent);

        // Flash white
        StartCoroutine(DamageFlash());

        // Floating Text
        SpawnDamageText(damage);

        // Update Boss UI
        RuntimeUIBuilder.UpdateBossHP(hp, maxHp);

        // Check Phase Transitions
        if (currentPhase == 1 && hp <= maxHp * 0.65f)
        {
            SetPhase(2);
        }
        else if (currentPhase == 2 && hp <= maxHp * 0.25f)
        {
            SetPhase(3);
        }

        if (hp <= 0)
        {
            Die();
        }
    }

    void SetPhase(int phase)
    {
        currentPhase = phase;
        if (currentPhase == 1)
        {
            currentColor = new Color(0f, 0.8f, 1f); // Cyan
        }
        else if (currentPhase == 2)
        {
            currentColor = new Color(1f, 0.55f, 0.1f); // Amber
            GameAudio.Play(AudioCue.Explosion);
        }
        else if (currentPhase == 3)
        {
            currentColor = new Color(1f, 0.1f, 0.2f); // Crimson
            GameAudio.Play(AudioCue.LevelUp);
        }

        if (coreMat != null)
        {
            coreMat.SetColor("_BaseColor", currentColor);
            coreMat.SetColor("_EmissionColor", currentColor * 3f);
        }
    }

    IEnumerator DamageFlash()
    {
        if (coreMat == null) yield break;
        coreMat.SetColor("_BaseColor", Color.white);
        coreMat.SetColor("_EmissionColor", Color.white * 5f);
        yield return new WaitForSeconds(0.06f);
        if (coreMat != null)
        {
            coreMat.SetColor("_BaseColor", currentColor);
            coreMat.SetColor("_EmissionColor", currentColor * 3f);
        }
    }

    void SpawnDamageText(int damage)
    {
        Vector3 surfaceNormal = transform.parent != null
            ? (transform.position - transform.parent.position).normalized : Vector3.up;
        Vector3 textPosition = transform.position + surfaceNormal * 2f;

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

        if (transform.parent != null) txtObj.transform.SetParent(transform.parent, true);
        FloatingText ft = txtObj.GetComponent<FloatingText>();
        if (ft == null) ft = txtObj.AddComponent<FloatingText>();
        ft.Setup(damage.ToString());
    }

    void Die()
    {
        isDead = true;
        GameAudio.Play(AudioCue.Explosion);

        // Hide Boss bar
        RuntimeUIBuilder.HideBossHealthBar();

        // Massive explosion FX
        for (int i = 0; i < 25; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = transform.position + Random.insideUnitSphere * 2f;
            cube.transform.localScale = Vector3.one * Random.Range(0.4f, 1.0f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = coreMat;

            Rigidbody crb = cube.AddComponent<Rigidbody>();
            crb.useGravity = false;
            crb.AddExplosionForce(800f, transform.position, 4f);

            GravityBody gbCube = cube.AddComponent<GravityBody>();
            if (gbCube != null && transform.parent != null)
            {
                gbCube.planet = transform.parent.GetComponent<PlanetGravity>();
            }
            Destroy(cube, 3f);
        }

        // Spawn XP Cluster (25 gems)
        if (EnemySpawner.Instance != null && EnemySpawner.Instance.gemPool != null)
        {
            for (int i = 0; i < 20; i++)
            {
                GameObject gem = EnemySpawner.Instance.gemPool.Get();
                gem.transform.position = transform.position + Random.insideUnitSphere * 2f;
                GravityBody gemGb = gem.GetComponent<GravityBody>();
                if (gemGb != null && transform.parent != null)
                {
                    gemGb.planet = transform.parent.GetComponent<PlanetGravity>();
                }
            }
        }

        // Unlock teleporters and resume game flow
        Teleporter.UnlockAllTeleporters();
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.isBossActive = false;
        }

        // Spawn guaranteed Drop Pod
        if (GameManager.Instance != null)
        {
            // Trigger victory screen
            RuntimeUIBuilder.BuildVictoryUI(GameManager.Instance);
        }

        Destroy(gameObject, 0.1f);
    }
}
