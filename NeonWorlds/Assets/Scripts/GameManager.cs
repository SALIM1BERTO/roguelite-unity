using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject floatingTextPrefab;
    public Transform player;
    public int hp = 100;
    public int maxHp = 100;

    public Text timeText;
    public Text hpText;
    public float matchTime = 0f;

    public bool isInvincible = false;
    public bool isGameOver = false;
    private float dropTimer = 30f;

    public int xp = 0;
    public int level = 1;
    public float magnetRadius = 5f;

    [Header("Meta-Progressão & Upgrades")]
    public int availableRerolls = 0;
    public float lifeStealChance = 0f;
    private Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();

    [Header("Level Up UI")]
    public GameObject levelUpPanel;
    public Button[] upgradeButtons;
    public Text[] upgradeTitles;
    public Text[] upgradeDescs;

    public enum UpgradeRarity { Common, Rare, Epic, Legendary }

    public enum UpgradeType
    {
        Spread,
        FireRate,
        Damage,
        Speed,
        Magnet,
        Heal,
        Pierce,
        Bounce,
        Critical,
        Explosive,
        OrbitalMines,
        SentinelDrone,
        AegisShield,
        LifeSteal,
        SupernovaGatling,
        NebulaFlak,
        AntimatterLance,
        VoidVortex,
        TeslaChain,
        HyperionBarrier
    }

    private UpgradeType[] currentUpgrades = new UpgradeType[3];
    public int xpToNextLevel = 100;
    public RectTransform xpFill;
    public Text levelText;
    private int pendingLevelUps = 0;

    public bool IsLevelUpActive()
    {
        return (levelUpPanel != null && levelUpPanel.activeSelf);
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        GameAudio.EnsureExists();
        GameMusic.EnsureExists();
    }

    void Start()
    {
        CreateHUD();
        SpawnPlayerCorrectly();
        UpdateUI();

        if (player != null)
        {
            MetaProgression.ApplyPermanentBuffs(this, player.GetComponent<PlayerMovement>(), player.GetComponent<Weapon>());
        }

        GameObject startingPlanet = GameObject.Find("Planet_1");
        if (startingPlanet != null)
        {
            PlanetaryBiome.OnPlayerArrived(startingPlanet.GetComponent<PlanetGravity>());
        }

        StartCarePackageScheduler();

        // Remove static teleporters from scene, we use dynamic overload teleporters now
        Teleporter[] staticTps = FindObjectsByType<Teleporter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tp in staticTps)
        {
            Destroy(tp.gameObject);
        }
    }

    public int GetUpgradeLevel(UpgradeType type)
    {
        if (upgradeLevels.ContainsKey(type)) return upgradeLevels[type];
        return 0;
    }

    public void SetUpgradeLevel(UpgradeType type, int level)
    {
        upgradeLevels[type] = level;
    }

    public bool IsLegendaryEvolution(UpgradeType type)
    {
        return type == UpgradeType.SupernovaGatling ||
               type == UpgradeType.NebulaFlak ||
               type == UpgradeType.AntimatterLance ||
               type == UpgradeType.VoidVortex ||
               type == UpgradeType.TeslaChain ||
               type == UpgradeType.HyperionBarrier;
    }

    public int GetMaxLevel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.SupernovaGatling:
            case UpgradeType.NebulaFlak:
            case UpgradeType.AntimatterLance:
            case UpgradeType.VoidVortex:
            case UpgradeType.TeslaChain:
            case UpgradeType.HyperionBarrier:
                return 1;
            case UpgradeType.Explosive: return 1;
            case UpgradeType.Pierce: return 3;
            case UpgradeType.Bounce: return 3;
            case UpgradeType.Critical: return 3;
            case UpgradeType.OrbitalMines: return 3;
            case UpgradeType.SentinelDrone: return 3;
            case UpgradeType.AegisShield: return 3;
            case UpgradeType.LifeSteal: return 3;
            case UpgradeType.Spread: return 4;
            case UpgradeType.Speed: return 4;
            case UpgradeType.Magnet: return 4;
            case UpgradeType.FireRate: return 5;
            case UpgradeType.Damage: return 5;
            case UpgradeType.Heal: return 99;
            default: return 5;
        }
    }

    public UpgradeRarity GetRarity(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.SupernovaGatling:
            case UpgradeType.NebulaFlak:
            case UpgradeType.AntimatterLance:
            case UpgradeType.VoidVortex:
            case UpgradeType.TeslaChain:
            case UpgradeType.HyperionBarrier:
                return UpgradeRarity.Legendary;
            case UpgradeType.Explosive:
            case UpgradeType.OrbitalMines:
            case UpgradeType.SentinelDrone:
            case UpgradeType.AegisShield:
            case UpgradeType.LifeSteal:
                return UpgradeRarity.Epic;
            case UpgradeType.Pierce:
            case UpgradeType.Bounce:
            case UpgradeType.Critical:
            case UpgradeType.Spread:
                return UpgradeRarity.Rare;
            default:
                return UpgradeRarity.Common;
        }
    }

    public void ShowLevelUpScreen()
    {
        if (levelUpPanel == null || upgradeButtons == null || upgradeButtons.Length < 3 || upgradeButtons[0] == null)
        {
            RuntimeUIBuilder.BuildLevelUpUI(this);
        }
        Time.timeScale = 0f;
        GameAudio.SetGameplayPaused(true);
        GameAudio.Play(AudioCue.LevelUp);
        levelUpPanel.SetActive(true);

        // Check synergies for Legendary Evolutions
        List<UpgradeType> legendaryPool = new List<UpgradeType>();
        bool canSupernova = GetUpgradeLevel(UpgradeType.FireRate) >= 5 && GetUpgradeLevel(UpgradeType.Critical) >= 1 && GetUpgradeLevel(UpgradeType.SupernovaGatling) < 1;
        bool canNebula = GetUpgradeLevel(UpgradeType.Spread) >= 4 && GetUpgradeLevel(UpgradeType.Bounce) >= 1 && GetUpgradeLevel(UpgradeType.NebulaFlak) < 1;
        bool canAntimatter = GetUpgradeLevel(UpgradeType.Pierce) >= 3 && GetUpgradeLevel(UpgradeType.Damage) >= 3 && GetUpgradeLevel(UpgradeType.AntimatterLance) < 1;
        bool canVoidVortex = GetUpgradeLevel(UpgradeType.OrbitalMines) >= 3 && GetUpgradeLevel(UpgradeType.Explosive) >= 1 && GetUpgradeLevel(UpgradeType.VoidVortex) < 1;
        bool canTesla = GetUpgradeLevel(UpgradeType.SentinelDrone) >= 3 && (GetUpgradeLevel(UpgradeType.Pierce) >= 1 || GetUpgradeLevel(UpgradeType.Bounce) >= 1) && GetUpgradeLevel(UpgradeType.TeslaChain) < 1;
        bool canHyperion = GetUpgradeLevel(UpgradeType.AegisShield) >= 3 && GetUpgradeLevel(UpgradeType.LifeSteal) >= 1 && GetUpgradeLevel(UpgradeType.HyperionBarrier) < 1;

        if (canSupernova) legendaryPool.Add(UpgradeType.SupernovaGatling);
        if (canNebula) legendaryPool.Add(UpgradeType.NebulaFlak);
        if (canAntimatter) legendaryPool.Add(UpgradeType.AntimatterLance);
        if (canVoidVortex) legendaryPool.Add(UpgradeType.VoidVortex);
        if (canTesla) legendaryPool.Add(UpgradeType.TeslaChain);
        if (canHyperion) legendaryPool.Add(UpgradeType.HyperionBarrier);

        // Build list of valid normal upgrades not at max level
        List<UpgradeType> pool = new List<UpgradeType>();
        foreach (UpgradeType t in System.Enum.GetValues(typeof(UpgradeType)))
        {
            if (IsLegendaryEvolution(t)) continue;
            if (GetUpgradeLevel(t) < GetMaxLevel(t))
            {
                pool.Add(t);
            }
        }

        // Shuffle pool
        for (int i = 0; i < pool.Count; i++)
        {
            int r = Random.Range(i, pool.Count);
            var temp = pool[i];
            pool[i] = pool[r];
            pool[r] = temp;
        }

        // Prioritize available legendary evolutions at the front of the pool
        for (int l = 0; l < legendaryPool.Count; l++)
        {
            pool.Insert(l, legendaryPool[l]);
        }

        for (int i = 0; i < 3; i++)
        {
            UpgradeType type = (i < pool.Count) ? pool[i] : UpgradeType.Heal;
            currentUpgrades[i] = type;
            int currentLvl = GetUpgradeLevel(type);
            int maxLvl = GetMaxLevel(type);
            UpgradeRarity rarity = GetRarity(type);

            RuntimeUIBuilder.StyleUpgradeCard(upgradeButtons[i], rarity);


            string levelBadge = maxLvl < 90 ? $" [NV {currentLvl + 1}/{maxLvl}]" : "";

            switch (type)
            {
                case UpgradeType.Spread:
                    upgradeTitles[i].text = "Tiro Múltiplo" + levelBadge;
                    upgradeDescs[i].text = "Adiciona +1 projétil aos seus disparos.";
                    break;
                case UpgradeType.FireRate:
                    upgradeTitles[i].text = "Tiro Rápido" + levelBadge;
                    upgradeDescs[i].text = "Aumenta a cadência de disparo em 25%.";
                    break;
                case UpgradeType.Damage:
                    upgradeTitles[i].text = "Sobrecarga de Energia" + levelBadge;
                    upgradeDescs[i].text = "Aumenta todo o dano da nave em +20%.";
                    break;
                case UpgradeType.Speed:
                    upgradeTitles[i].text = "Propulsores" + levelBadge;
                    upgradeDescs[i].text = "Aumenta a velocidade de movimento da nave.";
                    break;
                case UpgradeType.Magnet:
                    upgradeTitles[i].text = "Magnetismo" + levelBadge;
                    upgradeDescs[i].text = "Aumenta o raio de coleta de gemas de XP.";
                    break;
                case UpgradeType.Heal:
                    upgradeTitles[i].text = "Reparo de Emergência";
                    upgradeDescs[i].text = "Cura 60 HP e concede +20 de vida máxima.";
                    break;
                case UpgradeType.Pierce:
                    upgradeTitles[i].text = "Projétil Perfurante" + levelBadge;
                    upgradeDescs[i].text = "Seus tiros atravessam +1 inimigo antes de sumir.";
                    break;
                case UpgradeType.Bounce:
                    upgradeTitles[i].text = "Ricochete Cósmico" + levelBadge;
                    upgradeDescs[i].text = "Tiros quicam para o próximo inimigo mais próximo!";
                    break;
                case UpgradeType.Critical:
                    upgradeTitles[i].text = "Sobrecarga Crítica" + levelBadge;
                    upgradeDescs[i].text = "+15% de chance de causar 2.5x dano crítico!";
                    break;
                case UpgradeType.Explosive:
                    upgradeTitles[i].text = "Munição Explosiva" + levelBadge;
                    upgradeDescs[i].text = "Acertos geram uma explosão em área devastadora!";
                    break;
                case UpgradeType.OrbitalMines:
                    upgradeTitles[i].text = "Minas de Matéria Escura" + levelBadge;
                    upgradeDescs[i].text = "Solta minas gravitacionais na órbita que detonam em aproximação.";
                    break;
                case UpgradeType.SentinelDrone:
                    upgradeTitles[i].text = "Drone Sentinela" + levelBadge;
                    upgradeDescs[i].text = "Satélite orbital que dispara feixes laser automáticos.";
                    break;
                case UpgradeType.AegisShield:
                    upgradeTitles[i].text = "Escudo Aegis" + levelBadge;
                    upgradeDescs[i].text = "Barreira protetora que anula 1 impacto e regenera com o tempo.";
                    break;
                case UpgradeType.LifeSteal:
                    upgradeTitles[i].text = "Nanites Vampíricos" + levelBadge;
                    upgradeDescs[i].text = "+6% de chance de restaurar vida ao derrotar inimigos.";
                    break;
                case UpgradeType.SupernovaGatling:
                    upgradeTitles[i].text = "Supernova Gatling\n<color=#ffd700>[EVOLUÇÃO LENDÁRIA ★]</color>";
                    upgradeDescs[i].text = "Blaster dispara rajadas douradas supersônicas com micro-explosões solares em área!";
                    break;
                case UpgradeType.NebulaFlak:
                    upgradeTitles[i].text = "Canhão Nebular\n<color=#ffd700>[EVOLUÇÃO LENDÁRIA ★]</color>";
                    upgradeDescs[i].text = "Shotgun cósmica dispara 8 fragmentos que se dividem em estilhaços ao impactar!";
                    break;
                case UpgradeType.AntimatterLance:
                    upgradeTitles[i].text = "Lança de Anti-Matéria\n<color=#ffd700>[EVOLUÇÃO LENDÁRIA ★]</color>";
                    upgradeDescs[i].text = "Railgun dispara feixes perfurantes que deixam poças de radiação cósmica no planeta!";
                    break;
                case UpgradeType.VoidVortex:
                    upgradeTitles[i].text = "Vórtice de Singularidade\n<color=#ffd700>[EVOLUÇÃO LENDÁRIA ★]</color>";
                    upgradeDescs[i].text = "Minas orbitais geram buracos negros que sugam inimigos e implodem causando 120 de dano!";
                    break;
                case UpgradeType.TeslaChain:
                    upgradeTitles[i].text = "Rede Neural Tesla\n<color=#ffd700>[EVOLUÇÃO LENDÁRIA ★]</color>";
                    upgradeDescs[i].text = "Drones disparam arcos elétricos que saltam em cascata para até 3 inimigos próximos!";
                    break;
                case UpgradeType.HyperionBarrier:
                    upgradeTitles[i].text = "Barreira Hiperiônica\n<color=#ffd700>[EVOLUÇÃO LENDÁRIA ★]</color>";
                    upgradeDescs[i].text = "Escudo Aegis emite ondas de choque douradas ao quebrar e recarregar, repelindo e esmagando alvos!";
                    break;
            }

            RuntimeUIBuilder.StyleUpgrade(this, i, type);
            int index = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => ApplyUpgrade(index));
        }
        if(UnityEngine.EventSystems.EventSystem.current!=null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(upgradeButtons[0].gameObject);

        Transform rerollBtn = levelUpPanel.transform.Find("RerollBtn");
        if (rerollBtn != null)
        {
            rerollBtn.gameObject.SetActive(availableRerolls > 0);
            Text rTxt = rerollBtn.GetComponentInChildren<Text>();
            if (rTxt != null) rTxt.text = "RE-ROLL (" + availableRerolls + ")";
        }
    }

    void ApplyUpgrade(int index)
    {
        UpgradeType type = currentUpgrades[index];
        if (!upgradeLevels.ContainsKey(type)) upgradeLevels[type] = 0;
        upgradeLevels[type]++;

        Weapon w = player != null ? player.GetComponent<Weapon>() : null;
        PlayerMovement pm = player != null ? player.GetComponent<PlayerMovement>() : null;

        switch (type)
        {
            case UpgradeType.Spread:
                if (w) w.bonusSpread++;
                break;
            case UpgradeType.FireRate:
                if (w) w.fireRateMultiplier *= 1.25f;
                break;
            case UpgradeType.Damage:
                if (w) w.damageMultiplier += 0.20f;
                break;
            case UpgradeType.Speed:
                if (pm) pm.moveSpeed += 1.8f;
                break;
            case UpgradeType.Magnet:
                magnetRadius += 3.5f;
                break;
            case UpgradeType.Heal:
                maxHp += 20;
                hp = Mathf.Min(maxHp, hp + 60);
                UpdateHPText();
                break;
            case UpgradeType.Pierce:
                if (w) w.bonusPierce++;
                break;
            case UpgradeType.Bounce:
                if (w) w.bonusBounce++;
                break;
            case UpgradeType.Critical:
                if (w) w.critChance = Mathf.Min(0.75f, w.critChance + 0.15f);
                break;
            case UpgradeType.Explosive:
                if (w) w.isExplosive = true;
                break;
            case UpgradeType.OrbitalMines:
                if (player != null)
                {
                    OrbitalMines mines = player.GetComponent<OrbitalMines>();
                    if (mines == null) mines = player.gameObject.AddComponent<OrbitalMines>();
                    mines.level = upgradeLevels[type];
                }
                break;
            case UpgradeType.SentinelDrone:
                if (player != null)
                {
                    SentinelDrone drone = player.GetComponent<SentinelDrone>();
                    if (drone == null) drone = player.gameObject.AddComponent<SentinelDrone>();
                    drone.SetLevel(upgradeLevels[type]);
                }
                break;
            case UpgradeType.AegisShield:
                if (player != null)
                {
                    ShieldAegis shield = player.GetComponent<ShieldAegis>();
                    if (shield == null) shield = player.gameObject.AddComponent<ShieldAegis>();
                    shield.SetLevel(upgradeLevels[type]);
                }
                break;
            case UpgradeType.LifeSteal:
                lifeStealChance = Mathf.Min(0.25f, lifeStealChance + 0.06f);
                break;
            case UpgradeType.SupernovaGatling:
                if (w != null)
                {
                    w.isSupernova = true;
                    w.currentWeapon = Weapon.WeaponType.Blaster;
                    w.SetupWeapon();
                }
                break;
            case UpgradeType.NebulaFlak:
                if (w != null)
                {
                    w.isNebulaFlak = true;
                    w.currentWeapon = Weapon.WeaponType.Shotgun;
                    w.SetupWeapon();
                }
                break;
            case UpgradeType.AntimatterLance:
                if (w != null)
                {
                    w.isAntimatterLance = true;
                    w.currentWeapon = Weapon.WeaponType.Railgun;
                    w.SetupWeapon();
                }
                break;
            case UpgradeType.VoidVortex:
                if (player != null)
                {
                    OrbitalMines mines = player.GetComponent<OrbitalMines>();
                    if (mines == null) mines = player.gameObject.AddComponent<OrbitalMines>();
                    mines.isVoidVortex = true;
                    if (mines.level < 3) mines.level = 3;
                }
                break;
            case UpgradeType.TeslaChain:
                if (player != null)
                {
                    SentinelDrone drone = player.GetComponent<SentinelDrone>();
                    if (drone == null) drone = player.gameObject.AddComponent<SentinelDrone>();
                    drone.isTeslaChain = true;
                    drone.SetLevel(Mathf.Max(3, drone.level));
                }
                break;
            case UpgradeType.HyperionBarrier:
                if (player != null)
                {
                    ShieldAegis shield = player.GetComponent<ShieldAegis>();
                    if (shield == null) shield = player.gameObject.AddComponent<ShieldAegis>();
                    shield.isHyperionBarrier = true;
                    shield.SetLevel(Mathf.Max(3, shield.level));
                }
                break;
        }

        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
        GameAudio.SetGameplayPaused(false);
        GameAudio.Play(AudioCue.Upgrade);

        if (pendingLevelUps > 0)
        {
            CheckPendingLevelUp();
        }
    }

    void SpawnDropPod()
    {
        GameObject startingPlanet = GameObject.Find("Planet_1");
        if (startingPlanet == null) return;

        GameObject dropObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dropObj.name = "DropPod";
        dropObj.transform.localScale = new Vector3(2f, 2f, 2f);
        
        Vector3 randomDir = Random.onUnitSphere;
        float radius = startingPlanet.transform.localScale.x / 2f;
        Vector3 surfacePos = startingPlanet.transform.position + randomDir * radius;
        
        dropObj.transform.position = surfacePos;
        dropObj.transform.up = randomDir;
        dropObj.transform.SetParent(startingPlanet.transform, true);
        
        DropPod dp = dropObj.AddComponent<DropPod>();
        dp.planet = startingPlanet.transform;
    }

    void SpawnPlayerCorrectly()
    {
        if (player == null) return;
        GameObject startingPlanet = GameObject.Find("Planet_1");
        if (startingPlanet == null) return;

        GravityBody body = player.GetComponent<GravityBody>();
        if (body != null)
        {
            body.planet = startingPlanet.GetComponent<PlanetGravity>();
            player.position = startingPlanet.transform.position + startingPlanet.transform.up;
            body.SnapToSurface();
        }

        if (player.GetComponent<OrbitalMines>() == null) player.gameObject.AddComponent<OrbitalMines>();
        if (player.GetComponent<SentinelDrone>() == null) player.gameObject.AddComponent<SentinelDrone>();
        if (player.GetComponent<ShieldAegis>() == null) player.gameObject.AddComponent<ShieldAegis>();

        GameObject fxPrefab = Resources.Load<GameObject>("TeleportFX");
        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, player.position, Quaternion.identity);
            fx.transform.SetParent(startingPlanet.transform, true);
            Destroy(fx, 2f);
        }
    }

    void CreateHUD() { ModernHUD.Create(this); }

    public bool isPaused = false;
    private GameObject pausePanel;

    public void TogglePauseMenu()
    {
        if (isGameOver || (levelUpPanel != null && levelUpPanel.activeSelf)) return;

        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj != null)
        {
            Transform hangar = canvasObj.transform.Find("HangarPanel");
            if (hangar != null)
            {
                Destroy(hangar.gameObject);
                return;
            }
        }

        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            GameAudio.SetGameplayPaused(true);
            if (pausePanel == null)
            {
                pausePanel = RuntimeUIBuilder.BuildPauseMenu(this, () => TogglePauseMenu());
            }
            else
            {
                pausePanel.SetActive(true);
                RuntimeUIBuilder.RefreshPauseMenuHighlights();
            }
        }
        else
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
            GameAudio.SetGameplayPaused(false);
            if (pendingLevelUps > 0)
            {
                CheckPendingLevelUp();
            }
        }
    }

    void Update()
    {
        UpdateFrenzy();
        bool pauseInput = false;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            pauseInput = true;
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            pauseInput = true;

        if (pauseInput)
        {
            TogglePauseMenu();
        }

        if (pendingLevelUps > 0 && !isPaused && !IsLevelUpActive() && !BossIntroSequence.isIntroPlaying)
        {
            CheckPendingLevelUp();
        }

        if (Time.timeScale > 0)
        {
            matchTime += Time.deltaTime;
            if (timeText != null)
            {
                int minutes = (int)(matchTime / 60f);
                int seconds = (int)(matchTime % 60f);
                timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        
            dropTimer -= Time.deltaTime;
            if (dropTimer <= 0f)
            {
                dropTimer = 45f;
                SpawnDropPod();
            }
        }
    }

    public void UpdateHPText()
    {
        if (hpText != null)
        {
            hpText.text = $"HP {hp}/{maxHp}";
        }
    }

    public void AddXP(int amount)
    {
        if (PlanetaryBiome.CurrentBiome != null)
        {
            amount = Mathf.RoundToInt(amount * PlanetaryBiome.CurrentBiome.xpMultiplier);
        }
        xp += amount;
        while (xp >= xpToNextLevel)
        {
            xp -= xpToNextLevel;
            level++;
            xpToNextLevel = (int)(xpToNextLevel * 1.5f);
            pendingLevelUps++;
        }
        UpdateUI();
        CheckPendingLevelUp();
    }

    public void CheckPendingLevelUp()
    {
        if (pendingLevelUps <= 0) return;
        if (IsLevelUpActive()) return;
        if (BossIntroSequence.isIntroPlaying) return;
        if (isPaused) return;

        pendingLevelUps--;
        ShowLevelUpScreen();
    }

    void UpdateUI()
    {
        if (xpFill != null)
        {
            float fillPct = xpToNextLevel>0 ? Mathf.Clamp01((float)xp/xpToNextLevel) : 0f;
            xpFill.anchorMax = new Vector2(fillPct, 1);
        }
        if (levelText != null)
        {
            levelText.text = "NÍVEL " + level.ToString("00");
        }
    }

    public void Heal(int amount)
    {
        hp = Mathf.Min(maxHp, hp + amount);
        UpdateHPText();
    }

    private Coroutine rumbleRoutine;

    public void TriggerGamepadRumble(float lowFreq, float highFreq, float duration)
    {
        if (Gamepad.current == null) return;
        if (rumbleRoutine != null) StopCoroutine(rumbleRoutine);
        rumbleRoutine = StartCoroutine(RumbleRoutine(lowFreq, highFreq, duration));
    }

    private System.Collections.IEnumerator RumbleRoutine(float lowFreq, float highFreq, float duration)
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(lowFreq, highFreq);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (Gamepad.current != null)
        {
            Gamepad.current.ResetHaptics();
        }
        rumbleRoutine = null;
    }

    void OnDisable()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.ResetHaptics();
        }
    }

    void OnApplicationQuit()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.ResetHaptics();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || isGameOver) return;

        ShieldAegis shield = player != null ? player.GetComponent<ShieldAegis>() : null;
        if (shield != null && shield.TryAbsorbDamage())
        {
            TriggerGamepadRumble(0.3f, 0.5f, 0.15f);
            return;
        }

        if (damage > 0)
        {
            GameAudio.Play(AudioCue.PlayerHit);
            TriggerGamepadRumble(0.6f, 0.8f, 0.25f);
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.TriggerShake(0.18f, 0.4f);
            }
        }
        hp -= damage;
        UpdateHPText();
        
        if (hp <= 0)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        TriggerGamepadRumble(0.8f, 1.0f, 0.5f);
        RuntimeUIBuilder.BuildGameOverUI(this);
    }

    [Header("Frenzy State")]
    public bool isFrenzyActive = false;
    public float frenzyTimer = 0f;

    void UpdateFrenzy()
    {
        if (isFrenzyActive)
        {
            frenzyTimer -= Time.deltaTime;
            RuntimeUIBuilder.UpdateFrenzyHUD(frenzyTimer);
            if (frenzyTimer <= 0f)
            {
                isFrenzyActive = false;
                frenzyTimer = 0f;
                RuntimeUIBuilder.UpdateFrenzyHUD(0f);
                if (player != null)
                {
                    Weapon w = player.GetComponent<Weapon>();
                    if (w != null) w.fireRateMultiplier /= 2.0f;
                }
            }
        }
    }

    public void TriggerFrenzy(float duration = 8f)
    {
        if (!isFrenzyActive)
        {
            isFrenzyActive = true;
            if (player != null)
            {
                Weapon w = player.GetComponent<Weapon>();
                if (w != null) w.fireRateMultiplier *= 2.0f;
            }
        }
        frenzyTimer = Mathf.Max(frenzyTimer, duration);
        RuntimeUIBuilder.UpdateFrenzyHUD(frenzyTimer);
    }

    private Coroutine carePackageCoroutine;

    void StartCarePackageScheduler()
    {
        if (carePackageCoroutine != null) StopCoroutine(carePackageCoroutine);
        carePackageCoroutine = StartCoroutine(CarePackageSchedulerRoutine());
    }

    IEnumerator CarePackageSchedulerRoutine()
    {
        yield return new WaitForSeconds(45f); // First supply drop after 45s
        while (!isGameOver)
        {
            SpawnCarePackageOnCurrentPlanet();
            yield return new WaitForSeconds(Random.Range(60f, 75f));
        }
    }

    public void SpawnCarePackageOnCurrentPlanet()
    {
        PlanetGravity currentPlanet = null;
        if (EnemySpawner.Instance != null && EnemySpawner.Instance.currentPlanet != null)
        {
            currentPlanet = EnemySpawner.Instance.currentPlanet;
        }
        else
        {
            GameObject p1 = GameObject.Find("Planet_1");
            if (p1 != null) currentPlanet = p1.GetComponent<PlanetGravity>();
        }

        if (currentPlanet == null || player == null) return;

        Transform pTrans = currentPlanet.transform;
        Vector3 surfaceNormal = (player.position - pTrans.position).normalized;
        Vector3 randomTangent = Vector3.ProjectOnPlane(Random.onUnitSphere, surfaceNormal).normalized;
        if (randomTangent.sqrMagnitude < 0.01f) randomTangent = Vector3.Cross(surfaceNormal, Vector3.up).normalized;

        float distance = Random.Range(10f, 22f);
        Vector3 targetPos = player.position + randomTangent * distance;
        Vector3 finalNormal = (targetPos - pTrans.position).normalized;
        float radius = pTrans.localScale.x * 0.5f;
        Vector3 spawnPos = pTrans.position + finalNormal * radius;

        GameObject dropObj = new GameObject("CarePackageDrop");
        dropObj.transform.position = spawnPos;
        dropObj.transform.up = finalNormal;
        dropObj.transform.SetParent(pTrans, true);

        CarePackageDrop drop = dropObj.AddComponent<CarePackageDrop>();
        drop.Setup(pTrans, finalNormal, pTrans.lossyScale.x);
    }
}
