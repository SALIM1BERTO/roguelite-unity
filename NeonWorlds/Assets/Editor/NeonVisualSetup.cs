using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class NeonVisualSetup
{
    [MenuItem("Tools/NeonWorlds/Apply Modern Visuals")]
    public static void Apply()
    {
        Shader planetShader=Shader.Find("NeonWorlds/OrbitalSurface");
        if(planetShader==null || !planetShader.isSupported) throw new System.Exception("Orbital surface shader unavailable.");
        Color[] accents={new Color(.07f,.35f,.42f),new Color(.3f,.18f,.46f),new Color(.1f,.36f,.29f),new Color(.45f,.22f,.12f),new Color(.22f,.26f,.48f),new Color(.4f,.15f,.3f)};
        for(int i=1;i<=6;i++)
        {
            GameObject planet=GameObject.Find("Planet_"+i);
            if(planet==null) continue;
            string path="Assets/Materials/ModernPlanet"+i+".mat";
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(planetShader); AssetDatabase.CreateAsset(material,path); }
            material.shader=planetShader; material.SetColor("_BaseColor",new Color(.012f,.023f,.045f));
            material.SetColor("_Accent",accents[i-1]); material.SetFloat("_GridDensity",36);
            planet.GetComponent<Renderer>().sharedMaterial=material; EditorUtility.SetDirty(material);
        }
        Material player=NeonMaterial("Assets/Materials/ModernPlayer.mat",new Color(.24f,.85f,.94f),1.8f);
        Material enemy=NeonMaterial("Assets/Materials/ModernEnemy.mat",new Color(1f,.25f,.36f),1.1f);
        Material swarmer=NeonMaterial("Assets/Materials/ModernSwarmer.mat",new Color(1f,.55f,.24f),1.1f);
        Material tank=NeonMaterial("Assets/Materials/ModernTank.mat",new Color(.67f,.4f,1f),1.1f);
        Material gem=NeonMaterial("Assets/Materials/ModernGem.mat",new Color(.36f,1f,.72f),1.2f);
        Material bullet=NeonMaterial("Assets/Materials/ModernBullet.mat",new Color(.58f,.93f,1f),2f);
        GameObject sun=GameObject.Find("Sun");
        if(sun!=null && sun.GetComponent<Renderer>()!=null)
            sun.GetComponent<Renderer>().sharedMaterial=NeonMaterial("Assets/Materials/ModernSun.mat",new Color(1f,.58f,.28f),1.4f);
        Material star=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/NeonStars.mat");
        if(star==null) { star=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(star,"Assets/Resources/NeonStars.mat"); }
        star.SetColor("_BaseColor",new Color(.35f,.46f,.65f)); star.SetFloat("_Cull",0); EditorUtility.SetDirty(star);
        PlayerShip ship=Object.FindAnyObjectByType<PlayerShip>();
        if(ship!=null)
        {
            const string meshPath="Assets/Models/ModernShip.asset";
            Mesh shipMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(shipMesh==null)
            {
                Vector3[] outline={new Vector3(0,.10f,1),new Vector3(.65f,.10f,-.7f),new Vector3(0,.10f,-.3f),new Vector3(-.65f,.10f,-.7f)};
                var vertices=new Vector3[8];
                for(int i=0;i<4;i++) { vertices[i]=outline[i]; vertices[i+4]=outline[i]-Vector3.up*.2f; }
                var indices=new System.Collections.Generic.List<int> {0,1,2,0,2,3,4,6,5,4,7,6};
                for(int i=0;i<4;i++) { int next=(i+1)%4; indices.AddRange(new[]{i,i+4,next+4,i,next+4,next}); }
                shipMesh=new Mesh { name="Modern arrow ship",vertices=vertices,triangles=indices.ToArray() };
                shipMesh.RecalculateNormals(); shipMesh.RecalculateBounds(); AssetDatabase.CreateAsset(shipMesh,meshPath);
            }
            ship.visual.GetComponent<MeshFilter>().sharedMesh=shipMesh;
            ship.visual.GetComponent<Renderer>().sharedMaterial=player;
            foreach(TrailRenderer trail in ship.GetComponentsInChildren<TrailRenderer>()) { trail.startWidth=.16f; trail.endWidth=0; trail.time=.18f; trail.startColor=new Color(.15f,.8f,1f,.6f); trail.endColor=new Color(.15f,.8f,1f,0); }
        }
        StylePrefab("Assets/Prefabs/EnemyPrefab.prefab",enemy);
        StylePrefab("Assets/Resources/SwarmerPrefab.prefab",swarmer);
        StylePrefab("Assets/Resources/TankPrefab.prefab",tank);
        StylePrefab("Assets/Resources/GemPrefab.prefab",gem);
        StylePrefab("Assets/Prefabs/BulletPrefab.prefab",bullet);
        RenderSettings.skybox=null;
        RenderSettings.ambientLight=new Color(.13f,.19f,.28f);
        Camera camera=Camera.main;
        if(camera!=null)
        {
            CameraFollow follow=camera.GetComponent<CameraFollow>();
            if(follow!=null) { follow.height=24f; follow.distance=16f; }
            camera.fieldOfView=55f;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.005f,.009f,.022f);
            var data=camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing=true;
            camera.allowHDR=true;
        }
        foreach(Volume volume in Object.FindObjectsByType<Volume>())
        {
            if(volume.sharedProfile==null) continue;
            VolumeProfile profile=volume.sharedProfile;
            if(profile.TryGet<Bloom>(out var bloom)) { bloom.intensity.Override(.65f); bloom.threshold.Override(1f); bloom.scatter.Override(.55f); }
            if(profile.TryGet<Vignette>(out var vignette)) vignette.intensity.Override(.18f);
            if(profile.TryGet<ColorAdjustments>(out var color)) { color.postExposure.Override(0f); color.saturation.Override(-6f); color.contrast.Override(8f); }
            EditorUtility.SetDirty(profile);
        }
        if(Object.FindAnyObjectByType<OrbitalStars>()==null) new GameObject("OrbitalStars").AddComponent<OrbitalStars>();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }
    static Material NeonMaterial(string path,Color color,float glow)
    {
        Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader=Shader.Find("NeonWorlds/EmissiveGeometry");
        if(shader==null || !shader.isSupported) throw new System.Exception("Neon geometry shader unavailable.");
        if(material==null) { material=new Material(shader); AssetDatabase.CreateAsset(material,path); }
        material.shader=shader;
        material.SetColor("_BaseColor",color*.55f); material.SetColor("_EmissionColor",color*glow);
        EditorUtility.SetDirty(material); return material;
    }
    static void StylePrefab(string path,Material material)
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null) return;
        GameObject root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(renderer.name.Contains("Health")) { renderer.gameObject.SetActive(false); continue; }
                renderer.sharedMaterial=material;
            }
            foreach(TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>(true)) { trail.time=.12f; trail.startWidth=.08f; trail.endWidth=0; }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
