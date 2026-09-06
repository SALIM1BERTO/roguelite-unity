using UnityEngine;
using UnityEditor;

public class FixPlayerMagenta
{
    [MenuItem("Tools/NeonWorlds/Fix Player Magenta")]
    public static void DoIt()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            // Reset scale (assume world size should be ~2)
            if (player.transform.parent != null)
            {
                float parentScale = player.transform.parent.localScale.x;
                player.transform.localScale = new Vector3(2f / parentScale, 0.4f / parentScale, 2f / parentScale);
            }
            else
            {
                player.transform.localScale = new Vector3(2f, 0.4f, 2f);
            }
            
            // Fix Material (URP Lit)
            MeshRenderer mr = player.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", Color.green);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.green * 4f);
                mr.sharedMaterial = mat;
            }
            
            EditorUtility.SetDirty(player);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("Fixed Player Magenta and Scale!");
        }
    }
}
