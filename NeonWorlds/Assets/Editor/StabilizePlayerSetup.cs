using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StabilizePlayerSetup
{
    [MenuItem("Tools/NeonWorlds/Stabilize Player")]
    public static void Apply()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) throw new System.InvalidOperationException("Player was not found in the active scene.");
        Undo.RegisterFullObjectHierarchyUndo(player, "Stabilize Player");
        ApplyToPlayer(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
    }

    public static void ApplyToPlayer(GameObject player)
    {
        PlayerShip ship = player.GetComponent<PlayerShip>();
        if (ship == null) ship = player.AddComponent<PlayerShip>();
        ship.Configure();
        GravityBody body = player.GetComponent<GravityBody>();
        if (body != null && body.planet != null) body.SnapToSurface();
        EditorUtility.SetDirty(ship);
        EditorUtility.SetDirty(player);
    }

    // Explicit batch migration, never executed by importing or recompiling scripts.
    public static void MigrateSampleScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        Apply();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}

[CustomEditor(typeof(PlayerShip))]
public class PlayerShipEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Apply ship dimensions"))
        {
            PlayerShip ship = (PlayerShip)target;
            Undo.RegisterFullObjectHierarchyUndo(ship.gameObject, "Change Ship Dimensions");
            ship.Configure();
            EditorSceneManager.MarkSceneDirty(ship.gameObject.scene);
        }
    }
}
