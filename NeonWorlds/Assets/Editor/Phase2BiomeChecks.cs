using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Phase2BiomeChecks
{
    [InitializeOnLoadMethod]
    [MenuItem("NeonWorlds/Run Phase 2 Biome Checks")]
    public static void Run()
    {
        Debug.Log("[Phase2BiomeChecks] Running validation for Biomes and Care Packages...");

        GameObject root = new GameObject("Phase2BiomeChecks_Root");

        try
        {
            // 1. Test Biome Configuration for 6 Planets
            for (int i = 1; i <= 6; i++)
            {
                GameObject planetObj = new GameObject("Planet_" + i);
                planetObj.transform.SetParent(root.transform, false);
                PlanetGravity pg = planetObj.AddComponent<PlanetGravity>();
                PlanetaryBiome biome = planetObj.AddComponent<PlanetaryBiome>();
                biome.ConfigureDefaultBiome();

                Assert(!string.IsNullOrEmpty(biome.planetName), $"Planet_{i} must have a name");
                Assert(!string.IsNullOrEmpty(biome.hazardDescription), $"Planet_{i} must have a hazard description");
                Assert(biome.xpMultiplier >= 1.0f, $"Planet_{i} must have >= 1.0 XP multiplier");

                if (i == 1) Assert(biome.biomeType == BiomeType.Sanctuary, "Planet 1 must be Sanctuary");
                if (i == 2) Assert(biome.biomeType == BiomeType.InfernoPyre, "Planet 2 must be InfernoPyre");
                if (i == 3) Assert(biome.biomeType == BiomeType.VoidAbyss, "Planet 3 must be VoidAbyss");
                if (i == 4) Assert(biome.biomeType == BiomeType.ToxicJungle, "Planet 4 must be ToxicJungle");
                if (i == 5) Assert(biome.biomeType == BiomeType.TitanColossus, "Planet 5 must be TitanColossus");
                if (i == 6) Assert(biome.biomeType == BiomeType.ElectroNexus, "Planet 6 must be ElectroNexus");
            }

            // 2. Test OnPlayerArrived
            GameObject p2 = root.transform.Find("Planet_2").gameObject;
            PlanetaryBiome.OnPlayerArrived(p2.GetComponent<PlanetGravity>());
            Assert(PlanetaryBiome.CurrentBiome != null, "CurrentBiome must be set on arrival");
            Assert(PlanetaryBiome.CurrentBiome.biomeType == BiomeType.InfernoPyre, "CurrentBiome should be InfernoPyre on Planet 2");

            // 3. Test CarePackageDrop creation
            GameObject carePackageObj = new GameObject("TestCarePackage");
            carePackageObj.transform.SetParent(root.transform, false);
            CarePackageDrop carePackage = carePackageObj.AddComponent<CarePackageDrop>();
            carePackage.Setup(p2.transform, Vector3.up, 1f);
            Assert(carePackage != null, "CarePackageDrop must setup correctly");

            // 4. Test Frenzy System
            GameObject dummyGM = new GameObject("DummyGM");
            dummyGM.transform.SetParent(root.transform, false);
            GameManager gm = dummyGM.AddComponent<GameManager>();
            GameObject dummyPlayer = new GameObject("DummyPlayer");
            dummyPlayer.transform.SetParent(root.transform, false);
            Weapon w = dummyPlayer.AddComponent<Weapon>();
            w.fireRateMultiplier = 1.0f;
            gm.player = dummyPlayer.transform;

            gm.TriggerFrenzy(5f);
            Assert(gm.isFrenzyActive, "Frenzy should be active");
            Assert(gm.frenzyTimer > 0f, "Frenzy timer should be > 0");
            Assert(w.fireRateMultiplier >= 2.0f, "Fire rate multiplier must double during Frenzy");

            Debug.Log("<color=green>[Phase2BiomeChecks] ALL BIOME & CARE PACKAGE CHECKS PASSED!</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError("[Phase2BiomeChecks] FAILED: " + ex);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
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
