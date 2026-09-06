using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

#if NEONWORLDS_LEGACY_AUTO_SETUP
[InitializeOnLoad]
#endif
public class GeoWarsRefine
{
    static GeoWarsRefine()
    {
        EditorApplication.delayCall += ApplyRefinements;
    }

    static void ApplyRefinements()
    {
        if (EditorPrefs.GetBool("NeonWorlds_Refine1", false)) return;

        // 1. Anti-Aliasing (Tira o serrilhado)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null) {
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;
            }
            
            CameraFollow cf = mainCam.GetComponent<CameraFollow>();
            if (cf) {
                cf.height = 45f;
                cf.distance = 30f;
            }
        }

        // 2. Arredondar as formas (Menos pontas, mais próximo ao chão)
        Material matPlayer = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MatPlayer.mat");
        Material matEnemy = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MatEnemy.mat");
        Material matGem = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MatGem.mat");
        Material matBullet = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MatBullet.mat");

        // Player - Disco Redondo Suave
        GameObject player = GameObject.Find("Player");
        if (player)
        {
            // Limpa filhos antigos do visual
            foreach (Transform child in player.transform) {
                if (child.name != "PlayerLight") Object.DestroyImmediate(child.gameObject);
            }
            
            // Corpo base: Cilindro achatado (Disco)
            GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.transform.SetParent(player.transform);
            disk.transform.localPosition = new Vector3(0, 0.2f, 0); // Bem rente ao chão
            disk.transform.localRotation = Quaternion.identity;
            disk.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
            Object.DestroyImmediate(disk.GetComponent<Collider>());
            disk.GetComponent<Renderer>().sharedMaterial = matPlayer;

            // Núcleo: Esfera brilhante
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.transform.SetParent(player.transform);
            core.transform.localPosition = new Vector3(0, 0.5f, 0);
            core.transform.localRotation = Quaternion.identity;
            core.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            Object.DestroyImmediate(core.GetComponent<Collider>());
            core.GetComponent<Renderer>().sharedMaterial = matPlayer;

            player.transform.position = player.transform.position.normalized * 35f; // Cola no chão
        }

        // 3. Enemy Prefab - Esfera suave flutuando baixo
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyPrefab.prefab");
        if (enemyPrefab)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
            
            foreach (Transform child in instance.transform) { Object.DestroyImmediate(child.gameObject); }

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "EnemyVisual";
            visual.transform.SetParent(instance.transform);
            visual.transform.localPosition = new Vector3(0, 0.5f, 0); // Mais perto do chão
            visual.transform.localScale = new Vector3(1f, 1f, 1f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = matEnemy;
            
            PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Prefabs/EnemyPrefab.prefab");
            Object.DestroyImmediate(instance);
        }

        // 4. Gem Prefab - Esfera pequena
        GameObject gemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GemPrefab.prefab");
        if (gemPrefab)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(gemPrefab);
            
            // Troca malha para esfera
            MeshFilter mf = instance.GetComponent<MeshFilter>();
            if (mf) {
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mf.sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(temp);
            }
            instance.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Prefabs/GemPrefab.prefab");
            Object.DestroyImmediate(instance);
        }

        EditorPrefs.SetBool("NeonWorlds_Refine1", true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[NeonWorlds] Formas suavizadas, câmera diagonal afastada, SMAA ativado!");
    }
}
