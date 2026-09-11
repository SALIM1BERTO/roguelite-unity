using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class XpGem : MonoBehaviour
{
    public const int MaximumPerPlanet=96;
    public static readonly List<XpGem> Active=new List<XpGem>();
    public UnityEngine.Pool.ObjectPool<GameObject> pool;
    public float magnetRadius=25f,magnetSpeed=25f;
    public int Value {get;private set;}=50;
    public int Tier => Value>=800 ? 4 : Value>=400 ? 3 : Value>=200 ? 2 : Value>=100 ? 1 : 0;
    public PlanetGravity Planet => body!=null ? body.planet : null;
    bool collected;GravityBody body;Transform visual;MeshRenderer mesh;
    static Mesh crystal;static Material[] materials;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){Active.Clear();}
    void Awake()
    {
        var rb=GetComponent<Rigidbody>();rb.useGravity=false;rb.isKinematic=true;rb.constraints=RigidbodyConstraints.FreezeRotation;
        body=GetComponent<GravityBody>();if(body==null)body=gameObject.AddComponent<GravityBody>();body.enabled=false;body.surfaceOffset=.35f;
        var spinner=GetComponent<Spinner>();if(spinner!=null)spinner.enabled=false;
        var trail=GetComponent<LocalTrailAssigner>();if(trail!=null)trail.enabled=false;
        foreach(var particles in GetComponentsInChildren<ParticleSystem>()){particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var emission=particles.emission;emission.enabled=false;}
        foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=false;
        EnsureArt();visual=new GameObject("XpCrystal").transform;visual.SetParent(transform,false);
        visual.gameObject.AddComponent<MeshFilter>().sharedMesh=crystal;mesh=visual.gameObject.AddComponent<MeshRenderer>();mesh.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mesh.receiveShadows=false;
        var collider=GetComponent<SphereCollider>();if(collider!=null){collider.radius=.4f;collider.isTrigger=true;}
    }
    static void EnsureArt()
    {
        if(crystal==null)
        {
            Vector3[] corners={Vector3.up,Vector3.right,Vector3.forward,Vector3.left,Vector3.back,Vector3.down};
            int[] faces={0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};
            var vertices=new Vector3[24];var triangles=new int[24];for(int i=0;i<24;i++){vertices[i]=corners[faces[i]];triangles[i]=i;}
            crystal=new Mesh {name="Shared XP crystal",vertices=vertices,triangles=triangles};crystal.RecalculateNormals();crystal.RecalculateBounds();
        }
        if(materials==null || materials[0]==null)
        {
            Color[] colors={new Color(.2f,.8f,1f),new Color(.25f,1f,.55f),new Color(.7f,.35f,1f),new Color(1f,.7f,.15f),new Color(1f,.25f,.35f)};
            materials=new Material[5];for(int i=0;i<5;i++){var m=new Material(Shader.Find("NeonWorlds/EmissiveGeometry") ?? Shader.Find("Universal Render Pipeline/Unlit"));m.SetColor("_BaseColor",colors[i]);m.SetColor("_EmissionColor",colors[i]*1.3f);m.EnableKeyword("_EMISSION");materials[i]=m;}
        }
    }
    void OnEnable(){collected=false;Value=50;if(!Active.Contains(this))Active.Add(this);RefreshArt();}
    void OnDisable(){Active.Remove(this);}
    void OnDestroy(){Active.Remove(this);}
    void RefreshArt(){if(mesh==null)return;mesh.sharedMaterial=materials[Tier];visual.localScale=Vector3.one*(.25f+Tier*.045f);}
    public void Configure(PlanetGravity planet,Vector3 position,int amount)
    {
        body.planet=planet;Value=Mathf.Max(1,amount);collected=false;
        transform.SetParent(planet!=null ? planet.transform : null,true);transform.position=position;
        BossWorldMotion.SetWorldScale(transform,Vector3.one);if(planet!=null)body.SnapToSurface();RefreshArt();
    }
    public void AddValue(int amount){Value=(int)System.Math.Min(int.MaxValue,(long)Value+Mathf.Max(0,amount));RefreshArt();}
    public static XpGem Drop(PlanetGravity planet,Vector3 position,int value)
    {
        if(planet==null || value<=0 || EnemySpawner.Instance==null || EnemySpawner.Instance.gemPool==null)return null;
        XpGem nearest=null;float closest=float.PositiveInfinity;int count=0;
        foreach(var gem in Active)
        {
            if(gem==null || gem.collected || gem.Planet!=planet)continue;count++;
            float distance=(gem.transform.position-position).sqrMagnitude;if(distance<closest){closest=distance;nearest=gem;}
        }
        if(nearest!=null && (closest<2.25f || count>=MaximumPerPlanet)){nearest.AddValue(value);return nearest;}
        var obj=EnemySpawner.Instance.gemPool.Get();var orb=obj.GetComponent<XpGem>();orb.Configure(planet,position,value);return orb;
    }
    static GravityBody s_playerBody;
    static int s_playerBodyFrame = -1;
    static GravityBody GetPlayerBody(Transform player)
    {
        if (s_playerBodyFrame == Time.frameCount && s_playerBody != null) return s_playerBody;
        s_playerBodyFrame = Time.frameCount;
        s_playerBody = player != null ? player.GetComponent<GravityBody>() : null;
        return s_playerBody;
    }

    void Update()
    {
        if(collected || Time.timeScale<=0 || GameManager.Instance==null || GameManager.Instance.player==null || Planet==null)return;
        Transform player=GameManager.Instance.player;var playerBody=GetPlayerBody(player);if(playerBody==null || playerBody.planet!=Planet)return;
        float radius=GameManager.Instance.magnetRadius;
        if((player.position-transform.position).sqrMagnitude>radius*radius)return;
        float distance=SphericalCombat.Distance(Planet.transform,transform.position,player.position);
        if(distance>radius)return;
        float step=magnetSpeed*Time.deltaTime;
        if(distance<=Mathf.Max(.65f,step)){Collect(player);return;}
        Vector3 normal=(transform.position-Planet.transform.position).normalized;
        Vector3 direction=Vector3.ProjectOnPlane(player.position-transform.position,normal).normalized;
        float r=Mathf.Abs(Planet.transform.lossyScale.x)*.5f;
        transform.position=BossWorldMotion.SurfacePoint(Planet.transform,transform.position,direction,step*(1f+.35f/Mathf.Max(.1f,r)),.35f);
    }
    public bool Collect(Transform player)
    {
        if(collected || !isActiveAndEnabled || Time.timeScale<=0 || GameManager.Instance==null || player!=GameManager.Instance.player)return false;
        var playerBody=player!=null ? GetPlayerBody(player) : null;if(playerBody==null || playerBody.planet!=Planet)return false;
        collected=true;int reward=Value;
        if(pool!=null)pool.Release(gameObject);else{gameObject.SetActive(false);Destroy(gameObject);}
        GameAudio.Play(AudioCue.Pickup);GameManager.Instance.AddXP(reward);return true;
    }
    void OnCollisionEnter(Collision collision){OnTriggerEnter(collision.collider);}
    void OnTriggerEnter(Collider other)
    {
        var player=GameManager.Instance!=null ? GameManager.Instance.player : null;
        if(player!=null && (other.transform==player || other.transform.IsChildOf(player)))Collect(player);
    }
}
