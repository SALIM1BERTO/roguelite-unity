using UnityEngine;
using UnityEditor;

public class ReapplyAllFixes
{
    [MenuItem("Tools/NeonWorlds/Reapply All Fixes")]
    public static void DoIt()
    {
        // 1. Fix Player Magenta
        FixPlayerMagenta.DoIt();
        
        // 2. Fix Trails Child
        FixTrailsChild.DoIt();
        
        // 3. Reduce Speeds (manual implementation)
        Orbit[] orbits = Object.FindObjectsByType<Orbit>(FindObjectsInactive.Exclude);
        foreach(Orbit o in orbits)
        {
            if (Mathf.Abs(o.speed) > 5f) // only reduce if it's currently fast
            {
                o.speed = o.speed / 10f;
                EditorUtility.SetDirty(o);
            }
        }
        
        // 4. Fix Death FX
        FixDeathFX.DoIt();
        
        // 5. Create Teleport FX
        CreateTeleportFX.DoIt();
        
        // 6. Fix Gem Prefab
        // FixGemPrefab.DoIt();

        // 7. Auto Setup Enemies
        AutoSetupEnemies.DoIt();
        
        // SAVE SCENE
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        
        Debug.Log("ALL PREVIOUS FIXES REAPPLIED AND SAVED!");
    }
}
