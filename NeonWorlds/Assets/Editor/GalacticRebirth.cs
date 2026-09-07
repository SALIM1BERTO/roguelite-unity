using UnityEngine;
using UnityEditor;

public class GalacticRebirthMenu
{
    [MenuItem("NeonWorlds/Force Rebuild Solar System")]
    public static void Rebuild()
    {
        Debug.Log("INICIANDO RENASCIMENTO GALACTICO...");

        // 1. Deletar tudo que eh planeta, sol ou teleporter velho
        foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (obj.transform.parent != null) continue; // Pula filhos
            if (obj.name.StartsWith("Planet") || obj.name.StartsWith("Teleporter") || obj.name.StartsWith("OrbitLine") || obj.name == "Sun" || obj.name == "SunLight") 
            {
                Object.DestroyImmediate(obj);
            }
        }

        // 2. Criar o Sol
        GameObject sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sun.name = "Sun";
        sun.transform.position = Vector3.zero;
        sun.transform.localScale = new Vector3(300, 300, 300);
        Object.DestroyImmediate(sun.GetComponent<Collider>());
        
        Material sunMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MatSun.asset");
        if (sunMat == null) {
            sunMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sunMat.SetColor("_BaseColor", Color.yellow);
            sunMat.EnableKeyword("_EMISSION");
            sunMat.SetColor("_EmissionColor", Color.yellow * 8f);
            AssetDatabase.CreateAsset(sunMat, "Assets/Materials/MatSun.asset");
        }
        sun.GetComponent<MeshRenderer>().material = sunMat;

        GameObject sunLight = new GameObject("SunLight");
        sunLight.transform.parent = sun.transform;
        sunLight.transform.localPosition = Vector3.zero;
        Light l = sunLight.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 2000f;
        l.intensity = 50f;
        l.color = new Color(1f, 0.8f, 0.5f);

        // 3. Preparar material base dos planetas
        Material baseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MatPlanet.asset");
        if (baseMat == null) {
            baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        // Material para as linhas de orbita
        Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

        // 4. Configurar os 6 planetas espalhados em orbitas concentricas
        float[] sizes = { 70f, 30f, 120f, 50f, 200f, 90f };
        float[] dists = { 280f, 380f, 510f, 660f, 880f, 1120f };
        float[] angles = { 25f, 115f, 205f, 300f, 65f, 160f };
        Color[] colors = { Color.cyan, Color.red, new Color(0.5f, 0f, 1f), Color.green, new Color(1f, 0.5f, 0f), Color.yellow };

        PlanetGravity[] planets = new PlanetGravity[6];

        for (int i = 0; i < 6; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "Planet_" + (i+1);
            p.transform.localScale = new Vector3(sizes[i], sizes[i], sizes[i]);
            float rad = angles[i] * Mathf.Deg2Rad;
            p.transform.position = new Vector3(Mathf.Cos(rad) * dists[i], 0, Mathf.Sin(rad) * dists[i]);
            
            PlanetGravity pg = p.AddComponent<PlanetGravity>();
            pg.gravity = -12f;
            planets[i] = pg;

            Material pMat = new Material(baseMat);
            pMat.SetColor("_EmissionColor", colors[i] * 2f);
            AssetDatabase.CreateAsset(pMat, "Assets/Materials/MatGalacticPlanet_" + (i+1) + ".asset");
            p.GetComponent<MeshRenderer>().material = pMat;
        }

        // 5. Criar teleporters entre eles (1->2, 2->3, ..., 6->1)
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            CreateTeleporter(planets[i].gameObject, planets[next], colors[next], "Teleporter_" + (i+1) + "to" + (next+1));
        }

        // 6. Atualiza o player para o Planet_1
        GameObject player = GameObject.Find("Player");
        if (player) {
            player.transform.position = planets[0].transform.position + new Vector3(0, (sizes[0]/2f) + 1f, 0);
            player.GetComponent<GravityBody>().planet = planets[0];
            
            var weapon = Object.FindAnyObjectByType<Weapon>();
            // if (weapon) weapon.planet = planets[0];
            
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner) spawner.currentPlanet = planets[0];
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[NeonWorlds] Galaxia RECONSTRUIDA COM SUCESSO! Orbitas visuais desenhadas!");
    }

    static void CreateTeleporter(GameObject fromPlanet, PlanetGravity toPlanet, Color color, string name)
    {
        GameObject tp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tp.name = name;
        tp.transform.parent = fromPlanet.transform;
        
        Vector3 newLocalPos = new Vector3(0.353f, 0.353f, 0f).normalized * 0.5f;
        tp.transform.localPosition = newLocalPos;
        tp.transform.up = newLocalPos.normalized;
        tp.transform.localScale = new Vector3(10f / fromPlanet.transform.localScale.x, 0.5f / fromPlanet.transform.localScale.y, 10f / fromPlanet.transform.localScale.z);
        
        Object.DestroyImmediate(tp.GetComponent<MeshRenderer>());
        tp.GetComponent<Collider>().isTrigger = true;
        
        Teleporter script = tp.AddComponent<Teleporter>();
        script.targetPlanet = toPlanet;

        GameObject fxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TeleporterRingFX.prefab");
        if (fxPrefab)
        {
            GameObject fx = Object.Instantiate(fxPrefab, tp.transform);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localScale = Vector3.one;
            
            var ps = fx.GetComponent<ParticleSystem>().main;
            ps.startColor = color;
        }
    }
}
