using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class VisualRegressionChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int assertions;
    static void Check(bool condition,string message) { if(!condition) throw new Exception("Visual regression: "+message); assertions++; }
    public static int Run()
    {
        assertions=0;
        GameManager game=GameManager.Instance;
        ModernHUD hud=Object.FindAnyObjectByType<ModernHUD>();
        Check(hud!=null,"Modern HUD exists");
        Check(Object.FindObjectsByType<ModernHUD>().Length==1,"No duplicate HUD");
        Check(Object.FindObjectsByType<UIManager>().Length==0,"Legacy HUD is inactive");
        Check(game.xpFill!=null && game.levelText!=null,"HUD is bound to progression");
        Check(game.levelUpPanel!=null && !game.levelUpPanel.activeSelf,"Upgrade overlay starts closed");
        foreach(PlanetGravity planet in Object.FindObjectsByType<PlanetGravity>())
            if(planet.name.StartsWith("Planet_")) Check(planet.GetComponent<Renderer>().sharedMaterial.shader.name=="NeonWorlds/OrbitalSurface","Planet surface has the new shader");
        Check(Object.FindAnyObjectByType<OrbitalStars>()!=null,"Single-mesh starfield exists");
        Material shipMaterial=game.player.GetComponent<PlayerShip>().visual.GetComponent<Renderer>().sharedMaterial;
        Check(shipMaterial.shader.name=="NeonWorlds/EmissiveGeometry" && shipMaterial.shader.isSupported,"Ship uses the visible emissive material");
        UnityEngine.Random.State random=UnityEngine.Random.state;
        float time=Time.timeScale;
        var upgrades=(GameManager.UpgradeType[])typeof(GameManager).GetField("currentUpgrades",Private).GetValue(game);
        var saved=(GameManager.UpgradeType[])upgrades.Clone();
        int hp=game.hp;
        bool hadCores=PlayerPrefs.HasKey(MetaProgression.KEY_CORES);
        int cores=MetaProgression.GetStarCores();
        try
        {
            game.TakeDamage(17);
            typeof(ModernHUD).GetMethod("Update",Private).Invoke(hud,null);
            Check(hud.transform.Find("Telemetry/HealthValue").GetComponent<Text>().text.StartsWith(Mathf.Max(0,game.hp).ToString()),"Health label responds to damage");
            game.hp=hp;
            typeof(ModernHUD).GetMethod("Update",Private).Invoke(hud,null);
            Capture("visual-hud.png",1280,720);
            game.ShowLevelUpScreen();
            Check(Time.timeScale==0f,"Upgrade screen pauses combat");
            Check(upgrades[0]!=upgrades[1] && upgrades[1]!=upgrades[2] && upgrades[0]!=upgrades[2],"Three different upgrades are offered");
            for(int i=0;i<3;i++)
            {
                Check(game.upgradeButtons[i].GetComponentInChildren<UpgradeGlyph>()!=null,"Upgrade has its vector icon");
                Check(game.upgradeButtons[i].GetComponentInChildren<UpgradeGlyph>().GetComponent<CanvasRenderer>()!=null,"Vector icon has a CanvasRenderer");
                Check(!string.IsNullOrEmpty(game.upgradeTitles[i].text) && !string.IsNullOrEmpty(game.upgradeDescs[i].text),"Upgrade content is bound");
            }
            typeof(ModernHUD).GetMethod("Update",Private).Invoke(hud,null);
            Capture("visual-upgrades.png",1280,720);
            Capture("visual-upgrades-wide.png",1920,810);
            Capture("visual-upgrades-small.png",800,600);
            Canvas.ForceUpdateCanvases();
            Check(EventSystem.current.currentSelectedGameObject==game.upgradeButtons[0].gameObject,"Keyboard/gamepad selection starts on a card");
            foreach(GameManager.UpgradeType kind in Enum.GetValues(typeof(GameManager.UpgradeType)))
            {
                RuntimeUIBuilder.StyleUpgrade(game,0,kind);
                Check(game.upgradeButtons[0].GetComponentInChildren<UpgradeGlyph>().kind==kind,"All eight upgrade types have bound glyphs");
            }
            game.levelUpPanel.SetActive(false);
            RuntimeUIBuilder.BuildBossHealthBarDirect(null);
            RuntimeUIBuilder.UpdateBossHP(250,1000);
            Check(Mathf.Approximately(hud.transform.Find("BossHealthBar/Track/Fill").GetComponent<RectTransform>().anchorMax.x,.25f),"Boss bar reflects current HP");
            RuntimeUIBuilder.UpdateBossHP(0,0);
            Check(hud.transform.Find("BossHealthBar/Track/Fill").GetComponent<RectTransform>().anchorMax.x==0,"Boss bar handles zero maximum HP");
            RuntimeUIBuilder.HideBossHealthBar();
            for(int ending=0;ending<2;ending++)
            {
                if(ending==0) RuntimeUIBuilder.BuildGameOverUI(game); else RuntimeUIBuilder.BuildVictoryUI(game);
                typeof(ModernHUD).GetMethod("Update",Private).Invoke(hud,null);
                Check(RuntimeUIBuilder.HasEndScreen && Time.timeScale==0,"End screen pauses gameplay");
                Check(hud.transform.Find("Telemetry").GetComponent<CanvasGroup>().alpha==0,"End screen hides telemetry");
                Transform panel=hud.transform.Find(ending==0 ? "GameOverPanel" : "VictoryPanel");
                Check(panel.GetComponentsInChildren<Button>().Length==3,"End screen has restart, hangar and quit");
                Check(EventSystem.current.currentSelectedGameObject==panel.Find("Restart").gameObject,"Restart receives initial focus");
                Capture(ending==0 ? "visual-gameover.png" : "visual-victory.png",1280,720);
                Object.DestroyImmediate(panel.gameObject);
            }
        }
        finally
        {
            game.hp=hp;
            if(hadCores) PlayerPrefs.SetInt(MetaProgression.KEY_CORES,cores); else PlayerPrefs.DeleteKey(MetaProgression.KEY_CORES);
            PlayerPrefs.Save();
            Array.Copy(saved,upgrades,3);
            UnityEngine.Random.state=random;
            game.levelUpPanel.SetActive(false);
            Time.timeScale=time;
            GameAudio.SetGameplayPaused(time<=0f);
            GameAudio.Instance.StopAll();
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
            typeof(ModernHUD).GetMethod("Update",Private).Invoke(hud,null);
        }
        Debug.Log("NEON_VISUAL: PASS; "+assertions+" assertions; HUD and upgrades captured at three aspect ratios.");
        return assertions;
    }

    static void Capture(string file,int width,int height)
    {
        Camera camera=Camera.main;
        var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
        var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
        RenderTexture previous=RenderTexture.active,oldTarget=camera.targetTexture;
        float aspect=camera.aspect;
        Canvas[] canvases=Object.FindObjectsByType<Canvas>();
        var modes=new RenderMode[canvases.Length]; var cameras=new Camera[canvases.Length]; var distances=new float[canvases.Length];
        for(int i=0;i<canvases.Length;i++) { modes[i]=canvases[i].renderMode; cameras[i]=canvases[i].worldCamera; distances[i]=canvases[i].planeDistance; }
        try
        {
            camera.targetTexture=target; camera.aspect=(float)width/height;
            for(int i=0;i<canvases.Length;i++)
                if(modes[i]==RenderMode.ScreenSpaceOverlay) { canvases[i].renderMode=RenderMode.ScreenSpaceCamera; canvases[i].worldCamera=camera; canvases[i].planeDistance=camera.nearClipPlane+1f; }
            foreach(FloatingText text in Object.FindObjectsByType<FloatingText>()) typeof(FloatingText).GetMethod("LateUpdate",Private).Invoke(text,null);
            Canvas.ForceUpdateCanvases();
            // Force the scaler after changing the target resolution, before checking bounds.
            foreach(CanvasScaler scaler in Object.FindObjectsByType<CanvasScaler>())
                typeof(CanvasScaler).GetMethod("Handle",Private).Invoke(scaler,null);
            Canvas.ForceUpdateCanvases();
            if(GameManager.Instance.levelUpPanel.activeSelf)
                foreach(Button button in GameManager.Instance.upgradeButtons)
                {
                    Vector3[] corners=new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                    foreach(Vector3 corner in corners) { Vector3 p=camera.WorldToViewportPoint(corner); Check(p.x>=0 && p.x<=1 && p.y>=0 && p.y<=1,"Cards fit in "+width+"x"+height); }
                }
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
            RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,width,height),0,0); pixels.Apply();
            File.WriteAllBytes(Path.GetFullPath("../"+file),pixels.EncodeToPNG());
        }
        finally
        {
            for(int i=0;i<canvases.Length;i++) { canvases[i].renderMode=modes[i]; canvases[i].worldCamera=cameras[i]; canvases[i].planeDistance=distances[i]; }
            camera.targetTexture=oldTarget; camera.aspect=aspect; RenderTexture.active=previous;
            Canvas.ForceUpdateCanvases(); target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
        }
    }
}
