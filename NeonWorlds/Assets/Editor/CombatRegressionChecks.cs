using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

// Called synchronously from an Editor validation runner after entering Play Mode.
// No frame is advanced while the live game's singletons are temporarily isolated.
public static class CombatRegressionChecks
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static int Run()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Combat regression checks require Play Mode.");

        MethodInfo handleHit = RequireMethod("HandleHit", typeof(GameObject));
        MethodInfo triggerEnter = RequireMethod("OnTriggerEnter", typeof(Collider));
        MethodInfo update = RequireMethod("Update");
        FieldInfo timer = typeof(Bullet).GetField("timer", PrivateInstance);
        if (timer == null) throw new MissingFieldException(typeof(Bullet).FullName, "timer");

        var originalObjects = new HashSet<GameObject>();
        foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            originalObjects.Add(obj);

        GameManager originalGameManager = GameManager.Instance;
        EnemySpawner originalSpawner = EnemySpawner.Instance;
        ObjectPool<GameObject> pool = null;
        var testMaterials = new List<Material>();
        int passed = 0;

        try
        {
            GameManager.Instance = null;
            EnemySpawner.Instance = null;

            var root = new GameObject("CombatRegressionChecks_Temporary");
            root.transform.position = new Vector3(100000f, 100000f, 100000f);

            var enemyObject = new GameObject("RegressionEnemy");
            enemyObject.transform.SetParent(root.transform, false);
            Enemy enemy = enemyObject.AddComponent<Enemy>();
            enemy.maxHp = 10000;
            enemy.hp = enemy.maxHp;
            enemy.speed = 0f;

            // Destroy is deferred: the same unpooled projectile can still receive
            // another callback in this frame, but must deal damage only once.
            Bullet unpooled = CreateBullet(root.transform, "UnpooledDuplicate", true);
            unpooled.damage = 7;
            int hpBefore = enemy.hp;
            int textsBefore = CountTexts();
            Invoke(handleHit, unpooled, enemyObject);
            Invoke(handleHit, unpooled, enemyObject);
            Check(enemy.hp == hpBefore - 7, "An unpooled projectile damaged twice before deferred destruction.", ref passed);
            Check(CountTexts() == textsBefore + 1, "An unpooled impact produced duplicate damage text.", ref passed);

            // Separate projectiles are legitimate impacts even in the same frame.
            Bullet first = CreateBullet(root.transform, "SameFrameFirst", true);
            Bullet second = CreateBullet(root.transform, "SameFrameSecond", true);
            first.damage = 3;
            second.damage = 11;
            hpBefore = enemy.hp;
            textsBefore = CountTexts();
            int frameBefore = Time.frameCount;
            Invoke(handleHit, first, enemyObject);
            Invoke(handleHit, second, enemyObject);
            Check(Time.frameCount == frameBefore, "The same-frame test unexpectedly advanced a frame.", ref passed);
            Check(enemy.hp == hpBefore - 14, "Distinct projectiles in one frame did not both deal damage.", ref passed);
            Check(CountTexts() == textsBefore + 2, "Distinct impacts did not each produce one damage text.", ref passed);

            int releaseCount = 0;
            pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    Bullet created = CreateBullet(root.transform, "PooledRegressionBullet", false);
                    created.pool = pool;
                    return created.gameObject;
                },
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj =>
                {
                    releaseCount++;
                    obj.SetActive(false);
                },
                actionOnDestroy: obj =>
                {
                    if (obj != null) Object.DestroyImmediate(obj);
                },
                collectionCheck: true,
                defaultCapacity: 1,
                maxSize: 2);

            GameObject pooledObject = pool.Get();
            Bullet pooled = pooledObject.GetComponent<Bullet>();
            pooled.damage = 9;
            hpBefore = enemy.hp;
            textsBefore = CountTexts();
            Invoke(handleHit, pooled, enemyObject);
            Invoke(handleHit, pooled, enemyObject);
            Check(releaseCount == 1 && pool.CountInactive == 1, "Duplicate pooled callbacks released the projectile more than once.", ref passed);
            Check(!pooledObject.activeSelf, "A released pooled projectile remained active.", ref passed);
            Check(enemy.hp == hpBefore - 9, "Duplicate pooled callbacks dealt repeated damage.", ref passed);
            Check(CountTexts() == textsBefore + 1, "Duplicate pooled callbacks created repeated damage text.", ref passed);

            GameObject reusedObject = pool.Get();
            Check(reusedObject == pooledObject, "The reuse check did not obtain the same projectile instance.", ref passed);
            pooled = reusedObject.GetComponent<Bullet>();
            pooled.damage = 4;
            hpBefore = enemy.hp;
            textsBefore = CountTexts();
            Invoke(handleHit, pooled, enemyObject);
            Check(enemy.hp == hpBefore - 4, "A reused projectile retained its previous consumed state.", ref passed);
            Check(releaseCount == 2 && pool.CountInactive == 1, "A reused projectile did not release exactly once in its new cycle.", ref passed);
            Check(CountTexts() == textsBefore + 1, "A reused projectile did not produce exactly one damage text.", ref passed);

            pooled = pool.Get().GetComponent<Bullet>();
            pooled.damage = 17;
            pooled.speed = 0f;
            hpBefore = enemy.hp;
            textsBefore = CountTexts();
            timer.SetValue(pooled, -1f);
            Invoke(update, pooled);
            Invoke(handleHit, pooled, enemyObject);
            Invoke(update, pooled);
            Check(releaseCount == 3 && pool.CountInactive == 1, "Expiration followed by a callback released the projectile more than once.", ref passed);
            Check(enemy.hp == hpBefore, "An expired projectile still dealt damage.", ref passed);
            Check(CountTexts() == textsBefore, "An expired projectile created damage text.", ref passed);

            var childObject = new GameObject("EnemyChildCollider");
            childObject.transform.SetParent(enemyObject.transform, false);
            BoxCollider childCollider = childObject.AddComponent<BoxCollider>();
            childCollider.isTrigger = true;
            pooled = pool.Get().GetComponent<Bullet>();
            pooled.damage = 13;
            hpBefore = enemy.hp;
            textsBefore = CountTexts();
            Invoke(triggerEnter, pooled, childCollider);
            Invoke(handleHit, pooled, enemyObject);
            Check(enemy.hp == hpBefore - 13, "A child collider failed to resolve its parent Enemy exactly once.", ref passed);
            Check(CountTexts() == textsBefore + 1, "A child collider impact produced an incorrect number of damage texts.", ref passed);
            Check(releaseCount == 4 && pool.CountInactive == 1, "Child and parent callbacks did not share one projectile release.", ref passed);
            Check(enemy.hp > 0, "The regression target unexpectedly died.", ref passed);

            // A damaged pooled enemy must restore its material and health bar
            // immediately when disabled and enabled, without waiting for Start.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            Check(shader != null, "The regression scene is missing the URP Unlit shader.", ref passed);
            Material originalMaterial = new Material(shader);
            originalMaterial.SetColor("_BaseColor", Color.cyan);
            testMaterials.Add(originalMaterial);
            GameObject renderedObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            renderedObject.name = "RenderedRegressionEnemy";
            renderedObject.transform.SetParent(root.transform, false);
            MeshRenderer renderedMesh = renderedObject.GetComponent<MeshRenderer>();
            renderedMesh.sharedMaterial = originalMaterial;
            Enemy renderedEnemy = renderedObject.AddComponent<Enemy>();
            renderedEnemy.maxHp = 80;
            renderedObject.SetActive(false);
            renderedObject.SetActive(true);
            FieldInfo flashMaterialField = typeof(Enemy).GetField("flashMat", PrivateInstance);
            if (flashMaterialField != null)
            {
                Material flashMaterial = flashMaterialField.GetValue(renderedEnemy) as Material;
                if (flashMaterial != null) testMaterials.Add(flashMaterial);
            }
            Invoke(RequireMethod(typeof(Enemy), "Start"), renderedEnemy);
            Transform healthBg = renderedObject.transform.Find("HealthBg");
            Transform healthFill = renderedObject.transform.Find("HealthBg/HealthFill");
            Check(healthBg != null && healthFill != null, "Enemy.Start did not create its health bar.", ref passed);
            testMaterials.Add(healthBg.GetComponent<MeshRenderer>().sharedMaterial);
            testMaterials.Add(healthFill.GetComponent<MeshRenderer>().sharedMaterial);
            renderedEnemy.TakeDamage(20);
            Check(renderedEnemy.hp == 60 && Mathf.Approximately(healthFill.localScale.x, 0.75f), "Damage did not update the enemy health bar.", ref passed);
            Check(renderedMesh.sharedMaterial != originalMaterial, "The enemy hit flash did not activate.", ref passed);
            renderedObject.SetActive(false);
            Check(renderedMesh.sharedMaterial == originalMaterial, "Disabling an enemy left its flash material applied.", ref passed);
            renderedObject.SetActive(true);
            Check(renderedEnemy.hp == 80, "A reused enemy did not restore maximum HP.", ref passed);
            Check(Mathf.Approximately(healthFill.localScale.x, 1f), "A reused enemy retained a partially empty health bar.", ref passed);
            Check(renderedMesh.sharedMaterial == originalMaterial, "A reused enemy did not retain its original material.", ref passed);

            // On the southern hemisphere, world-up points into the planet.
            // Both spawn offset and subsequent text movement must use radial up.
            var planetObject = new GameObject("TextRegressionPlanet");
            planetObject.transform.SetParent(root.transform, false);
            // Keep Y close to zero so tiny frame deltas remain representable,
            // while X/Z keep the test isolated from the live solar system.
            planetObject.transform.position = new Vector3(100000f, 0f, 100000f);
            planetObject.transform.localScale = Vector3.one * 20f;
            PlanetGravity textPlanet = planetObject.AddComponent<PlanetGravity>();
            var southernObject = new GameObject("SouthernRegressionEnemy");
            southernObject.transform.SetParent(root.transform, false);
            southernObject.transform.position = planetObject.transform.position + Vector3.down * 10.3f;
            GravityBody southernGravity = southernObject.AddComponent<GravityBody>();
            southernGravity.planet = textPlanet;
            Enemy southernEnemy = southernObject.AddComponent<Enemy>();
            southernEnemy.maxHp = 10000;
            southernEnemy.hp = southernEnemy.maxHp;

            var textsBeforeSouthernHit = new HashSet<FloatingText>();
            foreach (FloatingText text in Object.FindObjectsByType<FloatingText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                textsBeforeSouthernHit.Add(text);
            southernEnemy.TakeDamage(1);
            FloatingText southernText = null;
            int newTextCount = 0;
            foreach (FloatingText text in Object.FindObjectsByType<FloatingText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!textsBeforeSouthernHit.Contains(text))
                {
                    southernText = text;
                    newTextCount++;
                }
            }
            Check(newTextCount == 1 && southernText != null, "A southern-hemisphere impact did not spawn exactly one text.", ref passed);
            Check(southernText.transform.parent == planetObject.transform, "Damage text did not attach to the struck enemy's planet.", ref passed);
            Vector3 center = planetObject.transform.position;
            Vector3 initialRadial = southernText.transform.position - center;
            Check(initialRadial.magnitude > (southernObject.transform.position - center).magnitude && Vector3.Dot(initialRadial.normalized, Vector3.down) > 0.9999f,
                "Southern damage text spawned inward instead of above the surface.", ref passed);
            Check(Time.deltaTime > 0f && Time.deltaTime < 1f, "The text motion test requires a positive frame delta below its one-second lifetime.", ref passed);
            Invoke(RequireMethod(typeof(FloatingText), "Update"), southernText);
            Vector3 movedRadial = southernText.transform.position - center;
            Check(movedRadial.magnitude > initialRadial.magnitude, "Southern damage text moved toward the planet instead of away from it.", ref passed);
            Check(Vector3.Dot(initialRadial.normalized, movedRadial.normalized) > 0.9999f, "Damage text did not preserve its radial movement direction.", ref passed);
            Camera mainCamera = Camera.main;
            Check(mainCamera != null, "The text billboard test requires the gameplay main camera.", ref passed);
            southernText.transform.rotation = Quaternion.Euler(7f, 19f, 37f);
            Invoke(RequireMethod(typeof(FloatingText), "LateUpdate"), southernText);
            Check(Quaternion.Angle(southernText.transform.rotation, mainCamera.transform.rotation) < 0.001f, "Damage text did not face the main camera during LateUpdate.", ref passed);

            Debug.Log("[NeonWorlds] Combat regression checks passed: " + passed);
            return passed;
        }
        finally
        {
            try
            {
                if (pool != null) pool.Dispose();

                // The checks never yield: all new scene objects here were created
                // by these checks, including impact effects and FloatingText.
                foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (obj != null && !originalObjects.Contains(obj))
                        Object.DestroyImmediate(obj);
                }

                foreach (Material material in testMaterials)
                {
                    if (material != null) Object.DestroyImmediate(material);
                }
            }
            finally
            {
                GameManager.Instance = originalGameManager;
                EnemySpawner.Instance = originalSpawner;
            }
        }
    }

    private static Bullet CreateBullet(Transform parent, string name, bool active)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        if (!active) obj.SetActive(false);
        obj.AddComponent<Rigidbody>();
        Bullet bullet = obj.AddComponent<Bullet>();
        bullet.speed = 0f;
        return bullet;
    }

    private static int CountTexts()
    {
        return Object.FindObjectsByType<FloatingText>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    private static MethodInfo RequireMethod(string name, params Type[] parameterTypes)
    {
        return RequireMethod(typeof(Bullet), name, parameterTypes);
    }

    private static MethodInfo RequireMethod(Type declaringType, string name, params Type[] parameterTypes)
    {
        MethodInfo method = declaringType.GetMethod(name, PrivateInstance, null, parameterTypes, null);
        if (method == null) throw new MissingMethodException(declaringType.FullName, name);
        return method;
    }

    private static void Invoke(MethodInfo method, object target, params object[] arguments)
    {
        try
        {
            method.Invoke(target, arguments);
        }
        catch (TargetInvocationException exception)
        {
            throw new InvalidOperationException("Combat regression callback failed: " + method.Name, exception.InnerException ?? exception);
        }
    }

    private static void Check(bool condition, string message, ref int passed)
    {
        if (!condition) throw new InvalidOperationException(message);
        passed++;
    }
}
