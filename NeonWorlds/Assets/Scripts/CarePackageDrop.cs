using UnityEngine;
using System.Collections;

public class CarePackageDrop : MonoBehaviour
{
    private Transform planet;
    private bool isCollected = false;
    private float lifetime = 50f;
    private GameObject beaconBeam;

    public void Setup(Transform p, Vector3 surfaceNormal, float pScale)
    {
        planet = p;
        pScale = Mathf.Max(0.01f, pScale);

        // Visual Supply Crate
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = "SupplyCrate";
        crate.transform.SetParent(transform, false);
        crate.transform.localPosition = new Vector3(0, 0.45f / pScale, 0);
        crate.transform.localScale = Vector3.one * (0.9f / pScale);
        Destroy(crate.GetComponent<Collider>());

        MeshRenderer mr = crate.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color crateColor = new Color(0f, 0.8f, 1f); // Neon Cyan / Gold
        mat.SetColor("_BaseColor", crateColor);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", crateColor * 3f);
        mr.material = mat;

        // Space Beacon Light Beam reaching towards space
        beaconBeam = new GameObject("BeaconBeam");
        beaconBeam.transform.SetParent(transform, false);
        beaconBeam.transform.localPosition = Vector3.zero;

        LineRenderer lr = beaconBeam.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = 0.4f / pScale;
        lr.endWidth = 0.1f / pScale;
        lr.positionCount = 2;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, Vector3.up * (40f / pScale));

        Material beamMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        Color beamColor = new Color(0f, 1f, 0.8f, 0.7f);
        beamMat.color = beamColor;
        lr.material = beamMat;

        GameAudio.Play(AudioCue.Teleport);
    }

    void Update()
    {
        if (isCollected) return;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.Rotate(0, 45f * Time.deltaTime, 0);

        // Check if player collects
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            float dist = Vector3.Distance(transform.position, GameManager.Instance.player.position);
            if (dist <= 2.2f)
            {
                Collect();
            }
        }
    }

    void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        GameAudio.Play(AudioCue.LevelUp);
        if (CameraShake.Instance != null) CameraShake.Instance.TriggerShake(0.2f, 0.4f);

        // Visual collection burst
        GameObject fxPrefab = Resources.Load<GameObject>("TeleportFX");
        if (fxPrefab != null)
        {
            GameObject fx = Instantiate(fxPrefab, transform.position, Quaternion.identity);
            if (planet != null) fx.transform.SetParent(planet, true);
            Destroy(fx, 2f);
        }

        int roll = Random.Range(0, 3);
        if (roll == 0)
        {
            // Buff 1: Frenesi / Overcharge (8 seconds)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerFrenzy(8f);
            }
            SpawnRewardText("★ FRENESI CÓSMICO! (8s) ★", new Color(1f, 0.85f, 0.1f));
        }
        else if (roll == 1)
        {
            // Buff 2: Orbital EMP Smart Bomb
            TriggerEMPBlast();
            SpawnRewardText("⚡ PULSO EMP ORBITAL! ⚡", new Color(0f, 0.9f, 1f));
        }
        else
        {
            // Buff 3: Star Core Cache (+5 to +10 Cores)
            int cores = Random.Range(5, 11);
            MetaProgression.AddStarCores(cores);
            SpawnRewardText($"★ +{cores} CÉLULAS ESTELARES! ★", new Color(0f, 1f, 0.5f));
        }

        Destroy(gameObject, 0.1f);
    }

    void TriggerEMPBlast()
    {
        GameAudio.Play(AudioCue.Explosion);

        // Aniquilar todos os inimigos normais na tela/planeta
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in enemies)
        {
            if (e != null && !e.isDead)
            {
                e.TakeDamage(180, true, DamageTextStyle.Electric);
            }
        }

        // Stagger / dano no Leviatã
        if (BossLeviathan.Instance != null && BossLeviathan.Instance.gameObject.activeInHierarchy)
        {
            BossLeviathan.Instance.TakeDamage(250, true, DamageTextStyle.Electric);
        }
    }

    void SpawnRewardText(string msg, Color col)
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

        if (planet != null) txtObj.transform.SetParent(planet, true);
        FloatingText ft = txtObj.GetComponent<FloatingText>();
        if (ft == null) ft = txtObj.AddComponent<FloatingText>();
        ft.Setup(msg, col, 1.4f);
    }
}
