using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public PlanetGravity targetPlanet;
    public float cooldown = 0f;
    public bool isLocked = false;

    private GameObject lockBarrier;
    private MeshRenderer meshR;
    private Material barrierMaterial;

    void Awake()
    {
        meshR = GetComponentInChildren<MeshRenderer>();
    }

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;
        
        // Spin faster if locked to look like an overloaded warning
        float rotSpeed = isLocked ? 180f : 90f;
        transform.Rotate(0, rotSpeed * Time.deltaTime, 0);

        if (isLocked && lockBarrier != null)
        {
            BossWorldMotion.SetWorldScale(lockBarrier.transform,Vector3.one);
            float pulse = Mathf.PingPong(Time.time * 4f, 1f);
            MeshRenderer bmr = lockBarrier.GetComponent<MeshRenderer>();
            if (bmr != null && bmr.material != null)
            {
                bmr.material.SetColor("_EmissionColor", Color.red * (3f + pulse * 4f));
            }
        }
    }

    public void Lock()
    {
        isLocked = true;
        if (lockBarrier == null)
        {
            lockBarrier = new GameObject("LockBarrier");
            lockBarrier.transform.SetParent(transform,false);
            BossWorldMotion.SetWorldScale(lockBarrier.transform,Vector3.one);
            LineRenderer ring=lockBarrier.AddComponent<LineRenderer>();
            ring.useWorldSpace=false; ring.loop=true; ring.positionCount=32; ring.widthMultiplier=.065f;
            barrierMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            barrierMaterial.SetColor("_BaseColor",new Color(1f,.3f,.4f));
            ring.sharedMaterial=barrierMaterial;
            for(int i=0;i<32;i++) { float angle=i*Mathf.PI/16; ring.SetPosition(i,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*1.15f); }

        }
        else
        {
            lockBarrier.SetActive(true);
        }
    }

    public void Unlock()
    {
        isLocked = false;
        if (lockBarrier != null)
        {
            Destroy(lockBarrier);
            lockBarrier = null;
            if(barrierMaterial!=null) Destroy(barrierMaterial);
            barrierMaterial=null;
        }

        // Victory burst on portal
        GameObject fxPrefab = Resources.Load<GameObject>("TeleportFX");
        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }
    }

    void OnDestroy() { if(barrierMaterial!=null) Destroy(barrierMaterial); }

    public static void LockAllTeleporters()
    {
        Teleporter[] allTps = FindObjectsByType<Teleporter>(FindObjectsInactive.Exclude);
        foreach (var tp in allTps) tp.Lock();
    }

    public static void UnlockAllTeleporters()
    {
        Teleporter[] allTps = FindObjectsByType<Teleporter>(FindObjectsInactive.Exclude);
        foreach (var tp in allTps) tp.Unlock();
    }

    void OnTriggerEnter(Collider other)
    {
        if (isLocked)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null)
            {
                GameAudio.Play(AudioCue.Hit);

                // Floating text warning
                SpawnWarningText("PORTAL BLOQUEADO PELO LEVIATA");

                // Repel player
                Vector3 repelDir = (other.transform.position - transform.position).normalized;
                other.transform.position += repelDir * 1.5f;
            }
            return;
        }

        if (cooldown > 0f) return;
        
        if (other.CompareTag("Player") && targetPlanet != null)
        {
            GravityBody body = other.GetComponent<GravityBody>();
            if (body == null) return;
            other.transform.position = targetPlanet.transform.position + targetPlanet.transform.up;
            body.planet = targetPlanet;
            body.SnapToSurface();
            GameAudio.Play(AudioCue.Teleport);
            EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
            if (spawner != null) spawner.currentPlanet = targetPlanet;
            
            Teleporter[] allTps = FindObjectsByType<Teleporter>(FindObjectsInactive.Exclude);
            foreach(var tp in allTps) tp.cooldown = 2f;

            PlanetaryBiome.OnPlayerArrived(targetPlanet);

            Debug.Log("Teleportado para " + targetPlanet.name);
        }
    }

    void SpawnWarningText(string msg)
    {
        Vector3 textPos = transform.position + transform.up * 2f;
        GameObject txtObj = null;
        if (GameManager.Instance != null && GameManager.Instance.floatingTextPrefab != null)
        {
            txtObj = Instantiate(GameManager.Instance.floatingTextPrefab, textPos, Quaternion.identity);
        }
        else
        {
            txtObj = new GameObject("FloatingText");
            txtObj.transform.position = textPos;
        }

        FloatingText ft = txtObj.GetComponent<FloatingText>();
        if (ft == null) ft = txtObj.AddComponent<FloatingText>();
        ft.Setup(msg);
    }
}
