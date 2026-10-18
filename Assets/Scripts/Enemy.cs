using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("=== ДВИЖЕНИЕ ===")]
    public float moveSpeed = 1.5f;
    public float stopDistance = 0.5f;

    [Header("=== HP ===")]
    public int maxHP = 10;
    public int currentHP = 10;

    [Header("=== ДЫРЫ В СПРАЙТЕ ===")]
    public float holeRadius = 0.1f;
    public bool makeHoles = true;

    [Header("=== ОТРЫВ ПРИ 0 HP ===")]
    public GameObject detachedPrefab;   // префаб, что отлетает при смерти

    [Header("=== ССЫЛКИ ===")]
    public Transform target;

    // Интерфейс для MultiPose/LimbAnimator
    public int bodyDir = 0;
    public bool isMoving = false;
    public int BodyDir => bodyDir;
    public bool IsMoving => isMoving;
    public bool IsRunning => false;
    public bool IsCrouching => false;
    public bool IsJumping => false;
    public float StepFrequency => 2f;
    public Vector2 AimDirection { get; private set; } = Vector2.right;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BoxCollider2D col;

    private Texture2D workTex;
    private Color[] pixels;
    private int width, height;
    private bool dead = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();

        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        InitTexture();
    }

    void InitTexture()
    {
        if (sr.sprite == null) { Debug.LogWarning("[Enemy] Нет спрайта!"); return; }

        var src = sr.sprite.texture;
        if (src == null || !src.isReadable)
        {
            Debug.LogWarning("[Enemy] Включи Read/Write Enabled на спрайте!");
            makeHoles = false;
            return;
        }

        width = src.width;
        height = src.height;

        workTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        workTex.filterMode = FilterMode.Point;
        workTex.SetPixels(src.GetPixels());
        workTex.Apply();

        sr.sprite = Sprite.Create(workTex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            sr.sprite.pixelsPerUnit);

        pixels = workTex.GetPixels();

        Debug.Log("[Enemy] Текстура готова: " + width + "x" + height);
    }

    void Update()
    {
        if (dead) { isMoving = false; return; }
        if (target == null) return;

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float dist = toTarget.magnitude;
        AimDirection = toTarget.normalized;

        if (Mathf.Abs(toTarget.y) >= Mathf.Abs(toTarget.x))
            bodyDir = toTarget.y > 0 ? 1 : 0;
        else
            bodyDir = toTarget.x > 0 ? 3 : 2;

        isMoving = dist > stopDistance;
    }

    void FixedUpdate()
    {
        if (dead) return;
        if (target == null || rb == null) return;

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        if (toTarget.magnitude <= stopDistance) return;

        rb.MovePosition(rb.position + toTarget.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    // ============================================================
    // УРОН
    // ============================================================
    public void TakeHit(Vector2 hitPoint)
    {
        if (dead) return;

        currentHP--;
        Debug.Log("[Enemy] HP: " + currentHP);

        if (makeHoles) MakeHole(hitPoint);

        if (currentHP <= 0) Die();
    }

    void MakeHole(Vector2 worldPoint)
    {
        if (workTex == null) return;

        Vector2 local = transform.InverseTransformPoint(worldPoint);
        float ppu = sr.sprite.pixelsPerUnit;

        int cx = Mathf.RoundToInt(local.x * ppu + width * 0.5f);
        int cy = Mathf.RoundToInt(local.y * ppu + height * 0.5f);

        int radius = Mathf.Max(2, Mathf.RoundToInt(holeRadius * ppu));

        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) continue;

                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                    pixels[y * width + x] = Color.clear;
            }
        }

        workTex.SetPixels(pixels);
        workTex.Apply();
    }

    void Die()
    {
        if (dead) return;
        dead = true;
        Debug.Log("[Enemy] Умер");

        // Спавн оторванной части (если задан префаб)
        if (detachedPrefab != null)
        {
            var go = Instantiate(detachedPrefab, transform.position, transform.rotation);
            var r = go.GetComponent<Rigidbody2D>();
            if (r == null) r = go.AddComponent<Rigidbody2D>();
            r.bodyType = RigidbodyType2D.Dynamic;
            r.gravityScale = 2f;
            r.AddForce(Random.insideUnitCircle * 3f, ForceMode2D.Impulse);
            r.AddTorque(Random.Range(-500f, 500f));
            Destroy(go, 10f);
        }

        // Отключить врага
        col.enabled = false;
        if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.3f);
    }
}