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
        
        // 3. Fix Death FX
        FixDeathFX.DoIt();
        
        // 4. Create Teleport FX
        CreateTeleportFX.DoIt();

        // 5. Auto Setup Enemies
        AutoSetupEnemies.DoIt();
        
        // SAVE SCENE
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        
        Debug.Log("ALL PREVIOUS FIXES REAPPLIED AND SAVED!");
    }
}
