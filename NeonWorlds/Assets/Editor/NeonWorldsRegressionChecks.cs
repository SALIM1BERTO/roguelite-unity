using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Explicit batch/Play Mode checks for ship dimensions and projectile regressions.</summary>
[InitializeOnLoad]
public static class NeonWorldsRegressionChecks
{
    const string RunningKey = "NeonWorlds.RegressionChecks.Running";
    static double started;
    static bool checkedRuntime;
    static bool captureRequested;
    static bool audioCounted;
    static bool musicStarted;
    static bool musicCounted;
    static bool bossStarted;
    static int assertions;
    static int runtimeErrors;
    static int editorSearchErrors;
    static string editorSearchDetails = "";

    static NeonWorldsRegressionChecks()
    {
        // This hook only resumes a test run explicitly started through RunBatch.
        if (SessionState.GetBool(RunningKey, false))
        {
            started = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
        }
    }

    public static void RunBatch()
    {
        try
        {
            NeonAudioSetup.CreatePrefab();
            NeonMusicSetup.CreatePrefab();
            StabilizePlayerSetup.MigrateSampleScene();
            NeonVisualSetup.Apply();
            CheckShip();
            SessionState.SetBool(RunningKey, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error)
        {
            Finish(error.ToString());
        }
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground = true;
        try
        {
            double elapsed = EditorApplication.timeSinceStartup - started;
            if (!checkedRuntime && elapsed > 2d && Time.timeSinceLevelLoad > 1.5f)
            {
                assertions += VisualRegressionChecks.Run();
                CheckShip();
                CheckTeleport();
                CheckNestedScale();
                assertions += CombatRegressionChecks.Run();
                assertions += AudioRegressionChecks.Run();
                AudioRegressionChecks.BeginOutputCapture();
                checkedRuntime = true;
                Debug.Log("NEON_CHECKS: behavioral checks passed; continuing scene smoke test.");
            }
            if (checkedRuntime && !audioCounted)
            {
                AudioRegressionChecks.CaptureFrame();
                if (AudioRegressionChecks.CaptureComplete)
                {
                    assertions += AudioRegressionChecks.CapturedAssertions;
                    audioCounted = true;
                }
            }
            if (checkedRuntime && !captureRequested && elapsed > 8d)
            {
                CaptureCamera();
                captureRequested = true;
            }
            if (audioCounted && !musicStarted)
            {
                MusicRegressionChecks.Begin();
                musicStarted = true;
            }
            if (musicStarted && !musicCounted)
            {
                MusicRegressionChecks.Tick();
                if (MusicRegressionChecks.Complete)
                {
                    assertions += MusicRegressionChecks.Assertions;
                    musicCounted = true;
                }
            }
            if (checkedRuntime && audioCounted && musicCounted && elapsed > 12d)
            {
                if(!bossStarted) { BossFightRegressionChecks.Begin(); bossStarted=true; }
                BossFightRegressionChecks.Tick();
                if(BossFightRegressionChecks.Complete)
                {
                    assertions+=BossFightRegressionChecks.Assertions;
                    Finish(runtimeErrors==0 ? null : runtimeErrors+" runtime errors were logged.");
                }
            }
            if (elapsed > 100d) Finish("Runtime tests timed out.");
        }
        catch (Exception error)
        {
            Finish(error.ToString());
        }
    }

    static void CheckShip()
    {
        PlayerShip ship = GameObject.Find("Player").GetComponent<PlayerShip>();
        Require(ship != null && ship.visual != null, "Player has its dedicated visual.");
        Near(ship.transform.lossyScale, Vector3.one, "Player world scale");
        Near(ship.visual.lossyScale, ship.visual.localScale, "Visual world scale remains independent of planet scale");
        Require(ship.visual.lossyScale.magnitude < 2.5f && ship.visual.lossyScale.magnitude > .5f, "Chassis stays within readable world dimensions");
        Require(!ship.GetComponent<MeshRenderer>().enabled, "Legacy root renderer disabled");
        Require(!ship.transform.Find("UfoBody").gameObject.activeSelf, "Legacy UFO disabled");
        Require(!ship.transform.Find("Cockpit").gameObject.activeSelf, "Legacy cockpit disabled");
        GravityBody body = ship.GetComponent<GravityBody>();
        float radius = Vector3.Distance(ship.transform.position, body.planet.transform.position);
        Require(Mathf.Abs(radius - body.planet.transform.lossyScale.x * 0.5f - ship.surfaceOffset) < 0.005f,
            "Player remains at configured surface offset");
        CapsuleCollider collider = ship.GetComponent<CapsuleCollider>();
        Require(Mathf.Abs(collider.radius - ship.colliderRadius) < 0.0001f, "Collider radius is independent of art scale");
    }

    static void CheckTeleport()
    {
        PlayerShip ship = GameObject.Find("Player").GetComponent<PlayerShip>();
        GravityBody body = ship.GetComponent<GravityBody>();
        PlanetGravity originalPlanet = body.planet;
        Vector3 originalPosition = ship.transform.position;
        Vector3 originalVisualScale = ship.visual.lossyScale;
        Quaternion originalRotation = ship.transform.rotation;
        EnemySpawner spawner = EnemySpawner.Instance;
        PlanetGravity originalSpawnPlanet = spawner.currentPlanet;
        GameObject gate = new GameObject("RegressionTestTeleport");
        Teleporter teleporter = gate.AddComponent<Teleporter>();
        MethodInfo enter = typeof(Teleporter).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            for (int index = 1; index <= 6; index++)
            {
                PlanetGravity target = GameObject.Find("Planet_" + index).GetComponent<PlanetGravity>();
                teleporter.targetPlanet = target;
                teleporter.cooldown = 0f;
                enter.Invoke(teleporter, new object[] { ship.GetComponent<Collider>() });
                Require(body.planet == target && ship.transform.parent == target.transform, "Teleport updates planet and parent");
                Require(spawner.currentPlanet == target, "Teleport updates spawner");
                Near(ship.transform.lossyScale, Vector3.one, "Teleport preserves root scale");
                Near(ship.visual.lossyScale, originalVisualScale, "Teleport preserves ship size");
                float distance = Vector3.Distance(ship.transform.position, target.transform.position);
                Require(Mathf.Abs(distance - target.transform.lossyScale.x * 0.5f - ship.surfaceOffset) < 0.005f,
                    "Teleport preserves surface clearance");
            }
        }
        finally
        {
            body.planet = originalPlanet;
            ship.transform.SetParent(originalPlanet.transform, true);
            ship.transform.SetPositionAndRotation(originalPosition, originalRotation);
            spawner.currentPlanet = originalSpawnPlanet;
            PlanetaryBiome.OnPlayerArrived(originalPlanet);
            UnityEngine.Object.DestroyImmediate(gate);
        }
    }

