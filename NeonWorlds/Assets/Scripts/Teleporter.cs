using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public PlanetGravity targetPlanet;
    public float cooldown = 0f;
    public bool isLocked = false;

    private GameObject lockBarrier;
    private MeshRenderer meshR;

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
            lockBarrier = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lockBarrier.name = "LockBarrier";
            lockBarrier.transform.SetParent(transform, false);
            lockBarrier.transform.localPosition = Vector3.zero;
            lockBarrier.transform.localScale = Vector3.one * 2.5f;
            Destroy(lockBarrier.GetComponent<Collider>());

            MeshRenderer bmr = lockBarrier.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(1f, 0f, 0.1f, 0.7f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.red * 4f);
            bmr.material = mat;
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
        }

        // Victory burst on portal
        GameObject fxPrefab = Resources.Load<GameObject>("TeleportFX");
        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }
    }

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
