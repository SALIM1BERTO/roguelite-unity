using UnityEngine;
using UnityEditor;

#if NEONWORLDS_LEGACY_AUTO_SETUP
[InitializeOnLoad]
#endif
public class ConvertTrailsToParticles
{
    static ConvertTrailsToParticles()
    {
        EditorApplication.delayCall += DoIt;
    }

    static void DoIt()
    {
        if (EditorPrefs.GetBool("NeonWorlds_ConvertTrails_v3", false)) return;

        ConvertOnPrefab("Assets/Resources/BulletPrefab.prefab");
        ConvertOnPrefab("Assets/Resources/EnemyPrefab.prefab");
        
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            ConvertOnGameObject(player);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        EditorPrefs.SetBool("NeonWorlds_ConvertTrails_v3", true);
    }

    static void ConvertOnPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            ConvertOnGameObject(prefab);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }
    }

    static void ConvertOnGameObject(GameObject go)
    {
        TrailRenderer tr = go.GetComponent<TrailRenderer>();
        if (tr != null)
        {
            Material trailMat = tr.sharedMaterial;
            float time = tr.time;
            float width = tr.startWidth;
            Color color = tr.startColor;
            
            Object.DestroyImmediate(tr, true);

            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            main.startLifetime = time;
            main.startSpeed = 0f;
            main.startSize = width;
            main.startColor = color;
            main.maxParticles = 1000;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 30f; // Emits particles based on distance moved

            var shape = ps.shape;
            shape.enabled = false;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material = trailMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            
            if (go.GetComponent<LocalTrailAssigner>() == null) go.AddComponent<LocalTrailAssigner>();
        }
    }
}
