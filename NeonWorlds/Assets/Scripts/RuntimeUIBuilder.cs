using UnityEngine;
using UnityEngine.UI;

public static class RuntimeUIBuilder
{
    private static GameObject bossBarObj;
    private static Image bossFillImg;
    private static Text bossText;

    public static void BuildBossHealthBar(BossLeviathan boss)
    {
        BuildBossHealthBarDirect(boss);
    }

    public static void BuildBossHealthBarDirect(BossLeviathan boss)
    {
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj == null) return;

        if (bossBarObj != null) Object.Destroy(bossBarObj);

        bossBarObj = new GameObject("BossHealthBarContainer");
        bossBarObj.transform.SetParent(canvasObj.transform, false);

        RectTransform contRt = bossBarObj.AddComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0.2f, 0.88f);
        contRt.anchorMax = new Vector2(0.8f, 0.94f);
        contRt.sizeDelta = Vector2.zero;
        contRt.anchoredPosition = Vector2.zero;

        // Background
        Image bg = bossBarObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        Outline outline = bossBarObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.2f, 0.4f, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        // Fill bar
        GameObject fillObj = new GameObject("BossFill");
        fillObj.transform.SetParent(bossBarObj.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;

        bossFillImg = fillObj.AddComponent<Image>();
        bossFillImg.color = new Color(0.9f, 0.15f, 0.35f, 1f);

        // Text label
        GameObject textObj = new GameObject("BossLabel");
        textObj.transform.SetParent(bossBarObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;

        bossText = textObj.AddComponent<Text>();
        bossText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bossText.text = "LEVIATÃ CÓSMICO - GUARDIÃO DO PLANETA";
        bossText.fontSize = 18;
        bossText.fontStyle = FontStyle.Bold;
        bossText.alignment = TextAnchor.MiddleCenter;
        bossText.color = Color.white;
    }

    public static void UpdateBossHP(int current, int max)
    {
        if (bossFillImg != null)
        {
            float pct = Mathf.Clamp01((float)current / max);
            bossFillImg.rectTransform.anchorMax = new Vector2(pct, 1f);
        }
    }

    public static void HideBossHealthBar()
    {
        if (bossBarObj != null)
        {
            Object.Destroy(bossBarObj);
            bossBarObj = null;
        }
    }

    public static void StyleUpgradeCard(Button btn, GameManager.UpgradeRarity rarity)
    {
        if (btn == null) return;
        Outline outline = btn.GetComponent<Outline>();
        Image img = btn.GetComponent<Image>();
        Color borderColor = Color.cyan;
        Color bgColor = new Color(0.06f, 0.07f, 0.12f, 0.96f);

        switch (rarity)
        {
            case GameManager.UpgradeRarity.Common:
                borderColor = new Color(0f, 0.9f, 0.8f);
                break;
            case GameManager.UpgradeRarity.Rare:
                borderColor = new Color(0.1f, 0.6f, 1f);
                break;
            case GameManager.UpgradeRarity.Epic:
                borderColor = new Color(0.85f, 0.15f, 1f);
                break;
            case GameManager.UpgradeRarity.Legendary:
                borderColor = new Color(1f, 0.85f, 0.1f);
                bgColor = new Color(0.14f, 0.10f, 0.03f, 0.98f);
                break;
        }

        if (outline != null)
        {
            outline.effectColor = (rarity == GameManager.UpgradeRarity.Legendary) ? new Color(1f, 0.88f, 0.2f, 1f) : borderColor;
            outline.effectDistance = (rarity == GameManager.UpgradeRarity.Legendary) ? new Vector2(3.5f, -3.5f) : new Vector2(2.5f, -2.5f);
        }
        if (img != null) img.color = bgColor;
    }

    public static void BuildVictoryUI(GameManager gm)
    {
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj == null) return;

        Time.timeScale = 0f;

        int earnedCores = 100 + (gm.level * 5);
        MetaProgression.AddStarCores(earnedCores);

        GameObject panel = new GameObject("VictoryPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero; panelRt.anchoredPosition = Vector2.zero;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.15f, 0.08f, 0.94f);

        // Titulo
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        RectTransform trt = title.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.68f); trt.anchorMax = new Vector2(1f, 0.92f);
        trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
        Text tText = title.AddComponent<Text>();
        tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tText.text = "PLANETA PURIFICADO!\nO Leviatã Foi Derrotado";
        tText.fontSize = 38; tText.alignment = TextAnchor.MiddleCenter; tText.color = new Color(0.2f, 1f, 0.5f);

        // Stats
        GameObject stats = new GameObject("Stats");
        stats.transform.SetParent(panel.transform, false);
        RectTransform srt = stats.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0.38f); srt.anchorMax = new Vector2(1f, 0.62f);
        srt.sizeDelta = Vector2.zero; srt.anchoredPosition = Vector2.zero;
        Text sText = stats.AddComponent<Text>();
        sText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        int minutes = Mathf.FloorToInt(gm.matchTime / 60F);
        int seconds = Mathf.FloorToInt(gm.matchTime - minutes * 60);
        sText.text = $"Nível Final: {gm.level}  |  Tempo: {minutes:00}:{seconds:00}\n" +
                     $"<color=#00ffcc>★ Células Estelares Obtidas: +{earnedCores}</color>\n" +
                     $"<color=#ffcc00>Total no Hangar: {MetaProgression.GetStarCores()}</color>";
        sText.fontSize = 24; sText.alignment = TextAnchor.MiddleCenter; sText.color = Color.white;

        // Botoes Container
        GameObject btnCont = new GameObject("Buttons");
        btnCont.transform.SetParent(panel.transform, false);
        RectTransform brt = btnCont.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.15f, 0.12f); brt.anchorMax = new Vector2(0.85f, 0.28f);
        brt.sizeDelta = Vector2.zero; brt.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup hlg = btnCont.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        // Jogar Novamente
        GameObject playAgainBtn = CreateStyledButton(btnCont.transform, "JOGAR NOVAMENTE", new Color(0.2f, 0.8f, 0.4f), Color.black, () => {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });

        // Hangar
        CreateStyledButton(btnCont.transform, "HANGAR", new Color(0.2f, 0.6f, 1f), Color.white, () => {
            BuildHangarUI(canvasObj.transform);
        });

        // Sair
        CreateStyledButton(btnCont.transform, "SAIR", new Color(0.4f, 0.4f, 0.5f), Color.white, () => {
            Application.Quit();
        });
    }

    public static void BuildGameOverUI(GameManager gm)
    {
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj == null) return;

        int earnedCores = Mathf.Max(5, (gm.level * 3) + (int)(gm.matchTime / 10f));
        MetaProgression.AddStarCores(earnedCores);

        GameObject panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero; panelRt.anchoredPosition = Vector2.zero;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0f, 0.03f, 0.94f);

        // Titulo
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        RectTransform trt = title.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.68f); trt.anchorMax = new Vector2(1f, 0.92f);
        trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
        Text tText = title.AddComponent<Text>();
        tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tText.text = "SISTEMAS CRÍTICOS FALHARAM\nNave Destruída";
        tText.fontSize = 38; tText.alignment = TextAnchor.MiddleCenter; tText.color = new Color(1f, 0.2f, 0.2f);

        // Stats
        GameObject stats = new GameObject("Stats");
        stats.transform.SetParent(panel.transform, false);
        RectTransform srt = stats.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0.38f); srt.anchorMax = new Vector2(1f, 0.62f);
        srt.sizeDelta = Vector2.zero; srt.anchoredPosition = Vector2.zero;
        Text sText = stats.AddComponent<Text>();
        sText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        int minutes = Mathf.FloorToInt(gm.matchTime / 60F);
        int seconds = Mathf.FloorToInt(gm.matchTime - minutes * 60);
        sText.text = $"Nível Alcançado: {gm.level}  |  Tempo: {minutes:00}:{seconds:00}\n" +
                     $"<color=#00ffcc>★ Células Estelares Coletadas: +{earnedCores}</color>\n" +
                     $"<color=#ffcc00>Total no Hangar: {MetaProgression.GetStarCores()}</color>";
        sText.fontSize = 24; sText.alignment = TextAnchor.MiddleCenter; sText.color = Color.white;

        // Botoes Container
        GameObject btnCont = new GameObject("Buttons");
        btnCont.transform.SetParent(panel.transform, false);
        RectTransform brt = btnCont.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.15f, 0.12f); brt.anchorMax = new Vector2(0.85f, 0.28f);
        brt.sizeDelta = Vector2.zero; brt.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup hlg = btnCont.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        CreateStyledButton(btnCont.transform, "REINICIAR", new Color(0.2f, 0.8f, 0.3f), Color.black, () => {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });

        CreateStyledButton(btnCont.transform, "HANGAR", new Color(0.2f, 0.6f, 1f), Color.white, () => {
            BuildHangarUI(canvasObj.transform);
        });

        CreateStyledButton(btnCont.transform, "SAIR", new Color(0.4f, 0.4f, 0.5f), Color.white, () => {
            Application.Quit();
        });
    }

    public static void BuildHangarUI(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find("HangarPanel");
        if (existing != null) Object.Destroy(existing.gameObject);

        GameObject panel = new GameObject("HangarPanel");
        panel.transform.SetParent(canvasTransform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.06f, 0.05f); panelRt.anchorMax = new Vector2(0.94f, 0.95f);
        panelRt.sizeDelta = Vector2.zero; panelRt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.1f, 0.98f);
        Outline outl = panel.AddComponent<Outline>();
        outl.effectColor = new Color(0.2f, 0.7f, 1f, 0.8f);
        outl.effectDistance = new Vector2(3, -3);

        // Header
        GameObject header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        RectTransform hrt = header.AddComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 0.90f); hrt.anchorMax = new Vector2(1f, 0.98f);
        hrt.sizeDelta = Vector2.zero; hrt.anchoredPosition = Vector2.zero;
        Text hText = header.AddComponent<Text>();
        hText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hText.text = "HANGAR ESTELAR - FROTAS & OFICINA";
        hText.fontSize = 26; hText.alignment = TextAnchor.MiddleCenter; hText.color = new Color(0f, 0.9f, 1f);

        // Currency Display
        GameObject currency = new GameObject("CurrencyText");
        currency.transform.SetParent(panel.transform, false);
        RectTransform crt = currency.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.05f, 0.84f); crt.anchorMax = new Vector2(0.95f, 0.89f);
        crt.sizeDelta = Vector2.zero; crt.anchoredPosition = Vector2.zero;
        Text cText = currency.AddComponent<Text>();
        cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cText.fontSize = 20; cText.alignment = TextAnchor.MiddleCenter;

        // Tab Navigation Container
        GameObject tabsObj = new GameObject("TabBar");
        tabsObj.transform.SetParent(panel.transform, false);
        RectTransform tabRt = tabsObj.AddComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0.18f, 0.76f); tabRt.anchorMax = new Vector2(0.82f, 0.82f);
        tabRt.sizeDelta = Vector2.zero; tabRt.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup thlg = tabsObj.AddComponent<HorizontalLayoutGroup>();
        thlg.spacing = 15; thlg.childForceExpandWidth = true; thlg.childForceExpandHeight = true;

        // Content Area
        GameObject contentArea = new GameObject("ContentArea");
        contentArea.transform.SetParent(panel.transform, false);
        RectTransform cart = contentArea.AddComponent<RectTransform>();
        cart.anchorMin = new Vector2(0.05f, 0.12f); cart.anchorMax = new Vector2(0.95f, 0.74f);
        cart.sizeDelta = Vector2.zero; cart.anchoredPosition = Vector2.zero;

        // Container 1: Ships
        GameObject shipsCont = new GameObject("ShipsList");
        shipsCont.transform.SetParent(contentArea.transform, false);
        RectTransform srt = shipsCont.AddComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.sizeDelta = Vector2.zero;
        VerticalLayoutGroup svlg = shipsCont.AddComponent<VerticalLayoutGroup>();
        svlg.spacing = 8; svlg.childControlHeight = true; svlg.childControlWidth = true;
        svlg.childForceExpandWidth = true; svlg.childForceExpandHeight = false;

        // Container 2: Upgrades
        GameObject upgCont = new GameObject("UpgradeList");
        upgCont.transform.SetParent(contentArea.transform, false);
        RectTransform urt = upgCont.AddComponent<RectTransform>();
        urt.anchorMin = Vector2.zero; urt.anchorMax = Vector2.one; urt.sizeDelta = Vector2.zero;
        VerticalLayoutGroup uvlg = upgCont.AddComponent<VerticalLayoutGroup>();
        uvlg.spacing = 8; uvlg.childControlHeight = true; uvlg.childControlWidth = true;
        uvlg.childForceExpandWidth = true; uvlg.childForceExpandHeight = false;

        System.Action refreshAll = null;

        System.Action rebuildShips = () => {
            foreach (Transform child in shipsCont.transform) Object.Destroy(child.gameObject);
            CreateShipRow(shipsCont.transform, MetaProgression.ShipChassis.Interceptor,
                "INTERCEPTOR", "CAÇA VELOZ • PADRÃO",
                "+15% Vel. Movimento | +10% Cadência de Tiro | Arma: Blaster de Plasma",
                new Color(0f, 0.9f, 1f), refreshAll);

            CreateShipRow(shipsCont.transform, MetaProgression.ShipChassis.Titan,
                "TITAN DREADNOUGHT", "ENCOURAÇADO PESADO",
                "+60 HP Máximo | +25% Dano Global | -15% Vel. Movimento | Arma: Shotgun",
                new Color(1f, 0.55f, 0.05f), refreshAll);

            CreateShipRow(shipsCont.transform, MetaProgression.ShipChassis.Spectre,
                "SPECTRE", "FANTASMA ESPECTRAL",
                "+25% Chance Crítica | +50% Dano Crítico | -25 HP Máximo | Arma: Railgun",
                new Color(0.85f, 0.2f, 1f), refreshAll);

            CreateShipRow(shipsCont.transform, MetaProgression.ShipChassis.Architect,
                "ARCHITECT", "ENGENHEIRO NEXUS",
                "Inicia com Drone Sentinela Nv 1 | +3m Ímã de Gemas | Halo Orbital | Arma: Blaster",
                new Color(0f, 0.95f, 0.45f), refreshAll);
        };

        System.Action rebuildUpgrades = () => {
            foreach (Transform child in upgCont.transform) Object.Destroy(child.gameObject);
            CreateHangarRow(upgCont.transform, MetaProgression.UP_HULL, "Blindagem de Titânio", "+15 HP Máximo inicial por nível", 5, 12, refreshAll);
            CreateHangarRow(upgCont.transform, MetaProgression.UP_SPEED, "Propulsores Iônicos", "+5% Velocidade de movimento por nível", 5, 12, refreshAll);
            CreateHangarRow(upgCont.transform, MetaProgression.UP_DAMAGE, "Condensador de Plasma", "+10% Dano global por nível", 5, 16, refreshAll);
            CreateHangarRow(upgCont.transform, MetaProgression.UP_MAGNET, "Coletor Gravitacional", "+2m Raio do ímã de gemas por nível", 5, 10, refreshAll);
            CreateHangarRow(upgCont.transform, MetaProgression.UP_REROLL, "Módulo de Re-roll", "+1 Troca de opções por partida", 2, 25, refreshAll);
        };

        refreshAll = () => {
            cText.text = $"<color=#00ffcc>★ Células Estelares Disponíveis: {MetaProgression.GetStarCores()}</color>";
            rebuildShips();
            rebuildUpgrades();
        };

        // Tab state switching
        bool showShips = true;
        GameObject tab1 = CreateStyledButton(tabsObj.transform, "🚀 NAVES & CHASSIS", new Color(0f, 0.8f, 1f), Color.black, null);
        GameObject tab2 = CreateStyledButton(tabsObj.transform, "⚡ MELHORIAS DA FROTA", new Color(0.15f, 0.2f, 0.3f), Color.white, null);

        System.Action updateTabs = () => {
            shipsCont.SetActive(showShips);
            upgCont.SetActive(!showShips);
            tab1.GetComponent<Image>().color = showShips ? new Color(0f, 0.8f, 1f) : new Color(0.15f, 0.2f, 0.3f);
            tab1.GetComponentInChildren<Text>().color = showShips ? Color.black : Color.white;
            tab2.GetComponent<Image>().color = !showShips ? new Color(0f, 0.8f, 1f) : new Color(0.15f, 0.2f, 0.3f);
            tab2.GetComponentInChildren<Text>().color = !showShips ? Color.black : Color.white;
        };

        tab1.GetComponent<Button>().onClick.AddListener(() => {
            showShips = true;
            GameAudio.Play(AudioCue.Upgrade);
            updateTabs();
        });

        tab2.GetComponent<Button>().onClick.AddListener(() => {
            showShips = false;
            GameAudio.Play(AudioCue.Upgrade);
            updateTabs();
        });

        refreshAll();
        updateTabs();

        // Close Button
        GameObject closeBtn = CreateStyledButton(panel.transform, "VOLTAR / FECHAR", new Color(0.8f, 0.2f, 0.3f), Color.white, () => {
            Object.Destroy(panel);
        });
        RectTransform cbrt = closeBtn.GetComponent<RectTransform>();
        cbrt.anchorMin = new Vector2(0.35f, 0.03f); cbrt.anchorMax = new Vector2(0.65f, 0.09f);
        cbrt.sizeDelta = Vector2.zero; cbrt.anchoredPosition = Vector2.zero;
    }

    static void CreateShipRow(Transform parent, MetaProgression.ShipChassis chassis, string name, string subtitle, string desc, Color themeColor, System.Action refreshAll)
    {
        GameObject row = new GameObject("ShipRow_" + chassis);
        row.transform.SetParent(parent, false);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 56; le.preferredHeight = 60;
        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.09f, 0.15f, 0.95f);

        // Accent colored stripe on the left
        GameObject stripe = new GameObject("AccentStripe");
        stripe.transform.SetParent(row.transform, false);
        RectTransform srt = stripe.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(0.012f, 1f);
        srt.sizeDelta = Vector2.zero; srt.anchoredPosition = Vector2.zero;
        Image sImg = stripe.AddComponent<Image>();
        sImg.color = themeColor;

        // Info text container
        GameObject info = new GameObject("Info");
        info.transform.SetParent(row.transform, false);
        RectTransform irt = info.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.025f, 0f); irt.anchorMax = new Vector2(0.68f, 1f);
        irt.sizeDelta = Vector2.zero; irt.anchoredPosition = Vector2.zero;
        Text iText = info.AddComponent<Text>();
        iText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iText.alignment = TextAnchor.MiddleLeft;

        // Action button container
        GameObject btnObj = new GameObject("ActionBtn");
        btnObj.transform.SetParent(row.transform, false);
        RectTransform brt = btnObj.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.70f, 0.12f); brt.anchorMax = new Vector2(0.98f, 0.88f);
        brt.sizeDelta = Vector2.zero; brt.anchoredPosition = Vector2.zero;
        Image bImg = btnObj.AddComponent<Image>();
        Button btn = btnObj.AddComponent<Button>();

        GameObject bTxtObj = new GameObject("Text");
        bTxtObj.transform.SetParent(btnObj.transform, false);
        Text bText = bTxtObj.AddComponent<Text>();
        bText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bText.fontSize = 16; bText.fontStyle = FontStyle.Bold; bText.alignment = TextAnchor.MiddleCenter;
        RectTransform btr = bTxtObj.GetComponent<RectTransform>();
        btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one; btr.sizeDelta = Vector2.zero;

        bool isUnlocked = MetaProgression.IsShipUnlocked(chassis);
        bool isSelected = MetaProgression.GetSelectedShip() == chassis;
        int cost = MetaProgression.GetShipCost(chassis);
        int cores = MetaProgression.GetStarCores();

        string hexColor = ColorUtility.ToHtmlStringRGB(themeColor);
        iText.text = $"<size=17><b><color=#{hexColor}>{name}</color></b></size> <size=12><color=#88bbdd>[{subtitle}]</color></size>\n<size=12><color=#cccccc>{desc}</color></size>";

        if (isSelected)
        {
            bText.text = "EQUIPADO ★";
            bText.color = Color.black;
            bImg.color = new Color(0.1f, 0.9f, 0.45f);
            btn.interactable = false;
        }
        else if (isUnlocked)
        {
            bText.text = "EQUIPAR";
            bText.color = Color.black;
            bImg.color = new Color(0f, 0.8f, 1f);
            btn.interactable = true;
            btn.onClick.AddListener(() => {
                MetaProgression.SetSelectedShip(chassis);
                GameAudio.Play(AudioCue.Upgrade);
                if (GameManager.Instance != null && GameManager.Instance.player != null)
                {
                    MetaProgression.ApplyShipChassis(chassis, GameManager.Instance,
                        GameManager.Instance.player.GetComponent<PlayerMovement>(),
                        GameManager.Instance.player.GetComponent<Weapon>());
                }
                if (refreshAll != null) refreshAll();
            });
        }
        else
        {
            bText.text = $"DESBLOQUEAR ({cost} ★)";
            bool canAfford = cores >= cost;
            bText.color = canAfford ? Color.black : Color.white;
            bImg.color = canAfford ? new Color(1f, 0.75f, 0.1f) : new Color(0.35f, 0.2f, 0.2f);
            btn.interactable = canAfford;
            btn.onClick.AddListener(() => {
                if (MetaProgression.TryUnlockShip(chassis))
                {
                    GameAudio.Play(AudioCue.LevelUp);
                    if (GameManager.Instance != null && GameManager.Instance.player != null)
                    {
                        MetaProgression.ApplyShipChassis(chassis, GameManager.Instance,
                            GameManager.Instance.player.GetComponent<PlayerMovement>(),
                            GameManager.Instance.player.GetComponent<Weapon>());
                    }
                    if (refreshAll != null) refreshAll();
                }
            });
        }
    }

    static void CreateHangarRow(Transform parent, string key, string title, string desc, int maxLvl, int baseCost, System.Action onPurchased)
    {
        GameObject row = new GameObject("Row_" + key);
        row.transform.SetParent(parent, false);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 48; le.preferredHeight = 50;
        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.16f, 0.9f);

        GameObject info = new GameObject("Info");
        info.transform.SetParent(row.transform, false);
        RectTransform irt = info.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.02f, 0f); irt.anchorMax = new Vector2(0.7f, 1f);
        irt.sizeDelta = Vector2.zero; irt.anchoredPosition = Vector2.zero;
        Text iText = info.AddComponent<Text>();
        iText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iText.fontSize = 16; iText.alignment = TextAnchor.MiddleLeft;

        GameObject btnObj = new GameObject("BuyBtn");
        btnObj.transform.SetParent(row.transform, false);
        RectTransform brt = btnObj.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.72f, 0.1f); brt.anchorMax = new Vector2(0.98f, 0.9f);
        brt.sizeDelta = Vector2.zero; brt.anchoredPosition = Vector2.zero;
        Image bImg = btnObj.AddComponent<Image>();
        Button btn = btnObj.AddComponent<Button>();
        GameObject bTxtObj = new GameObject("Text");
        bTxtObj.transform.SetParent(btnObj.transform, false);
        Text bText = bTxtObj.AddComponent<Text>();
        bText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bText.fontSize = 16; bText.alignment = TextAnchor.MiddleCenter;
        RectTransform btr = bTxtObj.GetComponent<RectTransform>();
        btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one; btr.sizeDelta = Vector2.zero;

        System.Action updateDisplay = () => {
            int currentLvl = MetaProgression.GetUpgradeLevel(key);
            iText.text = $"<b>{title}</b> [NV {currentLvl}/{maxLvl}]\n<size=12><color=#aaaaaa>{desc}</color></size>";

            if (currentLvl >= maxLvl)
            {
                bText.text = "MAX";
                bText.color = Color.white;
                bImg.color = new Color(0.3f, 0.3f, 0.35f);
                btn.interactable = false;
            }
            else
            {
                int cost = MetaProgression.GetUpgradeCost(key, baseCost);
                bText.text = $"COMPRAR ({cost} ★)";
                bool canAfford = MetaProgression.GetStarCores() >= cost;
                bText.color = canAfford ? Color.black : Color.white;
                bImg.color = canAfford ? new Color(0f, 0.9f, 0.8f) : new Color(0.4f, 0.2f, 0.2f);
                btn.interactable = canAfford;
            }
        };

        btn.onClick.AddListener(() => {
            if (MetaProgression.TryPurchaseUpgrade(key, maxLvl, baseCost))
            {
                GameAudio.Play(AudioCue.Upgrade);
                updateDisplay();
                if (onPurchased != null) onPurchased();
            }
        });

        updateDisplay();
    }

    public static GameObject CreateStyledButton(Transform parent, string label, Color bgColor, Color textColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = textColor;
        txt.fontSize = 20;
        txt.fontStyle = FontStyle.Bold;

        RectTransform rtr = txtObj.GetComponent<RectTransform>();
        rtr.anchorMin = Vector2.zero; rtr.anchorMax = Vector2.one; rtr.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(onClick);
        return btnObj;
    }

    public static void BuildLevelUpUI(GameManager gm)
    {
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj == null) return;

        Transform existingPanel = canvasObj.transform.Find("LevelUpPanel");
        if (existingPanel != null) Object.Destroy(existingPanel.gameObject);

        GameObject panel = new GameObject("LevelUpPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero; panelRt.anchoredPosition = Vector2.zero;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.05f, 0.95f);

        // Titulo
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        Text titleTxt = title.AddComponent<Text>();
        titleTxt.text = "SISTEMA OTIMIZADO - ESCOLHA UM UPGRADE";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 36;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0f, 1f, 0.8f);
        RectTransform titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.85f); titleRt.anchorMax = new Vector2(0.5f, 0.96f);
        titleRt.sizeDelta = new Vector2(800, 70); titleRt.anchoredPosition = Vector2.zero;

        // Container Horizontal
        GameObject container = new GameObject("CardsContainer");
        container.transform.SetParent(panel.transform, false);
        RectTransform contRt = container.AddComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0.05f, 0.16f); contRt.anchorMax = new Vector2(0.95f, 0.84f);
        contRt.sizeDelta = Vector2.zero; contRt.anchoredPosition = Vector2.zero;
        
        HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 35;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlHeight = true; hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = true;

        gm.upgradeButtons = new Button[3];
        gm.upgradeTitles = new Text[3];
        gm.upgradeDescs = new Text[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject card = new GameObject("Card_" + i);
            card.transform.SetParent(container.transform, false);
            
            Image cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.06f, 0.07f, 0.12f, 0.96f); 
            
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 1f, 0.8f, 0.7f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            
            Button btn = card.AddComponent<Button>();
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.flexibleWidth = 1; le.flexibleHeight = 1; le.preferredWidth = 250; le.preferredHeight = 350;

            GameObject cIcon = new GameObject("Icon");
            cIcon.transform.SetParent(card.transform, false);
            Text ci = cIcon.AddComponent<Text>();
            ci.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ci.fontSize = 54;
            ci.alignment = TextAnchor.MiddleCenter;
            ci.color = Color.white;
            RectTransform cir = cIcon.GetComponent<RectTransform>();
            cir.anchorMin = new Vector2(0, 0.6f); cir.anchorMax = new Vector2(1, 0.95f);
            cir.offsetMin = Vector2.zero; cir.offsetMax = Vector2.zero;

            GameObject cTitle = new GameObject("Title");
            cTitle.transform.SetParent(card.transform, false);
            Text ct = cTitle.AddComponent<Text>();
            ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ct.fontSize = 22;
            ct.fontStyle = FontStyle.Bold;
            ct.alignment = TextAnchor.MiddleCenter;
            ct.color = new Color(0f, 1f, 0.8f);
            RectTransform ctr = cTitle.GetComponent<RectTransform>();
            ctr.anchorMin = new Vector2(0, 0.44f); ctr.anchorMax = new Vector2(1, 0.6f);
            ctr.offsetMin = new Vector2(10, 0); ctr.offsetMax = new Vector2(-10, 0);

            GameObject cDesc = new GameObject("Desc");
            cDesc.transform.SetParent(card.transform, false);
            Text cd = cDesc.AddComponent<Text>();
            cd.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cd.fontSize = 17;
            cd.alignment = TextAnchor.UpperCenter;
            cd.color = new Color(0.85f, 0.85f, 0.95f);
            RectTransform cdr = cDesc.GetComponent<RectTransform>();
            cdr.anchorMin = new Vector2(0, 0.05f); cdr.anchorMax = new Vector2(1, 0.42f);
            cdr.offsetMin = new Vector2(18, 0); cdr.offsetMax = new Vector2(-18, 0);

            gm.upgradeButtons[i] = btn;
            gm.upgradeTitles[i] = ct;
            gm.upgradeDescs[i] = cd;
            btn.name = i.ToString(); 
        }

        // Re-roll button at bottom left
        GameObject rerollObj = new GameObject("RerollBtn");
        rerollObj.transform.SetParent(panel.transform, false);
        RectTransform rrt = rerollObj.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.22f, 0.035f); rrt.anchorMax = new Vector2(0.48f, 0.11f);
        rrt.sizeDelta = Vector2.zero; rrt.anchoredPosition = Vector2.zero;
        Image rrImg = rerollObj.AddComponent<Image>(); rrImg.color = new Color(0.12f, 0.16f, 0.28f, 0.95f);
        Outline rrOutline = rerollObj.AddComponent<Outline>();
        rrOutline.effectColor = new Color(0.2f, 0.6f, 1f, 0.8f);
        Button rrBtn = rerollObj.AddComponent<Button>();

        GameObject rrTxtObj = new GameObject("Text");
        rrTxtObj.transform.SetParent(rerollObj.transform, false);
        Text rrTxt = rrTxtObj.AddComponent<Text>();
        rrTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rrTxt.text = "RE-ROLL (0)";
        rrTxt.fontSize = 17; rrTxt.fontStyle = FontStyle.Bold;
        rrTxt.alignment = TextAnchor.MiddleCenter; rrTxt.color = Color.white;
        RectTransform rrtr = rrTxtObj.GetComponent<RectTransform>();
        rrtr.anchorMin = Vector2.zero; rrtr.anchorMax = Vector2.one; rrtr.sizeDelta = Vector2.zero;

        rrBtn.onClick.AddListener(() => {
            if (gm.availableRerolls > 0)
            {
                gm.availableRerolls--;
                GameAudio.Play(AudioCue.Upgrade);
                gm.ShowLevelUpScreen();
            }
        });

        // Synergy Codex button at bottom right
        GameObject codexBtnObj = new GameObject("CodexBtn");
        codexBtnObj.transform.SetParent(panel.transform, false);
        RectTransform crt = codexBtnObj.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.52f, 0.035f); crt.anchorMax = new Vector2(0.78f, 0.11f);
        crt.sizeDelta = Vector2.zero; crt.anchoredPosition = Vector2.zero;
        Image cdImg = codexBtnObj.AddComponent<Image>(); cdImg.color = new Color(0.18f, 0.14f, 0.04f, 0.95f);
        Outline cdOutline = codexBtnObj.AddComponent<Outline>();
        cdOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        Button cdBtn = codexBtnObj.AddComponent<Button>();

        GameObject cdTxtObj = new GameObject("Text");
        cdTxtObj.transform.SetParent(codexBtnObj.transform, false);
        Text cdTxt = cdTxtObj.AddComponent<Text>();
        cdTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cdTxt.text = "📖 CÓDEX DE SINERGIAS";
        cdTxt.fontSize = 17; cdTxt.fontStyle = FontStyle.Bold;
        cdTxt.alignment = TextAnchor.MiddleCenter; cdTxt.color = new Color(1f, 0.9f, 0.2f);
        RectTransform cdtr = cdTxtObj.GetComponent<RectTransform>();
        cdtr.anchorMin = Vector2.zero; cdtr.anchorMax = Vector2.one; cdtr.sizeDelta = Vector2.zero;

        cdBtn.onClick.AddListener(() => {
            BuildSynergyCodexUI(canvasObj.transform);
        });

        gm.levelUpPanel = panel;
        panel.SetActive(false);
    }

    public static GameObject pauseMenuPanel;
    private static Button[] modeButtons;
    private static Image[] modeBackgrounds;
    private static Outline[] modeOutlines;

    public static GameObject BuildPauseMenu(GameManager gm, System.Action onResume)
    {
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj == null) return null;

        Transform existing = canvasObj.transform.Find("PauseMenuPanel");
        if (existing != null) Object.Destroy(existing.gameObject);

        GameObject panel = new GameObject("PauseMenuPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.12f, 0.08f);
        panelRt.anchorMax = new Vector2(0.88f, 0.92f);
        panelRt.sizeDelta = Vector2.zero;
        panelRt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.04f, 0.09f, 0.97f);
        Outline outl = panel.AddComponent<Outline>();
        outl.effectColor = new Color(0.2f, 0.7f, 1f, 0.85f);
        outl.effectDistance = new Vector2(3, -3);

        // Header
        GameObject header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        RectTransform hrt = header.AddComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 0.86f); hrt.anchorMax = new Vector2(1f, 0.98f);
        hrt.sizeDelta = Vector2.zero; hrt.anchoredPosition = Vector2.zero;
        Text hText = header.AddComponent<Text>();
        hText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hText.text = "PAUSA / CONFIGURAÇÕES";
        hText.fontSize = 28;
        hText.fontStyle = FontStyle.Bold;
        hText.alignment = TextAnchor.MiddleCenter;
        hText.color = new Color(0f, 0.95f, 1f);

        // Subtitle / Section title
        GameObject sectionObj = new GameObject("SectionTitle");
        sectionObj.transform.SetParent(panel.transform, false);
        RectTransform srt = sectionObj.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.05f, 0.76f); srt.anchorMax = new Vector2(0.95f, 0.85f);
        srt.sizeDelta = Vector2.zero; srt.anchoredPosition = Vector2.zero;
        Text sText = sectionObj.AddComponent<Text>();
        sText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sText.text = "MODO DE CONTROLE (TECLADO & MOUSE)";
        sText.fontSize = 18;
        sText.fontStyle = FontStyle.Bold;
        sText.alignment = TextAnchor.MiddleCenter;
        sText.color = new Color(0.85f, 0.9f, 1f);

        // Options Container (Horizontal)
        GameObject optionsCont = new GameObject("OptionsContainer");
        optionsCont.transform.SetParent(panel.transform, false);
        RectTransform ort = optionsCont.AddComponent<RectTransform>();
        ort.anchorMin = new Vector2(0.04f, 0.28f); ort.anchorMax = new Vector2(0.96f, 0.74f);
        ort.sizeDelta = Vector2.zero; ort.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup hlg = optionsCont.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 18;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        modeButtons = new Button[3];
        modeBackgrounds = new Image[3];
        modeOutlines = new Outline[3];

        for (int i = 0; i < 3; i++)
        {
            int modeIdx = i;
            GameObject optCard = new GameObject("ModeCard_" + i);
            optCard.transform.SetParent(optionsCont.transform, false);

            Image cardBg = optCard.AddComponent<Image>();
            Outline cardOutl = optCard.AddComponent<Outline>();
            Button btn = optCard.AddComponent<Button>();

            modeButtons[i] = btn;
            modeBackgrounds[i] = cardBg;
            modeOutlines[i] = cardOutl;

            // Text inside card
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(optCard.transform, false);
            RectTransform trt = txtObj.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.05f, 0.05f); trt.anchorMax = new Vector2(0.95f, 0.95f);
            trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;

            btn.onClick.AddListener(() => {
                PlayerMovement.SetControlMode((ControlMode)modeIdx);
                GameAudio.Play(AudioCue.Upgrade);
                RefreshPauseMenuHighlights();
            });
        }

        RefreshPauseMenuHighlights();

        // Bottom Action Buttons Container
        GameObject btnCont = new GameObject("ActionButtons");
        btnCont.transform.SetParent(panel.transform, false);
        RectTransform brt = btnCont.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.06f, 0.08f); brt.anchorMax = new Vector2(0.94f, 0.22f);
        brt.sizeDelta = Vector2.zero; brt.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup bhlg = btnCont.AddComponent<HorizontalLayoutGroup>();
        bhlg.spacing = 20;
        bhlg.childForceExpandWidth = true; bhlg.childForceExpandHeight = true;

        CreateStyledButton(btnCont.transform, "CONTINUAR [ESC]", new Color(0.15f, 0.7f, 0.35f), Color.black, () => {
            if (onResume != null) onResume();
        });

        CreateStyledButton(btnCont.transform, "📖 CÓDEX", new Color(0.85f, 0.65f, 0.1f), Color.black, () => {
            BuildSynergyCodexUI(canvasObj.transform);
        });

        CreateStyledButton(btnCont.transform, "HANGAR", new Color(0.15f, 0.55f, 0.95f), Color.white, () => {
            BuildHangarUI(canvasObj.transform);
        });

        CreateStyledButton(btnCont.transform, "REINICIAR", new Color(0.9f, 0.5f, 0.1f), Color.black, () => {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });

        CreateStyledButton(btnCont.transform, "SAIR", new Color(0.45f, 0.45f, 0.55f), Color.white, () => {
            Application.Quit();
        });

        pauseMenuPanel = panel;
        return panel;
    }

    public static void RefreshPauseMenuHighlights()
    {
        if (modeButtons == null) return;
        ControlMode current = PlayerMovement.currentControlMode;

        string[] titles = {
            "PADRÃO (WASD)",
            "STRAFE (TWIN-STICK)",
            "GUIA 360° PELO MOUSE"
        };

        string[] descs = {
            "<b>Modo Clássico Original:</b>\n\n• WASD move a nave pela tela.\n• A nave vira para onde anda.\n• O mouse mira os tiros.",
            "<b>Modo Twin-Stick Shooter:</b>\n\n• WASD move a nave livremente.\n• A proa mira sempre no mouse.\n• Permite strafe 360° total.",
            "<b>Direção 360° Contínua:</b>\n\n• O mouse define a direção 360°.\n• A tecla W caminha até o cursor.\n• Permite curvas e navegação fluida."
        };

        for (int i = 0; i < modeButtons.Length; i++)
        {
            if (modeButtons[i] == null) continue;
            bool isSelected = ((int)current == i);

            Text txt = modeButtons[i].GetComponentInChildren<Text>();
            if (txt != null)
            {
                string status = isSelected ? "<color=#00ffcc><b>[ ATIVO ]</b></color>\n\n" : "<color=#888888>[ SELECIONAR ]</color>\n\n";
                string titleColor = isSelected ? "#00ffff" : "#cccccc";
                txt.text = $"{status}<size=18><color={titleColor}><b>{titles[i]}</b></color></size>\n\n<size=13><color=#dddddd>{descs[i]}</color></size>";
            }

            if (modeBackgrounds[i] != null)
            {
                modeBackgrounds[i].color = isSelected ? new Color(0.08f, 0.16f, 0.26f, 0.98f) : new Color(0.06f, 0.08f, 0.12f, 0.9f);
            }

            if (modeOutlines[i] != null)
            {
                modeOutlines[i].effectColor = isSelected ? new Color(0f, 1f, 0.85f, 1f) : new Color(0.2f, 0.25f, 0.35f, 0.6f);
                modeOutlines[i].effectDistance = isSelected ? new Vector2(3f, -3f) : new Vector2(1.5f, -1.5f);
            }
        }
    }

    public static GameObject BuildSynergyCodexUI(Transform canvasTransform, System.Action onClose = null)
    {
        if (canvasTransform == null) return null;

        Transform existing = canvasTransform.Find("SynergyCodexPanel");
        if (existing != null) Object.Destroy(existing.gameObject);

        GameObject panel = new GameObject("SynergyCodexPanel");
        panel.transform.SetParent(canvasTransform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.04f, 0.03f);
        panelRt.anchorMax = new Vector2(0.96f, 0.97f);
        panelRt.sizeDelta = Vector2.zero;
        panelRt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.04f, 0.08f, 0.98f);
        Outline outl = panel.AddComponent<Outline>();
        outl.effectColor = new Color(1f, 0.85f, 0.15f, 0.9f);
        outl.effectDistance = new Vector2(3.5f, -3.5f);

        // Header Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform trt = titleObj.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.88f); trt.anchorMax = new Vector2(1f, 0.98f);
        trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
        Text tText = titleObj.AddComponent<Text>();
        tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tText.text = "📖 CÓDEX DE SINERGIAS & EVOLUÇÕES LENDÁRIAS";
        tText.fontSize = 28;
        tText.fontStyle = FontStyle.Bold;
        tText.alignment = TextAnchor.MiddleCenter;
        tText.color = new Color(1f, 0.85f, 0.15f);

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(panel.transform, false);
        RectTransform srt = subObj.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0.82f); srt.anchorMax = new Vector2(1f, 0.88f);
        srt.sizeDelta = Vector2.zero; srt.anchoredPosition = Vector2.zero;
        Text sText = subObj.AddComponent<Text>();
        sText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sText.text = "Maximize uma arma ou habilidade e combine com o upgrade sinérgico para forjar a Evolução Suprema!";
        sText.fontSize = 15;
        sText.alignment = TextAnchor.MiddleCenter;
        sText.color = new Color(0.75f, 0.85f, 1f);

        // Grid Container (2 columns x 3 rows)
        GameObject gridObj = new GameObject("GridContainer");
        gridObj.transform.SetParent(panel.transform, false);
        RectTransform grt = gridObj.AddComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.04f, 0.11f);
        grt.anchorMax = new Vector2(0.96f, 0.81f);
        grt.sizeDelta = Vector2.zero;
        grt.anchoredPosition = Vector2.zero;

        GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
        glg.spacing = new Vector2(25, 14);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.cellSize = new Vector2(520, 140);
        glg.childAlignment = TextAnchor.MiddleCenter;

        GameManager gm = GameManager.Instance;

        // 1. Supernova Gatling
        CreateCodexCard(gridObj.transform, gm,
            "☀️ SUPERNOVA GATLING",
            "Blaster (Tiro Rápido Nv 5) + Sobrecarga Crítica (Nv 1+)",
            "Disparos dourados contínuos em cadência máxima com micro-explosões solares em área ao impactar.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.SupernovaGatling) >= 1,
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.FireRate) >= 5 && gm.GetUpgradeLevel(GameManager.UpgradeType.Critical) >= 1,
            $"Tiro Rápido ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.FireRate) : 0)}/5) + Crítico ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.Critical) : 0)}/1)"
        );

        // 2. Canhão Nebular
        CreateCodexCard(gridObj.transform, gm,
            "🌌 CANHÃO NEBULAR (NEBULA FLAK)",
            "Shotgun (Tiro Múltiplo Nv 4) + Ricochete Cósmico (Nv 1+)",
            "Dispara 8 fragmentos que ricocheteiam e explodem em múltiplos estilhaços cósmicos secundários.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.NebulaFlak) >= 1,
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.Spread) >= 4 && gm.GetUpgradeLevel(GameManager.UpgradeType.Bounce) >= 1,
            $"Tiro Múltiplo ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.Spread) : 0)}/4) + Ricochete ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.Bounce) : 0)}/1)"
        );

        // 3. Lança de Anti-Matéria
        CreateCodexCard(gridObj.transform, gm,
            "⚡ LANÇA DE ANTI-MATÉRIA",
            "Railgun (Perfurante Nv 3) + Sobrecarga de Dano (Nv 3+)",
            "Raio instantâneo hiper-perfurante de anti-matéria que deixa poças de radiação contínua no planeta.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.AntimatterLance) >= 1,
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.Pierce) >= 3 && gm.GetUpgradeLevel(GameManager.UpgradeType.Damage) >= 3,
            $"Perfurante ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.Pierce) : 0)}/3) + Dano ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.Damage) : 0)}/3)"
        );

        // 4. Vórtice de Singularidade
        CreateCodexCard(gridObj.transform, gm,
            "🕳️ VÓRTICE DE SINGULARIDADE",
            "Minas de Matéria Escura (Nv 3) + Munição Explosiva (Nv 1+)",
            "Minas colapsam em buracos negros que sugam inimigos para o centro antes de implodir causando 120 de dano.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.VoidVortex) >= 1,
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.OrbitalMines) >= 3 && gm.GetUpgradeLevel(GameManager.UpgradeType.Explosive) >= 1,
            $"Minas ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.OrbitalMines) : 0)}/3) + Explosiva ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.Explosive) : 0)}/1)"
        );

        // 5. Rede Neural Tesla
        CreateCodexCard(gridObj.transform, gm,
            "🛸 REDE NEURAL TESLA",
            "Drones Sentinelas (Nv 3) + Perfurante ou Ricochete (Nv 1+)",
            "Drones disparam rajadas elétricas com arcos secundários saltando em cascata entre até 4 inimigos.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.TeslaChain) >= 1,
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.SentinelDrone) >= 3 && (gm.GetUpgradeLevel(GameManager.UpgradeType.Pierce) >= 1 || gm.GetUpgradeLevel(GameManager.UpgradeType.Bounce) >= 1),
            $"Drones ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.SentinelDrone) : 0)}/3) + Perfurante ou Ricochete (Nv 1+)"
        );

        // 6. Barreira Hiperiônica
        CreateCodexCard(gridObj.transform, gm,
            "🛡️ BARREIRA HIPERIÔNICA",
            "Escudo Aegis (Nv 3) + Nanites Vampíricos (Nv 1+)",
            "Ao quebrar ou recarregar, emite uma devastadora onda de choque dourada que repele e esmaga alvos (60 dano).",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.HyperionBarrier) >= 1,
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.AegisShield) >= 3 && gm.GetUpgradeLevel(GameManager.UpgradeType.LifeSteal) >= 1,
            $"Escudo Aegis ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.AegisShield) : 0)}/3) + Nanites ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.LifeSteal) : 0)}/1)"
        );

        // Close Button
        GameObject closeBtnObj = CreateStyledButton(panel.transform, "VOLTAR / FECHAR", new Color(0.85f, 0.25f, 0.35f), Color.white, () => {
            Object.Destroy(panel);
            if (onClose != null) onClose();
        });
        RectTransform cbrt = closeBtnObj.GetComponent<RectTransform>();
        cbrt.anchorMin = new Vector2(0.4f, 0.025f);
        cbrt.anchorMax = new Vector2(0.6f, 0.085f);
        cbrt.sizeDelta = Vector2.zero;
        cbrt.anchoredPosition = Vector2.zero;

        return panel;
    }

    private static void CreateCodexCard(Transform parent, GameManager gm, string name, string recipe, string effect, bool isEvolved, bool isReady, string progressText)
    {
        GameObject card = new GameObject("CodexCard_" + name);
        card.transform.SetParent(parent, false);

        Image img = card.AddComponent<Image>();
        Outline outline = card.AddComponent<Outline>();

        string statusTag;
        Color statusBorder;
        Color statusBg;

        if (isEvolved)
        {
            statusTag = "<color=#ffd700><b>✔ EVOLUÇÃO ATIVA</b></color>";
            statusBorder = new Color(1f, 0.85f, 0.2f, 1f);
            statusBg = new Color(0.14f, 0.10f, 0.02f, 0.95f);
        }
        else if (isReady)
        {
            statusTag = "<color=#00ffcc><b>★ PRONTO PARA EVOLUIR NO PRÓXIMO NÍVEL!</b></color>";
            statusBorder = new Color(0f, 1f, 0.8f, 1f);
            statusBg = new Color(0.04f, 0.15f, 0.13f, 0.95f);
        }
        else
        {
            statusTag = $"<color=#ff6b6b>[BLOQUEADO]</color> <color=#8899aa>{progressText}</color>";
            statusBorder = new Color(0.2f, 0.3f, 0.45f, 0.8f);
            statusBg = new Color(0.05f, 0.07f, 0.11f, 0.92f);
        }

        img.color = statusBg;
        outline.effectColor = statusBorder;
        outline.effectDistance = isEvolved ? new Vector2(3f, -3f) : new Vector2(2f, -2f);

        GameObject txtObj = new GameObject("CardText");
        txtObj.transform.SetParent(card.transform, false);
        RectTransform trt = txtObj.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.04f, 0.04f);
        trt.anchorMax = new Vector2(0.96f, 0.96f);
        trt.sizeDelta = Vector2.zero;
        trt.anchoredPosition = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color = Color.white;
        string nameCol = isEvolved ? "#ffd700" : (isReady ? "#00ffff" : "#e0e6ed");
        txt.text = $"{statusTag}\n" +
                   $"<size=17><color={nameCol}><b>{name}</b></color></size>\n" +
                   $"<size=13><color=#ffd280><b>Receita:</b></color> <color=#ffffff>{recipe}</color></size>\n" +
                   $"<size=11><color=#b0c0d0>{effect}</color></size>";
    }
}
