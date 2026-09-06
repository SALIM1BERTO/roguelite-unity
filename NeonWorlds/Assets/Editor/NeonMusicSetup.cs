using UnityEditor;
using UnityEngine;

public static class NeonMusicSetup
{
    [MenuItem("Tools/NeonWorlds/Audio/Create Music Prefab")]
    public static void CreatePrefab()
    {
        const string prefabPath = "Assets/Resources/GameMusic.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
        string[] clips = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Music" });
        if (clips.Length != 1) throw new System.InvalidOperationException("Expected one soundtrack in Assets/Audio/Music.");
        string path = AssetDatabase.GUIDToAssetPath(clips[0]);
        AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
        importer.forceToMono = false;
        importer.loadInBackground = false;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = .7f;
        settings.preloadAudioData = true;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        GameObject root = new GameObject("GameMusic");
        try
        {
            root.AddComponent<GameMusic>().soundtrack = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); }
    }
}
