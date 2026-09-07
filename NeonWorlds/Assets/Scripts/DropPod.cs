using UnityEngine;
using System.Collections;

public class DropPod : MonoBehaviour
{
    public enum BuffType { Invincibility, Frenzy, SmartBomb }
    public BuffType buffType;
    public Transform planet;

    void Start()
    {
        buffType = (BuffType)Random.Range(0, 3);
        
        // Setup Visuals
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mr.material.EnableKeyword("_EMISSION");
            
            Color neonColor = Color.white;
            if (buffType == BuffType.Invincibility) neonColor = new Color(0f, 0.5f, 1f); // Blue
            if (buffType == BuffType.Frenzy) neonColor = new Color(1f, 0f, 0f); // Red
            if (buffType == BuffType.SmartBomb) neonColor = new Color(1f, 1f, 0f); // Yellow
            
            mr.material.SetColor("_BaseColor", neonColor);
            mr.material.SetColor("_EmissionColor", neonColor * 3f);
        }

        // Setup Collider
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 2f;
    }

    void Update()
    {
        // Gentle rotation
        transform.Rotate(0, 50f * Time.deltaTime, 0, Space.Self);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() != null)
        {
            ApplyBuff(other.GetComponentInParent<PlayerMovement>());
            
            // Visual pop
            GameObject fxPrefab = Resources.Load<GameObject>("TeleportFX");
            if (fxPrefab != null)
            {
                GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
                fx.transform.SetParent(planet, true);
                Destroy(fx, 1f);
            }
            
            Destroy(gameObject);
        }
    }

    void ApplyBuff(PlayerMovement player)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        switch (buffType)
        {
            case BuffType.Invincibility:
                gm.StartCoroutine(InvincibilityRoutine(player));
                break;
            case BuffType.Frenzy:
                gm.StartCoroutine(FrenzyRoutine(player.GetComponent<Weapon>()));
                break;
            case BuffType.SmartBomb:
                for (int i = Enemy.activeEnemies.Count - 1; i >= 0; i--)
                {
                    if (i < Enemy.activeEnemies.Count)
                    {
                        Enemy e = Enemy.activeEnemies[i];
                        if (e != null && !e.isDead) e.TakeDamage(9999);
                    }
                }
                break;
        }
    }

    IEnumerator InvincibilityRoutine(PlayerMovement player)
    {
        GameManager.Instance.isInvincible = true;
        yield return new WaitForSeconds(10f);
        GameManager.Instance.isInvincible = false;
    }

        IEnumerator FrenzyRoutine(Weapon w)
    {
        if (w == null) yield break;
        w.fireRateMultiplier = 10f; // Insane speed (10x faster)
        yield return new WaitForSeconds(8f);
        w.fireRateMultiplier = 1f;
    }
}

