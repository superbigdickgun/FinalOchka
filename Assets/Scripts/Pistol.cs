using UnityEngine;

public class Pistol : MonoBehaviour
{
    [Header("=== ССЫЛКИ ===")]
    public PlayerModular player;
    public PistolInventory ownerInv;
    public SpriteRenderer sprite;
    public Sprite pistolSprite;

    [Header("=== SORTING ===")]
    public int pistolSortingOrder = 14;
    public int laserSortingOrder = 940;

    [Header("=== MUZZLE ===")]
    public Transform muzzle;
    public Vector2 muzzleLocalOffset = new Vector2(0.3f, 0f);

    [Header("=== ЛАЗЕР ===")]
    public LineRenderer laser;
    public float laserRange = 12f;
    public float laserWidth = 0.03f;
    public Color laserColor = new Color(1f, 0.15f, 0.15f, 0.9f);
    public float laserEndPadding = 0.02f;
    public float laserZ = -1f;

    [Header("=== ПАТРОНЫ ===")]
    public int maxAmmo = 12;
    public int currentAmmo = 12;
    public float fireRate = 0.15f;

    [Header("=== ПУЛЯ ===")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 25f;
    public float bulletRange = 15f;
    public float bulletDamage = 10f;
    public Vector2 bulletSize = new Vector2(0.3f, 0.08f);
    public Color bulletColor = new Color(1f, 0.95f, 0.6f, 1f);
    public float bulletZ = -1f;

    [Header("=== ОТДАЧА ===")]
    public float recoilForce = 0.15f;

    [Header("=== ИСКРЫ ===")]
    public int sparkCount = 8;
    public float sparkSpread = 25f;
    public float sparkSpeed = 5f;
    public float sparkLifetime = 0.25f;
    public float sparkSize = 0.08f;
    public Color sparkColor1 = new Color(1f, 0.9f, 0.3f, 1f);
    public Color sparkColor2 = new Color(1f, 0.3f, 0.1f, 1f);

    [Header("=== ЗВУКИ ВЫСТРЕЛА (6 слотов) ===")]
    [Tooltip("6 звуков выстрела. Каждый выстрел — случайный.")]
    public AudioClip[] shotSounds;

    [Range(0f, 1f)] public float shotVolume = 1f;
    public float shotPitchVariation = 0.1f;
    public bool avoidRepeatShot = true;

    private float lastShot;
    private AudioSource audioSource;
    private int lastShotIndex = -1;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerModular>();
        if (ownerInv == null) ownerInv = GetComponentInParent<PistolInventory>();

        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            if (pistolSprite != null) sprite.sprite = pistolSprite;
            sprite.sortingOrder = pistolSortingOrder;
            sprite.enabled = false;
        }

        if (muzzle == null)
        {
            var m = new GameObject("Muzzle");
            m.transform.SetParent(transform, false);
            muzzle = m.transform;
        }
        muzzle.localPosition = new Vector3(muzzleLocalOffset.x, muzzleLocalOffset.y, 0f);

        if (laser == null)
        {
            var lgo = new GameObject("PistolLaser");
            lgo.transform.SetParent(null);
            laser = lgo.AddComponent<LineRenderer>();
        }

        laser.positionCount = 2;
        laser.material = new Material(Shader.Find("Sprites/Default"));
        laser.useWorldSpace = true;
        laser.sortingOrder = laserSortingOrder;
        laser.enabled = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (player == null) player = GetComponentInParent<PlayerModular>();
        if (ownerInv == null) ownerInv = GetComponentInParent<PistolInventory>();

        bool has = (ownerInv != null && ownerInv.hasPistol);

        if (sprite != null) sprite.enabled = has;
        if (laser != null) laser.enabled = has;

        if (!has) return;

        if (Input.GetMouseButtonDown(0) && Time.time - lastShot > fireRate)
        {
            if (currentAmmo > 0) Shoot();
            else Debug.Log("[Pistol] Нет патронов");
        }

