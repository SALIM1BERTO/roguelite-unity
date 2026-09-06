using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; public GameObject floatingTextPrefab;
    public Transform player; public int hp = 100; public int maxHp = 100;

    public UnityEngine.UI.Text timeText;
    public UnityEngine.UI.Text hpText;
    public float matchTime = 0f;

    public bool isInvincible = false;
    public bool isGameOver = false;
    private float dropTimer = 30f; // First drop at 30s



    public int xp = 0;
    public int level = 1;
    public float magnetRadius = 5f;
    
    [Header("Level Up UI")]
    public GameObject levelUpPanel;
    public UnityEngine.UI.Button[] upgradeButtons;
    public UnityEngine.UI.Text[] upgradeTitles;
    public UnityEngine.UI.Text[] upgradeDescs;

    public enum UpgradeType { Spread, FireRate, Speed, Magnet, Heal, Pierce, Bounce, Explosive }
    private UpgradeType[] currentUpgrades = new UpgradeType[3];

    public int xpToNextLevel = 100;

    public RectTransform xpFill;
    public Text levelText;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        GameAudio.EnsureExists();
        GameMusic.EnsureExists();
    }

    void Start()
    {
        CreateHUD(); SpawnPlayerCorrectly();
        UpdateUI();
    }

    
    
    void ShowLevelUpScreen()
    {
        if (levelUpPanel == null || upgradeButtons == null || upgradeButtons.Length < 3 || upgradeButtons[0] == null) { RuntimeUIBuilder.BuildLevelUpUI(this); }
        Time.timeScale = 0f;
        GameAudio.SetGameplayPaused(true);
        GameAudio.Play(AudioCue.LevelUp);
        levelUpPanel.SetActive(true);

        UpgradeType[] allTypes = (UpgradeType[])System.Enum.GetValues(typeof(UpgradeType));
        
        for (int i = 0; i < 3; i++)
        {
            UpgradeType type = allTypes[Random.Range(0, allTypes.Length)];
            currentUpgrades[i] = type;
            
            UnityEngine.UI.Text iconText = upgradeButtons[i].transform.Find("Icon") != null ? upgradeButtons[i].transform.Find("Icon").GetComponent<UnityEngine.UI.Text>() : null;

            switch (type)
            {
                case UpgradeType.Spread:
                    if (iconText != null) { iconText.text = "»»»"; iconText.color = new Color(1f, 0.5f, 0f); }
                    upgradeTitles[i].text = "Tiro Múltiplo";
                    upgradeDescs[i].text = "Adiciona +1 projétil aos seus disparos.";
                    break;
                case UpgradeType.FireRate:
                    if (iconText != null) { iconText.text = "⚡"; iconText.color = new Color(1f, 0.9f, 0f); }
                    upgradeTitles[i].text = "Tiro Rápido";
                    upgradeDescs[i].text = "Aumenta a cadência de disparo em 25%.";
                    break;
                case UpgradeType.Speed:
                    if (iconText != null) { iconText.text = "☄"; iconText.color = new Color(0f, 0.8f, 1f); }
                    upgradeTitles[i].text = "Propulsores";
                    upgradeDescs[i].text = "Aumenta a velocidade de movimento da nave.";
                    break;
                case UpgradeType.Magnet:
                    if (iconText != null) { iconText.text = "🧲"; iconText.color = new Color(1f, 0f, 1f); }
                    upgradeTitles[i].text = "Magnetismo";
                    upgradeDescs[i].text = "Aumenta o raio de coleta de gemas de XP.";
                    break;
                case UpgradeType.Heal:
                    if (iconText != null) { iconText.text = "♥"; iconText.color = new Color(1f, 0.2f, 0.4f); }
                    upgradeTitles[i].text = "Reparo Estrutural";
                    upgradeDescs[i].text = "Cura 50 HP e aumenta vida máxima.";
                    break;
                case UpgradeType.Pierce:
                    if (iconText != null) { iconText.text = "⤏"; iconText.color = new Color(0.8f, 1f, 0.8f); }
                    upgradeTitles[i].text = "Projétil Perfurante";
                    upgradeDescs[i].text = "Seus tiros atravessam +1 inimigo antes de sumir.";
                    break;
                case UpgradeType.Bounce:
                    if (iconText != null) { iconText.text = "⤡"; iconText.color = new Color(0.2f, 1f, 0.2f); }
                    upgradeTitles[i].text = "Ricochete Cósmico";
                    upgradeDescs[i].text = "Tiros quicam para o próximo inimigo mais próximo!";
                    break;
                case UpgradeType.Explosive:
                    if (iconText != null) { iconText.text = "✸"; iconText.color = new Color(1f, 0.1f, 0.1f); }
                    upgradeTitles[i].text = "Munição Explosiva";
                    upgradeDescs[i].text = "Acertos geram uma explosão em área! (AoE)";
                    break;
            }
            
            int index = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => ApplyUpgrade(index));
        }
    }

    void ApplyUpgrade(int index)
    {
        UpgradeType type = currentUpgrades[index];
        Weapon w = player.GetComponent<Weapon>();
        PlayerMovement pm = player.GetComponent<PlayerMovement>();

        switch (type)
        {
            case UpgradeType.Spread:
                if (w) w.spreadCount++;
                break;
            case UpgradeType.FireRate:
                if (w) w.fireRate *= 1.25f;
                break;
            case UpgradeType.Speed:
                if (pm) pm.moveSpeed += 2f;
                break;
            case UpgradeType.Magnet:
                magnetRadius += 3f;
                break;
            case UpgradeType.Heal:
                hp += 50;
                if (hp > maxHp) maxHp = hp;
                UpdateHPText();
                break;
            case UpgradeType.Pierce:
                if (w) w.pierceCount++;
                break;
            case UpgradeType.Bounce:
                if (w) w.bounceCount++;
                break;
            case UpgradeType.Explosive:
                if (w) w.explosive = true;
                break;
        }

        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
        GameAudio.SetGameplayPaused(false);
        GameAudio.Play(AudioCue.Upgrade);
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

        GameObject fxPrefab = Resources.Load<GameObject>("TeleportFX");
        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, player.position, Quaternion.identity);
            fx.transform.SetParent(startingPlanet.transform, true);
            Destroy(fx, 2f);
        }
    }

    void CreateHUD()
    {
        
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("CanvasHUD");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Thin XP Bar at bottom
            GameObject xpBarBg = new GameObject("XPBarBG");
            xpBarBg.transform.SetParent(canvasObj.transform, false);
            Image bgImg = xpBarBg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            RectTransform rtBg = xpBarBg.GetComponent<RectTransform>();
            rtBg.anchorMin = new Vector2(0, 0); rtBg.anchorMax = new Vector2(1, 0);
            rtBg.pivot = new Vector2(0.5f, 0); rtBg.anchoredPosition = new Vector2(0, 0);
            rtBg.sizeDelta = new Vector2(0, 15);

            GameObject xpBarFill = new GameObject("XPBarFill");
            xpBarFill.transform.SetParent(xpBarBg.transform, false);
            Image fillImg = xpBarFill.AddComponent<Image>();
            fillImg.color = new Color(0f, 1f, 0.8f); // Neon Cyan
            xpFill = xpBarFill.GetComponent<RectTransform>();
            xpFill.anchorMin = new Vector2(0, 0); xpFill.anchorMax = new Vector2(1, 1);
            xpFill.pivot = new Vector2(0, 0.5f); xpFill.anchoredPosition = new Vector2(0, 0);
            xpFill.sizeDelta = new Vector2(0, 0);

            // Level Text (Bottom Center)
            GameObject lvlTextObj = new GameObject("LevelText");
            lvlTextObj.transform.SetParent(xpBarBg.transform, false);
            levelText = lvlTextObj.AddComponent<Text>();
            levelText.text = "LVL 1";
            levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.color = Color.white;
            levelText.fontSize = 14;
            levelText.fontStyle = FontStyle.Bold;
            RectTransform rtTxt = lvlTextObj.GetComponent<RectTransform>();
            rtTxt.anchorMin = new Vector2(0.5f, 0); rtTxt.anchorMax = new Vector2(0.5f, 1);
            rtTxt.pivot = new Vector2(0.5f, 0.5f); rtTxt.anchoredPosition = new Vector2(0, 0);
            rtTxt.sizeDelta = new Vector2(200, 0);

            // Top HUD (Time & HP)
            GameObject topHud = new GameObject("TopHUD");
            topHud.transform.SetParent(canvasObj.transform, false);
            RectTransform rtTop = topHud.AddComponent<RectTransform>();
            rtTop.anchorMin = new Vector2(0, 1); rtTop.anchorMax = new Vector2(1, 1);
            rtTop.pivot = new Vector2(0.5f, 1); rtTop.anchoredPosition = new Vector2(0, -20);
            rtTop.sizeDelta = new Vector2(-40, 40);

            // Time Text (Center)
            GameObject timeObj = new GameObject("TimeText");
            timeObj.transform.SetParent(topHud.transform, false);
            timeText = timeObj.AddComponent<Text>();
            timeText.text = "00:00";
            timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timeText.alignment = TextAnchor.UpperCenter;
            timeText.color = Color.white;
            timeText.fontSize = 24;
            timeText.fontStyle = FontStyle.Bold;
            RectTransform timeRt = timeObj.GetComponent<RectTransform>();
            timeRt.anchorMin = new Vector2(0.5f, 1); timeRt.anchorMax = new Vector2(0.5f, 1);
            timeRt.pivot = new Vector2(0.5f, 1); timeRt.anchoredPosition = new Vector2(0, 0);
            timeRt.sizeDelta = new Vector2(200, 40);

            // HP Text (Left)
            GameObject hpObj = new GameObject("HPText");
            hpObj.transform.SetParent(topHud.transform, false);
            hpText = hpObj.AddComponent<Text>();
            hpText.text = "HP 100/100";
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpText.alignment = TextAnchor.UpperLeft;
            hpText.color = new Color(1f, 0.2f, 0.4f); // Neon Red/Pink
            hpText.fontSize = 20;
            hpText.fontStyle = FontStyle.Bold;
            RectTransform hpRt = hpObj.GetComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0, 1); hpRt.anchorMax = new Vector2(0, 1);
            hpRt.pivot = new Vector2(0, 1); hpRt.anchoredPosition = new Vector2(0, 0);
            hpRt.sizeDelta = new Vector2(200, 40);
        }
    }

    
    void Update()
    {
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
                dropTimer = 45f; // Drop every 45s
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
        xp += amount;
        if (xp >= xpToNextLevel)
        {
            xp -= xpToNextLevel;
            level++;
            xpToNextLevel = (int)(xpToNextLevel * 1.5f);
            LevelUp();
        }
        UpdateUI();
    }

    void LevelUp() { ShowLevelUpScreen(); }

    void UpdateUI()
    {
        if (xpFill != null)
        {
            float fillPct = (float)xp / xpToNextLevel;
            xpFill.anchorMax = new Vector2(fillPct, 1);
        }
        if (levelText != null)
        {
            levelText.text = "LVL " + level;
        }
    }

    
    public void TakeDamage(int damage)
    {
        if (isInvincible || isGameOver) return;
        if (damage > 0) GameAudio.Play(AudioCue.PlayerHit);
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
        RuntimeUIBuilder.BuildGameOverUI(this);
    }

}

