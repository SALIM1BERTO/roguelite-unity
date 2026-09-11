using UnityEngine;
using UnityEngine.UI;

public sealed class RouteEventDirector : MonoBehaviour
{
    public static RouteEventDirector Instance{get;private set;}
    public static bool EventActive=>Instance!=null&&Instance.active;
    public static float PressureMultiplier=>EventActive?Instance.currentPressure:1f;
    public static float XpMultiplier=>EventActive?Instance.currentXp:1f;
    public static string CurrentTitle=>EventActive?Instance.title:string.Empty;
    GameManager game;bool active;float nextEvent=90f,endTime,currentPressure=1f,currentXp=1f;int eventIndex;string title;GameObject hud;Text titleText,timeText;Image fill;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetState(){Instance=null;}
    public static void Begin(GameManager manager)
    {
        if(manager==null)return;RouteEventDirector director=manager.GetComponent<RouteEventDirector>();if(director==null)director=manager.gameObject.AddComponent<RouteEventDirector>();director.game=manager;Instance=director;director.BuildHud();
    }
    public static string TitleFor(ExpeditionRoute route){switch(route){case ExpeditionRoute.PyreGauntlet:return "TEMPESTADE SOLAR";case ExpeditionRoute.VoidSpiral:return "COLAPSO VETORIAL";case ExpeditionRoute.ColossusCircuit:return "CERCO TITÂNICO";default:return "PULSO PRISMÁTICO";}}
    public static float EventPressureFor(ExpeditionRoute route){switch(route){case ExpeditionRoute.PyreGauntlet:return 1.30f;case ExpeditionRoute.VoidSpiral:return 1.40f;case ExpeditionRoute.ColossusCircuit:return 1.55f;default:return 1.12f;}}
    public static float EventXpFor(ExpeditionRoute route){switch(route){case ExpeditionRoute.PyreGauntlet:return 1.25f;case ExpeditionRoute.VoidSpiral:return 1.35f;case ExpeditionRoute.ColossusCircuit:return 1.40f;default:return 1.15f;}}
    public static float DurationFor(ExpeditionRoute route){switch(route){case ExpeditionRoute.PyreGauntlet:return 26f;case ExpeditionRoute.VoidSpiral:return 28f;case ExpeditionRoute.ColossusCircuit:return 30f;default:return 24f;}}
    public static int RewardFor(ExpeditionRoute route){switch(route){case ExpeditionRoute.PyreGauntlet:return 6;case ExpeditionRoute.VoidSpiral:return 8;case ExpeditionRoute.ColossusCircuit:return 10;default:return 4;}}
    void BuildHud()
    {
        if(hud!=null)return;GameObject canvas=GameObject.Find("CanvasHUD");if(canvas==null)return;Transform parent=canvas.transform.Find("Telemetry");if(parent==null)parent=canvas.transform;
        Image panel=NeonUI.Panel("RouteEventHUD",parent,new Vector2(.5f,1),new Vector2(0,-115),new Vector2(500,64),new Color(.018f,.03f,.06f,.97f));hud=panel.gameObject;Outline outline=hud.AddComponent<Outline>();outline.effectDistance=new Vector2(2,-2);
        titleText=NeonUI.Label("Title",hud.transform,"",13,NeonUI.White,new Vector2(0,1),new Vector2(18,-10),new Vector2(350,22));titleText.fontStyle=FontStyle.Bold;timeText=NeonUI.Label("Time",hud.transform,"",13,NeonUI.White,new Vector2(1,1),new Vector2(-18,-10),new Vector2(90,22),TextAnchor.MiddleRight);timeText.fontStyle=FontStyle.Bold;
        Image track=NeonUI.Panel("Track",hud.transform,new Vector2(.5f,0),new Vector2(0,10),new Vector2(464,5),new Color(.1f,.14f,.22f,1));fill=NeonUI.Panel("Fill",track.transform,Vector2.zero,Vector2.zero,Vector2.zero,NeonUI.Cyan);fill.rectTransform.anchorMax=Vector2.one;hud.SetActive(false);
    }
    void Update()
    {
        if(game==null||game.isGameOver)return;if(!active&&game.matchTime>=nextEvent)StartEvent();if(!active)return;float remaining=Mathf.Max(0,endTime-game.matchTime),duration=DurationFor(ExpeditionRouteSystem.Selected);if(timeText!=null)timeText.text=remaining.ToString("00.0")+" s";if(fill!=null)fill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(remaining/duration),1);if(remaining<=0)CompleteEvent();
    }
    void StartEvent()
    {
        ExpeditionRoute route=ExpeditionRouteSystem.Selected;active=true;eventIndex++;title=TitleFor(route);currentPressure=EventPressureFor(route);currentXp=EventXpFor(route);float duration=DurationFor(route);endTime=game.matchTime+duration;Color color=ExpeditionRouteSystem.Color(route);BuildHud();if(hud!=null){hud.SetActive(true);hud.GetComponent<Outline>().effectColor=new Color(color.r,color.g,color.b,.85f);titleText.text="ANOMALIA  /  "+title;titleText.color=color;timeText.text=duration.ToString("00.0")+" s";fill.color=color;fill.rectTransform.anchorMax=Vector2.one;}RuntimeUIBuilder.ShowPlanetBanner(title,"SURTO ATIVO  •  XP +"+Mathf.RoundToInt((currentXp-1)*100)+"%  •  SOBREVIVA PARA RECUPERAR CÉLULAS",color);GameAudio.Play(AudioCue.LevelUp);
    }
    void CompleteEvent()
    {
        if(!active)return;ExpeditionRoute route=ExpeditionRouteSystem.Selected;int reward=RewardFor(route)+(eventIndex-1)*2;active=false;currentPressure=currentXp=1f;nextEvent=game.matchTime+150f;if(hud!=null)hud.SetActive(false);RunEconomy.CollectCells(reward);ProgressionContracts.RecordEvent(title+" SUPERADO  +"+reward+" ★");RuntimeUIBuilder.ShowPlanetBanner("EVENTO SUPERADO",title+"  •  +"+reward+" CÉLULAS RECUPERADAS",ExpeditionRouteSystem.Color(route));GameAudio.Play(AudioCue.Upgrade);
    }
#if UNITY_EDITOR
    public void DebugTriggerEvent(){if(!active)StartEvent();}
    public void DebugCompleteEvent(){CompleteEvent();}
#endif
}
