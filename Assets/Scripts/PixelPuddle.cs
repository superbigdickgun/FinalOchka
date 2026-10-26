using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class PixelPuddle : MonoBehaviour
{
    [Header("=== ЛУЖА (ПИКСЕЛИ) ===")]
    public int pixelSize = 4;
    public int puddleRadiusPixels = 64;
    public Color puddleColor = new Color(0.4f, 0f, 0f, 0.9f);
    [Range(0f, 0.5f)] public float colorVariation = 0.1f;
    [Range(0f, 1f)] public float edgeIrregularity = 0.3f;
    [Range(0f, 0.2f)] public float pixelJitter = 0.02f;

    [Header("=== РЯБЬ ===")]
    public float rippleRadius = 1f;
    public float rippleExpandSpeed = 3f;
    public float rippleLifetime = 1f;
    public Color rippleColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("=== ИГРОК ===")]
    public Transform player;

    [Header("=== ОПТИМИЗАЦИЯ ===")]
    public int textureSize = 256;
    public int maxRipples = 5;

    [Header("=== ГЕНЕРАЦИЯ ===")]
    public bool generateNow = false;

    private Texture2D texture;
    private SpriteRenderer sr;
    private Color[] pixels;
    private float[] rippleTimers = new float[10];
    private Vector2[] ripplePositions = new Vector2[10];
    private int currentRippleIndex = 0;
    private float lastRippleTime = 0f;
    private bool isInitialized = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // В редакторе генерируем сразу, если лужи нет
        if (!Application.isPlaying && sr != null && sr.sprite == null)
        {
            Generate();
        }
    }

    void Start()
    {
        // В Play Mode проверяем, что лужа сгенерирована
        if (!isInitialized)
        {
            Generate();
        }
    }

    void Update()
    {
        if (generateNow)
        {
            generateNow = false;
            Generate();
        }

        if (!Application.isPlaying) return;
        if (!isInitialized) return;

        if (player != null && pixels != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist < rippleRadius)
            {
                if (Time.time - lastRippleTime > 0.3f)
                {
                    lastRippleTime = Time.time;
                    CreateRipple(player.position);
                }
            }
        }
        UpdateRipples();
    }

    public void Generate()
    {
        sr = GetComponent<SpriteRenderer>();

        if (textureSize < puddleRadiusPixels * 2)
            textureSize = Mathf.NextPowerOfTwo(puddleRadiusPixels * 2);

        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        // ✅ ВАЖНО: создаём массив ДО вызова DrawPuddle
        pixels = new Color[textureSize * textureSize];

        DrawPuddle();

        texture.SetPixels(pixels);
        texture.Apply();

        sr.sprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 1f);

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        isInitialized = true;
        Debug.Log("[PixelPuddle] Лужа сгенерирована. Размер: " + textureSize + ". Слой: " + sr.sortingOrder);
    }

    void DrawPuddle()
    {
        if (pixels == null)
        {
            Debug.LogError("[PixelPuddle] pixels == null! Сначала вызовите Generate()");
            return;
        }

        float center = textureSize * 0.5f;
        float radius = puddleRadiusPixels;

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        for (int y = 0; y < textureSize; y += pixelSize)
        {
            for (int x = 0; x < textureSize; x += pixelSize)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                float threshold = 1f + (noise - 0.5f) * edgeIrregularity;

                if (dist < threshold)
                {
                    float jitterX = Random.Range(-pixelJitter, pixelJitter) * pixelSize;
                    float jitterY = Random.Range(-pixelJitter, pixelJitter) * pixelSize;

                    int px = Mathf.RoundToInt(x + jitterX);
                    int py = Mathf.RoundToInt(y + jitterY);

                    Color c = puddleColor;
                    c.r = Mathf.Clamp01(c.r + Random.Range(-colorVariation, colorVariation));
                    c.g = Mathf.Clamp01(c.g + Random.Range(-colorVariation, colorVariation));
                    c.b = Mathf.Clamp01(c.b + Random.Range(-colorVariation, colorVariation));

                    for (int oy = 0; oy < pixelSize; oy++)
                    {
                        for (int ox = 0; ox < pixelSize; ox++)
                        {
                            int tx = px + ox;
                            int ty = py + oy;
                            if (tx < 0 || tx >= textureSize || ty < 0 || ty >= textureSize) continue;
                            pixels[ty * textureSize + tx] = c;
                        }
                    }
                }
            }
        }
    }

    void CreateRipple(Vector2 worldPos)
    {
        if (pixels == null) return;

        int slot = currentRippleIndex % 10;
        currentRippleIndex++;

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        ripplePositions[slot] = new Vector2(
            (localPos.x / (puddleRadiusPixels / (float)textureSize * 2f) + 0.5f) * textureSize,
            (localPos.y / (puddleRadiusPixels / (float)textureSize * 2f) + 0.5f) * textureSize
        );
        rippleTimers[slot] = rippleLifetime;
    }

    void UpdateRipples()
    {
        if (pixels == null || texture == null) return;

        bool needsUpdate = false;

        for (int i = 0; i < 10; i++)
        {
            if (rippleTimers[i] > 0f)
            {
                rippleTimers[i] -= Time.deltaTime;
                needsUpdate = true;
            }
        }

        if (needsUpdate)
        {
            DrawPuddle();
            for (int i = 0; i < 10; i++)
            {
                if (rippleTimers[i] > 0f)
                    DrawRipple(i);
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }
    }

    void DrawRipple(int slot)
    {
        if (pixels == null) return;

        float timer = rippleTimers[slot];
        float alpha = timer / rippleLifetime;
        Vector2 center = ripplePositions[slot];
        float currentRadius = (rippleLifetime - timer) * rippleExpandSpeed * (textureSize / (puddleRadiusPixels * 2f));

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (Mathf.Abs(dist - currentRadius) < 3f)
                {
                    int index = y * textureSize + x;
                    pixels[index] = Color.Lerp(pixels[index], rippleColor, alpha * 0.5f);
                }
            }
        }
    }
}