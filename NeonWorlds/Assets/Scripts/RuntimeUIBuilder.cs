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
                break;
        }

        if (outline != null)
        {
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2.5f, -2.5f);
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
        panelRt.anchorMin = new Vector2(0.08f, 0.08f); panelRt.anchorMax = new Vector2(0.92f, 0.92f);
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
        hrt.anchorMin = new Vector2(0f, 0.86f); hrt.anchorMax = new Vector2(1f, 0.98f);
        hrt.sizeDelta = Vector2.zero; hrt.anchoredPosition = Vector2.zero;
        Text hText = header.AddComponent<Text>();
        hText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hText.text = "HANGAR ESTELAR - MELHORIAS PERMANENTES";
        hText.fontSize = 28; hText.alignment = TextAnchor.MiddleCenter; hText.color = new Color(0f, 0.9f, 1f);

        // Currency Display
        GameObject currency = new GameObject("CurrencyText");
        currency.transform.SetParent(panel.transform, false);
        RectTransform crt = currency.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.05f, 0.78f); crt.anchorMax = new Vector2(0.95f, 0.85f);
        crt.sizeDelta = Vector2.zero; crt.anchoredPosition = Vector2.zero;
        Text cText = currency.AddComponent<Text>();
        cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cText.fontSize = 22; cText.alignment = TextAnchor.MiddleCenter;
        System.Action refreshCurrency = () => {
            cText.text = $"<color=#00ffcc>★ Células Estelares Disponíveis: {MetaProgression.GetStarCores()}</color>";
        };
        refreshCurrency();

        // Items List
        GameObject listCont = new GameObject("UpgradeList");
        listCont.transform.SetParent(panel.transform, false);
        RectTransform lrt = listCont.AddComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.05f, 0.18f); lrt.anchorMax = new Vector2(0.95f, 0.76f);
        lrt.sizeDelta = Vector2.zero; lrt.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup vlg = listCont.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10; vlg.childControlHeight = true; vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        CreateHangarRow(listCont.transform, MetaProgression.UP_HULL, "Blindagem de Titânio", "+15 HP Máximo inicial por nível", 5, 12, refreshCurrency);
        CreateHangarRow(listCont.transform, MetaProgression.UP_SPEED, "Propulsores Iônicos", "+5% Velocidade de movimento por nível", 5, 12, refreshCurrency);
        CreateHangarRow(listCont.transform, MetaProgression.UP_DAMAGE, "Condensador de Plasma", "+10% Dano global por nível", 5, 16, refreshCurrency);
        CreateHangarRow(listCont.transform, MetaProgression.UP_MAGNET, "Coletor Gravitacional", "+2m Raio do ímã de gemas por nível", 5, 10, refreshCurrency);
        CreateHangarRow(listCont.transform, MetaProgression.UP_REROLL, "Módulo de Re-roll", "+1 Troca de opções por partida", 2, 25, refreshCurrency);

        // Close Button
        GameObject closeBtn = CreateStyledButton(panel.transform, "VOLTAR / FECHAR", new Color(0.8f, 0.2f, 0.3f), Color.white, () => {
            Object.Destroy(panel);
        });
        RectTransform cbrt = closeBtn.GetComponent<RectTransform>();
        cbrt.anchorMin = new Vector2(0.35f, 0.04f); cbrt.anchorMax = new Vector2(0.65f, 0.12f);
        cbrt.sizeDelta = Vector2.zero; cbrt.anchoredPosition = Vector2.zero;
    }

    static void CreateHangarRow(Transform parent, string key, string title, string desc, int maxLvl, int baseCost, System.Action onPurchased)
    {
        GameObject row = new GameObject("Row_" + key);
        row.transform.SetParent(parent, false);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 50; le.preferredHeight = 52;
        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.16f, 0.9f);

        GameObject info = new GameObject("Info");
        info.transform.SetParent(row.transform, false);
        RectTransform irt = info.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.02f, 0f); irt.anchorMax = new Vector2(0.7f, 1f);
        irt.sizeDelta = Vector2.zero; irt.anchoredPosition = Vector2.zero;
        Text iText = info.AddComponent<Text>();
        iText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iText.fontSize = 17; iText.alignment = TextAnchor.MiddleLeft;

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
        bText.fontSize = 17; bText.alignment = TextAnchor.MiddleCenter;
        RectTransform btr = bTxtObj.GetComponent<RectTransform>();
        btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one; btr.sizeDelta = Vector2.zero;

        System.Action updateDisplay = () => {
            int currentLvl = MetaProgression.GetUpgradeLevel(key);
            iText.text = $"<b>{title}</b> [NV {currentLvl}/{maxLvl}]\n<size=13><color=#aaaaaa>{desc}</color></size>";

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

        // Re-roll button at bottom
        GameObject rerollObj = new GameObject("RerollBtn");
        rerollObj.transform.SetParent(panel.transform, false);
        RectTransform rrt = rerollObj.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.4f, 0.035f); rrt.anchorMax = new Vector2(0.6f, 0.11f);
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
        rrTxt.fontSize = 18; rrTxt.fontStyle = FontStyle.Bold;
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

        gm.levelUpPanel = panel;
        panel.SetActive(false);
    }
}
