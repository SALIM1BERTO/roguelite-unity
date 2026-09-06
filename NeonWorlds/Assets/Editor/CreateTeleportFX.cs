using UnityEngine;
using UnityEditor;

public class CreateTeleportFX
{
    [MenuItem("Tools/NeonWorlds/Create Teleport FX")]
    public static void DoIt()
    {
        string path = "Assets/Resources/TeleportFX.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject fx = new GameObject("TeleportFX");
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = 1f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.scalingMode = ParticleSystemScalingMode.Shape; // Ignore planet scale!
        main.playOnAwake = true;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 1f;

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.cyan, 0f), new GradientColorKey(Color.blue, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOver.color = grad;

        var renderer = fx.GetComponent<ParticleSystemRenderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.cyan * 4f);
        renderer.material = mat;

        PrefabUtility.SaveAsPrefabAsset(fx, path);
        Object.DestroyImmediate(fx);
        Debug.Log("TeleportFX created!");
    }
}
