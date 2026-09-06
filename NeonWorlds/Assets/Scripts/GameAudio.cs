using System;
using UnityEngine;

public enum AudioCue { Shot, Hit, Explosion, Pickup, LevelUp, Upgrade, PlayerHit, Teleport }

/// <summary>Preallocated arcade SFX voices; independent of orbital speed and gameplay RNG.</summary>
[DisallowMultipleComponent]
public class GameAudio : MonoBehaviour
{
    [Serializable]
    public class Sound
    {
        public AudioCue cue;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.5f;
        [Range(0f, 0.2f)] public float pitchVariation = 0.04f;
        [Min(0f)] public float minimumInterval = 0.04f;
        [Range(0, 100)] public int importance = 50;
        public bool interfaceSound;
    }

    public static GameAudio Instance { get; private set; }
    [Header("Mix")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float gameplayVolume = 0.8f;
    [Range(0f, 1f)] public float interfaceVolume = 0.85f;
    public bool muted;
    [Header("Voice budget (allocated at startup)")]
    [Range(4, 24)] public int gameplayVoiceLimit = 12;
    [Range(1, 4)] public int interfaceVoiceLimit = 2;
    public Sound[] sounds = Array.Empty<Sound>();

    class Voice
    {
        public AudioSource source;
        public double until;
        public Sound sound;
        public float distanceGain;
    }

    Voice[] voices;
    Sound[] lookup;
    double[] nextAllowed;
    int gameplayVoices;
    bool gameplayPaused;
    Camera mainCamera;
    uint pitchState = 0x9E3779B9u;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    public static void EnsureExists()
    {
        if (Instance != null) return;
        GameAudio existing = FindAnyObjectByType<GameAudio>();
        if (existing != null)
        {
            Instance = existing;
            existing.Initialize();
            return;
        }
        GameObject prefab = Resources.Load<GameObject>("GameAudio");
        if (prefab == null)
        {
            Debug.LogError("GameAudio prefab is missing from Resources. Run Tools/NeonWorlds/Audio/Create Audio Prefab.");
            return;
        }
        Instantiate(prefab).name = "GameAudio";
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    void Initialize()
    {
        if (voices != null) return;
        int count = Enum.GetValues(typeof(AudioCue)).Length;
        lookup = new Sound[count];
        nextAllowed = new double[count];
        foreach (Sound sound in sounds)
        {
            if (sound == null || (int)sound.cue < 0 || (int)sound.cue >= count) continue;
            lookup[(int)sound.cue] = sound;
            if (sound.clip != null) sound.clip.LoadAudioData();
        }
        gameplayVoices = Mathf.Clamp(gameplayVoiceLimit, 4, 24);
        voices = new Voice[gameplayVoices + Mathf.Clamp(interfaceVoiceLimit, 1, 4)];
        for (int i = 0; i < voices.Length; i++)
        {
            GameObject child = new GameObject(i < gameplayVoices ? "GameplayVoice" : "InterfaceVoice");
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.ignoreListenerPause = i >= gameplayVoices;
            voices[i] = new Voice { source = source };
        }
        gameplayPaused = Time.timeScale <= 0f;
    }

    public static void Play(AudioCue cue)
    {
        if (Instance != null) Instance.TryPlay(cue);
    }

    public static void PlayAt(AudioCue cue, Vector3 position, PlanetGravity planet)
    {
        if (Instance == null) return;
        float gain = 1f;
        GameManager game = GameManager.Instance;
        if (game != null && game.player != null)
        {
            GravityBody playerBody = game.player.GetComponent<GravityBody>();
            if (planet != null && playerBody != null && playerBody.planet != planet) return;
            gain = 1f / (1f + Vector3.Distance(game.player.position, position) * 0.035f);
        }
        if (Instance.mainCamera == null) Instance.mainCamera = Camera.main;
        float pan = 0f;
        if (Instance.mainCamera != null)
        {
            Vector3 screen = Instance.mainCamera.WorldToViewportPoint(position);
            if (screen.z > 0f) pan = Mathf.Clamp((screen.x - 0.5f) * 1.3f, -0.65f, 0.65f);
        }
        Instance.TryPlay(cue, gain, pan);
    }

    public bool TryPlay(AudioCue cue, float gain = 1f, float pan = 0f)
    {
        if (!isActiveAndEnabled || voices == null || (int)cue < 0 || (int)cue >= lookup.Length) return false;
        Sound sound = lookup[(int)cue];
        if (sound == null || sound.clip == null || muted || masterVolume <= 0f || gain <= 0f) return false;
        if (sound.interfaceSound ? interfaceVolume <= 0f : gameplayVolume <= 0f) return false;
        if (!sound.interfaceSound && (gameplayPaused || Time.timeScale <= 0f)) return false;
        double now = Time.unscaledTimeAsDouble;
        if (now < nextAllowed[(int)cue]) return false;

        int first = sound.interfaceSound ? gameplayVoices : 0;
        int end = sound.interfaceSound ? voices.Length : gameplayVoices;
        Voice selected = null;
        for (int i = first; i < end; i++)
        {
            Voice voice = voices[i];
            if (voice.until <= now) { selected = voice; break; }
            if (voice.sound.importance <= sound.importance &&
                (selected == null || voice.sound.importance < selected.sound.importance ||
                 (voice.sound.importance == selected.sound.importance && voice.until < selected.until)))
                selected = voice;
        }
        if (selected == null) return false;
        // Local PRNG: cosmetic pitch must never change enemy spawns or upgrade rolls.
        pitchState ^= pitchState << 13;
        pitchState ^= pitchState >> 17;
        pitchState ^= pitchState << 5;
        float variation = Mathf.Clamp(sound.pitchVariation, 0f, 0.2f);
        float pitch = 1f + ((pitchState & 65535u) / 65535f * 2f - 1f) * variation;
        selected.source.Stop();
        selected.source.clip = sound.clip;
        selected.source.pitch = pitch;
        selected.source.panStereo = Mathf.Clamp(pan, -1f, 1f);
        selected.source.priority = sound.interfaceSound ? 32 : 255 - Mathf.Clamp(sound.importance, 0, 100);
        selected.sound = sound;
        selected.distanceGain = Mathf.Clamp01(gain);
        selected.until = now + sound.clip.length / pitch;
        nextAllowed[(int)cue] = now + Mathf.Max(0f, sound.minimumInterval);
        RefreshVolumes(now);
        selected.source.Play();
        return true;
    }

    public static void SetGameplayPaused(bool paused)
    {
        if (Instance != null) Instance.ApplyPause(paused);
    }

    void ApplyPause(bool paused)
    {
        gameplayPaused = paused;
        if (!paused || voices == null) return;
        for (int i = 0; i < gameplayVoices; i++)
        {
            voices[i].source.Stop();
            voices[i].until = 0d;
        }
    }

    void Update()
    {
        if (gameplayPaused != (Time.timeScale <= 0f)) ApplyPause(Time.timeScale <= 0f);
        RefreshVolumes(Time.unscaledTimeAsDouble);
    }

    void RefreshVolumes(double now)
    {
        if (voices == null) return;
        int activeGameplay = 0;
        for (int i = 0; i < gameplayVoices; i++) if (voices[i].until > now) activeGameplay++;
        float crowdGain = 1f / Mathf.Sqrt(Mathf.Max(1, activeGameplay));
        foreach (Voice voice in voices)
        {
            if (voice.sound == null) continue;
            float channel = voice.sound.interfaceSound ? interfaceVolume : gameplayVolume * crowdGain;
            voice.source.volume = muted ? 0f : Mathf.Clamp01(masterVolume) * Mathf.Clamp01(channel)
                * Mathf.Clamp01(voice.sound.volume) * voice.distanceGain;
        }
    }

    public void StopAll()
    {
        if (voices == null) return;
        foreach (Voice voice in voices) { voice.source.Stop(); voice.until = 0d; }
    }

    void OnDisable() { StopAll(); }
    void OnDestroy() { if (Instance == this) Instance = null; }
}
