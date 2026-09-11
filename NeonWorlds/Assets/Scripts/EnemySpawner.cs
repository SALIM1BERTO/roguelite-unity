using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;
    
    public GameObject xpGemPrefab;
    
    public GameObject standardPrefab;
    public GameObject swarmerPrefab;
    public GameObject tankPrefab;
    
    public PlanetGravity currentPlanet;
    
    public float spawnInterval = 1.5f;
    private float timer;

    private ObjectPool<GameObject> standardPool;
    private ObjectPool<GameObject> swarmerPool;
    private ObjectPool<GameObject> tankPool;
    private ObjectPool<GameObject> sniperPool;
    private ObjectPool<GameObject> volatilePool;
    private ObjectPool<GameObject> shielderPool;
    private ObjectPool<GameObject> burrowerPool;
    public ObjectPool<GameObject> gemPool;

    private float gameTimer = 0f;
    public bool bossSpawned = false;
    public bool isBossActive = false;

    void Awake()
    {
        if (xpGemPrefab == null) xpGemPrefab = Resources.Load<GameObject>("GemPrefab");
        if (standardPrefab == null) standardPrefab = Resources.Load<GameObject>("EnemyPrefab");
        if (swarmerPrefab == null) swarmerPrefab = Resources.Load<GameObject>("SwarmerPrefab");
        if (tankPrefab == null) tankPrefab = Resources.Load<GameObject>("TankPrefab");
        Instance = this;
    }

    void Start() { InitPools(); } void InitPools()
    {
        standardPool = CreatePool(standardPrefab, 100, 450);
        swarmerPool = CreatePool(swarmerPrefab, 150, 600);
        tankPool = CreatePool(tankPrefab, 20, 100);
        sniperPool = CreatePool(standardPrefab, 24, 140, typeof(EnemySniper), "SniperSpitter");
        volatilePool = CreatePool(swarmerPrefab, 48, 220, typeof(EnemyVolatileSpore), "VolatileSpore");
        shielderPool = CreatePool(standardPrefab, 12, 48, typeof(EnemyShielder), "NaniteShielder");
        burrowerPool = CreatePool(standardPrefab, 20, 80, typeof(EnemyVortexBurrower), "VortexBurrower");

        gemPool = new ObjectPool<GameObject>(
            createFunc: () => {
                GameObject obj = Instantiate(xpGemPrefab);
                XpGem g = obj.GetComponent<XpGem>();
                g.pool = gemPool;
                return obj;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: 150,
            maxSize: 1000
        );
    }

    ObjectPool<GameObject> CreatePool(GameObject prefab, int defaultCap, int max, System.Type behaviorType = null, string runtimeName = null)
    {
        var pool = new ObjectPool<GameObject>(
            createFunc: () => {
                GameObject obj = Instantiate(prefab);
                if (!string.IsNullOrEmpty(runtimeName)) obj.name = runtimeName;
                Enemy e = obj.GetComponent<Enemy>();
                e.pool = null; // Will set below since we cant capture self easily
                e.gemPrefab = xpGemPrefab;
                if (behaviorType != null && obj.GetComponent(behaviorType) == null) obj.AddComponent(behaviorType);
                return obj;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: defaultCap,
            maxSize: max
        );
        return pool;
    }

    void Update()
    {
        if (standardPool == null) InitPools(); if (currentPlanet == null) return;
        
        gameTimer += Time.deltaTime;

        // Boss trigger: 15:00 or Key 'B'
        if (!bossSpawned)
        {
            bool pressB = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame;
            if (gameTimer >= 900f || pressB)
            {
                bool isBlocked = (GameManager.Instance != null && GameManager.Instance.IsLevelUpActive()) || Time.timeScale == 0f || BossIntroSequence.isIntroPlaying;
                if (!isBlocked)
                {
                    SpawnBoss();
                }
            }
        }
        
        float routePressure=ExpeditionRouteSystem.Pressure(ExpeditionRouteSystem.Selected)*RouteEventDirector.PressureMultiplier;
        float currentInterval = IntervalFor(gameTimer, spawnInterval)/routePressure;
        int aliveCap = Mathf.RoundToInt(AliveCapFor(gameTimer)*Mathf.Lerp(1f,routePressure,.65f));
        
        // Dense batches create readable pressure waves while preserving an explicit performance ceiling.
        if (!isBossActive && Enemy.activeEnemies.Count < aliveCap)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                int spawnBatch = Mathf.Max(1,Mathf.RoundToInt(BatchFor(gameTimer, Random.value)*routePressure));

                for (int s = 0; s < spawnBatch; s++)
                {
                    if (Enemy.activeEnemies.Count >= aliveCap) break;
                    SpawnEnemy();
                }
                timer = currentInterval;
            }
        }
    }

    public enum EnemyKind { Standard, Swarmer, Tank, Sniper, Volatile, Shielder, Burrower }

    public static float IntervalFor(float seconds, float configuredInterval)
    {
        float interval = Mathf.Max(.16f, Mathf.Max(.35f, configuredInterval) * .55f * Mathf.Pow(.965f, Mathf.Max(0f, seconds) / 10f));
        return IsRecoveryWindow(seconds) ? interval * 1.65f : interval;
    }

    public static int AliveCapFor(float seconds)
    {
        return 240 + Mathf.Min(260, Mathf.FloorToInt(Mathf.Max(0f, seconds) / 60f) * 24);
    }

    public static int BatchFor(float seconds, float surgeRoll)
    {
        int batch = seconds >= 660f ? 6 : seconds >= 420f ? 5 : seconds >= 240f ? 4 : seconds >= 90f ? 3 : 2;
        if (IsRecoveryWindow(seconds)) return Mathf.Max(1, batch - 2);
        return batch + (seconds >= 150f && surgeRoll < .28f ? 1 : 0);
    }

    public static bool IsRecoveryWindow(float seconds) { return seconds >= 60f && Mathf.Repeat(seconds, 24f) >= 19f; }

    public static int SniperLimitFor(float seconds) { return 5 + Mathf.FloorToInt(Mathf.Max(0f, seconds) / 60f) * 2; }
    public static int VolatileLimitFor(float seconds) { return 12 + Mathf.FloorToInt(Mathf.Max(0f, seconds) / 60f) * 4; }
    public static int ShielderLimitFor(float seconds) { return Mathf.Min(7, 2 + Mathf.FloorToInt(Mathf.Max(0f, seconds) / 180f)); }
    public static int BurrowerLimitFor(float seconds) { return seconds < 240f ? 0 : Mathf.Min(8, 2 + Mathf.FloorToInt((seconds - 240f) / 180f)); }

    public static EnemyKind SelectKind(float seconds, float roll)
    {
        roll = Mathf.Clamp01(roll);
        if (seconds < 45f) return roll < .88f ? EnemyKind.Standard : EnemyKind.Swarmer;
        if (seconds < 90f) return roll < .70f ? EnemyKind.Standard : roll < .95f ? EnemyKind.Swarmer : EnemyKind.Volatile;
        if (seconds < 180f) return roll < .45f ? EnemyKind.Standard : roll < .75f ? EnemyKind.Swarmer : roll < .85f ? EnemyKind.Volatile : roll < .95f ? EnemyKind.Sniper : EnemyKind.Tank;
        if (seconds < 240f) return roll < .23f ? EnemyKind.Standard : roll < .55f ? EnemyKind.Swarmer : roll < .70f ? EnemyKind.Volatile : roll < .82f ? EnemyKind.Sniper : roll < .90f ? EnemyKind.Shielder : EnemyKind.Tank;
        if (seconds < 300f) return roll < .22f ? EnemyKind.Standard : roll < .52f ? EnemyKind.Swarmer : roll < .67f ? EnemyKind.Volatile : roll < .79f ? EnemyKind.Sniper : roll < .84f ? EnemyKind.Burrower : roll < .91f ? EnemyKind.Shielder : EnemyKind.Tank;
        return roll < .17f ? EnemyKind.Standard : roll < .42f ? EnemyKind.Swarmer : roll < .62f ? EnemyKind.Volatile : roll < .74f ? EnemyKind.Sniper : roll < .81f ? EnemyKind.Burrower : roll < .88f ? EnemyKind.Shielder : EnemyKind.Tank;
    }

    public void SpawnBoss()
    {
        if (bossSpawned || currentPlanet == null || GameManager.Instance == null || GameManager.Instance.player == null) return;
        if (GameManager.Instance.IsLevelUpActive() || Time.timeScale == 0f || BossIntroSequence.isIntroPlaying) return;
        bossSpawned = true;
        isBossActive = true;

        PlanetGravity arena=currentPlanet;
        Transform player=GameManager.Instance.player;
        Vector3 position=BossWorldMotion.SurfacePoint(arena.transform,player.position,player.forward,7f,.8f);
        BossIntroSequence.StartSequence(position,arena,() => {
            if(arena==null) return;
            GameObject bossObject=new GameObject("BossLeviathan");
            bossObject.transform.SetParent(arena.transform,false);
            bossObject.transform.position=position;
            bossObject.AddComponent<BossLeviathan>();
        });
    }

    public Enemy SpawnBossMinion(PlanetGravity arena,Vector3 position)
    {
        if(arena==null || swarmerPrefab==null) return null;
        if(swarmerPool==null) InitPools();
        GameObject obj=swarmerPool.Get();
        Enemy enemy=obj.GetComponent<Enemy>(); enemy.pool=swarmerPool; enemy.xpRewardMultiplier=.5f;
        enemy.maxHp=45; enemy.hp=45; enemy.speed=3.2f; enemy.attackDamage=8;
        obj.transform.SetParent(arena.transform, true); obj.transform.position = position;
        GravityBody body = obj.GetComponent<GravityBody>();
        if (body != null) { body.planet = arena; body.SnapToSurface(); }
        enemy.ResetScale();
        return enemy;
    }

    void SpawnEnemy()
    {
        if (GameManager.Instance == null || GameManager.Instance.player == null) return;
        
        Vector3 playerPos = GameManager.Instance.player.position;
        Vector3 randomDir = Random.onUnitSphere;
        Vector3 localPlayerDir = (playerPos - currentPlanet.transform.position).normalized;

        if (Vector3.Dot(randomDir, localPlayerDir) > 0.8f) {
            randomDir = -randomDir;
        }

        float radius = currentPlanet.transform.localScale.x / 2f;
        Vector3 spawnPos = currentPlanet.transform.position + randomDir * radius;
        
        EnemyKind kind = SelectKind(gameTimer, Random.value);
        if (kind == EnemyKind.Sniper && EnemySniper.ActiveCount >= SniperLimitFor(gameTimer)) kind = EnemyKind.Standard;
        if (kind == EnemyKind.Volatile && EnemyVolatileSpore.ActiveCount >= VolatileLimitFor(gameTimer)) kind = EnemyKind.Swarmer;
        if (kind == EnemyKind.Shielder && EnemyShielder.ActiveCount >= ShielderLimitFor(gameTimer)) kind = EnemyKind.Standard;
        if (kind == EnemyKind.Burrower && EnemyVortexBurrower.ActiveCount >= BurrowerLimitFor(gameTimer)) kind = EnemyKind.Standard;
        GameObject enemy = null;
        ObjectPool<GameObject> selectedPool = null;
        switch (kind)
        {
            case EnemyKind.Swarmer: selectedPool = swarmerPool; break;
            case EnemyKind.Tank: selectedPool = tankPool; break;
            case EnemyKind.Sniper: selectedPool = sniperPool; break;
            case EnemyKind.Volatile: selectedPool = volatilePool; break;
            case EnemyKind.Shielder: selectedPool = shielderPool; break;
            case EnemyKind.Burrower: selectedPool = burrowerPool; break;
            default: selectedPool = standardPool; break;
        }

        if (selectedPool != null && standardPrefab != null)
        {
            enemy = selectedPool.Get();
            Enemy e = enemy.GetComponent<Enemy>();
            e.pool = selectedPool;
            e.xpRewardMultiplier = selectedPool == tankPool ? 3.2f : selectedPool == swarmerPool ? .65f :
                selectedPool == sniperPool ? 1.55f : selectedPool == volatilePool ? 1.15f : 1f;
            if (selectedPool == shielderPool) e.xpRewardMultiplier = 1.8f;
            if (selectedPool == burrowerPool) e.xpRewardMultiplier = 1.65f;
            
        // Boost enemy health and speed based on time
            // Scaling: +15% HP per minute, +2% speed per minute, +1 Damage every 2 minutes
            float minutesPassed = gameTimer / 60f;
            float hpMultiplier = 1f + (0.15f * minutesPassed);
            float speedMultiplier = 1f + Mathf.Min(0.3f, 0.02f * minutesPassed);
            
            if (e.baseHp == -1) e.baseHp = e.maxHp; 
            e.maxHp = (int)(e.baseHp * hpMultiplier);
            e.hp = e.maxHp;
            
            if (e.baseSpeed < 0) e.baseSpeed = e.speed;
            e.speed = e.baseSpeed * speedMultiplier;
            
            if (e.baseAttackDamage == -1) e.baseAttackDamage = e.attackDamage;
            e.attackDamage = e.baseAttackDamage + Mathf.FloorToInt(minutesPassed / 2f);

            GravityBody gravity = enemy.GetComponent<GravityBody>();
            gravity.planet = currentPlanet;
            enemy.transform.SetParent(currentPlanet.transform, true);
            enemy.transform.position = spawnPos;
            gravity.SnapToSurface();
            e.ResetScale();
            EnemySpecialBehavior special = enemy.GetComponent<EnemySpecialBehavior>();
            if (special != null) special.ConfigureDifficulty(minutesPassed);
            RunTelemetry.RecordSpawn(kind);
        }
    }
}
