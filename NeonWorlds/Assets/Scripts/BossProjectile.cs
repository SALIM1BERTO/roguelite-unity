using UnityEngine;
[RequireComponent(typeof(SphereCollider),typeof(Rigidbody))]
public class BossProjectile : MonoBehaviour
{
    public float speed=8f,lifeTime=2f;
    public int damage=10;
    public Transform planet;
    BossLeviathan owner;
    float timer;
    bool hasHit;
    Vector3 direction;
    readonly RaycastHit[] sweepHits=new RaycastHit[16];
    void Awake()
    {
        SphereCollider sphere=GetComponent<SphereCollider>(); sphere.isTrigger=true; sphere.radius=.5f;
        Rigidbody body=GetComponent<Rigidbody>(); body.isKinematic=true; body.useGravity=false;
    }
    public void Launch(BossLeviathan boss,Transform world,Vector3 position,Vector3 heading,float velocity,int amount,float duration)
    {
        owner=boss; planet=world; speed=velocity; damage=amount; lifeTime=duration;
        hasHit=false; timer=duration; direction=heading.normalized;
        transform.SetParent(world,true); transform.position=position;
        transform.rotation=Quaternion.LookRotation(direction,(position-world.position).normalized);
        BossWorldMotion.SetWorldScale(transform,Vector3.one*.28f);
    }
    void Update()
    {
        if(hasHit || Time.timeScale<=0) return;
        BossWorldMotion.SetWorldScale(transform,Vector3.one*.28f);
        Vector3 previous=transform.position;
        BossWorldMotion.Move(transform,planet,ref direction,speed*Time.deltaTime,.5f);
        Vector3 delta=transform.position-previous;
        // Sweep the travelled segment so a slow frame cannot skip the player's hitbox.
        if(delta.sqrMagnitude>.000001f)
        {
            int count=Physics.SphereCastNonAlloc(previous,.14f,delta.normalized,sweepHits,delta.magnitude,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count && !hasHit;i++) HandleHit(sweepHits[i].collider.gameObject);
        }
        timer-=Time.deltaTime;
        if(timer<=0) Release();
    }
    void OnTriggerEnter(Collider other) { HandleHit(other.gameObject); }
    void HandleHit(GameObject other)
    {
        if(hasHit || !isActiveAndEnabled) return;
        if(other.GetComponentInParent<PlayerMovement>()==null && other.GetComponentInParent<PlayerShip>()==null) return;
        GravityBody playerBody=other.GetComponentInParent<GravityBody>();
        if(playerBody!=null && playerBody.planet!=null && playerBody.planet.transform!=planet) return;
        GameManager.Instance?.TakeDamage(damage);
        Release();
    }
    void Release()
    {
        if(hasHit) return;
        hasHit=true;
        if(owner!=null) owner.ReleaseProjectile(this); else Destroy(gameObject);
    }
}
