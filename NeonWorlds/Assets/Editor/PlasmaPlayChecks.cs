using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class PlasmaPlayChecks
{
    const string Key = "Neon.Plasma.Validation";
    static double started;
    static bool tested;
    static WeaponPlasmaFlamer flame;
    static int errors;
    static PlasmaPlayChecks()
    {
        if (!SessionState.GetBool(Key,false)) return;
        started=EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
        Application.logMessageReceived += Log;
    }
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
    }
    static void Log(string text,string stack,LogType type)
    {
        if (tested && (type==LogType.Exception || type==LogType.Error || type==LogType.Assert)) errors++;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground=true;
        try
        {
            double elapsed=Time.timeSinceLevelLoad;
            if (elapsed>3 && !tested)
            {
                PlasmaRegressionChecks.Run(); tested=true;
                var game=GameManager.Instance; game.isInvincible=true;
                if (EnemySpawner.Instance!=null) EnemySpawner.Instance.enabled=false;
                var weapon=game.player.GetComponent<Weapon>();
                weapon.currentWeapon=Weapon.WeaponType.PlasmaFlamer; weapon.SetupWeapon(); weapon.enabled=false;
                flame=game.player.gameObject.AddComponent<WeaponPlasmaFlamer>();
            }
            if (flame!=null)
            {
                Transform player=GameManager.Instance.player;
                Vector3 direction=Camera.main!=null ? Vector3.ProjectOnPlane(Camera.main.transform.up,player.up) : player.forward;
                flame.SetFiring(true,direction,2,1);
            }
            if (tested && elapsed>8)
            {
                var method=typeof(VisualRegressionChecks).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
                method.Invoke(null,new object[]{"plasma-flamer.png",1280,720});
                if(errors>0) throw new Exception(errors+" gameplay errors");
                Debug.Log("NEON_PLASMA_PLAY: PASS; scene, audio and flame smoke test; zero gameplay errors."); Finish(0);
            }
            if(EditorApplication.timeSinceStartup-started>100) throw new Exception("Plasma validation timed out");
        }
        catch(Exception e) { Debug.Log("NEON_PLASMA_PLAY: FAIL; "+e); Finish(1); }
    }
    static void Finish(int code)
    {
        SessionState.SetBool(Key,false); EditorApplication.update-=Tick;
        Application.logMessageReceived-=Log; EditorApplication.Exit(code);
    }
}


