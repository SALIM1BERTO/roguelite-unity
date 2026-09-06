using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MusicRegressionChecks
{
    static readonly float[] samples = new float[4096];
    static bool running, hadPreference, originalEnabled, originalBackground, originalEditorMute, originalListenerPause;
    static int originalPreference, phase, lastFrame;
    static float originalTime, peak, musicPeak, silentPeak;
    static double phaseStarted;
    public static bool Complete { get; private set; }
    public static int Assertions { get; private set; }

    public static void Begin()
    {
        Assertions = 0;
        Complete = false;
        GameMusic music = GameMusic.Instance;
        Check(music != null && music.soundtrack != null, "Soundtrack bootstraps automatically");
        hadPreference = PlayerPrefs.HasKey(GameMusic.PreferenceKey);
        originalPreference = PlayerPrefs.GetInt(GameMusic.PreferenceKey, 1);
        originalEnabled = music.MusicEnabled;
        originalTime = Time.timeScale;
        originalBackground = Application.runInBackground;
        originalEditorMute = EditorUtility.audioMasterMute;
        originalListenerPause = AudioListener.pause;
        running = true;
        Application.runInBackground = true;
        EditorUtility.audioMasterMute = false;
        AudioListener.pause = false;
        Time.timeScale = 0f;
        GameAudio.SetGameplayPaused(true);
        GameAudio.Instance.StopAll();

        Check(music.soundtrack.name == "Purple_Horizon_Drift" && music.soundtrack.length > 10f, "User soundtrack is assigned");
        Check(music.soundtrack.loadType == AudioClipLoadType.Streaming, "Long soundtrack streams from disk");
        Check(music.Source.loop && music.Source.spatialBlend == 0f && music.Source.dopplerLevel == 0f,
            "Music loops independently of orbital movement");
        Check(music.Source.ignoreListenerPause, "Music stays available during level-up");
        for (int i = 0; i < 10; i++) GameMusic.EnsureExists();
        Check(Object.FindObjectsByType<GameMusic>().Length == 1 && music.GetComponentsInChildren<AudioSource>().Length == 1,
            "Repeated bootstrap keeps one music voice");
        music.SetMusicEnabled(false);
        Check(!music.Source.isPlaying && PlayerPrefs.GetInt(GameMusic.PreferenceKey) == 0, "Disabling music pauses and saves");

        // A hidden editor routes keyboard events away from the Game View by default.
        // Use temporary input settings only for the synthetic key, then restore them.
        InputSettings testInputSettings = InputSystem.settings;
        InputSettings.BackgroundBehavior originalInputBackground = testInputSettings.backgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode originalInputRouting = testInputSettings.editorInputBehaviorInPlayMode;
        InputSettings.UpdateMode originalInputUpdate = testInputSettings.updateMode;
        testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testInputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>("MusicRegressionKeyboard");
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F8));
            InputSystem.Update();
            typeof(GameMusic).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(music, null);
            Check(music.MusicEnabled && PlayerPrefs.GetInt(GameMusic.PreferenceKey) == 1, "F8 enables music and saves the choice");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            testInputSettings.backgroundBehavior = originalInputBackground;
            testInputSettings.editorInputBehaviorInPlayMode = originalInputRouting;
            testInputSettings.updateMode = originalInputUpdate;
        }

        ClickToggle(music);
        Check(!music.MusicEnabled && PlayerPrefs.GetInt(GameMusic.PreferenceKey) == 0, "HUD button disables music while paused");
        Object.DestroyImmediate(music.gameObject);
        GameMusic.EnsureExists();
        music = GameMusic.Instance;
        Check(!music.MusicEnabled && !music.Source.isPlaying, "Disabled preference survives a fresh music instance");
        MusicControls.Create(music);
        Check(GameAudio.Instance.GetComponentsInChildren<AudioSource>().Length == 14 && !GameAudio.Instance.muted,
            "Music preference preserves SFX voices and mute state");
        music.SetMusicEnabled(true);
        music.Source.time = Mathf.Min(12f, music.soundtrack.length * .25f);
        AudioListener.pause = true;
        phase = 0;
        ResetPhase();
        Debug.Log("NEON_MUSIC: soundtrack=" + music.soundtrack.name + "; duration=" + music.soundtrack.length
            + "; channels=" + music.soundtrack.channels + "; rate=" + music.soundtrack.frequency);
    }

    static void ClickToggle(GameMusic music)
    {
        MusicControls.Create(music);
        Canvas.ForceUpdateCanvases();
        Button button = music.GetComponentInChildren<Button>();
        Check(button != null && EventSystem.current != null, "Music control and event system exist");
        RectTransform rect = (RectTransform)button.transform;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        PointerEventData pointer = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && hits[0].gameObject == button.gameObject, "Music button receives pointer hits above the HUD");
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    static void ResetPhase()
    {
        peak = 0f;
        phaseStarted = EditorApplication.timeSinceStartup;
        lastFrame = Time.frameCount;
    }

    public static void Tick()
    {
        if (!running || Complete || lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        double elapsed = EditorApplication.timeSinceStartup - phaseStarted;
        AudioListener.GetOutputData(samples, 0);
        // Exclude the mixer buffer tail when measuring silence after Pause().
        if (phase != 1 || elapsed > .4d)
            foreach (float value in samples) peak = Mathf.Max(peak, Mathf.Abs(value));
        if (elapsed < (phase == 0 ? 2.5d : 1.2d)) return;
        if (phase == 0)
        {
            Check(peak > .0001f && peak < 1f, "User soundtrack reaches the mixer during pause: " + peak);
            musicPeak = peak;
            ClickToggle(GameMusic.Instance);
            Check(!GameMusic.Instance.MusicEnabled, "Button turns off an actively playing track");
            phase = 1;
            ResetPhase();
        }
        else if (phase == 1)
        {
            Check(peak < .0001f, "Disabled music produces silence: " + peak);
            silentPeak = peak;
            Check(GameAudio.Instance.TryPlay(AudioCue.LevelUp), "SFX remains enabled while music is off");
            phase = 2;
            ResetPhase();
        }
        else
        {
            Check(peak > .0001f && peak < 1f, "SFX reaches the mixer with music disabled: " + peak);
            Debug.Log("NEON_MUSIC: PASS; " + Assertions + " assertions; music peak=" + musicPeak
                + "; disabled peak=" + silentPeak + "; SFX peak=" + peak);
            Complete = true;
            End();
        }
    }

    public static void End()
    {
        if (!running) return;
        running = false;
        if (GameMusic.Instance != null) GameMusic.Instance.SetMusicEnabled(originalEnabled);
        if (hadPreference) PlayerPrefs.SetInt(GameMusic.PreferenceKey, originalPreference);
        else PlayerPrefs.DeleteKey(GameMusic.PreferenceKey);
        PlayerPrefs.Save();
        Time.timeScale = originalTime;
        Application.runInBackground = originalBackground;
        EditorUtility.audioMasterMute = originalEditorMute;
        AudioListener.pause = originalListenerPause;
        GameAudio.SetGameplayPaused(originalTime <= 0f);
        if (GameAudio.Instance != null) GameAudio.Instance.StopAll();
    }

    static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("Music regression: " + description);
        Assertions++;
    }
}
