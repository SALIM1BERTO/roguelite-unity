using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class XpGem : MonoBehaviour
{
    private Rigidbody rb;
    private bool collected;
    public UnityEngine.Pool.ObjectPool<GameObject> pool; public float magnetRadius = 15f;
    public float magnetSpeed = 25f;

    void OnEnable() { collected = false; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Start() { GravityBody gb = GetComponent<GravityBody>(); if(gb != null && gb.planet != null) transform.SetParent(gb.planet.transform, true); } void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            float dist = Vector3.Distance(transform.position, GameManager.Instance.player.position);
            
            // Magnetismo: Se estiver perto, voa na direcao do jogador
            if (dist < (GameManager.Instance != null ? GameManager.Instance.magnetRadius : magnetRadius))
            {
                Vector3 dir = (GameManager.Instance.player.position - transform.position).normalized;
                transform.position = transform.position + dir * magnetSpeed * Time.deltaTime;
            }
            else
            {
                // Se nao, apenas sofre gravidade do planeta atual
                GravityBody gb = GetComponent<GravityBody>();
                if (gb != null && gb.planet != null)
                {
                    
                    gb.planet.Attract(rb);
                }
            }
        }
    }

    void OnCollisionEnter(Collision col) { OnTriggerEnter(col.collider); } void OnTriggerEnter(Collider other)
    {
        if (collected || !gameObject.activeInHierarchy) return;
        
        if (other.CompareTag("Player"))
        {
            collected = true;
            if (GameManager.Instance != null)
            {
                GameAudio.Play(AudioCue.Pickup);
                GameManager.Instance.AddXP(10);
            }
            
            if (EnemySpawner.Instance != null && EnemySpawner.Instance.gemPool != null)
                EnemySpawner.Instance.gemPool.Release(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
