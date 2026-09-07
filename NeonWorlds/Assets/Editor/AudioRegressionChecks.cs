using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AudioRegressionChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int passed;

    public static int Run()
    {
        passed = 0;
        GameAudio audio = GameAudio.Instance;
        Check(audio != null, "Audio bootstrap exists");
        AudioSource[] sources = audio.GetComponentsInChildren<AudioSource>();
        Check(sources.Length == 14, "Twelve gameplay voices and two UI voices were preallocated");
        foreach (AudioSource source in sources)
            Check(source.spatialBlend == 0f && source.dopplerLevel == 0f && !source.playOnAwake,
                "Orbital movement cannot change SFX attenuation or pitch");

        GameManager game = GameManager.Instance;
        EnemySpawner originalSpawner = EnemySpawner.Instance;
        Weapon weapon = game.player.GetComponent<Weapon>();
        int originalSpread = weapon.spreadCount, originalXp = game.xp, originalHp = game.hp;
        int originalThreshold = game.xpToNextLevel;
        float originalTime = Time.timeScale, originalVolume = audio.masterVolume;
        bool originalMuted = audio.muted, originalListenerPause = AudioListener.pause;
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        var before = new HashSet<GameObject>(Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include));
        var bulletsBefore = new HashSet<Bullet>(Object.FindObjectsByType<Bullet>(FindObjectsInactive.Include));
        float[] intervals = new float[audio.sounds.Length];
        for (int i = 0; i < intervals.Length; i++) intervals[i] = audio.sounds[i].minimumInterval;
        try
        {
            EnemySpawner.Instance = null;
            audio.muted = false;
            audio.masterVolume = .8f;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameAudio.SetGameplayPaused(false);
            Check(audio.sounds.Length == 8, "All eight cues are configured");
            foreach (GameAudio.Sound sound in audio.sounds)
            {
                Check(sound.clip != null && sound.clip.length > .05f, "Cue has an imported clip: " + sound.cue);
                float[] samples = new float[sound.clip.samples * sound.clip.channels];
                Check(sound.clip.GetData(samples, 0), "PCM data is readable: " + sound.cue);
                float peak = 0f;
                foreach (float value in samples) peak = Mathf.Max(peak, Mathf.Abs(value));
                Check(peak > .05f && peak < .95f, "Clip is audible and has headroom: " + sound.cue);
            }

            Clear(audio, sources);
            Check(audio.TryPlay(AudioCue.Shot), "First shot is accepted");
            Check(!audio.TryPlay(AudioCue.Shot), "A same-frame shot burst is rate limited");
            Check(Count(sources, Clip(audio, AudioCue.Shot)) == 1, "Rate limiting retains one source");
            Clear(audio, sources);
            UnityEngine.Random.State beforePitch = UnityEngine.Random.state;
            audio.TryPlay(AudioCue.Shot);
            Check(UnityEngine.Random.state.Equals(beforePitch), "Pitch variation preserves gameplay random state");

            Clear(audio, sources);
            weapon.spreadCount = 3;
            Invoke(weapon, "Shoot", Vector2.right);
            Check(Count(sources, Clip(audio, AudioCue.Shot)) == 1, "Spread volley plays one shot sound");

            Clear(audio, sources);
            GameObject enemyObject = new GameObject("AudioRegressionEnemy");
            enemyObject.transform.position = game.player.position;
            Enemy enemy = enemyObject.AddComponent<Enemy>();
            enemy.maxHp = 100;
            enemy.hp = 100;
            GameObject bulletObject = new GameObject("AudioRegressionBullet");
            bulletObject.AddComponent<Rigidbody>();
            Bullet bullet = bulletObject.AddComponent<Bullet>();
            bullet.damage = 5;
            FindSound(audio, AudioCue.Hit).minimumInterval = 0f;
            Invoke(bullet, "HandleHit", enemyObject);
            Invoke(bullet, "HandleHit", enemyObject);
            Check(Count(sources, Clip(audio, AudioCue.Hit)) == 1 && enemy.hp == 95,
                "Repeated projectile callbacks produce one impact sound");

            Clear(audio, sources);
            enemy.TakeDamage(200);
            enemy.TakeDamage(200);
            Check(Count(sources, Clip(audio, AudioCue.Explosion)) == 1, "Enemy death plays one explosion");
            Check(Count(sources, Clip(audio, AudioCue.Hit)) == 0, "Killing blow uses the explosion cue");

            Clear(audio, sources);
            game.xpToNextLevel = 100000;
            int beforeXp = game.xp;
            GameObject gemObject = new GameObject("AudioRegressionGem");
            gemObject.AddComponent<Rigidbody>();
            XpGem gem = gemObject.AddComponent<XpGem>();
            Invoke(gem, "OnTriggerEnter", game.player.GetComponent<Collider>());
            Invoke(gem, "OnTriggerEnter", game.player.GetComponent<Collider>());
            int expectedGemXP=PlanetaryBiome.CurrentBiome!=null ? Mathf.RoundToInt(10*PlanetaryBiome.CurrentBiome.xpMultiplier) : 10;
            Check(game.xp == beforeXp + expectedGemXP && Count(sources, Clip(audio, AudioCue.Pickup)) == 1,
                "Duplicate gem contacts award XP and play pickup once");

            Clear(audio, sources);
            game.TakeDamage(1);
            Check(Count(sources, Clip(audio, AudioCue.PlayerHit)) == 1, "Player damage is audible");

            Clear(audio, sources);
            Invoke(game, "ShowLevelUpScreen");
            Check(Time.timeScale == 0f && game.levelUpPanel.activeSelf, "Level-up opens while paused");
            Check(Count(sources, Clip(audio, AudioCue.LevelUp)) == 1, "Level-up chime is assigned while paused");
            Check(!audio.TryPlay(AudioCue.Shot), "Gameplay sounds are blocked during level-up");
            foreach (AudioSource source in sources)
                if (source.clip == Clip(audio, AudioCue.LevelUp)) Check(source.ignoreListenerPause, "UI voice bypasses listener pause");
            FieldInfo upgrades = typeof(GameManager).GetField("currentUpgrades", Private);
            upgrades.SetValue(game, new[] { GameManager.UpgradeType.Spread, GameManager.UpgradeType.Speed, GameManager.UpgradeType.Magnet });
            Invoke(game, "ApplyUpgrade", 0);
            Check(Time.timeScale == 1f && Count(sources, Clip(audio, AudioCue.Upgrade)) == 1,
                "Selecting upgrade resumes gameplay and plays confirmation");

            Clear(audio, sources);
            GameObject otherPlanet = new GameObject("AudioRegressionOtherPlanet");
            GameAudio.PlayAt(AudioCue.Hit, game.player.position, otherPlanet.AddComponent<PlanetGravity>());
            Check(Count(sources, Clip(audio, AudioCue.Hit)) == 0, "Impacts on other planets are filtered");

            Clear(audio, sources);
            foreach (GameAudio.Sound sound in audio.sounds) sound.minimumInterval = 0f;
            for (int i = 0; i < 1000; i++) audio.TryPlay(AudioCue.Hit);
            Check(audio.GetComponentsInChildren<AudioSource>().Length == 14, "A thousand hits allocate no extra voices");
            Check(audio.TryPlay(AudioCue.LevelUp), "A saturated gameplay budget leaves UI voices available");
            Check(Count(sources, Clip(audio, AudioCue.Hit)) <= 12, "Gameplay overlap stays within budget");

            Clear(audio, sources);
            audio.muted = true;
            Check(!audio.TryPlay(AudioCue.Shot) && !audio.TryPlay(AudioCue.LevelUp), "Mute blocks both channels");
            audio.muted = false;
            audio.masterVolume = 0f;
            Check(!audio.TryPlay(AudioCue.Shot), "Zero master volume blocks sounds");
            audio.masterVolume = .8f;


            Debug.Log("NEON_AUDIO: " + passed + " functional assertions passed.");
            return passed;
        }
        finally
        {
            audio.StopAll();
            audio.masterVolume = originalVolume;
            audio.muted = originalMuted;
            for (int i = 0; i < intervals.Length; i++) audio.sounds[i].minimumInterval = intervals[i];
            Time.timeScale = originalTime;
            AudioListener.pause = originalListenerPause;
            GameAudio.SetGameplayPaused(originalTime <= 0f);
            game.xp = originalXp;
            game.hp = originalHp;
            game.xpToNextLevel = originalThreshold;
            weapon.spreadCount = originalSpread;
            if (game.levelUpPanel != null) game.levelUpPanel.SetActive(false);
            EnemySpawner.Instance = originalSpawner;
            UnityEngine.Random.state = randomState;
            foreach (Bullet newBullet in Object.FindObjectsByType<Bullet>(FindObjectsInactive.Include))
            {
                if (newBullet != null && !bulletsBefore.Contains(newBullet) && newBullet.pool != null)
                {
                    if (newBullet.gameObject.activeSelf) newBullet.pool.Release(newBullet.gameObject);
                    before.Add(newBullet.gameObject);
                    foreach (Transform child in newBullet.GetComponentsInChildren<Transform>(true)) before.Add(child.gameObject);
                }
            }
            foreach (GameObject created in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
                if (created != null && !before.Contains(created) && !created.name.Contains("LevelUp"))
                {
                    // RuntimeUIBuilder may create the level-up UI; retain its complete hierarchy.
                    if (game.levelUpPanel != null && created.transform.IsChildOf(game.levelUpPanel.transform)) continue;
                    Object.DestroyImmediate(created);
                }
        }
    }

    static bool recording;
    static bool originalEditorMute;
    static bool originalBackground;
    static int originalCapture, lastFrame, captureFrames, phase, outputStart;
    static float outputPeak, shotPeak;
    static double captureStarted;
    static readonly float[] outputBuffer = new float[4096];
    public static bool CaptureComplete { get; private set; }
    public static int CapturedAssertions { get { return passed - outputStart; } }

    public static void BeginOutputCapture()
    {
        outputStart = passed;
        originalCapture = Time.captureFramerate;
        originalEditorMute = EditorUtility.audioMasterMute;
        EditorUtility.audioMasterMute = false;
        originalBackground = Application.runInBackground;
        Application.runInBackground = true;
        Time.captureFramerate = 0;
        recording = true;
        if (GameMusic.Instance != null) GameMusic.Instance.Source.Pause();
        GameAudio audio = GameAudio.Instance;
        Clear(audio, audio.GetComponentsInChildren<AudioSource>());
        Check(audio.TryPlay(AudioCue.Shot), "Shot accepted for mixer capture");
        foreach (AudioSource source in audio.GetComponentsInChildren<AudioSource>())
            if (source.clip != null) source.loop = true;
        captureStarted = EditorApplication.timeSinceStartup;
        lastFrame = Time.frameCount;
        phase = captureFrames = 0;
        outputPeak = 0f;
        CaptureComplete = false;
        Debug.Log("NEON_AUDIO: capture rate=" + AudioSettings.outputSampleRate + "; listener volume=" + AudioListener.volume
            + "; listeners=" + Object.FindObjectsByType<AudioListener>().Length + "; dsp=" + AudioSettings.dspTime
            + "; original editor mute=" + originalEditorMute);
    }

    public static void CaptureFrame()
    {
        if (CaptureComplete || Time.frameCount == lastFrame) return;
        // Observe the live mixer over real frames, including while game time is paused.
        lastFrame = Time.frameCount;
        // Editor startup work can block after BeginOutputCapture. Start the observation
        // window when the player loop actually resumes, rather than counting that stall.
        if (captureFrames == 0) captureStarted = EditorApplication.timeSinceStartup;
        if (captureFrames == 2)
        {
            foreach (AudioSource source in GameAudio.Instance.GetComponentsInChildren<AudioSource>())
                if (source.clip != null)
                    Debug.Log("NEON_AUDIO: " + source.clip.name + "; playing=" + source.isPlaying
                        + "; timeSamples=" + source.timeSamples + "; volume=" + source.volume + "; loaded=" + source.clip.loadState);
        }
        AudioListener.GetOutputData(outputBuffer, 0);
        foreach (float sample in outputBuffer) outputPeak = Mathf.Max(outputPeak, Mathf.Abs(sample));
        captureFrames++;
        if (captureFrames < 5 || EditorApplication.timeSinceStartup - captureStarted < 2d) return;
        Check(outputPeak > .0001f, "Unity mixer output is non-silent in phase " + phase + ": " + outputPeak);
        Check(outputPeak < 1f, "Mixer output retains headroom");
        if (phase == 0)
        {
            shotPeak = outputPeak;
            phase = 1;
            outputPeak = 0f;
            captureFrames = 0;
            GameAudio audio = GameAudio.Instance;
            Clear(audio, audio.GetComponentsInChildren<AudioSource>());
            Time.timeScale = 0f;
            GameAudio.SetGameplayPaused(true);
            AudioListener.pause = true;
            Check(audio.TryPlay(AudioCue.LevelUp), "UI chime accepted while time and listener are paused");
            foreach (AudioSource source in audio.GetComponentsInChildren<AudioSource>())
                if (source.clip != null) source.loop = true;
            captureStarted = EditorApplication.timeSinceStartup;
        }
        else
        {
            Debug.Log("NEON_AUDIO: mixer shot peak=" + shotPeak + "; paused UI peak=" + outputPeak);
            CaptureComplete = true;
            EndOutputCapture();
        }
    }

    public static void EndOutputCapture()
    {
        if (!recording) return;
        recording = false;
        Time.captureFramerate = originalCapture;
        Application.runInBackground = originalBackground;
        EditorUtility.audioMasterMute = originalEditorMute;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (GameMusic.Instance != null && GameMusic.Instance.MusicEnabled) GameMusic.Instance.Source.UnPause();
        GameAudio.SetGameplayPaused(false);
        if (GameAudio.Instance != null)
        {
            GameAudio.Instance.StopAll();
            foreach (AudioSource source in GameAudio.Instance.GetComponentsInChildren<AudioSource>()) source.loop = false;
        }
    }

    static void Clear(GameAudio audio, AudioSource[] sources)
    {
        audio.StopAll();
        foreach (AudioSource source in sources) { source.clip = null; source.loop = false; }
        var cooldowns = (double[])typeof(GameAudio).GetField("nextAllowed", Private).GetValue(audio);
        Array.Clear(cooldowns, 0, cooldowns.Length);
    }

    static GameAudio.Sound FindSound(GameAudio audio, AudioCue cue)
    {
        foreach (GameAudio.Sound sound in audio.sounds) if (sound.cue == cue) return sound;
        throw new Exception("Missing cue: " + cue);
    }
    static AudioClip Clip(GameAudio audio, AudioCue cue) { return FindSound(audio, cue).clip; }
    static int Count(AudioSource[] sources, AudioClip clip)
    {
        int count = 0;
        foreach (AudioSource source in sources) if (source.clip == clip) count++;
        return count;
    }
    static void Invoke(object target, string name, params object[] parameters)
    {
        target.GetType().GetMethod(name, Private | BindingFlags.Public).Invoke(target, parameters);
    }
    static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("Audio regression: " + description);
        passed++;
    }
}
