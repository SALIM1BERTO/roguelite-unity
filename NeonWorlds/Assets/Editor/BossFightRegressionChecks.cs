using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BossFightRegressionChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static double started,pauseStarted;
    static int step,assertions;
    static bool active,oldInvincible,oldWeapon,oldSpawner,hadCores;
    static int oldHp,cores;
    static GameManager game;
    static EnemySpawner spawner;
    static BossLeviathan boss;
    static Vector3 pausedPosition;
    public static bool Complete { get; private set; }
    public static int Assertions => assertions;
    public static void RunBatch() { NeonWorldsRegressionChecks.RunBatch(); }
    static void Check(bool condition,string message)
    {
        if(!condition) throw new Exception("Boss regression: "+message);
        assertions++;
    }
    static void Invoke(object target,string name,params object[] args) { target.GetType().GetMethod(name,Private).Invoke(target,args); }
    public static void Begin()
    {
        active=true; Complete=false; step=0; assertions=0;
        game=GameManager.Instance; spawner=EnemySpawner.Instance;
        oldInvincible=game.isInvincible; oldHp=game.hp; game.isInvincible=true;
        oldWeapon=game.player.GetComponent<Weapon>().enabled; game.player.GetComponent<Weapon>().enabled=false;
        oldSpawner=spawner.enabled; spawner.enabled=false;
        hadCores=PlayerPrefs.HasKey(MetaProgression.KEY_CORES); cores=MetaProgression.GetStarCores();
        Time.timeScale=1; GameAudio.SetGameplayPaused(false);
        started=EditorApplication.timeSinceStartup;
        spawner.SpawnBoss();
        Check(BossIntroSequence.isIntroPlaying,"Arrival sequence begins through the real spawner");
    }
    public static void Tick()
    {
        if(!active || Complete) return;
        double elapsed=EditorApplication.timeSinceStartup-started;
        if(elapsed>45) throw new Exception("Boss checks timed out at step "+step);
        if(boss==null) boss=BossLeviathan.Instance;
        if(step==0)
        {
            if(boss==null || boss.State==BossLeviathan.FightState.Deploy) return;
            Check(GameObject.Find("PlanetaryShockwave")==null,"Intro has no giant opaque shockwave");
            Check(boss.transform.lossyScale.x<1.001f,"Boss root cancels planet scale");
            foreach(Teleporter portal in Object.FindObjectsByType<Teleporter>())
            {
                Transform barrier=portal.transform.Find("LockBarrier");
                Check(barrier!=null && barrier.GetComponent<LineRenderer>()!=null && barrier.lossyScale.x<1.01f,"Portal lock uses a small ring, not a planet-sized sphere");
            }
            CheckVisualSizes(); CheckDamageText(); CheckBossImpacts();
            Capture("boss-scale-and-damage.png"); step=1;
        }
        else if(step==1)
        {
            if(boss.State==BossLeviathan.FightState.Windup && !captures.Contains("boss-nova-warning.png"))
            {
                Check((bool)typeof(BossLeviathan).GetMethod("IsVisibleToPlayer",Private).Invoke(boss,null),"Boss is visible during its first warning");
                CaptureOnce("boss-nova-warning.png");
            }
            if(boss.ActiveProjectileCount==0) return;
            Check(boss.ActiveProjectileCount<=8,"First nova has a limited projectile count and a safe gap");
            CheckProjectile(); Capture("boss-nova.png");
            boss.TakeDamage(Mathf.CeilToInt(boss.maxHp*.4f));
            Check(boss.currentPhase==2 && boss.State==BossLeviathan.FightState.Recovery,"Phase two grants a recovery window");
            Check(boss.ActiveProjectileCount==0,"Phase transition clears active projectiles"); step=2;
        }
        else if(step==2)
        {
            if(boss.State!=BossLeviathan.FightState.Windup || boss.AttackHint!="INVESTIDA") return;
            Capture("boss-dash-warning.png");
            pausedPosition=boss.transform.position; Time.timeScale=0;
            pauseStarted=EditorApplication.timeSinceStartup; step=3;
        }
        else if(step==3)
        {
            if(EditorApplication.timeSinceStartup-pauseStarted<.35) return;
            Check((pausedPosition-boss.transform.position).sqrMagnitude<.0001f && boss.State==BossLeviathan.FightState.Windup,"Pause freezes the attack windup");
            Time.timeScale=1; step=4;
        }
        else if(step==4)
        {
            if(boss.State!=BossLeviathan.FightState.Dash) return;
            step=5;
        }
        else if(step==5)
        {
            if(boss.State!=BossLeviathan.FightState.Recovery) return;
            Check(Vector3.Distance(pausedPosition,boss.transform.position)>.5f,"Telegraphed dash travels along the planet");
            boss.TakeDamage(Mathf.CeilToInt(boss.maxHp*.4f));
            Check(boss.currentPhase==3,"Phase three activates below 25 percent HP"); step=6;
        }
        else if(step==6)
        {
            if(boss.ActiveProjectileCount==0) return;
            Check(boss.ActiveProjectileCount<=12,"Final phase keeps a bounded projectile count");
            Invoke(boss,"SpawnMinionWave"); Invoke(boss,"SpawnMinionWave"); Invoke(boss,"SpawnMinionWave"); Invoke(boss,"SpawnMinionWave");
            Check(boss.EscortCount<=3,"Final phase caps living escorts at three");
            CheckContact();
            int before=MetaProgression.GetStarCores();
            boss.TakeDamage(100000); int rewarded=MetaProgression.GetStarCores(); boss.TakeDamage(100000);
            Check(rewarded>before && MetaProgression.GetStarCores()==rewarded,"Boss death grants its reward only once");
            Check(boss.ActiveProjectileCount==0 && !spawner.isBossActive,"Victory clears projectiles and ends boss state");
            Check(RuntimeUIBuilder.HasEndScreen,"Victory screen still opens");
            Complete=true; End();
            Debug.Log("NEON_BOSS: PASS; "+assertions+" assertions; real arrival, three phases, warnings, dash, pause, pools and victory.");
        }
    }
    static void CheckVisualSizes()
    {
        Transform parent=boss.transform.parent;
        Vector3 originalPosition=boss.transform.position;
        Quaternion originalRotation=boss.transform.rotation;
        var testPlanet=new GameObject("BossScaleTestParent");
        try
        {
            foreach(float scale in new[]{10f,100f,200f})
            {
                testPlanet.transform.localScale=Vector3.one*scale;
                boss.transform.SetParent(testPlanet.transform,true);
                boss.transform.SetPositionAndRotation(originalPosition,originalRotation);
                Invoke(boss,"LateUpdate");
                Bounds bounds=new Bounds(boss.transform.position,Vector3.zero);
                foreach(Renderer renderer in boss.transform.Find("BossVisual").GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
                Check(bounds.size.x<5f && bounds.size.y<5f && bounds.size.z<5f,"Boss visual stays under five world units on scale "+scale);
                Check(Mathf.Abs(boss.GetComponent<SphereCollider>().radius*boss.transform.lossyScale.x-.95f)<.001f,"Boss hitbox stays constant on scale "+scale);
            }
        }
        finally { boss.transform.SetParent(parent,true); boss.transform.SetPositionAndRotation(originalPosition,originalRotation); Object.DestroyImmediate(testPlanet); }
    }
    static void CheckDamageText()
    {
        var parent=new GameObject("DamageScaleTest");
        try
        {
            foreach(float scale in new[]{10f,100f,200f})
            {
                parent.transform.localScale=Vector3.one*scale;
                foreach(bool critical in new[]{false,true})
                {
                    var obj=new GameObject("DamageSizeProbe"); obj.transform.SetParent(parent.transform,false);
                    obj.transform.position=game.player.position+game.player.up;
                    FloatingText text=obj.AddComponent<FloatingText>(); text.SetupDamage(125,critical); Invoke(text,"LateUpdate");
                    Renderer renderer=obj.GetComponent<Renderer>();
                    Camera camera=Camera.main; Bounds local=renderer.localBounds;
                    Vector3 bottom=obj.transform.TransformPoint(local.center-Vector3.up*local.extents.y);
                    Vector3 top=obj.transform.TransformPoint(local.center+Vector3.up*local.extents.y);
                    float pixels=Mathf.Abs(camera.WorldToScreenPoint(top).y-camera.WorldToScreenPoint(bottom).y);
                    Check(pixels>5f && pixels<24f,"Damage label is small on screen on scale "+scale+"; pixels="+pixels);
                    Object.DestroyImmediate(obj);
                }
            }
        }
        finally { Object.DestroyImmediate(parent); }
        for(int i=0;i<42;i++)
        {
            var obj=new GameObject("FloatingText"); obj.transform.position=game.player.position+Camera.main.transform.right*((i%7)-3)*.35f+game.player.up*(.7f+(i%3)*.25f);
            obj.transform.SetParent(game.player.parent,true); obj.AddComponent<FloatingText>().SetupDamage(15+i,i%5==0);
        }
        Check(Object.FindObjectsByType<FloatingText>().Length<=32,"Simultaneous damage labels are capped");
    }
    static void CheckBossImpacts()
    {
        var obj=new GameObject("PiercingBossImpactCheck"); obj.AddComponent<Rigidbody>();
        Bullet bullet=obj.AddComponent<Bullet>(); bullet.pierceCount=3; bullet.damage=7;
        int before=boss.hp;
        Invoke(bullet,"HandleHit",boss.gameObject); Invoke(bullet,"HandleHit",boss.gameObject);
        Check(boss.hp==before-7,"Repeated callbacks from a piercing bullet hit the boss once");
        obj.SetActive(false); obj.SetActive(true); Invoke(bullet,"HandleHit",boss.gameObject);
        Check(boss.hp==before-14,"A reused projectile may hit the boss again");
        Object.DestroyImmediate(obj);
    }
    static void CheckProjectile()
    {
        BossProjectile projectile=Object.FindAnyObjectByType<BossProjectile>();
        Check(projectile!=null && projectile.transform.lossyScale.x<.3f,"Boss projectiles are small in world units");
        Check(projectile.lifeTime<=3f,"Projectiles expire before filling the orbit");
        int count=boss.ActiveProjectileCount;
        Invoke(projectile,"HandleHit",game.player.gameObject); Invoke(projectile,"HandleHit",game.player.gameObject);
        Check(boss.ActiveProjectileCount==count-1 && !projectile.gameObject.activeSelf,"Duplicate projectile contacts return to pool once");
    }
    static void CheckContact()
    {
        Vector3 position=boss.transform.position;
        var state=typeof(BossLeviathan).GetProperty("State");
        object previous=state.GetValue(boss);
        state.SetValue(boss,BossLeviathan.FightState.Approach);
        game.isInvincible=false; int hp=game.hp;
        try
        {
            Transform planet=game.player.parent;
            boss.transform.position=planet.position-(game.player.position-planet.position);
            Invoke(boss,"Update"); Check(game.hp==hp,"Opposite hemisphere causes no phantom contact damage");
        }
        finally { boss.transform.position=position; state.SetValue(boss,previous); game.hp=hp; game.isInvincible=true; }
    }
    static readonly System.Collections.Generic.HashSet<string> captures=new System.Collections.Generic.HashSet<string>();
    static void CaptureOnce(string name) { if(captures.Add(name)) Capture(name); }
    static void Capture(string name)
    {
        foreach(FloatingText text in Object.FindObjectsByType<FloatingText>()) Invoke(text,"LateUpdate");
        typeof(VisualRegressionChecks).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1280,720});
    }
    public static void End()
    {
        if(!active) return; active=false;
        if(game!=null) { game.isInvincible=oldInvincible; game.hp=oldHp; if(game.player!=null) game.player.GetComponent<Weapon>().enabled=oldWeapon; }
        if(spawner!=null) spawner.enabled=oldSpawner;
        if(hadCores) PlayerPrefs.SetInt(MetaProgression.KEY_CORES,cores); else PlayerPrefs.DeleteKey(MetaProgression.KEY_CORES);
        PlayerPrefs.Save(); Time.timeScale=1f;
    }
}
