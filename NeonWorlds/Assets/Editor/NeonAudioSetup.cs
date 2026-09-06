using UnityEditor;
using UnityEngine;

public static class NeonAudioSetup
{
    [MenuItem("Tools/NeonWorlds/Audio/Create Audio Prefab")]
    public static void CreatePrefab()
    {
        const string prefabPath = "Assets/Resources/GameAudio.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
        GameObject root = new GameObject("GameAudio");
        try
        {
            GameAudio audio = root.AddComponent<GameAudio>();
            audio.sounds = new[]
            {
                Entry(AudioCue.Shot, "shot", .42f, .055f, .035f, 50),
                Entry(AudioCue.Hit, "hit", .35f, .10f, .035f, 30),
                Entry(AudioCue.Explosion, "explosion", .65f, .09f, .06f, 60),
                Entry(AudioCue.Pickup, "pickup", .25f, .08f, .075f, 20),
                Entry(AudioCue.LevelUp, "level_up", .7f, 0f, .2f, 100, true),
                Entry(AudioCue.Upgrade, "upgrade", .5f, .025f, .08f, 80, true),
                Entry(AudioCue.PlayerHit, "player_hit", .65f, .035f, .12f, 90),
                Entry(AudioCue.Teleport, "teleport", .55f, .02f, .2f, 70),
            };
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); }
    }

    static GameAudio.Sound Entry(AudioCue cue, string file, float volume, float variation, float interval, int importance, bool ui = false)
    {
        string path = "Assets/Audio/SFX/" + file + ".wav";
        AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new System.IO.FileNotFoundException(path);
        importer.forceToMono = true;
        importer.loadInBackground = false;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.preloadAudioData = true;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) throw new System.InvalidOperationException("Audio clip did not import: " + path);
        return new GameAudio.Sound { cue = cue, clip = clip, volume = volume, pitchVariation = variation,
            minimumInterval = interval, importance = importance, interfaceSound = ui };
    }
}
