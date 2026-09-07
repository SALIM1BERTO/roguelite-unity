using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ModernHUD : MonoBehaviour
{
    GameManager game;
    Text health, experience, clock, sector, controls;
    RectTransform healthFill;
    Image healthImage;
    CanvasGroup group;
    int lastHp=int.MinValue,lastMaxHp,lastXp=-1,lastLevel,lastThreshold,lastSecond=-1;
    PlanetGravity lastPlanet;
    bool gamepad;

    public static void Create(GameManager game)
    {
        if (FindAnyObjectByType<ModernHUD>()!=null) return;
        foreach(UIManager legacy in FindObjectsByType<UIManager>()) legacy.gameObject.SetActive(false);
        GameObject old=GameObject.Find("CanvasHUD");
        if(old!=null) { old.SetActive(false); Destroy(old); }
        var root=new GameObject("CanvasHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder=20;
        var scaler=root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);
        scaler.matchWidthOrHeight=0f;
        ModernHUD hud=root.AddComponent<ModernHUD>(); hud.game=game;
        RectTransform content=NeonUI.Rect("Telemetry",root.transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
        content.anchorMin=Vector2.zero; content.anchorMax=Vector2.one;
        hud.group=content.gameObject.AddComponent<CanvasGroup>(); hud.group.blocksRaycasts=false;
        Vector2 tl=new Vector2(0,1), bl=Vector2.zero;
        NeonUI.Label("HealthCaption",content,"INTEGRIDADE",11,NeonUI.Muted,tl,new Vector2(32,-28),new Vector2(220,20));
        hud.health=NeonUI.Label("HealthValue",content,"",26,NeonUI.White,tl,new Vector2(32,-47),new Vector2(220,34));
        var bar=NeonUI.Panel("HealthTrack",content,tl,new Vector2(32,-91),new Vector2(220,3),new Color(.15f,.2f,.25f));
        hud.healthImage=NeonUI.Panel("HealthFill",bar.transform,Vector2.zero,Vector2.zero,Vector2.zero,NeonUI.Cyan);
        hud.healthFill=hud.healthImage.rectTransform; hud.healthFill.anchorMax=Vector2.one;
        hud.clock=NeonUI.Label("RunTime",content,"00:00",25,NeonUI.White,new Vector2(.5f,1),new Vector2(0,-30),new Vector2(150,34),TextAnchor.MiddleCenter);
        hud.sector=NeonUI.Label("Sector",content,"ÓRBITA 01",10,NeonUI.Muted,new Vector2(.5f,1),new Vector2(0,-65),new Vector2(180,20),TextAnchor.MiddleCenter);
        game.levelText=NeonUI.Label("LevelText",content,"",20,NeonUI.White,bl,new Vector2(32,57),new Vector2(150,32));
        hud.experience=NeonUI.Label("Experience",content,"",12,NeonUI.Muted,bl,new Vector2(190,61),new Vector2(130,24),TextAnchor.MiddleRight);
        var xpBar=NeonUI.Panel("ExperienceTrack",content,bl,new Vector2(32,43),new Vector2(288,3),new Color(.15f,.2f,.25f));
        game.xpFill=NeonUI.Panel("ExperienceFill",xpBar.transform,Vector2.zero,Vector2.zero,Vector2.zero,NeonUI.Violet).rectTransform;
        game.xpFill.anchorMax=new Vector2(0,1);
        NeonUI.Label("Brand",content,"N E O N   W O R L D S",10,NeonUI.Muted,bl,new Vector2(32,16),new Vector2(300,18));
        hud.controls=NeonUI.Label("ControlHint",content,"WASD  MOVER    /    MOUSE  MIRAR",10,NeonUI.Muted,new Vector2(1,0),new Vector2(-32,24),new Vector2(320,20),TextAnchor.MiddleRight);
        Button pause=NeonUI.Panel("Pause",root.transform,new Vector2(1,1),new Vector2(-32,-78),new Vector2(100,28),NeonUI.Surface,true).gameObject.AddComponent<Button>();
        pause.targetGraphic=pause.GetComponent<Image>(); NeonUI.StyleButton(pause);
        NeonUI.Label("Label",pause.transform,"PAUSA / ESC",10,NeonUI.Muted,new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,28),TextAnchor.MiddleCenter);
        pause.onClick.AddListener(game.TogglePauseMenu);
        hud.Refresh();
        RuntimeUIBuilder.BuildLevelUpUI(game);
    }
    void Update()
    {
        if(game==null) return;
        int second=Mathf.FloorToInt(game.matchTime);
        if(second!=lastSecond) { lastSecond=second; clock.text=(second/60).ToString("00")+":"+(second%60).ToString("00"); }
        Refresh();
        group.alpha=(game.levelUpPanel!=null && game.levelUpPanel.activeSelf) || RuntimeUIBuilder.HasEndScreen || game.isPaused ? 0f : 1f;
        float ratio=game.maxHp>0 ? Mathf.Clamp01((float)game.hp/game.maxHp) : 0f;
        healthFill.anchorMax=new Vector2(Mathf.Lerp(healthFill.anchorMax.x,ratio,1f-Mathf.Exp(-12f*Time.unscaledDeltaTime)),1);
        bool connected=Gamepad.current!=null;
        if(connected!=gamepad) { gamepad=connected; controls.text=connected ? "ANALÓGICO ESQ.  MOVER    /    DIR.  MIRAR" : "WASD  MOVER    /    MOUSE  MIRAR"; }
    }
    void Refresh()
    {
        if(lastHp!=game.hp || lastMaxHp!=game.maxHp)
        {
            lastHp=game.hp; lastMaxHp=game.maxHp;
            health.text=Mathf.Max(0,game.hp)+"  /  "+game.maxHp;
            healthImage.color=game.hp<=game.maxHp*.25f ? NeonUI.Danger : NeonUI.Cyan;
        }
        if(lastXp!=game.xp || lastLevel!=game.level || lastThreshold!=game.xpToNextLevel)
        {
            lastXp=game.xp; lastLevel=game.level; lastThreshold=game.xpToNextLevel;
            experience.text=game.xp+" / "+game.xpToNextLevel+" XP";
        }
        GravityBody body=game.player!=null ? game.player.GetComponent<GravityBody>() : null;
        if(body!=null && body.planet!=lastPlanet)
        {
            lastPlanet=body.planet;
            sector.text=lastPlanet!=null ? "ÓRBITA "+lastPlanet.name.Replace("Planet_","").PadLeft(2,'0') : "ESPAÇO ABERTO";
        }
    }
}
