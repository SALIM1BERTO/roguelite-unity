using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public enum WeaponType { Blaster, Shotgun, Railgun, PlasmaFlamer }
    [Header("Tipo de Arma")]
    public WeaponType currentWeapon = WeaponType.Blaster;

    // Base stats per weapon archetype
    public float baseFireRate = 2.5f;
    public int baseDamage = 10;
    public int baseSpreadCount = 1;
    public int basePierceCount = 0;
    public int baseBounceCount = 0;
    public bool baseExplosive = false;

    // Persistent player upgrades (never reset on weapon change)
    [Header("Upgrades Persistentes do Jogador")]
    public float fireRateMultiplier = 1f;
    public float damageMultiplier = 1f;
    public int bonusDamage = 0;
    public int bonusSpread = 0;
    public int bonusPierce = 0;
    public int bonusBounce = 0;
    public bool isExplosive = false;
    public float critChance = 0.05f; // 5% base
    public float critMultiplier = 2.5f;

    [Header("Evoluções Lendárias")]
    public bool isSupernova = false;
    public bool isNebulaFlak = false;
    public bool isAntimatterLance = false;

    // Compatibility properties
    public float fireRate { get => baseFireRate; set => baseFireRate = value; }
    public int damage { get => Mathf.RoundToInt((baseDamage + bonusDamage) * damageMultiplier); set => baseDamage = value; }
    public int spreadCount { get => baseSpreadCount + bonusSpread; set => bonusSpread = Mathf.Max(0, value - baseSpreadCount); }
    public int pierceCount { get => basePierceCount + bonusPierce; set => bonusPierce = Mathf.Max(0, value - basePierceCount); }
    public int bounceCount { get => baseBounceCount + bonusBounce; set => bonusBounce = Mathf.Max(0, value - baseBounceCount); }
    public bool explosive { get => baseExplosive || isExplosive; set => isExplosive = value; }

    public void SetupWeapon()
    {
        switch(currentWeapon)
        {
            case WeaponType.Blaster:
                baseFireRate = isSupernova ? 5.5f : 2.5f;
                baseDamage = isSupernova ? 18 : 10;
                baseSpreadCount = isSupernova ? 2 : 1;
                basePierceCount = 0; baseBounceCount = 0; baseExplosive = false;
                break;
            case WeaponType.Shotgun:
                int shotgunLevel = GameManager.Instance != null ? GameManager.Instance.GetUpgradeLevel(GameManager.UpgradeType.ShotgunMastery) : 0;
                int scatter = GameManager.Instance != null ? GameManager.Instance.GetUpgradeLevel(GameManager.UpgradeType.ScatterMatrix) : 0;
                baseFireRate = isNebulaFlak ? 1.4f : 1.0f;
                // Round upward so every mastery level visibly increases this low base value.
                baseDamage = Mathf.CeilToInt((isNebulaFlak ? 12 : 8) * (1f + shotgunLevel * .15f));
                baseSpreadCount = (isNebulaFlak ? 8 : 5) + scatter;
                basePierceCount = 0; baseBounceCount = 0; baseExplosive = false;
                break;
            case WeaponType.SeekerSwarm:
                int seekerLevel = GameManager.Instance != null ? GameManager.Instance.GetUpgradeLevel(GameManager.UpgradeType.SeekerSwarmMastery) : 0;
                baseFireRate = .7f; baseDamage = Mathf.RoundToInt(14f * (1f + seekerLevel * .15f)); baseSpreadCount = isHunterKiller ? 8 : 4;
                basePierceCount = 0; baseBounceCount = 0; baseExplosive = false;
                break;
            case WeaponType.Singularity:
                int singularityLevel = GameManager.Instance != null ? GameManager.Instance.GetUpgradeLevel(GameManager.UpgradeType.SingularityMastery) : 0;
                baseFireRate = .5f; baseDamage = Mathf.RoundToInt(5f * (1f + singularityLevel * .15f)); baseSpreadCount = 1;
                basePierceCount = 0; baseBounceCount = 0; baseExplosive = false;
                break;
            case WeaponType.ArcDischarger:
                int arcLevel = GameManager.Instance != null ? GameManager.Instance.GetUpgradeLevel(GameManager.UpgradeType.ArcDischargerMastery) : 0;
                baseFireRate = 1.25f; baseDamage = Mathf.RoundToInt(8f * (1f + arcLevel * .15f)); baseSpreadCount = 1;
                basePierceCount = 0; baseBounceCount = 0; baseExplosive = false;
                break;
            case WeaponType.PlasmaFlamer:
                baseFireRate = 10f; baseDamage = 2; baseSpreadCount = 1;
                basePierceCount = 0; baseBounceCount = 0; baseExplosive = false;
                break;
            case WeaponType.Railgun:
                baseFireRate = isAntimatterLance ? 0.8f : 0.5f;
                baseDamage = isAntimatterLance ? 70 : 40;
                baseSpreadCount = 1;
                basePierceCount = 999;
                baseBounceCount = 0; baseExplosive = false;
                break;
        }
    }

    public GameObject bulletPrefab;
    private ObjectPool<GameObject> bulletPool;
    private float fireTimer = 0f;
    private WeaponPlasmaFlamer plasma;

    void Start()
    {
        SetupWeapon();
        InitPool();
    }

    void InitPool()
    {
        if (bulletPool != null) return;
        bulletPool = new ObjectPool<GameObject>(
            createFunc: () => {
                GameObject obj = Instantiate(bulletPrefab);
                obj.GetComponent<Bullet>().pool = bulletPool;
                return obj;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: 50,
            maxSize: 200
        );
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) { currentWeapon = WeaponType.Blaster; SetupWeapon(); }
            if (Keyboard.current.digit2Key.wasPressedThisFrame) { currentWeapon = WeaponType.Shotgun; SetupWeapon(); }
            if (Keyboard.current.digit3Key.wasPressedThisFrame) { currentWeapon = WeaponType.Railgun; SetupWeapon(); }
        }

        if (Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame) { currentWeapon = WeaponType.PlasmaFlamer; SetupWeapon(); }
        if (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame)
        { currentWeapon = (WeaponType)(((int)currentWeapon + 1) % 4); SetupWeapon(); }
        if (Time.timeScale == 0) { if (plasma != null) plasma.StopFiring(); return; }
        fireTimer -= Time.deltaTime;

        Vector2 aimInput = Vector2.zero;
        bool isAiming = false;

        if (Gamepad.current != null)
        {
            aimInput = Gamepad.current.rightStick.ReadValue();
            if (aimInput.sqrMagnitude > 0.15f)
            {
                isAiming = true;
            }
            else if (Gamepad.current.rightTrigger.isPressed || Gamepad.current.rightShoulder.isPressed)
            {
                isAiming = true;
                aimInput = Vector2.zero;
            }
        }

        if (!isAiming && Mouse.current != null && Mouse.current.leftButton.isPressed
            && (UnityEngine.EventSystems.EventSystem.current==null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (Camera.main != null)
            {
                Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(transform.position);
                aimInput = (mousePos - playerScreenPos).normalized;
                isAiming = true;
            }
        }

        if (currentWeapon == WeaponType.PlasmaFlamer)
        {
            if (plasma == null) plasma = gameObject.AddComponent<WeaponPlasmaFlamer>();
            Vector3 direction = transform.forward;
            if (Camera.main != null && aimInput.sqrMagnitude > .01f)
                direction = Vector3.ProjectOnPlane(Camera.main.transform.right, transform.up).normalized * aimInput.x
                    + Vector3.ProjectOnPlane(Camera.main.transform.up, transform.up).normalized * aimInput.y;
            plasma.SetFiring(isAiming, direction, damage, fireRateMultiplier);
            return;
        }
        if (plasma != null) plasma.StopFiring();
        if (isAiming && fireTimer <= 0f)
        {
            Shoot(aimInput);
            fireTimer = 1f / (baseFireRate * fireRateMultiplier);
        }
    }

    void OnDisable() { if (plasma != null) plasma.StopFiring(); }

    void Shoot(Vector2 aimInput)
    {
        if (Camera.main == null) return;
        if (bulletPool == null) InitPool();

        Vector3 camUp = Vector3.ProjectOnPlane(Camera.main.transform.up, transform.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(Camera.main.transform.right, transform.up).normalized;
        
        Vector3 baseShootDir = (camRight * aimInput.x + camUp * aimInput.y).normalized;
        if (baseShootDir.sqrMagnitude < 0.01f)
            baseShootDir = transform.forward;

        if (baseShootDir.sqrMagnitude < 0.01f) return;

        int activeSpread = spreadCount;
        float spreadAngle = 15f;
        float startAngle = -spreadAngle * (activeSpread - 1) / 2f;

        bool rollCrit = Random.value < critChance;
        int activeDamage = damage;
        if (rollCrit) activeDamage = Mathf.RoundToInt(activeDamage * critMultiplier);

        for (int i = 0; i < activeSpread; i++)
        {
            float angle = startAngle + i * spreadAngle;
            Vector3 shootDir = Quaternion.AngleAxis(angle, transform.up) * baseShootDir;

            GameObject bulletObj = bulletPool.Get();
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            
            if (bulletScript != null) {
                bulletScript.damage = activeDamage;
                bulletScript.isCritical = rollCrit;
                bulletScript.pierceCount = pierceCount;
                bulletScript.bounceCount = bounceCount;
                bulletScript.explosive = explosive;

                if (currentWeapon == WeaponType.Blaster && isSupernova)
                {
                    bulletScript.isSupernova = true;
                    bulletScript.speed = 36f;
                }
                else if (currentWeapon == WeaponType.Shotgun && isNebulaFlak)
                {
                    bulletScript.isNebulaFlak = true;
                    bulletScript.speed = 30f;
                }
                else if (currentWeapon == WeaponType.Railgun && isAntimatterLance)
                {
                    bulletScript.isAntimatterLance = true;
                    bulletScript.speed = 55f;
                }
                else
                {
                    bulletScript.speed = 25f;
                }

                GravityBody gb = GetComponent<GravityBody>(); 
                if (gb != null && gb.planet != null) {
                    bulletScript.planet = gb.planet.transform; 
                    bulletObj.transform.SetParent(gb.planet.transform, true);
                }
            }

            bulletObj.transform.position = transform.position + transform.up * 0.3f;
            bulletObj.transform.rotation = Quaternion.LookRotation(shootDir, transform.up);
        }

        if (activeSpread > 0)
        {
            if (currentWeapon == WeaponType.Blaster && isSupernova)
            {
                GameAudio.Play(AudioCue.SupernovaShot);
            }
            else if (currentWeapon == WeaponType.Shotgun && isNebulaFlak)
            {
                GameAudio.Play(AudioCue.NebulaShot);
            }
            else if (currentWeapon == WeaponType.Railgun && isAntimatterLance)
            {
                GameAudio.Play(AudioCue.AntimatterBeam);
            }
            else
            {
                GameAudio.Play(AudioCue.Shot);
            }
        }
    }
}
