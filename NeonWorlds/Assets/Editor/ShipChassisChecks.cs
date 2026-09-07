using System;
using UnityEditor;
using UnityEngine;

public static class ShipChassisChecks
{
    [MenuItem("NeonWorlds/Run Ship Chassis Checks")]
    public static void Run()
    {
        Debug.Log("[ShipChassisChecks] Starting checks...");

        int initialCores = MetaProgression.GetStarCores();
        MetaProgression.ShipChassis initialShip = MetaProgression.GetSelectedShip();

        string titanKey=MetaProgression.KEY_SHIP_PREFIX+"Titan";
        bool hadTitan=PlayerPrefs.HasKey(titanKey);
        int titanState=PlayerPrefs.GetInt(titanKey);
        try
        {
            // 1. Check Costs
            Assert(MetaProgression.GetShipCost(MetaProgression.ShipChassis.Interceptor) == 0, "Interceptor should be 0 cost");
            Assert(MetaProgression.GetShipCost(MetaProgression.ShipChassis.Titan) == 35, "Titan cost should be 35");
            Assert(MetaProgression.GetShipCost(MetaProgression.ShipChassis.Spectre) == 50, "Spectre cost should be 50");
            Assert(MetaProgression.GetShipCost(MetaProgression.ShipChassis.Architect) == 75, "Architect cost should be 75");

            // 2. Check Interceptor unlocked by default
            Assert(MetaProgression.IsShipUnlocked(MetaProgression.ShipChassis.Interceptor), "Interceptor must be unlocked by default");

            // 3. Check Selection
            MetaProgression.SetSelectedShip(MetaProgression.ShipChassis.Interceptor);
            Assert(MetaProgression.GetSelectedShip() == MetaProgression.ShipChassis.Interceptor, "Failed to select Interceptor");

            // 4. Test Unlock with Cores
            PlayerPrefs.DeleteKey(titanKey);
            MetaProgression.AddStarCores(100);
            bool unlockedTitan = MetaProgression.TryUnlockShip(MetaProgression.ShipChassis.Titan);
            Assert(unlockedTitan, "Failed to unlock Titan with sufficient cores");
            Assert(MetaProgression.IsShipUnlocked(MetaProgression.ShipChassis.Titan), "Titan should report as unlocked");
            Assert(MetaProgression.GetSelectedShip() == MetaProgression.ShipChassis.Titan, "Titan should be auto-selected upon unlock");

            // 5. Test Ship Stat Application and Visuals
            GameObject dummyPlayer = new GameObject("DummyPlayer");
            PlayerMovement pm = dummyPlayer.AddComponent<PlayerMovement>();
            pm.moveSpeed = 10f;
            Weapon w = dummyPlayer.AddComponent<Weapon>();
            w.damage = 20;
            w.fireRateMultiplier = 1f;
            w.critChance = 0.05f;
            w.critMultiplier = 1.5f;
            PlayerShip ps = dummyPlayer.AddComponent<PlayerShip>();
            ps.visualScale = 0.8f;

            // Apply Titan
            MetaProgression.ApplyShipChassis(MetaProgression.ShipChassis.Titan, null, pm, w);
            Assert(w.currentWeapon == Weapon.WeaponType.Shotgun, "Titan starting weapon must be Shotgun");
            Assert(w.damageMultiplier > 1f, "Titan should increase damage multiplier");

            // Apply Spectre
            MetaProgression.ApplyShipChassis(MetaProgression.ShipChassis.Spectre, null, pm, w);
            Assert(w.currentWeapon == Weapon.WeaponType.Railgun, "Spectre starting weapon must be Railgun");
            Assert(w.critChance >= 0.25f, "Spectre should add crit chance");

            // Apply Architect
            MetaProgression.ApplyShipChassis(MetaProgression.ShipChassis.Architect, null, pm, w);
            Assert(w.currentWeapon == Weapon.WeaponType.Blaster, "Architect starting weapon must be Blaster");

            // Check Visual Addon
            Transform visual = dummyPlayer.transform.Find("ShipVisual");
            if (visual != null)
            {
                Transform addon = visual.Find("ChassisAddon");
                Assert(addon != null, "Chassis addon should exist on ship visual");
            }

            // 6. Test UI Build
            GameObject canvasObj = new GameObject("TestCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            RuntimeUIBuilder.BuildHangarUI(canvas.transform);
            Transform hangarPanel = canvas.transform.Find("HangarPanel");
            Assert(hangarPanel != null, "HangarPanel must be created");

            // Cleanup test objects
            UnityEngine.Object.DestroyImmediate(canvasObj);
            UnityEngine.Object.DestroyImmediate(dummyPlayer);

            Debug.Log("<color=green>[ShipChassisChecks] ALL SHIP CHASSIS CHECKS PASSED!</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError("[ShipChassisChecks] FAILED: " + ex);
        }
        finally
        {
            if(hadTitan) PlayerPrefs.SetInt(titanKey,titanState); else PlayerPrefs.DeleteKey(titanKey);
            // Restore initial state
            PlayerPrefs.SetInt(MetaProgression.KEY_CORES, initialCores);
            MetaProgression.SetSelectedShip(initialShip);
            PlayerPrefs.Save();
        }
    }

    private static void Assert(bool condition, string msg)
    {
        if (!condition)
        {
            throw new Exception("[ASSERTION FAILED] " + msg);
        }
    }
}
