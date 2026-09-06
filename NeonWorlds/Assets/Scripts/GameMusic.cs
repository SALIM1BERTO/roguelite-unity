using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Persistent background music with an independent, saved on/off preference.</summary>
[DisallowMultipleComponent]
public class GameMusic : MonoBehaviour
{
    public const string PreferenceKey = "NeonWorlds.Audio.MusicEnabled";
    public static GameMusic Instance { get; private set; }
    public AudioClip soundtrack;
    [Range(0f, 1f)] public float volume = 0.3f;
    public bool MusicEnabled { get; private set; }
    public AudioSource Source { get; private set; }
    bool startedPlayback;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    public static void EnsureExists()
    {
        if (Instance != null) return;
        GameMusic existing = FindAnyObjectByType<GameMusic>();
        if (existing != null) { Instance = existing; return; }
        GameObject prefab = Resources.Load<GameObject>("GameMusic");
        if (prefab != null) Instantiate(prefab).name = "GameMusic";
        else Debug.LogError("GameMusic prefab is missing from Resources.");
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        GameObject voice = new GameObject("MusicSource");
        voice.transform.SetParent(transform, false);
        Source = voice.AddComponent<AudioSource>();
        Source.playOnAwake = false;
        Source.loop = true;
        Source.spatialBlend = 0f;
        Source.dopplerLevel = 0f;
        Source.ignoreListenerPause = true;
        Source.priority = 64;
        Source.clip = soundtrack;
        MusicEnabled = PlayerPrefs.GetInt(PreferenceKey, 1) != 0;
        RefreshVolume();
    }

    void Start()
    {
        ApplyPlayback();
        MusicControls.Create(this);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) ToggleMusic();
        RefreshVolume();
    }

    public void ToggleMusic() { SetMusicEnabled(!MusicEnabled); }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyPlayback();
    }

    void ApplyPlayback()
    {
        if (Source == null || soundtrack == null || !isActiveAndEnabled) return;
        if (!MusicEnabled) { Source.Pause(); return; }
        if (startedPlayback) Source.UnPause();
        else { Source.Play(); startedPlayback = true; }
    }

    void RefreshVolume()
    {
        if (Source == null) return;
        GameAudio audio = GameAudio.Instance;
        Source.volume = Mathf.Clamp01(volume) * (audio != null ? Mathf.Clamp01(audio.masterVolume) : 1f);
        Source.mute = audio != null && audio.muted;
    }

    void OnEnable() { if (Source != null) ApplyPlayback(); }
    void OnDisable() { if (Source != null) Source.Pause(); }
    void OnDestroy() { if (Instance == this) Instance = null; }
}
