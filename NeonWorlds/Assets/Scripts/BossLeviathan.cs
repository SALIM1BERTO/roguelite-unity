using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossLeviathan : MonoBehaviour
{
    public static BossLeviathan Instance;

    [Header("Stats")]
    public int maxHp = 2400;
    public int hp;
    public float baseSpeed = 2.8f;
    public int contactDamage = 25;
    public float contactCooldown = 0.5f;
    private float lastContactTime = 0f;

    [Header("Phases")]
    public int currentPhase = 1;

    [Header("Visual Components")]
    private Transform coreTransform;
    private Transform eyeTransform;
    private Material coreMat;
    private Material eyeMat;
    private Material armorMat;
    private Color currentColor;

    private Transform innerRingPivot;
    private Transform outerRingPivot;
    private List<Transform> innerCannons = new List<Transform>();
    private List<Transform> outerFins = new List<Transform>();
    private List<Transform> armorPlates = new List<Transform>();

    private GravityBody gravityBody;
    private bool isDead = false;
    private bool isDashing = false;
    private Vector3 dashDirection;
    private float dashTimer = 0f;

    // Timers
    private float novaTimer = 2.5f;
    private float dashCooldownTimer = 7f;
    private float spawnMinionsTimer = 5f;
    private float antipodalTimer = 0f;
    private bool isIntercepting = false;

    void Awake()
    {
        Instance = this;
        hp = maxHp;

        gravityBody = GetComponent<GravityBody>();
        if (gravityBody == null) gravityBody = gameObject.AddComponent<GravityBody>();
        // Massive floating stance above planet surface
        gravityBody.surfaceOffset = 2.0f;

        BuildVisuals();

        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = false;
        sc.radius = 2.8f;
    }

    void Start()
    {
        SetPhase(1);
        Teleporter.LockAllTeleporters();

        // Clear minor mobs and summon elite escort
        WipeExistingMobsAndSummonEscort();

        RuntimeUIBuilder.BuildBossHealthBar(this);
    }

    void BuildVisuals()
    {
        // 1. Materials
        coreMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        coreMat.EnableKeyword("_EMISSION");

        eyeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        eyeMat.EnableKeyword("_EMISSION");
        eyeMat.SetColor("_BaseColor", Color.white);
        eyeMat.SetColor("_EmissionColor", Color.white * 4f);

        armorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        armorMat.SetColor("_BaseColor", new Color(0.08f, 0.09f, 0.13f)); // Dark Dreadnought Steel
        armorMat.SetFloat("_Smoothness", 0.85f);
        armorMat.EnableKeyword("_EMISSION");
        armorMat.SetColor("_EmissionColor", new Color(0.1f, 0.15f, 0.25f));

        // 2. Colossal Central Core (5.2 scale)
        GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObj.name = "BossCore";
        coreObj.transform.SetParent(transform, false);
        coreObj.transform.localPosition = Vector3.zero;
        coreObj.transform.localScale = Vector3.one * 5.2f;
        Destroy(coreObj.GetComponent<Collider>());
        coreObj.GetComponent<MeshRenderer>().material = coreMat;
        coreTransform = coreObj.transform;

        // 3. Central Eye / Focus Lens
        GameObject eyeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeObj.name = "BossEye";
        eyeObj.transform.SetParent(transform, false);
        eyeObj.transform.localPosition = new Vector3(0, 0, 2.4f);
        eyeObj.transform.localScale = Vector3.one * 2.0f;
        Destroy(eyeObj.GetComponent<Collider>());
        eyeObj.GetComponent<MeshRenderer>().material = eyeMat;
        eyeTransform = eyeObj.transform;

        // 4. Hexagonal Exoskeleton Armor Plates
        GameObject chassisObj = new GameObject("ChassisArmor");
        chassisObj.transform.SetParent(transform, false);
        chassisObj.transform.localPosition = Vector3.zero;

        int plateCount = 6;
        for (int i = 0; i < plateCount; i++)
        {
            float angle = i * (360f / plateCount);
            Quaternion rot = Quaternion.Euler(0, angle, 15f);
            Vector3 pos = rot * Vector3.forward * 3.2f;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "ArmorPlate_" + i;
            plate.transform.SetParent(chassisObj.transform, false);
            plate.transform.localPosition = pos;
            plate.transform.localRotation = rot;
            plate.transform.localScale = new Vector3(1.6f, 0.5f, 4.2f);
            Destroy(plate.GetComponent<Collider>());
            plate.GetComponent<MeshRenderer>().material = armorMat;
            armorPlates.Add(plate.transform);
        }

        // 5. Inner Ring (4 Heavy Plasma Cannons)
        GameObject innerPivot = new GameObject("InnerRingPivot");
        innerPivot.transform.SetParent(transform, false);
        innerPivot.transform.localPosition = Vector3.zero;
        innerRingPivot = innerPivot.transform;

        int innerCount = 4;
        for (int i = 0; i < innerCount; i++)
        {
            float angle = i * (360f / innerCount);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 pos = rot * Vector3.forward * 5.5f;

            GameObject cannon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cannon.name = "HeavyCannon_" + i;
            cannon.transform.SetParent(innerRingPivot, false);
            cannon.transform.localPosition = pos;
            cannon.transform.localRotation = rot;
            cannon.transform.localScale = new Vector3(0.9f, 0.9f, 2.4f);
            Destroy(cannon.GetComponent<Collider>());
            cannon.GetComponent<MeshRenderer>().material = coreMat;
            innerCannons.Add(cannon.transform);
        }

        // 6. Outer Ring (6 Gyroscopic Energy Fins)
        GameObject outerPivot = new GameObject("OuterRingPivot");
        outerPivot.transform.SetParent(transform, false);
        outerPivot.transform.localPosition = Vector3.zero;
        outerPivot.transform.localRotation = Quaternion.Euler(25f, 0, 0);
        outerRingPivot = outerPivot.transform;

        int outerCount = 6;
        for (int i = 0; i < outerCount; i++)
        {
            float angle = i * (360f / outerCount);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 pos = rot * Vector3.forward * 7.5f;

            GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fin.name = "EnergyFin_" + i;
            fin.transform.SetParent(outerRingPivot, false);
            fin.transform.localPosition = pos;
            fin.transform.localRotation = rot * Quaternion.Euler(45f, 0, 0);
            fin.transform.localScale = new Vector3(0.5f, 2.2f, 1.2f);
            Destroy(fin.GetComponent<Collider>());
            fin.GetComponent<MeshRenderer>().material = coreMat;
            outerFins.Add(fin.transform);
        }
    }

    void WipeExistingMobsAndSummonEscort()
    {
        // 1. Wipe minor existing mobs
        if (Enemy.activeEnemies != null)
        {
            List<Enemy> mobs = new List<Enemy>(Enemy.activeEnemies);
            foreach (var m in mobs)
            {
                if (m != null && !m.isDead) m.TakeDamage(9999);
            }
        }

        // 2. Summon 2 Elite Harbingers (Arautos do Vácuo) as flanks
        if (EnemySpawner.Instance != null && EnemySpawner.Instance.tankPrefab != null && transform.parent != null)
        {
            Transform planet = transform.parent;
            Vector3 rightFlank = Vector3.Cross(transform.up, transform.forward).normalized;

            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 spawnPos = transform.position + (rightFlank * i * 6f);
                GameObject harbinger = Instantiate(EnemySpawner.Instance.tankPrefab, spawnPos, Quaternion.identity);
                harbinger.name = "ArautoDoVacuo_" + (i > 0 ? "Right" : "Left");
                harbinger.transform.SetParent(planet, true);

                Enemy e = harbinger.GetComponent<Enemy>();
                if (e != null)
                {
                    e.maxHp = 200;
                    e.hp = 200;
                    e.speed = 4f;
                    e.attackDamage = 15;
                }

                GravityBody gb = harbinger.GetComponent<GravityBody>();
                if (gb != null) gb.planet = planet.GetComponent<PlanetGravity>();

                // Tint deep purple
                MeshRenderer mr = harbinger.GetComponentInChildren<MeshRenderer>();
                if (mr != null)
                {
                    mr.material.SetColor("_BaseColor", new Color(0.6f, 0f, 1f));
                    mr.material.SetColor("_EmissionColor", new Color(0.7f, 0.1f, 1f) * 3f);
                }
            }
        }
    }

    void Update()
    {
        if (isDead) return;

        // Dual Gyroscopic Rotation
        float innerSpeed = currentPhase == 1 ? 50f : (currentPhase == 2 ? 110f : 200f);
        float outerSpeed = currentPhase == 1 ? -35f : (currentPhase == 2 ? -80f : -150f);

        if (innerRingPivot != null) innerRingPivot.Rotate(0, innerSpeed * Time.deltaTime, 0, Space.Self);
        if (outerRingPivot != null) outerRingPivot.Rotate(0, outerSpeed * Time.deltaTime, 0, Space.Self);

        // Core Pulse Animation
        float pulseSpeed = currentPhase == 3 ? 8f : (currentPhase == 2 ? 4f : 2f);
        float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
        coreMat.SetColor("_EmissionColor", currentColor * (2.5f + pulse * 3.5f));

        if (GameManager.Instance == null || GameManager.Instance.player == null) return;
        Transform player = GameManager.Instance.player;

        Vector3 surfaceNormal = transform.parent != null
            ? (transform.position - transform.parent.position).normalized
            : transform.up;

        Vector3 toPlayer = player.position - transform.position;
        Vector3 planarDir = Vector3.ProjectOnPlane(toPlayer, surfaceNormal);
        float distToPlayer = planarDir.magnitude;
        planarDir = planarDir.normalized;

        // Aim eye towards player
        if (eyeTransform != null && planarDir.sqrMagnitude > 0.01f)
        {
            eyeTransform.localPosition = planarDir * 2.6f;
        }

        // Contact attack check
        if (distToPlayer <= 3.2f && Time.time >= lastContactTime + contactCooldown)
        {
            lastContactTime = Time.time;
            GameManager.Instance.TakeDamage(contactDamage);
        }

        // State Machine
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

    void MoveTowardsPlayer(Vector3 planarDir)
    {
        float speed = baseSpeed * (currentPhase == 1 ? 1f : (currentPhase == 2 ? 1.4f : 1.85f));
        float planetScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        float localSpeed = speed / planetScale;

        Vector3 localMove = transform.parent != null ? transform.parent.InverseTransformDirection(planarDir) : planarDir;
        transform.localPosition += localMove * localSpeed * Time.deltaTime;
    }

    void HandleAttackTimers(Vector3 planarDir, Vector3 surfaceNormal)
    {
        // 1. Radial Nova Attack
        novaTimer -= Time.deltaTime;
        float novaCooldown = currentPhase == 1 ? 3.6f : (currentPhase == 2 ? 2.6f : 1.6f);
        if (novaTimer <= 0f)
        {
            novaTimer = novaCooldown;
            FireRadialNova(surfaceNormal);
        }

        // 2. Dash Attack
        if (currentPhase >= 2)
        {
            dashCooldownTimer -= Time.deltaTime;
            float dashCooldown = currentPhase == 2 ? 7f : 4.5f;
            if (dashCooldownTimer <= 0f)
            {
                dashCooldownTimer = dashCooldown;
                StartCoroutine(TelegraphAndDash(planarDir));
            }
        }

        // 3. Swarmer Escort Influx
        spawnMinionsTimer -= Time.deltaTime;
        float spawnCooldown = currentPhase == 1 ? 10f : (currentPhase == 2 ? 6.5f : 4.5f);
        if (spawnMinionsTimer <= 0f)
        {
            spawnMinionsTimer = spawnCooldown;
            SpawnMinionWave();
        }
    }

    void FireRadialNova(Vector3 surfaceNormal)
    {
        GameAudio.Play(AudioCue.Shot);
        int projectileCount = currentPhase == 1 ? 18 : (currentPhase == 2 ? 24 : 32);
        float offsetAngle = (currentPhase == 3) ? Random.Range(0f, 45f) : 0f;

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
            proj.transform.position = transform.position + forwardOnSphere * 3.5f;
            proj.transform.localScale = Vector3.one * 1.1f;
            proj.transform.forward = forwardOnSphere;

            BossProjectile bp = proj.AddComponent<BossProjectile>();
            bp.planet = planetTransform;
            bp.speed = currentPhase == 3 ? 26f : (currentPhase == 2 ? 23f : 20f);
            bp.damage = currentPhase == 3 ? 18 : 14;
        }
    }

    IEnumerator TelegraphAndDash(Vector3 targetDir)
    {
        // Warning telegraph
        Color oldColor = currentColor;
        coreMat.SetColor("_BaseColor", Color.yellow);
        coreMat.SetColor("_EmissionColor", Color.yellow * 8f);
        GameAudio.Play(AudioCue.Hit);

        yield return new WaitForSeconds(0.9f);

        coreMat.SetColor("_BaseColor", oldColor);
        isDashing = true;
        dashDirection = targetDir;
        dashTimer = 1.1f;
    }

    void ExecuteDash()
    {
        dashTimer -= Time.deltaTime;
        float dashSpeed = 22f;
        float planetScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        float localSpeed = dashSpeed / planetScale;

        Vector3 localMove = transform.parent != null ? transform.parent.InverseTransformDirection(dashDirection) : dashDirection;
        transform.localPosition += localMove * localSpeed * Time.deltaTime;

        if (dashTimer <= 0f)
        {
            isDashing = false;
        }
    }

    void SpawnMinionWave()
    {
        if (EnemySpawner.Instance == null || EnemySpawner.Instance.swarmerPrefab == null || transform.parent == null) return;
        Transform planetTransform = transform.parent;

        int count = currentPhase == 1 ? 2 : (currentPhase == 2 ? 4 : 6);
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = transform.position + Random.onUnitSphere * 4.5f;
            GameObject minion = Instantiate(EnemySpawner.Instance.swarmerPrefab, spawnPos, Quaternion.identity);
            minion.transform.SetParent(planetTransform, true);
            GravityBody gb = minion.GetComponent<GravityBody>();
            if (gb != null) gb.planet = EnemySpawner.Instance.currentPlanet;
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
            if (antipodalTimer >= 4.0f)
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
        Vector3 newPos = player.position + aheadDir * 10f;
        Vector3 newNormal = (newPos - transform.parent.position).normalized;
        transform.position = transform.parent.position + newNormal * (planetRadius + 2.0f);

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

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        hp -= damage;
        GameAudio.PlayAt(AudioCue.Hit, transform.position, gravityBody != null ? gravityBody.planet : null);

        StartCoroutine(DamageFlash());
        SpawnDamageText(damage);

        RuntimeUIBuilder.UpdateBossHP(hp, maxHp);

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
            currentColor = new Color(0f, 0.9f, 1f); // Electric Cyan
        }
        else if (currentPhase == 2)
        {
            currentColor = new Color(1f, 0.55f, 0.05f); // Solar Flare Amber
            GameAudio.Play(AudioCue.Explosion);
        }
        else if (currentPhase == 3)
        {
            currentColor = new Color(1f, 0.05f, 0.2f); // Hyper-Nova Crimson
            GameAudio.Play(AudioCue.LevelUp);
        }

        if (coreMat != null)
        {
            coreMat.SetColor("_BaseColor", currentColor);
            coreMat.SetColor("_EmissionColor", currentColor * 3.5f);
        }
    }

    IEnumerator DamageFlash()
    {
        if (coreMat == null) yield break;
        coreMat.SetColor("_BaseColor", Color.white);
        coreMat.SetColor("_EmissionColor", Color.white * 6f);
        yield return new WaitForSeconds(0.05f);
        if (coreMat != null)
        {
            coreMat.SetColor("_BaseColor", currentColor);
            coreMat.SetColor("_EmissionColor", currentColor * 3.5f);
        }
    }

    void SpawnDamageText(int damage)
    {
        Vector3 surfaceNormal = transform.parent != null
            ? (transform.position - transform.parent.position).normalized : Vector3.up;
        Vector3 textPosition = transform.position + surfaceNormal * 3f;

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

        RuntimeUIBuilder.HideBossHealthBar();

        // Unlock teleporters and resume game flow
        Teleporter.UnlockAllTeleporters();
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.isBossActive = false;
        }

        // Cataclysmic explosion FX (40 debris cubes)
        for (int i = 0; i < 40; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = transform.position + Random.insideUnitSphere * 3.5f;
            cube.transform.localScale = Vector3.one * Random.Range(0.6f, 1.6f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = coreMat;

            Rigidbody crb = cube.AddComponent<Rigidbody>();
            crb.useGravity = false;
            crb.AddExplosionForce(1200f, transform.position, 6f);

            GravityBody gbCube = cube.AddComponent<GravityBody>();
            if (gbCube != null && transform.parent != null)
            {
                gbCube.planet = transform.parent.GetComponent<PlanetGravity>();
            }
            Destroy(cube, 4f);
        }

        // Massive XP cluster (35 gems)
        if (EnemySpawner.Instance != null && EnemySpawner.Instance.gemPool != null)
        {
            for (int i = 0; i < 35; i++)
            {
                GameObject gem = EnemySpawner.Instance.gemPool.Get();
                gem.transform.position = transform.position + Random.insideUnitSphere * 3f;
                GravityBody gemGb = gem.GetComponent<GravityBody>();
                if (gemGb != null && transform.parent != null)
                {
                    gemGb.planet = transform.parent.GetComponent<PlanetGravity>();
                }
            }
        }

        // Victory screen
        if (GameManager.Instance != null)
        {
            RuntimeUIBuilder.BuildVictoryUI(GameManager.Instance);
        }

        Destroy(gameObject, 0.1f);
    }
}
