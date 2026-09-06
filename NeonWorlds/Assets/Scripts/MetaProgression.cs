using UnityEngine;

public static class MetaProgression
{
    public const string KEY_CORES = "Meta_StarCores";
    public const string UP_HULL = "Meta_Hull";         // +15 HP base
    public const string UP_SPEED = "Meta_Speed";       // +1f move speed
    public const string UP_DAMAGE = "Meta_Damage";     // +10% base damage
    public const string UP_MAGNET = "Meta_Magnet";     // +2f magnet radius
    public const string UP_REROLL = "Meta_Reroll";     // +1 free reroll per game

    public static int GetStarCores()
    {
        return PlayerPrefs.GetInt(KEY_CORES, 0);
    }

    public static void AddStarCores(int amount)
    {
        if (amount <= 0) return;
        int current = GetStarCores();
        PlayerPrefs.SetInt(KEY_CORES, current + amount);
        PlayerPrefs.Save();
    }

    public static int GetUpgradeLevel(string key)
    {
        return PlayerPrefs.GetInt(key, 0);
    }

    public static int GetUpgradeCost(string key, int baseCost)
    {
        int lvl = GetUpgradeLevel(key);
        return baseCost * (lvl + 1);
    }

    public static bool TryPurchaseUpgrade(string key, int maxLevel, int baseCost)
    {
        int lvl = GetUpgradeLevel(key);
        if (lvl >= maxLevel) return false;

        int cost = GetUpgradeCost(key, baseCost);
        int cores = GetStarCores();
        if (cores >= cost)
        {
            PlayerPrefs.SetInt(KEY_CORES, cores - cost);
            PlayerPrefs.SetInt(key, lvl + 1);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    public static void ApplyPermanentBuffs(GameManager gm, PlayerMovement pm, Weapon w)
    {
        if (gm != null)
        {
            int hullLvl = GetUpgradeLevel(UP_HULL);
            gm.maxHp += hullLvl * 15;
            gm.hp = gm.maxHp;
            gm.UpdateHPText();

            int magnetLvl = GetUpgradeLevel(UP_MAGNET);
            gm.magnetRadius += magnetLvl * 2f;

            int rerollLvl = GetUpgradeLevel(UP_REROLL);
            gm.availableRerolls = rerollLvl;
        }

        if (pm != null)
        {
            int speedLvl = GetUpgradeLevel(UP_SPEED);
            pm.moveSpeed += speedLvl * 0.8f;
        }

        if (w != null)
        {
            int dmgLvl = GetUpgradeLevel(UP_DAMAGE);
            w.damageMultiplier += dmgLvl * 0.1f;
        }
    }
}
