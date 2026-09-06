using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public PlanetGravity targetPlanet;
    public float cooldown = 0f;

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;
        transform.Rotate(0, 90f * Time.deltaTime, 0);
    }

    void OnTriggerEnter(Collider other)
    {
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
            
            // Ativa o cooldown no teleporte de chegada (se houver) para nao teleportar infinitamente de volta
            Teleporter[] allTps = FindObjectsByType<Teleporter>(FindObjectsInactive.Exclude);
            foreach(var tp in allTps) tp.cooldown = 2f;

            Debug.Log("Teleportado para " + targetPlanet.name);
        }
    }
}
