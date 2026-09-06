using UnityEditor;
using UnityEngine;

public class FixPlayerManual {
    [MenuItem("Tools/Force Fix Player Mesh")]
    public static void Run() {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/ArrowShip.asset");
        if (mesh == null) {
            CreateArrowMesh.Run();
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/ArrowShip.asset");
        }
        
        GameObject player = GameObject.Find("Player");
        if (player != null) {
            MeshFilter mf = player.GetComponent<MeshFilter>();
            if (mf == null) mf = player.AddComponent<MeshFilter>();
            if (mesh != null) mf.sharedMesh = mesh;
            
            MeshRenderer mr = player.GetComponent<MeshRenderer>();
            if (mr == null) mr = player.AddComponent<MeshRenderer>();
            Material pmat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlayerMat.mat");
            if (pmat != null) mr.sharedMaterial = pmat;

            CapsuleCollider cc = player.GetComponent<CapsuleCollider>();
            if (cc == null) cc = player.AddComponent<CapsuleCollider>();
            cc.radius = 0.8f;
            cc.height = 1.0f;
            cc.center = new Vector3(0, 0, 0.25f);
            
            GameObject planet = GameObject.Find("Planet");
            if (planet != null) {
                float expectedDist = (planet.transform.localScale.x / 2f) + 1.0f;
                float currentDist = Vector3.Distance(player.transform.position, planet.transform.position);
                if (currentDist < expectedDist) {
                    Vector3 dir = (player.transform.position - planet.transform.position).normalized;
                    player.transform.position = planet.transform.position + dir * expectedDist;
                }
            }
            
            player.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            EditorUtility.SetDirty(player);
            Debug.Log("Forced Player Fix Completed!");
        }
    }
}
