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
        bossText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        bossText.text = "LEVIATA ORBITAL - GUARDIÃO DO PLANETA";
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

    public static void BuildVictoryUI(GameManager gm)
    {
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj == null) return;

        Time.timeScale = 0f;

        GameObject panel = new GameObject("VictoryPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero; panelRt.anchoredPosition = Vector2.zero;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.15f, 0.08f, 0.94f); // Verde neon escuro vitorioso

        // Titulo
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        RectTransform trt = title.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.65f); trt.anchorMax = new Vector2(1f, 0.9f);
        trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
        Text tText = title.AddComponent<Text>();
        tText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        tText.text = "PLANETA PURIFICADO!\nO Leviata Foi Derrotado";
        tText.fontSize = 38; tText.alignment = TextAnchor.MiddleCenter; tText.color = new Color(0.2f, 1f, 0.5f);

        // Stats
        GameObject stats = new GameObject("Stats");
        stats.transform.SetParent(panel.transform, false);
        RectTransform srt = stats.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0.4f); srt.anchorMax = new Vector2(1f, 0.6f);
        srt.sizeDelta = Vector2.zero; srt.anchoredPosition = Vector2.zero;
        Text sText = stats.AddComponent<Text>();
        sText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        int minutes = Mathf.FloorToInt(gm.matchTime / 60F);
        int seconds = Mathf.FloorToInt(gm.matchTime - minutes * 60);
        sText.text = "Nivel Final: " + gm.level + "\nTempo de Conquista: " + string.Format("{0:00}:{1:00}", minutes, seconds);
        sText.fontSize = 26; sText.alignment = TextAnchor.MiddleCenter; sText.color = Color.white;

        // Botoes
        GameObject btnCont = new GameObject("Buttons");
        btnCont.transform.SetParent(panel.transform, false);
        RectTransform brt = btnCont.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.2f, 0.15f); brt.anchorMax = new Vector2(0.8f, 0.3f);
        brt.sizeDelta = Vector2.zero; brt.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup hlg = btnCont.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 40; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        // Continuar / Reiniciar
        GameObject playAgainBtn = new GameObject("PlayAgainBtn");
        playAgainBtn.transform.SetParent(btnCont.transform, false);
        Image rImg = playAgainBtn.AddComponent<Image>(); rImg.color = new Color(0.2f, 0.8f, 0.4f);
        Button rBtn = playAgainBtn.AddComponent<Button>();
        GameObject rTxtObj = new GameObject("Text"); rTxtObj.transform.SetParent(playAgainBtn.transform, false);
        Text rTxt = rTxtObj.AddComponent<Text>(); rTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        rTxt.text = "JOGAR NOVAMENTE"; rTxt.alignment = TextAnchor.MiddleCenter; rTxt.color = Color.black; rTxt.fontSize = 22;
        RectTransform rtr = rTxtObj.GetComponent<RectTransform>(); rtr.anchorMin = Vector2.zero; rtr.anchorMax = Vector2.one; rtr.sizeDelta = Vector2.zero;
        rBtn.onClick.AddListener(() => { 
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); 
        });

        // Sair
        GameObject quitBtn = new GameObject("QuitBtn");
        quitBtn.transform.SetParent(btnCont.transform, false);
        Image qImg = quitBtn.AddComponent<Image>(); qImg.color = new Color(0.5f, 0.5f, 0.5f);
        Button qBtn = quitBtn.AddComponent<Button>();
        GameObject qTxtObj = new GameObject("Text"); qTxtObj.transform.SetParent(quitBtn.transform, false);
        Text qTxt = qTxtObj.AddComponent<Text>(); qTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        qTxt.text = "SAIR"; qTxt.alignment = TextAnchor.MiddleCenter; qTxt.color = Color.white; qTxt.fontSize = 22;
        RectTransform qtr = qTxtObj.GetComponent<RectTransform>(); qtr.anchorMin = Vector2.zero; qtr.anchorMax = Vector2.one; qtr.sizeDelta = Vector2.zero;
        qBtn.onClick.AddListener(() => { Application.Quit(); });
    }

    public static void BuildGameOverUI(GameManager gm)
    {
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj == null) return;

        GameObject panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero; panelRt.anchoredPosition = Vector2.zero;
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0f, 0f, 0.9f); // Vermelho escuro

        // Titulo
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        RectTransform trt = title.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.7f); trt.anchorMax = new Vector2(1f, 0.9f);
        trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
        Text tText = title.AddComponent<Text>();
        tText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        tText.text = "SISTEMAS CRITICOS FALHARAM\nNave Destruida";
        tText.fontSize = 40; tText.alignment = TextAnchor.MiddleCenter; tText.color = new Color(1f, 0.2f, 0.2f);

        // Stats
        GameObject stats = new GameObject("Stats");
        stats.transform.SetParent(panel.transform, false);
        RectTransform srt = stats.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0.4f); srt.anchorMax = new Vector2(1f, 0.6f);
        srt.sizeDelta = Vector2.zero; srt.anchoredPosition = Vector2.zero;
        Text sText = stats.AddComponent<Text>();
        sText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        int minutes = Mathf.FloorToInt(gm.matchTime / 60F);
        int seconds = Mathf.FloorToInt(gm.matchTime - minutes * 60);
        sText.text = "Nivel Alcancado: " + gm.level + "\nTempo Sobrevivido: " + string.Format("{0:00}:{1:00}", minutes, seconds);
        sText.fontSize = 28; sText.alignment = TextAnchor.MiddleCenter; sText.color = Color.white;

        // Botoes Container
        GameObject btnCont = new GameObject("Buttons");
        btnCont.transform.SetParent(panel.transform, false);
        RectTransform brt = btnCont.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.2f, 0.1f); brt.anchorMax = new Vector2(0.8f, 0.3f);
        brt.sizeDelta = Vector2.zero; brt.anchoredPosition = Vector2.zero;
        HorizontalLayoutGroup hlg = btnCont.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 50; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        // Restart
        GameObject restartBtn = new GameObject("RestartBtn");
        restartBtn.transform.SetParent(btnCont.transform, false);
        Image rImg = restartBtn.AddComponent<Image>(); rImg.color = new Color(0.2f, 0.8f, 0.2f);
        Button rBtn = restartBtn.AddComponent<Button>();
        GameObject rTxtObj = new GameObject("Text"); rTxtObj.transform.SetParent(restartBtn.transform, false);
        Text rTxt = rTxtObj.AddComponent<Text>(); rTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        rTxt.text = "REINICIAR"; rTxt.alignment = TextAnchor.MiddleCenter; rTxt.color = Color.black; rTxt.fontSize = 24;
        RectTransform rtr = rTxtObj.GetComponent<RectTransform>(); rtr.anchorMin = Vector2.zero; rtr.anchorMax = Vector2.one; rtr.sizeDelta = Vector2.zero;
        rBtn.onClick.AddListener(() => { UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); });

        // Quit
        GameObject quitBtn = new GameObject("QuitBtn");
        quitBtn.transform.SetParent(btnCont.transform, false);
        Image qImg = quitBtn.AddComponent<Image>(); qImg.color = new Color(0.8f, 0.2f, 0.2f);
        Button qBtn = quitBtn.AddComponent<Button>();
        GameObject qTxtObj = new GameObject("Text"); qTxtObj.transform.SetParent(quitBtn.transform, false);
        Text qTxt = qTxtObj.AddComponent<Text>(); qTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        qTxt.text = "SAIR"; qTxt.alignment = TextAnchor.MiddleCenter; qTxt.color = Color.black; qTxt.fontSize = 24;
        RectTransform qtr = qTxtObj.GetComponent<RectTransform>(); qtr.anchorMin = Vector2.zero; qtr.anchorMax = Vector2.one; qtr.sizeDelta = Vector2.zero;
        qBtn.onClick.AddListener(() => { Application.Quit(); });
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
        titleTxt.text = "SISTEMA OTIMIZADO";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 40;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0f, 1f, 0.8f);
        RectTransform titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.85f); titleRt.anchorMax = new Vector2(0.5f, 0.95f);
        titleRt.sizeDelta = new Vector2(600, 80); titleRt.anchoredPosition = Vector2.zero;

        // Container Horizontal
        GameObject container = new GameObject("CardsContainer");
        container.transform.SetParent(panel.transform, false);
        RectTransform contRt = container.AddComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0.05f, 0.15f); contRt.anchorMax = new Vector2(0.95f, 0.85f);
        contRt.sizeDelta = Vector2.zero; contRt.anchoredPosition = Vector2.zero;
        
        HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 40;
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
            cardImg.color = new Color(0.1f, 0.1f, 0.15f, 1f); 
            
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 1f, 0.8f, 0.5f);
            outline.effectDistance = new Vector2(2, -2);
            
            Button btn = card.AddComponent<Button>();
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.flexibleWidth = 1; le.flexibleHeight = 1; le.preferredWidth = 250; le.preferredHeight = 350;

            GameObject cIcon = new GameObject("Icon");
            cIcon.transform.SetParent(card.transform, false);
            Text ci = cIcon.AddComponent<Text>();
            ci.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ci.fontSize = 60;
            ci.alignment = TextAnchor.MiddleCenter;
            ci.color = Color.white;
            RectTransform cir = cIcon.GetComponent<RectTransform>();
            cir.anchorMin = new Vector2(0, 0.6f); cir.anchorMax = new Vector2(1, 0.95f);
            cir.offsetMin = Vector2.zero; cir.offsetMax = Vector2.zero;

            GameObject cTitle = new GameObject("Title");
            cTitle.transform.SetParent(card.transform, false);
            Text ct = cTitle.AddComponent<Text>();
            ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ct.fontSize = 24;
            ct.fontStyle = FontStyle.Bold;
            ct.alignment = TextAnchor.MiddleCenter;
            ct.color = new Color(0f, 1f, 0.8f);
            RectTransform ctr = cTitle.GetComponent<RectTransform>();
            ctr.anchorMin = new Vector2(0, 0.45f); ctr.anchorMax = new Vector2(1, 0.6f);
            ctr.offsetMin = new Vector2(10, 0); ctr.offsetMax = new Vector2(-10, 0);

            GameObject cDesc = new GameObject("Desc");
            cDesc.transform.SetParent(card.transform, false);
            Text cd = cDesc.AddComponent<Text>();
            cd.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cd.fontSize = 18;
            cd.alignment = TextAnchor.UpperCenter;
            cd.color = new Color(0.8f, 0.8f, 0.9f);
            RectTransform cdr = cDesc.GetComponent<RectTransform>();
            cdr.anchorMin = new Vector2(0, 0.05f); cdr.anchorMax = new Vector2(1, 0.4f);
            cdr.offsetMin = new Vector2(20, 0); cdr.offsetMax = new Vector2(-20, 0);

            gm.upgradeButtons[i] = btn;
            gm.upgradeTitles[i] = ct;
            gm.upgradeDescs[i] = cd;
            
            btn.name = i.ToString(); 
        }

        gm.levelUpPanel = panel;
        panel.SetActive(false);
    }
}