    static void CheckNestedScale()
    {
        GameObject ancestor = new GameObject("RegressionTestScaledAncestor");
        GameObject planet = new GameObject("RegressionTestScaledPlanet");
        GameObject player = new GameObject("RegressionTestScaledShip");
        try
        {
            ancestor.transform.localScale = Vector3.one * 2f;
            planet.transform.SetParent(ancestor.transform, false);
            planet.transform.localScale = Vector3.one * 10f;
            player.transform.SetParent(planet.transform, false);
            PlayerShip ship = player.AddComponent<PlayerShip>();
            Near(player.transform.lossyScale, Vector3.one, "Nested scaling uses lossyScale");
            player.transform.localRotation = Quaternion.Euler(35, 61, 14);
            planet.transform.localScale = Vector3.one * 30f;
            ship.ApplyWorldScale();
            Near(player.transform.lossyScale, Vector3.one, "Resized planet retains ship scale");
            player.transform.SetParent(null, true);
            Near(player.transform.lossyScale, Vector3.one, "Unparenting retains ship scale");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(ancestor);
        }
    }

    static void Near(Vector3 actual, Vector3 expected, string description)
    {
        Require((actual - expected).sqrMagnitude < 0.00001f, description + ": " + actual);
    }

    static void CaptureCamera()
    {
        Camera camera = Camera.main;
        RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        Texture2D pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        float originalAspect = camera.aspect;
        RenderTexture originalTarget = camera.targetTexture;
        Canvas musicCanvas = GameMusic.Instance.GetComponentInChildren<Canvas>();
        RenderMode originalMode = musicCanvas.renderMode;
        Camera originalCanvasCamera = musicCanvas.worldCamera;
        float originalPlaneDistance = musicCanvas.planeDistance;
        try
        {
            camera.aspect = 1280f / 720f;
            camera.targetTexture = target;
            musicCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            musicCanvas.worldCamera = camera;
            musicCanvas.planeDistance = camera.nearClipPlane + 1f;
            Canvas.ForceUpdateCanvases();
            var request = new RenderPipeline.StandardRequest { destination = target };
            Require(RenderPipeline.SupportsRenderRequest(camera, request), "URP supports validation render request");
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.GetFullPath("../validation-game.png"), pixels.EncodeToPNG());
        }
        finally
        {
            musicCanvas.renderMode = originalMode;
            musicCanvas.worldCamera = originalCanvasCamera;
            musicCanvas.planeDistance = originalPlaneDistance;
            camera.targetTexture = originalTarget;
            Canvas.ForceUpdateCanvases();
            camera.aspect = originalAspect;
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }

    static void Require(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        assertions++;
    }

    static void OnLog(string message, string trace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        // Unity 6000.5's search indexer can fail during test startup. Keep the
        // exact editor-only failure in the report; every other error fails the run.
        if (SessionState.GetBool(RunningKey, false) && type == LogType.Exception &&
            message.StartsWith("ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection.") &&
            message.Contains("Parameter name: index") &&
            trace.Contains("UnityEditor.Search.SearchDatabase+<EnumerateAll>") &&
            trace.Contains("UnityEditor.Search.SearchDatabase.GetDefaultSearchDatabase") &&
            trace.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup") &&
            !trace.Contains("Assets/") && !trace.Contains("Assets\\"))
        {
            editorSearchErrors++;
            editorSearchDetails += message + "\n" + trace + "\n";
            return;
        }
        runtimeErrors++;
    }

    static void Finish(string failure)
    {
        BossFightRegressionChecks.End();
        AudioRegressionChecks.EndOutputCapture();
        MusicRegressionChecks.End();
        SessionState.EraseBool(RunningKey);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        string result = failure == null ? "PASS: " + assertions + " assertions; scene smoke test and three-phase boss encounter; zero gameplay runtime errors."
            : "FAIL: " + failure;
        result += " Editor search startup errors (reported separately): " + editorSearchErrors + ".";
        File.WriteAllText(Path.GetFullPath("../regression-result.txt"), result);
        File.WriteAllText(Path.GetFullPath("../regression-editor-errors.txt"), editorSearchDetails);
        Debug.Log("NEON_CHECKS: " + result);
        EditorApplication.Exit(failure == null ? 0 : 1);
    }
}
