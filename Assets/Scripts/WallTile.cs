using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class WallTile : MonoBehaviour
{
    [Header("=== HP ===")]
    [Tooltip("Сколько попаданий держит тайл.")]
    public int maxHP = 2;
    public int currentHP = 2;

    [Header("=== ДЫРА В СПРАЙТЕ ===")]
    [Tooltip("Радиус дыры в юнитах.")]
    public float holeRadiusMin = 0.1f;
    public float holeRadiusMax = 0.2f;

    [Tooltip("Пробивать ли дыру в текстуре (не только HP).")]
    public bool punchTexture = true;

    [Header("=== РАЗЛЁТ ПИКСЕЛЕЙ ===")]
    public bool spawnDebris = true;
    public int debrisCountMin = 4;
    public int debrisCountMax = 8;
    public float debrisSpeed = 4f;
    public float debrisLifetime = 0.6f;
    public float debrisSize = 0.08f;

    [Header("=== ВИЗУАЛЬНЫЙ ЭФФЕКТ ===")]
    public bool flashOnHit = true;
    public Color flashColor = new Color(1f, 0.6f, 0.6f, 1f);
    public float flashDuration = 0.08f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private Texture2D workingTex;
    private SpriteRenderer sr;
    private BoxCollider2D col;
    private Color[] pixels;
    private int texW, texH;
    private float pixelsPerUnit;
    private Color originalColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();

        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("[WallTile] Нет спрайта на " + gameObject.name);
            enabled = false;
            return;
        }

        Sprite sprite = sr.sprite;
        Texture2D src = sprite.texture;

        if (src == null || !src.isReadable)
        {
            Debug.LogError("[WallTile] Read/Write Enabled на спрайте " + sprite.name);
            enabled = false;
            return;
        }

        texW = src.width;
        texH = src.height;
        pixelsPerUnit = sprite.pixelsPerUnit;

        // Рабочая текстура
        workingTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        workingTex.filterMode = FilterMode.Point;
        workingTex.SetPixels(src.GetPixels());
        workingTex.Apply();

        sr.sprite = Sprite.Create(workingTex,
            new Rect(0, 0, texW, texH),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);

        pixels = workingTex.GetPixels();
        originalColor = sr.color;

        currentHP = maxHP;
    }

    // ============================================================
    // ПОПАДАНИЕ
    // ============================================================
    public void TakeHit(Vector2 worldPoint)
    {
        if (currentHP <= 0) return;

        currentHP--;
        if (debugLogs)
            Debug.Log("[WallTile] " + gameObject.name + " HP: " + currentHP);

        // Дыра в текстуре в точке попадания
        if (punchTexture)
            PunchHoleAtPoint(worldPoint);

        // Разлёт пикселей
        if (spawnDebris)
            SpawnDebris(worldPoint);

        // Вспышка
        if (flashOnHit && sr != null)
        {
            sr.color = flashColor;
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), flashDuration);
        }

        if (currentHP <= 0)
            BreakTile();
    }

    void ResetColor()
    {
        if (sr != null) sr.color = originalColor;
    }

    void PunchHoleAtPoint(Vector2 worldPoint)
    {
        // Мировая точка → локальная внутри тайла
        Vector2 local = transform.InverseTransformPoint(worldPoint);

        Vector2 spriteSize = sr.sprite.bounds.size;
        Vector2 spriteMin = sr.sprite.bounds.min;

        // Относительные координаты внутри спрайта [0..1]
        float relX = (local.x - spriteMin.x) / spriteSize.x;
        float relY = (local.y - spriteMin.y) / spriteSize.y;

        int cx = Mathf.RoundToInt(relX * texW);
        int cy = Mathf.RoundToInt(relY * texH);

        float worldRadius = Random.Range(holeRadiusMin, holeRadiusMax);
        int radiusX = Mathf.Max(2, Mathf.RoundToInt(worldRadius / spriteSize.x * texW));
        int radiusY = Mathf.Max(2, Mathf.RoundToInt(worldRadius / spriteSize.y * texH));

        if (debugLogs)
            Debug.Log("[WallTile] Hole " + gameObject.name
                + " rel=(" + relX.ToString("F2") + "," + relY.ToString("F2") + ")"
                + " px=(" + cx + "," + cy + ") r=(" + radiusX + "," + radiusY + ")");

        for (int y = cy - radiusY; y <= cy + radiusY; y++)
        {
            for (int x = cx - radiusX; x <= cx + radiusX; x++)
            {
                if (x < 0 || x >= texW || y < 0 || y >= texH) continue;

                float dx = (x - cx) / (float)radiusX;
                float dy = (y - cy) / (float)radiusY;
                float dist = dx * dx + dy * dy;

                if (dist <= 1f)
                {
                    float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    if (noise > 0.3f || dist < 0.5f)
                        pixels[y * texW + x] = Color.clear;
                }
            }
        }

        workingTex.SetPixels(pixels);
        workingTex.Apply();
    }

    void SpawnDebris(Vector2 worldPoint)
    {
        int count = Random.Range(debrisCountMin, debrisCountMax + 1);
        Color debrisColor = GetPixelColorAt(worldPoint);

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Debris");
            go.transform.position = worldPoint + Random.insideUnitCircle * 0.15f;

            var dSr = go.AddComponent<SpriteRenderer>();
            dSr.sprite = LightSpriteGenerator.WhitePixel;
            dSr.color = debrisColor;
            dSr.sortingOrder = sr.sortingOrder + 5;
            go.transform.localScale = Vector3.one * debrisSize;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1.5f;
            rb.AddForce(Random.insideUnitCircle * debrisSpeed, ForceMode2D.Impulse);

            var script = go.AddComponent<WallDebris>();
            script.lifetime = debrisLifetime;
        }
    }

    Color GetPixelColorAt(Vector2 worldPoint)
    {
        Vector2 local = transform.InverseTransformPoint(worldPoint);
        Vector2 spriteSize = sr.sprite.bounds.size;
        Vector2 spriteMin = sr.sprite.bounds.min;

        float relX = (local.x - spriteMin.x) / spriteSize.x;
        float relY = (local.y - spriteMin.y) / spriteSize.y;

        int px = Mathf.Clamp(Mathf.RoundToInt(relX * texW), 0, texW - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(relY * texH), 0, texH - 1);

        Color c = pixels[py * texW + px];
        if (c.a < 0.1f) return Color.gray;

        return c;
    }

    void BreakTile()
    {
        if (debugLogs)
            Debug.Log("[WallTile] " + gameObject.name + " разрушен полностью");

        // Отключаем коллайдер — игрок проходит
        if (col != null) col.enabled = false;

        // Отключаем спрайт
        if (sr != null) sr.enabled = false;

        // Уничтожаем через время
        Destroy(gameObject, 2f);
    }
}