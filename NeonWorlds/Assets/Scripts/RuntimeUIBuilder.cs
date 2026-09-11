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
        title=title.Split('•')[0].Trim();
        game.upgradeTitles[index].text=title;
        int maximum=game.GetMaxLevel(kind);
        int shownLevel=game.GetCardUpgradeLevel(index,kind);
        card.Find("Select").GetComponent<Text>().text=game.IsWeaponAcquisition(kind) ? "ADQUIRIR E EQUIPAR" : maximum<90 ? "NÍVEL "+(shownLevel+1)+" / "+maximum : "SELECIONAR";
        if(game.IsWeaponAcquisition(kind))
        {
            Color discovery=new Color(.35f,1f,.72f);
            card.Find("Category").GetComponent<Text>().text=(index+1).ToString("00")+"  /  DESCOBERTA DE ARMA";
            card.Find("Accent").GetComponent<Image>().color=discovery;
            UpgradeGlyph discoveryGlyph=card.Find("Glyph").GetComponent<UpgradeGlyph>();discoveryGlyph.kind=kind;discoveryGlyph.color=discovery;discoveryGlyph.SetVerticesDirty();
            StyleUpgradeCard(game.upgradeButtons[index],game.GetRarity(kind));
            card.Find("Category").GetComponent<Text>().text="NOVA ARMA  •  RARA";
            return;
        }
        bool offense=kind==GameManager.UpgradeType.PlasmaReactor || kind==GameManager.UpgradeType.SunfireLance || kind==GameManager.UpgradeType.PlasmaFlamerMastery || kind==GameManager.UpgradeType.ArcDischargerMastery || kind==GameManager.UpgradeType.IonicCapacitor || kind==GameManager.UpgradeType.IonicStorm || kind==GameManager.UpgradeType.SingularityMastery || kind==GameManager.UpgradeType.SeekerSwarmMastery || kind==GameManager.UpgradeType.GravityAmplifier || kind==GameManager.UpgradeType.GuidanceMatrix || kind==GameManager.UpgradeType.EventHorizon || kind==GameManager.UpgradeType.HunterKillerPod || kind==GameManager.UpgradeType.Spread || kind==GameManager.UpgradeType.FireRate || kind==GameManager.UpgradeType.Pierce || kind==GameManager.UpgradeType.Bounce || kind==GameManager.UpgradeType.Explosive;
        offense=offense || kind==GameManager.UpgradeType.BlasterMastery || kind==GameManager.UpgradeType.ShotgunMastery || kind==GameManager.UpgradeType.RailgunMastery || kind==GameManager.UpgradeType.OverchargeCapacitor || kind==GameManager.UpgradeType.ScatterMatrix || kind==GameManager.UpgradeType.AntimatterCore;
        bool fortune=kind==GameManager.UpgradeType.Luck;
        Color accent=fortune ? new Color(1f,.72f,.12f) : offense ? NeonUI.Cyan : kind==GameManager.UpgradeType.Heal ? new Color(.51f,.94f,.73f) : NeonUI.Violet;
        card.Find("Category").GetComponent<Text>().text=(index+1).ToString("00")+"  /  "+(fortune ? "FORTUNA" : offense ? "ARSENAL" : kind==GameManager.UpgradeType.Heal ? "SOBREVIVÊNCIA" : "MOBILIDADE");
        card.Find("Accent").GetComponent<Image>().color=accent;
        UpgradeGlyph glyph=card.Find("Glyph").GetComponent<UpgradeGlyph>(); glyph.kind=kind; glyph.color=accent; glyph.SetVerticesDirty();
        StyleUpgradeCard(game.upgradeButtons[index],game.GetRarity(kind));
        if(game.IsProjectileModifier(kind))card.Find("Category").GetComponent<Text>().text="MODIFICADOR  •  "+WeaponArsenal.DisplayName(game.GetCardWeapon(index));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { bossBarObj=null; endScreen=null; bossFillImg=null; bossText=null; }

    static void BuildEndScreen(GameManager game,bool victory)
    {
        if(HasEndScreen) return;
        RunTelemetry.LogFinal(victory);
        int missionCores=RunEconomy.MissionReward(game.level,game.matchTime,victory);
        int recoveredCores=RunEconomy.CarriedCells;
        int earnedCores=missionCores+recoveredCores;
        MetaProgression.AddStarCores(earnedCores);
        var canvas=GameObject.Find("CanvasHUD");
        if(canvas==null) { ModernHUD.Create(game); canvas=GameObject.Find("CanvasHUD"); }
        if(game.levelUpPanel!=null) game.levelUpPanel.SetActive(false);
        HideBossHealthBar();
        Time.timeScale=0f; GameAudio.SetGameplayPaused(true);
        Vector2 center=new Vector2(.5f,.5f);
        var panel=NeonUI.Panel(victory ? "VictoryPanel" : "GameOverPanel",canvas.transform,center,Vector2.zero,Vector2.zero,new Color(.006f,.012f,.025f,.975f),true);
        panel.rectTransform.anchorMin=Vector2.zero; panel.rectTransform.anchorMax=Vector2.one;
        endScreen=panel.gameObject;
        Color accent=victory ? NeonUI.Cyan : NeonUI.Danger;
        RectTransform frame=NeonUI.Panel("ResultFrame",panel.transform,center,Vector2.zero,new Vector2(920,570),new Color(.018f,.03f,.055f,.995f)).rectTransform;
        frame.gameObject.AddComponent<ResponsiveMenuFrame>().designSize=new Vector2(920,570);
        Outline frameOutline=frame.gameObject.AddComponent<Outline>();frameOutline.effectColor=new Color(accent.r,accent.g,accent.b,.55f);frameOutline.effectDistance=new Vector2(2,-2);
        NeonUI.Panel("TopRail",frame,new Vector2(.5f,1),Vector2.zero,new Vector2(920,4),accent);
        NeonUI.Label("Caption",frame,"N E O N   W O R L D S  /  "+ExpeditionRouteSystem.Name(ExpeditionRouteSystem.Selected)+"  /  RELATÓRIO",10,accent,new Vector2(0,1),new Vector2(34,-28),new Vector2(700,22));
        Text resultTitle=NeonUI.Label("Title",frame,victory ? "ÓRBITA CONQUISTADA" : "EXPEDIÇÃO ENCERRADA",35,NeonUI.White,new Vector2(0,1),new Vector2(34,-59),new Vector2(790,50));resultTitle.fontStyle=FontStyle.Bold;
        NeonUI.Label("Subtitle",frame,victory ? "O guardião caiu. Uma nova rota foi aberta." : "Dados preservados. Reconfigure a nave e tente outra rota.",13,NeonUI.Muted,new Vector2(0,1),new Vector2(34,-108),new Vector2(790,25));
        int seconds=Mathf.Max(0,Mathf.FloorToInt(game.matchTime));
        ResultMetric(frame,"TimeMetric","TEMPO",(seconds/60).ToString("00")+":"+(seconds%60).ToString("00"),new Vector2(34,-159),accent);
        ResultMetric(frame,"LevelMetric","NÍVEL",game.level.ToString("00"),new Vector2(303,-159),NeonUI.Violet);
        ResultMetric(frame,"CoreMetric","CÉLULAS",earnedCores.ToString("+0"),new Vector2(572,-159),new Color(1f,.75f,.22f));
        Image rewards=NeonUI.Panel("Rewards",frame,new Vector2(0,1),new Vector2(34,-274),new Vector2(842,104),new Color(.028f,.048f,.078f,.95f));
        NeonUI.Label("Label",rewards.transform,"RECOMPENSAS RECUPERADAS",9,NeonUI.Muted,new Vector2(0,1),new Vector2(20,-15),new Vector2(300,18));
        NeonUI.Label("Mission",rewards.transform,"MISSÃO\n<color=#ffffff>+"+missionCores+" ★</color>",11,accent,new Vector2(0,1),new Vector2(20,-42),new Vector2(180,46));
        NeonUI.Label("Collected",rewards.transform,"COLETADAS\n<color=#ffffff>+"+recoveredCores+" ★</color>",11,accent,new Vector2(0,1),new Vector2(225,-42),new Vector2(180,46));
        NeonUI.Label("Total",rewards.transform,"SALDO TOTAL\n<color=#ffffff>"+MetaProgression.GetStarCores()+" ★</color>",11,new Color(1f,.75f,.22f),new Vector2(0,1),new Vector2(430,-42),new Vector2(180,46));
        NeonUI.Label("Discoveries",rewards.transform,"DESCOBERTAS\n<color=#ffffff>"+ProgressionContracts.RunReportCompact()+"</color>",9,NeonUI.Violet,new Vector2(0,1),new Vector2(635,-42),new Vector2(188,52));
        NeonUI.Label("RunTelemetry",frame,RunTelemetry.EndSummary(),10,NeonUI.Muted,new Vector2(.5f,1),new Vector2(0,-398),new Vector2(820,24),TextAnchor.MiddleCenter);
        Button hangar=EndButton(frame,"Hangar","HANGAR",new Vector2(0,-210),NeonUI.Cyan);
        hangar.onClick.AddListener(() => BuildHangarUI(canvas.transform));
        Button restart=EndButton(frame,"Restart","NOVA EXPEDIÇÃO",new Vector2(-274,-210),accent);
        restart.onClick.AddListener(() => {
            Time.timeScale=1f; GameAudio.SetGameplayPaused(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
        Button quit=EndButton(frame,"Quit","SAIR",new Vector2(274,-210),NeonUI.Muted);
        quit.onClick.AddListener(() => Application.Quit());
        NeonUI.Label("ResultHint",frame,"A / ENTER confirmar     •     navegue com setas ou analógico",9,NeonUI.Muted,new Vector2(.5f,0),new Vector2(0,13),new Vector2(620,18),TextAnchor.MiddleCenter);
        MenuScreenMotion.Attach(panel.gameObject,frame);
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
        panel.AddComponent<MenuPolish>();
        panel.transform.SetParent(canvasTransform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.035f, 0.035f); panelRt.anchorMax = new Vector2(0.965f, 0.965f);
        panelRt.sizeDelta = Vector2.zero; panelRt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.014f, 0.025f, 0.048f, 0.995f);
        Outline outl = panel.AddComponent<Outline>();
        outl.effectColor = new Color(0.2f, 0.7f, 1f, 0.8f);
        outl.effectDistance = new Vector2(2, -2);
        NeonUI.Panel("TopRail",panel.transform,new Vector2(.5f,1),Vector2.zero,new Vector2(1280,3),NeonUI.Cyan);

        // Header
        GameObject header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        RectTransform hrt = header.AddComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.04f, 0.895f); hrt.anchorMax = new Vector2(0.67f, 0.975f);
        hrt.sizeDelta = Vector2.zero; hrt.anchoredPosition = Vector2.zero;
        Text hText = header.AddComponent<Text>();
        hText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hText.text = "HANGAR ESTELAR";
        hText.fontSize = 28; hText.fontStyle=FontStyle.Bold;hText.alignment = TextAnchor.MiddleLeft; hText.color = NeonUI.White;
        NeonUI.Label("HeaderCaption",panel.transform,"FROTA  /  OFICINA  /  PROGRESSÃO PERMANENTE",9,NeonUI.Cyan,new Vector2(0,1),new Vector2(48,-20),new Vector2(520,18));

        // Currency Display
        GameObject currency = new GameObject("CurrencyText");
        currency.transform.SetParent(panel.transform, false);
        RectTransform crt = currency.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.69f, 0.905f); crt.anchorMax = new Vector2(0.96f, 0.965f);
        crt.sizeDelta = Vector2.zero; crt.anchoredPosition = Vector2.zero;
        Text cText = currency.AddComponent<Text>();
        cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cText.fontSize = 15; cText.fontStyle=FontStyle.Bold;cText.alignment = TextAnchor.MiddleRight;cText.color=new Color(1f,.78f,.2f);

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
        Image contentBackground=contentArea.AddComponent<Image>();contentBackground.color=new Color(.022f,.038f,.065f,.82f);
        contentArea.AddComponent<RectMask2D>();
        ScrollRect contentScroll=contentArea.AddComponent<ScrollRect>();contentScroll.viewport=cart;contentScroll.horizontal=false;contentScroll.vertical=true;contentScroll.movementType=ScrollRect.MovementType.Clamped;contentScroll.scrollSensitivity=28;

        // Container 1: Ships
        GameObject shipsCont = new GameObject("ShipsList");
        shipsCont.transform.SetParent(contentArea.transform, false);
        RectTransform srt = shipsCont.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0,1); srt.anchorMax = Vector2.one;srt.pivot=new Vector2(.5f,1);srt.sizeDelta = new Vector2(-24,380);srt.anchoredPosition=new Vector2(-6,-14);
        VerticalLayoutGroup svlg = shipsCont.AddComponent<VerticalLayoutGroup>();
        svlg.spacing = 8; svlg.childControlHeight = true; svlg.childControlWidth = true;
        svlg.childForceExpandWidth = true; svlg.childForceExpandHeight = false;

        // Container 2: Upgrades
        GameObject upgCont = new GameObject("UpgradeList");
        upgCont.transform.SetParent(contentArea.transform, false);
        RectTransform urt = upgCont.AddComponent<RectTransform>();
        urt.anchorMin = new Vector2(0,1); urt.anchorMax = Vector2.one;urt.pivot=new Vector2(.5f,1);urt.sizeDelta = new Vector2(-24,430);urt.anchoredPosition=new Vector2(-6,-14);
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
            CreateHangarRow(upgCont.transform, MetaProgression.UP_LUCK, "Calibrador de Fortuna", "+5% Sorte inicial por nível", 5, 22, refreshAll);
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
            contentScroll.content=showShips?srt:urt;contentScroll.verticalNormalizedPosition=1f;
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
        MenuScrollVisuals.AddVertical(contentScroll,contentArea.transform,NeonUI.Cyan,5);

        // Close Button
        GameObject closeBtn = CreateStyledButton(panel.transform, "B / ESC  VOLTAR", new Color(0.8f, 0.2f, 0.3f), Color.white, () => {
            Object.Destroy(panel);
        });
        RectTransform cbrt = closeBtn.GetComponent<RectTransform>();
        cbrt.anchorMin = new Vector2(0.74f, 0.025f); cbrt.anchorMax = new Vector2(0.96f, 0.09f);
        cbrt.sizeDelta = Vector2.zero; cbrt.anchoredPosition = Vector2.zero;
        NeonUI.Label("HangarHint",panel.transform,"LB / RB TROCAR ABA   •   A CONFIRMAR   •   B / ESC VOLTAR",9,NeonUI.Muted,new Vector2(0,0),new Vector2(48,24),new Vector2(650,18));
        MenuScreenMotion.Attach(panel,panelRt);

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
        le.minHeight = 70; le.preferredHeight = 74;
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
        string archiveLine=isUnlocked?ModifierDiscovery.ShipChallengeSummary(chassis):"CONTRATO  "+ProgressionContracts.ContractName(chassis)+"  •  "+ProgressionContracts.ProgressText(chassis);
        iText.text = $"<size=17><b><color=#{hexColor}>{name}</color></b></size> <size=12><color=#88bbdd>[{subtitle}]</color></size>\n<size=12><color=#cccccc>{desc}</color></size>\n<size=9><color=#{hexColor}>{archiveLine}</color></size>";
        if(!isUnlocked)
        {
            GameObject track=new GameObject("ContractTrack",typeof(RectTransform),typeof(Image));track.transform.SetParent(row.transform,false);RectTransform tr=track.GetComponent<RectTransform>();tr.anchorMin=new Vector2(.025f,0);tr.anchorMax=new Vector2(.68f,0);tr.sizeDelta=new Vector2(0,3);tr.anchoredPosition=new Vector2(0,3);track.GetComponent<Image>().color=new Color(.1f,.14f,.2f,1);
            GameObject fill=new GameObject("Fill",typeof(RectTransform),typeof(Image));fill.transform.SetParent(track.transform,false);RectTransform fr=fill.GetComponent<RectTransform>();fr.anchorMin=Vector2.zero;fr.anchorMax=new Vector2(ProgressionContracts.Completion(chassis),1);fr.sizeDelta=Vector2.zero;fr.anchoredPosition=Vector2.zero;fill.GetComponent<Image>().color=themeColor;
        }

        if (isSelected)
        {
            bText.text = "PRÓXIMA RUN ★";
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
        le.minHeight = 56; le.preferredHeight = 58;
        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.16f, 0.9f);

        GameObject info = new GameObject("Info");
        info.transform.SetParent(row.transform, false);
        RectTransform irt = info.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.02f, 0.12f); irt.anchorMax = new Vector2(0.7f, 1f);
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
        Image progressTrack=NeonUI.Panel("ProgressTrack",row.transform,new Vector2(0,0),new Vector2(14,7),new Vector2(610,3),new Color(.13f,.17f,.23f,1));
        Image progressFill=NeonUI.Panel("Fill",progressTrack.transform,Vector2.zero,Vector2.zero,Vector2.zero,NeonUI.Cyan);progressFill.rectTransform.anchorMax=Vector2.zero;

        System.Action updateDisplay = () => {
            int currentLvl = MetaProgression.GetUpgradeLevel(key);
            iText.text = $"<b>{title}</b> [NV {currentLvl}/{maxLvl}]\n<size=12><color=#aaaaaa>{desc}</color></size>";
            progressFill.rectTransform.anchorMax=new Vector2(maxLvl>0?(float)currentLvl/maxLvl:0,1);

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

        NeonUI.StyleButton(btn);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = MenuPolish.Clean(label);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = textColor;
        txt.fontSize = 14;
        txt.fontStyle = FontStyle.Normal;

        RectTransform rtr = txtObj.GetComponent<RectTransform>();
        rtr.anchorMin = Vector2.zero; rtr.anchorMax = Vector2.one; rtr.sizeDelta = Vector2.zero;

        txt.raycastTarget = false;
        if (label.Length > 3)
        {
            txt.alignment=TextAnchor.MiddleLeft; rtr.offsetMin=new Vector2(44,0);rtr.offsetMax=new Vector2(-12,0);
            var icon=NeonUI.Rect("MenuIcon",btnObj.transform,new Vector2(0,.5f),new Vector2(15,0),new Vector2(18,18)).gameObject.AddComponent<MenuIcon>();
            icon.symbol=label;icon.color=NeonUI.Cyan;icon.raycastTarget=false;
        }
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
        Image backdrop=NeonUI.Panel("LevelUpPanel",canvas,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(.006f,.012f,.026f,.965f),true);
        backdrop.rectTransform.anchorMin=Vector2.zero; backdrop.rectTransform.anchorMax=Vector2.one;
        RectTransform frame=NeonUI.Panel("UpgradeFrame",backdrop.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1060,640),new Color(.017f,.028f,.052f,.99f)).rectTransform;
        frame.gameObject.AddComponent<ResponsiveMenuFrame>().designSize=new Vector2(1060,640);
        Outline outline=frame.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.22f,.72f,.95f,.48f);outline.effectDistance=new Vector2(2,-2);
        Transform panel=frame;
        Vector2 center=new Vector2(.5f,.5f);
        NeonUI.Panel("TopRail",panel,new Vector2(.5f,1),Vector2.zero,new Vector2(1060,3),NeonUI.Cyan);
        NeonUI.Label("Caption",panel,"E V O L U Ç Ã O   D A   N A V E",10,NeonUI.Cyan,new Vector2(0,1),new Vector2(36,-27),new Vector2(600,22));
        Text levelTitle=NeonUI.Label("Title",panel,"Escolha o próximo avanço",29,NeonUI.White,new Vector2(0,1),new Vector2(36,-57),new Vector2(680,42));levelTitle.fontStyle=FontStyle.Bold;
        NeonUI.Label("Subtitle",panel,"Compare função, raridade e compatibilidade antes de confirmar.",12,NeonUI.Muted,new Vector2(0,1),new Vector2(36,-100),new Vector2(700,24));
        Image counter=NeonUI.Panel("ChoiceCounter",panel,new Vector2(1,1),new Vector2(-36,-34),new Vector2(210,48),new Color(.025f,.09f,.115f,.95f));
        NeonUI.Label("Label",counter.transform,"1 ESCOLHA DISPONÍVEL",10,NeonUI.Cyan,new Vector2(.5f,.5f),Vector2.zero,new Vector2(190,30),TextAnchor.MiddleCenter);
        NeonUI.Panel("HeaderRule",panel,new Vector2(.5f,1),new Vector2(0,-132),new Vector2(988,1),new Color(.18f,.32f,.4f,.65f));
        game.upgradeButtons=new Button[3]; game.upgradeTitles=new Text[3]; game.upgradeDescs=new Text[3];
        for(int i=0;i<3;i++)
        {
            Image card=NeonUI.Panel("Card_"+i,panel,center,new Vector2((i-1)*326,-30),new Vector2(306,338),new Color(.03f,.048f,.078f,.99f),true);
            Button button=card.gameObject.AddComponent<Button>(); button.targetGraphic=card; NeonUI.StyleButton(button);
            Outline cardOutline=card.gameObject.GetComponent<Outline>();if(cardOutline!=null){cardOutline.effectColor=new Color(.15f,.28f,.36f,.65f);cardOutline.effectDistance=new Vector2(1,-1);}
            NeonUI.Panel("Accent",card.transform,new Vector2(.5f,1),Vector2.zero,new Vector2(306,3),NeonUI.Cyan);
            NeonUI.Label("Category",card.transform,"",9,NeonUI.Muted,new Vector2(0,1),new Vector2(22,-20),new Vector2(260,22));
            Image glyphWell=NeonUI.Panel("GlyphWell",card.transform,new Vector2(0,1),new Vector2(22,-57),new Vector2(70,70),new Color(.08f,.15f,.21f,.78f));
            UpgradeGlyph glyph=NeonUI.Rect("Glyph",card.transform,new Vector2(0,1),new Vector2(31,-66),new Vector2(52,52)).gameObject.AddComponent<UpgradeGlyph>();
            glyph.raycastTarget=false;
            game.upgradeTitles[i]=NeonUI.Label("Title",card.transform,"",20,NeonUI.White,new Vector2(0,1),new Vector2(106,-58),new Vector2(176,68));game.upgradeTitles[i].fontStyle=FontStyle.Bold;
            NeonUI.Panel("InfoRule",card.transform,new Vector2(.5f,1),new Vector2(0,-142),new Vector2(262,1),new Color(.15f,.24f,.31f,.75f));
            NeonUI.Label("EffectCaption",card.transform,"EFEITO",9,NeonUI.Muted,new Vector2(0,1),new Vector2(22,-159),new Vector2(260,18));
            game.upgradeDescs[i]=NeonUI.Label("Description",card.transform,"",12,NeonUI.White,new Vector2(0,1),new Vector2(22,-181),new Vector2(262,92));game.upgradeDescs[i].lineSpacing=1.15f;
            Image selectBar=NeonUI.Panel("SelectBar",card.transform,new Vector2(.5f,0),new Vector2(0,18),new Vector2(262,38),new Color(.02f,.075f,.095f,.95f));
            NeonUI.Label("Select",card.transform,"SELECIONAR",10,NeonUI.Cyan,new Vector2(.5f,0),new Vector2(0,18),new Vector2(250,36),TextAnchor.MiddleCenter);
            game.upgradeButtons[i]=button;
        }
        Button reroll=EndButton(panel,"RerollBtn","SORTEAR NOVAMENTE",new Vector2(-145,-242),NeonUI.Cyan);
        reroll.onClick.AddListener(() => { if(game.availableRerolls>0) { game.availableRerolls--; GameAudio.Play(AudioCue.Upgrade); game.ShowLevelUpScreen(); } });
        Button codex=EndButton(panel,"CodexBtn","ABRIR CÓDEX",new Vector2(145,-242),NeonUI.Violet);
        codex.onClick.AddListener(() => BuildSynergyCodexUI(canvas));
        NeonUI.Label("Controls",panel,"← → ESCOLHER     •     A / ENTER CONFIRMAR     •     CÓDEX PAUSA A ESCOLHA",9,NeonUI.Muted,new Vector2(.5f,0),new Vector2(0,8),new Vector2(720,18),TextAnchor.MiddleCenter);
        game.levelUpPanel=backdrop.gameObject; game.levelUpPanel.SetActive(false);
        MenuScreenMotion.Attach(backdrop.gameObject,frame);
    }

    static void ResultMetric(Transform parent,string name,string label,string value,Vector2 position,Color accent)
    {
        Image card=NeonUI.Panel(name,parent,new Vector2(0,1),position,new Vector2(250,92),new Color(.032f,.055f,.09f,.98f));
        NeonUI.Panel("Accent",card.transform,new Vector2(0,1),Vector2.zero,new Vector2(250,2),accent);
        NeonUI.Label("Label",card.transform,label,9,NeonUI.Muted,new Vector2(0,1),new Vector2(16,-14),new Vector2(218,18));
        Text metric=NeonUI.Label("Value",card.transform,value,25,accent,new Vector2(0,1),new Vector2(16,-39),new Vector2(218,34));metric.fontStyle=FontStyle.Bold;
    }

    public static GameObject pauseMenuPanel;
    private static Button[] modeButtons;
    private static Image[] modeBackgrounds;
    private static Outline[] modeOutlines;

    public static GameObject BuildPauseMenu(GameManager gm, System.Action onResume)
    {
        pauseMenuPanel = ModernPauseMenu.Create(gm,onResume);
        return pauseMenuPanel;
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
        panel.AddComponent<MenuPolish>();
        panel.transform.SetParent(canvasTransform, false);
        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.025f, 0.025f);
        panelRt.anchorMax = new Vector2(0.975f, 0.975f);
        panelRt.sizeDelta = Vector2.zero;
        panelRt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.012f, 0.022f, 0.045f, 0.995f);
        Outline outl = panel.AddComponent<Outline>();
        outl.effectColor = new Color(1f, 0.85f, 0.15f, 0.9f);
        outl.effectDistance = new Vector2(2f, -2f);
        NeonUI.Panel("TopRail",panel.transform,new Vector2(.5f,1),Vector2.zero,new Vector2(1400,3),new Color(1f,.76f,.18f));

        // Header Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform trt = titleObj.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.88f); trt.anchorMax = new Vector2(1f, 0.98f);
        trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
        Text tText = titleObj.AddComponent<Text>();
        tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tText.text = "CÓDEX  /  EVOLUÇÕES LENDÁRIAS";
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
        sText.text = "Complete a maestria e o componente indicado. A evolução aparece na próxima seleção de nível.";
        sText.fontSize = 15;
        sText.alignment = TextAnchor.MiddleCenter;
        sText.color = new Color(0.75f, 0.85f, 1f);

        // Scrollable two-column archive.
        GameObject gridObj = new GameObject("GridContainer");
        gridObj.transform.SetParent(panel.transform, false);
        RectTransform grt = gridObj.AddComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.04f, 0.11f);
        grt.anchorMax = new Vector2(0.96f, 0.76f);
        grt.sizeDelta = Vector2.zero;
        grt.anchoredPosition = Vector2.zero;

        GridLayoutGroup glg = gridObj.AddComponent<GridLayoutGroup>();
        glg.spacing = new Vector2(22, 18);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.cellSize = new Vector2(520, 180);
        var viewport = new GameObject("CodexViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.transform.SetParent(panel.transform,false);
        var viewportRect=viewport.GetComponent<RectTransform>(); viewportRect.anchorMin=grt.anchorMin; viewportRect.anchorMax=grt.anchorMax; viewportRect.sizeDelta=Vector2.zero;
        viewport.GetComponent<Image>().color=Color.clear;
        grt.SetParent(viewport.transform,false); grt.anchorMin=new Vector2(0,1); grt.anchorMax=Vector2.one; grt.pivot=new Vector2(.5f,1); grt.anchoredPosition=Vector2.zero; grt.sizeDelta=new Vector2(-18,1060);
        var scroll=viewport.GetComponent<ScrollRect>(); scroll.viewport=viewportRect; scroll.content=grt; scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=30;
        MenuScrollVisuals.AddVertical(scroll,viewport.transform,new Color(1f,.76f,.18f),4);
        glg.childAlignment = TextAnchor.MiddleCenter;

        GameManager gm = GameManager.Instance;

        // 1. Supernova Gatling
        CreateCodexCard(gridObj.transform, gm,
            "SUPERNOVA GATLING",
            "Blaster Nv 5 + Capacitor de Sobrecarga Nv 3",
            "Disparos dourados contínuos em cadência máxima com micro-explosões solares em área ao impactar.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.SupernovaGatling) >= 1,
            gm != null && gm.CanEvolveSupernova,
            $"Blaster ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.BlasterMastery) : 0)}/5) + Capacitor ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.OverchargeCapacitor) : 0)}/3)"
        );

        // 2. Canhão Nebular
        CreateCodexCard(gridObj.transform, gm,
            "CANHÃO NEBULAR",
            "Shotgun Nv 5 + Matriz de Fragmentação Nv 3",
            "Dispara 8 fragmentos que ricocheteiam e explodem em múltiplos estilhaços cósmicos secundários.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.NebulaFlak) >= 1,
            gm != null && gm.CanEvolveNebula,
            $"Shotgun ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.ShotgunMastery) : 0)}/5) + Matriz ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.ScatterMatrix) : 0)}/3)"
        );

        // 3. Lança de Anti-Matéria
        CreateCodexCard(gridObj.transform, gm,
            "LANÇA DE ANTI-MATÉRIA",
            "Railgun Nv 5 + Núcleo de Antimatéria Nv 3",
            "Raio instantâneo hiper-perfurante de anti-matéria que deixa poças de radiação contínua no planeta.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.AntimatterLance) >= 1,
            gm != null && gm.CanEvolveAntimatter,
            $"Railgun ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.RailgunMastery) : 0)}/5) + Núcleo ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.AntimatterCore) : 0)}/3)"
        );

        // 4. Vórtice de Singularidade
        CreateCodexCard(gridObj.transform, gm,
            "VÓRTICE DE SINGULARIDADE",
            "Minas de Matéria Escura (Nv 3) + Munição Explosiva (Nv 1+)",
            "Minas colapsam em buracos negros que sugam inimigos para o centro antes de implodir causando 120 de dano.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.VoidVortex) >= 1,
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.OrbitalMines) >= 3 && gm.GetUpgradeLevel(GameManager.UpgradeType.Explosive) >= 1,
            $"Minas ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.OrbitalMines) : 0)}/3) + Explosiva ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.Explosive) : 0)}/1)"
        );

        // 5. Rede Neural Tesla
        CreateCodexCard(gridObj.transform, gm,
            "REDE NEURAL TESLA",
            "Drones Sentinelas (Nv 3) + Perfurante ou Ricochete (Nv 1+)",
            "Drones disparam rajadas elétricas com arcos secundários saltando em cascata entre até 4 inimigos.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.TeslaChain) >= 1,
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.SentinelDrone) >= 3 && (gm.GetUpgradeLevel(GameManager.UpgradeType.Pierce) >= 1 || gm.GetUpgradeLevel(GameManager.UpgradeType.Bounce) >= 1),
            $"Drones ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.SentinelDrone) : 0)}/3) + Perfurante ou Ricochete (Nv 1+)"
        );

        // 6. Barreira Hiperiônica
        CreateCodexCard(gridObj.transform, gm,
            "BARREIRA HIPERIÔNICA",
            "Escudo Aegis (Nv 3) + Nanites Vampíricos (Nv 1+)",
            "Ao quebrar ou recarregar, emite uma devastadora onda de choque dourada que repele e esmaga alvos (60 dano).",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.HyperionBarrier) >= 1,
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.AegisShield) >= 3 && gm.GetUpgradeLevel(GameManager.UpgradeType.LifeSteal) >= 1,
            $"Escudo Aegis ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.AegisShield) : 0)}/3) + Nanites ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.LifeSteal) : 0)}/1)"
        );

        CreateCodexCard(gridObj.transform, gm,
            "SUNFIRE LANCE", "Plasma Flamer Nv 5 + Reator de Plasma Nv 3",
            "Plasma Flamer evolui para feixe de 14 m com poças incandescentes.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.SunfireLance) >= 1,
            gm != null && gm.CanEvolveSunfire,
            $"Plasma ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.PlasmaFlamerMastery) : 0)}/5) + Reator ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.PlasmaReactor) : 0)}/3)"
        );
        CreateCodexCard(gridObj.transform, gm,
            "TEMPESTADE IÔNICA", "Arc Discharger Nv 5 + Capacitor Iônico Nv 3",
            "Sete alvos em cadeia recebem raios celestes que geram pulsos elétricos ao redor do impacto.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.IonicStorm) >= 1,
            gm != null && gm.CanEvolveIonicStorm,
            $"Arc ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.ArcDischargerMastery) : 0)}/5) + Capacitor ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.IonicCapacitor) : 0)}/3)"
        );
        CreateCodexCard(gridObj.transform, gm,
            "EVENT HORIZON", "Singularity Nv 5 + Amplificador Gravitacional Nv 3",
            "Campo maior e mais duradouro; a implosão repele e atordoa toda a horda próxima.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.EventHorizon) >= 1,
            gm != null && gm.CanEvolveEventHorizon,
            $"Singularidade ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.SingularityMastery) : 0)}/5) + Amplificador ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.GravityAmplifier) : 0)}/3)"
        );
        CreateCodexCard(gridObj.transform, gm,
            "HUNTER-KILLER POD", "Seeker Swarm Nv 5 + Matriz de Orientação Nv 3",
            "Oito mísseis de alta aceleração perseguem alvos e detonam em uma área ampliada.",
            gm != null && gm.GetUpgradeLevel(GameManager.UpgradeType.HunterKillerPod) >= 1,
            gm != null && gm.CanEvolveHunterKiller,
            $"Seekers ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.SeekerSwarmMastery) : 0)}/5) + Matriz ({(gm != null ? gm.GetUpgradeLevel(GameManager.UpgradeType.GuidanceMatrix) : 0)}/3)"
        );
        // A second archive keeps permanent modifier discovery separate from run evolution recipes.
        GameObject modifierGrid=new GameObject("ModifierGrid");modifierGrid.transform.SetParent(panel.transform,false);
        RectTransform mgrt=modifierGrid.AddComponent<RectTransform>();mgrt.anchorMin=new Vector2(0,1);mgrt.anchorMax=Vector2.one;mgrt.pivot=new Vector2(.5f,1);mgrt.anchoredPosition=Vector2.zero;mgrt.sizeDelta=new Vector2(0,1304);
        GridLayoutGroup mglg=modifierGrid.AddComponent<GridLayoutGroup>();mglg.spacing=new Vector2(22,16);mglg.constraint=GridLayoutGroup.Constraint.FixedColumnCount;mglg.constraintCount=2;mglg.cellSize=new Vector2(520,145);mglg.childAlignment=TextAnchor.UpperCenter;
        GameObject modifierViewport=new GameObject("ModifierViewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));modifierViewport.transform.SetParent(panel.transform,false);
        RectTransform mvrt=modifierViewport.GetComponent<RectTransform>();mvrt.anchorMin=new Vector2(.04f,.11f);mvrt.anchorMax=new Vector2(.96f,.76f);mvrt.sizeDelta=Vector2.zero;modifierViewport.GetComponent<Image>().color=Color.clear;
        mgrt.SetParent(modifierViewport.transform,false);ScrollRect mscroll=modifierViewport.GetComponent<ScrollRect>();mscroll.viewport=mvrt;mscroll.content=mgrt;mscroll.horizontal=false;mscroll.vertical=true;mscroll.movementType=ScrollRect.MovementType.Clamped;mscroll.scrollSensitivity=30;
        MenuScrollVisuals.AddVertical(mscroll,modifierViewport.transform,NeonUI.Violet,4);
        foreach(ProjectileModifier modifier in System.Enum.GetValues(typeof(ProjectileModifier)))CreateModifierCodexCard(modifierGrid.transform,modifier);
        modifierViewport.SetActive(false);

        GameObject evolutionTab=CreateStyledButton(panel.transform,"EVOLUÇÕES  10",new Color(1f,.72f,.14f),Color.black,null);
        int newModifiers=ModifierDiscovery.NewCount;
        GameObject modifierTab=CreateStyledButton(panel.transform,"MODIFICADORES  "+ModifierDiscovery.MasteredCount+"/"+System.Enum.GetValues(typeof(ProjectileModifier)).Length+(newModifiers>0?"  •  "+newModifiers+" NOVO"+(newModifiers>1?"S":""):""),NeonUI.Violet,Color.white,null);
        RectTransform ert=evolutionTab.GetComponent<RectTransform>();ert.anchorMin=new Vector2(.25f,.765f);ert.anchorMax=new Vector2(.49f,.815f);ert.sizeDelta=Vector2.zero;ert.anchoredPosition=Vector2.zero;
        RectTransform mrt=modifierTab.GetComponent<RectTransform>();mrt.anchorMin=new Vector2(.51f,.765f);mrt.anchorMax=new Vector2(.75f,.815f);mrt.sizeDelta=Vector2.zero;mrt.anchoredPosition=Vector2.zero;
        System.Action<bool> showModifiers=show=>
        {
            viewport.SetActive(!show);modifierViewport.SetActive(show);
            tText.text=show?"CÓDEX  /  ARQUIVO DE MODIFICADORES":"CÓDEX  /  EVOLUÇÕES LENDÁRIAS";
            sText.text=show?"Descubra protocolos em expedições específicas. Ao dominar um desafio, o card entra permanentemente na roleta.":"Complete a maestria e o componente indicado. A evolução aparece na próxima seleção de nível.";
            evolutionTab.GetComponent<Image>().color=show?new Color(.12f,.16f,.24f):new Color(1f,.72f,.14f);modifierTab.GetComponent<Image>().color=show?NeonUI.Violet:new Color(.12f,.16f,.24f);
            evolutionTab.GetComponentInChildren<Text>().color=show?NeonUI.Muted:Color.black;modifierTab.GetComponentInChildren<Text>().color=show?Color.white:NeonUI.Muted;
            if(UnityEngine.EventSystems.EventSystem.current!=null)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject((show?modifierTab:evolutionTab));
        };
        evolutionTab.GetComponent<Button>().onClick.AddListener(()=>showModifiers(false));modifierTab.GetComponent<Button>().onClick.AddListener(()=>showModifiers(true));
        // Close Button
        NeonUI.Label("ControllerScrollHint",panel.transform,"ANALÓGICO DIREITO / GATILHOS  ROLAR   •   LB / RB  TROCAR ABA",9,NeonUI.Muted,new Vector2(0,0),new Vector2(48,28),new Vector2(650,18));
        GameObject closeBtnObj = CreateStyledButton(panel.transform, "B / ESC  VOLTAR", new Color(0.85f, 0.25f, 0.35f), Color.white, () => {
            Object.Destroy(panel);
            if (onClose != null) onClose();
        });
        RectTransform cbrt = closeBtnObj.GetComponent<RectTransform>();
        cbrt.anchorMin = new Vector2(0.76f, 0.025f);
        cbrt.anchorMax = new Vector2(0.96f, 0.085f);
        cbrt.sizeDelta = Vector2.zero;
        cbrt.anchoredPosition = Vector2.zero;

        MenuScreenMotion.Attach(panel,panelRt);
        if (UnityEngine.EventSystems.EventSystem.current != null && evolutionTab != null)
        {
            Button firstTab = evolutionTab.GetComponent<Button>();
            if (firstTab != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstTab.gameObject);
        }
        ModifierDiscovery.MarkAllSeen();

        return panel;
    }

    static void CreateModifierCodexCard(Transform parent,ProjectileModifier modifier)
    {
        ModifierDiscoveryState state=ModifierDiscovery.GetState(modifier);bool unknown=state==ModifierDiscoveryState.Unknown,mastered=state==ModifierDiscoveryState.Mastered;
        Color accent=mastered?NeonUI.Cyan:unknown?new Color(.3f,.36f,.46f):NeonUI.Violet;
        GameObject card=new GameObject("ModifierCard_"+modifier);card.transform.SetParent(parent,false);Image image=card.AddComponent<Image>();image.color=mastered?new Color(.025f,.09f,.105f,.98f):new Color(.032f,.045f,.075f,.98f);
        Outline outline=card.AddComponent<Outline>();outline.effectColor=new Color(accent.r,accent.g,accent.b,.75f);outline.effectDistance=new Vector2(1.5f,-1.5f);
        NeonUI.Panel("Accent",card.transform,new Vector2(0,1),Vector2.zero,new Vector2(4,145),accent);
        GameManager.UpgradeType type=(GameManager.UpgradeType)((int)GameManager.UpgradeType.ModDuplicate+(int)modifier);
        UpgradeGlyph glyph=NeonUI.Rect("Glyph",card.transform,new Vector2(0,1),new Vector2(47,-46),new Vector2(58,58)).gameObject.AddComponent<UpgradeGlyph>();glyph.kind=type;glyph.color=accent;glyph.raycastTarget=false;
        string title=unknown?"SINAL DESCONHECIDO":ModifierDiscovery.DisplayName(modifier);
        Text name=NeonUI.Label("Name",card.transform,title,14,unknown?NeonUI.Muted:NeonUI.White,new Vector2(0,1),new Vector2(88,-13),new Vector2(286,23));name.fontStyle=FontStyle.Bold;
        string stateText=(ModifierDiscovery.IsNew(modifier)?"NOVO  •  ":"")+(mastered?"DOMINADO":unknown?"NÃO DECODIFICADO":"DESCOBERTO");
        Text badge=NeonUI.Label("State",card.transform,stateText,9,accent,new Vector2(1,1),new Vector2(-15,-15),new Vector2(125,20),TextAnchor.MiddleRight);badge.fontStyle=FontStyle.Bold;
        string requirement=unknown?ModifierDiscovery.DiscoveryHint(modifier):mastered?"COMPATÍVEL: "+ModifierDiscovery.CompatibilityText(modifier):ModifierDiscovery.ChallengeText(modifier);
        NeonUI.Label("Requirement",card.transform,requirement,10,accent,new Vector2(0,1),new Vector2(88,-41),new Vector2(410,18));
        string effect=unknown?"O comportamento e a assinatura visual ainda estão criptografados.":ModifierDiscovery.EffectText(modifier);
        Text effectText=NeonUI.Label("Effect",card.transform,effect,10,NeonUI.Muted,new Vector2(0,1),new Vector2(88,-64),new Vector2(405,34));effectText.verticalOverflow=VerticalWrapMode.Truncate;
        Text progress=NeonUI.Label("Progress",card.transform,ModifierDiscovery.ProgressText(modifier),9,accent,new Vector2(0,1),new Vector2(20,-111),new Vector2(475,14));progress.fontStyle=FontStyle.Bold;
        Image track=NeonUI.Panel("Track",card.transform,new Vector2(0,1),new Vector2(20,-132),new Vector2(475,4),new Color(.1f,.14f,.2f,1));Image fill=NeonUI.Panel("Fill",track.transform,Vector2.zero,Vector2.zero,Vector2.zero,accent);fill.rectTransform.anchorMax=new Vector2(ModifierDiscovery.Completion(modifier),1);
    }

    private static void CreateCodexCard(Transform parent, GameManager gm, string name, string recipe, string effect, bool isEvolved, bool isReady, string progressText)
    {
        GameObject card = new GameObject("CodexCard_" + name);
        card.transform.SetParent(parent, false);
        Image img = card.AddComponent<Image>();
        Outline outline = card.AddComponent<Outline>();
        CodexGlyphKind glyphKind;Color accent;ResolveCodexStyle(name,out glyphKind,out accent);
        Color stateColor=isEvolved?new Color(1f,.82f,.22f):isReady?new Color(.1f,1f,.78f):new Color(.42f,.54f,.68f);
        string state=isEvolved?"ATIVA":isReady?"PRONTA":"EM PROGRESSO";
        img.color=isEvolved?new Color(.105f,.083f,.025f,.98f):isReady?new Color(.025f,.12f,.11f,.98f):new Color(.035f,.052f,.085f,.97f);
        outline.effectColor=Color.Lerp(accent,stateColor,.45f);outline.effectDistance=new Vector2(isEvolved?2.5f:1.5f,isEvolved?-2.5f:-1.5f);

        RectTransform cardRect=card.GetComponent<RectTransform>();
        var stripe=NeonUI.Panel("Accent",card.transform,new Vector2(0,1),Vector2.zero,new Vector2(4,180),accent);stripe.raycastTarget=false;
        var iconWell=NeonUI.Panel("IconWell",card.transform,new Vector2(0,1),new Vector2(20,-20),new Vector2(68,68),new Color(accent.r,accent.g,accent.b,.11f));
        var iconOutline=iconWell.gameObject.AddComponent<Outline>();iconOutline.effectColor=new Color(accent.r,accent.g,accent.b,.65f);iconOutline.effectDistance=new Vector2(1,-1);
        var glyph=NeonUI.Rect("WeaponGlyph",iconWell.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(52,52)).gameObject.AddComponent<CodexWeaponGlyph>();
        glyph.kind=glyphKind;glyph.color=accent;glyph.raycastTarget=false;

        Text title=NeonUI.Label("WeaponName",card.transform,name,16,isEvolved?new Color(1f,.85f,.3f):NeonUI.White,new Vector2(0,1),new Vector2(104,-15),new Vector2(255,27));title.fontStyle=FontStyle.Bold;
        Image pill=NeonUI.Panel("Status",card.transform,new Vector2(1,1),new Vector2(-15,-15),new Vector2(126,23),new Color(stateColor.r,stateColor.g,stateColor.b,.13f));
        Text status=NeonUI.Label("Label",pill.transform,state,9,stateColor,new Vector2(.5f,.5f),Vector2.zero,new Vector2(118,21),TextAnchor.MiddleCenter);status.fontStyle=FontStyle.Bold;
        NeonUI.Panel("Divider",card.transform,new Vector2(0,1),new Vector2(104,-49),new Vector2(393,1),new Color(accent.r,accent.g,accent.b,.32f));

        Text recipeLabel=NeonUI.Label("RecipeLabel",card.transform,"RECEITA",9,accent,new Vector2(0,1),new Vector2(104,-58),new Vector2(390,15));recipeLabel.fontStyle=FontStyle.Bold;
        Text recipeValue=NeonUI.Label("RecipeValue",card.transform,recipe,11,NeonUI.White,new Vector2(0,1),new Vector2(104,-73),new Vector2(390,27));recipeValue.verticalOverflow=VerticalWrapMode.Truncate;
        Text effectLabel=NeonUI.Label("EffectLabel",card.transform,"EFEITO",9,accent,new Vector2(0,1),new Vector2(104,-105),new Vector2(390,15));effectLabel.fontStyle=FontStyle.Bold;
        Text effectValue=NeonUI.Label("EffectValue",card.transform,effect,10,NeonUI.Muted,new Vector2(0,1),new Vector2(104,-120),new Vector2(390,34));effectValue.verticalOverflow=VerticalWrapMode.Truncate;

        float completion=CodexCompletion(gm,glyphKind,isEvolved);
        Text progress=NeonUI.Label("Progress",card.transform,progressText,9,stateColor,new Vector2(0,1),new Vector2(20,-151),new Vector2(475,15));
        progress.alignment=TextAnchor.MiddleLeft;
        Image track=NeonUI.Panel("ProgressTrack",card.transform,new Vector2(0,1),new Vector2(20,-169),new Vector2(476,4),new Color(.12f,.16f,.22f,1));
        Image fill=NeonUI.Panel("Fill",track.transform,Vector2.zero,Vector2.zero,Vector2.zero,stateColor);fill.rectTransform.anchorMax=new Vector2(completion,1);
    }

    static void ResolveCodexStyle(string name,out CodexGlyphKind kind,out Color accent)
    {
        if(name.Contains("SUPERNOVA")){kind=CodexGlyphKind.Blaster;accent=new Color(1f,.68f,.18f);}
        else if(name.Contains("NEBULAR")){kind=CodexGlyphKind.Shotgun;accent=new Color(.78f,.42f,1f);}
        else if(name.Contains("ANTI-MATÉRIA")){kind=CodexGlyphKind.Railgun;accent=new Color(.25f,.9f,1f);}
        else if(name.Contains("VÓRTICE")){kind=CodexGlyphKind.Void;accent=new Color(.62f,.3f,1f);}
        else if(name.Contains("TESLA")){kind=CodexGlyphKind.Drone;accent=new Color(.2f,.82f,1f);}
        else if(name.Contains("BARREIRA")){kind=CodexGlyphKind.Shield;accent=new Color(1f,.77f,.22f);}
        else if(name.Contains("SUNFIRE")){kind=CodexGlyphKind.Plasma;accent=new Color(1f,.34f,.08f);}
        else if(name.Contains("IÔNICA")){kind=CodexGlyphKind.Arc;accent=new Color(.08f,.88f,1f);}
        else if(name.Contains("EVENT")){kind=CodexGlyphKind.Singularity;accent=new Color(.72f,.28f,1f);}
        else {kind=CodexGlyphKind.Seeker;accent=new Color(.32f,1f,.58f);}
    }

    static float CodexCompletion(GameManager gm,CodexGlyphKind kind,bool evolved)
    {
        if(evolved)return 1f;if(gm==null)return 0f;
        float a=0,b=0;
        switch(kind)
        {
            case CodexGlyphKind.Blaster:a=gm.GetUpgradeLevel(GameManager.UpgradeType.BlasterMastery)/5f;b=gm.GetUpgradeLevel(GameManager.UpgradeType.OverchargeCapacitor)/3f;break;
            case CodexGlyphKind.Shotgun:a=gm.GetUpgradeLevel(GameManager.UpgradeType.ShotgunMastery)/5f;b=gm.GetUpgradeLevel(GameManager.UpgradeType.ScatterMatrix)/3f;break;
            case CodexGlyphKind.Railgun:a=gm.GetUpgradeLevel(GameManager.UpgradeType.RailgunMastery)/5f;b=gm.GetUpgradeLevel(GameManager.UpgradeType.AntimatterCore)/3f;break;
            case CodexGlyphKind.Void:a=gm.GetUpgradeLevel(GameManager.UpgradeType.OrbitalMines)/3f;b=gm.GetUpgradeLevel(GameManager.UpgradeType.Explosive);break;
            case CodexGlyphKind.Drone:a=gm.GetUpgradeLevel(GameManager.UpgradeType.SentinelDrone)/3f;b=Mathf.Max(gm.GetUpgradeLevel(GameManager.UpgradeType.Pierce),gm.GetUpgradeLevel(GameManager.UpgradeType.Bounce));break;
            case CodexGlyphKind.Shield:a=gm.GetUpgradeLevel(GameManager.UpgradeType.AegisShield)/3f;b=gm.GetUpgradeLevel(GameManager.UpgradeType.LifeSteal);break;
            case CodexGlyphKind.Plasma:a=gm.GetUpgradeLevel(GameManager.UpgradeType.PlasmaFlamerMastery)/5f;b=gm.GetUpgradeLevel(GameManager.UpgradeType.PlasmaReactor)/3f;break;
            case CodexGlyphKind.Arc:a=gm.GetUpgradeLevel(GameManager.UpgradeType.ArcDischargerMastery)/5f;b=gm.GetUpgradeLevel(GameManager.UpgradeType.IonicCapacitor)/3f;break;
            case CodexGlyphKind.Singularity:a=gm.GetUpgradeLevel(GameManager.UpgradeType.SingularityMastery)/5f;b=gm.GetUpgradeLevel(GameManager.UpgradeType.GravityAmplifier)/3f;break;
            case CodexGlyphKind.Seeker:a=gm.GetUpgradeLevel(GameManager.UpgradeType.SeekerSwarmMastery)/5f;b=gm.GetUpgradeLevel(GameManager.UpgradeType.GuidanceMatrix)/3f;break;
        }
        return Mathf.Clamp01((a+b)*.5f);
    }

    private static GameObject currentPlanetBanner;

    public static void ShowPlanetBanner(string planetName,string hazardDesc,Color themeColor)
    {
        var canvas=GameObject.Find("CanvasHUD"); if(canvas==null) return;
        if(currentPlanetBanner!=null) { currentPlanetBanner.SetActive(false); Object.Destroy(currentPlanetBanner); }
        Transform parent=canvas.transform.Find("Telemetry"); if(parent==null) parent=canvas.transform;
        var panel=NeonUI.Panel("PlanetBanner",parent,new Vector2(.5f,1),new Vector2(0,-50),new Vector2(550,62),NeonUI.Surface);
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