        if (Input.GetKeyDown(KeyCode.R)) currentAmmo = maxAmmo;
    }

    void LateUpdate()
    {
        if (player == null) player = GetComponentInParent<PlayerModular>();
        if (ownerInv == null) ownerInv = GetComponentInParent<PistolInventory>();

        bool has = (ownerInv != null && ownerInv.hasPistol);
        if (!has || player == null || muzzle == null || laser == null) return;

        muzzle.localPosition = new Vector3(muzzleLocalOffset.x, muzzleLocalOffset.y, 0f);

        Vector2 origin = muzzle.position;
        Vector2 aim = player.AimDirection;

        int floorLayer = LayerMask.NameToLayer("Floor");
        int laserMask = floorLayer >= 0 ? ~(1 << floorLayer) : ~0;

        RaycastHit2D hit = Physics2D.Raycast(origin, aim, laserRange, laserMask);
        Vector2 end = hit.collider != null
            ? hit.point - aim * laserEndPadding
            : origin + aim * laserRange;

        laser.SetPosition(0, new Vector3(origin.x, origin.y, laserZ));
        laser.SetPosition(1, new Vector3(end.x, end.y, laserZ));
        laser.startColor = laserColor;
        laser.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.15f);
        laser.startWidth = laserWidth;
        laser.endWidth = laserWidth * 0.3f;
        laser.sortingOrder = laserSortingOrder;
    }

    void Shoot()
    {
        currentAmmo--;
        lastShot = Time.time;

        if (player == null || muzzle == null) return;

        Vector2 origin = muzzle.position;
        Vector2 aim = player.AimDirection;

        PlayShotSound();

        if (bulletPrefab != null)
        {
            var b = Instantiate(bulletPrefab, new Vector3(origin.x, origin.y, bulletZ), Quaternion.identity);
            var bullet = b.GetComponent<PistolBullet>();
            if (bullet == null) bullet = b.AddComponent<PistolBullet>();

            bullet.zPos = bulletZ;
            bullet.speed = bulletSpeed;
            bullet.range = bulletRange;
            bullet.damage = bulletDamage;
            bullet.size = bulletSize;
            bullet.color = bulletColor;
            bullet.Init(aim);
        }

        player.ApplyRecoil(-aim, recoilForce);

        for (int i = 0; i < sparkCount; i++)
        {
            var go = new GameObject("Spark");
            go.transform.position = new Vector3(origin.x, origin.y, bulletZ);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LightSpriteGenerator.WhitePixel;
            sr.color = Color.Lerp(sparkColor1, sparkColor2, Random.value);
            sr.sortingOrder = laserSortingOrder + 10;
            go.transform.localScale = Vector3.one * sparkSize;

            var spark = go.AddComponent<PistolSpark>();
            spark.velocity = Quaternion.Euler(0f, 0f, Random.Range(-sparkSpread, sparkSpread)) * aim * sparkSpeed * Random.Range(0.7f, 1.3f);
            spark.life = sparkLifetime * Random.Range(0.7f, 1.3f);
        }

        Debug.Log("[Pistol] Выстрел, патронов: " + currentAmmo);
    }

    void PlayShotSound()
    {
        if (audioSource == null) return;
        if (shotSounds == null || shotSounds.Length == 0) return;

        int index = Random.Range(0, shotSounds.Length);

        if (avoidRepeatShot && shotSounds.Length > 1 && index == lastShotIndex)
            index = (index + 1) % shotSounds.Length;

        lastShotIndex = index;

        AudioClip clip = shotSounds[index];
        if (clip == null) return;

        audioSource.pitch = 1f + Random.Range(-shotPitchVariation, shotPitchVariation);
        audioSource.PlayOneShot(clip, shotVolume);
    }

    public void Equip() { if (sprite != null) sprite.enabled = true; if (laser != null) laser.enabled = true; }
    public void Unequip() { if (sprite != null) sprite.enabled = false; if (laser != null) laser.enabled = false; }
}