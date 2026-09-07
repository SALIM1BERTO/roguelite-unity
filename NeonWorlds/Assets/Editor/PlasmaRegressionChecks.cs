using System;
using UnityEngine;
public static class PlasmaRegressionChecks
{
    public static int Run()
    {
        int count = 0;
        Action<bool,string> check = (ok, message) => { if (!ok) throw new Exception("PLASMA: " + message); count++; };
        var planet = new GameObject("PlasmaTestPlanet"); planet.transform.position = Vector3.one * 1000;
        planet.transform.localScale = Vector3.one * 10;
        Vector3 origin = planet.transform.position + Vector3.up * 5;
        Func<float,float,Vector3> point = (distance, heading) => BossWorldMotion.SurfacePoint(planet.transform, origin, Quaternion.AngleAxis(heading,Vector3.up)*Vector3.forward,distance,0);
        check(WeaponPlasmaFlamer.Contains(planet.transform,origin,Vector3.forward,point(7,20)), "inside curved cone");
        check(!WeaponPlasmaFlamer.Contains(planet.transform,origin,Vector3.forward,point(7,30)), "outside angle");
        check(!WeaponPlasmaFlamer.Contains(planet.transform,origin,Vector3.forward,point(9,0)), "outside range");
        check(!WeaponPlasmaFlamer.Contains(planet.transform,origin,Vector3.forward,point(15.70796f,0)), "opposite pole");
        var obj = new GameObject("PlasmaTestEnemy"); obj.transform.position = origin;
        var enemy = obj.AddComponent<Enemy>(); enemy.maxHp = enemy.hp = 1000;
        var status = obj.AddComponent<StatusEffectReceiver>();
        status.ApplyBurn(2); status.Advance(.24f); check(enemy.hp==1000,"no early tick");
        status.Advance(.01f); check(enemy.hp==998,"quarter-second tick");
        status.ApplyBurn(2); status.Advance(.25f); check(enemy.hp==996,"refresh does not stack");
        status.Advance(2.75f); check(enemy.hp==974 && !status.IsBurning,"12 ticks after refreshed duration");
        status.ApplyBurn(2); status.Advance(0); check(enemy.hp==974,"pause");
        obj.SetActive(false); obj.SetActive(true); check(!status.IsBurning,"pool reuse clears status");
        var weaponObj = new GameObject("PlasmaTestWeapon"); var weapon = weaponObj.AddComponent<Weapon>();
        weapon.bonusDamage=3; weapon.damageMultiplier=2; weapon.currentWeapon=Weapon.WeaponType.PlasmaFlamer; weapon.SetupWeapon();
        check(weapon.damage==10 && weapon.baseFireRate==10,"persistent upgrades and base tick rate");
        weapon.currentWeapon=Weapon.WeaponType.Blaster; weapon.SetupWeapon(); check(weapon.bonusDamage==3,"switch preserves upgrades");
        var gravity = planet.AddComponent<PlanetGravity>();
        var actorBody = weaponObj.AddComponent<GravityBody>(); actorBody.planet = gravity;
        weaponObj.transform.position = origin;
        var targetBody = obj.AddComponent<GravityBody>(); targetBody.planet = gravity;
        obj.transform.position = point(3, 0); enemy.hp = 1000;
        var flamer = weaponObj.AddComponent<WeaponPlasmaFlamer>();
        flamer.SetFiring(true, Vector3.forward, 2, 1);
        check(enemy.hp == 998 && status.IsBurning, "real flame tick applies direct hit and burn once");
        check(flamer.GetComponentsInChildren<LineRenderer>().Length == 5, "bounded flame geometry");
        check(weaponObj.GetComponent<AudioSource>().clip.samples == 22050, "generated sound exists");
        flamer.StopFiring(); check(!weaponObj.GetComponent<AudioSource>().isPlaying, "release stops audio");
        status.Clear(); targetBody.planet = null;
        flamer.SetFiring(true, Vector3.forward, 2, 1);
        check(enemy.hp == 998 && !status.IsBurning, "other planet excluded");
        flamer.StopFiring();
        UnityEngine.Object.DestroyImmediate(weaponObj); UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(planet);
        Debug.Log("NEON_PLASMA: PASS; " + count + " assertions."); return count;
    }
}
