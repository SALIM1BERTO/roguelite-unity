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
        standardPool = CreatePool(standardPrefab, 50, 200);
        swarmerPool = CreatePool(swarmerPrefab, 100, 300);
        tankPool = CreatePool(tankPrefab, 10, 50);

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
            defaultCapacity: 100,
            maxSize: 500
        );
    }

    ObjectPool<GameObject> CreatePool(GameObject prefab, int defaultCap, int max)
    {
        var pool = new ObjectPool<GameObject>(
            createFunc: () => {
                GameObject obj = Instantiate(prefab);
                Enemy e = obj.GetComponent<Enemy>();
                e.pool = null; // Will set below since we cant capture self easily
                e.gemPrefab = xpGemPrefab;
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

        // Boss trigger: 2:30 or Key 'B'
        if (!bossSpawned)
        {
            bool pressB = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame;
            if (gameTimer >= 150f || pressB)
            {
                bool isBlocked = (GameManager.Instance != null && GameManager.Instance.IsLevelUpActive()) || Time.timeScale == 0f || BossIntroSequence.isIntroPlaying;
                if (!isBlocked)
                {
                    SpawnBoss();
                }
            }
        }
        
        // Increase difficulty over time: spawn interval decreases by 5% every 10 seconds (cap at 0.2f)
        float currentInterval = Mathf.Max(0.2f, spawnInterval * Mathf.Pow(0.95f, gameTimer / 10f));
        
        // Pause regular horde while Boss is active
        if (!isBossActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                SpawnEnemy();
                timer = currentInterval;
            }
        }
    }

    public void SpawnBoss()
    {
        if (bossSpawned || currentPlanet == null || GameManager.Instance == null || GameManager.Instance.player == null) return;
        if (GameManager.Instance.IsLevelUpActive() || Time.timeScale == 0f || BossIntroSequence.isIntroPlaying) return;
        bossSpawned = true;
        isBossActive = true;

        Vector3 playerPos = GameManager.Instance.player.position;
        Vector3 playerDir = (playerPos - currentPlanet.transform.position).normalized;
        Vector3 spawnDir = -playerDir;

        float radius = currentPlanet.transform.localScale.x * 0.5f;
        Vector3 spawnPos = currentPlanet.transform.position + spawnDir * (radius + 3.8f);

        // Cinematic screen glitch, rumble, flash bang, and planetary shockwave
        BossIntroSequence.StartSequence(spawnPos, currentPlanet, () => {
            GameObject bossObj = new GameObject("BossLeviathan");
            bossObj.transform.position = spawnPos;
            bossObj.transform.SetParent(currentPlanet.transform, true);

            BossLeviathan boss = bossObj.AddComponent<BossLeviathan>();
            GravityBody gb = bossObj.GetComponent<GravityBody>();
            if (gb != null) gb.planet = currentPlanet;
        });
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
        
        // Determine which enemy to spawn based on gameTimer
        // Minute 0-1: 100% Standard
        // Minute 1-2: 60% Standard, 40% Swarmer
        // Minute 2-3: 40% Standard, 50% Swarmer, 10% Tank
        // Minute 3+: 30% Standard, 50% Swarmer, 20% Tank
        
        float r = Random.value;
        GameObject enemy = null;
        ObjectPool<GameObject> selectedPool = null;

        if (gameTimer < 60f)
        {
            selectedPool = standardPool;
        }
        else if (gameTimer < 120f)
        {
            selectedPool = r < 0.6f ? standardPool : swarmerPool;
        }
        else if (gameTimer < 180f)
        {
            if (r < 0.4f) selectedPool = standardPool;
            else if (r < 0.9f) selectedPool = swarmerPool;
            else selectedPool = tankPool;
        }
        else
        {
            if (r < 0.3f) selectedPool = standardPool;
            else if (r < 0.8f) selectedPool = swarmerPool;
            else selectedPool = tankPool;
        }

        if (selectedPool != null && standardPrefab != null)
        {
            enemy = selectedPool.Get();
            Enemy e = enemy.GetComponent<Enemy>();
            e.pool = selectedPool;
            
            // Boost enemy health and speed based on time
            float timeMultiplier = 1f + (gameTimer / 120f); // +50% stats every 2 mins
            if (e.baseHp == -1) e.baseHp = e.maxHp; e.maxHp = (int)(e.baseHp * timeMultiplier);
            e.hp = e.maxHp;
            
            enemy.GetComponent<GravityBody>().planet = currentPlanet; enemy.transform.SetParent(currentPlanet.transform, true);
            enemy.transform.position = spawnPos;
        }
    }
}
