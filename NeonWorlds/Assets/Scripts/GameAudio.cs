using System;
using UnityEngine;

public enum AudioCue
{
    Shot,
    Hit,
    Explosion,
    Pickup,
    LevelUp,
    Upgrade,
    PlayerHit,
    Teleport,
    SupernovaShot,
    NebulaShot,
    AntimatterBeam,
    VoidVortexDrone,
    TeslaArc,
    HyperionPulse,
    CriticalHit
}

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
        if (prefab != null)
        {
            Instantiate(prefab).name = "GameAudio";
            return;
        }
        GameObject audioObj = new GameObject("GameAudio");
        audioObj.AddComponent<GameAudio>();
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

        // Procedural synthesis fallback for any cues not in the prefab
        for (int i = 0; i < count; i++)
        {
            if (lookup[i] == null || lookup[i].clip == null)
            {
                AudioCue cue = (AudioCue)i;
                AudioClip genClip = GenerateProceduralClip(cue);
                if (genClip != null)
                {
                    lookup[i] = new Sound
                    {
                        cue = cue,
                        clip = genClip,
                        volume = GetDefaultVolume(cue),
                        pitchVariation = 0.05f,
                        minimumInterval = GetDefaultInterval(cue),
                        importance = GetDefaultImportance(cue),
                        interfaceSound = (cue == AudioCue.LevelUp || cue == AudioCue.Upgrade)
                    };
                }
            }
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

    static float GetDefaultVolume(AudioCue cue)
    {
        switch (cue)
        {
            case AudioCue.SupernovaShot: return 0.38f;
            case AudioCue.NebulaShot: return 0.52f;
            case AudioCue.AntimatterBeam: return 0.65f;
            case AudioCue.VoidVortexDrone: return 0.55f;
            case AudioCue.TeslaArc: return 0.45f;
            case AudioCue.HyperionPulse: return 0.60f;
            case AudioCue.CriticalHit: return 0.58f;
            default: return 0.5f;
        }
    }

    static float GetDefaultInterval(AudioCue cue)
    {
        switch (cue)
        {
            case AudioCue.SupernovaShot: return 0.035f;
            case AudioCue.NebulaShot: return 0.06f;
            case AudioCue.AntimatterBeam: return 0.1f;
            case AudioCue.VoidVortexDrone: return 0.2f;
            case AudioCue.TeslaArc: return 0.04f;
            case AudioCue.HyperionPulse: return 0.15f;
            case AudioCue.CriticalHit: return 0.05f;
            default: return 0.04f;
        }
    }

    static int GetDefaultImportance(AudioCue cue)
    {
        switch (cue)
        {
            case AudioCue.AntimatterBeam: return 85;
            case AudioCue.HyperionPulse: return 80;
            case AudioCue.CriticalHit: return 75;
            case AudioCue.NebulaShot: return 65;
            case AudioCue.SupernovaShot: return 60;
            default: return 50;
        }
    }

    AudioClip GenerateProceduralClip(AudioCue cue)
    {
        const int sampleRate = 44100;
        float duration = 0.1f;
        switch (cue)
        {
            case AudioCue.SupernovaShot: duration = 0.09f; break;
            case AudioCue.NebulaShot: duration = 0.14f; break;
            case AudioCue.AntimatterBeam: duration = 0.28f; break;
            case AudioCue.VoidVortexDrone: duration = 0.40f; break;
            case AudioCue.TeslaArc: duration = 0.08f; break;
            case AudioCue.HyperionPulse: duration = 0.25f; break;
            case AudioCue.CriticalHit: duration = 0.16f; break;
            default: duration = 0.1f; break;
        }

        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / sampleCount;
            float sample = 0f;

            switch (cue)
            {
                case AudioCue.SupernovaShot:
                    // Rapid laser chirp: 1300Hz down to 550Hz with fast decay
                    float freqSupernova = Mathf.Lerp(1300f, 550f, Mathf.Pow(progress, 0.7f));
                    float envSupernova = Mathf.Exp(-progress * 7f);
                    sample = Mathf.Sin(2f * Mathf.PI * freqSupernova * t) * envSupernova;
                    break;

                case AudioCue.NebulaShot:
                    // Resonant cluster burst + noise transient
                    float freqNebula = Mathf.Lerp(600f, 220f, progress);
                    float toneNebula = Mathf.Sin(2f * Mathf.PI * freqNebula * t);
                    float noiseNebula = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                    float envNebula = Mathf.Exp(-progress * 6f);
                    sample = (toneNebula * 0.6f + noiseNebula) * envNebula;
                    break;

                case AudioCue.AntimatterBeam:
                    // Heavy sub-bass boom + high voltage saturation
                    float freqBeam = Mathf.Lerp(160f, 42f, progress);
                    float toneBeam = Mathf.Sin(2f * Mathf.PI * freqBeam * t) * 1.8f;
                    sample = Mathf.Clamp(toneBeam, -0.95f, 0.95f) * Mathf.Exp(-progress * 4f);
                    break;

                case AudioCue.VoidVortexDrone:
                    // Undulating gravitational hum with low LFO
                    float lfo = 1f + 0.35f * Mathf.Sin(2f * Mathf.PI * 8f * t);
                    float freqVortex = Mathf.Lerp(95f, 55f, progress) * lfo;
                    sample = Mathf.Sin(2f * Mathf.PI * freqVortex * t) * (1f - progress);
                    break;

                case AudioCue.TeslaArc:
                    // Sharp electric crackle / square pulse wave
                    float freqTesla = UnityEngine.Random.Range(700f, 1800f);
                    float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freqTesla * t));
                    float envTesla = Mathf.Exp(-progress * 12f);
                    sample = sq * 0.7f * envTesla;
                    break;

                case AudioCue.HyperionPulse:
                    // Resonant metallic electromagnetic shockwave
                    float freqHyp = Mathf.Lerp(190f, 75f, progress);
                    float bell = Mathf.Sin(2f * Mathf.PI * freqHyp * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * freqHyp * 2.76f * t);
                    sample = bell * Mathf.Exp(-progress * 5f);
                    break;

                case AudioCue.CriticalHit:
                    // Harmonious crystalline chime (1500Hz + 2250Hz) with bell decay
                    float tone1 = Mathf.Sin(2f * Mathf.PI * 1500f * t);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * 2250f * t);
                    float envCrit = Mathf.Exp(-progress * 8f);
                    sample = (tone1 * 0.55f + tone2 * 0.45f) * envCrit;
                    break;

                default:
                    sample = Mathf.Sin(2f * Mathf.PI * 440f * t) * (1f - progress);
                    break;
            }

            samples[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("SFX_" + cue, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
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
