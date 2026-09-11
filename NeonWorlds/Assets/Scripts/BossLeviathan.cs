using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class BossLeviathan : MonoBehaviour
{
    public static BossLeviathan Instance;
    [Header("Combat (world units)")]
    public int maxHp=25000;
    public int hp;
    public float baseSpeed=4.2f;
    public int contactDamage=12;
    public float contactCooldown=.9f;
    [Range(.2f,.6f)] public float visualScale=.38f;
    public int currentPhase=1;
    public enum FightState { Deploy, Approach, Windup, Dash, Recovery, Dead }
    public FightState State { get; private set; }
    enum Attack { Nova, Dash, Reposition }
    Attack attack;
    float stateTimer,attackTimer=2.4f,lastContactTime=-10f,farTimer,flashUntil;
    int attackIndex;
    bool isDead;
    Vector3 attackDirection,arrivalPosition;
    Transform visualRoot,coreTransform,eyeTransform,innerRingPivot,outerRingPivot;
    Material coreMat,eyeMat,armorMat,projectileMat,warningMat;
    Color currentColor;
    readonly List<Transform> innerCannons=new List<Transform>(),outerFins=new List<Transform>(),armorPlates=new List<Transform>();
    readonly List<Enemy> escorts=new List<Enemy>();
    readonly HashSet<BossProjectile> projectiles=new HashSet<BossProjectile>();
    ObjectPool<BossProjectile> projectilePool;
    LineRenderer telegraph;
    readonly Vector3[] warningPoints=new Vector3[33];
    GravityBody gravityBody;
    Transform Planet => gravityBody!=null && gravityBody.planet!=null ? gravityBody.planet.transform : transform.parent;
    public string AttackHint => State==FightState.Windup ? (attack==Attack.Dash ? "INVESTIDA" : attack==Attack.Nova ? "NOVA" : "REPOSICIONANDO") : State==FightState.Recovery ? "RECUPERANDO" : "FASE "+currentPhase;
    public int ActiveProjectileCount => projectiles.Count;
    public int EscortCount { get { escorts.RemoveAll(e=>e==null || e.isDead || !e.gameObject.activeSelf); return escorts.Count; } }

    void Awake()
    {
        Instance=this; hp=maxHp;
        BossWorldMotion.SetWorldScale(transform,Vector3.one);
        gravityBody=GetComponent<GravityBody>();
        if(gravityBody==null) gravityBody=gameObject.AddComponent<GravityBody>();
        gravityBody.surfaceOffset=.8f;
        if(transform.parent!=null) gravityBody.planet=transform.parent.GetComponent<PlanetGravity>();
        visualRoot=new GameObject("BossVisual").transform;
        visualRoot.SetParent(transform,false); visualRoot.localScale=Vector3.one*visualScale*.1f;
        BuildVisuals();
        SphereCollider hitbox=GetComponent<SphereCollider>(); if(hitbox==null) hitbox=gameObject.AddComponent<SphereCollider>();
        hitbox.radius=.95f; hitbox.isTrigger=false;
        Rigidbody body=GetComponent<Rigidbody>(); if(body==null) body=gameObject.AddComponent<Rigidbody>();
        body.isKinematic=true; body.useGravity=false;
        telegraph=new GameObject("AttackWarning").AddComponent<LineRenderer>();
        telegraph.transform.SetParent(transform,false); telegraph.useWorldSpace=true;
        warningMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        warningMat.SetColor("_BaseColor",new Color(1f,.67f,.22f));
        telegraph.sharedMaterial=warningMat; telegraph.widthMultiplier=.1f;
        telegraph.positionCount=0;
        projectileMat=new Material(Shader.Find("NeonWorlds/EmissiveGeometry") ?? Shader.Find("Universal Render Pipeline/Lit"));
        projectileMat.SetColor("_BaseColor",new Color(1f,.24f,.42f));
        projectileMat.SetColor("_EmissionColor",new Color(1f,.24f,.42f)*1.5f);
        projectilePool=new ObjectPool<BossProjectile>(CreateProjectile,p=>p.gameObject.SetActive(true),p=>p.gameObject.SetActive(false),p=>{ if(p!=null) Destroy(p.gameObject); },true,24,48);
        State=FightState.Deploy; stateTimer=1.1f;
        SetPhase(1);
    }
    void Start()
    {
        gravityBody.SnapToSurface();
        Teleporter.LockAllTeleporters();
        // Returning mobs to their pools avoids a screen full of 9999-damage labels.
        foreach(Enemy enemy in new List<Enemy>(Enemy.activeEnemies)) DespawnEnemy(enemy);
        RuntimeUIBuilder.BuildBossHealthBar(this);
        RuntimeUIBuilder.UpdateBossHP(hp,maxHp);
    }
    void LateUpdate() { BossWorldMotion.SetWorldScale(transform,Vector3.one); }
    void OnTransformParentChanged() { BossWorldMotion.SetWorldScale(transform,Vector3.one); }
    void Update()
    {
        if(isDead || Time.timeScale<=0 || Planet==null) return;
        float dt=Time.deltaTime;
        if(State==FightState.Deploy)
        {
            stateTimer-=dt;
            visualRoot.localScale=Vector3.one*visualScale*Mathf.SmoothStep(.1f,1f,1f-stateTimer/1.1f);
            if(stateTimer<=0) { visualRoot.localScale=Vector3.one*visualScale; State=FightState.Recovery; stateTimer=1f; GameManager.Instance?.CheckPendingLevelUp(); }
            return;
        }
        innerRingPivot.Rotate(0,(35+currentPhase*20)*dt,0);
        outerRingPivot.Rotate(0,-(25+currentPhase*15)*dt,0);
        Color color=Time.time<flashUntil ? Color.white : State==FightState.Windup ? new Color(1f,.67f,.22f) : currentColor;
        coreMat.SetColor("_BaseColor",color); coreMat.SetColor("_EmissionColor",color*(State==FightState.Windup ? 2.2f : 1.6f));
        GameManager game=GameManager.Instance;
        if(game==null || game.player==null || game.isGameOver || RuntimeUIBuilder.HasEndScreen) return;
        GravityBody playerBody=game.player.GetComponent<GravityBody>();
        if(playerBody!=null && playerBody.planet!=gravityBody.planet) return;
        Vector3 normal=(transform.position-Planet.position).normalized;
        Vector3 toPlayer=game.player.position-transform.position;
        Vector3 tangent=Vector3.ProjectOnPlane(toPlayer,normal).normalized;
        float distance=toPlayer.magnitude;
        if(tangent.sqrMagnitude<.001f) tangent=Vector3.ProjectOnPlane(transform.forward,normal).normalized;
        eyeTransform.localPosition=visualRoot.InverseTransformDirection(tangent)*1.5f;
        // Chord distance avoids phantom contact damage from the opposite hemisphere.
        if((State==FightState.Approach || State==FightState.Dash) && distance<1.35f && Time.time-lastContactTime>=contactCooldown)
        {
            lastContactTime=Time.time; game.TakeDamage(State==FightState.Dash ? 18 : contactDamage);
        }
        if(State==FightState.Windup)
        {
            stateTimer-=dt;
            if(stateTimer<=0)
            {
                telegraph.positionCount=0;
                if(attack==Attack.Nova) { FireRadialNova(); BeginRecovery(1.15f); }
                else if(attack==Attack.Dash) { State=FightState.Dash; stateTimer=Mathf.Min(8f,Mathf.Abs(Planet.lossyScale.x)*.5f*1.1f)/12f; }
                else { transform.position=arrivalPosition; gravityBody.SnapToSurface(); BeginRecovery(1.4f); }
            }
            return;
        }
        if(State==FightState.Dash)
        {
            BossWorldMotion.Move(transform,Planet,ref attackDirection,12f*Mathf.Min(dt,stateTimer),gravityBody.surfaceOffset);
            stateTimer-=dt;
            if(stateTimer<=0) BeginRecovery(1.4f);
            return;
        }
        if(State==FightState.Recovery)
        {
            stateTimer-=dt;
            if(stateTimer<=0) { State=FightState.Approach; RuntimeUIBuilder.UpdateBossHP(hp,maxHp); attackTimer=currentPhase==3 ? 1.3f : 2f; }
            return;
        }
        farTimer=distance>18f ? farTimer+dt : 0f;
        if(farTimer>4f)
        {
            arrivalPosition=BossWorldMotion.SurfacePoint(Planet,game.player.position,game.player.forward,7f,gravityBody.surfaceOffset);
            BeginWarning(Attack.Reposition,tangent,1.2f); farTimer=0; return;
        }
        bool visible=IsVisibleToPlayer();
        if(distance>4.6f || !visible) BossWorldMotion.Move(transform,Planet,ref tangent,baseSpeed*dt,gravityBody.surfaceOffset);
        if(!visible) return;
        attackTimer-=dt;
        if(attackTimer<=0)
        {
            Attack next=currentPhase>=2 && attackIndex%2==1 ? Attack.Dash : Attack.Nova;
            BeginWarning(next,tangent,next==Attack.Dash ? 1f : .9f);
            attackIndex++;
        }
    }
    bool IsVisibleToPlayer()
    {
        Camera camera=Camera.main; if(camera==null) return false;
        Vector3 screen=camera.WorldToViewportPoint(transform.position);
        Vector3 normal=(transform.position-Planet.position).normalized;
        return screen.z>camera.nearClipPlane && screen.x>.08f && screen.x<.92f && screen.y>.08f && screen.y<.86f
            && Vector3.Dot(normal,(camera.transform.position-transform.position).normalized)>.25f;
    }
    void BeginWarning(Attack next,Vector3 direction,float duration)
    {
        attack=next; attackDirection=direction; State=FightState.Windup; stateTimer=duration;
        Vector3 normal=(transform.position-Planet.position).normalized;
        if(next==Attack.Nova) attackDirection=Quaternion.AngleAxis(attackIndex*47f,normal)*direction;
        if(next==Attack.Dash)
        {
            float length=Mathf.Min(8f,Mathf.Abs(Planet.lossyScale.x)*.5f*1.1f);
            for(int i=0;i<33;i++) warningPoints[i]=BossWorldMotion.SurfacePoint(Planet,transform.position,direction,length*i/32f,.18f);
        }
        else
        {
            Vector3 origin=next==Attack.Reposition ? arrivalPosition : transform.position;
            Vector3 up=(origin-Planet.position).normalized;
            Vector3 forward=Vector3.ProjectOnPlane(attackDirection,up).normalized;
            float start=next==Attack.Nova ? 45f : 0f,arc=next==Attack.Nova ? 270f : 360f;
            for(int i=0;i<33;i++) warningPoints[i]=BossWorldMotion.SurfacePoint(Planet,origin,Quaternion.AngleAxis(start+arc*i/32f,up)*forward,1.6f,.18f);
        }
        RuntimeUIBuilder.UpdateBossHP(hp,maxHp);
        telegraph.positionCount=33; telegraph.SetPositions(warningPoints);
        GameAudio.Play(AudioCue.Hit);
    }
    void BeginRecovery(float duration)
    {
        State=FightState.Recovery; stateTimer=duration; telegraph.positionCount=0;
        RuntimeUIBuilder.UpdateBossHP(hp,maxHp);
        if(currentPhase>=2 && attackIndex>0 && attackIndex%3==0) SpawnMinionWave();
    }
    void FireRadialNova()
    {
        int count=currentPhase==1 ? 14 : currentPhase==2 ? 20 : 28;
        Vector3 normal=(transform.position-Planet.position).normalized;
        float speed=9.5f+currentPhase*1.5f;
        for(int i=0;i<count;i++)
        {
            float angle=i*360f/count;
            // The same 90-degree opening is visible in the warning arc.
            if(angle<45f || angle>315f) continue;
            Vector3 dir=Quaternion.AngleAxis(angle,normal)*attackDirection;
            BossProjectile projectile=projectilePool.Get(); projectiles.Add(projectile);
            Vector3 position=BossWorldMotion.SurfacePoint(Planet,transform.position,dir,1.3f,.5f);
            Vector3 shotNormal=(position-Planet.position).normalized;
            dir=Quaternion.FromToRotation(normal,shotNormal)*dir;
            projectile.Launch(this,Planet,position,dir,speed,currentPhase==3 ? 12 : 10,Mathf.Min(3f,Mathf.Abs(Planet.lossyScale.x)*.5f*2.1f/speed));
        }
        GameAudio.Play(AudioCue.Shot);
    }
    BossProjectile CreateProjectile()
    {
        GameObject obj=GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.name="BossNovaBullet";
        obj.SetActive(false); obj.GetComponent<Renderer>().sharedMaterial=projectileMat;
        return obj.AddComponent<BossProjectile>();
    }
    public void ReleaseProjectile(BossProjectile projectile)
    {
        if(projectiles.Remove(projectile)) projectilePool.Release(projectile);
    }
    void ClearProjectiles()
    {
        foreach(BossProjectile projectile in new List<BossProjectile>(projectiles)) if(projectile!=null) ReleaseProjectile(projectile);
    }
    void SpawnMinionWave()
    {
        if(EnemySpawner.Instance==null || EscortCount>=(currentPhase==3 ? 8 : 4)) return;
        Vector3 dir=Vector3.Cross(transform.up,transform.forward).normalized;
        Vector3 position=BossWorldMotion.SurfacePoint(Planet,transform.position,dir,3f,.5f);
        Enemy escort=EnemySpawner.Instance.SpawnBossMinion(gravityBody.planet,position);
        if(escort!=null) escorts.Add(escort);
    }
    static void DespawnEnemy(Enemy enemy)
    {
        if(enemy==null || !enemy.gameObject.activeSelf || enemy.isDead) return;
        enemy.isDead=true;
        if(enemy.pool!=null) enemy.pool.Release(enemy.gameObject); else { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
    }
    public void TakeDamage(int damage) { TakeDamage(damage,false,DamageTextStyle.Normal); }
    public void TakeDamage(int damage,bool isCrit) { TakeDamage(damage,isCrit,DamageTextStyle.Normal); }
    public void TakeDamage(int damage,bool isCrit,DamageTextStyle style)
    {
        if(isDead || damage<=0) return;
        var receiver=GetComponent<StatusEffectReceiver>();
        if(receiver!=null) damage=receiver.ModifyIncomingDamage(damage);
        hp=Mathf.Max(0,hp-damage); flashUntil=Time.time+.06f;
        GameAudio.PlayAt(isCrit ? AudioCue.CriticalHit : AudioCue.Hit,transform.position,gravityBody.planet);
        SpawnDamageText(damage,isCrit,style);
        RuntimeUIBuilder.UpdateBossHP(hp,maxHp);
        if(hp==0) { Die(); return; }
        int phase=hp<=maxHp*.25f ? 3 : hp<=maxHp*.65f ? 2 : 1;
        if(phase!=currentPhase)
        {
            SetPhase(phase); ClearProjectiles();
            if(State!=FightState.Deploy) BeginRecovery(1.6f);
        }
    }
    void SetPhase(int phase)
    {
        currentPhase=phase;
        currentColor=phase==1 ? new Color(.2f,.8f,1f) : phase==2 ? new Color(1f,.6f,.2f) : new Color(1f,.25f,.4f);
        coreMat.SetColor("_BaseColor",currentColor); coreMat.SetColor("_EmissionColor",currentColor*1.6f);
    }
    void SpawnDamageText(int damage,bool isCrit,DamageTextStyle style)
    {
        FloatingText.Spawn(transform.position + transform.up * 0.7f, Planet, damage, isCrit, style);
    }
    void Die()
    {
        isDead=true; State=FightState.Dead; ClearProjectiles();
        foreach(Enemy escort in escorts) DespawnEnemy(escort); escorts.Clear();
        RuntimeUIBuilder.HideBossHealthBar(); Teleporter.UnlockAllTeleporters();
        if(EnemySpawner.Instance!=null) EnemySpawner.Instance.isBossActive=false;
        GameAudio.Play(AudioCue.Explosion);
        if(GameManager.Instance!=null) RuntimeUIBuilder.BuildVictoryUI(GameManager.Instance);
        gameObject.SetActive(false); Destroy(gameObject);
    }
    void OnDestroy()
    {
        if(Instance==this) Instance=null;
        foreach(BossProjectile projectile in projectiles) if(projectile!=null) Destroy(projectile.gameObject);
        projectiles.Clear(); projectilePool?.Clear();
        foreach(Enemy escort in escorts) DespawnEnemy(escort);
        foreach(Material material in new[]{coreMat,eyeMat,armorMat,warningMat,projectileMat}) if(material!=null) Destroy(material);
    }
    void BuildVisuals()
    {
        // 1. Materials
        coreMat = new Material((Shader.Find("NeonWorlds/EmissiveGeometry") ?? Shader.Find("Universal Render Pipeline/Lit")));
        coreMat.EnableKeyword("_EMISSION");

        eyeMat = new Material((Shader.Find("NeonWorlds/EmissiveGeometry") ?? Shader.Find("Universal Render Pipeline/Lit")));
        eyeMat.EnableKeyword("_EMISSION");
        eyeMat.SetColor("_BaseColor", Color.white);
        eyeMat.SetColor("_EmissionColor", Color.white * 1.6f);

        armorMat = new Material((Shader.Find("NeonWorlds/EmissiveGeometry") ?? Shader.Find("Universal Render Pipeline/Lit")));
        armorMat.SetColor("_BaseColor", new Color(0.08f, 0.09f, 0.13f)); // Dark Dreadnought Steel
        armorMat.SetFloat("_Smoothness", 0.85f);
        armorMat.EnableKeyword("_EMISSION");
        armorMat.SetColor("_EmissionColor", new Color(0.1f, 0.15f, 0.25f));

        // 2. Colossal Central Core (3.0 scale - balanced flagship size)
        GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObj.name = "BossCore";
        coreObj.transform.SetParent(visualRoot, false);
        coreObj.transform.localPosition = Vector3.zero;
        coreObj.transform.localScale = Vector3.one * 3.0f;
        Destroy(coreObj.GetComponent<Collider>());
        coreObj.GetComponent<MeshRenderer>().material = coreMat;
        coreTransform = coreObj.transform;

        // 3. Central Eye / Focus Lens
        GameObject eyeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeObj.name = "BossEye";
        eyeObj.transform.SetParent(visualRoot, false);
        eyeObj.transform.localPosition = new Vector3(0, 0, 1.5f);
        eyeObj.transform.localScale = Vector3.one * 1.1f;
        Destroy(eyeObj.GetComponent<Collider>());
        eyeObj.GetComponent<MeshRenderer>().material = eyeMat;
        eyeTransform = eyeObj.transform;

        // 4. Hexagonal Exoskeleton Armor Plates (Horizontal tangent to planet)
        GameObject chassisObj = new GameObject("ChassisArmor");
        chassisObj.transform.SetParent(visualRoot, false);
        chassisObj.transform.localPosition = new Vector3(0, 0.15f, 0);

        int plateCount = 6;
        for (int i = 0; i < plateCount; i++)
        {
            float angle = i * (360f / plateCount);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 pos = rot * Vector3.forward * 1.9f;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "ArmorPlate_" + i;
            plate.transform.SetParent(chassisObj.transform, false);
            plate.transform.localPosition = pos;
            plate.transform.localRotation = rot;
            plate.transform.localScale = new Vector3(0.9f, 0.3f, 2.2f);
            Destroy(plate.GetComponent<Collider>());
            plate.GetComponent<MeshRenderer>().material = armorMat;
            armorPlates.Add(plate.transform);
        }

        // 5. Inner Ring (4 Heavy Plasma Cannons)
        GameObject innerPivot = new GameObject("InnerRingPivot");
        innerPivot.transform.SetParent(visualRoot, false);
        innerPivot.transform.localPosition = new Vector3(0, 0.1f, 0);
        innerRingPivot = innerPivot.transform;

        int innerCount = 4;
        for (int i = 0; i < innerCount; i++)
        {
            float angle = i * (360f / innerCount);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 pos = rot * Vector3.forward * 3.2f;

            GameObject cannon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cannon.name = "HeavyCannon_" + i;
            cannon.transform.SetParent(innerRingPivot, false);
            cannon.transform.localPosition = pos;
            cannon.transform.localRotation = rot;
            cannon.transform.localScale = new Vector3(0.5f, 0.5f, 1.3f);
            Destroy(cannon.GetComponent<Collider>());
            cannon.GetComponent<MeshRenderer>().material = coreMat;
            innerCannons.Add(cannon.transform);
        }

        // 6. Outer Ring (6 Gyroscopic Energy Fins)
        GameObject outerPivot = new GameObject("OuterRingPivot");
        outerPivot.transform.SetParent(visualRoot, false);
        outerPivot.transform.localPosition = new Vector3(0, 0.4f, 0);
        outerPivot.transform.localRotation = Quaternion.identity;
        outerRingPivot = outerPivot.transform;

        int outerCount = 6;
        for (int i = 0; i < outerCount; i++)
        {
            float angle = i * (360f / outerCount);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 pos = rot * Vector3.forward * 4.2f;

            GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fin.name = "EnergyFin_" + i;
            fin.transform.SetParent(outerRingPivot, false);
            fin.transform.localPosition = pos;
            fin.transform.localRotation = rot * Quaternion.Euler(0, 0, 35f);
            fin.transform.localScale = new Vector3(0.3f, 0.9f, 1.1f);
            Destroy(fin.GetComponent<Collider>());
            fin.GetComponent<MeshRenderer>().material = coreMat;
            outerFins.Add(fin.transform);
        }
    }


}

