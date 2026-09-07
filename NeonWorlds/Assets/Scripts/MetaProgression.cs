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

    public enum ShipChassis
    {
        Interceptor = 0,
        Titan = 1,
        Spectre = 2,
        Architect = 3
    }

    public const string KEY_SELECTED_SHIP = "Meta_SelectedShip";
    public const string KEY_SHIP_PREFIX = "Meta_Ship_";

    public static ShipChassis GetSelectedShip()
    {
        return (ShipChassis)PlayerPrefs.GetInt(KEY_SELECTED_SHIP, 0);
    }

    public static void SetSelectedShip(ShipChassis chassis)
    {
        PlayerPrefs.SetInt(KEY_SELECTED_SHIP, (int)chassis);
        PlayerPrefs.Save();
    }

    public static bool IsShipUnlocked(ShipChassis chassis)
    {
        if (chassis == ShipChassis.Interceptor) return true;
        return PlayerPrefs.GetInt(KEY_SHIP_PREFIX + chassis.ToString(), 0) == 1;
    }

    public static int GetShipCost(ShipChassis chassis)
    {
        switch (chassis)
        {
            case ShipChassis.Titan: return 35;
            case ShipChassis.Spectre: return 50;
            case ShipChassis.Architect: return 75;
            default: return 0;
        }
    }

    public static bool TryUnlockShip(ShipChassis chassis)
    {
        if (IsShipUnlocked(chassis)) return true;
        int cost = GetShipCost(chassis);
        int cores = GetStarCores();
        if (cores >= cost)
        {
            PlayerPrefs.SetInt(KEY_CORES, cores - cost);
            PlayerPrefs.SetInt(KEY_SHIP_PREFIX + chassis.ToString(), 1);
            SetSelectedShip(chassis);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    public static void ApplyShipChassis(ShipChassis chassis, GameManager gm, PlayerMovement pm, Weapon w)
    {
        if (gm == null && pm == null && w == null) return;

        switch (chassis)
        {
            case ShipChassis.Interceptor:
                if (pm != null) pm.moveSpeed *= 1.15f;
                if (w != null)
                {
                    w.fireRateMultiplier *= 1.10f;
                    w.currentWeapon = Weapon.WeaponType.Blaster;
                    w.SetupWeapon();
                }
                break;

            case ShipChassis.Titan:
                if (gm != null)
                {
                    gm.maxHp += 60;
                    gm.hp = gm.maxHp;
                    gm.UpdateHPText();
                }
                if (pm != null) pm.moveSpeed *= 0.85f;
                if (w != null)
                {
                    w.damageMultiplier += 0.25f;
                    w.currentWeapon = Weapon.WeaponType.Shotgun;
                    w.SetupWeapon();
                }
                break;

            case ShipChassis.Spectre:
                if (gm != null)
                {
                    gm.maxHp = Mathf.Max(20, gm.maxHp - 25);
                    gm.hp = gm.maxHp;
                    gm.UpdateHPText();
                }
                if (w != null)
                {
                    w.critChance += 0.25f;
                    w.critMultiplier += 0.50f;
                    w.currentWeapon = Weapon.WeaponType.Railgun;
                    w.SetupWeapon();
                }
                break;

            case ShipChassis.Architect:
                if (gm != null)
                {
                    gm.magnetRadius += 3f;
                    if (gm.player != null)
                    {
                        SentinelDrone drone = gm.player.GetComponent<SentinelDrone>();
                        if (drone == null) drone = gm.player.gameObject.AddComponent<SentinelDrone>();
                        drone.SetLevel(1);
                        gm.upgrades[GameManager.UpgradeType.SentinelDrone] = 1;
                    }
                }
                if (w != null)
                {
                    w.currentWeapon = Weapon.WeaponType.Blaster;
                    w.SetupWeapon();
                }
                break;
        }

        // Apply chassis visual on PlayerShip
        if (gm != null && gm.player != null)
        {
            PlayerShip ship = gm.player.GetComponent<PlayerShip>();
            if (ship != null)
            {
                ship.ApplyChassisVisual(chassis);
            }
        }
        else if (pm != null)
        {
            PlayerShip ship = pm.GetComponent<PlayerShip>();
            if (ship != null)
            {
                ship.ApplyChassisVisual(chassis);
            }
        }
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

        // Apply selected ship chassis bonuses and visuals
        ApplyShipChassis(GetSelectedShip(), gm, pm, w);
    }
}
