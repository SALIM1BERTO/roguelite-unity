using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

#if NEONWORLDS_LEGACY_AUTO_SETUP
[InitializeOnLoad]
#endif
public class SceneAutoSetup
{
    static SceneAutoSetup()
    {
        EditorApplication.delayCall += DoSetup;
    }

    static void DoSetup()
    {
        if (EditorPrefs.GetBool("NeonWorlds_SetupDone3", false)) return;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        Scene activeScene = SceneManager.GetActiveScene();

        // Apagar lixo da cena anterior
        string[] toDelete = { "Planeta", "Player", "GameManager", "EnemySpawner" };
        foreach(string n in toDelete) {
            GameObject old = GameObject.Find(n);
            if (old) Object.DestroyImmediate(old);
        }

        // 1. Criar o Planeta
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = "Planeta";
        planet.transform.position = Vector3.zero;
        planet.transform.localScale = new Vector3(70, 70, 70);
        PlanetGravity pGravity = planet.AddComponent<PlanetGravity>();
        
        Renderer pRenderer = planet.GetComponent<Renderer>();
        if (pRenderer != null && pRenderer.sharedMaterial != null) {
            pRenderer.sharedMaterial.color = new Color(0.1f, 0.1f, 0.1f);
        }

        // 2. Criar Jogador
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0, 36, 0);
        
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        GravityBody gBody = player.AddComponent<GravityBody>();
        gBody.planet = pGravity;

        PlayerMovement pMove = player.AddComponent<PlayerMovement>();
        pMove.moveSpeed = 12f;

        Weapon weapon = player.AddComponent<Weapon>();
        // weapon.planet = pGravity;

        GameObject pLight = new GameObject("PlayerLight");
        pLight.transform.SetParent(player.transform);
        pLight.transform.localPosition = new Vector3(0, 3, 0);
        Light lightComp = pLight.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.range = 25f;
        lightComp.intensity = 5f;
        lightComp.color = new Color(0.6f, 0.2f, 1f);

        // 3. Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
            if (!camFollow) camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
            camFollow.target = player.transform;
            mainCam.backgroundColor = Color.black;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
        }

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach(var l in lights) {
            if (l.type == LightType.Directional) {
                Object.DestroyImmediate(l.gameObject);
            }
        }

        // 4. Game Manager
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.player = player.transform;

        // 5. Criar Prefabs
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bullet.name = "BulletPrefab";
        bullet.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        bullet.transform.rotation = Quaternion.Euler(90, 0, 0);
        Object.DestroyImmediate(bullet.GetComponent<CapsuleCollider>());
        SphereCollider bc = bullet.AddComponent<SphereCollider>();
        bc.isTrigger = true;
        Rigidbody brb = bullet.AddComponent<Rigidbody>();
        brb.useGravity = false;
        brb.isKinematic = true;
        bullet.AddComponent<GravityBody>();
        bullet.AddComponent<Bullet>();
        GameObject prefabBullet = PrefabUtility.SaveAsPrefabAsset(bullet, "Assets/Prefabs/BulletPrefab.prefab");
        Object.DestroyImmediate(bullet);

        GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gem.name = "GemPrefab";
        gem.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Object.DestroyImmediate(gem.GetComponent<BoxCollider>());
        SphereCollider gc = gem.AddComponent<SphereCollider>();
        gc.isTrigger = true;
        gem.AddComponent<GravityBody>();
        gem.AddComponent<XpGem>();
        GameObject prefabGem = PrefabUtility.SaveAsPrefabAsset(gem, "Assets/Prefabs/GemPrefab.prefab");
        Object.DestroyImmediate(gem);

        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        enemy.name = "standardPrefab";
        enemy.tag = "Enemy";
        Rigidbody erb = enemy.AddComponent<Rigidbody>();
        erb.useGravity = false;
        erb.constraints = RigidbodyConstraints.FreezeRotation;
        enemy.AddComponent<GravityBody>();
        Enemy eScript = enemy.AddComponent<Enemy>();
        eScript.gemPrefab = prefabGem;
        GameObject prefabEnemy = PrefabUtility.SaveAsPrefabAsset(enemy, "Assets/Prefabs/standardPrefab.prefab");
        Object.DestroyImmediate(enemy);

        // 6. Configurar Weapon e Spawner
        weapon.bulletPrefab = prefabBullet;

        GameObject spawnerObj = new GameObject("EnemySpawner");
        EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();
        spawner.currentPlanet = planet.GetComponent<PlanetGravity>();
        spawner.standardPrefab = prefabEnemy;
        spawner.xpGemPrefab = prefabGem;

        EditorPrefs.SetBool("NeonWorlds_SetupDone3", true);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[NeonWorlds] Jogo INTEIRO recriado pela IA!");
    }
}
