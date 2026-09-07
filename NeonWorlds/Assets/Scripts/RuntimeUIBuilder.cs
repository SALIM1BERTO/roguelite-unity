using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeUIBuilder
{

    static GameObject endScreen;
    public static bool HasEndScreen => endScreen!=null && endScreen.activeSelf;

    public static void StyleUpgrade(GameManager game,int index,GameManager.UpgradeType kind)
    {
        Transform card=game.upgradeButtons[index].transform;
        string title=game.upgradeTitles[index].text;
        title=System.Text.RegularExpressions.Regex.Replace(title,"<[^>]*>","").Split('\n')[0];
        title=System.Text.RegularExpressions.Regex.Replace(title,@" \[NV .*?\]","");
        game.upgradeTitles[index].text=title;
        int maximum=game.GetMaxLevel(kind);
        card.Find("Select").GetComponent<Text>().text=maximum<90 ? "NÍVEL "+(game.GetUpgradeLevel(kind)+1)+" / "+maximum : "SELECIONAR";
        bool offense=kind==GameManager.UpgradeType.Spread || kind==GameManager.UpgradeType.FireRate || kind==GameManager.UpgradeType.Pierce || kind==GameManager.UpgradeType.Bounce || kind==GameManager.UpgradeType.Explosive;
        Color accent=offense ? NeonUI.Cyan : kind==GameManager.UpgradeType.Heal ? new Color(.51f,.94f,.73f) : NeonUI.Violet;
        card.Find("Category").GetComponent<Text>().text=(index+1).ToString("00")+"  /  "+(offense ? "ARSENAL" : kind==GameManager.UpgradeType.Heal ? "SOBREVIVÊNCIA" : "MOBILIDADE");
        card.Find("Accent").GetComponent<Image>().color=accent;
        UpgradeGlyph glyph=card.Find("Glyph").GetComponent<UpgradeGlyph>(); glyph.kind=kind; glyph.color=accent; glyph.SetVerticesDirty();
        StyleUpgradeCard(game.upgradeButtons[index],game.GetRarity(kind));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { bossBarObj=null; endScreen=null; bossFillImg=null; bossText=null; }

    static void BuildEndScreen(GameManager game,bool victory)
    {
        if(HasEndScreen) return;
        int earnedCores=victory ? 100+game.level*5 : Mathf.Max(5,game.level*3+(int)(game.matchTime/10f));
        MetaProgression.AddStarCores(earnedCores);
        var canvas=GameObject.Find("CanvasHUD");
        if(canvas==null) { ModernHUD.Create(game); canvas=GameObject.Find("CanvasHUD"); }
        if(game.levelUpPanel!=null) game.levelUpPanel.SetActive(false);
        HideBossHealthBar();
        Time.timeScale=0f; GameAudio.SetGameplayPaused(true);
        Vector2 center=new Vector2(.5f,.5f);
        var panel=NeonUI.Panel(victory ? "VictoryPanel" : "GameOverPanel",canvas.transform,center,Vector2.zero,Vector2.zero,new Color(.013f,.022f,.04f,.97f),true);
        panel.rectTransform.anchorMin=Vector2.zero; panel.rectTransform.anchorMax=Vector2.one;
        endScreen=panel.gameObject;
        Color accent=victory ? NeonUI.Cyan : NeonUI.Danger;
        NeonUI.Label("Caption",panel.transform,"N E O N   W O R L D S",11,accent,center,new Vector2(0,155),new Vector2(700,28),TextAnchor.MiddleCenter);
        NeonUI.Label("Title",panel.transform,victory ? "Órbita conquistada" : "Fim da jornada",42,NeonUI.White,center,new Vector2(0,94),new Vector2(850,68),TextAnchor.MiddleCenter);
        NeonUI.Label("Subtitle",panel.transform,victory ? "O guardião caiu. O universo continua." : "Toda tentativa leva você mais longe.",16,NeonUI.Muted,center,new Vector2(0,39),new Vector2(800,32),TextAnchor.MiddleCenter);
        int seconds=Mathf.Max(0,Mathf.FloorToInt(game.matchTime));
        NeonUI.Label("Stats",panel.transform,"TEMPO   "+(seconds/60).ToString("00")+":"+(seconds%60).ToString("00")+"       /       NÍVEL   "+game.level.ToString("00"),15,NeonUI.White,center,new Vector2(0,-30),new Vector2(700,36),TextAnchor.MiddleCenter);
        NeonUI.Label("Reward",panel.transform,"CÉLULAS ESTELARES   +"+earnedCores+"     /     TOTAL   "+MetaProgression.GetStarCores(),12,accent,center,new Vector2(0,-68),new Vector2(800,24),TextAnchor.MiddleCenter);
        Button hangar=EndButton(panel.transform,"Hangar","HANGAR",new Vector2(0,-135),NeonUI.Cyan);
        hangar.onClick.AddListener(() => BuildHangarUI(canvas.transform));
        Button restart=EndButton(panel.transform,"Restart","JOGAR NOVAMENTE",new Vector2(-264,-135),accent);
        restart.onClick.AddListener(() => {
            Time.timeScale=1f; GameAudio.SetGameplayPaused(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
        Button quit=EndButton(panel.transform,"Quit","SAIR",new Vector2(264,-135),NeonUI.Muted);
        quit.onClick.AddListener(() => Application.Quit());
        if(UnityEngine.EventSystems.EventSystem.current!=null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(restart.gameObject);
    }

    static Button EndButton(Transform parent,string name,string label,Vector2 position,Color accent)
    {
        var image=NeonUI.Panel(name,parent,new Vector2(.5f,.5f),position,new Vector2(248,52),NeonUI.Surface,true);
        var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image; NeonUI.StyleButton(button);
        NeonUI.Panel("Accent",image.transform,Vector2.zero,Vector2.zero,new Vector2(248,2),accent);
        NeonUI.Label("Label",image.transform,label,12,accent,new Vector2(.5f,.5f),Vector2.zero,new Vector2(230,40),TextAnchor.MiddleCenter);
        return button;
    }

    private static GameObject bossBarObj;
    private static Image bossFillImg;
    private static Text bossText;

    public static void BuildBossHealthBar(BossLeviathan boss) { BuildBossHealthBarDirect(boss); }

    public static void BuildBossHealthBarDirect(BossLeviathan boss)
    {
        HideBossHealthBar();
        var canvas=GameObject.Find("CanvasHUD");
        if(canvas==null) return;
        var root=NeonUI.Rect("BossHealthBar",canvas.transform,new Vector2(.5f,1),new Vector2(0,-100),new Vector2(440,48));
        bossBarObj=root.gameObject;
        bossText=NeonUI.Label("BossName",root,"LEVIATÃ ORBITAL",11,NeonUI.Danger,new Vector2(.5f,1),Vector2.zero,new Vector2(440,24),TextAnchor.MiddleCenter);
        var track=NeonUI.Panel("Track",root,Vector2.zero,Vector2.zero,new Vector2(440,3),NeonUI.Surface);
        bossFillImg=NeonUI.Panel("Fill",track.transform,Vector2.zero,Vector2.zero,Vector2.zero,NeonUI.Danger);
        bossFillImg.rectTransform.anchorMax=Vector2.one;
        root.SetAsFirstSibling();
    }

    public static void UpdateBossHP(int current,int max)
    {
        if(bossFillImg!=null) bossFillImg.rectTransform.anchorMax=new Vector2(max>0 ? Mathf.Clamp01((float)current/max) : 0,1);
        if(bossText!=null) bossText.text="LEVIATÃ   /   "+(BossLeviathan.Instance!=null ? BossLeviathan.Instance.AttackHint+"   /   " : "")+Mathf.Max(0,current)+" HP";
    }

    public static void HideBossHealthBar()
    {
        if(bossBarObj!=null) { bossBarObj.SetActive(false); Object.Destroy(bossBarObj); }
        bossBarObj=null; bossFillImg=null; bossText=null;
    }

    public static void StyleUpgradeCard(Button btn,GameManager.UpgradeRarity rarity)
    {
        if(btn==null) return;
        Color accent=rarity==GameManager.UpgradeRarity.Legendary ? new Color(1f,.8f,.35f) : rarity==GameManager.UpgradeRarity.Epic ? NeonUI.Violet : rarity==GameManager.UpgradeRarity.Rare ? new Color(.38f,.7f,1f) : NeonUI.Cyan;
        string label=rarity==GameManager.UpgradeRarity.Legendary ? "LENDÁRIA" : rarity==GameManager.UpgradeRarity.Epic ? "ÉPICA" : rarity==GameManager.UpgradeRarity.Rare ? "RARA" : "COMUM";
        Transform card=btn.transform;
        if(card.Find("Accent")!=null) card.Find("Accent").GetComponent<Image>().color=accent;
        if(card.Find("Category")!=null) card.Find("Category").GetComponent<Text>().text=label;
        if(card.Find("Glyph")!=null) card.Find("Glyph").GetComponent<UpgradeGlyph>().color=accent;
        if(card.Find("Select")!=null) card.Find("Select").GetComponent<Text>().color=accent;
    }

    public static void BuildVictoryUI(GameManager game) { BuildEndScreen(game,true); }

    public static void BuildGameOverUI(GameManager game) { BuildEndScreen(game,false); }

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
        GameObject tab1 = null;
        GameObject tab2 = null;

        System.Action updateTabs = () => {
            shipsCont.SetActive(showShips);
            upgCont.SetActive(!showShips);
            if (tab1 != null)
            {
                tab1.GetComponent<Image>().color = showShips ? new Color(0f, 0.8f, 1f) : new Color(0.15f, 0.2f, 0.3f);
                tab1.GetComponentInChildren<Text>().color = showShips ? Color.black : Color.white;
            }
            if (tab2 != null)
            {
                tab2.GetComponent<Image>().color = !showShips ? new Color(0f, 0.8f, 1f) : new Color(0.15f, 0.2f, 0.3f);
                tab2.GetComponentInChildren<Text>().color = !showShips ? Color.black : Color.white;
            }
        };

        tab1 = CreateStyledButton(tabsObj.transform, "🚀 NAVES & CHASSIS", new Color(0f, 0.8f, 1f), Color.black, () => {
            showShips = true;
            GameAudio.Play(AudioCue.Upgrade);
            updateTabs();
        });

        tab2 = CreateStyledButton(tabsObj.transform, "⚡ MELHORIAS DA FROTA", new Color(0.15f, 0.2f, 0.3f), Color.white, () => {
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

        if (UnityEngine.EventSystems.EventSystem.current != null && tab1 != null)
        {
            Button tb = tab1.GetComponent<Button>();
            if (tb != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(tb.gameObject);
        }
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
        btn.targetGraphic = bImg;
        btnObj.AddComponent<UISelectionFeedback>();

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
        btn.targetGraphic = bImg;
        btnObj.AddComponent<UISelectionFeedback>();
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
        btn.targetGraphic = img;

        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.45f);
        colors.selectedColor = new Color(0f, 1f, 0.95f, 1f);
        colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.35f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        btnObj.AddComponent<UISelectionFeedback>();

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

        if (onClick != null)
        {
            btn.onClick.AddListener(onClick);
        }
        return btnObj;
    }

    public static void BuildLevelUpUI(GameManager game)
    {
        GameObject canvasObject=GameObject.Find("CanvasHUD");
        if(canvasObject==null) { ModernHUD.Create(game); return; }
        Transform canvas=canvasObject.transform;
        Transform old=canvas.Find("LevelUpPanel");
        if(old!=null) { old.gameObject.SetActive(false); Object.Destroy(old.gameObject); }
        Image backdrop=NeonUI.Panel("LevelUpPanel",canvas,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(.013f,.022f,.04f,.94f),true);
        backdrop.rectTransform.anchorMin=Vector2.zero; backdrop.rectTransform.anchorMax=Vector2.one;
        Transform panel=backdrop.transform;
        Vector2 center=new Vector2(.5f,.5f);
        NeonUI.Label("Caption",panel,"E V O L U Ç Ã O   D A   N A V E",11,NeonUI.Cyan,center,new Vector2(0,224),new Vector2(600,24),TextAnchor.MiddleCenter);
        NeonUI.Label("Title",panel,"Escolha seu próximo avanço",32,NeonUI.White,center,new Vector2(0,181),new Vector2(850,48),TextAnchor.MiddleCenter);
        NeonUI.Label("Subtitle",panel,"Uma melhoria. Novas possibilidades.",15,NeonUI.Muted,center,new Vector2(0,139),new Vector2(700,26),TextAnchor.MiddleCenter);
        game.upgradeButtons=new Button[3]; game.upgradeTitles=new Text[3]; game.upgradeDescs=new Text[3];
        for(int i=0;i<3;i++)
        {
            Image card=NeonUI.Panel("Card_"+i,panel,center,new Vector2((i-1)*304,-35),new Vector2(280,300),NeonUI.Surface,true);
            Button button=card.gameObject.AddComponent<Button>(); button.targetGraphic=card; NeonUI.StyleButton(button);
            NeonUI.Panel("Accent",card.transform,new Vector2(0,1),Vector2.zero,new Vector2(280,2),NeonUI.Cyan);
            NeonUI.Label("Category",card.transform,"",10,NeonUI.Muted,new Vector2(0,1),new Vector2(24,-20),new Vector2(230,24));
            UpgradeGlyph glyph=NeonUI.Rect("Glyph",card.transform,new Vector2(0,1),new Vector2(23,-61),new Vector2(62,62)).gameObject.AddComponent<UpgradeGlyph>();
            glyph.raycastTarget=false;
            game.upgradeTitles[i]=NeonUI.Label("Title",card.transform,"",23,NeonUI.White,new Vector2(0,1),new Vector2(24,-146),new Vector2(234,62));
            game.upgradeDescs[i]=NeonUI.Label("Description",card.transform,"",14,NeonUI.Muted,new Vector2(0,1),new Vector2(24,-205),new Vector2(232,68));
            NeonUI.Label("Select",card.transform,"SELECIONAR",10,NeonUI.Cyan,Vector2.zero,new Vector2(24,17),new Vector2(220,22));
            game.upgradeButtons[i]=button;
        }
        Button reroll=EndButton(panel,"RerollBtn","SORTEAR NOVAMENTE",new Vector2(-132,-231),NeonUI.Cyan);
        reroll.onClick.AddListener(() => { if(game.availableRerolls>0) { game.availableRerolls--; GameAudio.Play(AudioCue.Upgrade); game.ShowLevelUpScreen(); } });
        Button codex=EndButton(panel,"CodexBtn","CÓDEX DE SINERGIAS",new Vector2(132,-231),NeonUI.Violet);
        codex.onClick.AddListener(() => BuildSynergyCodexUI(canvas));
        game.levelUpPanel=backdrop.gameObject; game.levelUpPanel.SetActive(false);
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
            btn.targetGraphic = cardBg;
            optCard.AddComponent<UISelectionFeedback>();

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

        GameObject contBtn = CreateStyledButton(btnCont.transform, "CONTINUAR [ESC]", new Color(0.15f, 0.7f, 0.35f), Color.black, () => {
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
        if (UnityEngine.EventSystems.EventSystem.current != null && contBtn != null)
        {
            Button first = contBtn.GetComponent<Button>();
            if (first != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
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

        if (UnityEngine.EventSystems.EventSystem.current != null && closeBtnObj != null)
        {
            Button cb = closeBtnObj.GetComponent<Button>();
            if (cb != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(cb.gameObject);
        }

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

    private static GameObject currentPlanetBanner;

    public static void ShowPlanetBanner(string planetName,string hazardDesc,Color themeColor)
    {
        var canvas=GameObject.Find("CanvasHUD"); if(canvas==null) return;
        if(currentPlanetBanner!=null) { currentPlanetBanner.SetActive(false); Object.Destroy(currentPlanetBanner); }
        Transform parent=canvas.transform.Find("Telemetry"); if(parent==null) parent=canvas.transform;
        var panel=NeonUI.Panel("PlanetBanner",parent,new Vector2(.5f,1),new Vector2(0,-156),new Vector2(550,62),NeonUI.Surface);
        currentPlanetBanner=panel.gameObject;
        NeonUI.Panel("Accent",panel.transform,new Vector2(0,1),Vector2.zero,new Vector2(550,1),themeColor);
        NeonUI.Label("Planet",panel.transform,planetName,16,themeColor,new Vector2(.5f,1),new Vector2(0,-7),new Vector2(520,25),TextAnchor.MiddleCenter);
        NeonUI.Label("Hazard",panel.transform,hazardDesc,11,NeonUI.Muted,new Vector2(.5f,1),new Vector2(0,-33),new Vector2(520,22),TextAnchor.MiddleCenter);
        panel.gameObject.AddComponent<CanvasGroup>().alpha=0;
        panel.gameObject.AddComponent<BannerAnimator>();
    }

    private static GameObject frenzyBanner;

    public static void UpdateFrenzyHUD(float remainingTime)
    {
        var canvas=GameObject.Find("CanvasHUD"); if(canvas==null) return;
        if(remainingTime<=0) { if(frenzyBanner!=null) Object.Destroy(frenzyBanner); frenzyBanner=null; return; }
        if(frenzyBanner==null)
        {
            Transform parent=canvas.transform.Find("Telemetry"); if(parent==null) parent=canvas.transform;
            frenzyBanner=NeonUI.Label("FrenzyHUD",parent,"",13,new Color(1f,.8f,.35f),new Vector2(.5f,0),new Vector2(0,36),new Vector2(400,30),TextAnchor.MiddleCenter).gameObject;
        }
        frenzyBanner.GetComponent<Text>().text="FRENESI   /   "+remainingTime.ToString("F1")+" s";
    }
}

public class BannerAnimator : MonoBehaviour
{
    private CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        StartCoroutine(AnimRoutine());
    }

    IEnumerator AnimRoutine()
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            if (cg != null) cg.alpha = Mathf.Clamp01(t / 0.35f);
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(3.2f);

        t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime;
            if (cg != null) cg.alpha = Mathf.Clamp01(1f - (t / 0.45f));
            yield return null;
        }

        Destroy(gameObject);
    }
}
