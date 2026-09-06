using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public enum WeaponType { Blaster, Shotgun, Railgun }
    [Header("Tipo de Arma")]
    public WeaponType currentWeapon = WeaponType.Blaster;

    public void SetupWeapon()
    {
        switch(currentWeapon)
        {
            case WeaponType.Blaster:
                fireRate = 2.5f; damage = 10; spreadCount = 1; pierceCount = 0; bounceCount = 0; explosive = false;
                break;
            case WeaponType.Shotgun:
                fireRate = 1.0f; damage = 6; spreadCount = 5; pierceCount = 0; bounceCount = 0; explosive = false;
                break;
            case WeaponType.Railgun:
                fireRate = 0.5f; damage = 40; spreadCount = 1; pierceCount = 100; bounceCount = 0; explosive = false;
                break;
        }
    }
    public GameObject bulletPrefab;
    private ObjectPool<GameObject> bulletPool;
    public float fireRate = 2.0f;
    public float fireRateMultiplier = 1f;
    public int damage = 5;
    
    public int spreadCount = 1;
    public int pierceCount = 0;
    public int bounceCount = 0;
    public bool explosive = false;

    private float fireTimer = 0f;

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
        if (Keyboard.current.digit1Key.wasPressedThisFrame) { currentWeapon = WeaponType.Blaster; SetupWeapon(); }
        if (Keyboard.current.digit2Key.wasPressedThisFrame) { currentWeapon = WeaponType.Shotgun; SetupWeapon(); }
        if (Keyboard.current.digit3Key.wasPressedThisFrame) { currentWeapon = WeaponType.Railgun; SetupWeapon(); }
        if (Time.timeScale == 0) return; // Pausado
        fireTimer -= Time.deltaTime;

        Vector2 aimInput = Vector2.zero;
        bool isAiming = false;

        // 1. Tenta ler o Gamepad (Anal�gico Direito)
        if (Gamepad.current != null)
        {
            aimInput = Gamepad.current.rightStick.ReadValue();
            if (aimInput.sqrMagnitude > 0.1f) isAiming = true;
        }

        // 2. Se n�o usou Gamepad, tenta ler o Mouse (Bot�o Esquerdo Pressionado)
        if (!isAiming && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (Camera.main != null)
            {
                Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(transform.position);
                aimInput = (mousePos - playerScreenPos).normalized;
                isAiming = true;
            }
        }

        // 3. Se estiver mirando e a arma estiver carregada, atira!
        if (isAiming && fireTimer <= 0f)
        {
            Shoot(aimInput);
            fireTimer = 1f / (fireRate * fireRateMultiplier);
        }
    }

    void Shoot(Vector2 aimInput)
    {
        if (Camera.main == null) return;
        if (bulletPool == null) InitPool();

        Vector3 camUp = Vector3.ProjectOnPlane(Camera.main.transform.up, transform.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(Camera.main.transform.right, transform.up).normalized;
        
        Vector3 baseShootDir = (camRight * aimInput.x + camUp * aimInput.y).normalized;

        if (baseShootDir.sqrMagnitude < 0.01f) return;

        float spreadAngle = 15f; // graus entre cada tiro
        float startAngle = -spreadAngle * (spreadCount - 1) / 2f;

        for (int i = 0; i < spreadCount; i++)
        {
            float angle = startAngle + i * spreadAngle;
            Vector3 shootDir = Quaternion.AngleAxis(angle, transform.up) * baseShootDir;

            GameObject bulletObj = bulletPool.Get();
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            
            if (bulletScript != null) {
                bulletScript.damage = this.damage;
                bulletScript.pierceCount = this.pierceCount;
                bulletScript.bounceCount = this.bounceCount;
                bulletScript.explosive = this.explosive;

                GravityBody gb = GetComponent<GravityBody>(); 
                if (gb != null && gb.planet != null) {
                    bulletScript.planet = gb.planet.transform; 
                    bulletObj.transform.SetParent(gb.planet.transform, true);
                }
            }

            bulletObj.transform.position = transform.position + transform.up * 0.3f;
            bulletObj.transform.rotation = Quaternion.LookRotation(shootDir, transform.up);
            
            if (bulletScript != null) bulletScript.speed = 25f;
        }
        // One sound for the whole volley, including spread upgrades.
        if (spreadCount > 0) GameAudio.Play(AudioCue.Shot);
    }
}





























